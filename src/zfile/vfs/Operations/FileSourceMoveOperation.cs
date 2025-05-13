namespace zfile
{
    /// <summary>
    /// Operation that moves or renames files within the same file source
    /// (for example: in the same archive, in the same ftp server).
    /// </summary>
    public abstract class FileSourceMoveOperation : FileSourceOperation
    {
        private FileSourceCopyOperationStatistics _statistics;
        private FileSourceCopyOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private FileEntries _sourceFiles;
        private string _targetPath;
        private string _renameMask;

        /// <summary>
        /// File exists option for the operation
        /// </summary>
        protected FileSourceOperationOptionFileExists _fileExistsOption;

        /// <summary>
        /// Directory exists option for the operation
        /// </summary>
        protected FileSourceOperationOptionDirectoryExists _dirExistsOption;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.Move;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the source files
        /// </summary>
        protected FileEntries SourceFiles => _sourceFiles;

        /// <summary>
        /// Gets the target path
        /// </summary>
        protected string TargetPath => _targetPath;

        /// <summary>
        /// Gets or sets the rename mask
        /// </summary>
        public string RenameMask
        {
            get => _renameMask;
            set => _renameMask = value;
        }

        /// <summary>
        /// Gets or sets the file exists option
        /// </summary>
        public FileSourceOperationOptionFileExists FileExistsOption
        {
            get => _fileExistsOption;
            set => _fileExistsOption = value;
        }

        /// <summary>
        /// Gets or sets the directory exists option
        /// </summary>
        public FileSourceOperationOptionDirectoryExists DirExistsOption
        {
            get => _dirExistsOption;
            set => _dirExistsOption = value;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceMoveOperation"/> class
        /// </summary>
        /// <param name="aFileSource">File source within which the operation should take place</param>
        /// <param name="theSourceFiles">Files which are to be moved</param>
        /// <param name="aTargetPath">Path in the file source where the files should be moved</param>
        public FileSourceMoveOperation(IFileSource aFileSource, FileEntries theSourceFiles, string aTargetPath)
            : base(aFileSource)
        {
            _statistics = new FileSourceCopyOperationStatistics
            {
                CurrentFileFrom = "",
                CurrentFileTo = "",
                TotalFiles = 0,
                DoneFiles = 0,
                TotalBytes = 0,
                DoneBytes = 0,
                CurrentFileTotalBytes = 0,
                CurrentFileDoneBytes = 0,
                BytesPerSecond = 0,
                RemainingTime = DateTime.MinValue
            };

            _statisticsLock = new object();

            _fileSource = aFileSource;
            _sourceFiles = theSourceFiles ?? new FileEntries();
            _targetPath = Helper.IncludeTrailingPathDelimiter(aTargetPath);

            _renameMask = "";
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceFiles = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Updates the statistics for this operation
        /// </summary>
        /// <param name="newStatistics">The new statistics to update with</param>
        protected void UpdateStatistics(FileSourceCopyOperationStatistics newStatistics)
        {
            lock (_statisticsLock)
            {
                // Check if the value by which we calculate progress and remaining time has changed
                if (_statistics.DoneBytes != newStatistics.DoneBytes)
                {
                    newStatistics.RemainingTime = Helper.EstimateRemainingTime(
                        _statisticsAtStartTime.DoneBytes,
                        newStatistics.DoneBytes,
                        newStatistics.TotalBytes,
                        StartTime,
                        DateTime.Now,
                        out newStatistics.BytesPerSecond);

                    // Update overall progress
                    if (newStatistics.TotalBytes != 0)
                        UpdateProgress(newStatistics.DoneBytes / (double)newStatistics.TotalBytes);
                }

                _statistics = newStatistics;
            }
        }

        /// <summary>
        /// Updates the statistics at the start time of the operation
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            lock (_statisticsLock)
            {
                _statisticsAtStartTime = _statistics;
            }
        }

        /// <summary>
        /// Shows the compare files UI
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="targetFilePath">The target file path</param>
        protected void ShowCompareFilesUI(FileEntry sourceFile, string targetFilePath)
        {
            // Implementation would depend on UI framework
            // This is a placeholder for the actual implementation
        }

        /// <summary>
        /// Shows the compare files UI by file object
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="targetFile">The target file</param>
        protected void ShowCompareFilesUIByFileObject(FileEntry sourceFile, FileEntry targetFile)
        {
            // Implementation would depend on UI framework
            // This is a placeholder for the actual implementation
        }

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            string[] paths = new string[2];
            paths[0] = SourceFiles.Count > 0 ? SourceFiles[0].Path : "";  // Move source path
            paths[1] = TargetPath;                                         // Move target path
            _fileSource.Reload(paths);
        }

        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        /// <param name="details">The level of detail to include in the description</param>
        /// <returns>The description of this operation</returns>
        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            switch (details)
            {
                case FileSourceOperationDescriptionDetails.JobAndTarget:
                    if (SourceFiles.Count == 1)
                        return string.Format("Moving {0} to {1}", SourceFiles[0].Name, TargetPath);
                    else
                        return string.Format("Moving from {0} to {1}", SourceFiles.Count > 0 ? SourceFiles[0].Path : "", TargetPath);
                default:
                    return "Moving";
            }
        }

        /// <summary>
        /// Retrieves the current statistics for this operation
        /// </summary>
        /// <returns>The current statistics</returns>
        public FileSourceCopyOperationStatistics RetrieveStatistics()
        {
            lock (_statisticsLock)
            {
                return _statistics;
            }
        }
    }
}