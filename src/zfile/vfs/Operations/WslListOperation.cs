using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class WslListOperation : FileSystemListOperation
    {
        private readonly IWinNetFileSource _winNetFileSource;

        public WslListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries(path);
            _winNetFileSource = fileSource as IWinNetFileSource;
        }

        private void LinuxEnum()
        {
            try
            {
                var desktopFolder = GetDesktopFolder();
                var path = System.IO.Path.Combine(Path, "");
                var networkPidl = ParseDisplayName(desktopFolder, path);
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
                            file.Attributes = FileAttributes.Directory;
                            file.FullPath = GetDisplayName(folder, pidl, (uint)(SHGDN.FORPARSING | SHGDN.FORADDRESSBAR));

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
                //ShowError(thread, e.Message);
				Debug.Print(e.Message);
            }
        }

        protected override void MainExecute()
        {
            Files.Clear();
            if (_winNetFileSource.IsNetworkPath(Path))
                LinuxEnum();
            else
                base.MainExecute();
        }

        private static IShellFolder GetDesktopFolder()
        {
            // Implementation of SHGetDesktopFolder
            return w32.GetDesktopFolder(out _);
        }

        private static IntPtr ParseDisplayName(IShellFolder folder, string path)
        {
            // Implementation of ParseDisplayName
			if (folder == null)
			{
				uint attr = 0;
				folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, path, out var _, out var _, ref attr);
			}
            return IntPtr.Zero;
        }

        private static IShellFolder BindToObject(IShellFolder folder, IntPtr pidl)
        {
			// Implementation of BindToObject
			Guid iid = typeof(IShellFolder).GUID;
			folder.BindToObject(pidl, IntPtr.Zero, ref iid, out IShellFolder subFolder);
			return subFolder;
		}
		private static IShellFolder BindToObject(IShellFolder folder, string path)
		{
			// Implementation of BindToObject
			return null;
        }

        private static IEnumIDList EnumObjects(IShellFolder folder)
        {
            // Implementation of EnumObjects
			folder.EnumObjects(IntPtr.Zero, (SHCONTF.FOLDERS | SHCONTF.STORAGE), out var EnumPtr);
			if (EnumPtr == IntPtr.Zero)  //如果node=程序和功能,则EnumPtr=0，直接返回
				return null;

			return (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
        }

        private static string GetDisplayName(IShellFolder folder, IntPtr pidl, uint flags)
        {
            // Implementation of GetDisplayName
			return w32.GetDisplayName(folder, pidl, (SHGDN)flags);
        }

        private static void ShowError(Thread thread, string message)
        {
            // Implementation of error display
        }
    }
} 