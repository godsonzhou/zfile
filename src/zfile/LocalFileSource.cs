using System;

namespace Files.FileSources
{
	public interface ILocalFileSource : IRealFileSource
	{
	}

	public class LocalFileSource : RealFileSource, ILocalFileSource
	{
	}
}