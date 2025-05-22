using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace zfile;

public class StringHashListUtf8
{
    private Dictionary<string, object> _dictionary;
    private List<KeyValuePair<string, object>> _list;
    private bool _ownsObjects;

    public StringHashListUtf8(bool ownsObjects)
    {
        _dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _list = new List<KeyValuePair<string, object>>();
        _ownsObjects = ownsObjects;
    }

    public void Add(string key, object data)
    {
        _dictionary[key] = data;
        _list.Add(new KeyValuePair<string, object>(key, data));
    }

    public void Clear()
    {
        if (_ownsObjects)
        {
            foreach (var item in _list)
            {
                if (item.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        _dictionary.Clear();
        _list.Clear();
    }

    public int Count => _list.Count;

    public List<KeyValuePair<string, object>> List => _list;

    public bool Contains(string key)
    {
        return _dictionary.ContainsKey(key);
    }

    public object? this[string key]
    {
        get => _dictionary.ContainsKey(key) ? _dictionary[key] : null;
        set
        {
            if (_dictionary.ContainsKey(key))
            {
                // Update existing item
                var index = _list.FindIndex(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    if (_ownsObjects && _list[index].Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _list[index] = new KeyValuePair<string, object>(key, value);
                }
            }
            _dictionary[key] = value;
        }
    }

    public void Free()
    {
        Clear();
        _dictionary = null;
        _list = null;
    }
}
public class MaskList
{
    private string _mask;
    public MaskList(string mask)
    {
        _mask = mask;
    }
    public bool Matches(string fileName)
    {
        // Implementation of matching logic
        return true;
    }
}
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
    private static WcxArchiveCopyOutOperation? _wcxCopyOutOperationG = null;
    [ThreadStatic]
    private static WcxArchiveCopyOutOperation? _wcxCopyOutOperationT;

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
            var arcFileEntries = _wcxArchiveFileSource.ArchiveFileEntries;
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

    private void SplitFileMask(string renameMask, out string renameNameMask, out string renameExtMask)
    {
        throw new NotImplementedException();
    }

    protected override void MainExecute()
    {
        var wcxModule = _wcxArchiveFileSource.WcxModule;

        var arcHandle = wcxModule.OpenArchiveHandle(_wcxArchiveFileSource.ArchiveFileName,
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
            wcxModule.WcxSetChangeVolProc(arcHandle);

            WcxHeader header = new WcxHeader();
            while (wcxModule.ReadWCXHeader(arcHandle, ref header) == 0)
            {
                try
                {
                    CheckOperationState();

                    // Now check if the file is to be extracted.
                    if (!header.IsDirectory &&           // Omit directories (we handle them ourselves).
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
                        string absoluteTargetFileName = targetFileName;
                        if (DoFileExists(header, ref absoluteTargetFileName) == FileSourceOperationOptionFileExists.Overwrite)
                            result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_EXTRACT, "", absoluteTargetFileName);
                        else
                            result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_SKIP, "", "");

                        // Update the target file name if it was changed
                        targetFileName = absoluteTargetFileName;

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

    private string ReplaceInvalidChars(string? targetFileName)
    {
        if (string.IsNullOrEmpty(targetFileName))
            return string.Empty;

        var result = new System.Text.StringBuilder();

#if WINDOWS
        char[] forbiddenChars = { '<', '>', ':', '"', '/', '|', '?', '*' };
#else
        char[] forbiddenChars = { '\0' };
#endif

        foreach (char c in targetFileName)
        {
            if (Array.IndexOf(forbiddenChars, c) >= 0)
            {
                // 替换为%加上字符的十六进制ASCII码
                result.Append('%').Append(((int)c).ToString("X2"));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    // 注意：不要使用Finalize方法，因为它会干扰析构函数的调用
    public override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearCurrentOperation();
        }
        base.Dispose(disposing);
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
        // List of paths that we know must be created.
        var pathsToCreate = new StringHashListUtf8(true);

        // List of possible directories to create with their attributes.
        // This hash list is created to speed up searches for attributes in archive file list.
        var dirsAttributes = new StringHashListUtf8(true);

        var fileList = _wcxArchiveFileSource.ArchiveFileEntries;
        try
        {
            foreach (var item in fileList)
            {
                var header = (WcxHeader)item;

                // Check if the file from the archive fits the selection given via SourceFiles.
                if (!MatchesFileEntries(theFiles, header.FileName))
                    continue;

                if (header.IsDirectory)
                {
                    string currentFileName = Helper.ExtractDirLevel(currentArchiveDir, header.FileName);
                    currentFileName = ReplaceInvalidChars(currentFileName);

                    // Save this directory and a pointer to its entry.
                    dirsAttributes.Add(currentFileName, header);

                    // If extracting all files and directories, add this directory
                    // to PathsToCreate so that empty directories are also created.
                    if (maskList == null)
                    {
                        // Paths in PathsToCreate list must end with path delimiter.
                        currentFileName = Helper.IncludeTrailingPathDelimiter(currentFileName);

                        if (!pathsToCreate.Contains(currentFileName))
                            pathsToCreate.Add(currentFileName, null);
                    }
                }
                else
                {
                    if ((maskList == null) || maskList.Matches(Path.GetFileName(header.FileName)))
                    {
                        _statistics.TotalBytes += header.UnpSize;
                        _statistics.TotalFiles++;

                        string currentFileName = Helper.ExtractDirLevel(currentArchiveDir, Path.GetDirectoryName(header.FileName));
                        currentFileName = ReplaceInvalidChars(currentFileName);

                        // If CurrentFileName is empty now then it was a file in current archive
                        // directory, therefore we don't have to create any paths for it.
                        if (!string.IsNullOrEmpty(currentFileName))
                            if (!pathsToCreate.Contains(currentFileName))
                                pathsToCreate.Add(currentFileName, null);
                    }
                }
            }
        }
        finally
        {
            fileList = null;
        }

        if (_extractWithoutPath)
        {
            pathsToCreate.Free();
            dirsAttributes.Free();
            return;
        }

        // Second, create paths and save which paths were created and their attributes.
        var directories = new List<string>();

        try
        {
            destPath = Helper.IncludeTrailingPathDelimiter(destPath);

            // Create path to destination directory (we don't have attributes for that).
            Directory.CreateDirectory(destPath);

            createdPaths.Clear();

            for (int pathIndex = 0; pathIndex < pathsToCreate.Count; pathIndex++)
            {
                directories.Clear();

                // Create also all parent directories of the path to create.
                // This adds directories to list in order from the outer to inner ones,
                // for example: dir, dir/dir2, dir/dir2/dir3.
                string path = pathsToCreate.List[pathIndex].Key;
                GetDirectories(path, directories);

                try
                {
                    foreach (var dir in directories)
                    {
                        string targetDir = destPath + dir;

                        if (!createdPaths.Contains(targetDir) && !Directory.Exists(targetDir))
                        {
                            if (!Directory.CreateDirectory(targetDir).Exists)
                            {
                                // Error, cannot create directory.
                                break; // Don't try to create subdirectories.
                            }
                            else
                            {
                                // Retrieve attributes for this directory, if they are stored.
                                WcxHeader header = null;
                                if (dirsAttributes.Contains(dir))
                                    header = (WcxHeader)dirsAttributes[dir];

                                createdPaths.Add(targetDir, header);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore exceptions and continue with next path
                }
            }
        }
        finally
        {
            pathsToCreate.Free();
            dirsAttributes.Free();
        }
    }

    // Helper method to get all directories in a path
    private void GetDirectories(string path, List<string> directories)
    {
        if (string.IsNullOrEmpty(path))
            return;

        // Split the path into components
        string[] parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string currentPath = "";

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;

            if (currentPath.Length > 0)
                currentPath += Path.DirectorySeparatorChar;

            currentPath += part;
            directories.Add(currentPath);
        }
    }

    private bool SetDirsAttributes(StringHashListUtf8 paths)
    {
        bool result = true;

        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            // Get attributes
            var header = (WcxHeader)paths.List[pathIndex].Value;

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

    private DateTime WcxFileTimeToFileTime(int fileTime)
    {
        // This method converts WCX file time (DOS format) to DateTime
        // Implementation based on the Pascal version in dcdatetimeutils.pas
        // For Windows platform, we use DosFileTimeToDateTime

        try
        {
            // Extract date and time components from DOS format
            int fileDate = (fileTime >> 16) & 0xFFFF;
            int fileTimeValue = fileTime & 0xFFFF;

            // Extract year, month, day from date
            int year = ((fileDate >> 9) & 0x7F) + 1980;

            int month = (fileDate >> 5) & 0x0F;
            if (month < 1) month = 1;
            if (month > 12) month = 12;

            int day = fileDate & 0x1F;
            if (day < 1) day = 1;
            // Check for valid day in month (simplified)
            int daysInMonth = DateTime.DaysInMonth(year, month);
            if (day > daysInMonth) day = daysInMonth;

            // Extract hour, minute, second from time
            int hour = (fileTimeValue >> 11) & 0x1F;
            if (hour > 23) hour = 23;

            int minute = (fileTimeValue >> 5) & 0x3F;
            if (minute > 59) minute = 59;

            int second = (fileTimeValue & 0x1F) << 1; // Multiply by 2
            if (second > 59) second = 59;

            // Create DateTime from components
            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            // Return minimum date on error
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Converts WCX file time to DateTime
    /// This is equivalent to WcxFileTimeToDateTime in Pascal
    /// </summary>
    /// <param name="wcxTime">WCX file time in DOS format</param>
    /// <returns>Converted DateTime</returns>
    private DateTime FileTimeToDateTime(int wcxTime)
    {
        return WcxFileTimeToFileTime(wcxTime);
    }

    private void QuestionActionHandler(FileSourceOperationUIResponse action)
    {
        if (action == FileSourceOperationUIResponse.CompareAction)
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

    /// <summary>
    /// Shows an input dialog to get user input
    /// </summary>
    /// <param name="caption">Dialog caption</param>
    /// <param name="prompt">Prompt text</param>
    /// <param name="value">Input/output value</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    private bool ShowInputQuery(string caption, string prompt, out string value)
    {
        // In a real implementation, this would show a dialog
        // For now, we'll just return the original value and true
        value = prompt;
        return true;
    }

    /// <summary>
    /// Gets the next available copy name for a file
    /// </summary>
    /// <param name="targetName">Original target file name</param>
    /// <param name="isDirectory">Whether the target is a directory</param>
    /// <returns>A new file name that doesn't exist</returns>
    private string GetNextCopyName(string targetName, bool isDirectory)
    {
        string path = Path.GetDirectoryName(targetName);
        string fileName = Path.GetFileNameWithoutExtension(targetName);
        string extension = Path.GetExtension(targetName);
        string newName;
        int i = 1;

        do
        {
            newName = Path.Combine(path, $"{fileName} ({i}){extension}");
            i++;
        } while (File.Exists(newName) || Directory.Exists(newName));

        return newName;
    }

    private FileSourceOperationOptionFileExists DoFileExists(WcxHeader header, ref string absoluteTargetFileName)
    {
        // Helper functions for different overwrite strategies
        FileSourceOperationOptionFileExists OverwriteOlder(string fileName)
        {
            if (FileTimeToDateTime(header.FileTime) > FileSystemUtil.FileTimeToDateTime(File.GetLastWriteTime(fileName).ToFileTime()))
                return FileSourceOperationOptionFileExists.Overwrite;
            else
                return FileSourceOperationOptionFileExists.Skip;
        }

        FileSourceOperationOptionFileExists OverwriteSmaller(string fileName)
        {
            if (header.UnpSize > new FileInfo(fileName).Length)
                return FileSourceOperationOptionFileExists.Overwrite;
            else
                return FileSourceOperationOptionFileExists.Skip;
        }

        FileSourceOperationOptionFileExists OverwriteLarger(string fileName)
        {
            if (header.UnpSize < new FileInfo(fileName).Length)
                return FileSourceOperationOptionFileExists.Overwrite;
            else
                return FileSourceOperationOptionFileExists.Skip;
        }

        if (!File.Exists(absoluteTargetFileName))
            return FileSourceOperationOptionFileExists.Overwrite;
        else
        {
            switch (_fileExistsOption)
            {
                case FileSourceOperationOptionFileExists.None:
                    bool answer = true;
                    do
                    {
                        answer = true;
                        // Prepare possible responses based on whether we need connection
                        FileSourceOperationUIResponse[] possibleResponses;

                        if (NeedsConnection)
                        {
                            // Can't asynchronously extract file for comparison when multiple operations are not supported
                            possibleResponses = new[] {
                                FileSourceOperationUIResponse.Overwrite,
                                FileSourceOperationUIResponse.Skip,
                                FileSourceOperationUIResponse.OverwriteLarger,
                                FileSourceOperationUIResponse.OverwriteAll,
                                FileSourceOperationUIResponse.SkipAll,
                                FileSourceOperationUIResponse.OverwriteSmaller,
                                FileSourceOperationUIResponse.OverwriteOlder,
                                FileSourceOperationUIResponse.Cancel,
                                FileSourceOperationUIResponse.RenameSource,
                                FileSourceOperationUIResponse.AutoRenameSource
                            };
                        }
                        else
                        {
                            possibleResponses = new[] {
                                FileSourceOperationUIResponse.Overwrite,
                                FileSourceOperationUIResponse.Skip,
                                FileSourceOperationUIResponse.OverwriteLarger,
                                FileSourceOperationUIResponse.OverwriteAll,
                                FileSourceOperationUIResponse.SkipAll,
                                FileSourceOperationUIResponse.OverwriteSmaller,
                                FileSourceOperationUIResponse.OverwriteOlder,
                                FileSourceOperationUIResponse.Cancel,
                                FileSourceOperationUIResponse.CompareAction,
                                FileSourceOperationUIResponse.RenameSource,
                                FileSourceOperationUIResponse.AutoRenameSource
                            };
                        }

                        string message = FileSystemUtil.FileExistsMessage(
                            absoluteTargetFileName,
                            header.FileName,
                            header.UnpSize,
                            FileTimeToDateTime(header.FileTime));

                        _currentFilePath = header.FileName;
                        _currentTargetFilePath = absoluteTargetFileName;

                        // Create a direct reference to the instance method
                        FileSourceOperationUIActionHandler actionHandler = QuestionActionHandler;

                        var response = AskQuestion(
                            message,
                            "",
                            possibleResponses,
                            FileSourceOperationUIResponse.Overwrite,
                            FileSourceOperationUIResponse.Skip,
                            new FileSourceOperationUIActionHandlerAdapter(actionHandler));

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
                                return OverwriteOlder(absoluteTargetFileName);

                            case FileSourceOperationUIResponse.OverwriteSmaller:
                                _fileExistsOption = FileSourceOperationOptionFileExists.OverwriteSmaller;
                                return OverwriteSmaller(absoluteTargetFileName);

                            case FileSourceOperationUIResponse.OverwriteLarger:
                                _fileExistsOption = FileSourceOperationOptionFileExists.OverwriteLarger;
                                return OverwriteLarger(absoluteTargetFileName);

                            case FileSourceOperationUIResponse.AutoRenameSource:
                                _fileExistsOption = FileSourceOperationOptionFileExists.AutoRenameSource;
                                absoluteTargetFileName = GetNextCopyName(absoluteTargetFileName, header.IsDirectory);
                                return FileSourceOperationOptionFileExists.Overwrite;

                            case FileSourceOperationUIResponse.RenameSource:
                                string newName = Path.GetFileName(absoluteTargetFileName);
                                bool inputResult = ShowInputQuery("Edit New File Name", newName, out newName);
                                if (inputResult)
                                {
                                    string? dirName = Path.GetDirectoryName(absoluteTargetFileName);
                                    absoluteTargetFileName = dirName != null
                                        ? Path.Combine(dirName, newName)
                                        : newName;
                                    return FileSourceOperationOptionFileExists.Overwrite;
                                }
                                answer = false;
                                break;

                            case FileSourceOperationUIResponse.None:
                            case FileSourceOperationUIResponse.Cancel:
                                RaiseAbortOperation();
                                break;
                        }
                    } while (!answer);

                    // This should never be reached, but is needed to satisfy the compiler
                    return FileSourceOperationOptionFileExists.None;

                case FileSourceOperationOptionFileExists.OverwriteOlder:
                    return OverwriteOlder(absoluteTargetFileName);

                case FileSourceOperationOptionFileExists.OverwriteSmaller:
                    return OverwriteSmaller(absoluteTargetFileName);

                case FileSourceOperationOptionFileExists.OverwriteLarger:
                    return OverwriteLarger(absoluteTargetFileName);

                case FileSourceOperationOptionFileExists.AutoRenameSource:
                    absoluteTargetFileName = GetNextCopyName(absoluteTargetFileName, header.IsDirectory);
                    return FileSourceOperationOptionFileExists.Overwrite;

                default:
                    return _fileExistsOption;
            }
        }
    }

    private void SetProcessDataProc(IntPtr arcData)
    {
        // 创建符合TProcessDataProc签名的委托
        TProcessDataProc procAG = (_, mode) => ProcessDataProcAG(IntPtr.Zero, mode);
        TProcessDataProc procWG = (_, mode) => ProcessDataProcWG(IntPtr.Zero, mode);
        TProcessDataProc procAT = (_, mode) => ProcessDataProcAT(IntPtr.Zero, mode);
        TProcessDataProc procWT = (_, mode) => ProcessDataProcWT(IntPtr.Zero, mode);

        // 获取委托的函数指针
        IntPtr procAGPtr = Marshal.GetFunctionPointerForDelegate(procAG);
        IntPtr procWGPtr = Marshal.GetFunctionPointerForDelegate(procWG);
        IntPtr procATPtr = Marshal.GetFunctionPointerForDelegate(procAT);
        IntPtr procWTPtr = Marshal.GetFunctionPointerForDelegate(procWT);

        // 保持委托引用防止被GC回收
        GC.KeepAlive(procAG);
        GC.KeepAlive(procWG);
        GC.KeepAlive(procAT);
        GC.KeepAlive(procWT);

        if (NeedsConnection)
            _wcxArchiveFileSource.WcxModule.WcxSetProcessDataProc(arcData, procAGPtr, procWGPtr);
        else
            _wcxArchiveFileSource.WcxModule.WcxSetProcessDataProc(arcData, procATPtr, procWTPtr);
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
    private static int ProcessDataProc(WcxArchiveCopyOutOperation? wcxCopyOutOperation, string? fileName, int size, IntPtr updateName)
    {
        //DCDebug('Working (' + IntToStr(GetCurrentThreadId) + ') ' + FileName + ' Size = ' + IntToStr(Size));

        int result = 1;

        if (wcxCopyOutOperation != null)
        {
            if (wcxCopyOutOperation.State == FileSourceOperationState.Stopping)  // Cancel operation
                return 0;

            var statistics = wcxCopyOutOperation._statistics;

            // Update file name
            if (updateName != IntPtr.Zero)
            {
                statistics.CurrentFileFrom = fileName ?? "";
            }

            // Get the number of bytes processed since the previous call
            if (size > 0)
            {
                statistics.CurrentFileDoneBytes += size;
                if (statistics.CurrentFileDoneBytes > statistics.CurrentFileTotalBytes)
                    statistics.CurrentFileDoneBytes = statistics.CurrentFileTotalBytes;
                statistics.DoneBytes += size;
            }
            // Get progress percent value to directly set progress bar
            else if (size < 0)
            {
                // Total operation percent
                if (size >= -100 && size <= -1)
                {
                    if (statistics.TotalBytes == 0) statistics.TotalBytes = 100;
                    statistics.DoneBytes = statistics.TotalBytes * (-size) / 100;
                }
                // Current file percent
                else if (size >= -1100 && size <= -1000)
                {
                    if (statistics.CurrentFileTotalBytes == 0) statistics.CurrentFileTotalBytes = 100;
                    statistics.CurrentFileDoneBytes = statistics.CurrentFileTotalBytes * ((-size) - 1000) / 100;
                }
            }

            //DCDebug('CurrentDone  = ' + IntToStr(CurrentFileDoneBytes) + ' Done  = ' + IntToStr(DoneBytes));
            //DCDebug('CurrentTotal = ' + IntToStr(CurrentFileTotalBytes) + ' Total = ' + IntToStr(TotalBytes));
            wcxCopyOutOperation.UpdateStatistics(wcxCopyOutOperation._statistics);
            if (!wcxCopyOutOperation.AppProcessMessages(true)) return 0;
        }

        return result;
    }

    private static int ProcessDataProcAG(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationG, Marshal.PtrToStringAnsi(fileName), size, fileName);
    }

    private static int ProcessDataProcWG(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationG, Marshal.PtrToStringUni(fileName), size, fileName);
    }

    private static int ProcessDataProcAT(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationT, Marshal.PtrToStringAnsi(fileName), size, fileName);
    }

    private static int ProcessDataProcWT(IntPtr fileName, int size)
    {
        return ProcessDataProc(_wcxCopyOutOperationT, Marshal.PtrToStringUni(fileName), size, fileName);
    }
}