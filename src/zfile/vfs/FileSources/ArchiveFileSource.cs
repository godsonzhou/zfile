namespace Zfile.FileSources
{
    /// <summary>
    /// Interface for archive file sources
    /// </summary>
    public interface IArchiveFileSource : ILocalFileSource
    {
        /// <summary>
        /// Checks if the archive file has changed
        /// </summary>
        /// <returns>True if the archive file has changed, false otherwise</returns>
        bool Changed();

        /// <summary>
        /// Gets the packer used for this archive
        /// </summary>
        string Packer { get; }

        /// <summary>
        /// Gets the full path to the archive file on the parent file source
        /// </summary>
        string ArchiveFileName { get; }
    }

    /// <summary>
    /// Base class for archive file sources
    /// </summary>
    public abstract class ArchiveFileSource : LocalFileSource, IArchiveFileSource
    {
        private FileAttributes _attributes;
        private long _fileSize;
        private DateTime _lastWriteTime;

        /// <summary>
        /// Gets the packer used for this archive
        /// </summary>
        public abstract string Packer { get; }

        /// <summary>
        /// Gets the full path to the archive file on the parent file source
        /// </summary>
        public string ArchiveFileName => CurrentAddress;

        /// <summary>
        /// Creates a new instance of the ArchiveFileSource class
        /// </summary>
        /// <param name="archiveFileSource">File source that stores the archive</param>
        /// <param name="archiveFileName">Full path to the archive on the archiveFileSource</param>
        protected ArchiveFileSource(IFileSource archiveFileSource, string archiveFileName) : base()
        {
            CurrentAddress = archiveFileName;
            ParentFileSource = archiveFileSource;
            
            // Store initial file attributes
            var fileInfo = new FileInfo(archiveFileName);
            if (fileInfo.Exists)
            {
                _attributes = fileInfo.Attributes;
                _fileSize = fileInfo.Length;
                _lastWriteTime = fileInfo.LastWriteTime;
            }
        }

        /// <summary>
        /// Creates a file object with the appropriate properties
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <returns>A new file object</returns>
        public static new FileEntry CreateFile(string path)
        {
            var result = new FileEntry(path);

            // Set file properties
            result.Size = 0;
            result.CompressedSize = 0;
            result.Attributes = FileAttributes.Normal;
            result.ModificationTime = DateTime.MinValue;

            return result;
        }

        /// <summary>
        /// Checks if the archive file has changed
        /// </summary>
        /// <returns>True if the archive file has changed, false otherwise</returns>
        public bool Changed()
        {
            var fileInfo = new FileInfo(ArchiveFileName);
            if (!fileInfo.Exists)
            {
                return false;
            }
            
            bool result = (fileInfo.Length != _fileSize) || 
                          (fileInfo.LastWriteTime != _lastWriteTime);

            if (result)
            {
                // Update stored attributes if changed
                _attributes = fileInfo.Attributes;
                _fileSize = fileInfo.Length;
                _lastWriteTime = fileInfo.LastWriteTime;
            }

            return result;
        }

        /// <summary>
        /// Gets the supported file properties
        /// </summary>
        /// <returns>The supported file properties</returns>
        public override FilePropertyType RetrievableFileProperties
        {
            get
            {
                return base.RetrievableFileProperties | 
                       FilePropertyType.Size | 
                       FilePropertyType.CompressedSize | 
                       FilePropertyType.Attributes | 
                       FilePropertyType.ModificationTime;
            }
        }
    }
}