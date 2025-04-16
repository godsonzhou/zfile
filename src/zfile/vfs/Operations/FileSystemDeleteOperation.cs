namespace zfile
{
    public class FileSystemDeleteOperation : FileSourceDeleteOperation
    {
        private FileEntries fullFilesTreeToDelete;  // 源文件，包括所有子目录中的文件/目录
        private FileSourceDeleteOperationStatistics statistics; // 统计信息的本地副本
        private Description description;

        // 选项
        private FileSourceOperationOptionSymLink symLinkOption;
        private bool skipErrors;
        private bool recycle;
        private FileSourceOperationOptionGeneral deleteReadOnly;
        private FileSourceOperationOptionGeneral deleteDirectly;

        public FileSystemDeleteOperation(
            IFileSource targetFileSource,
            FileEntries filesToDelete)
            : base(targetFileSource, filesToDelete)
        {
            symLinkOption = FileSourceOperationOptionSymLink.None;
            skipErrors = GlobalSettings.SkipFileOpError;
            recycle = false;
            deleteReadOnly = FileSourceOperationOptionGeneral.None;
            deleteDirectly = FileSourceOperationOptionGeneral.None;

            if (GlobalSettings.ProcessComments)
                description = new Description(true);
        }

        protected override void Initialize()
        {
            // 获取初始化的统计信息；然后我们只更改需要的内容
            statistics = RetrieveStatistics;

            if (recycle)
            {
                fullFilesTreeToDelete = FilesToDelete;
                statistics.TotalFiles = fullFilesTreeToDelete.Count;
            }
            else
            {
                FillAndCount(FilesToDelete, true, false,
                    out fullFilesTreeToDelete,
                    out statistics.TotalFiles,
                    out statistics.TotalBytes);     // 获取文件的完整列表（递归）
            }

            if (GlobalSettings.ProcessComments)
                description.Clear();

#if MSWINDOWS
            if (ElevateAction == DuplicateAction.Ignore)
                ElevateAction = DuplicateAction.Error;
#endif
        }

        protected override void MainExecute()
        {
            ProcessList(fullFilesTreeToDelete);
        }

        protected override void Finalize()
        {
            // 清理操作，当前不需要额外处理
        }

        private void DeleteSubDirectory(FileEntry file)
        {
            var rootFiles = new FileEntries { file };
            FileEntries subFiles;
            long filesCount, bytesCount;

            // 只为子文件统计，因为根目录的统计已经完成
            FillAndCount(rootFiles, true, true, out subFiles, out filesCount, out bytesCount);

            statistics.TotalFiles += filesCount;
            statistics.TotalBytes += bytesCount;

            // 现在插入根目录
            subFiles.Insert(0, file);

            // 只有在删除到回收站失败时才会调用此函数
            // 所以我们可以假设Recycle为True。暂时关闭，因为我们删除这个子目录
            recycle = false;

            ProcessList(subFiles);
            recycle = true;
        }

        private void ProcessFile(FileEntry file)
        {
            var fileName = file.FullPath;
            bool retry;
            int lastError;
            var removeDirectly = FileSourceOperationOptionGeneral.None;
            string message, question;
            LogOption logOptions;
            bool deleteResult;

            if (file.IsReadOnly)
            {
                switch (deleteReadOnly)
                {
                    case FileSourceOperationOptionGeneral.None:
                        var response = AskQuestion(
                            string.Format(Resources.MsgFileReadOnly, WrapTextSimple(fileName)),
                            string.Empty,
                            new[] {
                                FileSourceOperationUIResponse.Yes,
                                FileSourceOperationUIResponse.Skip,
                                FileSourceOperationUIResponse.Abort,
                                FileSourceOperationUIResponse.All,
                                FileSourceOperationUIResponse.SkipAll
                            },
                            FileSourceOperationUIResponse.Yes,
                            FileSourceOperationUIResponse.Abort);

                        switch (response)
                        {
                            case FileSourceOperationUIResponse.All:
                                deleteReadOnly = FileSourceOperationOptionGeneral.Yes;
                                break;
                            case FileSourceOperationUIResponse.Skip:
                                return;
                            case FileSourceOperationUIResponse.SkipAll:
                                deleteReadOnly = FileSourceOperationOptionGeneral.No;
                                return;
                            case FileSourceOperationUIResponse.Abort:
                                RaiseAbortOperation();
                                break;
                        }
                        break;
                    case FileSourceOperationOptionGeneral.No:
                        return;
                }
            }

            do
            {
                retry = false;

                if (!recycle)
                {
                    if (file.IsReadOnly)
                        File.SetAttributes(fileName, FileAttributes.Normal);

                    if (file.IsDirectory)
                    {
                        deleteResult = RemoveDirectoryUAC(fileName);
                    }
                    else
                    {
                        deleteResult = DeleteFileUAC(fileName);
                    }
                }
                else
                {
                    // 删除到回收站（文件和文件夹使用同一个函数）
                    deleteResult = FileTrashUtf8(fileName);
                    if (!deleteResult)
                    {
                        deleteResult = !File.Exists(fileName);
                    }
                    if (!deleteResult)
                    {
                        switch (deleteDirectly)
                        {
                            case FileSourceOperationOptionGeneral.None:
                                var response = AskQuestion(
                                    string.Format(Resources.MsgDelToTrashForce, WrapTextSimple(fileName)),
                                    string.Empty,
                                    new[] {
                                        FileSourceOperationUIResponse.Yes,
                                        FileSourceOperationUIResponse.All,
                                        FileSourceOperationUIResponse.Skip,
                                        FileSourceOperationUIResponse.SkipAll,
                                        FileSourceOperationUIResponse.Abort
                                    },
                                    FileSourceOperationUIResponse.Yes,
                                    FileSourceOperationUIResponse.Abort);

                                switch (response)
                                {
                                    case FileSourceOperationUIResponse.Yes:
                                        removeDirectly = FileSourceOperationOptionGeneral.Yes;
                                        break;
                                    case FileSourceOperationUIResponse.All:
                                        deleteDirectly = FileSourceOperationOptionGeneral.Yes;
                                        removeDirectly = FileSourceOperationOptionGeneral.Yes;
                                        break;
                                    case FileSourceOperationUIResponse.Skip:
                                        removeDirectly = FileSourceOperationOptionGeneral.No;
                                        break;
                                    case FileSourceOperationUIResponse.SkipAll:
                                        deleteDirectly = FileSourceOperationOptionGeneral.No;
                                        removeDirectly = FileSourceOperationOptionGeneral.No;
                                        break;
                                    case FileSourceOperationUIResponse.Abort:
                                        RaiseAbortOperation();
                                        break;
                                }
                                break;
                            case FileSourceOperationOptionGeneral.Yes:
                                removeDirectly = FileSourceOperationOptionGeneral.Yes;
                                break;
                            case FileSourceOperationOptionGeneral.No:
                                removeDirectly = FileSourceOperationOptionGeneral.No;
                                break;
                        }

                        if (removeDirectly == FileSourceOperationOptionGeneral.Yes)
                        {
                            if (file.IsLink && file.IsDirectory)
                            {
                                deleteResult = RemoveDirectoryUAC(fileName);
                            }
                            else if (file.IsDirectory)
                            {
                                DeleteSubDirectory(file);
                                return;
                            }
                            else
                            {
                                deleteResult = DeleteFileUAC(fileName);
                            }
                        }
                    }
                }

                if (deleteResult)
                {
                    // 处理注释（如果需要）
                    if (GlobalSettings.ProcessComments)
                    {
                        description.DeleteDescription(fileName);
                        if (string.Equals(file.Name, "DESCRIPT.ION", StringComparison.OrdinalIgnoreCase))
                            description.Reset();
                    }

                    if (file.IsDirectory)
                    {
                        LogMessage(
                            string.Format(Resources.MsgLogSuccess + Resources.MsgLogRmDir, fileName),
                            LogOption.DirectoryOperations | LogOption.Delete,
                            LogOption.Success);
                    }
                    else
                    {
                        LogMessage(
                            string.Format(Resources.MsgLogSuccess + Resources.MsgLogDelete, fileName),
                            LogOption.Delete,
                            LogOption.Success);
                    }
                }
                else
                {
                    if (file.IsDirectory)
                    {
                        logOptions = LogOption.DirectoryOperations | LogOption.Delete;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogRmDir, fileName);
                        question = string.Format(Resources.MsgCannotDeleteDirectory, fileName);
                    }
                    else
                    {
                        logOptions = LogOption.Delete;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogDelete, fileName);
                        question = string.Format(Resources.MsgNotDelete, fileName);
                    }

                    if (skipErrors || removeDirectly == FileSourceOperationOptionGeneral.No)
                    {
                        LogMessage(message, logOptions, LogOption.Error);
                    }
                    else
                    {
                        if (!recycle || removeDirectly == FileSourceOperationOptionGeneral.Yes)
                        {
                            lastError = Marshal.GetLastWin32Error();
#if MSWINDOWS
                            ProcessInfo[] processInfo;
                            if (GetFileInUseProcessFast(fileName, out processInfo))
                            {
                                question += Environment.NewLine + Environment.NewLine + Resources.MsgOpenInAnotherProgram + Environment.NewLine;
                                question += Environment.NewLine + string.Format(Resources.MsgProcessId, processInfo[0].ProcessId) + Environment.NewLine;
                                if (!string.IsNullOrEmpty(processInfo[0].ApplicationName))
                                {
                                    question += string.Format(Resources.MsgApplicationName, processInfo[0].ApplicationName) + Environment.NewLine;
                                }
                                if (!string.IsNullOrEmpty(processInfo[0].ExecutablePath))
                                {
                                    question += string.Format(Resources.MsgExecutablePath, processInfo[0].ExecutablePath) + Environment.NewLine;
                                }
                            }
                            else
#endif
                            question += Environment.NewLine + GetLastErrorMessage(lastError);
                        }

#if MSWINDOWS
                        FileSourceOperationUIResponse[] possibleResponses;
                        if (ElevateAction != DuplicateAction.Accept && ElevationRequired(lastError))
                        {
                            possibleResponses = new FileSourceOperationUIResponse[] {
                                FileSourceOperationUIResponse.Retry,
                                FileSourceOperationUIResponse.Skip,
                                FileSourceOperationUIResponse.SkipAll,
                                FileSourceOperationUIResponse.Abort,
                                FileSourceOperationUIResponse.RetryAdmin
                            };
                        }
                        else
#endif
                        {
                            possibleResponses = new FileSourceOperationUIResponse[] {
                                FileSourceOperationUIResponse.Retry,
                                FileSourceOperationUIResponse.Skip,
                                FileSourceOperationUIResponse.SkipAll,
                                FileSourceOperationUIResponse.Abort
                            };
                        }

#if MSWINDOWS
                        if (processInfo?.Length > 0 || lastError == Win32Error.ERROR_ACCESS_DENIED || lastError == Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            Array.Resize(ref possibleResponses, possibleResponses.Length + 1);
                            possibleResponses[possibleResponses.Length - 1] = FileSourceOperationUIResponse.Unlock;
                        }
#endif

                        var response = AskQuestion(question, string.Empty, possibleResponses,
                            FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Abort);

                        switch (response)
                        {
                            case FileSourceOperationUIResponse.Retry:
                                retry = true;
                                break;
                            case FileSourceOperationUIResponse.SkipAll:
                                skipErrors = true;
                                break;
                            case FileSourceOperationUIResponse.Abort:
                                RaiseAbortOperation();
                                break;
#if MSWINDOWS
                            case FileSourceOperationUIResponse.RetryAdmin:
                                retry = true;
                                ElevateAction = DuplicateAction.Accept;
                                break;
                            case FileSourceOperationUIResponse.Unlock:
                                retry = true;
                                GetFileInUseProcessSlow(fileName, lastError, out processInfo);
                                ShowUnlockForm(processInfo);
                                break;
#endif
                        }
                    }
                }
            } while (retry);
        }

        private void ProcessList(FileEntries files)
        {
            for (int i = files.Count - 1; i >= 0; i--)
            {
                var file = files[i];

                statistics.CurrentFile = file.FullPath;
                UpdateStatistics(statistics);

                ProcessFile(file);

                statistics.DoneFiles++;
                statistics.DoneBytes += file.Size;
                UpdateStatistics(statistics);

                AppProcessMessages();
                CheckOperationState();
            }
        }

        private FileSourceOperationUIResponse ShowError(string message)
        {
            if (skipErrors)
            {
                Log.Write(Thread, message, LogOption.Error, true);
                return FileSourceOperationUIResponse.Skip;
            }
            else
            {
                var response = AskQuestion(message, string.Empty,
                    new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Cancel },
                    FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Cancel);
                if (response == FileSourceOperationUIResponse.Cancel)
                    RaiseAbortOperation();
                return response;
            }
        }

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            switch (logMsgType)
            {
                case LogOption.Error:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Error)) return;
                    break;
                case LogOption.Info:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
                    break;
                case LogOption.Success:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;
                    break;
            }

            if ((logOptions & GlobalSettings.LogOptions) == logOptions)
            {
                Log.Write(Thread, message, logMsgType);
            }
        }

        // 属性
        public bool Recycle
        {
            get { return recycle; }
            set { recycle = value; }
        }

        public FileSourceOperationOptionGeneral DeleteReadOnly
        {
            get { return deleteReadOnly; }
            set { deleteReadOnly = value; }
        }

        public FileSourceOperationOptionSymLink SymLinkOption
        {
            get { return symLinkOption; }
            set { symLinkOption = value; }
        }

        public bool SkipErrors
        {
            get { return skipErrors; }
            set { skipErrors = value; }
        }
    }
} 