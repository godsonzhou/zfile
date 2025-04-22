using System.Runtime.InteropServices;
using System.Text;
using WinShell;
namespace zfile
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

        protected override void MainExecute()
        {
            Files.Clear();
            try
            {
                IShellFolder desktopFolder;
                w32.OleCheck(API.SHGetDesktopFolder(out desktopFolder));

                IntPtr trashPIDL;
                w32.OleCheck(API.SHGetFolderLocation(IntPtr.Zero, CSIDL.BITBUCKET,
                    IntPtr.Zero, 0, out trashPIDL));

                try
                {
                    // Get the IShellFolder interface for the Recycle Bin
                    IShellFolder tempFolder;
                    Guid iid = Guids.IID_IShellFolder;
                    desktopFolder.BindToObject(trashPIDL, IntPtr.Zero, ref iid, out tempFolder);
                    // Cast to IShellFolder2
                    IShellFolder2 folder = (IShellFolder2)tempFolder;

                    IEnumIDList enumIDList;
                    w32.OleCheck(folder.EnumObjects(IntPtr.Zero,
                        (uint)(SHCONTF.FOLDERS | SHCONTF.NONFOLDERS |
                        SHCONTF.INCLUDEHIDDEN), out enumIDList));

                    IntPtr pidl;
                    uint numIDs;
                    while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                    {
                        try
                        {
                            CheckOperationState();

							StringBuilder pszPath = new();
							API.SHGetPathFromIDList(pidl, pszPath);
							var file = RecycleBinFileSource.CreateFile(Path+ pszPath.ToString());
							file.Name = w32.GetNameByIShell(folder, pidl);//删除前的名称
							//file.FullPath = w32.GetDisplayName2(folder, pidl, SHGDN.NORMAL); //删除前的路径
							//file.FullPath = w32.GetPathByIShell(folder, pidl); //删除前的路径
							file.FullPath = w32.GetNameByIShell(folder, pidl); //删除前的名称
							file.LinkProperty.LinkTarget = w32.GetPathByIShell(folder, pidl);// w32.GetDisplayName2(folder, pidl, SHGDN.FORPARSING);

                            FileAttributeData attr;
                            if (FileSystemUtil.FileGetAttr(file.LinkProperty.LinkTarget, out attr))
                            {
                                file.Size = attr.Size;
                                file.Attributes = attr.Attr;
                                file.CreationTime = DateTime.FromFileTime(attr.CreationTime);
                                file.LastAccessTime = DateTime.FromFileTime(attr.LastAccessTime);
                                file.ModificationTime = DateTime.FromFileTime(attr.LastWriteTime);
                                file.CommentProperty.Value = w32.GetDetails(folder, pidl, SCID_OriginalLocation);
                                //file.ChangeTime = DateTime.FromOADate(
                                    //Convert.ToDouble(w32.GetDetails(folder, pidl, SCID_DateDeleted)));
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