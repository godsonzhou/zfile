using System;

namespace zfile
{
    /// <summary>
    /// Operation for calculating statistics of files in a WCX archive
    /// </summary>
    public class WcxArchiveCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly IWcxArchiveFileSource _fileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        /// <summary>
        /// Initializes a new instance of the <see cref="WcxArchiveCalcStatisticsOperation"/> class
        /// </summary>
        /// <param name="fileSource">The file source</param>
        /// <param name="files">The files to calculate statistics for</param>
        public WcxArchiveCalcStatisticsOperation(IWcxArchiveFileSource fileSource, FileEntries files)
            : base(fileSource, files)
        {
            _fileSource = fileSource;
        }

        /// <summary>
        /// Initializes the operation
        /// </summary>
        protected override void Initialize()
        {
            // Get initialized statistics; then we change only what is needed
            _statistics = RetrieveStatistics();
        }

        /// <summary>
        /// Executes the main operation
        /// </summary>
        protected override void MainExecute()
        {
            // Implementation of statistics calculation
            for (int i = 0; i < Files.Count; i++)
            {
                ProcessFile(Files[i]);
                CheckOperationState();
            }
        }

        /// <summary>
        /// Process a single file for statistics
        /// </summary>
        /// <param name="file">The file to process</param>
        private void ProcessFile(FileEntry file)
        {
            _statistics.CurrentFile = file.FullPath;
            UpdateStatistics(_statistics);

            // Update statistics based on file type
            if (file.IsDirectory)
            {
                _statistics.Directories++;
                // Process subdirectories if needed
                // ProcessSubDirs(file.FullName);
            }
            else if (file.IsLink)
            {
                _statistics.Links++;
            }
            else
            {
                _statistics.Files++;
                _statistics.Size += file.Size;
                _statistics.CompressedSize += file.CompressedSize;

                // Update oldest/newest file information if available
                if (file.ModificationTime != DateTime.MinValue)
                {
                    if (file.ModificationTime < _statistics.OldestFile || _statistics.OldestFile == DateTime.MaxValue)
                        _statistics.OldestFile = file.ModificationTime;

                    if (file.ModificationTime > _statistics.NewestFile)
                        _statistics.NewestFile = file.ModificationTime;
                }
            }

            UpdateStatistics(_statistics);
        }
    }
}
