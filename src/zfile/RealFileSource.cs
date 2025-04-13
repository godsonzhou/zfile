using System;

namespace Files.FileSources
{
    public interface IRealFileSource : IFileSource
    {
    }

    public class RealFileSource : FileSource, IRealFileSource
    {
    }
}