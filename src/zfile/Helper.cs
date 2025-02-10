using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinShell;

namespace WinFormsApp1
{
	public static class Constants
	{
		public const string ZfilePath = "D:\\gitrepos\\Files\\config\\";
		public const int CacheTimeout = 5000; // 缓存超时时间(毫秒)
		public static readonly string[] TextFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
	}
	internal static class Helper
	{
		public static void CopyFilesAndDirectories(string[] sourcePaths, string destinationDirectory)
		{
			foreach (string sourcePath in sourcePaths)
			{
				if (File.Exists(sourcePath))
				{
					CopyFile(sourcePath, destinationDirectory);
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

		private static void CopyDirectory(string sourceDir, string destinationDirectory)
		{
			DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);
			string dirName = sourceDirInfo.Name;

			if (string.IsNullOrEmpty(dirName))
			{
				throw new ArgumentException("Source directory is a root directory and cannot be copied.");
			}

			string destDir = Path.Combine(destinationDirectory, dirName);
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
		public static string[] RemoveQuotes(string[] originalList)
		{
			List<string> resultList = new();
			foreach (string item in originalList)
			{
				if (item.StartsWith('"') && item.EndsWith('"'))
				{
					// Remove the leading and trailing double - quotes
					resultList.Add(item.Substring(1, item.Length - 2));
				}
				else
				{
					resultList.Add(item);
				}
			}
			return [.. resultList];
		}

		public static int GetFlowLayoutPanelLineCount(FlowLayoutPanel panel)
		{
			if (panel.Controls.Count == 0)
			{
				return 0;
			}

			int rowCount = 1;
			int currentMaxY = panel.Controls[0].Top;

			// 遍历所有子控件
			foreach (Control control in panel.Controls)
			{
				if (control.Top > currentMaxY)
				{
					// 如果当前控件的 Y 坐标大于之前记录的最大 Y 坐标，说明进入了新的一行
					rowCount++;
					currentMaxY = control.Top;
				}
			}

			return rowCount;
		}
		public static List<string> ReadSectionContent(string filePath, string targetSection)
		{
			List<string> sectionContent = new List<string>();
			bool isInTargetSection = false;

			try
			{
				// 打开文件并逐行读取
				using (StreamReader reader = new StreamReader(filePath, Encoding.GetEncoding("GB2312")))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						// 检查是否为节的起始行
						if (line.StartsWith("[") && line.EndsWith("]"))
						{
							string currentSection = line.Substring(1, line.Length - 2);
							if (currentSection == targetSection)
							{
								isInTargetSection = true;
							}
							else
							{
								if (isInTargetSection)
								{
									// 遇到下一个节，停止收集内容
									break;
								}
							}
						}
						else if (isInTargetSection)
						{
							// 收集目标节内的内容
							sectionContent.Add(line);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"读取文件时发生错误: {ex.Message}");
			}

			return sectionContent;
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

		public static Dictionary<string,string> GetSpecFolderPaths()
		{
			//遍历ShellSpecialFolders枚举值，获取对应的路径并存入一个列表
			Dictionary<string, string> specFolderPaths = new Dictionary<string, string>();
			foreach (ShellSpecialFolders folder in Enum.GetValues(typeof(ShellSpecialFolders)))
			{
				string path = w32.GetSpecialFolderPath(IntPtr.Zero, folder);
				specFolderPaths[folder.ToString()] = path;
				Debug.Print("{0}={1}", folder, path);
			}
			//download path need to be processed by special method GUID
			string downloadPath = API.SHGetKnownFolderPath(Guids.DownloadFolderGuid, 0, IntPtr.Zero);
			specFolderPaths["迅雷下载"] = (downloadPath);
			return specFolderPaths;
		}
		public static void GetSpecPathFromReg()
		{
			RegistryKey folders;
			folders = OpenRegistryPath(Registry.CurrentUser, @"\software\microsoft\windows\currentversion\explorer\shell folders");
			//Windows用户桌面路径
			string desktopPath = folders.GetValue("Desktop").ToString();
			//Windows用户字体目录路径
			string fontsPath = folders.GetValue("Fonts").ToString();
			//Windows用户网络邻居路径
			string nethoodPath = folders.GetValue("Nethood").ToString();
			//Windows用户我的文档路径
			string personalPath = folders.GetValue("Personal").ToString();
			//Windows用户开始菜单程序路径
			string programsPath = folders.GetValue("Programs").ToString();
			//Windows用户存放用户最近访问文档快捷方式的目录路径
			string recentPath = folders.GetValue("Recent").ToString();
			//Windows用户发送到目录路径
			string sendtoPath = folders.GetValue("Sendto").ToString();
			//Windows用户开始菜单目录路径
			string startmenuPath = folders.GetValue("Start menu").ToString();
			//Windows用户开始菜单启动项目录路径
			string startupPath = folders.GetValue("Startup").ToString();
			//Windows用户收藏夹目录路径
			string favoritesPath = folders.GetValue("Favorites").ToString();
			//Windows用户网页历史目录路径
			string historyPath = folders.GetValue("History").ToString();
			//Windows用户Cookies目录路径
			string cookiesPath = folders.GetValue("Cookies").ToString();
			//Windows用户Cache目录路径
			string cachePath = folders.GetValue("Cache").ToString();
			//Windows用户应用程式数据目录路径
			string appdataPath = folders.GetValue("Appdata").ToString();
			//Windows用户打印目录路径
			string printhoodPath = folders.GetValue("Printhood").ToString();
			String Path = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);//返回收藏夹位置
			Console.WriteLine(Path);
		}
		private static RegistryKey OpenRegistryPath(RegistryKey root, string s)
		{
			s = s.Remove(0, 1) + @"\";
			while (s.IndexOf(@"\") != -1)
			{
				root = root.OpenSubKey(s.Substring(0, s.IndexOf(@"\")));
				s = s.Remove(0, s.IndexOf(@"\") + 1);
			}
			return root;
		}
		public static void getEnv()
		{
			//把环境变量中所有的值取出来，放到变量environment中
			IDictionary environment = Environment.GetEnvironmentVariables();
			//打印表头
			Console.WriteLine("环境变量名\t=\t环境变量值");
			//遍历environment中所有键值
			foreach (string environmentKey in environment.Keys)
			{
				//打印出所有环境变量的名称和值
				Console.WriteLine("(0}\t=\t{(1}", environmentKey, environment[environmentKey].ToString());
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

		public static string FormatFileSize(long bytes)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			int order = 0;
			double size = bytes;
			while (size >= 1024 && order < sizes.Length - 1)
			{
				order++;
				size = size / 1024;
			}
			return $"{size:0.##} {sizes[order]}";
		}

		public static string getFSpath(string path)
		{
			if (path.Contains(':'))
			{
				var pathParts = path.Split(':');
				var len = pathParts[0].Length;
				var drive = pathParts[0].Substring(len - 1, 1);
				return drive + ":" + pathParts[1].TrimStart(')');
			}
			return path;
		}
		//获取当前树节点的实际文件系统路径，eg. 'system (c:)' -> c:\\
		public static string getFSpathbyTree(TreeNode Node)
		{
			if (Node.Parent == null)
			{
				//top node process, does not need to process listviewbyfilesystem
				return string.Empty;
			}
			var parentfolder = ((ShellItem)Node.Parent.Tag).ShellFolder;	//获取父节点的ishellfoler
			var pidl = ((ShellItem)Node.Tag).PIDL;	//获取c:\\节点的pidl
			return w32.GetPathByIShell(parentfolder, pidl);	//取得实际path
		}
		public static string getFSpathbyList(string path)
		{
			var parentfolder = w32.GetParentFolder(path);
			var pidl = API.ILCreateFromPath(path);
			return w32.GetPathByIShell(parentfolder, pidl); //TODO: bug here
		}
		public static string ConvertGB2312ToUTF8(string str)
		{
			Encoding utf8;
			Encoding gb2312;
			utf8 = Encoding.GetEncoding("UTF-8");
			gb2312 = Encoding.GetEncoding("GB2312");
			byte[] gb = gb2312.GetBytes(str);
			gb = Encoding.Convert(gb2312, utf8, gb);
			return utf8.GetString(gb);
		}
		
	}

}
