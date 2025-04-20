using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zfile
{
    /// <summary>
    /// Global settings for the application
    /// </summary>
    public static partial class GlobalSettings
    {
        //private static readonly VfsModuleList _vfsModuleList = new VfsModuleList();

        /// <summary>
        /// Gets the VFS module list
        /// </summary>
        //public static VfsModuleList GetVfsModuleList() { return _vfsModuleList; }

        //#region Operation Options

        ///// <summary>
        ///// Gets or sets the file exists option
        ///// </summary>
        //public static FileSourceOperationOptionFileExists OperationOptionFileExists { get; set; }

        ///// <summary>
        ///// Gets or sets the directory exists option
        ///// </summary>
        //public static DirectoryExistsOption OperationOptionDirectoryExists { get; set; }

        ///// <summary>
        ///// Gets or sets the set property error option
        ///// </summary>
        //public static SetPropertyErrorOption OperationOptionSetPropertyError { get; set; }

        ///// <summary>
        ///// Gets or sets the copy on write option
        ///// </summary>
        //public static CopyOnWriteOption OperationOptionCopyOnWrite { get; set; }

        ///// <summary>
        ///// Gets or sets whether to verify copies
        ///// </summary>
        //public static bool OperationOptionVerify { get; set; }

        ///// <summary>
        ///// Gets or sets whether to copy attributes
        ///// </summary>
        //public static bool OperationOptionCopyAttributes { get; set; }

        ///// <summary>
        ///// Gets or sets whether to copy extended attributes
        ///// </summary>
        //public static bool OperationOptionCopyXattributes { get; set; }

        ///// <summary>
        ///// Gets or sets whether to copy time
        ///// </summary>
        //public static bool OperationOptionCopyTime { get; set; }

        ///// <summary>
        ///// Gets or sets whether to copy ownership
        ///// </summary>
        //public static bool OperationOptionCopyOwnership { get; set; }

        ///// <summary>
        ///// Gets or sets whether to copy permissions
        ///// </summary>
        //public static bool OperationOptionCopyPermissions { get; set; }

        ///// <summary>
        ///// Gets or sets whether to drop read-only flag
        ///// </summary>
        //public static bool DropReadOnlyFlag { get; set; }

        ///// <summary>
        ///// Gets or sets the symlinks option
        ///// </summary>
        //public static SymLinksOption OperationOptionSymLinks { get; set; }

        ///// <summary>
        ///// Gets or sets whether to correct links
        ///// </summary>
        //public static bool OperationOptionCorrectLinks { get; set; }

        ///// <summary>
        ///// Gets or sets whether to reserve space
        ///// </summary>
        //public static bool OperationOptionReserveSpace { get; set; }

        ///// <summary>
        ///// Gets or sets whether to check free space
        ///// </summary>
        //public static bool OperationOptionCheckFreeSpace { get; set; }

        ///// <summary>
        ///// Gets or sets whether to exclude empty directories
        ///// </summary>
        //public static bool OperationOptionExcludeEmptyDirectories { get; set; }

        ///// <summary>
        ///// Gets or sets whether to skip file operation errors
        ///// </summary>
        //public static bool SkipFileOpError { get; set; }

        ///// <summary>
        ///// Gets or sets the wipe pass number
        ///// </summary>
        //public static int WipePassNumber { get; set; } = 1;

        ///// <summary>
        ///// Gets or sets whether to process comments
        ///// </summary>
        //public static bool ProcessComments { get; set; }

        //#endregion

        /// <summary>
        /// Static constructor
        /// </summary>
        //static GlobalSettings()
        //{
        //    // Initialize default values
        //    OperationOptionFileExists = FileSourceOperationOptionFileExists.Ask;
        //    OperationOptionDirectoryExists = DirectoryExistsOption.Ask;
        //    OperationOptionSetPropertyError = SetPropertyErrorOption.Ask;
        //    OperationOptionCopyOnWrite = CopyOnWriteOption.No;
        //    OperationOptionVerify = false;
        //    OperationOptionCopyAttributes = true;
        //    OperationOptionCopyXattributes = false;
        //    OperationOptionCopyTime = true;
        //    OperationOptionCopyOwnership = false;
        //    OperationOptionCopyPermissions = false;
        //    DropReadOnlyFlag = false;
        //    OperationOptionSymLinks = SymLinksOption.Follow;
        //    OperationOptionCorrectLinks = true;
        //    OperationOptionReserveSpace = true;
        //    OperationOptionCheckFreeSpace = true;
        //    OperationOptionExcludeEmptyDirectories = true;
        //    SkipFileOpError = false;
        //    WipePassNumber = 1;
        //    ProcessComments = false;
        //}
    }
}
