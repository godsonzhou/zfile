using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace FileSystemOperations
{
    public class ShellCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private IShellFileSource shellFileSource;
        private FileSourceCalcStatisticsOperationStatistics statistics;

        public ShellCalcStatisticsOperation(IFileSource targetFileSource, List<FileInfo> files)
            : base(targetFileSource, files)
        {
            shellFileSource = targetFileSource as IShellFileSource;
        }

        public override void Initialize()
        {
            // 获取初始化的统计信息；然后我们只更改需要的内容
            statistics = RetrieveStatistics();
        }

        public override void MainExecute()
        {
            foreach (var file in Files)
            {
                ProcessFile(file);
                CheckOperationState();
            }
        }

        private void ProcessFile(FileInfo file)
        {
            statistics.CurrentFile = file.FullPath;
            UpdateStatistics(statistics);

            if (file.IsDirectory)
            {
                statistics.Directories++;
                IShellFolder2 folder;
                if (shellFileSource.FindFolder(file.Path, out folder))
                {
                    IntPtr objectPtr;
                    if (shellFileSource.FindObject(folder, file.Name, out objectPtr))
                    {
                        try
                        {
                            ProcessSubDirs(folder, objectPtr);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(objectPtr);
                        }
                    }
                }
            }
            else
            {
                statistics.Files++;
                statistics.Size += file.Size;
                if (file.ModificationTime < statistics.OldestFile)
                    statistics.OldestFile = file.ModificationTime;
                if (file.ModificationTime > statistics.NewestFile)
                    statistics.NewestFile = file.ModificationTime;
            }
            UpdateStatistics(statistics);
        }

        private void ProcessSubDirs(IShellFolder2 parent, IntPtr objectPtr)
        {
            try
            {
                IShellFolder2 folder;
                Guid iid = typeof(IShellFolder2).GUID;
                OleCheck(parent.BindToObject(objectPtr, IntPtr.Zero, ref iid, out folder));

                IEnumIDList enumIDList;
                OleCheck(folder.EnumObjects(IntPtr.Zero,
                    SHCONTF.SHCONTF_FOLDERS | SHCONTF.SHCONTF_NONFOLDERS |
                    SHCONTF.SHCONTF_STORAGE | SHCONTF.SHCONTF_INCLUDEHIDDEN,
                    out enumIDList));

                IntPtr pidl;
                uint numIDs;
                while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                {
                    try
                    {
                        if (GetIsFolder(parent, pidl))
                        {
                            statistics.Directories++;
                            ProcessSubDirs(folder, pidl);
                        }
                        else
                        {
                            long size = GetDetails(folder, pidl, SCID_FileSize);
                            statistics.Size += size;
                            statistics.Files++;
                        }
                        CheckOperationState();
                        UpdateStatistics(statistics);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pidl);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message, LogOptions.Errors, LogMessageType.Error);
            }
        }

        private void LogMessage(string message, LogOptions logOptions, LogMessageType logMsgType)
        {
            switch (logMsgType)
            {
                case LogMessageType.Error:
                    if (!GlobalSettings.LogErrors) return;
                    break;
                case LogMessageType.Info:
                    if (!GlobalSettings.LogInfo) return;
                    break;
                case LogMessageType.Success:
                    if (!GlobalSettings.LogSuccess) return;
                    break;
            }

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(Thread.CurrentThread, message, logMsgType);
            }
        }

        private void OleCheck(int hr)
        {
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        private bool GetIsFolder(IShellFolder2 folder, IntPtr pidl)
        {
            uint attributes = SFGAO.SFGAO_FOLDER;
            folder.GetAttributesOf(1, new[] { pidl }, ref attributes);
            return (attributes & SFGAO.SFGAO_FOLDER) != 0;
        }

        private long GetDetails(IShellFolder2 folder, IntPtr pidl, SHCOLUMNID columnID)
        {
            object value;
            folder.GetDetailsEx(pidl, ref columnID, out value);
            return Convert.ToInt64(value ?? 0);
        }
    }

    public enum LogOptions
    {
        None = 0,
        Errors = 1,
        Info = 2,
        Success = 4
    }

    public enum LogMessageType
    {
        Error,
        Info,
        Success
    }

    public enum SFGAO
    {
        SFGAO_FOLDER = 0x20000000
    }

    public static class GlobalSettings
    {
        public static bool LogErrors { get; set; }
        public static bool LogInfo { get; set; }
        public static bool LogSuccess { get; set; }
        public static LogOptions LogOptions { get; set; }
    }

    public static class Logger
    {
        public static void Write(System.Threading.Thread thread, string message, LogMessageType type)
        {
            // 实现日志记录逻辑
        }
    }
} 