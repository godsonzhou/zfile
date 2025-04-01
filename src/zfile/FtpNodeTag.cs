using System.Net;
using FluentFTP;
namespace Zfile
{
	public class ArchNodeTag
	{
		public IntPtr Handler;
		public string Path;
	}
    /// <summary>
    /// FTP节点标签类，用于存储FTP节点的相关信息
    /// </summary>
    public class FtpNodeTag
    {
        /// <summary>
        /// 获取或设置FTP连接名称
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// 获取或设置FTP路径
        /// </summary>
        public string Path { get; set; }
    }
	public class FtpRootNodeTag(string name)
	{
		public string Name { get; set; } = name;
	
	}
	public class FtpConnectionConfig
	{
		public string SessionName { get; set; } = string.Empty;
		public string HostName { get; set; } = string.Empty;
		public int Port { get; set; } = 21;
		public bool UseSsl { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string RemoteDirectory { get; set; } = "/";
		public string LocalDirectory { get; set; } = string.Empty;
		public bool UsePassiveMode { get; set; } = true;
		public bool UseFirewall { get; set; }
	}
	public class FtpConnectionInfo
	{
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
	
}