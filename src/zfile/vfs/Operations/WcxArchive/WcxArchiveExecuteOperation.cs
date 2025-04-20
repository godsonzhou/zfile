using zfile.Dialogs;

namespace zfile
{
	public class WcxArchiveExecuteOperation : FileSourceExecuteOperation
	{
		private IWcxArchiveFileSource _wcxArchiveFileSource;

		public WcxArchiveExecuteOperation(IFileSource targetFileSource, FileEntry executableFile, string currentPath, string verb)
			: base(targetFileSource, executableFile, currentPath, verb)
		{
			_wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
		}

		protected override void Initialize()
		{
		}

		protected override void MainExecute()
		{
			if (Verb != "properties" && MatchesMaskList(ExecutableFile.Name, GlobalSettings.AutoExtractOpenMask))
			{
				ExecuteOperationResult = FileSourceExecuteOperationResult.YourSelf;
			}
			else
			{
				ExecuteOperationResult = PackInfoDialog.Show(_wcxArchiveFileSource, ExecutableFile);
			}
		}

		protected override void Finalize()
		{
		}
	}
}