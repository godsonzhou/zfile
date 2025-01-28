
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WinShell
{
	public static class w32
	{
		public const int MAX_PATH = 260;
		public const int S_OK = 0;
		public const int S_FALSE = 1;
		public const uint CMD_FIRST = 1;
		public const uint CMD_LAST = 30000;
		public static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
		public static readonly Guid IID_IContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");
		public const int SW_SHOWNORMAL = 1;
		public const uint SHGFI_ICON = 0x000000100;
		public const uint SHGFI_SMALLICON = 0x000000001;
		public static IShellFolder GetDesktopFolder(out IntPtr ppshf)
		{
			API.SHGetDesktopFolder(out ppshf);
			Object obj = Marshal.GetObjectForIUnknown(ppshf);
			return (IShellFolder)obj;
		}


		public static string GetPathByIShell(IShellFolder Root, IntPtr pidlSub)
		{
			IntPtr strr = Marshal.AllocCoTaskMem(MAX_PATH * 2 + 4);
			Marshal.WriteInt32(strr, 0, 0);
			StringBuilder buf = new StringBuilder(MAX_PATH);
			if (Root.GetDisplayNameOf(pidlSub, SHGDN.FORADDRESSBAR | SHGDN.FORPARSING, out strr) == S_OK)
				API.StrRetToBuf(strr, pidlSub, buf, MAX_PATH);
			Marshal.FreeCoTaskMem(strr);
			return buf.ToString();
		}


		public static string GetNameByIShell(IShellFolder Root, IntPtr pidlSub)
		{
			IntPtr strr = Marshal.AllocCoTaskMem(MAX_PATH * 2 + 4);
			Marshal.WriteInt32(strr, 0, 0);
			StringBuilder buf = new StringBuilder(MAX_PATH);
			if (Root.GetDisplayNameOf(pidlSub, SHGDN.INFOLDER, out strr) == S_OK)
				API.StrRetToBuf(strr, pidlSub, buf, MAX_PATH);
			Marshal.FreeCoTaskMem(strr);
			return buf.ToString();
		}


		public static string GetNameByPIDL(IntPtr pidl)
		{
			SHFILEINFO info = new SHFILEINFO();
			API.SHGetFileInfo(pidl, 0, ref info, Marshal.SizeOf(typeof(SHFILEINFO)),
				SHGFI.PIDL | SHGFI.DISPLAYNAME | SHGFI.TYPENAME);
			return info.szDisplayName;
		}


		public static string GetSpecialFolderPath(IntPtr hwnd, ShellSpecialFolders nFolder)
		{
			StringBuilder sb = new StringBuilder(MAX_PATH);
			API.SHGetSpecialFolderPath(hwnd, sb, nFolder, false);
			return sb.ToString();
		}


		public static IShellFolder GetShellFolder(IShellFolder desktop, string path, out IntPtr Pidl, bool getfolder = true)
		{
			IShellFolder IFolder;
			uint i, j = 0;
			desktop.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, path, out i, out Pidl, ref j);
			if (getfolder)
				desktop.BindToObject(Pidl, IntPtr.Zero, ref Guids.IID_IShellFolder, out IFolder);
			else
				IFolder = null;
			return IFolder;
		}


		public static IShellFolder GetShellFolder(IShellFolder desktop, string path)
		{
			IntPtr Pidl;
			return GetShellFolder(desktop, path, out Pidl);
		}
		public static IShellFolder GetParentFolder(string path)
		{
			IntPtr pidl = API.ILCreateFromPath(path);
			try
			{
				IShellFolder desktop = GetDesktopFolder(out _);
				Guid iid = Guids.IID_IShellFolder;
				IShellFolder folder;
				desktop.BindToObject(pidl, IntPtr.Zero, ref iid, out folder);
				return folder;
			}
			finally
			{
				if (pidl != IntPtr.Zero)
				{
					API.ILFree(pidl);
				}
			}
		}
	}
	public class API
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

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);
		//[DllImport("user32.dll", CharSet = CharSet.Auto)]
		//private static extern bool DestroyIcon(IntPtr handle);
		[DllImport("shell32.dll")]
		public static extern uint ExtractIconEx(
		  string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);
		#region API

		[DllImport("shell32.dll")]
		public static extern Int32 SHGetDesktopFolder(out IntPtr ppshf);

		[DllImport("Shlwapi.Dll", CharSet = CharSet.Auto)]
		public static extern Int32 StrRetToBuf(IntPtr pstr, IntPtr pidl, StringBuilder pszBuf, int cchBuf);

		[DllImport("shell32.dll")]
		public static extern int SHGetSpecialFolderLocation(IntPtr handle, CSIDL nFolder, out IntPtr ppidl);

		[DllImport("shell32", EntryPoint = "SHGetFileInfo", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SHGetFileInfo(
			IntPtr ppidl,
			FILE_ATTRIBUTE dwFileAttributes,
			ref SHFILEINFO sfi,
			int cbFileInfo,
			SHGFI uFlags);

		//[DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern IntPtr CreatePopupMenu();

		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		public static extern uint TrackPopupMenuEx(
			IntPtr hmenu,
			TPM flags,
			int x,
			int y,
			IntPtr hwnd,
			IntPtr lptpm);

		[DllImport("Shell32.Dll")]
		public static extern bool SHGetSpecialFolderPath(
			IntPtr hwndOwner,
			StringBuilder lpszPath,
			ShellSpecialFolders nFolder,
			bool fCreate);

		#endregion

	}
}

