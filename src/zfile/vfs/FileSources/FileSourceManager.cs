using System.IO;

namespace zfile
{
    /// <summary>
    /// Manages file sources
    /// </summary>
    public partial class FileSourceManager
    {
        //private static FileSourceManager _instance;
        private readonly List<IFileSource> _fileSources = new List<IFileSource>();
        private readonly object _syncRoot = new object();
        private WcxModuleList _wcxModuleList;
        private FTPMGR _ftpManager;
		/// <summary>
		/// 管理不同类型的文件源，并根据路径提供合适的文件源实例
		/// </summary>

		private static readonly Lazy<FileSourceManager> _instance = new Lazy<FileSourceManager>(() => new FileSourceManager());
		public static FileSourceManager Instance => _instance.Value;

		//private WcxModuleList _wcxModuleList;
		//private FTPMGR _ftpManager;
		private readonly Dictionary<string, Type> _fileSourceTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

		//private FileSourceManager()
		//{
		//}

		/// <summary>
		/// 初始化 FileSourceManager
		/// </summary>
		public void Initialize(WcxModuleList wcxModuleList, FTPMGR ftpManager)
		{
			_wcxModuleList = wcxModuleList;
			_ftpManager = ftpManager;

			// 注册默认文件源类型
			RegisterFileSourceType<FileSystemFileSource>("filesystem");
			RegisterFileSourceType<RecycleBinFileSource>("recycleBin");
			RegisterFileSourceType<WcxArchiveFileSource>("archive");
			RegisterFileSourceType<FtpFileSource>("ftp");
			RegisterFileSourceType<ControlPanelFileSource>("controlpanel");
		}

		/// <summary>
		/// 注册文件源类型
		/// </summary>
		public void RegisterFileSourceType<T>(string typeName) where T : IFileSource
		{
			_fileSourceTypes[typeName] = typeof(T);
		}

		/// <summary>
		/// Gets the singleton instance of the FileSourceManager
		/// </summary>
		//public static FileSourceManager Instance
		//{
		//    get
		//    {
		//        if (_instance == null)
		//        {
		//            _instance = new FileSourceManager();
		//        }
		//        return _instance;
		//    }
		//}

		/// <summary>
		/// Creates a new instance of the FileSourceManager class
		/// </summary>
		private FileSourceManager()
        {
        }

        /// <summary>
        /// Initializes the FileSourceManager with required dependencies
        /// </summary>
        /// <param name="wcxModuleList">WCX module list for archive handling</param>
        /// <param name="ftpManager">FTP manager for FTP connections</param>
        //public void Initialize(WcxModuleList wcxModuleList, FTPMGR ftpManager)
        //{
        //    _wcxModuleList = wcxModuleList;
        //    _ftpManager = ftpManager;
        //}

        /// <summary>
        /// Checks if a path is an archive file
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the path is an archive file</returns>
        public bool IsArchiveFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            string ext = Path.GetExtension(path).ToLower();
            return _wcxModuleList?.GetModuleByExt(ext) != null;
        }

        /// <summary>
        /// Gets the appropriate file source for the given path
        /// </summary>
        /// <param name="path">Path to get file source for</param>
        /// <returns>A file source that can handle the path</returns>
        public IFileSource GetFileSourceForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return new FileSystemFileSource();

            // Check for existing file source first
            var existingFileSource = _fileSources.FirstOrDefault(fs =>
                string.Equals(fs.CurrentPath, path, StringComparison.OrdinalIgnoreCase));

            if (existingFileSource != null)
                return existingFileSource;

            // Check for recycle bin
            if (path == "回收站" || path.Contains(Resources.VfsRecycleBin))
                return new RecycleBinFileSource();

            // Check for control panel
            if (path == "控制面板" || path.StartsWith("controlpanel://"))
                return new ControlPanelFileSource();

            // Check for FTP path
            if (_ftpManager != null && _ftpManager.IsFtpPath(path))
            {
                return _ftpManager.GetFtpSource(path);
            }

            // Check for archive file
            if (IsArchiveFile(path))
            {
                return WcxArchiveFileSource.CreateByArchiveName(new FileSystemFileSource(), path);
            }

            // Default to file system
            return new FileSystemFileSource();
        }

        /// <summary>
        /// Creates an appropriate copy operation between two file sources
        /// </summary>
        /// <param name="sourceFileSource">Source file source</param>
        /// <param name="targetFileSource">Target file source</param>
        /// <param name="sourceFiles">Files to copy</param>
        /// <param name="targetPath">Target path</param>
        /// <returns>A file source operation for copying</returns>
        public FileSourceOperation CreateCopyOperation(
            IFileSource sourceFileSource,
            IFileSource targetFileSource,
            FileEntries sourceFiles,
            string targetPath)
        {
            // If source and target are the same type, use regular copy
            if (sourceFileSource.GetType() == targetFileSource.GetType())
            {
                return sourceFileSource.CreateCopyOperation(sourceFiles, targetPath);
            }
            // If target is an archive, use copy in
            else if (targetFileSource is IArchiveFileSource)
            {
                return targetFileSource.CreateCopyInOperation(sourceFileSource, sourceFiles, targetPath);
            }
            // If source is an archive, use copy out
            else if (sourceFileSource is IArchiveFileSource)
            {
                return sourceFileSource.CreateCopyOutOperation(targetFileSource, sourceFiles, targetPath);
            }
            // Otherwise try to use target's copy in
            else
            {
                return targetFileSource.CreateCopyInOperation(sourceFileSource, sourceFiles, targetPath);
            }
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
        public IFileSource? Find(Type fileSourceClass, string address, bool caseSensitive = true)
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

		/// <summary>
		/// 创建适合的复制操作
		/// </summary>
		//public FileSourceOperation CreateCopyOperation(
		//    IFileSource sourceFileSource,
		//    IFileSource targetFileSource,
		//    FileEntries fileEntries,
		//    string targetPath)
		//{
		//    // 如果源和目标是同一类型的文件源，使用源文件源的复制操作
		//    if (sourceFileSource.GetType() == targetFileSource.GetType())
		//    {
		//        return sourceFileSource.CreateCopyOperation(fileEntries, targetPath);
		//    }

		//    // 如果是不同类型的文件源，创建跨文件源复制操作
		//    return new CrossFileSourceCopyOperation(sourceFileSource, targetFileSource, fileEntries, targetPath);
		//}
	}
}