namespace zfile
{
    /// <summary>
    /// Possible responses for file source operation UI questions
    /// </summary>
    public enum FileSourceOperationUIResponse
    {
        Invalid,
        Ok,
        No,
        Yes,
        Cancel,
        None,
        Append,      // for files
        Resume,      // for files
        CopyInto,    // for directories
        CopyIntoAll, // for directories
        Overwrite,
        OverwriteAll,
        OverwriteOlder,
        OverwriteSmaller,
        OverwriteLarger,
        AutoRenameSource,
        AutoRenameTarget,
        RenameSource,
        Skip,
        SkipAll,
        Ignore,
        IgnoreAll,
        All,
        Retry,
        Abort,
        RetryAdmin,
        Unlock,
        // Actions will never be returned since they do not close the window, handle them in ActionHandler.
        CompareAction // The first action, hardcoded. Add new actions after this.
    }

    /// <summary>
    /// UI answers (excluding actions)
    /// </summary>
    public enum FileSourceOperationUIAnswer
    {
        Invalid = FileSourceOperationUIResponse.Invalid,
        Ok = FileSourceOperationUIResponse.Ok,
        No = FileSourceOperationUIResponse.No,
        Yes = FileSourceOperationUIResponse.Yes,
        Cancel = FileSourceOperationUIResponse.Cancel,
        None = FileSourceOperationUIResponse.None,
        Append = FileSourceOperationUIResponse.Append,
        Resume = FileSourceOperationUIResponse.Resume,
        CopyInto = FileSourceOperationUIResponse.CopyInto,
        CopyIntoAll = FileSourceOperationUIResponse.CopyIntoAll,
        Overwrite = FileSourceOperationUIResponse.Overwrite,
        OverwriteAll = FileSourceOperationUIResponse.OverwriteAll,
        OverwriteOlder = FileSourceOperationUIResponse.OverwriteOlder,
        OverwriteSmaller = FileSourceOperationUIResponse.OverwriteSmaller,
        OverwriteLarger = FileSourceOperationUIResponse.OverwriteLarger,
        AutoRenameSource = FileSourceOperationUIResponse.AutoRenameSource,
        AutoRenameTarget = FileSourceOperationUIResponse.AutoRenameTarget,
        RenameSource = FileSourceOperationUIResponse.RenameSource,
        Skip = FileSourceOperationUIResponse.Skip,
        SkipAll = FileSourceOperationUIResponse.SkipAll,
        Ignore = FileSourceOperationUIResponse.Ignore,
        IgnoreAll = FileSourceOperationUIResponse.IgnoreAll,
        All = FileSourceOperationUIResponse.All,
        Retry = FileSourceOperationUIResponse.Retry,
        Abort = FileSourceOperationUIResponse.Abort,
        RetryAdmin = FileSourceOperationUIResponse.RetryAdmin,
        Unlock = FileSourceOperationUIResponse.Unlock
    }

    /// <summary>
    /// UI actions
    /// </summary>
    public enum FileSourceOperationUIAction
    {
        CompareAction = FileSourceOperationUIResponse.CompareAction
    }

    /// <summary>
    /// Delegate for handling UI actions
    /// </summary>
    /// <param name="action">The action to handle</param>
    public delegate void FileSourceOperationUIActionHandler(FileSourceOperationUIAction action);

    /// <summary>
    /// General interface for communication: operation <-> user
    /// </summary>
    public abstract class FileSourceOperationUI
    {
        /// <summary>
        /// Creates a new instance of the FileSourceOperationUI class
        /// </summary>
        protected FileSourceOperationUI()
        {
        }

        /// <summary>
        /// Asks a question to the user
        /// </summary>
        /// <param name="msg">Message to display</param>
        /// <param name="question">Question to ask</param>
        /// <param name="possibleResponses">Possible responses</param>
        /// <param name="defaultOKResponse">Default OK response</param>
        /// <param name="defaultCancelResponse">Default cancel response</param>
        /// <param name="actionHandler">Handler for UI actions</param>
        /// <returns>User's answer</returns>
        public abstract FileSourceOperationUIAnswer AskQuestion(
            string msg, 
            string question,
            FileSourceOperationUIResponse[] possibleResponses,
            FileSourceOperationUIResponse defaultOKResponse,
            FileSourceOperationUIAnswer defaultCancelResponse,
            FileSourceOperationUIActionHandler actionHandler = null);
    }
}