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

        public override FileSourceOperationTypes GetOperationsTypes()
        {
            // 默认只支持fsoList
            // 其他操作只有在文件源支持时才支持
            // 但这只适用于单个文件源
            return FileSourceOperationTypes.List |
                   (_fileSource.GetOperationsTypes() &
                    (FileSourceOperationTypes.CopyOut |
                     FileSourceOperationTypes.Delete |
                     FileSourceOperationTypes.Wipe |
                     FileSourceOperationTypes.CalcChecksum |
                     FileSourceOperationTypes.CalcStatistics |
                     FileSourceOperationTypes.SetFileProperty |
                     FileSourceOperationTypes.Execute |
                     FileSourceOperationTypes.TestArchive));
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

        public override IFileSourceOperation CreateListOperation(string targetPath)
        {
            return new MultiListListOperation(this, targetPath);
        }

        public override IFileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, ref FileInfo[] sourceFiles, string targetPath)
        {
            return _fileSource.CreateCopyOutOperation(targetFileSource, ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateMoveOperation(ref FileInfo[] sourceFiles, string targetPath)
        {
            return _fileSource.CreateMoveOperation(ref sourceFiles, targetPath);
        }

        public override IFileSourceOperation CreateDeleteOperation(ref FileInfo[] filesToDelete)
        {
            return _fileSource.CreateDeleteOperation(ref filesToDelete);
        }

        public override IFileSourceOperation CreateWipeOperation(ref FileInfo[] filesToWipe)
        {
            return _fileSource.CreateWipeOperation(ref filesToWipe);
        }

        public override IFileSourceOperation CreateExecuteOperation(ref FileInfo executableFile, string basePath, string verb)
        {
            return _fileSource.CreateExecuteOperation(ref executableFile, basePath, verb);
        }

        public override IFileSourceOperation CreateTestArchiveOperation(ref FileInfo[] sourceFiles)
        {
            return _fileSource.CreateTestArchiveOperation(ref sourceFiles);
        }

        public override IFileSourceOperation CreateCalcChecksumOperation(ref FileInfo[] files, string targetPath, string targetMask)
        {
            return _fileSource.CreateCalcChecksumOperation(ref files, targetPath, targetMask);
        }

        public override IFileSourceOperation CreateCalcStatisticsOperation(ref FileInfo[] files)
        {
            return _fileSource.CreateCalcStatisticsOperation(ref files);
        }

        public override IFileSourceOperation CreateSetFilePropertyOperation(ref FileInfo[] targetFiles, ref FileProperties newProperties)
        {
            return _fileSource.CreateSetFilePropertyOperation(ref targetFiles, ref newProperties);
        }
    }
} 