using Zfile;
using Zfile.FileSources;
namespace ZFile.FileSources.WcxArchive
{
	public interface IWcxArchiveFileSource : IArchiveFileSource
	{
		ThreadSafeList<WcxHeader> ArchiveFileList { get; }
		int PluginCapabilities { get; }
		WcxModule WcxModule { get; }
	}
}