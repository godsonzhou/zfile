using System;
using System.Runtime.InteropServices;

public class EverythingWrapper
{
	// 错误码
	public const int EVERYTHING_OK = 0;
	public const int EVERYTHING_ERROR_MEMORY = 1;
	public const int EVERYTHING_ERROR_IPC = 2;

	// 搜索模式
	public const int EVERYTHING_SORT_NAME_ASCENDING = 1;

	// 初始化 Everything
	[DllImport("Everything.dll", CharSet = CharSet.Unicode)]
	public static extern int Everything_GetSearch(string searchText);

	[DllImport("Everything.dll")]
	public static extern void Everything_SetSearch(string searchText);

	[DllImport("Everything.dll")]
	public static extern bool Everything_Query(bool wait);

	[DllImport("Everything.dll")]
	public static extern int Everything_GetNumResults();

	[DllImport("Everything.dll")]
	public static extern ulong Everything_GetResultSize(int index);

	[DllImport("Everything.dll")]
	public static extern ulong Everything_GetTotFileSize();

	[DllImport("Everything.dll")]
	public static extern int Everything_GetLastError();

	[DllImport("Everything.dll")]
	public static extern void Everything_CleanUp();
	public static long CalculateDirectorySize(string directoryPath)
	{
		// 确保路径以反斜杠结尾并添加通配符（匹配所有子文件/文件夹）
		string searchPath = directoryPath.TrimEnd('\\') + "\\*";

		// 设置搜索参数
		Everything_SetSearch(searchPath);
		Everything_Query(wait: true);

		// 检查错误
		int error = Everything_GetLastError();
		if (error != EVERYTHING_OK)
		{
			throw new Exception($"Everything 查询失败，错误码: {error}");
		}

		// 获取总文件大小（字节）
		ulong totalSize = Everything_GetTotFileSize();
		return (long)totalSize;
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