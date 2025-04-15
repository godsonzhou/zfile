using System;
using System.Runtime.InteropServices;

namespace FileSystemOperations
{
    public class ShellCreateDirectoryOperation : FileSourceCreateDirectoryOperation
    {
        private IShellFileSource shellFileSource;

        public ShellCreateDirectoryOperation(IFileSource targetFileSource,
                                          string currentPath,
                                          string directoryPath)
            : base(targetFileSource, currentPath, directoryPath)
        {
            shellFileSource = targetFileSource as IShellFileSource;
        }

        public override void MainExecute()
        {
            if (shellFileSource.CreateDirectory(AbsolutePath))
            {
                if (GlobalSettings.LogDirectoryOperations && GlobalSettings.LogSuccess)
                {
                    Logger.Write(Thread.CurrentThread,
                               string.Format("Success: Create directory {0}", AbsolutePath),
                               LogMessageType.Success);
                }
            }
            else
            {
                if (GlobalSettings.LogDirectoryOperations && GlobalSettings.LogErrors)
                {
                    Logger.Write(Thread.CurrentThread,
                               string.Format("Error: Create directory {0}", AbsolutePath),
                               LogMessageType.Error);
                }

                if (MessageBox.Show(string.Format("Error creating directory: {0}", AbsolutePath),
                                  "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error) == DialogResult.OK)
                {
                    // 用户确认错误
                }
            }
        }
    }

    public static class GlobalSettings
    {
        public static bool LogDirectoryOperations { get; set; }
        public static bool LogSuccess { get; set; }
        public static bool LogErrors { get; set; }
    }

    public static class Logger
    {
        public static void Write(System.Threading.Thread thread, string message, LogMessageType type)
        {
            // 实现日志记录逻辑
        }
    }

    public enum LogMessageType
    {
        Error,
        Info,
        Success
    }
} 