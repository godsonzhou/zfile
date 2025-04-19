namespace zfile
{
    public class MultiArchiveCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly IMultiArchiveFileSource? _fileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        public override FileSourceOperationType OperationType => FileSourceOperationType.CalcStatistics;

        public MultiArchiveCalcStatisticsOperation(IFileSource targetFileSource, FileEntries files)
            : base(targetFileSource, files)
        {
            _fileSource = targetFileSource as IMultiArchiveFileSource;
        }

        protected override void Initialize()
        {
            // 获取初始化的统计信息；然后我们只更改需要的内容
            _statistics = RetrieveStatistics();
        }

        protected override void MainExecute()
        {
            for (int i = 0; i < Files.Count; i++)
            {
                ProcessFile(Files[i]);
                CheckOperationState();
            }
        }

        private void ProcessFile(FileEntry file)
        {
            _statistics.CurrentFile = file.Path + file.Name;
            UpdateStatistics(_statistics);

            if (file.IsDirectory)
            {
                _statistics.Directories++;
                ProcessSubDirs(file.Path + file.Name + Path.DirectorySeparatorChar);
            }
            else if (file.IsLink)
            {
                _statistics.Links++;
            }
            else
            {
                // 在Unix上，这可能不总是常规文件（可能是socket、FIFO、block、char等）
                // 也许在Unix上用FPS_ISREG()检查？

                _statistics.Files++;
                _statistics.Size += file.Size;
                if (file.ModificationTime < _statistics.OldestFile)
                    _statistics.OldestFile = file.ModificationTime;
                if (file.ModificationTime > _statistics.NewestFile)
                    _statistics.NewestFile = file.ModificationTime;
            }

            UpdateStatistics(_statistics);
        }

        private void ProcessSubDirs(string srcPath)
        {
            if (_fileSource == null)
                return;

            var archiveEntries = _fileSource.ArchiveFileEntries;
            for (int i = 0; i < archiveEntries.Count; i++)
            {
                var fileEntry = archiveEntries[i];
                string currFileName = Path.DirectorySeparatorChar + fileEntry.Name;

                if (!FileSystemUtil.IsInPath(srcPath, currFileName, true, false))
                    continue;

                if (fileEntry.IsDirectory)
                    _statistics.Directories++;
                else if (fileEntry.IsLink)
                    _statistics.Links++;
                else
                {
                    _statistics.Files++;
                    _statistics.Size += fileEntry.Size;
                    try
                    {
                        DateTime modificationTime = fileEntry.ModificationTime;
                        if (modificationTime < _statistics.OldestFile)
                            _statistics.OldestFile = modificationTime;
                        if (modificationTime > _statistics.NewestFile)
                            _statistics.NewestFile = modificationTime;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // 忽略日期转换错误
                    }
                }
            }
        }
    }
}