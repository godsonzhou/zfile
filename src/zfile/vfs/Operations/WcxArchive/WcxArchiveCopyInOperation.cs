using Zfile.FileSources;

namespace Zfile.Operations;

    public class WcxArchiveCopyInOperation : ArchiveCopyInOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;
        private StringHashListUtf8 _fileList;
        private bool _tarBefore;
        private string _tarFileName;
        private File _currentFile;
        private string _currentTargetFilePath;

        // Static variables for WCX callbacks
        private static WcxArchiveCopyInOperation _wcxCopyInOperationG = null;
        [ThreadStatic]
        private static WcxArchiveCopyInOperation _wcxCopyInOperationT;

        public WcxArchiveCopyInOperation(IFileSource sourceFileSource, 
                                        IFileSource targetFileSource, 
                                        Files sourceFiles, 
                                        string targetPath) : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
            _packingFlags = WcxModule.PK_PACK_SAVE_PATHS;
            _tarBefore = false;

            _needsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_PACK) == 0;

            _fileList = new StringHashListUtf8(true);

            // Get initialized statistics; then we change only what is needed.
            _statistics = RetrieveStatistics();
            _statistics.DoneFiles = -1;
            _statistics.CurrentFileDoneBytes = -1;
            UpdateStatistics(_statistics);
        }

        protected override void Initialize()
        {
            // Is plugin allow multiple Operations?
            if (_needsConnection)
                _wcxCopyInOperationG = this;
            else
                _wcxCopyInOperationT = this;

            // Gets full list of files (recursive)
            FillAndCount(SourceFiles,
                         ref _fullFilesTree,
                         ref _statistics.TotalFiles,
                         ref _statistics.TotalBytes);

            // Need to check file existence
            if (_fileExistsOption != FileSourceOperationOptionFileExists.Overwrite)
            {
                var fileList = _wcxArchiveFileSource.ArchiveFileList.LockList();
                try
                {
                    // Populate archive file list
                    foreach (var item in fileList)
                    {
                        var clonedItem = ((ObjectEx)item).Clone();
                        _fileList.Add(((WcxHeader)clonedItem).FileName.ToLowerInvariant(), clonedItem);
                    }
                }
                finally
                {
                    _wcxArchiveFileSource.ArchiveFileList.UnlockList();
                }
            }
        }

        public override void MainExecute()
        {
            // Put to TAR archive if needed
            if (_tarBefore && Tar()) return;

            var wcxModule = _wcxArchiveFileSource.WcxModule;

            string destPath = ExcludeFrontPathDelimiter(_targetPath);
            destPath = ExcludeTrailingPathDelimiter(destPath);

            _statistics.CurrentFileTo = _wcxArchiveFileSource.ArchiveFileName;
            if (_tarBefore) _statistics.CurrentFileDoneBytes = -1;
            UpdateStatistics(_statistics);

            SetProcessDataProc(WcxModule.WcxInvalidHandle);
            wcxModule.WcxSetChangeVolProc(WcxModule.WcxInvalidHandle);

            // Convert TFiles into String
            string fileList = GetFileList(_fullFilesTree);
            // Nothing to pack (user skip all files)
            if (fileList == "\0") return;

            int result = wcxModule.WcxPackFiles(
                           _wcxArchiveFileSource.ArchiveFileName,
                           destPath, // no trailing path delimiter here
                           IncludeTrailingPathDelimiter(_fullFilesTree.Path), // end with path delimiter here
                           fileList,
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
                           _wcxArchiveFileSource.ArchiveFileName), LogOption.ArcOp, LogMsgType.Success);

                _statistics.DoneFiles = _statistics.TotalFiles;
                UpdateStatistics(_statistics);
            }

            // Delete temporary TAR archive if needed
            if (_tarBefore) File.Delete(_tarFileName);
        }

        public override void Finalize()
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

        private string GetFileList(Files theFiles)
        {
            string result = "";
            bool archiveExists = _fileList.Count > 0;
            string subPath = ExcludeFrontPathDelimiter(_targetPath).ToLowerInvariant();

            foreach (var file in theFiles)
            {
                // Filenames must be relative to the current directory.
                string fileName = ExtractDirLevel(theFiles.Path, file.FullPath);

                // Special treatment of directories.
                if (file.IsDirectory)
                {
                    // TC ends paths to directories to be packed with '\'.
                    fileName = IncludeTrailingPathDelimiter(fileName);
                }
                // Need to check file existence
                else if (archiveExists)
                {
                    var header = (WcxHeader)_fileList[subPath + fileName.ToLowerInvariant()];
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
                if (_tarBefore && _wcxArchiveFileSource.WcxModule.PackToMem != null && 
                    (_wcxArchiveFileSource.WcxModule.PluginCapabilities & WcxModule.PK_CAPS_MEMPACK) != 0)
                    _needsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_MEMPACK) == 0;
                else
                    _needsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_PACK) == 0;
            }
        }

        private void ShowError(string message, int error, LogOption logOptions = LogOption.None)
        {
            LogMessage(message, logOptions, LogMsgType.Error);

            if (!GlobalSettings.SkipFileOpError && error > WcxModule.E_SUCCESS)
            {
                if (AskQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                               FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
                {
                    RaiseAbortOperation();
                }
            }
        }

        private void LogMessage(string message, LogOption logOptions, LogMsgType logMsgType)
        {
            switch (logMsgType)
            {
                case LogMsgType.Error:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Errors)) return;
                    break;
                case LogMsgType.Info:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
                    break;
                case LogMsgType.Success:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;
                    break;
            }

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(Thread, message, logMsgType);
            }
        }

        private void DeleteFiles(Files files)
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
            if (_needsConnection)
                _wcxArchiveFileSource.WcxModule.WcxSetProcessDataProc(arcData, ProcessDataProcAG, ProcessDataProcWG);
            else
                _wcxArchiveFileSource.WcxModule.WcxSetProcessDataProc(arcData, ProcessDataProcAT, ProcessDataProcWT);
        }

        private void QuestionActionHandler(FileSourceOperationUIAction action)
        {
            if (action == FileSourceOperationUIAction.Compare)
                ShowCompareFilesUI(_currentFile, IncludeFrontPathDelimiter(_currentTargetFilePath));
        }

        private string FileExistsMessage(File sourceFile, WcxHeader targetHeader)
        {
            string result = "File exists. Overwrite?\n" + targetHeader.FileName + "\n";

            result += string.Format("Size: {0}, Date: {1}\n", 
                                   targetHeader.UnpSize.ToString(),
                                   WcxFileTimeToDateTime(targetHeader.FileTime).ToString());

            result += "\nWith file:\n" + sourceFile.FullPath + "\n" +
                      string.Format("Size: {0}, Date: {1}", 
                                   sourceFile.Size.ToString(), 
                                   sourceFile.ModificationTime.ToString());

            return result;
        }

        private FileSourceOperationOptionFileExists FileExists(File sourceFile, WcxHeader targetHeader)
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
                                                    FileSourceOperationUIResponse.Compare },
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

        private FileSourceOperationOptionFileExists OverwriteOlder(File sourceFile, WcxHeader targetHeader)
        {
            if (sourceFile.ModificationTime > WcxFileTimeToDateTime(targetHeader.FileTime))
                return FileSourceOperationOptionFileExists.Overwrite;
            else
                return FileSourceOperationOptionFileExists.Skip;
        }

        private FileSourceOperationOptionFileExists OverwriteSmaller(File sourceFile, WcxHeader targetHeader)
        {
            if (sourceFile.Size > targetHeader.UnpSize)
                return FileSourceOperationOptionFileExists.Overwrite;
            else
                return FileSourceOperationOptionFileExists.Skip;
        }

        private FileSourceOperationOptionFileExists OverwriteLarger(File sourceFile, WcxHeader targetHeader)
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
                if (_wcxArchiveFileSource.WcxModule.PackToMem != null && 
                    (_wcxArchiveFileSource.WcxModule.PluginCapabilities & WcxModule.PK_CAPS_MEMPACK) != 0)
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
                    if (result && (_packingFlags & WcxModule.PK_PACK_MOVE_FILES) != 0)
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
            return ProcessDataProc(_wcxCopyInOperationG, System.Runtime.InteropServices.Marshal.PtrToStringAnsi(fileName), size);
        }

        private static int ProcessDataProcWG(IntPtr fileName, int size)
        {
            return ProcessDataProc(_wcxCopyInOperationG, System.Runtime.InteropServices.Marshal.PtrToStringUni(fileName), size);
        }

        private static int ProcessDataProcAT(IntPtr fileName, int size)
        {
            return ProcessDataProc(_wcxCopyInOperationT, System.Runtime.InteropServices.Marshal.PtrToStringAnsi(fileName), size);
        }

        private static int ProcessDataProcWT(IntPtr fileName, int size)
        {
            return ProcessDataProc(_wcxCopyInOperationT, System.Runtime.InteropServices.Marshal.PtrToStringUni(fileName), size);
        }
    }