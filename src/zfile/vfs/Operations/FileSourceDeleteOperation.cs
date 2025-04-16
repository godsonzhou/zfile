namespace zfile
{
    /// <summary>
    /// Statistics for delete operation
    /// </summary>
    public struct FileSourceDeleteOperationStatistics
    {
        public string CurrentFile;
        public long TotalFiles;
        public long DoneFiles;
        public long TotalBytes;
        public long DoneBytes;
        public long FilesPerSecond;
        public DateTime RemainingTime;
    }

    /// <summary>
    /// Operation that deletes files from an arbitrary file source.
    /// File source should match the class type.
    /// </summary>
    public abstract class FileSourceDeleteOperation : FileSourceOperation
    {
        private FileSourceDeleteOperationStatistics _statistics;
        private FileSourceDeleteOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private FileEntries _filesToDelete;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.Delete;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the files to delete
        /// </summary>
        protected FileEntries FilesToDelete => _filesToDelete;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceDeleteOperation"/> class
        /// </summary>
        /// <param name="aTargetFileSource">File source from which the files will be deleted</param>
        /// <param name="theFilesToDelete">Files which are to be deleted</param>
        public FileSourceDeleteOperation(IFileSource aTargetFileSource, FileEntries theFilesToDelete)
            : base(aTargetFileSource)
        {
            _statistics = new FileSourceDeleteOperationStatistics
            {
                CurrentFile = "",
                TotalFiles = 0,
                DoneFiles = 0,
                TotalBytes = 0,
                DoneBytes = 0,
                FilesPerSecond = 0,
                RemainingTime = DateTime.MinValue
            };

            _fileSource = aTargetFileSource;
            _filesToDelete = theFilesToDelete ?? new FileEntries();
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _filesToDelete = null;
            }

            base.Dispose(disposing);
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
                    if (FilesToDelete.Count == 1)
                        return string.Format("Deleting {0}", FilesToDelete[0].FullPath);
                    else
                        return string.Format("Deleting in {0}", FilesToDelete.Count > 0 ? FilesToDelete[0].Path : "");
                default:
                    return "Deleting";
            }
        }

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            if (FilesToDelete.Count > 0)
            {
                _fileSource.Reload(new[] { FilesToDelete[0].Path });
            }
        }

        /// <summary>
        /// Updates the statistics for this operation
        /// </summary>
        /// <param name="newStatistics">The new statistics to update with</param>
        protected void UpdateStatistics(FileSourceDeleteOperationStatistics newStatistics)
        {
            lock (_statisticsLock)
            {
                // Check if the value by which we calculate progress and remaining time has changed
                if (_statistics.DoneFiles != newStatistics.DoneFiles)
                {
                    newStatistics.RemainingTime = Helper.EstimateRemainingTime(
                        _statisticsAtStartTime.DoneFiles,
                        newStatistics.DoneFiles,
                        newStatistics.TotalFiles,
                        StartTime,
                        DateTime.Now,
                        newStatistics.FilesPerSecond);

                    // Update overall progress
                    if (newStatistics.TotalFiles != 0)
                        UpdateProgress(newStatistics.DoneFiles / (double)newStatistics.TotalFiles);
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
        public FileSourceDeleteOperationStatistics RetrieveStatistics()
        {
            lock (_statisticsLock)
            {
                return _statistics;
            }
        }
    }
}