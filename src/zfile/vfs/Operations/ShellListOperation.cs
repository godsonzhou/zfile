using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public static class SCIDHelper
    {
        private const string SID_SYSTEM = "{B725F130-47EF-101A-A5F1-02608C9EEBAC}";
        private const string SID_COMPUTER = "{9B174B35-40FF-11D2-A27E-00C04FC30871}";

        public static readonly SHCOLUMNID Capacity = new WinShell.SHCOLUMNID { fmtid = new Guid(SID_COMPUTER), pid = 3 };
        public static readonly SHCOLUMNID FileSize = new WinShell.SHCOLUMNID { fmtid = new Guid(SID_SYSTEM), pid = 0x0000000B };
        public static readonly SHCOLUMNID DateCreated = new WinShell.SHCOLUMNID { fmtid = new Guid(SID_SYSTEM), pid = 0x0000000D };
        public static readonly SHCOLUMNID DateModified = new WinShell.SHCOLUMNID { fmtid = new Guid(SID_SYSTEM), pid = 0x0000000E };
    }
    public class ShellListOperation : FileSourceListOperation
    {
        private readonly IShellFileSource shellFileSource;

        public ShellListOperation(IFileSource fileSource, string path) : base(fileSource, path)
        {
            shellFileSource = fileSource as IShellFileSource ?? throw new ArgumentException("fileSource must be an IShellFileSource");
            Files = new FileEntries();
        }

        protected override void MainExecute()
        {
            Files.Clear();
            try
            {
                if (shellFileSource.IsPathAtRoot(Path))
                {
                    ListDrives();
                }
                else
                {
                    ListDirectory();
                }
            }
            catch
            {
                // Log the error
                throw;
            }
        }

        private void ListFolder(IShellFolder2 folder, uint grfFlags)
        {
            const uint SFGAOF_DEFAULT = (uint)(SFGAO.STORAGE | SFGAO.HIDDEN | SFGAO.FOLDER);
            w32.OleCheck(API.SHGetIDListFromObject(folder, out IntPtr parent));
            try
            {
                w32.OleCheck(folder.EnumObjects(0, grfFlags, out IEnumIDList enumIDList));

                while (enumIDList.Next(1, out IntPtr pidl, out _) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = w32.GetDisplayName2(folder, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = API.ILCombine(parent, pidl);
                        file.LinkProperty.LinkTarget = w32.GetDisplayName(folder, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

                        uint attributes = SFGAOF_DEFAULT;
                        if (folder.GetAttributesOf(1, new IntPtr[] { pidl }, ref attributes) == 0)
                        {
                            if ((attributes & (uint)SFGAO.STORAGE) != 0)
                            {
                                file.Attributes = FileAttributes.Device;
                            }
                            if ((attributes & (uint)SFGAO.FOLDER) != 0)
                            {
                                file.Attributes |= FileAttributes.Directory;
                            }
                            if ((attributes & (uint)SFGAO.HIDDEN) != 0)
                            {
                                file.Attributes |= FileAttributes.Hidden;
                            }
                        }

                        object value = w32.GetDetails(folder, pidl, SCIDHelper.FileSize);
                        if (value is long longValue)
                        {
                            file.Size = longValue;
                        }
                        else if (file.IsDirectory)
                        {
                            file.Size = 0;
                        }
                        else
                        {
                            file.SizeProperty.IsValid = false;
                        }

                        value = w32.GetDetails(folder, pidl, SCIDHelper.DateModified);
                        if (value != null)
                        {
                            file.ModificationTime = (DateTime)value;
                        }
                        else
                        {
                            file.ModificationTimeProperty.IsValid = false;
                        }

                        value = w32.GetDetails(folder, pidl, SCIDHelper.DateCreated);
                        if (value != null)
                        {
                            file.CreationTime = (DateTime)value;
                        }
                        else
                        {
                            file.CreationTimeProperty.IsValid = false;
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
                Marshal.FreeCoTaskMem(parent);
            }
        }

        private void ListDrives()
        {
            const uint SFGAOF_DEFAULT = (uint)(SFGAO.FILESYSTEM | SFGAO.FOLDER);
            w32.OleCheck(API.SHGetDesktopFolder(out IShellFolder desktopFolder));
            w32.OleCheck(API.SHGetFolderLocation(0, CSIDL.DRIVES, 0, 0, out IntPtr drivesPidl));
            try
            {
                // Get the IShellFolder interface first
                Guid iid = typeof(IShellFolder).GUID;
                desktopFolder.BindToObject(drivesPidl, IntPtr.Zero, ref iid, out IShellFolder folder);

                // Check if it supports IShellFolder2
                if (folder is not IShellFolder2 shellFolder2)
                {
                    throw new InvalidOperationException("Failed to get IShellFolder2 interface");
                }

                // Use the IShellFolder2 interface
                w32.OleCheck(shellFolder2.EnumObjects(0, (uint)(SHCONTF.FOLDERS | SHCONTF.STORAGE), out IEnumIDList enumIDList));

                while (enumIDList.Next(1, out IntPtr pidl, out _) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = w32.GetDisplayName2(shellFolder2, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = API.ILCombine(drivesPidl, pidl);
                        file.LinkProperty.LinkTarget = w32.GetDisplayName(shellFolder2, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

                        uint attributes = SFGAOF_DEFAULT;
                        file.Attributes = FileAttributes.Device;

                        if (shellFolder2.GetAttributesOf(1, new IntPtr[] { pidl }, ref attributes) == 0)
                        {
                            if ((attributes & (uint)SFGAO.FILESYSTEM) != 0)
                            {
                                file.Attributes |= FileAttributes.Normal;
                            }
                            else if ((attributes & (uint)SFGAO.FOLDER) != 0)
                            {
                                file.Attributes |= FileAttributes.Directory;
                            }
                        }

                        file.ModificationTimeProperty.IsValid = false;

                        object value = w32.GetDetails(shellFolder2, pidl, SCIDHelper.Capacity);
                        if (value is long longValue)
                        {
                            file.Size = longValue;
                        }
                        else if (file.IsDirectory)
                        {
                            file.Size = 0;
                        }
                        else
                        {
                            file.SizeProperty.IsValid = false;
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
                Marshal.FreeCoTaskMem(drivesPidl);
            }
        }

        private void ListDirectory()
        {
            if (shellFileSource.FindFolder(Path.TrimEnd('\\'), out IShellFolder2 folder) == 0)
            {
                ListFolder(folder, (uint)(SHCONTF.FOLDERS | SHCONTF.NONFOLDERS | SHCONTF.INCLUDEHIDDEN));
            }
        }
    }
}