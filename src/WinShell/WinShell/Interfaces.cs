using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace WinShell
{
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

	[ComImport, Guid("000214FA-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IExtractIconW
	{
		void GetIconLocation(uint uFlags, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szIconFile,
			int cchMax, out int piIndex, out ExtractIconFlags pwFlags);

		void Extract([MarshalAs(UnmanagedType.LPWStr)] string pszFile, uint nIconIndex,
			out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);
	}

	[Flags]
	public enum ExtractIconFlags : uint
	{
		GIL_OPENICON = 0x0001,
		GIL_FORSHELL = 0x0002
	}

}
