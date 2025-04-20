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
        NoToAll,
        Yes,
        YesToAll,
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
        CompareAction, // The first action, hardcoded. Add new actions after this.

        /// <summary>
        /// Resume all response.
        /// </summary>
        ResumeAll,

        /// <summary>
        /// Rename response.
        /// </summary>
        Rename,

        /// <summary>
        /// Rename all response.
        /// </summary>
        RenameAll,

    }
    /// <summary>
    /// Delegate for handling UI actions
    /// </summary>
    /// <param name="action">The action to handle</param>
    public delegate void FileSourceOperationUIActionHandler(FileSourceOperationUIResponse action);

    /// <summary>
    /// General interface for communication: operation <-> user
    /// </summary>
    public abstract class FileSourceOperationUI : IFileSourceOperationUI
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
        public abstract FileSourceOperationUIResponse AskQuestion(
            string msg,
            string question,
            FileSourceOperationUIResponse[] possibleResponses,
            FileSourceOperationUIResponse defaultOKResponse,
            FileSourceOperationUIResponse defaultCancelResponse,
            FileSourceOperationUIActionHandler actionHandler = null);
    }
}