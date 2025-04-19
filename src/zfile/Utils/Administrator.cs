using System;
using System.Diagnostics;
using System.Security.Principal;

namespace zfile.Utils
{
    /// <summary>
    /// Utility class for handling administrator privileges and elevation
    /// </summary>
    public static class Administrator
    {
        /// <summary>
        /// Gets a value indicating whether the current process is running with elevated privileges
        /// </summary>
        public static bool IsElevated
        {
            get
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
        }

        /// <summary>
        /// Runs an action with elevated privileges
        /// </summary>
        /// <param name="action">The action to run</param>
        /// <returns>True if the action was successful, false otherwise</returns>
        public static bool RunElevated(Action action)
        {
            if (IsElevated)
            {
                // Already running with elevated privileges
                try
                {
                    action();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // Need to request elevation
                // This is a simplified implementation - in a real application,
                // you would need to launch a new process with elevated privileges
                // and communicate with it
                
                // For now, we'll just show a message and return false
                System.Windows.Forms.MessageBox.Show(
                    "This operation requires administrator privileges. Please run the application as administrator.",
                    "Elevation Required",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                
                return false;
            }
        }
    }
}
