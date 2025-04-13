using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Zfile.FileSources;
using ZFile.FileSources.WcxArchive;
using ZFile.Operations;

namespace ZFile.Operations.WcxArchive
{
    public class WcxArchiveCopyOutOperation : FileSourceOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;
        private FileSourceCopyOperationStatistics _statistics;
        private bool _renamingFiles;
        private string _renameNameMask;
        private string _renameExtMask;
        private bool _extractWithoutPath;
        private string _targetPath;
        private List<FileSystemInfo> _sourceFiles;

        public WcxArchiveCopyOutOperation(IFileSource sourceFileSource, IFileSource targetFileSource, List<FileSystemInfo> sourceFiles, string targetPath)
            : base(sourceFileSource)
        {
            _wcxArchiveFileSource = sourceFileSource as IWcxArchiveFileSource;
            _sourceFiles = sourceFiles;
            _targetPath = targetPath;
            _extractWithoutPath = false;
        }

        public override void Initialize()
        {
            _extractWithoutPath = _sourceFiles.Count == 1;
            _renamingFiles = !string.IsNullOrEmpty(RenameMask) && RenameMask != "*.*";
            if (_renamingFiles)
                SplitFileMask(RenameMask, out _renameNameMask, out _renameExtMask);
            _statistics = new FileSourceCopyOperationStatistics();
        }

        public override async Task ExecuteAsync()
        {
            var arcHandle = _wcxArchiveFileSource.WcxModule.OpenArchiveHandle(_wcxArchiveFileSource.ArchiveFileName, OpenMode.Extract);
            if (arcHandle == IntPtr.Zero)
                throw new OperationAbortedException("Failed to open archive");

            try
            {
                var createdPaths = new Dictionary<string, WcxHeader>();
                CreateDirectoriesAndCountFiles(_sourceFiles, null, _targetPath, _sourceFiles[0].FullName, createdPaths);
                SetProcessDataProc(arcHandle);
                _wcxArchiveFileSource.WcxModule.SetChangeVolProc(arcHandle);

                WcxHeader header;
                while ((header = _wcxArchiveFileSource.WcxModule.ReadHeader(arcHandle)) != null)
                {
                    CheckOperationState();

                    if (!header.IsDirectory && MatchesFileList(_sourceFiles, header.FileName))
                    {
                        var targetFileName = _extractWithoutPath ? 
                            Path.GetFileName(header.FileName) : 
                            Path.Combine(_targetPath, header.FileName);

                        if (_renamingFiles)
                            targetFileName = Path.Combine(
                                Path.GetDirectoryName(targetFileName),
                                ApplyRenameMask(Path.GetFileName(targetFileName), _renameNameMask, _renameExtMask));

                        _statistics.CurrentFileFrom = header.FileName;
                        _statistics.CurrentFileTo = targetFileName;
                        _statistics.CurrentFileTotalBytes = header.UnpackedSize;
                        _statistics.CurrentFileDoneBytes = 0;
                        UpdateStatistics(_statistics);

                        var result = _wcxArchiveFileSource.WcxModule.ProcessFile(
                            arcHandle,
                            ProcessMode.Extract,
                            string.Empty,
                            targetFileName);

                        if (result != OperationResult.Success)
                        {
                            if (result == OperationResult.Aborted)
                                throw new OperationAbortedException();

                            LogError($"Error extracting {header.FileName} to {targetFileName}", result);
                        }
                        else
                        {
                            LogSuccess($"Successfully extracted {header.FileName} to {targetFileName}");
                        }

                        _statistics.DoneFiles++;
                        UpdateStatistics(_statistics);
                    }
                    else
                    {
                        _wcxArchiveFileSource.WcxModule.ProcessFile(
                            arcHandle,
                            ProcessMode.Skip,
                            string.Empty,
                            string.Empty);
                    }
                }

                if (!_extractWithoutPath)
                    SetDirectoryAttributes(createdPaths);
            }
            finally
            {
                _wcxArchiveFileSource.WcxModule.CloseArchive(arcHandle);
            }
        }

        private void SetProcessDataProc(IntPtr arcHandle)
        {
            _wcxArchiveFileSource.WcxModule.SetProcessDataProc(arcHandle, (fileName, size) =>
            {
                if (State == OperationState.Stopping)
                    return 0;

                if (size > 0)
                {
                    _statistics.CurrentFileDoneBytes += size;
                    if (_statistics.CurrentFileDoneBytes > _statistics.CurrentFileTotalBytes)
                        _statistics.CurrentFileDoneBytes = _statistics.CurrentFileTotalBytes;
                    _statistics.DoneBytes += size;
                }
                else if (size < 0)
                {
                    if (size >= -100 && size <= -1)
                    {
                        if (_statistics.TotalBytes == 0)
                            _statistics.TotalBytes = 100;
                        _statistics.DoneBytes = _statistics.TotalBytes * (-size) / 100;
                    }
                    else if (size >= -1100 && size <= -1000)
                    {
                        if (_statistics.CurrentFileTotalBytes == 0)
                            _statistics.CurrentFileTotalBytes = 100;
                        _statistics.CurrentFileDoneBytes = _statistics.CurrentFileTotalBytes * ((-size) - 1000) / 100;
                    }
                }

                UpdateStatistics(_statistics);
                return ProcessMessages() ? 1 : 0;
            });
        }

        public string RenameMask { get; set; }
    }
}