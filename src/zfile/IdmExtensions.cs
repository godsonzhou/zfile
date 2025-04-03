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
		/// <summary>
		/// 带进度报告的下载方法
		/// </summary>
		/// <param name="url">下载地址</param>
		/// <param name="localfile">本地保存路径</param>
		/// <param name="chunks">分块数量</param>
		/// <param name="cancellationToken">取消令牌</param>
		/// <param name="progressCallback">进度回调，参数为：进度百分比、下载速度(bytes/s)、文件总大小、分块进度</param>
		/// <returns>下载任务</returns>
		public static async Task StartWithProgress(string url, string localfile, int chunks = 4, 
            CancellationToken cancellationToken = default, 
            Action<double, double, long, Dictionary<long, long>> progressCallback = null)
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

   
}