namespace zfile
{
    public interface IRecycleBinFileSource : IVirtualFileSource
    {
    }

    public class RecycleBinFileSource : VirtualFileSource, IRecycleBinFileSource
    {
        protected override bool SetCurrentWorkingDirectory(string newDir)
        {
            return IsPathAtRoot(newDir);
        }

        public static bool IsSupportedPath(string path)
        {
            return string.Equals(Path.GetDirectoryName(path), 
                Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + 
                Path.DirectorySeparatorChar + Resources.VfsRecycleBin, 
                StringComparison.OrdinalIgnoreCase);
        }

        public static FileEntry CreateFile(string path)
        {
            var file = new FileEntry(path);
            file.Attributes = new FileAttributesProperty();
            file.Size = new FileSizeProperty();
            file.ModificationTime = new FileModificationDateTimeProperty();
            file.CreationTime = new FileCreationDateTimeProperty();
            file.LastAccessTime = new FileLastAccessDateTimeProperty();
            file.ChangeTime = new FileChangeDateTimeProperty();
            file.Link = new FileLinkProperty();
            file.Comment = new FileCommentProperty();
            return file;
        }

        public static bool GetMainIcon(out string path)
        {
            path = "%SystemRoot%\\System32\\shell32.dll,31";
            return true;
        }

        public override FileSourceOperationTypes GetOperationsTypes()
        {
            return FileSourceOperationTypes.List;
        }

        public override FilePropertyType GetSupportedFileProperties()
        {
            return base.GetSupportedFileProperties() |
                   FilePropertyType.Size |
                   FilePropertyType.Attributes |
                   FilePropertyType.ModificationTime |
                   FilePropertyType.CreationTime |
                   FilePropertyType.LastAccessTime |
                   FilePropertyType.ChangeTime |
                   FilePropertyType.Link |
                   FilePropertyType.Comment;
        }

        public override bool GetLocalName(ref FileInfo file)
        {
            file.FullPath = file.Link.LinkTo;
            return true;
        }

        public override string GetRootDir(string path)
        {
            return Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + 
                   Path.DirectorySeparatorChar + Resources.VfsRecycleBin + 
                   Path.DirectorySeparatorChar;
        }

        public override FileSourceProperties GetProperties()
        {
            return FileSourceProperties.DirectAccess | 
                   FileSourceProperties.Virtual | 
                   FileSourceProperties.LinkToLocalFiles;
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            IFileSource targetFileSource = this;
            return new RecycleBinListOperation(targetFileSource, targetPath);
        }
    }
} 