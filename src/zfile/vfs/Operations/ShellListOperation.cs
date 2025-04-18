using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
	public enum SCID
	{
		Capacity = 0x0000000C,
		FileSize = 0x0000000B,
		DateCreated = 0x0000000D,
		DateModified = 0x0000000E
	}
    public class ShellListOperation : FileSourceListOperation
    {
        private readonly IShellFileSource shellFileSource;

        public ShellListOperation(IFileSource fileSource, string path) : base(fileSource, path)
        {
            shellFileSource = fileSource as IShellFileSource;
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
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        private void ListFolder(IShellFolder2 folder, uint grfFlags)
        {
            const uint SFGAOF_DEFAULT = (uint)(SFGAO.STORAGE | SFGAO.HIDDEN | SFGAO.FOLDER);
            IntPtr parent;
            w32.OleCheck(API.SHGetIDListFromObject(folder, out parent));
            try
            {
                IEnumIDList enumIDList;
                w32.OleCheck(folder.EnumObjects(0, grfFlags, out enumIDList));

                IntPtr pidl;
                uint numIDs;
                while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = w32.GetDisplayName2(folder, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = API.ILCombine(parent, pidl);
                        file.LinkProperty.LinkTarget = w32.GetDisplayName(folder, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

                        uint attributes = SFGAOF_DEFAULT;
                        if (folder.GetAttributesOf(1, new[] { pidl }, ref attributes) == 0)
                        {
                            if ((attributes & (uint)SFGAO.STORAGE) != 0)
                            {
                                file.Attributes = FileAttributes.Device | FileAttributes.Virtual;
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

                        object value = w32.GetDetails(folder, pidl, SCID.FileSize);
                        if (value is long)
                        {
                            file.Size = (long)value;
                        }
                        else if (file.IsDirectory)
                        {
                            file.Size = 0;
                        }
                        else
                        {
                            file.SizeProperty.IsValid = false;
                        }

                        value = w32.GetDetails(folder, pidl, SCID.DateModified);
                        if (value != null)
                        {
                            file.ModificationTime = (DateTime)value;
                        }
                        else
                        {
                            file.ModificationTimeProperty.IsValid = false;
                        }

                        value = w32.GetDetails(folder, pidl, SCID.DateCreated);
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
            IShellFolder desktopFolder;
            w32.OleCheck(API.SHGetDesktopFolder(out desktopFolder));
            IntPtr drivesPidl;
            w32.OleCheck(API.SHGetFolderLocation(0, CSIDL.DRIVES, 0, 0, out drivesPidl));
            try
            {
                IShellFolder2 folder;
                w32.OleCheck(desktopFolder.BindToObject(drivesPidl, IntPtr.Zero, typeof(IShellFolder2).GUID, out folder));

                IEnumIDList enumIDList;
                w32.OleCheck(folder.EnumObjects(0, (uint)(SHCONTF.FOLDERS | SHCONTF.STORAGE), out enumIDList));

                IntPtr pidl;
                uint numIDs;
                while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = w32.GetDisplayName2(folder, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = API.ILCombine(drivesPidl, pidl);
                        file.LinkProperty.LinkTarget = w32.GetDisplayName(folder, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

                        uint attributes = SFGAOF_DEFAULT;
                        file.Attributes = FileAttributes.Device | FileAttributes.Virtual;

                        if (folder.GetAttributesOf(1, new[] { pidl }, ref attributes) == 0)
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

                        object value = w32.GetDetails(folder, pidl, SCID.Capacity);
                        if (value is long)
                        {
                            file.Size = (long)value;
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
            IShellFolder2 folder;
            if (shellFileSource.FindFolder(Path.TrimEnd('\\'), out folder) == 0)
            {
                ListFolder(folder, (uint)(SHCONTF.FOLDERS | SHCONTF.NONFOLDERS | SHCONTF.INCLUDEHIDDEN));
            }
        }
    }
}