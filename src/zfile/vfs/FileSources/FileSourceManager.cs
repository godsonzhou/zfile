namespace zfile
{
    /// <summary>
    /// Manages file sources
    /// </summary>
    public class FileSourceManager
    {
        private static FileSourceManager _instance;
        private readonly List<IFileSource> _fileSources = new List<IFileSource>();
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Gets the singleton instance of the FileSourceManager
        /// </summary>
        public static FileSourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FileSourceManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates a new instance of the FileSourceManager class
        /// </summary>
        private FileSourceManager()
        {
        }

        /// <summary>
        /// Adds a file source to the manager
        /// </summary>
        /// <param name="fileSource">The file source to add</param>
        public void Add(IFileSource fileSource)
        {
            if (fileSource == null)
                return;

            lock (_syncRoot)
            {
                if (!_fileSources.Contains(fileSource))
                {
                    _fileSources.Add(fileSource);
                }
            }
        }

        /// <summary>
        /// Removes a file source from the manager
        /// </summary>
        /// <param name="fileSource">The file source to remove</param>
        public void Remove(IFileSource fileSource)
        {
            if (fileSource == null)
                return;

            lock (_syncRoot)
            {
                _fileSources.Remove(fileSource);
            }
        }

        /// <summary>
        /// Finds a file source by class type and address
        /// </summary>
        /// <param name="fileSourceClass">The file source class type</param>
        /// <param name="address">The address</param>
        /// <param name="caseSensitive">Whether the address comparison is case sensitive</param>
        /// <returns>The file source if found, null otherwise</returns>
        public IFileSource Find(Type fileSourceClass, string address, bool caseSensitive = true)
        {
            if (fileSourceClass == null || string.IsNullOrEmpty(address))
                return null;

            lock (_syncRoot)
            {
                StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                return _fileSources.FirstOrDefault(fs =>
                    fs.IsClass(fileSourceClass) &&
                    string.Equals(fs.CurrentAddress, address, comparison));
            }
        }

        /// <summary>
        /// Gets all file sources
        /// </summary>
        /// <returns>The list of file sources</returns>
        public List<IFileSource> GetAllFileSources()
        {
            lock (_syncRoot)
            {
                return new List<IFileSource>(_fileSources);
            }
        }
    }
}