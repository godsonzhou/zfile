//namespace zfile
//{
//    public abstract class FileSourceCopyOperation : IDisposable
//    {
//        protected IFileSource SourceFileSource { get; }
//        protected IFileSource TargetFileSource { get; }
//        protected FileEntries SourceFiles { get; }
//        protected string TargetPath { get; }
//        protected CopyAttributesOption CopyAttributesOptions { get; set; }
//        protected FileSourceOperationOptionGeneral FileExistsOption { get; set; }
//        protected FileSourceOperationOptionGeneral DirExistsOption { get; set; }
//        protected FileSourceOperationOptionGeneral SymLinkOption { get; set; }
//        protected string RenameMask { get; set; }
//        protected Thread Thread { get; }

//        protected FileSourceCopyOperation(
//            IFileSource sourceFileSource,
//            IFileSource targetFileSource,
//            FileEntries sourceFiles,
//            string targetPath)
//        {
//            SourceFileSource = sourceFileSource;
//            TargetFileSource = targetFileSource;
//            SourceFiles = sourceFiles;
//            TargetPath = targetPath;
//            Thread = Thread.CurrentThread;
//        }

//        public abstract void Initialize();
//        public abstract void MainExecute();
//        public abstract void Finalize();
//        protected abstract FileSourceOperationType GetID();

//        protected virtual FileSourceCopyOperationStatistics RetrieveStatistics()
//        {
//            return new FileSourceCopyOperationStatistics();
//        }

//        protected virtual void AskQuestion(string question, out bool answer)
//        {
//            answer = false;
//        }

//        protected virtual void RaiseAbortOperation()
//        {
//        }

//        protected virtual void AppProcessMessages()
//        {
//        }

//        protected virtual void CheckOperationState()
//        {
//        }

//        //protected virtual void UpdateStatistics(FileSourceCopyOperationStatistics statistics)
//        //{
//        //}

//        protected virtual void ShowCompareFilesUI(string sourceFile, string targetFile)
//        {
//        }

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//        }
//    }

    
//} 