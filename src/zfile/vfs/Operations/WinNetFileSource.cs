using System.Runtime.InteropServices;

namespace zfile
{
    public interface IWinNetFileSource : IVirtualFileSource
    {
        bool Samba1 { get; }
        string ProviderName { get; }
        bool IsNetworkPath(string path);
    }

    public class WinNetFileSource : FileSystemFileSource, IWinNetFileSource
    {
        private bool _samba1;
        private readonly char[] _providerName = new char[MAX_PATH];

        public bool Samba1 => _samba1;
        public string ProviderName => new string(_providerName).TrimEnd('\0');

        public WinNetFileSource()
        {
            var bufferSize = MAX_PATH;
            if (WNetGetProviderName(WNNC_NET_LANMAN, _providerName, ref bufferSize) != 0)
                throw new Exception(GetLastError());

            _samba1 = Environment.OSVersion.Version.Major < 6 || GetServiceStatus("mrxsmb10") == ServiceStatus.Running;
        }

        public override string GetParentDir(string path)
        {
            var result = GetRootDir();
            if (path.StartsWith("\\\\"))
            {
                if (!_samba1)
                {
                    if (IsNetworkPath(path))
                        result = Path.GetDirectoryName(path).TrimEnd('\\');
                    else
                        result = Path.GetDirectoryName(path);
                    return result;
                }

                var filePath = path.TrimEnd('\\');
                var netResource = new NetResource
                {
                    Scope = ResourceScope.GlobalNet,
                    Type = ResourceType.Disk,
                    DisplayType = ResourceDisplayType.Server,
                    Usage = ResourceUsage.Container,
                    RemoteName = filePath,
                    Provider = ProviderName
                };

                var bufferSize = 4096;
                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    var resultCode = WNetGetResourceParent(netResource, buffer, ref bufferSize);
                    if (resultCode != 0)
                        ShowError(GetLastError());
                    else
                    {
                        var parentPath = Marshal.PtrToStructure<NetResource>(buffer);
                        result = Path.Combine("", parentPath.RemoteName.TrimEnd('\\'));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return result;
        }

        public override bool IsPathAtRoot(string path)
        {
            return string.IsNullOrEmpty(Path.GetDirectoryName(path));
        }

        public override string GetRootDir(string path)
        {
            return "\\";
        }

        public override string GetRootDir()
        {
            return "\\";
        }

        public override bool GetFreeSpace(string path, out long freeSize, out long totalSize)
        {
            if (IsNetworkPath(path))
            {
                freeSize = 0;
                totalSize = 0;
                return false;
            }
            return base.GetFreeSpace(path, out freeSize, out totalSize);
        }

        public override FileSourceProperties GetProperties()
        {
            return base.GetProperties() | FileSourceProperties.Virtual & ~FileSourceProperties.NoneParent;
        }

        public bool IsNetworkPath(string path)
        {
            return path.Count(c => c == '\\') < 3;
        }

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            if (IsNetworkPath(newDir))
                return false;
            Directory.SetCurrentDirectory(newDir);
            return true;
        }

        public static bool IsSupportedPath(string path)
        {
            return path.StartsWith("\\\\");
        }

        public static bool GetMainIcon(out string path)
        {
            path = "%SystemRoot%\\System32\\shell32.dll,17";
            return true;
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            return new WinNetListOperation(this, targetPath);
        }

        public override FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCopyOperation(sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCopyInOperation(sourceFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
        {
            if (IsNetworkPath(sourceFiles.Path))
                return null;
            return base.CreateCopyOutOperation(targetFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateMoveOperation(sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
        {
            if (IsNetworkPath(filesToDelete.Path))
                return null;
            return base.CreateDeleteOperation(filesToDelete);
        }

        public override FileSourceOperation CreateWipeOperation(FileEntries filesToWipe)
        {
            if (IsNetworkPath(filesToWipe.Path))
                return null;
            return base.CreateWipeOperation(filesToWipe);
        }

        public override FileSourceOperation CreateSplitOperation(FileEntry sourceFile, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateSplitOperation(sourceFile, targetPath);
        }

        public override FileSourceOperation CreateCombineOperation(FileEntries sourceFiles, string targetFile)
        {
            if (IsNetworkPath(targetFile))
                return null;
            return base.CreateCombineOperation(sourceFiles, targetFile);
        }

        public override FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
        {
            if (IsNetworkPath(directoryPath))
                return null;
            return base.CreateCreateDirectoryOperation(basePath, directoryPath);
        }

        public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
        {
            return new WinNetExecuteOperation(this, ref executableFile, basePath, verb);
        }

        public override FileSourceOperation CreateCalcChecksumOperation(FileEntries files, string targetPath, string targetMask)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCalcChecksumOperation(files, targetPath, targetMask);
        }

        public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
        {
            if (IsNetworkPath(files.Path))
                return null;
            return base.CreateCalcStatisticsOperation(files);
        }

        public override FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperties newProperties)
        {
            if (IsNetworkPath(targetFiles.Path))
                return null;
            return base.CreateSetFilePropertyOperation(targetFiles, newProperties);
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetProviderName(int netType, char[] providerName, ref int bufferSize);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetResourceParent(NetResource netResource, IntPtr buffer, ref int bufferSize);

        private static string GetLastError()
        {
            return Marshal.GetLastWin32Error().ToString();
        }

        private static void ShowError(string message)
        {
            // Implementation of error display
        }

        private static ServiceStatus GetServiceStatus(string serviceName)
        {
            // Implementation of service status check
            return ServiceStatus.Stopped;
        }

        private const int MAX_PATH = 260;
        private const int WNNC_NET_LANMAN = 0x00020000;
    }

    public enum ServiceStatus
    {
        Stopped = 1,
        StartPending = 2,
        StopPending = 3,
        Running = 4,
        ContinuePending = 5,
        PausePending = 6,
        Paused = 7
    }
}