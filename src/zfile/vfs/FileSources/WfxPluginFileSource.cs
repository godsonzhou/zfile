
namespace zfile;

public interface IWfxPluginFileSource : IFileSource
{
	void FillAndCount(FileEntries files, bool countDirs, bool excludeRootDir,
		out FileEntries newFiles, out long filesCount, out long filesSize);
	bool FillSingleFile(string fullPath, out FileEntry file);
	FsFileResult WfxCopyMove(string sourceFile, string targetFile, FsCopyFlags flags, FileEntry remoteInfo,
		bool isInternal, bool isCopyMoveIn);
	int PluginNumber { get; }
	WfxModule WfxModule { get; }
	string PluginName { get; }
	string RootDirectory { get; set; }
	StringList WfxOperationList { get; }
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
public class WfxPluginFileSource : FileSource, IWfxPluginFileSource
{
	private readonly WfxModule _wfxModule;
	private readonly string _pluginName;
	private string _currentAddress;
	private string _rootDirectory;
	private readonly List<FileSourceConnection> _connections = new List<FileSourceConnection>();
	private readonly object _connectionLock = new object();
	private readonly List<IFileSourceOperation> _operationsQueue = new();
	private readonly object _operationsQueueLock = new object();
	private static readonly StringList _wfxOperationList = new StringList();

	public WfxModule WfxModule => _wfxModule;
	public string PluginName => _pluginName;
	public int PluginNumber => _wfxModule.PluginNumber;
	public StringList WfxOperationList => _wfxOperationList;
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
	//public string ClassName => GetType().Name;
	public int RefCount { get; private set; } = 1;
	public override string FileSystem => _pluginName;
	public override string CurrentWorkingDirectory => _currentAddress;
	public FilePropertyType SupportedFileProperties => FilePropertyType.Name | FilePropertyType.Size |
		FilePropertyType.Attributes | FilePropertyType.ModificationTime |
		FilePropertyType.CreationTime | FilePropertyType.LastAccessTime;
	public FilePropertyType RetrievableFileProperties => SupportedFileProperties;
	public FileSourceOperationType[] OperationsTypes => new[] {
		FileSourceOperationType.List,
		FileSourceOperationType.Copy,
		FileSourceOperationType.CopyIn,
		FileSourceOperationType.CopyOut,
		FileSourceOperationType.Move,
		FileSourceOperationType.Delete,
		FileSourceOperationType.CreateDirectory,
		FileSourceOperationType.Execute,
		FileSourceOperationType.SetFileProperty
	};
	public FileSourceProperty Properties => FileSourceProperty.IsVirtual |
		FileSourceProperty.IsRemote |
		FileSourceProperty.CanCreateDirectory |
		FileSourceProperty.HasAttributesSupport;
	//public IFileSource ParentFileSource { get; set; }

	public override bool Equals(IFileSource fileSource)
	{
		if (fileSource is IWfxPluginFileSource wfxFileSource)
		{
			return wfxFileSource.PluginName == _pluginName &&
				wfxFileSource.CurrentAddress == _currentAddress;
		}
		return false;
	}

	public override bool IsInterface(Type interfaceType)
	{
		return interfaceType.IsAssignableFrom(GetType());
	}

	public override bool IsClass(Type classType)
	{
		return GetType() == classType || GetType().IsSubclassOf(classType);
	}

	public override bool SetCurrentWorkingDirectory(string newDir)
	{
		_currentAddress = newDir;
		return true;
	}

	public override FileEntries GetFiles(string targetPath)
	{
		var files = new FileEntries();
		GetFiles(targetPath, files);
		return files;
	}

	public override FileEntry CreateFile(string path)
	{
		// Create a file object for the specified path
		var fileName = Path.GetFileName(path);
		var isDirectory = path.EndsWith("\\") || path.EndsWith("/");

		return new FileEntry
		{
			Name = fileName,
			IsDirectory = isDirectory,
			Attributes = isDirectory ? (FileAttributes)WfxConstants.FILE_ATTRIBUTE_DIRECTORY : (FileAttributes)WfxConstants.FILE_ATTRIBUTE_NORMAL
		};
	}

	public static FileEntry CreateFile(string path, WfxFindData findData)
	{
		return new FileEntry
		{
			Name = findData.FileName,
			Size = findData.FileSize,
			Attributes = (FileAttributes)findData.FileAttributes,
			CreationTime = DateTime.FromFileTime(findData.CreationTime),
			LastAccessTime = DateTime.FromFileTime(findData.LastAccessTime),
			ModificationTime = DateTime.FromFileTime(findData.LastWriteTime),
			IsDirectory = (findData.FileAttributes & WfxConstants.FILE_ATTRIBUTE_DIRECTORY) != 0,
			Path = path
		};
	}

	public override bool CanRetrieveProperties(FileEntry file, FilePropertyType properties)
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
					Attributes = (FileAttributes)findData.FileAttributes,
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

	public override bool CreateDirectory(string path)
	{
		var result = _wfxModule.CreateDirectory(path);
		if (result)
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
		return _wfxModule.DeleteFile(path);
	}

	public bool DeleteDirectory(string path)
	{
		return _wfxModule.RemoveDirectory(path);
	}

	public override bool FileSystemEntryExists(string path)
	{
		return _wfxModule.FileExists(path);
	}

	public override bool GetDefaultView(out FileSourceField[] defaultView)
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

	public override bool QueryContextMenu(FileEntries files, ref ContextMenuStrip menu)
	{
		// Add WFX specific context menu items
		return true;
	}

	public override FileSourceConnection GetConnection(FileSourceOperation operation)
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

	public override void RemoveOperationFromQueue(IFileSourceOperation operation)
	{
		lock (_operationsQueueLock)
		{
			_operationsQueue.Remove(operation);
		}
	}

	public override void Dispose()
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
				Attributes = (FileAttributes)findData.FileAttributes,
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
		file = new FileEntry();
		var filePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
		var expectedFileName = Path.GetFileName(fullPath);

		foreach (var findData in _wfxModule.FindFiles(filePath))
		{
			if (findData.FileName == expectedFileName)
			{
				file = new FileEntry
				{
					Name = findData.FileName,
					Size = findData.FileSize,
					Attributes = (FileAttributes)findData.FileAttributes,
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

	public FsFileResult WfxCopyMove(string sourceFile, string targetFile, FsCopyFlags flags, FileEntry remoteInfo,
		bool isInternal, bool isCopyMoveIn)
	{
		if (isInternal)
		{
			bool isMove = (flags & FsCopyFlags.Move) != 0;
			bool overwrite = (flags & FsCopyFlags.Overwrite) != 0;
			var _remoteInfo = new RemoteFileInfo
			{
				SizeLow = (int)(remoteInfo.Size & 0xFFFFFFFF),
				SizeHigh = (int)(remoteInfo.Size >> 32),
				Attr = (int)remoteInfo.Attributes,
				LastWriteTime = DateTimeToWfxFileTime(remoteInfo.ModificationTime)
			};
			return (FsFileResult)_wfxModule.MoveFile(sourceFile, targetFile, isMove, overwrite, _remoteInfo);
		}
		else
		{
			if (isCopyMoveIn)
				return _wfxModule.PutFile(sourceFile, targetFile, flags);
			else
				return _wfxModule.GetFile(sourceFile, targetFile, flags, remoteInfo);
		}
	}

	private long DateTimeToWfxFileTime(DateTime modificationTime)
	{
		throw new NotImplementedException();
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
				var allowedOps = new List<FileSourceOperationType>();
				if (operation.OperationType == FileSourceOperationType.CopyIn ||
					operation.OperationType == FileSourceOperationType.CopyOut ||
					operation.OperationType == FileSourceOperationType.Delete ||
					operation.OperationType == FileSourceOperationType.Copy ||
					operation.OperationType == FileSourceOperationType.Move)
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

	private void NotifyNextWaitingOperation(List<FileSourceOperationType> allowedOps)
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

	public override FileSourceOperation CreateListOperation(string targetPath)
	{
		return new WfxPluginListOperation(this, targetPath);
	}

	public override FileSourceOperation CreateCopyOperation(FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyOperation(this, this, sourceFiles, targetPath);
	}

	public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
	}

	public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
	}

	public override FileSourceOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
	{
		return new WfxPluginMoveOperation(this, sourceFiles, targetPath);
	}

	public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
	{
		return new WfxPluginDeleteOperation(this, filesToDelete);
	}

	public override FileSourceOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
	{
		return new WfxPluginCreateDirectoryOperation(this, basePath, directoryPath);
	}

	public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
	{
		return new WfxPluginExecuteOperation(this, executableFile, basePath, verb);
	}

	public override FileSourceOperation CreateSetFilePropertyOperation(FileEntries targetFiles, FileProperty[] newProperties)
	{
		return new WfxPluginSetFilePropertyOperation(this, targetFiles, newProperties);
	}

	public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
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

	internal static IFileSource? CreateByRootName(string name)
	{
		throw new NotImplementedException();
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