namespace zfile
{
    /// <summary>
    /// Operation that splits file within the same file source.
    /// </summary>
    public abstract class FileSourceSplitOperation : FileSourceOperation
    {
        private FileSourceSplitOperationStatistics _statistics;
        private FileSourceSplitOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private FileEntry _sourceFile;
        private string _targetPath;
        private long _volumeSize;
        private int _volumeNumber;
        private bool _requireACRC32VerificationFile;
        private uint _currentCRC32;
        private bool _automaticSplitMode;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.Split;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the source file
        /// </summary>
        protected FileEntry SourceFile => _sourceFile;

        /// <summary>
        /// Gets the target path
        /// </summary>
        protected string TargetPath => _targetPath;

        /// <summary>
        /// Gets or sets the volume size
        /// </summary>
        public long VolumeSize
        {
            get => _volumeSize;
            set => _volumeSize = value;
        }

        /// <summary>
        /// Gets or sets the volume number
        /// </summary>
        public int VolumeNumber
        {
            get => _volumeNumber;
            set => _volumeNumber = value;
        }

        /// <summary>
        /// Gets or sets whether a CRC32 verification file is required
        /// </summary>
        public bool RequireACRC32VerificationFile
        {
            get => _requireACRC32VerificationFile;
            set => _requireACRC32VerificationFile = value;
        }

        /// <summary>
        /// Gets or sets the current CRC32
        /// </summary>
        public uint CurrentCRC32
        {
            get => _currentCRC32;
            set => _currentCRC32 = value;
        }

        /// <summary>
        /// Gets or sets whether automatic split mode is enabled
        /// </summary>
        public bool AutomaticSplitMode
        {
            get => _automaticSplitMode;
            set => _automaticSplitMode = value;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceSplitOperation"/> class
        /// </summary>
        /// <param name="aFileSource">File source within which the operation should take place</param>
        /// <param name="aSourceFile">The file which is to be split</param>
        /// <param name="aTargetPath">Target path for split files</param>
        public FileSourceSplitOperation(IFileSource aFileSource, FileEntry aSourceFile, string aTargetPath)
            : base(aFileSource)
        {
            _statistics = new FileSourceSplitOperationStatistics
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

            _fileSource = aFileSource;
            _sourceFile = aSourceFile ?? throw new ArgumentNullException(nameof(aSourceFile));
            _targetPath = Helper.IncludeTrailingPathDelimiter(aTargetPath);
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceFile = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Updates the statistics for this operation
        /// </summary>
        /// <param name="newStatistics">The new statistics to update with</param>
        protected void UpdateStatistics(FileSourceSplitOperationStatistics newStatistics)
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
        /// Retrieves the current statistics for this operation
        /// </summary>
        /// <returns>The current statistics</returns>
        public FileSourceSplitOperationStatistics RetrieveStatistics()
        {
            lock (_statisticsLock)
            {
                return _statistics;
            }
        }

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            string[] paths = new string[1];
            paths[0] = _targetPath;  // Split target path
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
                    return string.Format("Splitting from {0} to {1}", SourceFile?.Path ?? "", TargetPath);
                default:
                    return "Splitting";
            }
        }
    }
}