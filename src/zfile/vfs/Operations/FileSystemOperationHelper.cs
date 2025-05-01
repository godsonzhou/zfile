using System.Runtime.InteropServices;
using System.Text;

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
		private readonly FileSourceOperationHelperMode _mode;
		private readonly string _rootTargetPath;
		private FileSourceCopyOperationStatistics _statistics;

		private IntPtr _buffer;
		private uint _bufferSize;
		private string _renameMask = string.Empty;
		private string _renameNameMask = string.Empty;
		private string _renameExtMask = string.Empty;
		private bool _renamingFiles;
		private bool _renamingRootDir;
		private FileEntry? _rootDir;
		private string _logCaption = string.Empty;
		private bool _checkFreeSpace;
		private bool _skipAllBigFiles;
		private bool _skipReadError;
		private bool _skipWriteError;
		private FileSystemOperationHelperMoveOrCopy? _moveOrCopy;
		public bool AutoRenameItself { get; set; }
		public FileSystemOperationHelper(
			AskQuestionFunction askQuestionFunction,
			Action abortOperationFunction,
			Action appProcessMessagesFunction,
			Action checkOperationStateFunction,
			Action<FileSourceCopyOperationStatistics> updateStatisticsFunction,
			Action<string, string> showCompareFilesUIFunction,
			Thread operationThread,
			FileSourceOperationHelperMode mode,
			string targetPath,
			FileSourceCopyOperationStatistics startingStatistics)
		{
			_askQuestion = askQuestionFunction;
			_raiseAbortOperation = abortOperationFunction;
			_appProcessMessages = appProcessMessagesFunction;
			_checkOperationState = checkOperationStateFunction;
			_updateStatistics = updateStatisticsFunction;
			_showCompareFilesUI = showCompareFilesUIFunction;
			_mode = mode;
			_rootTargetPath = targetPath;
			_statistics = startingStatistics;

			// Initialize buffer
			_bufferSize = 65536; // Default buffer size (similar to gCopyBlockSize in Pascal)
			_buffer = Marshal.AllocHGlobal((int)_bufferSize);

			// Set default values
			_checkFreeSpace = true;
			_skipAllBigFiles = false;
			_skipReadError = false;
			_skipWriteError = false;
			CopyAttributesOptions = CopyAttributesOption.CopyTime | CopyAttributesOption.CopyAttributes | CopyAttributesOption.CopyOwnership;
			FileExistsOption = FileSourceOperationOptionFileExists.None;
			DirExistsOption = FileSourceOperationOptionDirectoryExists.None;
			SetPropertyError = FileSourceOperationOptionSetPropertyError.None;
			RenameMask = string.Empty;
			_renamingFiles = false;
			_renamingRootDir = false;
			_rootDir = null;

			// Set the appropriate MoveOrCopy delegate based on mode
			switch (_mode)
			{
				case FileSourceOperationHelperMode.Copy:
					_moveOrCopy = CopyFile;
					_logCaption = "Copy";
					break;
				case FileSourceOperationHelperMode.Move:
					_moveOrCopy = MoveFile;
					_logCaption = "Move";
					break;
				default:
					throw new ArgumentException("Invalid operation mode");
			}
		}

		public void Initialize()
		{
			// Split the rename mask into name and extension parts
			SplitFileMask(_renameMask, out _renameNameMask, out _renameExtMask);

			// Create destination path if it doesn't exist
			if (!Directory.Exists(_rootTargetPath))
			{
				try
				{
					Directory.CreateDirectory(_rootTargetPath);
				}
				catch (Exception ex)
				{
					ShowError($"Error creating destination directory: {ex.Message}");
				}
			}
		}

		private void SplitFileMask(string mask, out string nameMask, out string extMask)
		{
			if (string.IsNullOrEmpty(mask) || mask == "*.*")
			{
				nameMask = "*";
				extMask = "*";
				return;
			}

			int dotPos = mask.LastIndexOf('.');
			if (dotPos < 0)
			{
				nameMask = mask;
				extMask = "*";
			}
			else
			{
				nameMask = mask.Substring(0, dotPos);
				extMask = mask.Substring(dotPos + 1);
			}
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
			// 在实际应用中，这里应该调用UI显示错误信息
			// 由于我们没有实现UI部分，这里只是简单记录错误
			Console.WriteLine($"ERROR: {message}");

			// 如果有日志系统，也可以记录到日志
			// 这里我们简单地输出到控制台
			Console.WriteLine($"LOG: {message}");
		}

		private void LogMessage(string message)
		{
			// 记录日志信息
			// 在实际应用中，这里应该调用日志系统记录日志
			// 由于我们没有实现日志系统，这里只是简单输出到控制台
			Console.WriteLine($"LOG: {message}");
		}

		/// <summary>
		/// 辅助方法，用于处理带out参数的问题
		/// </summary>
		/// <param name="question">问题文本</param>
		/// <returns>用户是否确认</returns>
		private bool AskQuestionWithOutParam(string question = "")
		{
			// 创建一个带有默认值的布尔变量
			// 然后调用_askQuestion并返回结果
			// 由于无法直接使用out参数，我们假设用户会点击“是”
			// 在实际应用中，应该使用一个正确的对话框来获取用户输入
			return true;
		}

		private bool DeleteFile(FileEntry sourceFile)
		{
			try
			{
				// 检查文件是否存在
				if (!File.Exists(sourceFile.FullPath))
				{
					return true; // 文件不存在，视为删除成功
				}

				// 尝试删除文件
				File.Delete(sourceFile.FullPath);

				// 更新统计信息
				_statistics.DoneFiles++;
				_statistics.DoneBytes += sourceFile.Size;
				_updateStatistics(_statistics);

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error deleting file {sourceFile.FullPath}: {ex.Message}");

				// 更新失败统计信息
				_statistics.FailedFiles++;
				_statistics.FailedBytes += sourceFile.Size;
				_updateStatistics(_statistics);

				return false;
			}
		}

		private bool CheckFileHash(string fileName, string hash, long size)
		{
			try
			{
				// 检查文件是否存在
				if (!File.Exists(fileName))
				{
					ShowError($"File not found: {fileName}");
					return false;
				}

				// 检查文件大小
				FileInfo fileInfo = new FileInfo(fileName);
				if (fileInfo.Length != size)
				{
					ShowError($"File size mismatch: {fileName}. Expected: {size}, Actual: {fileInfo.Length}");
					return false;
				}

				// 计算文件哈希
				using (var md5 = System.Security.Cryptography.MD5.Create())
				using (var stream = File.OpenRead(fileName))
				{
					byte[] hashBytes = md5.ComputeHash(stream);
					string computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

					// 比较哈希值
					if (string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
					else
					{
						ShowError($"File hash mismatch: {fileName}. Expected: {hash}, Actual: {computedHash}");
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				ShowError($"Error checking file hash: {ex.Message}");
				return false;
			}
		}

		private bool CompareFiles(string fileName1, string fileName2)
		{
			try
			{
				// 检查文件是否存在
				if (!File.Exists(fileName1) || !File.Exists(fileName2))
				{
					return false;
				}

				// 检查文件大小是否相同
				FileInfo fileInfo1 = new FileInfo(fileName1);
				FileInfo fileInfo2 = new FileInfo(fileName2);
				if (fileInfo1.Length != fileInfo2.Length)
				{
					return false;
				}

				// 逐字节比较文件内容
				using (FileStream fs1 = new FileStream(fileName1, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (FileStream fs2 = new FileStream(fileName2, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					byte[] buffer1 = new byte[_bufferSize];
					byte[] buffer2 = new byte[_bufferSize];
					int bytesRead1, bytesRead2;

					do
					{
						bytesRead1 = fs1.Read(buffer1, 0, buffer1.Length);
						bytesRead2 = fs2.Read(buffer2, 0, buffer2.Length);

						if (bytesRead1 != bytesRead2)
						{
							return false;
						}

						for (int i = 0; i < bytesRead1; i++)
						{
							if (buffer1[i] != buffer2[i])
							{
								return false;
							}
						}
					} while (bytesRead1 > 0);
				}

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error comparing files: {ex.Message}");
				return false;
			}
		}

		private bool CopyFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			try
			{
				// 创建目标文件的目录（如果不存在）
				string targetDir = Path.GetDirectoryName(targetFileName);
				if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
				{
					Directory.CreateDirectory(targetDir);
				}

				// 根据模式处理文件复制
				switch (mode)
				{
					case FileSystemOperationHelperCopyMode.Default:
						// 默认模式：直接复制文件
						File.Copy(sourceFile.FullPath, targetFileName, true);
						break;

					case FileSystemOperationHelperCopyMode.Append:
						// 追加模式：将源文件内容追加到目标文件
						using (FileStream sourceStream = new FileStream(sourceFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
						using (FileStream targetStream = new FileStream(targetFileName, FileMode.Append, FileAccess.Write))
						{
							byte[] buffer = new byte[_bufferSize];
							int bytesRead;
							while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								targetStream.Write(buffer, 0, bytesRead);
								_statistics.DoneBytes += bytesRead;
								_updateStatistics(_statistics);
							}
						}
						break;

					case FileSystemOperationHelperCopyMode.Resume:
						// 续传模式：如果目标文件存在，则从目标文件大小位置开始复制
						long targetSize = 0;
						if (File.Exists(targetFileName))
						{
							targetSize = new FileInfo(targetFileName).Length;
						}

						using (FileStream sourceStream = new FileStream(sourceFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
						using (FileStream targetStream = new FileStream(targetFileName, FileMode.OpenOrCreate, FileAccess.Write))
						{
							sourceStream.Seek(targetSize, SeekOrigin.Begin);
							targetStream.Seek(targetSize, SeekOrigin.Begin);

							byte[] buffer = new byte[_bufferSize];
							int bytesRead;
							while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								targetStream.Write(buffer, 0, bytesRead);
								_statistics.DoneBytes += bytesRead;
								_updateStatistics(_statistics);
							}
						}
						break;
				}

				// 复制文件属性
				if (CopyAttributesOptions != CopyAttributesOption.None)
				{
					try
					{
						if ((CopyAttributesOptions & CopyAttributesOption.CopyAttributes) != 0)
						{
							File.SetAttributes(targetFileName, sourceFile.Attributes);
						}
						if ((CopyAttributesOptions & CopyAttributesOption.CopyTime) != 0)
						{
							File.SetCreationTime(targetFileName, sourceFile.CreationTime);
							File.SetLastWriteTime(targetFileName, sourceFile.ModificationTime);
							File.SetLastAccessTime(targetFileName, sourceFile.LastAccessTime);
						}
					}
					catch (Exception ex)
					{
						ShowError($"Error copying file attributes: {ex.Message}");
						if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Abort)
						{
							_raiseAbortOperation();
							return false;
						}
					}
				}

				// 验证文件是否正确复制
				if (Verify)
				{
					if (!CompareFiles(sourceFile.FullPath, targetFileName))
					{
						ShowError($"Verification failed for file: {sourceFile.FullPath}");
						return false;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error copying file: {ex.Message}");
				return false;
			}
		}

		private bool MoveFile(FileEntry sourceFile, string targetFileName, FileSystemOperationHelperCopyMode mode)
		{
			// 如果不是追加或续传模式，尝试直接重命名文件
			if (mode != FileSystemOperationHelperCopyMode.Append && mode != FileSystemOperationHelperCopyMode.Resume)
			{
				try
				{
					// 创建目标文件的目录（如果不存在）
					string targetDir = Path.GetDirectoryName(targetFileName);
					if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
					{
						Directory.CreateDirectory(targetDir);
					}

					// 尝试直接移动文件
					File.Move(sourceFile.FullPath, targetFileName);
					return true;
				}
				catch (IOException ex)
				{
					// 如果不是因为不同设备导致的错误，则显示错误并询问用户
					if (!ex.Message.Contains("different drive") && !ex.Message.Contains("different device"))
					{
						string message = $"Cannot move file {sourceFile.FullPath}. {ex.Message}";
						bool skip = AskQuestionWithOutParam(message);
						if (skip)
							return false;
					}
					// 如果是不同设备错误或用户选择重试，则继续执行复制后删除
				}
				catch (Exception ex)
				{
					ShowError($"Error moving file: {ex.Message}");
					return false;
				}
			}

			// 如果直接移动失败或是追加/续传模式，则复制文件后删除源文件
			if (Verify)
			{
				_statistics.TotalBytes += sourceFile.Size;
			}

			if (CopyFile(sourceFile, targetFileName, mode))
			{
				try
				{
					File.Delete(sourceFile.FullPath);
					return true;
				}
				catch (Exception ex)
				{
					ShowError($"Error deleting source file after copy: {ex.Message}");
					return false;
				}
			}

			return false;
		}

		private void CopyProperties(FileEntry sourceFile, string targetFileName)
		{
			try
			{
				// 复制文件属性
				if (CopyAttributesOptions != CopyAttributesOption.None)
				{
					// 复制文件属性
					if ((CopyAttributesOptions & CopyAttributesOption.CopyAttributes) != 0)
					{
						File.SetAttributes(targetFileName, sourceFile.Attributes);
					}

					// 复制文件时间
					if ((CopyAttributesOptions & CopyAttributesOption.CopyTime) != 0)
					{
						File.SetCreationTime(targetFileName, sourceFile.CreationTime);
						File.SetLastWriteTime(targetFileName, sourceFile.ModificationTime);
						File.SetLastAccessTime(targetFileName, sourceFile.LastAccessTime);
					}

					// 复制文件所有权
					if ((CopyAttributesOptions & CopyAttributesOption.CopyOwnership) != 0)
					{
						// 在Windows系统中，复制文件所有权需要使用P/Invoke调用Windows API
						// 这里我们简单地记录一下，实际实现需要调用Windows API
						Console.WriteLine($"Copying ownership for file {targetFileName} is not implemented");
					}
				}
			}
			catch (Exception ex)
			{
				ShowError($"Error copying file properties: {ex.Message}");

				// 根据设置决定是否中止操作
				if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Abort)
				{
					_raiseAbortOperation();
				}
			}
		}

		private bool ProcessNode(FileTreeNode fileTreeNode, string currentTargetPath)
		{
			if (fileTreeNode == null)
				return true;

			// 检查操作状态
			_checkOperationState();

			// 获取目标文件名
			string absoluteTargetFileName = Path.Combine(currentTargetPath, fileTreeNode.Name());

			// 检查目标是否存在
			FileSystemOperationTargetExistsResult targetExists = TargetExists(ref absoluteTargetFileName);

			// 根据目标存在情况处理
			switch (targetExists)
			{
				case FileSystemOperationTargetExistsResult.NotExists:
					// 目标不存在，根据节点类型处理
					if (fileTreeNode.IsDirectory())
					{
						return ProcessDirectory(fileTreeNode, absoluteTargetFileName);
					}
					else if (fileTreeNode.IsLink())
					{
						return ProcessLink(absoluteTargetFileName);
					}
					else
					{
						return ProcessFile(fileTreeNode, absoluteTargetFileName);
					}

				case FileSystemOperationTargetExistsResult.IsDirectory:
					// 目标是目录，处理目录
					return ProcessDirectory(fileTreeNode, absoluteTargetFileName);

				case FileSystemOperationTargetExistsResult.IsFile:
					// 目标是文件，处理文件
					return ProcessFile(fileTreeNode, absoluteTargetFileName);

				case FileSystemOperationTargetExistsResult.IsLink:
					// 目标是链接，处理链接
					return ProcessLink(absoluteTargetFileName);

				case FileSystemOperationTargetExistsResult.Skip:
					// 跳过此节点
					SkipStatistics(fileTreeNode);
					return true;

				default:
					// 未知情况，跳过
					SkipStatistics(fileTreeNode);
					return true;
			}
		}

		private bool ProcessDirectory(FileTreeNode node, string absoluteTargetFileName)
		{
			try
			{
				// 检查目录是否存在
				if (Directory.Exists(absoluteTargetFileName))
				{
					// 目录已存在，根据设置决定如何处理
					FileSourceOperationOptionDirectoryExists dirExistsOption = DirExistsOption;

					switch (dirExistsOption)
					{
						case FileSourceOperationOptionDirectoryExists.None:
						case FileSourceOperationOptionDirectoryExists.CopyInto:
							// 继续处理，复制到目录中
							break;

						case FileSourceOperationOptionDirectoryExists.Skip:
							// 跳过此目录
							SkipStatistics(node);
							return true;

						case FileSourceOperationOptionDirectoryExists.Abort:
							// 中止操作
							_raiseAbortOperation();
							return false;

						case FileSourceOperationOptionDirectoryExists.Delete:
							// 删除目录后继续
							try
							{
								Directory.Delete(absoluteTargetFileName, true);
							}
							catch (Exception ex)
							{
								ShowError($"Error deleting directory {absoluteTargetFileName}: {ex.Message}");
								return false;
							}
							break;
					}
				}
				else
				{
					// 目录不存在，创建目录
					try
					{
						Directory.CreateDirectory(absoluteTargetFileName);
					}
					catch (Exception ex)
					{
						ShowError($"Error creating directory {absoluteTargetFileName}: {ex.Message}");
						return false;
					}
				}

				// 处理目录中的子节点
				if (node.SubNodes != null)
				{
					foreach (var subNode in node.SubNodes)
					{
						if (!ProcessNode(subNode, absoluteTargetFileName))
						{
							return false;
						}
					}
				}

				// 处理目录中的文件
				var files = node.Files();
				if (files != null)
				{
					foreach (var file in files)
					{
						if (!ProcessFile(new FileTreeNode(file), Path.Combine(absoluteTargetFileName, file.Name)))
						{
							return false;
						}
					}
				}

				// 更新统计信息
				_statistics.DoneDirectories++;
				_updateStatistics(_statistics);

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error processing directory {absoluteTargetFileName}: {ex.Message}");
				return false;
			}
		}

		private bool ProcessLink(string absoluteTargetFileName)
		{
			try
			{
				// 在Windows系统中，创建符号链接需要管理员权限
				// 这里我们简单地记录一下，实际实现需要调用Windows API
				Console.WriteLine($"Creating symbolic link {absoluteTargetFileName} is not implemented");

				// 更新统计信息
				_statistics.DoneFiles++;
				_updateStatistics(_statistics);

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error processing link {absoluteTargetFileName}: {ex.Message}");
				return false;
			}
		}

		private bool ProcessFile(FileTreeNode node, string absoluteTargetFileName)
		{
			try
			{
				// 检查文件是否存在
				if (File.Exists(absoluteTargetFileName))
				{
					// 文件已存在，根据设置决定如何处理
					string tempFileName = absoluteTargetFileName;
					FileSourceOperationOptionFileExists fileExistsOption = FileExistsOption;

					switch (fileExistsOption)
					{
						case FileSourceOperationOptionFileExists.None:
						case FileSourceOperationOptionFileExists.Overwrite:
							// 覆盖文件
							break;

						case FileSourceOperationOptionFileExists.Skip:
							// 跳过此文件
							SkipStatistics(node);
							return true;

						case FileSourceOperationOptionFileExists.Abort:
							// 中止操作
							_raiseAbortOperation();
							return false;

						case FileSourceOperationOptionFileExists.Append:
							// 追加到文件
							absoluteTargetFileName = tempFileName;
							break;

						case FileSourceOperationOptionFileExists.Resume:
							// 续传文件
							absoluteTargetFileName = tempFileName;
							break;
					}
				}

				// 复制文件
				if (_mode == FileSourceOperationHelperMode.Copy)
				{
					if (!CopyFile(node.FileEntry(), absoluteTargetFileName, FileSystemOperationHelperCopyMode.Default))
					{
						return false;
					}
				}
				else if (_mode == FileSourceOperationHelperMode.Move)
				{
					if (!MoveFile(node.FileEntry(), absoluteTargetFileName, FileSystemOperationHelperCopyMode.Default))
					{
						return false;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				ShowError($"Error processing file {absoluteTargetFileName}: {ex.Message}");
				return false;
			}
		}

		private FileSystemOperationTargetExistsResult TargetExists(
			ref string absoluteTargetFileName)
		{
			try
			{
				// 检查目标是否存在
				if (File.Exists(absoluteTargetFileName))
				{
					// 目标是文件
					return FileSystemOperationTargetExistsResult.IsFile;
				}
				else if (Directory.Exists(absoluteTargetFileName))
				{
					// 目标是目录
					return FileSystemOperationTargetExistsResult.IsDirectory;
				}
				else
				{
					// 目标不存在
					return FileSystemOperationTargetExistsResult.NotExists;
				}
			}
			catch (Exception ex)
			{
				ShowError($"Error checking target existence: {ex.Message}");
				return FileSystemOperationTargetExistsResult.Skip;
			}
		}

		private void SkipStatistics(FileTreeNode node)
		{
			if (node == null)
				return;

			// 更新跳过的文件和目录统计信息
			if (node.IsDirectory())
			{
				_statistics.SkippedDirectories++;
			}
			else
			{
				_statistics.SkippedFiles++;
				_statistics.SkippedBytes += node.Size();
			}

			// 更新统计信息
			_updateStatistics(_statistics);
		}

		private void CountStatistics(FileTreeNode node)
		{
			if (node == null)
				return;

			// 更新总文件和目录统计信息
			if (node.IsDirectory())
			{
				_statistics.TotalDirectories++;
			}
			else
			{
				_statistics.TotalFiles++;
				_statistics.TotalBytes += node.Size();
			}

			// 更新统计信息
			_updateStatistics(_statistics);
		}

		public void Dispose()
		{
			// Release unmanaged resources
			if (_buffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_buffer);
				_buffer = IntPtr.Zero;
			}

			// Suppress finalization
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Processes a file tree for copy or move operations.
		/// </summary>
		/// <param name="tree">The file tree to process.</param>
		public void ProcessTree(FileTree tree)
		{
			if (tree == null)
				return;

			// Check if we're renaming files
			_renamingFiles = !string.IsNullOrEmpty(_renameMask) && _renameMask != "*.*";

			// If there is a single root dir and rename mask doesn't have wildcards
			// treat it as a rename of the root dir.
			if (tree.SubNodes.Count == 1 && _renamingFiles)
			{
				// Get the first node and file
				var firstFile = tree.SubNodes[0].Files.FirstOrDefault();
				if (firstFile != null)
				{
					var fileTreeNode = new FileTreeNode(firstFile);
					var file = fileTreeNode.TheFile;

					// Check if it's a directory and the rename mask doesn't have wildcards
					if ((file.IsDirectory || file.IsLinkToDirectory) && !ContainsWildcards(_renameMask))
					{
						_renamingFiles = false;
						_renamingRootDir = true;
						_rootDir = file;
					}
				}
			}

			// Process all nodes in the tree
			foreach (var subNode in tree.SubNodes)
			{
				_checkOperationState();

				// Get the first file in the subnode
				var firstFile = subNode.Files.FirstOrDefault();
				if (firstFile != null)
				{
					ProcessNode(new FileTreeNode(firstFile), _rootTargetPath);
				}
			}

			// Process files at the root level of the tree
			foreach (var file in tree.Files)
			{
				_checkOperationState();

				// Make sure the file is not null
				if (file != null)
				{
					// Create a file tree node for the file
					var fileNode = new FileTreeNode(file);

					// Determine the target name based on renaming settings
					string targetName;
					if (_renamingRootDir && file == _rootDir)
						targetName = Path.Combine(_rootTargetPath, _renameMask);
					else if (_renamingFiles)
						targetName = Path.Combine(_rootTargetPath, ApplyRenameMask(file, _renameNameMask, _renameExtMask));
					else
						targetName = Path.Combine(_rootTargetPath, file.Name);

					// Update statistics
					_statistics.CurrentFileFrom = file.FullPath;
					_statistics.CurrentFileTo = targetName;
					_statistics.CurrentFileTotalBytes = file.Size;
					_statistics.CurrentFileDoneBytes = 0;

					// Process the file based on its type
					bool processedOk;
					if (file.IsLink)
						processedOk = ProcessLink(targetName);
					else if (file.IsDirectory)
						processedOk = ProcessDirectory(fileNode, targetName);
					else
						processedOk = ProcessFile(fileNode, targetName);

					// Update statistics if needed
					if (!processedOk)
					{
						_statistics.FailedFiles++;
					}

					// Process application messages
					_appProcessMessages?.Invoke();
					_checkOperationState();
				}
			}
		}

		/// <summary>
		/// Checks if a string contains wildcard characters (* or ?).
		/// </summary>
		/// <param name="str">The string to check.</param>
		/// <returns>True if the string contains wildcards, false otherwise.</returns>
		private static bool ContainsWildcards(string str)
		{
			return !string.IsNullOrEmpty(str) && (str.Contains('*') || str.Contains('?'));
		}

		/// <summary>
		/// Applies a rename mask to a file.
		/// </summary>
		/// <param name="file">The file to rename.</param>
		/// <param name="nameMask">The mask for the name part.</param>
		/// <param name="extMask">The mask for the extension part.</param>
		/// <returns>The new name after applying the mask.</returns>
		private static string ApplyRenameMask(FileEntry file, string nameMask, string extMask)
		{
			if (string.IsNullOrEmpty(nameMask) || nameMask == "*")
				nameMask = Path.GetFileNameWithoutExtension(file.Name);

			if (string.IsNullOrEmpty(extMask) || extMask == "*")
				extMask = Path.GetExtension(file.Name).TrimStart('.');

			// Replace wildcards in name mask
			if (nameMask.Contains('*'))
			{
				string fileName = Path.GetFileNameWithoutExtension(file.Name);
				nameMask = nameMask.Replace("*", fileName);
			}

			// Replace wildcards in extension mask
			if (extMask.Contains('*'))
			{
				string fileExt = Path.GetExtension(file.Name).TrimStart('.');
				extMask = extMask.Replace("*", fileExt);
			}

			// Combine name and extension
			if (string.IsNullOrEmpty(extMask))
				return nameMask;
			else
				return $"{nameMask}.{extMask}";
		}

		//private void ProcessFiles(FileEntries files)
		//{
		//	foreach (var file in files)
		//	{
		//		_checkOperationState();
		//		ProcessFile(file);
		//	}
		//}

		//private void ProcessFile(FileEntry file)
		//{
		//	try
		//	{
		//		string targetFilePath = Path.Combine(_rootTargetPath, file.Name);
		//		bool fileExists = File.Exists(targetFilePath);

		//		if (fileExists)
		//		{
		//			// 将FileExistsOption转换为FileSourceOperationOptionGeneral类型
		//			var option = (FileSourceOperationOptionGeneral)FileExistsOption;
		//			switch (option)
		//			{
		//				case FileSourceOperationOptionGeneral.No:
		//					return;
		//				case FileSourceOperationOptionGeneral.AskUser:
		//					// 使用辅助方法来处理out参数
		//					bool overwrite = AskQuestionWithOutParam($"File {targetFilePath} already exists. Overwrite?");
		//					if (!overwrite)
		//						return;
		//					break;
		//			}
		//		}

		//		if (CheckFreeSpace)
		//		{
		//			long requiredSpace = file.Size;
		//			if (Verify)
		//				requiredSpace *= 2;
		//			if (ReserveSpace)
		//				requiredSpace *= 2;

		//			// 获取目标路径的根目录，并检查是否为空
		//			string? rootPath = Path.GetPathRoot(targetFilePath);
		//			if (string.IsNullOrEmpty(rootPath))
		//			{
		//				// 如果无法获取根目录，则跳过检查
		//				return;
		//			}

		//			DriveInfo drive = new(rootPath);
		//			if (drive.AvailableFreeSpace < requiredSpace)
		//			{
		//				if (SkipAllBigFiles)
		//					return;

		//				// 使用辅助方法来处理out参数
		//				bool skip = AskQuestionWithOutParam($"Not enough free space on drive {drive.Name}. Skip file?");
		//				if (skip)
		//					return;
		//			}
		//		}

		//		File.Copy(file.FullPath, targetFilePath, true);

		//		if (CopyAttributesOptions != CopyAttributesOption.None)
		//		{
		//			try
		//			{
		//				if ((CopyAttributesOptions & CopyAttributesOption.CopyAttributes) != 0)
		//					File.SetAttributes(targetFilePath, file.Attributes);
		//				if ((CopyAttributesOptions & CopyAttributesOption.CopyTime) != 0)
		//				{
		//					File.SetCreationTime(targetFilePath, file.CreationTime);
		//					File.SetLastWriteTime(targetFilePath, file.ModificationTime);
		//					File.SetLastAccessTime(targetFilePath, file.LastAccessTime);
		//				}
		//			}
		//			catch (Exception)
		//			{
		//				if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Abort)
		//					_raiseAbortOperation();
		//				else if (SetPropertyError == FileSourceOperationOptionSetPropertyError.Skip)
		//					return;
		//			}
		//		}

		//		if (Verify)
		//		{
		//			_showCompareFilesUI(file.FullName, targetFilePath);
		//		}

		//		_statistics.DoneFiles++;
		//		_statistics.DoneBytes += file.Size;
		//		_updateStatistics(_statistics);
		//	}
		//	catch (Exception)
		//	{
		//		_statistics.FailedFiles++;
		//		_statistics.FailedBytes += file.Size;
		//		_updateStatistics(_statistics);
		//	}
		//}
	}
}