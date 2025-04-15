using Files.FileSources.FileSystem;
using Zfile;
using Zfile.FileSources;
using Zfile.Operations;

namespace FileSystemOperations
{
	public class FileSystemCopyOperation : FileSourceCopyOperation
	{
		private FileSystemOperationHelper _operationHelper;
		private bool _excludeEmptyTemplateDirectories;
		private SearchTemplate _searchTemplate;
		private FileSourceOperationOptionGeneral _copyOnWrite;
		private FileSourceOperationOptionSetPropertyError _setPropertyError;
		private FileTree _sourceFilesTree;
		private FileSourceCopyOperationStatistics _statistics;

		// Options
		private bool _verify;
		private bool _reserveSpace;
		private bool _checkFreeSpace;
		private bool _skipAllBigFiles;
		private bool _autoRenameItself;
		private bool _correctSymLinks;

		public bool Verify
		{
			get => _verify;
			set => _verify = value;
		}

		public bool CheckFreeSpace
		{
			get => _checkFreeSpace;
			set => _checkFreeSpace = value;
		}

		public bool ReserveSpace
		{
			get => _reserveSpace;
			set => _reserveSpace = value;
		}

		public bool SkipAllBigFiles
		{
			get => _skipAllBigFiles;
			set => _skipAllBigFiles = value;
		}

		public bool AutoRenameItself
		{
			get => _autoRenameItself;
			set => _autoRenameItself = value;
		}

		public bool CorrectSymLinks
		{
			get => _correctSymLinks;
			set => _correctSymLinks = value;
		}

		public FileSourceOperationOptionGeneral CopyOnWrite
		{
			get => _copyOnWrite;
			set => _copyOnWrite = value;
		}

		public FileSourceOperationOptionSetPropertyError SetPropertyError
		{
			get => _setPropertyError;
			set => _setPropertyError = value;
		}

		public bool ExcludeEmptyTemplateDirectories
		{
			get => _excludeEmptyTemplateDirectories;
			set => _excludeEmptyTemplateDirectories = value;
		}

		public SearchTemplate SearchTemplate
		{
			get => _searchTemplate;
			set
			{
				_searchTemplate?.Dispose();
				_searchTemplate = value;
			}
		}

		public FileSystemCopyOperation(
			IFileSource sourceFileSource,
			IFileSource targetFileSource,
			List<FileInfo> sourceFiles,
			string targetPath)
			: base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
		{
			// Initialize default values from global settings
			SymLinkOption = GlobalSettings.OperationOptionSymLinks;
			_setPropertyError = GlobalSettings.OperationOptionSetPropertyError;
			_reserveSpace = GlobalSettings.OperationOptionReserveSpace;
			_checkFreeSpace = GlobalSettings.OperationOptionCheckFreeSpace;
			_skipAllBigFiles = false;
			_autoRenameItself = false;
			_correctSymLinks = GlobalSettings.OperationOptionCorrectLinks;
			_excludeEmptyTemplateDirectories = true;

			_copyOnWrite = GlobalSettings.OperationOptionCopyOnWrite;
			FileExistsOption = GlobalSettings.OperationOptionFileExists;
			DirExistsOption = GlobalSettings.OperationOptionDirectoryExists;

			// Set copy attributes options
			if (GlobalSettings.OperationOptionCopyAttributes)
				CopyAttributesOptions |= CopyAttributesOption.CopyAttributes;
			if (GlobalSettings.OperationOptionCopyXattributes)
				CopyAttributesOptions |= CopyAttributesOption.CopyXattributes;
			if (GlobalSettings.OperationOptionCopyTime)
				CopyAttributesOptions |= CopyAttributesOption.CopyTime;
			if (GlobalSettings.OperationOptionCopyOwnership)
				CopyAttributesOptions |= CopyAttributesOption.CopyOwnership;
			if (GlobalSettings.OperationOptionCopyPermissions)
				CopyAttributesOptions |= CopyAttributesOption.CopyPermissions;
			if (GlobalSettings.DropReadOnlyFlag)
				CopyAttributesOptions |= CopyAttributesOption.RemoveReadOnlyAttr;
		}

		public override void Initialize()
		{
			_statistics = RetrieveStatistics();

			var treeBuilder = new FileSystemTreeBuilder(
				AskQuestion,
				CheckOperationState)
			{
				SymLinkOption = SymLinkOption,
				SearchTemplate = SearchTemplate,
				ExcludeEmptyTemplateDirectories = ExcludeEmptyTemplateDirectories
			};

			try
			{
				treeBuilder.BuildFromFiles(SourceFiles);
				_sourceFilesTree = treeBuilder.ReleaseTree();
				_statistics.TotalFiles = treeBuilder.FilesCount;
				_statistics.TotalBytes = treeBuilder.FilesSize;
				if (_verify)
					_statistics.TotalBytes *= 2;
			}
			finally
			{
				treeBuilder.Dispose();
			}

			_operationHelper?.Dispose();
			_operationHelper = new FileSystemOperationHelper(
				AskQuestion,
				RaiseAbortOperation,
				AppProcessMessages,
				CheckOperationState,
				UpdateStatistics,
				ShowCompareFilesUI,
				Thread,
				FileSourceOperationHelperMode.Copy,
				TargetPath,
				_statistics)
			{
				Verify = _verify,
				RenameMask = RenameMask,
				CopyOnWrite = _copyOnWrite,
				ReserveSpace = _reserveSpace,
				CheckFreeSpace = CheckFreeSpace,
				CopyAttributesOptions = CopyAttributesOptions,
				SkipAllBigFiles = SkipAllBigFiles,
				AutoRenameItself = AutoRenameItself,
				CorrectSymLinks = CorrectSymLinks,
				FileExistsOption = FileExistsOption,
				DirExistsOption = DirExistsOption,
				SetPropertyError = SetPropertyError
			};

			_operationHelper.Initialize();
		}

		public override void MainExecute()
		{
			_operationHelper.ProcessTree(_sourceFilesTree);
		}

		public override void Finalize()
		{
			FileExistsOption = _operationHelper.FileExistsOption;
			_operationHelper.Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_sourceFilesTree?.Dispose();
				_operationHelper?.Dispose();
				_searchTemplate?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}