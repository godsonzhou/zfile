namespace zfile
{
    public enum WfxPluginOperationHelperMode
    {
        Copy,
        CopyIn,
        CopyOut,
        Move
    }

    public class WfxTreeBuilder : FileSourceTreeBuilder
    {
        private WfxModule _wfxModule;

		public WfxTreeBuilder(AskQuestionFunction askQuestionFunction, CheckOperationStateFunction checkOperationStateFunction) : base(askQuestionFunction, checkOperationStateFunction)
		{
		}

		public WfxModule WfxModule
        {
            get => _wfxModule;
            set => _wfxModule = value;
        }

        protected override void AddLinkTarget(FileEntry file, FileTreeNode currentNode)
        {
            if (file.AttributesProperty is NtfsFileAttributesProperty)
            {
                file.Attributes &= ~FILE_ATTRIBUTE_REPARSE_POINT;
            }
            else
            {
                file.Attributes &= ~S_IFLNK;
            }

            if (!file.IsLinkToDirectory)
            {
                AddFile(file, currentNode);
            }
            else
            {
                if (file.AttributesProperty is NtfsFileAttributesProperty)
                {
                    file.Attributes |= FileAttributes.Directory;
                }
                else
                {
                    file.Attributes |= S_IFDIR;
                }
                AddDirectory(file, currentNode);
            }
        }

        protected override void AddFilesInDirectory(string srcPath, FileTreeNode currentNode)
        {
            var handle = _wfxModule.WfxFindFirst(srcPath, out var findData);
            if (handle == WfxModule.WfxInvalidHandle) return;

            try
            {
                do
                {
                    if (findData.FileName == "." || findData.FileName == "..") continue;
                    var file = WfxPluginFileSource.CreateFile(srcPath, findData);
                    AddItem(file, currentNode);
                } while (_wfxModule.WfxFindNext(handle, out findData));
            }
            finally
            {
                _wfxModule.FsFindClose(handle);
            }
        }
    }

    public class WfxPluginOperationHelper
    {
        private FileEntry _rootDir;
        private IWfxPluginFileSource _wfxPluginFileSource;
        private Thread _operationThread;
        private WfxPluginOperationHelperMode _mode;
        private string _rootTargetPath;
        private string _renameMask;
        private string _renameNameMask;
        private string _renameExtMask;
        private string _logCaption;
        private bool _renamingFiles;
        private bool _renamingRootDir;
        private bool _internal;
        private FileSourceCopyOperationStatistics _statistics;
        private CopyAttributesOption _copyAttributesOptions;
        private FileSourceOperationOptionFileExists _fileExistsOption;

        private FileEntry _currentFile;
        private FileEntry _currentTargetFile;
        private string _currentTargetFilePath;

        private AskQuestionFunction _askQuestion;
        private Action _abortOperation;
        private CheckOperationStateFunction _checkOperationState;
        private UpdateStatisticsFunction _updateStatistics;
        private ShowCompareFilesUIFunction _showCompareFilesUI;
        private ShowCompareFilesUIByFileObjectFunction _showCompareFilesUIByFileObject;

        public WfxPluginOperationHelper(
            IFileSource fileSource,
            AskQuestionFunction askQuestionFunction,
            AbortOperationFunction abortOperationFunction,
            CheckOperationStateFunction checkOperationStateFunction,
            UpdateStatisticsFunction updateStatisticsFunction,
            ShowCompareFilesUIFunction showCompareFilesUIFunction,
            ShowCompareFilesUIByFileObjectFunction showCompareFilesUIByFileObjectFunction,
            Thread operationThread,
            WfxPluginOperationHelperMode mode,
            string targetPath)
        {
            _wfxPluginFileSource = fileSource as IWfxPluginFileSource;
            _askQuestion = askQuestionFunction;
            _abortOperation = abortOperationFunction;
            _checkOperationState = checkOperationStateFunction;
            _updateStatistics = updateStatisticsFunction;
            _showCompareFilesUI = showCompareFilesUIFunction;
            _showCompareFilesUIByFileObject = showCompareFilesUIByFileObjectFunction;
            _operationThread = operationThread;
            _mode = mode;
            _rootTargetPath = targetPath;
        }

        public void Initialize()
        {
            // Implementation of initialization logic
        }

        public void ProcessTree(FileTree fileTree, ref FileSourceCopyOperationStatistics statistics)
        {
            // Implementation of tree processing logic
        }

        public FileSourceOperationOptionFileExists FileExistsOption
        {
            get => _fileExistsOption;
            set => _fileExistsOption = value;
        }

        public CopyAttributesOption CopyAttributesOptions
        {
            get => _copyAttributesOptions;
            set => _copyAttributesOptions = value;
        }

        public string RenameMask
        {
            get => _renameMask;
            set => _renameMask = value;
        }

        private void ShowError(string message)
        {
            if (GlobalSettings.SkipFileOpError)
            {
                if (GlobalSettings.LogOptions.HasFlag(LogOption.Error))
                {
                    Logger.Write(_operationThread, message, LogOption.Error, true);
                }
            }
            else
            {
                if (_askQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                    FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
                {
                    _abortOperation();
                }
            }
        }

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            if (logMsgType == LogOption.Error && !GlobalSettings.LogOptions.HasFlag(LogOption.Error)) return;
            if (logMsgType == LogOption.Info && !GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
            if (logMsgType == LogOption.Success && !GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(_operationThread, message, logMsgType);
            }
        }
    }
	public struct WfxFileTime
	{
		public long Time;
		public uint dwLowDateTime;
		public uint dwHighDateTime;
		public static WfxFileTime FromWinFileTime(long winFileTime)
		{
			return new WfxFileTime
			{
				dwLowDateTime = (uint)(winFileTime & 0xFFFFFFFF),
				dwHighDateTime = (uint)(winFileTime >> 32)
			};
		}
		public long ToWinFileTime()
		{
			return ((long)dwHighDateTime << 32) | (uint)dwLowDateTime;
		}
	}
	public class RemoteFileEntry : FileEntry 
	{
		public int SizeLow;
		public int SizeHigh;
		public int Attr;
		public WfxFileTime LastWriteTime;
		public string Path;
		public string Name;
		public RemoteFileEntry()
		{
			Path = string.Empty;
			Name = string.Empty;
			SizeLow = 0;
			SizeHigh = 0;
			Attr = 0;
			LastWriteTime = new WfxFileTime();
		}
	}
	[Flags]
	public enum FsCopyFlags : int
	{
		None = 0,
		Overwrite = 1,
		NoConfirmMkDir = 2,
		NoConfirm = 4,
		NoRecursion = 8,
		Move = 16,
		DeleteSourceFiles = 32,
		DeleteSourceDirs = 64,
		DeleteEmptyDirs = 128,
		DeleteEmptySourceDirs = 256,
		DeleteEmptyTargetDirs = 512,
		DeleteEmptyTargetDir = 1024
	}

	public enum FsFileResult
	{
		Ok,
		Error
	}
	public static class WfxPluginUtil
    {
        public static bool WfxRenameFile(IWfxPluginFileSource fileSource, FileEntry file, string newFileName)
        {
            var remoteInfo = new RemoteFileEntry
            {
                SizeLow = (int)(file.Size & 0xFFFFFFFF),
                SizeHigh = (int)(file.Size >> 32),
                Attr = (int)file.Attributes,
                LastWriteTime = DateTimeToWfxFileTime(file.ModificationTime)
            };

            return fileSource.WfxCopyMove(file.Path + file.Name, file.Path + newFileName,
                FsCopyFlags.Move, remoteInfo, true, true) == FsFileResult.Ok;
        }

        public static DateTime WfxFileTimeToDateTime(WfxFileTime fileTime)
        {
            const double NULL_DATE_TIME = 2958466.0;
            if (fileTime.dwLowDateTime == 0xFFFFFFFE && fileTime.dwHighDateTime == 0xFFFFFFFF)
                return DateTime.FromOADate(NULL_DATE_TIME);
            if (fileTime.ToWinFileTime() == 0)
                return DateTime.FromOADate(NULL_DATE_TIME);
            return DateTime.FromFileTime(fileTime.ToWinFileTime());
        }

        public static WfxFileTime DateTimeToWfxFileTime(DateTime dateTime)
        {
            if (dateTime <= DateTime.MaxValue)
                return WfxFileTime.FromWinFileTime(dateTime.ToFileTime());
            return new WfxFileTime { dwLowDateTime = 0xFFFFFFFE, dwHighDateTime = 0xFFFFFFFF };
        }
    }
} 