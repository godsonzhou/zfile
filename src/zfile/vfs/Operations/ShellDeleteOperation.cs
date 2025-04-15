using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace FileSystemOperations
{
    public class ShellDeleteOperation : FileSourceDeleteOperation
    {
        private IFileOperation fileOp;
        private List<IntPtr> sourceFilesTree;
        private IShellFileSource shellFileSource;
        private FileSourceDeleteOperationStatistics statistics;

        public ShellDeleteOperation(IFileSource targetFileSource,
                                  List<FileInfo> filesToDelete)
            : base(targetFileSource, filesToDelete)
        {
            shellFileSource = targetFileSource as IShellFileSource;
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_FileOperation)));
        }

        public override void Initialize()
        {
            statistics = RetrieveStatistics();
            sourceFilesTree = new List<IntPtr>();

            try
            {
                foreach (var file in FilesToDelete)
                {
                    var item = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
                    Marshal.StructureToPtr(((FileShellProperty)file.LinkProperty).Item, item, false);
                    sourceFilesTree.Add(item);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        public override void MainExecute()
        {
            var sink = new FileOperationProgressSink(statistics, UpdateStatistics, CheckOperationStateSafe);
            fileOp.SetOperationFlags(FOF_SILENT | FOF_NOCONFIRMATION | FOF_NORECURSION);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    IShellItemArray itemArray;
                    OleCheck(SHCreateShellItemArrayFromIDLists(sourceFilesTree.Count, sourceFilesTree.ToArray(), out itemArray));
                    OleCheck(fileOp.DeleteItems(itemArray));
                    int result = fileOp.PerformOperations();
                    if (result != 0)
                    {
                        if (result == COPYENGINE_E_USER_CANCELLED)
                            RaiseAbortOperation();
                        else
                            Marshal.ThrowExceptionForHR(result);
                    }
                }
                finally
                {
                    fileOp.Unadvise(cookie);
                }
            }
            catch (COMException ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            if (GlobalSettings.LogErrors && GlobalSettings.LogDelete)
            {
                Logger.Write(Thread.CurrentThread, message, LogMessageType.Error);
            }

            if (MessageBox.Show(message, "", MessageBoxButtons.SkipCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
            {
                RaiseAbortOperation();
            }
        }

        private void OleCheck(int hr)
        {
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    public static class Constants
    {
        public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";
        public const int FOF_SILENT = 0x0004;
        public const int FOF_NOCONFIRMATION = 0x0010;
        public const int FOF_NORECURSION = 0x1000;
        public const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);
    }

    public static class GlobalSettings
    {
        public static bool LogErrors { get; set; }
        public static bool LogDelete { get; set; }
    }

    public static class Logger
    {
        public static void Write(System.Threading.Thread thread, string message, LogMessageType type)
        {
            // 实现日志记录逻辑
        }
    }

    public enum LogMessageType
    {
        Error,
        Info,
        Success
    }
} 