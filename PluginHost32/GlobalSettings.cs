using System.Diagnostics;

namespace zfile
{
	[Flags]
	public enum FileSourceOperationTypes : uint
	{
		None = 0,

		/// <summary>
		/// List operation
		/// </summary>
		List = 1,

		/// <summary>
		/// Copy operation
		/// </summary>
		Copy = 2,

		/// <summary>
		/// Copy in operation
		/// </summary>
		CopyIn = 4,

		/// <summary>
		/// Copy out operation
		/// </summary>
		CopyOut = 8,

		/// <summary>
		/// Move operation
		/// </summary>
		Move = 16,

		/// <summary>
		/// Delete operation
		/// </summary>
		Delete = 32,

		/// <summary>
		/// Wipe operation
		/// </summary>
		Wipe = 64,

		/// <summary>
		/// Create directory operation
		/// </summary>
		CreateDirectory = 128,

		/// <summary>
		/// Execute operation
		/// </summary>
		Execute = 256,

		/// <summary>
		/// Calculate checksum operation
		/// </summary>
		CalcChecksum = 512,

		/// <summary>
		/// Calculate statistics operation
		/// </summary>
		CalcStatistics = 1024,

		/// <summary>
		/// Set file property operation
		/// </summary>
		SetFileProperty = 2048,

		/// <summary>
		/// Split operation
		/// </summary>
		Split = 4096,

		/// <summary>
		/// Combine operation
		/// </summary>
		Combine = 8192,

		/// <summary>
		/// Test archive operation
		/// </summary>
		TestArchive = 16384

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
	public static class Constants
	{
		public const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);
		public const int SEE_MASK_IDLIST = 0x00000004;
		public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";

		public const int FOF_SILENT = 0x0004;
		public const int FOF_NOCONFIRMMKDIR = 0x0200;
		public const int FOF_NOCONFIRMATION = 0x0010;
		public const int FOF_NORECURSION = 0x1000;

		public const int ILD_TRANSPARENT = 0x00000001;
		public const int ILD_IMAGE = 0x00000020;
		//public static string ZfilePath => ZfileBinPath.Replace("src\\zfile\\bin\\Debug", "",StringComparison.OrdinalIgnoreCase).Replace("src\\zfile\\bin\\Release", "", StringComparison.OrdinalIgnoreCase);
		public static string ZfileCfgPath => ZfileBinPath + "\\Config\\";
		public static string ZfileBinPath; // => ZfilePath + "src\\zfile\\bin\\Debug\\";
		public static string ZfilePluginPath => ZfileBinPath + "\\plugins\\";
		public const int CacheTimeout = 500; // ª∫¥Ê≥¨ ± ±º‰(∫¡√Î)
		public static readonly string[] TextFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
	}
	public static class Resources
	{
		internal static string? MsgLogError;
		internal static string? MsgLogWipe;
		internal static string? MsgLogWipeDir;
		internal static IFormatProvider? MsgErrDateNotSupported;
		internal static IFormatProvider? MsgFileReadOnly;
		internal static IFormatProvider? FileExistsMessage;
		internal static IFormatProvider? MsgErrRename;
		internal static string? MsgLogMove;
		internal static string? MsgLogCopy;
		internal static string? MsgLogDelete;
		internal static string? MsgLogRmDir;
		internal static IFormatProvider? MsgErrDirExists;
		internal static IFormatProvider? MsgErrForceDir;
		internal static IFormatProvider? MsgNotDelete;
		internal static string? MsgErrEWrite;
		internal static string? MsgErrNotSupported;
		internal static string? MsgNoFreeSpaceCont;
		internal static IFormatProvider? MsgInsertNextDisk;
		internal static string? MsgErrEOpen;
		internal static IFormatProvider? MsgCannotDeleteDirectory;
		internal static int MsgLoadingFileEntries;
		internal static string? FileOpCopyMoveFileExistsOptions;
		internal static IFormatProvider? MsgDelToTrashForce;
		internal static string? VfsRecycleBin = "Recycle Bin";
		internal static IFormatProvider? MsgExecutablePath;
		internal static IFormatProvider? MsgApplicationName;
		internal static IFormatProvider? MsgProcessId;
		internal static string MsgOpenInAnotherProgram;

		public static string? MsgLogSuccess { get; internal set; }
		public static string? MsgLogMkDir { get; internal set; }
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
	
		public static FileSourceOperationOptionSetPropertyError OperationOptionSetPropertyError { get; set; } = FileSourceOperationOptionSetPropertyError.Skip;
		public static bool OperationOptionReserveSpace { get; set; } = true;
		public static bool OperationOptionCheckFreeSpace { get; set; } = true;
		public static bool OperationOptionCorrectLinks { get; set; } = true;
		public static FileSourceOperationOptionGeneral OperationOptionCopyOnWrite { get; set; } = FileSourceOperationOptionGeneral.No;
	
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
	

		// Initialize global settings
		static GlobalSettings()
		{
			// Register the VFS file source
		

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
	



} 