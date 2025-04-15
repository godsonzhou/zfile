using Zfile.Operations;
namespace FileSystemOperations
{
    public class WfxPluginCopyOutOperation : FileSourceCopyOutOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private WfxPluginOperationHelper _operationHelper;
        private CallbackDataClass _callbackDataClass;
        private FileTree _sourceFilesTree;
        private FileSourceCopyOperationStatistics _statistics;
        private int _infoOperation;
        private bool _needsConnection;

        public WfxPluginCopyOutOperation(IFileSource sourceFileSource, IFileSource targetFileSource, ref FileList sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, ref sourceFiles, targetPath)
        {
            _wfxPluginFileSource = sourceFileSource as IWfxPluginFileSource;
            _callbackDataClass = (CallbackDataClass)_wfxPluginFileSource.WfxOperationList.Objects[_wfxPluginFileSource.PluginNumber];
            SetNeedsConnection(_needsConnection);
        }

        private void SetNeedsConnection(bool value)
        {
            _needsConnection = value;
            if (!_needsConnection)
            {
                _infoOperation = FsStatusOperation.GetMultiThread;
            }
            else if (SourceFiles.Count > 1)
            {
                _infoOperation = FsStatusOperation.GetMulti;
            }
            else
            {
                _infoOperation = FsStatusOperation.GetSingle;
            }
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

            if (!Application.DoEvents())
            {
                return 1;
            }

            return 0;
        }

        public override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(SourceFiles.Path, FsStatus.Start, _infoOperation);
            _callbackDataClass.UpdateProgressFunction = UpdateProgress;
            UpdateProgressFunction = UpdateProgress;

            _statistics = RetrieveStatistics;

            var treeBuilder = new WfxTreeBuilder(AskQuestion, CheckOperationState);
            try
            {
                treeBuilder.WfxModule = _wfxPluginFileSource.WfxModule;
                treeBuilder.SymLinkOption = FileSourceOperationSymlinkOption.Follow;
                treeBuilder.BuildFromFiles(SourceFiles);
                _sourceFilesTree = treeBuilder.ReleaseTree();
                _statistics.TotalFiles = treeBuilder.FilesCount;
                _statistics.TotalBytes = treeBuilder.FilesSize;
            }
            finally
            {
                treeBuilder.Dispose();
            }

            _operationHelper?.Dispose();
            _operationHelper = new WfxPluginOperationHelper(
                _wfxPluginFileSource,
                AskQuestion,
                RaiseAbortOperation,
                CheckOperationState,
                UpdateStatistics,
                ShowCompareFilesUI,
                ShowCompareFilesUIByFileObject,
                Thread,
                WfxPluginOperationHelperMode.CopyOut,
                TargetPath);

            _operationHelper.RenameMask = RenameMask;
            _operationHelper.FileExistsOption = FileExistsOption;
            _operationHelper.CopyAttributesOptions = CopyAttributesOptions;

            _operationHelper.Initialize();
        }

        public override void MainExecute()
        {
            _operationHelper.ProcessTree(_sourceFilesTree, _statistics);
        }

        public override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(SourceFiles.Path, FsStatus.End, _infoOperation);
            _callbackDataClass.UpdateProgressFunction = null;
            UpdateProgressFunction = null;
            FileExistsOption = _operationHelper.FileExistsOption;
            _operationHelper.Dispose();
        }

        public static Type GetOptionsUIClass()
        {
            return typeof(WfxPluginCopyOutOperationOptionsUI);
        }

        public bool NeedsConnection
        {
            get => _needsConnection;
            set => SetNeedsConnection(value);
        }
    }
} 