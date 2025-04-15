using Zfile.FileSources;

namespace Zfile.Operations
{
    /// <summary>
    /// Checksum operation mode
    /// </summary>
    public enum CalcCheckSumOperationMode
    {
        /// <summary>Calculate checksum</summary>
        Calculate,
        
        /// <summary>Verify checksum</summary>
        Verify
    }

    /// <summary>
    /// Verify checksum result
    /// </summary>
    public struct VerifyChecksumResult
    {
        public List<string> Success;
        public List<string> Broken;
        public List<string> Missing;
        public List<string> ReadError;
    }

    /// <summary>
    /// Statistics for checksum calculation operation
    /// </summary>
    public struct FileSourceCalcChecksumOperationStatistics
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
    /// Operation that calculates checksum of the files.
    /// </summary>
    public abstract class FileSourceCalcChecksumOperation : FileSourceOperation
    {
        private FileSourceCalcChecksumOperationStatistics _statistics;
        private FileSourceCalcChecksumOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _fileSource;
        private List<FileEntry> _files;
        private CalcCheckSumOperationMode _mode;
        private string _targetPath;
        private string _targetMask;
        private HashAlgorithm _algorithm;
        private bool _oneFile;
        private bool _openFileAfterOperationCompleted;

        /// <summary>
        /// Gets the verify checksum result
        /// </summary>
        protected VerifyChecksumResult _result;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.CalcChecksum;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the files
        /// </summary>
        protected List<FileEntry> Files => _files;

        /// <summary>
        /// Gets the target path
        /// </summary>
        protected string TargetPath => _targetPath;

        /// <summary>
        /// Gets the target mask
        /// </summary>
        protected string TargetMask => _targetMask;

        /// <summary>
        /// Gets or sets the operation mode
        /// </summary>
        public CalcCheckSumOperationMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        /// Gets or sets the hash algorithm
        /// </summary>
        public HashAlgorithm Algorithm
        {
            get => _algorithm;
            set => _algorithm = value;
        }

        /// <summary>
        /// Gets or sets whether to create one file
        /// </summary>
        public bool OneFile
        {
            get => _oneFile;
            set => _oneFile = value;
        }

        /// <summary>
        /// Gets or sets whether to open file after operation completed
        /// </summary>
        public bool OpenFileAfterOperationCompleted
        {
            get => _openFileAfterOperationCompleted;
            set => _openFileAfterOperationCompleted = value;
        }

        /// <summary>
        /// Gets the verify checksum result
        /// </summary>
        public VerifyChecksumResult Result => _result;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceCalcChecksumOperation"/> class
        /// </summary>
        /// <param name="aTargetFileSource">File source where the checksum should be calculated</param>
        /// <param name="theFiles">Files which are to be checksummed</param>
        /// <param name="aTargetPath">Target path for checksum files</param>
        /// <param name="aTargetMask">Target mask for checksum files</param>
        public FileSourceCalcChecksumOperation(IFileSource aTargetFileSource, List<FileEntry> theFiles, 
                                              string aTargetPath, string aTargetMask)
            : base(aTargetFileSource)
        {
            _statistics = new FileSourceCalcChecksumOperationStatistics
            {
                CurrentFile = "",
                TotalFiles = 0,
                DoneFiles = 0,
                TotalBytes = 0,
                DoneBytes = 0,
                BytesPerSecond = 0,
                RemainingTime = DateTime.MinValue
            };

            _fileSource = aTargetFileSource;
            _files = theFiles ?? new List<FileEntry>();

            _targetPath = aTargetPath;
            _targetMask = aTargetMask;
            _mode = CalcCheckSumOperationMode.Calculate;
            _algorithm = HashAlgorithm.MD5;
            _oneFile = false;
            _openFileAfterOperationCompleted = false;

            _result = new VerifyChecksumResult
            {
                Success = new List<string>(),
                Broken = new List<string>(),
                Missing = new List<string>(),
                ReadError = new List<string>()
            };
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _files = null;
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
            switch (Mode)
            {
                case CalcCheckSumOperationMode.Calculate:
                    switch (details)
                    {
                        case FileSourceOperationDescriptionDetails.JobAndTarget:
                            if (Files.Count == 1)
                                return string.Format("Calculating checksum of {0}", Files[0].FullPath);
                            else
                                return string.Format("Calculating checksum in {0}", Files.Count > 0 ? Files[0].Path : "");
                        default:
                            return "Calculating checksum";
                    }
                case CalcCheckSumOperationMode.Verify:
                    switch (details)
                    {
                        case FileSourceOperationDescriptionDetails.JobAndTarget:
                            if (Files.Count == 1)
                                return string.Format("Verifying checksum of {0}", Files[0].FullPath);
                            else
                                return string.Format("Verifying checksum in {0}", Files.Count > 0 ? Files[0].Path : "");
                        default:
                            return "Verifying checksum";
                    }
                default:
                    return GetDescription(details);
            }
        }

        /// <summary>
        /// Updates the statistics for this operation
        /// </summary>
        /// <param name="newStatistics">The new statistics to update with</param>
        protected void UpdateStatistics(FileSourceCalcChecksumOperationStatistics newStatistics)
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
                        newStatistics.BytesPerSecond);

                    // Update overall progress
                    if (newStatistics.TotalFiles != 0)
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
        public FileSourceCalcChecksumOperationStatistics RetrieveStatistics()
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
            if (OneFile && OpenFileAfterOperationCompleted)
            {
                // Implementation would depend on UI framework
                // This is a placeholder for the actual implementation to show viewer
            }
        }
    }

    /// <summary>
    /// Hash algorithm
    /// </summary>
    public enum HashAlgorithm
    {
        /// <summary>MD5 algorithm</summary>
        MD5,
        
        /// <summary>SHA1 algorithm</summary>
        SHA1,
        
        /// <summary>SHA256 algorithm</summary>
        SHA256,
        
        /// <summary>SHA512 algorithm</summary>
        SHA512,
        
        /// <summary>CRC32 algorithm</summary>
        CRC32
    }
}