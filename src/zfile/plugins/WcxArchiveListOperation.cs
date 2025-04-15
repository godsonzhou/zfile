using System;
using System.Collections.Generic;
using System.IO;

namespace Files.FileSources.WcxArchive
{
    public class WcxArchiveListOperation : FileSourceListOperation
    {
        private IWcxArchiveFileSource _wcxArchiveFileSource;

        public WcxArchiveListOperation(IFileSource fileSource, string path) : base(fileSource, path)
        {
            _files = new Files(path);
            _wcxArchiveFileSource = (IWcxArchiveFileSource)fileSource;
        }

        public override void MainExecute()
        {
            _files.Clear();

            if (_wcxArchiveFileSource.Changed)
            {
                _wcxArchiveFileSource.Reload(Path);
            }

            if (!FileSource.IsPathAtRoot(Path))
            {
                var file = WcxArchiveFileSource.CreateFile(Path);
                file.Name = "..";
                file.Attributes = FileAttributes.Directory;
                _files.Add(file);
            }

            var arcFileList = _wcxArchiveFileSource.ArchiveFileList.Clone();
            try
            {
                foreach (var item in arcFileList)
                {
                    CheckOperationState();

                    var header = (WcxHeader)item;
                    string currFileName = Path.DirectorySeparatorChar + header.FileName;

                    if (!IsInPath(Path, currFileName, _flatView, false))
                        continue;

                    File file;
                    if (!_flatView)
                        file = WcxArchiveFileSource.CreateFile(Path, header);
                    else
                    {
                        if (FileAttributes.IsDirectory(header.FileAttr)) continue;
                        file = WcxArchiveFileSource.CreateFile(Path.GetDirectoryName(currFileName), header);
                    }

                    _files.Add(file);
                }
            }
            finally
            {
                arcFileList = null;
            }
        }
    }
}