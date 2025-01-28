using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinShell
{

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E6-0000-0000-C000-000000000046")]
	public interface IShellFolder
	{
		void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		int EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IntPtr ppenumIDList);//out IEnumIDList ppenumIDList);
		void BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IShellFolder ppv);
		void BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
		[PreserveSig]
		int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
		void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref SFGAO rgfInOut);
		IntPtr GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
		int GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out IntPtr pName);      //out STRRET pName);
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
	//[ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), GuidAttribute("000214e4-0000-0000-c000-000000000046")]
	//public interface IContextMenu
	//{
	//	[PreserveSig()]
	//	Int32 QueryContextMenu(
	//		IntPtr hmenu,
	//		uint iMenu,
	//		uint idCmdFirst,
	//		uint idCmdLast,
	//		CMF uFlags);

	//	[PreserveSig()]
	//	Int32 InvokeCommand(
	//		ref CMINVOKECOMMANDINFOEX info);

	//	[PreserveSig()]
	//	void GetCommandString(
	//		int idcmd,
	//		GetCommandStringInformations uflags,
	//		int reserved,
	//		StringBuilder commandstring,
	//		int cch);
	//}

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F2-0000-0000-C000-000000000046")]
	public interface IEnumIDList
	{
		[PreserveSig]
		int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
		void Skip(uint celt);
		void Reset();
		void Clone(out IEnumIDList ppenum);
	}
	//[ComImport(),
	//Guid("000214F2-0000-0000-C000-000000000046"),
	//InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//public interface IEnumIDList
	//{
	//	[PreserveSig()]
	//	uint Next(
	//		uint celt,
	//		out IntPtr rgelt,
	//		out int pceltFetched);

	//	void Skip(
	//		uint celt);

	//	void Reset();

	//	IEnumIDList Clone();
	//}
	/// <summary>
	/// IShellFolder 接口用于操作和管理 Shell 文件夹对象。
	/// </summary>
	//[ComImport]
	//[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//[Guid("000214E6-0000-0000-C000-000000000046")]
	//public interface IShellFolder
	//{
	//	/// <summary>
	//	/// 将显示名称解析为项标识符列表。
	//	/// </summary>
	//	/// <param name="hwnd">父窗口句柄。</param>
	//	/// <param name="pbc">绑定上下文。</param>
	//	/// <param name="pszDisplayName">要解析的显示名称。</param>
	//	/// <param name="pchEaten">接收解析的字符数。</param>
	//	/// <param name="ppidl">接收项标识符列表。</param>
	//	/// <param name="pdwAttributes">接收项的属性。</param>
	//	void ParseDisplayName(
	//		IntPtr hwnd,
	//		IntPtr pbc,
	//		[MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
	//		out uint pchEaten,
	//		out IntPtr ppidl,
	//		ref uint pdwAttributes);

	//	/// <summary>
	//	/// 枚举文件夹中的项。
	//	/// </summary>
	//	/// <param name="hWnd">父窗口句柄。</param>
	//	/// <param name="flags">枚举选项。</param>
	//	/// <param name="enumIDList">接收枚举器对象。</param>
	//	/// <returns>操作结果代码。</returns>
	//	[PreserveSig]
	//	int EnumObjects(IntPtr hWnd, SHCONTF flags, out IntPtr enumIDList);

	//	/// <summary>
	//	/// 绑定到子文件夹。
	//	/// </summary>
	//	/// <param name="pidl">子文件夹的项标识符列表。</param>
	//	/// <param name="pbc">绑定上下文。</param>
	//	/// <param name="riid">接口标识符。</param>
	//	/// <param name="ppv">接收子文件夹接口。</param>
	//	void BindToObject(
	//		IntPtr pidl,
	//		IntPtr pbc,
	//		[In()] ref Guid riid,
	//		out IShellFolder ppv);

	//	/// <summary>
	//	/// 绑定到存储对象。
	//	/// </summary>
	//	/// <param name="pidl">存储对象的项标识符列表。</param>
	//	/// <param name="pbc">绑定上下文。</param>
	//	/// <param name="riid">接口标识符。</param>
	//	/// <param name="ppv">接收存储对象接口。</param>
	//	void BindToStorage(
	//		IntPtr pidl,
	//		IntPtr pbc,
	//		[In()] ref Guid riid,
	//		[MarshalAs(UnmanagedType.Interface)] out object ppv);

	//	/// <summary>
	//	/// 比较两个项标识符列表。
	//	/// </summary>
	//	/// <param name="lParam">比较参数。</param>
	//	/// <param name="pidl1">第一个项标识符列表。</param>
	//	/// <param name="pidl2">第二个项标识符列表。</param>
	//	/// <returns>比较结果代码。</returns>
	//	[PreserveSig()]
	//	uint CompareIDs(
	//		int lParam,
	//		IntPtr pidl1,
	//		IntPtr pidl2);

	//	/// <summary>
	//	/// 创建视图对象。
	//	/// </summary>
	//	/// <param name="hwndOwner">所有者窗口句柄。</param>
	//	/// <param name="riid">接口标识符。</param>
	//	/// <param name="ppv">接收视图对象接口。</param>
	//	void CreateViewObject(
	//		IntPtr hwndOwner,
	//		[In()] ref Guid riid,
	//		[MarshalAs(UnmanagedType.Interface)] out object ppv);

	//	/// <summary>
	//	/// 获取项的属性。
	//	/// </summary>
	//	/// <param name="cidl">项的数量。</param>
	//	/// <param name="apidl">项标识符列表数组。</param>
	//	/// <param name="rgfInOut">接收项的属性。</param>
	//	void GetAttributesOf(
	//		uint cidl,
	//		[In(), MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
	//		ref SFGAO rgfInOut);

	//	/// <summary>
	//	/// 获取用户界面对象。
	//	/// </summary>
	//	/// <param name="hwndOwner">所有者窗口句柄。</param>
	//	/// <param name="cidl">项的数量。</param>
	//	/// <param name="apidl">项标识符列表数组。</param>
	//	/// <param name="riid">接口标识符。</param>
	//	/// <param name="rgfReserved">保留参数。</param>
	//	/// <returns>接收用户界面对象接口。</returns>
	//	IntPtr GetUIObjectOf(
	//		IntPtr hwndOwner,
	//		uint cidl,
	//		[MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
	//		[In()] ref Guid riid,
	//		out IntPtr rgfReserved);

	//	/// <summary>
	//	/// 获取项的显示名称。
	//	/// </summary>
	//	/// <param name="pidl">项标识符列表。</param>
	//	/// <param name="uFlags">显示名称选项。</param>
	//	/// <param name="lpName">接收显示名称。</param>
	//void GetDisplayNameOf(
	//	IntPtr pidl,
	//	SHGDN uFlags,
	//	IntPtr lpName);

	//	/// <summary>
	//	/// 设置项的名称。
	//	/// </summary>
	//	/// <param name="hwnd">父窗口句柄。</param>
	//	/// <param name="pidl">项标识符列表。</param>
	//	/// <param name="pszName">新名称。</param>
	//	/// <param name="uFlags">显示名称选项。</param>
	//	/// <returns>接收新的项标识符列表。</returns>
	//	IntPtr SetNameOf(
	//		IntPtr hwnd,
	//		IntPtr pidl,
	//		[MarshalAs(UnmanagedType.LPWStr)] string pszName,
	//		SHGNO uFlags);
	//}

}
