namespace zfile.Operations
{
    public class OperationsManagerItem
    {
        private int handle;
        private IFileSourceOperation operation;
        private Thread operationThread;
        private OperationsManagerQueue queue;

        public int Handle => handle;
        public IFileSourceOperation Operation => operation;
        public OperationsManagerQueue Queue => queue;

        public OperationsManagerItem(int handle, IFileSourceOperation operation)
        {
            this.handle = handle;
            this.operation = operation;
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
                operation.Resume();
            }
        }

        public void SetQueue(OperationsManagerQueue newQueue, bool insertAtFront)
        {
            if (queue != newQueue && newQueue != null)
            {
                queue?.Remove(this);
                queue = newQueue;
                if (insertAtFront)
                    newQueue.Insert(this, 0);
                else
                    newQueue.Insert(this);
            }
        }
    }

    public class OperationsManagerQueue
    {
        private int identifier;
        private List<OperationsManagerItem> items = new List<OperationsManagerItem>();
        private bool paused;

        public int Identifier => identifier;
        public bool Paused => paused;
        public int Count => items.Count;

        public OperationsManagerQueue(int identifier)
        {
            this.identifier = identifier;
        }

        public void Insert(OperationsManagerItem item, int position = -1)
        {
            if (position == -1)
                position = items.Count;

            if (!IsFree && position == 0 && items.Count > 0)
                items[0].Operation.Pause();

            items.Insert(position, item);

            if (!paused && (IsFree || position == 0))
                item.Start();
            else
                item.Operation.Pause();
        }

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

        public bool IsFree => identifier == 0 || identifier == -1;
    }

    public class OperationsManager
    {
        private static readonly int FreeOperationsQueueId = 0;
        private static readonly int ModalQueueId = -1;
        private static readonly int SingleQueueId = 1;

        private int lastUsedHandle;
        private List<OperationsManagerQueue> queues = new List<OperationsManagerQueue>();
        private List<EventHandler<OperationEventArgs>> eventListeners = new List<EventHandler<OperationEventArgs>>();

        public int AddOperation(IFileSourceOperation operation, bool showProgress = true)
        {
            if (operation.IsModal)
                return AddOperation(operation, ModalQueueId, false, showProgress);
            else
                return AddOperation(operation, FreeOperationsQueueId, false, showProgress);
        }

        public int AddOperation(IFileSourceOperation operation, int queueIdentifier, bool insertAtFrontOfQueue, bool showProgress = true)
        {
            if (operation == null) return 0;

            int handle = Interlocked.Increment(ref lastUsedHandle);
            var item = new OperationsManagerItem(handle, operation);

            try
            {
                var queue = GetOrCreateQueue(queueIdentifier);
                item.SetQueue(queue, insertAtFrontOfQueue);
                NotifyEvent(item, OperationEventType.Added);
                return handle;
            }
            catch
            {
                return 0;
            }
        }

        private OperationsManagerQueue GetOrCreateQueue(int identifier)
        {
            var queue = queues.Find(q => q.Identifier == identifier);
            if (queue == null)
            {
                queue = new OperationsManagerQueue(identifier);
                queues.Add(queue);
            }
            return queue;
        }

        private void NotifyEvent(OperationsManagerItem item, OperationEventType eventType)
        {
            var args = new OperationEventArgs(item, eventType);
            foreach (var listener in eventListeners)
                listener?.Invoke(this, args);
        }

        public void AddEventListener(EventHandler<OperationEventArgs> listener)
        {
            if (!eventListeners.Contains(listener))
                eventListeners.Add(listener);
        }

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
        public OperationsManagerItem Item { get; }
        public OperationEventType EventType { get; }

        public OperationEventArgs(OperationsManagerItem item, OperationEventType eventType)
        {
            Item = item;
            EventType = eventType;
        }
    }
}