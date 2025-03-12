using System.Diagnostics;

namespace zfile
{
	public class FileSystemManager
	{
		private readonly Dictionary<string, List<FileSystemInfo>> _directoryCache = new();
		private Dictionary<string, DateTime> _lastCacheUpdate = new();
		public bool isDirBranchMode = false;

		public static void CopyFilesAndDirectories(string sourcePath, string destinationFolder)
		{
			if (File.Exists(sourcePath))
			{
				// 如果是文件，直接复制
				string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
				string destinationFileDirectory = Path.GetDirectoryName(destinationFilePath);
				// 创建目标文件所在的目录
				if (!Directory.Exists(destinationFileDirectory))
				{
					Directory.CreateDirectory(destinationFileDirectory);
				}
				File.Copy(sourcePath, destinationFilePath, true);
			}
			else if (Directory.Exists(sourcePath))
			{
				// 如果是目录，递归复制
				string relativePath = Path.GetRelativePath(Path.GetDirectoryName(sourcePath), sourcePath);
				string destinationDirectory = Path.Combine(destinationFolder, relativePath);
				// 创建目标目录
				if (!Directory.Exists(destinationDirectory))
				{
					Directory.CreateDirectory(destinationDirectory);
				}
				// 复制目录中的所有文件
				string[] files = Directory.GetFiles(sourcePath);
				foreach (string file in files)
				{
					string destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file));
					File.Copy(file, destinationFilePath, true);
				}
				// 递归复制子目录
				string[] subDirectories = Directory.GetDirectories(sourcePath);
				foreach (string subDirectory in subDirectories)
				{
					CopyFilesAndDirectories(subDirectory, destinationFolder);
				}
			}
		}
		public static void CopyFilesAndDirectories(string[] sourcePaths, string destinationDirectory)
		{
			//           foreach (var file in sourceFiles)
			//           {
			//if (Directory.Exists(file)) {
			//	Helper.CopyFilesAndDirectories(file, targetPath);
			//}
			//else
			//{
			//	var fileName = Path.GetFileName(file);
			//	if (isSamePath) fileName = "copy of " + fileName;
			//	var targetFile = Path.Combine(targetPath, fileName);
			//	if (!File.Exists(targetFile))
			//		File.Copy(file, targetFile, true);
			//	else
			//	{
			//		var result = MessageBox.Show("file already exist, overwrite it ?", "warning");
			//		if (result == DialogResult.OK)
			//			File.Copy(file, targetFile, true);
			//	}
			//}
			//           }
			foreach (string sourcePath in sourcePaths)
			{
				if (File.Exists(sourcePath))
				{
					CopyFile(sourcePath, destinationDirectory);
					Debug.Print("file [{0}] copyed to [{1}]", sourcePath, destinationDirectory);
				}
				else if (Directory.Exists(sourcePath))
				{
					CopyDirectory(sourcePath, destinationDirectory);
				}
				else
				{
					throw new ArgumentException($"Source path '{sourcePath}' does not exist.");
				}
			}
		}

		private static void CopyFile(string sourceFile, string destinationDirectory)
		{
			string fileName = Path.GetFileName(sourceFile);
			string destFile = Path.Combine(destinationDirectory, fileName);

			Directory.CreateDirectory(destinationDirectory);
			File.Copy(sourceFile, destFile, overwrite: true);
		}

		public static void CopyDirectory(string sourceDir, string destinationDir)
		{
			DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);
			string dirName = sourceDirInfo.Name;

			if (string.IsNullOrEmpty(dirName))
			{
				throw new ArgumentException("Source directory is a root directory and cannot be copied.");
			}

			string destDir = Path.Combine(destinationDir, dirName);
			Directory.CreateDirectory(destDir);

			foreach (FileInfo file in sourceDirInfo.GetFiles())
			{
				string destFile = Path.Combine(destDir, file.Name);
				file.CopyTo(destFile, overwrite: true);
			}

			foreach (DirectoryInfo subDir in sourceDirInfo.GetDirectories())
			{
				CopyDirectory(subDir.FullName, destDir);
			}
		}
		public static void CopyDirectory1(string sourceDir, string targetDir)
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
					CopyDirectory1(directory, targetDirectoryPath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public static void DeleteFile(string path)
		{
			try
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, true);
				}
				else if (File.Exists(path))
				{
					// 创建 FileInfo 对象
					// FileInfo fileInfo = new FileInfo(path);
					// // 去除只读属性
					// if (fileInfo.IsReadOnly)
					// 	fileInfo.IsReadOnly = false;
					//fileInfo.Delete();

					// 去除只读属性
					FileAttributes attributes = File.GetAttributes(path);
					if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					{
						attributes &= ~FileAttributes.ReadOnly;
						File.SetAttributes(path, attributes);
					}
					File.Delete(path);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static void CreateDirectory(string path)
		{
			try
			{
				var counter = 1;
				var newpath = path;
				while (Directory.Exists(newpath))
				{
					newpath = $"{path} ({counter})";
					counter++;
				}
				Directory.CreateDirectory(newpath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static void MoveFileOrDirectory(string sourcePath, string targetPath)
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

		public static async Task<List<FileSystemInfo>> GetDirectoryContentsAsync(string path)
		{
			return await Task.Run(() => GetDirectoryContents(path));
		}
		public static List<FileSystemInfo> GetDirectoryContents(string path, bool includeFolder = false)
		{
			var result = new List<FileSystemInfo>();
			if (!Directory.Exists(path)) return result;

			try
			{
				var dirInfo = new DirectoryInfo(path);
				if (includeFolder)
				{
					var directories = dirInfo.GetDirectories()
						.Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
					result.AddRange(directories);
				}
				var files = dirInfo.GetFiles()
						.Where(f => (f.Attributes & FileAttributes.Hidden) == 0);
				result.AddRange(files);
			}
			catch (UnauthorizedAccessException)
			{
				// 忽略访问受限的目录
			}
			return result;
		}
		public List<FileSystemInfo> GetDirectoryContents(string path, WinShell.ReadDirContentsMode readmode = WinShell.ReadDirContentsMode.Both)
		{
			//var currentTime = DateTime.Now;
			//var needsUpdate = !_directoryCache.ContainsKey(path) ||
			//                (currentTime - _lastCacheUpdate[path]).TotalMilliseconds > Constants.CacheTimeout;

			//if (needsUpdate)
			//{
			var items = new List<FileSystemInfo>();
			if (!isDirBranchMode) {

				var directoryInfo = new DirectoryInfo(path);

				try
				{
					if ((readmode & WinShell.ReadDirContentsMode.Folder) != 0)
					{
						foreach (var dir in directoryInfo.GetDirectories())
						{
							if ((dir.Attributes & FileAttributes.Hidden) == 0)
							{
								items.Add(dir);
							}
						}
					}
					if ((readmode & WinShell.ReadDirContentsMode.File) != 0)
					{
						foreach (var file in directoryInfo.GetFiles())
						{
							if ((file.Attributes & FileAttributes.Hidden) == 0)
							{
								items.Add(file);
							}
						}
					}
					_directoryCache[path] = items;
					//_lastCacheUpdate[path] = currentTime;
					//Debug.Print("dir cache updated {0} >", currentTime);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"读取目录内容失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				//         }
				//else
				//{
				//	Debug.Print("dir cache used!!!!!! {0}, {1} >", currentTime, _lastCacheUpdate[path]);
				//}

				return _directoryCache[path];
			}
			else 
			{
				// 目录分支模式：读取所有子目录中的文件
				var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

				foreach (var filePath in allFiles)
				{
					var fileInfo = new FileInfo(filePath);
					if ((fileInfo.Attributes & FileAttributes.Hidden) == 0)
					{
						// 为文件添加相对路径信息
						var relativePathProperty = new FileInfo(filePath);
						relativePathProperty.Refresh(); // 确保获取最新信息

						// 计算相对路径
						var relativePath = Path.GetRelativePath(path, filePath);
						// 将相对路径信息存储在文件的扩展属性中（如果需要）

						items.Add(fileInfo);
					}
				}

				// 如果需要显示文件夹
				if ((readmode & WinShell.ReadDirContentsMode.Folder) != 0)
				{
					var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
						.Select(d => new DirectoryInfo(d))
						.Where(d => (d.Attributes & FileAttributes.Hidden) == 0);

					items.AddRange(dirs);
				}
				_directoryCache[path] = items;
				return items;
			}
		}
		public static string FormatFileSize(long bytes, bool needFormat = false)
		{
			if (!needFormat) { return bytes.ToString(); }
			string[] units = { "B", "KB", "MB", "GB", "TB" };
			int unitIdx = 0;
			double size = bytes;
			while (size >= 1024 && unitIdx < units.Length - 1)
			{
				unitIdx++;
				size /= 1024;
			}
			return $"{size:0.##} {units[unitIdx]}";
		}
		// 判断文件是否为文本文件
		public static bool IsTextFile(string extension)
		{
			return Constants.TextFileExtensions.Contains(extension.ToLower());
		}
		public static bool IsValidFileSystemPath(string path)
		{
			try
			{
				return Directory.Exists(path) || File.Exists(path);
			}
			catch
			{
				return false;
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
			imageListLargeIcon.ImageSize = new Size(64, 64);
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