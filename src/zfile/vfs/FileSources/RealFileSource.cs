namespace zfile
{
    public interface IRealFileSource : IFileSource
    {
    }

    public abstract class RealFileSource : FileSource, IRealFileSource
    {
    }
}