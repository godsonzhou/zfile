
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Forms;
namespace WinShell
{

	public static class ContextMenuHandler
	{
		//static void Main1(string[] args)
		//{
		//	// 获取所有用户上下文菜单项
		//	IntPtr hwnd = IntPtr.Zero;
		//	Guid riid = new Guid("000214E5-0000-0000-C000-000000000046");
		//	int hr = API.SHGetAllUserShellContextMenus(out hwnd, ref riid);

		//	if (hr != 0)
		//	{
		//		Console.WriteLine("Failed to get context menus.");
		//		return;
		//	}

		//	// 遍历所有菜单项并调用
		//	IShellExtInit shellExt = null;
		//	foreach (string file in args) // 假设 args 是文件路径列表
		//	{
		//		try
		//		{
		//			shellExt = (IShellExtInit)hwnd;
		//			shellExt.Initialize(file);

		//			// 获取命令并执行
		//			string command = GetCommandFromRegistry(file);
		//			if (!string.IsNullOrEmpty(command))
		//			{
		//				Process.Start(command);
		//			}
		//		}
		//		catch (Exception ex)
		//		{
		//			Console.WriteLine($"Error: {ex.Message}");
		//		}
		//	}

		//	Marshal.ReleaseComObject(hwnd);
		//}
		static string GetCommandFromRegistry(string file)
		{
			// 从注册表中获取命令
			using (RegistryKey key = Registry.ClassesRoot.OpenSubKey($@"{file}\shell"))
			{
				if (key != null)
				{
					foreach (string subKeyName in key.GetSubKeyNames())
					{
						using (RegistryKey subKey = key.OpenSubKey(subKeyName))
						{
							object value = subKey.GetValue("command");
							if (value != null)
							{
								return value.ToString();
							}
						}
					}
				}

				// 如果未找到命令，尝试从 shellex 获取
				using (RegistryKey keyShellex = Registry.ClassesRoot.OpenSubKey($@"{file}\shellex"))
				{
					if (keyShellex != null)
					{
						foreach (string subKeyName in keyShellex.GetSubKeyNames())
						{
							using (RegistryKey subKeyShellex = keyShellex.OpenSubKey(subKeyName))
							{
								object value = subKeyShellex.GetValue("command");
								if (value != null)
								{
									return value.ToString();
								}
							}
						}
					}
				}

				return null;
			}
		}
		//this is my invokecommand implementation
		public static void InvokeCmd(IContextMenu iContextMenu, uint cmd, POINT MousePosition)
		{
			var invoke = new CMINVOKECOMMANDINFOEX();
			invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
			invoke.lpVerb = (IntPtr)(cmd - 1);
			Debug.Print(invoke.lpVerb.ToString());
			invoke.lpDirectory = string.Empty;
			invoke.fMask = 0;
			invoke.ptInvoke = MousePosition;// new POINT(MousePosition.X, MousePosition.Y);
			invoke.nShow = 1;
			iContextMenu.InvokeCommand(ref invoke);
		}
		//this is a complete invokecommand implementation version to fully support file / folder context menu operation
		public static void InvokeCommand(IContextMenu oContextMenu, uint nCmd, string strFolder, POINT pointInvoke)
		{
			CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
			invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));//cbInvokeCommand;
			invoke.lpVerb = (IntPtr)(nCmd - w32.CMD_FIRST);
			invoke.lpDirectory = strFolder;
			invoke.lpVerbW = (IntPtr)(nCmd - w32.CMD_FIRST);
			invoke.lpDirectoryW = strFolder;
			invoke.fMask = (uint)(CMIC.UNICODE | CMIC.PTINVOKE |
				((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
				((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0));
			invoke.ptInvoke = pointInvoke;// new POINT(pointInvoke.X, pointInvoke.Y);
			invoke.nShow = (int)SW.SHOWNORMAL;

			oContextMenu.InvokeCommand(ref invoke);
		}
		/// <summary>
		/// Invokes a specific command from an IContextMenu
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu containing the item</param>
		/// <param name="cmdA">the Ansi execute string to invoke</param>
		/// <param name="cmdW">the Unicode execute string to invoke</param>
		/// <param name="parentDir">the parent directory from where to invoke</param>
		/// <param name="ptInvoke">the point (in screen coördinates) from which to invoke</param>
		public static void InvokeCommand(IContextMenu iContextMenu, string cmd, string parentDir, POINT ptInvoke)
		{
			CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
			invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));// ShellAPI.cbInvokeCommand;
			invoke.lpVerb = Marshal.StringToHGlobalAnsi(cmd);
			invoke.lpDirectory = parentDir;
			invoke.lpVerbW = Marshal.StringToHGlobalUni(cmd);
			invoke.lpDirectoryW = parentDir;
			invoke.fMask = (uint)(CMIC.UNICODE | CMIC.PTINVOKE |
				((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
				((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0));
			invoke.ptInvoke = ptInvoke;// new POINT(ptInvoke.X, ptInvoke.Y);
			invoke.nShow = (int)SW.SHOWNORMAL;

			iContextMenu.InvokeCommand(ref invoke);
		}
		/// <summary>
		/// Invokes a specific command from an IContextMenu
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu containing the item</param>
		/// <param name="cmd">the execute string to invoke</param>
		/// <param name="parentDir">the parent node from where to invoke</param>
		/// <param name="ptInvoke">the point (in screen coцrdinates) from which to invoke</param>
		public static void InvokeCommand1(IContextMenu iContextMenu, string cmd, string parentDir, POINT ptInvoke)
		{
			CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
			invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
			invoke.lpVerb = Marshal.StringToHGlobalAnsi(cmd);
			invoke.lpDirectory = parentDir;
			invoke.lpVerbW = Marshal.StringToHGlobalUni(cmd);
			invoke.lpDirectoryW = parentDir;
			invoke.fMask = (uint)(CMIC.UNICODE | CMIC.PTINVOKE |
				((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
				((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0));
			invoke.ptInvoke = ptInvoke;// new POINT(ptInvoke);
			invoke.nShow = (int)SW.SHOWNORMAL;

			iContextMenu.InvokeCommand(ref invoke);
		}
		private static void InvokeCommand1(IContextMenu oContextMenu, uint nCmd, string strFolder, POINT pointInvoke)
		{
			var invoke = new CMINVOKECOMMANDINFOEX
			{
				cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX)),//CbInvokeCommand,
				lpVerb = (IntPtr)(nCmd - w32.CMD_FIRST),
				lpDirectory = strFolder,
				lpVerbW = (IntPtr)(nCmd - w32.CMD_FIRST),
				lpDirectoryW = strFolder,
				fMask = (uint)(CMIC.UNICODE | CMIC.PTINVOKE | ((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) | ((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0)),
				ptInvoke = pointInvoke,
				nShow = (int)SW.SHOWNORMAL
			};
			oContextMenu.InvokeCommand(ref invoke);
		}
		public static bool ExecuteVerb(IWin32Window owner, string verb, string parentName, IContextMenu contextMenu)
		{
			if (contextMenu == null)
			{
				return false;
			}
			CMINVOKECOMMANDINFOEX structure = new CMINVOKECOMMANDINFOEX();
			try
			{
				structure.cbSize = Marshal.SizeOf(structure);
				if (verb != null)
				{
					structure.lpVerb = Marshal.StringToHGlobalAnsi(verb);
					structure.lpVerbW = Marshal.StringToHGlobalUni(verb);
				}
				if (!string.IsNullOrEmpty(parentName))
				{
					structure.lpDirectory = parentName;
					structure.lpDirectoryW = parentName;
				}
				if (owner != null)
				{
					structure.hwnd = owner.Handle;
				}
				structure.fMask = (uint)((CMIC.UNICODE | (((Control.ModifierKeys & Keys.Control) > Keys.None) ? CMIC.CONTROL_DOWN : ((CMIC)0))) | (((Control.ModifierKeys & Keys.Shift) > Keys.None) ? CMIC.SHIFT_DOWN : ((CMIC)0)));
				structure.nShow = (int)SW.SHOWNORMAL;
				contextMenu.InvokeCommand(ref structure);
				Marshal.ReleaseComObject(contextMenu);
			}
			finally
			{
				Marshal.FreeHGlobal(structure.lpVerb);
				Marshal.FreeHGlobal(structure.lpVerbW);
			}
			return true;
		}
		/// <summary>
		/// Invokes a specific command from an IContextMenu
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu containing the item</param>
		/// <param name="cmd">the index of the command to invoke</param>
		/// <param name="parentDir">the parent directory from where to invoke</param>
		/// <param name="ptInvoke">the point (in screen coördinates) from which to invoke</param>
		public static void InvokeCommand2(IContextMenu iContextMenu, uint cmd, string parentDir, POINT ptInvoke)
		{
			CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
			invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));// NativeShellAPI.cbInvokeCommand;
			invoke.lpVerb = (IntPtr)cmd;
			invoke.lpDirectory = parentDir;
			invoke.lpVerbW = (IntPtr)cmd;
			invoke.lpDirectoryW = parentDir;
			invoke.fMask = (uint)(CMIC.UNICODE | CMIC.PTINVOKE |
				((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
				((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0));
			invoke.ptInvoke = ptInvoke;
			invoke.nShow = (int)SW.SHOWNORMAL;

			iContextMenu.InvokeCommand(ref invoke);
		}
		public static void InvokeComMethod(object comObject, string methodName, params object[] parameters)
		{
			Type comType = comObject.GetType();
			var method = comType.GetMethod(methodName);
			if (method != null)
			{
				method.Invoke(comObject, parameters);
			}
		}

		public static object CreateComObject(Guid clsid)
		{
			Type comType = Type.GetTypeFromCLSID(clsid);
			if (comType != null)
			{
				return Activator.CreateInstance(comType);
			}
			return null;
		}

		public static Guid? GetContextMenuHandlerGuid(string registryPath)
		{
			using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryPath))
			{
				if (key != null)
				{
					string guidString = key.GetValue(null) as string;//todo: bugfix null->openwithsublimetext
					if (Guid.TryParse(guidString, out Guid guid))
					{
						return guid;
					}
				}
			}
			return null;
		}
	}
	public static class w32
	{
		public const int MAX_PATH = 260;
		public const int S_OK = 0;
		public const int S_FALSE = 1;
		public const uint CMD_FIRST = 1;
		public const uint CMD_LAST = 30000;
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
			if (Root.GetDisplayNameOf(pidlSub, SHGDN.FORADDRESSBAR | SHGDN.FORPARSING, strr) == S_OK)
				API.StrRetToBuf(strr, pidlSub, buf, MAX_PATH);
			Marshal.FreeCoTaskMem(strr);
			return buf.ToString();
		}
		public static string GetNameByIShell(IShellFolder Root, IntPtr pidlSub)
		{
			IntPtr strr = Marshal.AllocCoTaskMem(MAX_PATH * 2 + 4);
			Marshal.WriteInt32(strr, 0, 0);
			StringBuilder buf = new StringBuilder(MAX_PATH);
			if (Root.GetDisplayNameOf(pidlSub, SHGDN.INFOLDER, strr) == S_OK)
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
		public static extern int SHGetAllUserShellContextMenus(out IntPtr hwnd, ref Guid riid);

	
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
		public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);
		//[DllImport("shell32.dll")]
		//public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);
		#region API

		[DllImport("shell32.dll")]
		public static extern Int32 SHGetDesktopFolder(out IntPtr ppshf);

		[DllImport("Shlwapi.Dll", CharSet = CharSet.Auto)]
		public static extern Int32 StrRetToBuf(IntPtr pstr, IntPtr pidl, StringBuilder pszBuf, int cchBuf);

		[DllImport("shell32.dll")]
		public static extern int SHGetSpecialFolderLocation(IntPtr handle, CSIDL nFolder, out IntPtr ppidl);

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

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
		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		public static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, int flags, IntPtr token);
	}
}
