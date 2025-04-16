using System.Runtime.InteropServices;

namespace zfile
{
    public class WinNetListOperation : FileSystemListOperation
    {
        private readonly IWinNetFileSource _winNetFileSource;

        public WinNetListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileList(path);
            _winNetFileSource = fileSource as IWinNetFileSource;
        }

        private bool Connect()
        {
            var abortMethod = Thread.CurrentThread.ManagedThreadId == 1 ? null : (Action)CheckOperationState;
            string serverPath;
            if (_winNetFileSource.IsNetworkPath(Path))
                serverPath = Path.TrimEnd('\\');
            else
            {
                var index = Path.IndexOf('\\', Path.IndexOf('\\', Path.IndexOf('\\') + 1) + 1);
                if (index == -1) index = int.MaxValue;
                serverPath = Path.Substring(0, index - 1);
            }

            var result = NetworkThread.Connect(null, serverPath, ResourceType.Any, abortMethod);
            if (result != 0)
            {
                if (result == ERROR_CANCELLED)
                    RaiseAbortOperation();
                ShowError(Thread, GetLastError());
                return false;
            }
            return true;
        }

        private void WorkgroupEnum()
        {
            try
            {
                var netResource = new NetResource
                {
                    Scope = ResourceScope.GlobalNet,
                    Type = ResourceType.Any,
                    Provider = _winNetFileSource.ProviderName
                };

                if (!_winNetFileSource.IsPathAtRoot(Path))
                {
                    var filePath = Path.TrimEnd('\\');
                    netResource.RemoteName = filePath.TrimStart('\\');
                }

                var handle = IntPtr.Zero;
                var result = WNetOpenEnum(ResourceScope.GlobalNet, ResourceType.Any, 0, ref netResource, out handle);
                if (result != 0) return;

                var bufferSize = 0x100000;
                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    var count = uint.MaxValue;
                    result = WNetEnumResource(handle, ref count, buffer, ref bufferSize);
                    if (result == ERROR_NO_MORE_ITEMS) return;
                    if (result != 0) return;

                    var netResourceList = buffer;
                    for (var i = 0; i < count; i++)
                    {
                        CheckOperationState();
                        var file = WinNetFileSource.CreateFile(Path);
                        var resource = Marshal.PtrToStructure<NetResource>(netResourceList);
                        file.FullPath = resource.RemoteName;
                        file.CommentProperty.Value = resource.Comment;
                        if (resource.DisplayType == ResourceDisplayType.Share)
                            file.Attributes = FileAttributes.Directory;
                        Files.Add(file);
                        netResourceList += Marshal.SizeOf<NetResource>();
                    }
                }
                finally
                {
                    if (handle != IntPtr.Zero)
                        WNetCloseEnum(handle);
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception e)
            {
                ShowError(Thread, e.Message);
            }
        }

        private void ShareEnum()
        {
            if (!Connect()) return;

            var serverPath = Path.TrimEnd('\\');
            var buffer = IntPtr.Zero;
            try
            {
                uint entriesRead;
                uint totalEntries;
                var result = NetShareEnum(serverPath, 1, out buffer, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, IntPtr.Zero);
                while (result == ERROR_SUCCESS || result == ERROR_MORE_DATA)
                {
                    var shareInfo = buffer;
                    for (var i = 0; i < entriesRead; i++)
                    {
                        CheckOperationState();
                        var file = WinNetFileSource.CreateFile(Path);
                        var info = Marshal.PtrToStructure<ShareInfo1>(shareInfo);
                        file.Name = info.NetName;
                        file.CommentProperty.Value = info.Remark;
                        switch (info.Type & 0xFF)
                        {
                            case ShareType.DiskTree:
                                file.Attributes = FileAttributes.Directory;
                                break;
                            case ShareType.Ipc:
                                file.Attributes = FileAttributes.System;
                                break;
                        }
                        if ((info.Type & ShareType.Special) == ShareType.Special)
                            file.Attributes |= FileAttributes.Hidden;
                        if (string.Equals(info.NetName, "FAX$", StringComparison.OrdinalIgnoreCase))
                            file.Attributes |= FileAttributes.Hidden;
                        if (string.Equals(info.NetName, "PRINT$", StringComparison.OrdinalIgnoreCase))
                            file.Attributes |= FileAttributes.Hidden;
                        Files.Add(file);
                        shareInfo += Marshal.SizeOf<ShareInfo1>();
                    }
                    NetApiBufferFree(buffer);
                    if (result != ERROR_MORE_DATA) break;
                    result = NetShareEnum(serverPath, 1, out buffer, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, IntPtr.Zero);
                }
                if (result != ERROR_SUCCESS)
                    ShowError(Thread, GetLastError());
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    NetApiBufferFree(buffer);
            }
        }

        private void ShellEnum()
        {
            try
            {
                var desktopFolder = GetDesktopFolder();
                var networkPidl = GetFolderLocation(CSIDL_NETWORK);
                try
                {
                    var folder = BindToObject(desktopFolder, networkPidl);
                    var enumIdList = EnumObjects(folder);

                    while (enumIdList.Next(1, out var pidl, out var numIds) == 0)
                    {
                        try
                        {
                            CheckOperationState();

                            var file = WinNetFileSource.CreateFile(Path);
                            file.FullPath = GetDisplayName(folder, pidl, SHGDN_FORPARSING | SHGDN_FORADDRESSBAR);

                            Files.Add(file);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pidl);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(networkPidl);
                }
            }
            catch (Exception e)
            {
                ShowError(Thread, e.Message);
            }
        }

        public override void MainExecute()
        {
            Files.Clear();
            if (!_winNetFileSource.IsNetworkPath(Path))
            {
                if (Connect())
                    base.MainExecute();
            }
            else
            {
                if (!_winNetFileSource.IsPathAtRoot(Path) && Path.StartsWith("\\\\"))
                    ShareEnum();
                else if (!_winNetFileSource.Samba1)
                    ShellEnum();
                else
                    WorkgroupEnum();
            }
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetOpenEnum(ResourceScope scope, ResourceType type, uint usage, ref NetResource netResource, out IntPtr handle);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetEnumResource(IntPtr handle, ref uint count, IntPtr buffer, ref int bufferSize);

        [DllImport("mpr.dll")]
        private static extern int WNetCloseEnum(IntPtr handle);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetShareEnum(string serverName, int level, out IntPtr buffer, int prefMaxLen, out uint entriesRead, out uint totalEntries, IntPtr resumeHandle);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        private static IShellFolder GetDesktopFolder()
        {
            // Implementation of SHGetDesktopFolder
            return null;
        }

        private static IntPtr GetFolderLocation(int csidl)
        {
            // Implementation of SHGetFolderLocation
            return IntPtr.Zero;
        }

        private static IShellFolder BindToObject(IShellFolder folder, IntPtr pidl)
        {
            // Implementation of BindToObject
            return null;
        }

        private static IEnumIDList EnumObjects(IShellFolder folder)
        {
            // Implementation of EnumObjects
            return null;
        }

        private static string GetDisplayName(IShellFolder folder, IntPtr pidl, uint flags)
        {
            // Implementation of GetDisplayName
            return string.Empty;
        }

        private static string GetLastError()
        {
            return Marshal.GetLastWin32Error().ToString();
        }

        private static void ShowError(Thread thread, string message)
        {
            // Implementation of error display
        }

        private const int ERROR_SUCCESS = 0;
        private const int ERROR_MORE_DATA = 234;
        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_CANCELLED = 1223;
        private const int MAX_PREFERRED_LENGTH = -1;
        private const int CSIDL_NETWORK = 0x0012;
        private const uint SHCONTF_FOLDERS = 0x0020;
        private const uint SHCONTF_NONFOLDERS = 0x0040;
        private const uint SHCONTF_INCLUDEHIDDEN = 0x0080;
        private const uint SHGDN_FORPARSING = 0x8000;
        private const uint SHGDN_FORADDRESSBAR = 0x4000;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ShareInfo1
    {
        public string NetName;
        public ShareType Type;
        public string Remark;
    }

    public enum ShareType : uint
    {
        DiskTree = 0x00000000,
        PrintQueue = 0x00000001,
        Device = 0x00000002,
        Ipc = 0x00000003,
 