namespace zfile
{
	#region Enums and Data Structures

	/// <summary>
	/// Represents a field in file source view
	/// </summary>
	//public struct FileSourceField
	//{
	//	public string Content { get; set; }
	//	public string Header { get; set; }
	//	public int Width { get; set; }
	//	public string Option { get; set; }
	//	public HorizontalAlignment Align { get; set; }
	//}

	

	/// <summary>
	/// Represents file source properties
	/// </summary>
	[Flags]
	public enum FileSourceProperty
	{
		None = 0,
		IsVirtual = 1 << 0,
		IsLocal = 1 << 1,
		IsRemote = 1 << 2,
		IsArchive = 1 << 3,
		CanCreateDirectory = 1 << 4,
		HasAttributesSupport = 1 << 5,
		HasReloadSupport = 1 << 6
	}
	public enum FileSourceProperties
	{
		DirectAccess,
		CaseSensitive,
		Virtual,
		LinkToLocalFiles,
		UsersConnections,
		ListOnMainThread,
		CopyInOnMainThread,
		CopyOutOnMainThread,
		ListFlatView,
		NoneParent,
		DefaultView,
		ContextMenu
	}
	/// <summary>
	/// Represents file properties types
	/// </summary>
	//[Flags]
	//public enum FilePropertyType : uint
	//{
	//	Name = 0,
	//	Size = 1,
	//	CompressedSize = 2,
	//	Owner = 3,
	//	Attributes = 4,
	//	ModificationTime = 5,
	//	CreationTime = 6,
	//	LastAccessTime = 7,
	//	ChangeTime = 8,
	//	Link = 9,
	//	Type = 10,
	//	Comment = 11,
	//	Invalid = 12,
	//	Variant = 128,
	//	Maximum = 255
	//}

	/// <summary>
	/// Represents path types
	/// </summary>
	public enum PathType
	{
		Unknown,
		Relative,
		Absolute,
		Network
	}

	/// <summary>
	/// Represents file source operation state
	/// </summary>
	public enum FileSourceOperationState
	{
		NotStarted,
		Starting,
		Running,
		Pausing,
		Paused,
		WaitingForFeedback,
		WaitingForConnection,
		Stopping,
		Stopped,
		Finished,
		Failed,
		Cancelled
	}

	/// <summary>
	/// Represents the result of a file source operation
	/// </summary>
	public enum FileSourceOperationResult
	{
		/// <summary>
		/// Operation has finished successfully
		/// </summary>
		Finished,
		
		/// <summary>
		/// Operation has been aborted by user
		/// </summary>
		Aborted
	}

	#endregion

	#region Delegates

	/// <summary>
	/// Delegate for file source reload event notification
	/// </summary>
	/// <param name="fileSource">The file source that was reloaded</param>
	/// <param name="reloadedPaths">The paths that were reloaded</param>
	public delegate void FileSourceReloadEventHandler(IFileSource fileSource, string[] reloadedPaths);

	#endregion

	#region Interfaces

	/// <summary>
	/// Interface for file source
	/// </summary>
	public interface IFileSource : IDisposable
	{
		/// <summary>
		/// Checks if this file source equals another file source
		/// </summary>
		/// <param name="fileSource">The file source to compare with</param>
		/// <returns>True if the file sources are equal, false otherwise</returns>
		bool Equals(IFileSource fileSource);

		/// <summary>
		/// Checks if this file source implements the specified interface
		/// </summary>
		/// <param name="interfaceType">The interface type to check</param>
		/// <returns>True if the file source implements the interface, false otherwise</returns>
		bool IsInterface(Type interfaceType);

		/// <summary>
		/// Checks if this file source is of the specified class type
		/// </summary>
		/// <param name="classType">The class type to check</param>
		/// <returns>True if the file source is of the specified class type, false otherwise</returns>
		bool IsClass(Type classType);

		/// <summary>
		/// Gets the URI of this file source
		/// </summary>
		Uri Uri { get; }

		/// <summary>
		/// Gets the class name of this file source
		/// </summary>
		string ClassName { get; }

		/// <summary>
		/// Gets the reference count of this file source
		/// </summary>
		int RefCount { get; }

		/// <summary>
		/// Gets the file system of this file source
		/// </summary>
		string FileSystem { get; }

		/// <summary>
		/// Gets the current address of this file source
		/// </summary>
		string CurrentAddress { get; set; }

		/// <summary>
		/// Gets the current working directory of this file source
		/// </summary>
		string CurrentWorkingDirectory { get; }

		/// <summary>
		/// Sets the current working directory of this file source
		/// </summary>
		/// <param name="newDir">The new directory</param>
		/// <returns>True if the directory was changed successfully, false otherwise</returns>
		bool SetCurrentWorkingDirectory(string newDir);

		/// <summary>
		/// Gets the supported file properties of this file source
		/// </summary>
		FilePropertyType SupportedFileProperties { get; }

		/// <summary>
		/// Gets the retrievable file properties of this file source
		/// </summary>
		FilePropertyType RetrievableFileProperties { get; }

		/// <summary>
		/// Gets the operation types supported by this file source
		/// </summary>
		FileSourceOperationType OperationsTypes { get; }

		/// <summary>
		/// Gets the properties of this file source
		/// </summary>
		FileSourceProperties Properties { get; }

		/// <summary>
		/// Gets the files in the specified target path
		/// </summary>
		/// <param name="targetPath">The target path</param>
		/// <returns>The files in the target path</returns>
		FileEntries GetFiles(string targetPath);

		/// <summary>
		/// Gets or sets the parent file source of this file source
		/// </summary>
		IFileSource ParentFileSource { get; set; }

		/// <summary>
		/// Creates a file object with the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The file object</returns>
		FileEntry CreateFile(string path);

		/// <summary>
		/// Checks if the file source can retrieve the specified properties for the file
		/// </summary>
		/// <param name="file">The file</param>
		/// <param name="propertiesToSet">The properties to set</param>
		/// <returns>True if the properties can be retrieved, false otherwise</returns>
		bool CanRetrieveProperties(FileEntry file, FilePropertyType propertiesToSet);

		/// <summary>
		/// Retrieves the specified properties for the file
		/// </summary>
		/// <param name="file">The file</param>
		/// <param name="propertiesToSet">The properties to set</param>
		/// <param name="variantProperties">The variant properties</param>
		void RetrieveProperties(FileEntry file, FilePropertyType propertiesToSet, string[] variantProperties);

		/// <summary>
		/// Creates a list operation for the specified target path
		/// </summary>
		/// <param name="targetPath">The target path</param>
		/// <returns>The list operation</returns>
		FileSourceOperation? CreateListOperation(string targetPath);

		/// <summary>
		/// Creates a copy operation for the specified source files and target path
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy operation</returns>
		FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath);

		/// <summary>
		/// Creates a copy in operation for the specified source file source, source files and target path
		/// </summary>
		/// <param name="sourceFileSource">The source file source</param>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy in operation</returns>
		FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath);

		/// <summary>
		/// Creates a copy out operation for the specified target file source, source files and target path
		/// </summary>
		/// <param name="targetFileSource">The target file source</param>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy out operation</returns>
		FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath);

		/// <summary>
		/// Creates a move operation for the specified source files and target path
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The move operation</returns>
		FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath);

		/// <summary>
		/// Creates a delete operation for the specified files to delete
		/// </summary>
		/// <param name="filesToDelete">The files to delete</param>
		/// <returns>The delete operation</returns>
		FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete);

		/// <summary>
		/// Creates a wipe operation for the specified files to wipe
		/// </summary>
		/// <param name="filesToWipe">The files to wipe</param>
		/// <returns>The wipe operation</returns>
		FileSourceOperation CreateWipeOperation(FileEntries filesToWipe);

		/// <summary>
		/// Creates a split operation for the specified source file and target path
		/// </summary>
		/// <param name="sourceFile">The source file</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The split operation</returns>
		FileSourceOperation CreateSplitOperation(FileEntry sourceFile, string targetPath);

		/// <summary>
		/// Creates a combine operation for the specified source files and target file
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetFile">The target file</param>
		/// <returns>The combine operation</returns>
		FileSourceOperation CreateCombineOperation(FileEntries sourceFiles, string targetFile);

		/// <summary>
		/// Creates a create directory operation for the specified base path and directory path
		/// </summary>
		/// <param name="basePath">The base path</param>
		/// <param name="directoryPath">The directory path</param>
		/// <returns>The create directory operation</returns>
		FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath);

		/// <summary>
		/// Creates an execute operation for the specified executable file, base path and verb
		/// </summary>
		/// <param name="executableFile">The executable file</param>
		/// <param name="basePath">The base path</param>
		/// <param name="verb">The verb</param>
		/// <returns>The execute operation</returns>
		FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb);

		/// <summary>
		/// Creates a test archive operation for the specified source files
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <returns>The test archive operation</returns>
		FileSourceOperation CreateTestArchiveOperation(FileEntries sourceFiles);

		/// <summary>
		/// Creates a calculate checksum operation for the specified files, target path and target mask
		/// </summary>
		/// <param name="files">The files</param>
		/// <param name="targetPath">The target path</param>
		/// <param name="targetMask">The target mask</param>
		/// <returns>The calculate checksum operation</returns>
		FileSourceOperation CreateCalcChecksumOperation(FileEntries files, string targetPath, string targetMask);

		/// <summary>
		/// Creates a calculate statistics operation for the specified files
		/// </summary>
		/// <param name="files">The files</param>
		/// <returns>The calculate statistics operation</returns>
		FileSourceOperation CreateCalcStatisticsOperation(FileEntries files);

		/// <summary>
		/// Creates a set file property operation for the specified target files and new properties
		/// </summary>
		/// <param name="targetFiles">The target files</param>
		/// <param name="newProperties">The new properties</param>
		/// <returns>The set file property operation</returns>
		FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperty[] newProperties);

		/// <summary>
		/// Gets the operation class for the specified operation type
		/// </summary>
		/// <param name="operationType">The operation type</param>
		/// <returns>The operation class</returns>
		Type GetOperationClass(FileSourceOperationType operationType);

		/// <summary>
		/// Checks if the specified path is at the root of the file source
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the path is at the root, false otherwise</returns>
		bool IsPathAtRoot(string path);
		string CurrentPath { get; }
	
	
		/// <summary>
		/// Gets the parent directory of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The parent directory</returns>
		string GetParentDir(string path);

		/// <summary>
		/// Gets the root directory of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The root directory</returns>
		string GetRootDir(string path);

		/// <summary>
		/// Gets the root directory of the file source
		/// </summary>
		/// <returns>The root directory</returns>
		string GetRootDir();

		/// <summary>
		/// Gets the path type of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The path type</returns>
		PathType GetPathType(string path);

		/// <summary>
		/// Gets the free space of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <param name="freeSize">The free size</param>
		/// <param name="totalSize">The total size</param>
		/// <returns>True if the free space was retrieved successfully, false otherwise</returns>
		bool GetFreeSpace(string path, out long freeSize, out long totalSize);

		/// <summary>
		/// Gets the local name of the specified file
		/// </summary>
		/// <param name="file">The file</param>
		/// <returns>True if the local name was retrieved successfully, false otherwise</returns>
		bool GetLocalName(ref FileEntry file);

		/// <summary>
		/// Creates a directory at the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the directory was created successfully, false otherwise</returns>
		bool CreateDirectory(string path);

		/// <summary>
		/// Checks if a file system entry exists at the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the file system entry exists, false otherwise</returns>
		bool FileSystemEntryExists(string path);

		/// <summary>
		/// Gets the default view for the file source
		/// </summary>
		/// <param name="defaultView">The default view</param>
		/// <returns>True if the default view was retrieved successfully, false otherwise</returns>
		bool GetDefaultView(out FileSourceField[] defaultView);

		/// <summary>
		/// Queries the context menu for the specified files
		/// </summary>
		/// <param name="files">The files</param>
		/// <param name="menu">The menu</param>
		/// <returns>True if the context menu was queried successfully, false otherwise</returns>
		bool QueryContextMenu(FileEntries files, ref ContextMenuStrip menu);

		/// <summary>
		/// Gets a connection for the specified operation
		/// </summary>
		/// <param name="operation">The operation</param>
		/// <returns>The connection</returns>
		FileSourceConnection GetConnection(IFileSourceOperation operation);

		/// <summary>
		/// Removes the specified operation from the queue
		/// </summary>
		/// <param name="operation">The operation</param>
		void RemoveOperationFromQueue(IFileSourceOperation operation);

		/// <summary>
		/// Adds a child file source to this file source
		/// </summary>
		/// <param name="fileSource">The child file source</param>
		void AddChild(IFileSource fileSource);

		/// <summary>
		/// Reloads the file source with the specified paths to reload
		/// </summary>
		/// <param name="pathsToReload">The paths to reload</param>
		void Reload(string[] pathsToReload);

		/// <summary>
		/// Reloads the file source with the specified path to reload
		/// </summary>
		/// <param name="pathToReload">The path to reload</param>
		void Reload(string pathToReload);

		/// <summary>
		/// Adds a reload event listener
		/// </summary>
		/// <param name="handler">The handler</param>
		void AddReloadEventListener(FileSourceReloadEventHandler handler);

		/// <summary>
		/// Removes a reload event listener
		/// </summary>
		/// <param name="handler">The handler</param>
		void RemoveReloadEventListener(FileSourceReloadEventHandler handler);
	}

	#endregion

	#region Classes

	/// <summary>
	/// Represents a file entry
	/// </summary>
	//public class FileEntry
	//{
	//	/// <summary>
	//	/// Gets or sets the name of the file
	//	/// </summary>
	//	public string Name { get; set; }

	//	/// <summary>
	//	/// Gets or sets the path of the file
	//	/// </summary>
	//	public string Path { get; set; }

	//	/// <summary>
	//	/// Gets or sets the size of the file
	//	/// </summary>
	//	public long Size { get; set; }

	//	/// <summary>
	//	/// Gets or sets the attributes of the file
	//	/// </summary>
	//	public FileAttributes Attributes { get; set; }

	//	/// <summary>
	//	/// Gets or sets the modification time of the file
	//	/// </summary>
	//	public DateTime ModificationTime { get; set; }

	//	/// <summary>
	//	/// Gets or sets the creation time of the file
	//	/// </summary>
	//	public DateTime CreationTime { get; set; }

	//	/// <summary>
	//	/// Gets or sets the last access time of the file
	//	/// </summary>
	//	public DateTime LastAccessTime { get; set; }

	//	/// <summary>
	//	/// Gets or sets the link target of the file
	//	/// </summary>
	//	public string LinkTarget { get; set; }

	//	/// <summary>
	//	/// Gets or sets the owner of the file
	//	/// </summary>
	//	public string Owner { get; set; }

	//	/// <summary>
	//	/// Gets or sets the group of the file
	//	/// </summary>
	//	public string Group { get; set; }

	//	/// <summary>
	//	/// Gets or sets the type of the file
	//	/// </summary>
	//	public string Type { get; set; }

	//	/// <summary>
	//	/// Gets or sets the comment of the file
	//	/// </summary>
	//	public string Comment { get; set; }

	//	/// <summary>
	//	/// Gets or sets the compressed size of the file
	//	/// </summary>
	//	public long CompressedSize { get; set; }

	//	/// <summary>
	//	/// Gets or sets the extension of the file
	//	/// </summary>
	//	public string Extension { get; set; }

	//	/// <summary>
	//	/// Gets a value indicating whether the file is a directory
	//	/// </summary>
	//	public bool IsDirectory => (Attributes & FileAttributes.Directory) == FileAttributes.Directory;

	//	/// <summary>
	//	/// Gets a value indicating whether the file is a link
	//	/// </summary>
	//	public bool IsLink => !string.IsNullOrEmpty(LinkTarget);

	//	public FilePropertyType AssignedProperties;

	//	/// <summary>
	//	/// Creates a new instance of the FileEntry class
	//	/// </summary>
	//	public FileEntry()
	//	{
	//	}

	//	/// <summary>
	//	/// Creates a new instance of the FileEntry class with the specified path
	//	/// </summary>
	//	/// <param name="path">The path</param>
	//	public FileEntry(string path)
	//	{
	//		Path = path;
	//		Name = System.IO.Path.GetFileName(path);
	//	}

	//	/// <summary>
	//	/// Creates a clone of this file entry
	//	/// </summary>
	//	/// <returns>The cloned file entry</returns>
	//	public FileEntry Clone()
	//	{
	//		return new FileEntry
	//		{
	//			Name = Name,
	//			Path = Path,
	//			Size = Size,
	//			Attributes = Attributes,
	//			ModificationTime = ModificationTime,
	//			CreationTime = CreationTime,
	//			LastAccessTime = LastAccessTime,
	//			LinkTarget = LinkTarget,
	//			Owner = Owner,
	//			Group = Group,
	//			Type = Type,
	//			Comment = Comment,
	//			CompressedSize = CompressedSize,
	//			Extension = Extension
	//		};
	//	}
	//}

	

	/// <summary>
	/// Base class for file sources
	/// </summary>
	public abstract class FileSource : IFileSource
	{
		private int _refCount;
		private readonly List<IFileSource> _children = new List<IFileSource>();
		private readonly List<FileSourceReloadEventHandler> _reloadEventListeners = new List<FileSourceReloadEventHandler>();
		private readonly List<FileSourceConnection> _connections = new List<FileSourceConnection>();
		private readonly object _syncRoot = new object();
		protected Dictionary<FileSourceOperationType, Type> OperationsClasses = new();
		/// <summary>
		/// Gets the URI of this file source
		/// </summary>
		public virtual Uri Uri { get; }

		/// <summary>
		/// Gets the class name of this file source
		/// </summary>
		public virtual string ClassName => GetType().Name;

		/// <summary>
		/// Gets the reference count of this file source
		/// </summary>
		public int RefCount => _refCount;

		/// <summary>
		/// Gets the file system of this file source
		/// </summary>
		public virtual string FileSystem { get; }

		/// <summary>
		/// Gets the current address of this file source
		/// </summary>
		public virtual string CurrentAddress { get; set; }

		/// <summary>
		/// Gets the current working directory of this file source
		/// </summary>
		public virtual string CurrentWorkingDirectory { get; }

		/// <summary>
		/// Gets the supported file properties of this file source
		/// </summary>
		public virtual FilePropertyType SupportedFileProperties { get; protected set; }

		/// <summary>
		/// Gets the retrievable file properties of this file source
		/// </summary>
		public virtual FilePropertyType RetrievableFileProperties { get; protected set; }

		/// <summary>
		/// Gets the operation types supported by this file source
		/// </summary>
		public virtual FileSourceOperationType OperationsTypes { get; }

		/// <summary>
		/// Gets the properties of this file source
		/// </summary>
		public virtual FileSourceProperties Properties { get; protected set; }

		/// <summary>
		/// Gets or sets the parent file source of this file source
		/// </summary>
		public virtual IFileSource ParentFileSource { get; set; }
		public virtual string CurrentPath { get; }

		/// <summary>
		/// Creates a new instance of the FileSource class
		/// </summary>
		protected FileSource()
		{
			_refCount = 1;
			FileSourceManager.Instance.Add(this);
		}

		/// <summary>
		/// Finalizes an instance of the FileSource class
		/// </summary>
		~FileSource()
		{
			Dispose(false);
		}

		/// <summary>
		/// Disposes the file source
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the file source
		/// </summary>
		/// <param name="disposing">Whether the method is called from Dispose or the finalizer</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (_syncRoot)
				{
					foreach (var child in _children.ToList())
					{
						child.Dispose();
					}

					_children.Clear();
					_reloadEventListeners.Clear();
					_connections.Clear();
				}

				FileSourceManager.Instance.Remove(this);
			}
		}

		/// <summary>
		/// Checks if this file source equals another file source
		/// </summary>
		/// <param name="fileSource">The file source to compare with</param>
		/// <returns>True if the file sources are equal, false otherwise</returns>
		public virtual bool Equals(IFileSource fileSource)
		{
			if (fileSource == null)
				return false;

			return GetType() == fileSource.GetType() &&
				   string.Equals(CurrentAddress, fileSource.CurrentAddress, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks if this file source implements the specified interface
		/// </summary>
		/// <param name="interfaceType">The interface type to check</param>
		/// <returns>True if the file source implements the interface, false otherwise</returns>
		public bool IsInterface(Type interfaceType)
		{
			if (interfaceType == null)
				return false;

			return interfaceType.IsAssignableFrom(GetType());
		}

		/// <summary>
		/// Checks if this file source is of the specified class type
		/// </summary>
		/// <param name="classType">The class type to check</param>
		/// <returns>True if the file source is of the specified class type, false otherwise</returns>
		public bool IsClass(Type classType)
		{
			if (classType == null)
				return false;

			return GetType() == classType || GetType().IsSubclassOf(classType);
		}

		/// <summary>
		/// Sets the current working directory of this file source
		/// </summary>
		/// <param name="newDir">The new directory</param>
		/// <returns>True if the directory was changed successfully, false otherwise</returns>
		public virtual bool SetCurrentWorkingDirectory(string newDir)
		{
			return true;
		}

		/// <summary>
		/// Gets the files in the specified target path
		/// </summary>
		/// <param name="targetPath">The target path</param>
		/// <returns>The files in the target path</returns>
		public virtual FileEntries GetFiles(string targetPath)
		{
			return new FileEntries(targetPath);
		}

		/// <summary>
		/// Creates a file object with the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The file object</returns>
		public virtual FileEntry CreateFile(string path)
		{
			return new FileEntry(path);
		}

		/// <summary>
		/// Checks if the file source can retrieve the specified properties for the file
		/// </summary>
		/// <param name="file">The file</param>
		/// <param name="propertiesToSet">The properties to set</param>
		/// <returns>True if the properties can be retrieved, false otherwise</returns>
		public virtual bool CanRetrieveProperties(FileEntry file, FilePropertyType propertiesToSet)
		{
			return (((uint)propertiesToSet & ~(uint)file.AssignedProperties) & (uint)RetrievableFileProperties) != 0;
		}

		/// <summary>
		/// Retrieves the specified properties for the file
		/// </summary>
		/// <param name="file">The file</param>
		/// <param name="propertiesToSet">The properties to set</param>
		/// <param name="variantProperties">The variant properties</param>
		public virtual void RetrieveProperties(FileEntry file, FilePropertyType propertiesToSet, string[] variantProperties)
		{
			// Default implementation is empty
		}

		/// <summary>
		/// Creates a list operation for the specified target path
		/// </summary>
		/// <param name="targetPath">The target path</param>
		/// <returns>The list operation</returns>
		public virtual FileSourceOperation? CreateListOperation(string targetPath)
		{
			return null;
		}

		/// <summary>
		/// Creates a copy operation for the specified source files and target path
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy operation</returns>
		public virtual FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a copy in operation for the specified source file source, source files and target path
		/// </summary>
		/// <param name="sourceFileSource">The source file source</param>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy in operation</returns>
		public virtual FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a copy out operation for the specified target file source, source files and target path
		/// </summary>
		/// <param name="targetFileSource">The target file source</param>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The copy out operation</returns>
		public virtual FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a move operation for the specified source files and target path
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The move operation</returns>
		public virtual FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath) { 
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a delete operation for the specified files to delete
		/// </summary>
		/// <param name="filesToDelete">The files to delete</param>
		/// <returns>The delete operation</returns>
		public virtual FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a wipe operation for the specified files to wipe
		/// </summary>
		/// <param name="filesToWipe">The files to wipe</param>
		/// <returns>The wipe operation</returns>
		public virtual FileSourceOperation CreateWipeOperation(FileEntries filesToWipe)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a split operation for the specified source file and target path
		/// </summary>
		/// <param name="sourceFile">The source file</param>
		/// <param name="targetPath">The target path</param>
		/// <returns>The split operation</returns>
		public virtual FileSourceOperation CreateSplitOperation(FileEntry sourceFile, string targetPath)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a combine operation for the specified source files and target file
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <param name="targetFile">The target file</param>
		/// <returns>The combine operation</returns>
		public virtual FileSourceOperation CreateCombineOperation(FileEntries sourceFiles, string targetFile)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a create directory operation for the specified base path and directory path
		/// </summary>
		/// <param name="basePath">The base path</param>
		/// <param name="directoryPath">The directory path</param>
		/// <returns>The create directory operation</returns>
		public virtual FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates an execute operation for the specified executable file, base path and verb
		/// </summary>
		/// <param name="executableFile">The executable file</param>
		/// <param name="basePath">The base path</param>
		/// <param name="verb">The verb</param>
		/// <returns>The execute operation</returns>
		public virtual FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a test archive operation for the specified source files
		/// </summary>
		/// <param name="sourceFiles">The source files</param>
		/// <returns>The test archive operation</returns>
		public virtual FileSourceOperation CreateTestArchiveOperation(FileEntries sourceFiles)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a calculate checksum operation for the specified files, target path and target mask
		/// </summary>
		/// <param name="files">The files</param>
		/// <param name="targetPath">The target path</param>
		/// <param name="targetMask">The target mask</param>
		/// <returns>The calculate checksum operation</returns>
		public virtual FileSourceOperation CreateCalcChecksumOperation(FileEntries files, string targetPath, string targetMask)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a calculate statistics operation for the specified files
		/// </summary>
		/// <param name="files">The files</param>
		/// <returns>The calculate statistics operation</returns>
		public virtual FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a set file property operation for the specified target files and new properties
		/// </summary>
		/// <param name="targetFiles">The target files</param>
		/// <param name="newProperties">The new properties</param>
		/// <returns>The set file property operation</returns>
		public virtual FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperty[] newProperties)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the operation class for the specified operation type
		/// </summary>
		/// <param name="operationType">The operation type</param>
		/// <returns>The operation class</returns>
		public virtual Type GetOperationClass(FileSourceOperationType operationType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Checks if the specified path is at the root of the file source
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the path is at the root, false otherwise</returns>
		public virtual bool IsPathAtRoot(string path)
		{
			return path == GetRootDir(path);
		}

		/// <summary>
		/// Gets the parent directory of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The parent directory</returns>
		public virtual string GetParentDir(string path)
		{
			return System.IO.Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Gets the root directory of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The root directory</returns>
		public virtual string GetRootDir(string path)
		{
			return Path.DirectorySeparatorChar.ToString();
		}

		/// <summary>
		/// Gets the root directory of the file source
		/// </summary>
		/// <returns>The root directory</returns>
		public virtual string GetRootDir()
		{
			return GetRootDir("");
		}

		/// <summary>
		/// Gets the path type of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The path type</returns>
		public virtual PathType GetPathType(string path)
		{
			if (string.IsNullOrEmpty(path))
				return PathType.Unknown;

			if (path[0] == Path.DirectorySeparatorChar)
				return PathType.Absolute;
			else if (path.Contains(Path.DirectorySeparatorChar))
				return PathType.Relative;

			return PathType.Unknown;
		}

		/// <summary>
		/// Gets the free space of the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <param name="freeSize">The free size</param>
		/// <param name="totalSize">The total size</param>
		/// <returns>True if the free space was retrieved successfully, false otherwise</returns>
		public virtual bool GetFreeSpace(string path, out long freeSize, out long totalSize)
		{
			freeSize = 0;
			totalSize = 0;
			return false;
		}

		/// <summary>
		/// Gets the local name of the specified file
		/// </summary>
		/// <param name="file">The file</param>
		/// <returns>True if the local name was retrieved successfully, false otherwise</returns>
		public virtual bool GetLocalName(ref FileEntry file)
		{
			return false;
		}

		/// <summary>
		/// Creates a directory at the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the directory was created successfully, false otherwise</returns>
		public virtual bool CreateDirectory(string path)
		{
			return false;
		}

		/// <summary>
		/// Checks if a file system entry exists at the specified path
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the file system entry exists, false otherwise</returns>
		public virtual bool FileSystemEntryExists(string path)
		{
			return true;
		}

		/// <summary>
		/// Gets the default view for the file source
		/// </summary>
		/// <param name="defaultView">The default view</param>
		/// <returns>True if the default view was retrieved successfully, false otherwise</returns>
		public virtual bool GetDefaultView(out FileSourceField[] defaultView)
		{
			defaultView = null;
			return false;
		}

		/// <summary>
		/// Queries the context menu for the specified files
		/// </summary>
		/// <param name="files">The files</param>
		/// <param name="menu">The menu</param>
		/// <returns>True if the context menu was queried successfully, false otherwise</returns>
		public virtual bool QueryContextMenu(FileEntries files, ref ContextMenuStrip menu)
		{
			return false;
		}

		/// <summary>
		/// Gets a connection for the specified operation
		/// </summary>
		/// <param name="operation">The operation</param>
		/// <returns>The connection</returns>
		public virtual FileSourceConnection GetConnection(IFileSourceOperation operation)
		{
			if (operation == null)
				return null;

			lock (_syncRoot)
			{
				// Try to find an available connection
				foreach (var connection in _connections)
				{
					if (connection.IsAvailable() && connection.Acquire(operation))
					{
						return connection;
					}
				}

				// Create a new connection if none available
				var newConnection = new FileSourceConnection();
				if (newConnection.Acquire(operation))
				{
					_connections.Add(newConnection);
					return newConnection;
				}

				return null;
			}
		}

		/// <summary>
		/// Removes the specified operation from the queue
		/// </summary>
		/// <param name="operation">The operation</param>
		public virtual void RemoveOperationFromQueue(IFileSourceOperation operation)
		{
			// Default implementation does nothing
		}

		/// <summary>
		/// Adds a child file source to this file source
		/// </summary>
		/// <param name="fileSource">The child file source</param>
		public virtual void AddChild(IFileSource fileSource)
		{
			if (fileSource == null)
				return;

			lock (_syncRoot)
			{
				if (!_children.Contains(fileSource))
				{
					_children.Add(fileSource);
					fileSource.ParentFileSource = this;
				}
			}
		}

		/// <summary>
		/// Reloads the file source with the specified paths to reload
		/// </summary>
		/// <param name="pathsToReload">The paths to reload</param>
		public virtual void Reload(string[] pathsToReload)
		{
			if (pathsToReload == null || pathsToReload.Length == 0)
				return;

			lock (_syncRoot)
			{
				foreach (var handler in _reloadEventListeners)
				{
					handler?.Invoke(this, pathsToReload);
				}
			}
		}

		/// <summary>
		/// Reloads the file source with the specified path to reload
		/// </summary>
		/// <param name="pathToReload">The path to reload</param>
		public virtual void Reload(string pathToReload)
		{
			if (string.IsNullOrEmpty(pathToReload))
				return;

			Reload(new[] { pathToReload });
		}

		/// <summary>
		/// Adds a reload event listener
		/// </summary>
		/// <param name="handler">The handler</param>
		public virtual void AddReloadEventListener(FileSourceReloadEventHandler handler)
		{
			if (handler == null)
				return;

			lock (_syncRoot)
			{
				if (!_reloadEventListeners.Contains(handler))
				{
					_reloadEventListeners.Add(handler);
				}
			}
		}

		/// <summary>
		/// Removes a reload event listener
		/// </summary>
		/// <param name="handler">The handler</param>
		public virtual void RemoveReloadEventListener(FileSourceReloadEventHandler handler)
		{
			if (handler == null)
				return;

			lock (_syncRoot)
			{
				_reloadEventListeners.Remove(handler);
			}
		}

		/// <summary>
		/// Gets the main icon for the file source
		/// </summary>
		/// <param name="path">The path to the icon</param>
		/// <returns>True if the icon was retrieved successfully, false otherwise</returns>
		public virtual bool GetMainIcon(out string path)
		{
			path = null;
			return false;
		}

		/// <summary>
		/// Checks if the specified path is supported by the file source
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>True if the path is supported, false otherwise</returns>
		public virtual bool IsSupportedPath(string path)
		{
			return true;
		}

		/// <summary>
		/// Handles the completion of an operation
		/// </summary>
		/// <param name="operation">The completed operation</param>
		public virtual void OperationFinished(IFileSourceOperation operation)
		{
			// Default implementation is empty
		}

		/// <summary>
		/// Handles the reloading of the file source
		/// </summary>
		/// <param name="pathsToReload">The paths to reload</param>
		public virtual void DoReload(string[] pathsToReload)
		{
			// Default implementation is empty
		}

		/// <summary>
		/// Gets the file system of the file source
		/// </summary>
		/// <returns>The file system</returns>
		public virtual string GetFileSystem()
		{
			return string.Empty;
		}
	}

	#endregion
}