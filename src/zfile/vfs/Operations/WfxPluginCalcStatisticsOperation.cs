namespace zfile
{
    public class WfxPluginCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly IWfxPluginFileSource? _wfxPluginFileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        public WfxPluginCalcStatisticsOperation(IFileSource targetFileSource, FileEntries files)
            : base(targetFileSource, files)
        {
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
        }

        protected override void Initialize()
        {
            _statistics = RetrieveStatistics();

            _wfxPluginFileSource?.WfxModule.setStatusInfo(Files.Path, (int)FsStatus.Start, (int)FsStatusOperation.CalcSize);
        }

        protected override void MainExecute()
        {
            for (int currentFileIndex = 0; currentFileIndex < Files.Count; currentFileIndex++)
            {
                ProcessFile(Files[currentFileIndex]);
            }
        }

        protected override void Finalize()
        {
            _wfxPluginFileSource?.WfxModule.setStatusInfo(Files.Path, (int)FsStatus.End, (int)FsStatusOperation.CalcSize);
        }

        private void ProcessFile(FileEntry file)
        {
            _statistics.CurrentFile = file.Path + file.Name;
            UpdateStatistics(_statistics);

            Application.DoEvents();

            CheckOperationState();

            if (file.IsDirectory)
            {
                _statistics.Directories++;
                ProcessSubDirs(file.Path + file.Name + Path.DirectorySeparatorChar);
            }
            else if (file.IsLink)
            {
                _statistics.Links++;
            }
            else
            {
                _statistics.Files++;
                _statistics.Size += file.Size;
                if (file.ModificationTime < _statistics.OldestFile)
                {
                    _statistics.OldestFile = file.ModificationTime;
                }
                if (file.ModificationTime > _statistics.NewestFile)
                {
                    _statistics.NewestFile = file.ModificationTime;
                }
            }

            UpdateStatistics(_statistics);
        }

        private void ProcessSubDirs(string srcPath)
        {
            var wfxModule = _wfxPluginFileSource?.WfxModule;
			WfxFindData findData = new();
            var handle = wfxModule?.WfxFindFirst(srcPath, out findData);
			//WfxFindData findData = handle.First();
			if (handle == WfxModule.WfxInvalidHandle)
			{
				return;
			}

			try
			{
                do
                {
                    if (findData.FileName == "." || findData.FileName == "..")
                    {
                        continue;
                    }

                    var file = _wfxPluginFileSource.CreateFile(srcPath + findData.FileName);
                    try
                    {
                        ProcessFile(file);
                    }
                    finally
                    {
                        //file.Dispose();
                    }
                } while (wfxModule?.WfxFindNext(handle, out findData));
            }
            finally
            {
                wfxModule.FsFindClose(handle);
            }
        }
    }
} 