using FluentFTP;
using System.Diagnostics;
using System.Text;
using static OpenQA.Selenium.BiDi.Modules.BrowsingContext.Locator;
namespace zfile
{
	/// <summary>
	/// FTP文件源实现，用于处理FTP虚拟文件系统
	/// </summary>
	public class FtpFileSource : FileSourceBase
	{
		/// <summary>
		/// 将FTP文件属性转换为L777格式的字符串
		/// </summary>
		/// <param name="item">FTP文件项</param>
		/// <returns>格式化的属性字符串</returns>
		private string GetFtpAttributesString(FtpListItem item)
		{
			StringBuilder sb = new StringBuilder("-----");

			// 检查是否为链接文件
			if (item.Type == FtpObjectType.Link)
				sb[0] = 'L';

			// 处理权限信息 (使用777格式：RWX分别对应421)
			int ownerValue = 0;
			int groupValue = 0;
			int othersValue = 0;

			// 所有者权限
			if (item.OwnerPermissions.HasFlag(FtpPermission.Read))
				ownerValue += 4;
			if (item.OwnerPermissions.HasFlag(FtpPermission.Write))
				ownerValue += 2;
			if (item.OwnerPermissions.HasFlag(FtpPermission.Execute))
				ownerValue += 1;

			// 组权限
			if (item.GroupPermissions.HasFlag(FtpPermission.Read))
				groupValue += 4;
			if (item.GroupPermissions.HasFlag(FtpPermission.Write))
				groupValue += 2;
			if (item.GroupPermissions.HasFlag(FtpPermission.Execute))
				groupValue += 1;

			// 其他用户权限
			if (item.OthersPermissions.HasFlag(FtpPermission.Read))
				othersValue += 4;
			if (item.OthersPermissions.HasFlag(FtpPermission.Write))
				othersValue += 2;
			if (item.OthersPermissions.HasFlag(FtpPermission.Execute))
				othersValue += 1;

			// 设置权限值
			sb[1] = ownerValue.ToString()[0];
			sb[2] = groupValue.ToString()[0];
			sb[3] = othersValue.ToString()[0];

			return sb.ToString();
		}
		private FtpClient _client;
		private AsyncFtpClient _clientAsync;
		private string _currentPath = "/";
		private string _ftpHost;
		private string _connectionName;
		private Form1 _owner;

		public FtpFileSource()
		{
		}

		public FtpFileSource(Form1 owner, string connectionName, FtpClient client)
		{
			_owner = owner;
			_connectionName = connectionName;
			_client = client;
			_ftpHost = client.Host;
		}

		public override bool IsSupportedPath(string path)
		{
			return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
				   path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase);
		}

		public override void Initialize()
		{
			// 初始化FTP连接已在构造函数中完成
		}

		public override void Finalize()
		{
			// 断开FTP连接
			_client?.Disconnect();
		}

		/// <summary>
		/// 获取当前FTP路径下的文件和文件夹列表
		/// </summary>
		/// <param name="path">FTP路径</param>
		/// <returns>文件和文件夹列表</returns>
		public List<ListViewItem> GetListing(string path = "", FtpListOption listOption = FtpListOption.Auto)
		{
			if (string.IsNullOrEmpty(path))
				path = _currentPath;

			var items = new List<ListViewItem>();

			try
			{
				// 获取FTP目录列表
				var listing = _client.GetListing(path, listOption);

				foreach (var item in listing)
				{
					// 创建ListViewItem
					var listItem = new ListViewItem(item.Name);

					// 添加子项
					listItem.SubItems.Add(item.FullName); // 完整路径作为第二列

					// 根据类型设置不同的显示
					if (item.Type == FtpObjectType.Directory)
					{
						listItem.SubItems.Add(""); // 大小
						listItem.SubItems.Add("<DIR>"); // 类型
					}
					else
					{
						listItem.SubItems.Add(FileSystemManager.FormatFileSize(item.Size, true)); // 格式化文件大小
						listItem.SubItems.Add(Path.GetExtension(item.Name).TrimStart('.')); // 扩展名
					}
					listItem.SubItems.Add(item.Modified.ToString()); // 修改时间
					listItem.SubItems.Add(item.Size.ToString()); //real size

					// 添加FTP文件属性列
					string attrStr = GetFtpAttributesString(item);
					listItem.SubItems.Add(attrStr); // 属性

					// 设置图标
					listItem.ImageKey = item.Type == FtpObjectType.Directory ? "folder" : GetFileIconKey(item.Name);

					// 添加到列表
					items.Add(listItem);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"获取FTP目录列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return items;
		}

		/// <summary>
		/// 下载FTP文件到本地临时目录
		/// </summary>
		/// <param name="remotePath">远程文件路径</param>
		/// <returns>本地临时文件路径</returns>
		public string DownloadFile(string remotePath)
		{
			try
			{
				// 创建临时目录
				string tempDir = Path.Combine(Path.GetTempPath(), "FtpTemp");
				if (!Directory.Exists(tempDir))
					Directory.CreateDirectory(tempDir);

				// 生成临时文件路径
				string fileName = Path.GetFileName(remotePath);
				string localPath = Path.Combine(tempDir, fileName);

				// 下载文件
				var success = _client.DownloadFile(localPath, remotePath);

				if (success.HasFlag(FtpStatus.Success))
					return localPath;
				else
					throw new Exception("下载失败");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"下载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
		}
		public bool DownloadCanBeResumed()
		{
			// 检查服务器是否支持续传
			if (!_client.HasFeature(FtpCapability.REST))
			{
				Debug.Print("服务器不支持断点续传！");
				return false;
			}
			return true;
		}
		//public CancellationTokenSource DownloadFileAsync(string remotePath)
		//{
		//	try
		//	{
		//		var cts = new CancellationTokenSource();
		//		// 创建临时目录
		//		string tempDir = Path.Combine(Path.GetTempPath(), "FtpTemp");
		//		if (!Directory.Exists(tempDir))
		//			Directory.CreateDirectory(tempDir);

		//		// 生成临时文件路径
		//		string fileName = Path.GetFileName(remotePath);
		//		string localPath = Path.Combine(tempDir, fileName);

		//		await _client.DownloadFileAsync(
		//			localPath: localPath,
		//			remotePath: remotePath,
		//			existsMode: FtpRemoteExists.Resume,
		//			cancellationToken: cts.Token
		//		);
		//		Debug.Print("下载完成！");
		//	}
		//	catch (OperationCanceledException)
		//	{
		//		Debug.Print("下载已取消，可稍后恢复。");
		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.Print($"下载失败: {ex.Message}");
		//	}
		//}
		/// <summary>
		/// 上传文件到FTP服务器
		/// </summary>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <returns>是否上传成功</returns>
		public bool UploadFile(string localPath, string remotePath)
		{
			try
			{
				var result = _client.UploadFile(localPath, remotePath);
				return result.HasFlag(FtpStatus.Success);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"上传文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 创建FTP目录
		/// </summary>
		/// <param name="path">目录路径</param>
		/// <returns>是否创建成功</returns>
		public bool CreateDirectory(string path)
		{
			try
			{
				_client.CreateDirectory(path);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 删除FTP文件
		/// </summary>
		/// <param name="path">文件路径</param>
		/// <returns>是否删除成功</returns>
		public bool DeleteFile(string path)
		{
			try
			{
				_client.DeleteFile(path);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"删除文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 删除FTP目录
		/// </summary>
		/// <param name="path">目录路径</param>
		/// <returns>是否删除成功</returns>
		public bool DeleteDirectory(string path)
		{
			try
			{
				_client.DeleteDirectory(path);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"删除目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 重命名FTP文件或目录
		/// </summary>
		/// <param name="oldPath">原路径</param>
		/// <param name="newPath">新路径</param>
		/// <returns>是否重命名成功</returns>
		public bool Rename(string oldPath, string newPath)
		{
			try
			{
				_client.Rename(oldPath, newPath);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 格式化文件大小显示
		/// </summary>
		/// <param name="size">文件大小（字节）</param>
		/// <returns>格式化后的文件大小</returns>
		//private string FormatFileSize(long size)
		//{
		//    string[] units = { "B", "KB", "MB", "GB", "TB" };
		//    double dSize = size;
		//    int unitIndex = 0;

		//    while (dSize >= 1024 && unitIndex < units.Length - 1)
		//    {
		//        dSize /= 1024;
		//        unitIndex++;
		//    }

		//    return $"{dSize:0.##} {units[unitIndex]}";
		//}

		/// <summary>
		/// 根据文件名获取图标键
		/// </summary>
		/// <param name="fileName">文件名</param>
		/// <returns>图标键</returns>
		private string GetFileIconKey(string fileName)
		{
			string extension = Path.GetExtension(fileName).ToLower();

			// 根据扩展名返回不同的图标键
			switch (extension)
			{
				case ".txt":
					return "text";
				case ".pdf":
					return "pdf";
				case ".doc":
				case ".docx":
					return "word";
				case ".xls":
				case ".xlsx":
					return "excel";
				case ".jpg":
				case ".jpeg":
				case ".png":
				case ".gif":
				case ".bmp":
					return "image";
				case ".zip":
				case ".rar":
				case ".7z":
					return "archive";
				case ".exe":
					return "executable";
				default:
					return "file";
			}
		}

		/// <summary>
		/// 获取FTP连接名称
		/// </summary>
		public string ConnectionName => _connectionName;

		/// <summary>
		/// 获取FTP主机地址
		/// </summary>
		public string Host => _ftpHost;

		/// <summary>
		/// 获取或设置当前FTP路径
		/// </summary>
		public string CurrentPath
		{
			get => _currentPath;
			set => _currentPath = value;
		}

		/// <summary>
		/// 获取FTP客户端实例
		/// </summary>
		public FtpClient Client => _client;
	}
}