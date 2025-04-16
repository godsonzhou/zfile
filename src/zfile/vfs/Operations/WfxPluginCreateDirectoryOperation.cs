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

        protected override void Initialize()
        {
        }

        protected override void MainExecute()
        {
            var result = _wfxPluginFileSource.WfxModule.WfxMkDir(BasePath, AbsolutePath);
            switch (result)
            {
                case WfxResult.NotSupported:
                    AskQuestion(Resources.MsgErrNotSupported, string.Empty, new[] { FileSourceOperationUIResponse.Ok }, FileSourceOperationUIResponse.Ok, FileSourceOperationUIResponse.Ok);
                    break;
                case WfxResult.Success:
                    if ((LogOption.VfsOp & LogOption.Success) != 0)
                    {
                        Logger.Write(Thread, string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, AbsolutePath), LogOption.Success);
                    }
                    break;
                default:
                    if ((LogOption.VfsOp & LogOption.Error) != 0)
                    {
                        Logger.Write(Thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, AbsolutePath), LogOption.Error);
                    }
                    break;
            }
        }

        protected override void Finalize()
        {
        }
    }
} 