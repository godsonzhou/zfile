using System.Runtime.InteropServices;

namespace zfile
{
    public class WinNetExecuteOperation : FileSourceExecuteOperation
    {
        private readonly IWinNetFileSource _winNetFileSource;

        public WinNetExecuteOperation(
            IFileSource targetFileSource,
            ref FileEntry executableFile,
            string currentPath,
            string verb)
            : base(targetFileSource, executableFile, currentPath, verb)
        {
            _winNetFileSource = targetFileSource as IWinNetFileSource;
        }

        protected override void MainExecute()
        {
            ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
            ResultString = Path.Combine("", ExecutableFile.FullPath);

            if (ResultString.StartsWith("\\\\"))
            {
                var fileName = ResultString;
                try
                {
                    var bufferSize = 4096;
                    var buffer = new byte[bufferSize];
                    var netResource = new NetResource
                    {
                        Scope = ResourceScope.GlobalNet,
                        Type = ResourceType.Any,
                        RemoteName = fileName,
                        Provider = _winNetFileSource.ProviderName
                    };

                    var result = WNetAddConnection2(netResource, null, null, ConnectFlags.Interactive);
                    if (result != 0) return;

                    result = WNetGetResourceInformation(netResource, buffer, ref bufferSize, out var system);
                    if (result != 0) return;

                    var resourceInfo = Marshal.PtrToStructure<NetResource>(buffer);
                    if (resourceInfo.Type == ResourceType.Print)
                    {
                        if (ShellExecute(IntPtr.Zero, "open", resourceInfo.RemoteName, null, null, ShowWindowCommands.Show) > 32)
                            ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                        return;
                    }
                }
                finally
                {
                    if (ResultString == null)
                        ResultString = GetLastError();
                }
            }

            ExecuteOperationResult = FileSourceExecuteOperationResult.SymLink;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, ConnectFlags flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetResourceInformation(NetResource netResource, byte[] buffer, ref int bufferSize, out string system);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ShellExecute(IntPtr hwnd, string operation, string file, string parameters, string directory, ShowWindowCommands showCmd);

        private static string GetLastError()
        {
            return Marshal.GetLastWin32Error().ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NetResource
    {
        public ResourceScope Scope;
        public ResourceType Type;
        public ResourceDisplayType DisplayType;
        public ResourceUsage Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }

    public enum ResourceScope
    {
        Connected = 1,
        GlobalNet,
        Remembered,
        Recent,
        Context
    }

    public enum ResourceType
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8
    }

    public enum ResourceDisplayType
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        ShareAdmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        NdsContainer = 0x0b
    }

    public enum ResourceUsage
    {
        Connectable = 0x00000001,
        Container = 0x00000002,
        NoLocalDevice = 0x00000004,
        Sibling = 0x00000008,
        Attached = 0x00000010,
        All = (Connectable | Container | Attached),
        Reserved = unchecked((int)0x80000000)
    }

    [Flags]
    public enum ConnectFlags
    {
        Interactive = 0x00000008,
        Prompt = 0x00000010,
        Redirect = 0x00000080,
        UpdateProfile = 0x00000001,
        CommandLine = 0x00000800,
        CmdSaveCred = 0x00001000,
        CredReset = 0x00002000
    }

    public enum ShowWindowCommands
    {
        Hide = 0,
        Normal = 1,
        ShowMinimized = 2,
        Maximize = 3,
        ShowMaximized = 3,
        ShowNoActivate = 4,
        Show = 5,
        Minimize = 6,
        ShowMinNoActive = 7,
        ShowNA = 8,
        Restore = 9,
        ShowDefault = 10,
        ForceMinimize = 11
    }
} 