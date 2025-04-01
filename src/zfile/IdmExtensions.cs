using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Zfile
{
    /// <summary>
    /// Idm类的扩展，提供带进度报告的下载功能
    /// </summary>
    public static class IdmExtensions
    {
        /// <summary>
        /// 带进度报告的下载方法
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="localfile">本地保存路径</param>
        /// <param name="chunks">分块数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调，参数为：进度百分比、下载速度(bytes/s)、文件总大小</param>
        /// <returns>下载任务</returns>
        public static async Task StartWithProgress(string url, string localfile, int chunks = 4, 
            CancellationToken cancellationToken = default, 
            Action<double, double, long> progressCallback = null)
        {
            var downloader = new ChunkDownloaderWithProgress(
                url,
                localfile,
                chunks,
                progressCallback
            );

            try
            {
                await downloader.DownloadAsync(cancellationToken);
                Debug.Print("Download completed successfully!");
            }
            catch (OperationCanceledException)
            {
                Debug.Print("Download was cancelled");
                throw; // 重新抛出取消异常
            }
            catch (Exception ex)
            {
                Debug.Print($"Download failed: {ex.Message}");
                Debug.Print("Resume the download later by rerunning the program");
                throw; // 重新抛出异常
            }
        }
    }

    /// <summary>
    /// 带进度报告的分块下载器
    /// </summary>
    public class ChunkDownloaderWithProgress : ChunkDownloader
    {
        private readonly Action<double, double, long> _progressCallback;
        private long _lastReportTime;
        private long _lastDownloadedBytes;
        private double _currentSpeed;

        public ChunkDownloaderWithProgress(string url, string savePath, int chunks = 4, 
            Action<double, double, long> progressCallback = null) 
            : base(url, savePath, chunks)
        {
            _progressCallback = progressCallback;
            _lastReportTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        /// <summary>
        /// 重写进度显示方法，添加进度回调
        /// </summary>
        protected async Task ShowProgressAsync(long totalSize)
        {
            while (true)
            {
                var downloaded = _progress.Values.Sum();
                var progress = (double)downloaded / totalSize * 100;
                
                // 计算下载速度
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long timeElapsed = currentTime - _lastReportTime;
                
                if (timeElapsed > 0)
                {
                    long bytesChange = downloaded - _lastDownloadedBytes;
                    _currentSpeed = bytesChange * 1000.0 / timeElapsed; // bytes per second
                    
                    _lastReportTime = currentTime;
                    _lastDownloadedBytes = downloaded;
                }
                
                Debug.Print($"Progress: {progress:F2}% ({downloaded}/{totalSize}) Speed: {FormatSpeed(_currentSpeed)}");
                
                // 调用进度回调
                _progressCallback?.Invoke(progress, _currentSpeed, totalSize);

                if (downloaded >= totalSize) break;
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// 格式化速度显示
        /// </summary>
        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F2} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
        }

        /// <summary>
        /// 重写下载方法，添加取消令牌支持
        /// </summary>
        public async Task DownloadAsync(CancellationToken cancellationToken = default)
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
                tasks[i] = DownloadChunkAsync(chunks[chunkId], fileStream, cancellationToken);
            }

            // 显示进度
            var progressTask = ShowProgressAsync(totalSize);

            // 等待所有任务完成或取消
            try
            {
                await Task.WhenAll(tasks);
                await progressTask;

                // 重命名临时文件
                File.Move(_tempFile, _savePath, true);
            }
            catch
            {
                // 如果任务被取消，保留临时文件和进度文件以便后续恢复
                throw;
            }
        }

        /// <summary>
        /// 重写分块下载方法，添加取消令牌支持
        /// </summary>
        protected async Task DownloadChunkAsync((long Start, long End) range, FileStream fileStream, CancellationToken cancellationToken)
        {
            int retry = 0;
            const int maxRetries = 5;

            while (retry < maxRetries)
            {
                try
                {
                    // 检查取消令牌
                    cancellationToken.ThrowIfCancellationRequested();

                    using var request = new HttpRequestMessage(HttpMethod.Get, _url);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(range.Start + _progress.GetValueOrDefault<long, long>(range.Start), range.End);

                    using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    using var stream = await response.Content.ReadAsStreamAsync();

                    var buffer = new byte[8192];
                    int bytesRead;
                    long totalRead = _progress.GetValueOrDefault<long, long>(range.Start);

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        lock (fileStream)
                        {
                            fileStream.Seek(range.Start + totalRead, SeekOrigin.Begin);
                            fileStream.Write(buffer, 0, bytesRead);
                        }

                        totalRead += bytesRead;
                        _progress[range.Start] = totalRead;
                        SaveProgress();

                        // 检查取消令牌
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return;
                }
                catch (OperationCanceledException)
                {
                    // 重新抛出取消异常
                    throw;
                }
                catch (Exception ex) when (retry < maxRetries - 1)
                {
                    retry++;
                    await Task.Delay(1000 * retry, cancellationToken);
                }
            }
            throw new Exception($"Chunk download failed after {maxRetries} retries");
        }
    }
}