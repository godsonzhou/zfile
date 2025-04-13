using System;

namespace Zfile.FileSources
{
    public interface IRealFileSource : IFileSource
    {
    }

    public abstract class RealFileSource : FileSource, IRealFileSource
    {
    }
}