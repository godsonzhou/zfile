using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WinShell
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct STRRET
	{
		public uint uType;
		public IntPtr pOleStr;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct MENUITEMINFO
	{
		public uint cbSize;
		public MIIM fMask;
		public uint fType;
		public uint fState;
		public uint wID;
		public IntPtr hSubMenu;
		public IntPtr hbmpChecked;
		public IntPtr hbmpUnchecked;
		public IntPtr dwItemData;
		public string dwTypeData;
		public uint cch;
		public IntPtr hbmpItem;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public SFGAO dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = w32.MAX_PATH)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct CMINVOKECOMMANDINFO
    {
        public int cbSize;				// sizeof(CMINVOKECOMMANDINFO)
        public int fMask;				// any combination of CMIC_MASK_*
        public IntPtr hwnd;				// might be NULL (indicating no owner window)
        public IntPtr lpVerb;			// either a string or MAKEINTRESOURCE(idOffset)
        public IntPtr lpParameters;		// might be NULL (indicating no parameter)
        public IntPtr lpDirectory;		// might be NULL (indicating no specific directory)
        public int nShow;				// one of SW_ values for ShowWindow() API
        public int dwHotKey;
        public IntPtr hIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CMINVOKECOMMANDINFOEX
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public IntPtr lpVerb;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpDirectory;
        public int nShow;
        public int dwHotKey;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpTitle;
        public IntPtr lpVerbW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpParametersW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDirectoryW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpTitleW;
        public POINT ptInvoke;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct POINT
    {
        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;
    }

}
