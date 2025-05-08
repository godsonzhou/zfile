using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP移动操作，用于在FTP服务器上移动或重命名文件和目录
    /// </summary>
    public class FtpMoveOperation : FileSourceMoveOperation
    {
        private readonly FtpFileSource _ftpFileSource;
        private FileSourceCopyOperationStatistics _statistics;

        /// <summary>
        /// 创建FTP移动操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="sourceFiles">源文件列表</param>
        /// <param name="targetPath">目标路径</param>
        public FtpMoveOperation(FtpFileSource fileSource, FileEntries sourceFiles, string targetPath)
            : base(fileSource, sourceFiles, targetPath)
        {
            _ftpFileSource = fileSource;
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
                CheckOperationState();
                
                string targetFilePath = Path.Combine(TargetPath, file.Name).Replace('\\', '/');
                
                // 移动文件或目录
                MoveItem(file.FullPath, targetFilePath);
            }
        }

        /// <summary>
        /// 移动项目
        /// </summary>
        /// <param name="sourcePath">源路径</param>
        /// <param name="targetPath">目标路径</param>
        private void MoveItem(string sourcePath, string targetPath)
        {
            try
            {
                // 更新统计信息
                _statistics.TotalFiles++;
                UpdateStatistics(_statistics);
                
                // 重命名/移动文件或目录
                bool success = _ftpFileSource.Rename(sourcePath, targetPath);
                
                if (success)
                {
                    // 更新统计信息
                    _statistics.DoneFiles++;
                    UpdateStatistics(_statistics);
                }
                else
                {
                    throw new Exception("移动项目失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移动FTP项目失败: {ex.Message}");
                if (AskQuestion($"移动 {sourcePath} 到 {targetPath} 失败: {ex.Message}\n是否继续?", string.Empty,
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
