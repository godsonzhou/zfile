using System;
using System.Windows.Forms;

namespace zfile.Operations
{
    /// <summary>
    /// Base class for file source operation options UI
    /// </summary>
    public class FileSourceOperationOptionsUI : UserControl
    {
        /// <summary>
        /// Creates a new instance of the FileSourceOperationOptionsUI class
        /// </summary>
        /// <param name="owner">The owner component</param>
        /// <param name="fileSource">The file source</param>
        public FileSourceOperationOptionsUI(IComponent owner, object fileSource) : base()
        {
            // Base implementation does nothing
        }

        /// <summary>
        /// Gets the options class for this UI
        /// </summary>
        /// <returns>The options class</returns>
        public static Type GetOptionsClass()
        {
            return typeof(FileSourceOperationOptionsUI);
        }

        /// <summary>
        /// Saves the options from the UI controls
        /// </summary>
        public virtual void SaveOptions()
        {
            // Base implementation does nothing
        }

        /// <summary>
        /// Sets operation options from GUI controls
        /// </summary>
        /// <param name="operation">The operation to set options for</param>
        public virtual void SetOperationOptions(object operation)
        {
            // Base implementation does nothing
        }
    }
}