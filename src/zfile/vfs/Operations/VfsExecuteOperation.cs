namespace zfile
{
    public class VfsExecuteOperation : FileSourceExecuteOperation
    {
        private readonly IVfsFileSource _vfsFileSource;

        public VfsExecuteOperation(IFileSource targetFileSource, ref FileEntry executableFile, string currentPath, string verb)
            : base(targetFileSource, executableFile, currentPath, verb)
        {
            _vfsFileSource = targetFileSource as IVfsFileSource;
        }

        protected override void Initialize()
        {
        }

        protected override void MainExecute()
        {
            ExecuteOperationResult = FileSourceExecuteOperationResult.Success;

            if (string.Equals(Verb, "properties", StringComparison.OrdinalIgnoreCase))
            {
                var index = _vfsFileSource.VfsFileEntries.FindFirstEnabledByName(RelativePath);
                if (index >= 0)
                {
                    var wfxModule = WfxPlugins.LoadModule(_vfsFileSource.VfsFileEntries.FileName[index]);
                    if (wfxModule != null)
                    {
                        wfxModule.VfsInit();
                        // 获取主窗体的句柄作为父窗口
                        var mainForm = Application.OpenForms[0];
                        if (mainForm != null && mainForm.Tag != null)
                        {
                            wfxModule.VfsConfigure(mainForm.Tag);
                        }
                        else if (mainForm != null)
                        {
                            wfxModule.VfsConfigure(mainForm.Handle);
                        }
                    }
                }
            }
        }

        protected new void Finalize()
        {
            // 清理资源
            base.Finalize();
        }
    }
}