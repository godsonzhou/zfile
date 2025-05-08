using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP创建目录操作，用于在FTP服务器上创建目录
    /// </summary>
    public class FtpCreateDirectoryOperation : FileSourceCreateDirectoryOperation
    {
        private readonly FtpFileSource _ftpFileSource;

        /// <summary>
        /// 创建FTP创建目录操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="basePath">基础路径</param>
        /// <param name="directoryPath">目录路径</param>
        public FtpCreateDirectoryOperation(FtpFileSource fileSource, string basePath, string directoryPath)
            : base(fileSource, basePath, directoryPath)
        {
            _ftpFileSource = fileSource;
        }

        /// <summary>
        /// 执行主操作
        /// </summary>
        protected override void MainExecute()
        {
            try
            {
                // 构建完整路径
                string fullPath = Path.Combine(BasePath, DirectoryPath).Replace('\\', '/');
                
                // 创建目录
                bool success = _ftpFileSource.CreateDirectory(fullPath);
                
                if (!success)
                {
                    throw new Exception("创建目录失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建FTP目录失败: {ex.Message}");
                throw;
            }
        }
    }
}
