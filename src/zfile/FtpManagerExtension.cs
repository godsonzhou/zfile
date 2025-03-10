using FluentFTP;
using System.Collections.Generic;
using System.IO;
using System.Net;
using WinFormsApp1;
using zfile;

namespace WinFormsApp1
{
	/// <summary>
	/// FTP管理器扩展类，用于处理FTP连接和UI交互
	/// </summary>
	public static class FtpManagerExtension
	{
		private static readonly Dictionary<string, TreeNode> _ftpNodes = new Dictionary<string, TreeNode>();
		private static readonly Dictionary<string, FtpFileSource> _ftpSources = new Dictionary<string, FtpFileSource>();
		private static readonly List<string> _registeredDrives = new List<string>();
		private static TreeNode _ftpRootNode;
		private static VfsModuleManager _vfsManager;

		/// <summary>
		/// 显示FTP项目属性
		/// </summary>
		private static void ShowFtpItemProperties(Form1 form, FtpFileSource source, string path, bool isDirectory)
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
		public static void HandleFtpNodeDoubleClick(Form1 form, TreeNode node, ListView listView)
		{
			if (node.Tag is FtpNodeTag tag)
			{
				// 加载FTP目录内容
				LoadFtpDirectory(form, tag.ConnectionName, tag.Path, listView);
			}
		}

		/// <summary>
		/// 处理FTP列表项双击事件
		/// </summary>
		public static void HandleFtpListItemDoubleClick(Form1 form, string connectionName, ListViewItem item, ListView listView)
		{
			bool isDirectory = item.SubItems[3].Text == "<DIR>";
			string path = item.SubItems[1].Text;

			if (isDirectory)
			{
				// 如果是目录，进入该目录
				LoadFtpDirectory(form, connectionName, path, listView);

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
					ViewFtpFile(form, source, path);
				}
			}
		}

		/// <summary>
		/// 初始化FTP管理器扩展
		/// </summary>
		/// <param name="form">主窗体</param>
		public static void Initialize(Form1 form)
		{
			// 初始化VFS管理器
			_vfsManager = new VfsModuleManager();
			_vfsManager.RegisterVirtualFileSource<FtpFileSource>("FTP", true);

			// 创建FTP根节点
			CreateFtpRootNode(form);

			// 加载已保存的FTP连接
			LoadSavedFtpConnections(form);
		}

		/// <summary>
		/// 创建FTP根节点
		/// </summary>
		/// <param name="form">主窗体</param>
		private static void CreateFtpRootNode(Form1 form)
		{
			// 在桌面节点下创建FTP连接节点
			_ftpRootNode = new TreeNode("FTP连接")
			{
				ImageKey = "folder",
				SelectedImageKey = "folder"
			};

			// 添加到左侧和右侧树视图的桌面节点下
			if (form.leftRoot != null && form.rightRoot != null)
			{
				form.leftRoot.Nodes.Add(_ftpRootNode);
				form.rightRoot.Nodes.Add((TreeNode)_ftpRootNode.Clone());
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
		private static void LoadSavedFtpConnections(Form1 form)
		{
			// 从配置加载FTP连接
			var connections = form.fTPMGR.GetConnections();
			foreach (var conn in connections)
			{
				RegisterFtpConnection(form, conn.Name);
			}
		}

		/// <summary>
		/// 注册FTP连接为虚拟盘
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否注册成功</returns>
		public static bool RegisterFtpConnection(Form1 form, string connectionName)
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
					_ftpRootNode.Nodes.Add(ftpNode);
					_ftpNodes[connectionName] = ftpNode;
					_registeredDrives.Add(driveId);

					// 添加到DriveComboBox
					AddToDriveComboBox(form, connectionName, driveId);

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
		/// 获取下一个可用的驱动器盘符
		/// </summary>
		/// <returns>可用的驱动器盘符</returns>
		private static char GetNextAvailableDriveLetter()
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
		private static void AddToDriveComboBox(Form1 form, string connectionName, string driveId)
		{
			// 添加到左侧和右侧驱动器下拉框
			form.uiManager.LeftDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
			form.uiManager.RightDriveComboBox.Items.Add($"{driveId} [{connectionName}]");
		}

		/// <summary>
		/// 取消注册FTP连接
		/// </summary>
		/// <param name="form">主窗体</param>
		/// <param name="connectionName">连接名称</param>
		/// <returns>是否取消注册成功</returns>
		public static bool UnregisterFtpConnection(Form1 form, string connectionName)
		{
			try //TODO: 在ftp disconnect时调用
			{
				if (_ftpNodes.TryGetValue(connectionName, out TreeNode node))
				{
					// 获取驱动器标识符
					string nodeText = node.Text;
					string driveId = nodeText.Substring(nodeText.IndexOf('(') + 1, 2);

					// 从树视图中移除节点
					_ftpRootNode.Nodes.Remove(node);
					_ftpNodes.Remove(connectionName);

					// 从驱动器列表中移除
					_registeredDrives.Remove(driveId);

					// 从驱动器下拉框中移除
					RemoveFromDriveComboBox(form, driveId);

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
		private static void RemoveFromDriveComboBox(Form1 form, string driveId)
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
		public static void LoadFtpDirectory(Form1 form, string connectionName, string path, ListView listView)
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
		public static void ShowFtpContextMenu(Form1 form, string connectionName, ListViewItem item, Point location)
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
					contextMenu.Items.Add("复制", null, (s, e) => CopyFtpItem(form, source, path));
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItem(form, source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItem(form, source, path, true));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadList(form, source, path, true));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemProperties(form, source, path, true));
				}
				else
				{
					// 文件菜单项
					contextMenu.Items.Add("查看", null, (s, e) => ViewFtpFile(form, source, path));
					contextMenu.Items.Add("编辑", null, (s, e) => EditFtpFile(form, source, path));
					contextMenu.Items.Add("复制", null, (s, e) => CopyFtpItem(form, source, path));
					contextMenu.Items.Add("重命名", null, (s, e) => RenameFtpItem(form, source, path));
					contextMenu.Items.Add("删除", null, (s, e) => DeleteFtpItem(form, source, path, false));
					contextMenu.Items.Add("添加到下载列表", null, (s, e) => AddToDownloadList(form, source, path, false));
					contextMenu.Items.Add("属性", null, (s, e) => ShowFtpItemProperties(form, source, path, false));
				}

				// 显示菜单
				contextMenu.Show(form, location);
			}
		}

		/// <summary>
		/// 查看FTP文件
		/// </summary>
		private static void ViewFtpFile(Form1 form, FtpFileSource source, string path)
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
		private static void EditFtpFile(Form1 form, FtpFileSource source, string path)
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
		private static void CopyFtpItem(Form1 form, FtpFileSource source, string path)
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
		private static void RenameFtpItem(Form1 form, FtpFileSource source, string path)
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
						LoadFtpDirectory(form, source.ConnectionName, parentPath, form.activeListView);
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
		private static void DeleteFtpItem(Form1 form, FtpFileSource source, string path, bool isDirectory)
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
						LoadFtpDirectory(form, source.ConnectionName, parentPath, form.activeListView);
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
		private static void AddToDownloadList(Form1 form, FtpFileSource source, string path, bool isDirectory)
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

	}
}
