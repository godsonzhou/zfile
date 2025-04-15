using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace FileSystemOperations
{
    public class MultiArchiveDeleteOperation : FileSourceDeleteOperation
    {
        private readonly IMultiArchiveFileSource _fileSource;
        private FileSourceDeleteOperationStatistics _statistics;
        private FileInfo[] _fullFilesTreeToDelete;
        private string _password;
        private Process _exProcess;
        private string _tempFile;
        private int _errorLevel;
        private string _commandLine;

        public MultiArchiveDeleteOperation(IFileSource fileSource, FileInfo[] filesToDelete)
            : base(fileSource, filesToDelete)
        {
            _fileSource = fileSource as IMultiArchiveFileSource;
            _password = _fileSource.Password;
            _fullFilesTreeToDelete = null;

            // 获取初始化的统计信息；然后我们只更改需要的内容
            _statistics = RetrieveStatistics();
            _statistics.DoneFiles = -1;
            _statistics.CurrentFileDoneBytes = -1;
            UpdateStatistics(_statistics);
        }

        ~MultiArchiveDeleteOperation()
        {
            _fullFilesTreeToDelete = null;
        }

        public override void Initialize()
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

            AddStateChangedListener(new[] { FileSourceOperationState.Starting, FileSourceOperationState.Pausing, FileSourceOperationState.Stopping }, FileSourceOperationStateChangedNotify);

            if (SourceFiles.Length == 1)
            {
                _statistics.CurrentFileFrom = SourceFiles[0].FullPath;
            }
            else
            {
                _statistics.CurrentFileFrom = SourceFiles[0].Path + "*.*";
            }
            _statistics.CurrentFileTo = _fileSource.ArchiveFileName;

            _fileSource.FillAndCount("*", SourceFiles, true, out _fullFilesTreeToDelete, out _statistics.TotalFiles, out _statistics.TotalBytes);

            _commandLine = _fileSource.MultiArcItem.Delete;
            _errorLevel = ExtractErrorLevel(_commandLine);
        }

        public override void MainExecute()
        {
            var multiArcItem = _fileSource.MultiArcItem;
            string destPath = string.Empty;
            string rootPath = _fullFilesTreeToDelete[0].Path;
            ChangeFileListRoot(string.Empty, _fullFilesTreeToDelete);

            if (_commandLine.Contains("%F"))
            {
                // 逐个文件删除
                for (int i = _fullFilesTreeToDelete.Length - 1; i >= 0; i--)
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

                    OnReadLn(readyCommand);

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
                LogMessage(e.Data, LogOptions.None, LogMsgType.Info);
            }
        }

        private void OnQueryString(string str)
        {
            // 实现密码查询处理
        }

        private void UpdateProgress(string sourceName, string targetName, long incSize)
        {
            _statistics.CurrentFileFrom = sourceName;
            _statistics.CurrentFileTo = targetName;
            _statistics.CurrentFileDoneBytes += incSize;
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

        private void ShowError(string message, LogOptions logOptions = LogOptions.None)
        {
            LogMessage(message, logOptions, LogMsgType.Error);
        }

        private void LogMessage(string message, LogOptions logOptions, LogMsgType logMsgType)
        {
            // 实现日志记录
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