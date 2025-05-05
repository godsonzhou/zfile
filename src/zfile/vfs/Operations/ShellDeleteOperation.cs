using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinShell;
namespace zfile
{
    public class ShellDeleteOperation : FileSourceDeleteOperation
    {
        private IFileOperation fileOp;
        private List<IntPtr> sourceFilesTree;
        private IShellFileSource shellFileSource;
        private FileSourceDeleteOperationStatistics statistics;

        protected void UpdateStatistics(ref FileSourceDeleteOperationStatistics newStatistics)
        {
            // Update statistics in the base class
            // Calculate progress percentage based on files
            double progressPercentage = 0;
            if (newStatistics.TotalFiles > 0)
                progressPercentage = (double)newStatistics.DoneFiles / newStatistics.TotalFiles;

            UpdateProgress(progressPercentage);
        }

        protected bool CheckOperationStateSafe()
        {
            try
            {
                CheckOperationState();
                return true;
            }
            catch
            {
                return false;
            }
        }

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
            var sink = new FileOperationProgressSink(ref statistics, UpdateStatistics, CheckOperationStateSafe);
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
            // Log the error message
            // Since we're not sure if GlobalSettings has the required properties,
            // we'll just log the error unconditionally
            Logger.Write(_thread, message, LogOption.Error);

            // Use standard MessageBoxButtons instead of SkipCancel which doesn't exist
            if (MessageBox.Show(message, "", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
            {
                RaiseAbortOperation();
            }
        }

    }

}