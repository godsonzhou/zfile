using Mono.Nat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace zfile
{
    public interface IWcxArchiveFileSource : IArchiveFileSource
    {
        ThreadSafeList<WcxHeader> ArchiveFileEntries { get; }
        int PluginCapabilities { get; }
        WcxModule WcxModule { get; }
    }
    public class WcxArchiveFileSource : ArchiveFileSource, IWcxArchiveFileSource
    {
        private string _moduleFileName;
        private int _pluginCapabilities;
        private ThreadSafeList<WcxHeader> _arcFileEntries;
        private WcxModule _wcxModule;
        private int _openResult;
        private List<FileSourceConnection> _connections;
        private List<FileSourceOperation> _operationsQueue;
        private object _operationsQueueLock = new object();
        private object _connectionsLock = new object();

        public ThreadSafeList<WcxHeader> ArchiveFileEntries => _arcFileEntries;
        public int PluginCapabilities => _pluginCapabilities;
        public WcxModule WcxModule => _wcxModule;

        public WcxArchiveFileSource(IFileSource archiveFileSource, string archiveFileName, string wcxPluginFileName, int wcxPluginCapabilities)
            : base(archiveFileSource, archiveFileName)
        {
            _moduleFileName = wcxPluginFileName;
            _pluginCapabilities = wcxPluginCapabilities;
            _arcFileEntries = new ThreadSafeList<WcxHeader>();
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
            _arcFileEntries = new ThreadSafeList<WcxHeader>();
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

        protected override void Dispose(bool disposing)
        {
            //_arcFileEntries?.Dispose();
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
                file.ModificationTime = WcxModuleExtensions.FileTimeToDateTime(header.FileTime);
            }
            catch (Exception) { }

            // Set name after assigning Attributes property, because it is used to get extension
            file.Name = Path.GetFileName(header.FileName);

            return file;
        }

        public FileSourceOperationType GetOperationTypes()
        {
            var result = FileSourceOperationType.List | FileSourceOperationType.CopyOut |
                        FileSourceOperationType.TestArchive | FileSourceOperationType.Execute |
                        FileSourceOperationType.CalcStatistics;

            if (((_pluginCapabilities & (int)PackerCaps.PK_CAPS_NEW) != 0 || (_pluginCapabilities & (int)PackerCaps.PK_CAPS_MODIFY) != 0) &&
                (_wcxModule._packFiles != null || _wcxModule._packFilesW != null))
                result |= FileSourceOperationType.CopyIn;

            if ((_pluginCapabilities & (int)PackerCaps.PK_CAPS_DELETE) != 0 &&
                (_wcxModule._deleteFiles != null || _wcxModule._deleteFilesW != null))
                result |= FileSourceOperationType.Delete;

            return result;
        }

        public FileSourceProperties GetProperties()
        {
            return FileSourceProperties.UsersConnections | FileSourceProperties.ListFlatView;
        }

        protected FilePropertyType GetSupportedFileProperties()
        {
            return base.SupportedFileProperties;
        }

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            if (string.IsNullOrEmpty(newDir)) return false;
            if (newDir == GetRootDir()) return true;

            newDir = Path.GetFullPath(newDir + Path.DirectorySeparatorChar);

            lock (_arcFileEntries)
            {
                foreach (var header in _arcFileEntries)
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

        protected string GetPacker()
        {
            return _wcxModule.Name;
        }

        private void SetCryptCallback()
        {
            var flags = PasswordStore.HasMasterKey ? (int)CryptOpt.PK_CRYPTOPT_MASTERPASS_SET : 0;

            // Use the extension method to set the callback with the appropriate delegates
            // The extension method expects cryptoNr, flags, and delegate functions with non-ref parameters
            _wcxModule.SetCryptCallback(0, flags,
                (cryptoNumber, mode, archiveName, password) =>
                {
                    string pwd = password;
                    int result = CryptProcA(cryptoNumber, mode, archiveName, ref pwd);
                    return result;
                },
                (cryptoNumber, mode, archiveName, password) =>
                {
                    string pwd = password;
                    int result = CryptProcW(cryptoNumber, mode, archiveName, ref pwd);
                    return result;
                });
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

                _arcFileEntries.Clear();

                var header = new WcxHeader();
                while (_wcxModule.ReadWCXHeader(arcHandle, ref header) == 0)
                {
                    _arcFileEntries.Add(header.Clone());
					_wcxModule.ProcessFile(arcHandle, ProcessMode.PK_SKIP, "", "");
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
                    if (connection.AssignedOperation == operation)
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

        private void NotifyNextWaitingOperation(FileSourceOperationType allowedOps)
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
                connection.AssignedOperation = null;
        }

        public override void OperationFinished(FileSourceOperation operation)
        {
            base.OperationFinished(operation);
            ClearCurrentOperation(operation);

            // Determine which operations can be performed based on the finished operation
            FileSourceOperationType allowedOps = FileSourceOperationType.None;

            switch (operation.OperationType)
            {
                case FileSourceOperationType.CopyIn:
                    allowedOps = FileSourceOperationType.CopyIn;
                    break;
                case FileSourceOperationType.CopyOut:
                    allowedOps = FileSourceOperationType.CopyOut;
                    break;
                case FileSourceOperationType.Delete:
                    allowedOps = FileSourceOperationType.Delete;
                    break;
                case FileSourceOperationType.TestArchive:
                    allowedOps = FileSourceOperationType.TestArchive;
                    break;
            }

            NotifyNextWaitingOperation(allowedOps);
        }

        public override void DoReload(string[] pathsToReload)
        {
            ReadArchive();
        }

        public new FileSourceConnection GetConnection(FileSourceOperation operation)
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
                    case FileSourceOperationType.CopyIn:
                        connIndex = 0; // connCopyIn
                        break;
                    case FileSourceOperationType.CopyOut:
                        connIndex = 1; // connCopyOut
                        break;
                    case FileSourceOperationType.Delete:
                        connIndex = 2; // connDelete
                        break;
                    case FileSourceOperationType.TestArchive:
                        connIndex = 3; // connTestArchive
                        break;
                }

                if (connIndex >= 0 && connIndex < _connections.Count)
                {
                    result = _connections[connIndex];
                    if (result.AssignedOperation == null)
                    {
                        result.AssignedOperation = operation;
                        return result;
                    }
                }
            }

            // If no connection available, queue the operation
            AddToConnectionQueue(operation);
            return null;
        }

        public void RemoveOperationFromQueue(FileSourceOperation operation)
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
                                if (wcxPlugin.CanYouHandleThisFile(archiveFileName))
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
                            if (MatchesMask(archiveFileName, "*." + WcxPlugins.Ext[i]))
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
                string mask = "*." + WcxPlugins.Ext[i];
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
                string mask = "*." + WcxPlugins.Ext[i];
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

        private static int CryptProc(int _, int mode, string archiveName, ref string password)
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

    public class PasswordStore
    {
        // Simple implementation of a password store
        private static readonly Dictionary<string, string> _passwords = new Dictionary<string, string>();

        /// <summary>
        /// Deletes a password from the store
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="group">The group</param>
        /// <param name="archiveName">The archive name</param>
        /// <returns>True if the password was deleted, false otherwise</returns>
        public static bool DeletePassword(string prefix, string group, string archiveName)
        {
            string key = GetKey(prefix, group, archiveName);
            return _passwords.Remove(key);
        }

        /// <summary>
        /// Writes a password to the store
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="group">The group</param>
        /// <param name="archiveName">The archive name</param>
        /// <param name="password">The password</param>
        /// <returns>True if the password was written, false otherwise</returns>
        public static bool WritePassword(string prefix, string group, string archiveName, string password)
        {
            string key = GetKey(prefix, group, archiveName);
            _passwords[key] = password;
            return true;
        }

        /// <summary>
        /// Reads a password from the store
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="group">The group</param>
        /// <param name="archiveName">The archive name</param>
        /// <param name="password">The password</param>
        /// <returns>True if the password was read, false otherwise</returns>
        public static bool ReadPassword(string prefix, string group, string archiveName, out string password)
        {
            string key = GetKey(prefix, group, archiveName);
            if (_passwords.TryGetValue(key, out string? value))
            {
                password = value ?? string.Empty;
                return true;
            }

            password = string.Empty;
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether a master key is available
        /// </summary>
        public static bool HasMasterKey => true;

        /// <summary>
        /// Gets a key for the password store
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="group">The group</param>
        /// <param name="archiveName">The archive name</param>
        /// <returns>The key</returns>
        private static string GetKey(string prefix, string group, string archiveName)
        {
            return $"{prefix}:{group}:{archiveName}";
        }

    }

    public class ModuleNotLoadedException(string message) : Exception(message)
    {
    }

    public class WcxModuleException(int errorCode) : Exception($"WCX module error: {errorCode}")
    {
        public WcxModuleErrorCode ErrorCode { get; } = (WcxModuleErrorCode)errorCode;
    }
    public enum WcxModuleErrorCode
    {
        Handled
    }
}