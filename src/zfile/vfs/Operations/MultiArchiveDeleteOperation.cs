using System.Diagnostics;

namespace zfile
{
    public class MultiArchiveDeleteOperation : FileSourceDeleteOperation
    {
        private readonly IMultiArchiveFileSource _fileSource;
        private FileSourceDeleteOperationStatistics _statistics;
        private FileEntries _fullFilesTreeToDelete;
        private string _password;
        private Process _exProcess;
        private string _tempFile;
        private int _errorLevel;
        private string _commandLine;
        private FileEntries SourceFiles;

        public MultiArchiveDeleteOperation(IFileSource fileSource, FileEntries filesToDelete)
            : base(fileSource, filesToDelete)
        {
            _fileSource = fileSource as IMultiArchiveFileSource;
            _password = _fileSource.Password;
            _fullFilesTreeToDelete = null;

            // 获取初始化的统计信息；然后我们只更改需要的内容
            _statistics = RetrieveStatistics();
            _statistics.DoneFiles = -1;
            _statistics.DoneBytes = -1;
            UpdateStatistics(_statistics);
        }

        ~MultiArchiveDeleteOperation()
        {
            _fullFilesTreeToDelete = null;
        }

        protected override void Initialize()
        {
            _exProcess = new Process();
            _exProcess.StartInfo.RedirectStandardOutput = true;
            _exProcess.StartInfo.UseShellExecute = false;
            _exProcess.StartInfo.CreateNoWindow = true;
            _exProcess.OutputDataReceived += OnReadLn;
            _tempFile = Path.GetTempFileName();

            if (!string.IsNullOrEmpty(_fileSource.MultiArcItem.PasswordQuery))
            {
                _exProcess.StartInfo.Arguments = _fileSource.MultiArcItem.PasswordQuery;
                _exProcess.StartInfo.RedirectStandardInput = true;
            }

            AddStateChangedListener(new[] { FileSourceOperationState.Starting, FileSourceOperationState.Pausing, FileSourceOperationState.Stopping }, (operation, state) => { if (operation != null) FileSourceOperationStateChangedNotify((IFileSourceOperation)operation, state); });

            if (SourceFiles.Count == 1)
            {
                _statistics.CurrentFile = SourceFiles[0].FullPath;
            }
            else
            {
                _statistics.CurrentFile = SourceFiles[0].Path + "*.*";
            }
            _statistics.CurrentFile = _fileSource.ArchiveFileName;

            _fileSource.FillAndCount("*", SourceFiles, true, out _fullFilesTreeToDelete, out _statistics.TotalFiles, out _statistics.TotalBytes);

            _commandLine = _fileSource.MultiArcItem.Delete;
            _errorLevel = ExtractErrorLevel(_commandLine);
        }

        protected override void MainExecute()
        {
            var multiArcItem = _fileSource.MultiArcItem;
            string destPath = string.Empty;
            string rootPath = _fullFilesTreeToDelete[0].Path;
            ChangeFileEntriesRoot(string.Empty, _fullFilesTreeToDelete);

            if (_commandLine.Contains("%F"))
            {
                // 逐个文件删除
                for (int i = _fullFilesTreeToDelete.Count - 1; i >= 0; i--)
                {
                    var file = _fullFilesTreeToDelete[i];
                    UpdateProgress(rootPath + file.FullPath, destPath, 0);

                    string readyCommand = FormatArchiverCommand(
                        multiArcItem.Archiver,
                        _commandLine,
                        _fileSource.ArchiveFileName,
                        null,
                        file.FullPath,
                        destPath,
                        _tempFile,
                        _password,
                        string.Empty,
                        string.Empty);

                    LogCommand(readyCommand);

                    // 设置归档器当前路径为文件列表根目录
                    _exProcess.StartInfo.WorkingDirectory = rootPath;
                    _exProcess.StartInfo.FileName = readyCommand;
                    _exProcess.Start();
                    _exProcess.BeginOutputReadLine();
                    _exProcess.WaitForExit();

                    if (!CheckForErrors(file.FullPath, _exProcess.ExitCode))
                        break;
                }
            }
        }

        private void OnReadLn(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                LogMessage(e.Data, LogOption.None, LogOption.Info);
            }
        }

        private void OnQueryString(string str)
        {
            // 实现密码查询处理
        }

        private void UpdateProgress(string sourceName, string targetName, long incSize)
        {
            _statistics.CurrentFile = sourceName;
            _statistics.DoneBytes += incSize;
            UpdateStatistics(_statistics);
        }

        private void FileSourceOperationStateChangedNotify(IFileSourceOperation operation, FileSourceOperationState state)
        {
            if (state == FileSourceOperationState.Stopping)
            {
                if (_exProcess != null && !_exProcess.HasExited)
                {
                    _exProcess.Kill();
                }
            }
        }

        private void ShowError(string message, LogOption logOptions = LogOption.None)
        {
            LogMessage(message, logOptions, LogOption.Error);
        }

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            // 实现日志记录
        }

        private void LogCommand(string command)
        {
            LogMessage(command, LogOption.None, LogOption.Info);
        }

        private bool CheckForErrors(string fileName, int exitStatus)
        {
            if (exitStatus > _errorLevel)
            {
                ShowError($"处理文件 {fileName} 时出错，退出代码: {exitStatus}");
                return false;
            }
            return true;
        }

        public string Password { get; set; }
    }
}