using CmdProcessor;
using FluentFTP;
using System.Diagnostics;
using System.Net;
using zfile;

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
		ListView ftplistView;
		public Form1 form;
		private Form ftpConnMgrform;
		/// <summary>
		/// FTP连接监视器，用于检测被动断开的情况
		/// </summary>
		private FtpConnectionMonitor _connectionMonitor;
		#endregion
		private  readonly Dictionary<string, TreeNode> _ftpNodes = new Dictionary<string, TreeNode>();
		private  readonly Dictionary<string, FtpFileSource> _ftpSources = new Dictionary<string, FtpFileSource>();
		private  readonly List<string> _registeredDrives = new List<string>();
		private  TreeNode _ftpRootNodeL, _ftpRootNodeR;
		public TreeNode ftpRootNode => form.uiManager.isleft ? _ftpRootNodeL : _ftpRootNodeR;
		private  VfsModuleManager _vfsManager;

		/// <summary>
		/// 显示FTP项目属性
		/// </summary>
		private void ShowFtpItemProperties(FtpFileSource source, string path, bool isDirectory)
		{
			try
			{
				// 获取文件或文件夹信息
				if (isDirectory)
				{
					var listing = source.Client.GetListing(path);
					int fileCount = listing.Count(i => i.Type == FtpObjectType.File);
					int dirCount = listing.Count(i => i.Type == FtpObjectType.Directory);
					long totalSize = listing.Where(i => i.Type == FtpObjectType.File).Sum(i => i.Size);

					// 显示属性对话框
					MessageBox.Show(
						$"路径: {path}\n" +
						$"类型: 文件夹\n" +
						$"文件数: {fileCount}\n" +
						$"文件夹数: {dirCount}\n" +
						$"总大小: {FileSystemManager.FormatFileSize(totalSize)}",
						"属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					// 获取文件信息
					var fileInfo = source.Client.GetObjectInfo(path);

					// 显示属性对话框
					MessageBox.Show(
						$"文件名: {Path.GetFileName(path)}\n" +
						$"路径: {path}\n" +
						$"大小: {FileSystemManager.FormatFileSize(fileInfo.Size)}\n" +
						$"修改时间: {fileInfo.Modified}\n" +
						$"权限: {fileInfo.Chmod}",
						"属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"获取属性失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 处理FTP节点双击事件
		/// </summary>
		public  void HandleFtpNodeDoubleClick(TreeNode node, ListView listView)
		{
			if (node.Tag is FtpNodeTag tag)
			{
				// 加载FTP目录内容
				LoadFtpDirectory( tag.ConnectionName, tag.Path, listView);
			}
		}

		/// <summary>
		/// 处理FTP列表项双击事件
		/// </summary>
		public  void HandleFtpListItemDoubleClick(string connectionName, ListViewItem item, ListView listView)
		{
			bool isDirectory = item.SubItems[3].Text == "<DIR>";
			string path = item.SubItems[1].Text;

			if (isDirectory)
			{
				// 如果是目录，进入该目录
				LoadFtpDirectory(connectionName, path, listView);

				// 更新当前FTP节点的路径
				if (_ftpNodes.TryGetValue(connectionName, out TreeNode node) && node.Tag is FtpNodeTag tag)
				{
					tag.Path = path;
				}
			}
			else
			{
				// 如果是文件，查看文件
				if (_ftpSources.TryGetValue(connectionName, out FtpFileSource source))
				{
					ViewFtpFile( source, path);
				}
			}
		}

		/// <summary>
		/// 初始化FTP管理器扩展
		/// </summary>
		/// <param name="form">主窗体</param>
		public  void Initialize()
		{
			// 初始化VFS管理器
			_vfsManager = new VfsModuleManager();
			_vfsManager.RegisterVirtualFileSource<FtpFileSource>("FTP", true);

			// 创建FTP根节点
			CreateFtpRootNode();

			// 加载已保存的FTP连接 //启动程序时不自动连接FTP
			//LoadSavedFtpConnections();
		}

		/// <summary>
		/// 创建FTP根节点
		/// </summary>
		/// <param name="form">主窗体</param>
		private  void CreateFtpRootNode()
		{
			// 在桌面节点下创建FTP连接节点
			_ftpRootNodeL = new TreeNode("FTP连接")
			{
				ImageKey = "folder",
				SelectedImageKey = "folder",
				Tag = new FtpRootNodeTag("Left")	//tag must not be null, otherwise 无法正常刷新高亮状态
			};
			_ftpRootNodeR = new TreeNode("FTP连接")
			{
				ImageKey = "folder",
				SelectedImageKey = "folder",
				Tag = new FtpRootNodeTag("Right")    //tag must not be null, otherwise 无法正常刷新高亮状态
			};
			// 添加到左侧和右侧树视图的桌面节点下
			if (form.leftRoot != null && form.rightRoot != null)
			{
				form.leftRoot.Nodes.Add(_ftpRootNodeL);
				form.rightRoot.Nodes.Add(_ftpRootNodeR);
			}
			else
			{
				MessageBox.Show("树视图根节点尚未初始化，FTP连接将在下次启动时显示", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		/// <summary>
		/// 加载已保存的FTP连接
		/// </summary>
		/// <param name="form">主窗体</param>
		private void LoadSavedFtpConnections()
		{
			// 从配置加载FTP连接
			var connections = form.fTPMGR.GetConnections();
			foreach (var conn in connections)
			{
				RegisterFtpConnection(conn.Name);
			}
		}

		/// <summary>
		/// 注册FTP连接为虚拟盘
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否注册成功</returns>
		public  bool RegisterFtpConnection( string connectionName)
		{
			// 检查是否已达到最大连接数
			if (_registeredDrives.Count >= 5)
			{
				MessageBox.Show("已达到最大FTP虚拟盘数量(5个)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			try
			{
				// 连接FTP服务器
				if (form.fTPMGR.Connect(connectionName))
				{
					// 创建虚拟盘标识符（F: ~ J:）
					char driveLetter = GetNextAvailableDriveLetter();
					string driveId = $"{driveLetter}:";

					// 创建FTP文件源
					var ftpSource = new FtpFileSource(form, connectionName, form.fTPMGR.ActiveClient);
					_ftpSources[connectionName] = ftpSource;

					// 创建FTP节点
					var ftpNode = new TreeNode($"{connectionName} ({driveId})")
					{
						ImageKey = "ftp",
						SelectedImageKey = "ftp",
						Tag = new FtpNodeTag { ConnectionName = connectionName, Path = "/" }
					};

					// 添加到FTP根节点
					AddFtpNode(ftpNode);
					_ftpNodes[connectionName] = ftpNode;
					_registeredDrives.Add(driveId);

					// 添加到DriveComboBox
					AddToDriveComboBox(connectionName, driveId);

					return true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"注册FTP连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return false;
		}
		private void AddFtpNode(TreeNode ftpNode)
		{
			_ftpRootNodeL.Nodes.Add(ftpNode);
			_ftpRootNodeR.Nodes.Add(ftpNode);
		}
		/// <summary>
		/// 获取下一个可用的驱动器盘符
		/// </summary>
		/// <returns>可用的驱动器盘符</returns>
		private  char GetNextAvailableDriveLetter()
		{
			// 从F开始分配，最多到J
			for (char c = 'F'; c <= 'J'; c++)
			{
				string drive = $"{c}:";
				if (!_registeredDrives.Contains(drive) && !Directory.Exists(drive))
				{
					return c;
				}
			}

			throw new Exception("没有可用的驱动器盘符");
		}

		/// <summary>
		/// 添加FTP连接到驱动器下拉框
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <param name="driveId">驱动器标识符</param>
		private void AddToDriveComboBox( string connectionName, string driveId)
		{
			// 添加到左侧和右侧驱动器下拉框
			form.uiManager.LeftDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
			form.uiManager.RightDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
		}

		private void RemoveFtpNode(TreeNode node)
		{
			_ftpRootNodeL.Nodes.Remove(node);
			_ftpRootNodeR.Nodes.Remove(node);
		}
		/// <summary>
		/// 取消注册FTP连接
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否取消注册成功</returns>
		public  bool UnregisterFtpConnection(string connectionName)
		{
			try
			{
				if (_ftpNodes.TryGetValue(connectionName, out TreeNode node))
				{
					// 从连接监视器中移除
					_connectionMonitor.RemoveConnection(connectionName);

					// 获取驱动器标识符
					string nodeText = node.Text;
					string driveId = nodeText.Substring(nodeText.IndexOf('(') + 1, 2);

					// 从树视图中移除节点
					RemoveFtpNode(node);
					_ftpNodes.Remove(connectionName);

					// 从驱动器列表中移除
					_registeredDrives.Remove(driveId);

					// 从驱动器下拉框中移除
					RemoveFromDriveComboBox(driveId);

					// 断开FTP连接
					if (_ftpSources.TryGetValue(connectionName, out FtpFileSource source))
					{
						source.Finalize();
						_ftpSources.Remove(connectionName);
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"取消注册FTP连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return false;
		}

		/// <summary>
		/// 从驱动器下拉框中移除FTP连接
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="driveId">驱动器标识符</param>
		private  void RemoveFromDriveComboBox(string driveId)
		{
			// 从左侧和右侧驱动器下拉框中移除
			for (int i = form.uiManager.LeftDriveComboBox.Items.Count - 1; i >= 0; i--)
			{
				string item = form.uiManager.LeftDriveComboBox.Items[i].ToString();
				if (item.StartsWith(driveId))
				{
					form.uiManager.LeftDriveComboBox.Items.RemoveAt(i);
				}
			}

			for (int i = form.uiManager.RightDriveComboBox.Items.Count - 1; i >= 0; i--)
			{
				string item = form.uiManager.RightDriveComboBox.Items[i].ToString();
				if (item.StartsWith(driveId))
				{
					form.uiManager.RightDriveComboBox.Items.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// 加载FTP目录内容到ListView
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <param name="path">FTP路径</param>
		/// <param name="listView">目标ListView</param>
		public  void LoadFtpDirectory(string connectionName, string path, ListView listView)
		{
			try
			{
				if (_ftpSources.TryGetValue(connectionName, out FtpFileSource source))
				{
					// 设置当前路径
					source.CurrentPath = path;

					// 清空ListView
					listView.BeginUpdate();
					listView.Items.Clear();

					// 获取目录列表
					var items = source.GetListing(path);
					foreach (var item in items)
					{
						// 确保图标已加载
						if (!form.iconManager.HasIconKey(item.ImageKey, false))
						{
							// 使用默认图标
							item.ImageKey = item.SubItems[3].Text == "<DIR>" ? "folder" : "file";
						}

						// 添加到ListView
						listView.Items.Add(item);
					}

					listView.EndUpdate();

					// 更新当前目录
					form.currentDirectory = $"ftp://{source.Host}{path}";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载FTP目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 处理FTP文件或文件夹的右键菜单
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <param name="item">选中的ListViewItem</param>
		/// <param name="location">鼠标位置</param>
		public  void ShowFtpContextMenu(string connectionName, ListViewItem item, Point location)
		{
			if (_ftpSources.TryGetValue(connectionName, out FtpFileSource source))
			{
				bool isDirectory = item.SubItems[3].Text == "<DIR>";
				string path = item.SubItems[1].Text;

				// 创建右键菜单
				var contextMenu = new ContextMenuStrip();

				// 添加通用菜单项
				if (isDirectory)
				{
					// 文件夹菜单项
					contextMenu.Items.Add("复制", null, (s, e) => CopyFtpItem(source, path));
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItem(source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItem(source, path, true));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadList(source, path, true));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemProperties(source, path, true));
				}
				else
				{
					// 文件菜单项
					contextMenu.Items.Add("查看", null, (s, e) => ViewFtpFile(source, path));
					contextMenu.Items.Add("编辑", null, (s, e) => EditFtpFile(source, path));
					contextMenu.Items.Add("复制", null, (s, e) => CopyFtpItem(source, path));
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItem(source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItem(source, path, false));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadList(source, path, false));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemProperties(source, path, false));
				}

				// 显示菜单
				contextMenu.Show(location);
			}
		}

		/// <summary>
		/// 查看FTP文件
		/// </summary>
		private  void ViewFtpFile(FtpFileSource source, string path)
		{
			try
			{
				// 下载文件到临时目录
				string localPath = source.DownloadFile(path);
				if (!string.IsNullOrEmpty(localPath))
				{
					// 调用CmdProc的do_cm_list方法查看文件
					form.do_cm_list(localPath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"查看文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 编辑FTP文件
		/// </summary>
		private  void EditFtpFile(FtpFileSource source, string path)
		{
			try
			{
				// 下载文件到临时目录
				string localPath = source.DownloadFile(path);
				if (!string.IsNullOrEmpty(localPath))
				{
					// 调用CmdProc的do_cm_edit方法编辑文件
					form.do_cm_edit(localPath);

					// 监视文件变化，如果有修改则上传
					FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(localPath), Path.GetFileName(localPath));
					watcher.NotifyFilter = NotifyFilters.LastWrite;
					watcher.Changed += (s, e) =>
					{
						if (e.ChangeType == WatcherChangeTypes.Changed)
						{
							// 确保文件不再被占用
							System.Threading.Thread.Sleep(500);
							source.UploadFile(localPath, path);
						}
					};
					watcher.EnableRaisingEvents = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"编辑文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 复制FTP项目
		/// </summary>
		private static void CopyFtpItem(FtpFileSource source, string path)
		{
			try
			{
				// 显示目标选择对话框
				FolderBrowserDialog dialog = new FolderBrowserDialog();
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					string targetPath = dialog.SelectedPath;
					string fileName = Path.GetFileName(path);
					string localTargetPath = Path.Combine(targetPath, fileName);

					// 下载文件或文件夹
					if (path.EndsWith("/"))
					{
						// 创建目标文件夹
						Directory.CreateDirectory(localTargetPath);

						// 递归下载文件夹内容
						DownloadDirectory(source, path, localTargetPath);
					}
					else
					{
						// 下载单个文件
						source.Client.DownloadFile(localTargetPath, path);
					}

					MessageBox.Show($"复制完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 递归下载目录
		/// </summary>
		private static void DownloadDirectory(FtpFileSource source, string remotePath, string localPath)
		{
			// 获取目录列表
			var listing = source.Client.GetListing(remotePath);

			foreach (var item in listing)
			{
				string remoteFilePath = item.FullName;
				string localFilePath = Path.Combine(localPath, item.Name);

				if (item.Type == FtpObjectType.Directory)
				{
					// 创建本地目录
					Directory.CreateDirectory(localFilePath);
					// 递归下载子目录
					DownloadDirectory(source, remoteFilePath, localFilePath);
				}
				else
				{
					// 下载文件
					source.Client.DownloadFile(localFilePath, remoteFilePath);
				}
			}
		}

		/// <summary>
		/// 重命名FTP项目
		/// </summary>
		private  void RenameFtpItem( FtpFileSource source, string path)
		{
			try
			{
				string oldName = Path.GetFileName(path);
				string newName = Microsoft.VisualBasic.Interaction.InputBox(
					"请输入新名称：", "重命名", oldName);

				if (!string.IsNullOrEmpty(newName) && newName != oldName)
				{
					string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
					if (!parentPath.EndsWith("/"))
						parentPath += "/";

					string newPath = parentPath + newName;

					// 执行重命名
					if (source.Rename(path, newPath))
					{
						// 刷新列表
						LoadFtpDirectory( source.ConnectionName, parentPath, form.activeListView);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 删除FTP项目
		/// </summary>
		private  void DeleteFtpItem(FtpFileSource source, string path, bool isDirectory)
		{
			try
			{
				if (MessageBox.Show($"确定要删除 {Path.GetFileName(path)} 吗？", "确认删除",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					bool success;
					if (isDirectory)
					{
						success = source.DeleteDirectory(path);
					}
					else
					{
						success = source.DeleteFile(path);
					}

					if (success)
					{
						// 获取父目录路径
						string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
						if (!parentPath.EndsWith("/"))
							parentPath += "/";

						// 刷新列表
						LoadFtpDirectory(source.ConnectionName, parentPath, form.activeListView);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 添加到下载列表
		/// </summary>
		private void AddToDownloadList( FtpFileSource source, string path, bool isDirectory)
		{
			try
			{
				// 显示目标选择对话框
				FolderBrowserDialog dialog = new FolderBrowserDialog();
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					string targetPath = dialog.SelectedPath;
					string fileName = Path.GetFileName(path);
					string localTargetPath = Path.Combine(targetPath, fileName);

					// 这里可以实现一个下载队列管理器
					// 简单实现：直接下载
					if (isDirectory)
					{
						// 创建目标文件夹
						Directory.CreateDirectory(localTargetPath);
						// 异步下载文件夹
						Task.Run(() =>
						{
							try
							{
								DownloadDirectory(source, path, localTargetPath);
								form.Invoke(new Action(() =>
								{
									MessageBox.Show("文件夹下载完成", "下载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
								}));
							}
							catch (Exception ex)
							{
								form.Invoke(new Action(() =>
								{
									MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
								}));
							}
						});
					}
					else
					{
						// 异步下载文件
						Task.Run(() =>
						{
							try
							{
								source.Client.DownloadFile(localTargetPath, path);
								form.Invoke(new Action(() =>
								{
									MessageBox.Show("文件下载完成", "下载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
								}));
							}
							catch (Exception ex)
							{
								form.Invoke(new Action(() =>
								{
									MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
								}));
							}
						});
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"添加到下载列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#region 构造函数

		/// <summary>
		/// 初始化FTP管理器
		/// </summary>
		public FTPMGR(Form1 form)
		{
			this.form = form;
			_connections = new Dictionary<string, FtpConnectionInfo>();
			ftplistView = new ListView
			{
				Dock = DockStyle.Left,
				View = View.Details,
				FullRowSelect = true,
				Location = new Point(10, 10),
				Size = new Size(420, 300),
				MultiSelect = false
			};

			ftplistView.Columns.Add("名称", 150);
			ftplistView.Columns.Add("主机", 200);
			Init(); //init connctions from ftpcfgloader

			// 初始化FTP连接监视器
			_connectionMonitor = new FtpConnectionMonitor(this);
		}

		#endregion
		~FTPMGR()
		{
			// 释放连接监视器资源
			_connectionMonitor?.Dispose();
		}
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
				var profile = _activeClient.AutoConnect();  //connect()
				if (profile != null) {
					Debug.Print("ftp auto connect success.");
					form.uiManager.ftpController.UpdateStatus(true);

					// 添加到连接监视器进行监控
					_connectionMonitor.AddConnection(connectionName, _activeClient);
				}
				else
				{
					Debug.Print("ftp auto connect failed.");
				}
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
		public bool ChangeConnection(string name, string host = null, string username = null, string password = null, int? port = null, FtpEncryptionMode? encryptionMode = null)
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
			// 获取当前连接名称（如果有）
			//string connectionName = null;
			//if (_activeClient != null && _activeClient.IsConnected)
			//{
			//	// 尝试找到当前连接的名称
			//	foreach (var conn in _connections)
			//	{
			//		if (conn.Value.Host == _activeClient.Host && 
			//			conn.Value.Credentials.UserName == _activeClient.Credentials.UserName)
			//		{
			//			connectionName = conn.Key;
			//			break;
			//		}
			//	}

			//	// 断开连接
			//	_activeClient.Disconnect();

			//	// 如果找到了连接名称，从监视器中移除
			//	if (connectionName != null)
			//	{
			//		_connectionMonitor.RemoveConnection(connectionName);
			//	}
			//}
			//_activeClient = null;
			if (ActiveClient != null && ActiveClient.IsConnected)
			{
				// 查找当前连接的名称
				string connectionName = null;
				foreach (var node in _ftpNodes)
				{
					if (node.Value.Tag is FtpNodeTag tag &&
						_ftpSources.TryGetValue(tag.ConnectionName, out var source) &&
						source.Client == ActiveClient)
					{
						connectionName = tag.ConnectionName;
						break;
					}
				}

				// 断开连接
				ActiveClient.Disconnect();
				form.uiManager.ftpController.UpdateStatus(false);
				form.activeListView.Items.Clear();
				// 如果找到了连接名称，注销FTP连接
				if (!string.IsNullOrEmpty(connectionName))
				{
					UnregisterFtpConnection(connectionName);
					_connectionMonitor.RemoveConnection(connectionName); //，从监视器中移除
				}
			}
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
		public void ShowFtpConnectionForm()
		{
			ftpConnMgrform = new Form
			{
				Text = "FTP 连接管理器",
				Size = new Size(600, 500),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			};

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
			btnConnect.Click += connectButton_click;

			btnNewConnection.Click += (s, e) => {
				//  调用 FtpMgr.CreateNewConnection 方法
				EditConnectionDialog();
			};

			btnNewUrl.Click += (s, e) => {
				// 调用 FtpMgr.CreateNewUrl 方法
				ShowNewUrlDialog();
			};

			btnCopyConnection.Click += (s, e) => {
				if (ftplistView.SelectedItems.Count > 0)
				{
					// 调用 FtpMgr.CopyConnection 方法
					var selectedItem = ftplistView.SelectedItems[0];
					CopyFtpConnection(selectedItem.Text);
				}
			};

			btnNewFolder.Click += (s, e) => {
				// 调用 FtpMgr.CreateNewFolder 方法
				ShowNewFolderDialog();
			};

			btnEditConnection.Click += (s, e) => {
				if (ftplistView.SelectedItems.Count > 0)
				{
					// 调用 FtpMgr.EditConnection 方法
					var selectedItem = ftplistView.SelectedItems[0];
					EditFtpConnection(selectedItem.Text);
				}
			};

			btnDeleteConnection.Click += (s, e) => {
				if (ftplistView.SelectedItems.Count > 0)
				{
					if (MessageBox.Show("确定要删除选中的连接吗？", "确认删除",
						MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						//  调用 FtpMgr.DeleteConnection 方法
						var selectedItem = ftplistView.SelectedItems[0];
						DeleteFtpConnection(selectedItem.Text);
						ftplistView.Items.Remove(selectedItem);
					}
				}
			};

			btnEncrypt.Click += (s, e) => {
				// 调用 FtpMgr.EncryptConnections 方法
				EncryptFtpConnections();
			};

			btnClose.Click += (s, e) => ftpConnMgrform.Close();

			// 将按钮添加到面板
			buttonPanel.Controls.AddRange(new Control[] {
					btnConnect, btnNewConnection, btnNewUrl,
					btnCopyConnection, btnNewFolder, btnEditConnection,
					btnDeleteConnection, btnEncrypt, btnClose
				});
			buttonPanel.Dock = DockStyle.Right;
			// 添加控件到窗体
			ftpConnMgrform.Controls.Add(ftplistView);
			ftpConnMgrform.Controls.Add(buttonPanel);

			// 加载现有FTP连接
			ReloadListview(ftplistView);

			// 显示窗体
			ftpConnMgrform.ShowDialog();
		}
		private void connectButton_click(object s, EventArgs e)
		{
			if (ftplistView.SelectedItems.Count > 0)
			{
				var selectedItem = ftplistView.SelectedItems[0];
				string connectionName = selectedItem.Text;
				// 调用 FtpMgr.Connect 方法
				//Connect(selectedItem.Text);//bugfix: connect 不会维护drivecombobox和ftptreenode,改用registerftpconnection
				if (RegisterFtpConnection(connectionName))
				{
					// 获取新添加的FTP节点并设置为活动树的SelectedNode
					if (_ftpNodes.TryGetValue(connectionName, out TreeNode ftpNode))
					{
						// 设置活动树的SelectedNode为新添加的FTP节点
						form.activeTreeview.SelectedNode = ftpNode;

						// 触发节点双击事件，加载FTP目录内容
						if (ftpNode.Tag is FtpNodeTag tag)
						{
							// 加载FTP目录内容到活动列表视图
							LoadFtpDirectory(tag.ConnectionName, tag.Path, form.activeListView);
						}
					}
					ftpConnMgrform.Close();
				}
			}
			else
			{
				MessageBox.Show("请选择要连接的FTP站点", "提示");
			}
		}

		private void ReloadListview(ListView listView)
		{
			//  从 FtpMgr 获取现有连接列表并填充到 ListView
			listView.Items.Clear();
			var connections = GetConnections();
			foreach (var conn in connections)
			{
				var item = new ListViewItem(conn.Name);
				item.SubItems.Add(conn.Host);
				listView.Items.Add(item);
			}
			listView.Refresh();
		}
		/// <summary>
		/// 获取所有FTP连接信息
		/// </summary>
		/// <returns>FTP连接信息列表</returns>
		public List<FtpConnectionInfo> GetConnections()
		{ 
			return _connections.Values.ToList();
		}

		public void EditConnectionDialog(string connectionName = "")
		{
			FtpConnectionInfo connection = new();
			bool isEditMode = false;
			if (!string.IsNullOrWhiteSpace(connectionName)) { 
				connection = _connections[connectionName];
				isEditMode = true;
			}

			var form = new Form
			{
				Text = isEditMode ? "编辑FTP连接" : "新建FTP连接",
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
				Width = 250,
				ReadOnly = isEditMode,
				Text = isEditMode ? connection.Name : ""
			};

			var hostLabel = new Label { Text = "主机名[端口](&H):", Location = new Point(10, 50) };
			var hostTextBox = new TextBox
			{
				Location = new Point(150, 47),
				Width = 200,
				Text = isEditMode ? connection.Host : ""
			};
			var portTextBox = new TextBox
			{
				Location = new Point(360, 47),
				Width = 40,
				Text = isEditMode ? connection.Port.ToString() : "21"
			};

			var sslCheckBox = new CheckBox
			{
				Text = "SSL/TLS",
				Location = new Point(150, 80),
				AutoSize = true,
				Checked = isEditMode ? connection.EncryptionMode.HasValue &&
				 connection.EncryptionMode.Value != FluentFTP.FtpEncryptionMode.None : false
			};

			var userLabel = new Label { Text = "用户名(&U):", Location = new Point(10, 110) };
			var userTextBox = new TextBox
			{
				Location = new Point(150, 107),
				Width = 250,
				Text = isEditMode ? connection.Credentials.UserName : ""
			};

			var passwordLabel = new Label { Text = "密码(&P):", Location = new Point(10, 140) };
			var passwordTextBox = new TextBox
			{
				Location = new Point(150, 137),
				Width = 250,
				PasswordChar = '*',
				Text = isEditMode ? connection.Credentials.Password : ""
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
				if (isEditMode) {
					try
					{
						// 更新连接配置
						if (ChangeConnection(
							connectionName,
							hostTextBox.Text,
							userTextBox.Text,
							passwordTextBox.Text,
							port,
							sslCheckBox.Checked ? FluentFTP.FtpEncryptionMode.Explicit : FluentFTP.FtpEncryptionMode.None))
						{
							// 如果当前连接是活动连接，则断开重连
							if (_activeClient != null && _activeClient.IsConnected &&
								_connections[connectionName].Host == _activeClient.Host &&
								_connections[connectionName].Credentials.UserName == _activeClient.Credentials.UserName)
							{
								try
								{
									Connect(connectionName);
								}
								catch (Exception ex)
								{
									MessageBox.Show($"重新连接时出错: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								}
							}

							MessageBox.Show("连接配置已更新", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
							form.DialogResult = DialogResult.OK;
							form.Close();
						}
						else
						{
							MessageBox.Show("更新连接配置失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"更新连接时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				else 
				{
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
					// 保存配置到FtpMgr
					SaveFtpConnection(config);
					form.DialogResult = DialogResult.OK;
					form.Close();
				}
				ReloadListview(ftplistView);

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
			// 可以保存到配置文件或数据库中
			CreateConnection(config.SessionName, config.HostName, config.UserName, config.Password, config.Port, config.UseSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None);
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
			CopyConnection(connectionName, connectionName + "_Copy");
			ReloadListview(ftplistView);
		}

		private void EditFtpConnection(string connectionName)
		{
			EditConnectionDialog(connectionName);
		}

		private void DeleteFtpConnection(string connectionName)
		{
			DeleteConnection(connectionName);
		}

		private void EncryptFtpConnections()
		{
		}
		#endregion

		#region 辅助类
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
		/// <summary>
		/// FTP连接信息类
		/// </summary>
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

		#endregion
		public void SaveToCfgloader()
		{
			// 保存配置到cfgloader
			try
			{
				var connectionsSection = form.ftpconfigLoader.sections.Find(s => s.Name.Equals("connections"));
				foreach(var i in connectionsSection.Items)
				{
					if(i.Key == "default") continue;
					// 清除已有的FTP相关配置
					form.ftpconfigLoader.RemoveSection(i.Value);
				}
				form.ftpconfigLoader.RemoveSection("connections");

				// 创建connections节的配置项
				var connectionItems = new List<ConfigItem>();
				int index = 1;
				string defaultConnection = "";

				foreach (var conn in _connections)
				{
					string sectionName = $"{conn.Key}";
					defaultConnection = defaultConnection == "" ? conn.Key : defaultConnection;

					// 添加到connections列表
					connectionItems.Add(new ConfigItem
					{
						Key = index.ToString(),
						Value = conn.Key
					});

					// 创建每个连接的配置项
					var connectionConfig = new List<ConfigItem>
					{
						new ConfigItem { Key = "host", Value = conn.Value.Host },
						new ConfigItem { Key = "username", Value = conn.Value.Credentials.UserName },
						new ConfigItem { Key = "password", Value = conn.Value.Credentials.Password },
						new ConfigItem { Key = "port", Value = conn.Value.Port.ToString() },
						new ConfigItem
						{
							Key = "pasvmode",
							Value = (conn.Value.Config?.DataConnectionType == FtpDataConnectionType.AutoPassive
									|| conn.Value.Config?.DataConnectionType == FtpDataConnectionType.PASV)
									? "1" : "0"
						}
					};

					// 如果有加密模式，添加加密配置
					if (conn.Value.EncryptionMode.HasValue)
					{
						connectionConfig.Add(new ConfigItem
						{
							Key = "encryption",
							Value = conn.Value.EncryptionMode.Value.ToString()
						});
					}

					// 添加该连接的配置节
					form.ftpconfigLoader.AddOrUpdateSection(sectionName, connectionConfig);
					index++;
				}

				// 添加默认连接配置
				connectionItems.Add(new ConfigItem { Key = "default", Value = defaultConnection });

				// 添加connections节
				form.ftpconfigLoader.AddOrUpdateSection("connections", connectionItems);

				// 保存到文件
				form.ftpconfigLoader.SaveConfig();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存FTP配置时发生错误: {ex.Message}", "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

		}
		private void Init()
		{
			var defaultConnName = form.ftpconfigLoader.FindConfigValue("connections", "default");
			//init _connection by ftp.configloader.sections:
			/*
			 * [connections]
				1=tt
				default=tt
				[tt]
				host=localhost
				username=isa
				password=713A3D5726ACF87D597A0283
				pasvmode=0
			 */
			if (string.IsNullOrEmpty(defaultConnName))
			{
				return; // 没有配置默认连接
			}

			// 读取指定连接名称的配置
			var host = form.ftpconfigLoader.FindConfigValue(defaultConnName, "host");
			var username = form.ftpconfigLoader.FindConfigValue(defaultConnName, "username");
			var password = form.ftpconfigLoader.FindConfigValue(defaultConnName, "password");
			var pasvModeStr = form.ftpconfigLoader.FindConfigValue(defaultConnName, "pasvmode");

			// 验证必要参数
			if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
			{
				return; // 缺少必要参数
			}

			// 转换被动模式设置
			bool usePasvMode = true; // 默认使用被动模式
			if (!string.IsNullOrEmpty(pasvModeStr) && int.TryParse(pasvModeStr, out int pasvMode))
			{
				usePasvMode = pasvMode != 0;
			}

			// 创建FTP连接配置
			var connectionInfo = new FtpConnectionInfo
			{
				Name = defaultConnName,
				Host = host,
				Credentials = new NetworkCredential(username, password),
				Port = 21, // 使用默认端口
				Config = new FtpConfig
				{
					DataConnectionType = usePasvMode ?
						FtpDataConnectionType.AutoPassive :
						FtpDataConnectionType.AutoActive
				}
			};

			// 添加到连接字典
			_connections[defaultConnName] = connectionInfo;

			// 读取所有配置的连接
			// 先获取 connections 段下的所有键值对
			var connections = form.ftpconfigLoader.GroupConfigItemsByNumberOrName()
				.Where(x => x.Key == "connections")
				.SelectMany(x => x.Value)
				.Where(x => x.Key != "default"); // 排除default配置项

			// 遍历添加其他连接
			foreach (var conn in connections)
			{
				var connName = conn.Value;
				if (connName == defaultConnName) continue; // 跳过已添加的默认连接

				// 读取连接配置
				host = form.ftpconfigLoader.FindConfigValue(connName, "host");
				username = form.ftpconfigLoader.FindConfigValue(connName, "username");
				password = form.ftpconfigLoader.FindConfigValue(connName, "password");
				pasvModeStr = form.ftpconfigLoader.FindConfigValue(connName, "pasvmode");

				// 验证必要参数
				if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
					continue;

				// 转换被动模式设置
				usePasvMode = true;
				if (!string.IsNullOrEmpty(pasvModeStr) && int.TryParse(pasvModeStr, out pasvMode))
				{
					usePasvMode = pasvMode != 0;
				}

				// 创建并添加连接配置
				_connections[connName] = new FtpConnectionInfo
				{
					Name = connName,
					Host = host,
					Credentials = new NetworkCredential(username, password),
					Port = 21,
					Config = new FtpConfig
					{
						DataConnectionType = usePasvMode ?
							FtpDataConnectionType.AutoPassive :
							FtpDataConnectionType.AutoActive
					}
				};
			}

			// 刷新ListView显示
			if (ftplistView != null)
			{
				ReloadListview(ftplistView);
			}

		}
	}
}