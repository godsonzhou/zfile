using System;
using System.IO;

namespace Files.FileSources.WcxArchive
{
    public class WcxArchiveExecuteOperation : FileSourceExecuteOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;

        public WcxArchiveExecuteOperation(IFileSource targetFileSource,
                                         File executableFile,
                                         string currentPath,
                                         string verb) : base(targetFileSource, executableFile, currentPath, verb)
        {
            _wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
        }

        public override void Initialize()
        {
            // No initialization needed
        }

        public override void MainExecute()
        {
            if (Verb != "properties" && MatchesMaskList(ExecutableFile.Name, GlobalSettings.AutoExtractOpenMask))
                _executeOperationResult = FileSourceExecuteOperationResult.YourSelf;
            else
                _executeOperationResult = ShowPackInfoDlg(_wcxArchiveFileSource, ExecutableFile);
        }

        public override void Finalize()
        {
            // No finalization needed
        }
    }
}