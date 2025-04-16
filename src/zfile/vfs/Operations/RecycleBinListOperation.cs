using System.Runtime.InteropServices;
using Zfile;
using WinShell;
namespace FileSystemOperations
{
    public class RecycleBinListOperation : FileSystemListOperation
    {
        private const string SID_DISPLACED = "{9B174B33-40FF-11d2-A27E-00C04FC30871}";
        private static readonly SHCOLUMNID SCID_OriginalLocation = new SHCOLUMNID 
        { 
            fmtid = new Guid(SID_DISPLACED), 
            pid = (uint)PID_DISPLACED.PID_DISPLACED_FROM 
        };
        private static readonly SHCOLUMNID SCID_DateDeleted = new SHCOLUMNID 
        { 
            fmtid = new Guid(SID_DISPLACED), 
            pid = (uint)PID_DISPLACED.PID_DISPLACED_DATE 
        };

        public RecycleBinListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries();
        }

        public override void MainExecute()
        {
            Files.Clear();
            try
            {
                IShellFolder desktopFolder;
                OleCheck(SHGetDesktopFolder(out desktopFolder));

                IntPtr trashPIDL;
                OleCheck(SHGetFolderLocation(IntPtr.Zero, CSIDL.BITBUCKET, 
                    IntPtr.Zero, 0, out trashPIDL));

                try
                {
                    IShellFolder2 folder;
                    Guid iid = typeof(IShellFolder2).GUID;
                    OleCheck(desktopFolder.BindToObject(trashPIDL, IntPtr.Zero, 
                        ref iid, out folder));

                    IEnumIDList enumIDList;
                    OleCheck(folder.EnumObjects(IntPtr.Zero, 
                        SHCONTF.FOLDERS | SHCONTF.NONFOLDERS | 
                        SHCONTF.INCLUDEHIDDEN, out enumIDList));

                    IntPtr pidl;
                    uint numIDs;
                    while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                    {
                        try
                        {
                            CheckOperationState();

                            var file = RecycleBinFileSource.CreateFile(Path);
                            file.FullPath = GetDisplayName(folder, pidl, SHGDN.NORMAL);
                            file.Link.LinkTo = GetDisplayName(folder, pidl, SHGDN.FORPARSING);

                            FileAttributeData attr;
                            if (FileSystemUtil.FileGetAttr(file.Link.LinkTo, out attr))
                            {
                                file.Size = attr.Size;
                                file.Attributes = attr.Attributes;
                                file.CreationTime = DateTime.FromFileTime(attr.PlatformTime);
                                file.LastAccessTime = DateTime.FromFileTime(attr.LastAccessTime);
                                file.ModificationTime = DateTime.FromFileTime(attr.LastWriteTime);
                                file.Comment.Value = GetDetails(folder, pidl, SCID_OriginalLocation);
                                file.ChangeTime = DateTime.FromOADate(
                                    Convert.ToDouble(GetDetails(folder, pidl, SCID_DateDeleted)));
                            }

                            Files.Add(file);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pidl);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(trashPIDL);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private string GetDisplayName(IShellFolder2 folder, IntPtr pidl, SHGDN flags)
        {
            IntPtr pszName;
            folder.GetDisplayNameOf(pidl, (uint)flags, out pszName);
            try
            {
                return Marshal.PtrToStringAuto(pszName);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pszName);
            }
        }

        private string GetDetails(IShellFolder2 folder, IntPtr pidl, SHCOLUMNID columnID)
        {
            object value;
            folder.GetDetailsEx(pidl, ref columnID, out value);
            return value?.ToString() ?? string.Empty;
        }

        private void OleCheck(int hr)
        {
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        private void ShowError(string message)
        {
            // 实现错误显示逻辑
        }
    }

    //[ComImport]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //[Guid("000214E6-0000-0000-C000-000000000046")]
    //public interface IShellFolder
    //{
    //    void ParseDisplayName(IntPtr hwnd, IntPtr pbc, string pszDisplayName, 
    //        ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
    //    void EnumObjects(IntPtr hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
    //    void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, 
    //        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    //    void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, 
    //        out IntPtr ppv);
    //    void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
    //    void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, 
    //        out IntPtr ppv);
    //    void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, 
    //        ref uint rgfInOut);
    //    void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl, 
    //        [In] ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
    //    void GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr ppszName);
    //    void SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, uint uFlags, 
    //        out IntPtr ppidlOut);
    //}

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("93F2F68C-1D1B-11D3-A30E-00C04F79ABD1")]
    public interface IShellFolder2 : IShellFolder
    {
        new void ParseDisplayName(IntPtr hwnd, IntPtr pbc, string pszDisplayName, 
            ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
        new void EnumObjects(IntPtr hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
        new void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, 
            [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        new void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, 
            out IntPtr ppv);
        new void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        new void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, 
            out IntPtr ppv);
        new void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, 
            ref uint rgfInOut);
        new void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl, 
            [In] ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
        new void GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr ppszName);
        new void SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, uint uFlags, 
            out IntPtr ppidlOut);
        void GetDefaultSearchGUID(out Guid pguid);
        void EnumSearches(out IntPtr ppenum);
        void GetDefaultColumn(uint dwRes, out uint pSort, out uint pDisplay);
        void GetDefaultColumnState(uint iColumn, out uint pcsFlags);
        void GetDetailsEx(IntPtr pidl, ref SHCOLUMNID pscid, out object pv);
        void GetDetailsOf(IntPtr pidl, uint iColumn, out IntPtr psd);
        void MapColumnToSCID(uint iColumn, out SHCOLUMNID pscid);
    }

    //[ComImport]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //[Guid("000214F2-0000-0000-C000-000000000046")]
    //public interface IEnumIDList
    //{
    //    [PreserveSig]
    //    int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
    //    void Skip(uint celt);
    //    void Reset();
    //    void Clone(out IEnumIDList ppenum);
    //}

    [StructLayout(LayoutKind.Sequential)]
    public struct SHCOLUMNID
    {
        public Guid fmtid;
        public uint pid;
    }

    public enum PID_DISPLACED
    {
        PID_DISPLACED_FROM = 2,
        PID_DISPLACED_DATE = 3
    }

    //public enum SHCONTF
    //{
    //    FOLDERS = 0x20,
    //    NONFOLDERS = 0x40,
    //    INCLUDEHIDDEN = 0x80
    //}

    //public enum SHGDN
    //{
    //    NORMAL = 0,
    //    FORPARSING = 0x8000
    //}

    //public enum CSIDL
    //{
    //    BITBUCKET = 0x0a
    //}

 
} 