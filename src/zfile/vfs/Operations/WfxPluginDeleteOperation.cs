namespace zfile
{
    public class WfxPluginDeleteOperation : FileSourceDeleteOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private FileEntries _fullFilesTreeToDelete;
        private FileSourceDeleteOperationStatistics _statistics;
        private FileSourceOperationSymlinkOption _symLinkOption;
        private bool _skipErrors;
        private FileSourceOperationOptionGeneral _deleteReadOnly;

        public WfxPluginDeleteOperation(IFileSource targetFileSource, ref FileEntries filesToDelete)
            : base(targetFileSource, ref filesToDelete)
        {
            _symLinkOption = FileSourceOperationSymlinkOption.None;
            _skipErrors = false;
            _deleteReadOnly = FileSourceOperationOptionGeneral.None;
            _fullFilesTreeToDelete = null;
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
        }

        protected override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(FilesToDelete.Path, FsStatus.Start, FsStatusOperation.Delete);
            _statistics = RetrieveStatistics();
            _wfxPluginFileSource.FillAndCount(FilesToDelete, true, false, out _fullFilesTreeToDelete, out _statistics.TotalFiles, out _statistics.TotalBytes);
        }

        protected override void MainExecute()
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

        protected override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(FilesToDelete.Path, FsStatus.End, FsStatusOperation.Delete);
        }

        private bool ProcessFile(FileEntry file)
        {
            var fileName = file.Path + file.Name;
            var retry = false;

            if (FileSystemUtil.FileIsReadOnly(file.Attributes))
            {
                switch (_deleteReadOnly)
                {
                    case FileSourceOperationOptionGeneral.None:
                        switch (AskQuestion(string.Format(Resources.MsgFileReadOnly, fileName), string.Empty,
                            new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.All, FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.SkipAll },
							FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.Skip))
                        {
                            case FileSourceOperationUIResponse.All:
                                _deleteReadOnly = FileSourceOperationOptionGeneral.Yes;
                                break;
                            case FileSourceOperationUIResponse.Skip:
                                return false;
                            case FileSourceOperationUIResponse.SkipAll:
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
                        LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogRmDir, fileName), LogOption.VfsOp, LogOption.Success);
                    }
                    else
                    {
                        LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogDelete, fileName), LogOption.VfsOp, LogOption.Success);
                    }
                    return true;
                }
                else
                {
                    string message;
                    string question;
                    LogOption logOptions;

                    if (file.IsDirectory)
                    {
                        logOptions = LogOption.VfsOp;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogRmDir, fileName);
                        question = string.Format(Resources.MsgNotDelete, fileName);
                    }
                    else
                    {
                        logOptions = LogOption.VfsOp;
                        message = string.Format(Resources.MsgLogError + Resources.MsgLogDelete, fileName);
                        question = string.Format(Resources.MsgNotDelete, fileName);
                    }

                    if (GlobalSettings.SkipFileOpError || _skipErrors)
                    {
                        LogMessage(message, logOptions, LogOption.Error);
                    }
                    else
                    {
                        switch (AskQuestion(question, string.Empty,
                            new[] { FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.SkipAll, FileSourceOperationUIResponse.Abort },
                            FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip))
                        {
                            case FileSourceOperationUIResponse.Retry:
                                retry = true;
                                break;
                            case FileSourceOperationUIResponse.SkipAll:
                                _skipErrors = true;
                                break;
                            case FileSourceOperationUIResponse.Abort:
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
            if (GlobalSettings.SkipFileOpError)
            {
                Logger.Write(_Thread, message, LogOption.Error, true);
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

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            switch (logMsgType)
            {
                case LogOption.Error:
                    if ((LogOption.Error & GlobalSettings.LogOptions) == 0) return;
                    break;
                case LogOption.Info:
                    if ((LogOption.Info & GlobalSettings.LogOptions) == 0) return;
                    break;
                case LogOption.Success:
                    if ((LogOption.Success & GlobalSettings.LogOptions) == 0) return;
                    break;
            }

            if ((logOptions & GlobalSettings.LogOptions) != 0)
            {
                Logger.Write(_Thread, message, logMsgType);
            }
        }
    }
} 