namespace zfile
{
    public class FileTree : IDisposable
    {
        private readonly List<FileTree> _subNodes;
        private readonly FileEntries _files = new();

        public string Path { get; }
        //public IReadOnlyList<FileTree> SubNodes => _subNodes;
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
 //   }


	///// <summary>
	///// Represents a node in a file tree.
	///// </summary>
	//public class FileTree : IDisposable
	//{
		//private readonly List<FileTree> _subNodes;
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
		//private List<FileTreeNode> _subNodes;
		//private object _data;
		public List<FileTree> SubNodes => _subNodes;
		//public int SubNodesCount
		//{
		//	get { return _subNodes.Count; }
		//	set
		//	{
		//		if (value < _subNodes.Count)
		//		{
		//			_subNodes.RemoveRange(value, _subNodes.Count - value);
		//		}
		//		else if (value > _subNodes.Count)
		//		{
		//			for (int i = _subNodes.Count; i < value; i++)
		//			{
		//				_subNodes.Add(null);
		//			}
		//		}
		//	}
		//}
		///// <summary>
		///// Gets the subnodes of this node.
		///// </summary>
		//public IReadOnlyList<FileTreeNode> SubNodes => _subNodes;

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

		/// <summary>
		/// Creates a new instance of the FileTreeNode class with the specified file.
		/// </summary>
		/// <param name="file">The file associated with this node.</param>
		//public FileTreeNode(FileEntry file) : this()
		//{
		//	TheFile = file;
		//}
		public FileTree this[int index] => _subNodes[index];

		public FileTree(FileEntry file) : this()
		{
			_file = file;
		}

		//public FileTreeNode(FileEntry file, Type dataType) : this(file)
		//{
		//	if (dataType != null)
		//	{
		//		_data = Activator.CreateInstance(dataType);
		//	}
		//}

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
		//public void Dispose()
		//{
		//	Dispose(true);
		//	GC.SuppressFinalize(this);
		//}

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

		//public bool IsDirectory(FileSystemOperationTargetExistsResult result)
		//{
		//	return result == FileSystemOperationTargetExistsResult.IsDirectory;
		//}

		/// <summary>
		/// Checks if the result represents a file
		/// </summary>
		//public bool IsFile(this FileSystemOperationTargetExistsResult result)
		//{
		//	return result == FileSystemOperationTargetExistsResult.IsFile;
		//}

		///// <summary>
		///// Checks if the result represents a symbolic link
		///// </summary>
		////public bool IsLink(this FileSystemOperationTargetExistsResult result)
		////{
		////	return result == FileSystemOperationTargetExistsResult.IsLink;
		////}

		///// <summary>
		///// Gets the Delete option for directory exists
		///// </summary>
		//public bool Delete(this FileSourceOperationOptionDirectoryExists option)
		//{
		//	return option == FileSourceOperationOptionDirectoryExists.Delete;
		//}

		///// <summary>
		///// Gets the Append option for file exists
		///// </summary>
		//public bool Append(this FileSourceOperationOptionFileExists option)
		//{
		//	return option == FileSourceOperationOptionFileExists.Append;
		//}

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