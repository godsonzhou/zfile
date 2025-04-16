namespace zfile
{
    public interface ITempFileSystemFileSource : IFileSystemFileSource
    {
        bool DeleteOnDestroy { get; set; }
        string FileSystemRoot { get; }
    }

    public class TempFileSystemFileSource : FileSystemFileSource, ITempFileSystemFileSource
    {
        private bool _deleteOnDestroy;
        private string _tempRootDir;

        public bool DeleteOnDestroy
        {
            get => _deleteOnDestroy;
            set => _deleteOnDestroy = value;
        }

        public string FileSystemRoot => _tempRootDir;

        public TempFileSystemFileSource() : this(string.Empty)
        {
        }

        public TempFileSystemFileSource(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                _tempRootDir = path;
            }
            else
            {
                _tempRootDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (string.IsNullOrEmpty(_tempRootDir) || !Directory.CreateDirectory(_tempRootDir).Exists)
                {
                    _deleteOnDestroy = false;
                    throw new CannotCreateTempFileSourceException("Cannot create temp file source");
                }
            }

            CurrentAddress = _tempRootDir;
            _deleteOnDestroy = true;

            _tempRootDir = Path.Combine(_tempRootDir, string.Empty);
        }

        ~TempFileSystemFileSource()
        {
            if (_deleteOnDestroy && Directory.Exists(_tempRootDir))
            {
                Directory.Delete(CurrentAddress, true);
            }
        }

        public static ITempFileSystemFileSource GetFileSource()
        {
            return new TempFileSystemFileSource();
        }

        public override bool GetFreeSpace(string path, out long freeSize, out long totalSize)
        {
            return GetDiskFreeSpace(_tempRootDir, out freeSize, out totalSize);
        }

		private bool GetDiskFreeSpace(string tempRootDir, out long freeSize, out long totalSize)
		{
			throw new NotImplementedException();
		}

		public override bool IsPathAtRoot(string path)
        {
            return Path.Combine(path, string.Empty) == _tempRootDir;
        }

        public override string GetParentDir(string path)
        {
            return IsPathAtRoot(path) ? string.Empty : Path.GetDirectoryName(path);
        }

        public override string GetRootDir(string path)
        {
            return _tempRootDir;
        }

        public override string GetRootDir()
        {
            return _tempRootDir;
        }
    }

    public class TempFileSourceException : Exception
    {
        public TempFileSourceException(string message) : base(message)
        {
        }
    }

    public class CannotCreateTempFileSourceException : TempFileSourceException
    {
        public CannotCreateTempFileSourceException(string message) : base(message)
        {
        }
    }
} 