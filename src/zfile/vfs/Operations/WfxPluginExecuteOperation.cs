namespace zfile;

public class WfxPluginExecuteOperation : FileSourceExecuteOperation
{
	private readonly IWfxPluginFileSource _wfxPluginFileSource;

	public WfxPluginExecuteOperation(
		IFileSource targetFileSource,
		ref FileEntry executableFile,
		string currentPath,
		string verb) : base(targetFileSource, executableFile, currentPath, verb)
	{
		_wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
	}

	protected override void Initialize()
	{
		_wfxPluginFileSource.WfxModule.setStatusInfo(CurrentPath, FS_STATUS_START, FS_STATUS_OP_EXEC);
	}

	protected override void MainExecute()
	{
		string remoteName;
		if (Verb.StartsWith("quote "))
		{
			remoteName = CurrentPath;
		}
		else
		{
			remoteName = AbsolutePath;
		}

		var result = _wfxPluginFileSource.WfxModule.ExecuteFile(
			Application.OpenForms[0].Tag,
			remoteName,
			Verb);

		switch (result)
		{
			case FS_EXEC_OK:
				ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
				break;
			case FS_EXEC_ERROR:
				ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
				break;
			case FS_EXEC_YOURSELF:
				ExecuteOperationResult = FileSourceExecuteOperationResult.YourSelf;
				break;
			case FS_EXEC_SYMLINK:
				ResultString = remoteName;
				ExecuteOperationResult = FileSourceExecuteOperationResult.SymLink;
				break;
		}
	}

	protected override void Finalize()
	{
		_wfxPluginFileSource.WfxModule.setStatusInfo(CurrentPath, FS_STATUS_END, FS_STATUS_OP_EXEC);
	}
}