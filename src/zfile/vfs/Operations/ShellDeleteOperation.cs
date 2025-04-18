using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class ShellDeleteOperation : FileSourceDeleteOperation
    {
        private IFileOperation fileOp;
        private List<IntPtr> sourceFilesTree;
        private IShellFileSource shellFileSource;
        private FileSourceDeleteOperationStatistics statistics;

        public ShellDeleteOperation(IFileSource targetFileSource,
                                  FileEntries filesToDelete)
            : base(targetFileSource, filesToDelete)
        {
            shellFileSource = targetFileSource as IShellFileSource;
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(Constants.CLSID_FileOperation)));
        }

        protected override void Initialize()
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

        protected override void MainExecute()
        {
            var sink = new FileOperationProgressSink(statistics, UpdateStatistics, CheckOperationStateSafe);
            fileOp.SetOperationFlags(Constants.FOF_SILENT | Constants.FOF_NOCONFIRMATION | Constants.FOF_NORECURSION);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    IShellItemArray itemArray;
                    w32.OleCheck(API.SHCreateShellItemArrayFromIDLists((uint)sourceFilesTree.Count, sourceFilesTree.ToArray(), out itemArray));
                    w32.OleCheck(fileOp.DeleteItems(itemArray));
                    int result = fileOp.PerformOperations();
                    if (result != 0)
                    {
                        if (result == Constants.COPYENGINE_E_USER_CANCELLED)
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
                Logger.Write(Thread.CurrentThread, message, LogOption.Error);
            }

            if (MyMessageBox.Show(message, "", MessageBoxButtons.SkipCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
            {
                RaiseAbortOperation();
            }
        }

    }
  
} 