using System.Diagnostics;

namespace zfile
{
    public enum DuplicateAction
    {
        None = 0,
        Error = 1
    }

    public class MultiArchiveCopyInOperation : ArchiveCopyInOperation
    {
        private readonly IMultiArchiveFileSource _fileSource;
        private FileEntries _removeFilesTree;
        private string _password;
        private string _volumeSize;
        private string _customParams;
        private bool _callResult;
        private Process _exProcess;
        private string _tempFile;
        private int _errorLevel;
        private string _commandLine;
        public int PackingFlags { get; set; }
        public string Password { get; set; }
        public string VolumeSize { get; set; }
        public string CustomParams { get; set; }
        public bool TarBefore { get; set; }
        public FileSourceCopyOperationStatistics Statistics;
        private DuplicateAction ElevateAction;

        public MultiArchiveCopyInOperation(IFileSource sourceFileSource, IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _fileSource = targetFileSource as IMultiArchiveFileSource;
            _password = _fileSource.Password;
            _removeFilesTree = null;
            PackingFlags = 0;
            _volumeSize = string.Empty;
            TarBefore = false;

            // 获取初始化的统计信息；然后我们只更改需要的内容
            Statistics = RetrieveStatistics();
            Statistics.DoneFiles = -1;
            Statistics.CurrentFileDoneBytes = -1;
            UpdateStatistics(Statistics);
        }

        ~MultiArchiveCopyInOperation()
        {
            _removeFilesTree = null;
        }

        protected override void Initialize()
        {
            if (Path.GetExtension(_fileSource.ArchiveFileName) == _fileSource.GetSfxExt() &&
                !string.IsNullOrEmpty(_fileSource.MultiArcItem.AddSelfExtract))
            {
                _commandLine = _fileSource.MultiArcItem.AddSelfExtract;
            }
            else
            {
                _commandLine = _fileSource.MultiArcItem.Add;
            }

            if (TargetPath != Path.DirectorySeparatorChar.ToString() && !_commandLine.Contains("%R"))
            {
                MessageBox.Show("不支持的操作", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RaiseAbortOperation();
            }

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

            AddStateChangedListener(new[] { FileSourceOperationState.Starting, FileSourceOperationState.Pausing, FileSourceOperationState.Stopping }, (sender, state) => FileSourceOperationStateChangedNotify((IFileSourceOperation)sender, state));

            if (SourceFiles.Count == 1)
            {
                Statistics.CurrentFileFrom = SourceFiles[0].FullPath;
            }
            else
            {
                Statistics.CurrentFileFrom = SourceFiles[0].Path + "*.*";
            }
            Statistics.CurrentFileTo = _fileSource.ArchiveFileName;

            ElevateAction = DuplicateAction.Error;

            FileSystemUtil.FillAndCount(SourceFiles, false, false, out _removeFilesTree, out Statistics.TotalFiles, out Statistics.TotalBytes);
        }

        protected override void MainExecute()
        {
            // 如果需要，先打包成TAR
            if (TarBefore)
                Tar();

            var multiArcItem = _fileSource.MultiArcItem;
            string destPath = TargetPath.TrimStart(Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            string rootPath = _removeFilesTree[0].Path;
            ChangeFileEntriesRoot(string.Empty, _removeFilesTree);

            // 获取最大可接受的命令错误级别
            _errorLevel = ExtractErrorLevel(_commandLine);
            if (_commandLine.Contains("%F"))
            {
                // 逐个文件打包
                for (int i = _removeFilesTree.Count - 1; i >= 0; i--)
                {
                    var file = _removeFilesTree[i];
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
                        _volumeSize,
                        _customParams);

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

        private bool Tar()
        {
            // 实现TAR打包逻辑
            return true;
        }

        private void OnReadLn(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                LogMessage(e.Data, LogOption.None, LogOption.Info);
            }
        }

        private void OperationProgressHandler()
        {
            CheckOperationState();
        }

        private void OnQueryString(string str)
        {
            // 实现密码查询处理
        }

        private void UpdateProgress(string sourceName, string targetName, long incSize)
        {
            Statistics.CurrentFileFrom = sourceName;
            Statistics.CurrentFileTo = targetName;
            Statistics.CurrentFileDoneBytes += incSize;
            UpdateStatistics(Statistics);
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

        private void DeleteFile(string basePath, FileEntry file)
        {
            // 实现文件删除
        }

        private void DeleteFiles(string basePath, FileEntry[] files)
        {
            // 实现文件批量删除
        }


    }
}