namespace zfile
{
	public class FileView
	{
		private ListView _listView;
		private List<IFileSource> _fileSources = new List<IFileSource>();
		private IFileSource _activeFileSource;
		private string _currentPath;
		private readonly object _syncRoot = new object();

		public FileView(ListView listView)
		{
			_listView = listView;
			_currentPath = string.Empty;
		}

		public IFileSource ActiveFileSource { get => _activeFileSource; set => _activeFileSource = value; }
		public string CurrentPath { get => _currentPath; set => _currentPath = value; }

		public void AddFileSource(IFileSource fileSource, string path)
		{
			if (fileSource == null) return;

			lock (_syncRoot)
			{
				// Add to file source manager
				FileSourceManager.Instance.Add(fileSource);

				// Add to local list
				if (!_fileSources.Contains(fileSource))
				{
					_fileSources.Add(fileSource);
				}

				// Set as active file source
				_activeFileSource = fileSource;
				_currentPath = path;

				// If this is a WFX file source, add to connection manager
				if (fileSource is IWfxPluginFileSource wfxFileSource)
				{
					ConnectionManager.AddNetworkConnection(
						wfxFileSource.PluginName,
						wfxFileSource.RootDirectory,
						wfxFileSource);
				}

				// Refresh view
				RefreshFileList();
			}
		}

		public void RemoveFileSource(IFileSource fileSource)
		{
			if (fileSource == null) return;

			lock (_syncRoot)
			{
				// Remove from local list
				_fileSources.Remove(fileSource);

				// If this is a WFX file source, remove from connection manager
				if (fileSource is IWfxPluginFileSource wfxFileSource)
				{
					ConnectionManager.RemoveNetworkConnection(
						wfxFileSource.PluginName,
						wfxFileSource.RootDirectory);
				}

				// If this was the active file source, switch to another one
				if (_activeFileSource == fileSource)
				{
					if (_fileSources.Count > 0)
					{
						_activeFileSource = _fileSources[0];
						_currentPath = "";
					}
					else
					{
						_activeFileSource = null;
						_currentPath = "";
					}

					// Refresh view
					RefreshFileList();
				}
			}
		}

		public void ChangeDirectory(string path)
		{
			if (_activeFileSource == null) return;

			_currentPath = path;
			RefreshFileList();
		}

		public void RefreshFileList()
		{
			if (_activeFileSource == null) return;

			_listView.Items.Clear();

			//var files = new FileEntries();
			var files = _activeFileSource.GetFiles(_currentPath);
			if (files.Count != 0)
			{
				foreach (var file in files)
				{
					var item = new ListViewItem(file.Name);
					item.SubItems.Add(file.IsDirectory ? "<DIR>" : file.Size.ToString());
					item.SubItems.Add(file.IsDirectory ? "Directory" : Path.GetExtension(file.Name));
					item.SubItems.Add(file.ModificationTime.ToString());
					item.Tag = file;
					item.ImageIndex = file.IsDirectory ? 0 : 1; // Assuming folder and file icons

					_listView.Items.Add(item);
				}
			}
		}

		public void ShowConnectionManager()
		{
			Forms.ConnectionManagerHelper.ShowConnectionManager(this);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				foreach (var fileSource in _fileSources)
				{
					if (fileSource is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
				_fileSources.Clear();
				_activeFileSource = null;
			}
		}

		internal void ChangePathToChild(FileEntry file)
		{
			throw new NotImplementedException();
		}
	}
}