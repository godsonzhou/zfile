//using System;
//using ZFile.FileSources;
//using ZFile.FileSources.WcxArchive;
//using ZFile.Operations;

//namespace ZFile.Operations.WcxArchive
//{
//    public class WcxArchiveExecuteOperation : FileSourceExecuteOperation
//    {
//        private IWcxArchiveFileSource _wcxArchiveFileSource;

//        public WcxArchiveExecuteOperation(IFileSource targetFileSource, File executableFile, string currentPath, string verb)
//            : base(targetFileSource, executableFile, currentPath, verb)
//        {
//            _wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
//        }

//        public override void Initialize()
//        {
//        }

//        public override void MainExecute()
//        {
//            if (Verb != "properties" && Masks.MatchesMaskList(ExecutableFile.Name, Globals.AutoExtractOpenMask))
//            {
//                ExecuteOperationResult = FileSourceExecuteOperationResult.YourSelf;
//            }
//            else
//            {
//                ExecuteOperationResult = PackInfoDialog.Show(_wcxArchiveFileSource, ExecutableFile);
//            }
//        }

//        public override void Finalize()
//        {
//        }
//    }
//}