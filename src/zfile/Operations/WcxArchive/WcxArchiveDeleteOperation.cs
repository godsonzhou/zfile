using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZFile.FileSources;
using ZFile.FileSources.WcxArchive;

namespace ZFile.Operations.WcxArchive
{
    public class WcxArchiveDeleteOperation : FileSourceDeleteOperation
    {
        private IWcxArchiveFileSource wcxArchiveFileSource;
        private FileSourceDeleteOperationStatistics statistics;
        private static WcxArchiveDeleteOperation currentOperation;

        public WcxArchiveDeleteOperation(IFileSource targetFileSource, List<FileSystemInfo> filesToDelete)
            : base(targetFileSource, filesToDelete)
        {
            wcxArchiveFileSource = targetFileSource as IWcxArchiveFileSource;
        }

        public override void Initialize()
        {
            if (currentOperation != null && currentOperation != this)
                throw new Exception("Another WCX delete operation is already running");

            currentOperation = this;
            statistics = RetrieveStatistics();
            CountFiles(FilesToDelete, "*.*");
        }

        public override void MainExecute()
        {
            var wcxModule = wcxArchiveFileSource.WcxModule;
            wcxModule.SetProcessDataProc(ProcessDataProc);

            int result = wcxModule.DeleteFiles(wcxArchiveFileSource.ArchiveFileName, GetFileList(FilesToDelete));

            if (result != 0)
            {
                if (result == -1) return;
                ShowError($"Error deleting files from {wcxArchiveFileSource.ArchiveFileName} - {GetErrorMessage(result)}", result);
            }
            else
            {
                LogMessage($"Successfully deleted files from {wcxArchiveFileSource.ArchiveFileName}", LogMessageType.Success);
            }
        }

        public override void Finalize()
        {
            ClearCurrentOperation();
        }

        private int ProcessDataProc(string fileName, long size)
        {
            if (State == OperationState.Stopping)
                return 0;

            statistics.CurrentFile = fileName;

            if (size > 0)
            {
                statistics.TotalFiles = 100;
                statistics.DoneBytes += size;
                statistics.DoneFiles = (int)(statistics.DoneBytes * 100 / statistics.TotalBytes);
            }
            else if (size < 0 && size >= -100)
            {
                statistics.TotalFiles = 100;
                statistics.DoneFiles = (int)-size;
            }

            UpdateStatistics(statistics);
            return CheckOperationState() ? 1 : 0;
        }

        private void CountFiles(List<FileSystemInfo> files, string fileMask)
        {
            var archiveFiles = wcxArchiveFileSource.ArchiveFileList;
            lock (archiveFiles)
            {
                foreach (var header in archiveFiles)
                {
                    if (!header.IsDirectory && 
                        MatchesFileList(files, header.FileName) && 
                        (fileMask == "*.*" || fileMask == "*" || MatchesMask(Path.GetFileName(header.FileName), fileMask)))
                    {
                        statistics.TotalBytes += header.UnpackedSize;
                        statistics.TotalFiles++;
                    }
                }
            }

            UpdateStatistics(statistics);
        }

        private string GetFileList(List<FileSystemInfo> files)
        {
            var result = new StringBuilder();
            foreach (var file in files)
            {
                string fileName = file.FullName.TrimStart(Path.DirectorySeparatorChar);
                if (file is DirectoryInfo)
                    fileName = Path.Combine(fileName, "*.*");
                result.Append(fileName).Append('\0');
            }
            result.Append('\0');
            return result.ToString();
        }

        private void ShowError(string message, int error)
        {
            LogMessage(message, LogMessageType.Error);
            if (!SkipFileOperationError && error > 0)
            {
                if (AskQuestion(message, "", new[] { "Skip", "Abort" }) == "Abort")
                    RaiseAbortOperation();
            }
        }

        private void LogMessage(string message, LogMessageType messageType)
        {
            if (!ShouldLog(messageType)) return;
            Logger.Log(message, messageType);
        }

        private static void ClearCurrentOperation()
        {
            currentOperation = null;
        }
    }
}