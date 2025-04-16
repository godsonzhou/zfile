using System.Reflection.Metadata;

namespace zfile
{

	public static class Logger
	{
		public static void Write(System.Threading.Thread thread, string message, LogOption type, bool Reservedflag = true )
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
		ArcOp
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
        public static FileSourceOperationOptionGeneral OperationOptionSymLinks { get; set; } = FileSourceOperationOptionGeneral.AskUser;
        public static FileSourceOperationOptionSetPropertyError OperationOptionSetPropertyError { get; set; } = FileSourceOperationOptionSetPropertyError.Skip;
        public static bool OperationOptionReserveSpace { get; set; } = true;
        public static bool OperationOptionCheckFreeSpace { get; set; } = true;
        public static bool OperationOptionCorrectLinks { get; set; } = true;
        public static FileSourceOperationOptionGeneral OperationOptionCopyOnWrite { get; set; } = FileSourceOperationOptionGeneral.No;
        public static FileSourceOperationOptionGeneral OperationOptionFileExists { get; set; } = FileSourceOperationOptionGeneral.AskUser;
        public static FileSourceOperationOptionGeneral OperationOptionDirectoryExists { get; set; } = FileSourceOperationOptionGeneral.AskUser;
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
		public static bool SkipFileOpError { get; set; } = true;
	}
} 