using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;
using Zfile.Forms;

namespace Zfile
{
    /// <summary>
    /// 种子下载任务类，继承自DownloadTask，用于处理磁力链接和种子文件下载
    /// </summary>
    public class TorrentDownloadTask : DownloadTask
    {
        /// <summary>
        /// 种子ID，用于在TorrentManager中标识该下载任务
        /// </summary>
        public string TorrentId { get; set; }

        /// <summary>
        /// 是否是磁力链接
        /// </summary>
        public bool IsMagnetLink { get; set; }

        /// <summary>
        /// 种子文件路径（如果是种子文件下载）
        /// </summary>
        public string TorrentFilePath { get; set; }

        /// <summary>
        /// 选中下载的文件索引列表
        /// </summary>
        public List<int> SelectedFileIndices { get; set; } = new List<int>();

        /// <summary>
        /// 种子信息
        /// </summary>
        public TorrentInfo TorrentInfo { get; set; }

        /// <summary>
        /// 上传速度（字节/秒）
        /// </summary>
        public double UploadSpeed { get; set; }

        /// <summary>
        /// 已上传字节数
        /// </summary>
        public long UploadedBytes { get; set; }

        /// <summary>
        /// 连接的Peer数量
        /// </summary>
        public int Peers { get; set; }

        /// <summary>
        /// 连接的做种用户数量
        /// </summary>
        public int Seeds { get; set; }

        /// <summary>
        /// 连接的下载用户数量
        /// </summary>
        public int Leechs { get; set; }

        /// <summary>
        /// 创建一个新的种子下载任务（磁力链接）
        /// </summary>
        /// <param name="magnetLink">磁力链接</param>
        /// <param name="savePath">保存路径</param>
        public TorrentDownloadTask(string magnetLink, string savePath)
        {
            Url = magnetLink;
            SavePath = savePath;
            FileName = Path.GetFileName(savePath);
            Status = DownloadStatus.Pending;
            CreatedTime = DateTime.Now;
            IsMagnetLink = true;
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 创建一个新的种子下载任务（种子文件）
        /// </summary>
        /// <param name="torrentFilePath">种子文件路径</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="isTorrentFile">是否是种子文件（如果为false则表示是磁力链接）</param>
        public TorrentDownloadTask(string torrentFilePath, string savePath, bool isTorrentFile)
        {
            if (isTorrentFile)
            {
                TorrentFilePath = torrentFilePath;
                Url = $"file://{torrentFilePath}";
                IsMagnetLink = false;
            }
            else
            {
                Url = torrentFilePath;
                IsMagnetLink = true;
            }

            SavePath = savePath;
            FileName = Path.GetFileName(savePath);
            Status = DownloadStatus.Pending;
            CreatedTime = DateTime.Now;
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 更新种子下载任务的状态和进度
        /// </summary>
        public void UpdateFromTorrentInfo()
        {
            if (TorrentInfo == null) return;

            Progress = TorrentInfo.Progress;
            Speed = TorrentInfo.DownloadSpeed;
            UploadSpeed = TorrentInfo.UploadSpeed;
            TotalSize = TorrentInfo.Size;
            UploadedBytes = TorrentInfo.UploadedBytes;
            Peers = TorrentInfo.Peers;
            Seeds = TorrentInfo.Seeds;
            Leechs = TorrentInfo.Leechs;

            // 根据种子状态更新下载状态
            if (TorrentInfo.State == "Seeding")
            {
                Status = DownloadStatus.Completed;
                Progress = 100;
            }
            else if (TorrentInfo.State == "Downloading")
            {
                Status = DownloadStatus.Downloading;
            }
            else if (TorrentInfo.State == "Paused" || TorrentInfo.State == "Stopped")
            {
                Status = DownloadStatus.Paused;
            }
            else if (TorrentInfo.State == "Error")
            {
                Status = DownloadStatus.Error;
            }
            else if (TorrentInfo.State == "Hashing")
            {
                Status = DownloadStatus.Downloading;
            }
            else if (TorrentInfo.State == "Metadata")
            {
                Status = DownloadStatus.Downloading;
            }
            else
            {
                Status = DownloadStatus.Pending;
            }
        }
    }
}