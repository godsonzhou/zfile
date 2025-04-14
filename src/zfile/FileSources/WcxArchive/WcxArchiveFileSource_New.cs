using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using Zfile;
using Zfile.FileSources;

namespace ZFile.FileSources.WcxArchive
{
    public class WcxArchiveFileSource : ArchiveFileSource, IWcxArchiveFileSource
    {
        private string _moduleFileName;
        private int _pluginCapabilities;
        private ThreadSafeList<WcxHeader> _arcFileList;
        private WcxModule _wcxModule;
        private int _openResult;
        private List<FileSourceConnection> _connections;
        private List<FileSourceOperation> _operationsQueue;
        private object _operationsQueueLock = new object();
        private object _connectionsLock = new object();

        public ThreadSafeList<WcxHeader> ArchiveFileList => _arcFileList;
        public int PluginCapabilities => _pluginCapabilities;
        public WcxModule WcxModule => _wcxModule;

        public WcxArchiveFileSource(IFileSource archiveFileSource, string archiveFileName, string wcxPluginFileName, int wcxPluginCapabilities) 
            : base(archiveFileSource, archiveFileName)
        {
            _moduleFileName = wcxPluginFileName;
            _pluginCapabilities = wcxPluginCapabilities;
            _arcFileList = new ThreadSafeList<WcxHeader>();
            _wcxModule = WcxPlugins.LoadModule(_moduleFileName);
            _connections = new List<FileSourceConnection>();
            _operationsQueue = new List<FileSourceOperation>();

            if (_wcxModule == null)
                throw new ModuleNotLoadedException($"Cannot load WCX module {_moduleFileName}");

            SetCryptCallback();

            if (File.Exists(archiveFileName))
            {
                if (!ReadArchive())
                    throw new WcxModuleException(_openResult);
            }

            CreateConnections();
        }

        public WcxArchiveFileSource(IFileSource archiveFileSource, string archiveFileName, WcxModule wcxPluginModule, int wcxPluginCapabilities, IntPtr archiveHandle) 
            : base(archiveFileSource, archiveFileName)
        {
            _pluginCapabilities = wcxPluginCapabilities;
            _arcFileList = new ThreadSafeList<WcxHeader>();
            _wcxModule = wcxPluginModule;
            _connections = new List<FileSourceConnection>();
            _operationsQueue = new List<FileSourceOperation>();

            SetCryptCallback();

            if (File.Exists(archiveFileName))
            {
                if (!ReadArchive(archiveHandle))
                    throw new WcxModuleException(_openResult);
            }

            CreateConnections();
        }

        public override void Dispose()
        {
            _arcFileList?.Dispose();
            base.Dispose();
        }

        public static FileEntry CreateFile(string path, WcxHeader header)
        {
            var file = new FileEntry(path)
            {
                Attributes = header.FileAttr,
                Size = header.IsDirectory ? 0 : header.UnpSize,
                CompressedSize = header.IsDirectory ? 0 : header.PackSize
            };

            try
            {
                file.ModificationTime = WcxFileTimeToDateTime(header.FileTime);
            }
            catch (Exception) { }

            // Set name after assigning Attributes property, because it is used to get extension
            file.Name = Path.GetFileName(header.FileName);

            return file;
        }

        public override FileSourceOperationTypes GetOperationTypes()
        {
            var result = FileSourceOperationTypes.List | FileSourceOperationTypes.CopyOut | 
                        FileSourceOperationTypes.TestArchive | FileSourceOperationTypes.Execute | 
                        FileSourceOperationTypes.CalcStatistics;

            if (((_pluginCapabilities & (int)PackerCaps.PK_CAPS_NEW) != 0 || (_pluginCapabilities & (int)PackerCaps.PK_CAPS_MODIFY) != 0) &&
                (_wcxModule.PackFiles != null || _wcxModule.PackFilesW != null))
                result |= FileSourceOperationTypes.CopyIn;

            if ((_pluginCapabilities & (int)PackerCaps.PK_CAPS_DELETE) != 0 &&
                (_wcxModule.DeleteFiles != null || _wcxModule.DeleteFilesW != null))
                result |= FileSourceOperationTypes.Delete;

            return result;
        }

        public override FileSourceProperties GetProperties()
        {
            return FileSourceProperties.UsesConnections | FileSourceProperties.ListFlatView;
        }

        protected override FilePropertiesTypes GetSupportedFileProperties()
        {
            return base.GetSupportedFileProperties();
        }

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            if (string.IsNullOrEmpty(newDir)) return false;
            if (newDir == GetRootDir()) return true;

            newDir = Path.GetFullPath(newDir + Path.DirectorySeparatorChar);

            lock (_arcFileList)
            {
                foreach (var header in _arcFileList)
                {
                    if (header.IsDirectory && header.FileName.Length > 0)
                    {
                        if (string.Equals(newDir, Path.GetFullPath(Path.Combine(GetRootDir(), header.FileName) + Path.DirectorySeparatorChar), 
                            StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            return false;
        }

        protected override string GetPacker()
        {
            return _wcxModule.ModuleName;
        }

        private void SetCryptCallback()
        {
            var flags = PasswordStore.MasterKeySet ? (int)CryptOpt.PK_CRYPTOPT_MASTERPASS_SET : 0;
            _wcxModule.SetCryptCallback(0, flags, CryptProcA, CryptProcW);
        }

        private bool ReadArchive(IntPtr archiveHandle = default)
        {
            IntPtr arcHandle = archiveHandle;
            bool result = false;

            try
            {
                if (arcHandle == IntPtr.Zero)
                {
                    arcHandle = _wcxModule.OpenArchiveHandle(ArchiveFileName, (int)OpenMode.PK_OM_LIST, out _openResult);
                    if (arcHandle == IntPtr.Zero || _openResult != 0)
                        return false;
                }

                _arcFileList.Clear();

                var header = new WcxHeader();
                while (_wcxModule.ReadWCXHeader(arcHandle, ref header) == 0)
                {
                    _arcFileList.Add(header.Clone());
                    header = new WcxHeader();
                }

                result = true;
            }
            finally
            {
                if (arcHandle != IntPtr.Zero && arcHandle != archiveHandle)
                    _wcxModule.CloseArchive(arcHandle);
            }

            return result;
        }

        private void CreateConnections()
        {
            // Create connections for different operation types
            AddConnection(CreateConnection()); // CopyIn
            AddConnection(CreateConnection()); // CopyOut
            AddConnection(CreateConnection()); // Delete
            AddConnection(CreateConnection()); // TestArchive
        }

        private FileSourceConnection CreateConnection()
        {
            return new WcxArchiveFileSourceConnection(_wcxModule);
        }

        private void AddConnection(FileSourceConnection connection)
        {
            lock (_connectionsLock)
            {
                _connections.Add(connection);
            }
        }

        private void RemoveConnection(FileSourceConnection connection)
        {
            lock (_connectionsLock)
            {
                _connections.Remove(connection);
            }
        }

        private FileSourceConnection FindConnectionByOperation(FileSourceOperation operation)
        {
            lock (_connectionsLock)
            {
                foreach (var connection in _connections)
                {
                    if (connection.Operation == operation)
                        return connection;
                }
            }
            return null;
        }

        private void AddToConnectionQueue(FileSourceOperation operation)
        {
            lock (_operationsQueueLock)
            {
                _operationsQueue.Add(operation);
            }
        }

        private void RemoveFromConnectionQueue(FileSourceOperation operation)
        {
            lock (_operationsQueueLock)
            {
                _operationsQueue.Remove(operation);
            }
        }

        private void NotifyNextWaitingOperation(FileSourceOperationTypes allowedOps)
        {
            lock (_operationsQueueLock)
            {
                foreach (var operation in _operationsQueue)
                {
                    if ((operation.OperationType & allowedOps) != 0)
                    {
                        operation.Start();
                        break;
                    }
                }
            }
        }

        private void ClearCurrentOperation(FileSourceOperation operation)
        {
            var connection = FindConnectionByOperation(operation);
            if (connection != null)
                connection.Operation = null;
        }

        protected override void OperationFinished(FileSourceOperation operation)
        {
            base.OperationFinished(operation);
            ClearCurrentOperation(operation);

            // Determine which operations can be performed based on the finished operation
            FileSourceOperationTypes allowedOps = FileSourceOperationTypes.None;

            switch (operation.OperationType)
            {
                case FileSourceOperationTypes.CopyIn:
                    allowedOps = FileSourceOperationTypes.CopyIn;
                    break;
                case FileSourceOperationTypes.CopyOut:
                    allowedOps = FileSourceOperationTypes.CopyOut;
                    break;
                case FileSourceOperationTypes.Delete:
                    allowedOps = FileSourceOperationTypes.Delete;
                    break;
                case FileSourceOperationTypes.TestArchive:
                    allowedOps = FileSourceOperationTypes.TestArchive;
                    break;
            }

            NotifyNextWaitingOperation(allowedOps);
        }

        protected override void DoReload(string[] pathsToReload)
        {
            ReadArchive();
        }

        public override FileSourceConnection GetConnection(FileSourceOperation operation)
        {
            FileSourceConnection result = null;

            // First check if the operation has a connection assigned
            result = FindConnectionByOperation(operation);
            if (result != null)
                return result;

            // Find appropriate connection for the operation type
            lock (_connectionsLock)
            {
                int connIndex = -1;

                switch (operation.OperationType)
                {
                    case FileSourceOperationTypes.CopyIn:
                        connIndex = 0; // connCopyIn
                        break;
                    case FileSourceOperationTypes.CopyOut:
                        connIndex = 1; // connCopyOut
                        break;
                    case FileSourceOperationTypes.Delete:
                        connIndex = 2; // connDelete
                        break;
                    case FileSourceOperationTypes.TestArchive:
                        connIndex = 3; // connTestArchive
                        break;
                }

                if (connIndex >= 0 && connIndex < _connections.Count)
                {
                    result = _connections[connIndex];
                    if (result.Operation == null)
                    {
                        result.Operation = operation;
                        return result;
                    }
                }
            }

            // If no connection available, queue the operation
            AddToConnectionQueue(operation);
            return null;
        }

        public override void RemoveOperationFromQueue(FileSourceOperation operation)
        {
            RemoveFromConnectionQueue(operation);
        }

        public override FileSourceOperation CreateListOperation(string targetPath)
        {
            return new WcxArchiveListOperation(this, targetPath);
        }

        public override FileSourceOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
        {
            return new WcxArchiveCopyInOperation(sourceFileSource, this, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
        {
            return new WcxArchiveCopyOutOperation(this, targetFileSource, sourceFiles, targetPath);
        }

        public override FileSourceOperation CreateDeleteOperation(FileEntries filesToDelete)
        {
            return new WcxArchiveDeleteOperation(this, filesToDelete);
        }

        public override FileSourceOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string verb)
        {
            return new WcxArchiveExecuteOperation(this, executableFile, basePath, verb);
        }

        public override FileSourceOperation CreateTestArchiveOperation(FileEntries sourceFiles)
        {
            return new WcxArchiveTestArchiveOperation(this, sourceFiles);
        }

        public override FileSourceOperation CreateCalcStatisticsOperation(FileEntries files)
        {
            return new WcxArchiveCalcStatisticsOperation(this, files);
        }

        public static IWcxArchiveFileSource CreateByArchiveSign(IFileSource archiveFileSource, string archiveFileName)
        {
            bool found = false;
            WcxModule wcxPlugin = null;
            WcxModule wcxPrevious = null;
            int pluginIndex = -1;
            IntPtr archiveHandle = IntPtr.Zero;
            int openResult = 0;

            // Check if there is a registered plugin for the archive file by content
            for (int i = 0; i < WcxPlugins.Count; i++)
            {
                if (WcxPlugins.Enabled[i])
                {
                    string moduleFileName = WcxPlugins.FileName[i];
                    wcxPlugin = WcxPlugins.LoadModule(moduleFileName);
                    
                    if (wcxPlugin != null)
                    {
                        if ((WcxPlugins.Flags[i] & (int)PackerCaps.PK_CAPS_BY_CONTENT) == (int)PackerCaps.PK_CAPS_BY_CONTENT)
                        {
                            if (wcxPlugin != wcxPrevious)
                            {
                                wcxPrevious = wcxPlugin;
                                if (wcxPlugin.WcxCanYouHandleThisFile(archiveFileName))
                                {
                                    archiveHandle = wcxPlugin.OpenArchiveHandle(archiveFileName, (int)OpenMode.PK_OM_LIST, out openResult);
                                    if (archiveHandle != IntPtr.Zero && openResult == 0)
                                    {
                                        found = true;
                                        pluginIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                        else if ((WcxPlugins.Flags[i] & (int)PackerCaps.PK_CAPS_HIDE) == (int)PackerCaps.PK_CAPS_HIDE)
                        {
                            if (MatchesMask(archiveFileName, "*" + Path.DirectorySeparatorChar + WcxPlugins.Ext[i]))
                            {
                                found = true;
                                pluginIndex = i;
                                break;
                            }
                        }
                    }
                }
            }

            if (found)
            {
                return new WcxArchiveFileSource(archiveFileSource, archiveFileName, wcxPlugin, WcxPlugins.Flags[pluginIndex], archiveHandle);
            }

            return null;
        }

        public static IWcxArchiveFileSource CreateByArchiveType(IFileSource archiveFileSource, string archiveFileName, string archiveType, bool includeHidden = false)
        {
            // Check if there is a registered plugin for the extension of the archive file name
            for (int i = 0; i < WcxPlugins.Count; i++)
            {
                if (WcxPlugins.Enabled[i] && string.Equals(archiveType, WcxPlugins.Ext[i], StringComparison.OrdinalIgnoreCase) &&
                    (includeHidden || (WcxPlugins.Flags[i] & (int)PackerCaps.PK_CAPS_HIDE) != (int)PackerCaps.PK_CAPS_HIDE))
                {
                    string moduleFileName = WcxPlugins.FileName[i];
                    return new WcxArchiveFileSource(archiveFileSource, archiveFileName, moduleFileName, WcxPlugins.Flags[i]);
                }
            }

            return null;
        }

        public static IWcxArchiveFileSource CreateByArchiveName(IFileSource archiveFileSource, string archiveFileName, bool includeHidden = false)
        {
            // Check if there is a registered plugin for the archive file name
            for (int i = 0; i < WcxPlugins.Count; i++)
            {
                string mask = "*" + Path.DirectorySeparatorChar + WcxPlugins.Ext[i];
                if (WcxPlugins.Enabled[i] && MatchesMask(archiveFileName, mask) &&
                    (includeHidden || (WcxPlugins.Flags[i] & (int)PackerCaps.PK_CAPS_HIDE) != (int)PackerCaps.PK_CAPS_HIDE))
                {
                    string moduleFileName = WcxPlugins.FileName[i];
                    return new WcxArchiveFileSource(archiveFileSource, archiveFileName, moduleFileName, WcxPlugins.Flags[i]);
                }
            }

            return null;
        }

        public static bool CheckPluginByName(string archiveFileName)
        {
            for (int i = 0; i < WcxPlugins.Count; i++)
            {
                string mask = "*" + Path.DirectorySeparatorChar + WcxPlugins.Ext[i];
                if (WcxPlugins.Enabled[i] && MatchesMask(archiveFileName, mask))
                    return true;
            }
            return false;
        }

        private static bool MatchesMask(string fileName, string mask)
        {
            // Simple implementation - in a real application, use a proper wildcard matching function
            return Path.GetExtension(fileName).Equals(Path.GetExtension(mask), StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime WcxFileTimeToDateTime(uint fileTime)
        {
            try
            {
                // Convert DOS time format to DateTime
                int year = (int)(((fileTime >> 25) & 0x7F) + 1980);
                int month = (int)((fileTime >> 21) & 0x0F);
                int day = (int)((fileTime >> 16) & 0x1F);
                int hour = (int)((fileTime >> 11) & 0x1F);
                int minute = (int)((fileTime >> 5) & 0x3F);
                int second = (int)((fileTime & 0x1F) * 2);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static int CryptProcA(int cryptoNumber, int mode, string archiveName, ref string password)
        {
            return CryptProc(cryptoNumber, mode, archiveName, ref password);
        }

        private static int CryptProcW(int cryptoNumber, int mode, string archiveName, ref string password)
        {
            return CryptProc(cryptoNumber, mode, archiveName, ref password);
        }

        private static int CryptProc(int cryptoNumber, int mode, string archiveName, ref string password)
        {
            const string prefix = "wcx";
            string group = Path.GetExtension(archiveName);
            bool result = true;

            switch (mode)
            {
                case (int)CryptMode.PK_CRYPT_SAVE_PASSWORD:
                    result = PasswordStore.WritePassword(prefix, group, archiveName, password);
                    break;

                case (int)CryptMode.PK_CRYPT_LOAD_PASSWORD:
                case (int)CryptMode.PK_CRYPT_LOAD_PASSWORD_NO_UI:
                    if (mode == (int)CryptMode.PK_CRYPT_LOAD_PASSWORD_NO_UI && !PasswordStore.HasMasterKey)
                        return (int)CryptResult.E_NO_FILES;
                    result = PasswordStore.ReadPassword(prefix, group, archiveName, out password);
                    break;

                case (int)CryptMode.PK_CRYPT_COPY_PASSWORD:
                case (int)CryptMode.PK_CRYPT_MOVE_PASSWORD:
                    string tempPassword;
                    result = PasswordStore.ReadPassword(prefix, group, archiveName, out tempPassword);
                    if (result)
                    {
                        result = PasswordStore.WritePassword(prefix, group, password, tempPassword);
                        if (result && mode == (int)CryptMode.PK_CRYPT_MOVE_PASSWORD)
                        {
                            result = PasswordStore.DeletePassword(prefix, group, archiveName);
                        }
                    }
                    break;

                case (int)CryptMode.PK_CRYPT_DELETE_PASSWORD:
                    result = PasswordStore.DeletePassword(prefix, group, archiveName);
                    break;
            }

            return result ? (int)CryptResult.E_SUCCESS : (int)CryptResult.E_EWRITE;
        }
    }

    public enum CryptMode
    {
        PK_CRYPT_SAVE_PASSWORD = 1,
        PK_CRYPT_LOAD_PASSWORD = 2,
        PK_CRYPT_LOAD_PASSWORD_NO_UI = 3,
        PK_CRYPT_COPY_PASSWORD = 4,
        PK_CRYPT_MOVE_PASSWORD = 5,
        PK_CRYPT_DELETE_PASSWORD = 6
    }

    public enum CryptResult
    {
        E_SUCCESS = 0,
        E_ECREATE = 1,
        E_EWRITE = 2,
        E_EREAD = 3,
        E_NO_FILES = 4
    }

    public enum CryptOpt
    {
        PK_CRYPTOPT_MASTERPASS_SET = 1
    }

    public enum OpenMode
    {
        PK_OM_LIST = 0,
        PK_OM_EXTRACT = 1
    }

    public enum PackerCaps
    {
        PK_CAPS_NEW = 1,
        PK_CAPS_MODIFY = 2,
        PK_CAPS_MULTIPLE = 4,
        PK_CAPS_DELETE = 8,
        PK_CAPS_OPTIONS = 16,
        PK_CAPS_MEMPACK = 32,
        PK_CAPS_BY_CONTENT = 64,
        PK_CAPS_SEARCHTEXT = 128,
        PK_CAPS_HIDE = 256
    }

    public class ModuleNotLoadedException : Exception
    {
        public ModuleNotLoadedException(string message) : base(message) { }
    }

    public class WcxModuleException : Exception
    {
        public WcxModuleException(int errorCode) : base($"WCX module error: {errorCode}") { }
    }
}