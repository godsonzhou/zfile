﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public class EverythingWrapper
{
	const int EVERYTHING_OK = 0;
	const int EVERYTHING_ERROR_MEMORY = 1;
	const int EVERYTHING_ERROR_IPC = 2;
	const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
	const int EVERYTHING_ERROR_CREATEWINDOW = 4;
	const int EVERYTHING_ERROR_CREATETHREAD = 5;
	const int EVERYTHING_ERROR_INVALIDINDEX = 6;
	const int EVERYTHING_ERROR_INVALIDCALL = 7;

	public const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
	public const int EVERYTHING_REQUEST_PATH = 0x00000002;
	const int EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
	const int EVERYTHING_REQUEST_EXTENSION = 0x00000008;
	const int EVERYTHING_REQUEST_SIZE = 0x00000010;
	const int EVERYTHING_REQUEST_DATE_CREATED = 0x00000020;
	const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
	const int EVERYTHING_REQUEST_DATE_ACCESSED = 0x00000080;
	const int EVERYTHING_REQUEST_ATTRIBUTES = 0x00000100;
	const int EVERYTHING_REQUEST_FILE_LIST_FILE_NAME = 0x00000200;
	const int EVERYTHING_REQUEST_RUN_COUNT = 0x00000400;
	const int EVERYTHING_REQUEST_DATE_RUN = 0x00000800;
	const int EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED = 0x00001000;
	const int EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME = 0x00002000;
	const int EVERYTHING_REQUEST_HIGHLIGHTED_PATH = 0x00004000;
	const int EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000;

	const int EVERYTHING_SORT_NAME_ASCENDING = 1;
	const int EVERYTHING_SORT_NAME_DESCENDING = 2;
	public const int EVERYTHING_SORT_PATH_ASCENDING = 3;
	const int EVERYTHING_SORT_PATH_DESCENDING = 4;
	const int EVERYTHING_SORT_SIZE_ASCENDING = 5;
	const int EVERYTHING_SORT_SIZE_DESCENDING = 6;
	const int EVERYTHING_SORT_EXTENSION_ASCENDING = 7;
	const int EVERYTHING_SORT_EXTENSION_DESCENDING = 8;
	const int EVERYTHING_SORT_TYPE_NAME_ASCENDING = 9;
	const int EVERYTHING_SORT_TYPE_NAME_DESCENDING = 10;
	const int EVERYTHING_SORT_DATE_CREATED_ASCENDING = 11;
	const int EVERYTHING_SORT_DATE_CREATED_DESCENDING = 12;
	const int EVERYTHING_SORT_DATE_MODIFIED_ASCENDING = 13;
	const int EVERYTHING_SORT_DATE_MODIFIED_DESCENDING = 14;
	const int EVERYTHING_SORT_ATTRIBUTES_ASCENDING = 15;
	const int EVERYTHING_SORT_ATTRIBUTES_DESCENDING = 16;
	const int EVERYTHING_SORT_FILE_LIST_FILENAME_ASCENDING = 17;
	const int EVERYTHING_SORT_FILE_LIST_FILENAME_DESCENDING = 18;
	const int EVERYTHING_SORT_RUN_COUNT_ASCENDING = 19;
	const int EVERYTHING_SORT_RUN_COUNT_DESCENDING = 20;
	const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_ASCENDING = 21;
	const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_DESCENDING = 22;
	const int EVERYTHING_SORT_DATE_ACCESSED_ASCENDING = 23;
	const int EVERYTHING_SORT_DATE_ACCESSED_DESCENDING = 24;
	const int EVERYTHING_SORT_DATE_RUN_ASCENDING = 25;
	const int EVERYTHING_SORT_DATE_RUN_DESCENDING = 26;

	const int EVERYTHING_TARGET_MACHINE_X86 = 1;
	const int EVERYTHING_TARGET_MACHINE_X64 = 2;
	const int EVERYTHING_TARGET_MACHINE_ARM = 3;


	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern UInt32 Everything_SetSearchW(string lpSearchString);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetMatchPath(bool bEnable);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetMatchCase(bool bEnable);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetMatchWholeWord(bool bEnable);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetRegex(bool bEnable);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetMax(UInt32 dwMax);
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetOffset(UInt32 dwOffset);

	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetMatchPath();
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetMatchCase();
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetMatchWholeWord();
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetRegex();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetMax();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetOffset();
	[DllImport("Everything64.dll")]
	public static extern IntPtr Everything_GetSearchW();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetLastError();

	[DllImport("Everything64.dll")]
	public static extern bool Everything_QueryW(bool bWait);

	[DllImport("Everything64.dll")]
	public static extern void Everything_SortResultsByPath();

	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetNumFileResults();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetNumFolderResults();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetNumResults();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetTotFileResults();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetTotFolderResults();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetTotResults();
	[DllImport("Everything64.dll")]
	public static extern bool Everything_IsVolumeResult(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_IsFolderResult(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_IsFileResult(UInt32 nIndex);
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern void Everything_GetResultFullPathName(UInt32 nIndex, StringBuilder lpString, UInt32 nMaxCount);
	[DllImport("Everything64.dll")]
	public static extern void Everything_Reset();

	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultFileName(UInt32 nIndex);

	// Everything 1.4
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetSort(UInt32 dwSortType);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetSort();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetResultListSort();
	[DllImport("Everything64.dll")]
	public static extern void Everything_SetRequestFlags(UInt32 dwRequestFlags);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetRequestFlags();
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetResultListRequestFlags();
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultExtension(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultSize(UInt32 nIndex, out long lpFileSize);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultDateCreated(UInt32 nIndex, out long lpFileTime);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultDateModified(UInt32 nIndex, out long lpFileTime);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultDateAccessed(UInt32 nIndex, out long lpFileTime);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetResultAttributes(UInt32 nIndex);
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultFileListFileName(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetResultRunCount(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultDateRun(UInt32 nIndex, out long lpFileTime);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_GetResultDateRecentlyChanged(UInt32 nIndex, out long lpFileTime);
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultHighlightedFileName(UInt32 nIndex);
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultHighlightedPath(UInt32 nIndex);
	[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr Everything_GetResultHighlightedFullPathAndFileName(UInt32 nIndex);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_GetRunCountFromFileName(string lpFileName);
	[DllImport("Everything64.dll")]
	public static extern bool Everything_SetRunCountFromFileName(string lpFileName, UInt32 dwRunCount);
	[DllImport("Everything64.dll")]
	public static extern UInt32 Everything_IncRunCountFromFileName(string lpFileName);
	public static long CalculateDirectorySize(string directoryPath)
	{
		// 确保路径以反斜杠结尾并添加通配符（匹配所有子文件/文件夹）
		var path = directoryPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < path.Length; i++)
		{
			if (path[i].Contains(" "))
			{
				path[i] = $"\"{path[i]}\"";
			}
		}
		var searchPath = string.Join("\\", path) + "\\*";//bugfix: if dir name contains space, it will not work, so use "dir name" instead of dir name

		// 设置请求标志以获取大小
		Everything_SetRequestFlags(EVERYTHING_REQUEST_FILE_NAME | EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_DATE_MODIFIED | EVERYTHING_REQUEST_SIZE);

		// 设置搜索参数
		Everything_SetSearchW(searchPath);
		Everything_QueryW(true);

		// 检查错误
		var error = Everything_GetLastError();
		if (error != EVERYTHING_OK)
		{
			throw new Exception($"Everything 查询失败，错误码: {error}");
		}
		long totalSize = 0;
		uint resultCount = Everything_GetNumResults();

		// 遍历所有结果累加大小
		for (uint i = 0; i < resultCount; i++)
		{
			if (Everything_GetResultSize(i, out long fileSize))
			{
				totalSize += fileSize;
			}
		}

		return totalSize;
	}
	// 检查 Everything 服务是否运行
	public static bool IsEverythingServiceRunning()
	{
		try
		{
			Everything_SetSearchW("test");
			return Everything_GetLastError() != EVERYTHING_ERROR_IPC;
		}
		catch
		{
			Debug.Print("everything is running check exception");
			return false;
		}
	}

	// 转换为友好单位（如 GB/MB）
	//public static string FormatFileSize(long bytes)
	//{
	//	string[] units = { "B", "KB", "MB", "GB", "TB" };
	//	int unitIndex = 0;
	//	double size = bytes;
	//	while (size >= 1024 && unitIndex < units.Length - 1)
	//	{
	//		size /= 1024;
	//		unitIndex++;
	//	}
	//	return $"{size:0.##} {units[unitIndex]}";
	//}
}