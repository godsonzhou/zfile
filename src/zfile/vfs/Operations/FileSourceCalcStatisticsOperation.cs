using System;
using System.Threading;

namespace zfile
{
    /// <summary>
    /// 文件源统计操作的统计信息记录结构
    /// </summary>
    public struct FileSourceCalcStatisticsOperationStatistics
    {
        public FilePropertyType SupportedProperties;
        public string CurrentFile;
        public long Files;          // 仅文件，即非目录
        public long Directories;
        public long Links;
        public long Size;           // 所有文件的总大小
        public long CompressedSize; // 如果支持fpCompressedSize
        public DateTime OldestFile; // 如果支持fpModificationTime（或fpDateTime）
        public DateTime NewestFile;
        public long FilesPerSecond;
        // 可能还有其他：
        // SystemFiles
        // ReadOnlyFiles
        // ExecutableFiles
    }

    /// <summary>
    /// 计算目录树的各种统计信息的操作
    /// </summary>
    public abstract class FileSourceCalcStatisticsOperation : FileSourceOperation
    {
        private FileSourceCalcStatisticsOperationStatistics _statistics;
        private FileSourceCalcStatisticsOperationStatistics _statisticsAtStartTime;
        private readonly ReaderWriterLockSlim _statisticsLock = new ReaderWriterLockSlim(); // 用于同步统计信息
        private readonly IFileSource _fileSource;
        private readonly FileEntries _files;

        // 选项
        private FileSourceOperationSymLinkOption _symLinkOption;
        private bool _skipErrors;

        /// <summary>
        /// 创建文件源统计操作的实例
        /// </summary>
        /// <param name="targetFileSource">目标文件源</param>
        /// <param name="files">要统计的文件列表</param>
        public FileSourceCalcStatisticsOperation(IFileSource targetFileSource, FileEntries files)
            : base(targetFileSource)
        {
            _statistics = new FileSourceCalcStatisticsOperationStatistics
            {
                SupportedProperties = targetFileSource.SupportedFileProperties,
                CurrentFile = string.Empty,
                Files = 0,
                Directories = 0,
                Links = 0,
                Size = 0,
                CompressedSize = 0,
                OldestFile = DateTime.MaxValue,
                NewestFile = DateTime.MinValue
            };

            _fileSource = targetFileSource;
            _files = files;

            _symLinkOption = FileSourceOperationSymLinkOption.None;
            _skipErrors = GlobalSettings.SkipFileOpError;
        }

        /// <summary>
        /// 析构函数，释放资源
        /// </summary>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _statisticsLock.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 获取操作描述
        /// </summary>
        /// <param name="details">描述详细程度</param>
        /// <returns>操作描述字符串</returns>
        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            switch (details)
            {
                case FileSourceOperationDescriptionDetails.JobAndTarget:
                    return string.Format("正在计算统计信息: {0}", _files.Path);
                default:
                    return "正在计算统计信息";
            }
        }

        /// <summary>
        /// 获取操作类型ID
        /// </summary>
        /// <returns>操作类型</returns>
        public FileSourceOperationType GetID()
        {
            return FileSourceOperationType.CalcStatistics;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="newStatistics">新的统计信息</param>
        protected void UpdateStatistics(FileSourceCalcStatisticsOperationStatistics newStatistics)
        {
            _statisticsLock.EnterWriteLock();
            try
            {
                // 无法确定此操作的进度或剩余时间
                // 仅计算速度

                EstimateRemainingTime(_statisticsAtStartTime.Files,
                                     newStatistics.Files,
                                     0, // 未知
                                     StartTime,
                                     DateTime.Now,
                                     out newStatistics.FilesPerSecond);

                _statistics = newStatistics;
            }
            finally
            {
                _statisticsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 更新开始时间的统计信息
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            _statisticsLock.EnterWriteLock();
            try
            {
                _statisticsAtStartTime = _statistics;
            }
            finally
            {
                _statisticsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 检索当前统计信息
        /// </summary>
        /// <returns>统计信息结构</returns>
        public FileSourceCalcStatisticsOperationStatistics RetrieveStatistics()
        {
            // 统计信息必须同步，因为有多个值，它们在每个时刻都必须保持一致
            _statisticsLock.EnterReadLock();
            try
            {
                return _statistics;
            }
            finally
            {
                _statisticsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 符号链接选项
        /// </summary>
        public FileSourceOperationSymLinkOption SymLinkOption
        {
            get { return _symLinkOption; }
            set { _symLinkOption = value; }
        }

        /// <summary>
        /// 是否跳过错误
        /// </summary>
        public bool SkipErrors
        {
            get { return _skipErrors; }
            set { _skipErrors = value; }
        }

        /// <summary>
        /// 文件源
        /// </summary>
        protected IFileSource FileSource => _fileSource;

        /// <summary>
        /// 文件列表
        /// </summary>
        protected FileEntries Files => _files;

        /// <summary>
        /// 估算剩余时间
        /// </summary>
        protected void EstimateRemainingTime(long startValue, long currentValue, long totalValue, 
            DateTime startTime, DateTime currentTime, out long itemsPerSecond)
        {
            // 计算每秒处理的项目数
            TimeSpan elapsedTime = currentTime - startTime;
            long processedItems = currentValue - startValue;

            if (elapsedTime.TotalSeconds > 0)
                itemsPerSecond = (long)(processedItems / elapsedTime.TotalSeconds);
            else
                itemsPerSecond = 0;
        }
    }
}