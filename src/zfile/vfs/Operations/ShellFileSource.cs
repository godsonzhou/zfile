using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
	public interface IShellFileSource : IVirtualFileSource
	{
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
			//IShellFolder tempFolder;
			//_desktopFolder.BindToObject(_drives, IntPtr.Zero, ref Guids.IID_IShellFolder, out tempFolder);
			//_rootFolder = (IShellFolder2)tempFolder; //change rootfolder from 此电脑 -> 桌面
			_rootFolder = (IShellFolder2)w32.GetDesktopFolder(out _);//(IShellFolder2)_desktopFolder;
			//_rootPath = ""; // w32.GetDisplayName(_desktopFolder, _drives, SHGDN.INFOLDER);  //c: d: e: use filesystemfilesource, use shellfilesource to process virtual node, for instance, control panel, desktop, etc.

			OperationsClasses[FileSourceOperationTypes.Move] = typeof(ShellMoveOperation);
			OperationsClasses[FileSourceOperationTypes.Copy] = typeof(ShellCopyOperation);
			OperationsClasses[FileSourceOperationTypes.CopyIn] = typeof(ShellCopyInOperation);
			OperationsClasses[FileSourceOperationTypes.CopyOut] = typeof(ShellCopyOutOperation);
			OperationsClasses[FileSourceOperationTypes.List] = typeof(ShellListOperation);
			OperationsClasses[FileSourceOperationTypes.Delete] = typeof(ShellDeleteOperation);
			OperationsClasses[FileSourceOperationTypes.CreateDirectory] = typeof(ShellCreateDirectoryOperation);
			OperationsClasses[FileSourceOperationTypes.Execute] = typeof(ShellExecuteOperation);
			OperationsClasses[FileSourceOperationTypes.CalcStatistics] = typeof(ShellCalcStatisticsOperation);
			OperationsClasses[FileSourceOperationTypes.SetFileProperty] = typeof(ShellSetFilePropertyOperation);
			//_rootPath = w32.GetDisplayName(_desktopFolder, _drives, SHGDN.INFOLDER);    //此电脑
			_rootPath = "桌面";
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
			// 实现创建文件夹的功能
			// 这里需要使用Shell API创建文件夹
			// 在Pascal版本中，这个方法是通过调用Shell32的API来实现的
			// 在C#中，我们可以使用类似的方式
			try
			{
				// 创建一个新的文件夹
				IntPtr pidl;
				uint attrs = 0;
				return parent.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, newDir, out _, out pidl, ref attrs);
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException(ex);
			}
		}

		public int FindFolder(string path, out IShellFolder2 folder)
		{
			folder = null;
			try
			{
				// 分割路径
				string[] pathParts = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

				if (pathParts.Length == 0)
				{
					return unchecked((int)0x80030002); // STG_E_PATHNOTFOUND
				}
				else
				{
					if (pathParts[0] != _rootPath)
					{
						return unchecked((int)0x80030002); // STG_E_PATHNOTFOUND
					}
					else
					{
						folder = _rootFolder;
						// 查找子目录
						for (int i = 1; i < pathParts.Length; i++)
						{
							int result = List(folder, pathParts[i]);
							if (result < 0) // Failed
							{
								return result;
							}
						}
					}
				}
				return 0; // S_OK
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException(ex);
			}
		}

		private int List(IShellFolder2 folder, string name)
		{
			try
			{
				IntPtr pidl;
				uint attrs = 0;
				int hr = folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, name, out _, out pidl, ref attrs);
				if (hr >= 0) // Succeeded
				{
					IShellFolder tempFolder;
					hr = folder.BindToObject(pidl, IntPtr.Zero, ref Guids.IID_IShellFolder, out tempFolder);
					if (hr >= 0) // Succeeded
					{
						folder = (IShellFolder2)tempFolder;
					}
					Marshal.FreeCoTaskMem(pidl);
				}
				return hr;
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException(ex);
			}
		}

		public int FindObject(string obj, out IntPtr pidl)
		{
			pidl = IntPtr.Zero;
			try
			{
				string path = Path.GetDirectoryName(obj);
				IShellFolder2 folder;
				int result = FindFolder(path, out folder);

				if (result >= 0) // Succeeded
				{
					IntPtr itemPidl;
					result = FindObject(folder, Path.GetFileName(obj), out itemPidl);

					if (result >= 0) // Succeeded
					{
						IntPtr folderPidl;
						result = API.SHGetIDListFromObject(folder, out folderPidl);
						if (result >= 0) // Succeeded
						{
							pidl = API.ILCombine(folderPidl, itemPidl);
							Marshal.FreeCoTaskMem(folderPidl);
						}
						Marshal.FreeCoTaskMem(itemPidl);
					}
				}
				return result;
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException(ex);
			}
		}

		public int FindObject(IShellFolder2 parent, string name, out IntPtr pidl)
		{
			pidl = IntPtr.Zero;
			try
			{
				uint attrs = 0;
				return parent.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, name, out _, out pidl, ref attrs);
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException(ex);
			}
		}

		public override bool CreateDirectory(string path)
		{
			string name = Path.GetFileName(path);
			IShellFolder2 parent;
			bool result = FindFolder(Path.GetDirectoryName(path), out parent) >= 0;
			if (result)
			{
				result = CreateFolder(parent, name) >= 0;
			}
			return result;
		}

		public override bool FileSystemEntryExists(string path)
		{
			IntPtr obj;
			bool result = FindObject(path, out obj) >= 0;
			if (result)
			{
				Marshal.FreeCoTaskMem(obj);
			}
			return result;
		}

		public override FileSourceOperationTypes OperationsTypes => FileSourceOperationTypes.List |
			FileSourceOperationTypes.Execute |
			FileSourceOperationTypes.Delete |
			FileSourceOperationTypes.CreateDirectory |
			FileSourceOperationTypes.CopyIn |
			FileSourceOperationTypes.CopyOut |
			FileSourceOperationTypes.SetFileProperty |
			FileSourceOperationTypes.CalcStatistics;

		public override FilePropertiesTypes SupportedFileProperties => 
			base.SupportedFileProperties |
			FilePropertiesTypes.Size |
			FilePropertiesTypes.Attributes |
			FilePropertiesTypes.ModificationTime |
			FilePropertiesTypes.CreationTime |
			FilePropertiesTypes.Link |
			FilePropertiesTypes.Comment;
		

		public override string GetRootDir(string path)
		{
			return Path.DirectorySeparatorChar.ToString() +
				   Path.DirectorySeparatorChar.ToString() +
				   Path.DirectorySeparatorChar.ToString() +
				   _rootPath +
				   Path.DirectorySeparatorChar.ToString();
		}

		public override FileSourceProperties Properties => FileSourceProperties.Virtual;

		public override FileSourceOperation CreateListOperation(string targetPath)
		{
			return new ShellListOperation(this, targetPath);
		}

		public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
		{
			return new ShellDeleteOperation(this, filesToDelete);
		}

		public override FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
		{
			return new ShellCreateDirectoryOperation(this, basePath, directoryPath);
		}

		public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
		{
			return new ShellExecuteOperation(this, executableFile, basePath, verb);
		}

		public override FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
		{
			return new ShellMoveOperation(this, sourceFiles, targetPath);
		}

		public override FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
		{
			return new ShellCopyOperation(this, this, sourceFiles, targetPath);
		}

		public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
		{
			return new ShellCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
		}

		public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
		{
			return new ShellCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
		}

		public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
		{
			return new ShellCalcStatisticsOperation(this, files);
		}

		public override FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperty[] newProperties)
		{
			return new ShellSetFilePropertyOperation(this, targetFiles, newProperties);
		}
	}
}