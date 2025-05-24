using ICSharpCode.TextEditor.Actions;
using System.Diagnostics;
using System.Threading;
namespace zfile
{
    public struct StateChangedEventEntry
    {
        public EventHandler<FileSourceOperationState> FunctionToCall;
        public List<FileSourceOperationState> States;
    }

    public struct DuplicateOption
    {

    }
    public class FileSourceOperationAbortingException : Exception;

    public interface IFileSourceOperation
    {   /// <summary>
        /// Gets the operation type
        /// </summary>
        FileSourceOperationTypes OperationType { get; }

        /// <summary>
        /// Gets the file source associated with this operation
        /// </summary>
        IFileSource FileSource { get; }

        /// <summary>
        /// Gets the target path for this operation
        /// </summary>
        string TargetPath { get; }

        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        string Description { get; }

        int OperationHandle { get; }
        FileSourceOperationState State { get; }
        bool IsFree { get; }
        bool IsModal { get; }
        string ResultString { get; }

        void Start();
        void Pause();
        void Stop();
        void Resume();
        string OperationName { get; }
        bool IsAborted { get; }
        void Abort();
        void ConnectionAvailableNotify();

        /// <summary>
        /// Event raised when the operation state changes
        /// </summary>
        event EventHandler<FileSourceOperationState> StateChanged;
        //event EventHandler<EventArgs> StateChanged;
        event EventHandler<EventArgs> ProgressChanged;
    }
    /// <summary>
    /// Base class for all file source operations
    /// </summary>
    public abstract class FileSourceOperation : IDisposable
    {
        protected IFileSource _fileSource;
        private double _progress;
        private DateTime _startTime;
        private FileSourceOperationState _state;
        private FileSourceOperationState _desiredState;
        protected FileSourceOperationResult _operationResult;
        private bool _operationInitialized;
        private object? _connection;
        private bool _needsConnection;
        private bool _wantsNewConnection;
        private int _connectionTimeout = -1; // Infinite timeout
        //private FileSourceOperation _parentOperation;
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
        private FileSourceOperationUIResponse _uiDefaultCancelResponse;
        private IFileSourceOperationUIActionHandler _uiActionHandler;
        private FileSourceOperationUIResponse _uiResponse;
        //private bool _tryAskQuestionResult;
        protected TOperationThread _thread;
		internal FileSourceOperationOptionSetPropertyError SetPropertyErrorOption;
		internal bool CopyTime;
		internal bool CopyOwnership;
		internal bool CopyPermissions;
		internal bool DropReadOnlyFlag;
		internal bool FollowLinks;
		internal bool CorrectLinks;
		internal bool ExcludeEmptyDirectories;
		public TOperationThread _Thread => _thread;
		public void AssignThread(TOperationThread thread)
		{
			_thread = thread;
		}
        public virtual bool NeedsConnection { get => _needsConnection; set => _needsConnection = value; }
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
        public virtual string TargetPath { get; set; }

        /// <summary>
        /// Gets the operation state
        /// </summary>
        public FileSourceOperationState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, _state);
                }
            }
        }
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
        public virtual string Description => GetDescription(FileSourceOperationDescriptionDetails.Basic);

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
			_needsConnection = _fileSource != null && _fileSource.Properties.HasFlag(FileSourceProperties.UsersConnections);
        }

        /// <summary>
        /// Starts the operation
        /// </summary>
        public virtual void Start()
        {
			//}
			FileSourceOperationState localstate;
			lock (_stateLock)
			{
				if (_state == FileSourceOperationState.Pausing)
					_state = FileSourceOperationState.Running;
				else if (_state == FileSourceOperationState.NotStarted || _state == FileSourceOperationState.Paused)
					_state = FileSourceOperationState.Starting;
				else
					return;
				localstate = _state;
			}
			NotifyStateChanged(localstate);
			_desiredState = FileSourceOperationState.Running;
			DoUnPause();
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
				UpdateProgress(1);
      
            }
            finally
            {
				// Set final state
				UpdateState(FileSourceOperationState.Stopped);
				
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
		public virtual void DoUnPause()
		{
			_pauseEvent.Set();
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
			//lock (_stateLock)
			//{
			//	if(_state != FileSourceOperationState.Stopping && _state != FileSourceOperationState.Stopped)
			//		_state = FileSourceOperationState.Stopping;
			//	else
			//		return;
			//}
			//NotifyStateChanged(FileSourceOperationState.Stopping);
			//_desiredState = FileSourceOperationState.Stopped;
			//DoUnPause();
			//// Also set "Connection available" event in case the operation is waiting
			//// for a connection and the user wants to abort it
			//// (this must be after setting desired state).
			//ConnectionAvailableNotify();

			//// The operation may be waiting for the user's response.
			//// Wake it up then, because it is being aborted
			//// (this must be after setting state to Stopping).
			////RTLeventSetEvent(FUserInterfaceAssignedEvent);
			//_userInterfaceAssignedEvent.Set();
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
				//_fileSource.Reload(TargetPath);
				// Nothing by default.
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
        protected virtual void Finalize()
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
        protected virtual object? GetConnection()
        {
            if (_fileSource != null)
            {
				_fileSource.GetConnection(this);
            }
            return null;
        }

		protected int DoWaitForConnection()
		{
			_connectionAvailableEvent.Reset();
			return _connectionAvailableEvent.WaitOne(_connectionTimeout) ? 0 : WaitHandle.WaitTimeout;
		}
		/// <summary>
		/// Waits for a connection to become available
		/// </summary>
		/// <returns>The wait result</returns>
		protected int WaitForConnection()
        {
			// Convert boolean result to WaitHandle result
			UpdateState(FileSourceOperationState.WaitingForConnection);
			var result = DoWaitForConnection();
			UpdateStartTime(DateTime.Now);
			UpdateState(FileSourceOperationState.Running);
			return result;
        }

        /// <summary>
        /// Updates the start time of the operation
        /// </summary>
        /// <param name="newStartTime">The new start time</param>
        protected void UpdateStartTime(DateTime newStartTime)
        {
            _startTime = newStartTime;
			UpdateStatisticsAtStartTime();
		}
        protected virtual FileSourceOperationTypes GetID()
        {
            return OperationType;
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
				if (_state == newState) 
					return true;
                else if (expectedStates != null && !expectedStates.Contains(_state))
					return false;
                _state = newState;
            }
			NotifyStateChanged(newState);
			return true;
        }

        /// <summary>
        /// Must be called from the operation thread
        /// </summary>
        /// <param name="desiredStates">If desired state is one of these states the pause is executed, otherwise nothing happens</param>
        protected void DoPauseIfNeeded(FileSourceOperationState[] desiredStates)
        {
			lock (_stateLock)
			{
				if (!desiredStates.Contains(_desiredState))
					return;

				_pauseEvent.Reset();
			}

			//UpdateState(_desiredState);
			//if curent threadid <> mainthreadid, then wait indefinitely
			//else wait 100ms
			if (Thread.CurrentThread.ManagedThreadId != _thread.Thread.ManagedThreadId)//TODO: NEED CONFIRM
				_pauseEvent.WaitOne();
			else
				while(_pauseEvent.WaitOne(100))
				{
					AppProcessMessages(); //widgetset.appprocessmessages() in pascal
				}
			
        }

        /// <summary>
        /// This function does some checks on the current and desired state of the operation
        /// </summary>
        protected void CheckOperationState()
        {
			//if (_desiredState == FileSourceOperationState.Paused)
			//{
			//    DoPauseIfNeeded(new[] { FileSourceOperationState.Paused });
			//}
			//else if (_desiredState == FileSourceOperationState.Stopped)
			//{
			//    throw new FileSourceOperationAbortingException();
			//}
			var desiredState = GetDesiredState();
			switch (desiredState)
			{
				case FileSourceOperationState.Paused:
					if( UpdateState(FileSourceOperationState.Paused, new[] { FileSourceOperationState.Pausing }))
					{
						DoPauseIfNeeded(new[] { FileSourceOperationState.Paused });
						// check if the operation was unpaused because it is being aborted.
						if (GetDesiredState() == FileSourceOperationState.Stopped)
							RaiseAbortOperation();
						UpdateStartTime(DateTime.Now);
						if (_operationInitialized)
							UpdateState(FileSourceOperationState.Running);
						else
							UpdateState(FileSourceOperationState.Starting);
					}
					break;

				case FileSourceOperationState.Stopped:
					RaiseAbortOperation();
					break;
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
                    States = [..states]
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
        public FileSourceOperationUIResponse AskQuestion(
            string message,
            string question,
            FileSourceOperationUIResponse[] possibleResponses,
            FileSourceOperationUIResponse defaultOKResponse,
            FileSourceOperationUIResponse defaultCancelResponse,
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
            if (_uiResponse == FileSourceOperationUIResponse.Abort)
            {
                throw new FileSourceOperationAbortingException();
            }

            // Return to running state
            UpdateState(FileSourceOperationState.Running);

            return _uiResponse;
        }

        internal void RaiseAbortOperation()
        {
            throw new FileSourceOperationAbortingException();
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
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pauseEvent.Dispose();
                _connectionAvailableEvent.Dispose();
                _userInterfaceAssignedEvent.Dispose();
            }
        }

        /// <summary>
        /// Converts a nullable DateTime to a nullable DateTime representing a file time
        /// </summary>
        /// <param name="value">The DateTime value to convert</param>
        /// <returns>The converted DateTime value or null if input is null</returns>
        internal static DateTime? DateTimeToFileTimeEx(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            // Check if the value is beyond the maximum allowed DateTime
            if (value > DateTime.MaxValue)
                return DateTime.MaxValue;

            return value;
        }

        /// <summary>
        /// Converts a file time (long) to a DateTime
        /// </summary>
        /// <param name="lastWriteTime">The file time to convert</param>
        /// <returns>The converted DateTime</returns>
        internal static DateTime FileTimeToDateTime(long lastWriteTime)
        {
            try
            {
                // If the file time is 0, return a default value
                if (lastWriteTime == 0)
                    return DateTime.MinValue;

                // Convert Windows file time to DateTime
                return DateTime.FromFileTime(lastWriteTime);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Formats an archiver command with the given parameters
        /// </summary>
        /// <param name="archiver">The archiver executable</param>
        /// <param name="commandLine">The command line template</param>
        /// <param name="archiveFileName">The archive file name</param>
        /// <param name="value">Additional value (can be a file list)</param>
        /// <param name="fullPath">The full path</param>
        /// <param name="destPath">The destination path</param>
        /// <param name="tempFile">Temporary file path</param>
        /// <param name="password">Password for the archive</param>
        /// <param name="empty1">Reserved parameter</param>
        /// <param name="empty2">Reserved parameter</param>
        /// <returns>The formatted command</returns>
        internal static string FormatArchiverCommand(string archiver, string commandLine, string archiveFileName, object value, string fullPath, string destPath, string tempFile, string password, string _, string __)
        {
            string result = commandLine;

            // Replace placeholders with actual values
            if (!string.IsNullOrEmpty(archiver))
                result = result.Replace("%A", archiver);

            if (!string.IsNullOrEmpty(archiveFileName))
                result = result.Replace("%P", archiveFileName);

            if (!string.IsNullOrEmpty(fullPath))
                result = result.Replace("%F", fullPath);

            if (!string.IsNullOrEmpty(destPath))
                result = result.Replace("%D", destPath);

            if (!string.IsNullOrEmpty(tempFile))
                result = result.Replace("%T", tempFile);

            if (!string.IsNullOrEmpty(password))
                result = result.Replace("%W", password);

            // Handle file list if value is a FileEntries object
            if (value is FileEntries files)
            {
                string fileList = string.Empty;
                foreach (var file in files)
                {
                    fileList += $"\"{file.FullPath}\" ";
                }
                result = result.Replace("%L", fileList.TrimEnd());
            }

            return result;
        }

        /// <summary>
        /// Checks if a file name matches a mask list
        /// </summary>
        /// <param name="fileName">The file name to check</param>
        /// <param name="maskList">The mask list (semicolon-separated)</param>
        /// <returns>True if the file name matches any mask in the list</returns>
        internal static bool MatchesMaskList(string fileName, string maskList)
        {
            if (string.IsNullOrEmpty(maskList))
                return false;

            // Split the mask list by semicolons
            string[] masks = maskList.Split(';');

            // Check each mask
            foreach (string mask in masks)
            {
                if (string.IsNullOrEmpty(mask))
                    continue;

                // Use wildcard matching from our extension method
                if (System.IO.Path.GetFileName(fileName).MatchesWildcard(mask))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the root path of file entries
        /// </summary>
        /// <param name="empty">Reserved parameter</param>
        /// <param name="fullFilesTreeToDelete">The file entries to modify</param>
        internal static void ChangeFileEntriesRoot(string _, FileEntries fullFilesTreeToDelete)
        {
            if (fullFilesTreeToDelete == null || fullFilesTreeToDelete.IsEmpty)
                return;

            // In the original Pascal code, this changes the root path of all entries
            // We'll implement a basic version that removes the current path
            string currentPath = fullFilesTreeToDelete.PathName;

            foreach (var entry in fullFilesTreeToDelete)
            {
                if (entry.FullPath.StartsWith(currentPath))
                {
                    // Remove the current path prefix
                    entry.Path = entry.Path[currentPath.Length..].TrimStart('\\', '/');
                }
            }
        }

        /// <summary>
        /// Extracts the error level from a command line
        /// </summary>
        /// <param name="commandLine">The command line to parse</param>
        /// <returns>The extracted error level or 0 if not found</returns>
        internal static int ExtractErrorLevel(string commandLine)
        {
            int result = 0;

            // Look for %E followed by digits in the command line
            int index = commandLine.IndexOf("%E");
            if (index >= 0)
            {
                int startIndex = index + 2;
                int endIndex = startIndex;

                // Find the end of the digits
                while (endIndex < commandLine.Length && char.IsDigit(commandLine[endIndex]))
                {
                    endIndex++;
                }

                // Extract and parse the error level
                if (endIndex > startIndex)
                {
                    string errorLevelStr = commandLine[startIndex..endIndex];
                    if (!int.TryParse(errorLevelStr, out result))
                    {
                        result = 0; // Default to 0 if parsing fails
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a file name matches any file in a file entries collection
        /// </summary>
        /// <param name="files">The file entries to check against</param>
        /// <param name="fileName">The file name to check</param>
        /// <returns>True if the file name matches any file in the collection</returns>
        internal static bool MatchesFileEntries(FileEntries files, string fileName)
        {
            if (files == null || files.IsEmpty)
                return false;

            // Check if the file name matches any file in the collection
            foreach (var file in files)
            {
                if (string.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase))
                    return true;
				if (file.IsDirectory)
				{
					if (FileSystemUtil.IsInPath(file.FullPath, fileName, true, true))
						return true;
				}
				else
				{
					if (string.Equals(file.FullPath, fileName, StringComparison.OrdinalIgnoreCase))
						return true;
				}
            }

            return false;
        }

        protected bool CheckOperationStateSafe()
        {
            try
            {
                CheckOperationState();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Process application messages
        /// </summary>
        protected bool AppProcessMessages(bool checkstate = false)
        {
			//in pascal version like :
			/*  
			 *  if GetCurrentThreadId = MainThreadID then
			begin
				WidgetSet.AppProcessMessages;
			end;*/

			// 关键：如果在线程中应该让出CPU，允许UI线程处理消息
			// 如果在主线程中，直接调用application.doevents()
			if(_thread.Thread.ManagedThreadId != MainForm.MainThreadId)
			{
				// 在非主线程中，使用sleep(0)来让出CPU
				// 允许UI线程处理消息
				System.Threading.Thread.Sleep(0);
				//Debug.Print("AppProcessMessages: Sleep(0)");
			}
			else
			{
				// 在主线程中，直接调用application.doevents()
				System.Windows.Forms.Application.DoEvents();
			}

			try
			{
				if (checkstate)
					CheckOperationState();
				return true;
			}
			catch (FileSourceOperationAbortingException e) 
			{
				return false;
			}
        }

        protected virtual void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            switch (logMsgType)
            {
                case LogOption.Error:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Error)) return;
                    break;
                case LogOption.Info:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Info)) return;
                    break;
                case LogOption.Success:
                    if (!GlobalSettings.LogOptions.HasFlag(LogOption.Success)) return;
                    break;
            }

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(_thread, message, logMsgType);
            }
        }

        protected virtual void ShowError(string message, int error, LogOption logOptions = LogOption.None)
        {
            LogMessage(message, logOptions, LogOption.Error);

            if (!GlobalSettings.SkipFileOpError && error > WcxModule.E_SUCCESS)
            {
                if (AskQuestion(message, "",
                               [FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort],
                               FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort) == FileSourceOperationUIResponse.Abort)
                {
                    RaiseAbortOperation();
                }
            }
        }
    }

    public interface IFileSourceOperationUIActionHandler
    {
        void HandleAction(FileSourceOperationUIResponse action);
    }

    public interface IFileSourceOperationUI
    {
        FileSourceOperationUIResponse AskQuestion(string uiMessage, string uiQuestion, FileSourceOperationUIResponse[] uiPossibleResponses, FileSourceOperationUIResponse uiDefaultOKResponse, FileSourceOperationUIResponse uiDefaultCancelResponse, IFileSourceOperationUIActionHandler uiActionHandler);
    }

	/// <summary>
	/// 提供文件源操作的扩展方法
	/// </summary>
	public static class FileSourceOperationExtensions
	{
		/// <summary>
		/// 创建一个包装委托，用于处理带有 actionHandler 参数的 AskQuestion 方法
		/// </summary>
		/// <param name="operation">文件源操作</param>
		/// <returns>包装后的委托</returns>
		public static AskQuestionFunction CreateAskQuestionDelegate(this FileSourceOperation operation)
		{
			return (msg, question, possibleResponses, defaultOKResponse, defaultCancelResponse) =>
				operation.AskQuestion(msg, question, possibleResponses, defaultOKResponse, defaultCancelResponse, null);
		}
	}
}