namespace zfile
{
    public class FileTree : IDisposable
    {
        private readonly List<FileTree> _subNodes;
        private readonly FileEntries _files;

        public string Path { get; }
        public IReadOnlyList<FileTree> SubNodes => _subNodes;
        public FileEntries Files => _files;

        public FileTree(string path)
        {
            Path = path;
            _subNodes = new List<FileTree>();
            _files = new FileEntries();
        }

        public void AddSubNode(FileTree node)
        {
            _subNodes.Add(node);
        }

        public void AddFile(FileEntry file)
        {
            _files.Add(file);
        }

        public void Dispose()
        {
            foreach (var node in _subNodes)
            {
                node.Dispose();
            }
            _subNodes.Clear();
            _files.Clear();
        }
    }

   
} 