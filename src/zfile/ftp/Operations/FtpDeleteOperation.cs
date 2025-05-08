using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP删除操作，用于删除FTP服务器上的文件和目录
    /// </summary>
    public class FtpDeleteOperation : FileSourceDeleteOperation
    {
        private readonly FtpFileSource _ftpFileSource;
        private FileSourceDeleteOperationStatistics _statistics;

        /// <summary>
        /// 创建FTP删除操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="filesToDelete">要删除的文件列表</param>
        public FtpDeleteOperation(FtpFileSource fileSource, FileEntries filesToDelete)
            : base(fileSource, filesToDelete)
        {
            _ftpFileSource = fileSource;
            _statistics = new FileSourceDeleteOperationStatistics();
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
            // 处理要删除的文件
            foreach (var file in FilesToDelete)
            {
                CheckOperationState();
                
                if (file.IsDirectory)
                {
                    // 删除目录
                    DeleteDirectory(file.FullPath);
                }
                else
                {
                    // 删除文件
                    DeleteFile(file.FullPath);
                }
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="path">目录路径</param>
        private void DeleteDirectory(string path)
        {
            try
            {
                // 获取目录列表
                var listing = _ftpFileSource.Client.GetListing(path);
                
                // 先删除目录中的所有文件和子目录
                foreach (var item in listing)
                {
                    CheckOperationState();
                    
                    if (item.Name == "." || item.Name == "..")
                        continue;
                    
                    if (item.Type == FtpObjectType.Directory)
                    {
                        // 递归删除子目录
                        DeleteDirectory(item.FullName);
                    }
                    else
                    {
                        // 删除文件
                        DeleteFile(item.FullName);
                    }
                }
                
                // 删除空目录
                bool success = _ftpFileSource.DeleteDirectory(path);
                
                if (success)
                {
                    // 更新统计信息
                    _statistics.DoneFiles++;
                    UpdateStatistics(_statistics);
                }
                else
                {
                    throw new Exception("删除目录失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除FTP目录失败: {ex.Message}");
                if (!AskQuestion($"删除目录 {path} 失败: {ex.Message}\n是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    == FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        private void DeleteFile(string path)
        {
            try
            {
                // 更新统计信息
                _statistics.TotalFiles++;
                UpdateStatistics(_statistics);
                
                // 删除文件
                bool success = _ftpFileSource.DeleteFile(path);
                
                if (success)
                {
                    // 更新统计信息
                    _statistics.DoneFiles++;
                    UpdateStatistics(_statistics);
                }
                else
                {
                    throw new Exception("删除文件失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除FTP文件失败: {ex.Message}");
                if (!AskQuestion($"删除文件 {path} 失败: {ex.Message}\n是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    == FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }
    }
}
