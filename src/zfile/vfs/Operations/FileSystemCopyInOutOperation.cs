namespace zfile
{
    public class FileSystemCopyInOperation : FileSystemCopyOperation
    {
        public FileSystemCopyInOperation(
            IFileSource sourceFileSource,
            IFileSource targetFileSource,
            FileEntries sourceFiles,
            string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        protected override FileSourceOperationTypes GetID()
        {
            return FileSourceOperationTypes.CopyIn;
        }
    }

    public class FileSystemCopyOutOperation : FileSystemCopyOperation
    {
        public FileSystemCopyOutOperation(
            IFileSource sourceFileSource,
            IFileSource targetFileSource,
            FileEntries sourceFiles,
            string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
        }

        protected override FileSourceOperationTypes GetID()
        {
            return FileSourceOperationTypes.CopyOut;
        }
    }
} 