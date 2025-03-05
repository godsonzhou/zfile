using CmdProcessor;
using FluentFTP;
using System.Net;

namespace WinFormsApp1
{
	/// <summary>
	/// FTP管理器类，用于管理FTP连接和操作
	/// 提供连接管理、文件操作等功能
	/// </summary>
	public class FTPMGR
	{
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
		public FTPMGR()
		{
			_connections = new Dictionary<string, FtpConnectionInfo>();
		}

		#endregion

		#region 连接管理

		/// <summary>
		/// 连接到FTP服务器
		/// </summary>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否连接成功</returns>
		public bool Connect(string connectionName)
		{
			if (!_connections.ContainsKey(connectionName))
			{
				throw new ArgumentException($"连接 {connectionName} 不存在");
			}

			var connectionInfo = _connections[connectionName];
			try
			{
				// 如果已有活动连接，先断开
				if (_activeClient != null && _activeClient.IsConnected)
				{
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
				if (connectionInfo.EncryptionMode.HasValue)
				{
					_activeClient.Config.EncryptionMode = connectionInfo.EncryptionMode.Value;
				}

				// 连接到服务器
				_activeClient.Connect();
				return _activeClient.IsConnected;
			}
			catch (Exception ex)
			{
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
		public bool CreateConnection(string name, string host, string username, string password, int port = 21, FtpEncryptionMode? encryptionMode = null)
		{
			if (_connections.ContainsKey(name))
			{
				return false; // 连接名已存在
			}

			var connectionInfo = new FtpConnectionInfo
			{
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
		public bool CreateUrlConnection(string name, string url)
		{
			try
			{
				Uri uri = new Uri(url);
				if (uri.Scheme != "ftp")
				{
					return false;
				}

				string host = uri.Host;
				int port = uri.Port > 0 ? uri.Port : 21;
				string username = "anonymous";
				string password = "anonymous@";

				if (!string.IsNullOrEmpty(uri.UserInfo))
				{
					string[] userInfo = uri.UserInfo.Split(':');
					username = userInfo[0];
					if (userInfo.Length > 1)
					{
						password = userInfo[1];
					}
				}

				return CreateConnection(name, host, username, password, port);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 复制连接配置
		/// </summary>
		/// <param name="sourceName">源连接名称</param>
		/// <param name="targetName">目标连接名称</param>
		/// <returns>是否复制成功</returns>
		public bool CopyConnection(string sourceName, string targetName)
		{
			if (!_connections.ContainsKey(sourceName) || _connections.ContainsKey(targetName))
			{
				return false;
			}

			var source = _connections[sourceName];
			var target = new FtpConnectionInfo
			{
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
		public bool EditConnection(string name, string host = null, string username = null, string password = null, int? port = null, FtpEncryptionMode? encryptionMode = null)
		{
			if (!_connections.ContainsKey(name))
			{
				return false;
			}

			var connection = _connections[name];

			if (host != null)
			{
				connection.Host = host;
			}

			if (username != null && password != null)
			{
				connection.Credentials = new NetworkCredential(username, password);
			}
			else if (username != null)
			{
				connection.Credentials = new NetworkCredential(username, connection.Credentials.Password);
			}
			else if (password != null)
			{
				connection.Credentials = new NetworkCredential(connection.Credentials.UserName, password);
			}

			if (port.HasValue)
			{
				connection.Port = port.Value;
			}

			if (encryptionMode.HasValue)
			{
				connection.EncryptionMode = encryptionMode;
			}

			return true;
		}

		/// <summary>
		/// 删除连接配置
		/// </summary>
		/// <param name="name">连接名称</param>
		/// <returns>是否删除成功</returns>
		public bool DeleteConnection(string name)
		{
			if (!_connections.ContainsKey(name))
			{
				return false;
			}

			// 如果是当前活动连接，先断开
			if (_activeClient != null && _activeClient.IsConnected &&
				_connections[name].Host == _activeClient.Host &&
				_connections[name].Credentials.UserName == _activeClient.Credentials.UserName)
			{
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
		public bool SetEncryption(string name, FtpEncryptionMode encryptionMode)
		{
			if (!_connections.ContainsKey(name))
			{
				return false;
			}

			_connections[name].EncryptionMode = encryptionMode;

			// 如果是当前活动连接，更新加密设置
			if (_activeClient != null && _activeClient.IsConnected &&
				_connections[name].Host == _activeClient.Host &&
				_connections[name].Credentials.UserName == _activeClient.Credentials.UserName)
			{
				_activeClient.Config.EncryptionMode = encryptionMode;
			}

			return true;
		}

		/// <summary>
		/// 关闭当前连接
		/// </summary>
		public void CloseConnection()
		{
			if (_activeClient != null && _activeClient.IsConnected)
			{
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
		public bool CreateDirectory(string path)
		{
			if (_activeClient == null || !_activeClient.IsConnected)
			{
				return false;
			}

			try
			{
				_activeClient.CreateDirectory(path);
				return true;
			}
			catch
			{
				return false;
			}
		}
		// 添加新的私有方法来处理 FTP 连接管理器
		private void ShowFtpConnectionManager()
		{
			var form = new Form
			{
				Text = "FTP 连接管理器",
				Size = new Size(600, 500),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var listView = new ListView
			{
				Dock = DockStyle.Left,
				View = View.Details,
				FullRowSelect = true,
				Location = new Point(10, 10),
				Size = new Size(420, 300),
				MultiSelect = false
			};

			listView.Columns.Add("名称", 150);
			listView.Columns.Add("主机", 200);

			// 创建按钮面板
			var buttonPanel = new FlowLayoutPanel
			{
				Location = new Point(10, 320),
				Size = new Size(365, 130),
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true
			};
			var buttonWidth = 150;
			var btnConnect = new Button { Text = "连接(&C)", Width = buttonWidth };
			var btnNewConnection = new Button { Text = "新建连接(&N)...", Width = buttonWidth };
			var btnNewUrl = new Button { Text = "新建网址(&U)...", Width = buttonWidth };
			var btnCopyConnection = new Button { Text = "复制连接(&P)", Width = buttonWidth };
			var btnNewFolder = new Button { Text = "新建文件夹(&F)", Width = buttonWidth };
			var btnEditConnection = new Button { Text = "编辑连接(&E)", Width = buttonWidth };
			var btnDeleteConnection = new Button { Text = "删除连接(&D)", Width = buttonWidth };
			var btnEncrypt = new Button { Text = "加密(&Y)", Width = buttonWidth };
			var btnClose = new Button { Text = "关闭", Width = buttonWidth };

			// 添加按钮事件处理
			btnConnect.Click += (s, e) => {
				if (listView.SelectedItems.Count > 0)
				{
					var selectedItem = listView.SelectedItems[0];
					// TODO: 调用 FtpMgr.Connect 方法
					form.Close();
				}
				else
				{
					MessageBox.Show("请选择要连接的FTP站点", "提示");
				}
			};

			btnNewConnection.Click += (s, e) => {
				// TODO: 调用 FtpMgr.CreateNewConnection 方法
				ShowNewConnectionDialog();
			};

			btnNewUrl.Click += (s, e) => {
				// TODO: 调用 FtpMgr.CreateNewUrl 方法
				ShowNewUrlDialog();
			};

			btnCopyConnection.Click += (s, e) => {
				if (listView.SelectedItems.Count > 0)
				{
					// TODO: 调用 FtpMgr.CopyConnection 方法
					var selectedItem = listView.SelectedItems[0];
					CopyFtpConnection(selectedItem.Text);
				}
			};

			btnNewFolder.Click += (s, e) => {
				// TODO: 调用 FtpMgr.CreateNewFolder 方法
				ShowNewFolderDialog();
			};

			btnEditConnection.Click += (s, e) => {
				if (listView.SelectedItems.Count > 0)
				{
					// TODO: 调用 FtpMgr.EditConnection 方法
					var selectedItem = listView.SelectedItems[0];
					EditFtpConnection(selectedItem.Text);
				}
			};

			btnDeleteConnection.Click += (s, e) => {
				if (listView.SelectedItems.Count > 0)
				{
					if (MessageBox.Show("确定要删除选中的连接吗？", "确认删除",
						MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						// TODO: 调用 FtpMgr.DeleteConnection 方法
						var selectedItem = listView.SelectedItems[0];
						DeleteFtpConnection(selectedItem.Text);
						listView.Items.Remove(selectedItem);
					}
				}
			};

			btnEncrypt.Click += (s, e) => {
				// TODO: 调用 FtpMgr.EncryptConnections 方法
				EncryptFtpConnections();
			};

			btnClose.Click += (s, e) => form.Close();

			// 将按钮添加到面板
			buttonPanel.Controls.AddRange(new Control[] {
					btnConnect, btnNewConnection, btnNewUrl,
					btnCopyConnection, btnNewFolder, btnEditConnection,
					btnDeleteConnection, btnEncrypt, btnClose
				});
			buttonPanel.Dock = DockStyle.Right;
			// 添加控件到窗体
			form.Controls.Add(listView);
			form.Controls.Add(buttonPanel);

			// 加载现有FTP连接
			LoadFtpConnections(listView);

			// 显示窗体
			form.ShowDialog();
		}

		private void LoadFtpConnections(ListView listView)
		{
			// TODO: 从 FtpMgr 获取现有连接列表并填充到 ListView
			// 示例代码:
			/*
			var connections = FtpMgr.GetConnections();
			foreach (var conn in connections)
			{
				var item = new ListViewItem(conn.Name);
				item.SubItems.Add(conn.Host);
				listView.Items.Add(item);
			}
			*/
		}

		private void ShowNewConnectionDialog()
		{
			var form = new Form
			{
				Text = "新建FTP连接",
				Width = 450,
				Height = 500,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false,
				Padding = new Padding(10)
			};

			// 创建界面元素
			var sessionLabel = new Label { Text = "会话(&S):", Location = new Point(10, 20) };
			var sessionTextBox = new TextBox
			{
				Location = new Point(150, 17),
				Width = 250
			};

			var hostLabel = new Label { Text = "主机名[端口](&H):", Location = new Point(10, 50) };
			var hostTextBox = new TextBox
			{
				Location = new Point(150, 47),
				Width = 200
			};
			var portTextBox = new TextBox
			{
				Location = new Point(360, 47),
				Width = 40,
				Text = "21"
			};

			var sslCheckBox = new CheckBox
			{
				Text = "SSL/TLS",
				Location = new Point(150, 80),
				AutoSize = true
			};

			var userLabel = new Label { Text = "用户名(&U):", Location = new Point(10, 110) };
			var userTextBox = new TextBox
			{
				Location = new Point(150, 107),
				Width = 250
			};

			var passwordLabel = new Label { Text = "密码(&P):", Location = new Point(10, 140) };
			var passwordTextBox = new TextBox
			{
				Location = new Point(150, 137),
				Width = 250,
				PasswordChar = '*'
			};

			var passwordWarning = new Label
			{
				Text = "警告：保存密码不安全！",
				Location = new Point(150, 167),
				ForeColor = Color.Red,
				AutoSize = true
			};

			var remoteLabel = new Label { Text = "远程文件夹(&D):", Location = new Point(10, 200) };
			var remoteTextBox = new TextBox
			{
				Location = new Point(150, 197),
				Width = 250,
				Text = "/"
			};

			var localLabel = new Label { Text = "本地文件夹(&L):", Location = new Point(10, 230) };
			var localTextBox = new TextBox
			{
				Location = new Point(150, 227),
				Width = 250
			};
			var browseButton = new Button
			{
				Text = "...",
				Location = new Point(410, 226),
				Width = 30
			};

			var passiveModeCheckBox = new CheckBox
			{
				Text = "使用被动模式传输",
				Location = new Point(150, 260),
				AutoSize = true,
				Checked = true
			};

			var firewallCheckBox = new CheckBox
			{
				Text = "使用防火墙（代理服务）",
				Location = new Point(150, 290),
				AutoSize = true
			};

			// 添加确定和取消按钮
			var okButton = new Button
			{
				Text = "确定",
				DialogResult = DialogResult.OK,
				Location = new Point(150, 380)
			};

			var cancelButton = new Button
			{
				Text = "取消",
				DialogResult = DialogResult.Cancel,
				Location = new Point(270, 380)
			};

			// 添加浏览按钮事件处理
			browseButton.Click += (s, e) => {
				using var dialog = new FolderBrowserDialog();
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					localTextBox.Text = dialog.SelectedPath;
				}
			};

			// 添加确定按钮事件处理
			okButton.Click += (s, e) => {
				if (string.IsNullOrWhiteSpace(sessionTextBox.Text))
				{
					MessageBox.Show("请输入会话名称", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (string.IsNullOrWhiteSpace(hostTextBox.Text))
				{
					MessageBox.Show("请输入主机名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				if (!int.TryParse(portTextBox.Text, out int port) || port <= 0 || port > 65535)
				{
					MessageBox.Show("请输入有效的端口号(1-65535)", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// 创建配置对象
				var config = new FtpConnectionConfig
				{
					SessionName = sessionTextBox.Text,
					HostName = hostTextBox.Text,
					Port = port,
					UseSsl = sslCheckBox.Checked,
					UserName = userTextBox.Text,
					Password = passwordTextBox.Text,
					RemoteDirectory = remoteTextBox.Text,
					LocalDirectory = localTextBox.Text,
					UsePassiveMode = passiveModeCheckBox.Checked,
					UseFirewall = firewallCheckBox.Checked
				};

				// TODO: 保存配置到FtpMgr
				SaveFtpConnection(config);

				form.DialogResult = DialogResult.OK;
				form.Close();
			};

			// 将控件添加到窗体
			form.Controls.AddRange(new Control[] {
					sessionLabel, sessionTextBox,
					hostLabel, hostTextBox, portTextBox,
					sslCheckBox,
					userLabel, userTextBox,
					passwordLabel, passwordTextBox,
					passwordWarning,
					remoteLabel, remoteTextBox,
					localLabel, localTextBox, browseButton,
					passiveModeCheckBox,
					firewallCheckBox,
					okButton, cancelButton
				});

			form.AcceptButton = okButton;
			form.CancelButton = cancelButton;
			form.ShowDialog();
		}

		private void SaveFtpConnection(FtpConnectionConfig config)
		{
			// TODO: 实现保存FTP连接配置的逻辑
			// 可以保存到配置文件或数据库中
		}
		private void ShowNewUrlDialog()
		{
			// 实现新建URL对话框
			var form = new Form
			{
				Text = "新建 FTP 网址",
				Size = new Size(400, 200),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			};

			// TODO: 添加URL配置界面元素和处理逻辑
		}

		private void ShowNewFolderDialog()
		{
			// 实现新建文件夹对话框
			var folderName = Microsoft.VisualBasic.Interaction.InputBox(
				"请输入文件夹名称：",
				"新建文件夹",
				"新建文件夹");

			if (!string.IsNullOrEmpty(folderName))
			{
				// TODO: 调用 FtpMgr 创建文件夹
			}
		}

		private void CopyFtpConnection(string connectionName)
		{
			// TODO: 实现复制连接的逻辑
		}

		private void EditFtpConnection(string connectionName)
		{
			// TODO: 实现编辑连接的逻辑
		}

		private void DeleteFtpConnection(string connectionName)
		{
			// TODO: 实现删除连接的逻辑
		}

		private void EncryptFtpConnections()
		{
			// TODO: 实现加密连接的逻辑
		}
		#endregion

		#region 辅助类

		/// <summary>
		/// FTP连接信息类
		/// </summary>
		private class FtpConnectionInfo
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

		#endregion
	}
}