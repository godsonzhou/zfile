using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        public ThreadSafeList<WcxHeader> ArchiveFileList => _arcFileList;
        public int PluginCapabilities => _pluginCapabilities;
        public WcxModule WcxModule => _wcxModule;

        public WcxArchiveFileSource(IFileSource archiveFileSource, string archiveFileName, string wcxPluginFileName, int wcxPluginCapabilities) : base(archiveFileSource, archiveFileName)
        {
            _moduleFileName = wcxPluginFileName;
            _pluginCapabilities = wcxPluginCapabilities;
            _arcFileList = new ThreadSafeList<WcxHeader>();
            _wcxModule = WcxPlugins.LoadModule(_moduleFileName);

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

        public WcxArchiveFileSource(IFileSource archiveFileSource, string archiveFileName, WcxModule wcxPluginModule, int wcxPluginCapabilities, IntPtr archiveHandle) : base(archiveFileSource, archiveFileName)
        {
            _pluginCapabilities = wcxPluginCapabilities;
            _arcFileList = new ThreadSafeList<WcxHeader>();
            _wcxModule = wcxPluginModule;

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
            var file = new FileEntry
            {
                Name = Path.GetFileName(header.FileName),
                Attributes = header.FileAttr,
                Size = header.IsDirectory ? 0 : header.UnpSize,
                CompressedSize = header.IsDirectory ? 0 : header.PackSize
            };

            try
            {
                file.ModificationTime = WcxFileTimeToDateTime(header.FileTime);
            }
            catch (Exception) { }

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

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            if (string.IsNullOrEmpty(newDir)) return false;
            if (newDir == GetRootDir()) return true;

            newDir = Path.GetFullPath(newDir);

            lock (_arcFileList)
            {
                foreach (var header in _arcFileList)
                {
                    if (header.IsDirectory && header.FileName.Length > 0)
                    {
                        if (string.Equals(newDir, Path.GetFullPath(Path.Combine(GetRootDir(), header.FileName)), StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            return false;
        }

        private void SetCryptCallback()
        {
            var flags = PasswordStore.MasterKeySet ? PK_CRYPTOPT_MASTERPASS_SET : 0;
            _wcxModule.SetCryptCallback(0, flags, CryptProcA, CryptProcW);
        }

        private bool ReadArchive(IntPtr archiveHandle = default)
        {
            // Implementation for reading archive content
            return true;
        }

        private void CreateConnections()
        {
            // Implementation for creating connections
        }

        private static DateTime WcxFileTimeToDateTime(uint fileTime)
        {
            // Implementation for converting WCX file time to DateTime
            return DateTime.FromFileTime(fileTime);
        }
    }
}