using WinShell;

namespace zfile
{
    public class ShellSetFilePropertyOperation : FileSourceSetFilePropertyOperation
    {
        private IFileOperation fileOp;
        private int currentFileIndex;
        private ItemList sourceFilesTree;
        private readonly IShellFileSource shellFileSource;
        private FileSourceSetFilePropertyOperationStatistics statistics;

        public ShellSetFilePropertyOperation(IFileSource targetFileSource, FileEntries targetFiles, FileProperty[] newProperties)
            : base(targetFileSource, targetFiles, newProperties)
        {
            shellFileSource = targetFileSource as IShellFileSource;
            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileOperation)));
            SupportedProperties = FilePropertyType.Name ;
        }

        ~ShellSetFilePropertyOperation()
        {
            sourceFilesTree = null;
        }

        protected override void Initialize()
        {
            statistics = RetrieveStatistics();

            sourceFilesTree = new ItemList();
            try
            {
                foreach (var file in TargetFiles)
                {
                    var item = API.ILClone(((FileShellProperty)file.LinkProperty).Item);
                    sourceFilesTree.Add(item);
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

            uint cookie;
            fileOp.Advise(sink, out cookie);
            try
            {
                for (currentFileIndex = 0; currentFileIndex < sourceFilesTree.Count; currentFileIndex++)
                {
                    var file = TargetFiles[currentFileIndex];
                    var templateFile = TemplateFiles != null && currentFileIndex < TemplateFiles.Count ? TemplateFiles[currentFileIndex] : null;

                    SetProperties(currentFileIndex, file, templateFile);

                    statistics.DoneFiles++;
                    UpdateStatistics(statistics);

                    CheckOperationState();
                }
            }
            finally
            {
                fileOp.Unadvise(cookie);
            }
        }

        protected override SetFilePropertyResult SetNewProperty(FileEntry file, FileProperty templateProperty)
        {
            var result = SetFilePropertyResult.Success;

            var pidl = (IntPtr)sourceFilesTree[currentFileIndex];
            IShellItem item;
            if (Failed(API.SHCreateItemFromIDList(pidl, ref typeof(IShellItem).GUID, out item)))
                return SetFilePropertyResult.Error;

            switch (templateProperty.ID)
            {
                case FilePropertyType.Name:
                    var fileNameProperty = (FileNameProperty)templateProperty;
                    if (fileNameProperty.Value != file.Name)
                    {
                        if (!Succeeded(fileOp.RenameItem(item, fileNameProperty.Value, null)))
                        {
                            result = SetFilePropertyResult.Error;
                        }
                        else
                        {
                            var res = fileOp.PerformOperations();
                            if (Failed(res))
                            {
                                if (res == COPYENGINE_E_USER_CANCELLED)
                                {
                                    RaiseAbortOperation();
                                }
                                else
                                {
                                    result = SetFilePropertyResult.Error;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;
                default:
                    throw new Exception("Trying to set unsupported property");
            }

            return result;
        }

        private void ShowError(string message)
        {
            if ((GlobalSettings.LogOptions & LogOption.Error) != 0)
            {
                Logger.Write(message, LogOption.Error);
            }

            if (AskQuestion(message, "", new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                           FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
            {
                RaiseAbortOperation();
            }
        }

        private bool Failed(int hr)
        {
            return hr != 0;
        }

        private bool Succeeded(int hr)
        {
            return hr == 0;
        }
    }

  
} 