using System.Text;
using Zfile.FileSources;
using ZFile.FileSources.WcxArchive;

namespace Zfile.Operations;

    public class WcxArchiveDeleteOperation : FileSourceDeleteOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;
        private FileSourceDeleteOperationStatistics _statistics;

        // WCX interface cannot discern different operations (for reporting progress),
        // so this static variable is used to store currently running operation.
        private static WcxArchiveDeleteOperation _wcxDeleteOperation = null;

        public WcxArchiveDeleteOperation(IFileSource targetFileSource, Files filesToDelete) 
            : base(targetFileSource, filesToDelete)
        {
            _wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
        }

        public override void Initialize()
        {
            if (_wcxDeleteOperation != null && _wcxDeleteOperation != this)
                throw new Exception("Another WCX delete operation is already running");

            _wcxDeleteOperation = this;

            // Get initialized statistics; then we change only what is needed.
            _statistics = RetrieveStatistics();

            CountFiles(FilesToDelete, "*.*");
        }

        public override void MainExecute()
        {
            var wcxModule = _wcxArchiveFileSource.WcxModule;

            wcxModule.WcxSetChangeVolProc(WcxModule.WcxInvalidHandle);
            wcxModule.WcxSetProcessDataProc(WcxModule.WcxInvalidHandle, ProcessDataProcA, ProcessDataProcW);

            int result = wcxModule.WcxDeleteFiles(_wcxArchiveFileSource.ArchiveFileName,
                                               GetFileList(FilesToDelete));

            // Check for errors.
            if (result != WcxModule.E_SUCCESS)
            {
                // User aborted operation.
                if (result == WcxModule.E_EABORTED) return;

                ShowError(string.Format("Error deleting from {0}: {1}",
                           _wcxArchiveFileSource.ArchiveFileName,
                           WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
            }
            else
            {
                LogMessage(string.Format("Successfully deleted from {0}",
                           _wcxArchiveFileSource.ArchiveFileName), LogOption.ArcOp, LogMsgType.Success);
            }
        }

        public override void Finalize()
        {
            ClearCurrentOperation();
        }

        private void ShowError(string message, int error, LogOption logOptions = LogOption.None)
        {
            LogMessage(message, logOptions, LogMsgType.Error);

            if (!GlobalSettings.SkipFileOpError && error > WcxModule.E_SUCCESS)
            {
                if (AskQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                               FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
                {
                    RaiseAbortOperation();
                }
            }
        }

        private void LogMessage(string message, LogOption logOptions, LogMsgType logMsgType)
        {
            switch (logMsgType)
            {
                case LogMsgType.Error:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Errors)) return;
                    break;
                case LogMsgType.Info:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
                    break;
                case LogMsgType.Success:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;
                    break;
            }

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(Thread, message, logMsgType);
            }
        }

        private void CountFiles(Files files, string fileMask)
        {
            var arcFileList = _wcxArchiveFileSource.ArchiveFileList.LockList();
            try
            {
                foreach (var item in arcFileList)
                {
                    var header = (WcxHeader)item;

                    // Check if the file from the archive fits the selection given via theFiles.
                    if (!FileAttributes.IsDirectory(header.FileAttr) &&           // Omit directories
                        MatchesFileList(files, header.FileName) &&    // Check if it's included in the filelist
                        (fileMask == "*.*" || fileMask == "*" ||    // And name matches file mask
                         MatchesMaskList(Path.GetFileName(header.FileName), fileMask)))
                    {
                        _statistics.TotalBytes += header.UnpSize;
                        _statistics.TotalFiles++;
                    }
                }
            }
            finally
            {
                _wcxArchiveFileSource.ArchiveFileList.UnlockList();
            }

            UpdateStatistics(_statistics);
        }

        private string GetFileList(Files files)
        {
            string result = "";

            foreach (var file in files)
            {
                // Filenames must be relative to archive root and shouldn't start with path delimiter.
                string fileName = ExcludeFrontPathDelimiter(file.FullPath);

                // Special treatment of directories.
                if (file.IsDirectory)
                    // TC ends paths to directories to be deleted with '\*.*'
                    // (which means delete this directory and all files in it).
                    fileName = IncludeTrailingPathDelimiter(fileName) + "*.*";

                result += fileName + "\0";
            }

            result += "\0";
            return result;
        }

        public static void ClearCurrentOperation()
        {
            _wcxDeleteOperation = null;
        }

        // WCX callback methods
        private static int ProcessDataProc(string fileName, int size)
        {
            // Implementation of process data callback
            int result = 1;

            if (_wcxDeleteOperation != null)
            {
                if (_wcxDeleteOperation.State == FileSourceOperationState.Stopping)  // Cancel operation
                    return 0;

                var statistics = _wcxDeleteOperation._statistics;
                statistics.CurrentFile = fileName;

                // Get the number of bytes processed since the previous call
                if (size > 0)
                {
                    statistics.TotalFiles = 100;
                    statistics.DoneBytes += size;
                    statistics.DoneFiles = statistics.DoneBytes * 100 / statistics.TotalBytes;
                }
                // Get progress percent value to directly set progress bar
                else if (size < 0)
                {
                    // Total operation percent
                    if (size >= -100 && size <= -1)
                    {
                        statistics.TotalFiles = 100;
                        statistics.DoneFiles = -size;
                    }
                }

                _wcxDeleteOperation.UpdateStatistics(statistics);
                if (!_wcxDeleteOperation.CheckOperationStateSafe()) return 0;
            }

            return result;
        }

        private static int ProcessDataProcA(IntPtr fileName, int size)
        {
            return ProcessDataProc(System.Runtime.InteropServices.Marshal.PtrToStringAnsi(fileName), size);
        }

        private static int ProcessDataProcW(IntPtr fileName, int size)
        {
            return ProcessDataProc(System.Runtime.InteropServices.Marshal.PtrToStringUni(fileName), size);
        }
    }