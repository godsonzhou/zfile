namespace FileSystemOperations
{
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
		public static LogOptions LogOptions { get; set; }
		public static bool LogDelete { get; set; }
	}
} 