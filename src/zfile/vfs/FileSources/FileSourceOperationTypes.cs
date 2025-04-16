namespace zfile
{
	[Flags]
    public enum FileSourceOperationType : uint
    {
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
	public enum CopyAttributesOption
	{
		None = 0,
		CopyAttributes = 1,
		CopyXattributes = 2,
		CopyTime = 4,
		CopyOwnership = 8,
		CopyPermissions = 16,
		RemoveReadOnlyAttr = 32
	}

	public enum FileSourceOperationOptionGeneral
    {
        No,
        Yes,
        AskUser
    }

    public enum FileSourceOperationOptionSetPropertyError
    {
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