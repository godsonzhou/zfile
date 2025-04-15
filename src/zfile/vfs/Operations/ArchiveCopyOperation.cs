namespace Zfile.Operations
{
    /// <summary>
    /// Extract flag enumeration
    /// </summary>
    [Flags]
    public enum ExtractFlag
    {
        /// <summary>
        /// Smart extract
        /// </summary>
        SmartExtract = 1
    }

    /// <summary>
    /// Archive copy in operation
    /// </summary>
    public abstract class ArchiveCopyInOperation : FileSourceCopyInOperation
    {
        /// <summary>
        /// Local copy of statistics
        /// </summary>
        protected FileSourceCopyOperationStatistics statistics;

        /// <summary>
        /// Packing flags passed to plugin
        /// </summary>
        protected int packingFlags;

        /// <summary>
        /// Full list of files (recursive)
        /// </summary>
        protected FileEntries fullFilesTree;

        /// <summary>
        /// Create new archive
        /// </summary>
        protected bool createNew;

        /// <summary>
        /// Create TAR archive first
        /// </summary>
        protected bool tarBefore;

        /// <summary>
        /// Temporary TAR archive name
        /// </summary>
        protected string tarFileName;

        /// <summary>
        /// Gets or sets a value indicating whether to create new archive
        /// </summary>
        public bool CreateNew
        {
            get { return createNew; }
            set { createNew = value; }
        }

        /// <summary>
        /// Reload file sources
        /// </summary>
        protected override void DoReloadFileSources()
        {
            if (!createNew)
                base.DoReloadFileSources();
        }
    }

    /// <summary>
    /// Archive copy out operation
    /// </summary>
    public abstract class ArchiveCopyOutOperation : FileSourceCopyOutOperation
    {
        /// <summary>
        /// Extract mask
        /// </summary>
        protected string extractMask;

        /// <summary>
        /// Extract flags
        /// </summary>
        protected ExtractFlag extractFlags;

        /// <summary>
        /// Gets or sets the extract mask
        /// </summary>
        public string ExtractMask
        {
            get { return extractMask; }
            set { extractMask = value; }
        }

        /// <summary>
        /// Gets or sets the extract flags
        /// </summary>
        public ExtractFlag ExtractFlags
        {
            get { return extractFlags; }
            set { extractFlags = value; }
        }
    }
}