using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP执行操作，用于执行FTP服务器上的文件
    /// </summary>
    public class FtpExecuteOperation : FileSourceExecuteOperation
    {
        private readonly FtpFileSource _ftpFileSource;

        /// <summary>
        /// 创建FTP执行操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="executableFile">可执行文件</param>
        /// <param name="basePath">基础路径</param>
        /// <param name="parameters">执行参数</param>
        public FtpExecuteOperation(FtpFileSource fileSource, FileEntry executableFile, string basePath, string parameters)
            : base(fileSource, executableFile, basePath, parameters)
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
                // 下载文件到临时目录
                string localPath = _ftpFileSource.DownloadFile(ExecutableFile.FullPath);
                
                if (string.IsNullOrEmpty(localPath))
                {
                    throw new Exception("下载文件失败");
                }
                
                // 执行本地文件
                Process process = new Process();
                process.StartInfo.FileName = localPath;
                
                if (!string.IsNullOrEmpty(Parameters))
                {
                    process.StartInfo.Arguments = Parameters;
                }
                
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(localPath);
                process.Start();
                
                // 设置结果
                Result = FileSourceExecuteOperationResult.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行FTP文件失败: {ex.Message}");
                Result = FileSourceExecuteOperationResult.Error;
                throw;
            }
        }
    }
}
