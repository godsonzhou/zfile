using FluentFTP;
using System.Diagnostics;
using System.Net;

namespace zfile
{
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
	/// <summary>
	/// FTP管理器异步类，用于管理FTP连接和操作
	/// 提供连接管理、文件操作等功能的异步实现
	/// </summary>
	public class AsyncFTPMGR
	{
		#region 属性

		/// <summary>
		/// 存储FTP连接配置的字典
		/// </summary>
		private Dictionary<string, FtpConnectionInfo> _connections;

		/// <summary>
		/// 当前活动的FTP客户端
		/// </summary>
		private AsyncFtpClient _activeClient;

		/// <summary>
		/// 获取当前活动的FTP客户端
		/// </summary>
		public AsyncFtpClient ActiveClient => _activeClient;
		ListView ftplistView;
		public Form1 form;
		private Form ftpConnMgrform;
		/// <summary>
		/// FTP连接监视器，用于检测被动断开的情况
		/// </summary>
		private AsyncFtpConnectionMonitor _connectionMonitor;
		#endregion
		private readonly Dictionary<string, TreeNode> _ftpNodesL = new Dictionary<string, TreeNode>();
		private readonly Dictionary<string, TreeNode> _ftpNodesR = new Dictionary<string, TreeNode>();
		private Dictionary<string, TreeNode> _ftpNodes => form.uiManager.isleft ? _ftpNodesL : _ftpNodesR;
		private readonly Dictionary<string, AsyncFtpFileSource> _ftpSources = new Dictionary<string, AsyncFtpFileSource>();
		private readonly List<string> _registeredDrives = new List<string>();
		private TreeNode _ftpRootNodeL, _ftpRootNodeR;
		public TreeNode ftpRootNode => form.uiManager.isleft ? _ftpRootNodeL : _ftpRootNodeR;
		private VfsModuleManager _vfsManager;
		public Dictionary<string, AsyncFtpFileSource> ftpSources => _ftpSources;
		private bool _isDownloading = false;
		private FtpListOption _listOption = FtpListOption.Auto;
		public FtpListOption ListOption { get => _listOption; set => _listOption = value; }

		public bool HasPendingDownloads()
		{
			return true;
		}
		public void ProcessDownloadList()
		{

		}
		public void AbortDownload()
		{

		}
		public bool IsDownloading()
		{
			return _isDownloading;
		}

		public void ResumeDownload()
		{

		}
		public void AddToDownloadList(FtpFileSource source, string path, bool isDirectory)
		{

		}
		/// <summary>
		/// 显示FTP项目属性
		/// </summary>
		private async Task ShowFtpItemPropertiesAsync(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			try
			{
				// 获取文件或文件夹信息
				if (isDirectory)
				{
					var listing = await source.Client.GetListing(path, _listOption);
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
					var fileInfo = await source.Client.GetObjectInfo(path);

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
		/// 同步版本的显示FTP项目属性（为了兼容性）
		/// </summary>
		private void ShowFtpItemProperties(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			ShowFtpItemPropertiesAsync(source, path, isDirectory).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 处理FTP节点双击事件
		/// </summary>
		public void HandleFtpNodeDoubleClick(TreeNode node, ListView listView)
		{
			if (node.Tag is FtpNodeTag tag)
			{
				// 加载FTP目录内容
				LoadFtpDirectory(tag.ConnectionName, tag.Path, listView);
			}
		}

		/// <summary>
		/// 处理FTP列表项双击事件
		/// </summary>
		public async Task HandleFtpListItemDoubleClickAsync(string connectionName, ListViewItem item, ListView listView)
		{
			bool isDirectory = item.SubItems[3].Text == "<DIR>";
			string path = item.SubItems[1].Text;

			if (isDirectory)
			{
				// 如果是目录，进入该目录
				await LoadFtpDirectoryAsync(connectionName, path, listView);

				// 更新当前FTP节点的路径
				if (_ftpNodes.TryGetValue(connectionName, out TreeNode node) && node.Tag is FtpNodeTag tag)
				{
					tag.Path = path;
				}
			}
			else
			{
				// 如果是文件，查看文件
				if (_ftpSources.TryGetValue(connectionName, out AsyncFtpFileSource source))
				{
					await ViewFtpFileAsync(source, path);
				}
			}
		}

		/// <summary>
		/// 同步版本的处理FTP列表项双击事件（为了兼容性）
		/// </summary>
		public void HandleFtpListItemDoubleClick(string connectionName, ListViewItem item, ListView listView)
		{
			HandleFtpListItemDoubleClickAsync(connectionName, item, listView).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 初始化FTP管理器扩展
		/// </summary>
		public void Initialize()
		{
			// 初始化VFS管理器
			_vfsManager = new VfsModuleManager();
			_vfsManager.RegisterVirtualFileSource<AsyncFtpFileSource>("FTP", true);

			// 创建FTP根节点
			CreateFtpRootNode();
		}

		/// <summary>
		/// 创建FTP根节点
		/// </summary>
		private void CreateFtpRootNode()
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
		/// 注册FTP连接为虚拟盘
		/// </summary>
		public async Task<bool> RegisterFtpConnectionAsync(string connectionName)
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
				if (await ConnectAsync(connectionName))
				{
					//虚拟盘标识符（F: ~ J:）
					char driveLetter = GetNextAvailableDriveLetter();
					string driveId = $"{driveLetter}:";

					// 创建FTP文件源
					var ftpSource = new AsyncFtpFileSource(form, connectionName, form.fTPMGR.ActiveClient);
					_ftpSources[connectionName] = ftpSource;

					// 创建FTP节点
					var ftpNode = new TreeNode($"{driveId} [{connectionName}]")
					{
						ImageKey = "ftp",
						SelectedImageKey = "ftp",
						Tag = new FtpNodeTag { ConnectionName = connectionName, Path = "/" }
					};
					var ftpNodeR = (TreeNode)ftpNode.Clone();
					// 添加到FTP根节点
					AddFtpNode(ftpNode, true);
					AddFtpNode(ftpNodeR);
					_ftpNodesL[connectionName] = ftpNode;
					_ftpNodesR[connectionName] = ftpNodeR;
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

		/// <summary>
		/// 同步版本的注册FTP连接为虚拟盘（为了兼容性）
		/// </summary>
		public bool RegisterFtpConnection(string connectionName)
		{
			return RegisterFtpConnectionAsync(connectionName).GetAwaiter().GetResult();
		}

		private void AddFtpNode(TreeNode ftpNode, bool isleft = false)
		{
			if(isleft)
				_ftpRootNodeL.Nodes.Add(ftpNode);
			else
				_ftpRootNodeR.Nodes.Add(ftpNode);
		}

		/// <summary>
		/// 获取下一个可用的驱动器盘符
		/// </summary>
		/// <returns>可用的驱动器盘符</returns>
		private char GetNextAvailableDriveLetter()
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
		/// <param name="connectionName">连接名称</param>
		/// <param name="driveId">驱动器标识符</param>
		private void AddToDriveComboBox(string connectionName, string driveId)
		{
			// 添加到左侧和右侧驱动器下拉框
			form.uiManager.LeftDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
			form.uiManager.RightDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
		}

		private void RemoveFtpNode(TreeNode node, bool isleft = false)
		{
			if(isleft)
				_ftpRootNodeL.Nodes.Remove(node);
			else
				_ftpRootNodeR.Nodes.Remove(node);
		}

		/// <summary>
		/// 取消注册FTP连接
		/// </summary>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否取消注册成功</returns>
		public async Task<bool> UnregisterFtpConnectionAsync(string connectionName)
		{
			try
			{
				if (_ftpNodesL.TryGetValue(connectionName, out TreeNode node))
				{
					_ftpNodesR.TryGetValue(connectionName, out TreeNode nodeR);
					// 从连接监视器中移除
					_connectionMonitor.RemoveConnection(connectionName);

					// 获取驱动器标识符
					string nodeText = node.Text;
					string driveId = nodeText.Substring(0, 2);

					// 从树视图中移除节点
					RemoveFtpNode(node, true);
					RemoveFtpNode(nodeR);
					_ftpNodesL.Remove(connectionName);
					_ftpNodesR.Remove(connectionName);

					// 从驱动器列表中移除
					_registeredDrives.Remove(driveId);

					// 从驱动器下拉框中移除
					RemoveFromDriveComboBox(driveId);

					// 断开FTP连接
					if (_ftpSources.TryGetValue(connectionName, out AsyncFtpFileSource source))
					{
						await source.FinalizeAsync();
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
		/// 同步版本的取消注册FTP连接（为了兼容性）
		/// </summary>
		public bool UnregisterFtpConnection(string connectionName)
		{
			return UnregisterFtpConnectionAsync(connectionName).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 从驱动器下拉框中移除FTP连接
		/// </summary>
		/// <param name="driveId">驱动器标识符</param>
		private void RemoveFromDriveComboBox(string driveId)
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
		/// 异步加载FTP目录内容到ListView
		/// </summary>
		/// <param name="connectionName">连接名称</param>
		/// <param name="path">FTP路径</param>
		/// <param name="listView">目标ListView</param>
		public async Task LoadFtpDirectoryAsync(string connectionName, string path, ListView listView)
		{
			try
			{
				if (_ftpSources.TryGetValue(connectionName, out AsyncFtpFileSource source))
				{
					// 设置当前路径
					source.CurrentPath = path;

					// 清空ListView
					listView.BeginUpdate();
					listView.Items.Clear();

					// 获取目录列表
					var items = await source.GetListingAsync(path, _listOption);
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
		/// 同步版本的加载FTP目录内容到ListView（为了兼容性）
		/// </summary>
		public void LoadFtpDirectory(string connectionName, string path, ListView listView)
		{
			LoadFtpDirectoryAsync(connectionName, path, listView).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 处理FTP文件或文件夹的右键菜单
		/// </summary>
		/// <param name="connectionName">连接名称</param>
		/// <param name="item">选中的ListViewItem</param>
		/// <param name="location">鼠标位置</param>
		public void ShowFtpContextMenu(string connectionName, ListViewItem item, Point location)
		{
			if (_ftpSources.TryGetValue(connectionName, out AsyncFtpFileSource source))
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
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItemAsync(source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItemAsync(source, path, true));
					contextMenu.Items.Add("下载", null, (s, e) => DownloadListAsync(source, path, true));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadListAsync(source, path, true));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemPropertiesAsync(source, path, true));
				}
				else
				{
					// 文件菜单项
					contextMenu.Items.Add("查看", null, (s, e) => ViewFtpFileAsync(source, path));
					contextMenu.Items.Add("编辑", null, (s, e) => EditFtpFileAsync(source, path));
					contextMenu.Items.Add("复制", null, (s, e) => CopyFtpItem(source, path));
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItemAsync(source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItemAsync(source, path, false));
					contextMenu.Items.Add("下载", null, (s, e) => DownloadListAsync(source, path, false));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadListAsync(source, path, false));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemPropertiesAsync(source, path, false));
				}

				// 显示菜单
				contextMenu.Show(location);
			}
		}

		/// <summary>
		/// 异步查看FTP文件
		/// </summary>
		private async Task ViewFtpFileAsync(AsyncFtpFileSource source, string path)
		{
			try
			{
				// 异步下载文件到临时目录
				string localPath = await source.DownloadFileAsync(path);
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
		/// 同步版本的查看FTP文件（为了兼容性）
		/// </summary>
		private void ViewFtpFile(AsyncFtpFileSource source, string path)
		{
			ViewFtpFileAsync(source, path).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 异步编辑FTP文件
		/// </summary>
		private async Task EditFtpFileAsync(AsyncFtpFileSource source, string path)
		{
			try
			{
				// 异步下载文件到临时目录
				string localPath = await source.DownloadFileAsync(path);
				if (!string.IsNullOrEmpty(localPath))
				{
					// 调用CmdProc的do_cm_edit方法编辑文件
					form.do_cm_edit(localPath);

					// 监视文件变化，如果有修改则上传
					FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(localPath), Path.GetFileName(localPath));
					watcher.NotifyFilter = NotifyFilters.LastWrite;
					watcher.Changed += async (s, e) =>
					{
						if (e.ChangeType == WatcherChangeTypes.Changed)
						{
							// 确保文件不再被占用
							System.Threading.Thread.Sleep(500);
							await source.UploadFileAsync(localPath, path);
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
		/// 同步版本的编辑FTP文件（为了兼容性）
		/// </summary>
		private void EditFtpFile(AsyncFtpFileSource source, string path)
		{
			EditFtpFileAsync(source, path).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 复制FTP项目
		/// </summary>
		private void CopyFtpItem(AsyncFtpFileSource source, string path)
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
						Task.Run(async () => await DownloadDirectoryAsync(source, path, localTargetPath));
					}
					else
					{
						// 下载单个文件
						Task.Run(async () => await source.Client.DownloadFile(localTargetPath, path));
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
		/// 异步递归下载目录
		/// </summary>
		private async Task DownloadDirectoryAsync(AsyncFtpFileSource source, string remotePath, string localPath)
		{
			// 获取目录列表
			var listing = await source.Client.GetListing(remotePath, _listOption);

			foreach (var item in listing)
			{
				string remoteFilePath = item.FullName;
				string localFilePath = Path.Combine(localPath, item.Name);

				if (item.Type == FtpObjectType.Directory)
				{
					// 创建本地目录
					Directory.CreateDirectory(localFilePath);
					// 递归下载子目录
					await DownloadDirectoryAsync(source, remoteFilePath, localFilePath);
				}
				else
				{
					// 下载文件
					await source.Client.DownloadFile(localFilePath, remoteFilePath);
				}
			}
		}

		/// <summary>
		/// 同步版本的递归下载目录（为了兼容性）
		/// </summary>
		private void DownloadDirectory(AsyncFtpFileSource source, string remotePath, string localPath)
		{
			DownloadDirectoryAsync(source, remotePath, localPath).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 异步重命名FTP项目
		/// </summary>
		private async Task RenameFtpItemAsync(AsyncFtpFileSource source, string path)
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
					if (await source.RenameAsync(path, newPath))
					{
						// 刷新列表
						await LoadFtpDirectoryAsync(source.ConnectionName, parentPath, form.activeListView);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 同步版本的重命名FTP项目（为了兼容性）
		/// </summary>
		private void RenameFtpItem(AsyncFtpFileSource source, string path)
		{
			RenameFtpItemAsync(source, path).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 异步删除FTP项目
		/// </summary>
		private async Task DeleteFtpItemAsync(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			try
			{
				if (MessageBox.Show($"确定要删除 {Path.GetFileName(path)} 吗？", "确认删除",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					bool success;
					if (isDirectory)
					{
						success = await source.DeleteDirectoryAsync(path);
					}
					else
					{
						success = await source.DeleteFileAsync(path);
					}

					if (success)
					{
						// 获取父目录路径
						string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
						if (!parentPath.EndsWith("/"))
							parentPath += "/";

						// 刷新列表
						await LoadFtpDirectoryAsync(source.ConnectionName, parentPath, form.activeListView);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 同步版本的删除FTP项目（为了兼容性）
		/// </summary>
		private void DeleteFtpItem(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			DeleteFtpItemAsync(source, path, isDirectory).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 异步添加到下载列表
		/// </summary>
		public async Task DownloadListAsync(AsyncFtpFileSource source, string path, bool isDirectory)
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
						Task.Run(async () =>
						{
							try
							{
								await DownloadDirectoryAsync(source, path, localTargetPath);
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
						Task.Run(async () =>
						{
							try
							{
								await source.Client.DownloadFile(localTargetPath, path);
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

		/// <summary>
		/// 异步添加到下载列表
		/// </summary>
		public async Task AddToDownloadListAsync(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			// 这里可以实现一个下载队列管理器
			// 简单实现：添加到下载列表
			try
			{
				// 显示目标选择对话框
				FolderBrowserDialog dialog = new FolderBrowserDialog();
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					string targetPath = dialog.SelectedPath;
					string fileName = Path.GetFileName(path);
					string localTargetPath = Path.Combine(targetPath, fileName);

					// 添加到下载列表（这里可以扩展为实际的下载队列管理）
					MessageBox.Show($"已添加到下载列表: {path} -> {localTargetPath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"添加到下载列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 同步版本的添加到下载列表（为了兼容性）
		/// </summary>
		public void AddToDownloadList(AsyncFtpFileSource source, string path, bool isDirectory)
		{
			AddToDownloadListAsync(source, path, isDirectory).GetAwaiter().GetResult();
		}

		#region 构造函数

		/// <summary>
		/// 初始化FTP管理器
		/// </summary>
		public AsyncFTPMGR(Form1 form)
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
			_connectionMonitor = new AsyncFtpConnectionMonitor(this);
		}

		#endregion

		~AsyncFTPMGR()
		{
			// 释放连接监视器资源
			_connectionMonitor?.Dispose();
		}

		#region 连接管理

		/// <summary>
		/// 异步连接到FTP服务器
		/// </summary>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否连接成功</returns>
		public async Task<bool> ConnectAsync(string connectionName)
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
					await _activeClient.Disconnect();
				}

				// 创建新的异步FTP客户端
				_activeClient = new AsyncFtpClient(
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
				var profile = await _activeClient.AutoConnect();  //connect()
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
		/// 同步版本的连接到FTP服务器（为了兼容性）
		/// </summary>
		public bool Connect(string connectionName)
		{
			return ConnectAsync(connectionName).GetAwaiter().GetResult();
		}

		/// <summary>
		/// 异步关闭当前连接
		/// </summary>
		public async Task CloseConnectionAsync()
		{
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
				await ActiveClient.Disconnect();
				form.uiManager.ftpController.UpdateStatus(false);
				form.activeListView.Items.Clear();
				// 如果找到了连接名称，注销FTP连接
				if (!string.IsNullOrEmpty(connectionName))
				{
					await UnregisterFtpConnectionAsync(connectionName);
					_connectionMonitor.RemoveConnection(connectionName); //，从监视器中移除
				}
			}
		}

		/// <summary>
		/// 同步版本的关闭当前连接（为了兼容性）
		/// </summary>
		public void CloseConnection()
		{
			CloseConnectionAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		/// 初始化连接配置
		/// </summary>
		private void Init()
		{
			// 从配置文件加载FTP连接信息
			// 这里可以实现从配置文件加载连接信息的逻辑
		}

		/// <summary>
		/// 获取所有FTP连接信息
		/// </summary>
		/// <returns>FTP连接信息列表</returns>
		public List<FtpConnectionInfo> GetConnections()
		{ 
			return _connections.Values.ToList();
		}

			#endregion
	}
}