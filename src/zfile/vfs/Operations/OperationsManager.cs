using System.Diagnostics;

namespace zfile
{

	public class TOperationThread : IDisposable
	{
		private readonly Thread _thread;
		private FileSourceOperation _operation;
		private bool _disposed;

		public event EventHandler<ThreadExceptionEventArgs> OnException;
		public event EventHandler OnTerminated;

		public bool FreeOnTerminate { get; set; } = true;

		public TOperationThread(bool createSuspended, FileSourceOperation operation)
		{
			_operation = operation ?? throw new ArgumentNullException(nameof(operation));
			_operation.AssignThread(this);

			_thread = new Thread(ExecuteWorker)
			{
				IsBackground = true  // 默认设置为后台线程
			};

			if (!createSuspended)
			{
				Start();
			}
		}

		public void Start()
		{
			if (_thread.ThreadState == System.Threading.ThreadState.Unstarted)
			{
				_thread.Start();
			}
		}

		private void ExecuteWorker()
		{
			try
			{
				_operation.Execute();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				OnTerminated?.Invoke(this, EventArgs.Empty);
				if (FreeOnTerminate)
				{
					Dispose();
				}
			}
		}

		private void HandleException(Exception ex)
		{
			var args = new ThreadExceptionEventArgs(ex);
			OnException?.Invoke(this, args);

			// 如果没有订阅异常处理事件，记录到调试输出
			//if (!args.Handled)
			//{
			//	Debug.WriteLine($"Unhandled operation thread exception: {ex}");
			//}
		}

		public void WaitFor()
		{
			if (_thread.IsAlive)
			{
				_thread.Join();
			}
		}

		public void Abort()
		{
			if (_thread.IsAlive)
			{
				_thread.Abort();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// 释放托管资源
					_operation = null;
				}
				_disposed = true;
			}
		}
	}

	public class OperationsManagerItem
    {
        private int handle;
        private FileSourceOperation operation;
        private Thread operationThread;
        private OperationsManagerQueue queue;

        public int Handle => handle;
        public FileSourceOperation Operation => operation;
        public OperationsManagerQueue Queue { get => queue; set => queue = value; }
        public Thread OperationThread => operationThread;

        public OperationsManagerItem(int handle, FileSourceOperation operation)
        {
            this.handle = handle;
            this.operation = operation;
        }

        public OperationsManagerItem(int handle, FileSourceOperation operation, Thread thread)
        {
            this.handle = handle;
            this.operation = operation;
            this.operationThread = thread;
        }

        public void Start()
        {
            if (operationThread == null)
            {
                operationThread = new Thread(() => operation.Start());
                operationThread.Start();
            }
            else
            {
                operation.Start();
            }
        }

        /// <summary>
        /// Moves the item and places it before or after another operation.
        /// </summary>
        /// <param name="targetOperation">Handle to another operation where item should be moved.</param>
        /// <param name="placeBefore">If true then places item before TargetOperation, if false then places item after TargetOperation.</param>
        public void Move(int targetOperation, bool placeBefore)
        {
            var targetItem = OperationsManager.Instance.GetItemByHandle(targetOperation);
            if (targetItem != null)
            {
                if (Queue == targetItem.Queue)
                    Queue.Move(this, targetItem, placeBefore);
                else
                    SetQueue(targetItem.Queue, targetOperation, placeBefore);
            }
        }

        /// <summary>
        /// Moves the item to the bottom of its queue.
        /// </summary>
        public void MoveToBottom()
        {
            Queue.Move(this, null, false);
        }

        /// <summary>
        /// Moves the item to a new queue.
        /// </summary>
        /// <returns>The identifier of the new queue.</returns>
        public int MoveToNewQueue()
        {
            return OperationsManager.Instance.MoveToNewQueue(this);
        }

        /// <summary>
        /// Moves the item to the top of its queue.
        /// </summary>
        public void MoveToTop()
        {
            Queue.Move(this, null, true);
        }

        /// <summary>
        /// Removes the item from its queue.
        /// </summary>
        /// <returns>True if the item was successfully removed.</returns>
        public bool RemoveFromQueue()
        {
            bool result = Queue.Remove(this);
            if (Queue.Count == 0)
            {
                OperationsManager.Instance.RemoveQueue(Queue);
                Queue = null;
            }
            return result;
        }

        /// <summary>
        /// Sets the queue for this item.
        /// </summary>
        /// <param name="newQueue">The new queue to set.</param>
        /// <param name="insertAtFront">If true, inserts at the front of the queue, otherwise at the back.</param>
        public void SetQueue(OperationsManagerQueue newQueue, bool insertAtFront)
        {
            if (queue != newQueue && newQueue != null)
            {
                if (queue == null || RemoveFromQueue())
                {
                    queue = newQueue;
                    if (insertAtFront)
                        newQueue.Insert(this, 0);
                    else
                        newQueue.Insert(this);

                    OperationsManager.Instance.NotifyEvent(this, OperationEventType.Moved);
                }
            }
        }

        /// <summary>
        /// Sets the queue for this item and positions it relative to another operation.
        /// </summary>
        /// <param name="newQueue">The new queue to set.</param>
        /// <param name="targetOperation">The operation to position relative to.</param>
        /// <param name="placeBefore">If true, places before the target operation, otherwise after.</param>
        public void SetQueue(OperationsManagerQueue newQueue, int targetOperation, bool placeBefore)
        {
            if (queue != newQueue && newQueue != null)
            {
                if (queue == null || RemoveFromQueue())
                {
                    queue = newQueue;
                    newQueue.Insert(this, targetOperation, placeBefore);
                    OperationsManager.Instance.NotifyEvent(this, OperationEventType.Moved);
                }
            }
        }
    }

    public class OperationsManagerQueue
    {
        private readonly int identifier;
        private readonly List<OperationsManagerItem> items = new List<OperationsManagerItem>();
        private bool paused;

        public int Identifier => identifier;
        public bool Paused => paused;
        public int Count => items.Count;

        public OperationsManagerQueue(int identifier)
        {
            this.identifier = identifier;
        }

        /// <summary>
        /// Gets the index of an operation by its handle.
        /// </summary>
        /// <param name="handle">The operation handle to find.</param>
        /// <returns>The index of the operation in the queue, or -1 if not found.</returns>
        public int GetIndexByHandle(int handle)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Handle == handle)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets an operation item by its index in the queue.
        /// </summary>
        /// <param name="index">The index of the operation.</param>
        /// <returns>The operation item at the specified index.</returns>
        public OperationsManagerItem GetItem(int index)
        {
            return items[index];
        }

        /// <summary>
        /// Gets an operation item by its handle.
        /// </summary>
        /// <param name="handle">The handle of the operation to find.</param>
        /// <returns>The operation item with the specified handle, or null if not found.</returns>
        public OperationsManagerItem? GetItemByHandle(int handle)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Handle == handle)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Gets a description of the queue.
        /// </summary>
        /// <param name="includeCount">Whether to include the number of operations in the description.</param>
        /// <returns>A string describing the queue.</returns>
        public string GetDescription(bool includeCount)
        {
            string result = $"Queue #{Identifier}";
            if (includeCount)
                result += $" [{Count}]";
            return result;
        }

        /// <summary>
        /// Inserts an operation item into the queue.
        /// </summary>
        /// <param name="item">The operation item to insert.</param>
        /// <param name="position">The position at which to insert the item. -1 means at the end.</param>
        /// <returns>The position at which the item was inserted.</returns>
        public int Insert(OperationsManagerItem item, int position = -1)
        {
            if (position == -1)
                position = items.Count;
            else
            {
                if (!IsFree && position == 0 && items.Count > 0)
                    items[0].Operation.Pause();
            }

            items.Insert(position, item);

            if (!paused && (IsFree || position == 0))
                item.Start();
            else
                item.Operation.Pause();

            return position;
        }

        /// <summary>
        /// Inserts an operation item into the queue relative to another operation.
        /// </summary>
        /// <param name="item">The operation item to insert.</param>
        /// <param name="targetOperation">The handle of the operation to insert relative to.</param>
        /// <param name="placeBefore">If true, inserts before the target operation, otherwise after.</param>
        /// <returns>The position at which the item was inserted, or -1 if the target operation was not found.</returns>
        public int Insert(OperationsManagerItem item, int targetOperation, bool placeBefore)
        {
            int index = GetIndexByHandle(targetOperation);
            if (index >= 0)
            {
                if (!placeBefore)
                    index++;
                return Insert(item, index);
            }
            return -1;
        }

        /// <summary>
        /// Moves an operation within the queue.
        /// </summary>
        /// <param name="sourceItem">The operation item to move.</param>
        /// <param name="targetItem">The target operation item to move relative to. If null, moves to the top or bottom.</param>
        /// <param name="placeBefore">If true, places before the target, otherwise after. If target is null, true means top, false means bottom.</param>
        public void Move(OperationsManagerItem sourceItem, OperationsManagerItem targetItem, bool placeBefore)
        {
            int fromIndex = items.IndexOf(sourceItem);
            if (fromIndex < 0)
                return;

            int toIndex;
            bool shouldMove = false;

            if (targetItem == null)
            {
                if (placeBefore)
                    toIndex = 0;
                else
                    toIndex = items.Count - 1;
                shouldMove = true;
            }
            else
            {
                toIndex = items.IndexOf(targetItem);
                if (toIndex >= 0)
                {
                    if (placeBefore)
                    {
                        if (fromIndex < toIndex)
                            toIndex--;
                    }
                    else
                    {
                        if (fromIndex > toIndex)
                            toIndex++;
                    }
                    shouldMove = true;
                }
            }

            if (shouldMove && fromIndex != toIndex)
            {
                if (!paused && (fromIndex == 0 || toIndex == 0) && !IsFree)
                    items[0].Operation.Pause();

                items.RemoveAt(fromIndex);
                items.Insert(toIndex, sourceItem);

                if (!paused && (fromIndex == 0 || toIndex == 0) && !IsFree)
                    items[0].Start();

                OperationsManager.Instance.NotifyEvent(sourceItem, OperationEventType.Moved);
            }
        }

        /// <summary>
        /// Removes an operation item from the queue.
        /// </summary>
        /// <param name="item">The operation item to remove.</param>
        /// <returns>True if the item was successfully removed.</returns>
        public bool Remove(OperationsManagerItem item)
        {
            int index = items.IndexOf(item);
            if (index >= 0)
            {
                items.RemoveAt(index);
                if (!paused && !IsFree && index == 0 && items.Count > 0)
                    items[0].Start();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pauses all operations in the queue.
        /// </summary>
        public void Pause()
        {
            if (IsFree)
            {
                foreach (var item in items)
                    item.Operation.Pause();
            }
            else
            {
                paused = true;
                if (items.Count > 0)
                    items[0].Operation.Pause();
            }
        }

        /// <summary>
        /// Resumes all operations in the queue.
        /// </summary>
        public void Resume()
        {
            if (IsFree)
            {
                foreach (var item in items)
                    item.Start();
            }
            else
            {
                if (items.Count > 0)
                    items[0].Start();
                paused = false;
            }
        }

        /// <summary>
        /// Stops all operations in the queue.
        /// </summary>
        public void Stop()
        {
            foreach (var item in items)
                item.Operation.Stop();
        }

        /// <summary>
        /// Toggles the pause state of the queue.
        /// </summary>
        public void TogglePause()
        {
            if (paused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Returns true if this queue is a free operations queue.
        /// </summary>
        public bool IsFree => identifier == 0 || identifier == -1;
    }

    public class OperationsManager
    {
        internal static readonly int FreeOperationsQueueId = 0;
        private static readonly int ModalQueueId = -1;
        internal static readonly int SingleQueueId = 1;
        private const int InvalidOperationHandle = 0;

        private static OperationsManager? _instance;
        public static OperationsManager Instance => _instance ??= new OperationsManager();

        private int lastUsedHandle;
        private readonly List<OperationsManagerQueue> queues = new List<OperationsManagerQueue>();
        private readonly List<EventHandler<OperationEventArgs>> eventListeners = new List<EventHandler<OperationEventArgs>>();

        /// <summary>
        /// Creates a new instance of the OperationsManager.
        /// </summary>
        public OperationsManager()
        {
            lastUsedHandle = InvalidOperationHandle;
        }

        /// <summary>
        /// Gets the number of operations across all queues.
        /// </summary>
        public int OperationsCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < queues.Count; i++)
                    count += queues[i].Count;
                return count;
            }
        }

        /// <summary>
        /// Gets the number of queues.
        /// </summary>
        public int QueuesCount => queues.Count;

        /// <summary>
        /// Gets a queue by its index.
        /// </summary>
        /// <param name="index">The index of the queue.</param>
        /// <returns>The queue at the specified index, or null if the index is out of range.</returns>
        public OperationsManagerQueue? GetQueueByIndex(int index)
        {
            if (index >= 0 && index < queues.Count)
                return queues[index];
            return null;
        }

        /// <summary>
        /// Gets a queue by its identifier.
        /// </summary>
        /// <param name="identifier">The identifier of the queue.</param>
        /// <returns>The queue with the specified identifier, or null if not found.</returns>
        public OperationsManagerQueue? GetQueueByIdentifier(int identifier)
        {
            for (int i = 0; i < queues.Count; i++)
            {
                var queue = queues[i];
                if (queue.Identifier == identifier)
                    return queue;
            }
            return null;
        }

        /// <summary>
        /// Adds an operation to the manager.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <param name="showProgress">Whether to automatically show progress window.</param>
        /// <returns>The handle of the added operation.</returns>
        public int AddOperation(FileSourceOperation operation, bool showProgress = true)
        {
			if (operation.FileSource.Properties.HasFlag(FileSourceProperties.ListOnMainThread))
				return AddOperation(operation, ModalQueueId, false, showProgress);
			else
				return AddOperation(operation, FreeOperationsQueueId, false, showProgress);
		}

		/// <summary>
		/// Adds an operation to the manager.
		/// </summary>
		/// <param name="operation">The operation to add.</param>
		/// <param name="queueIdentifier">The identifier of the queue to add the operation to.</param>
		/// <param name="insertAtFrontOfQueue">Whether to insert at the front of the queue.</param>
		/// <param name="showProgress">Whether to automatically show progress window.</param>
		/// <returns>The handle of the added operation.</returns>
		public int AddOperation(FileSourceOperation operation, int queueIdentifier, bool insertAtFrontOfQueue, bool showProgress = true)
        {
            if (queueIdentifier == ModalQueueId)
            {
                return AddOperationModal(operation);
            }

            if (operation == null) return InvalidOperationHandle;

            int handle = GetNextUnusedHandle();
            var item = new OperationsManagerItem(handle, operation);

            try
            {
                operation.PreventStart();

                var queue = GetOrCreateQueue(queueIdentifier);
                item.SetQueue(queue, insertAtFrontOfQueue);
                NotifyEvent(item, OperationEventType.Added);

                if (showProgress)
                {
                    // In Pascal version, this would call ShowOperation(Item)
                    // Implement this if needed
                }

                return handle;
            }
            catch
            {
                return InvalidOperationHandle;
            }
        }

        /// <summary>
        /// Adds a modal operation to the manager.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The handle of the added operation.</returns>
        public int AddOperationModal(FileSourceOperation operation)
        {
            if (operation == null) return InvalidOperationHandle;

            // In Pascal, this would create a thread and execute the operation modally
            // For now, we'll just create a thread and add it to the modal queue
            Thread thread = new Thread(() => operation.Start());
            int handle = GetNextUnusedHandle();
            var item = new OperationsManagerItem(handle, operation, thread);

            try
            {
                operation.PreventStart();

                var queue = GetOrCreateQueue(ModalQueueId);
                item.SetQueue(queue, false);
                NotifyEvent(item, OperationEventType.Added);

                // In Pascal version, this would call ShowOperationModal(Item)
                // and then ThreadTerminatedEvent(Thread)
                // For now, we'll just start the thread
                thread.Start();

                return handle;
            }
            catch
            {
                return InvalidOperationHandle;
            }
        }

        /// <summary>
        /// Gets an operation item by its handle.
        /// </summary>
        /// <param name="handle">The handle of the operation.</param>
        /// <returns>The operation item with the specified handle, or null if not found.</returns>
        public OperationsManagerItem? GetItemByHandle(int handle)
        {
            if (handle != InvalidOperationHandle)
            {
                for (int i = 0; i < queues.Count; i++)
                {
                    var queue = queues[i];
                    var item = queue.GetItemByHandle(handle);
                    if (item != null)
                        return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an operation item by its operation.
        /// </summary>
        /// <param name="operation">The operation to find.</param>
        /// <returns>The operation item containing the specified operation, or null if not found.</returns>
        public OperationsManagerItem? GetItemByOperation(FileSourceOperation operation)
        {
            for (int i = 0; i < queues.Count; i++)
            {
                var queue = queues[i];
                for (int j = 0; j < queue.Count; j++)
                {
                    var item = queue.GetItem(j);
                    if (item.Operation == operation)
                        return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an operation item by its index across all queues.
        /// </summary>
        /// <param name="index">The index of the operation.</param>
        /// <returns>The operation item at the specified index, or null if the index is out of range.</returns>
        public OperationsManagerItem? GetItemByIndex(int index)
        {
            int counter = 0;
            for (int i = 0; i < queues.Count; i++)
            {
                var queue = queues[i];
                for (int j = 0; j < queue.Count; j++)
                {
                    if (counter == index)
                        return queue.GetItem(j);
                    counter++;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets or creates a queue with the specified identifier.
        /// </summary>
        /// <param name="identifier">The identifier of the queue.</param>
        /// <returns>The queue with the specified identifier, creating it if it doesn't exist.</returns>
        public OperationsManagerQueue GetOrCreateQueue(int identifier)
        {
            var queue = GetQueueByIdentifier(identifier);
            if (queue == null)
            {
                queue = new OperationsManagerQueue(identifier);
                queues.Add(queue);
            }
            return queue;
        }

        /// <summary>
        /// Gets a new unused queue identifier.
        /// </summary>
        /// <returns>A new queue identifier that is not currently in use.</returns>
        public int GetNewQueueIdentifier()
        {
            for (int id = FreeOperationsQueueId + 1; id < int.MaxValue; id++)
            {
                if (GetQueueByIdentifier(id) == null)
                    return id;
            }
            return FreeOperationsQueueId + 1; // Fallback, should never happen
        }

        /// <summary>
        /// Moves an operation to a new queue.
        /// </summary>
        /// <param name="item">The operation item to move.</param>
        /// <returns>The identifier of the new queue.</returns>
        public int MoveToNewQueue(OperationsManagerItem item)
        {
            for (int id = FreeOperationsQueueId + 1; id < int.MaxValue; id++)
            {
                if (GetQueueByIdentifier(id) == null)
                {
                    var newQueue = GetOrCreateQueue(id);
                    item.SetQueue(newQueue, false);
                    return id;
                }
            }
            return FreeOperationsQueueId + 1; // Fallback, should never happen
        }

        /// <summary>
        /// Moves an operation to a specific queue.
        /// </summary>
        /// <param name="item">The operation item to move.</param>
        /// <param name="queueIdentifier">The identifier of the queue to move to.</param>
        public void MoveToQueue(OperationsManagerItem item, int queueIdentifier)
        {
            var queue = GetOrCreateQueue(queueIdentifier);
            item.SetQueue(queue, false);
        }

        /// <summary>
        /// Removes a queue from the manager.
        /// </summary>
        /// <param name="queue">The queue to remove.</param>
        public void RemoveQueue(OperationsManagerQueue queue)
        {
            queues.Remove(queue);
        }

        /// <summary>
        /// Gets the next unused operation handle.
        /// </summary>
        /// <returns>A new operation handle that is not currently in use.</returns>
        private int GetNextUnusedHandle()
        {
            int result = Interlocked.Increment(ref lastUsedHandle);
            if (result == InvalidOperationHandle)
                result = Interlocked.Increment(ref lastUsedHandle);
            return result;
        }

        /// <summary>
        /// Notifies all listeners that an event has occurred.
        /// </summary>
        /// <param name="item">The operation item that the event is for.</param>
        /// <param name="eventType">The type of event that occurred.</param>
        public void NotifyEvent(OperationsManagerItem item, OperationEventType eventType)
        {
            var args = new OperationEventArgs(item, eventType);
            foreach (var listener in eventListeners)
                listener?.Invoke(this, args);
        }

        /// <summary>
        /// Handles a thread termination event.
        /// </summary>
        /// <param name="thread">The thread that terminated.</param>
        public void ThreadTerminatedEvent(Thread thread)
        {
            // Search for the terminated thread in the operations list
            for (int i = 0; i < queues.Count; i++)
            {
                var queue = queues[i];
                for (int j = 0; j < queue.Count; j++)
                {
                    var item = queue.GetItem(j);
                    if (item.OperationThread == thread)
                    {
                        item.RemoveFromQueue();
                        NotifyEvent(item, OperationEventType.Removed);
                        // Here the operation should not be used anymore
                        // (by the thread and by any operations viewer).
                        // In Pascal, this would free the item
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Pauses all operations in all queues.
        /// </summary>
        public void PauseAll()
        {
            for (int i = 0; i < queues.Count; i++)
                queues[i].Pause();
        }

        /// <summary>
        /// Stops all operations in all queues.
        /// </summary>
        public void StopAll()
        {
            for (int i = 0; i < queues.Count; i++)
                queues[i].Stop();
        }

        /// <summary>
        /// Resumes all operations in all queues.
        /// </summary>
        public void UnPauseAll()
        {
            for (int i = 0; i < queues.Count; i++)
                queues[i].Resume();
        }

        /// <summary>
        /// Calculates the overall progress of all operations.
        /// </summary>
        /// <returns>The average progress of all operations, as a percentage (0-100).</returns>
        public double AllProgressPoint()
        {
            double result = 0;
            if (OperationsCount > 0)
            {
                for (int i = 0; i < OperationsCount; i++)
                {
                    var item = GetItemByIndex(i);
                    if (item != null)
                        result += item.Operation.Progress;
                }
                result /= OperationsCount;
            }
            return result;
        }

        /// <summary>
        /// Adds an event listener.
        /// </summary>
        /// <param name="listener">The event listener to add.</param>
        public void AddEventListener(EventHandler<OperationEventArgs> listener)
        {
            if (!eventListeners.Contains(listener))
                eventListeners.Add(listener);
        }

        /// <summary>
        /// Removes an event listener.
        /// </summary>
        /// <param name="listener">The event listener to remove.</param>
        public void RemoveEventListener(EventHandler<OperationEventArgs> listener)
        {
            eventListeners.Remove(listener);
        }
    }

    public enum OperationEventType
    {
        Added,
        Removed,
        Moved
    }

    public class OperationEventArgs : EventArgs
    {
        public OperationsManagerItem? Item { get; }
        public OperationEventType EventType { get; }

        public OperationEventArgs(OperationsManagerItem? item, OperationEventType eventType)
        {
            Item = item;
            EventType = eventType;
        }
    }
}