namespace zfile
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

        protected override void MainExecute()
        {
            if (shellFileSource.CreateDirectory(AbsolutePath))
            {
                if (GlobalSettings.LogDirectoryOperations && GlobalSettings.LogSuccess)
                {
                    Logger.Write(_thread,
                               string.Format("Success: Create directory {0}", AbsolutePath),
                               LogOption.Success);
                }
            }
            else
            {
                if (GlobalSettings.LogDirectoryOperations && GlobalSettings.LogErrors)
                {
                    Logger.Write(_thread,
                               string.Format("Error: Create directory {0}", AbsolutePath),
                               LogOption.Error);
                }

                if (System.Windows.Forms.MessageBox.Show(string.Format("Error creating directory: {0}", AbsolutePath),
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