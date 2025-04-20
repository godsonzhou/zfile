namespace zfile
{
	[Flags]
    public enum FileSourceOperationType : uint
    {
		None = 0,
		Copy,
        CopyIn,
        CopyOut,
        Move,
        Delete,
        CreateDirectory,
        Execute,
        TestArchive,
        CalcChecksum,
        SetFileProperty,
        Split,
        Combine,
        CreateHardLink,
        CreateSymLink,
        Wipe,
        CalcStatistics,
		List,
		Compare,
		CompareFiles,
		CompareFilesByFileObject,
		CreateArchive,
		ExtractArchive,
		ExtractArchiveToTemp,
		ExtractArchiveToTempAndDelete,
		ExtractArchiveToTempAndDeleteAll,
		ExtractArchiveToTempAndDeleteAllAndMove,
		ExtractArchiveToTempAndDeleteAllAndMoveAndDelete
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
} 