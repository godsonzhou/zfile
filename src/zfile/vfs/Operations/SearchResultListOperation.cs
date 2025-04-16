namespace zfile
{
    public class SearchResultListOperation : FileSourceListOperation
    {
        private readonly ISearchResultFileSource _fileSource;

        public SearchResultListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries();
            _fileSource = fileSource as ISearchResultFileSource;
            NeedsConnection = false;
        }

        protected override void MainExecute()
        {
            Files.Clear();

            // 目前"扁平模式"始终启用（添加树中的所有文件）
            if (FileSource.IsPathAtRoot(Path))
            {
                AddNode(_fileSource.FileEntries);
            }
        }

        private void AddNode(FileTreeNode node)
        {
            if (node != null)
            {
                foreach (var subNode in node.SubNodes)
                {
                    CheckOperationState();
                    Files.Add(subNode.TheFile.Clone());
                    AddNode(subNode);
                }
            }
        }
    }
} 