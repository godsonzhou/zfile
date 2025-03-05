using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;

namespace FluentFTP {
    /// <summary>
    /// FTP管理器类，用于管理FTP连接和操作
    /// 提供连接管理、文件操作等功能
    /// </summary>
    public class FTPMGR {
        #region 属性

        /// <summary>
        /// 存储FTP连接配置的字典
        /// </summary>
        private Dictionary<string, FtpConnectionInfo> _connections;

        /// <summary>
        /// 当前活动的FTP客户端
        /// </summary>
        private FtpClient _activeClient;

        /// <summary>
        /// 获取当前活动的FTP客户端
        /// </summary>
        public FtpClient ActiveClient => _activeClient;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化FTP管理器
        /// </summary>
        public FTPMGR() {
            _connections = new Dictionary<string, FtpConnectionInfo>();
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 连接到FTP服务器
        /// </summary>
        /// <param name="connectionName">连接名称</param>
        /// <returns>是否连接成功</returns>
        public bool Connect(string connectionName) {
            if (!_connections.ContainsKey(connectionName)) {
                throw new ArgumentException($"连接 {connectionName} 不存在");
            }

            var connectionInfo = _connections[connectionName];
            try {
                // 如果已有活动连接，先断开
                if (_activeClient != null && _activeClient.IsConnected) {
                    _activeClient.Disconnect();
                }

                // 创建新的FTP客户端
                _activeClient = new FtpClient(
                    connectionInfo.Host,
                    connectionInfo.Credentials,
                    connectionInfo.Port,
                    connectionInfo.Config,
                    connectionInfo.Logger
                );

                // 设置加密模式
                if (connectionInfo.EncryptionMode.HasValue) {
                    _activeClient.Config.EncryptionMode = connectionInfo.EncryptionMode.Value;
                }

                // 连接到服务器
                _activeClient.Connect();
                return _activeClient.IsConnected;
            }
            catch (Exception ex) {
                Console.WriteLine($"连接失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 新建FTP连接配置
        /// </summary>
        /// <param name="name">连接名称</param>
        /// <param name="host">主机地址</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="port">端口号，默认为21</param>
        /// <param name="encryptionMode">加密模式，默认为None</param>
        /// <returns>是否创建成功</returns>
        public bool CreateConnection(string name, string host, string username, string password, int port = 21, FtpEncryptionMode? encryptionMode = null) {
            if (_connections.ContainsKey(name)) {
                return false; // 连接名已存在
            }

            var connectionInfo = new FtpConnectionInfo {
                Name = name,
                Host = host,
                Credentials = new NetworkCredential(username, password),
                Port = port,
                EncryptionMode = encryptionMode
            };

            _connections.Add(name, connectionInfo);
            return true;
        }

        /// <summary>
        /// 新建URL连接
        /// </summary>
        /// <param name="name">连接名称</param>
        /// <param name="url">FTP URL，格式如：ftp://username:password@host:port</param>
        /// <returns>是否创建成功</returns>
        public bool CreateUrlConnection(string name, string url) {
            try {
                Uri uri = new Uri(url);
                if (uri.Scheme != "ftp") {
                    return false;
                }

                string host = uri.Host;
                int port = uri.Port > 0 ? uri.Port : 21;
                string username = "anonymous";
                string password = "anonymous@";

                if (!string.IsNullOrEmpty(uri.UserInfo)) {
                    string[] userInfo = uri.UserInfo.Split(':');
                    username = userInfo[0];
                    if (userInfo.Length > 1) {
                        password = userInfo[1];
                    }
                }

                return CreateConnection(name, host, username, password, port);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// 复制连接配置
        /// </summary>
        /// <param name="sourceName">源连接名称</param>
        /// <param name="targetName">目标连接名称</param>
        /// <returns>是否复制成功</returns>
        public bool CopyConnection(string sourceName, string targetName) {
            if (!_connections.ContainsKey(sourceName) || _connections.ContainsKey(targetName)) {
                return false;
            }

            var source = _connections[sourceName];
            var target = new FtpConnectionInfo {
                Name = targetName,
                Host = source.Host,
                Credentials = new NetworkCredential(source.Credentials.UserName, source.Credentials.Password),
                Port = source.Port,
                Config = source.Config?.Clone(),
                EncryptionMode = source.EncryptionMode,
                Logger = source.Logger
            };

            _connections.Add(targetName, target);
            return true;
        }

        /// <summary>
        /// 编辑连接配置
        /// </summary>
        /// <param name="name">连接名称</param>
        /// <param name="host">主机地址</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="port">端口号</param>
        /// <param name="encryptionMode">加密模式</param>
        /// <returns>是否编辑成功</returns>
        public bool EditConnection(string name, string host = null, string username = null, string password = null, int? port = null, FtpEncryptionMode? encryptionMode = null) {
            if (!_connections.ContainsKey(name)) {
                return false;
            }

            var connection = _connections[name];

            if (host != null) {
                connection.Host = host;
            }

            if (username != null && password != null) {
                connection.Credentials = new NetworkCredential(username, password);
            }
            else if (username != null) {
                connection.Credentials = new NetworkCredential(username, connection.Credentials.Password);
            }
            else if (password != null) {
                connection.Credentials = new NetworkCredential(connection.Credentials.UserName, password);
            }

            if (port.HasValue) {
                connection.Port = port.Value;
            }

            if (encryptionMode.HasValue) {
                connection.EncryptionMode = encryptionMode;
            }

            return true;
        }

        /// <summary>
        /// 删除连接配置
        /// </summary>
        /// <param name="name">连接名称</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteConnection(string name) {
            if (!_connections.ContainsKey(name)) {
                return false;
            }

            // 如果是当前活动连接，先断开
            if (_activeClient != null && _activeClient.IsConnected && 
                _connections[name].Host == _activeClient.Host && 
                _connections[name].Credentials.UserName == _activeClient.Credentials.UserName) {
                _activeClient.Disconnect();
                _activeClient = null;
            }

            return _connections.Remove(name);
        }

        /// <summary>
        /// 设置连接加密
        /// </summary>
        /// <param name="name">连接名称</param>
        /// <param name="encryptionMode">加密模式</param>
        /// <returns>是否设置成功</returns>
        public bool SetEncryption(string name, FtpEncryptionMode encryptionMode) {
            if (!_connections.ContainsKey(name)) {
                return false;
            }

            _connections[name].EncryptionMode = encryptionMode;

            // 如果是当前活动连接，更新加密设置
            if (_activeClient != null && _activeClient.IsConnected && 
                _connections[name].Host == _activeClient.Host && 
                _connections[name].Credentials.UserName == _activeClient.Credentials.UserName) {
                _activeClient.Config.EncryptionMode = encryptionMode;
            }

            return true;
        }

        /// <summary>
        /// 关闭当前连接
        /// </summary>
        public void CloseConnection() {
            if (_activeClient != null && _activeClient.IsConnected) {
                _activeClient.Disconnect();
            }
            _activeClient = null;
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 创建远程文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>是否创建成功</returns>
        public bool CreateDirectory(string path) {
            if (_activeClient == null || !_activeClient.IsConnected) {
                return false;
            }

            try {
                _activeClient.CreateDirectory(path);
                return true;
            }
            catch {
                return false;
            }
        }

        #endregion

        #region 辅助类

        /// <summary>
        /// FTP连接信息类
        /// </summary>
        private class FtpConnectionInfo {
            /// <summary>
            /// 连接名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 主机地址
            /// </summary>
            public string Host { get; set; }

            /// <summary>
            /// 凭证（用户名和密码）
            /// </summary>
            public NetworkCredential Credentials { get; set; }

            /// <summary>
            /// 端口号
            /// </summary>
            public int Port { get; set; } = 21;

            /// <summary>
            /// FTP配置
            /// </summary>
            public FtpConfig Config { get; set; }

            /// <summary>
            /// 加密模式
            /// </summary>
            public FtpEncryptionMode? EncryptionMode { get; set; }

            /// <summary>
            /// 日志记录器
            /// </summary>
            public IFtpLogger Logger { get; set; }
        }

        #endregion
    }
}