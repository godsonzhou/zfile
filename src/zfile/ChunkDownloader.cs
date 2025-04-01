using System.Collections.Concurrent;
using System.Diagnostics;

public class ChunkDownloader
{
	public  string _url;
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

	public async Task<long> GetFileSizeAsyncbak()
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, _url);
		var response = await _client.SendAsync(request);
		return response.Content.Headers.ContentLength ?? throw new Exception("Unsupported content length");
	}
	public async Task<long> GetFileSizeAsync()
	{
		try
		{
			// 第一阶段：尝试 HEAD 请求
			using var headRequest = new HttpRequestMessage(HttpMethod.Head, _url);

			// 添加通用浏览器头避免被拦截
			headRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

			var headResponse = await _client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);

			// 处理重定向后的最终 URL（重要！）
			_url = headResponse.RequestMessage.RequestUri.ToString();

			if (headResponse.Content.Headers.ContentLength.HasValue)
			{
				return headResponse.Content.Headers.ContentLength.Value;
			}

			// 第二阶段：回退到 GET + Range 请求
			using var getRequest = new HttpRequestMessage(HttpMethod.Get, _url);
			getRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

			var getResponse = await _client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);

			// 验证响应状态
			if (!getResponse.IsSuccessStatusCode)
			{
				throw new Exception($"HTTP Error: {getResponse.StatusCode}");
			}

			// 优先从 Content-Range 获取完整大小
			if (getResponse.Content.Headers.ContentRange?.HasLength == true)
			{
				return getResponse.Content.Headers.ContentRange.Length.Value;
			}

			// 次选 Content-Length
			if (getResponse.Content.Headers.ContentLength.HasValue)
			{
				return getResponse.Content.Headers.ContentLength.Value;
			}

			throw new Exception("无法获取文件大小：服务器未返回有效长度信息");
		}
		catch (HttpRequestException ex)
		{
			throw new Exception($"网络请求失败，请检查: {ex.Message}");
		}
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