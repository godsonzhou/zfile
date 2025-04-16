namespace zfile
{
    public class WfxPluginCreateDirectoryOperation : FileSourceCreateDirectoryOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;

        public WfxPluginCreateDirectoryOperation(IFileSource targetFileSource, string currentPath, string directoryPath)
            : base(targetFileSource, currentPath, directoryPath)
        {
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;
        }

        public override void Initialize()
        {
        }

        public override void MainExecute()
        {
            var result = _wfxPluginFileSource.WfxModule.WfxMkDir(BasePath, AbsolutePath);
            switch (result)
            {
                case WfxResult.NotSupported:
                    AskQuestion(Resources.MsgErrNotSupported, string.Empty, new[] { FileSourceOperationUIResult.Ok }, FileSourceOperationUIResult.Ok, FileSourceOperationUIResult.Ok);
                    break;
                case WfxResult.Success:
                    if ((LogOption.VfsOp & LogOption.Success) != 0)
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, AbsolutePath), LogOption.Success);
                    }
                    break;
                default:
                    if ((LogOption.VfsOp & LogOption.Error) != 0)
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, AbsolutePath), LogOption.Error);
                    }
                    break;
            }
        }

        public override void Finalize()
        {
        }
    }
} 