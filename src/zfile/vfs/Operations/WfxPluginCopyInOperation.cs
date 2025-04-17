namespace zfile;

public class WfxPluginCopyInOperation : FileSourceCopyInOperation
{
	private readonly IWfxPluginFileSource? _wfxPluginFileSource;
	private WfxPluginOperationHelper _operationHelper;
	private CallbackDataClass _callbackDataClass;
	private FileTree _sourceFilesTree;
	private FileSourceCopyOperationStatistics _statistics;
	private int _infoOperation;
	private bool _needsConnection;

	public WfxPluginCopyInOperation(
		IFileSource sourceFileSource,
		IFileSource targetFileSource,
		ref FileEntries sourceFiles,
		string targetPath) : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
	{
		_wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
		_callbackDataClass = (CallbackDataClass)_wfxPluginFileSource.WfxOperationList.Objects[PluginNumber];
		SetNeedsConnection(_needsConnection);
	}

	private void SetNeedsConnection(bool value)
	{
		_needsConnection = value;
		if (!_needsConnection)
		{
			_infoOperation = FS_STATUS_OP_PUT_MULTI_THREAD;
		}
		else if (SourceFiles.Count > 1)
		{
			_infoOperation = FS_STATUS_OP_PUT_MULTI;
		}
		else
		{
			_infoOperation = FS_STATUS_OP_PUT_SINGLE;
		}
	}

	private int UpdateProgress(string sourceName, string targetName, int percentDone)
	{
		if (State == FileSourceOperationState.Stopping)
			return 1;

		if (!string.IsNullOrEmpty(sourceName))
		{
			_statistics.CurrentFileFrom = sourceName;
		}
		if (!string.IsNullOrEmpty(targetName))
		{
			_statistics.CurrentFileTo = targetName;
		}

		var temp = _statistics.CurrentFileTotalBytes * percentDone / 100;
		_statistics.DoneBytes += (temp - _statistics.CurrentFileDoneBytes);
		_statistics.CurrentFileDoneBytes = temp;

		UpdateStatistics(_statistics);

		return AppProcessMessages(true) ? 0 : 1;
	}

	protected override void Initialize()
	{
		_wfxPluginFileSource.WfxModule.setStatusInfo(TargetPath, FS_STATUS_START, _infoOperation);
		_callbackDataClass.UpdateProgressFunction = UpdateProgress;
		UpdateProgressFunction = UpdateProgress;

		_statistics = RetrieveStatistics();

		var treeBuilder = new FileSystemTreeBuilder(AskQuestion, CheckOperationState);
		try
		{
			treeBuilder.ElevateAction = DuplicateAction.Error;
			treeBuilder.SymLinkOption = FileSourceOperationSymLinkOption.Follow;
			treeBuilder.BuildFromFiles(SourceFiles);
			_sourceFilesTree = treeBuilder.ReleaseTree();
			_statistics.TotalFiles = treeBuilder.FilesCount;
			_statistics.TotalBytes = treeBuilder.FilesSize;
		}
		finally
		{
			treeBuilder.Dispose();
		}

		if (_operationHelper != null)
		{
			_operationHelper.Dispose();
		}

		_operationHelper = new WfxPluginOperationHelper(
			_wfxPluginFileSource,
			AskQuestion,
			RaiseAbortOperation,
			CheckOperationState,
			UpdateStatistics,
			ShowCompareFilesUI,
			ShowCompareFilesUIByFileObject,
			Thread,
			WfxPluginOperationHelperMode.CopyIn,
			TargetPath);

		_operationHelper.RenameMask = RenameMask;
		_operationHelper.FileExistsOption = FileExistsOption;
		_operationHelper.CopyAttributesOptions = CopyAttributesOptions;

		_operationHelper.Initialize();
	}

	protected override void MainExecute()
	{
		_operationHelper.ProcessTree(_sourceFilesTree, ref _statistics);
	}

	protected override void Finalize()
	{
		_wfxPluginFileSource.WfxModule.setStatusInfo(TargetPath, (int)FsStatus.End, _infoOperation);
		_callbackDataClass.UpdateProgressFunction = null;
		UpdateProgressFunction = null;
		FileExistsOption = _operationHelper.FileExistsOption;
		_operationHelper.Dispose();
	}

	public Type GetOptionsUIClass()
	{
		return typeof(WfxPluginCopyInOperationOptionsUI);
	}
}