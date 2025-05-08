using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP计算统计信息操作，用于计算FTP目录的统计信息
    /// </summary>
    public class FtpCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private readonly FtpFileSource _ftpFileSource;
        private FileSourceCalcStatisticsOperationStatistics _statistics;

        /// <summary>
        /// 创建FTP计算统计信息操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="files">文件列表</param>
        public FtpCalcStatisticsOperation(FtpFileSource fileSource, FileEntries files)
            : base(fileSource, files)
        {
            _ftpFileSource = fileSource;
            _statistics = new FileSourceCalcStatisticsOperationStatistics();
        }

        /// <summary>
        /// 初始化操作
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            _statistics = RetrieveStatistics();
        }

        /// <summary>
        /// 执行主操作
        /// </summary>
        protected override void MainExecute()
        {
            // 处理文件列表
            foreach (var file in Files)
            {
                CheckOperationState();
                
                if (file.IsDirectory)
                {
                    // 处理目录
                    ProcessDirectory(file.FullPath);
                }
                else
                {
                    // 处理文件
                    ProcessFile(file);
                }
            }
        }

        /// <summary>
        /// 处理目录
        /// </summary>
        /// <param name="path">目录路径</param>
        private void ProcessDirectory(string path)
        {
            try
            {
                // 更新统计信息
                _statistics.Directories++;
                UpdateStatistics(_statistics);
                
                // 获取目录列表
                var listing = _ftpFileSource.Client.GetListing(path);
                
                // 处理目录中的所有文件和子目录
                foreach (var item in listing)
                {
                    CheckOperationState();
                    
                    if (item.Name == "." || item.Name == "..")
                        continue;
                    
                    if (item.Type == FtpObjectType.Directory)
                    {
                        // 递归处理子目录
                        ProcessDirectory(item.FullName);
                    }
                    else
                    {
                        // 处理文件
                        ProcessFile(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理FTP目录统计信息失败: {ex.Message}");
                if (!AskQuestion($"处理目录 {path} 统计信息失败: {ex.Message}\n是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    == FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }

        /// <summary>
        /// 处理文件
        /// </summary>
        /// <param name="file">文件条目</param>
        private void ProcessFile(FileEntry file)
        {
            try
            {
                // 更新统计信息
                _statistics.Files++;
                _statistics.Size += file.Size;
                UpdateStatistics(_statistics);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理FTP文件统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理文件
        /// </summary>
        /// <param name="item">FTP列表项</param>
        private void ProcessFile(FtpListItem item)
        {
            try
            {
                // 更新统计信息
                _statistics.Files++;
                _statistics.Size += item.Size;
                UpdateStatistics(_statistics);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理FTP文件统计信息失败: {ex.Message}");
            }
        }
    }
}
