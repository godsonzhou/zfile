using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using Zfile.FileSources;

namespace Zfile
{
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

		public WfxModule WfxModule => _wfxModule;
		public string PluginName => _pluginName;
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

		public List<FileEntry> GetFiles(string targetPath)
		{
			var files = new List<FileEntry>();
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
			return (SupportedFileProperties & properties) == properties;
		}

		public void RetrieveProperties(FileEntry file, FilePropertyType properties)
		{
			// Properties are already retrieved during GetFiles
		}

		public bool GetLocalName(FileEntry file)
		{
			// Implementation would get local name for the file
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

		public bool GetFiles(string path, List<FileEntry> files)
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
						LastWriteTime = DateTime.FromFileTime(findData.LastWriteTime),
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
			return _wfxModule.CreateDirectory(path);
		}

		public bool DeleteFile(string path)
		{
			return _wfxModule.DeleteFile(path);
		}

		public bool DeleteDirectory(string path)
		{
			return _wfxModule.RemoveDirectory(path);
		}

		public bool FileSystemEntryExists(string path)
		{
			// Implementation would check if file or directory exists
			return true;
		}

		public bool GetDefaultView(out FileSourceField[] defaultView)
		{
			// Define default columns for WFX file source
			defaultView = new FileSourceField[]
			{
				FileSourceField.Name,
				FileSourceField.Size,
				FileSourceField.Type,
				FileSourceField.Modified
			};
			return true;
		}

		public bool QueryContextMenu(List<FileEntry> files, ref ContextMenuStrip menu)
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
			lock (_connectionLock)
			{
				var connection = FindConnectionByOperation(operation);
				if (connection != null)
				{
					connection.Release();
				}
			}
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

	public interface IFileSourceOperation
	{
		string OperationName { get; }
		bool IsAborted { get; }
		void Abort();
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
}