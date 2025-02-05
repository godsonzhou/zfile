namespace WinFormsApp1
{
	public class FileSystemManager
    {
        private readonly Dictionary<string, List<FileSystemInfo>> _directoryCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        public void CopyDirectory(string sourceDir, string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Copy(file, targetFilePath);
                }

                foreach (var directory in Directory.GetDirectories(sourceDir))
                {
                    var targetDirectoryPath = Path.Combine(targetDir, Path.GetFileName(directory));
                    CopyDirectory(directory, targetDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public List<FileSystemInfo> GetDirectoryContents(string path)
        {
            var currentTime = DateTime.Now;
            var needsUpdate = !_directoryCache.ContainsKey(path) ||
                            (currentTime - _lastCacheUpdate).TotalMilliseconds > Constants.CacheTimeout;

            if (needsUpdate)
            {
                var items = new List<FileSystemInfo>();
                var directoryInfo = new DirectoryInfo(path);

                try
                {
                    foreach (var dir in directoryInfo.GetDirectories())
                    {
                        if ((dir.Attributes & FileAttributes.Hidden) == 0)
                        {
                            items.Add(dir);
                        }
                    }

                    foreach (var file in directoryInfo.GetFiles())
                    {
                        if ((file.Attributes & FileAttributes.Hidden) == 0)
                        {
                            items.Add(file);
                        }
                    }

                    _directoryCache[path] = items;
                    _lastCacheUpdate = currentTime;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取目录内容失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return _directoryCache[path];
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public bool IsTextFile(string extension)
        {
            return Constants.TextFileExtensions.Contains(extension.ToLower());
        }

        public void DeleteFile(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void MoveFileOrDirectory(string sourcePath, string targetPath)
        {
            try
            {
                if (Directory.Exists(sourcePath))
                {
                    Directory.Move(sourcePath, targetPath);
                }
                else if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, targetPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

	class FileInfoList
	{
		public List<FileInfoWithIcon> list;
		public ImageList imageListLargeIcon;
		public ImageList imageListSmallIcon;

		/// <summary>
		/// 根据文件路径获取生成文件信息，并提取文件的图标
		/// </summary>
		/// <param name="filespath"></param>
		public FileInfoList(string[] filespath)
		{
			list = new List<FileInfoWithIcon>();
			imageListLargeIcon = new ImageList();
			imageListLargeIcon.ImageSize = new Size(32, 32);
			imageListSmallIcon = new ImageList();
			imageListSmallIcon.ImageSize = new Size(16, 16);
			foreach (string path in filespath)
			{
				FileInfoWithIcon file = new FileInfoWithIcon(path);
				imageListLargeIcon.Images.Add(file.largeIcon);
				imageListSmallIcon.Images.Add(file.smallIcon);
				file.iconIndex = imageListLargeIcon.Images.Count - 1;
				list.Add(file);
			}
		}
	}
	class FileInfoWithIcon
	{
		public FileInfo fileInfo;
		public Icon largeIcon;
		public Icon smallIcon;
		public int iconIndex;
		public FileInfoWithIcon(string path)
		{
			fileInfo = new FileInfo(path);
			largeIcon = IconManager.GetIconByFileName(path, true);
			if (largeIcon == null)
				largeIcon = IconManager.GetIconByFileType(Path.GetExtension(path), true);


			smallIcon = IconManager.GetIconByFileName(path, false);
			if (smallIcon == null)
				smallIcon = IconManager.GetIconByFileType(Path.GetExtension(path), false);
		}
	}
} 