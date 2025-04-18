namespace zfile
{
    /// <summary>
    /// Statistics for wipe operation
    /// </summary>
    public struct FileSourceWipeOperationStatistics
    {
        public string CurrentFile;
        public long CurrentFileTotalBytes;
        public long CurrentFileDoneBytes;
        public long TotalFiles;
        public long DoneFiles;
        public long TotalBytes;
        public long DoneBytes;
        public long BytesPerSecond;
        public DateTime RemainingTime;
    }

    /// <summary>
    /// Operation that wipes files from an arbitrary file source.
    /// File source should match the class type.
    /// </summary>
    public abstract class FileSourceWipeOperation : FileSourceOperation
    {
        private FileSourceWipeOperationStatistics _statistics;
        private FileSourceWipeOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private FileEntries _filesToWipe;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.Wipe;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the files to wipe
        /// </summary>
        protected FileEntries FilesToWipe => _filesToWipe;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceWipeOperation"/> class
        /// </summary>
        /// <param name="aTargetFileSource">File source from which the files will be wiped</param>
        /// <param name="theFilesToWipe">Files which are to be wiped</param>
        public FileSourceWipeOperation(IFileSource aTargetFileSource, FileEntries theFilesToWipe)
            : base(aTargetFileSource)
        {
            _statistics = new FileSourceWipeOperationStatistics
            {
                CurrentFile = "",
                TotalFiles = 0,
                DoneFiles = 0,
                TotalBytes = 0,
                DoneBytes = 0,
                CurrentFileTotalBytes = 0,
                CurrentFileDoneBytes = 0,
                BytesPerSecond = 0,
                RemainingTime = DateTime.MinValue
            };

            _fileSource = aTargetFileSource;
            _filesToWipe = theFilesToWipe ?? new FileEntries();
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _filesToWipe = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            if (FilesToWipe.Count > 0)
            {
                _fileSource.Reload(new[] { FilesToWipe[0].Path });
            }
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
                    if (FilesToWipe.Count == 1)
                        return string.Format("Wiping {0}", FilesToWipe[0].FullPath);
                    else
                        return string.Format("Wiping in {0}", FilesToWipe.Count > 0 ? FilesToWipe[0].Path : "");
                default:
                    return "Wiping";
            }
        }

        /// <summary>
        /// Updates the statistics for this operation
        /// </summary>
        /// <param name="newStatistics">The new statistics to update with</param>
        protected void UpdateStatistics(FileSourceWipeOperationStatistics newStatistics)
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
        public FileSourceWipeOperationStatistics RetrieveStatistics()
        {
            lock (_statisticsLock)
            {
                return _statistics;
            }
        }
    }
}