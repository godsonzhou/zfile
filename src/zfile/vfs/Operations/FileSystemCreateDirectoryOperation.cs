using System;
using System.IO;
using System.Threading.Tasks;

namespace FileSystemOperations
{
    public class FileSystemCreateDirectoryOperation : FileSourceCreateDirectoryOperation
    {
        private IFileSystemFileSource fileSystemFileSource;

        public FileSystemCreateDirectoryOperation(
            IFileSource targetFileSource,
            string currentPath,
            string directoryPath)
            : base(targetFileSource, currentPath, directoryPath)
        {
            fileSystemFileSource = targetFileSource as IFileSystemFileSource;
        }

        public override void Initialize()
        {
            // 初始化操作，当前不需要额外处理
        }

        public override void MainExecute()
        {
            try
            {
                // 检查目录是否已存在
                if (Directory.Exists(AbsolutePath))
                {
                    AskQuestion(string.Format(Resources.MsgErrDirExists, AbsolutePath), 
                        string.Empty, 
                        new[] { FileSourceOperationUIResponse.Ok }, 
                        FileSourceOperationUIResponse.Ok, 
                        FileSourceOperationUIResponse.Ok);
                    return;
                }

                // 尝试创建目录
                if (!ForceDirectoriesUAC(AbsolutePath))
                {
                    // 记录错误日志
                    if (GlobalSettings.LogOptions.HasFlag(LogOptions.DirectoryOperations) && 
                        GlobalSettings.LogOptions.HasFlag(LogOptions.Errors))
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, AbsolutePath), 
                            LogMessageType.Error);
                    }

                    AskQuestion(string.Format(Resources.MsgErrForceDir, AbsolutePath), 
                        string.Empty, 
                        new[] { FileSourceOperationUIResponse.Ok }, 
                        FileSourceOperationUIResponse.Ok, 
                        FileSourceOperationUIResponse.Ok);
                }
                else
                {
                    // 记录成功日志
                    if (GlobalSettings.LogOptions.HasFlag(LogOptions.DirectoryOperations) && 
                        GlobalSettings.LogOptions.HasFlag(LogOptions.Success))
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, AbsolutePath), 
                            LogMessageType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Log.Write(Thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir + ": {0}", 
                    AbsolutePath, ex.Message), LogMessageType.Error);
                throw;
            }
        }

        public override void Finalize()
        {
            // 清理操作，当前不需要额外处理
        }

        private bool ForceDirectoriesUAC(string path)
        {
            try
            {
                // 检查是否需要管理员权限
                if (Administrator.IsElevated)
                {
                    Directory.CreateDirectory(path);
                    return true;
                }
                else
                {
                    // 尝试使用普通权限创建
                    Directory.CreateDirectory(path);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 如果需要管理员权限，尝试使用管理员权限
                return Administrator.RunElevated(() => Directory.CreateDirectory(path));
            }
        }
    }
} 