using System;

namespace Zfile.FileSources
{
    public interface IRealFileSource : IFileSource
    {
    }

    public class RealFileSource : FileSource, IRealFileSource
    {
    }
}