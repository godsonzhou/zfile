using System;
using System.Collections.Generic;
using System.IO;
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
        /// <param name="progressCallback">进度回调，参数为：进度百分比、下载速度(bytes/s)、文件总大小</param>
        /// <returns>下载任务</returns>
        public static async Task StartDownloadWithProgress(string url, string savePath, int chunks = 4, 
            CancellationToken cancellationToken = default, 
            Action<double, double, long> progressCallback = null)
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
    }
}