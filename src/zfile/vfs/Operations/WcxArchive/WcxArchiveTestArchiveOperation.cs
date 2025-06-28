using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace zfile;

public class WcxArchiveTestArchiveOperation : FileSourceTestArchiveOperation
{
    private IWcxArchiveFileSource _wcxArchiveFileSource;
    //private FileSourceTestArchiveOperationStatistics statistics;
    private long _currentFileSize;

    // Static variables for WCX callbacks
    private static WcxArchiveTestArchiveOperation _wcxTestArchiveOperationG = null;
    [ThreadStatic]
    private static WcxArchiveTestArchiveOperation _wcxTestArchiveOperationT;

    public WcxArchiveTestArchiveOperation(IFileSource sourceFileSource, FileEntries sourceFiles)
        : base(sourceFileSource, sourceFiles)
    {
        _wcxArchiveFileSource = (IWcxArchiveFileSource)sourceFileSource;
        NeedsConnection = (_wcxArchiveFileSource.WcxModule.BackgroundFlags & WcxModule.BACKGROUND_UNPACK) == 0;
    }

    protected override void Initialize()
    {
        // Is plugin allow multiple Operations?
        if (NeedsConnection)
            _wcxTestArchiveOperationG = this;
        else
            _wcxTestArchiveOperationT = this;

        // Get initialized statistics; then we change only what is needed.
        statistics = RetrieveStatistics();
        statistics.ArchiveFile = _wcxArchiveFileSource.ArchiveFileName;
		statistics.TotalBytes = new FileInfo(statistics.ArchiveFile).Length;  // bugfix: 应为totalbytes为0，导致无法计算donebytes, 所以无法触发RemainingTime计算逻辑，进度条无法更新
		//statistics.TotalFiles = SourceFiles.Count;
	}

    protected override void MainExecute()
    {
        var wcxModule = _wcxArchiveFileSource.WcxModule;

        var arcHandle = wcxModule.OpenArchiveHandle(_wcxArchiveFileSource.ArchiveFileName,
                                                  (int)OpenMode.PK_OM_EXTRACT,
                                                  out int openResult);
        if (arcHandle == 0)
        {
            AskQuestion(WcxModule.GetErrorMsg(openResult), "", new[] { FileSourceOperationUIResponse.Ok },
                        FileSourceOperationUIResponse.Ok, FileSourceOperationUIResponse.Ok);
            RaiseAbortOperation();
        }

        // Convert file list so that filenames are relative to archive root.
        var files = SourceFiles.Clone();
        ChangeFileEntriesRoot(Path.DirectorySeparatorChar.ToString(), files);

        try
        {
            SetProcessDataProc(arcHandle);
            wcxModule.WcxSetChangeVolProc(arcHandle);

            WcxHeader header = new WcxHeader();
            while (wcxModule.ReadWCXHeader(arcHandle, ref header) == 0)
            {
                try
                {
                    CheckOperationState();

                    // Now check if the file is to be tested.
                    if (!header.IsDirectory && MatchesFileEntries(files, header.FileName))            // Omit directories (we handle them ourselves).// Check if it's included in the FileEntries
					{
                        statistics.CurrentFile = header.FileName;
						Debug.Print($"Testing file: {statistics.CurrentFile}");
						statistics.CurrentFileTotalBytes = header.UnpSize;
                        statistics.CurrentFileDoneBytes = 0;

                        UpdateStatistics(statistics);
                        _currentFileSize = header.UnpSize;

                        int result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_TEST, "", "");

                        if (result != WcxModule.E_SUCCESS)
                        {
                            // User aborted operation.
                            if (result == WcxModule.E_EABORTED)
                                break;

                            ShowError(string.Format("Error testing {0}: {1}",
                                       _wcxArchiveFileSource.ArchiveFileName + Path.DirectorySeparatorChar +
                                       header.FileName, WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
                        }
                        else
                        {
                            LogMessage(string.Format("Successfully tested {0}",
                                        _wcxArchiveFileSource.ArchiveFileName + Path.DirectorySeparatorChar +
                                        header.FileName), LogOption.ArcOp, LogOption.Success);
                        }
                    }
                    else // Skip
                    {
                        int result = wcxModule.ProcessFile(arcHandle, ProcessMode.PK_SKIP, "", "");

                        // Check for errors
                        if (result != WcxModule.E_SUCCESS)
                        {
                            ShowError(string.Format("Error testing {0}: {1}",
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
        }
        finally
        {
            wcxModule.CloseArchive(arcHandle);
            files = null;
        }
    }

    protected override void Finalize()
    {
        ClearCurrentOperation();
    }

    private void SetProcessDataProc(IntPtr arcData)
    {
        // 创建符合TProcessDataProc签名的委托
        TProcessDataProc procAG = (string arcName, int size) =>
        {
            return ProcessDataProcAG(arcName, size);
        };
        TProcessDataProcW procWG = (string arcName, int size) =>
        {
            return ProcessDataProcWG(arcName, size);
        };
        TProcessDataProc procAT = (string arcName, int size) =>
        {
            return ProcessDataProcAT(arcName, size);
        };
        TProcessDataProcW procWT = (string arcName, int size) =>
        {
            return ProcessDataProcWT(arcName, size);
        };

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
        _wcxTestArchiveOperationG = null;
    }

    // WCX callback methods
    private static int ProcessDataProc(WcxArchiveTestArchiveOperation operation, string fileName, int size, string updateName)
    {
        // Implementation of process data callback
        int result = 1;

        if (operation != null)
        {
            if (operation.State == FileSourceOperationState.Stopping)  // Cancel operation
                return 0;

            var statistics = operation.statistics;

            // Update file name
            if (string.IsNullOrEmpty(updateName))
            {
                statistics.CurrentFile = fileName;
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
                    statistics.DoneBytes = statistics.TotalBytes * (-size) / 100;
                }
                // Current file percent
                else if (size >= -1100 && size <= -1000)
                {
                    statistics.CurrentFileTotalBytes = 100;
                    statistics.CurrentFileDoneBytes = (-size) - 1000;
                }
            }

            operation.UpdateStatistics(statistics);
            if (!operation.CheckOperationStateSafe()) return 0;
        }

        return result;
    }

    private static int ProcessDataProcAG(string fileName, int size)
    {
        return ProcessDataProc(_wcxTestArchiveOperationG, (fileName), size, fileName);
    }

    private static int ProcessDataProcWG(string fileName, int size)
    {
        return ProcessDataProc(_wcxTestArchiveOperationG, (fileName), size, fileName);
    }

    private static int ProcessDataProcAT(string fileName, int size)
    {
        return ProcessDataProc(_wcxTestArchiveOperationT, (fileName), size, fileName);
    }

    private static int ProcessDataProcWT(string fileName, int size)
    {
        return ProcessDataProc(_wcxTestArchiveOperationT, (fileName), size, fileName);
    }
}