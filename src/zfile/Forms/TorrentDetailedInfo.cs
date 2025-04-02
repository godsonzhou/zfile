using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zfile.Forms
{
    /// <summary>
    /// 种子下载详细信息类，用于提供种子下载的详细状态和调试信息
    /// </summary>
    public class TorrentDetailedInfo
    {
        /// <summary>
        /// 种子名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 种子大小
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 已下载字节数
        /// </summary>
        public long DownloadedBytes { get; set; }

        /// <summary>
        /// 已上传字节数
        /// </summary>
        public long UploadedBytes { get; set; }

        /// <summary>
        /// 下载进度（百分比）
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 下载速度（字节/秒）
        /// </summary>
        public double DownloadSpeed { get; set; }

        /// <summary>
        /// 上传速度（字节/秒）
        /// </summary>
        public double UploadSpeed { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 连接的Peers列表
        /// </summary>
        public List<PeerInfo> Peers { get; set; } = new List<PeerInfo>();

        /// <summary>
        /// Trackers列表
        /// </summary>
        public List<TrackerInfo> Trackers { get; set; } = new List<TrackerInfo>();

        /// <summary>
        /// 文件列表
        /// </summary>
        public List<TorrentFileInfo> Files { get; set; } = new List<TorrentFileInfo>();

        /// <summary>
        /// DHT状态
        /// </summary>
        public string DhtStatus { get; set; }

        /// <summary>
        /// DHT节点列表
        /// </summary>
        public List<DhtNodeInfo> DhtNodes { get; set; } = new List<DhtNodeInfo>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedTime { get; set; }

        /// <summary>
        /// 下载用时
        /// </summary>
        public TimeSpan DownloadTime
        {
            get
            {
                if (CompletedTime.HasValue)
                    return CompletedTime.Value - CreatedTime;
                else
                    return DateTime.Now - CreatedTime;
            }
        }

        /// <summary>
        /// 磁力链接或种子文件路径
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 是否是磁力链接
        /// </summary>
        public bool IsMagnetLink { get; set; }

        /// <summary>
        /// 保存路径
        /// </summary>
        public string SavePath { get; set; }

        /// <summary>
        /// 哈希值
        /// </summary>
        public string InfoHash { get; set; }

        /// <summary>
        /// 调试信息
        /// </summary>
        public string DebugInfo { get; set; }
    }

    /// <summary>
    /// Peer信息类
    /// </summary>
    public class PeerInfo
    {
        /// <summary>
        /// Peer地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 客户端软件
        /// </summary>
        public string ClientSoftware { get; set; }

        /// <summary>
        /// 下载速度（字节/秒）
        /// </summary>
        public double DownloadSpeed { get; set; }

        /// <summary>
        /// 上传速度（字节/秒）
        /// </summary>
        public double UploadSpeed { get; set; }

        /// <summary>
        /// 下载进度（百分比）
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否是种子
        /// </summary>
        public bool IsSeeder { get; set; }

        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectedTime { get; set; }
    }

    /// <summary>
    /// Tracker信息类
    /// </summary>
    public class TrackerInfo
    {
        /// <summary>
        /// Tracker URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 做种数
        /// </summary>
        public int Seeds { get; set; }

        /// <summary>
        /// Peer数
        /// </summary>
        public int Peers { get; set; }

        /// <summary>
        /// 下次更新时间
        /// </summary>
        public DateTime NextUpdate { get; set; }

        /// <summary>
        /// 警告信息
        /// </summary>
        public string WarningMessage { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// DHT节点信息类
    /// </summary>
    public class DhtNodeInfo
    {
        /// <summary>
        /// 节点地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; }
    }
}