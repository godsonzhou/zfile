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
} 