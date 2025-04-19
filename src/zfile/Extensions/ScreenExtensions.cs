using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace zfile.Extensions
{
    /// <summary>
    /// Extension methods for the Screen class
    /// </summary>
    public static class ScreenExtensions
    {
        /// <summary>
        /// Gets the logical DPI of the screen
        /// </summary>
        /// <param name="screen">The screen</param>
        /// <returns>The logical DPI of the screen</returns>
        public static int LogicalDpi(this Screen screen)
        {
            // Default DPI value if screen is null
            if (screen == null)
            {
                return 96; // Default Windows DPI
            }

            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                // Get the DPI of the screen
                return (int)graphics.DpiX;
            }
        }
    }
}
