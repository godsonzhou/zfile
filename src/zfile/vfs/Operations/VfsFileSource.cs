namespace zfile
{
    public interface IVfsFileSource : IVirtualFileSource
    {
        WfxModuleList VfsFileEntries { get; }
    }

	public partial class VfsFileSource : VirtualFileSource, IVfsFileSource
	{
        private readonly WfxModuleList _wfxModuleList;

        public WfxModuleList VfsFileEntries => _wfxModuleList;

        public VfsFileSource(WfxModuleList wfxModuleList)
        {
            _wfxModuleList = new WfxModuleList("");
        }

        ~VfsFileSource()
        {
            _wfxModuleList?.Dispose();
        }
		/// <summary>
		/// Creates a new VfsFileSource
		/// </summary>
		public VfsFileSource()
		{
			// Initialize with default values
		}

		/// <summary>
		/// Checks if the path is supported by this file source
		/// </summary>
		public override bool IsSupportedPath(string path)
		{
			return path.StartsWith("vfs:", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Initializes the file source
		/// </summary>
		public void Initialize()
		{
			// Load VFS modules
		}

		/// <summary>
		/// Creates a file object for the specified path
		/// </summary>
		//public static FileEntry CreateFile(string path)
		//{
		//    return new FileEntry(path);
		//}

		/// <summary>
		/// Gets the main icon for this file source
		/// </summary>
		public static bool GetMainIcon(out string path)
		{
			path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "vfs.ico");
			return File.Exists(path);
		}
		public static FileEntry CreateFile(string path)
        {
            var result = new FileEntry(path);
            result.LinkProperty = new FileLinkProperty();
            //result.AttributesProperty = new NtfsFileAttributesProperty();
            return result;
        }

        public FileSourceOperationTypes GetOperationsTypes()
        {
            return FileSourceOperationTypes.List | FileSourceOperationTypes.Execute;
        }

        public FileSourceProperties GetProperties()
        {
            return FileSourceProperties.Virtual;
        }

        public override string GetRootDir(string path)
        {
            return "vfs:" + Path.DirectorySeparatorChar;
        }

        protected FilePropertyType GetSupportedFileProperties()
        {
            return base.SupportedFileProperties | FilePropertyType.Attributes | FilePropertyType.Link;
        }

        public FileSourceOperation CreateListOperation(string targetPath)
        {
            IFileSource targetFileSource = this;
            return new VfsListOperation(targetFileSource, targetPath);
        }

        public FileSourceOperation CreateExecuteOperation(ref FileEntry executableFile, string basePath, string verb)
        {
            IFileSource targetFileSource = this;
            return new VfsExecuteOperation(targetFileSource, ref executableFile, basePath, verb);
        }
    }
} 