using System.Runtime.InteropServices;

namespace zfile
{
    public class ShellMoveOperation : FileSourceMoveOperation
    {
        private IFileOperation fileOp;
        private IShellItem targetFolder;
        private ItemList sourceFilesTree;
        private readonly IShellFileSource shellFileSource;
        private FileSourceMoveOperationStatistics statistics;

        public ShellMoveOperation(IFileSource fileSource, List<FileInfo> sourceFiles, string targetPath)
            : base(fileSource, sourceFiles, targetPath)
        {
            shellFileSource = fileSource as IShellFileSource;
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileOperation)));
        }

        ~ShellMoveOperation()
        {
            sourceFilesTree = null;
        }

        public override void Initialize()
        {
            statistics = RetrieveStatistics();

            sourceFilesTree = new ItemList();
            try
            {
                foreach (var file in SourceFiles)
                {
                    var item = ILClone(((FileShellProperty)file.LinkProperty).Item);
                    sourceFilesTree.Add(item);
                }

                IShellFolder2 folder;
                OleCheck(shellFileSource.FindFolder(TargetPath, out folder));

                IntPtr objectPtr;
                OleCheck(SHGetIDListFromObject(folder, out objectPtr));
                try
                {
                    OleCheck(SHCreateItemFromIDList(objectPtr, typeof(IShellItem).GUID, out targetFolder));
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

        public override void MainExecute()
        {
            var sink = new FileOperationProgressSink(ref statistics, UpdateStatistics, CheckOperationStateSafe);

            fileOp.SetOperationFlags(FOF.SILENT | FOF.NOCONFIRMMKDIR);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    IShellItemArray itemArray;
                    OleCheck(SHCreateShellItemArrayFromIDLists((uint)sourceFilesTree.Count, sourceFilesTree.ToArray(), out itemArray));
                    OleCheck(fileOp.MoveItems(itemArray, targetFolder));
                    int result = fileOp.PerformOperations();
                    if (result != 0)
                    {
                        if (result == COPYENGINE_E_USER_CANCELLED)
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

        public override void Finalize()
        {
        }

        private void ShowError(string message)
        {
            if ((GlobalSettings.LogOptions & LogOptions.Errors) != 0)
            {
                Logger.Write(Thread, message, LogMsgType.Error);
            }

            if (AskQuestion(message, "", new[] { FileSourceOperationResponse.Skip, FileSourceOperationResponse.Abort },
                           FileSourceOperationResponse.Skip, FileSourceOperationResponse.Abort) == FileSourceOperationResponse.Abort)
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

  
} 