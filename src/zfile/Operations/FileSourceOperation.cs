using Zfile.FileSources;

namespace Zfile.Operations
{
    /// <summary>
    /// Base class for all file source operations
    /// </summary>
    public abstract class FileSourceOperation : IDisposable
    {
        private IFileSource _fileSource;
        private double _progress;
        private DateTime _startTime;
        private FileSourceOperationState _state;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public abstract FileSourceOperationTypes OperationType { get; }

        /// <summary>
        /// Gets the file source
        /// </summary>
        public IFileSource FileSource => _fileSource;

        /// <summary>
        /// Gets the target path
        /// </summary>
        public virtual string TargetPath => string.Empty;

        /// <summary>
        /// Gets the operation state
        /// </summary>
        public FileSourceOperationState State => _state;

        /// <summary>
        /// Gets the start time of the operation
        /// </summary>
        public DateTime StartTime => _startTime;

        /// <summary>
        /// Gets the progress of the operation (0.0 to 1.0)
        /// </summary>
        public double Progress => _progress;

        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        public string Description => GetDescription(FileSourceOperationDescriptionDetails.Basic);

        /// <summary>
        /// Event raised when the operation state changes
        /// </summary>
        public event EventHandler<FileSourceOperationState> StateChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="FileSourceOperation"/> class
        /// </summary>
        /// <param name="aFileSource">File source for this operation</param>
        protected FileSourceOperation(IFileSource aFileSource)
        {
            _fileSource = aFileSource;
            _state = FileSourceOperationState.NotStarted;
            _progress = 0.0;
        }

        /// <summary>
        /// Starts the operation
        /// </summary>
        public virtual void Start()
        {
            if (_state == FileSourceOperationState.NotStarted)
            {
                _startTime = DateTime.Now;
                UpdateStatisticsAtStartTime();
                ChangeState(FileSourceOperationState.Running);
            }
        }

        /// <summary>
        /// Pauses the operation
        /// </summary>
        public virtual void Pause()
        {
            if (_state == FileSourceOperationState.Running)
            {
                ChangeState(FileSourceOperationState.Paused);
            }
        }

        /// <summary>
        /// Stops the operation
        /// </summary>
        public virtual void Stop()
        {
            if (_state == FileSourceOperationState.Running || _state == FileSourceOperationState.Paused)
            {
                ChangeState(FileSourceOperationState.Stopped);
            }
        }

        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        /// <param name="details">The level of detail to include in the description</param>
        /// <returns>The description of this operation</returns>
        public abstract string GetDescription(FileSourceOperationDescriptionDetails details);

        /// <summary>
        /// Updates the progress of the operation
        /// </summary>
        /// <param name="newProgress">The new progress value (0.0 to 1.0)</param>
        protected void UpdateProgress(double newProgress)
        {
            _progress = Math.Min(Math.Max(newProgress, 0.0), 1.0);
        }

        /// <summary>
        /// Changes the state of the operation
        /// </summary>
        /// <param name="newState">The new state</param>
        protected void ChangeState(FileSourceOperationState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                OnStateChanged();

                if (_state == FileSourceOperationState.Finished || 
                    _state == FileSourceOperationState.Stopped || 
                    _state == FileSourceOperationState.Failed)
                {
                    DoReloadFileSources();
                }
            }
        }

        /// <summary>
        /// Raises the state changed event
        /// </summary>
        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, _state);
        }

        /// <summary>
        /// Updates the statistics at the start time of the operation
        /// </summary>
        protected abstract void UpdateStatisticsAtStartTime();

        /// <summary>
        /// Reloads file sources after the operation is complete
        /// </summary>
        protected virtual void DoReloadFileSources()
        {
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}