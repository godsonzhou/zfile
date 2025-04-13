//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;
//using System.IO;
//using Zfile.FileSources;

//namespace Zfile
//{
//    public class WfxPluginFileSource : IWfxPluginFileSource
//    {
//        private readonly WfxModule _wfxModule;
//        private readonly string _pluginName;
//        private string _currentAddress;
//        private string _rootDirectory;
//        private readonly List<FileSourceConnection> _connections = new List<FileSourceConnection>();
//        private readonly object _connectionLock = new object();

//        public WfxModule WfxModule => _wfxModule;
//        public string PluginName => _pluginName;
//        public string CurrentAddress
//        {
//            get => _currentAddress;
//            set => _currentAddress = value;
//        }
//        public string RootDirectory
//        {
//            get => _rootDirectory;
//            set => _rootDirectory = value;
//        }

//        public WfxPluginFileSource(string modulePath, string pluginName)
//        {
//            _wfxModule = new WfxModule(modulePath);
//            _pluginName = pluginName;
//            _currentAddress = string.Empty;
//            _rootDirectory = string.Empty;

//            if (!_wfxModule.LoadModule())
//            {
//                throw new Exception($"Failed to load WFX module: {modulePath}");
//            }
//        }

//        public bool CanRead(string path) => true;
//        public bool CanWrite(string path) => true;
//        public bool CanDelete(string path) => true;
//        public bool CanExecute(string path) => true;
//        public bool CanRename(string path) => true;
//        public bool CanCreateDirectory(string path) => true;
//        public bool CanCopyFrom(string path) => true;
//        public bool CanCopyTo(string path) => true;
//        public bool CanMoveFrom(string path) => true;
//        public bool CanMoveTo(string path) => true;
//        public bool CanCalculateSize(string path) => true;
//        public bool CanCalculateChecksum(string path) => false;
//        public bool CanExtractArchive(string path) => false;
//        public bool CanSetAttributes(string path) => true;
//        public bool CanSetTime(string path) => true;

//        public bool GetFiles(string path, List<FileEntry> files)
//        {
//            try
//            {
//                foreach (var findData in _wfxModule.FindFiles(path))
//                {
//                    var entry = new FileEntry
//                    {
//                        Name = findData.FileName,
//                        Size = findData.FileSize,
//                        Attributes = findData.FileAttributes,
//                        CreationTime = DateTime.FromFileTime(findData.CreationTime),
//                        LastAccessTime = DateTime.FromFileTime(findData.LastAccessTime),
//                        LastWriteTime = DateTime.FromFileTime(findData.LastWriteTime),
//                        IsDirectory = (findData.FileAttributes & WfxConstants.FILE_ATTRIBUTE_DIRECTORY) != 0
//                    };

//                    files.Add(entry);
//                }
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public bool CreateDirectory(string path)
//        {
//            return _wfxModule.CreateDirectory(path);
//        }

//        public bool DeleteFile(string path)
//        {
//            return _wfxModule.DeleteFile(path);
//        }

//        public bool DeleteDirectory(string path)
//        {
//            return _wfxModule.RemoveDirectory(path);
//        }

//        public bool FileSystemEntryExists(string path)
//        {
//            // Implementation would check if file or directory exists
//            return true;
//        }

//        public bool GetDefaultView(out FileSourceField[] defaultView)
//        {
//            // Define default columns for WFX file source
//            defaultView = new FileSourceField[]
//            {
//                FileSourceField.Name,
//                FileSourceField.Size,
//                FileSourceField.Type,
//                FileSourceField.Modified
//            };
//            return true;
//        }

//        public bool QueryContextMenu(List<FileEntry> files, ref ContextMenuStrip menu)
//        {
//            // Add WFX specific context menu items
//            return true;
//        }

//        public FileSourceConnection GetConnection(IFileSourceOperation operation)
//        {
//            lock (_connectionLock)
//            {
//                // Try to find an available connection
//                foreach (var connection in _connections)
//                {
//                    if (connection.IsAvailable())
//                    {
//                        if (connection.Acquire(operation))
//                        {
//                            return connection;
//                        }
//                    }
//                }

//                // Create a new connection if none available
//                var newConnection = new FileSourceConnection();
//                if (newConnection.Acquire(operation))
//                {
//                    _connections.Add(newConnection);
//                    return newConnection;
//                }

//                return null;
//            }
//        }

//        public void RemoveOperationFromQueue(IFileSourceOperation operation)
//        {
//            // Implementation would remove operation from queue
//        }

//        public void Dispose()
//        {
//            // Disconnect all connections
//            if (_wfxModule.IsLoaded)
//            {
//                _wfxModule.Disconnect(_rootDirectory);
//            }

//            // Clean up connections
//            lock (_connectionLock)
//            {
//                foreach (var connection in _connections)
//                {
//                    // Clean up connection resources
//                }
//                _connections.Clear();
//            }
//        }
//    }

//    //public class FileEntry
//    //{
//    //    public string Name { get; set; }
//    //    public long Size { get; set; }
//    //    public int Attributes { get; set; }
//    //    public DateTime CreationTime { get; set; }
//    //    public DateTime LastAccessTime { get; set; }
//    //    public DateTime LastWriteTime { get; set; }
//    //    public bool IsDirectory { get; set; }
//    //}

//    public enum FileSourceField
//    {
//        Name,
//        Size,
//        Type,
//        Modified,
//        Created,
//        Accessed,
//        Attributes,
//        Extension
//    }

//    public interface IFileSourceOperation
//    {
//        string OperationName { get; }
//        bool IsAborted { get; }
//        void Abort();
//    }
//}