using System;
using System.IO;
using System.Threading;
using zfile.Utils;

namespace zfile
{
    public class FileSystemCreateDirectoryOperation : FileSourceCreateDirectoryOperation
    {
        private IFileSystemFileSource? fileSystemFileSource;
        //private readonly TOperationThread _thread;

        public FileSystemCreateDirectoryOperation(
            IFileSource targetFileSource,
            string currentPath,
            string directoryPath)
            : base(targetFileSource, currentPath, directoryPath)
        {
            fileSystemFileSource = targetFileSource as IFileSystemFileSource;
            _thread = new TOperationThread(false, this);
        }

        protected override void Initialize()
        {
            // 初始化操作，当前不需要额外处理
        }

        protected override void MainExecute()
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
                    if (GlobalSettings.LogOptions.HasFlag(LogOption.DirectoryOperation) &&
                        GlobalSettings.LogOptions.HasFlag(LogOption.Error))
                    {
                        Logger.Write(_thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, AbsolutePath),
                            LogOption.Error);
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
                    if (GlobalSettings.LogOptions.HasFlag(LogOption.DirectoryOperation) &&
                        GlobalSettings.LogOptions.HasFlag(LogOption.Success))
                    {
                        Logger.Write(_thread, string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, AbsolutePath),
                            LogOption.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Logger.Write(_thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir + ": {0}",
                    AbsolutePath, ex.Message), LogOption.Error);
                throw;
            }
        }

        protected override void Finalize()
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