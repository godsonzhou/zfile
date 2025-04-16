namespace zfile
{
    public class WfxPluginListOperation : FileSourceListOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private readonly CallbackDataClass _callbackDataClass;
        private readonly string _currentPath;

        public WfxPluginListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries(path);
            _wfxPluginFileSource = fileSource as IWfxPluginFileSource;
            _callbackDataClass = (CallbackDataClass)_wfxPluginFileSource.WfxOperationList.Objects[_wfxPluginFileSource.PluginNumber];
            _currentPath = ExcludeBackPathDelimiter(path);
        }

        private int UpdateProgress(string sourceName, string targetName, int percentDone)
        {
            if (State == FileSourceOperationState.Stopping)
            {
                return 1;
            }

            Log.Write(Resources.MsgLoadingFileList + percentDone + "%", LogOption.Info, false, false);

            return CheckOperationStateSafe() ? 0 : 1;
        }

        public override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(_currentPath, FsStatus.Start, FsStatusOperation.List);
            _callbackDataClass.UpdateProgressFunction = UpdateProgress;
            UpdateProgressFunction = UpdateProgress;
        }

        public override void MainExecute()
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
                    var file = WfxPluginFileSource.CreateFile(Path);
                    file.Name = "..";
                    file.Attributes = GenericAttribute.Folder;
                    Files.Insert(0, file);
                }
            }
        }

        public override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(_currentPath, FsStatus.End, FsStatusOperation.List);
            _callbackDataClass.UpdateProgressFunction = null;
            UpdateProgressFunction = null;
        }
    }
} 