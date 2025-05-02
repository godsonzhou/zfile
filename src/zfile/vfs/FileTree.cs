namespace zfile
{
    public class FileTree : IDisposable
    {
        private readonly List<FileTree> _subNodes;
        private readonly FileEntries _files = new();

        public string Path { get; }
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
 
		private object _data;

		/// <summary>
		/// Gets the file associated with this node.
		/// </summary>
		//public FileEntry TheFile { get; }

		private FileEntry _file;
		public FileEntry TheFile => _file;

		public object Data
		{
			get { return _data; }
			set
			{
				if (_data != value)
				{
					_data = value;
				}
			}
		}

		public List<FileTree> SubNodes => _subNodes;

		/// <summary>
		/// Gets the number of subnodes.
		/// </summary>
		public int SubNodesCount => _subNodes.Count;

		/// <summary>
		/// Creates a new instance of the FileTreeNode class.
		/// </summary>
		public FileTree()
		{
			_subNodes = new List<FileTree>();
		}

		public FileTree this[int index] => _subNodes[index];

		public FileTree(FileEntry file) : this()
		{
			_file = file;
		}

		/// <summary>
		/// Adds a subnode with the specified file.
		/// </summary>
		/// <param name="file">The file to add.</param>
		/// <returns>The index of the added node.</returns>
		public int AddSubNode(FileEntry file)
		{
			var node = new FileTree(file);
			_subNodes.Add(node);
			return _subNodes.Count - 1;
		}

		/// <summary>
		/// Removes a subnode at the specified index.
		/// </summary>
		/// <param name="index">The index of the subnode to remove.</param>
		public void RemoveSubNode(int index)
		{
			if (index >= 0 && index < _subNodes.Count)
			{
				_subNodes.RemoveAt(index);
			}
		}

		private bool _disposed = false;

		/// <summary>
		/// Disposes resources used by the FileTreeNode.
		/// </summary>
		/// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources
					foreach (var node in _subNodes)
					{
						node.Dispose();
					}
					_subNodes.Clear();
					(_data as IDisposable)?.Dispose();

					// Dispose FileEntry if it implements IDisposable
					(_file as IDisposable)?.Dispose();
				}

				// Clean up unmanaged resources
				// No unmanaged resources to clean up in this class currently

				_disposed = true;
			}
		}

		/// <summary>
		/// Whether to process subdirectories recursively.
		/// </summary>
		public bool Recursive { get; set; }

		/// <summary>
		/// True if any of the subnodes (recursively) are links.
		/// </summary>
		public bool SubnodesHaveLinks { get; set; }

		/// <summary>
		/// Whether directory or subdirectories have any elements that will not be copied/moved.
		/// </summary>
		public bool SubnodesHaveExclusions { get; set; }

		/// <summary>
		/// Finalizer to ensure resources are cleaned up if Dispose is not called.
		/// </summary>
		~FileTree()
		{
			Dispose(false);
		}

		/// <summary>
		/// Gets the name of the file associated with this node
		/// </summary>
		public string Name => TheFile?.Name ?? string.Empty;
		
		/// <summary>
		/// Checks if the node represents a directory
		/// </summary>
		public bool IsDirectory => TheFile?.IsDirectory ?? false;

		/// <summary>
		/// Checks if the node represents a symbolic link
		/// </summary>
		public bool IsLink =>TheFile?.IsLink ?? false;

		/// <summary>
		/// Gets the files associated with this node
		/// </summary>
		//public FileEntries Files => new FileEntries();

		/// <summary>
		/// Gets the size of the file associated with this node
		/// </summary>
		public long Size => TheFile?.Size ?? 0;

		/// <summary>
		/// Gets the file entry associated with this node
		/// </summary>
		public FileEntry FileEntry => TheFile;
	}

	/// <summary>
	/// Creates a new instance of the FileTreeNodeData class.
	/// </summary>
	/// <param name="recursive">Whether to process subdirectories recursively.</param>
	public class FileTreeNodeData(bool recursive)
	{
		public bool Recursive = recursive;
		public bool SubnodesHaveLinks = false;
		public bool SubnodesHaveExclusions = false;
	}
} 