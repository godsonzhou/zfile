namespace zfile
{

    /// <summary>
    /// Delegate for asking questions during operations.
    /// </summary>
    /// <param name="caption">The caption of the question.</param>
    /// <param name="msg">The message of the question.</param>
    /// <param name="possibleResponses">The possible responses.</param>
    /// <param name="defaultResponse">The default response.</param>
    /// <param name="skipResponse">The skip response.</param>
    /// <returns>The selected response.</returns>
    public delegate FileSourceOperationUIResponse AskQuestionFunction(string caption, string msg, FileSourceOperationUIResponse[] possibleResponses, FileSourceOperationUIResponse defaultResponse, FileSourceOperationUIResponse skipResponse);

    /// <summary>
    /// Delegate for checking the operation state.
    /// </summary>
    public delegate void CheckOperationStateFunction();
	public delegate void AbortOperationFunction();

	/// <summary>
	/// Base class for building file trees.
	/// </summary>
	public abstract class FileSourceTreeBuilder : IDisposable
    {
        private FileTree _filesTree;
        protected long _filesCount;
        private int _currentDepth;
        private long _directoriesCount;
        protected long _filesSize;
        private bool _excludeRootDir;
        private SearchTemplate _fileTemplate;
        private bool _excludeEmptyTemplateDirectories;
        private FileSourceOperationSymLinkOption _symlinkOption;
        private bool _recursive;
        private string _rootDir;

        protected AskQuestionFunction _askQuestion;
        protected CheckOperationStateFunction _checkOperationState;

        /// <summary>
        /// Creates a new instance of the FileSourceTreeBuilder class.
        /// </summary>
        /// <param name="askQuestionFunction">The function to ask questions.</param>
        /// <param name="checkOperationStateFunction">The function to check the operation state.</param>
        protected FileSourceTreeBuilder(AskQuestionFunction askQuestionFunction, CheckOperationStateFunction checkOperationStateFunction)
        {
            _askQuestion = askQuestionFunction;
            _checkOperationState = checkOperationStateFunction;
            _recursive = true;
            _symlinkOption = FileSourceOperationSymLinkOption.None;
        }

        /// <summary>
        /// Builds a file tree from a node.
        /// </summary>
        /// <param name="node">The node to build from.</param>
        public void BuildFromNode(FileTree node)
        {
            _filesSize = 0;
            _filesCount = 0;
            _currentDepth = 0;
            _directoriesCount = 0;

            _filesTree = node;
            _rootDir = node.TheFile.Path;
            ((FileTreeNodeData)_filesTree.Data).Recursive = _recursive;

            AddFilesInDirectory(node.TheFile.FullPath + Path.DirectorySeparatorChar, _filesTree);

            _filesTree = null;
        }

        /// <summary>
        /// Builds a file tree from files.
        /// </summary>
        /// <param name="files">The files to build from.</param>
        public void BuildFromFiles(FileEntries files)
        {
            if (_filesTree != null)
            {
                _filesTree.Dispose();
                _filesTree = null;
            }

            _filesTree = new FileTree();
            _filesTree.Data = new FileTreeNodeData(_recursive);
            _filesSize = 0;
            _filesCount = 0;
            _directoriesCount = 0;
            _currentDepth = 0;
            _rootDir = files.Path;

            if (_excludeRootDir)
            {
                foreach (var file in files)
                {
                    if (file.IsDirectory)
                        AddFilesInDirectory(file.FullPath + Path.DirectorySeparatorChar, _filesTree);
                }
            }
            else
            {
                foreach (var file in files)
                    AddItem(file.Clone(), _filesTree);
            }
        }

        /// <summary>
        /// Adds a file to the tree.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="currentNode">The current node.</param>
        protected void AddFile(FileEntry file, FileTree currentNode)
        {
            int addedIndex = currentNode.AddSubNode(file);
            FileTree addedNode = currentNode.SubNodes[addedIndex];
            addedNode.Data = new FileTreeNodeData(_recursive);

            _filesCount++;
            _filesSize += file.Size;
            _checkOperationState();
        }

        /// <summary>
        /// Adds a link to the tree.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="currentNode">The current node.</param>
        protected virtual void AddLink(FileEntry file, FileTree currentNode)
        {
            int addedIndex = currentNode.AddSubNode(file);
            FileTree addedNode = currentNode.SubNodes[addedIndex];
            addedNode.Data = new FileTreeNodeData(_recursive);

            ((FileTreeNodeData)currentNode.Data).SubnodesHaveLinks = true;

            _filesCount++;
        }

        /// <summary>
        /// Adds a directory to the tree.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="currentNode">The current node.</param>
        protected void AddDirectory(FileEntry file, FileTree currentNode)
        {
            int addedIndex = currentNode.AddSubNode(file);
            FileTree addedNode = currentNode.SubNodes[addedIndex];
            FileTreeNodeData nodeData = new FileTreeNodeData(_recursive);
            addedNode.Data = nodeData;

            _directoriesCount++;

            if (_recursive)
            {
                if (_fileTemplate == null ||
                    _fileTemplate.SearchDepth < 0 ||
                    _currentDepth <= _fileTemplate.SearchDepth)
                {
                    _currentDepth++;
                    AddFilesInDirectory(file.FullPath + Path.DirectorySeparatorChar, addedNode);
                    _currentDepth--;
                }

                if (_fileTemplate != null && _excludeEmptyTemplateDirectories &&
                    addedNode.SubNodesCount == 0)
                {
                    currentNode.RemoveSubNode(addedIndex);
                    ((FileTreeNodeData)currentNode.Data).SubnodesHaveExclusions = true;
                }
                else
                {
                    // Propagate flags to parent.
                    if (nodeData.SubnodesHaveLinks)
                        ((FileTreeNodeData)currentNode.Data).SubnodesHaveLinks = true;
                    if (nodeData.SubnodesHaveExclusions)
                        ((FileTreeNodeData)currentNode.Data).SubnodesHaveExclusions = true;
                }
            }
        }

        /// <summary>
        /// Decides what to do with a link.
        /// </summary>
        /// <param name="file">The file to process.</param>
        /// <param name="currentNode">The current node.</param>
        protected void DecideOnLink(FileEntry file, FileTree currentNode)
        {
            switch (_symlinkOption)
            {
                case FileSourceOperationSymLinkOption.Follow:
                    AddLinkTarget(file, currentNode);
                    break;
                case FileSourceOperationSymLinkOption.DontFollow:
                    AddLink(file, currentNode);
                    break;
                case FileSourceOperationSymLinkOption.None:
                    FileSourceOperationUIResponse response = _askQuestion("", string.Format("Follow symbolic link '{0}'?", file.Name),
                        new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.All, FileSourceOperationUIResponse.No, FileSourceOperationUIResponse.SkipAll },
                        FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No);

                    switch (response)
                    {
                        case FileSourceOperationUIResponse.Yes:
                            AddLinkTarget(file, currentNode);
                            break;
                        case FileSourceOperationUIResponse.All:
                            _symlinkOption = FileSourceOperationSymLinkOption.Follow;
                            AddLinkTarget(file, currentNode);
                            break;
                        case FileSourceOperationUIResponse.No:
                            AddLink(file, currentNode);
                            break;
                        case FileSourceOperationUIResponse.SkipAll:
                            _symlinkOption = FileSourceOperationSymLinkOption.DontFollow;
                            AddLink(file, currentNode);
                            break;
                        default:
                            throw new Exception("Invalid user response");
                    }
                    break;
                default:
                    throw new Exception("Invalid symlink option");
            }
        }

        /// <summary>
        /// Adds an item to the tree.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="currentNode">The current node.</param>
        protected void AddItem(FileEntry file, FileTree currentNode)
        {
            bool matches = true;

            if (_fileTemplate != null)
            {
                if (file.IsDirectory || file.IsLinkToDirectory)
                {
                    matches = _fileTemplate.CheckDirectoryName(file.Name) &&
                             _fileTemplate.CheckDirectoryNameEx(file.FullPath, _rootDir);
                }
                else
                {
                    matches = _fileTemplate.Check(file);
                }

                if (!matches)
                {
                    ((FileTreeNodeData)currentNode.Data).SubnodesHaveExclusions = true;
                    return;
                }
            }

            if (file.IsLink)
                DecideOnLink(file, currentNode);
            else if (file.IsDirectory)
                AddDirectory(file, currentNode);
            else
                AddFile(file, currentNode);
        }

        /// <summary>
        /// Releases the tree.
        /// </summary>
        /// <returns>The released tree.</returns>
        public FileTree ReleaseTree()
        {
            FileTree result = _filesTree;
            _filesTree = null;
            return result;
        }

        /// <summary>
        /// Gets the total number of items.
        /// </summary>
        /// <returns>The total number of items.</returns>
        public long ItemsCount => FilesCount + DirectoriesCount;

        /// <summary>
        /// Adds files in a directory to the tree.
        /// </summary>
        /// <param name="srcPath">The source path.</param>
        /// <param name="currentNode">The current node.</param>
        protected abstract void AddFilesInDirectory(string srcPath, FileTree currentNode);

        /// <summary>
        /// Adds a link target to the tree.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="currentNode">The current node.</param>
        protected abstract void AddLinkTarget(FileEntry file, FileTree currentNode);

        /// <summary>
        /// Gets or sets whether to exclude the root directory.
        /// </summary>
        public bool ExcludeRootDir
        {
            get => _excludeRootDir;
            set => _excludeRootDir = value;
        }

        /// <summary>
        /// Gets or sets whether to process subdirectories recursively.
        /// </summary>
        public bool Recursive
        {
            get => _recursive;
            set => _recursive = value;
        }

        /// <summary>
        /// Gets or sets the symlink option.
        /// </summary>
        public FileSourceOperationSymLinkOption SymLinkOption
        {
            get => _symlinkOption;
            set => _symlinkOption = value;
        }

        /// <summary>
        /// Gets the files tree.
        /// </summary>
        public FileTree FilesTree => _filesTree;

        /// <summary>
        /// Gets the total size of files.
        /// </summary>
        public long FilesSize => _filesSize;

        /// <summary>
        /// Gets the number of files.
        /// </summary>
        public long FilesCount => _filesCount;

        /// <summary>
        /// Gets the number of directories.
        /// </summary>
        public long DirectoriesCount => _directoriesCount;

        /// <summary>
        /// Gets or sets whether to exclude empty template directories.
        /// </summary>
        public bool ExcludeEmptyTemplateDirectories
        {
            get => _excludeEmptyTemplateDirectories;
            set => _excludeEmptyTemplateDirectories = value;
        }

        /// <summary>
        /// Gets or sets the search template.
        /// Does not take ownership of SearchTemplate and does not free it.
        /// </summary>
        public SearchTemplate SearchTemplate
        {
            get => _fileTemplate;
            set => _fileTemplate = value;
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            _filesTree?.Dispose();
        }
    }

}