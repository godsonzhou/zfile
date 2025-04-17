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

        protected override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyIn;
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

        protected override FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CopyOut;
        }
    }
} 