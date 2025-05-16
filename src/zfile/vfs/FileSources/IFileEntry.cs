
namespace zfile
{
	public interface IFileEntry
	{
		FilePropertiesTypes AssignedProperties { get; }
		FileAttributes Attributes { get; set; }
		FileAttributesProperty AttributesProperty { get; set; }
		DateTime ChangeTime { get; set; }
		FileChangeDateTimeProperty ChangeTimeProperty { get; set; }
		FileCommentProperty CommentProperty { get; set; }
		long CompressedSize { get; set; }
		FileCompressedSizeProperty CompressedSizeProperty { get; set; }
		DateTime CreationTime { get; set; }
		FileCreationDateTimeProperty CreationTimeProperty { get; set; }
		bool Exists { get; }
		string Extension { get; }
		string FileType { get; set; }
		string FullPath { get; set; }
		bool IsDirectory { get; set; }
		bool IsHidden { get; }
		bool IsLink { get; }
		bool IsLinkToDirectory { get; set; }
		bool IsReadOnly { get; }
		bool IsSysFile { get; }
		DateTime LastAccessTime { get; set; }
		FileLastAccessDateTimeProperty LastAccessTimeProperty { get; set; }
		FileLinkProperty LinkProperty { get; set; }
		DateTime ModificationTime { get; set; }
		FileModificationDateTimeProperty ModificationTimeProperty { get; set; }
		string Name { get; set; }
		string NameNoExt { get; }
		FileNameProperty NameProperty { get; set; }
		FileOwnerProperty OwnerProperty { get; set; }
		string Path { get; set; }
		char PathSeparator { get; }
		Dictionary<FilePropertiesTypes, FileProperty> Properties { get; }
		long Size { get; set; }
		FileSizeProperty SizeProperty { get; set; }
		FilePropertiesTypes SupportedProperties { get; }
		FileTypeProperty TypeProperty { get; set; }
		IReadOnlyList<FileVariantProperty> VariantProperties { get; }

		void ClearProperties();
		void ClearVariantProperties();
		FileEntry Clone();
		void CloneTo(FileEntry file);
		FilePropertiesTypes Compare(FileEntry file);
		void Dispose();
		bool IsExecutable();
		bool IsNameValid();
		Stream OpenRead();
		Stream OpenRead(long offset, long size);
		FileProperty ReleaseProperty(FilePropertiesTypes propType);
		object? Tag { get; set; }
	}
}