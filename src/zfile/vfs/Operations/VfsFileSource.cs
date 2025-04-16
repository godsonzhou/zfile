namespace zfile
{
    public interface IVfsFileSource : IVirtualFileSource
    {
        WfxModuleList VfsFileEntries { get; }
    }

    public class VfsFileSource : VirtualFileSource, IVfsFileSource
    {
        private readonly WfxModuleList _wfxModuleList;

        public WfxModuleList VfsFileEntries => _wfxModuleList;

        public VfsFileSource(WfxModuleList wfxModuleList)
        {
            _wfxModuleList = new WfxModuleList();
            _wfxModuleList.Assign(wfxModuleList);
        }

        ~VfsFileSource()
        {
            _wfxModuleList?.Dispose();
        }

        public static FileEntry CreateFile(string path)
        {
            var result = new FileEntry(path);
            result.LinkProperty = new FileLinkProperty();
            result.AttributesProperty = new NtfsFileAttributesProperty();
            return result;
        }

        public override FileSourceOperationType GetOperationsTypes()
        {
            return FileSourceOperationType.List | FileSourceOperationType.Execute;
        }

        public override FileSourceProperties GetProperties()
        {
            return FileSourceProperties.Virtual;
        }

        public override string GetRootDir(string path)
        {
            return "vfs:" + Path.DirectorySeparatorChar;
        }

        protected override FilePropertiesTypes GetSupportedFileProperties()
        {
            return base.GetSupportedFileProperties() | FilePropertiesTypes.Attributes | FilePropertiesTypes.Link;
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            IFileSource targetFileSource = this;
            return new VfsListOperation(targetFileSource, targetPath);
        }

        public override FileSourceOperation CreateExecuteOperation(ref FileEntry executableFile, string basePath, string verb)
        {
            IFileSource targetFileSource = this;
            return new VfsExecuteOperation(targetFileSource, ref executableFile, basePath, verb);
        }
    }
} 