using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinShell
{
	[ComImport]
	[Guid("000214E5-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellExtInit
	{
		void Initialize([MarshalAs(UnmanagedType.LPStr)] string pszFile);
	}
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E6-0000-0000-C000-000000000046")]
	public interface IShellFolder
	{
		void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		int EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IntPtr ppenumIDList);
		void BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IShellFolder ppv);
		void BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
		[PreserveSig]
		int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
		void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref SFGAO rgfInOut);
		IntPtr GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref Guid riid, out IntPtr ppv);
		int GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, IntPtr pName);      //out STRRET pName);  //bugfix: remove 'out' prefix of pName parameter
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
	

}
