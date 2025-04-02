using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using static MonoTorrent.Factories;
using MonoTorrent.Trackers;
using System.Text;

namespace Zfile.Forms
{
    /// <summary>
    /// 种子下载管理器，提供磁力链接和种子文件下载功能
    /// </summary>
    public static class TorrentManager
    {
        // 默认下载目录
        private static string DefaultDownloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads");

        // 种子文件和磁力链接的临时存储目录
        private static string TorrentCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "Zfile", "TorrentCache");

        // 客户端引擎实例
        private static ClientEngine _engine;

        // 活动的种子下载任务
        private static Dictionary<string, MonoTorrent.Client.TorrentManager> _activeTorrents = 
            new Dictionary<string, MonoTorrent.Client.TorrentManager>();

        // 种子下载进度回调字典
        private static Dictionary<string, Action<double, double, long, Dictionary<long, long>>> _progressCallbacks = 
            new Dictionary<string, Action<double, double, long, Dictionary<long, long>>>();

        /// <summary>
        /// 初始化种子下载引擎
        /// </summary>
        public static async Task InitializeAsync()
        {
            try
            {
                // 确保目录存在
                Directory.CreateDirectory(DefaultDownloadDirectory);
                Directory.CreateDirectory(TorrentCacheDirectory);

                // 创建引擎设置
                var engineSettings = new EngineSettingsBuilder
                {
                    AllowPortForwarding = true,
                    AutoSaveLoadDhtCache = true,
                    AutoSaveLoadFastResume = true,
                    // ListenPort = 55123, // 可以设置为随机端口
                    MaximumConnections = 200,
                    //MaximumDownloadSpeed = 0, // 无限制
                    //MaximumUploadSpeed = 0,   // 无限制
                    MaximumOpenFiles = 20
                }.ToSettings();

                // 初始化引擎
                _engine = new ClientEngine(engineSettings);
                //_engine.Settings.ListenPort = 55123;
                // 启动DHT服务
                //await _engine.Dht.//StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化BT下载引擎失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从磁力链接添加下载任务
        /// </summary>
        /// <param name="magnetLink">磁力链接</param>
        /// <param name="savePath">保存路径，如果为null则使用默认下载目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>下载任务ID</returns>
        public static async Task<string> AddMagnetLinkAsync(
            string magnetLink, 
            string savePath = null, 
            CancellationToken cancellationToken = default,
            Action<double, double, long, Dictionary<long, long>> progressCallback = null)
        {
            try
            {
                // 确保引擎已初始化
                if (_engine == null)
                    await InitializeAsync();

				// 解析磁力链接
				//var magnetLinkParser = new MagnetLinkParser();
				var parsedLink = MagnetLink.Parse(magnetLink);//magnetLinkParser.Parse(magnetLink);

                // 设置保存路径
                string downloadDirectory = string.IsNullOrEmpty(savePath) 
                    ? DefaultDownloadDirectory 
                    : Path.GetDirectoryName(savePath);

                // 创建种子设置
                var torrentSettings = new TorrentSettingsBuilder
                {
                    MaximumConnections = 60,
                    UploadSlots = 10,
                    CreateContainingDirectory = true
                }.ToSettings();

                // 创建种子管理器
                var manager = await _engine.AddAsync(parsedLink, downloadDirectory, torrentSettings);

                // 生成唯一ID
                string torrentId = manager.InfoHashes.V1.ToHex();

                // 保存管理器和回调
                _activeTorrents[torrentId] = manager;
                if (progressCallback != null)
                    _progressCallbacks[torrentId] = progressCallback;

                // 注册事件
                RegisterTorrentEvents(manager);

                // 开始下载
                await manager.StartAsync();

                return torrentId;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加磁力链接下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 从种子文件添加下载任务
        /// </summary>
        /// <param name="torrentFilePath">种子文件路径</param>
        /// <param name="savePath">保存路径，如果为null则使用默认下载目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>下载任务ID</returns>
        public static async Task<string> AddTorrentFileAsync(
            string torrentFilePath, 
            string savePath = null, 
            CancellationToken cancellationToken = default,
            Action<double, double, long, Dictionary<long, long>> progressCallback = null)
        {
            try
            {
                // 确保引擎已初始化
                if (_engine == null)
                    await InitializeAsync();

                // 加载种子文件
                var torrent = await Torrent.LoadAsync(torrentFilePath);

                // 设置保存路径
                string downloadDirectory = string.IsNullOrEmpty(savePath) 
                    ? DefaultDownloadDirectory 
                    : Path.GetDirectoryName(savePath);

                // 创建种子设置
                var torrentSettings = new TorrentSettingsBuilder
                {
                    MaximumConnections = 60,
                    UploadSlots = 10,
                    CreateContainingDirectory = true
                }.ToSettings();

                // 创建种子管理器
                var manager = await _engine.AddAsync(torrent, downloadDirectory, torrentSettings);

                // 生成唯一ID
                string torrentId = manager.InfoHashes.V1.ToHex();

                // 保存管理器和回调
                _activeTorrents[torrentId] = manager;
                if (progressCallback != null)
                    _progressCallbacks[torrentId] = progressCallback;

                // 注册事件
                RegisterTorrentEvents(manager);

                // 开始下载
                await manager.StartAsync();

                return torrentId;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加种子文件下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 暂停种子下载
        /// </summary>
        /// <param name="torrentId">种子ID</param>
        public static async Task PauseTorrentAsync(string torrentId)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager))
            {
                await manager.PauseAsync();
            }
        }

        /// <summary>
        /// 恢复种子下载
        /// </summary>
        /// <param name="torrentId">种子ID</param>
        public static async Task ResumeTorrentAsync(string torrentId)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager))
            {
                await manager.StartAsync();
            }
        }

        /// <summary>
        /// 停止并移除种子下载
        /// </summary>
        /// <param name="torrentId">种子ID</param>
        /// <param name="deleteFiles">是否删除已下载的文件</param>
        public static async Task RemoveTorrentAsync(string torrentId, bool deleteFiles = false)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager))
            {
                await _engine.RemoveAsync(manager, deleteFiles ? RemoveMode.CacheDataAndDownloadedData : RemoveMode.CacheDataOnly);
                _activeTorrents.Remove(torrentId);
                _progressCallbacks.Remove(torrentId);
            }
        }

        /// <summary>
        /// 获取种子下载信息
        /// </summary>
        /// <param name="torrentId">种子ID</param>
        /// <returns>种子信息</returns>
        public static TorrentInfo GetTorrentInfo(string torrentId)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager))
            {
                return new TorrentInfo
                {
                    Name = manager.Torrent?.Name ?? "未知",
                    Size = manager.Torrent?.Size ?? 0,
                    DownloadedBytes = manager.Monitor.DataBytesDownloaded,
                    UploadedBytes = manager.Monitor.DataBytesUploaded,
                    Progress = manager.Progress,
                    DownloadSpeed = manager.Monitor.DownloadSpeed,
                    UploadSpeed = manager.Monitor.UploadSpeed,
                    State = manager.State.ToString(),
                    Peers = manager.Peers.Available,
                    Seeds = manager.Peers.Seeds,
                    Leechs = manager.Peers.Leechs,
                    Files = manager.Files?.Select(f => new TorrentFileInfo
                    {
                        Path = f.Path,
                        Length = f.Length,
                        Priority = f.Priority.ToString(),
                        Progress = f.BitField.PercentComplete
                    }).ToList() ?? new List<TorrentFileInfo>()
                };
            }

            return null;
        }

        /// <summary>
        /// 获取种子下载详细信息，包括Peers、Trackers和DHT节点等信息
        /// </summary>
        /// <param name="torrentId">种子ID</param>
        /// <returns>种子详细信息</returns>
        public static TorrentDetailedInfo GetDetailedTorrentInfo(string torrentId)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager))
            {
                var detailedInfo = new TorrentDetailedInfo
                {
                    Name = manager.Torrent?.Name ?? "未知",
                    Size = manager.Torrent?.Size ?? 0,
                    DownloadedBytes = manager.Monitor.DataBytesDownloaded,
                    UploadedBytes = manager.Monitor.DataBytesUploaded,
                    Progress = manager.Progress,
                    DownloadSpeed = manager.Monitor.DownloadSpeed,
                    UploadSpeed = manager.Monitor.UploadSpeed,
                    State = manager.State.ToString(),
                    InfoHash = torrentId,
                    CreatedTime = DateTime.Now, // 这里应该从任务创建时保存
                    DhtStatus = _engine.Dht.State.ToString()
                };

                // 添加Peers信息
                foreach (var peer in manager.Peers.ConnectedPeers)
                {
                    detailedInfo.Peers.Add(new PeerInfo
                    {
                        Address = peer.Uri.ToString(),
                        ClientSoftware = peer.ClientApp.Client,
                        DownloadSpeed = peer.Monitor.DownloadSpeed,
                        UploadSpeed = peer.Monitor.UploadSpeed,
                        Progress = peer.AmRequestingPiecesCount > 0 ? 
                            (double)peer.AmRequestingPiecesCount / manager.Torrent.PieceCount * 100 : 0,
                        Status = peer.ConnectionDirection.ToString(),
                        IsSeeder = peer.IsSeeder,
                        ConnectedTime = DateTime.Now // 这里应该从连接时保存
                    });
                }

                // 添加Trackers信息
                foreach (var tracker in manager.TrackerManager.Trackers)
                {
                    detailedInfo.Trackers.Add(new TrackerInfo
                    {
                        Url = tracker.Uri.ToString(),
                        Status = tracker.Status.ToString(),
                        LastUpdated = DateTime.Now, // 这里应该从更新时保存
                        Seeds = tracker.Announces.Count > 0 ? tracker.Announces.Last().Complete : 0,
                        Peers = tracker.Announces.Count > 0 ? tracker.Announces.Last().Incomplete : 0,
                        NextUpdate = DateTime.Now.AddSeconds(tracker.UpdateInterval.TotalSeconds),
                        WarningMessage = tracker.WarningMessage,
                        ErrorMessage = tracker.FailureMessage
                    });
                }

                // 添加文件信息
                if (manager.Files != null)
                {
                    foreach (var file in manager.Files)
                    {
                        detailedInfo.Files.Add(new TorrentFileInfo
                        {
                            Path = file.Path,
                            Length = file.Length,
                            Priority = file.Priority.ToString(),
                            Progress = file.BitField.PercentComplete
                        });
                    }
                }

                // 添加DHT节点信息（示例数据，实际需要从DHT引擎获取）
                try
                {
                    // 获取DHT节点数量
                    int dhtNodeCount = _engine.Dht.NodeCount;
                    detailedInfo.DhtNodes.Add(new DhtNodeInfo
                    {
                        Address = "DHT节点总数",
                        Status = dhtNodeCount.ToString(),
                        LastSeen = DateTime.Now,
                        NodeId = "N/A"
                    });

                    // 添加调试信息
                    StringBuilder debugInfo = new StringBuilder();
                    debugInfo.AppendLine($"引擎状态: {_engine.IsRunning}");
                    debugInfo.AppendLine($"DHT状态: {_engine.Dht.State}");
                    debugInfo.AppendLine($"DHT节点数: {dhtNodeCount}");
                    //debugInfo.AppendLine($"监听端口: {_engine.Settings.ListenPort}");
                    debugInfo.AppendLine($"最大连接数: {_engine.Settings.MaximumConnections}");
                    //debugInfo.AppendLine($"下载速度限制: {(_engine.Settings.MaximumDownloadSpeed == 0 ? "无限制" : $"{_engine.Settings.MaximumDownloadSpeed / 1024} KB/s")}");
                    //debugInfo.AppendLine($"上传速度限制: {(_engine.Settings.MaximumUploadSpeed == 0 ? "无限制" : $"{_engine.Settings.MaximumUploadSpeed / 1024} KB/s")}");
                    debugInfo.AppendLine($"当前下载速度: {manager.Monitor.DownloadSpeed / 1024:F2} KB/s");
                    debugInfo.AppendLine($"当前上传速度: {manager.Monitor.UploadSpeed / 1024:F2} KB/s");
                    debugInfo.AppendLine($"已下载数据: {manager.Monitor.DataBytesDownloaded / (1024 * 1024):F2} MB");
                    debugInfo.AppendLine($"已上传数据: {manager.Monitor.DataBytesUploaded / (1024 * 1024):F2} MB");
                    debugInfo.AppendLine($"协议下载: {manager.Monitor.ProtocolBytesDownloaded / 1024:F2} KB");
                    debugInfo.AppendLine($"协议上传: {manager.Monitor.ProtocolBytesUploaded / 1024:F2} KB");
                    //debugInfo.AppendLine($"废弃数据: {manager.Monitor..WastedBytes / 1024:F2} KB");
                    //debugInfo.AppendLine($"已接收块: {manager.Monitor.BlocksReceived}");
                    //debugInfo.AppendLine($"已发送块: {manager.Monitor.BlocksSent}");
                    //debugInfo.AppendLine($"已接收消息: {manager.Monitor.MessagesReceived}");
                    //debugInfo.AppendLine($"已发送消息: {manager.Monitor.MessagesSent}");
                    debugInfo.AppendLine($"开始时间: {manager.StartTime}");
                    debugInfo.AppendLine($"完成哈希: {manager.Complete}");
                    debugInfo.AppendLine($"哈希失败: {manager.HashFails}");
                    debugInfo.AppendLine($"已完成片段: {manager.Bitfield.TrueCount} / {manager.Bitfield.Length}");
                    //debugInfo.AppendLine($"连接的Peers: {manager.Peers.ConnectedPeers.Count}");
                    debugInfo.AppendLine($"半开连接: {manager.OpenConnections}");
                    //debugInfo.AppendLine($"已下载片段: {manager.PieceManager.CurrentRequestCount}");

                    detailedInfo.DebugInfo = debugInfo.ToString();
                }
                catch (Exception ex)
                {
                    detailedInfo.ErrorMessage = $"获取DHT信息失败: {ex.Message}";
                }

                return detailedInfo;
            }

            return null;
        }

        /// <summary>
        /// 注册种子事件
        /// </summary>
        private static void RegisterTorrentEvents(MonoTorrent.Client.TorrentManager manager)
        {
            string torrentId = manager.InfoHashes.V1.ToHex();

            // 进度更新事件
            manager.PieceHashed += (sender, e) =>
            {
                UpdateProgress(torrentId);
            };

            // 下载完成事件
            manager.TorrentStateChanged += (sender, e) =>
            {
                if (e.NewState == TorrentState.Seeding || e.NewState == TorrentState.Stopped)
                {
                    UpdateProgress(torrentId);
                }
            };
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        private static void UpdateProgress(string torrentId)
        {
            if (_activeTorrents.TryGetValue(torrentId, out var manager) && 
                _progressCallbacks.TryGetValue(torrentId, out var callback))
            {
                // 计算进度
                double progress = manager.Progress;
                double speed = manager.Monitor.DownloadSpeed;
                long totalSize = manager.Torrent?.Size ?? 0;

                // 创建分块进度字典（简化处理）
                var chunksProgress = new Dictionary<long, long>();
                for (int i = 0; i < manager.Bitfield.Length; i++)
                {
                    if (manager.Bitfield[i])
                    {
                        chunksProgress[i] = 1;
                    }
                    else
                    {
                        chunksProgress[i] = 0;
                    }
                }

                // 调用回调
                callback(progress, speed, totalSize, chunksProgress);
            }
        }

        /// <summary>
        /// 关闭引擎
        /// </summary>
        public static async Task ShutdownAsync()
        {
            if (_engine != null)
            {
                // 停止所有活动的种子
                foreach (var manager in _activeTorrents.Values)
                {
                    await manager.StopAsync();
                }

                // 关闭引擎
                await _engine.StopAllAsync();
                _engine.Dispose();
                _engine = null;
            }
        }
    }

    /// <summary>
    /// 种子信息类
    /// </summary>
    public class TorrentInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public long DownloadedBytes { get; set; }
        public long UploadedBytes { get; set; }
        public double Progress { get; set; }
        public double DownloadSpeed { get; set; }
        public double UploadSpeed { get; set; }
        public string State { get; set; }
        public int Peers { get; set; }
        public int Seeds { get; set; }
        public int Leechs { get; set; }
        public List<TorrentFileInfo> Files { get; set; }
    }

    /// <summary>
    /// 种子文件信息类
    /// </summary>
    public class TorrentFileInfo
    {
        public string Path { get; set; }
        public long Length { get; set; }
        public string Priority { get; set; }
        public double Progress { get; set; }
    }
}