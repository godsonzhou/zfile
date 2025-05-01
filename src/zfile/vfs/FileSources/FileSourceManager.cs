using System.IO;

namespace zfile
{
    /// <summary>
    /// Manages file sources
    /// </summary>
    public partial class FileSourceManager
    {
		private MainForm _mainform;
		//private static FileSourceManager _instance;
		private List<IFileSource> _fileSources => _mainform.isleft ? _leftPanelFileSources.Values.ToList() : _rightPanelFileSources.Values.ToList();
        private readonly object _syncRoot = new object();
        private WcxModuleList _wcxModuleList;
        private FTPMGR _ftpManager;
        /// <summary>
        /// 管理不同类型的文件源，并根据路径提供合适的文件源实例
        /// </summary>

        private static readonly Lazy<FileSourceManager> _instance = new Lazy<FileSourceManager>(() => new FileSourceManager());
        public static FileSourceManager Instance => _instance.Value;

        // 左右面板的FileSource缓存，键为路径，值为FileSource实例
        private readonly Dictionary<string, IFileSource> _leftPanelFileSources = new Dictionary<string, IFileSource>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IFileSource> _rightPanelFileSources = new Dictionary<string, IFileSource>(StringComparer.OrdinalIgnoreCase);

        //private WcxModuleList _wcxModuleList;
        //private FTPMGR _ftpManager;
        private readonly Dictionary<string, Type> _fileSourceTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        //private FileSourceManager()
        //{
        //}

        /// <summary>
        /// 初始化 FileSourceManager
        /// </summary>
        public void Initialize(WcxModuleList wcxModuleList, FTPMGR ftpManager, MainForm mainform)
        {
			_mainform = mainform;
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
        /// <param name="isLeftPanel">True if this is for the left panel, false for the right panel</param>
        /// <returns>A file source that can handle the path</returns>
        public IFileSource GetFileSourceForPath(string path, bool isLeftPanel)
		{
			// 获取对应面板的缓存
			var panelCache = isLeftPanel ? _leftPanelFileSources : _rightPanelFileSources;

            // 如果路径为空，返回默认的FileSystemFileSource
            if (string.IsNullOrEmpty(path))
            {
                var rootPath = "C:\\";
                var cacheKey = $"filesystem:{rootPath}";

                // 检查缓存中是否已有此路径的FileSource
                if (panelCache.TryGetValue(cacheKey, out var cachedSource))
                    return cachedSource;

                // 创建新的FileSystemFileSource
                var newSource = new FileSystemFileSource();
                newSource.SetRootPath(rootPath);

                // 添加到缓存
                panelCache[cacheKey] = newSource;
                return newSource;
            }

            // 检查缓存中是否已有此路径的FileSource
            if (panelCache.TryGetValue(path, out var fileSource))
                return fileSource;

            // 检查是否是压缩文件内部路径
            // 遍历所有已存在的文件源，查找是否有WcxArchiveFileSource包含当前路径
            var archiveFileSource = _fileSources.FirstOrDefault(fs =>
                fs is WcxArchiveFileSource wcxArchiveFileSource &&
                path.StartsWith(wcxArchiveFileSource.ArchivePath, StringComparison.OrdinalIgnoreCase));
            if (archiveFileSource != null)
            {
                // 添加到缓存
                panelCache[path] = archiveFileSource;
                return archiveFileSource;
            }

            // Check for archive file
            if (IsArchiveFile(path))
            {
                // 为压缩文件创建新的FileSystemFileSource作为基础文件源
                var dirPath = Path.GetDirectoryName(path) ?? "C:\\";
                var baseFileSource = GetFileSourceForPath(dirPath, isLeftPanel);
                var archiveSource = WcxArchiveFileSource.CreateByArchiveName(baseFileSource, path);

                // 添加到缓存
                panelCache[path] = archiveSource;
                return archiveSource;
            }

            // Check for existing file source first
            var existingFileSource = _fileSources.FirstOrDefault(fs =>
                fs is not WcxArchiveFileSource &&
                path.StartsWith(fs.GetRootDir(), StringComparison.OrdinalIgnoreCase));
            if (existingFileSource != null)
            {
                // 添加到缓存
                panelCache[path] = existingFileSource;
                return existingFileSource;
            }

            // Check for recycle bin
            if (path == "回收站" || (Resources.VfsRecycleBin != null && path.Contains(Resources.VfsRecycleBin)))
            {
                var recycleBinSource = new RecycleBinFileSource();

                // 添加到缓存
                panelCache[path] = recycleBinSource;
                return recycleBinSource;
            }

            // Check for control panel
            if (path == "控制面板" || path.StartsWith("controlpanel://"))
            {
                var controlPanelSource = new ControlPanelFileSource();

                // 添加到缓存
                panelCache[path] = controlPanelSource;
                return controlPanelSource;
            }

            // Check for FTP path
            if (_ftpManager != null && _ftpManager.IsFtpPath(path))
            {
                var ftpSource = _ftpManager.GetFtpSource(path);

                // 添加到缓存
                if (ftpSource != null)
                {
                    panelCache[path] = ftpSource;
                    return ftpSource;
                }

                // 如果无法获取FTP源，则返回默认的文件系统源
                var defaultFs = new FileSystemFileSource();
                defaultFs.SetRootPath("C:\\");
                return defaultFs;
            }

            // Default to file system
            var drive = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(drive))
                drive = "C:\\";

			var lr = isLeftPanel ? 'l' : 'r';
			var cacheKeyFs = path; //$"{lr}filesystem:\\\\{drive}";

            // 检查缓存中是否已有此驱动器的FileSource
            if (panelCache.TryGetValue(cacheKeyFs, out var cachedFsSource))
            {
                // 更新CurrentPath
                cachedFsSource.CurrentPath = path;
                return cachedFsSource;
            }

            // 创建新的FileSystemFileSource
            var fileSystemSource = new FileSystemFileSource();
            fileSystemSource.SetRootPath(drive);
            fileSystemSource.CurrentPath = path;

            // 添加到缓存
            panelCache[cacheKeyFs] = fileSystemSource;

            return fileSystemSource;
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