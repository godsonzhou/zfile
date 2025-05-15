namespace zfile
{
	public interface ILocalFileSource : IRealFileSource
	{
	}

	public abstract class LocalFileSource : RealFileSource, ILocalFileSource
	{
	
	}
}