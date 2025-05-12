using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;

namespace zfile;

[Flags]
public enum FilePropertyType : uint
{
    None = 0,
    Name = 1 << 0,
    Size = 1 << 1,
    Attributes = 1 << 2,
    ModificationTime = 1 << 3,
    CreationTime = 1 << 4,
    LastAccessTime = 1 << 5,
    Link = 1 << 6,
    Owner = 1 << 7,
    Group = 1 << 8,
    Type = 1 << 9,
    Comment = 1 << 10,
    CompressedSize = 1 << 11,
    Extension = 1 << 12,
    ChangeTime = 1 << 13,
    Variant = 1 << 14,
    Standard = Name | Size | Attributes | ModificationTime | CreationTime | LastAccessTime | Link | Owner | Group | Type | Comment | CompressedSize | Extension | ChangeTime,
    All = 0xffffffff
}
public class UnixFileAttributesProperty : FileProperty
{
    public FileAttributes Value { get; set; }
    public override FileProperty Clone()
    {
        return new UnixFileAttributesProperty { Value = this.Value };
    }
    public override bool Equals(FileProperty other)
    {
        return other is UnixFileAttributesProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Attributes;
}
public class NtfsFileAttributesProperty : FileProperty
{
    public FileAttributes Value { get; set; }
    public override FileProperty Clone()
    {
        return new NtfsFileAttributesProperty { Value = this.Value };
    }
    public override bool Equals(FileProperty other)
    {
        return other is NtfsFileAttributesProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Attributes;
}
public abstract class FileProperty
{
    public abstract FileProperty Clone();
    public abstract bool Equals(FileProperty other);
    /// <summary>
    /// Gets the property ID
    /// </summary>
    public abstract FilePropertyType ID { get; }
    public bool IsValid { get; internal set; }

}

public class FileNameProperty(string filename) : FileProperty
{
    public string Value { get; set; } = filename;

    public override FileProperty Clone()
    {
        return new FileNameProperty(Value);
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileNameProperty prop && Value == prop.Value;
    }    /// <summary>
         /// Gets the property ID
         /// </summary>
    public override FilePropertyType ID => FilePropertyType.Name;

}

public class FileSizeProperty : FileProperty
{
    public long Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileSizeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileSizeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Size;

}

public class FileCompressedSizeProperty : FileProperty
{
    public long Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileCompressedSizeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileCompressedSizeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.CompressedSize;
}

public class FileAttributesProperty : FileProperty
{
    public FileAttributes Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileAttributesProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileAttributesProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Attributes;
}

public class FileModificationDateTimeProperty : FileProperty
{
    public DateTime Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileModificationDateTimeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileModificationDateTimeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.ModificationTime;
}

public class FileCreationDateTimeProperty : FileProperty
{
    public DateTime Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileCreationDateTimeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileCreationDateTimeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.CreationTime;
}

public class FileLastAccessDateTimeProperty : FileProperty
{
    public DateTime Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileLastAccessDateTimeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileLastAccessDateTimeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.LastAccessTime;
}

public class FileChangeDateTimeProperty : FileProperty
{
    public DateTime Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileChangeDateTimeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileChangeDateTimeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.ChangeTime;
}

public class FileLinkProperty : FileProperty
{
    public string LinkTarget { get; set; }
    public bool IsLinkToDirectory { get; set; }
    //public bool IsValid;

    public override FileProperty Clone()
    {
        return new FileLinkProperty { LinkTarget = this.LinkTarget, IsLinkToDirectory = this.IsLinkToDirectory };
    }

    public virtual void CloneTo(FileProperty fileProperty)
    {
        if (fileProperty != null && fileProperty is FileLinkProperty linkProperty)
        {
            linkProperty.LinkTarget = this.LinkTarget;
            linkProperty.IsLinkToDirectory = this.IsLinkToDirectory;
            linkProperty.IsValid = this.IsValid;
        }
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileLinkProperty prop &&
               LinkTarget == prop.LinkTarget &&
               IsLinkToDirectory == prop.IsLinkToDirectory;
    }
    public override FilePropertyType ID => FilePropertyType.Link;
}

public class FileOwnerProperty : FileProperty
{
    public string Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileOwnerProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileOwnerProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Owner;
}

public class FileTypeProperty : FileProperty
{
    public string Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileTypeProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileTypeProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Type;
}

public class FileCommentProperty : FileProperty
{
    public string Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileCommentProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileCommentProperty prop && Value == prop.Value;
    }
    public override FilePropertyType ID => FilePropertyType.Comment;
}

public class FileVariantProperty : FileProperty
{
    public object Value { get; set; }

    public override FileProperty Clone()
    {
        return new FileVariantProperty { Value = this.Value };
    }

    public override bool Equals(FileProperty other)
    {
        return other is FileVariantProperty prop &&
               (Value == null && prop.Value == null ||
                Value != null && Value.Equals(prop.Value));
    }
    public override FilePropertyType ID => FilePropertyType.Variant;
}

public class FtpFileEntry : FileEntry
{
	public FtpFileEntry(string path) : base(path)
	{
	}
	public FtpFileEntry(string path, string name) : base(path, name)
	{
	}
	public override char PathSeparator => '/';
	public override string Path {
		get => base.Path;
		set
		{
			if (string.IsNullOrEmpty(value))
				base.Path = string.Empty;
			else
			{
				// 处理路径分隔符
				value = value.Replace('\\', PathSeparator);
				_path = value.EndsWith(PathSeparator) ? value : value + PathSeparator;
			}
		}
	}
}
public class FileEntry : IDisposable, IFileEntry
{
	private string _extension;
	private string _nameNoExt;
	protected string _path;
	private Dictionary<FilePropertyType, FileProperty> _properties;
	private List<FileVariantProperty> _variantProperties;
	private FilePropertyType _supportedProperties;
	private bool _disposed = false;

	public Dictionary<FilePropertyType, FileProperty> Properties => _properties;
	public virtual char PathSeparator => System.IO.Path.DirectorySeparatorChar;

	private void SplitIntoNameAndExtension(string fileName, out string fileNameOnly, out string extension)
	{
		int dotIndex = fileName.LastIndexOf('.');
		if (dotIndex > 0 && dotIndex < fileName.Length - 1)
		{
			fileNameOnly = fileName.Substring(0, dotIndex);
			extension = fileName.Substring(dotIndex + 1);
		}
		else
		{
			fileNameOnly = fileName;
			extension = string.Empty;
		}
	}

	private void UpdateNameAndExtension(string fileName)
	{
		SplitIntoNameAndExtension(fileName, out _nameNoExt, out _extension);
	}
	public Stream OpenRead()
	{
		return OpenRead(0, Size);
	}
	public Stream OpenRead(long offset, long size)
	{
		if (size <= 0)
			return null;
		Stream stream = null;
		try
		{
			stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			stream.Seek(offset, SeekOrigin.Begin);
			return stream;
		}
		catch (Exception ex)
		{
			stream?.Dispose();
			throw new IOException($"Failed to open file {FullPath}: {ex.Message}", ex);
		}
	}

	protected FileProperty GetProperty(FilePropertyType propType)
	{
		if (propType < FilePropertyType.Variant)
		{
			if (_properties.ContainsKey(propType))
				return _properties[propType];
			return null;
		}
		else
		{
			int index = (int)propType - (int)FilePropertyType.Variant;
			if (index >= 0 && index < _variantProperties.Count)
				return _variantProperties[index];
			return null;
		}
	}

	protected void SetProperty(FilePropertyType propType, FileProperty newValue)
	{
		if (propType < FilePropertyType.Variant)
		{
			if (newValue == null)
			{
				if (_properties.ContainsKey(propType))
				{
					_properties.Remove(propType);
					_supportedProperties &= ~propType;
				}
			}
			else
			{
				_properties[propType] = newValue;
				_supportedProperties |= propType;
			}
		}
		else
		{
			int index = (int)propType - (int)FilePropertyType.Variant;
			if (index >= _variantProperties.Count)
			{
				for (int i = _variantProperties.Count; i <= index; i++)
					_variantProperties.Add(null);
			}

			_variantProperties[index] = newValue as FileVariantProperty;

			if (newValue != null)
				_supportedProperties |= propType;
			else
				_supportedProperties &= ~propType;
		}
	}

	public virtual string FullPath
	{
		get { return _path + ((FileNameProperty)_properties[FilePropertyType.Name]).Value; }
		set
		{
			if (!string.IsNullOrEmpty(value))
			{
				if (value[value.Length - 1] == PathSeparator)
				{
					Path = value;
					Name = string.Empty;
				}
				else
				{
					string extractedName = System.IO.Path.GetFileName(value);
					Path = value.Substring(0, value.Length - extractedName.Length);
					Name = extractedName;
				}
			}
		}
	}

	public virtual string Path
	{
		get { return _path; }
		set
		{
			if (string.IsNullOrEmpty(value))
				_path = string.Empty;
			else
				_path = value.EndsWith(PathSeparator) ? value : value + PathSeparator;
		}
	}

	public string Name
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Name);
			return ((FileNameProperty)_properties[FilePropertyType.Name]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.Name);
			((FileNameProperty)_properties[FilePropertyType.Name]).Value = value;
			UpdateNameAndExtension(value);
		}
	}

	public string Extension
	{
		get { return _extension; }
	}

	public string NameNoExt
	{
		get { return _nameNoExt; }
	}


	private void EnsurePropertyExists(FilePropertyType propertyType)
	{
		if (!_properties.ContainsKey(propertyType))
		{
			switch (propertyType)
			{
				case FilePropertyType.Name:
					_properties[propertyType] = new FileNameProperty(string.Empty);
					break;
				case FilePropertyType.Attributes:
					_properties[propertyType] = new FileAttributesProperty();
					break;
				case FilePropertyType.Size:
					_properties[propertyType] = new FileSizeProperty();
					break;
				case FilePropertyType.CompressedSize:
					_properties[propertyType] = new FileCompressedSizeProperty();
					break;
				case FilePropertyType.ModificationTime:
					_properties[propertyType] = new FileModificationDateTimeProperty();
					break;
				case FilePropertyType.CreationTime:
					_properties[propertyType] = new FileCreationDateTimeProperty();
					break;
				case FilePropertyType.LastAccessTime:
					_properties[propertyType] = new FileLastAccessDateTimeProperty();
					break;
				case FilePropertyType.ChangeTime:
					_properties[propertyType] = new FileChangeDateTimeProperty();
					break;
				case FilePropertyType.Link:
					_properties[propertyType] = new FileLinkProperty();
					break;
				case FilePropertyType.Owner:
					_properties[propertyType] = new FileOwnerProperty();
					break;
				case FilePropertyType.Group:
					// 注意：Group属性类尚未实现，需要添加
					break;
				case FilePropertyType.Type:
					_properties[propertyType] = new FileTypeProperty();
					break;
				case FilePropertyType.Comment:
					_properties[propertyType] = new FileCommentProperty();
					break;
				case FilePropertyType.Extension:
					// 注意：Extension属性类尚未实现，需要添加
					break;
				case FilePropertyType.Variant:
					_properties[propertyType] = new FileVariantProperty();
					break;
			}
			_supportedProperties |= propertyType;
		}
	}
	public FileAttributes Attributes
	{
		//get { return ((FileAttributesProperty)_properties[FilePropertyType.Attributes]).Value; }
		//set { ((FileAttributesProperty)_properties[FilePropertyType.Attributes]).Value = value; }
		get
		{
			//if (!_properties.ContainsKey(FilePropertyType.Attributes))
			//{
			//	_properties[FilePropertyType.Attributes] = new FileAttributesProperty();
			//	_supportedProperties |= FilePropertyType.Attributes;
			//}
			EnsurePropertyExists(FilePropertyType.Attributes);
			return ((FileAttributesProperty)_properties[FilePropertyType.Attributes]).Value;
		}
		set
		{
			//if (!_properties.ContainsKey(FilePropertyType.Attributes))
			//{
			//	_properties[FilePropertyType.Attributes] = new FileAttributesProperty();
			//	_supportedProperties |= FilePropertyType.Attributes;
			//}
			EnsurePropertyExists(FilePropertyType.Attributes);
			((FileAttributesProperty)_properties[FilePropertyType.Attributes]).Value = value;
		}
	}

	public long Size
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Size);
			return ((FileSizeProperty)_properties[FilePropertyType.Size]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.Size);
			((FileSizeProperty)_properties[FilePropertyType.Size]).Value = value;
		}
	}

	public long CompressedSize
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.CompressedSize);
			return ((FileCompressedSizeProperty)_properties[FilePropertyType.CompressedSize]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.CompressedSize);
			((FileCompressedSizeProperty)_properties[FilePropertyType.CompressedSize]).Value = value;
		}
	}

	public DateTime ModificationTime
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.ModificationTime);
			return ((FileModificationDateTimeProperty)_properties[FilePropertyType.ModificationTime]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.ModificationTime);
			((FileModificationDateTimeProperty)_properties[FilePropertyType.ModificationTime]).Value = value;
		}
	}

	public DateTime CreationTime
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.CreationTime);
			return ((FileCreationDateTimeProperty)_properties[FilePropertyType.CreationTime]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.CreationTime);
			((FileCreationDateTimeProperty)_properties[FilePropertyType.CreationTime]).Value = value;
		}
	}

	public DateTime LastAccessTime
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.LastAccessTime);
			return ((FileLastAccessDateTimeProperty)_properties[FilePropertyType.LastAccessTime]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.LastAccessTime);
			((FileLastAccessDateTimeProperty)_properties[FilePropertyType.LastAccessTime]).Value = value;
		}
	}

	public DateTime ChangeTime
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.ChangeTime);
			return ((FileChangeDateTimeProperty)_properties[FilePropertyType.ChangeTime]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.ChangeTime);
			((FileChangeDateTimeProperty)_properties[FilePropertyType.ChangeTime]).Value = value;
		}
	}

	public bool IsLinkToDirectory
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Link);
			if (_supportedProperties.HasFlag(FilePropertyType.Link))
				return ((FileLinkProperty)_properties[FilePropertyType.Link]).IsLinkToDirectory;
			return false;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.Link);
			((FileLinkProperty)_properties[FilePropertyType.Link]).IsLinkToDirectory = value;
		}
	}

	public string FileType
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Type);
			return ((FileTypeProperty)_properties[FilePropertyType.Type]).Value;
		}
		set
		{
			EnsurePropertyExists(FilePropertyType.Type);
			((FileTypeProperty)_properties[FilePropertyType.Type]).Value = value;
		}
	}

	public FileNameProperty NameProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Name);
			return (FileNameProperty)_properties[FilePropertyType.Name];
		}
		set { SetProperty(FilePropertyType.Name, value); }
	}

	public FileSizeProperty SizeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Size);
			return (FileSizeProperty)_properties[FilePropertyType.Size];
		}
		set { SetProperty(FilePropertyType.Size, value); }
	}

	public FileCompressedSizeProperty CompressedSizeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.CompressedSize);
			return (FileCompressedSizeProperty)_properties[FilePropertyType.CompressedSize];
		}
		set { SetProperty(FilePropertyType.CompressedSize, value); }
	}

	public FileAttributesProperty AttributesProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Attributes);
			return (FileAttributesProperty)_properties[FilePropertyType.Attributes];
		}
		set
		{
			SetProperty(FilePropertyType.Attributes, value);
			if (value != null)
				UpdateNameAndExtension(Name);
		}
	}

	public FileModificationDateTimeProperty ModificationTimeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.ModificationTime);
			return (FileModificationDateTimeProperty)_properties[FilePropertyType.ModificationTime];
		}
		set { SetProperty(FilePropertyType.ModificationTime, value); }
	}

	public FileCreationDateTimeProperty CreationTimeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.CreationTime);
			return (FileCreationDateTimeProperty)_properties[FilePropertyType.CreationTime];
		}
		set { SetProperty(FilePropertyType.CreationTime, value); }
	}

	public FileLastAccessDateTimeProperty LastAccessTimeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.LastAccessTime);
			return (FileLastAccessDateTimeProperty)_properties[FilePropertyType.LastAccessTime];
		}
		set { SetProperty(FilePropertyType.LastAccessTime, value); }
	}

	public FileChangeDateTimeProperty ChangeTimeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.ChangeTime);
			return (FileChangeDateTimeProperty)_properties[FilePropertyType.ChangeTime];
		}
		set { SetProperty(FilePropertyType.ChangeTime, value); }
	}

	public FileLinkProperty LinkProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Link);
			return (FileLinkProperty)_properties[FilePropertyType.Link];
		}
		set { SetProperty(FilePropertyType.Link, value); }
	}

	public FileOwnerProperty OwnerProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Owner);
			return (FileOwnerProperty)_properties[FilePropertyType.Owner];
		}
		set { SetProperty(FilePropertyType.Owner, value); }
	}

	public FileTypeProperty TypeProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Type);
			return (FileTypeProperty)_properties[FilePropertyType.Type];
		}
		set { SetProperty(FilePropertyType.Type, value); }
	}

	public FileCommentProperty CommentProperty
	{
		get
		{
			EnsurePropertyExists(FilePropertyType.Comment);
			return (FileCommentProperty)_properties[FilePropertyType.Comment];
		}
		set { SetProperty(FilePropertyType.Comment, value); }
	}

	public IReadOnlyList<FileVariantProperty> VariantProperties => _variantProperties.AsReadOnly();

	public FilePropertyType SupportedProperties => _supportedProperties;

	public FilePropertyType AssignedProperties => _supportedProperties;

	public FileEntry(string path)
	{
		_properties = new Dictionary<FilePropertyType, FileProperty>();
		_variantProperties = new List<FileVariantProperty>();
		_supportedProperties = FilePropertyType.Name;
		if (File.Exists(path))
		{
			//NameProperty = new FileNameProperty(System.IO.Path.GetFileName(path));
			//NameProperty = new FileNameProperty(); // use ensurepropertyexist, so do not need to init nameproperty here.
			FullPath = path;
			//Path = path;
		}
		else
			Path = path;
	}
	public FileEntry(string path, string name)
	{
		_properties = new Dictionary<FilePropertyType, FileProperty>();
		_variantProperties = new List<FileVariantProperty>();
		_supportedProperties = FilePropertyType.Name;

		//NameProperty = new FileNameProperty(name); // bugfix: use this method will not update extension and namewithoutextension,so use name assignment 
		Name = name;
		//NameProperty = new FileNameProperty(); // use ensurepropertyexist, so do not need to init nameproperty here.
		Path = path;
	}

	public FileEntry()
	{
		_properties = new Dictionary<FilePropertyType, FileProperty>();
		_variantProperties = new List<FileVariantProperty>();
	}

	public FileEntry Clone()
	{
		FileEntry result = new FileEntry();
		CloneTo(result);
		return result;
	}

	public void CloneTo(FileEntry file)
	{
		if (file != null)
		{
			file._extension = _extension;
			file._nameNoExt = _nameNoExt;
			file._path = _path;
			file._supportedProperties = _supportedProperties;

			foreach (var kvp in _properties)
			{
				file._properties[kvp.Key] = kvp.Value.Clone();
			}

			file._variantProperties = new List<FileVariantProperty>(_variantProperties.Count);
			for (int i = 0; i < _variantProperties.Count; i++)
			{
				file._variantProperties.Add(_variantProperties[i]?.Clone() as FileVariantProperty);
			}
		}
	}

	public FilePropertyType Compare(FileEntry file)
	{
		FilePropertyType result = FilePropertyType.None;

		if (_path != file._path)
		{
			result |= FilePropertyType.Name;
			return result;
		}

		foreach (var kvp in _properties)
		{
			if (file._properties.TryGetValue(kvp.Key, out var otherProp))
			{
				if (!kvp.Value.Equals(otherProp))
					result |= kvp.Key;
			}
			else
			{
				result |= kvp.Key;
			}
		}

		foreach (var kvp in file._properties)
		{
			if (!_properties.ContainsKey(kvp.Key))
				result |= kvp.Key;
		}

		if (_variantProperties.Count != file._variantProperties.Count)
		{
			result |= FilePropertyType.Variant;
			return result;
		}

		for (int i = 0; i < _variantProperties.Count; i++)
		{
			var prop1 = _variantProperties[i];
			var prop2 = file._variantProperties[i];

			if ((prop1 == null && prop2 != null) ||
				(prop1 != null && prop2 == null) ||
				(prop1 != null && prop2 != null && !prop1.Equals(prop2)))
			{
				result |= FilePropertyType.Variant;
				break;
			}
		}

		return result;
	}

	public void ClearProperties()
	{
		ClearVariantProperties();
		foreach (var key in _properties.Keys.ToList())
		{
			if (key != FilePropertyType.Name)
			{
				_properties.Remove(key);
			}
		}
		_supportedProperties = FilePropertyType.Name;
	}

	public void ClearVariantProperties()
	{
		_variantProperties.Clear();
		_supportedProperties &= ~FilePropertyType.Variant;
	}

	public FileProperty ReleaseProperty(FilePropertyType propType)
	{
		FileProperty result = null;

		if (propType < FilePropertyType.Variant)
		{
			if (_properties.TryGetValue(propType, out result))
			{
				_properties.Remove(propType);
				_supportedProperties &= ~propType;
			}
		}
		else
		{
			int index = (int)propType - (int)FilePropertyType.Variant;
			if (index >= 0 && index < _variantProperties.Count)
			{
				result = _variantProperties[index];
				_variantProperties[index] = null;
				_supportedProperties &= ~propType;
			}
		}

		return result;
	}

	public bool IsNameValid()
	{
		return Name != "..";
	}

	public bool IsDirectory
	{
		get => _supportedProperties.HasFlag(FilePropertyType.Attributes) &&
			   (Attributes & FileAttributes.Directory) == FileAttributes.Directory;
		set => Attributes = value ? Attributes | FileAttributes.Directory : Attributes & ~FileAttributes.Directory;
	}

	public bool IsSysFile
	{
		get => _supportedProperties.HasFlag(FilePropertyType.Attributes) &&
			   (Attributes & FileAttributes.System) == FileAttributes.System;
	}

	public bool IsHidden
	{
		get => _supportedProperties.HasFlag(FilePropertyType.Attributes) &&
			   (Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
	}

	public bool IsLink
	{
		get => _supportedProperties.HasFlag(FilePropertyType.Link) &&
			   !string.IsNullOrEmpty(((FileLinkProperty)_properties[FilePropertyType.Link]).LinkTarget);
	}

	public bool IsExecutable()
	{
		if (!_supportedProperties.HasFlag(FilePropertyType.Name))
			return false;

		string ext = Extension.ToLower();
		return ext == "exe" || ext == "bat" || ext == "cmd" || ext == "com";
	}

	public bool IsReadOnly
	{
		get => _supportedProperties.HasFlag(FilePropertyType.Attributes) && (Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
	}
	public bool Exists { get; internal set; }

	/// <summary>
	/// Disposes resources used by the FileEntry.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes resources used by the FileEntry.
	/// </summary>
	/// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				// Dispose managed resources
				// No managed resources to dispose in this class currently
				// If streams or other disposable objects are added as members, dispose them here
			}

			// Clean up unmanaged resources
			// No unmanaged resources to clean up in this class currently

			_disposed = true;
		}
	}

	/// <summary>
	/// Finalizer to ensure resources are cleaned up if Dispose is not called.
	/// </summary>
	~FileEntry()
	{
		Dispose(false);
	}
}

public class FileEntries : IEnumerable<FileEntry>
{
    private List<FileEntry> _list;
    private bool _flat;
    private bool _ownsObjects;
    private string _path;

    public static FileEntries Empty => new FileEntries();
    public static FileEntries EmptyFlat => new FileEntries { _flat = true };
    public bool IsEmpty => _list.Count == 0;
    public bool IsFlat => _flat;
    public string Name => _list.Count > 0 ? _list[0].Name : string.Empty;
    public string PathName => _path;
	public char PathSeparator = System.IO.Path.DirectorySeparatorChar;
	public static FileEntries FromArray(FileEntry[] files)
    {
        var fileEntries = new FileEntries();
        foreach (var file in files)
        {
            fileEntries.Add(file);
        }
        return fileEntries;
    }
    public FileEntry this[string name]
    {
        get
        {
            foreach (var file in _list)
            {
                if (file.Name == name)
                    return file;
            }
            return null;
        }
    }
    public int Count
    {
        get { return _list.Count; }
        set
        {
            if (value < _list.Count)
            {
                if (_ownsObjects)
                {
                    for (int i = value; i < _list.Count; i++)
                    {
                        // In C# we don't need to manually free objects
                    }
                }
                _list.RemoveRange(value, _list.Count - value);
            }
            else if (value > _list.Count)
            {
                for (int i = _list.Count; i < value; i++)
                {
                    _list.Add(null);
                }
            }
        }
    }

    public FileEntry this[int index]
    {
        get { return _list[index]; }
        set { _list[index] = value; }
    }

    public List<FileEntry> List => _list;

    public bool OwnsObjects
    {
        get { return _ownsObjects; }
        set { _ownsObjects = value; }
    }

    public string Path
    {
        get { return _path; }
        set
        {
			if (string.IsNullOrWhiteSpace(value))
				_path = string.Empty;
			else
			{
				_path = value;
				if (value[^1] != PathSeparator)
					_path += PathSeparator;
			}
            if (_flat)
            {
                foreach (var file in _list)
                {
                    if (file != null)
                    {
                        file.Path = value;
                    }
                }
            }
        }
    }

    public bool Flat
    {
        get { return _flat; }
        set { _flat = value; }
    }

    public FileEntries(string? path = "")
    {
        _list = new List<FileEntry>();
        _ownsObjects = true;
        Path = path;
    }

    ~FileEntries()
    {
        Clear();
    }

    public FileEntries Clone()
    {
        FileEntries result = new FileEntries(_path);
        CloneTo(result);
        return result;
    }

    public void CloneTo(FileEntries files)
    {
        files._flat = _flat;
        files._path = _path;
        files.Count = 0;

        foreach (var file in _list)
        {
            if (file != null)
            {
                files.Add(file.Clone());
            }
            else
            {
                files.Add(null);
            }
        }
    }

    public int Add(FileEntry file)
    {
        _list.Add(file);
		if (string.IsNullOrEmpty(_path))
		{
			_path = file.Path;
			PathSeparator = file.PathSeparator;
		}
		return _list.Count - 1;
    }

    public void Insert(FileEntry file, int atIndex)
    {
        _list.Insert(atIndex, file);
    }

    public void Delete(int atIndex)
    {
        if (_ownsObjects && _list[atIndex] != null)
        {
            // In C# we don't need to manually free objects
        }
        _list.RemoveAt(atIndex);
    }

    public void Clear()
    {
        if (_ownsObjects)
        {
            foreach (var file in _list)
            {
                // In C# we don't need to manually free objects
            }
        }
        _list.Clear();
    }
    //public FileEntry GetEnumable()
    //{
    //	foreach (var file in _list)
    //	{
    //		if (file != null)
    //		{
    //			yield return file;
    //		}
    //	}
    //	return null;
    //}

    public IEnumerator<FileEntry> GetEnumerator()
    {
        return ((IEnumerable<FileEntry>)_list).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_list).GetEnumerator();
    }
}


