using System;

namespace zfile
{
    public class CallbackDataClass
    {
        public delegate int UpdateProgressDelegate(string sourceName, string targetName, int percentDone);
        public UpdateProgressDelegate UpdateProgressFunction { get; set; }
        public CallbackDataClass() { }
    }

    public class WfxPluginCopyOperation : FileSourceCopyOperation
    {
        private readonly IWfxPluginFileSource? _wfxPluginFileSource;
        private WfxPluginOperationHelper _operationHelper;
        private CallbackDataClass _callbackDataClass;
        private FileTree _sourceFilesTree;
        //private FileSourceCopyOperationStatistics _statistics;
        private int _infoOperation;

        public WfxPluginCopyOperation(IFileSource sourceFileSource, IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _wfxPluginFileSource = sourceFileSource as IWfxPluginFileSource;
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
            // 在C#中我们不使用静态字段

            _statistics = RetrieveStatistics();

            var treeBuilder = new WfxTreeBuilder(
                this.CreateAskQuestionDelegate(),
                () => CheckOperationState());
            try
            {
                treeBuilder.WfxModule = _wfxPluginFileSource.WfxModule;
                treeBuilder.SymLinkOption = FileSourceOperationSymLinkOption.Follow;
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

            if (_operationHelper != null)
            {
                _operationHelper = null; // 释放资源
            }
            _operationHelper = new WfxPluginOperationHelper(
                _wfxPluginFileSource,
                this.CreateAskQuestionDelegate(),
                () => RaiseAbortOperation(),
                () => CheckOperationState(),
                (ref FileSourceCopyOperationStatistics stats) => UpdateStatistics(stats),
                () => { /* 暂未实现 */ },
                ShowCompareFilesUIByFileObject,
                _thread,
                WfxPluginOperationHelperMode.Copy,
                TargetPath);

            _operationHelper.RenameMask = RenameMask;
            _operationHelper.FileExistsOption = FileExistsOption;
            _operationHelper.CopyAttributesOptions = CopyAttributesOptions;

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
            _callbackDataClass.UpdateProgressFunction = null;
            // 清除UpdateProgress函数引用
            FileExistsOption = _operationHelper.FileExistsOption;
            _operationHelper = null; // 释放资源
        }

        public static Type GetOptionsUIClass()
        {
            return typeof(WfxPluginCopyOperationOptionsUI);
        }
    }
}