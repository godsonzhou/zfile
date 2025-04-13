using System;
using System.Collections.Generic;

namespace Files.Operations
{
    public enum OperationState
    {
        NotStarted,
        Running,
        Paused,
        Completed,
        Stopped,
        Failed
    }

    public interface IFileSourceOperation
    {
        int OperationHandle { get; }
        OperationState State { get; }
        bool IsFree { get; }
        bool IsModal { get; }
        
        void Start();
        void Pause();
        void Stop();
        void Resume();
        
        event EventHandler<EventArgs> StateChanged;
        event EventHandler<EventArgs> ProgressChanged;
    }
}