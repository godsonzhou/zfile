using System.Reflection.Metadata;

namespace zfile
{
	public static class Constants
	{
		public const int CSIDL_DRIVES = 0x0011;
		public const int SHGDN_INFOLDER = 0x0001;
		public const int SHGDN_FORPARSING = 0x8000;
		public const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);

		public const int SW_SHOWNORMAL = 1;
		public const int SEE_MASK_IDLIST = 0x00000004;

		public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";

		public const int FOF_SILENT = 0x0004;
		public const int FOF_NOCONFIRMMKDIR = 0x0200;
		public const int FOF_NOCONFIRMATION = 0x0010;
		public const int FOF_NORECURSION = 0x1000;
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

		public static string MsgLogSuccess { get; internal set; }
		public static string MsgLogMkDir { get; internal set; }
	}
	public static class Logger
	{
		public static void Write(System.Threading.Thread thread, string message, LogOption type, bool Reservedflag = true )
		{
			// 实现日志记录逻辑
		}
		public static void Write(string message, LogOption type, bool Reservedflag = true)
		{
			// 实现日志记录逻辑
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
	public enum HashAlgorithm
	{
		MD5,
		SHA1,
		SHA256,
		SHA512,
		SFV
	}

	
	public enum FileExistsOption
	{
		None,
		Overwrite,
		OverwriteOlder,
		Skip
	}

	public enum DirectoryExistsOption
	{
		None,
		CopyInto,
		Skip
	}

	public enum SetPropertyErrorOption
	{
		None,
		DontSet,
		IgnoreErrors
	}

	public enum CopyOnWriteOption
	{
		None,
		Yes,
		No
	}

	public enum SymLinksOption
	{
		Follow,
		DontFollow,
		None
	}
	public static class GlobalSettings
    {
		/// <summary>
		/// Whether to skip file operation errors
		/// </summary>
		public static bool SkipFileOpError { get; set; } = false;

		/// <summary>
		/// File operations progress kind
		/// </summary>
		public static FileOperationsProgressKind FileOperationsProgressKind { get; set; } = FileOperationsProgressKind.SeparateWindow;
		public static FileSourceOperationOptionGeneral OperationOptionSymLinks { get; set; } = FileSourceOperationOptionGeneral.AskUser;
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
	}
} 