using System;
using System.Windows.Forms;

namespace FileSystemOperations
{
    public class VfsExecuteOperation : FileSourceExecuteOperation
    {
        private readonly IVfsFileSource _vfsFileSource;

        public VfsExecuteOperation(IFileSource targetFileSource, ref FileInfo executableFile, string currentPath, string verb)
            : base(targetFileSource, ref executableFile, currentPath, verb)
        {
            _vfsFileSource = targetFileSource as IVfsFileSource;
        }

        public override void Initialize()
        {
        }

        public override void MainExecute()
        {
            ExecuteOperationResult = FileSourceExecuteOperationResult.Success;

            if (string.Equals(Verb, "properties", StringComparison.OrdinalIgnoreCase))
            {
                var index = _vfsFileSource.VfsFileList.FindFirstEnabledByName(RelativePath);
                if (index >= 0)
                {
                    var wfxModule = GlobalSettings.WfxPlugins.LoadModule(_vfsFileSource.VfsFileList.FileName[index]);
                    if (wfxModule != null)
                    {
                        wfxModule.VfsInit();
                        wfxModule.VfsConfigure(Application.OpenForms[0].Tag);
                    }
                }
            }
        }

        public override void Finalize()
        {
        }
    }
} 