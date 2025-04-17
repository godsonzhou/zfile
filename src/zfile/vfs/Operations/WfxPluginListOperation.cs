namespace zfile
{
    public class WfxPluginListOperation : FileSourceListOperation
    {
        private readonly IWfxPluginFileSource? _wfxPluginFileSource;
        private readonly CallbackDataClass _callbackDataClass;
        private readonly string _currentPath;

        public WfxPluginListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries(path);
            _wfxPluginFileSource = fileSource as IWfxPluginFileSource;
            _callbackDataClass = (CallbackDataClass)_wfxPluginFileSource.WfxOperationList.Objects[_wfxPluginFileSource.PluginNumber];
            _currentPath = Helper.ExcludeTrailingPathDelimiter(path);
        }

        private int UpdateProgress(string sourceName, string targetName, int percentDone)
        {
            if (State == FileSourceOperationState.Stopping)
            {
                return 1;
            }

            Logger.Write(Resources.MsgLoadingFileEntries + percentDone + "%", LogOption.Info, false);

            return CheckOperationStateSafe() ? 0 : 1;
        }

        protected override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.setStatusInfo(_currentPath, (int)FsStatus.Start, (int)FsStatusOperation.List);
            _callbackDataClass.UpdateProgressFunction = UpdateProgress;
            UpdateProgressFunction = UpdateProgress;
        }

        protected override void MainExecute()
        {
            var haveUpDir = false;
            try
            {
                Files.Clear();
                var handle = _wfxPluginFileSource.WfxModule.WfxFindFirst(_currentPath, out var findData);
                if (handle != WfxModule.WfxInvalidHandle)
                {
                    try
                    {
                        do
                        {
                            CheckOperationState();
                            if (findData.FileName == ".") continue;
                            if (findData.FileName == "..")
                            {
                                haveUpDir = true;
                                continue;
                            }

                            var file = WfxPluginFileSource.CreateFile(Path, findData);
                            Files.Add(file);
                        } while (_wfxPluginFileSource.WfxModule.WfxFindNext(handle, out findData));
                    }
                    finally
                    {
                        _wfxPluginFileSource.WfxModule.FsFindClose(handle);
                    }
                }
            }
            finally
            {
                if (!haveUpDir)
                {
                    var file = _wfxPluginFileSource.CreateFile(Path);
                    file.Name = "..";
                    file.Attributes = GenericAttribute.Folder;
                    Files.Insert(0, file);
                }
            }
        }

        protected override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.setStatusInfo(_currentPath, (int)FsStatus.End, (int)FsStatusOperation.List);
            _callbackDataClass.UpdateProgressFunction = null;
            UpdateProgressFunction = null;
        }
    }
} 