using System.Collections.Concurrent;
using System.Diagnostics;

public class ChunkDownloader
{
	public readonly string _url;
	public readonly string _savePath;
	public readonly int _chunks;
	public readonly HttpClient _client;
	public readonly ConcurrentDictionary<long, long> _progress;
	public readonly string _tempFile;

	public ChunkDownloader(string url, string savePath, int chunks = 4)
	{
		_url = url;
		_savePath = savePath;
		_chunks = chunks;
		_client = new HttpClient();
		_progress = new ConcurrentDictionary<long, long>();
		_tempFile = Path.ChangeExtension(savePath, ".tmp");
	}

	public async Task DownloadAsync()
	{
		// 获取文件总大小
		var totalSize = await GetFileSizeAsync();

		// 初始化/恢复下载进度
		var chunks = InitializeChunks(totalSize);

		// 创建/打开临时文件
		using var fileStream = new FileStream(_tempFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
		fileStream.SetLength(totalSize);

		// 多线程下载
		var tasks = new Task[_chunks];
		for (int i = 0; i < _chunks; i++)
		{
			int chunkId = i;
			tasks[i] = DownloadChunkAsync(chunks[chunkId], fileStream);
		}

		// 显示进度
		var progressTask = ShowProgressAsync(totalSize);

		await Task.WhenAll(tasks);
		await progressTask;

		// 重命名临时文件
		File.Move(_tempFile, _savePath, true);
	}

	public async Task<long> GetFileSizeAsync()
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, _url);
		var response = await _client.SendAsync(request);
		return response.Content.Headers.ContentLength ?? throw new Exception("Unsupported content length");
	}

	public (long Start, long End)[] InitializeChunks(long totalSize)
	{
		// 尝试加载进度文件
		if (File.Exists(_tempFile + ".progress"))
		{
			var lines = File.ReadAllLines(_tempFile + ".progress");
			foreach (var line in lines)
			{
				var parts = line.Split(':');
				_progress[int.Parse(parts[0])] = long.Parse(parts[1]);
			}
		}

		var chunkSize = totalSize / _chunks;
		var chunks = new (long Start, long End)[_chunks];
		for (int i = 0; i < _chunks; i++)
		{
			var start = i * chunkSize;
			var end = (i == _chunks - 1) ? totalSize - 1 : start + chunkSize - 1;
			chunks[i] = (start, end);
		}
		return chunks;
	}

	private async Task DownloadChunkAsync((long Start, long End) range, FileStream fileStream)
	{
		int retry = 0;
		const int maxRetries = 5;

		while (retry < maxRetries)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, _url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(range.Start + _progress.GetValueOrDefault<long, long>(range.Start), range.End);

				using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
				using var stream = await response.Content.ReadAsStreamAsync();

				var buffer = new byte[8192];
				int bytesRead;
				long totalRead = _progress.GetValueOrDefault<long, long>(range.Start);

				while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
				{
					lock (fileStream)
					{
						fileStream.Seek(range.Start + totalRead, SeekOrigin.Begin);
						fileStream.Write(buffer, 0, bytesRead);
					}

					totalRead += bytesRead;
					_progress[range.Start] = totalRead;
					SaveProgress();
				}
				return;
			}
			catch (Exception ex) when (retry < maxRetries - 1)
			{
				retry++;
				await Task.Delay(1000 * retry);
			}
		}
		throw new Exception($"Chunk download failed after {maxRetries} retries");
	}

	public void SaveProgress()
	{
		var lines = _progress.Select(p => $"{p.Key}:{p.Value}");
		File.WriteAllLines(_tempFile + ".progress", lines);
	}

	private async Task ShowProgressAsync(long totalSize)
	{
		while (true)
		{
			var downloaded = _progress.Values.Sum();
			var progress = (double)downloaded / totalSize * 100;
			Debug.Print($"Progress: {progress:F2}% ({downloaded}/{totalSize})");

			if (downloaded >= totalSize) break;
			await Task.Delay(1000);
		}
	}
}

// 使用示例
public class Idm
{
	public static async Task Start(string url, string localfile, int chunks = 4)
	{
		var downloader = new ChunkDownloader(
			url, //"https://example.com/large-file.zip",
			localfile, //"downloaded-file.zip",
			chunks: chunks // 4 chunks by default
		);

		try
		{
			await downloader.DownloadAsync();
			Debug.Print("Download completed successfully!");
		}
		catch (Exception ex)
		{
			Debug.Print($"Download failed: {ex.Message}");
			Debug.Print("Resume the download later by rerunning the program");
		}
	}
}