using FluentFTP;
using System.Diagnostics;
using System.Text;

namespace Zfile
{
    /// <summary>
    /// FTP文件源异步实现，用于处理FTP虚拟文件系统的异步操作
    /// </summary>
    public class AsyncFtpFileSource : FileSourceBase
    {
		private AsyncFtpClient _client;
		private string _currentPath = "/";
		private string _ftpHost;
		private string _connectionName;
		private MainForm _owner;

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

        public AsyncFtpFileSource()
        {
        }

        public AsyncFtpFileSource(MainForm owner, string connectionName, AsyncFtpClient client)
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
        /// 异步断开FTP连接
        /// </summary>
        public async Task FinalizeAsync()
        {
            // 异步断开FTP连接
            if (_client != null)
            {
                await _client.Disconnect();
            }
        }

        /// <summary>
        /// 异步获取当前FTP路径下的文件和文件夹列表
        /// </summary>
        /// <param name="path">FTP路径</param>
        /// <param name="listOption">列表选项</param>
        /// <returns>文件和文件夹列表</returns>
        public async Task<List<ListViewItem>> GetListingAsync(string path = "", FtpListOption listOption = FtpListOption.Auto)
        {
            if (string.IsNullOrEmpty(path))
                path = _currentPath;

            var items = new List<ListViewItem>();

            try
            {
                // 异步获取FTP目录列表
                var listing = await _client.GetListing(path, listOption);

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
        /// 同步获取当前FTP路径下的文件和文件夹列表（为了兼容性）
        /// </summary>
        /// <param name="path">FTP路径</param>
        /// <param name="listOption">列表选项</param>
        /// <returns>文件和文件夹列表</returns>
        public List<ListViewItem> GetListing(string path = "", FtpListOption listOption = FtpListOption.Auto)
        {
            return GetListingAsync(path, listOption).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步下载FTP文件到本地临时目录
        /// </summary>
        /// <param name="remotePath">远程文件路径</param>
        /// <returns>本地临时文件路径</returns>
        public async Task<string> DownloadFileAsync(string remotePath)
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

                // 异步下载文件
                var success = await _client.DownloadFile(localPath, remotePath);

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

        /// <summary>
        /// 同步下载FTP文件到本地临时目录（为了兼容性）
        /// </summary>
        /// <param name="remotePath">远程文件路径</param>
        /// <returns>本地临时文件路径</returns>
        public string DownloadFile(string remotePath)
        {
            return DownloadFileAsync(remotePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 检查服务器是否支持断点续传
        /// </summary>
        /// <returns>是否支持断点续传</returns>
        public async Task<bool> DownloadCanBeResumedAsync()
        {
            // 检查服务器是否支持续传
            if (! _client.HasFeature(FtpCapability.REST))
            {
                Debug.Print("服务器不支持断点续传！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 同步检查服务器是否支持断点续传（为了兼容性）
        /// </summary>
        /// <returns>是否支持断点续传</returns>
        public bool DownloadCanBeResumed()
        {
            return DownloadCanBeResumedAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步下载文件，支持取消操作
        /// </summary>
        /// <param name="remotePath">远程文件路径</param>
        /// <param name="localPath">本地文件路径，如果为null则使用临时目录</param>
        /// <returns>取消令牌源</returns>
        public async Task<CancellationTokenSource> DownloadFileWithCancellationAsync(string remotePath, string localPath = null)
        {
            var cts = new CancellationTokenSource();
            try
            {
                // 创建临时目录
                if (localPath == null)
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "FtpTemp");
                    if (!Directory.Exists(tempDir))
                        Directory.CreateDirectory(tempDir);

                    // 生成临时文件路径
                    string fileName = Path.GetFileName(remotePath);
                    localPath = Path.Combine(tempDir, fileName);
                }

                // 异步下载文件，支持断点续传
                await _client.DownloadFile(
                    localPath: localPath,
                    remotePath: remotePath,
                    //existsMode: FtpRemoteExists.Resume,
                    token: cts.Token
                );
                Debug.Print("下载完成！");
                return cts;
            }
            catch (OperationCanceledException)
            {
                Debug.Print("下载已取消，可稍后恢复。");
                return cts;
            }
            catch (Exception ex)
            {
                Debug.Print($"下载失败: {ex.Message}");
                return cts;
            }
        }

        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="localPath">本地文件路径</param>
        /// <param name="remotePath">远程文件路径</param>
        /// <returns>是否上传成功</returns>
        public async Task<bool> UploadFileAsync(string localPath, string remotePath)
        {
            try
            {              
                var result = await _client.UploadFile(localPath, remotePath);
                return result.HasFlag(FtpStatus.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 同步上传文件到FTP服务器（为了兼容性）
        /// </summary>
        /// <param name="localPath">本地文件路径</param>
        /// <param name="remotePath">远程文件路径</param>
        /// <returns>是否上传成功</returns>
        public bool UploadFile(string localPath, string remotePath)
        {
            return UploadFileAsync(localPath, remotePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步创建FTP目录
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否创建成功</returns>
        public async Task<bool> CreateDirectoryAsync(string path)
        {
            try
            {
                await _client.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 同步创建FTP目录（为了兼容性）
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否创建成功</returns>
        public bool CreateDirectory(string path)
        {
            return CreateDirectoryAsync(path).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步删除FTP文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteFileAsync(string path)
        {
            try
            {
                await _client.DeleteFile(path);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 同步删除FTP文件（为了兼容性）
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteFile(string path)
        {
            return DeleteFileAsync(path).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步删除FTP目录
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteDirectoryAsync(string path)
        {
            try
            {
                await _client.DeleteDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 同步删除FTP目录（为了兼容性）
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteDirectory(string path)
        {
            return DeleteDirectoryAsync(path).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步重命名FTP文件或目录
        /// </summary>
        /// <param name="oldPath">原路径</param>
        /// <param name="newPath">新路径</param>
        /// <returns>是否重命名成功</returns>
        public async Task<bool> RenameAsync(string oldPath, string newPath)
        {
            try
            {
                await _client.Rename(oldPath, newPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 同步重命名FTP文件或目录（为了兼容性）
        /// </summary>
        /// <param name="oldPath">原路径</param>
        /// <param name="newPath">新路径</param>
        /// <returns>是否重命名成功</returns>
        public bool Rename(string oldPath, string newPath)
        {
            return RenameAsync(oldPath, newPath).GetAwaiter().GetResult();
        }

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
        public AsyncFtpClient Client => _client;
    }
}