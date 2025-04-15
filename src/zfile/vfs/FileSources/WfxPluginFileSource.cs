namespace Zfile.FileSources;

public interface IWfxPluginFileSource : IFileSource
{
	void FillAndCount(FileEntries files, bool countDirs, bool excludeRootDir,
		out FileEntries newFiles, out long filesCount, out long filesSize);
	bool FillSingleFile(string fullPath, out FileEntry file);
	int WfxCopyMove(string sourceFile, string targetFile, int flags, RemoteInfo remoteInfo,
		bool isInternal, bool isCopyMoveIn);
	int PluginNumber { get; }
	WfxModule WfxModule { get; }
}

public class RemoteInfo
{
	public string RemoteName { get; set; }
	public string UserName { get; set; }
	public string Password { get; set; }
}

/// <summary>
/// Represents a WFX plugin file source
/// </summary>
public class WfxPluginFileSource : IWfxPluginFileSource, IFileSource
{
	private readonly WfxModule _wfxModule;
	private readonly string _pluginName;
	private string _currentAddress;
	private string _rootDirectory;
	private readonly List<FileSourceConnection> _connections = new List<FileSourceConnection>();
	private readonly object _connectionLock = new object();
	private readonly ThreadSafeList<IFileSourceOperation> _operationsQueue = new ThreadSafeList<IFileSourceOperation>();
	private readonly object _operationsQueueLock = new object();

	public WfxModule WfxModule => _wfxModule;
	public string PluginName => _pluginName;
	public int PluginNumber => _wfxModule.PluginNumber;
	public string CurrentAddress
	{
		get => _currentAddress;
		set => _currentAddress = value;
	}
	public string RootDirectory
	{
		get => _rootDirectory;
		set => _rootDirectory = value;
	}

	public WfxPluginFileSource(string modulePath, string pluginName)
	{
		_wfxModule = new WfxModule(modulePath);
		_pluginName = pluginName;
		_currentAddress = string.Empty;
		_rootDirectory = string.Empty;
		Uri = new Uri($"wfx://{pluginName}/");

		if (!_wfxModule.LoadModule())
		{
			throw new Exception($"Failed to load WFX module: {modulePath}");
		}
	}

	#region IFileSource Implementation
	// File source properties
	public Uri Uri { get; private set; }
	public string ClassName => GetType().Name;
	public int RefCount { get; private set; } = 1;
	public string FileSystem => _pluginName;
	public string CurrentWorkingDirectory => _currentAddress;
	public FilePropertyType SupportedFileProperties => FilePropertyType.Name | FilePropertyType.Size | 
		FilePropertyType.Attributes | FilePropertyType.ModificationTime | 
		FilePropertyType.CreationTime | FilePropertyType.LastAccessTime;
	public FilePropertyType RetrievableFileProperties => SupportedFileProperties;
	public FileSourceOperationTypes[] OperationsTypes => new[] {
		FileSourceOperationTypes.List,
		FileSourceOperationTypes.Copy,
		FileSourceOperationTypes.CopyIn,
		FileSourceOperationTypes.CopyOut,
		FileSourceOperationTypes.Move,
		FileSourceOperationTypes.Delete,
		FileSourceOperationTypes.CreateDirectory,
		FileSourceOperationTypes.Execute,
		FileSourceOperationTypes.SetFileProperty
	};
	public FileSourceProperty Properties => FileSourceProperty.IsVirtual | 
		FileSourceProperty.IsRemote | 
		FileSourceProperty.CanCreateDirectory | 
		FileSourceProperty.HasAttributesSupport;
	public IFileSource ParentFileSource { get; set; }

	public bool Equals(IFileSource fileSource)
	{
		if (fileSource is IWfxPluginFileSource wfxFileSource)
		{
			return wfxFileSource.PluginName == _pluginName && 
				wfxFileSource.CurrentAddress == _currentAddress;
		}
		return false;
	}

	public bool IsInterface(Type interfaceType)
	{
		return interfaceType.IsAssignableFrom(GetType());
	}

	public bool IsClass(Type classType)
	{
		return GetType() == classType || GetType().IsSubclassOf(classType);
	}

	public bool SetCurrentWorkingDirectory(string newDir)
	{
		_currentAddress = newDir;
		return true;
	}

	public FileEntries GetFiles(string targetPath)
	{
		var files = new FileEntries();
		GetFiles(targetPath, files);
		return files;
	}

	public FileEntry CreateFileObject(string path)
	{
		// Create a file object for the specified path
		var fileName = Path.GetFileName(path);
		var isDirectory = path.EndsWith("\\") || path.EndsWith("/");

		return new FileEntry
		{
			Name = fileName,
			IsDirectory = isDirectory,
			Attributes = isDirectory ? WfxConstants.FILE_ATTRIBUTE_DIRECTORY : WfxConstants.FILE_ATTRIBUTE_NORMAL
		};
	}

	public bool CanRetrieveProperties(FileEntry file, FilePropertyType properties)
	{
		return (_wfxModule.ContentPlugin && (properties & FilePropertyType.Variant) != 0) ||
			   ((properties & SupportedFileProperties) == properties);
	}

	public void RetrieveProperties(FileEntry file, FilePropertyType properties)
	{
		if (_wfxModule.ContentPlugin)
		{
			// Handle variant properties for content plugins
			if ((properties & FilePropertyType.Variant) != 0)
			{
				// Retrieve variant properties
			}
		}
	}

	public bool GetLocalName(FileEntry file)
	{
		return _wfxModule.GetLocalName(file.FullPath, 260);
	}
	#endregion

	// #region WFX Plugin Capabilities
	public bool CanRead(string path) => true;
	public bool CanWrite(string path) => true;
	public bool CanDelete(string path) => true;
	public bool CanExecute(string path) => true;
	public bool CanRename(string path) => true;
	public bool CanCreateDirectory(string path) => true;
	public bool CanCopyFrom(string path) => true;
	public bool CanCopyTo(string path) => true;
	public bool CanMoveFrom(string path) => true;
	public bool CanMoveTo(string path) => true;
	public bool CanCalculateSize(string path) => true;
	public bool CanCalculateChecksum(string path) => false;
	public bool CanExtractArchive(string path) => false;
	public bool CanSetAttributes(string path) => true;
	public bool CanSetTime(string path) => true;

	public bool GetFiles(string path, FileEntries files)
	{
		try
		{
			foreach (var findData in _wfxModule.FindFiles(path))
			{
				var entry = new FileEntry
				{
					Name = findData.FileName,
					Size = findData.FileSize,
					Attributes = findData.FileAttributes,
					CreationTime = DateTime.FromFileTime(findData.CreationTime),
					LastAccessTime = DateTime.FromFileTime(findData.LastAccessTime),
					ModificationTime = DateTime.FromFileTime(findData.LastWriteTime),
					IsDirectory = (findData.FileAttributes & WfxConstants.FILE_ATTRIBUTE_DIRECTORY) != 0
				};

				files.Add(entry);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool CreateDirectory(string path)
	{
		var result = _wfxModule.CreateDirectory(path);
		if (result == WfxConstants.WFX_SUCCESS)
		{
			// Log success
			return true;
		}
		else
		{
			// Log error
			return false;
		}
	}

	public bool DeleteFile(string path)
	{
		return _wfxModule.DeleteFile(path) == WfxConstants.WFX_SUCCESS;
	}

	public bool DeleteDirectory(string path)
	{
		return _wfxModule.RemoveDirectory(path) == WfxConstants.WFX_SUCCESS;
	}

	public bool FileSystemEntryExists(string path)
	{
		return _wfxModule.FileExists(path);
	}

	public bool GetDefaultView(out FileSourceField[] defaultView)
	{
		defaultView = new FileSourceField[]
		{
			FileSourceField.Name,
			FileSourceField.Size,
			FileSourceField.Type,
			FileSourceField.Modified
		};
		return true;
	}

	public bool QueryContextMenu(FileEntries files, ref ContextMenuStrip menu)
	{
		// Add WFX specific context menu items
		return true;
	}

	public FileSourceConnection GetConnection(IFileSourceOperation operation)
	{
		lock (_connectionLock)
		{
			// Try to find an available connection
			foreach (var connection in _connections)
			{
				if (connection.IsAvailable())
				{
					if (connection.Acquire(operation))
					{
						return connection;
					}
				}
			}

			// Create a new connection if none available
			var newConnection = new WfxPluginFileSourceConnection(_wfxModule);
			if (newConnection.Acquire(operation))
			{
				_connections.Add(newConnection);
				return newConnection;
			}

			return null;
		}
	}

	public void RemoveOperationFromQueue(IFileSourceOperation operation)
	{
		lock (_operationsQueueLock)
		{
			_operationsQueue.Remove(operation);
		}
	}

	public void Dispose()
	{
		// Disconnect all connections
		if (_wfxModule.IsLoaded)
		{
			_wfxModule.Disconnect(_rootDirectory);
		}

		// Clean up connections
		lock (_connectionLock)
		{
			foreach (var connection in _connections)
			{
				connection.Release();
			}
			_connections.Clear();
		}

		// Dispose WfxModule
		_wfxModule.Dispose();
	}

	public void FillAndCount(FileEntries files, bool countDirs, bool excludeRootDir,
		out FileEntries newFiles, out long filesCount, out long filesSize)
	{
		filesCount = 0;
		filesSize = 0;
		newFiles = new FileEntries();

		if (excludeRootDir)
		{
			if (files.Count != 1)
				throw new Exception("Only a single directory can be set with ExcludeRootDir=True");

			FillAndCountRecursive(files[0].Path, newFiles, ref filesCount, ref filesSize, countDirs);
		}
		else
		{
			foreach (var file in files)
			{
				newFiles.Add(file.Clone());

				if (file.IsDirectory && !file.IsLinkToDirectory)
				{
					if (countDirs)
						filesCount++;
					FillAndCountRecursive(file.Path, newFiles, ref filesCount, ref filesSize, countDirs);
				}
				else
				{
					filesCount++;
					filesSize += file.Size;
				}
			}
		}
	}

	private void FillAndCountRecursive(string path, FileEntries newFiles, ref long filesCount, ref long filesSize, bool countDirs)
	{
		foreach (var findData in _wfxModule.FindFiles(path))
		{
			if (findData.FileName == "." || findData.FileName == "..")
				continue;

			var entry = new FileEntry
			{
				Name = findData.FileName,
				Size = findData.FileSize,
				Attributes = findData.FileAttributes,
				CreationTime = DateTime.FromFileTime(findData.CreationTime),
				LastAccessTime = DateTime.FromFileTime(findData.LastAccessTime),
				ModificationTime = DateTime.FromFileTime(findData.LastWriteTime),
				IsDirectory = (findData.FileAttributes & WfxConstants.FILE_ATTRIBUTE_DIRECTORY) != 0
			};

			newFiles.Add(entry);

			if (entry.IsDirectory)
			{
				if (countDirs)
					filesCount++;
				FillAndCountRecursive(Path.Combine(path, findData.FileName), newFiles, ref filesCount, ref filesSize, countDirs);
			}
			else
			{
				filesSize += entry.Size;
				filesCount++;
			}
		}
	}

	public bool FillSingleFile(string fullPath, out FileEntry file)
	{
		file = null;
		var filePath = Path.GetDirectoryName(fullPath);
		var expectedFileName = Path.GetFileName(fullPath);

		foreach (var findData in _wfxModule.FindFiles(filePath))
		{
			if (findData.FileName == expectedFileName)
			{
				file = new FileEntry
				{
					Name = findData.FileName,
					Size = findData.FileSize,
					Attributes = findData.FileAttributes,
					CreationTime = DateTime.FromFileTime(findData.CreationTime),
					LastAccessTime = DateTime.FromFileTime(findData.LastAccessTime),
					ModificationTime = DateTime.FromFileTime(findData.LastWriteTime),
					IsDirectory = (findData.FileAttributes & WfxConstants.FILE_ATTRIBUTE_DIRECTORY) != 0
				};
				return true;
			}
		}
		return false;
	}

	public int WfxCopyMove(string sourceFile, string targetFile, int flags, RemoteInfo remoteInfo,
		bool isInternal, bool isCopyMoveIn)
	{
		if (isInternal)
		{
			bool isMove = (flags & WfxConstants.FS_COPYFLAGS_MOVE) != 0;
			bool overwrite = (flags & WfxConstants.FS_COPYFLAGS_OVERWRITE) != 0;
			return _wfxModule.RenameMoveFile(sourceFile, targetFile, isMove, overwrite, remoteInfo);
		}
		else
		{
			if (isCopyMoveIn)
				return _wfxModule.PutFile(sourceFile, targetFile, flags);
			else
				return _wfxModule.GetFile(sourceFile, targetFile, flags, remoteInfo);
		}
	}

	public void AddToConnectionQueue(IFileSourceOperation operation)
	{
		lock (_operationsQueueLock)
		{
			if (!_operationsQueue.Contains(operation))
				_operationsQueue.Add(operation);
		}
	}

	public void RemoveFromConnectionQueue(IFileSourceOperation operation)
	{
		lock (_operationsQueueLock)
		{
			_operationsQueue.Remove(operation);
		}
	}

	public void AddConnection(FileSourceConnection connection)
	{
		lock (_connectionLock)
		{
			if (!_connections.Contains(connection))
				_connections.Add(connection);
		}
	}

	public void RemoveConnection(FileSourceConnection connection)
	{
		lock (_connectionLock)
		{
			_connections.Remove(connection);
		}
	}

	public void OperationFinished(IFileSourceOperation operation)
	{
		var connection = FindConnectionByOperation(operation);
		if (connection != null)
		{
			connection.Release();

			lock (_connectionLock)
			{
				var allowedOps = new List<FileSourceOperationTypes>();
				if (operation.OperationType == FileSourceOperationTypes.CopyIn ||
					operation.OperationType == FileSourceOperationTypes.CopyOut ||
					operation.OperationType == FileSourceOperationTypes.Delete ||
					operation.OperationType == FileSourceOperationTypes.Copy ||
					operation.OperationType == FileSourceOperationTypes.Move)
				{
					allowedOps.Add(operation.OperationType);
					NotifyNextWaitingOperation(allowedOps);
				}
				else
				{
					_connections.Remove(connection);
				}
			}
		}
	}

	private void NotifyNextWaitingOperation(List<FileSourceOperationTypes> allowedOps)
	{
		lock (_operationsQueueLock)
		{
			foreach (var operation in _operationsQueue.ToList())
			{
				if (operation.State == FileSourceOperationState.WaitingForConnection &&
					allowedOps.Contains(operation.OperationType))
				{
					operation.ConnectionAvailableNotify();
					break;
				}
			}
		}
	}

	public void CreateConnections()
	{
		lock (_connectionLock)
		{
			if (_connections.Count == 0)
			{
				// Reserve some connections
				_connections.Add(new WfxPluginFileSourceConnection(_wfxModule)); // CopyIn
				_connections.Add(new WfxPluginFileSourceConnection(_wfxModule)); // CopyOut
				_connections.Add(new WfxPluginFileSourceConnection(_wfxModule)); // Delete
				_connections.Add(new WfxPluginFileSourceConnection(_wfxModule)); // CopyMove
			}
		}
	}

	public IFileSourceOperation CreateListOperation(string targetPath)
	{
		return new WfxPluginListOperation(this, targetPath);
	}

	public IFileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyOperation(this, this, sourceFiles, targetPath);
	}

	public IFileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
	}

	public IFileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
	}

	public IFileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginMoveOperation(this, sourceFiles, targetPath);
	}

	public IFileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
	{
		return new WfxPluginDeleteOperation(this, filesToDelete);
	}

	public IFileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
	{
		return new WfxPluginCreateDirectoryOperation(this, basePath, directoryPath);
	}

	public IFileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
	{
		return new WfxPluginExecuteOperation(this, executableFile, basePath, verb);
	}

	public IFileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, Dictionary<FilePropertyType, object> newProperties)
	{
		return new WfxPluginSetFilePropertyOperation(this, targetFiles, newProperties);
	}

	public IFileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
	{
		return new WfxPluginCalcStatisticsOperation(this, files);
	}

	private FileSourceConnection FindConnectionByOperation(IFileSourceOperation operation)
	{
		if (operation == null)
			return null;

		lock (_connectionLock)
		{
			foreach (var connection in _connections)
			{
				if (connection.AssignedOperation == operation)
				{
					return connection;
				}
			}
			return null;
		}
	}
}

public enum FileSourceField
{
	Name,
	Size,
	Type,
	Modified,
	Created,
	Accessed,
	Attributes,
	Extension
}

/// <summary>
/// Represents a connection to a WFX plugin file source
/// </summary>
public class WfxPluginFileSourceConnection : FileSourceConnection
{
	private readonly WfxModule _wfxModule;

	/// <summary>
	/// Gets the WFX module
	/// </summary>
	public WfxModule WfxModule => _wfxModule;

	/// <summary>
	/// Creates a new instance of the WfxPluginFileSourceConnection class
	/// </summary>
	/// <param name="wfxModule">The WFX module</param>
	public WfxPluginFileSourceConnection(WfxModule wfxModule)
	{
		_wfxModule = wfxModule;
	}
}