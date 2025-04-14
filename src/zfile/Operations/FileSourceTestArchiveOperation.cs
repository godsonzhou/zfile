namespace Zfile.Operations
{
    /// <summary>
    /// Statistics for TestArchive operation
    /// </summary>
    public class FileSourceTestArchiveOperationStatistics
    {
        public string ArchiveFile { get; set; }
        public string CurrentFile { get; set; }
        public long CurrentFileTotalBytes { get; set; }
        public long CurrentFileDoneBytes { get; set; }
        public long TotalFiles { get; set; }
        public long DoneFiles { get; set; }
        public long TotalBytes { get; set; }
        public long DoneBytes { get; set; }
        public long BytesPerSecond { get; set; }
        public DateTime RemainingTime { get; set; }
    }

    /// <summary>
    /// Operation that test files in archive
    /// </summary>
    public class FileSourceTestArchiveOperation : FileSourceOperation
    {
        private FileSourceTestArchiveOperationStatistics statistics;
        private FileSourceTestArchiveOperationStatistics statisticsAtStartTime;
        private ReaderWriterLockSlim statisticsLock;
        private IFileSource sourceFileSource;
        private List<FileEntry> sourceFiles;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.TestArchive;

        /// <summary>
        /// Gets the source files
        /// </summary>
        protected List<FileEntry> SourceFiles => sourceFiles;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceTestArchiveOperation"/> class
        /// </summary>
        /// <param name="aSourceFileSource">File source from which the files will be tested</param>
        /// <param name="theSourceFiles">Files which are to be tested</param>
        public FileSourceTestArchiveOperation(IFileSource aSourceFileSource, List<FileEntry> theSourceFiles)
            : base(aSourceFileSource)
        {
            statistics = new FileSourceTestArchiveOperationStatistics
            {
                ArchiveFile = "",
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

            statisticsLock = new ReaderWriterLockSlim();
            sourceFileSource = aSourceFileSource;
            sourceFiles = theSourceFiles;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="FileSourceTestArchiveOperation"/> object
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (statisticsLock != null)
            {
                statisticsLock.Dispose();
                statisticsLock = null;
            }
        }

        /// <summary>
        /// Gets the description of the operation
        /// </summary>
        /// <param name="details">Details level</param>
        /// <returns>Description string</returns>
        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            switch (details)
            {
                case FileSourceOperationDescriptionDetails.JobAndTarget:
                    if (sourceFiles.Count == 1)
                        return string.Format("Testing {0}", sourceFiles[0].Name);
                    else
                        return string.Format("Testing in {0}", sourceFiles[0].Path);
                default:
                    return "Testing";
            }
        }

        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="newStatistics">New statistics</param>
        protected void UpdateStatistics(FileSourceTestArchiveOperationStatistics newStatistics)
        {
            statisticsLock.EnterWriteLock();
            try
            {
                // Check if the value by which we calculate progress and remaining time has changed
                if (statistics.DoneBytes != newStatistics.DoneBytes)
                {
                    newStatistics.RemainingTime = Helper.EstimateRemainingTime(
                        statisticsAtStartTime.DoneBytes,
                        newStatistics.DoneBytes,
                        newStatistics.TotalBytes,
                        StartTime,
                        DateTime.Now,
                        out newStatistics.BytesPerSecond);

                    // Update overall progress
                    if (newStatistics.TotalBytes != 0)
                        UpdateProgress((double)newStatistics.DoneBytes / newStatistics.TotalBytes);
                }

                statistics = newStatistics;
            }
            finally
            {
                statisticsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Updates the statistics at start time
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            statisticsLock.EnterWriteLock();
            try
            {
                statisticsAtStartTime = statistics;
            }
            finally
            {
                statisticsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Retrieves the current statistics
        /// </summary>
        /// <returns>Current statistics</returns>
        public FileSourceTestArchiveOperationStatistics RetrieveStatistics()
        {
            statisticsLock.EnterReadLock();
            try
            {
                return statistics;
            }
            finally
            {
                statisticsLock.ExitReadLock();
            }
        }
    }
}