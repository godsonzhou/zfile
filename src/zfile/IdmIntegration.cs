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
                if (!File.Exists(manifestPath))
                {
                    // 如果清单文件不存在，从资源中提取
                    string manifestTemplate = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome_host_manifest.json"));
                    manifestTemplate = manifestTemplate.Replace("ZFILE_PATH_PLACEHOLDER", appPath.Replace("\\", "\\\\"));
                    
                    // 保存清单文件
                    File.WriteAllText(manifestPath, manifestTemplate);
                }
                
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
                if (string.IsNullOrEmpty(savePath))
                {
                    // 弹出新建下载对话框
                    using (var dialog = new NewDownloadDialog())
                    {
                        // 预填充URL
                        if (!string.IsNullOrEmpty(url))
                        {
                            typeof(NewDownloadDialog).GetProperty("Url").SetValue(dialog, url, null);
                        }

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // 对话框会处理下载
                        }
                    }
                }
                else
                {
                    // 检查是否有额外的HTTP头或Cookies
                    if (headers != null || !string.IsNullOrEmpty(cookies) || !string.IsNullOrEmpty(referrer))
                    {
                        // 使用带HTTP头和Cookies的下载方法
                        IdmManager.StartDownloadWithHeaders(url, savePath, headers, cookies, referrer).ConfigureAwait(false);
                    }
                    else
                    {
                        // 直接开始下载
                        IdmManager.StartDownload(url, savePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}