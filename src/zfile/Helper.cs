using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinShell;

namespace WinFormsApp1
{
	internal static class Helper
	{
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
		/// <summary>
		/// ////
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
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
		public static ImageList LoadIconsFromFile(string path)
		{
			var count = API.ExtractIconEx(path, -1, null, null, 0);
			var phiconLarge = new IntPtr[count];
			var phiconSmall = new IntPtr[count];
			API.ExtractIconEx(path, 0, phiconLarge, null, count);

			var imageList = new ImageList
			{
				ImageSize = SystemInformation.IconSize
			};
			imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			phiconLarge.ToList().ForEach(x => API.DestroyIcon(x));

			return imageList;
		}
		/// <summary>
		/// 依据文件名读取图标，若指定文件不存在，则返回空值。  
		/// </summary>
		/// <param name="fileName">文件路径</param>
		/// <param name="isLarge">是否返回大图标</param>
		/// <returns></returns>
		public static Icon GetIconByFileName(string fileName, bool isLarge = true)
		{
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			//文件名 图标索引 
			API.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
			IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

			if (IconHnd.ToString() == "0")
				return null;
			return Icon.FromHandle(IconHnd);
		}
		/// <summary>  
		/// 根据文件扩展名（如:.*），返回与之关联的图标。
		/// 若不以"."开头则返回文件夹的图标。  
		/// </summary>  
		/// <param name="fileType">文件扩展名</param>  
		/// <param name="isLarge">是否返回大图标</param>  
		/// <returns></returns>  
		public static Icon GetIconByFileType(string fileType, bool isLarge)
		{
			if (fileType == null || fileType.Equals(string.Empty)) return null;

			RegistryKey regVersion = null;
			string regFileType = null;
			string regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";

			if (fileType[0] == '.')
			{
				//读系统注册表中文件类型信息  
				regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
				if (regVersion != null)
				{
					regFileType = regVersion.GetValue("") as string;
					regVersion.Close();
					regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
					if (regVersion != null)
					{
						regIconString = regVersion.GetValue("") as string;
						regVersion.Close();
					}
				}
				if (regIconString == null)
				{
					//没有读取到文件类型注册信息，指定为未知文件类型的图标  
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//直接指定为文件夹图标  
				regIconString = systemDirectory + "shell32.dll,3";
			}
			string[] fileIcon = regIconString.Split(new char[] { ',' });
			if (fileIcon.Length != 2)
			{
				//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标  
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
			}
			Icon resultIcon = null;
			try
			{
				//调用API方法读取图标  
				IntPtr[] phiconLarge = new IntPtr[1];
				IntPtr[] phiconSmall = new IntPtr[1];
				uint count = API.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
				IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);
				resultIcon = Icon.FromHandle(IconHnd);
			}
			catch { }
			return resultIcon;
		}
		/// <summary>
		/// 通过文件名称获取文件图标
		/// </summary>
		/// <param name="tcType">指定参数tcFullName的类型: FILE/DIR</param>
		/// <param name="tcFullName">需要获取图片的全路径文件名</param>
		/// <param name="tlIsLarge">是否获取大图标(32*32)</param>
		/// <returns></returns>
		public static Icon GetIconByFileName(string tcType, string tcFullName, bool tlIsLarge = false)
		{
			Icon ico = null;

			string fileType = tcFullName.Contains(".") ? tcFullName.Substring(tcFullName.LastIndexOf('.')).ToLower() : string.Empty;

			RegistryKey regVersion = null;
			string regFileType = null;
			string regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			IntPtr hIcon = IntPtr.Zero;
			uint rst = 0;

			if (tcType == "FILE")
			{
				//含图标的文件，优先使用文件中自带图标
				if (".exe.ico".Contains(fileType))
				{
					//文件名 图标索引
					phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
					rst = API.ExtractIconEx(tcFullName, 0, phiconLarge, phiconSmall, 1);
					hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
					ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
					if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
					if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
					if (ico != null)
						return ico;
				}

				//通过文件扩展名读取图标
				regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
				if (regVersion != null)
				{
					regFileType = regVersion.GetValue("") as string;
					regVersion.Close();
					regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
					if (regVersion != null)
					{
						regIconString = regVersion.GetValue("") as string;
						regVersion.Close();
					}
				}
				if (regIconString == null)
				{
					//没有读取到文件类型注册信息，指定为未知文件类型的图标
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//直接指定为文件夹图标
				regIconString = systemDirectory + "shell32.dll,3";
			}

			string[] fileIcon = regIconString.Split(new char[] { ',' });
			//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
			fileIcon = fileIcon.Length == 2 ? fileIcon : new string[] { systemDirectory + "shell32.dll", "2" };

			phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
			rst = API.ExtractIconEx(fileIcon[0].Trim('\"'), Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
			hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
			ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
			if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
			if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			if (ico != null)
				return ico;

			// 对于文件，如果提取文件图标失败，则重新使用可执行文件通用图标
			if (tcType == "FILE")
			{
				//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
				phiconLarge = new IntPtr[1];
				phiconSmall = new IntPtr[1];
				rst = API.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
				hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
				ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
				if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
				if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			}

			return ico;
		}
	}
	public static class Constants
	{
		public const string ZfilePath = "D:\\gitrepos\\Files\\config\\";
		public const int CacheTimeout = 5000; // 缓存超时时间(毫秒)
		public static readonly string[] TextFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
	}
}
