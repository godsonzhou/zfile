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
                    var wfxModule = GlobalSettings.WfxPlugins.LoadModule(_vfsFileSource.VfsFileEntries.FileName[index]);
                    if (wfxModule != null)
                    {
                        wfxModule.VfsInit();
                        wfxModule.VfsConfigure(Application.OpenForms[0].Tag);
                    }
                }
            }
        }

        protected override void Finalize()
        {
        }
    }
} 