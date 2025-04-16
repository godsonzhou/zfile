namespace zfile
{
    public class WfxPluginDeleteOperation : FileSourceDeleteOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private FileList _fullFilesTreeToDelete;
        private FileSourceDeleteOperationStatistics _statistics;
        private FileSourceOperationSymlinkOption _symLinkOption;
        private bool _skipErrors;
        private FileSourceOperationOptionGeneral _deleteReadOnly;

        public WfxPluginDeleteOperation(IFileSource targetFileSource, ref FileList filesToDelete)
            : base(targetFileSource, ref filesToDelete)
        {
            _symLinkOption = FileSourceOperationSymlinkOption.None;
            _skipErrors = false;
            _deleteReadOnly = FileSourceOperationOptionGeneral.None;
            _fullFilesTreeToDelete = null;
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
        }

        public override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(FilesToDelete.Path, FsStatus.Start, FsStatusOperation.Delete);
            _statistics = RetrieveStatistics;
            _wfxPluginFileSource.FillAndCount(FilesToDelete, true, false, ref _fullFilesTreeToDelete, ref _statistics.TotalFiles, ref _statistics.TotalBytes);
        }

        public override void MainExecute()
        {
            for (int currentFileIndex = _fullFilesTreeToDelete.Count - 1; currentFileIndex >= 0; currentFileIndex--)
            {
                var file = _fullFilesTreeToDelete[currentFileIndex];
                _statistics.CurrentFile = file.Path + file.Name;
                UpdateStatistics(_statistics);

                ProcessFile(file);

                _statistics.DoneFiles++;
                _statistics.DoneBytes += file.Size;
                UpdateStatistics(_statistics);

                Application.DoEvents();
                CheckOperationState();
            }
        }

        public override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(FilesToDelete.Path, FsStatus.End, FsStatusOperation.Delete);
        }

        private bool ProcessFile(FileInfo file)
        {
            var fileName = file.Path + file.Name;
            var retry = false;

            if (FileIsReadOnly(file.Attributes))
            {
                switch (_deleteReadOnly)
                {
                    case FileSourceOperationOptionGeneral.None:
                        switch (AskQuestion(string.Format(Resources.MsgFileReadOnly, fileName), string.Empty,
                            new[] { FileSourceOperationUIResult.Yes, FileSourceOperationUIResult.All, FileSourceOperationUIResult.Skip, FileSourceOperationUIResult.SkipAll },
                            FileSourceOperationUIResult.Yes, FileSourceOperationUIResult.Skip))
                        {
                            case FileSourceOperationUIResult.All:
                                _deleteReadOnly = FileSourceOperationOptionGeneral.Yes;
                                break;
                            case FileSourceOperationUIResult.Skip:
                                return false;
                            case FileSourceOperationUIResult.SkipAll:
                                _deleteReadOnly = FileSourceOperationOptionGeneral.No;
                                return false;
                        }
                        break;
                    case FileSourceOperationOptionGeneral.No:
                        return false;
                }
            }

            do
            {
                retry = false;
                bool result;

                if (file.IsDirectory)
                {
                    result = _wfxPluginFileSource.WfxModule.WfxRemoveDir(fileName);
                }
                else
                {
                    result = _wfxPluginFileSource.WfxModule.WfxDeleteFile(fileName);
                }

                if (result)
                {
                    if (file.IsDirectory)
                    {
                        LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogRmDir, fileName), LogOptions.VfsOp, LogMessageType.Success);
                    }
                    else
                    {
                        LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogDelete, fileName), LogOptions.VfsOp, LogMessageType.Success);
                    }
                    return true;
                }
                else
                {
                    string message;
                    string question;
                    LogOptions logOptions;

                    if (file.IsDirectory)
                    {
                        logOptions = LogOptions.VfsOp;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogRmDir, fileName);
                        question = string.Format(Resources.MsgNotDelete, fileName);
                    }
                    else
                    {
                        logOptions = LogOptions.VfsOp;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogDelete, fileName);
                        question = string.Format(Resources.MsgNotDelete, fileName);
                    }

                    if (GlobalOptions.SkipFileOpError || _skipErrors)
                    {
                        LogMessage(message, logOptions, LogMessageType.Error);
                    }
                    else
                    {
                        switch (AskQuestion(question, string.Empty,
                            new[] { FileSourceOperationUIResult.Retry, FileSourceOperationUIResult.Skip, FileSourceOperationUIResult.SkipAll, FileSourceOperationUIResult.Abort },
                            FileSourceOperationUIResult.Retry, FileSourceOperationUIResult.Skip))
                        {
                            case FileSourceOperationUIResult.Retry:
                                retry = true;
                                break;
                            case FileSourceOperationUIResult.SkipAll:
                                _skipErrors = true;
                                break;
                            case FileSourceOperationUIResult.Abort:
                                RaiseAbortOperation();
                                break;
                        }
                    }
                }
            } while (retry);

            return false;
        }

        private FileSourceOperationUIResult ShowError(string message)
        {
            if (GlobalOptions.SkipFileOpError)
            {
                Log.Write(Thread, message, LogMessageType.Error, true);
                return FileSourceOperationUIResult.Skip;
            }
            else
            {
                var result = AskQuestion(message, string.Empty,
                    new[] { FileSourceOperationUIResult.Skip, FileSourceOperationUIResult.Cancel },
                    FileSourceOperationUIResult.Skip, FileSourceOperationUIResult.Cancel);
                if (result == FileSourceOperationUIResult.Cancel)
                {
                    RaiseAbortOperation();
                }
                return result;
            }
        }

        private void LogMessage(string message, LogOptions logOptions, LogMessageType logMsgType)
        {
            switch (logMsgType)
            {
                case LogMessageType.Error:
                    if ((LogOptions.Errors & GlobalOptions.LogOptions) == 0) return;
                    break;
                case LogMessageType.Info:
                    if ((LogOptions.Info & GlobalOptions.LogOptions) == 0) return;
                    break;
                case LogMessageType.Success:
                    if ((LogOptions.Success & GlobalOptions.LogOptions) == 0) return;
                    break;
            }

            if ((logOptions & GlobalOptions.LogOptions) != 0)
            {
                Log.Write(Thread, message, logMsgType);
            }
        }
    }
} 