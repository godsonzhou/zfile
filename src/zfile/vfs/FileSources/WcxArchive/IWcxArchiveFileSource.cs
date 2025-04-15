namespace Zfile.FileSources
{
	public interface IWcxArchiveFileSource : IArchiveFileSource
	{
		ThreadSafeList<WcxHeader> ArchiveFileList { get; }
		int PluginCapabilities { get; }
		WcxModule WcxModule { get; }
	}
}