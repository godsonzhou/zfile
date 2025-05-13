using System.Runtime.InteropServices;

namespace zfile
{
	[ComImport]
	[Guid("1E598290-5E66-423C-BB55-333E293106E8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IRecycleBinFileSource : IVirtualFileSource
    {
    }

    public class RecycleBinFileSource : VirtualFileSource, IRecycleBinFileSource
    {
        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            return IsPathAtRoot(newDir);
        }

        public static bool IsSupportedPath(string path)
        {
			//return string.Equals(Path.GetDirectoryName(path), 
			//    Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + 
			//    Path.DirectorySeparatorChar + Resources.VfsRecycleBin, 
			//    StringComparison.OrdinalIgnoreCase);
			// 去除所有尾部路径分隔符
			string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar);

			// 构造期望路径格式（三个分隔符+资源标识）
			string expectedPath = new string(Path.DirectorySeparatorChar, 3)
								+ Resources.VfsRecycleBin;

			// 不区分大小写比较
			return string.Equals(
				trimmedPath,
				expectedPath,
				StringComparison.OrdinalIgnoreCase);
		}

        public static FileEntry CreateFile(string path)
        {
			var dir = Path.GetDirectoryName(path);
			var name = Path.GetFileName(path);
			var file = new FileEntry(dir, name);
            file.AttributesProperty = new FileAttributesProperty();
            file.SizeProperty = new FileSizeProperty();
            file.ModificationTimeProperty = new FileModificationDateTimeProperty();
            file.CreationTimeProperty = new FileCreationDateTimeProperty();
            file.LastAccessTimeProperty = new FileLastAccessDateTimeProperty();
            file.ChangeTimeProperty = new FileChangeDateTimeProperty();
            file.LinkProperty = new FileLinkProperty();
            file.CommentProperty = new FileCommentProperty();
            return file;
        }

        public static bool GetMainIcon(out string path)
        {
            path = "%SystemRoot%\\System32\\shell32.dll,31";
            return true;
        }

        public FileSourceOperationType GetOperationsTypes()
        {
            return FileSourceOperationType.List;
        }

        public FilePropertyType GetSupportedFileProperties()
        {
            return base.SupportedFileProperties |
                   FilePropertyType.Size |
                   FilePropertyType.Attributes |
                   FilePropertyType.ModificationTime |
                   FilePropertyType.CreationTime |
                   FilePropertyType.LastAccessTime |
                   FilePropertyType.ChangeTime |
                   FilePropertyType.Link |
                   FilePropertyType.Comment;
        }

        public override bool GetLocalName(ref FileEntry file)
        {
            file.FullPath = file.LinkProperty.LinkTarget;
            return true;
        }

        public override string GetRootDir(string path)
        {
            return Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + 
                   Path.DirectorySeparatorChar + Resources.VfsRecycleBin + 
                   Path.DirectorySeparatorChar;
        }

        public FileSourceProperties GetProperties()
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