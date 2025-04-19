using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class ShellExecuteOperation : FileSourceExecuteOperation
    {
        private readonly IShellFileSource? shellFileSource;

        public ShellExecuteOperation(IFileSource targetFileSource,
                                   FileEntry executableFile,
                                   string currentPath,
                                   string verb)
            : base(targetFileSource, executableFile, currentPath, verb)
        {
            shellFileSource = targetFileSource as IShellFileSource;
        }

        protected override void MainExecute()
        {
            if (Verb == "properties")
            {
                try
                {
                    IntPtr pidl = ((FileShellProperty)ExecutableFile.LinkProperty).Item;
                    IShellFolder2 folder;
                    var Guid = typeof(IShellFolder2).GUID;
                    object? folderObj = null;
                    w32.OleCheck(API.SHBindToParent(pidl, ref Guid, out folderObj, out pidl));
                    folder = (IShellFolder2)folderObj!;
                    IntPtr menuPtr = IntPtr.Zero;
                    var contextMenuGuid = typeof(IContextMenu).GUID;
                    folder.GetUIObjectOf(MainForm._Handle, 1, new[] { pidl }, ref contextMenuGuid, IntPtr.Zero, out menuPtr);
                    IContextMenu? menu = null;
                    if (menuPtr != IntPtr.Zero)
                    {
                        menu = (IContextMenu)Marshal.GetObjectForIUnknown(menuPtr);
                    }
                    if (menu != null)
                    {
                        var cmici = new CMINVOKECOMMANDINFOEX
                        {
                            cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFO)),
                            hwnd = MainForm._Handle,
                            lpVerb = Marshal.StringToHGlobalAnsi(Verb),
                            nShow = (int)SW.SHOWNORMAL
                        };
                        menu.InvokeCommand(ref cmici);
                        Marshal.FreeHGlobal(cmici.lpVerb);
                    }
                }
                catch
                {
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
                }
            }
            else if (shellFileSource != null && shellFileSource.IsPathAtRoot(CurrentPath))
            {
                ResultString = ExecutableFile.LinkProperty.LinkTarget;
                ExecuteOperationResult = FileSourceExecuteOperationResult.SymLink;
            }
            else
            {
                var execInfo = new SHELLEXECUTEINFO
                {
                    cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
                    lpIDList = ((FileShellProperty)ExecutableFile.LinkProperty).Item,
                    fMask = 0x00000100 // SEE_MASK_IDLIST
                };

                if (ShellExecuteEx(ref execInfo))
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                else
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CMINVOKECOMMANDINFO
    {
        public int cbSize;
        public int fMask;
        public IntPtr hwnd;
        public string lpVerb;
        public string lpParameters;
        public string lpDirectory;
        public int nShow;
        public int dwHotKey;
        public IntPtr hIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public int fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public int dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }


}