using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using Zfile.Forms;

namespace Zfile
{
    /// <summary>
    /// IDM下载管理器，提供UI界面与下载功能的连接
    /// </summary>
    public class IdmManager
    {
		public List<DownloadTask> downloadTasks = new List<DownloadTask>();
		//private CancellationTokenSource cancellationTokenSource;
		/// <summary>
		/// 显示IDM下载管理器窗口
		/// </summary>
		public void ShowIdmForm()
        {
			var form = new IdmForm(this);
			form.Show();
		}

        /// <summary>
        /// 启动下载任务
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="chunks">分块数量</param>
        /// <returns>下载任务</returns>
        public static async Task StartDownload(string url, string savePath, int chunks = 4)
        {
            try
            {
                await Start(url, savePath, chunks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 启动带进度报告的下载任务
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="chunks">分块数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调，参数为：进度百分比、下载速度(bytes/s)、文件总大小、分块进度</param>
        /// <returns>下载任务</returns>
        public static async Task StartDownloadWithProgress(string url, string savePath, int chunks = 4, 
            CancellationToken cancellationToken = default, 
            Action<double, double, long, Dictionary<long, long>> progressCallback = null)
        {
            try
            {
                await StartWithProgress(url, savePath, chunks, cancellationToken, progressCallback);
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，不显示错误消息
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
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
		/// <summary>
		/// 检查下载任务是否可以恢复
		/// </summary>
		/// <param name="savePath">保存路径</param>
		/// <returns>是否可以恢复</returns>
		public static bool CanResumeDownload(string savePath)
        {
            string tempFile = Path.ChangeExtension(savePath, ".tmp");
            string progressFile = tempFile + ".progress";
            return File.Exists(tempFile) && File.Exists(progressFile);
        }
        
        /// <summary>
        /// 启动带有HTTP头和Cookies的下载任务
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="headers">HTTP请求头</param>
        /// <param name="cookies">Cookies</param>
        /// <param name="referrer">引用页</param>
        /// <param name="chunks">分块数量</param>
        /// <returns>下载任务</returns>
        public static async Task StartDownloadWithHeaders(string url, string savePath, Dictionary<string, string> headers = null, string cookies = null, string referrer = null, int chunks = 4)
        {
            try
            {
                // 创建自定义的HttpClient
                var handler = new HttpClientHandler();
                
                // 设置Cookies
                if (!string.IsNullOrEmpty(cookies))
                {
                    handler.CookieContainer = new CookieContainer();
                    
                    // 解析cookies字符串并添加到CookieContainer
                    Uri uri = new Uri(url);
                    string domain = uri.Host;
                    
                    foreach (var cookiePair in cookies.Split(';'))
                    {
                        string[] parts = cookiePair.Trim().Split('=');
                        if (parts.Length == 2)
                        {
                            handler.CookieContainer.Add(new Cookie(parts[0], parts[1], "/", domain));
                        }
                    }
                }
                
                // 创建HttpClient
                var httpClient = new HttpClient(handler);
                
                // 设置HTTP头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                
                // 设置Referer
                if (!string.IsNullOrEmpty(referrer))
                {
                    httpClient.DefaultRequestHeaders.Referrer = new Uri(referrer);
                }
                
                // 设置User-Agent
                if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
                }
                
                // 创建自定义的ChunkDownloader实例
                var downloader = new ChunkDownloader(url, savePath, chunks)
                {
                    _client = httpClient
                };
                
                await downloader.DownloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
		/// <summary>
		/// 显示IDM下载管理器窗口
		/// </summary>
		public void ShowIdmManager()
		{
			try
			{
				ShowIdmForm();
				InitializeChromeExtensionSupport();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"启动IDM下载管理器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 初始化Chrome扩展支持
		/// </summary>
		public static void InitializeChromeExtensionSupport()
		{
			try
			{
				// 启动Chrome扩展消息监听
				ChromeExtensionHandler.StartListening();

				// 注册Native Messaging主机
				RegisterChromeNativeMessagingHost();

				// 显示系统托盘图标
				TrayIconManager.Instance.Show("下载管理器监听已启动，可以接收Chrome扩展的下载请求");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"初始化Chrome扩展支持失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 注册Chrome Native Messaging主机
		/// </summary>
		private static void RegisterChromeNativeMessagingHost()
		{
			try
			{
				// 获取应用程序路径
				string appPath = Application.ExecutablePath;

				// 读取清单模板
				string manifestPath = Path.Combine(Path.GetDirectoryName(appPath), "chrome_host_manifest.json");

				// 无论文件是否存在，都重新创建以确保内容正确
				string manifestTemplate = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome_host_manifest.json"));
				manifestTemplate = manifestTemplate.Replace("ZFILE_PATH_PLACEHOLDER", appPath.Replace("\\", "\\\\"));

				// Chrome要求allowed_origins必须是具体的扩展ID，不能使用通配符
				// 修改为支持特定的扩展ID和通配符
				// 首先尝试从命令行参数或配置中获取扩展ID
				string extensionId = "gpibfiieigpfadmnjmdmgmfcnolodbjm"; // GetChromeExtensionId();

				if (!string.IsNullOrEmpty(extensionId))
				{
					// 使用特定的扩展ID
					manifestTemplate = manifestTemplate.Replace("chrome-extension://EXTENSION_ID_PLACEHOLDER/",
						$"chrome-extension://{extensionId}/");
					Debug.WriteLine($"使用特定的Chrome扩展ID: {extensionId}");
				}
				else
				{
					// 使用通配符（可能在某些情况下不工作）
					manifestTemplate = manifestTemplate.Replace("chrome-extension://EXTENSION_ID_PLACEHOLDER/",
						"chrome-extension://*/*");
					Debug.WriteLine("警告：使用通配符作为扩展ID，这可能导致Chrome扩展无法正确通信");
				}

				// 保存清单文件
				File.WriteAllText(manifestPath, manifestTemplate);

				Debug.WriteLine("已更新Chrome扩展清单文件: " + manifestPath);
				Debug.WriteLine("清单内容: " + manifestTemplate);

				// 注册清单到Chrome
				string hostName = "com.zfile.idm_integration";

				// 注册到Chrome
				using (RegistryKey chromeKey = Registry.CurrentUser.CreateSubKey(
					@"Software\Google\Chrome\NativeMessagingHosts\" + hostName))
				{
					chromeKey.SetValue("", manifestPath);
				}

				// 注册到Edge
				try
				{
					using (RegistryKey edgeKey = Registry.CurrentUser.CreateSubKey(
						@"Software\Microsoft\Edge\NativeMessagingHosts\" + hostName))
					{
						edgeKey.SetValue("", manifestPath);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"注册Edge Native Messaging主机失败: {ex.Message}");
					// 忽略Edge注册失败，因为用户可能没有安装Edge
				}

				Debug.WriteLine("Chrome Native Messaging主机注册成功");
			}
			catch (Exception ex)
			{
				throw new Exception($"注册Chrome Native Messaging主机失败: {ex.Message}", ex);
			}
		}

		/// <summary>
		/// 使用IDM下载指定URL的文件
		/// </summary>
		/// <param name="url">下载地址</param>
		/// <param name="savePath">保存路径，如果为null则弹出对话框让用户选择</param>
		/// <param name="headers">HTTP请求头</param>
		/// <param name="cookies">Cookies</param>
		/// <param name="referrer">引用页</param>
		public void DownloadFile(string url, string savePath = null, Dictionary<string, string> headers = null, string cookies = null, string referrer = null)
		{
			try
			{
				Debug.WriteLine($"开始下载: URL={url}, SavePath={savePath}, Headers={headers?.Count ?? 0}, Cookies={(cookies != null)}, Referrer={(referrer != null)}");

				if (string.IsNullOrEmpty(savePath))
				{
					// 弹出新建下载对话框
					using (var dialog = new IdmForm(this))
					{
						// 预填充URL
						if (!string.IsNullOrEmpty(url))
						{
							dialog.AddNewDownload(url, null, headers, cookies, referrer);
						}

						dialog.ShowDialog();
					}
				}
				else
				{
					// 检查是否有额外的HTTP头或Cookies
					if (headers != null || !string.IsNullOrEmpty(cookies) || !string.IsNullOrEmpty(referrer))
					{
						Debug.WriteLine("使用带HTTP头和Cookies的下载方法");
						// 使用带HTTP头和Cookies的下载方法
						_ = Task.Run(async () => {
							try
							{
								await IdmManager.StartDownloadWithHeaders(url, savePath, headers, cookies, referrer);
								// 显示系统托盘通知
								TrayIconManager.Instance.ShowBalloonTip("下载已开始", $"文件: {Path.GetFileName(savePath)}", ToolTipIcon.Info);
							}
							catch (Exception ex)
							{
								Debug.WriteLine($"下载失败: {ex.Message}");
								TrayIconManager.Instance.ShowBalloonTip("下载失败", ex.Message, ToolTipIcon.Error);
							}
						});
					}
					else
					{
						Debug.WriteLine("直接开始下载");
						// 直接开始下载
						_ = Task.Run(async () => {
							try
							{
								await IdmManager.StartDownload(url, savePath);
								// 显示系统托盘通知
								TrayIconManager.Instance.ShowBalloonTip("下载已开始", $"文件: {Path.GetFileName(savePath)}", ToolTipIcon.Info);
							}
							catch (Exception ex)
							{
								Debug.WriteLine($"下载失败: {ex.Message}");
								TrayIconManager.Instance.ShowBalloonTip("下载失败", ex.Message, ToolTipIcon.Error);
							}
						});
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"下载失败: {ex.Message}");
				MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				// 显示系统托盘通知
				TrayIconManager.Instance.ShowBalloonTip("下载失败", ex.Message, ToolTipIcon.Error);
			}
		}
	}
}