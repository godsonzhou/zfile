namespace zfile
{
    public class MultiArchiveCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly IMultiArchiveFileSource _fileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        public MultiArchiveCalcStatisticsOperation(IFileSource targetFileSource, FileInfo[] files)
            : base(targetFileSource, files)
        {
            _fileSource = targetFileSource as IMultiArchiveFileSource;
        }

        public override void Initialize()
        {
            // 获取初始化的统计信息；然后我们只更改需要的内容
            _statistics = RetrieveStatistics();
        }

        public override void MainExecute()
        {
            for (int i = 0; i < Files.Length; i++)
            {
                ProcessFile(Files[i]);
                CheckOperationState();
            }
        }

        private void ProcessFile(FileInfo file)
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
            var fileList = _fileSource.ArchiveFileList.LockList();
            try
            {
                for (int i = 0; i < fileList.Count; i++)
                {
                    var archiveItem = fileList[i];
                    string currFileName = Path.DirectorySeparatorChar + archiveItem.FileName;

                    if (!IsInPath(srcPath, currFileName, true, false))
                        continue;

                    if (_fileSource.FileIsDirectory(archiveItem))
                        _statistics.Directories++;
                    else if (_fileSource.FileIsLink(archiveItem))
                        _statistics.Links++;
                    else
                    {
                        _statistics.Files++;
                        _statistics.Size += archiveItem.UnpSize;
                        try
                        {
                            DateTime modificationTime = new DateTime(
                                archiveItem.Year, archiveItem.Month, archiveItem.Day,
                                archiveItem.Hour, archiveItem.Minute, archiveItem.Second);
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
            finally
            {
                _fileSource.ArchiveFileList.UnlockList();
            }
        }

        private bool IsInPath(string path1, string path2, bool allowPartial, bool caseSensitive)
        {
            return path1.StartsWith(path2, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
    }
} 