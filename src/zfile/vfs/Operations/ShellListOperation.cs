using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace FileSystemOperations
{
    public class ShellListOperation : FileSourceListOperation
    {
        private readonly IShellFileSource shellFileSource;

        public ShellListOperation(IFileSource fileSource, string path) : base(fileSource, path)
        {
            shellFileSource = fileSource as IShellFileSource;
            Files = new List<FileInfo>();
        }

        public override void MainExecute()
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
            OleCheck(SHGetIDListFromObject(folder, out parent));
            try
            {
                IEnumIDList enumIDList;
                OleCheck(folder.EnumObjects(0, grfFlags, out enumIDList));

                IntPtr pidl;
                uint numIDs;
                while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = GetDisplayNameEx(folder, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = ILCombine(parent, pidl);
                        file.LinkProperty.LinkTo = GetDisplayName(folder, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

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

                        object value = GetDetails(folder, pidl, SCID.FileSize);
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

                        value = GetDetails(folder, pidl, SCID.DateModified);
                        if (value != null)
                        {
                            file.ModificationTime = (DateTime)value;
                        }
                        else
                        {
                            file.ModificationTimeProperty.IsValid = false;
                        }

                        value = GetDetails(folder, pidl, SCID.DateCreated);
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
            OleCheck(SHGetDesktopFolder(out desktopFolder));
            IntPtr drivesPidl;
            OleCheck(SHGetFolderLocation(0, CSIDL.DRIVES, 0, 0, out drivesPidl));
            try
            {
                IShellFolder2 folder;
                OleCheck(desktopFolder.BindToObject(drivesPidl, IntPtr.Zero, typeof(IShellFolder2).GUID, out folder));

                IEnumIDList enumIDList;
                OleCheck(folder.EnumObjects(0, SHCONTF.FOLDERS | SHCONTF.STORAGE, out enumIDList));

                IntPtr pidl;
                uint numIDs;
                while (enumIDList.Next(1, out pidl, out numIDs) == 0)
                {
                    try
                    {
                        CheckOperationState();

                        var file = ShellFileSource.CreateFile(Path);
                        file.Name = GetDisplayNameEx(folder, pidl, SHGDN.INFOLDER);
                        ((FileShellProperty)file.LinkProperty).Item = ILCombine(drivesPidl, pidl);
                        file.LinkProperty.LinkTo = GetDisplayName(folder, pidl, SHGDN.INFOLDER | SHGDN.FORPARSING);

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

                        object value = GetDetails(folder, pidl, SCID.Capacity);
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
                ListFolder(folder, SHCONTF.FOLDERS | SHCONTF.NONFOLDERS | SHCONTF.INCLUDEHIDDEN);
            }
        }

        private void OleCheck(int hr)
        {
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    public static class Constants
    {
        public const int CSIDL_DRIVES = 0x0011;
        public const int SHGDN_INFOLDER = 0x0001;
        public const int SHGDN_FORPARSING = 0x8000;
    }
} 