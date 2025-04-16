namespace zfile
{
    /// <summary>
    /// Operation that lists files in a file source
    /// </summary>
    public abstract class FileSourceListOperation : FileSourceOperation
    {
        private IFileSource _fileSource;
        private string _path;
        private FileEntries _files;
        private bool _flatView;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationType OperationType => FileSourceOperationType.List;

        /// <summary>
        /// Gets the file source
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the files
        /// </summary>
        public FileEntries Files { get  => _files; set => _files = value; }

        /// <summary>
        /// Gets the path
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// Gets or sets whether to use flat view
        /// </summary>
        public bool FlatView
        {
            get => _flatView;
            set => _flatView = value;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceListOperation"/> class
        /// </summary>
        /// <param name="aFileSource">File source to list files from</param>
        /// <param name="aPath">Path to list files from</param>
        public FileSourceListOperation(IFileSource aFileSource, string aPath)
            : base(aFileSource)
        {
            _fileSource = aFileSource;
            _path = aPath;
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_files != null)
                {
                    _files.Clear();
                    _files = null;
                }
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
                    return string.Format("Listing in {0}", Path);
                default:
                    return "Listing";
            }
        }

        /// <summary>
        /// Gets the files
        /// </summary>
        /// <returns>The files</returns>
        protected FileEntries GetFiles()
        {
            return _files;
        }

        /// <summary>
        /// Retrieves files and revokes ownership of the list
        /// </summary>
        /// <returns>The files</returns>
        public FileEntries ReleaseFiles()
        {
            var result = _files;
            _files = null; // revoke ownership
            return result;
        }

        /// <summary>
        /// Updates the statistics at the start time of the operation
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            // Empty
        }
    }
}