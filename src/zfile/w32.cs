using System.Runtime.InteropServices;
using System.Text;
using WinShell;

namespace WinFormsApp1
{
	//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	//public struct SHFILEINFO
	//{
	//	public IntPtr hIcon;
	//	public int iIcon;
	//	public uint dwAttributes;
	//	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
	//	public string szDisplayName;
	//	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
	//	public string szTypeName;
	//}
	//[Flags]
	//public enum SHCONTF
	//{
	//	CHECKING_FOR_CHILDREN = 0x00010,
	//	FOLDERS = 0x00020,
	//	NONFOLDERS = 0x00040,
	//	INCLUDEHIDDEN = 0x00080,
	//	INIT_ON_FIRST_NEXT = 0x00100,
	//	NETPRINTERSRCH = 0x00200,
	//	SHAREABLE = 0x00400,
	//	STORAGE = 0x00800,
	//	NAVIGATION_ENUM = 0x01000,
	//	FASTITEMS = 0x02000,
	//	FLATLIST = 0x04000,
	//	ENABLE_ASYNC = 0x08000,
	//	INCLUDESUPERHIDDEN = 0x10000
	//}

	//[Flags]
	//public enum SFGAO : uint
	//{
	//	CANCOPY = 0x1,
	//	CANMOVE = 0x2,
	//	CANLINK = 0x4,
	//	STORAGE = 0x00000008,
	//	CANRENAME = 0x00000010,
	//	CANDELETE = 0x00000020,
	//	HASPROPSHEET = 0x00000040,
	//	DROPTARGET = 0x00000100,
	//	CAPABILITYMASK = 0x00000177,
	//	ENCRYPTED = 0x00002000,
	//	ISSLOW = 0x00004000,
	//	GHOSTED = 0x00008000,
	//	LINK = 0x00010000,
	//	SHARE = 0x00020000,
	//	READONLY = 0x00040000,
	//	HIDDEN = 0x00080000,
	//	DISPLAYATTRMASK = 0x000FC000,
	//	FILESYSANCESTOR = 0x10000000,
	//	FOLDER = 0x20000000,
	//	FILESYSTEM = 0x40000000,
	//	HASSUBFOLDER = 0x80000000,
	//	CONTENTSMASK = 0x80000000,
	//	VALIDATE = 0x01000000,
	//	REMOVABLE = 0x02000000,
	//	COMPRESSED = 0x04000000,
	//	BROWSABLE = 0x08000000,
	//	NONENUMERATED = 0x00100000,
	//	NEWCONTENT = 0x00200000,
	//	CANMONIKER = 0x00400000,
	//	HASSTORAGE = 0x00400000,
	//	STREAM = 0x00400000,
	//	STORAGEANCESTOR = 0x00800000,
	//	STORAGECAPMASK = 0x70C50008,
	//	PKEYSFGAOMASK = 0x81044000
	//}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct STRRET
	{
		public uint uType;
		public IntPtr pOleStr;
	}

	[Flags]
	public enum SHGDN : uint
	{
		NORMAL = 0x0000,
		INFOLDER = 0x0001,
		FOREDITING = 0x1000,
		FORADDRESSBAR = 0x4000,
		FORPARSING = 0x8000
	}

	//[Flags]
	//public enum GCS : uint
	//{
	//	VERBA = 0x00000000,
	//	HELPTEXTA = 0x00000001,
	//	VALIDATEA = 0x00000002,
	//	VERBW = 0x00000004,
	//	HELPTEXTW = 0x00000005,
	//	VALIDATEW = 0x00000006,
	//	UNICODE = 0x00000004
	//}
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

	[Flags]
	public enum MIIM : uint
	{
		STATE = 0x00000001,
		ID = 0x00000002,
		SUBMENU = 0x00000004,
		CHECKMARKS = 0x00000008,
		TYPE = 0x00000010,
		DATA = 0x00000020,
		STRING = 0x00000040,
		BITMAP = 0x00000080,
		FTYPE = 0x00000100
	}

	//[StructLayout(LayoutKind.Sequential)]
	//public struct CMINVOKECOMMANDINFOEX
	//{
	//	public int cbSize;
	//	public CMIC fMask;
	//	public IntPtr hwnd;
	//	public IntPtr lpVerb;
	//	public string lpParameters;
	//	public string lpDirectory;
	//	public int nShow;
	//	public int dwHotKey;
	//	public IntPtr hIcon;
	//	public string lpTitle;
	//	public IntPtr lpVerbW;
	//	public string lpParametersW;
	//	public string lpDirectoryW;
	//	public string lpTitleW;
	//	public POINT ptInvoke;
	//}

	[Flags]
	public enum CMIC : uint
	{
		MASK_ICON = 0x00000010,
		MASK_HOTKEY = 0x00000020,
		MASK_NOASYNC = 0x00000100,
		MASK_FLAG_NO_UI = 0x00000400,
		MASK_UNICODE = 0x00004000,
		MASK_NO_CONSOLE = 0x00008000,
		MASK_ASYNCOK = 0x00100000,
		MASK_NOZONECHECKS = 0x00800000,
		MASK_SHIFT_DOWN = 0x10000000,
		MASK_CONTROL_DOWN = 0x40000000,
		MASK_FLAG_LOG_USAGE = 0x04000000,
		MASK_PTINVOKE = 0x20000000
	}

	//[StructLayout(LayoutKind.Sequential)]
	//public struct POINT
	//{
	//	public int x;
	//	public int y;
	//	public POINT(int x, int y)
	//	{
	//		this.x = x;
	//		this.y = y;
	//	}
	//}

	//[Flags]
	//public enum CMF : uint
	//{
	//	NORMAL = 0x00000000,
	//	DEFAULTONLY = 0x00000001,
	//	VERBSONLY = 0x00000002,
	//	EXPLORE = 0x00000004,
	//	NOVERBS = 0x00000008,
	//	CANRENAME = 0x00000010,
	//	NODEFAULT = 0x00000020,
	//	INCLUDESTATIC = 0x00000040,
	//	ITEMMENU = 0x00000080,
	//	EXTENDEDVERBS = 0x00000100,
	//	DISABLEDVERBS = 0x00000200,
	//	ASYNCVERBSTATE = 0x00000400,
	//	OPTIMIZEFORINVOKE = 0x00000800,
	//	SYNCCASCADEMENU = 0x00001000,
	//	DONOTPICKDEFAULT = 0x00002000,
	//	RESERVED = 0xffff0000
	//}
	public enum ShowWindowCommands : int
	{
		SW_HIDE = 0,
		SW_SHOWNORMAL = 1,
		SW_NORMAL = 1,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = 3,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,  //显示一个窗口，同时令其进入活动状态
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9,
		SW_SHOWDEFAULT = 10,
		SW_MAX = 10
	}
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E6-0000-0000-C000-000000000046")]
	public interface IShellFolder
	{
		void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		void EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList);
		void BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IShellFolder ppv);
		void BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
		[PreserveSig]
		int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
		void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref SFGAO rgfInOut);
		void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref Guid riid, IntPtr rgfReserved, out IContextMenu ppv);
		void GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out STRRET pName);
		void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, SHCONTF uFlags, out IntPtr ppidlOut);
	}

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E4-0000-0000-C000-000000000046")]
	public interface IContextMenu
	{
		[PreserveSig]
		int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, CMF uFlags);
		void InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
		void GetCommandString(uint idCmd, GetCommandStringInformations uType, IntPtr pReserved, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName, uint cchMax);
	}


	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F2-0000-0000-C000-000000000046")]
	public interface IEnumIDList
	{
		[PreserveSig]
		int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
		void Skip(uint celt);
		void Reset();
		void Clone(out IEnumIDList ppenum);
	}
	public static class w32
	{
		[DllImport("shell32.dll")]
		public static extern IntPtr ShellExecute(IntPtr hwnd, //窗口句柄
			string lpOperation, //指定要进行的操作
			string lpFile,  //要执行的程序、要浏览的文件夹或者网址
			string lpParameters, //若lpFile参数是一个可执行程序，则此参数指定命令行参数
			string lpDirectory, //指定默认目录
			int nShowCmd   //若lpFile参数是一个可执行程序，则此参数指定程序窗口的初始显示方式(参考如下枚举)
		);
		[DllImport("kernel32.dll")]
		public static extern int WinExec(string programPath, int operType);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr CreatePopupMenu();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyMenu(IntPtr hMenu);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetMenuItemCount(IntPtr hMenu);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool GetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition, ref MENUITEMINFO lpmii);

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr ILCreateFromPath(string pszPath);

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern void ILFree(IntPtr pidl);

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern IntPtr ILClone(IntPtr pidl);

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern void ILRemoveLastID(IntPtr pidl);

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern IntPtr ILFindLastID(IntPtr pidl);

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern int SHGetDesktopFolder(out IShellFolder ppshf);
		public static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
		public static readonly Guid IID_IContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");
		public const int SW_SHOWNORMAL = 1;
		public const uint SHGFI_ICON = 0x000000100;
		public const uint SHGFI_SMALLICON = 0x000000001;

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);
		// 在 API 类中添加
		public static WinShell.IShellFolder GetParentFolder(string path)
		{
			IntPtr pidl = w32.ILCreateFromPath(path);
			try
			{
				WinShell.IShellFolder desktop = API.GetDesktopFolder(out _);
				Guid iid = Guids.IID_IShellFolder;
				WinShell.IShellFolder folder;
				desktop.BindToObject(pidl, IntPtr.Zero, ref iid, out folder);
				return folder;
			}
			finally
			{
				if (pidl != IntPtr.Zero)
				{
					w32.ILFree(pidl);
				}
			}
		}
	}

}
