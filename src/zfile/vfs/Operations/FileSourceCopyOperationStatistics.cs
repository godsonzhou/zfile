namespace zfile
{
    /// <summary>
    /// Statistics for copy operation
    /// </summary>
    public struct FileSourceCopyOperationStatistics
    {
        public string? CurrentFileFrom;
        public string? CurrentFileTo;
        public long CurrentFileTotalBytes;
        public long CurrentFileDoneBytes;
        public long TotalFiles;
        public long DoneFiles;
        public long TotalBytes;
        public long DoneBytes;
        public long BytesPerSecond;
        public DateTime RemainingTime;
        public long SkippedFiles;
        public long SkippedBytes;
        public long FailedFiles;
        public long FailedBytes;
        public long TotalDirectories;
        public long DoneDirectories;
        public long SkippedDirectories;
    }

    /// <summary>
    /// File exists option for operations
    /// </summary>
    public enum FileSourceOperationOptionFileExists
    {
        None,
        Overwrite,
        OverwriteOlder,
        OverwriteSmaller,
        OverwriteLarger,
        Skip,
        SkipAll,
        AutoRename,
        Resume,
        ResumeAll,
        Abort,
        Append,
        AutoRenameSource
    }

    /// <summary>
    /// Directory exists option for operations
    /// </summary>
    public enum FileSourceOperationOptionDirectoryExists
    {
        None,
        Merge,
        Skip,
        SkipAll,
        Abort,
        CopyInto,
        Delete
    }
}