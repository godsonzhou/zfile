using System.Diagnostics;

namespace zfile
{
	[Flags]
	public enum FileSourceOperationType : uint
	{
		None = 0,

		/// <summary>
		/// List operation
		/// </summary>
		List,

		/// <summary>
		/// Copy operation
		/// </summary>
		Copy,

		/// <summary>
		/// Copy in operation
		/// </summary>
		CopyIn,

		/// <summary>
		/// Copy out operation
		/// </summary>
		CopyOut,

		/// <summary>
		/// Move operation
		/// </summary>
		Move,

		/// <summary>
		/// Delete operation
		/// </summary>
		Delete,

		/// <summary>
		/// Wipe operation
		/// </summary>
		Wipe,

		/// <summary>
		/// Create directory operation
		/// </summary>
		CreateDirectory,

		/// <summary>
		/// Execute operation
		/// </summary>
		Execute,

		/// <summary>
		/// Calculate checksum operation
		/// </summary>
		CalcChecksum,

		/// <summary>
		/// Calculate statistics operation
		/// </summary>
		CalcStatistics,

		/// <summary>
		/// Set file property operation
		/// </summary>
		SetFileProperty,

		/// <summary>
		/// Split operation
		/// </summary>
		Split,

		/// <summary>
		/// Combine operation
		/// </summary>
		Combine,

		/// <summary>
		/// Test archive operation
		/// </summary>
		TestArchive

		//the following may be used in the future
		//CreateHardLink,
		//CreateSymLink,
		//Compare,
		//CompareFiles,
		//CompareFilesByFileObject,
		//CreateArchive,
		//ExtractArchive,
		//ExtractArchiveToTemp,
		//ExtractArchiveToTempAndDelete,
		//ExtractArchiveToTempAndDeleteAll,
		//ExtractArchiveToTempAndDeleteAllAndMove,
		//ExtractArchiveToTempAndDeleteAllAndMoveAndDelete,
	}

	[Flags]
	public enum CopyAttributesOption : uint
	{
		None = 0,
		CopyAttributes = 1,
		CopyXattributes = 2,
		CopyTime = 4,
		CopyOwnership = 8,
		CopyPermissions = 16,
		RemoveReadOnlyAttr = 32,
		/// <summary>Copy mode (Unix)</summary>
		CopyMode = 64,
		/// <summary>Copy owner (Unix)</summary>
		CopyOwner = 128,
		/// <summary>Copy security attributes (Windows)</summary>
		CopySecurity = 256,
		All = CopyAttributes | CopyXattributes | CopyTime | CopyOwnership | CopyPermissions | RemoveReadOnlyAttr | CopyMode | CopyOwner | CopySecurity
	}

	public enum FileSourceOperationOptionGeneral
	{
		None,
		No,
		Yes,
		AskUser
	}

	public enum FileSourceOperationOptionSetPropertyError
	{
		None,
		DontSet,
		IgnoreErrors,
		Ignore,
		Skip,
		Abort
	}

	public enum FileSourceOperationHelperMode
	{
		Copy,
		Move,
		Delete
	}
	public enum WfxResult
	{
		Success,
		NotSupported
	}
	public static partial class Constants
	{
		//public const int CSIDL_DRIVES = 0x0011;
		//public const int SHGDN_INFOLDER = 0x0001;
		//public const int SHGDN_FORPARSING = 0x8000;
		public const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);

		//public const int SW_SHOWNORMAL = 1;
		public const int SEE_MASK_IDLIST = 0x00000004;

		public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";

		public const int FOF_SILENT = 0x0004;
		public const int FOF_NOCONFIRMMKDIR = 0x0200;
		public const int FOF_NOCONFIRMATION = 0x0010;
		public const int FOF_NORECURSION = 0x1000;

		public const int ILD_TRANSPARENT = 0x00000001;
		public const int ILD_IMAGE = 0x00000020;
	}
	public static class Resources
	{
		internal static string MsgLogError;
		internal static string MsgLogWipe;
		internal static string MsgLogWipeDir;
		internal static IFormatProvider? MsgErrDateNotSupported;
		internal static IFormatProvider? MsgFileReadOnly;
		internal static IFormatProvider? FileExistsMessage;
		internal static IFormatProvider? MsgErrRename;
		internal static string MsgLogMove;
		internal static string MsgLogCopy;
		internal static string MsgLogDelete;
		internal static string MsgLogRmDir;
		internal static IFormatProvider? MsgErrDirExists;
		internal static IFormatProvider? MsgErrForceDir;
		internal static IFormatProvider? MsgNotDelete;
		internal static string MsgErrEWrite;
		internal static string MsgErrNotSupported;
		internal static string MsgNoFreeSpaceCont;
		internal static IFormatProvider? MsgInsertNextDisk;
		internal static string MsgErrEOpen;
		internal static IFormatProvider? MsgCannotDeleteDirectory;
		internal static int MsgLoadingFileEntries;
		internal static string? FileOpCopyMoveFileExistsOptions;
		internal static IFormatProvider? MsgDelToTrashForce;
		internal static string? VfsRecycleBin = "Recycle Bin";

		public static string MsgLogSuccess { get; internal set; }
		public static string MsgLogMkDir { get; internal set; }
	}
	public static class Logger
	{
		public static void Write(TOperationThread thread, string message, LogOption type, bool Reservedflag = true )
		{
			// 实现日志记录逻辑
		}
		public static void Write(string message, LogOption type, bool Reservedflag = true)
		{
			// 实现日志记录逻辑
			Debug.Print(message);
		}
	}
	public enum LogOption
	{
		None,
		Info,
		Warning,
		Error,
		Success,
		ArcOp,
		VfsOp,
		Delete,
		DirectoryOperation,
		CopyMoveLink
	}
	
	/// <summary>
	/// Checksum operation mode
	/// </summary>
	public enum CalcCheckSumOperationMode
	{
		/// <summary>Calculate checksum</summary>
		Calc,

		/// <summary>Verify checksum</summary>
		Verify
	}
	public enum HashAlgorithm1
	{
		MD5,
		SHA1,
		SHA256,
		SHA512,
		SFV
	}

	public static partial class GlobalSettings
	{
		internal static bool ProcessComments;
		internal static int HashBlockSize;
		internal static int WipePassNumber = 1;
		internal static bool UseConfigInProgramDir;

		/// <summary>
		/// Whether to skip file operation errors
		/// </summary>
		public static bool SkipFileOpError { get; set; } = false;

		/// <summary>
		/// File operations progress kind
		/// </summary>
		public static FileOperationsProgressKind FileOperationsProgressKind { get; set; } = FileOperationsProgressKind.SeparateWindow;
		public static FileSourceOperationSymLinkOption OperationOptionSymLinks { get; set; } = FileSourceOperationSymLinkOption.None;
		public static FileSourceOperationOptionSetPropertyError OperationOptionSetPropertyError { get; set; } = FileSourceOperationOptionSetPropertyError.Skip;
		public static bool OperationOptionReserveSpace { get; set; } = true;
		public static bool OperationOptionCheckFreeSpace { get; set; } = true;
		public static bool OperationOptionCorrectLinks { get; set; } = true;
		public static FileSourceOperationOptionGeneral OperationOptionCopyOnWrite { get; set; } = FileSourceOperationOptionGeneral.No;
		public static FileSourceOperationOptionFileExists OperationOptionFileExists { get; set; } = FileSourceOperationOptionFileExists.None;
		public static FileSourceOperationOptionDirectoryExists OperationOptionDirectoryExists { get; set; } = FileSourceOperationOptionDirectoryExists.None;
		public static bool OperationOptionCopyAttributes { get; set; } = true;
		public static bool OperationOptionCopyXattributes { get; set; } = true;
		public static bool OperationOptionCopyTime { get; set; } = true;
		public static bool OperationOptionCopyOwnership { get; set; } = true;
		public static bool OperationOptionCopyPermissions { get; set; } = true;
		public static bool DropReadOnlyFlag { get; set; } = true;
		public static bool OperationOptionVerify { get; set; }
		public static bool OperationOptionExcludeEmptyDirectories { get; set; } = true;
		public static bool LogErrors { get; set; }
		public static bool LogInfo { get; set; }
		public static bool LogSuccess { get; set; }
		public static bool LogDirectoryOperations { get; set; }
		public static LogOption LogOptions { get; set; }
		public static bool LogDelete { get; set; }
		public static uint CopyBlockSize { get; set; } = 65536;
		public static string AutoExtractOpenMask { get; set; } = "*.zip;*.rar;*.7z;*.tar;*.gz;*.bz2;*.xz";
		// VFS Module List
		public static VfsModuleList VfsModuleList { get; private set; } = new VfsModuleList();
		public static WfxModuleList WfxPlugins { get; internal set; }

		public static VfsModuleList GetVfsModuleList() { return _vfsModuleList; }

		// Initialize global settings
		static GlobalSettings()
		{
			// Register the VFS file source
			VfsModuleList.AddObject("VFS", new VfsModule(true, typeof(VfsFileSource)));

			// Add more VFS modules as needed
			// VfsModuleList.AddObject("FTP", new VfsModule(true, typeof(FtpFileSource)));
			// VfsModuleList.AddObject("ZIP", new VfsModule(true, typeof(ZipFileSource)));
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
		}
		// VFS module list
		private static VfsModuleList _vfsModuleList;

		// Extensions manager
		private static ExtensionsManager _extensions;

		// Archive options
		public static List<string> ArchiveExtensions { get; } = new List<string>();

		/// <summary>
		/// Gets the extensions manager
		/// </summary>
		public static ExtensionsManager Extensions
		{
			get
			{
				if (_extensions == null)
				{
					_extensions = new ExtensionsManager();
				}
				return _extensions;
			}
		}

		/// <summary>
		/// Initializes the global settings
		/// </summary>
		public static void Initialize()
		{
			// Initialize VFS module list
			_vfsModuleList = new VfsModuleList();

			// Initialize extensions manager
			_extensions = new ExtensionsManager();

			// Load settings from configuration file
			LoadSettings();
		}

		/// <summary>
		/// Loads settings from configuration file
		/// </summary>
		private static void LoadSettings()
		{
			// TODO: Load settings from configuration file
		}

		/// <summary>
		/// Saves settings to configuration file
		/// </summary>
		public static void SaveSettings()
		{
			// TODO: Save settings to configuration file
		}
	}

	/// <summary>
	/// Extensions manager
	/// </summary>
	public class ExtensionsManager
	{
		/// <summary>
		/// Gets the command for the specified action on the specified file
		/// </summary>
		/// <param name="file">The file</param>
		/// <param name="action">The action</param>
		/// <param name="cmd">The command</param>
		/// <param name="parameters">The parameters</param>
		/// <param name="startPath">The start path</param>
		/// <returns>True if the command was found, false otherwise</returns>
		public bool GetExtActionCmd(FileEntry file, string action, out string cmd, out string parameters, out string startPath)
		{
			// Default values
			cmd = string.Empty;
			parameters = string.Empty;
			startPath = string.Empty;

			// TODO: Implement extension action lookup
			if (action == "open" && !string.IsNullOrEmpty(file.Extension))
			{
				// For now, just return a simple command based on the file extension
				switch (file.Extension.ToLower())
				{
					case ".txt":
					case ".log":
					case ".ini":
					case ".xml":
					case ".json":
						cmd = "notepad.exe";
						parameters = $"\"{file.FullPath}\"";
						return true;
					case ".jpg":
					case ".jpeg":
					case ".png":
					case ".gif":
					case ".bmp":
						cmd = "mspaint.exe";
						parameters = $"\"{file.FullPath}\"";
						return true;
					case ".pdf":
						cmd = "explorer.exe";
						parameters = $"\"{file.FullPath}\"";
						return true;
					case ".exe":
					case ".bat":
					case ".cmd":
						cmd = file.FullPath;
						return true;
				}
			}

			return false;
		}
	}



} 