using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class ShellMoveOperation : FileSourceMoveOperation
    {
        private IFileOperation fileOp;
        private IShellItem targetFolder;
        private ItemList sourceFilesTree;
        private readonly IShellFileSource? shellFileSource;
        private FileSourceMoveOperationStatistics statistics;

        public ShellMoveOperation(IFileSource fileSource, FileEntries sourceFiles, string targetPath)
            : base(fileSource, sourceFiles, targetPath)
        {
            shellFileSource = fileSource as IShellFileSource;
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(Constants.CLSID_FileOperation)));
        }

        ~ShellMoveOperation()
        {
            sourceFilesTree = null;
        }

        protected override void Initialize()
        {
            statistics = RetrieveStatistics();

            sourceFilesTree = new ItemList();
            try
            {
                foreach (var file in SourceFiles)
                {
                    var item = API.ILClone(((FileShellProperty)file.LinkProperty).Item);
                    sourceFilesTree.Add(item);
                }

                IShellFolder2 folder;
                w32.OleCheck(shellFileSource.FindFolder(TargetPath, out folder));

                IntPtr objectPtr;
                w32.OleCheck(API.SHGetIDListFromObject(folder, out objectPtr));
                try
                {
					var guid = typeof(IShellItem).GUID;
					w32.OleCheck(API.SHCreateItemFromIDList(objectPtr, ref guid, out targetFolder));
                }
                finally
                {
                    Marshal.FreeCoTaskMem(objectPtr);
                }
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        protected override void MainExecute()
        {
            var sink = new FileOperationProgressSink(ref statistics, UpdateStatistics, CheckOperationStateSafe);

            fileOp.SetOperationFlags(Constants.FOF_SILENT | Constants.FOF_NOCONFIRMMKDIR);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    //IShellItemArray itemArray;
                    w32.OleCheck(API.SHCreateShellItemArrayFromIDLists((uint)sourceFilesTree.Count, sourceFilesTree.ToArray(), out var itemArray));
                    w32.OleCheck(fileOp.MoveItems(itemArray, targetFolder));
                    int result = fileOp.PerformOperations();
                    if (result != 0)
                    {
                        if (result == Constants.COPYENGINE_E_USER_CANCELLED)
                        {
                            RaiseAbortOperation();
                        }
                        else
                        {
                            Marshal.ThrowExceptionForHR(result);
                        }
                    }
                }
                finally
                {
                    fileOp.Unadvise(cookie);
                }
            }
            catch (COMException e)
            {
                ShowError(e.Message);
            }
        }

        private void ShowError(string message)
        {
            if ((GlobalSettings.LogOptions & LogOption.Error) != 0)
            {
                Logger.Write(_thread, message, LogOption.Error);
            }

            if (AskQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                           FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
            {
                RaiseAbortOperation();
            }
        }
    }
  
} 