using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileSystemOperations
{
    public interface IFileSource
    {
        string CurrentPath { get; }
        bool IsPathAtRoot(string path);
        bool IsPathAtRoot(FileInfo file);
        bool IsPathAtRoot(DirectoryInfo dir);
        bool IsPathAtRoot(FileSystemInfo fileSystemInfo);
        bool IsPathAtRoot(string path, out string parentPath);
        bool IsPathAtRoot(FileInfo file, out string parentPath);
        bool IsPathAtRoot(DirectoryInfo dir, out string parentPath);
        bool IsPathAtRoot(FileSystemInfo fileSystemInfo, out string parentPath);
        bool IsPathAtRoot(string path, out string parentPath, out string rootPath);
        bool IsPathAtRoot(FileInfo file, out string parentPath, out string rootPath);
        bool IsPathAtRoot(DirectoryInfo dir, out string parentPath, out string rootPath);
        bool IsPathAtRoot(FileSystemInfo fileSystemInfo, out string parentPath, out string rootPath);
        bool IsPathAtRoot(string path, out string parentPath, out string rootPath, out string rootName);
        bool IsPathAtRoot(FileInfo file, out string parentPath, out string rootPath, out string rootName);
        bool IsPathAtRoot(DirectoryInfo dir, out string parentPath, out string rootPath, out string rootName);
        bool IsPathAtRoot(FileSystemInfo fileSystemInfo, out string parentPath, out string rootPath, out string rootName);
    }

    public abstract class FileSourceCopyOperation : IDisposable
    {
        protected IFileSource SourceFileSource { get; }
        protected IFileSource TargetFileSource { get; }
        protected List<FileInfo> SourceFiles { get; }
        protected string TargetPath { get; }
        protected CopyAttributesOption CopyAttributesOptions { get; set; }
        protected FileSourceOperationOptionGeneral FileExistsOption { get; set; }
        protected FileSourceOperationOptionGeneral DirExistsOption { get; set; }
        protected FileSourceOperationOptionGeneral SymLinkOption { get; set; }
        protected string RenameMask { get; set; }
        protected Thread Thread { get; }

        protected FileSourceCopyOperation(
            IFileSource sourceFileSource,
            IFileSource targetFileSource,
            List<FileInfo> sourceFiles,
            string targetPath)
        {
            SourceFileSource = sourceFileSource;
            TargetFileSource = targetFileSource;
            SourceFiles = sourceFiles;
            TargetPath = targetPath;
            Thread = Thread.CurrentThread;
        }

        public abstract void Initialize();
        public abstract void MainExecute();
        public abstract void Finalize();
        protected abstract FileSourceOperationType GetID();

        protected virtual FileSourceCopyOperationStatistics RetrieveStatistics()
        {
            return new FileSourceCopyOperationStatistics();
        }

        protected virtual void AskQuestion(string question, out bool answer)
        {
            answer = false;
        }

        protected virtual void RaiseAbortOperation()
        {
        }

        protected virtual void AppProcessMessages()
        {
        }

        protected virtual void CheckOperationState()
        {
        }

        protected virtual void UpdateStatistics(FileSourceCopyOperationStatistics statistics)
        {
        }

        protected virtual void ShowCompareFilesUI(string sourceFile, string targetFile)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    public class FileSourceCopyOperationStatistics
    {
        public long TotalFiles { get; set; }
        public long TotalBytes { get; set; }
        public long DoneFiles { get; set; }
        public long DoneBytes { get; set; }
        public long SkippedFiles { get; set; }
        public long SkippedBytes { get; set; }
        public long FailedFiles { get; set; }
        public long FailedBytes { get; set; }
        public long CurrentFileDoneBytes { get; set; }
        public long CurrentFileTotalBytes { get; set; }
        public string CurrentFileFrom { get; set; }
        public string CurrentFileTo { get; set; }
    }
} 