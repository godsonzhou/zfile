using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class EverythingWrapper
{
	// 错误码
	public const int EVERYTHING_OK = 0;
	public const int EVERYTHING_ERROR_MEMORY = 1;
	public const int EVERYTHING_ERROR_IPC = 2;
	// 新增请求标志常量
	public const uint EVERYTHING_REQUEST_SIZE = 0x00000010;

	// 搜索模式
	public const int EVERYTHING_SORT_NAME_ASCENDING = 1;
	// 修正后的函数签名
	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern void Everything_SetRequestFlags(uint dwFlags);

	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern void Everything_SetSearch(string lpSearchString);

	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern bool Everything_Query(bool bWait);

	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern uint Everything_GetNumResults();

	// 修正后的 GetResultSize 声明
	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern bool Everything_GetResultSize(uint index, out long size);

	// 新增错误处理函数
	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern uint Everything_GetLastError();

	// 初始化 Everything
	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern int Everything_GetSearch(string searchText);

	[DllImport("Everything.dll")]
	public static extern ulong Everything_GetResultSize(int index, IntPtr lpsize);

	[DllImport("Everything.dll")]
	public static extern ulong Everything_GetTotFileSize();


	[DllImport("Everything.dll")]
	public static extern void Everything_CleanUp();
	public static long CalculateDirectorySize(string directoryPath)
	{
		// 确保路径以反斜杠结尾并添加通配符（匹配所有子文件/文件夹）
		string searchPath = directoryPath.TrimEnd('\\') + "\\*";
		// 设置请求标志以获取大小
		Everything_SetRequestFlags(EVERYTHING_REQUEST_SIZE);

		// 设置搜索参数
		Everything_SetSearch(searchPath);
		Everything_Query(true);

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
			Everything_SetSearch("test");
			return Everything_GetLastError() != EVERYTHING_ERROR_IPC;
		}
		catch
		{
			return false;
		}
	}

	// 转换为友好单位（如 GB/MB）
	public static string FormatFileSize(long bytes)
	{
		string[] units = { "B", "KB", "MB", "GB", "TB" };
		int unitIndex = 0;
		double size = bytes;
		while (size >= 1024 && unitIndex < units.Length - 1)
		{
			size /= 1024;
			unitIndex++;
		}
		return $"{size:0.##} {units[unitIndex]}";
	}
}