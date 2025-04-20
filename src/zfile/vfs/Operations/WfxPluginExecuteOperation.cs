namespace zfile;

public class WfxPluginExecuteOperation : FileSourceExecuteOperation
{
	private readonly IWfxPluginFileSource? _wfxPluginFileSource;

	public WfxPluginExecuteOperation(
		IFileSource targetFileSource,
		FileEntry executableFile,
		string currentPath,
		string verb) : base(targetFileSource, executableFile, currentPath, verb)
	{
		_wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
	}

	protected override void Initialize()
	{
		_wfxPluginFileSource?.WfxModule.setStatusInfo(CurrentPath, WfxConstants.FS_STATUS_START, WfxConstants.FS_STATUS_OP_EXEC);
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

		IntPtr mainWin;
		var mainForm = Application.OpenForms[0];
		if (mainForm?.Tag is IntPtr intPtrTag)
		{
			mainWin = intPtrTag;
		}
		else if (mainForm != null)
		{
			mainWin = mainForm.Handle;
		}
		else
		{
			mainWin = IntPtr.Zero;
		}

		if (_wfxPluginFileSource?.WfxModule == null)
		{
			ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
			return;
		}

		var result = _wfxPluginFileSource.WfxModule.ExecuteFile(
			mainWin,
			remoteName,
			Verb);

		switch (result)
		{
			case WfxConstants.FS_EXEC_OK:
				ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
				break;
			case WfxConstants.FS_EXEC_ERROR:
				ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
				break;
			case WfxConstants.FS_EXEC_YOURSELF:
				ExecuteOperationResult = FileSourceExecuteOperationResult.YourSelf;
				break;
			case WfxConstants.FS_EXEC_SYMLINK:
				ResultString = remoteName;
				ExecuteOperationResult = FileSourceExecuteOperationResult.SymLink;
				break;
		}
	}

	protected new void Finalize()
	{
		_wfxPluginFileSource?.WfxModule?.setStatusInfo(CurrentPath, WfxConstants.FS_STATUS_END, WfxConstants.FS_STATUS_OP_EXEC);
		base.Finalize();
	}
}