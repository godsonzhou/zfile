//namespace zfile
//{
//    public abstract class FileSourceCopyOperation : IDisposable
//    {
//        protected Thread Thread { get; }

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

//        protected virtual void RaiseAbortOperation() {}

//        protected virtual void AppProcessMessages(){}

//        protected virtual void CheckOperationState(){}

//        //protected virtual void UpdateStatistics(FileSourceCopyOperationStatistics statistics){}

//        protected virtual void ShowCompareFilesUI(string sourceFile, string targetFile){}

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//    }

//} 