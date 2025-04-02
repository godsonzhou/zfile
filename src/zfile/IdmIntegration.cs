using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Zfile.Forms;

namespace Zfile
{
    /// <summary>
    /// IDM下载管理器集成类，提供与主程序的集成接口
    /// </summary>
    public static class IdmIntegration
    {
        /// <summary>
        /// 显示IDM下载管理器窗口
        /// </summary>
        public static void ShowIdmManager()
        {
            try
            {
                IdmManager.ShowIdmManager();
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
                
                // 替换扩展ID占位符 - 使用通配符允许任何扩展ID
                manifestTemplate = manifestTemplate.Replace("chrome-extension://EXTENSION_ID_PLACEHOLDER/", 
                    "chrome-extension://*/*");
                
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
        public static void DownloadFile(string url, string savePath = null, Dictionary<string, string> headers = null, string cookies = null, string referrer = null)
        {
            try
            {
                Debug.WriteLine($"开始下载: URL={url}, SavePath={savePath}, Headers={headers?.Count ?? 0}, Cookies={(cookies != null)}, Referrer={(referrer != null)}");
                
                if (string.IsNullOrEmpty(savePath))
                {
                    // 弹出新建下载对话框
                    using (var dialog = new IdmForm())
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
                        Task.Run(async () => {
                            try {
                                await IdmManager.StartDownloadWithHeaders(url, savePath, headers, cookies, referrer);
                                // 显示系统托盘通知
                                TrayIconManager.Instance.ShowBalloonTip("下载已开始", $"文件: {Path.GetFileName(savePath)}", ToolTipIcon.Info);
                            }
                            catch (Exception ex) {
                                Debug.WriteLine($"下载失败: {ex.Message}");
                                TrayIconManager.Instance.ShowBalloonTip("下载失败", ex.Message, ToolTipIcon.Error);
                            }
                        });
                    }
                    else
                    {
                        Debug.WriteLine("直接开始下载");
                        // 直接开始下载
                        Task.Run(async () => {
                            try {
                                await IdmManager.StartDownload(url, savePath);
                                // 显示系统托盘通知
                                TrayIconManager.Instance.ShowBalloonTip("下载已开始", $"文件: {Path.GetFileName(savePath)}", ToolTipIcon.Info);
                            }
                            catch (Exception ex) {
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