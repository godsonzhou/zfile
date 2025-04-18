using System;
using System.Runtime.InteropServices;

namespace zfile
{
    /// <summary>
    /// Extension methods for WcxModule
    /// </summary>
    public static class WcxModuleExtensions
    {
        /// <summary>
        /// Reads a header from a WCX archive
        /// </summary>
        /// <param name="module">The WCX module</param>
        /// <param name="arcHandle">The archive handle</param>
        /// <param name="header">The header to fill</param>
        /// <returns>0 on success, non-zero on failure</returns>
        public static int ReadWCXHeader(this WcxModule module, IntPtr arcHandle, ref WcxHeader header)
        {
            THeaderDataExW headerData = new THeaderDataExW();
            if (module.ReadHeader(arcHandle, out headerData))
            {
                // Convert THeaderDataExW to WcxHeader
                header.FileName = headerData.FileName;
                header.FileAttr = (FileAttributes)headerData.FileAttr;
                header.PackSize = (long)((ulong)headerData.PackSizeHigh << 32 | headerData.PackSizeLow);
                header.UnpSize = (long)((ulong)headerData.UnpSizeHigh << 32 | headerData.UnpSizeLow);
                header.FileTime = headerData.FileTime;
                header.CRC = headerData.FileCRC;
                header.Method = headerData.Method;
                header.Flags = headerData.Flags;
                return 0; // Success
            }
            return -1; // Error
        }

        /// <summary>
        /// Converts a DOS file time to a DateTime
        /// </summary>
        /// <param name="fileTime">The DOS file time</param>
        /// <returns>The corresponding DateTime</returns>
        public static DateTime FileTimeToDateTime(int fileTime)
        {
            try
            {
                // Convert DOS time format to DateTime
                int year = (int)(((fileTime >> 25) & 0x7F) + 1980);
                int month = (int)((fileTime >> 21) & 0x0F);
                int day = (int)((fileTime >> 16) & 0x1F);
                int hour = (int)((fileTime >> 11) & 0x1F);
                int minute = (int)((fileTime >> 5) & 0x3F);
                int second = (int)((fileTime & 0x1F) * 2);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Sets the crypt callback for a WCX module
        /// </summary>
        /// <param name="module">The WCX module</param>
        /// <param name="cryptoNr">The crypto number</param>
        /// <param name="flags">The flags</param>
        /// <param name="cryptProcA">The ANSI crypt callback</param>
        /// <param name="cryptProcW">The Unicode crypt callback</param>
        public static void SetCryptCallback(this WcxModule module, int cryptoNr, int flags,
            Func<int, int, string, string, int> cryptProcA, Func<int, int, string, string, int> cryptProcW)
        {
            if (module.IsUnicode && cryptProcW != null)
            {
                IntPtr pProc = Marshal.GetFunctionPointerForDelegate(cryptProcW);
                module.SetCryptCallback(pProc, cryptoNr, flags);
            }
            else if (cryptProcA != null)
            {
                IntPtr pProc = Marshal.GetFunctionPointerForDelegate(cryptProcA);
                module.SetCryptCallback(pProc, cryptoNr, flags);
            }
        }
    }
}
