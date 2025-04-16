using System.Runtime.InteropServices;
namespace zfile
{
    public interface IFileSystemFileSource : ILocalFileSource
    {
        // 接口定义
    }

    public class FileSystemFileSource : LocalFileSource, IFileSystemFileSource
    {
        private Description description;

        public FileSystemFileSource()
        {
            description = new Description(false);

            // 注册操作类
            OperationsClasses[FileSourceOperationType.List] = typeof(FileSystemListOperation);
            OperationsClasses[FileSourceOperationType.Copy] = typeof(FileSystemCopyOperation);
            OperationsClasses[FileSourceOperationType.CopyIn] = typeof(FileSystemCopyInOperation);
            OperationsClasses[FileSourceOperationType.CopyOut] = typeof(FileSystemCopyOutOperation);
            OperationsClasses[FileSourceOperationType.Move] = typeof(FileSystemMoveOperation);
            OperationsClasses[FileSourceOperationType.Delete] = typeof(FileSystemDeleteOperation);
            OperationsClasses[FileSourceOperationType.Wipe] = typeof(FileSystemWipeOperation);
            OperationsClasses[FileSourceOperationType.Combine] = typeof(FileSystemCombineOperation);
            OperationsClasses[FileSourceOperationType.CreateDirectory] = typeof(FileSystemCreateDirectoryOperation);
            OperationsClasses[FileSourceOperationType.CalcChecksum] = typeof(FileSystemCalcChecksumOperation);
            OperationsClasses[FileSourceOperationType.CalcStatistics] = typeof(FileSystemCalcStatisticsOperation);
            OperationsClasses[FileSourceOperationType.SetFileProperty] = typeof(FileSystemSetFilePropertyOperation);
            OperationsClasses[FileSourceOperationType.Execute] = typeof(FileSystemExecuteOperation);
        }

        ~FileSystemFileSource()
        {
            description?.Dispose();
        }

        public static FileEntry CreateFile(string path)
        {
            var file = new FileEntry(path);
            file.Attributes = FileAttributes.Normal;
            file.Size = 0;
            file.ModificationTime = DateTime.Now;
            file.CreationTime = DateTime.Now;
            file.LastAccessTime = DateTime.Now;
            file.Link = new FileLinkProperty();
            file.Owner = new FileOwnerProperty();
            file.Type = new FileTypeProperty();
            file.Comment = new FileCommentProperty();
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
            file.Link = new FileLinkProperty();

            if (FileAttributes.ReparsePoint == (file.Attributes & FileAttributes.ReparsePoint))
            {
                var linkAttrs = File.GetAttributes(path);
                file.Link.LinkTo = File.ResolveLinkTarget(path, true)?.FullName;
                file.Link.IsValid = linkAttrs != (FileAttributes)(-1);
                if (file.Link.IsValid)
                {
                    file.Link.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
                    if (file.Link.IsLinkToDirectory)
                        file.Size = 0;
                }
            }

            file.Name = searchRec.Name;
            return file;
        }

        public static FileEntry CreateFileFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var file = new FileEntry(Path.GetDirectoryName(filePath));
            var fileInfo = new FileEntry(filePath);

            file.Attributes = fileInfo.Attributes;
            file.Size = fileInfo.Length;
            file.ModificationTime = fileInfo.LastWriteTime;
            file.CreationTime = fileInfo.CreationTime;
            file.LastAccessTime = fileInfo.LastAccessTime;
            file.Link = new FileLinkProperty();

            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                var linkAttrs = File.GetAttributes(filePath);
                file.Link.LinkTo = File.ResolveLinkTarget(filePath, true)?.FullName;
                file.Link.IsValid = linkAttrs != (FileAttributes)(-1);
                if (file.Link.IsValid)
                {
                    file.Link.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
                }
            }

            file.FullPath = filePath;
            return file;
        }

        public static List<FileEntry> CreateFilesFromFileList(string path, List<string> fileNamesList, bool omitNotExisting = false)
        {
            var result = new List<FileEntry>();
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

        public override void RetrieveProperties(FileEntry file, FilePropertyType propertiesToSet, string[] variantProperties)
        {
            var assignedProperties = file.AssignedProperties;
            propertiesToSet = propertiesToSet - assignedProperties;

            if (propertiesToSet == FilePropertyType.None)
                return;

            var fullPath = file.FullPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows特定实现
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException(fullPath);

                if (!assignedProperties.HasFlag(FilePropertyType.Attributes))
                    file.Attributes = fileInfo.Attributes;

                if (!assignedProperties.HasFlag(FilePropertyType.Size))
                    file.Size = fileInfo.Length;

                if (!assignedProperties.HasFlag(FilePropertyType.ModificationTime))
                    file.ModificationTime = fileInfo.LastWriteTime;

                if (!assignedProperties.HasFlag(FilePropertyType.CreationTime))
                    file.CreationTime = fileInfo.CreationTime;

                if (!assignedProperties.HasFlag(FilePropertyType.LastAccessTime))
                    file.LastAccessTime = fileInfo.LastAccessTime;

                if (propertiesToSet.HasFlag(FilePropertType.Link))
                {
                    file.Link = new FileLinkProperty();
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        var linkAttrs = File.GetAttributes(fullPath);
                        file.Link.LinkTo = File.ResolveLinkTarget(fullPath, true)?.FullName;
                        file.Link.IsValid = linkAttrs != (FileAttributes)(-1);
                        if (file.Link.IsValid)
                        {
                            file.Link.IsLinkToDirectory = (linkAttrs & FileAttributes.Directory) != 0;
                        }
                    }
                }

                if (propertiesToSet.HasFlag(FilePropertyType.Owner))
                {
                    SetOwner(file);
                }

                if (propertiesToSet.HasFlag(FilePropertyType.Type))
                {
                    file.Type = new FileTypeProperty();
                    file.Type.Value = GetFileDescription(fullPath);
                }

                if (propertiesToSet.HasFlag(FilePropertyType.CompressedSize))
                {
                    file.CompressedSize = new FileCompressedSizeProperty();
                    file.CompressedSize.Value = GetCompressedFileSize(fullPath);
                }
            }
            else
            {
                // Unix特定实现
                // 这里需要根据Unix系统实现相应的功能
            }

            if (propertiesToSet.HasFlag(FilePropertyType.Comment))
            {
                file.Comment = new FileCommentProperty();
                file.Comment.Value = description.ReadDescription(fullPath);
            }
        }

        public static IFileSystemFileSource GetFileSource()
        {
            var fileSource = FileSourceManager.Find(typeof(FileSystemFileSource), string.Empty);
            if (fileSource == null)
                return new FileSystemFileSource();
            return fileSource as IFileSystemFileSource;
        }

        public override FileSourceOperationTypes GetOperationsTypes()
        {
            return FileSourceOperationType.List |
                   FileSourceOperationType.Copy |
                   FileSourceOperationType.CopyIn |
                   FileSourceOperationType.CopyOut |
                   FileSourceOperationType.Move |
                   FileSourceOperationType.Delete |
                   FileSourceOperationType.Wipe |
                   FileSourceOperationType.Split |
                   FileSourceOperationType.Combine |
                   FileSourceOperationType.CreateDirectory |
                   FileSourceOperationType.CalcChecksum |
                   FileSourceOperationType.CalcStatistics |
                   FileSourceOperationType.SetFileProperty |
                   FileSourceOperationType.Execute;
        }

        public override FileSourceProperties GetProperties()
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

        public override string GetCurrentWorkingDirectory()
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

        public override string GetRootDir(string path)
        {
            return Path.GetPathRoot(path);
        }

        public override string GetRootDir()
        {
            return GetRootDir(Directory.GetCurrentDirectory());
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
                if (GlobalSettings.LogOptions.HasFlag(LogOptions.DirectoryOperations) &&
                    GlobalSettings.LogOptions.HasFlag(LogOptions.Success))
                {
                    Logger.Write(string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, path),
                        LogMessageType.Success);
                }
                return true;
            }
            catch (Exception)
            {
                if (GlobalSettings.LogOptions.HasFlag(LogOptions.DirectoryOperations) &&
                    GlobalSettings.LogOptions.HasFlag(LogOptions.Errors))
                {
                    Logger.Write(string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, path),
                        LogMessageType.Error);
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

        public override FilePropertyType GetSupportedFileProperties()
        {
            var properties = base.GetSupportedFileProperties();
            properties |= FilePropertyType.Size |
                        FilePropertyType.Attributes |
                        FilePropertyType.ModificationTime |
                        FilePropertyType.LastAccessTime |
                        FilePropertyType.Link;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                properties |= FilePropertyType.CreationTime;
            }
            else
            {
                properties |= FilePropertyType.ChangeTime;
            }

            return properties;
        }

        public override FilePropertyType GetRetrievableFileProperties()
        {
            var properties = base.GetRetrievableFileProperties();
            properties |= FilePropertyType.Size |
                        FilePropertyType.Attributes |
                        FilePropertyType.ModificationTime |
                        FilePropertyType.LastAccessTime |
                        FilePropertyType.Link |
                        FilePropertyType.Owner |
                        FilePropertyType.Type |
                        FilePropertyType.Comment;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                properties |= FilePropertyType.CreationTime |
                            FilePropertyType.CompressedSize;
            }
            else
            {
                properties |= FilePropertyType.ChangeTime;
            }

            return properties;
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            return new FileSystemListOperation(this, targetPath);
        }

        public override FileSourceOperation CreateCopyOperation(List<FileInfo> sourceFiles, string targetPath)
        {
            return new FileSystemCopyOperation(this, this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, List<FileInfo> sourceFiles, string targetPath)
        {
            return new FileSystemCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, List<FileInfo> sourceFiles, string targetPath)
        {
            return new FileSystemCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateMoveOperation(List<FileInfo> sourceFiles, string targetPath)
        {
            return new FileSystemMoveOperation(this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateDeleteOperation(List<FileInfo> filesToDelete)
        {
            return new FileSystemDeleteOperation(this, filesToDelete);
        }

        public override FileSourceOperation CreateWipeOperation(List<FileInfo> filesToWipe)
        {
            return new FileSystemWipeOperation(this, filesToWipe);
        }

        public override FileSourceOperation CreateSplitOperation(FileInfo sourceFile, string targetPath)
        {
            return new FileSystemSplitOperation(this, sourceFile, targetPath);
        }

        public override FileSourceOperation CreateCombineOperation(List<FileInfo> sourceFiles, string targetFile)
        {
            return new FileSystemCombineOperation(this, sourceFiles, targetFile);
        }

        public override FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
        {
            return new FileSystemCreateDirectoryOperation(this, basePath, directoryPath);
        }

        public override FileSourceOperation CreateExecuteOperation(FileInfo executableFile, string basePath, string verb)
        {
            return new FileSystemExecuteOperation(this, executableFile, basePath, verb);
        }

        public override FileSourceOperation CreateCalcChecksumOperation(List<FileInfo> files, string targetPath, string targetMask)
        {
            return new FileSystemCalcChecksumOperation(this, files, targetPath, targetMask);
        }

        public override FileSourceOperation CreateCalcStatisticsOperation(List<FileInfo> files)
        {
            return new FileSystemCalcStatisticsOperation(this, files);
        }

        public override FileSourceOperation CreateSetFilePropertyOperation(List<FileInfo> targetFiles, FileProperties newProperties)
        {
            return new FileSystemSetFilePropertyOperation(this, targetFiles, newProperties);
        }

        private void SetOwner(FileEntry file)
        {
            file.Owner = new FileOwnerProperty();
            // 这里需要根据操作系统实现获取文件所有者的功能
        }

        private string GetFileDescription(string path)
        {
            // 这里需要根据操作系统实现获取文件描述的功能
            return string.Empty;
        }

        private long GetCompressedFileSize(string path)
        {
            // 这里需要根据Windows API实现获取压缩文件大小的功能
            return 0;
        }

        private string GetDeepestExistingPath(string path)
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