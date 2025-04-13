using System;
using Zfile.FileSources;
namespace Files.FileSources
{
    public interface IRealFileSource : IFileSource
    {
    }

    public class RealFileSource : FileSource, IRealFileSource
    {
    }
}