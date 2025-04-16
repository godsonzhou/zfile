namespace zfile
{
    /// <summary>
    /// Statistics for copy operation
    /// </summary>
    public struct FileSourceCopyOperationStatistics
    {
        public string CurrentFileFrom;
        public string CurrentFileTo;
        public long CurrentFileTotalBytes;
        public long CurrentFileDoneBytes;
        public long TotalFiles;
        public long DoneFiles;
        public long TotalBytes;
        public long DoneBytes;
        public long BytesPerSecond;
        public DateTime RemainingTime;
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
        Abort
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
        Abort
    }

   
}