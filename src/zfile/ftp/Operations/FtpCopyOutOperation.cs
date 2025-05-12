using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP复制出操作，用于将文件从FTP服务器复制到本地
    /// </summary>
    public class FtpCopyOutOperation : FileSourceCopyOutOperation
    {
        private readonly FtpFileSource _ftpFileSource;
        private FileSourceCopyOperationStatistics _statistics;

        /// <summary>
        /// 创建FTP复制出操作
        /// </summary>
        /// <param name="sourceFileSource">源FTP文件源</param>
        /// <param name="targetFileSource">目标文件源</param>
        /// <param name="sourceFiles">源文件列表</param>
        /// <param name="targetPath">目标路径</param>
        public FtpCopyOutOperation(FtpFileSource sourceFileSource, IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _ftpFileSource = sourceFileSource;
            _statistics = new FileSourceCopyOperationStatistics();
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
            // 处理源文件
            foreach (var file in SourceFiles) 
            {
				//TODO: CURRENT SOURCEFILES USE '\' AS PATH SEPERATOR, SHOULD USE '/' AS DELIMITER INSTEAD. CONSIDER FILE ENTRY DO NOT SUPPORT '/', SO DO CONVERT IS NECESSARY
				//another way is to use ftpfileentry
				//file.FullPath = file.FullPath.Replace('\\', '/');
				CheckOperationState();
                
                if (file.IsDirectory)
                {
					string targetFilePath = Path.Combine(TargetPath, file.Name);
					// 创建目录
					try
					{
						if (!Directory.Exists(targetFilePath))
                            Directory.CreateDirectory(targetFilePath);
                        
                        // 递归处理子目录
                        ProcessDirectory(file.FullPath, targetFilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"创建目录失败: {ex.Message}");
                        if (AskQuestion($"创建目录 {targetFilePath} 失败: {ex.Message}\n", "是否继续?", 
                            new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                            FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                            != FileSourceOperationUIResponse.Yes)
                        {
                            RaiseAbortOperation();
                            return;
                        }
                    }
                }
                else
                {
                    // 复制文件
                    CopyFile(file.FullPath, TargetPath);
                }
            }
        }

        /// <summary>
        /// 处理目录
        /// </summary>
        /// <param name="sourcePath">源目录路径</param>
        /// <param name="targetPath">目标目录路径</param>
        private void ProcessDirectory(string sourcePath, string targetPath)
        {
            try
            {
                // 获取目录列表
                var listing = _ftpFileSource.Client.GetListing(sourcePath);
                
                foreach (var item in listing)
                {
                    CheckOperationState();
                    
                    if (item.Name == "." || item.Name == "..")
                        continue;

                    if (item.Type == FtpObjectType.Directory)
                    {
						string newTargetPath = Path.Combine(targetPath, item.Name);

						// 创建目录
						if (!Directory.Exists(newTargetPath))
                        {
                            Directory.CreateDirectory(newTargetPath);
                        }
                        
                        // 递归处理子目录
                        ProcessDirectory(item.FullName, newTargetPath);
                    }
                    else
                    {
                        // 复制文件
                        CopyFile(item.FullName, targetPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理FTP目录失败: {ex.Message}");
                if (AskQuestion($"处理目录 {sourcePath} 失败: {ex.Message}\n", "是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    != FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标文件路径</param>
        private void CopyFile(string sourcePath, string targetPath)
        {
            try
            {
                // 获取文件大小
                var fileInfo = _ftpFileSource.Client.GetObjectInfo(sourcePath);
                long fileSize = fileInfo.Size;
                
                // 更新统计信息
                _statistics.TotalBytes += fileSize;
                _statistics.TotalFiles++;
                UpdateStatistics(_statistics);
                
                // 下载文件
                string localPath = _ftpFileSource.DownloadFile(sourcePath, targetPath);
                
                if (!string.IsNullOrEmpty(localPath))
                {
                    // 更新统计信息
                    _statistics.DoneBytes += fileSize;
                    _statistics.DoneFiles++;
                    UpdateStatistics(_statistics);
                }
                else
                {
                    throw new Exception("下载文件失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从FTP复制文件失败: {ex.Message}");
                if (AskQuestion($"复制文件 {sourcePath} 到 {targetPath} 失败: {ex.Message}\n", "是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    != FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }
    }
}
