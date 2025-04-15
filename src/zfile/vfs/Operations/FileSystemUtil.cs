using FileSystemOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Zfile.Operations;
using Zfile;

namespace Files.FileSources.FileSystem
{
	public static class FileSystemUtil
	{
		private const string HASH_TYPE = "HASH_BEST";

		public static string ApplyRenameMask(File file, string nameMask, string extMask)
		{
			// 只对文件进行重命名
			if (file.IsDirectory || file.IsLink)
				return file.Name;
			else
				return ApplyRenameMask(file.Name, nameMask, extMask);
		}

		public static void FillAndCount(
			Files files,
			bool countDirs,
			bool excludeRootDir,
			out Files newFiles,
			out long filesCount,
			out long filesSize)
		{
			filesCount = 0;
			filesSize = 0;

			if (excludeRootDir)
			{
				if (files.Count != 1)
					throw new Exception("Only a single directory can be set with ExcludeRootDir=True");

				newFiles = new Files(files[0].FullPath);
				FillAndCountRec(files[0].FullPath + Path.DirectorySeparatorChar);
			}
			else
			{
				newFiles = new Files(files.Path);
				foreach (var file in files)
				{
					newFiles.Add(file);
					if (file.IsLink)
					{
						// 处理链接文件
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							filesCount++;
						FillAndCountRec(file.FullPath + Path.DirectorySeparatorChar);
					}
					else
					{
						filesSize += file.Size;
						filesCount++;
					}
				}
			}

			void FillAndCountRec(string srcPath)
			{
				var searchResults = Directory.GetFileSystemEntries(srcPath);
				foreach (var entry in searchResults)
				{
					if (entry == "." || entry == "..") continue;

					var file = FileSystemFileSource.CreateFile(srcPath, entry);
					newFiles.Add(file);

					if (file.IsLink)
					{
						// 处理链接文件
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							filesCount++;
						FillAndCountRec(Path.Combine(srcPath, Path.GetFileName(entry)));
					}
					else
					{
						filesSize += file.Size;
						filesCount++;
					}
				}
			}
		}

		public static string FileExistsMessage(string targetName, string sourceName, long sourceSize, DateTime sourceTime)
		{
			return string.Format(Resources.FileExistsMessage,
				targetName,
				sourceName,
				sourceSize,
				sourceTime);
		}
	}

	public delegate void UpdateStatisticsFunction(ref FileSourceCopyOperationStatistics newStatistics);

	public enum FileSystemOperationTargetExistsResult
	{
		NotExists,
		Deleted,
		AddToTarget,
		Resume,
		Skip,
		Renamed
	}

	public enum FileSystemOperationHelperMode
	{
		Copy,
		Move
	}

	public enum FileSystemOperationHelperCopyMode
	{
		Default,
		Append,
		Resume
	}

	public delegate bool FileSystemOperationHelperMoveOrCopy(
		File sourceFile,
		string targetFileName,
		FileSystemOperationHelperCopyMode mode);

	public class FileSystemTreeBuilder : FileSourceTreeBuilder
	{
		protected override void AddLinkTarget(File file, FileTreeNode currentNode)
		{
			// 实现链接目标的添加
		}

		protected override void AddFilesInDirectory(string srcPath, FileTreeNode currentNode)
		{
			// 实现目录中文件的添加
		}
	}

	public class FileSystemOperationHelper : IDisposable
	{
		private Thread _operationThread;
		private FileSystemOperationHelperMode _mode;
		private IntPtr _buffer;
		private uint _bufferSize;
		private string _rootTargetPath;
		private string _renameMask;
		private string _renameNameMask;
		private string _renameExtMask;
		private FileSourceOperationOptionSetPropertyError _setPropertyError;
		private FileSourceCopyOperationStatistics _statistics;
		private Description _description;
		private string _logCaption;
		private bool _renamingFiles;
		private bool _renamingRootDir;
		private File _rootDir;
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
		private CopyAttributesOptions _copyAttributesOptions;
		private FileSourceOperationUIResponse _maxPathOption;
		private FileSourceOperationOptionGeneral _copyOnWrite;
		private FileSourceOperationUIResponse _deleteFileOption;
		private FileSourceOperationOptionFileExists _fileExistsOption;
		private FileSourceOperationOptionDirectoryExists _dirExistsOption;

		private File _currentFile;
		private string _currentTargetFilePath;

		private AskQuestionFunction _askQuestion;
		private AbortOperationFunction _abortOperation;
		private CheckOperationStateFunction _checkOperationState;
		private UpdateStatisticsFunction _updateStatistics;
		private AppProcessMessagesFunction _appProcessMessages;
		private ShowCompareFilesUIFunction _showCompareFilesUI;
		private FileSystemOperationHelperMoveOrCopy _moveOrCopy;

		public FileSystemOperationHelper(
			AskQuestionFunction askQuestionFunction,
			AbortOperationFunction abortOperationFunction,
			AppProcessMessagesFunction appProcessMessagesFunction,
			CheckOperationStateFunction checkOperationStateFunction,
			UpdateStatisticsFunction updateStatisticsFunction,
			ShowCompareFilesUIFunction showCompareFilesUIFunction,
			Thread operationThread,
			FileSystemOperationHelperMode mode,
			string targetPath,
			FileSourceCopyOperationStatistics startingStatistics)
		{
			_askQuestion = askQuestionFunction;
			_abortOperation = abortOperationFunction;
			_appProcessMessages = appProcessMessagesFunction;
			_checkOperationState = checkOperationStateFunction;
			_updateStatistics = updateStatisticsFunction;
			_showCompareFilesUI = showCompareFilesUIFunction;
			_operationThread = operationThread;
			_mode = mode;
			_rootTargetPath = targetPath;
			_statistics = startingStatistics;
		}

		public void Initialize()
		{
			// 初始化操作
		}

		public void ProcessTree(FileTree fileTree)
		{
			// 处理文件树
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
		public CopyAttributesOptions CopyAttributesOptions { get; set; }
		public bool CorrectSymLinks { get; set; }
		public string RenameMask { get; set; }

		private void ShowError(string message)
		{
			// 显示错误信息
		}

		private void LogMessage(string message, LogOptions logOptions, LogMsgType logMsgType)
		{
			// 记录日志信息
		}

		private bool DeleteFile(File sourceFile)
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

		private bool CopyFile(File sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// 复制文件
			return true;
		}

		private bool MoveFile(File sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// 移动文件
			return true;
		}

		private void CopyProperties(File sourceFile, string targetFileName)
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
			File file,
			string absoluteTargetFileName,
			bool allowCopyInto,
			bool allowDelete)
		{
			// 检查目录是否存在
			return FileSourceOperationOptionDirectoryExists.None;
		}

		private void QuestionActionHandler(FileSourceOperationUIAction action)
		{
			// 处理问题操作
		}

		private FileSourceOperationOptionFileExists FileExists(
			File file,
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
	}
}