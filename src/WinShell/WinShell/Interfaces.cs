using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WinShell
{
	// IBindCtx接口定义
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("0000000E-0000-0000-C000-000000000046")]
	public interface IBindCtx
	{
		[PreserveSig]
		int GetBindOptions(ref BIND_OPTS3 pbindopts);

		[PreserveSig]
		int SetBindOptions([In] ref BIND_OPTS3 pbindopts);

		[PreserveSig]
		int GetRunningObjectTable(out IRunningObjectTable pprot);

		[PreserveSig]
		int RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		[PreserveSig]
		int RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);

		[PreserveSig]
		int ReleaseBoundObjects();

		[PreserveSig]
		int SetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] object punk);

		[PreserveSig]
		int GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] out object punk);

		[PreserveSig]
		int EnumObjectParam(out IEnumString ppenum);

		[PreserveSig]
		int RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey);
	}
	[ComImport]
	[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellItemArray
	{
		[PreserveSig]
		int BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid rbhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppvOut);

		[PreserveSig]
		int GetPropertyStore(int Flags, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetPropertyDescriptionList([MarshalAs(UnmanagedType.LPStruct)] PropertyKey keyType, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetAttributes(int dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);

		[PreserveSig]
		int GetCount(out uint pdwNumItems);

		[PreserveSig]
		int GetItemAt(uint dwIndex, out IShellItem ppsi);

		[PreserveSig]
		int EnumItems(out IntPtr ppenumShellItems);
	}

	[ComImport]
	[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellItem
	{
		[PreserveSig]
		void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
		[PreserveSig]
		int GetAttributes(SFGAO sfgaoMask, out SFGAO psfgaoAttribs);

		[PreserveSig]
		int Compare(IShellItem psi, SICHINTF hint, out int piOrder);
		[PreserveSig]
		int BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetParent(out IShellItem ppsi);
	}
	[ComImportAttribute()]
	[GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IImageList
	{
		[PreserveSig]
		int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

		[PreserveSig]
		int ReplaceIcon(int i, IntPtr hicon, ref int pi);

		[PreserveSig]
		int SetOverlayImage(int iImage, int iOverlay);

		[PreserveSig]
		int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

		[PreserveSig]
		int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

		[PreserveSig]
		int Draw(ref IMAGELISTDRAWPARAMS pimldp);

		[PreserveSig]
		int Remove(int i);

		[PreserveSig]
		int GetIcon(int i, int flags, ref IntPtr picon);

		[PreserveSig]
		int GetImageInfo(int i, ref IMAGEINFO pImageInfo);

		[PreserveSig]
		int Copy(int iDst, IImageList punkSrc, int iSrc, int uFlags);

		[PreserveSig]
		int Merge(int i1, IImageList punk2, int i2, int dx, int dy, ref Guid riid, ref IntPtr ppv);

		[PreserveSig]
		int Clone(ref Guid riid, ref IntPtr ppv);

		[PreserveSig]
		int GetImageRect(int i, ref Rectangle prc);

		[PreserveSig]
		int GetIconSize(ref int cx, ref int cy);

		[PreserveSig]
		int SetIconSize(int cx, int cy);

		[PreserveSig]
		int GetImageCount(ref int pi);

		[PreserveSig]
		int SetImageCount(int uNewCount);

		[PreserveSig]
		int SetBkColor(int clrBk, ref int pclr);

		[PreserveSig]
		int GetBkColor(ref int pclr);

		[PreserveSig]
		int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);

		[PreserveSig]
		int EndDrag();

		[PreserveSig]
		int DragEnter(IntPtr hwndLock, int x, int y);

		[PreserveSig]
		int DragLeave(IntPtr hwndLock);

		[PreserveSig]
		int DragMove(int x, int y);

		[PreserveSig]
		int SetDragCursorImage(ref IImageList punk, int iDrag, int dxHotspot, int dyHotspot);

		[PreserveSig]
		int DragShowNolock(int fShow);

		[PreserveSig]
		int GetDragImage(ref Point ppt, ref Point pptHotspot, ref Guid riid, ref IntPtr ppv);

		[PreserveSig]
		int GetItemFlags(int i, ref int dwFlags);

		[PreserveSig]
		int GetOverlayImage(int iOverlay, ref int piIndex);
	}

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
		int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		int EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IntPtr ppenumIDList);
		int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IShellFolder ppv);
		void BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
		[PreserveSig]
		int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
		void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref SFGAO rgfInOut);
		IntPtr GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref Guid riid, out IntPtr ppv);
		int GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, IntPtr pName);      //out STRRET pName);  //bugfix: remove 'out' prefix of pName parameter
		void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, SHCONTF uFlags, out IntPtr ppidlOut);
	}
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("93F2F68C-1D1B-11D3-A30E-00C04F79ABD1")]
	public interface IShellFolder2 : IShellFolder
	{
		void ParseDisplayName(IntPtr hwnd, IntPtr pbc, string pszDisplayName,
			ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		int EnumObjects(IntPtr hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
		int BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid,
			[MarshalAs(UnmanagedType.IUnknown)] out IShellFolder2 ppv);
		new void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid,
			out IntPtr ppv);
		new void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		new void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid,
			out IntPtr ppv);
		int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl,
			ref uint rgfInOut);
		void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl,
			[In] ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
		void GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr ppszName);
		void SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, uint uFlags,
			out IntPtr ppidlOut);
		void GetDefaultSearchGUID(out Guid pguid);
		void EnumSearches(out IntPtr ppenum);
		void GetDefaultColumn(uint dwRes, out uint pSort, out uint pDisplay);
		void GetDefaultColumnState(uint iColumn, out uint pcsFlags);
		void GetDetailsEx(IntPtr pidl, ref SHCOLUMNID pscid, out object pv);
		void GetDetailsOf(IntPtr pidl, uint iColumn, out IntPtr psd);
		void MapColumnToSCID(uint iColumn, out SHCOLUMNID pscid);
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

	[ComImport, Guid("000214FA-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IExtractIconW
	{
		void GetIconLocation(uint uFlags, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szIconFile,
			int cchMax, out int piIndex, out ExtractIconFlags pwFlags);

		void Extract([MarshalAs(UnmanagedType.LPWStr)] string pszFile, uint nIconIndex,
			out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);
	}
	[ComImport]
	[Guid("000214EB-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IExtractIcon
	{
		[PreserveSig]
		int GetIconLocation(uint uFlags, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 260)] StringBuilder szIconFile, int cchMax, out int piIndex, out uint pwFlags);
		[PreserveSig]
		int Extract([MarshalAs(UnmanagedType.LPWStr)] string pszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);
	}

	[Flags]
	public enum ExtractIconFlags : uint
	{
		GIL_OPENICON = 0x0001,
		GIL_FORSHELL = 0x0002
	}

}
