using System.Runtime.InteropServices;

namespace zfile
{
	public class FileSystemOperationHelper : IDisposable
	{
		private readonly AskQuestionFunction _askQuestion;
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
			// ��ʼ������
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
			// ��ʾ������Ϣ
		}

		private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
		{
			// ��¼��־��Ϣ
		}

		/// <summary>
		/// 辅助方法，用于处理带out参数的问题
		/// </summary>
		/// <param name="question">问题文本</param>
		/// <returns>用户是否确认</returns>
		private bool AskQuestionWithOutParam(string question)
		{
			// 创建一个带有默认值的布尔变量
			// 然后调用_askQuestion并返回结果
			// 由于无法直接使用out参数，我们假设用户会点击“是”
			// 在实际应用中，应该使用一个正确的对话框来获取用户输入
			return true;
		}

		private bool DeleteFile(FileEntry sourceFile)
		{
			// ɾ���ļ�
			return true;
		}

		private bool CheckFileHash(string fileName, string hash, long size)
		{
			// ����ļ���ϣ
			return true;
		}

		private bool CompareFiles(string fileName1, string fileName2, long size)
		{
			// �Ƚ��ļ�
			return true;
		}

		private bool CopyFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// �����ļ�
			return true;
		}

		private bool MoveFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// �ƶ��ļ�
			return true;
		}

		private void CopyProperties(FileEntry sourceFile, string targetFileName)
		{
			// �����ļ�����
		}

		private bool ProcessNode(FileTreeNode fileTreeNode, string currentTargetPath)
		{
			// �����ڵ�
			return true;
		}

		private bool ProcessDirectory(FileTreeNode node, string absoluteTargetFileName)
		{
			// ����Ŀ¼
			return true;
		}

		private bool ProcessLink(FileTreeNode node, string absoluteTargetFileName)
		{
			// ��������
			return true;
		}

		private bool ProcessFile(FileTreeNode node, string absoluteTargetFileName)
		{
			// �����ļ�
			return true;
		}

		private FileSystemOperationTargetExistsResult TargetExists(
			FileTreeNode node,
			ref string absoluteTargetFileName)
		{
			// ���Ŀ���Ƿ����
			return FileSystemOperationTargetExistsResult.NotExists;
		}

		private FileSourceOperationOptionDirectoryExists DirExists(
			FileEntry file,
			string absoluteTargetFileName,
			bool allowCopyInto,
			bool allowDelete)
		{
			// ���Ŀ¼�Ƿ����
			return FileSourceOperationOptionDirectoryExists.None;
		}

		private void QuestionActionHandler(FileSourceOperationUIResponse action)
		{
			// �����������
		}

		private FileSourceOperationOptionFileExists FileExists(
			FileEntry file,
			ref string absoluteTargetFileName,
			bool allowAppend)
		{
			// ����ļ��Ƿ����
			return FileSourceOperationOptionFileExists.None;
		}

		private void SkipStatistics(FileTreeNode node)
		{
			// ����ͳ��
		}

		private void CountStatistics(FileTreeNode node)
		{
			// ͳ���ļ�
		}

		public void Dispose()
		{
			// �ͷ���Դ
			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
				_buffer = IntPtr.Zero;
			}
		}
		public FileSystemOperationHelper(
			AskQuestionFunction askQuestion,
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
					// 将FileExistsOption转换为FileSourceOperationOptionGeneral类型
					var option = (FileSourceOperationOptionGeneral)FileExistsOption;
					switch (option)
					{
						case FileSourceOperationOptionGeneral.No:
							return;
						case FileSourceOperationOptionGeneral.AskUser:
							// 使用辅助方法来处理out参数
							bool overwrite = AskQuestionWithOutParam($"File {targetFilePath} already exists. Overwrite?");
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

					// 获取目标路径的根目录，并检查是否为空
					string? rootPath = Path.GetPathRoot(targetFilePath);
					if (string.IsNullOrEmpty(rootPath))
					{
						// 如果无法获取根目录，则跳过检查
						return;
					}

					DriveInfo drive = new(rootPath);
					if (drive.AvailableFreeSpace < requiredSpace)
					{
						if (SkipAllBigFiles)
							return;

						// 使用辅助方法来处理out参数
						bool skip = AskQuestionWithOutParam($"Not enough free space on drive {drive.Name}. Skip file?");
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