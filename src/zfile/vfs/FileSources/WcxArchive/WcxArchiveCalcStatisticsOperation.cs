using System;

namespace zfile
{
    /// <summary>
    /// Operation for calculating statistics of files in a WCX archive
    /// </summary>
    public class WcxArchiveCalcStatisticsOperation : FileSourceOperation
    {
        private readonly IWcxArchiveFileSource _fileSource;
        private readonly FileEntries _files;

        /// <summary>
        /// Initializes a new instance of the <see cref="WcxArchiveCalcStatisticsOperation"/> class
        /// </summary>
        /// <param name="fileSource">The file source</param>
        /// <param name="files">The files to calculate statistics for</param>
        public WcxArchiveCalcStatisticsOperation(IWcxArchiveFileSource fileSource, FileEntries files)
            : base(fileSource)
        {
            _fileSource = fileSource;
            _files = files;
            OperationType = FileSourceOperationType.CalcStatistics;
        }

        /// <summary>
        /// Executes the operation
        /// </summary>
        protected override void Execute()
        {
            // Implementation of statistics calculation
            // This is a placeholder implementation
            long totalSize = 0;
            long totalCompressedSize = 0;

            foreach (var file in _files)
            {
                totalSize += file.Size;
                totalCompressedSize += file.CompressedSize;
            }

            // Set the result
            SetResult(new FileSourceOperationResult
            {
                Success = true,
                Message = $"Total size: {totalSize} bytes, Compressed size: {totalCompressedSize} bytes"
            });
        }
    }
}
