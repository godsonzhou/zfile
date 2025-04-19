using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinShell;

namespace zfile
{
    public class ShellCopyOperation : FileSourceCopyOperation
    {
        private IFileOperation? fileOp;
        protected IShellItem targetFolder;
        protected List<IntPtr> sourceFilesTree;
        protected IShellFileSource? shellFileSource;
        protected FileSourceCopyOperationStatistics statistics;

        protected void UpdateStatistics(ref FileSourceCopyOperationStatistics newStatistics)
        {
            // Update statistics in the base class
            // Calculate progress percentage based on bytes
            double progressPercentage = 0;
            if (newStatistics.TotalBytes > 0)
                progressPercentage = (double)newStatistics.DoneBytes / newStatistics.TotalBytes;

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

        public ShellCopyOperation(IFileSource sourceFileSource,
                                IFileSource targetFileSource,
                                FileEntries sourceFiles,
                                string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            switch (GetID())
            {
                case FileSourceOperationType.Copy:
                case FileSourceOperationType.CopyOut:
                    shellFileSource = sourceFileSource as IShellFileSource;
                    break;
                case FileSourceOperationType.CopyIn:
                    shellFileSource = targetFileSource as IShellFileSource;
                    break;
            }
            fileOp = (IFileOperation?)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(Constants.CLSID_FileOperation)));
        }

        protected override void Initialize()
        {
            statistics = RetrieveStatistics();
            sourceFilesTree = new List<IntPtr>();

            try
            {
                foreach (var file in SourceFiles)
                {
                    var item = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
                    Marshal.StructureToPtr(((FileShellProperty)file.LinkProperty).Item, item, false);
                    sourceFilesTree.Add(item);
                }

                switch (GetID())
                {
                    case FileSourceOperationType.Copy:
                        IShellFolder2 folder;
                        w32.OleCheck(shellFileSource.FindFolder(TargetPath, out folder));
                        IntPtr objectPtr;
                        w32.OleCheck(API.SHGetIDListFromObject(folder, out objectPtr));
                        try
                        {
                            var copyItemGuid = typeof(IShellItem).GUID;
                            object copyShellItem;
                            w32.OleCheck(API.SHCreateItemFromIDList(objectPtr, ref copyItemGuid, out copyShellItem));
                            targetFolder = (IShellItem)copyShellItem;
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(objectPtr);
                        }
                        break;
                    case FileSourceOperationType.CopyOut:
                        var shellItemGuid = typeof(IShellItem).GUID;
                        object shellItem;
                        w32.OleCheck(API.SHCreateItemFromParsingName(TargetPath, IntPtr.Zero, ref shellItemGuid, out shellItem));
                        targetFolder = (IShellItem)shellItem;
                        break;
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
            fileOp.SetOperationFlags(Constants.FOF_SILENT | Constants.FOF_NOCONFIRMMKDIR);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    IShellItemArray itemArray;
                    w32.OleCheck(API.SHCreateShellItemArrayFromIDLists((uint)sourceFilesTree.Count, sourceFilesTree.ToArray(), out itemArray));
                    w32.OleCheck(fileOp.CopyItems(itemArray, targetFolder));
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

        protected void ShowError(string message)
        {
            // Log the error message
            // Since we're not sure if GlobalSettings has the required properties,
            // we'll just log the error unconditionally
            Logger.Write(Thread.CurrentThread, message, LogOption.Error);

            // Use standard MessageBoxButtons instead of SkipCancel which doesn't exist
            if (MessageBox.Show(message, "", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
            {
                RaiseAbortOperation();
            }
        }
    }

    internal interface IFileOperation
    {
        int CopyItems(IShellItemArray shellItemArray, IShellItem shellItem) { return -1; }
        int PerformOperations();
        void SetOperationFlags(int flags) { }
        void Advise(FileOperationProgressSink sink, out uint cookie);
        void Unadvise(uint cookie);
        int DeleteItems(IShellItemArray shellItemArray) { return -1; }
        int MoveItems(IShellItemArray shellItemArray, IShellItem shellItem) { return -1; }
        void RenameItem(IShellItem shellItem, string newName, object obj) { }

    }

    public class ShellCopyInOperation : ShellCopyOperation
    {
        public ShellCopyInOperation(IFileSource sourceFileSource,
                                  IFileSource targetFileSource,
                                  FileEntries sourceFiles,
                                  string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        protected override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyIn;
        }

        protected override void Initialize()
        {
            statistics = RetrieveStatistics();
            sourceFilesTree = new List<IntPtr>();

            try
            {
                foreach (var file in SourceFiles)
                {
                    var fileObjectPtr = API.ILCreateFromPath(file.FullPath);
                    sourceFilesTree.Add(fileObjectPtr);
                }

                IShellFolder2 folder;
                w32.OleCheck(shellFileSource.FindFolder(TargetPath, out folder));
                IntPtr folderObjectPtr;
                w32.OleCheck(API.SHGetIDListFromObject(folder, out folderObjectPtr));
                var copyInGuid = typeof(IShellItem).GUID;

                object copyInShellItem;
                w32.OleCheck(API.SHCreateItemFromIDList(folderObjectPtr, ref copyInGuid, out copyInShellItem));
                targetFolder = (IShellItem)copyInShellItem;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }
    }

    public class ShellCopyOutOperation : ShellCopyOperation
    {
        public ShellCopyOutOperation(IFileSource sourceFileSource,
                                   IFileSource targetFileSource,
                                   FileEntries sourceFiles,
                                   string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        protected override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyOut;
        }
    }


}