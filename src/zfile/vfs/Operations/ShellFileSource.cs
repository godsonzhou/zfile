using FileSystemOperations;
using System.Runtime.InteropServices;
using WinShell;
namespace Zfile.FileSources
{
	public class ShellFileSource : VirtualFileSource, IShellFileSource
	{
		private string _rootPath;
		private IntPtr _drives;
		private IShellFolder2 _rootFolder;
		private IShellFolder _desktopFolder;

		public ShellFileSource()
		{
			OleCheck(API.SHGetDesktopFolder(out _desktopFolder));
			OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out _drives));
			OleCheck(_desktopFolder.BindToObject(_drives, null, ref IID_IShellFolder2, out _rootFolder));
			_rootPath = GetDisplayName(_desktopFolder, _drives, SHGDN.INFOLDER);

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

		public static bool IsSupportedPath(string path)
		{
			return path.StartsWith(Path.DirectorySeparatorChar + Path.DirectorySeparatorChar +
								 Path.DirectorySeparatorChar + RootName);
		}

		public static FileEntry CreateFile(string path)
		{
			var file = new FileEntry(path);
			file.AttributesProperty = new FileAttributesProperty();
			file.SizeProperty = new FileSizeProperty();
			file.ModificationTimeProperty = new FileModificationDateTimeProperty();
			file.CreationTimeProperty = new FileCreationDateTimeProperty();
			file.LinkProperty = new FileShellProperty();
			file.Comment = new FileCommentProperty();
			return file;
		}

		public static bool GetMainIcon(out string path)
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
				OleCheck(API.SHGetDesktopFolder(out desktopFolder));
				OleCheck(SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out drivesPidl));
				try
				{
					return GetDisplayName(desktopFolder, drivesPidl, SHGDN.INFOLDER);
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
			OleCheck(API.SHGetDesktopFolder(out desktopFolder));
			OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.DRIVES, IntPtr.Zero, 0, out drivesPidl));
			try
			{
				IShellFolder2 folder;
				OleCheck(desktopFolder.BindToObject(drivesPidl, null, ref IID_IShellFolder2, out folder));
				IEnumIDList enumIdList;
				OleCheck(folder.EnumObjects(IntPtr.Zero, (uint)(SHCONTF.FOLDERS | SHCONTF.STORAGE), out enumIdList));
				string rootPath = "\\\\\\" + GetDisplayName(desktopFolder, drivesPidl, SHGDN.INFOLDER);

				IntPtr pidl;
				uint numIds;
				int index = 0;
				while (enumIdList.Next(1, out pidl, out numIds) == 0)
				{
					try
					{
						uint rgfInOut = SFGAOF_DEFAULT;
						if (folder.GetAttributesOf(1, ref pidl, ref rgfInOut) == 0)
						{
							if ((SFGAOF_DEFAULT & rgfInOut) == (uint)SFGAO.FOLDER)
							{
								string deviceId = GetDisplayName(folder, pidl, SHGDN.FORPARSING);
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
									drive.DriveLabel = GetDisplayNameEx(folder, pidl, SHGDN.INFOLDER);
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

		// ... 其他接口实现 ...
	}
}