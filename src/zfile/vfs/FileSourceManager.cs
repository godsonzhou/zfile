using zfile.vfs.Operations;

namespace zfile
{
    /// <summary>
    /// 管理不同类型的文件源，并根据路径提供合适的文件源实例
    /// </summary>
    public partial class FileSourceManager
    {
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
        /// 根据路径获取合适的文件源
        /// </summary>
        //public IFileSource GetFileSourceForPath(string path)
        //{
        //    if (string.IsNullOrEmpty(path))
        //        return new FileSystemFileSource();

        //    // 检查是否是回收站
        //    if (path == "回收站" || path.StartsWith("回收站\\"))
        //        return new RecycleBinFileSource();

        //    // 检查是否是控制面板
        //    if (path == "控制面板" || path.StartsWith("控制面板\\"))
        //        return new ControlPanelFileSource();

        //    // 检查是否是FTP路径
        //    if (_ftpManager.IsFtpPath(path))
        //    {
        //        var ftpSource = _ftpManager.GetFtpSource(path);
        //        if (ftpSource != null)
        //            return ftpSource;
        //    }

        //    // 检查是否是压缩文件
        //    if (IsArchiveFile(path))
        //    {
        //        return WcxArchiveFileSource.CreateByArchiveName(new FileSystemFileSource(), path);
        //    }

        //    // 默认使用文件系统文件源
        //    return new FileSystemFileSource();
        //}

        /// <summary>
        /// 检查文件是否是压缩文件
        /// </summary>
        //public bool IsArchiveFile(string path)
        //{
        //    if (string.IsNullOrEmpty(path) || !File.Exists(path))
        //        return false;

        //    string ext = Path.GetExtension(path).ToLower();
        //    return _wcxModuleList.GetModuleByExt(ext) != null;
        //}

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
