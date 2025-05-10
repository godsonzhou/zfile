namespace zfile
{
    /// <summary>
    /// Base class for CopyIn and CopyOut operations.
    /// </summary>
    public abstract class FileSourceCopyOperation : FileSourceOperation
    {
        protected FileSourceCopyOperationStatistics _statistics;
        private FileSourceCopyOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        private IFileSource _sourceFileSource;
        private IFileSource _targetFileSource;
        private FileEntries _sourceFiles;
        private string _renameMask;

        protected string _targetPath;
        protected CopyAttributesOption _copyAttributesOptions;
        protected FileSourceOperationSymLinkOption _symLinkOption;
        protected FileSourceOperationOptionFileExists _fileExistsOption;
        protected FileSourceOperationOptionDirectoryExists _dirExistsOption;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.Copy;

        /// <summary>
        /// Gets the target path
        /// </summary>
        public override string TargetPath { get => _targetPath; set => _targetPath = value; }

		/// <summary>
		/// Gets the source files
		/// </summary>
		public FileEntries SourceFiles => _sourceFiles;

        /// <summary>
        /// Gets the source file source
        /// </summary>
        public IFileSource SourceFileSource => _sourceFileSource;

        /// <summary>
        /// Gets the target file source
        /// </summary>
        public IFileSource TargetFileSource => _targetFileSource;

        /// <summary>
        /// Gets or sets the rename mask
        /// </summary>
        public string RenameMask
        {
            get => _renameMask;
            set => _renameMask = value;
        }

        /// <summary>
        /// Gets or sets the symlink option
        /// </summary>
        public FileSourceOperationSymLinkOption SymLinkOption
        {
            get => _symLinkOption;
            set => _symLinkOption = value;
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
        /// Gets or sets the copy attributes options
        /// </summary>
        public CopyAttributesOption CopyAttributesOptions
        {
            get => _copyAttributesOptions;
            set => _copyAttributesOptions = value;
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
        /// Creates a new instance of the <see cref="FileSourceCopyOperation"/> class
        /// </summary>
        /// <param name="aSourceFileSource">File source from which the files will be copied</param>
        /// <param name="aTargetFileSource">File source to which the files will be copied</param>
        /// <param name="theSourceFiles">Files which are to be copied</param>
        /// <param name="aTargetPath">Path in the target file source where the files should be copied to</param>
        public FileSourceCopyOperation(IFileSource aSourceFileSource, IFileSource aTargetFileSource, 
                                      FileEntries theSourceFiles, string aTargetPath)
            : base(GetOperationFileSource(aSourceFileSource, aTargetFileSource))
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

            _sourceFileSource = aSourceFileSource;
            _targetFileSource = aTargetFileSource;
            _sourceFiles = theSourceFiles ?? new FileEntries();
            _targetPath = Helper.IncludeTrailingPathDelimiter(aTargetPath, aTargetFileSource.PathSep);

            _renameMask = "";

            // Set default copy time option based on global setting
            if (GlobalSettings.OperationOptionCopyTime)
                _copyAttributesOptions |= CopyAttributesOption.CopyTime;
        }

        /// <summary>
        /// Gets the appropriate file source for the operation based on operation type
        /// </summary>
        private static IFileSource GetOperationFileSource(IFileSource sourceFileSource, IFileSource targetFileSource)
        {
            // For CopyIn operations, run on target
            // For CopyOut operations, run on source
            // Default to target for regular copy
            return targetFileSource;
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
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            _targetFileSource.Reload(new[] { _targetPath });
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
                        return string.Format("Copying {0} to {1}", SourceFiles[0].Name, TargetPath);
                    else
                        return string.Format("Copying from {0} to {1}", SourceFiles.Count > 0 ? SourceFiles[0].Path : "", TargetPath);
                default:
                    return "Copying";
            }
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
        /// Shows the compare files UI
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="targetFilePath">The target file path</param>
        protected void ShowCompareFilesUI(FileEntry sourceFile, string targetFilePath)
        {
            // Implementation would depend on UI framework
            // This is a placeholder for the actual implementation
        }
    }

    /// <summary>
    /// Operation that copies files from another file source into a file source of specific type
    /// (to file system for TFileSystemCopyInOperation,
    /// to network for TNetworkCopyInOperation, etc.).
    /// </summary>
    public abstract class FileSourceCopyInOperation : FileSourceCopyOperation
    {
        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.CopyIn;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceCopyInOperation"/> class
        /// </summary>
        /// <param name="aSourceFileSource">File source from which the files will be copied</param>
        /// <param name="aTargetFileSource">File source to which the files will be copied</param>
        /// <param name="theSourceFiles">Files which are to be copied</param>
        /// <param name="aTargetPath">Path in the target file source where the files should be copied to</param>
        public FileSourceCopyInOperation(IFileSource aSourceFileSource, IFileSource aTargetFileSource,
                                        FileEntries theSourceFiles, string aTargetPath)
            : base(aSourceFileSource, aTargetFileSource, theSourceFiles, aTargetPath)
        {
        }
    }

    /// <summary>
    /// Operation that copies files into another file source from a file source of specific type
    /// (from file system for TFileSystemCopyOutOperation,
    /// from network for TNetworkCopyOutOperation, etc.).
    /// </summary>
    public abstract class FileSourceCopyOutOperation : FileSourceCopyOperation
    {
        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.CopyOut;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceCopyOutOperation"/> class
        /// </summary>
        /// <param name="aSourceFileSource">File source from which the files will be copied</param>
        /// <param name="aTargetFileSource">File source to which the files will be copied</param>
        /// <param name="theSourceFiles">Files which are to be copied</param>
        /// <param name="aTargetPath">Path in the target file source where the files should be copied to</param>
        public FileSourceCopyOutOperation(IFileSource aSourceFileSource, IFileSource aTargetFileSource,
                                         FileEntries theSourceFiles, string aTargetPath)
            : base(aSourceFileSource, aTargetFileSource, theSourceFiles, aTargetPath)
        {
        }
    }

}