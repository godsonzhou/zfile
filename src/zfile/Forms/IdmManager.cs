using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zfile.Forms
{
    /// <summary>
    /// IDM下载管理器，提供UI界面与下载功能的连接
    /// </summary>
    public static class IdmManager
    {
        /// <summary>
        /// 显示IDM下载管理器窗口
        /// </summary>
        public static void ShowIdmManager()
        {
            IdmForm.ShowIdmForm();
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
                await Idm.Start(url, savePath, chunks);
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
                await IdmExtensions.StartWithProgress(url, savePath, chunks, cancellationToken, progressCallback);
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
    }
}