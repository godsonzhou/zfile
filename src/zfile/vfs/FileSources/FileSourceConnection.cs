namespace zfile
{
    /// <summary>
    /// Represents a connection to a file source
    /// </summary>
    public class FileSourceConnection
    {
        private IFileSourceOperation _assignedOperation;
        private readonly object _operationLock = new object();
        private string _currentPath;

        /// <summary>
        /// Gets the assigned operation
        /// </summary>
        public IFileSourceOperation AssignedOperation => _assignedOperation;

        /// <summary>
        /// Gets or sets the current path
        /// </summary>
        public virtual string CurrentPath
        {
            get => _currentPath;
            set => _currentPath = value;
        }
		protected virtual void SetCurrentPath(string path)
		{
			_currentPath = path;
		}
		/// <summary>
		/// Creates a new instance of the FileSourceConnection class
		/// </summary>
		public FileSourceConnection()
        {
            _currentPath = string.Empty;
        }

        /// <summary>
        /// Checks if the connection is available
        /// </summary>
        /// <returns>True if the connection is available, false otherwise</returns>
        public bool IsAvailable()
        {
            lock (_operationLock)
            {
                return _assignedOperation == null;
            }
        }

        /// <summary>
        /// Acquires the connection for the specified operation
        /// </summary>
        /// <param name="operation">The operation</param>
        /// <returns>True if the connection was acquired successfully, false otherwise</returns>
        public bool Acquire(IFileSourceOperation operation)
        {
            if (operation == null)
                return false;

            lock (_operationLock)
            {
                if (_assignedOperation != null)
                    return false;

                _assignedOperation = operation;
                return true;
            }
        }

        /// <summary>
        /// Releases the connection
        /// </summary>
        public void Release()
        {
            lock (_operationLock)
            {
                _assignedOperation = null;
            }
        }
    }
}