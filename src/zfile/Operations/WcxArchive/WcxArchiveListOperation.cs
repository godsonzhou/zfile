using System;
using System.Collections.Generic;
using System.IO;

namespace ZFile.Operations.WcxArchive
{
    public class WcxArchiveListOperation : FileSourceListOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;
        private bool _flatView;

        public WcxArchiveListOperation(IFileSource fileSource, string path) : base(fileSource, path)
        {
            Files = new List<FileEntry>();
            _wcxArchiveFileSource = (IWcxArchiveFileSource)fileSource;
        }

        public override void MainExecute()
        {
            Files.Clear();

            if (_wcxArchiveFileSource.Changed)
            {
                _wcxArchiveFileSource.Reload(Path);
            }

            if (!FileSource.IsPathAtRoot(Path))
            {
                var parentDir = new FileEntry
                {
                    Name = "..",
                    Attributes = FileAttributes.Directory
                };
                Files.Add(parentDir);
            }

            var arcFileList = _wcxArchiveFileSource.ArchiveFileList.Clone();
            try
            {
                foreach (var header in arcFileList)
                {
                    CheckOperationState();

                    var currFileName = Path.DirectorySeparatorChar + header.FileName;

                    if (!IsInPath(Path, currFileName, _flatView, false))
                        continue;

                    FileEntry file;
                    if (!_flatView)
                    {
                        file = WcxArchiveFileSource.CreateFile(Path, header);
                    }
                    else
                    {
                        if ((header.FileAttr & FileAttributes.Directory) != 0)
                            continue;
                        file = WcxArchiveFileSource.CreateFile(System.IO.Path.GetDirectoryName(currFileName), header);
                    }

                    Files.Add(file);
                }
            }
            finally
            {
                arcFileList.Dispose();
            }
        }
    }
}