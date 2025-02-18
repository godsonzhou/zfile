﻿using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WinShell;

namespace WinFormsApp1
{
	public static class Constants
	{
		public const string ZfilePath = "D:\\gitrepos\\Files\\config\\";
		public const int CacheTimeout = 500; // 缓存超时时间(毫秒)
		public static readonly string[] TextFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
	}
	internal static class Helper
	{
		public static Dictionary<string, string> ParseConfig(List<string> config)
		{
			/*
			 * [ListerPlugins]
				0=%COMMANDER_PATH%\Plugins\Wlx\AKFont\AKFont.wlx64
				0_detect=FORCE | EXT="TTF" | EXT="PFM" | EXT="OTF" | EXT="TTC" | EXT="FON" | EXT="PFB"
				1=%COMMANDER_PATH%\Plugins\Wlx\LinkInfo\LinkInfo.wlx
				1_detect=force | (ext="LNK")
				2=%COMMANDER_PATH%\Plugins\Wlx\IniEd\IniEd.wlx64
				2_detect=EXT="INI"|EXT="INF"|EXT="REG"|EXT="URL"
				3=%COMMANDER_PATH%\Plugins\Wlx\Fileinfo\Fileinfo.wlx64
				4=%COMMANDER_PATH%\Plugins\Wlx\HTMLView\HTMLView.wlx64
				5=%COMMANDER_PATH%\Plugins\Wlx\ICLView\ICLView.wlx64
				5_detect=MULTIMEDIA & (ext="DLL" | ext="EXE" | ext="ICL" | ext="ICL32" | ext="ICO" | size=0 | force)
				6=%COMMANDER_PATH%\Plugins\Wlx\sLister\sLister.wlx64
				6_detect=MULTIMEDIA & (EXT="PDF" | EXT="DJVU" | EXT="DJV"| EXT="XPS" | EXT="CBZ" | EXT="CBR" | EXT="EPUB" | EXT="MOBI" | EXT="AZW" | EXT="AZW3")
				7=%COMMANDER_PATH%\Plugins\Wlx\SWFView\SWFView.wlx64
				7_detect=MULTIMEDIA & EXT="SWF" | (([0]="F" & [1]="W" & [2]="S")|([0]="C" & [1]="W" & [2]="S") & FORCE)
				8=%COMMANDER_PATH%\Plugins\Wlx\SQLiteViewer\SQLiteViewer.wlx
				8_detect=MULTIMEDIA & ext="DB"|ext="DB3"|ext="SQLITE"|ext="SQLITE3"|ext="FOSSIL"
				9=%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wlx64
				9_detect=MULTIMEDIA
				10=%COMMANDER_PATH%\Plugins\Wlx\MMedia\MMedia.wlx64
				10_detect=MULTIMEDIA
				11=%COMMANDER_PATH%\Plugins\Wlx\MarkdownView\MarkdownView.wlx
				12=%COMMANDER_PATH%\Plugins\Wlx\CudaLister\cudalister.wlx
				13=%COMMANDER_PATH%\Plugins\Wlx\uLister\uLister.wlx64
			 */
			Dictionary<string, string> result = new Dictionary<string, string>();
			//string[] lines = configText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			Dictionary<int, string> pathMap = new Dictionary<int, string>();
			Dictionary<int, string> detectMap = new Dictionary<int, string>();
			// 首先解析路径和检测规则
			foreach (string line in config)
			{
				Match pathMatch = Regex.Match(line, @"^(\d+)=.*\\([^\\]+)\.wlx(?:64)?$");
				if (pathMatch.Success)
				{
					int index = int.Parse(pathMatch.Groups[1].Value);
					string pluginName = pathMatch.Groups[2].Value;
					pathMap[index] = pluginName;
				}
				Match detectMatch = Regex.Match(line, @"^(\d+)_detect=(.*)$");
				if (detectMatch.Success)
				{
					int index = int.Parse(detectMatch.Groups[1].Value);
					string detectRule = detectMatch.Groups[2].Value;
					detectMap[index] = detectRule;
				}
			}
			// 将有检测规则的插件添加到结果字典中
			foreach (var kvp in detectMap)
			{
				int index = kvp.Key;
				if (pathMap.ContainsKey(index))
				{
					string pluginName = pathMap[index];
					string detectRule = kvp.Value;
					result[pluginName.ToUpper()] = detectRule;
				}
			}
			return result;
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
	
		public static string GetListItemPath(ListViewItem item)
		{
			TreeNode node = (TreeNode)item.Tag;
			var path = getFSpath(node.FullPath);
			var return1 = Path.Combine(path, item.Text);
			var sitem = (ShellItem)node.Tag;
			var return2 = sitem.parsepath;
			if (return1 != return2)
			{
				Debug.Print($"get listitem path diff >>> getfspath:{return1}, sitem.parsepath:{return2}");
			}
			return return1;
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
