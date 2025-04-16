namespace zfile
{
    /// <summary>
    /// Represents the description details for file source operations
    /// </summary>
    //public enum FileSourceOperationDescriptionDetails
    //{
    //    /// <summary>
    //    /// Basic description
    //    /// </summary>
    //    Basic,
        
    //    /// <summary>
    //    /// Job and target description
    //    /// </summary>
    //    JobAndTarget
    //}
    
    /// <summary>
    /// Represents the result of a file source execute operation
    /// </summary>
    public enum FileSourceExecuteOperationResult
    {
        /// <summary>The command was executed successfully</summary>
        Success,
        
        /// <summary>Execution failed</summary>
        Error,
        
        /// <summary>Cancelled by user (nothing happened)</summary>
        Cancelled,
        
        /// <summary>DC should download/extract the file and execute it locally</summary>
        YourSelf,
        
        /// <summary>DC should download/extract all files and execute chosen file locally</summary>
        WithAll,
        
        /// <summary>This was a (symbolic) link or .lnk file pointing to a different directory</summary>
        SymLink
    }

    /// <summary>
    /// Represents a file source execute operation
    /// </summary>
    public abstract class FileSourceExecuteOperation : FileSourceOperation
    {
        private IFileSource _fileSource;
        private string _currentPath;
        private FileEntry _executableFile;
        private string _absolutePath;
        private string _relativePath;
        private string _verb;
        
        /// <summary>
        /// Gets the current path
        /// </summary>
        public string CurrentPath => _currentPath;
        
        /// <summary>
        /// Gets the executable file
        /// </summary>
        public FileEntry ExecutableFile => _executableFile;
        
        /// <summary>
        /// Gets or sets the result string
        /// </summary>
        public string ResultString { get; set; }
        
        /// <summary>
        /// Gets the absolute path
        /// </summary>
        public string AbsolutePath => _absolutePath;
        
        /// <summary>
        /// Gets the relative path
        /// </summary>
        public string RelativePath => _relativePath;
        
        /// <summary>
        /// Gets the verb
        /// </summary>
        public string Verb => _verb;
        
        /// <summary>
        /// Gets the execute operation result
        /// </summary>
        public FileSourceExecuteOperationResult ExecuteOperationResult { get; protected set; }
        
        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceExecuteOperation"/> class
        /// </summary>
        /// <param name="targetFileSource">File source where the file should be executed</param>
        /// <param name="executableFile">File that should be executed</param>
        /// <param name="currentPath">Path of the file source where the execution should take place</param>
        /// <param name="verb">The verb to use for execution</param>
        public FileSourceExecuteOperation(IFileSource targetFileSource, FileEntry executableFile, string currentPath, string verb)
            : base(targetFileSource)
        {
            _fileSource = targetFileSource;
            _currentPath = currentPath;
            _executableFile = executableFile;
            _verb = verb;
            ExecuteOperationResult = FileSourceExecuteOperationResult.Cancelled;
            
            _absolutePath = _executableFile.FullPath;
            _relativePath = _executableFile.Name;
        }
        
        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.Execute;
        
        /// <summary>
        /// Updates statistics at start time
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            // Empty implementation
        }
        
        /// <summary>
        /// Reloads file sources
        /// </summary>
        protected override void DoReloadFileSources()
        {
            if (ExecuteOperationResult != FileSourceExecuteOperationResult.Cancelled)
            {
                _fileSource.Reload(_currentPath);
            }
        }
        
        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        public override string Description => GetDescription(FileSourceOperationDescriptionDetails.Basic);
        
        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        /// <param name="details">The description details</param>
        /// <returns>The description</returns>
        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            switch (details)
            {
                case FileSourceOperationDescriptionDetails.JobAndTarget:
                    return string.Format("Executing {0}", ExecutableFile.Name);
                default:
                    return "Executing";
            }
        }
    }
}