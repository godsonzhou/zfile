namespace zfile
{
    /// <summary>
    /// Operation that creates a directory in a file source
    /// </summary>
    public abstract class FileSourceCreateDirectoryOperation : FileSourceOperation
    {
        private IFileSource _fileSource;
        private string _basePath;
        private string _directoryPath;
        private string _absolutePath;
        private string _relativePath;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.CreateDirectory;

        /// <summary>
        /// Gets the base path
        /// </summary>
        protected string BasePath => _basePath;

        /// <summary>
        /// Gets the directory path
        /// </summary>
        protected string DirectoryPath => _directoryPath;

        /// <summary>
        /// Gets the absolute path
        /// </summary>
        protected string AbsolutePath => _absolutePath;

        /// <summary>
        /// Gets the relative path
        /// </summary>
        protected string RelativePath => _relativePath;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceCreateDirectoryOperation"/> class
        /// </summary>
        /// <param name="aTargetFileSource">File source where the directory should be created</param>
        /// <param name="aCurrentPath">Absolute path to current directory where the new directory
        /// should be created (if its path is not absolute)</param>
        /// <param name="aDirectoryPath">Absolute or relative (to TargetFileSource.CurrentPath) path
        /// to a directory that should be created</param>
        public FileSourceCreateDirectoryOperation(IFileSource aTargetFileSource, string aCurrentPath, string aDirectoryPath)
            : base(aTargetFileSource)
        {
            _fileSource = aTargetFileSource;
            _basePath = aCurrentPath;
            _directoryPath = aDirectoryPath;

            if (_fileSource.GetPathType(_directoryPath) == PathType.Absolute)
            {
                _absolutePath = _directoryPath;
                _relativePath = Helper.ExtractDirLevel(aCurrentPath, _directoryPath);
            }
            else
            {
                _absolutePath = aCurrentPath + Path.DirectorySeparatorChar + _directoryPath;
                _relativePath = _directoryPath;
            }
        }

        /// <summary>
        /// Updates the statistics at the start time of the operation
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            // empty
        }

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected override void DoReloadFileSources()
        {
            _fileSource.Reload(new[] { _fileSource.GetParentDir(_absolutePath) });
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
                    return string.Format("Creating directory {0}", AbsolutePath);
                default:
                    return "Creating directory";
            }
        }
    }
}