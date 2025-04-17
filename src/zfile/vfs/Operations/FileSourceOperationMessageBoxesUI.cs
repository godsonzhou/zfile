namespace zfile
{
    /// <summary>
    /// Implementation of file source operation UI using message boxes
    /// We assume here the UI is used only from the GUI thread.
    /// </summary>
    public class FileSourceOperationMessageBoxesUI : FileSourceOperationUI
    {
        private FileSourceOperationUIActionHandler _uiActionHandler;

        /// <summary>
        /// Creates a new instance of the FileSourceOperationMessageBoxesUI class
        /// </summary>
        public FileSourceOperationMessageBoxesUI() : base()
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
        public override FileSourceOperationUIResponse AskQuestion(
            string msg, 
            string question,
            FileSourceOperationUIResponse[] possibleResponses,
            FileSourceOperationUIResponse defaultOKResponse,
            FileSourceOperationUIResponse defaultCancelResponse,
            FileSourceOperationUIActionHandler actionHandler = null)
        {
            _uiActionHandler = actionHandler;

            // Convert responses to message box buttons
            List<MessageBoxButton> buttons = new List<MessageBoxButton>();
            foreach (var response in possibleResponses)
            {
                buttons.Add(ResponseToButton(response));
            }

            // Combine message and question
            string textMessage = msg;
            if (!string.IsNullOrEmpty(msg) && !string.IsNullOrEmpty(question))
            {
                textMessage = textMessage + " ";
            }
            textMessage = textMessage + question;

            // Show message box
            MessageBoxResult result = MessageBox.Show(
                textMessage,
                buttons.ToArray(),
                ResponseToButton(defaultOKResponse),
                ResponseToButton(defaultCancelResponse),
                QuestionActionHandler);

            return ResultToResponse(result);
        }

        /// <summary>
        /// Handles question actions
        /// </summary>
        /// <param name="button">The action button</param>
        private void QuestionActionHandler(MessageBoxActionButton button)
        {
            if (_uiActionHandler != null)
            {
                _uiActionHandler(ButtonToUIAction(button));
            }
        }

        /// <summary>
        /// Converts a UI response to a message box button
        /// </summary>
        /// <param name="response">The UI response</param>
        /// <returns>The message box button</returns>
        private MessageBoxButton ResponseToButton(FileSourceOperationUIResponse response)
        {
            switch (response)
            {
                case FileSourceOperationUIResponse.Ok: return MessageBoxButton.OK;
                case FileSourceOperationUIResponse.No: return MessageBoxButton.No;
                case FileSourceOperationUIResponse.Yes: return MessageBoxButton.Yes;
                case FileSourceOperationUIResponse.Cancel: return MessageBoxButton.Cancel;
                case FileSourceOperationUIResponse.None: return MessageBoxButton.None;
                case FileSourceOperationUIResponse.Append: return MessageBoxButton.Append;
                case FileSourceOperationUIResponse.Resume: return MessageBoxButton.Resume;
                case FileSourceOperationUIResponse.CopyInto: return MessageBoxButton.CopyInto;
                case FileSourceOperationUIResponse.CopyIntoAll: return MessageBoxButton.CopyIntoAll;
                case FileSourceOperationUIResponse.Overwrite: return MessageBoxButton.Overwrite;
                case FileSourceOperationUIResponse.OverwriteAll: return MessageBoxButton.OverwriteAll;
                case FileSourceOperationUIResponse.OverwriteOlder: return MessageBoxButton.OverwriteOlder;
                case FileSourceOperationUIResponse.OverwriteSmaller: return MessageBoxButton.OverwriteSmaller;
                case FileSourceOperationUIResponse.OverwriteLarger: return MessageBoxButton.OverwriteLarger;
                case FileSourceOperationUIResponse.AutoRenameSource: return MessageBoxButton.AutoRenameSource;
                case FileSourceOperationUIResponse.AutoRenameTarget: return MessageBoxButton.AutoRenameTarget;
                case FileSourceOperationUIResponse.RenameSource: return MessageBoxButton.RenameSource;
                case FileSourceOperationUIResponse.Skip: return MessageBoxButton.Skip;
                case FileSourceOperationUIResponse.SkipAll: return MessageBoxButton.SkipAll;
                case FileSourceOperationUIResponse.Ignore: return MessageBoxButton.Ignore;
                case FileSourceOperationUIResponse.IgnoreAll: return MessageBoxButton.IgnoreAll;
                case FileSourceOperationUIResponse.All: return MessageBoxButton.All;
                case FileSourceOperationUIResponse.Retry: return MessageBoxButton.Retry;
                case FileSourceOperationUIResponse.Abort: return MessageBoxButton.Abort;
                case FileSourceOperationUIResponse.RetryAdmin: return MessageBoxButton.RetryAdmin;
                case FileSourceOperationUIResponse.Unlock: return MessageBoxButton.Unlock;
                case FileSourceOperationUIResponse.CompareAction: return MessageBoxButton.Compare;
                default: return MessageBoxButton.OK;
            }
        }

        /// <summary>
        /// Converts a message box result to a UI response
        /// </summary>
        /// <param name="result">The message box result</param>
        /// <returns>The UI response</returns>
        private FileSourceOperationUIResponse ResultToResponse(MessageBoxResult result)
        {
            switch (result)
            {
                case MessageBoxResult.OK: return FileSourceOperationUIResponse.Ok;
                case MessageBoxResult.No: return FileSourceOperationUIResponse.No;
                case MessageBoxResult.Yes: return FileSourceOperationUIResponse.Yes;
                case MessageBoxResult.Cancel: return FileSourceOperationUIResponse.Cancel;
                case MessageBoxResult.None: return FileSourceOperationUIResponse.None;
                case MessageBoxResult.Append: return FileSourceOperationUIResponse.Append;
                case MessageBoxResult.Resume: return FileSourceOperationUIResponse.Resume;
                case MessageBoxResult.CopyInto: return FileSourceOperationUIResponse.CopyInto;
                case MessageBoxResult.CopyIntoAll: return FileSourceOperationUIResponse.CopyIntoAll;
                case MessageBoxResult.Overwrite: return FileSourceOperationUIResponse.Overwrite;
                case MessageBoxResult.OverwriteAll: return FileSourceOperationUIResponse.OverwriteAll;
                case MessageBoxResult.OverwriteOlder: return FileSourceOperationUIResponse.OverwriteOlder;
                case MessageBoxResult.OverwriteSmaller: return FileSourceOperationUIResponse.OverwriteSmaller;
                case MessageBoxResult.OverwriteLarger: return FileSourceOperationUIResponse.OverwriteLarger;
                case MessageBoxResult.AutoRenameSource: return FileSourceOperationUIResponse.AutoRenameSource;
                case MessageBoxResult.AutoRenameTarget: return FileSourceOperationUIResponse.AutoRenameTarget;
                case MessageBoxResult.RenameSource: return FileSourceOperationUIResponse.RenameSource;
                case MessageBoxResult.Skip: return FileSourceOperationUIResponse.Skip;
                case MessageBoxResult.SkipAll: return FileSourceOperationUIResponse.SkipAll;
                case MessageBoxResult.Ignore: return FileSourceOperationUIResponse.Ignore;
                case MessageBoxResult.IgnoreAll: return FileSourceOperationUIResponse.IgnoreAll;
                case MessageBoxResult.All: return FileSourceOperationUIResponse.All;
                case MessageBoxResult.Retry: return FileSourceOperationUIResponse.Retry;
                case MessageBoxResult.Abort: return FileSourceOperationUIResponse.Abort;
                case MessageBoxResult.RetryAdmin: return FileSourceOperationUIResponse.RetryAdmin;
                case MessageBoxResult.Unlock: return FileSourceOperationUIResponse.Unlock;
                default: return FileSourceOperationUIResponse.Invalid;
            }
        }

        /// <summary>
        /// Converts a message box action button to a UI action
        /// </summary>
        /// <param name="button">The message box action button</param>
        /// <returns>The UI action</returns>
        private FileSourceOperationUIResponse ButtonToUIAction(MessageBoxActionButton button)
        {
            switch (button)
            {
                case MessageBoxActionButton.Compare: return FileSourceOperationUIResponse.CompareAction;
                default: throw new ArgumentException("Unknown action button", nameof(button));
            }
        }
    }

    // These enums and classes would typically be in a separate file for message box functionality
    // but are included here for completeness

    /// <summary>
    /// Message box button
    /// </summary>
    public enum MessageBoxButton
    {
        OK, No, Yes, Cancel, None, Append, Resume,
        CopyInto, CopyIntoAll, Overwrite, OverwriteAll, OverwriteOlder,
        OverwriteSmaller, OverwriteLarger, AutoRenameSource, AutoRenameTarget, RenameSource,
        Skip, SkipAll, Ignore, IgnoreAll, All, Retry, Abort,
        RetryAdmin, Unlock, Compare
    }

    /// <summary>
    /// Message box action button
    /// </summary>
    public enum MessageBoxActionButton
    {
        Compare
    }

    /// <summary>
    /// Message box result
    /// </summary>
    public enum MessageBoxResult
    {
        OK, No, Yes, Cancel, None, Append, Resume,
        CopyInto, CopyIntoAll, Overwrite, OverwriteAll, OverwriteOlder,
        OverwriteSmaller, OverwriteLarger, AutoRenameSource, AutoRenameTarget, RenameSource,
        Skip, SkipAll, Ignore, IgnoreAll, All, Retry, Abort,
        RetryAdmin, Unlock
    }

    /// <summary>
    /// Static class for showing message boxes
    /// </summary>
    public static class MyMessageBox
    {
        /// <summary>
        /// Delegate for handling message box actions
        /// </summary>
        /// <param name="button">The action button</param>
        public delegate void MessageBoxActionHandler(MessageBoxActionButton button);

        /// <summary>
        /// Shows a message box
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="buttons">The buttons to show</param>
        /// <param name="defaultOKButton">The default OK button</param>
        /// <param name="defaultCancelButton">The default cancel button</param>
        /// <param name="actionHandler">Handler for action buttons</param>
        /// <returns>The result of the message box</returns>
        public static MessageBoxResult Show(
            string message,
            MessageBoxButton[] buttons,
            MessageBoxButton defaultOKButton,
            MessageBoxButton defaultCancelButton,
            MessageBoxActionHandler actionHandler = null)
        {
            // This is a placeholder implementation
            // In a real implementation, this would show a dialog with the specified buttons
            // and return the result based on the button clicked
            // For now, just return the default OK button result
            return MessageBoxResult.OK;
        }
    }
}