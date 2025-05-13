using WinShell;

namespace zfile
{
    public class ShellSetFilePropertyOperation : FileSourceSetFilePropertyOperation
    {
        private readonly IFileOperation fileOp;
        private int currentFileIndex;
        private ItemList? sourceFilesTree;

        private FileSourceSetFilePropertyOperationStatistics? statistics;

        public ShellSetFilePropertyOperation(IFileSource targetFileSource, FileEntries targetFiles, FileProperty[] newProperties)
            : base(targetFileSource, targetFiles, newProperties)
        {

            fileOp = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(Constants.CLSID_FileOperation)))!;
            SupportedProperties = FilePropertiesTypes.Name;
        }

        ~ShellSetFilePropertyOperation()
        {
            sourceFilesTree = null!;
        }

        protected override void Initialize()
        {
            statistics = RetrieveStatistics();
            sourceFilesTree = [];

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

        protected void UpdateStatistics(ref FileSourceSetFilePropertyOperationStatistics newStatistics)
        {
            // Update progress percentage based on files
            double progressPercentage = 0;
            if (newStatistics.TotalFiles > 0)
                progressPercentage = (double)newStatistics.DoneFiles / newStatistics.TotalFiles;

            UpdateProgress(progressPercentage);
        }

		protected override void MainExecute()
        {
            var sink = new FileOperationProgressSink(ref statistics!, UpdateStatistics, CheckOperationStateSafe);

            fileOp.SetOperationFlags(Constants.FOF_SILENT | Constants.FOF_NOCONFIRMMKDIR);

            fileOp.Advise(sink, out uint cookie);
            try
            {
                for (currentFileIndex = 0; currentFileIndex < sourceFilesTree!.Count; currentFileIndex++)
                {
                    var file = TargetFiles[currentFileIndex];
                    var templateFile = TemplateFiles != null && currentFileIndex < TemplateFiles.Count ? TemplateFiles[currentFileIndex] : null;

                    SetProperties(currentFileIndex, file, templateFile);

                    statistics!.DoneFiles++;
                    UpdateStatistics(ref statistics!);

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

            var pidl = sourceFilesTree![currentFileIndex];
            var guid = typeof(IShellItem).GUID;
            if (Failed(API.SHCreateItemFromIDList(pidl, ref guid, out IShellItem item)))
                return SetFilePropertyResult.Error;

            switch (templateProperty.ID)
            {
                case FilePropertiesTypes.Name:
                    var fileNameProperty = (FileNameProperty)templateProperty;
                    if (fileNameProperty.Value != file.Name)
                    {
                        // Pass null as the third parameter (IFileOperationProgressSink)
                        fileOp.RenameItem(item, fileNameProperty.Value, null!);

                        // Perform the operations and check the result
                        var res = fileOp.PerformOperations();
                        if (Failed(res))
                        {
                            if (res == Constants.COPYENGINE_E_USER_CANCELLED)
                            {
                                RaiseAbortOperation();
                            }
                            else
                            {
                                result = SetFilePropertyResult.Error;
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

            if (AskQuestion(message, "", [FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort],
                           FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
            {
                RaiseAbortOperation();
            }
        }

        private static bool Failed(int hr)
        {
            return hr != 0;
        }


    }


}