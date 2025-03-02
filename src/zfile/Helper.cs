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
		public const string ZfilePath = "D:\\gitrepos\\Files\\";
		public const string ZfileCfgPath = ZfilePath + "Config\\";
		public const string ZfileBinPath = ZfilePath + "src\\zfile\\bin\\Debug\\";
		public const int CacheTimeout = 500; // 缓存超时时间(毫秒)
		public static readonly string[] TextFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
	}

	// 定义MenuInfo类来存储每个按钮的信息
	public class MenuInfo
	{
		public string Button { get; set; } = string.Empty;
		public string Cmd { get; set; } = string.Empty;
		public string Param { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public int Iconic { get; set; }
		public string Menu { get; set; } = string.Empty;
		public MenuInfo()
		{

		}
		public MenuInfo(string button, string cmd, string param, string path, int iconic, string menu)
		{
			Button = button;
			Cmd = cmd;
			Param = param;
			Path = path;
			Iconic = iconic;
			Menu = menu;
		}
	}
	
	internal static class Helper
	{
		private static string GetPathByEnv(string path)
		{
			//如果路径中包含环境变量，替换为实际路径
			if (path.Contains("%"))
			{
				path = path.Replace("%COMMANDER_PATH%", Constants.ZfileCfgPath, StringComparison.OrdinalIgnoreCase);
				path = Environment.ExpandEnvironmentVariables(path);
			}
			return path;
		}
		public static List<MenuInfo> ReadButtonbarFile(string filePath)
		{
			List<MenuInfo> menuInfos = new List<MenuInfo>();

			try
			{
				// 读取文件的所有行
				string[] lines = File.ReadAllLines(filePath);

				// 用于匹配按钮信息的正则表达式
				Regex buttonRegex = new Regex(@"button(\d+)=(.*)");
				Regex cmdRegex = new Regex(@"cmd(\d+)=(.*)");
				Regex paramRegex = new Regex(@"param(\d+)=(.*)");
				Regex pathRegex = new Regex(@"path(\d+)=(.*)");
				Regex iconicRegex = new Regex(@"iconic(\d+)=(\d+)");
				Regex menuRegex = new Regex(@"menu(\d+)=(.*)");

				// 用于存储每个按钮的信息
				Dictionary<int, MenuInfo> buttonInfoMap = new Dictionary<int, MenuInfo>();

				foreach (string line in lines)
				{
					Match buttonMatch = buttonRegex.Match(line);
					if (buttonMatch.Success)
					{
						int buttonNumber = int.Parse(buttonMatch.Groups[1].Value);
						string buttonValue = GetPathByEnv(buttonMatch.Groups[2].Value);

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Button = buttonValue;
						continue;
					}

					Match cmdMatch = cmdRegex.Match(line);
					if (cmdMatch.Success)
					{
						int buttonNumber = int.Parse(cmdMatch.Groups[1].Value);
						string cmdValue = GetPathByEnv(cmdMatch.Groups[2].Value);

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Cmd = cmdValue;
						continue;
					}

					Match paramMatch = paramRegex.Match(line);
					if (paramMatch.Success)
					{
						int buttonNumber = int.Parse(paramMatch.Groups[1].Value);
						string paramValue = paramMatch.Groups[2].Value;

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Param = paramValue;
						continue;
					}

					Match pathMatch = pathRegex.Match(line);
					if (pathMatch.Success)
					{
						int buttonNumber = int.Parse(pathMatch.Groups[1].Value);
						string pathValue = GetPathByEnv(pathMatch.Groups[2].Value);

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Path = pathValue;
						continue;
					}

					Match iconicMatch = iconicRegex.Match(line);
					if (iconicMatch.Success)
					{
						int buttonNumber = int.Parse(iconicMatch.Groups[1].Value);
						int iconicValue = int.Parse(iconicMatch.Groups[2].Value);

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Iconic = iconicValue;
						continue;
					}

					Match menuMatch = menuRegex.Match(line);
					if (menuMatch.Success)
					{
						int buttonNumber = int.Parse(menuMatch.Groups[1].Value);
						string menuValue = menuMatch.Groups[2].Value;

						if (!buttonInfoMap.ContainsKey(buttonNumber))
						{
							buttonInfoMap[buttonNumber] = new MenuInfo();
						}

						buttonInfoMap[buttonNumber].Menu = menuValue;
					}
				}

				// 将字典中的信息添加到列表中
				foreach (var kvp in buttonInfoMap)
				{
					var mi = kvp.Value;
					if (mi.Cmd.Equals(string.Empty) || mi.Cmd.EndsWith("default.bar", StringComparison.OrdinalIgnoreCase))
						continue;
					
					if (mi.Path.Equals(string.Empty))
						mi.Path = Path.GetDirectoryName(mi.Cmd) ?? string.Empty;
					if (mi.Path.Equals(string.Empty))
						mi.Path = Path.GetDirectoryName(mi.Button) ?? string.Empty;
					if(mi.Path.Equals(string.Empty))
						Debug.Print($"{mi.Button} for {mi.Cmd} > path is empty!");
					menuInfos.Add(mi);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"读取文件时发生错误: {ex.Message}");
			}

			return menuInfos;
		}
		public static string ConvertKeyToString(Keys k)
		{
			string str = k.ToString();

			// 特殊按键转换
			if (str.StartsWith("D") && str.Length == 2 && char.IsDigit(str[1]))
				return str[1].ToString(); // D1->1, D2->2 etc.

			if (str.StartsWith("NumPad"))
				return "NUM" + str[6..]; // NumPad1 -> NUM1

			switch (str)
			{
				case "Oemtilde": return "`";
				case "Multiply": return "*";
				case "Divide": return "/";
				case "Oemcomma": return ",";
				case "OemPeriod": return ".";
				case "OemMinus": return "-";
				case "Oemplus": return "=";
				case "OemOpenBrackets": return "[";
				case "OemCloseBrackets": return "]";
				case "OemPipe": return "\\";
				case "OemSemicolon": return ";";
				case "OemQuotes": return "'";
				case "OemQuestion": return "Oem_us/?";
				case "Escape": return "ESC";
				default:
					// 如果是单个字母,转换为大写
					if (str.Length == 1)
						return str.ToUpper();
					return str;
			}
		}
		public static Keys ConvertKeyStringToEnum(string str)
		{
			try
			{
				return (Keys)Enum.Parse(typeof(Keys), str);
			}
			catch { return Keys.None; }
		}
		public static string ConvertStringToKey(string str)
		{
			//F1 -> keys.F1
			//None -> keys.None
			//A -> keys.A
			//ControlKey -> keys.ControlKey
			//1 -> keys.D1
			if (str == "None")
				return str;
			if (int.TryParse(str, out _))
				return "D" + str;
			else if (str.StartsWith("NUM"))
				return str.Replace("NUM", "NumPad");
			else if (str.ToUpper().Equals("OEM_US`~"))
				return "Oemtilde";
			else if (str.ToUpper().Equals("OEM_"))
				return "Oemplus";
			else if (str.Equals("*"))
				return "Multiply";
			else if (str.Equals("/"))
				return "Divide";
			else if (str.Equals(","))
				return "Oemcomma";
			else if (str.Equals("."))
				return "OemPeriod";
			else if (str.Equals("-"))
				return "OemMinus";
			//else if (str.Equals("+"))		// + is impossible, because of the seperator is +
			//	str = "Add";
			else if (str.Equals("["))
				return "OemOpenBrackets";
			else if (str.Equals("]"))
				return "OemCloseBrackets";
			else if (str.Equals("\\"))
				return "OemPipe";
			else if (str.Equals(";"))
				return "OemSemicolon";
			else if (str.Equals("'"))
				return "OemQuotes";
			else if (str.Equals("="))
				return "Oemplus";
			else if (str.Equals("`"))
				return "Oemtilde";
			else if (str.Equals("\\"))
				return "OemPipe";
			else if (str.Equals("ESC"))
				return "Escape";
			else if (str.Equals("Oem_us/?"))
				return "OemQuestion";
			else
			{
				//all is letter, use camel case
				return str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower();
			}
		}
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
		public static void WriteSectionContent(string filePath, string sectionContent, List<string> content)
		{
			try
			{
				// 读取文件内容
				string fileContent = File.ReadAllText(filePath, Encoding.GetEncoding("GB2312"));
				// 查找目标节��起始位置
				int sectionStartIndex = fileContent.IndexOf(sectionContent);
				if (sectionStartIndex == -1)
				{
					// 如果找不到目标节，直接返回
					return;
				}
				// 查找目标节的结束位置
				int sectionEndIndex = fileContent.IndexOf('[', sectionStartIndex + sectionContent.Length);
				if (sectionEndIndex == -1)
				{
					// 如果找不到下一个节，说明目标节是文件的最后一节
					sectionEndIndex = fileContent.Length;
				}
				// 将目标节的内容替换为新内容
				fileContent = fileContent.Remove(sectionStartIndex + sectionContent.Length + 1, sectionEndIndex - sectionStartIndex - sectionContent.Length - 1);
				fileContent = fileContent.Insert(sectionStartIndex + sectionContent.Length + 1, "\r\n"+string.Join("\r\n", content)) + "\r\n";
				// 写入文件
				File.WriteAllText(filePath, fileContent, Encoding.GetEncoding("GB2312"));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"写入文件时发生错误: {ex.Message}");
			}
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
