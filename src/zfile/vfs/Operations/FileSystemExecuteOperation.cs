namespace zfile
{
    public class FileSystemExecuteOperation : FileSourceExecuteOperation
    {
        private IFileSystemFileSource fileSystemFileSource;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="targetFileSource">应该执行文件的文件源</param>
        /// <param name="executableFile">应该执行的文件</param>
        /// <param name="currentPath">执行应该发生的文件源路径</param>
        /// <param name="verb">执行动作</param>
        public FileSystemExecuteOperation(
            IFileSource targetFileSource,
            FileInfo executableFile,
            string currentPath,
            string verb)
            : base(targetFileSource, executableFile, currentPath, verb)
        {
            fileSystemFileSource = targetFileSource as IFileSystemFileSource;
        }

        public override void Initialize()
        {
            Cursor.Current = Cursors.WaitCursor;
        }

        public override void MainExecute()
        {
            if (Verb == "properties")
            {
                ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                var files = new List<FileInfo> { ExecutableFile };
                try
                {
                    Cursor.Current = Cursors.Default;
                    ShowFilePropertiesDialog(fileSystemFileSource, files);
                }
                catch (ContextMenuException ex)
                {
                    ShowException(ex);
                }
                return;
            }

            // 如果文件是文件夹的链接，则返回SymLink
            string resultString;
            if (FileIsLinkToFolder(AbsolutePath, out resultString))
            {
                ExecuteOperationResult = FileSourceExecuteOperationResult.SymLink;
                return;
            }

            // 尝试通过系统打开
            Directory.SetCurrentDirectory(CurrentPath);
            switch (ShellExecute(AbsolutePath))
            {
                case true:
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                    break;
                case false:
                    ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
                    break;
            }
        }

        public override void Finalize()
        {
            Cursor.Current = Cursors.Default;
        }

        private bool FileIsLinkToFolder(string path, out string resultString)
        {
            resultString = string.Empty;
            try
            {
                if (File.Exists(path))
                {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        var targetPath = File.ResolveLinkTarget(path, true);
                        if (targetPath != null && Directory.Exists(targetPath.FullName))
                        {
                            resultString = targetPath.FullName;
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略错误
            }
            return false;
        }

        private bool ShellExecute(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ShowFilePropertiesDialog(IFileSystemFileSource fileSource, List<FileInfo> files)
        {
            // 实现文件属性对话框显示
            // 这里需要根据实际UI框架来实现
        }

        private void ShowException(Exception ex)
        {
            // 实现异常显示
            // 这里需要根据实际UI框架来实现
        }
    }
} 