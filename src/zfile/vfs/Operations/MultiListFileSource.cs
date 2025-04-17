namespace zfile
{
    public interface IMultiListFileSource : IFileSource
    {
        void AddList(ref FileTreeNode fileList, IFileSource fileSource);
        FileTreeNode FileList { get; }
        IFileSource FileSource { get; }
    }

    public class MultiListFileSource : FileSource, IMultiListFileSource
    {
        private FileTreeNode _fileList;
        private IFileSource _fileSource;

        public MultiListFileSource()
        {
            _fileList = null;
            _fileSource = null;
        }

        ~MultiListFileSource()
        {
            if (_fileSource != null)
            {
                _fileSource.RemoveReloadEventListener(FileSourceReloadEvent);
            }
            _fileList = null;
            _fileSource = null;
        }

        public void AddList(ref FileTreeNode fileList, IFileSource fileSource)
        {
            if (_fileList != null)
            {
                _fileList = null;
            }

            _fileList = fileList;
            fileList = null;
            _fileSource = fileSource;

            _fileSource.AddReloadEventListener(FileSourceReloadEvent);
        }

        public FileTreeNode FileList => _fileList;
        public IFileSource FileSource => _fileSource;

        private void FileSourceReloadEvent(IFileSource fileSource, string[] reloadedPaths)
        {
            DoReload(reloadedPaths);
        }

        protected override void DoReload(string[] pathsToReload)
        {
            // 实现重新加载逻辑
        }

        public override FilePropertiesTypes GetSupportedFileProperties()
        {
            return _fileSource.GetSupportedFileProperties();
        }

        public override FileSourceOperationType GetOperationsTypes()
        {
            // 默认只支持fsoList
            // 其他操作只有在文件源支持时才支持
            // 但这只适用于单个文件源
            return FileSourceOperationType.List |
                   (_fileSource.GetOperationsTypes() &
                    (FileSourceOperationType.CopyOut |
                     FileSourceOperationType.Delete |
                     FileSourceOperationType.Wipe |
                     FileSourceOperationType.CalcChecksum |
                     FileSourceOperationType.CalcStatistics |
                     FileSourceOperationType.SetFileProperty |
                     FileSourceOperationType.Execute |
                     FileSourceOperationType.TestArchive));
        }

        public override FileSourceProperties GetProperties()
        {
            // 标志取决于底层文件源
            return _fileSource.GetProperties();
        }

        public override bool CreateDirectory(string path)
        {
            return _fileSource.CreateDirectory(path);
        }

        public override bool FileSystemEntryExists(string path)
        {
            return _fileSource.FileSystemEntryExists(path);
        }

        public override FilePropertiesTypes GetRetrievableFileProperties()
        {
            return _fileSource.GetRetrievableFileProperties();
        }

        public override void RetrieveProperties(FileInfo file, FilePropertiesTypes propertiesToSet, string[] variantProperties)
        {
            _fileSource.RetrieveProperties(file, propertiesToSet, variantProperties);
        }

        public override bool CanRetrieveProperties(FileInfo file, FilePropertiesTypes propertiesToSet)
        {
            return _fileSource.CanRetrieveProperties(file, propertiesToSet);
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            return new MultiListListOperation(this, targetPath);
        }

        public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
        {
            return _fileSource.CreateCopyOutOperation(targetFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
        {
            return _fileSource.CreateMoveOperation(sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
        {
            return _fileSource.CreateDeleteOperation(filesToDelete);
        }

        public override FileSourceOperation CreateWipeOperation(FileEntries filesToWipe)
        {
            return _fileSource.CreateWipeOperation(filesToWipe);
        }

        public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
        {
            return _fileSource.CreateExecuteOperation(executableFile, basePath, verb);
        }

        public override FileSourceOperation CreateTestArchiveOperation(FileEntries sourceFiles)
        {
            return _fileSource.CreateTestArchiveOperation(sourceFiles);
        }

        public override FileSourceOperation CreateCalcChecksumOperation(FileEntries files, string targetPath, string targetMask)
        {
            return _fileSource.CreateCalcChecksumOperation(files, targetPath, targetMask);
        }

        public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
        {
            return _fileSource.CreateCalcStatisticsOperation(files);
        }

        public override FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperty[] newProperties)
        {
            return _fileSource.CreateSetFilePropertyOperation(targetFiles, newProperties);
        }
    }
} 