namespace zfile
{
    public class FileTree : IDisposable
    {
        private readonly List<FileTree> _subNodes;
        private readonly FileEntries _files;

        public string Path { get; }
        public IReadOnlyList<FileTree> SubNodes => _subNodes;
        public IReadOnlyFileEntries Files => _files;

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

    public class FileSystemTreeBuilder : IDisposable
    {
        private readonly Action<string, bool> _askQuestion;
        private readonly Action _checkOperationState;
        private FileTree _currentTree;
        private long _filesCount;
        private long _filesSize;

        public FileSourceOperationOptionGeneral SymLinkOption { get; set; }
        public SearchTemplate SearchTemplate { get; set; }
        public bool ExcludeEmptyTemplateDirectories { get; set; }

        public long FilesCount => _filesCount;
        public long FilesSize => _filesSize;
		public bool Recursive { get; set; }

		public FileSystemTreeBuilder(Action<string, bool> askQuestion, Action checkOperationState)
        {
            _askQuestion = askQuestion;
            _checkOperationState = checkOperationState;
        }

        public void BuildFromFiles(FileEntries files)
        {
            _currentTree = new FileTree(string.Empty);
            _filesCount = 0;
            _filesSize = 0;

            foreach (var file in files)
            {
                _checkOperationState();
                ProcessFile(file);
            }
        }

        private void ProcessFile(FileEntry file)
        {
            if (SearchTemplate != null && !SearchTemplate.Check(file))
                return;

            _filesCount++;
            _filesSize += file.Size;
            _currentTree.AddFile(file);
        }

        public FileTree ReleaseTree()
        {
            var tree = _currentTree;
            _currentTree = null;
            return tree;
        }

        public void Dispose()
        {
            _currentTree?.Dispose();
        }
    }
} 