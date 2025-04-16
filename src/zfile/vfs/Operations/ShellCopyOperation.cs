using System.Runtime.InteropServices;
using WinShell;
using Zfile.FileSources;
namespace FileSystemOperations
{
    public class ShellCopyOperation : FileSourceCopyOperation
    {
        private IFileOperation fileOp;
        private IShellItem targetFolder;
        private List<IntPtr> sourceFilesTree;
        private IShellFileSource shellFileSource;
        private FileSourceCopyOperationStatistics statistics;

        public ShellCopyOperation(IFileSource sourceFileSource,
                                IFileSource targetFileSource,
                                List<FileInfo> sourceFiles,
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
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID_FileOperation)));
        }

        public override void Initialize()
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
                        OleCheck(shellFileSource.FindFolder(TargetPath, out folder));
                        IntPtr objectPtr;
                        OleCheck(API.SHGetIDListFromObject(folder, out objectPtr));
                        try
                        {
                            OleCheck(SHCreateItemFromIDList(objectPtr, typeof(IShellItem).GUID, out targetFolder));
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(objectPtr);
                        }
                        break;
                    case FileSourceOperationType.CopyOut:
                        OleCheck(SHCreateItemFromParsingName(TargetPath, IntPtr.Zero, typeof(IShellItem).GUID, out targetFolder));
                        break;
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
            fileOp.SetOperationFlags(FOF_SILENT | FOF_NOCONFIRMMKDIR);

            try
            {
                uint cookie;
                fileOp.Advise(sink, out cookie);
                try
                {
                    IShellItemArray itemArray;
                    OleCheck(SHCreateShellItemArrayFromIDLists(sourceFilesTree.Count, sourceFilesTree.ToArray(), out itemArray));
                    OleCheck(fileOp.CopyItems(itemArray, targetFolder));
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
            if (GlobalSettings.LogCopyMove && GlobalSettings.LogErrors)
            {
                Logger.Write(Thread.CurrentThread, message, LogMessageType.Error);
            }

            if (MessageBox.Show(message, "", MessageBoxButtons.SkipCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
            {
                RaiseAbortOperation();
            }
        }

       
    }

    public class ShellCopyInOperation : ShellCopyOperation
    {
        public ShellCopyInOperation(IFileSource sourceFileSource,
                                  IFileSource targetFileSource,
                                  List<FileInfo> sourceFiles,
                                  string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        public override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyIn;
        }

        public override void Initialize()
        {
            statistics = RetrieveStatistics();
            sourceFilesTree = new List<IntPtr>();

            try
            {
                foreach (var file in SourceFiles)
                {
                    var objectPtr = API.ILCreateFromPath(file.FullPath);
                    sourceFilesTree.Add(objectPtr);
                }

                IShellFolder2 folder;
                OleCheck(shellFileSource.FindFolder(TargetPath, out folder));
                IntPtr objectPtr;
                OleCheck(API.SHGetIDListFromObject(folder, out objectPtr));
                OleCheck(API.SHCreateItemFromIDList(objectPtr, typeof(IShellItem).GUID, out targetFolder));
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
                                   List<FileInfo> sourceFiles,
                                   string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        public override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyOut;
        }
    }

  
} 