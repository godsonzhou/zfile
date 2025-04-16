namespace zfile
{
    public interface ISearchResultFileSource : IMultiListFileSource
    {
    }

    public class SearchResultFileSource : MultiListFileSource, ISearchResultFileSource
    {
        public override string GetRootDir(string path)
        {
            return Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + "SearchResult" + Path.DirectorySeparatorChar;
        }

        public override FileSourceProperties GetProperties()
        {
            var properties = base.GetProperties();
            properties &= ~(FileSourceProperties.NoneParent | FileSourceProperties.ListFlatView);
            if (properties.HasFlag(FileSourceProperties.DirectAccess))
                properties |= FileSourceProperties.LinksToLocalFiles;
            return properties;
        }

        public override bool SetCurrentWorkingDirectory(string newDir)
        {
            return IsPathAtRoot(newDir);
        }

        public static new FileInfo CreateFile(string path)
        {
            return FileSystemFileSource.CreateFile(path);
        }

        public override IFileSourceOperation CreateListOperation(string targetPath)
        {
            return new SearchResultListOperation(this, targetPath);
        }

        public override bool GetLocalName(ref FileInfo file)
        {
            if (FileSource.Properties.HasFlag(FileSourceProperties.LinksToLocalFiles))
                return FileSource.GetLocalName(ref file);
            return true;
        }
    }
} 