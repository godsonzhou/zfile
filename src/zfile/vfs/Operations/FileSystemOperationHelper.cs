using System.Runtime.InteropServices;

namespace zfile
{
    public class FileSystemOperationHelper : IDisposable
    {
        private readonly Action<string, bool> _askQuestion;
        private readonly Action _raiseAbortOperation;
        private readonly Action _appProcessMessages;
        private readonly Action _checkOperationState;
        private readonly Action<FileSourceCopyOperationStatistics> _updateStatistics;
        private readonly Action<string, string> _showCompareFilesUI;
        private readonly Thread _thread;
        private readonly FileSourceOperationHelperMode _mode;
        private readonly string _targetPath;
        private FileSourceCopyOperationStatistics _statistics;

        //public bool Verify { get; set; }
        //public string RenameMask { get; set; }
        //public FileSourceOperationOptionGeneral CopyOnWrite { get; set; }
        //public bool ReserveSpace { get; set; }
        //public bool CheckFreeSpace { get; set; }
        //public CopyAttributesOption CopyAttributesOptions { get; set; }
        //public bool SkipAllBigFiles { get; set; }
        public bool AutoRenameItself { get; set; }
		//public bool CorrectSymLinks { get; set; }
		//public FileSourceOperationOptionGeneral FileExistsOption { get; set; }
		//public FileSourceOperationOptionGeneral DirExistsOption { get; set; }
		//public FileSourceOperationOptionSetPropertyError SetPropertyError { get; set; }
		private Thread _operationThread;
		//private FileSystemOperationHelperMode _mode;
		private IntPtr _buffer;
		private uint _bufferSize;
		private string _rootTargetPath;
		private string _renameMask;
		private string _renameNameMask;
		private string _renameExtMask;
		private FileSourceOperationOptionSetPropertyError _setPropertyError;
		//private FileSourceCopyOperationStatistics _statistics;
		private Description _description;
		private string _logCaption;
		private bool _renamingFiles;
		private bool _renamingRootDir;
		private FileEntry _rootDir;
		private bool _verify;
		private bool _reserveSpace;
		private bool _checkFreeSpace;
		private bool _skipAllBigFiles;
		private bool _skipAllSpecialFiles;
		private bool _skipRenameError;
		private bool _skipOpenForReadingError;
		private bool _skipOpenForWritingError;
		private bool _skipReadError;
		private bool _skipWriteError;
		private bool _skipCopyError;
		private bool _autoRenameItSelf;
		private bool _correctSymLinks;
		private CopyAttributesOption _copyAttributesOptions;
		private FileSourceOperationUIResponse _maxPathOption;
		private FileSourceOperationOptionGeneral _copyOnWrite;
		private FileSourceOperationUIResponse _deleteFileOption;
		private FileSourceOperationOptionFileExists _fileExistsOption;
		private FileSourceOperationOptionDirectoryExists _dirExistsOption;

		private FileEntry _currentFile;
		private string _currentTargetFilePath;

		//private AskQuestionFunction _askQuestion;
		private Action _abortOperation;
		//private CheckOperationStateFunction _checkOperationState;
		//private UpdateStatisticsFunction _updateStatistics;
		//private AppProcessMessagesFunction _appProcessMessages;
		//private ShowCompareFilesUIFunction _showCompareFilesUI;
		private FileSystemOperationHelperMoveOrCopy _moveOrCopy;

		//public FileSystemOperationHelper(
		//	AskQuestionFunction askQuestionFunction,
		//	AbortOperationFunction abortOperationFunction,
		//	AppProcessMessagesFunction appProcessMessagesFunction,
		//	CheckOperationStateFunction checkOperationStateFunction,
		//	UpdateStatisticsFunction updateStatisticsFunction,
		//	ShowCompareFilesUIFunction showCompareFilesUIFunction,
		//	Thread operationThread,
		//	FileSystemOperationHelperMode mode,
		//	string targetPath,
		//	FileSourceCopyOperationStatistics startingStatistics)
		//{
		//	_askQuestion = askQuestionFunction;
		//	_abortOperation = abortOperationFunction;
		//	_appProcessMessages = appProcessMessagesFunction;
		//	_checkOperationState = checkOperationStateFunction;
		//	_updateStatistics = updateStatisticsFunction;
		//	_showCompareFilesUI = showCompareFilesUIFunction;
		//	_operationThread = operationThread;
		//	_mode = mode;
		//	_rootTargetPath = targetPath;
		//	_statistics = startingStatistics;
		//}

		public void Initialize()
		{
			// 初始化操作
		}

		public bool Verify { get; set; }
		public FileSourceOperationOptionGeneral CopyOnWrite { get; set; }
		public FileSourceOperationOptionFileExists FileExistsOption { get; set; }
		public FileSourceOperationOptionDirectoryExists DirExistsOption { get; set; }
		public bool CheckFreeSpace { get; set; }
		public bool ReserveSpace { get; set; }
		public FileSourceOperationOptionSetPropertyError SetPropertyError { get; set; }
		public bool SkipAllBigFiles { get; set; }
		public bool AutoRenameItSelf { get; set; }
		public CopyAttributesOption CopyAttributesOptions { get; set; }
		public bool CorrectSymLinks { get; set; }
		public string RenameMask { get; set; }

		private void ShowError(string message)
		{
			// 显示错误信息
		}

		private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
		{
			// 记录日志信息
		}

		private bool DeleteFile(FileEntry sourceFile)
		{
			// 删除文件
			return true;
		}

		private bool CheckFileHash(string fileName, string hash, long size)
		{
			// 检查文件哈希
			return true;
		}

		private bool CompareFiles(string fileName1, string fileName2, long size)
		{
			// 比较文件
			return true;
		}

		private bool CopyFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// 复制文件
			return true;
		}

		private bool MoveFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// 移动文件
			return true;
		}

		private void CopyProperties(FileEntry sourceFile, string targetFileName)
		{
			// 复制文件属性
		}

		private bool ProcessNode(FileTreeNode fileTreeNode, string currentTargetPath)
		{
			// 处理节点
			return true;
		}

		private bool ProcessDirectory(FileTreeNode node, string absoluteTargetFileName)
		{
			// 处理目录
			return true;
		}

		private bool ProcessLink(FileTreeNode node, string absoluteTargetFileName)
		{
			// 处理链接
			return true;
		}

		private bool ProcessFile(FileTreeNode node, string absoluteTargetFileName)
		{
			// 处理文件
			return true;
		}

		private FileSystemOperationTargetExistsResult TargetExists(
			FileTreeNode node,
			ref string absoluteTargetFileName)
		{
			// 检查目标是否存在
			return FileSystemOperationTargetExistsResult.NotExists;
		}

		private FileSourceOperationOptionDirectoryExists DirExists(
			FileEntry file,
			string absoluteTargetFileName,
			bool allowCopyInto,
			bool allowDelete)
		{
			// 检查目录是否存在
			return FileSourceOperationOptionDirectoryExists.None;
		}

		private void QuestionActionHandler(FileSourceOperationUIResponse action)
		{
			// 处理问题操作
		}

		private FileSourceOperationOptionFileExists FileExists(
			FileEntry file,
			ref string absoluteTargetFileName,
			bool allowAppend)
		{
			// 检查文件是否存在
			return FileSourceOperationOptionFileExists.None;
		}

		private void SkipStatistics(FileTreeNode node)
		{
			// 跳过统计
		}

		private void CountStatistics(FileTreeNode node)
		{
			// 统计文件
		}

		public void Dispose()
		{
			// 释放资源
			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
				_buffer = IntPtr.Zero;
			}
		}
		public FileSystemOperationHelper(
            Action<string, bool> askQuestion,
            Action raiseAbortOperation,
            Action appProcessMessages,
            Action checkOperationState,
            Action<FileSourceCopyOperationStatistics> updateStatistics,
            Action<string, string> showCompareFilesUI,
            Thread thread,
            FileSourceOperationHelperMode mode,
            string targetPath,
            FileSourceCopyOperationStatistics statistics)
        {
            _askQuestion = askQuestion;
            _raiseAbortOperation = raiseAbortOperation;
            _appProcessMessages = appProcessMessages;
            _checkOperationState = checkOperationState;
            _updateStatistics = updateStatistics;
            _showCompareFilesUI = showCompareFilesUI;
            _thread = thread;
            _mode = mode;
            _targetPath = targetPath;
            _statistics = statistics;
        }

      

        public void ProcessTree(FileTree tree)
        {
            if (tree == null)
                return;

            ProcessFiles(tree.Files);
            foreach (var subNode in tree.SubNodes)
            {
                _checkOperationState();
                ProcessTree(subNode);
            }
        }

        private void ProcessFiles(FileEntries files)
        {
            foreach (var file in files)
            {
                _checkOperationState();
                ProcessFile(file);
            }
        }

        private void ProcessFile(FileEntry file)
        {
            try
            {
                string targetFilePath = Path.Combine(_targetPath, file.Name);
                bool fileExists = File.Exists(targetFilePath);

                if (fileExists)
                {
                    switch (FileExistsOption)
                    {
                        case FileSourceOperationOptionGeneral.No:
                            return;
                        case FileSourceOperationOptionGeneral.AskUser:
                            bool overwrite;
                            _askQuestion($"File {targetFilePath} already exists. Overwrite?", out overwrite);
                            if (!overwrite)
                                return;
                            break;
                    }
                }

                if (CheckFreeSpace)
                {
                    long requiredSpace = file.Size;
                    if (Verify)
                        requiredSpace *= 2;
                    if (ReserveSpace)
                        requiredSpace *= 2;

                    DriveInfo drive = new DriveInfo(Path.GetPathRoot(targetFilePath));
                    if (drive.AvailableFreeSpace < requiredSpace)
                    {
                        if (SkipAllBigFiles)
                            return;

                        bool skip;
                        _askQuestion($"Not enough free space on drive {drive.Name}. Skip file?", out skip);
                        if (skip)
                            return;
                    }
                }

                File.Copy(file.Name, targetFilePath, true);

                if (CopyAttributesOptions != CopyAttributesOption.None)
                {
                    try
                    {
                        if ((CopyAttributesOptions & CopyAttributesOption.CopyAttributes) != 0)
                            File.SetAttributes(targetFilePath, file.Attributes);
                        if ((CopyAttributesOptions & CopyAttributesOption.CopyTime) != 0)
                        {
                            File.SetCreationTime(targetFilePath, file.CreationTime);
                            File.SetLastWriteTime(targetFilePath, file.ModificationTime);
                            File.SetLastAccessTime(targetFilePath, file.LastAccessTime);
                        }
                    }
                    catch (Exception)
                    {
                        if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Abort)
                            _raiseAbortOperation();
                        else if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Skip)
                            return;
                    }
                }

                if (Verify)
                {
                    _showCompareFilesUI(file.FullName, targetFilePath);
                }

                _statistics.DoneFiles++;
                _statistics.DoneBytes += file.Size;
                _updateStatistics(_statistics);
            }
            catch (Exception)
            {
                _statistics.FailedFiles++;
                _statistics.FailedBytes += file.Size;
                _updateStatistics(_statistics);
            }
        }

      
    }
} 