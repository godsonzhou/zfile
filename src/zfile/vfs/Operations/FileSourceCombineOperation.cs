namespace zfile
{
    /// <summary>
    /// Operation that combines files within the same file source.
    /// </summary>
    public abstract class FileSourceCombineOperation : FileSourceOperation
    {
        private FileSourceCombineOperationStatistics _statistics;
        private FileSourceCombineOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private FileEntries _sourceFiles;
        private string _targetFile;
        private bool _requireDynamicMode;
        private bool _weGotTheCRC32VerificationFile;
        private uint _expectedCRC32;
        private uint _currentCRC32;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.Combine;

        /// <summary>
        /// Gets the file source
        /// </summary>
        //protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the source files
        /// </summary>
        protected FileEntries SourceFiles => _sourceFiles;

        /// <summary>
        /// Gets or sets the target file
        /// </summary>
        public string TargetFile
        {
            get => _targetFile;
            set => _targetFile = value;
        }

        /// <summary>
        /// Gets or sets whether dynamic mode is required
        /// </summary>
        public bool RequireDynamicMode
        {
            get => _requireDynamicMode;
            set => _requireDynamicMode = value;
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
        /// Gets or sets the expected CRC32
        /// </summary>
        public uint ExpectedCRC32
        {
            get => _expectedCRC32;
            set => _expectedCRC32 = value;
        }

        /// <summary>
        /// Gets or sets whether we got the CRC32 verification file
        /// </summary>
        public bool WeGotTheCRC32VerificationFile
        {
            get => _weGotTheCRC32VerificationFile;
            set => _weGotTheCRC32VerificationFile = value;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceCombineOperation"/> class
        /// </summary>
        /// <param name="aFileSource">File source within which the operation should take place</param>
        /// <param name="theSourceFiles">Files which are to be combined</param>
        /// <param name="aTargetFile">Target name of combined file</param>
        public FileSourceCombineOperation(IFileSource aFileSource, FileEntries theSourceFiles, string aTargetFile)
            : base(aFileSource)
        {
            _statistics = new FileSourceCombineOperationStatistics
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
            _sourceFiles = theSourceFiles ?? new FileEntries();
            _targetFile = aTargetFile;
            _requireDynamicMode = false; // By default, DC mode which means user selected ALL the files
            _expectedCRC32 = 0x00000000; // By default, the expected CRC32 is 0, which is undefined
            _currentCRC32 = 0x00000000; // Initial value of CRC32
            _weGotTheCRC32VerificationFile = false; // By default, we still don't have in hand info from summary file
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
        protected void UpdateStatistics(FileSourceCombineOperationStatistics newStatistics)
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
        public FileSourceCombineOperationStatistics RetrieveStatistics()
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
            paths[0] = Path.GetDirectoryName(_targetFile);  // Combine target path
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
                    return string.Format("Combining from {0} to {1}", SourceFiles.Count > 0 ? SourceFiles[0].Path : "", TargetFile);
                default:
                    return "Combining";
            }
        }
    }
}