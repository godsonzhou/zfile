namespace zfile;

public class WcxArchiveCopyOutOperation : ArchiveCopyOutOperation
{
    private IWcxArchiveFileSource _wcxArchiveFileSource;
    private FileSourceCopyOperationStatistics _statistics;
    private bool _renamingFiles;
    private string _renameNameMask, _renameExtMask;
    private bool _extractWithoutPath;
    private string _currentFilePath;
    private string _currentTargetFilePath;
	private string _extractMask;

    // Static variables for WCX callbacks
    private static WcxArchiveCopyOutOperation _wcxCopyOutOperationG = null;
    [ThreadStatic]
    private static WcxArchiveCopyOutOperation _wcxCopyOutOperationT;

    public WcxArchiveCopyOutOperation(IFileSource sourceFileSource, 
                                        IFileSource targetFileSource, 
                                        FileEntries sourceFiles, 
                                        string targetPath) : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
    {
        _wcxArchiveFileSource = (IWcxArchiveFileSource)sourceFileSource;
        _fileExistsOption = FileSourceOperationOptionFileExists.None;
        _extractWithoutPath = false;

        NeedsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_UNPACK) == 0;
    }

    protected override void Initialize()
    {
        // Is plugin allow multiple Operations?
        if (NeedsConnection)
            _wcxCopyOutOperationG = this;
        else
            _wcxCopyOutOperationT = this;

        // Extract without path from flat view
        if (!_extractWithoutPath)
        {
            _extractWithoutPath = SourceFiles.Flat;
        }

        if ((ExtractFlags & ExtractFlag.SmartExtract) != 0)
        {
            int count = 0;
            var arcFileEntries = _wcxArchiveFileSource.ArchiveFileEntries.Clone();
            try
            {
                foreach (var item in arcFileEntries)
                {
                    var header = (WcxHeader)item;
                    string fileName = Path.DirectorySeparatorChar + header.FileName;

                    if (FileSystemUtil.IsInPath(Path.DirectorySeparatorChar.ToString(), fileName, false, false))
                    {
                        count++;
                        if (count > 1)
                        {
                            _targetPath = _targetPath + Path.GetFileNameWithoutExtension(_wcxArchiveFileSource.ArchiveFileName) + Path.DirectorySeparatorChar;
                            break;
                        }
                    }
                }
            }
            finally
            {
                arcFileEntries = null;
            }
        }

        // Check rename mask
        _renamingFiles = (RenameMask != "*.*") && (RenameMask != "");
        if (_renamingFiles) SplitFileMask(RenameMask, out _renameNameMask, out _renameExtMask);
            
        // Get initialized statistics; then we change only what is needed.
        _statistics = RetrieveStatistics();
    }

    protected override void MainExecute()
    {
        var wcxModule = _wcxArchiveFileSource.WcxModule;

        var arcHandle = wcxModule.OpenArchive(_wcxArchiveFileSource.ArchiveFileName,
                                                    (int)OpenMode.PK_OM_EXTRACT, out int openResult);
        if (arcHandle == 0)
        {
            AskQuestion(WcxModule.GetErrorMsg(openResult), "", new[] { FileSourceOperationUIResponse.Ok }, 
                        FileSourceOperationUIResponse.Ok, FileSourceOperationUIResponse.Ok);
            RaiseAbortOperation();
        }

        // Extract all selected files/folders
        MaskList maskList = null;
        if (string.IsNullOrEmpty(_extractMask) || _extractMask == "*.*" || _extractMask == "*")
            maskList = null;
        else
            maskList = new MaskList(_extractMask);

        // Convert file list so that filenames are relative to archive root.
        var files = SourceFiles.Clone();
        ChangeFileEntriesRoot(Path.DirectorySeparatorChar.ToString(), files);

        var createdPaths = new StringHashListUtf8(true);

        try
        {
            // Count total files size and create needed directories.
            CreateDirsAndCountFiles(files, maskList,
                                    _targetPath, files.Path,
                                    ref createdPaths);

            SetProcessDataProc(arcHandle);
            wcxModule.SetChangeVolProc(arcHandle);

            WcxHeader header;
            while ((header = wcxModule.ReadWCXHeader(arcHandle)) != null)
            {
                try
                {
                    CheckOperationState();

                    // Now check if the file is to be extracted.
                    if (!FileAttributes.IsDirectory(header.FileAttr) &&           // Omit directories (we handle them ourselves).
                        MatchesFileEntries(files, header.FileName) &&    // Check if it's included in the FileEntries
                        (maskList == null || maskList.Matches(Path.GetFileName(header.FileName)))) // And name matches file mask
                    {
                        string targetFileName;
                        if (_extractWithoutPath)
                            targetFileName = Path.GetFileName(header.FileName);
                        else
                            targetFileName = Helper.ExtractDirLevel(files.Path, header.FileName);

                        if (_renamingFiles)
                        {
                            targetFileName = Path.GetDirectoryName(targetFileName) +
                                            FileSystemUtil.ApplyRenameMask(Path.GetFileName(targetFileName),
                                                            _renameNameMask, _renameExtMask);
                        }

                        targetFileName = _targetPath + ReplaceInvalidChars(targetFileName);

                        _statistics.CurrentFileFrom = header.FileName;
                        _statistics.CurrentFileTo = targetFileName;
                        _statistics.CurrentFileTotalBytes = header.UnpSize;
                        _statistics.CurrentFileDoneBytes = 0;

                        UpdateStatistics(_statistics);

                        int result;
                        if (DoFileExists(header, ref targetFileName) == FileSourceOperationOptionFileExists.Overwrite)
                            result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_EXTRACT, "", targetFileName);
                        else
                            result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_SKIP, "", "");

                        if (result != WcxModule.E_SUCCESS)
                        {
                            // User aborted operation.
                            if (result == WcxModule.E_EABORTED) RaiseAbortOperation();

                            ShowError(string.Format("Error extracting {0} -> {1}: {2}",
                                        _wcxArchiveFileSource.ArchiveFileName + Path.DirectorySeparatorChar +
                                        header.FileName, targetFileName, WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
                        }
                        else
                        {
                            LogMessage(string.Format("Successfully extracted {0} -> {1}",
                                        _wcxArchiveFileSource.ArchiveFileName + Path.DirectorySeparatorChar +
                                        header.FileName, targetFileName), LogOption.ArcOp, LogOption.Success);
                        }

                        _statistics.DoneFiles++;
                        UpdateStatistics(_statistics);
                    }
                    else // Skip
                    {
                        int result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_SKIP, "", "");

                        // Check for errors
                        if (result != WcxModule.E_SUCCESS)
                        {
                            ShowError(string.Format("Error extracting {0}: {1}",
                                        _wcxArchiveFileSource.ArchiveFileName + Path.DirectorySeparatorChar +
                                        header.FileName, WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
                        }
                    }
                }
                finally
                {
                    header = null;
                }
            }

            if (!_extractWithoutPath) SetDirsAttributes(createdPaths);
        }
        finally
        {
            // Close archive, ignore function result
            wcxModule.CloseArchive(arcHandle);
            files = null;
            maskList = null;
            createdPaths = null;
        }
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
                return string.Format("Extracting from {0} to {1}", _wcxArchiveFileSource.ArchiveFileName, _targetPath);
            default:
                return "Extracting";
        }
    }

    private void CreateDirsAndCountFiles(FileEntries theFiles, MaskList maskList,
                                        string destPath, string currentArchiveDir,
                                        ref StringHashListUtf8 createdPaths)
    {
        // Implementation of directory creation and file counting logic
        // This would be a complex method with similar logic to the Pascal version
    }

    private bool SetDirsAttributes(StringHashListUtf8 paths)
    {
        bool result = true;

        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            // Get attributes
            var header = (WcxHeader)paths.List[pathIndex].Data;

            if (header != null)
            {
                string targetDir = paths.List[pathIndex].Key;

                try
                {
                    // Restore attributes
                    File.SetAttributes(targetDir, (FileAttributes)header.FileAttr);

                    var time = WcxFileTimeToFileTime(header.FileTime);

                    // Set creation, modification time
                    File.SetCreationTime(targetDir, time);
                    File.SetLastWriteTime(targetDir, time);
                    File.SetLastAccessTime(targetDir, time);
                }
                catch
                {
                    result = false;
                }
            }
        }

        return result;
    }

    private void QuestionActionHandler(FileSourceOperationUIAction action)
    {
        if (action == FileSourceOperationUIAction.CompareAction)
        {
            var file = new FileEntry("");
            try
            {
                file.FullPath = Helper.IncludeFrontPathDelimiter(_currentFilePath);
                ShowCompareFilesUI(file, _currentTargetFilePath);
            }
            finally
            {
                file = null;
            }
        }
    }

    private FileSourceOperationOptionFileExists DoFileExists(WcxHeader header, ref string absoluteTargetFileName)
    {
        // Implementation of file exists handling logic
        // This would be a complex method with similar logic to the Pascal version
        return FileSourceOperationOptionFileExists.None;
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
            Logger.Write(Thread, message, logMsgType);
        }
    }

    private void SetProcessDataProc(IntPtr arcData)
    {
        if (NeedsConnection)
            _wcxArchiveFileSource.WcxModule.SetProcessDataProc(arcData, ProcessDataProcAG, ProcessDataProcWG);
        else
            _wcxArchiveFileSource.WcxModule.SetProcessDataProc(arcData, ProcessDataProcAT, ProcessDataProcWT);
    }

    public static void ClearCurrentOperation()
    {
        _wcxCopyOutOperationG = null;
    }

    public static Type GetOptionsUIClass()
    {
        return typeof(WcxArchiveCopyOperationOptionsUI);
    }

    public bool ExtractWithoutPath
    {
        get { return _extractWithoutPath; }
        set { _extractWithoutPath = value; }
    }

    // WCX callback methods would be implemented here
    private static int ProcessDataProc(WcxArchiveCopyOutOperation operation, string fileName, int size, IntPtr updateName)
    {
        // Implementation of process data callback
        return 1;
    }

    private static int ProcessDataProcAG(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationG, System.Runtime.InteropServices.Marshal.PtrToStringAnsi(fileName), size, fileName);
    }

    private static int ProcessDataProcWG(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationG, System.Runtime.InteropServices.Marshal.PtrToStringUni(fileName), size, fileName);
    }

    private static int ProcessDataProcAT(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationT, System.Runtime.InteropServices.Marshal.PtrToStringAnsi(fileName), size, fileName);
    }

    private static int ProcessDataProcWT(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationT, System.Runtime.InteropServices.Marshal.PtrToStringUni(fileName), size, fileName);
    }
}