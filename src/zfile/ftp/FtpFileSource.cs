using FluentFTP;
using System.Diagnostics;
using System.Text;
using static OpenQA.Selenium.BiDi.Modules.BrowsingContext.Locator;
using static System.Net.WebRequestMethods;
namespace zfile
{
	/// <summary>
	/// FTP文件源实现，用于处理FTP虚拟文件系统
	/// </summary>
	public class FtpFileSource : FileSource
	{
		private FtpClient? _client;
		private readonly string? _ftpHost;
		private readonly string? _connectionName;
		public override char PathSep => '/';
		/// <summary>
		/// 操作类字典
		/// </summary>
		private readonly Dictionary<FileSourceOperationTypes, Type> _operationsClasses;

		/// <summary>
		/// 获取支持的操作类型
		/// </summary>
		public override FileSourceOperationTypes OperationsTypes =>
			FileSourceOperationTypes.List |
			FileSourceOperationTypes.CopyIn |
			FileSourceOperationTypes.CopyOut |
			FileSourceOperationTypes.Delete |
			FileSourceOperationTypes.CreateDirectory |
			FileSourceOperationTypes.Execute |
			FileSourceOperationTypes.Move |
			FileSourceOperationTypes.CalcStatistics;
		/// <summary>
		/// 获取FTP连接名称
		/// </summary>
		public string ConnectionName => _connectionName ?? string.Empty;

		/// <summary>
		/// 获取FTP主机地址
		/// </summary>
		public string Host => _ftpHost ?? string.Empty;

		/// <summary>
		/// 获取FTP客户端实例
		/// </summary>
		public FtpClient? Client => _client;

		public FtpFileSource()
		{
			_operationsClasses = [];
			InitializeOperationsClasses();
			_rootpath = PathSep.ToString();
		}

		public FtpFileSource(string connectionName, FtpClient client)
		{
			_connectionName = connectionName;
			_client = client;
			_ftpHost = client.Host;
			_operationsClasses = [];
			InitializeOperationsClasses();
		}

		/// <summary>
		/// 初始化操作类字典
		/// </summary>
		private void InitializeOperationsClasses()
		{
			// 注册操作类
			_operationsClasses[FileSourceOperationTypes.List] = typeof(FtpListOperation);
			_operationsClasses[FileSourceOperationTypes.CopyIn] = typeof(FtpCopyInOperation);
			_operationsClasses[FileSourceOperationTypes.CopyOut] = typeof(FtpCopyOutOperation);
			_operationsClasses[FileSourceOperationTypes.Delete] = typeof(FtpDeleteOperation);
			_operationsClasses[FileSourceOperationTypes.CreateDirectory] = typeof(FtpCreateDirectoryOperation);
			_operationsClasses[FileSourceOperationTypes.Execute] = typeof(FtpExecuteOperation);
			_operationsClasses[FileSourceOperationTypes.Move] = typeof(FtpMoveOperation);
			_operationsClasses[FileSourceOperationTypes.CalcStatistics] = typeof(FtpCalcStatisticsOperation);
		}
		/// <summary>
		/// 将FTP文件属性转换为L777格式的字符串
		/// </summary>
		/// <param name="item">FTP文件项</param>
		/// <returns>格式化的属性字符串</returns>
		private static string GetFtpAttributesString(FtpListItem item)
		{
			var sb = new StringBuilder("-----");

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

		public override bool IsSupportedPath(string path)
		{
			return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
				   path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase);
		}
	
		public override string CurrentFullPath
		{
			get => $"ftp://{Host}{CurrentPath}"; 
			set => CurrentPath = Helper.IncludeTrailingPathDelimiter(GetRelativePath(value), '/'); 
		}
		public override void Initialize()
		{
			// 初始化FTP连接已在构造函数中完成
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_client?.Disconnect();
				// 释放托管资源
				_client?.Dispose();
				_client = null;
			}
			// 释放非托管资源
		}

		/// <summary>
		/// 获取当前FTP路径下的文件和文件夹列表
		/// </summary>
		/// <param name="path">FTP路径</param>
		/// <returns>文件和文件夹列表</returns>
		public List<ListViewItem> GetListing(string path = "", FtpListOption listOption = FtpListOption.Auto)
		{
			if (string.IsNullOrEmpty(path))
				path = CurrentPath;

			var items = new List<ListViewItem>();

			try
			{
				// 获取FTP目录列表
				var listing = _client?.GetListing(path, listOption);

				if (listing != null)
				{
					foreach (var item in listing)
					{
						// 创建ListViewItem
						var listItem = new ListViewItem(item.Name);

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

						// 添加FTP文件属性列
						string attrStr = GetFtpAttributesString(item);
						listItem.SubItems.Add(attrStr); // 属性

						// 设置图标
						listItem.ImageKey = item.Type == FtpObjectType.Directory ? "folder" : GetFileIconKey(item.Name);
						var fileentry = CreateFile(item.FullName);
						fileentry.ModificationTime = item.Modified;
						fileentry.CreationTime = item.Created;
						fileentry.IsDirectory = item.Type == FtpObjectType.Directory;
						fileentry.Size = item.Size;

						listItem.Tag = new LvItemTag(fileentry, null);  // 将文件对象存储在Tag属性中
						
						// 添加到列表
						items.Add(listItem);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"获取FTP目录列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return items;
		}
		public override FileEntry CreateFile(string path)
		{
			var filename = Path.GetFileName(path);
			var dir = Path.GetDirectoryName(path) ?? string.Empty;
			return new FileEntry(dir, filename);
		}
		/// <summary>
		/// 下载FTP文件到本地临时目录
		/// </summary>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径，不包括文件名，若为空，则下载到临时文件夹</param>
		/// <returns>本地临时文件路径</returns>
		public string DownloadFile(string remotePath, string? localpath = null)
		{
			try
			{
				string? tempDir = null;
				if (localpath == null)
				{
					// 创建临时目录
					tempDir = Path.Combine(Path.GetTempPath(), "FtpTemp");
					if (!Directory.Exists(tempDir))
						Directory.CreateDirectory(tempDir);
				}
				// 生成临时文件路径
				string fileName = Path.GetFileName(remotePath);
				string localPath = Path.Combine(tempDir ?? localpath, fileName);

				// 下载文件
				var success = _client?.DownloadFile(localPath, remotePath) ?? FtpStatus.Failed;

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
			if (_client == null || !_client.HasFeature(FtpCapability.REST))
			{
				Debug.Print("服务器不支持断点续传！");
				return false;
			}
			return true;
		}
	
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
				var result = _client?.UploadFile(localPath, remotePath) ?? FtpStatus.Failed;
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
		public new bool CreateDirectory(string path)
		{
			try
			{
				_client?.CreateDirectory(path);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		/// <summary>
		/// 创建目录操作
		/// </summary>
		/// <param name="basePath">基础路径</param>
		/// <param name="directoryPath">目录路径</param>
		/// <returns>目录创建操作</returns>
		public override FileSourceCreateDirectoryOperation CreateCreateDirectoryOperation(string basePath, string directoryPath)
		{
			return new FtpCreateDirectoryOperation(this, GetRelativePath(basePath), directoryPath);
		}

		/// <summary>
		/// 创建列表操作
		/// </summary>
		/// <param name="path">路径</param>
		/// <returns>列表操作</returns>
		public override FileSourceListOperation CreateListOperation(string path)
		{
			return new FtpListOperation(this, GetRelativePath(path));
		}
		public string GetRelativePath(string targetPath)
		{
			if (targetPath.StartsWith("ftp://"))
				return targetPath.Replace($"ftp://{_ftpHost}", string.Empty);   //去掉ftp://和主机名部分，
			if (targetPath.Length > 2 && targetPath[1].Equals(':')) //处理虚拟盘符的情况（比如"F:*" 去除虚拟盘符）
				return targetPath.Substring(2);
			return targetPath;
		}
		/// <summary>
		/// 创建复制入操作
		/// </summary>
		/// <param name="sourceFileSource">源文件源</param>
		/// <param name="sourceFiles">源文件列表</param>
		/// <param name="targetPath">目标路径</param>
		/// <returns>复制入操作</returns>
		public override FileSourceCopyInOperation CreateCopyInOperation(IFileSource sourceFileSource, FileEntries sourceFiles, string targetPath)
		{
			return new FtpCopyInOperation(sourceFileSource, this, sourceFiles, GetRelativePath(targetPath));
		}

		/// <summary>
		/// 创建复制出操作
		/// </summary>
		/// <param name="sourceFiles">源文件列表</param>
		/// <param name="targetFileSource">目标文件源</param>
		/// <param name="targetPath">目标路径</param>
		/// <returns>复制出操作</returns>
		public override FileSourceCopyOutOperation CreateCopyOutOperation(IFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
		{
			return new FtpCopyOutOperation(this, targetFileSource, sourceFiles.Path.Contains('\\') ? FileSourceUtil.FileEntryListToFileEntries(sourceFiles.List) : sourceFiles, targetPath);
		}

		/// <summary>
		/// 创建删除操作
		/// </summary>
		/// <param name="filesToDelete">要删除的文件列表</param>
		/// <returns>删除操作</returns>
		public override FileSourceDeleteOperation CreateDeleteOperation(FileEntries filesToDelete)
		{
			return new FtpDeleteOperation(this, filesToDelete);
		}

		/// <summary>
		/// 创建执行操作
		/// </summary>
		/// <param name="executableFile">可执行文件</param>
		/// <param name="basePath">基础路径</param>
		/// <param name="parameters">执行参数</param>
		/// <returns>执行操作</returns>
		public override FileSourceExecuteOperation CreateExecuteOperation(FileEntry executableFile, string basePath, string parameters)
		{
			return new FtpExecuteOperation(this, executableFile, basePath, parameters);
		}

		/// <summary>
		/// 创建移动操作
		/// </summary>
		/// <param name="sourceFiles">源文件列表</param>
		/// <param name="targetPath">目标路径</param>
		/// <returns>移动操作</returns>
		public override FileSourceMoveOperation CreateMoveOperation(FileEntries sourceFiles, string targetPath)
		{
			return new FtpMoveOperation(this, sourceFiles, targetPath);
		}

		/// <summary>
		/// 创建计算统计信息操作
		/// </summary>
		/// <param name="files">文件列表</param>
		/// <returns>计算统计信息操作</returns>
		public override FileSourceCalcStatisticsOperation CreateCalcStatisticsOperation(FileEntries files)
		{
			return new FtpCalcStatisticsOperation(this, files);
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
				_client?.DeleteFile(path);
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
				_client?.DeleteDirectory(path);
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
				_client?.Rename(oldPath, newPath);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}


		/// <summary>
		/// 根据文件名获取图标键
		/// </summary>
		/// <param name="fileName">文件名</param>
		/// <returns>图标键</returns>
		private static string GetFileIconKey(string fileName)
		{
			string extension = Path.GetExtension(fileName).ToLower();

			// 根据扩展名返回不同的图标键
			return extension switch
			{
				".txt" => "text",
				".pdf" => "pdf",
				".doc" => "word",
				".docx" => "word",
				".xls" => "excel",
				".xlsx" => "excel",
				".jpg" => "image",
				".jpeg" => "image",
				".png" => "image",
				".gif" => "image",
				".bmp" => "image",
				".zip" => "archive",
				".rar" => "archive",
				".7z" => "archive",
				".exe" => "executable",
				_ => "file"
			};
		}
		public override string GetRootDir()
		{
			return $"ftp://{Host}/";
		}
	}
}