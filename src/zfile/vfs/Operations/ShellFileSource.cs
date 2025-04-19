using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
	public interface IShellFileSource : IVirtualFileSource
	{
		bool SetCurrentWorkingDirectory(string newDir);
		int CreateFolder(IShellFolder2 parent, string newDir);
		int FindFolder(string path, out IShellFolder2 folder);
		int FindObject(string obj, out IntPtr pidl);
		int FindObject(IShellFolder2 parent, string name, out IntPtr pidl);
	}
	public class ShellFileSource : VirtualFileSource, IShellFileSource
	{
		private string _rootPath;
		private IntPtr _drives;
		private IShellFolder2 _rootFolder;
		private IShellFolder _desktopFolder;

		public ShellFileSource()
		{
			w32.OleCheck(API.SHGetDesktopFolder(out _desktopFolder));
			w32.OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out _drives));

			// Use IShellFolder interface for BindToObject, then cast to IShellFolder2
			IShellFolder tempFolder;
			_desktopFolder.BindToObject(_drives, IntPtr.Zero, ref Guids.IID_IShellFolder, out tempFolder);
			_rootFolder = (IShellFolder2)tempFolder;
			_rootPath = w32.GetDisplayName(_desktopFolder, _drives, SHGDN.INFOLDER);

			OperationsClasses[FileSourceOperationType.Move] = typeof(ShellMoveOperation);
			OperationsClasses[FileSourceOperationType.Copy] = typeof(ShellCopyOperation);
			OperationsClasses[FileSourceOperationType.CopyIn] = typeof(ShellCopyInOperation);
			OperationsClasses[FileSourceOperationType.CopyOut] = typeof(ShellCopyOutOperation);
		}

		~ShellFileSource()
		{
			Marshal.FreeCoTaskMem(_drives);
		}

		public override bool SetCurrentWorkingDirectory(string newDir)
		{
			return true;
		}

		public static new bool IsSupportedPath(string path)
		{
			return path.StartsWith(Path.DirectorySeparatorChar + Path.DirectorySeparatorChar +
								 Path.DirectorySeparatorChar + RootName);
		}

		public static new FileEntry CreateFile(string path)
		{
			var file = new FileEntry(path);
			file.AttributesProperty = new FileAttributesProperty();
			file.SizeProperty = new FileSizeProperty();
			file.ModificationTimeProperty = new FileModificationDateTimeProperty();
			file.CreationTimeProperty = new FileCreationDateTimeProperty();
			file.LinkProperty = new FileShellProperty();
			file.CommentProperty = new FileCommentProperty();
			return file;
		}

		public static new bool GetMainIcon(out string path)
		{
			path = "%SystemRoot%\\System32\\shell32.dll,15";
			return true;
		}

		public static string RootName
		{
			get
			{
				IntPtr drivesPidl;
				IShellFolder desktopFolder;
				w32.OleCheck(API.SHGetDesktopFolder(out desktopFolder));
				w32.OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out drivesPidl));
				try
				{
					return w32.GetDisplayName(desktopFolder, drivesPidl, SHGDN.INFOLDER);
				}
				finally
				{
					Marshal.FreeCoTaskMem(drivesPidl);
				}
			}
		}

		public static void ListDrives(DrivesList drivesList, bool upperCase)
		{
			const uint SFGAOF_DEFAULT = (uint)(SFGAO.FILESYSTEM | SFGAO.FOLDER);
			string[] upperLetters = { "Ù", "Ú", "Û", "Ü", "Ũ", "Ū", "Ŭ", "Ů", "Ű", "Ų", "Ȕ", "Ȗ" };
			string[] lowerLetters = { "ù", "ú", "û", "ü", "ũ", "ū", "ŭ", "ů", "ű", "ų", "ȕ", "ȗ" };

			IntPtr drivesPidl;
			IShellFolder desktopFolder;
			w32.OleCheck(API.SHGetDesktopFolder(out desktopFolder));
			w32.OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out drivesPidl));
			try
			{
				// Use IShellFolder interface for BindToObject, then cast to IShellFolder2
				IShellFolder tempFolder;
				desktopFolder.BindToObject(drivesPidl, IntPtr.Zero, ref Guids.IID_IShellFolder, out tempFolder);
				IShellFolder2 folder = (IShellFolder2)tempFolder;
				IEnumIDList enumIdList;
				w32.OleCheck(folder.EnumObjects(IntPtr.Zero, (uint)(SHCONTF.FOLDERS | SHCONTF.STORAGE), out enumIdList));
				string rootPath = "\\\\\\" + w32.GetDisplayName(desktopFolder, drivesPidl, SHGDN.INFOLDER);

				int index = 0;
				IntPtr pidl = IntPtr.Zero;
				uint numIds;
				while (enumIdList.Next(1, out pidl, out numIds) == 0)
				{
					try
					{
						uint rgfInOut = SFGAOF_DEFAULT;
						IntPtr[] pidlArray = new IntPtr[] { pidl };
						if (folder.GetAttributesOf(1, pidlArray, ref rgfInOut) == 0)
						{
							if ((SFGAOF_DEFAULT & rgfInOut) == (uint)SFGAO.FOLDER)
							{
								string deviceId = w32.GetDisplayName2(folder, pidl, SHGDN.FORPARSING);
								if (deviceId.Contains("\\\\?\\usb"))
								{
									var drive = new Drive();
									if (upperCase)
									{
										drive.DisplayName = upperLetters[index];
									}
									else
									{
										drive.DisplayName = lowerLetters[index];
									}
									drive.IsMounted = true;
									drive.DeviceId = deviceId;
									drive.DriveType = DriveType.Special;
									drive.IsMediaAvailable = true;
									drive.DriveLabel = w32.GetDisplayName2(folder, pidl, SHGDN.INFOLDER);
									drive.Path = rootPath + Path.DirectorySeparatorChar + drive.DriveLabel;
									drivesList.Add(drive);
									index++;
									if (index > lowerLetters.Length - 1) break;
								}
							}
						}
					}
					finally
					{
						Marshal.FreeCoTaskMem(pidl);
					}
				}
			}
			finally
			{
				Marshal.FreeCoTaskMem(drivesPidl);
			}
		}

		public int CreateFolder(IShellFolder2 parent, string newDir)
		{
			throw new NotImplementedException();
		}

		public int FindFolder(string path, out IShellFolder2 folder)
		{
			throw new NotImplementedException();
		}

		public int FindObject(string obj, out nint pidl)
		{
			throw new NotImplementedException();
		}

		public int FindObject(IShellFolder2 parent, string name, out nint pidl)
		{
			throw new NotImplementedException();
		}

		// ... 其他接口实现 ...
	}
}