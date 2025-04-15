using System;
using System.Runtime.InteropServices;

namespace FileSystemOperations
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
                var buffer = new byte[bufferSize];
                var resultCode = WNetGetResourceParent(netResource, buffer, ref bufferSize);
                if (resultCode != 0)
                    ShowError(GetLastError());
                else
                {
                    var parentPath = Marshal.PtrToStructure<NetResource>(buffer);
                    result = Path.Combine("", parentPath.RemoteName.TrimEnd('\\'));
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
                return true;
            return Directory.SetCurrentDirectory(newDir);
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

        public override IFileSourceOperation CreateListOperation(string targetPath)
        {
            return new WinNetListOperation(this, targetPath);
        }

        public override IFileSourceOperation CreateCopyOperation(ref FileList sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCopyOperation(ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, ref FileList sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCopyInOperation(sourceFileSource, ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, ref FileList sourceFiles, string targetPath)
        {
            if (IsNetworkPath(sourceFiles.Path))
                return null;
            return base.CreateCopyOutOperation(targetFileSource, ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateMoveOperation(ref FileList sourceFiles, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateMoveOperation(ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateDeleteOperation(ref FileList filesToDelete)
        {
            if (IsNetworkPath(filesToDelete.Path))
                return null;
            return base.CreateDeleteOperation(ref filesToDelete);
        }

        public override IFileSourceOperation CreateWipeOperation(ref FileList filesToWipe)
        {
            if (IsNetworkPath(filesToWipe.Path))
                return null;
            return base.CreateWipeOperation(ref filesToWipe);
        }

        public override IFileSourceOperation CreateSplitOperation(ref FileInfo sourceFile, string targetPath)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateSplitOperation(ref sourceFile, targetPath);
        }

        public override IFileSourceOperation CreateCombineOperation(ref FileList sourceFiles, string targetFile)
        {
            if (IsNetworkPath(targetFile))
                return null;
            return base.CreateCombineOperation(ref sourceFiles, targetFile);
        }

        public override IFileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
        {
            if (IsNetworkPath(directoryPath))
                return null;
            return base.CreateCreateDirectoryOperation(basePath, directoryPath);
        }

        public override IFileSourceOperation CreateExecuteOperation(ref FileInfo executableFile, string basePath, string verb)
        {
            return new WinNetExecuteOperation(this, ref executableFile, basePath, verb);
        }

        public override IFileSourceOperation CreateCalcChecksumOperation(ref FileList files, string targetPath, string targetMask)
        {
            if (IsNetworkPath(targetPath))
                return null;
            return base.CreateCalcChecksumOperation(ref files, targetPath, targetMask);
        }

        public override IFileSourceOperation CreateCalcStatisticsOperation(ref FileList files)
        {
            if (IsNetworkPath(files.Path))
                return null;
            return base.CreateCalcStatisticsOperation(ref files);
        }

        public override IFileSourceOperation CreateSetFilePropertyOperation(ref FileList targetFiles, ref FileProperties newProperties)
        {
            if (IsNetworkPath(targetFiles.Path))
                return null;
            return base.CreateSetFilePropertyOperation(ref targetFiles, ref newProperties);
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetProviderName(int netType, char[] providerName, ref int bufferSize);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetResourceParent(NetResource netResource, byte[] buffer, ref int bufferSize);

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