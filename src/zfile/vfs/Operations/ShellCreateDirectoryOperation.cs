using Zfile.Operations;
using Zfile.FileSources;
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

  
} 