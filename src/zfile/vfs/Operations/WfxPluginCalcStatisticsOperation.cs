namespace zfile
{
    public class WfxPluginCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        public WfxPluginCalcStatisticsOperation(IFileSource targetFileSource, ref FileList files)
            : base(targetFileSource, ref files)
        {
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
        }

        public override void Initialize()
        {
            _statistics = RetrieveStatistics();

            _wfxPluginFileSource.WfxModule.WfxStatusInfo(Files.Path, FsStatus.Start, FsStatusOperation.CalcSize);
        }

        public override void MainExecute()
        {
            for (int currentFileIndex = 0; currentFileIndex < Files.Count; currentFileIndex++)
            {
                ProcessFile(Files[currentFileIndex]);
            }
        }

        public override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(Files.Path, FsStatus.End, FsStatusOperation.CalcSize);
        }

        private void ProcessFile(FileInfo file)
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
            var wfxModule = _wfxPluginFileSource.WfxModule;
            var handle = wfxModule.WfxFindFirst(srcPath, out var findData);
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

                    var file = WfxPluginFileSource.CreateFile(srcPath, findData);
                    try
                    {
                        ProcessFile(file);
                    }
                    finally
                    {
                        file.Dispose();
                    }
                } while (wfxModule.WfxFindNext(handle, out findData));
            }
            finally
            {
                wfxModule.FsFindClose(handle);
            }
        }
    }
} 