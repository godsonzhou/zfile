using System;
using Zfile;
using Zfile.FileSources;
namespace ZFile.FileSources.WcxArchive
{
    public class WcxArchiveFileSourceConnection : FileSourceConnection
    {
        private WcxModule _wcxModule;

        public WcxModule WcxModule => _wcxModule;

        public WcxArchiveFileSourceConnection(WcxModule wcxModule)
        {
            _wcxModule = wcxModule ?? throw new ArgumentNullException(nameof(wcxModule));
        }

        //public override void Dispose()
        //{
        //    _wcxModule = null;
        //    base.Dispose();
        //}
    }
}