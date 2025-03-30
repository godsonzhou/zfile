using System.Text;
using System.Text.RegularExpressions;
namespace zfile
{
	// 定义配置项类
	public class ConfigItem
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}

	// 定义配置节类
	public class ConfigSection
	{
		public string Name { get; set; }	//section name
		public List<ConfigItem> Items { get; set; }

		public ConfigSection()
		{
			Items = new List<ConfigItem>();
		}

		// 根据关键字查找配置项的值
		public string FindValue(string key)
		{
			var item = Items.FirstOrDefault(i => i.Key == key);
			return item?.Value;
		}
	}

	// 定义 CFGLOADER 类
	public class CFGLOADER
	{
		public List<ConfigSection> sections;
		private string cfgfile;

		public CFGLOADER()
		{
			sections = new List<ConfigSection>();
		}

		public CFGLOADER(string filePath) : this()
		{
			cfgfile = filePath;
			LoadConfig(filePath);
		}

		// 读取配置文件
		public void LoadConfig(string filePath)
		{
			sections.Clear();
			ConfigSection currentSection = null;
			foreach (var line in File.ReadAllLines(filePath))
			{
				var trimmedLine = line.Trim();
				if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
				{
					continue;
				}
				if (trimmedLine.StartsWith("["))
				{
					var sectionName = trimmedLine.Trim('[', ']');
					currentSection = new ConfigSection { Name = sectionName };
					sections.Add(currentSection);
				}
				else if (currentSection != null)
				{
					var parts = trimmedLine.Split('=', 2);
					if (parts.Length == 2)
					{
						currentSection.Items.Add(new ConfigItem { Key = parts[0].Trim(), Value = parts[1].Trim() });
					}
				}
			}
		}

		// 保存配置到文件
		public void SaveConfig()//todo: while writing into wincmd.ini, should use unicode, not gbk, otherwise, such as wcx_ftp.ini, use gbk not unicode
		{
			using (var writer = new StreamWriter(cfgfile))
			{
				foreach (var section in sections)
				{
					writer.WriteLine($"[{section.Name}]");
					foreach (var item in section.Items)
						writer.WriteLine($"{item.Key}={item.Value}");
					writer.WriteLine();
				}
			}
		}
		// 添加或更新配置节
		public void AddOrUpdateSection(string sectionName, List<ConfigItem> items)
		{
			var section = sections.FirstOrDefault(s => s.Name == sectionName);
			if (section == null)
			{
				section = new ConfigSection { Name = sectionName };
				sections.Add(section);
			}
			section.Items = items;
		}

		// 移除配置节
		public void RemoveSection(string sectionName)
		{
			sections.RemoveAll(s => s.Name == sectionName);
		}

		// 清除指定前缀的所有配置节
		public void ClearSectionsWithPrefix(string prefix)
		{
			sections.RemoveAll(s => s.Name.StartsWith(prefix));
		}
		// 根据关键字查找配置
		public string FindConfigValue(string sectionName, string key)
		{
			var section = sections.FirstOrDefault(s => s.Name == sectionName);
			return section?.FindValue(key);
		}
		public ConfigSection? GetConfigSection(string sectionName)
		{
			return sections.FirstOrDefault(s => s.Name == sectionName);
		}
		public bool SetConfigValue(string sectionName, string key, string value)
		{
			var section = sections.FirstOrDefault(s => s.Name == sectionName);
			if (section == null)
			{
				return false;
			}
			var item = section.Items.FirstOrDefault(i => i.Key == key);
			if (item == null)
			{
				section.Items.Add(new ConfigItem { Key = key, Value = value });
			}
			else
			{
				item.Value = value;
			}
			return true;
		}

		// 根据编号或名字归类配置项
		public Dictionary<string, List<ConfigItem>> GroupConfigItemsByNumberOrName()
		{
			var groupedItems = new Dictionary<string, List<ConfigItem>>();
			foreach (var section in sections)
			{
				foreach (var item in section.Items)
				{
					var match = Regex.Match(item.Key, @"^(\d+)[_]?");
					if (match.Success)
					{
						var groupKey = match.Groups[1].Value;
						if (!groupedItems.ContainsKey(groupKey))
						{
							groupedItems[groupKey] = new List<ConfigItem>();
						}
						groupedItems[groupKey].Add(item);
					}
				}
			}
			return groupedItems;
		}
		public static CmdTable LoadCmdTable(string totalCmdPath, string wcmIconsPath)
		{
			var cmdTable = new CmdTable();
			var zhDescDict = LoadZhDesc(wcmIconsPath);

			using (var reader = new StreamReader(totalCmdPath, Encoding.GetEncoding("GB2312")))
			{
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if (line.StartsWith("cm_"))
					{
						var parts = line.Split(';');
						var cmdParts = parts[0].Split('=');
						if (cmdParts.Length == 2 && int.TryParse(cmdParts[1], out var cmdId))
						{
							var cmdName = cmdParts[0].ToLower();//bugfix: cm_SrcThumbs in cfgfile， but in toolbarstrip, the button trigger cmd is cm_srcthumbs, so we need to convert cm_SrcThumbs to cm_srcthumbs
							var description = parts.Length > 1 ? parts[1] : string.Empty;
							var zhDesc = zhDescDict.TryGetValue(cmdId, out var desc) ? desc : string.Empty;
							var cmdItem = new CmdTableItem(cmdName, cmdId, description, zhDesc);
							cmdTable.Add(cmdItem);
						}
					}
				}
			}

			return cmdTable;
		}

		private static Dictionary<int, string> LoadZhDesc(string wcmIconsPath)
		{
			var zhDescDict = new Dictionary<int, string>();

			using (var reader = new StreamReader(wcmIconsPath, Encoding.GetEncoding("GB2312")))
			{
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if (line.Contains('='))
					{
						var parts = line.Split('=');
						if (parts.Length == 2 && int.TryParse(parts[0], out var cmdId))
						{
							zhDescDict[cmdId] = parts[1];
						}
					}
				}
			}

			return zhDescDict;
		}
	}
}