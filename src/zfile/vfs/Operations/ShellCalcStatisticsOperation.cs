using System.Runtime.InteropServices;
using WinShell;
namespace zfile
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

        private void ProcessFile(FileEntry file)
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
                w32.OleCheck(parent.BindToObject(objectPtr, IntPtr.Zero, ref iid, out folder));

                IEnumIDList enumIDList;
                w32.OleCheck(folder.EnumObjects(IntPtr.Zero,
                    (uint)(SHCONTF.FOLDERS | SHCONTF.NONFOLDERS |
                    SHCONTF.STORAGE | SHCONTF.INCLUDEHIDDEN),
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
                LogMessage(ex.Message, LogOption.Error, LogOption.Error);
            }
        }

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            switch (logMsgType)
            {
                case LogOption.Error:
                    if (!GlobalSettings.LogErrors) return;
                    break;
                case LogOption.Info:
                    if (!GlobalSettings.LogInfo) return;
                    break;
                case LogOption.Success:
                    if (!GlobalSettings.LogSuccess) return;
                    break;
            }

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(Thread.CurrentThread, message, logMsgType);
            }
        }

        private bool GetIsFolder(IShellFolder2 folder, IntPtr pidl)
        {
            uint attributes = (uint)SFGAO.FOLDER;
            folder.GetAttributesOf(1, new[] { pidl }, ref attributes);
            return (attributes & (uint)SFGAO.FOLDER) != 0;
        }

        private long GetDetails(IShellFolder2 folder, IntPtr pidl, SHCOLUMNID columnID)
        {
            object value;
            folder.GetDetailsEx(pidl, ref columnID, out value);
            return Convert.ToInt64(value ?? 0);
        }
    }

}