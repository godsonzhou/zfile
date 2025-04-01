using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using DiffPlex;
using System.Text;

//using Timer = System.Threading.Thread;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Zfile
{
	public class FileContentWatcher
	{
		private static FileSystemWatcher _watcher;
		private static string _lastContentHash; // 保存上一次内容的哈希值
		private static System.Threading.Timer _debounceTimer;     // 防抖计时器
		private static readonly object _lock = new object();


		public static void Main(string filePath)
		{
			//string filePath = @"C:\Test\example.txt"; // 监控的文件路径

			// 初始化文件内容哈希
			_lastContentHash = GetFileHash(filePath);

			_watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(filePath),
				Filter = Path.GetFileName(filePath),
				NotifyFilter = NotifyFilters.LastWrite // 仅监控内容修改
			};

			_watcher.Changed += OnFileChanged;
			_watcher.EnableRaisingEvents = true;
			// 增大缓冲区避免事件丢失
			_watcher.InternalBufferSize = 65536; // 64KB

			Debug.Print($"监控文件内容变化: {filePath}");
			Debug.Print("按 Q 退出...");
			while (Console.ReadKey().Key != ConsoleKey.Q) { }
		}

		// 文件变化事件（含防抖）
		private static void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = new System.Threading.Timer(_ =>
			{
				CheckContentChanges(e.FullPath);
			}, null, 500, Timeout.Infinite); // 延迟 500ms 确保写入完成
		}

		// 检查内容变化
		private static void CheckContentChanges(string filePath)
		{
			lock (_lock)
			{
				try
				{
					string currentHash = GetFileHash(filePath);
					if (currentHash != _lastContentHash)
					{
						string newContent = ReadFileWithRetry(filePath);
						string oldContent = _lastContentHash == null ? "" : ReadFileWithRetry(filePath);

						Console.WriteLine($"\n文件内容已修改 ({DateTime.Now:HH:mm:ss})");
						Console.WriteLine("差异内容：");
						Console.WriteLine(GetTextDiff(oldContent, newContent));

						_lastContentHash = currentHash;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"错误: {ex.Message}");
				}
			}
		}

		// 读取文件内容（含重试机制）
		private static string ReadFileWithRetry(string path, int retryCount = 3)
		{
			for (int i = 0; i < retryCount; i++)
			{
				try
				{
					using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (var reader = new StreamReader(fs))
					{
						return reader.ReadToEnd();
					}
				}
				catch (IOException) when (i < retryCount - 1)
				{
					Thread.Sleep(100); // 等待文件释放
				}
			}
			return string.Empty;
		}

		// 计算文件哈希（用于快速比较内容变化）
		private static string GetFileHash(string filePath)
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(filePath))
			{
				byte[] hash = md5.ComputeHash(stream);
				return BitConverter.ToString(hash);
			}
		}

		// 简单文本差异比较（可替换为更复杂的算法）
		private static string GetTextDiff1(string oldText, string newText)
		{
			if (oldText == newText) return "[无变化]";

			// 简单实现：显示新旧内容（实际项目可用 DiffPlex 等库）
			return $"旧内容长度: {oldText.Length}\n新内容长度: {newText.Length}\n" +
				   "----------------------------------\n" +
				   newText;
		}
		private static string GetTextDiff(string oldText, string newText)
		{
			var diffBuilder = new InlineDiffBuilder(new Differ());
			var diff = diffBuilder.BuildDiffModel(oldText, newText);

			var result = new StringBuilder();
			foreach (var line in diff.Lines)
			{
				result.AppendLine($"{line.Type} : {line.Text}");
			}
			return result.ToString();
		}
	}
}