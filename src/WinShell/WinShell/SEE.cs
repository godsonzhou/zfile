using System;

namespace WinShell
{
    /// <summary>
    /// ShellExecuteEx flags
    /// </summary>
    [Flags]
    public enum SEE : uint
    {
        /// <summary>
        /// Use the PIDL in lpIDList instead of the file name in lpFile
        /// </summary>
        MASK_IDLIST = 0x00000100,
        
        /// <summary>
        /// The process handle is returned in hProcess
        /// </summary>
        MASK_NOCLOSEPROCESS = 0x00000040,
        
        /// <summary>
        /// Use the class name given by lpClass
        /// </summary>
        MASK_CLASSNAME = 0x00000001,
        
        /// <summary>
        /// Use the class key given by hkeyClass
        /// </summary>
        MASK_CLASSKEY = 0x00000003,
        
        /// <summary>
        /// Invokes the default verb
        /// </summary>
        MASK_INVOKEIDLIST = 0x0000000C,
        
        /// <summary>
        /// The window handle in hwnd is used as the parent for a message box
        /// </summary>
        MASK_CONNECTNETDRV = 0x00000080,
        
        /// <summary>
        /// Don't display an error message box if an error occurs
        /// </summary>
        MASK_FLAG_NO_UI = 0x00000400,
        
        /// <summary>
        /// Use the directory given by lpDirectory as the working directory
        /// </summary>
        MASK_DOENVSUBST = 0x00000200,
        
        /// <summary>
        /// Don't use the search path to find the executable
        /// </summary>
        MASK_FLAG_NO_CONSOLE = 0x00008000,
        
        /// <summary>
        /// Don't create a new console window for the process
        /// </summary>
        MASK_ASYNCOK = 0x00100000,
        
        /// <summary>
        /// The function returns immediately, not waiting for the application to finish
        /// </summary>
        MASK_HMONITOR = 0x00200000
    }
}
