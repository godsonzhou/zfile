using System.Diagnostics;

namespace zfile
{
    public class MultiArchiveCopyOutOperation : ArchiveCopyOutOperation
    {
        private readonly IMultiArchiveFileSource _fileSource;
        private FileSourceCopyOperationStatistics _statistics;
        private FileInfo[] _fullFilesTreeToExtract;
        private string _password;
        private bool _extractWithoutPath;
        private FileInfo _currentFile;
        private string _currentTargetFilePath;
        private Process _exProcess;
        private string _tempFile;
        private int _errorLevel;

        public MultiArchiveCopyOutOperation(IFileSource sourceFileSource, IFileSource targetFileSource, FileInfo[] sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _fileSource = sourceFileSource as IMultiArchiveFileSource;
            _password = _fileSource.Password;
            _fullFilesTreeToExtract = null;
            FileExistsOption = FileSourceOperationOptionFileExists.None;
            _extractWithoutPath = false;

            // 获取初始化的统计信息；然后我们只更改需要的内容
            _statistics = RetrieveStatistics();
            _statistics.DoneFiles = -1;
            _statistics.CurrentFileDoneBytes = -1;
            UpdateStatistics(_statistics);
        }

        ~MultiArchiveCopyOutOperation()
        {
            _fullFilesTreeToExtract = null;
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

            if (_fileSource.MultiArcItem.ExtractFlags.HasFlag(ExtractFlags.SmartExtract))
            {
                int count = 0;
                var arcFileList = _fileSource.ArchiveFileList.Clone();
                try
                {
                    for (int i = 0; i < arcFileList.Count; i++)
                    {
                        string fileName = Path.DirectorySeparatorChar + arcFileList[i].FileName;
                        if (IsInPath(Path.DirectorySeparatorChar.ToString(), fileName, false, false))
                        {
                            count++;
                            if (count > 1)
                            {
                                TargetPath = Path.Combine(TargetPath, Path.GetFileNameWithoutExtension(_fileSource.ArchiveFileName));
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    arcFileList.Clear();
                }
            }

            AddStateChangedListener(new[] { FileSourceOperationState.Starting, FileSourceOperationState.Pausing, FileSourceOperationState.Stopping }, FileSourceOperationStateChangedNotify);

            if (string.IsNullOrEmpty(ExtractMask))
                ExtractMask = "*";

            _fileSource.FillAndCount(ExtractMask, SourceFiles, true, out _fullFilesTreeToExtract, out _statistics.TotalFiles, out _statistics.TotalBytes);
        }

        public override void MainExecute()
        {
            string targetFileName;
            string sourcePath;
            string tempDir;
            var createdPaths = new Dictionary<string, FileAttributes>();
            FileInfo[] filesToExtract = null;

            try
            {
                for (int i = 0; i < _fullFilesTreeToExtract.Length; i++)
                {
                    var file = _fullFilesTreeToExtract[i];
                    _currentFile = file;
                    sourcePath = file.Path;
                    targetFileName = Path.Combine(TargetPath, file.Name);

                    if (!_extractWithoutPath)
                    {
                        string relativePath = sourcePath.Substring(1); // 移除前导分隔符
                        targetFileName = Path.Combine(TargetPath, relativePath, file.Name);
                    }

                    _currentTargetFilePath = targetFileName;

                    if (file.IsDirectory)
                    {
                        if (!Directory.Exists(targetFileName))
                        {
                            Directory.CreateDirectory(targetFileName);
                            createdPaths[targetFileName] = file.Attributes;
                        }
                        continue;
                    }

                    var fileExistsOption = DoFileExists(file, targetFileName);
                    if (fileExistsOption == FileSourceOperationOptionFileExists.Skip)
                        continue;

                    if (fileExistsOption == FileSourceOperationOptionFileExists.Overwrite)
                    {
                        if (File.Exists(targetFileName))
                            File.Delete(targetFileName);
                    }

                    string readyCommand = FormatArchiverCommand(
                        _fileSource.MultiArcItem.Archiver,
                        _fileSource.MultiArcItem.Extract,
                        _fileSource.ArchiveFileName,
                        file.FullPath,
                        targetFileName,
                        _tempFile,
                        _password,
                        string.Empty,
                        string.Empty);

                    OnReadLn(readyCommand);

                    _exProcess.StartInfo.WorkingDirectory = TargetPath;
                    _exProcess.StartInfo.FileName = readyCommand;
                    _exProcess.Start();
                    _exProcess.BeginOutputReadLine();
                    _exProcess.WaitForExit();

                    CheckForErrors(file.FullPath, targetFileName, _exProcess.ExitCode);

                    if (File.Exists(targetFileName))
                    {
                        File.SetAttributes(targetFileName, file.Attributes);
                        File.SetLastWriteTime(targetFileName, file.ModificationTime);
                    }

                    UpdateProgress(file.FullPath, targetFileName, file.Size);
                }
            }
            finally
            {
                SetDirsAttributes(createdPaths);
            }
        }

        private void CreateDirs(FileInfo[] files, string destPath, string currentArchiveDir, Dictionary<string, FileAttributes> createdPaths)
        {
            foreach (var file in files)
            {
                if (file.IsDirectory)
                {
                    string targetDir = Path.Combine(destPath, file.Name);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                        createdPaths[targetDir] = file.Attributes;
                    }
                }
            }
        }

        private bool SetDirsAttributes(Dictionary<string, FileAttributes> paths)
        {
            bool result = true;
            foreach (var path in paths)
            {
                try
                {
                    Directory.SetAttributes(path.Key, path.Value);
                }
                catch (Exception)
                {
                    result = false;
                }
            }
            return result;
        }

        private FileSourceOperationOptionFileExists DoFileExists(FileInfo file, string absoluteTargetFileName)
        {
            if (!File.Exists(absoluteTargetFileName))
                return FileSourceOperationOptionFileExists.None;

            switch (FileExistsOption)
            {
                case FileSourceOperationOptionFileExists.None:
                    var action = AskQuestion(
                        "文件已存在",
                        $"文件 {file.Name} 已存在。是否覆盖？",
                        new[] { FileSourceOperationUIAction.Yes, FileSourceOperationUIAction.No, FileSourceOperationUIAction.YesToAll, FileSourceOperationUIAction.NoToAll },
                        FileSourceOperationUIAction.Yes,
                        FileSourceOperationUIAction.No);

                    switch (action)
                    {
                        case FileSourceOperationUIAction.Yes:
                            return FileSourceOperationOptionFileExists.Overwrite;
                        case FileSourceOperationUIAction.No:
                            return FileSourceOperationOptionFileExists.Skip;
                        case FileSourceOperationUIAction.YesToAll:
                            FileExistsOption = FileSourceOperationOptionFileExists.Overwrite;
                            return FileSourceOperationOptionFileExists.Overwrite;
                        case FileSourceOperationUIAction.NoToAll:
                            FileExistsOption = FileSourceOperationOptionFileExists.Skip;
                            return FileSourceOperationOptionFileExists.Skip;
                    }
                    break;
                case FileSourceOperationOptionFileExists.Overwrite:
                    return FileSourceOperationOptionFileExists.Overwrite;
                case FileSourceOperationOptionFileExists.Skip:
                    return FileSourceOperationOptionFileExists.Skip;
            }

            return FileSourceOperationOptionFileExists.None;
        }

        private void ShowError(string message, LogOptions logOptions = LogOptions.None)
        {
            LogMessage(message, logOptions, LogMsgType.Error);
        }

        private void LogMessage(string message, LogOptions logOptions, LogMsgType logMsgType)
        {
            // 实现日志记录
        }

        private void CheckForErrors(string sourceName, string targetName, int exitStatus)
        {
            if (exitStatus > _errorLevel)
            {
                ShowError($"处理文件 {sourceName} -> {targetName} 时出错，退出代码: {exitStatus}");
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

        private void QuestionActionHandler(FileSourceOperationUIAction action)
        {
            // 实现问题处理
        }

        public string Password { get; set; }
        public bool ExtractWithoutPath { get; set; }
    }
} 