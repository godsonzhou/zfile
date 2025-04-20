namespace zfile
{
    //public struct FileSourceCopyOperationStatistics
    //{
    //	public string CurrentFileFrom;
    //	public string CurrentFileTo;
    //	public long CurrentFileTotalBytes;
    //	public long CurrentFileDoneBytes;
    //	public long TotalFiles;
    //	public long DoneFiles;
    //	public long TotalBytes;
    //	public long DoneBytes;
    //	public long BytesPerSecond;
    //	public DateTime RemainingTime;

    //	public long SkippedFiles;
    //	public long SkippedBytes;
    //	public long FailedFiles;
    //	public long FailedBytes;
    //}


    public class WfxPluginMoveOperation : FileSourceMoveOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private WfxPluginOperationHelper? _operationHelper;
        private readonly CallbackDataClass _callbackDataClass;
        private FileTree? _sourceFilesTree;
        private FileSourceCopyOperationStatistics _statistics = new();
        private readonly int _infoOperation;

        public WfxPluginMoveOperation(IFileSource fileSource, FileEntries sourceFiles, string targetPath)
            : base(fileSource, sourceFiles, targetPath)
        {
            _wfxPluginFileSource = fileSource as IWfxPluginFileSource ?? throw new ArgumentException("FileSource must be an IWfxPluginFileSource");
            _callbackDataClass = (CallbackDataClass)_wfxPluginFileSource.WfxOperationList.Objects[_wfxPluginFileSource.PluginNumber];

            _infoOperation = (int)(sourceFiles.Count > 1 ? FsStatusOperation.RenMovMulti : FsStatusOperation.RenMovSingle);
        }

        private int UpdateProgress(string sourceName, string targetName, int percentDone)
        {
            if (State == FileSourceOperationState.Stopping)
            {
                return 1;
            }

            if (!string.IsNullOrEmpty(sourceName))
            {
                _statistics.CurrentFileFrom = sourceName;
            }
            if (!string.IsNullOrEmpty(targetName))
            {
                _statistics.CurrentFileTo = targetName;
            }

            var temp = _statistics.CurrentFileTotalBytes * percentDone / 100;
            _statistics.DoneBytes += temp - _statistics.CurrentFileDoneBytes;
            _statistics.CurrentFileDoneBytes = temp;

            UpdateStatistics(_statistics);

            // 在Pascal版本中使用Application.ProcessMessages
            // 在C#中我们使用不同的方式处理消息循环

            return 0;
        }

        protected override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.setStatusInfo(SourceFiles.Path, (int)FsStatus.Start, _infoOperation);
            _callbackDataClass.UpdateProgressFunction = UpdateProgress;
            // 在Pascal版本中使用threadvar存储UpdateProgress函数
            // 在C#中我们不使用静态字段，而是直接使用实例方法

            _statistics = RetrieveStatistics();

            var treeBuilder = new WfxTreeBuilder(this.CreateAskQuestionDelegate(), CheckOperationState);
            try
            {
                treeBuilder.WfxModule = _wfxPluginFileSource.WfxModule;
                treeBuilder.SymLinkOption = FileSourceOperationSymLinkOption.DontFollow;
                treeBuilder.BuildFromFiles(SourceFiles);
                var node = treeBuilder.ReleaseTree();
                _sourceFilesTree = new FileTree(node.TheFile.Path);
                _statistics.TotalFiles = treeBuilder.FilesCount;
                _statistics.TotalBytes = treeBuilder.FilesSize;
            }
            finally
            {
                treeBuilder.Dispose();
            }

            // 创建新的操作助手
            _operationHelper = new WfxPluginOperationHelper(
                _wfxPluginFileSource,
                this.CreateAskQuestionDelegate(),
                () => RaiseAbortOperation(),
                () => CheckOperationState(),
                (ref FileSourceCopyOperationStatistics stats) => UpdateStatistics(stats),
                () => { /* 暂未实现 */ },
                ShowCompareFilesUIByFileObject,
                _thread,
                WfxPluginOperationHelperMode.Move,
                TargetPath);

            _operationHelper.RenameMask = RenameMask;
            _operationHelper.FileExistsOption = FileExistsOption;

            _operationHelper.Initialize();
        }

        protected override void MainExecute()
        {
            if (_operationHelper != null && _sourceFilesTree != null)
            {
                _operationHelper.ProcessTree(_sourceFilesTree, ref _statistics);
            }
        }

        protected override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.setStatusInfo(SourceFiles.Path, (int)FsStatus.End, _infoOperation);
            if (_callbackDataClass != null)
            {
                _callbackDataClass.UpdateProgressFunction = null!;
            }
            // 清除UpdateProgress函数引用
            if (_operationHelper != null)
            {
                FileExistsOption = _operationHelper.FileExistsOption;
                // 释放资源
                _operationHelper = null;
            }
        }

        public static Type GetOptionsUIClass()
        {
            return typeof(WfxPluginMoveOperationOptionsUI);
        }
    }
}