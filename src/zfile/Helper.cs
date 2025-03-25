using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WinShell;

namespace zfile
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
		public string Name { get; set; } = string.Empty;
		public string Button { get; set; } = string.Empty;
		public string Cmd { get; set; } = string.Empty;
		public string Param { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public int Iconic { get; set; }
		public string Menu { get; set; } = string.Empty;
		public MenuInfo() { }
		public MenuInfo(string name)
		{
			Name = name;
		}
		public MenuInfo(string name, string button, string cmd, string param, string path, int iconic, string menu)
		{
			Name = name;
			Button = button;
			Cmd = cmd;
			Param = param;
			Path = path;
			Iconic = iconic;
			Menu = menu;
		}
		public MenuInfo Clone()
		{
			return new MenuInfo(Name, Button, Cmd, Param, Path, Iconic, Menu);
		}
	}
	
	internal static class Helper
	{
		public static (string cmd, string arg) SplitCommand(string input)
		{
			bool inQuotes = false;
			int firstNonQuotedSpaceIndex = -1;

			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] == '"')
				{
					inQuotes = !inQuotes;
				}
				else if (input[i] == ' ' && !inQuotes)
				{
					firstNonQuotedSpaceIndex = i;
					break;
				}
			}

			if (firstNonQuotedSpaceIndex == -1)
			{
				return (input, "");
			}

			string cmd = input.Substring(0, firstNonQuotedSpaceIndex);
			string arg = input.Substring(firstNonQuotedSpaceIndex + 1);

			return (cmd, arg);
		}

		public static void ApplyFontToControls(Control control, Font font)
		{
			control.Font = font;
			foreach (Control child in control.Controls)
				ApplyFontToControls(child, font);
		}
		public static bool IsTextFileViaFileExe(string filename)
		{
			return (Getfiletype(filename).Contains("text", StringComparison.OrdinalIgnoreCase));
		}
		public static bool IsTextFile(string filePath)//doubao
		{
			try
			{
				// 定义最大读取字节数
				const int maxBytesToRead = 4096;
				// 以二进制模式打开文件
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					// 确保文件有内容
					if (fs.Length == 0) return true;
					// 读取文件的字节数
					int bytesToRead = (int)Math.Min(maxBytesToRead, fs.Length);
					byte[] buffer = new byte[bytesToRead];
					fs.Read(buffer, 0, bytesToRead);
					int controlCharCount = 0;
					// 遍历字节数组
					foreach (byte b in buffer)
					{
						if (b < 32 && b != 9 && b != 10 && b != 13)
						{
							controlCharCount++;
						}
					}
					// 计算控制字符的比例
					double controlCharRatio = (double)controlCharCount / bytesToRead;
					// 若控制字符比例小于阈值，则判定为文本文件
					return controlCharRatio < 0.05;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
		public static string ExtractResponseContent(string input)
		{
			// 定义正则表达式模式，用于匹配 response 字段的值
			string pattern = @"\""response\"":\""(.*?)\""";
			MatchCollection matches = Regex.Matches(input, pattern);

			// 用于存储提取的内容
			StringBuilder result = new StringBuilder();

			// 遍历所有匹配结果
			foreach (Match match in matches)
			{
				// 提取匹配的内容
				string responseValue = match.Groups[1].Value;
				// 将提取的内容添加到结果中
				result.Append(responseValue);
			}

			return result.ToString();
		}
		//public static string Getfiletype(string args)
		//{
		//	// 配置启动参数
		//	ProcessStartInfo startInfo = new ProcessStartInfo
		//	{
		//		FileName = Constants.ZfileBinPath + "file.exe",        // 程序路径
		//		Arguments = Path.GetFileName(args), // 参数（可选）
		//		WorkingDirectory = Path.GetDirectoryName(args),
		//		UseShellExecute = false,      // 不使用系统外壳程序
		//		RedirectStandardOutput = true,// 重定向标准输出
		//		RedirectStandardError = true, // 重定向错误输出
		//		CreateNoWindow = true         // 不创建新窗口
		//	};

		//	using (Process process = new Process())
		//	{
		//		process.StartInfo = startInfo;

		//		try
		//		{
		//			// 启动进程
		//			process.Start();

		//			// 异步读取输出（防止死锁）
		//			string output = process.StandardOutput.ReadToEnd();
		//			string error = process.StandardError.ReadToEnd();

		//			// 等待程序结束（可设置超时时间，单位毫秒）
		//			process.WaitForExit();

		//			// 获取退出代码
		//			int exitCode = process.ExitCode;

		//			Debug.Print("输出内容：\n" + output);
		//			if (!string.IsNullOrEmpty(error))
		//			{
		//				Debug.Print("错误信息：\n" + error);
		//			}
		//			Debug.Print($"退出代码：{exitCode}");
		//			return output;
		//		}
		//		catch (Exception ex)
		//		{
		//			Debug.Print($"执行出错：{ex.Message}");
		//		}
		//	}
		//	return string.Empty; 
		//}
		public static string Getfiletype(string args)
		{
			try
			{
				string fileExePath = Path.Combine(Constants.ZfileBinPath, "file.exe");
				if (!File.Exists(fileExePath))
				{
					Debug.Print($"file.exe not found at: {fileExePath}");
					return string.Empty;
				}

				// 确保参数是完整路径
				string fullPath = Path.GetFullPath(args);
				if (!File.Exists(fullPath))
				{
					Debug.Print($"Target file not found: {fullPath}");
					return string.Empty;
				}

				// 配置启动参数
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = fileExePath,
					Arguments = $"\"{fullPath}\"", // 用引号包裹路径，避免空格问题
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = Encoding.UTF8
				};

				using Process process = new();
				process.StartInfo = startInfo;
				process.Start();

				// 异步读取输出
				string output = process.StandardOutput.ReadToEnd();
				string error = process.StandardError.ReadToEnd();

				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					Debug.Print($"Process exited with code: {process.ExitCode}");
					Debug.Print($"Error output: {error}");
					return string.Empty;
				}

				Debug.Print($"File type result: {output}");
				return output;
			}
			catch (Exception ex)
			{
				Debug.Print($"Error in Getfiletype: {ex.Message}");
				return string.Empty;
			}
		}

		public static void WriteConfigToFile( string path, List<MenuInfo> list)
		{
			var cfg = Write_em_Config(list);
			File.WriteAllText(path, cfg);
		}
		public static List<MenuInfo> ReadConfigFromFile(string filePath)
		{
			try
			{
				// 读取文件内容
				string configContent = File.ReadAllText(filePath);
				// 调用 ReadConfig 函数处理配置内容
				return Read_em_Config(configContent);
			}
			catch (Exception ex)
			{
				// 若读取文件或处理配置过程中出现异常，打印错误信息
				Console.WriteLine($"读取配置文件时发生错误: {ex.Message}");
				return new List<MenuInfo>();
			}
		}
		public static List<MenuInfo> Read_em_Config(string config)
		{
			List<MenuInfo> menuInfos = new List<MenuInfo>();
			string[] sections = Regex.Split(config, @"\[(em_[^\]]+)\]");

			for (int i = 1; i < sections.Length; i += 2)
			{
				string sectionName = sections[i];
				string sectionContent = sections[i + 1];

				string cmd = string.Empty;
				string path = string.Empty;
				string param = string.Empty;
				string menu = string.Empty;
				string button = string.Empty;
				int iconic = 0;

				string[] lines = sectionContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string line in lines)
				{
					if (line.StartsWith("cmd="))
						cmd = line[4..];
					else if (line.StartsWith("path="))
						path = line[5..];
					else if (line.StartsWith("param="))
						param = line[6..];
					else if (line.StartsWith("menu="))
						menu = line[5..];
					else if (line.StartsWith("button="))
						button = line[7..];
					else if (line.StartsWith("iconic="))
						iconic = line[7..] == null ? 0 : int.Parse(line[7..]);
				}

				MenuInfo menuInfo = new MenuInfo(sectionName, button, cmd, param, path, iconic, menu);
				menuInfos.Add(menuInfo);
			}

			return menuInfos;
		}
		public static string Write_em_Config(List<MenuInfo> menuInfos)
		{
			StringBuilder configBuilder = new StringBuilder();

			foreach (MenuInfo menuInfo in menuInfos)
			{
				// 写入节名
				configBuilder.AppendLine($"[{menuInfo.Name}]");

				// 写入 cmd 行
				if (!string.IsNullOrEmpty(menuInfo.Cmd))
				{
					configBuilder.AppendLine($"cmd={menuInfo.Cmd}");
				}

				// 写入 path 行
				if (!string.IsNullOrEmpty(menuInfo.Path))
				{
					configBuilder.AppendLine($"path={menuInfo.Path}");
				}

				// 写入 param 行
				if (!string.IsNullOrEmpty(menuInfo.Param))
				{
					configBuilder.AppendLine($"param={menuInfo.Param}");
				}

				// 写入 menu 行
				if (!string.IsNullOrEmpty(menuInfo.Menu))
				{
					configBuilder.AppendLine($"menu={menuInfo.Menu}");
				}

				// 写入 button 行
				if (!string.IsNullOrEmpty(menuInfo.Button))
				{
					configBuilder.AppendLine($"button={menuInfo.Button}");
				}

				// 写入 iconic 行
				configBuilder.AppendLine($"iconic={menuInfo.Iconic}");

				// 写入空行分隔不同的节
				configBuilder.AppendLine();
			}

			return configBuilder.ToString();
		}
		public static string ReplaceEnvironmentVariables(string input)
		{
			// 定义一个正则表达式模式，用于匹配被 % 包裹的环境变量
			string pattern = @"%([^%]+)%";
			bool hasReplacement;

			do
			{
				hasReplacement = false;
				// 使用正则表达式匹配输入字符串中的环境变量
				var matches = System.Text.RegularExpressions.Regex.Matches(input, pattern);
				foreach (System.Text.RegularExpressions.Match match in matches)
				{
					string variableName = match.Groups[1].Value;
					// 获取环境变量的实际值
					string variableValue = Environment.GetEnvironmentVariable(variableName);
					if (variableValue != null)
					{
						// 将匹配到的环境变量替换为实际值
						input = input.Replace(match.Value, variableValue);
						hasReplacement = true;
					}
				}
			} while (hasReplacement);

			return input;
		}
		public static string GetPathByEnv(string path)
		{
			//如果路径中包含环境变量，替换为实际路径
			if (path.Contains("%"))
			{
				path = path.Replace("%COMMANDER_PATH%\\", Constants.ZfileCfgPath, StringComparison.OrdinalIgnoreCase);
				path = Environment.ExpandEnvironmentVariables(path);
				//将path中的%环境变量%替换为实际路径
				//path = ReplaceEnvironmentVariables(path);
			}
			return path;
		}
		public static List<MenuInfo> GetMenuInfoFromList(string[] lines)
		{
			List<MenuInfo> menuInfos = new List<MenuInfo>();

			try
			{
				// 用于匹配按钮信息的正则表达式
				Regex buttonRegex = new Regex(@"button(\d+)=(.*)");
				Regex cmdRegex = new Regex(@"cmd(\d+)=(.*)"); //also can be used by dynamic menu contruction
				Regex paramRegex = new Regex(@"param(\d+)=(.*)"); //also can be used by dynamic menu contruction
				Regex pathRegex = new Regex(@"path(\d+)=(.*)");
				Regex iconicRegex = new Regex(@"iconic(\d+)=(\d+)");
				Regex menuRegex = new Regex(@"menu(\d+)=(.*)"); // also can be used by dynamic menu contruction

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
					if (mi.Path.Equals(string.Empty))
						Debug.Print($"{mi.Button} for {mi.Cmd} > path is empty!");
					menuInfos.Add(mi);//TODO: BUGFIX PATH=. will cause Problem
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"读取文件时发生错误: {ex.Message}");
			}
			return menuInfos;
		}
		public static List<MenuInfo> ReadButtonbarFile(string filePath)
		{
			// 读取文件的所有行
			return GetMenuInfoFromList(File.ReadAllLines(filePath));
		}
		public static string ConvertKeyToString(Keys k, bool excludeSpecKey = true)
		{
			if (excludeSpecKey)
				if (k == Keys.ControlKey || k == Keys.Menu || k == Keys.ShiftKey || k == Keys.LWin || k == Keys.RWin)
					return "";
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
				// 查找目标节起始位置
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
		public static Dictionary<string, string> GetSpecPathFromReg()
		{
			Dictionary<string, string> specialpaths = new Dictionary<string, string>();
			RegistryKey folders;
			folders = OpenRegistryPath(Registry.CurrentUser, @"\software\microsoft\windows\currentversion\explorer\shell folders");
			//Windows用户桌面路径
			specialpaths["deskktop"] = folders.GetValue("Desktop").ToString();
			//Windows用户字体目录路径
			specialpaths["fonts"] = folders.GetValue("Fonts").ToString();
			//Windows用户网络邻居路径
			specialpaths["nethood"] = folders.GetValue("Nethood").ToString();
			//Windows用户我的文档路径
			specialpaths["personal"] = folders.GetValue("Personal").ToString();
			//Windows用户开始菜单程序路径
			specialpaths["programs"] = folders.GetValue("Programs").ToString();
			//Windows用户存放用户最近访问文档快捷方式的目录路径
			specialpaths["recent"] = folders.GetValue("Recent").ToString();
			//Windows用户发送到目录路径
			specialpaths["sendto"] = folders.GetValue("Sendto").ToString();
			//Windows用户开始菜单目录路径
			specialpaths["startmenu"] = folders.GetValue("Start menu").ToString();
			//Windows用户开始菜单启动项目录路径
			specialpaths["startup"] = folders.GetValue("Startup").ToString();
			//Windows用户收藏夹目录路径
			specialpaths["favorites"] = folders.GetValue("Favorites").ToString();
			//Windows用户网页历史目录路径
			specialpaths["history"] = folders.GetValue("History").ToString();
			//Windows用户Cookies目录路径
			specialpaths["cookies"] = folders.GetValue("Cookies").ToString();
			//Windows用户Cache目录路径
			specialpaths["cache"] = folders.GetValue("Cache").ToString();
			//Windows用户应用程式数据目录路径
			specialpaths["appdata"] = folders.GetValue("Appdata").ToString();
			//Windows用户打印目录路径
			specialpaths["printhood"] = folders.GetValue("Printhood").ToString();
			String Path = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);//返回收藏夹位置
			foreach(var p in specialpaths)
				Debug.Print(p.Key + ":" + p.Value);
			return specialpaths;
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
		public static IDictionary getEnv()
		{
			//把环境变量中所有的值取出来，放到变量environment中
			IDictionary environment = Environment.GetEnvironmentVariables();
			//打印表头
			Debug.Print("环境变量名\t=\t环境变量值");
			//遍历environment中所有键值
			foreach (string environmentKey in environment.Keys)//打印出所有环境变量的名称和值
				Debug.Print($"{environmentKey}={environment[environmentKey].ToString()}");
			return environment;
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
			if (Node.Parent == null || Node.Parent.Tag is not ShellItem || Node.Tag is not ShellItem)
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
