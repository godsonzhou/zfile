using System.Globalization;

namespace zfile
{
    /// <summary>
    /// Miscellaneous functions for file source operations and queues
    /// </summary>
    public static class FileSourceOperationMisc
    {
        /// <summary>
        /// Gets a string representation of an operation state
        /// </summary>
        /// <param name="operationState">The operation state</param>
        /// <returns>String representation of the operation state</returns>
        public static string GetOperationStateString(FileSourceOperationState operationState)
        {
            if (operationState != FileSourceOperationState.Running)
                return " [" + GetFileSourceOperationStateText(operationState) + "]";
            else
                return string.Empty;
        }

        /// <summary>
        /// Gets the text representation of a file source operation state
        /// </summary>
        /// <param name="state">The operation state</param>
        /// <returns>Text representation of the state</returns>
        private static string GetFileSourceOperationStateText(FileSourceOperationState state)
        {
            switch (state)
            {
                case FileSourceOperationState.NotStarted:
                    return "Not Started";
                case FileSourceOperationState.Paused:
                    return "Paused";
                case FileSourceOperationState.Stopping:
                    return "Stopping";
                case FileSourceOperationState.Stopped:
                    return "Stopped";
                case FileSourceOperationState.WaitingForFeedback:
                    return "Waiting for Feedback";
                case FileSourceOperationState.WaitingForConnection:
                    return "Waiting for Connection";
                case FileSourceOperationState.Starting:
                    return "Starting";
                default:
                    return state.ToString();
            }
        }

        /// <summary>
        /// Gets a string representation of a progress value
        /// </summary>
        /// <param name="progress">The progress value (0.0 to 1.0)</param>
        /// <returns>String representation of the progress</returns>
        public static string GetProgressString(double progress)
        {
            return (progress * 100).ToString("F0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Shows an operation in a non-modal window
        /// </summary>
        /// <param name="operationItem">The operation item</param>
        public static void ShowOperation(OperationsManagerItem operationItem)
        {
            if (operationItem.Queue.IsFree || operationItem.Queue.Count == 1)
            {
                if (GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindow ||
                    GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindowMinimized)
                {
                    OperationProgressWindowOptions options = OperationProgressWindowOptions.None;
                    
                    if (GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindowMinimized)
                        options |= OperationProgressWindowOptions.StartMinimized;
                    
                    FileOperationDialog.ShowFor(operationItem.Handle, options);
                }
            }
        }

        /// <summary>
        /// Shows an operation in a modal window
        /// </summary>
        /// <param name="operationItem">The operation item</param>
        public static void ShowOperationModal(OperationsManagerItem operationItem)
        {
            using (var dialog = new FileOperationDialog(operationItem.Handle))
            {
                dialog.ShowDialog();
            }
        }
    }

    /// <summary>
    /// File operations progress kind
    /// </summary>
    public enum FileOperationsProgressKind
    {
        /// <summary>
        /// No progress indication
        /// </summary>
        None,

        /// <summary>
        /// Progress in separate window
        /// </summary>
        SeparateWindow,

        /// <summary>
        /// Progress in separate minimized window
        /// </summary>
        SeparateWindowMinimized,

        /// <summary>
        /// Progress in operations panel
        /// </summary>
        OperationsPanel
    }

    /// <summary>
    /// Operation progress window options
    /// </summary>
    [Flags]
    public enum OperationProgressWindowOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0,

        /// <summary>
        /// Start minimized
        /// </summary>
        StartMinimized = 1
    }

    /// <summary>
    /// Placeholder for the file operation dialog
    /// </summary>
    public class FileOperationDialog : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the FileOperationDialog class
        /// </summary>
        /// <param name="handle">The operation handle</param>
        public FileOperationDialog(int handle)
        {
            // Implementation would go here
        }

        /// <summary>
        /// Shows the dialog for an operation
        /// </summary>
        /// <param name="handle">The operation handle</param>
        /// <param name="options">The options</param>
        public static void ShowFor(int handle, OperationProgressWindowOptions options)
        {
            // Implementation would go here
        }

        /// <summary>
        /// Shows the dialog modally
        /// </summary>
        public void ShowDialog()
        {
            // Implementation would go here
        }

        /// <summary>
        /// Disposes the dialog
        /// </summary>
        public void Dispose()
        {
            // Implementation would go here
        }
    }
}