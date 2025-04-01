using System;
using System.Windows.Forms;
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
        /// 使用IDM下载指定URL的文件
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径，如果为null则弹出对话框让用户选择</param>
        public static void DownloadFile(string url, string savePath = null)
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
                    // 直接开始下载
                    IdmManager.StartDownload(url, savePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}