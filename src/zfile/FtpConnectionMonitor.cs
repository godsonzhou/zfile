using FluentFTP;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;
namespace zfile
{
    /// <summary>
    /// FTP连接监视器，用于监控FTP连接状态并处理被动断开的情况
    /// </summary>
    public class FtpConnectionMonitor
    {
        private readonly FTPMGR _ftpManager;
        private readonly Dictionary<string, FtpMonitorItem> _monitoredConnections;
        private readonly Timer _checkTimer;
        private readonly int _checkInterval = 30000; // 默认30秒检查一次
        private bool _isDisposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ftpManager">FTP管理器实例</param>
        /// <param name="checkIntervalMs">检查间隔（毫秒）</param>
        public FtpConnectionMonitor(FTPMGR ftpManager, int checkIntervalMs = 30000)
        {
            _ftpManager = ftpManager;
            _monitoredConnections = new Dictionary<string, FtpMonitorItem>();
            _checkInterval = checkIntervalMs;
            
            // 创建定时器，定期检查连接状态
            _checkTimer = new Timer();
			_checkTimer.Interval = checkIntervalMs;
        }

        /// <summary>
        /// 添加要监控的FTP连接
        /// </summary>
        /// <param name="connectionName">连接名称</param>
        /// <param name="client">FTP客户端实例</param>
        public void AddConnection(string connectionName, FtpClient client)
        {
            if (!_monitoredConnections.ContainsKey(connectionName))
            {
                _monitoredConnections.Add(connectionName, new FtpMonitorItem
                {
                    ConnectionName = connectionName,
                    Client = client,
                    LastCheckTime = DateTime.Now
                });
                
                Debug.Print($"已添加FTP连接监控: {connectionName}");
            }
        }

        /// <summary>
        /// 移除监控的FTP连接
        /// </summary>
        /// <param name="connectionName">连接名称</param>
        public void RemoveConnection(string connectionName)
        {
            if (_monitoredConnections.ContainsKey(connectionName))
            {
                _monitoredConnections.Remove(connectionName);
                Debug.Print($"已移除FTP连接监控: {connectionName}");
            }
        }

        /// <summary>
        /// 检查所有监控的FTP连接状态
        /// </summary>
        private void CheckConnectionsStatus(object state)
        {
            if (_isDisposed) return;

            List<string> disconnectedConnections = new List<string>();

            foreach (var item in _monitoredConnections)
            {
                string connectionName = item.Key;
                FtpMonitorItem monitorItem = item.Value;

                try
                {
                    // 检查连接是否仍然活跃
                    bool isConnected = IsConnectionActive(monitorItem.Client);
                    monitorItem.LastCheckTime = DateTime.Now;

                    if (!isConnected)
                    {
                        Debug.Print($"检测到FTP连接已被动断开: {connectionName}");
                        disconnectedConnections.Add(connectionName);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"检查FTP连接状态时出错: {connectionName}, {ex.Message}");
                    disconnectedConnections.Add(connectionName);
                }
            }

            // 处理已断开的连接
            foreach (string connectionName in disconnectedConnections)
            {
                HandleDisconnectedConnection(connectionName);
            }
        }

        /// <summary>
        /// 检查FTP连接是否仍然活跃
        /// </summary>
        /// <param name="client">FTP客户端实例</param>
        /// <returns>连接是否活跃</returns>
        private bool IsConnectionActive(FtpClient client)
        {
            if (client == null) return false;

            // 首先检查基本连接状态
            if (!client.IsConnected) return false;

            try
            {
                // 使用IsStillConnected方法进行更彻底的检查
                return client.IsStillConnected(5000); // 5秒超时
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理已断开的FTP连接
        /// </summary>
        /// <param name="connectionName">连接名称</param>
        private void HandleDisconnectedConnection(string connectionName)
        {
            try
            {
                // 从监控列表中移除
                RemoveConnection(connectionName);

                // 调用FTP管理器的UnregisterFtpConnection方法注销连接
                _ftpManager.UnregisterFtpConnection(connectionName);

                // 更新UI状态
                _ftpManager.form.Invoke(new Action(() =>
                {
                    _ftpManager.form.uiManager.ftpController.UpdateStatus(false);
                    MessageBox.Show($"FTP连接 '{connectionName}' 已断开，可能是由于网络问题或服务器超时。", 
                        "FTP连接断开", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            catch (Exception ex)
            {
                Debug.Print($"处理断开的FTP连接时出错: {connectionName}, {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _checkTimer?.Dispose();
                _monitoredConnections.Clear();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// FTP监控项
        /// </summary>
        private class FtpMonitorItem
        {
            public string ConnectionName { get; set; }
            public FtpClient Client { get; set; }
            public DateTime LastCheckTime { get; set; }
        }
    }
}