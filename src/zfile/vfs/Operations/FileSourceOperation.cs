using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
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
        private FileSourceOperationState _desiredState;
        private FileSourceOperationResult _operationResult;
        private bool _operationInitialized;
        private object _connection;
        private bool _needsConnection;
        private bool _wantsNewConnection;
        private int _connectionTimeout = -1; // Infinite timeout
        private FileSourceOperation _parentOperation;
        private DuplicateOption _elevate;

        // Synchronization objects
        private readonly object _stateLock = new object();
        private readonly object _eventsLock = new object();
        private readonly ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        private readonly ManualResetEvent _connectionAvailableEvent = new ManualResetEvent(false);
        private readonly List<StateChangedEventEntry> _stateChangedEventListeners = new List<StateChangedEventEntry>();
        private readonly List<IFileSourceOperationUI> _userInterfaces = new List<IFileSourceOperationUI>();
        private readonly AutoResetEvent _userInterfaceAssignedEvent = new AutoResetEvent(false);

        // Parameters for UI question
        private string _uiMessage;
        private string _uiQuestion;
        private FileSourceOperationUIResponse[] _uiPossibleResponses;
        private FileSourceOperationUIResponse _uiDefaultOKResponse;
        private FileSourceOperationUIAnswer _uiDefaultCancelResponse;
        private IFileSourceOperationUIActionHandler _uiActionHandler;
        private FileSourceOperationUIAnswer _uiResponse;
        private bool _tryAskQuestionResult;

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
        public FileSourceOperationState State => GetState();

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
        /// Gets or sets whether the operation should be executed with elevated privileges
        /// </summary>
        public DuplicateOption Elevate
        {
            get => _elevate;
            set => _elevate = value;
        }

        /// <summary>
        /// Gets or sets whether the operation wants a new connection
        /// </summary>
        public bool WantsNewConnection
        {
            get => _wantsNewConnection;
            set => _wantsNewConnection = value;
        }

        /// <summary>
        /// Gets the result of the operation
        /// </summary>
        public FileSourceOperationResult Result => _operationResult;

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
            _desiredState = FileSourceOperationState.Running; // Set for auto-start unless prevented
            _operationResult = FileSourceOperationResult.Aborted;
            _progress = 0.0;
            
            // Check if file source uses connections
            _needsConnection = _fileSource != null && _fileSource.Properties.HasFlag(FileSourceProperties.UsesConnections);
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
        /// Executes the operation
        /// </summary>
        public void Execute()
        {
            try
            {
                UpdateProgress(0);
                _operationResult = FileSourceOperationResult.Aborted;

                try
                {
                    // Wait for start command if not started automatically
                    DoPauseIfNeeded(new[] { FileSourceOperationState.NotStarted, FileSourceOperationState.Paused });

                    // Check if wasn't aborted while paused
                    CheckOperationState();

                    if (_needsConnection)
                    {
                        // Wait for connection to file source
                        while (true)
                        {
                            _connection = GetConnection();

                            if (_connection != null)
                                break;

                            UpdateState(FileSourceOperationState.WaitingForConnection);

                            if (WaitForConnection() == WaitHandle.WaitTimeout)
                                break;

                            // Allow pausing and aborting the operation
                            CheckOperationState();
                        }
                    }

                    // Initialize
                    UpdateState(FileSourceOperationState.Starting);

                    Initialize();
                    _operationInitialized = true;

                    UpdateStartTime(DateTime.Now);
                    UpdateState(FileSourceOperationState.Running);

                    MainExecute();

                    _operationResult = FileSourceOperationResult.Finished;
                }
                catch (FileSourceOperationAbortingException)
                {
                    _operationResult = FileSourceOperationResult.Aborted;
                }

                if (_operationInitialized)
                {
                    Finalize();
                }

                // Set final state
                if (_operationResult == FileSourceOperationResult.Finished)
                {
                    UpdateState(FileSourceOperationState.Finished);
                }
                else
                {
                    UpdateState(FileSourceOperationState.Stopped);
                }
            }
            finally
            {
                // Make sure all events are set when we're done
                _pauseEvent.Set();
                _connectionAvailableEvent.Set();
            }
        }

        /// <summary>
        /// Pauses the operation
        /// </summary>
        public virtual void Pause()
        {
            if (_state == FileSourceOperationState.Running)
            {
                _desiredState = FileSourceOperationState.Paused;
                UpdateState(FileSourceOperationState.Pausing);
                _pauseEvent.Reset();
            }
        }

        /// <summary>
        /// Stops the operation
        /// </summary>
        public virtual void Stop()
        {
            if (_state == FileSourceOperationState.Running || _state == FileSourceOperationState.Paused)
            {
                _desiredState = FileSourceOperationState.Stopped;
                UpdateState(FileSourceOperationState.Stopping);
                _pauseEvent.Set(); // Wake up if paused
            }
        }
        
        /// <summary>
        /// Prevents auto start of the operation on Execute
        /// </summary>
        public void PreventStart()
        {
            if (_state == FileSourceOperationState.NotStarted)
            {
                _desiredState = FileSourceOperationState.NotStarted;
            }
        }
        
        /// <summary>
        /// If the operation can be paused it pauses otherwise starts the operation
        /// </summary>
        public void TogglePause()
        {
            if (_state == FileSourceOperationState.Running)
            {
                Pause();
            }
            else if (_state == FileSourceOperationState.Paused)
            {
                _desiredState = FileSourceOperationState.Running;
                _pauseEvent.Set();
            }
            else if (_state == FileSourceOperationState.NotStarted)
            {
                Start();
            }
        }
        
        /// <summary>
        /// Notifies the operation that possibly a connection is available from the file source
        /// </summary>
        public void ConnectionAvailableNotify()
        {
            _connectionAvailableEvent.Set();
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
            if (UpdateState(newState))
            {
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
            NotifyStateChanged(_state);
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
            if (_fileSource != null)
            {
                // In a real implementation, this would reload file sources
                // For example: _fileSource.Reload();
            }
        }
        
        /// <summary>
        /// Initializes the operation
        /// </summary>
        protected virtual void Initialize()
        {
            // Override in descendant classes
        }
        
        /// <summary>
        /// Executes the main operation
        /// </summary>
        protected abstract void MainExecute();
        
        /// <summary>
        /// Finalizes the operation
        /// </summary>
        protected virtual new void Finalize()
        {
            // Override in descendant classes
        }

        /// <summary>
        /// Notifies all listeners that operation has changed its state
        /// </summary>
        /// <param name="newState">The new state</param>
        protected void NotifyStateChanged(FileSourceOperationState newState)
        {
            // First notify through the event
            StateChanged?.Invoke(this, newState);
            
            // Then notify all registered listeners
            List<EventHandler<FileSourceOperationState>> functionsToCall = new List<EventHandler<FileSourceOperationState>>();
            
            lock (_eventsLock)
            {
                foreach (var entry in _stateChangedEventListeners)
                {
                    if (entry.States.Contains(newState))
                    {
                        functionsToCall.Add(entry.FunctionToCall);
                    }
                }
            }
            
            // Call all listeners outside the lock
            foreach (var func in functionsToCall)
            {
                func(this, newState);
            }
        }
        
        /// <summary>
        /// Gets a connection from the file source
        /// </summary>
        /// <returns>The connection object</returns>
        protected virtual object GetConnection()
        {
            if (_fileSource != null)
            {
                // In a real implementation, this would get a connection from the file source
                // For example: return _fileSource.GetConnection(this);
            }
            return null;
        }
        
        /// <summary>
        /// Waits for a connection to become available
        /// </summary>
        /// <returns>The wait result</returns>
        protected int WaitForConnection()
        {
            return _connectionAvailableEvent.WaitOne(_connectionTimeout);
        }
        
        /// <summary>
        /// Updates the start time of the operation
        /// </summary>
        /// <param name="newStartTime">The new start time</param>
        protected void UpdateStartTime(DateTime newStartTime)
        {
            _startTime = newStartTime;
        }
        
        /// <summary>
        /// Gets the current state of the operation
        /// </summary>
        /// <returns>The current state</returns>
        protected FileSourceOperationState GetState()
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
        
        /// <summary>
        /// Gets the desired state of the operation
        /// </summary>
        /// <returns>The desired state</returns>
        protected FileSourceOperationState GetDesiredState()
        {
            return _desiredState;
        }
        
        /// <summary>
        /// Updates the state of the operation
        /// </summary>
        /// <param name="newState">The new state</param>
        /// <param name="expectedStates">The expected states</param>
        /// <returns>True if the state was updated, false otherwise</returns>
        protected bool UpdateState(FileSourceOperationState newState, FileSourceOperationState[] expectedStates = null)
        {
            lock (_stateLock)
            {
                if (expectedStates == null || expectedStates.Contains(_state) || _state == newState)
                {
                    if (_state != newState)
                    {
                        _state = newState;
                        return true;
                    }
                    return true; // State already is newState
                }
                return false;
            }
        }
        
        /// <summary>
        /// Must be called from the operation thread
        /// </summary>
        /// <param name="desiredStates">If desired state is one of these states the pause is executed</param>
        protected void DoPauseIfNeeded(FileSourceOperationState[] desiredStates)
        {
            if (desiredStates.Contains(_desiredState))
            {
                _pauseEvent.Reset();
                UpdateState(_desiredState);
                _pauseEvent.WaitOne();
            }
        }
        
        /// <summary>
        /// This function does some checks on the current and desired state of the operation
        /// </summary>
        protected void CheckOperationState()
        {
            if (_desiredState == FileSourceOperationState.Paused)
            {
                DoPauseIfNeeded(new[] { FileSourceOperationState.Paused });
            }
            else if (_desiredState == FileSourceOperationState.Stopped)
            {
                throw new FileSourceOperationAbortingException();
            }
        }
        
        /// <summary>
        /// Adds a function to call when the operation's state changes
        /// </summary>
        /// <param name="states">The states to listen for</param>
        /// <param name="functionToCall">The function to call</param>
        public void AddStateChangedListener(FileSourceOperationState[] states, EventHandler<FileSourceOperationState> functionToCall)
        {
            lock (_eventsLock)
            {
                _stateChangedEventListeners.Add(new StateChangedEventEntry
                {
                    FunctionToCall = functionToCall,
                    States = new HashSet<FileSourceOperationState>(states)
                });
            }
        }

        /// <summary>
        /// Removes a registered function callback for state-changed event
        /// </summary>
        /// <param name="states">The states to remove listener for</param>
        /// <param name="functionToCall">The function to remove</param>
        public void RemoveStateChangedListener(FileSourceOperationState[] states, EventHandler<FileSourceOperationState> functionToCall)
        {
            lock (_eventsLock)
            {
                for (int i = _stateChangedEventListeners.Count - 1; i >= 0; i--)
                {
                    var entry = _stateChangedEventListeners[i];
                    if (entry.FunctionToCall == functionToCall && 
                        states.All(s => entry.States.Contains(s)) && 
                        entry.States.Count == states.Length)
                    {
                        _stateChangedEventListeners.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a user interface to the operation
        /// </summary>
        /// <param name="userInterface">The user interface to add</param>
        public void AddUserInterface(IFileSourceOperationUI userInterface)
        {
            lock (_eventsLock)
            {
                if (!_userInterfaces.Contains(userInterface))
                {
                    _userInterfaces.Add(userInterface);
                    _userInterfaceAssignedEvent.Set();
                }
            }
        }

        /// <summary>
        /// Removes a user interface from the operation
        /// </summary>
        /// <param name="userInterface">The user interface to remove</param>
        public void RemoveUserInterface(IFileSourceOperationUI userInterface)
        {
            lock (_eventsLock)
            {
                _userInterfaces.Remove(userInterface);
            }
        }
        
        /// <summary>
        /// General function to ask questions from operations
        /// </summary>
        protected FileSourceOperationUIAnswer AskQuestion(
            string message, 
            string question,
            FileSourceOperationUIResponse[] possibleResponses,
            FileSourceOperationUIResponse defaultOKResponse,
            FileSourceOperationUIAnswer defaultCancelResponse,
            IFileSourceOperationUIActionHandler actionHandler = null)
        {
            // Store parameters for UI question
            _uiMessage = message;
            _uiQuestion = question;
            _uiPossibleResponses = possibleResponses;
            _uiDefaultOKResponse = defaultOKResponse;
            _uiDefaultCancelResponse = defaultCancelResponse;
            _uiActionHandler = actionHandler;
            
            // Change state to waiting for feedback
            UpdateState(FileSourceOperationState.WaitingForFeedback);
            
            // Wait for UI to be assigned if needed
            while (true)
            {
                lock (_eventsLock)
                {
                    if (_userInterfaces.Count > 0)
                    {
                        break;
                    }
                }
                
                // Wait for UI to be assigned
                _userInterfaceAssignedEvent.WaitOne();
                
                // Check if operation was aborted while waiting
                CheckOperationState();
            }
            
            // Get the most recently assigned UI
            IFileSourceOperationUI ui;
            lock (_eventsLock)
            {
                ui = _userInterfaces[_userInterfaces.Count - 1];
            }
            
            // Ask the question
            _uiResponse = ui.AskQuestion(
                _uiMessage,
                _uiQuestion,
                _uiPossibleResponses,
                _uiDefaultOKResponse,
                _uiDefaultCancelResponse,
                _uiActionHandler);
            
            // Check if operation should be aborted
            if (_uiResponse.Abort)
            {
                throw new FileSourceOperationAbortingException();
            }
            
            // Return to running state
            UpdateState(FileSourceOperationState.Running);
            
            return _uiResponse;
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
            if (disposing)
            {
                _pauseEvent.Dispose();
                _connectionAvailableEvent.Dispose();
                _userInterfaceAssignedEvent.Dispose();
            }
        }
    }
}