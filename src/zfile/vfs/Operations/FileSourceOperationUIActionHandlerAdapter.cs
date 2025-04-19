namespace zfile
{
    /// <summary>
    /// Adapter class that implements IFileSourceOperationUIActionHandler interface
    /// and wraps a FileSourceOperationUIActionHandler delegate
    /// </summary>
    public class FileSourceOperationUIActionHandlerAdapter : IFileSourceOperationUIActionHandler
    {
        private readonly FileSourceOperationUIActionHandler _handler;

        /// <summary>
        /// Gets the wrapped handler
        /// </summary>
        public FileSourceOperationUIActionHandler Handler => _handler;

        /// <summary>
        /// Creates a new instance of the FileSourceOperationUIActionHandlerAdapter class
        /// </summary>
        /// <param name="handler">The delegate to wrap</param>
        public FileSourceOperationUIActionHandlerAdapter(FileSourceOperationUIActionHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Invokes the wrapped handler
        /// </summary>
        /// <param name="action">The action to handle</param>
        public void HandleAction(FileSourceOperationUIResponse action)
        {
            _handler?.Invoke(action);
        }
    }
}
