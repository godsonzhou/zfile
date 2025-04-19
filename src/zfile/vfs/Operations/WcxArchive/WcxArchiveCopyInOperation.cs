using SharpCompress.Archives.Tar;
using SharpCompress.Writers;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace zfile
{
	public class WcxArchiveCopyInOperation : ArchiveCopyInOperation
	{
		private IWcxArchiveFileSource _wcxArchiveFileSource;
		private StringHashListUtf8 _FileEntries;
		private bool _tarBefore;
		private string? _tarFileName;
		private FileEntry? _currentFile;
		private string? _currentTargetFilePath;
		private FileEntries _fullFilesTree;
		private int _packingFlags;

		// Static variables for WCX callbacks
		private static WcxArchiveCopyInOperation _wcxCopyInOperationG = null;
		[ThreadStatic]
		private static WcxArchiveCopyInOperation _wcxCopyInOperationT;

		public WcxArchiveCopyInOperation(IFileSource sourceFileSource,
										IFileSource targetFileSource,
										FileEntries sourceFiles,
										string targetPath) : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
		{
			_wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
			_packingFlags = (int)PackFilesFlags.PK_PACK_SAVE_PATHS;
			_tarBefore = false;
			_fullFilesTree = new FileEntries();

			NeedsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_PACK) == 0;

			_FileEntries = new StringHashListUtf8(true);

			// Get initialized statistics; then we change only what is needed.
			_statistics = RetrieveStatistics();
			_statistics.DoneFiles = -1;
			_statistics.CurrentFileDoneBytes = -1;
			UpdateStatistics(_statistics);
		}

		protected override void Initialize()
		{
			// Is plugin allow multiple Operations?
			if (NeedsConnection)
				_wcxCopyInOperationG = this;
			else
				_wcxCopyInOperationT = this;

			// Gets full list of files (recursive)
			FileSystemUtil.FillAndCount(SourceFiles,
						 ref _fullFilesTree,
						 ref _statistics.TotalFiles,
						 ref _statistics.TotalBytes);

			// Need to check file existence
			if (_fileExistsOption != FileSourceOperationOptionFileExists.Overwrite)
			{
				var FileEntries = _wcxArchiveFileSource.ArchiveFileEntries.LockList();
				try
				{
					// Populate archive file list
					foreach (var item in FileEntries)
					{
						var clonedItem = item.Clone();
						_FileEntries.Add(clonedItem.FileName.ToLowerInvariant(), clonedItem);
					}
				}
				finally
				{
					_wcxArchiveFileSource.ArchiveFileEntries.UnlockList();
				}
			}
		}

		protected override void MainExecute()
		{
			// Put to TAR archive if needed
			if (_tarBefore && Tar()) return;

			var wcxModule = _wcxArchiveFileSource.WcxModule;

			string destPath = Helper.ExcludeFrontPathDelimiter(_targetPath);
			destPath = Helper.ExcludeTrailingPathDelimiter(destPath);

			_statistics.CurrentFileTo = _wcxArchiveFileSource.ArchiveFileName;
			if (_tarBefore) _statistics.CurrentFileDoneBytes = -1;
			UpdateStatistics(_statistics);

			SetProcessDataProc(WcxModule.WcxInvalidHandle);
			wcxModule.SetChangeVolProc(WcxModule.WcxInvalidHandle);

			// Convert TFiles into String
			string FileEntries = GetFileEntries(_fullFilesTree);
			// Nothing to pack (user skip all files)
			if (FileEntries == "\0") return;

			int result = wcxModule.PackFiles(
						   _wcxArchiveFileSource.ArchiveFileName,
						   destPath, // no trailing path delimiter here
						   Helper.IncludeTrailingPathDelimiter(_fullFilesTree.Path), // end with path delimiter here
						   FileEntries,
						   _packingFlags);

			// Check for errors.
			if (result != WcxModule.E_SUCCESS)
			{
				// User aborted operation.
				if (result == WcxModule.E_EABORTED) RaiseAbortOperation();

				ShowError(string.Format("Error packing to {0}: {1}",
						   _wcxArchiveFileSource.ArchiveFileName,
						   WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
			}
			else
			{
				LogMessage(string.Format("Successfully packed to {0}",
						   _wcxArchiveFileSource.ArchiveFileName), LogOption.ArcOp, LogOption.Success);

				_statistics.DoneFiles = _statistics.TotalFiles;
				UpdateStatistics(_statistics);
			}

			// Delete temporary TAR archive if needed
			if (_tarBefore) File.Delete(_tarFileName);
		}

		protected override void Finalize()
		{
			ClearCurrentOperation();
		}

		public override string GetDescription(FileSourceOperationDescriptionDetails details)
		{
			switch (details)
			{
				case FileSourceOperationDescriptionDetails.JobAndTarget:
					if (SourceFiles.Count == 1)
						return string.Format("Packing {0} to {1}", SourceFiles[0].Name, _wcxArchiveFileSource.ArchiveFileName);
					else
						return string.Format("Packing from {0} to {1}", SourceFiles.Path, _wcxArchiveFileSource.ArchiveFileName);
				default:
					return "Packing";
			}
		}

		private string GetFileEntries(FileEntries theFiles)
		{
			string result = "";
			bool archiveExists = _FileEntries.Count > 0;
			string subPath = Helper.ExcludeFrontPathDelimiter(_targetPath).ToLowerInvariant();

			foreach (var file in theFiles)
			{
				// Filenames must be relative to the current directory.
				string fileName = Helper.ExtractDirLevel(theFiles.Path, file.FullPath);

				// Special treatment of directories.
				if (file.IsDirectory)
				{
					// TC ends paths to directories to be packed with '\'.
					fileName = Helper.IncludeTrailingPathDelimiter(fileName);
				}
				// Need to check file existence
				else if (archiveExists)
				{
					var header = (WcxHeader)_FileEntries[subPath + fileName.ToLowerInvariant()];
					if (header != null)
					{
						if (FileExists(file, header) == FileSourceOperationOptionFileExists.Skip)
							continue;
					}
				}

				result += fileName + "\0";
			}

			result += "\0";
			return result;
		}

		public bool TarBefore
		{
			get { return _tarBefore; }
			set
			{
				_tarBefore = value;
				if (_tarBefore && _wcxArchiveFileSource.WcxModule._packToMem != null &&
					(_wcxArchiveFileSource.WcxModule.PluginCapabilities & (int)PackerCaps.PK_CAPS_MEMPACK) != 0)
					NeedsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_MEMPACK) == 0;
				else
					NeedsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_PACK) == 0;
			}
		}

		private void ShowError(string message, int error, LogOption logOptions = LogOption.None)
		{
			LogMessage(message, logOptions, LogOption.Error);

			if (!GlobalSettings.SkipFileOpError && error > WcxModule.E_SUCCESS)
			{
				if (AskQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
							   FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
				{
					RaiseAbortOperation();
				}
			}
		}

		private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
		{
			switch (logMsgType)
			{
				case LogOption.Error:
					if (!GlobalSettings.LogOptions.HasFlag(LogOption.Error)) return;
					break;
				case LogOption.Info:
					if (!GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
					break;
				case LogOption.Success:
					if (!GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;
					break;
			}

			if (logOptions <= GlobalSettings.LogOptions)
			{
				Logger.Write(_thread, message, logMsgType);
			}
		}

		private void DeleteFiles(FileEntries files)
		{
			for (int i = files.Count - 1; i >= 0; i--)
			{
				var file = files[i];
				if (file.IsDirectory)
					Directory.Delete(file.FullPath);
				else
					File.Delete(file.FullPath);
			}
		}

		private void SetProcessDataProc(IntPtr arcData)
		{
			if (NeedsConnection)
				_wcxArchiveFileSource.WcxModule.SetProcessDataProc(arcData, ProcessDataProcAG, ProcessDataProcWG);
			else
				_wcxArchiveFileSource.WcxModule.SetProcessDataProc(arcData, ProcessDataProcAT, ProcessDataProcWT);
		}

		private void QuestionActionHandler(FileSourceOperationUIResponse action)
		{
			if (action == FileSourceOperationUIResponse.CompareAction)
				ShowCompareFilesUI(_currentFile, Helper.IncludeFrontPathDelimiter(_currentTargetFilePath));
		}

		private string FileExistsMessage(FileEntry sourceFile, WcxHeader targetHeader)
		{
			string result = "File exists. Overwrite?\n" + targetHeader.FileName + "\n";

			result += string.Format("Size: {0}, Date: {1}\n",
								   targetHeader.UnpSize.ToString(),
								   FileTimeToDateTime(targetHeader.FileTime).ToString());

			result += "\nWith file:\n" + sourceFile.FullPath + "\n" +
					  string.Format("Size: {0}, Date: {1}",
								   sourceFile.Size.ToString(),
								   sourceFile.ModificationTime.ToString());

			return result;
		}

		private FileSourceOperationOptionFileExists FileExists(FileEntry sourceFile, WcxHeader targetHeader)
		{
			switch (_fileExistsOption)
			{
				case FileSourceOperationOptionFileExists.None:
					_currentFile = sourceFile;
					_currentTargetFilePath = targetHeader.FileName;
					var response = AskQuestion(FileExistsMessage(sourceFile, targetHeader), "",
											 new[] { FileSourceOperationUIResponse.Overwrite,
													FileSourceOperationUIResponse.Skip,
													FileSourceOperationUIResponse.OverwriteLarger,
													FileSourceOperationUIResponse.OverwriteAll,
													FileSourceOperationUIResponse.SkipAll,
													FileSourceOperationUIResponse.OverwriteSmaller,
													FileSourceOperationUIResponse.OverwriteOlder,
													FileSourceOperationUIResponse.Cancel,
													FileSourceOperationUIResponse.CompareAction },
											 FileSourceOperationUIResponse.Overwrite,
											 FileSourceOperationUIResponse.Skip,
											 QuestionActionHandler);
					switch (response)
					{
						case FileSourceOperationUIResponse.Overwrite:
							return FileSourceOperationOptionFileExists.Overwrite;
						case FileSourceOperationUIResponse.Skip:
							return FileSourceOperationOptionFileExists.Skip;
						case FileSourceOperationUIResponse.OverwriteAll:
							_fileExistsOption = FileSourceOperationOptionFileExists.Overwrite;
							return FileSourceOperationOptionFileExists.Overwrite;
						case FileSourceOperationUIResponse.SkipAll:
							_fileExistsOption = FileSourceOperationOptionFileExists.Skip;
							return FileSourceOperationOptionFileExists.Skip;
						case FileSourceOperationUIResponse.OverwriteOlder:
							_fileExistsOption = FileSourceOperationOptionFileExists.OverwriteOlder;
							return OverwriteOlder(sourceFile, targetHeader);
						case FileSourceOperationUIResponse.OverwriteSmaller:
							_fileExistsOption = FileSourceOperationOptionFileExists.OverwriteSmaller;
							return OverwriteSmaller(sourceFile, targetHeader);
						case FileSourceOperationUIResponse.OverwriteLarger:
							_fileExistsOption = FileSourceOperationOptionFileExists.OverwriteLarger;
							return OverwriteLarger(sourceFile, targetHeader);
						case FileSourceOperationUIResponse.None:
						case FileSourceOperationUIResponse.Cancel:
							RaiseAbortOperation();
							return FileSourceOperationOptionFileExists.None; // Never reached
						default:
							return FileSourceOperationOptionFileExists.None;
					}
				case FileSourceOperationOptionFileExists.OverwriteOlder:
					return OverwriteOlder(sourceFile, targetHeader);
				case FileSourceOperationOptionFileExists.OverwriteSmaller:
					return OverwriteSmaller(sourceFile, targetHeader);
				case FileSourceOperationOptionFileExists.OverwriteLarger:
					return OverwriteLarger(sourceFile, targetHeader);
				default:
					return _fileExistsOption;
			}
		}

		private FileSourceOperationOptionFileExists OverwriteOlder(FileEntry sourceFile, WcxHeader targetHeader)
		{
			if (sourceFile.ModificationTime > FileTimeToDateTime(targetHeader.FileTime))
				return FileSourceOperationOptionFileExists.Overwrite;
			else
				return FileSourceOperationOptionFileExists.Skip;
		}

		private FileSourceOperationOptionFileExists OverwriteSmaller(FileEntry sourceFile, WcxHeader targetHeader)
		{
			if (sourceFile.Size > targetHeader.UnpSize)
				return FileSourceOperationOptionFileExists.Overwrite;
			else
				return FileSourceOperationOptionFileExists.Skip;
		}

		private FileSourceOperationOptionFileExists OverwriteLarger(FileEntry sourceFile, WcxHeader targetHeader)
		{
			if (sourceFile.Size < targetHeader.UnpSize)
				return FileSourceOperationOptionFileExists.Overwrite;
			else
				return FileSourceOperationOptionFileExists.Skip;
		}

		public static void ClearCurrentOperation()
		{
			_wcxCopyInOperationG = null;
		}

		public static Type GetOptionsUIClass()
		{
			return typeof(WcxArchiveCopyInOperationOptionsUI);
		}

		private bool Tar()
		{
			TarWriter tarWriter = null;
			bool result;

			try
			{
				if (_wcxArchiveFileSource.WcxModule._packToMem != null &&
					(_wcxArchiveFileSource.WcxModule.PluginCapabilities & (int)PackerCaps.PK_CAPS_MEMPACK) != 0)
				{
					_tarFileName = _wcxArchiveFileSource.ArchiveFileName;
					tarWriter = new TarWriter(_tarFileName,
											 AskQuestion,
											 RaiseAbortOperation,
											 CheckOperationState,
											 UpdateStatistics,
											 _wcxArchiveFileSource.WcxModule);
					result = true;
				}
				else
				{
					_tarFileName = Path.ChangeExtension(_wcxArchiveFileSource.ArchiveFileName, null);
					tarWriter = new TarWriter(_tarFileName,
											 AskQuestion,
											 RaiseAbortOperation,
											 CheckOperationState,
											 UpdateStatistics);
					result = false;
				}

				if (tarWriter.ProcessTree(_fullFilesTree, _statistics))
				{
					if (result && (_packingFlags & (int)PackFilesFlags.PK_PACK_MOVE_FILES) != 0)
						DeleteFiles(_fullFilesTree);
					else
					{
						// Fill file list with tar archive file
						_fullFilesTree.Clear();
						_fullFilesTree.Path = Path.GetDirectoryName(_tarFileName);
						_fullFilesTree.Add(FileSystemFileSource.CreateFileFromFile(_tarFileName));
					}
				}

				return result;
			}
			finally
			{
				if (tarWriter != null) tarWriter.Dispose();
			}
		}

		public int PackingFlags
		{
			get { return _packingFlags; }
			set { _packingFlags = value; }
		}

		// WCX callback methods
		private static int ProcessDataProc(WcxArchiveCopyInOperation operation, string fileName, int size)
		{
			// Implementation of process data callback
			return 1;
		}

		private static int ProcessDataProcAG(IntPtr fileName, int size)
		{
			return ProcessDataProc(_wcxCopyInOperationG, Marshal.PtrToStringAnsi(fileName), size);
		}

		private static int ProcessDataProcWG(IntPtr fileName, int size)
		{
			return ProcessDataProc(_wcxCopyInOperationG, Marshal.PtrToStringUni(fileName), size);
		}

		private static int ProcessDataProcAT(IntPtr fileName, int size)
		{
			return ProcessDataProc(_wcxCopyInOperationT, Marshal.PtrToStringAnsi(fileName), size);
		}

		private static int ProcessDataProcWT(IntPtr fileName, int size)
		{
			return ProcessDataProc(_wcxCopyInOperationT, Marshal.PtrToStringUni(fileName), size);
		}
	}
}