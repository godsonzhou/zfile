using System;
using System.Runtime.InteropServices;

namespace FileSystemOperations
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
                    if ((LogOptions.VfsOp & LogOptions.Success) != 0)
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogSuccess + Resources.MsgLogMkDir, AbsolutePath), LogMessageType.Success);
                    }
                    break;
                default:
                    if ((LogOptions.VfsOp & LogOptions.Errors) != 0)
                    {
                        Log.Write(Thread, string.Format(Resources.MsgLogError + Resources.MsgLogMkDir, AbsolutePath), LogMessageType.Error);
                    }
                    break;
            }
        }

        public override void Finalize()
        {
        }
    }
} 