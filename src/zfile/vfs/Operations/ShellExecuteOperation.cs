using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class ShellExecuteOperation : FileSourceExecuteOperation
    {
        private IShellFileSource shellFileSource;

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
					var type = typeof(IShellFolder2);
					w32.OleCheck(API.SHBindToParent(pidl, ref typeof(IShellFolder2).GUID, out folder, out pidl));
                    IContextMenu menu;
                    w32.OleCheck(folder.GetUIObjectOf(MainForm._Handle, 1, new[] { pidl }, typeof(IContextMenu).GUID, IntPtr.Zero, out menu));
                    if (menu != null)
                    {
                        var cmici = new CMINVOKECOMMANDINFOEX
                        {
                            cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFO)),
                            hwnd = MainForm._Handle,
                            lpVerb = Verb,
                            nShow = (int)SW.SHOWNORMAL
                        };
                        w32.OleCheck(menu.InvokeCommand(ref cmici));
                    }
                }
                catch
                {
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
                }
            }
            else if (shellFileSource.IsPathAtRoot(CurrentPath))
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
                    fMask = SEE_MASK_IDLIST
                };

                if (API.ShellExecuteEx(ref execInfo))
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                else
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
            }
        }

      
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