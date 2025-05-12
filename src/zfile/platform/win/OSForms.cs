using System.Runtime.InteropServices;

namespace zfile.Platform
{
    public static class OSForms
    {
        public static void NetConnect()
        {
            //if ((sender as ToolStripMenuItem)?.Tag is int tag && tag == 0)
            {
                var res = new NETRESOURCE();
                res.dwType = RESOURCETYPE_DISK;

                var cds = new CONNECTDLGSTRUCT();
                cds.cbStructure = (uint)Marshal.SizeOf(typeof(CONNECTDLGSTRUCT));
                cds.hwndOwner = MainForm.Instance.Handle;
                cds.lpConnRes = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NETRESOURCE)));
                Marshal.StructureToPtr(res, cds.lpConnRes, false);
                cds.dwFlags = 0;

                try
                {
                    uint ret = WNetConnectionDialog1(ref cds);
                    if (ret == NO_ERROR)
                    {
                        // Convert drive number to drive letter path (e.g., "C:\")
                        char driveLetter = (char)('a' + cds.dwDevNum - 1);
                        string drivePath = $"{char.ToUpper(driveLetter)}:\\";

                        // Set the file system path in the active frame
                        //MainForm.Instance.SetFileSystemPath(MainForm.Instance.ActiveFrame, drivePath);
					}
                    else if (ret != 0xFFFFFFFF) // DWORD(-1)
                    {
                        MessageBox.Show(GetSysErrorMessage(ret), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    // Free allocated memory
                    if (cds.lpConnRes != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(cds.lpConnRes);
                    }
                }
            }
        }
		public static void NetDisconnect()
		{

		}
		// Win32 API constants
		private const uint NO_ERROR = 0;
        private const uint RESOURCETYPE_DISK = 1;

        // Win32 API structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpLocalName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpRemoteName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpComment;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpProvider;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct CONNECTDLGSTRUCT
        {
            public uint cbStructure;
            public IntPtr hwndOwner;
            public IntPtr lpConnRes;
            public uint dwFlags;
            public uint dwDevNum;
        }

        // Win32 API functions
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern uint WNetConnectionDialog1(ref CONNECTDLGSTRUCT lpConnDlgStruct);

        // Helper method to get system error message
        private static string GetSysErrorMessage(uint errorCode)
        {
            IntPtr lpMsgBuf = IntPtr.Zero;
            uint dwFlags = 0x00001000 | 0x00000200; // FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM

            int size = FormatMessage(dwFlags, IntPtr.Zero, errorCode, 0, ref lpMsgBuf, 0, IntPtr.Zero);
            if (size == 0)
                return $"Error code: {errorCode}";

            string message = Marshal.PtrToStringAuto(lpMsgBuf);
            LocalFree(lpMsgBuf);
            return message;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId,
            uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr Arguments);
    }
}