using System;
using System.IO;
using System.Threading;

namespace FileSystemOperations
{
    public class FileSystemOperationHelper : IDisposable
    {
        private readonly Action<string, out bool> _askQuestion;
        private readonly Action _raiseAbortOperation;
        private readonly Action _appProcessMessages;
        private readonly Action _checkOperationState;
        private readonly Action<FileSourceCopyOperationStatistics> _updateStatistics;
        private readonly Action<string, string> _showCompareFilesUI;
        private readonly Thread _thread;
        private readonly FileSourceOperationHelperMode _mode;
        private readonly string _targetPath;
        private readonly FileSourceCopyOperationStatistics _statistics;

        public bool Verify { get; set; }
        public string RenameMask { get; set; }
        public FileSourceOperationOptionGeneral CopyOnWrite { get; set; }
        public bool ReserveSpace { get; set; }
        public bool CheckFreeSpace { get; set; }
        public CopyAttributesOption CopyAttributesOptions { get; set; }
        public bool SkipAllBigFiles { get; set; }
        public bool AutoRenameItself { get; set; }
        public bool CorrectSymLinks { get; set; }
        public FileSourceOperationOptionGeneral FileExistsOption { get; set; }
        public FileSourceOperationOptionGeneral DirExistsOption { get; set; }
        public FileSourceOperationOptionSetPropertyError SetPropertyError { get; set; }

        public FileSystemOperationHelper(
            Action<string, out bool> askQuestion,
            Action raiseAbortOperation,
            Action appProcessMessages,
            Action checkOperationState,
            Action<FileSourceCopyOperationStatistics> updateStatistics,
            Action<string, string> showCompareFilesUI,
            Thread thread,
            FileSourceOperationHelperMode mode,
            string targetPath,
            FileSourceCopyOperationStatistics statistics)
        {
            _askQuestion = askQuestion;
            _raiseAbortOperation = raiseAbortOperation;
            _appProcessMessages = appProcessMessages;
            _checkOperationState = checkOperationState;
            _updateStatistics = updateStatistics;
            _showCompareFilesUI = showCompareFilesUI;
            _thread = thread;
            _mode = mode;
            _targetPath = targetPath;
            _statistics = statistics;
        }

        public void Initialize()
        {
            // Initialize any necessary resources
        }

        public void ProcessTree(FileTree tree)
        {
            if (tree == null)
                return;

            ProcessFiles(tree.Files);
            foreach (var subNode in tree.SubNodes)
            {
                _checkOperationState();
                ProcessTree(subNode);
            }
        }

        private void ProcessFiles(System.Collections.Generic.IReadOnlyList<FileInfo> files)
        {
            foreach (var file in files)
            {
                _checkOperationState();
                ProcessFile(file);
            }
        }

        private void ProcessFile(FileInfo file)
        {
            try
            {
                string targetFilePath = Path.Combine(_targetPath, file.Name);
                bool fileExists = File.Exists(targetFilePath);

                if (fileExists)
                {
                    switch (FileExistsOption)
                    {
                        case FileSourceOperationOptionGeneral.No:
                            return;
                        case FileSourceOperationOptionGeneral.AskUser:
                            bool overwrite;
                            _askQuestion($"File {targetFilePath} already exists. Overwrite?", out overwrite);
                            if (!overwrite)
                                return;
                            break;
                    }
                }

                if (CheckFreeSpace)
                {
                    long requiredSpace = file.Length;
                    if (Verify)
                        requiredSpace *= 2;
                    if (ReserveSpace)
                        requiredSpace *= 2;

                    DriveInfo drive = new DriveInfo(Path.GetPathRoot(targetFilePath));
                    if (drive.AvailableFreeSpace < requiredSpace)
                    {
                        if (SkipAllBigFiles)
                            return;

                        bool skip;
                        _askQuestion($"Not enough free space on drive {drive.Name}. Skip file?", out skip);
                        if (skip)
                            return;
                    }
                }

                File.Copy(file.FullName, targetFilePath, true);

                if (CopyAttributesOptions != CopyAttributesOption.None)
                {
                    try
                    {
                        if ((CopyAttributesOptions & CopyAttributesOption.CopyAttributes) != 0)
                            File.SetAttributes(targetFilePath, file.Attributes);
                        if ((CopyAttributesOptions & CopyAttributesOption.CopyTime) != 0)
                        {
                            File.SetCreationTime(targetFilePath, file.CreationTime);
                            File.SetLastWriteTime(targetFilePath, file.LastWriteTime);
                            File.SetLastAccessTime(targetFilePath, file.LastAccessTime);
                        }
                    }
                    catch (Exception)
                    {
                        if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Abort)
                            _raiseAbortOperation();
                        else if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Skip)
                            return;
                    }
                }

                if (Verify)
                {
                    _showCompareFilesUI(file.FullName, targetFilePath);
                }

                _statistics.DoneFiles++;
                _statistics.DoneBytes += file.Length;
                _updateStatistics(_statistics);
            }
            catch (Exception)
            {
                _statistics.FailedFiles++;
                _statistics.FailedBytes += file.Length;
                _updateStatistics(_statistics);
            }
        }

        public void Dispose()
        {
            // Clean up any resources
        }
    }
} 