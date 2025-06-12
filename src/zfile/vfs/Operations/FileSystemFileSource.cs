using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public struct SearchRec
    {
        public string Name;
        public FileAttributes Attributes;
        public long Size;
        public DateTime Time;
        public DateTime PlatformTime;
        public DateTime LastAccessTime;
    }
    public interface IFileSystemFileSource : ILocalFileSource
    {
        // 接口定义
    }

    public class FileSystemFileSource : LocalFileSource, IFileSystemFileSource
    {
		// Windows API常量
		private const int MAX_PATH = 260;
		private const string LONG_PATH_PREFIX = @"\\?\";
		private const string UNC_PREFIX = @"\\";
		private const string UNC_LONG_PATH_PREFIX = @"\\?\UNC\";
		private Description description;

        public FileSystemFileSource()
        {
            description = new Description(false);
            // 注册操作类
            OperationsClasses[FileSourceOperationTypes.List] = typeof(FileSystemListOperation);
            OperationsClasses[FileSourceOperationTypes.Copy] = typeof(FileSystemCopyOperation);
            OperationsClasses[FileSourceOperationTypes.CopyIn] = typeof(FileSystemCopyInOperation);
            OperationsClasses[FileSourceOperationTypes.CopyOut] = typeof(FileSystemCopyOutOperation);
            OperationsClasses[FileSourceOperationTypes.Move] = typeof(FileSystemMoveOperation);
            OperationsClasses[FileSourceOperationTypes.Delete] = typeof(FileSystemDeleteOperation);
            OperationsClasses[FileSourceOperationTypes.Wipe] = typeof(FileSystemWipeOperation);
            OperationsClasses[FileSourceOperationTypes.Combine] = typeof(FileSystemCombineOperation);
			OperationsClasses[FileSourceOperationTypes.Split] = typeof(FileSystemSplitOperation);
            OperationsClasses[FileSourceOperationTypes.CreateDirectory] = typeof(FileSystemCreateDirectoryOperation);
            OperationsClasses[FileSourceOperationTypes.CalcChecksum] = typeof(FileSystemCalcChecksumOperation);
            OperationsClasses[FileSourceOperationTypes.CalcStatistics] = typeof(FileSystemCalcStatisticsOperation);
            OperationsClasses[FileSourceOperationTypes.SetFileProperty] = typeof(FileSystemSetFilePropertyOperation);
            OperationsClasses[FileSourceOperationTypes.Execute] = typeof(FileSystemExecuteOperation);
        }
		public FileSystemFileSource(String rootpath) 
		{
			//_rootpath = rootpath;
			SetRootPath(rootpath);
		}
        ~FileSystemFileSource()
        {
            description?.Dispose();
        }
		public new static FileEntry CreateFile(string path)
        {
            var file = new FileEntry(path);
            file.Attributes = FileAttributes.Normal;
            file.Size = 0;
            file.ModificationTime = DateTime.Now;
            file.CreationTime = DateTime.Now;
            file.LastAccessTime = DateTime.Now;
            file.LinkProperty = new FileLinkProperty();
            file.OwnerProperty = new FileOwnerProperty();
            file.TypeProperty = new FileTypeProperty();
            file.CommentProperty = new FileCommentProperty();
            return file;
        }

        public static FileEntry CreateFile(string path, SearchRec searchRec)
        {
            var file = new FileEntry(path);
            file.Attributes = searchRec.Attributes;
            file.Size = searchRec.Size;
            file.ModificationTime = searchRec.Time;
            file.CreationTime = searchRec.PlatformTime;
            file.LastAccessTime = searchRec.LastAccessTime;
            file.LinkProperty = new FileLinkProperty();

            if (FileAttributes.ReparsePoint == (file.Attributes & FileAttributes.ReparsePoint))
            {
                var linkAttrs = File.GetAttributes(path);
                file.LinkProperty.LinkTarget = File.ResolveLinkTarget(path, true)?.FullName;
                file.LinkProperty.IsValid = linkAttrs != (FileAttributes)(-1);
                if (file.LinkProperty.IsValid)
                {
                    file.LinkProperty.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
                    if (file.LinkProperty.IsLinkToDirectory)
                        file.Size = 0;
                }
            }

            file.Name = searchRec.Name;
            return file;
        }

		//public static FileEntry CreateFileFromFile(string filePath)
		//{
		//    if (!File.Exists(filePath))
		//        throw new FileNotFoundException(filePath);

		//    var file = new FileEntry(Path.GetDirectoryName(filePath));
		//    var FileEntry = new FileEntry(filePath);

		//    file.Attributes = FileEntry.Attributes;
		//    file.Size = FileEntry.Size;
		//    file.ModificationTime = FileEntry.ModificationTime;
		//    file.CreationTime = FileEntry.CreationTime;
		//    file.LastAccessTime = FileEntry.LastAccessTime;
		//    file.LinkProperty = new FileLinkProperty();

		//    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
		//    {
		//        var linkAttrs = File.GetAttributes(filePath);
		//        file.LinkProperty.LinkTarget = File.ResolveLinkTarget(filePath, true)?.FullName;
		//        file.LinkProperty.IsValid = linkAttrs != (FileAttributes)(-1);
		//        if (file.LinkProperty.IsValid)
		//        {
		//            file.LinkProperty.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
		//        }
		//    }

		//    file.FullPath = filePath;
		//    return file;
		//}
		// 移除长路径前缀
		public static string RemoveLongPathPrefix(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			if (path.StartsWith(LONG_PATH_PREFIX, StringComparison.OrdinalIgnoreCase))
				return path.Substring(LONG_PATH_PREFIX.Length);

			if (path.StartsWith(UNC_LONG_PATH_PREFIX, StringComparison.OrdinalIgnoreCase))
				return UNC_PREFIX + path.Substring(UNC_LONG_PATH_PREFIX.Length);

			return path;
		}

		// 使用长路径支持的文件查找方法
		public static WIN32_FIND_DATA FindFilesWithLongPathSupport(string directoryPath, string searchPattern = "*")
		{
			// 添加长路径前缀
			string fullPath = AddLongPathPrefix(Path.Combine(directoryPath, searchPattern));

			var findHandle = API.FindFirstFile(fullPath, out WIN32_FIND_DATA findData);
			if (findHandle.IsInvalid) // == new IntPtr(-1))
			{
				int errorCode = Marshal.GetLastWin32Error();
				throw new System.ComponentModel.Win32Exception(errorCode,
					$"无法查找文件: {RemoveLongPathPrefix(fullPath)}");
			}

			//try
			//{
			//	do
			//	{
			//		// 跳过 "." 和 ".."
			//		if (findData.cFileName == "." || findData.cFileName == "..")
			//			continue;

			//		// 处理找到的文件
			//		string fileName = findData.cFileName;
			//		string fullFilePath = Path.Combine(directoryPath, fileName);

			//		Debug.WriteLine($"找到文件: {fullFilePath}");

			//		// 这里可以添加对文件的处理逻辑
			//	}
			//	while (API.FindNextFileW(findHandle, out findData));

			//	int lastError = Marshal.GetLastWin32Error();
			//	if (lastError != 0 && lastError != 2) // 2 = ERROR_NO_MORE_FILES
			//	{
			//		throw new System.ComponentModel.Win32Exception(lastError);
			//	}
			//}
			//finally
			//{
			//	//API.FindClose(findHandle);
			//	findHandle.Dispose();
			//}
			return findData;
		}
		// 添加长路径前缀
		public static string AddLongPathPrefix(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			// 如果已经有长路径前缀，直接返回
			if (path.StartsWith(LONG_PATH_PREFIX, StringComparison.OrdinalIgnoreCase))
				return path;

			// 处理UNC路径
			if (path.StartsWith(UNC_PREFIX, StringComparison.OrdinalIgnoreCase))
			{
				return UNC_LONG_PATH_PREFIX + path.Substring(UNC_PREFIX.Length);
			}

			// 确保路径是绝对路径
			if (!Path.IsPathRooted(path))
				path = Path.GetFullPath(path);

			return LONG_PATH_PREFIX + path;
		}
		public static FileEntry CreateFileFromFile(string filePath)
		{
			WIN32_FIND_DATA findData;
			using (var findHandle = API.FindFirstFile(filePath, out findData))
			{
				var file = new FileEntry(Path.GetDirectoryName(filePath));
				if (findHandle.IsInvalid)
				{
					//长文件名特殊处理			
					//使用system.io来获取文件的基本信息
					var fileInfo = new FileInfo(filePath);
					if (!fileInfo.Exists)
						throw new FileNotFoundException(filePath);
					file.Name = fileInfo.Name;
					file.Attributes = fileInfo.Attributes;
					file.Size = fileInfo.Length;
					file.ModificationTime = fileInfo.LastWriteTime;
					file.CreationTime = fileInfo.CreationTime;
					file.LastAccessTime = fileInfo.LastAccessTime;
				}
				else
				{
					// 设置基本属性
					file.Attributes = findData.dwFileAttributes;
					file.Size = ((long)findData.nFileSizeHigh << 32) | findData.nFileSizeLow;
					file.ModificationTime = DateTime.FromFileTime(((long)findData.ftLastWriteTime.dwHighDateTime << 32) |
																(uint)findData.ftLastWriteTime.dwLowDateTime);
					file.CreationTime = DateTime.FromFileTime(((long)findData.ftCreationTime.dwHighDateTime << 32) |
															(uint)findData.ftCreationTime.dwLowDateTime);
					file.LastAccessTime = DateTime.FromFileTime(((long)findData.ftLastAccessTime.dwHighDateTime << 32) |
															  (uint)findData.ftLastAccessTime.dwLowDateTime);
				}
				file.LinkProperty = new FileLinkProperty();

				// 处理符号链接
				if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
				{
					var linkAttrs = File.GetAttributes(filePath);
					file.LinkProperty.LinkTarget = File.ResolveLinkTarget(filePath, true)?.FullName;
					file.LinkProperty.IsValid = linkAttrs != (FileAttributes)(-1);
					if (file.LinkProperty.IsValid)
					{
						file.LinkProperty.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
					}
				}

				file.FullPath = filePath;
				return file;
			}
		}

		public static FileEntries CreateFilesFromFileEntries(string path, List<string> fileNamesList, bool omitNotExisting = false)
        {
            var result = new FileEntries();
            if (fileNamesList != null && fileNamesList.Count > 0)
            {
                foreach (var fileName in fileNamesList)
                {
                    try
                    {
                        result.Add(CreateFileFromFile(fileName));
                    }
                    catch (FileNotFoundException)
                    {
                        if (!omitNotExisting)
                            throw;
                    }
                }
            }
            return result;
        }

        public override void RetrieveProperties(FileEntry file, FilePropertiesTypes propertiesToSet, string[] variantProperties)
        {
            var assignedProperties = file.AssignedProperties;
            propertiesToSet = propertiesToSet & ~assignedProperties;

            if (propertiesToSet == FilePropertiesTypes.None)
                return;

            var fullPath = file.FullPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows特定实现
                var FileEntry = new FileEntry(fullPath);
                if (!FileEntry.Exists)
                    throw new FileNotFoundException(fullPath);

                if (!assignedProperties.HasFlag(FilePropertiesTypes.Attributes))
                    file.Attributes = FileEntry.Attributes;

                if (!assignedProperties.HasFlag(FilePropertiesTypes.Size))
                    file.Size = FileEntry.Size;

                if (!assignedProperties.HasFlag(FilePropertiesTypes.ModificationTime))
                    file.ModificationTime = FileEntry.ModificationTime;

                if (!assignedProperties.HasFlag(FilePropertiesTypes.CreationTime))
                    file.CreationTime = FileEntry.CreationTime;

                if (!assignedProperties.HasFlag(FilePropertiesTypes.LastAccessTime))
                    file.LastAccessTime = FileEntry.LastAccessTime;

                if (propertiesToSet.HasFlag(FilePropertiesTypes.Link))
                {
                    file.LinkProperty = new FileLinkProperty();
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        var linkAttrs = File.GetAttributes(fullPath);
                        file.LinkProperty.LinkTarget = File.ResolveLinkTarget(fullPath, true)?.FullName;
                        file.LinkProperty.IsValid = linkAttrs != (FileAttributes)(-1);
                        if (file.LinkProperty.IsValid)
                        {
                            file.LinkProperty.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
                        }
                    }
                }

                if (propertiesToSet.HasFlag(FilePropertiesTypes.Owner))
                {
                    SetOwner(file);
                }

                if (propertiesToSet.HasFlag(FilePropertiesTypes.Type))
                {
                    file.TypeProperty = new FileTypeProperty();
                    file.TypeProperty.Value = GetFileDescription(fullPath);
                }

                if (propertiesToSet.HasFlag(FilePropertiesTypes.CompressedSize))
                {
                    file.CompressedSizeProperty = new FileCompressedSizeProperty();
                    file.CompressedSize = GetCompressedFileSize(fullPath);
                }
            }
            else
            {
                // Unix特定实现
                // 这里需要根据Unix系统实现相应的功能
            }

            if (propertiesToSet.HasFlag(FilePropertiesTypes.Comment))
            {
				file.CommentProperty = new() {
					Value = description.ReadDescription(fullPath)
				};
            }
        }

        public static IFileSystemFileSource? GetFileSource()
        {
            var fileSource = FileSourceManager.Instance.Find(typeof(FileSystemFileSource), string.Empty);
            if (fileSource == null)
                return new FileSystemFileSource();
            return fileSource as IFileSystemFileSource;
        }

        public override FileSourceOperationTypes OperationsTypes => FileSourceOperationTypes.List |
                   FileSourceOperationTypes.Copy |
                   FileSourceOperationTypes.CopyIn |
                   FileSourceOperationTypes.CopyOut |
                   FileSourceOperationTypes.Move |
                   FileSourceOperationTypes.Delete |
                   FileSourceOperationTypes.Wipe |
                   FileSourceOperationTypes.Split |
                   FileSourceOperationTypes.Combine |
                   FileSourceOperationTypes.CreateDirectory |
                   FileSourceOperationTypes.CalcChecksum |
                   FileSourceOperationTypes.CalcStatistics |
                   FileSourceOperationTypes.SetFileProperty |
                   FileSourceOperationTypes.Execute;
        

        public virtual FileSourceProperties GetProperties()
        {
            var properties = FileSourceProperties.DirectAccess |
                           FileSourceProperties.ListFlatView |
                           FileSourceProperties.NoneParent;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                properties |= FileSourceProperties.CaseSensitive;
            }

            return properties;
        }

        public string GetCurrentWorkingDirectory()
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(currentDir))
                currentDir = Path.Combine(currentDir, Path.DirectorySeparatorChar.ToString());
            return currentDir;
        }

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            if (!Directory.Exists(newDir))
                return false;
            Directory.SetCurrentDirectory(newDir);
            return true;
        }

        public override void DoReload(string[] pathsToReload)
        {
            description.Reset();
        }

        public override bool IsPathAtRoot(string path)
        {
            var sPath = Path.GetDirectoryName(path);
			if (sPath == null)
				return true;
			if (sPath.StartsWith(@"\\") && sPath.Count(c => c == Path.DirectorySeparatorChar) == 3)
                return true;
            return string.IsNullOrEmpty(Path.GetDirectoryName(path));
        }

        public override string GetParentDir(string path)
        {
            var result = base.GetParentDir(path);
            result = GetDeepestExistingPath(result);
            if (string.IsNullOrEmpty(result))
                result = AppDomain.CurrentDomain.BaseDirectory;
            return result;
        }

		public void SetRootPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return;
			_rootpath = Helper.ExcludeTrailingPathDelimiter(path);
		}

		public override string GetRootDir(string path)
        {
            //return Path.GetPathRoot(path);
			return _rootpath;
		}

        public override string GetRootDir()
        {
            //return GetRootDir(Directory.GetCurrentDirectory());
			return _rootpath;
        }

        public override PathType GetPathType(string path)
        {
            if (path.StartsWith(@"\\"))
                return PathType.Network;
            if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
                return PathType.Absolute;
            return PathType.Relative;
        }

        public override bool CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                if (GlobalSettings.LogOptions.HasFlag(LogOption.DirectoryOperation) &&
                    GlobalSettings.LogOptions.HasFlag(LogOption.Success))
                {
                    Logger.Write(string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, path),
                        LogOption.Success);
                }
                return true;
            }
            catch (Exception)
            {
                if (GlobalSettings.LogOptions.HasFlag(LogOption.DirectoryOperation) &&
                    GlobalSettings.LogOptions.HasFlag(LogOption.Error))
                {
                    Logger.Write(string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, path),
                        LogOption.Error);
                }
                return false;
            }
        }

        public override bool FileSystemEntryExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public override bool GetFreeSpace(string path, out long freeSize, out long totalSize)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(path));
                freeSize = driveInfo.AvailableFreeSpace;
                totalSize = driveInfo.TotalSize;
                return true;
            }
            catch (Exception)
            {
                freeSize = 0;
                totalSize = 0;
                return false;
            }
        }

        public FilePropertiesTypes GetSupportedFileProperties()
        {
            var properties = base.SupportedFileProperties;
            properties |= FilePropertiesTypes.Size |
                        FilePropertiesTypes.Attributes |
                        FilePropertiesTypes.ModificationTime |
                        FilePropertiesTypes.LastAccessTime |
                        FilePropertiesTypes.Link;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                properties |= FilePropertiesTypes.CreationTime;
            }
            else
            {
                properties |= FilePropertiesTypes.ChangeTime;
            }

            return properties;
        }

        public FilePropertiesTypes GetRetrievableFileProperties()
        {
            var properties = base.RetrievableFileProperties;
            properties |= FilePropertiesTypes.Size |
                        FilePropertiesTypes.Attributes |
                        FilePropertiesTypes.ModificationTime |
                        FilePropertiesTypes.LastAccessTime |
                        FilePropertiesTypes.Link |
                        FilePropertiesTypes.Owner |
                        FilePropertiesTypes.Type |
                        FilePropertiesTypes.Comment;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                properties |= FilePropertiesTypes.CreationTime |
                            FilePropertiesTypes.CompressedSize;
            }
            else
            {
                properties |= FilePropertiesTypes.ChangeTime;
            }

            return properties;
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            return new FileSystemListOperation(this, targetPath);
        }

        public override FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
        {
            return new FileSystemCopyOperation(this, this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
        {
            return new FileSystemCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
        {
            return new FileSystemCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
        {
            return new FileSystemMoveOperation(this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
        {
            return new FileSystemDeleteOperation(this, filesToDelete);
        }

        public override FileSourceOperation CreateWipeOperation(FileEntries filesToWipe)
        {
            return new FileSystemWipeOperation(this, filesToWipe);
        }

        public override FileSourceOperation CreateSplitOperation(FileEntry sourceFile, string targetPath)
        {
            return new FileSystemSplitOperation(this, sourceFile, targetPath);
        }

        public override FileSourceOperation CreateCombineOperation(FileEntries sourceFiles, string targetFile)
        {
            return new FileSystemCombineOperation(this, sourceFiles, targetFile);
        }

        public override FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
        {
            return new FileSystemCreateDirectoryOperation(this, basePath, directoryPath);
        }

        public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
        {
            return new FileSystemExecuteOperation(this, executableFile, basePath, verb);
        }

        public override FileSourceOperation CreateCalcChecksumOperation(FileEntries files, string targetPath, string targetMask)
        {
            return new FileSystemCalcChecksumOperation(this, files, targetPath, targetMask);
        }

        public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
        {
            return new FileSystemCalcStatisticsOperation(this, files);
        }

        public override FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperties newProperties)
        {
            return new FileSystemSetFilePropertyOperation(this, targetFiles, newProperties);
        }

        private static void SetOwner(FileEntry file)
        {
            file.OwnerProperty = new FileOwnerProperty();
            // 这里需要根据操作系统实现获取文件所有者的功能
        }

        private static string GetFileDescription(string path)
        {
            // 这里需要根据操作系统实现获取文件描述的功能
            return string.Empty;
        }

        private static long GetCompressedFileSize(string path)
        {
            // 这里需要根据Windows API实现获取压缩文件大小的功能
            return 0;
        }

        private static string? GetDeepestExistingPath(string path)
        {
            while (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return path;
        }
    }

    public class FileSystemFileSourceConnection : FileSourceConnection
    {
        protected override void SetCurrentPath(string newPath)
        {
            if (!Directory.Exists(newPath))
                newPath = Directory.GetCurrentDirectory();
            else
                Directory.SetCurrentDirectory(newPath);

            base.SetCurrentPath(newPath);
        }
    }
}