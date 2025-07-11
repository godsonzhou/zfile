using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace zfile
{
	// 定义 ColDef 类
	public class ColDef
	{
		public string header;
		public int width;
		public string content;
	}
	public class ViewMode
	{
		public string Name { get; set; }
		public string Icon { get; set; }
		public string Options { get; set; }

		public override string ToString()
		{
			return $"ViewMode(name='{Name}', icon='{Icon}', options='{Options}')";
		}
	}
	public class ViewSwitchRule
	{
		public string rules;
		public string mode;
	}

	public class ViewMgr
	{
		private MainForm form;
		private List<ColDef> colDefs = new();
		public Dictionary<string, List<ColDef>> colDefDict = new();
		public List<string> ColViewNames => colDefDict.Keys.ToList();
		public Dictionary<string, ViewMode> viewModes = new();
		public Dictionary<string, ViewSwitchRule> viewSwitchRules = new();

		// Default view mode to use when no rules match
		private string defaultViewMode = "1";	// 0 means default

		// Currently applied view modes for left and right panels
		private string currentLeftViewMode = "1";
		private string currentRightViewMode = "1";

		public ViewMgr(MainForm form)
		{
			this.form = form;
			ParseConfig();
			ParseViewModeCfg();
			ParseViewSwitchRule();
		}

		public string GetColDef(string viewMode)
		{
			if (colDefDict.ContainsKey(viewMode))
			{
				var colDefs = colDefDict[viewMode];
				var result = new StringBuilder();
				foreach (var colDef in colDefs)
					result.Append($"[{colDef.header}] ");
				return result.ToString();
			}
			return "";
		}

		/// <summary>
		/// Apply view settings to a ListView based on folder statistics and rules
		/// </summary>
		public string ApplyViewToListView(ListView listView, string folderPath, IFileSource fileSource, out FileEntries files, string colViewId = "")
		{
			try
			{
				// Get folder statistics
				var stats = FolderStatistics.GetFolderStats(folderPath, fileSource);
				files = stats.files;

				// Determine which view mode to use based on rules
				//bugfix: 当filesource is not filesystemfilesource, do not support custome view mode
				 if (string.IsNullOrEmpty(colViewId))
				{
					if(form.uiManager.isThumbs)
						colViewId = "4"; // Use Tile view (=4) for thumbnails mode, ignore custom view mode
					else
						colViewId = (fileSource is not FileSystemFileSource) ? ((int)listView.View).ToString() : DetermineViewMode(stats, folderPath);
				}
				// Apply column configuration from the selected view mode
				ApplyColumnConfiguration(listView, colViewId);
		
				Debug.Print($"Applied view mode '{colViewId}' to {(listView.Name)} panel for path: {folderPath}");
				return colViewId;
			}
			catch (Exception ex)
			{
				Debug.Print($"Error applying view to ListView: {ex.Message}");
			}
			files = new();
			return defaultViewMode;
		}

		/// <summary>
		/// Determine which view mode to use based on folder statistics and rules
		/// </summary>
		private string DetermineViewMode(FolderStatistics.FolderStats stats, string folderPath)
		{
			// Default to the default view mode
			string selectedViewMode = defaultViewMode;

			// Check each rule in priority order (lower index = higher priority)
			foreach (var ruleEntry in viewSwitchRules.OrderBy(r => int.Parse(r.Key)))
			{
				var rule = ruleEntry.Value;
				if (EvaluateRule(rule.rules, stats, folderPath))
				{
					selectedViewMode = rule.mode;
					break;
				}
			}
	
			// If the selected view mode doesn't exist in our definitions, fall back to default
			//if (!viewModes.Values.Any(vm => vm.Name == selectedViewMode))
			//	selectedViewMode = defaultViewMode;

			if (selectedViewMode != defaultViewMode)
				return viewModes[selectedViewMode].Options.Split('|')[0]; //{[3, {ViewMode(name='图片', icon='', options='10|-1|0||32896|-1|-1|-1|-1')}]} //10-6=4 is 列视图编号
			return selectedViewMode;
		}
		private string GetViewModeOptionString(string viewmode)
		{
			foreach(var v in viewModes.Values)
			{
				if (v.Options.Split("|")[0] == viewmode)
					return v.Options;
			}
			return string.Empty;
		}
		/// <summary>
		/// Evaluate a rule against folder statistics
		/// </summary>
		private bool EvaluateRule(string ruleString, FolderStatistics.FolderStats stats, string folderPath)
		{
			// Split the rule into sub-rules
			string[] subRules = ruleString.Split('|');
			if (subRules.Length == 0)
				return false;

			// The first sub-rule is evaluated independently
			bool result = EvaluateSubRule(subRules[0], stats, folderPath);

			// Evaluate additional sub-rules with logical operators
			for (int i = 1; i < subRules.Length; i++)
			{
				// Even indices are operators (AND/OR), odd indices are sub-rules
				if (i % 2 == 0)
				{
					// This is a sub-rule
					bool subResult = EvaluateSubRule(subRules[i], stats, folderPath);

					// Apply the previous operator
					string op = subRules[i - 1];
					if (op == "且")
						result = result && subResult;
					else // "或"
						result = result || subResult;
				}
			}

			return result;
		}

		/// <summary>
		/// Evaluate a single sub-rule against folder statistics
		/// </summary>
		private bool EvaluateSubRule(string subRule, FolderStatistics.FolderStats stats, string folderPath)
		{
			if (string.IsNullOrEmpty(subRule) || subRule.Length < 2)
				return false;

			// Extract rule type and value
			char ruleType = subRule[0];
			string ruleValue = subRule.Substring(1);

			// Evaluate based on rule type
			switch (ruleType)
			{
				case '+': // Exact match for file extensions
					return MatchFilePatterns(folderPath, ruleValue, true);

				case '-': // Exclude file extensions
					return !MatchFilePatterns(folderPath, ruleValue, true);

				case '%': // At least half match
					return MatchFilePatterns(folderPath, ruleValue, false, 0.5);

				case '2': // At least one match
					return MatchFilePatterns(folderPath, ruleValue, false, 0.01);

				case 'D': // Is directory/folder
					return stats.TotalFolders > 0;

				case 'L': // Contains drive letter
					return Path.GetPathRoot(folderPath)?.Length > 0;

				case 'U': // Network path
					return stats.IsNetworkPath;

				case 'V': // Virtual folder
					return stats.IsVirtualFolder;

				case 'F': // FTP connection
					return stats.IsFtpFolder;

				case 'A': // Archive file
					return stats.IsArchiveFolder;

				case 'P': // File system plugin
					return stats.IsPluginFolder;

				case 'S': // Search result
					return stats.IsSearchResult;

				default:
					return false;
			}
		}

		/// <summary>
		/// Match file patterns against files in a folder
		/// </summary>
		private bool MatchFilePatterns(string folderPath, string patterns, bool exactMatch, double threshold = 1.0)
		{
			try
			{
				if (!Directory.Exists(folderPath))
					return false;

				string[] patternList = patterns.Split(' ');

				// Get all files in the directory
				string[] files = Directory.GetFiles(folderPath);
				if (files.Length == 0)
					return false;

				int matchCount = 0;

				foreach (string file in files)
				{
					string extension = Path.GetExtension(file).ToLowerInvariant();

					foreach (string pattern in patternList)
					{
						string cleanPattern = pattern.Trim().ToLowerInvariant();

						// Handle wildcard patterns
						if (cleanPattern.Contains("*"))
						{
							if (IsWildcardMatch(Path.GetFileName(file).ToLowerInvariant(), cleanPattern))
							{
								matchCount++;
								break; // Count each file only once
							}
						}
						// Handle extension matching
						else if (cleanPattern.StartsWith(".") && extension.Equals(cleanPattern, StringComparison.OrdinalIgnoreCase))
						{
							matchCount++;
							break; // Count each file only once
						}
					}
				}

				// Calculate match ratio
				double matchRatio = (double)matchCount / files.Length;

				// For exact match, all files must match
				if (exactMatch)
					return matchRatio >= 0.99;

				// For partial match, compare against threshold
				return matchRatio >= threshold;
			}
			catch (Exception ex)
			{
				Debug.Print($"Error matching file patterns: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Check if a filename matches a wildcard pattern
		/// </summary>
		private bool IsWildcardMatch(string filename, string pattern)
		{
			// Convert wildcard pattern to regex
			string regexPattern = "^" + Regex.Escape(pattern)
				.Replace("\\*", ".*")
				.Replace("\\?", ".") + "$";

			return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase);
		}

		/// <summary>
		/// Apply column configuration to a ListView based on view mode
		/// </summary>
		public void ApplyColumnConfiguration(ListView listView, string colViewId)
		{
			if (colViewId.Equals(listView.Name.Equals("L") ? currentLeftViewMode : currentRightViewMode))
				return;
			// Update current view mode
			if (listView.Name.Equals("L"))
				currentLeftViewMode = colViewId;
			else
				currentRightViewMode = colViewId;
			var options = GetViewModeOptionString(colViewId);
			//options look like "6|-1|0||-1|-1|-1|-1|-1"
			//"14|1|0|1|128|-1|-1|-1|-1"
			//对应定义： 列视图编号|排序方式（-1不变，0文件名，1扩展名...）|升降序（0-升序，1-降序）|附加排序列号|标签颜色-255（红色）|？|？|？|？（背景颜色/偶数行背景颜色）
			var viewmodeid = int.Parse(colViewId);
			if (viewmodeid < 5) 
			{
				// Apply default view mode
				listView.View = (View)viewmodeid;	//default is 0, largeicon mode
				listView.Columns.Clear();
				listView.Columns.Add("名称", 250);
				listView.Columns.Add("大小", 80);
				listView.Columns.Add("类型", 60);
				listView.Columns.Add("修改时间", 130);
				listView.Columns.Add("属性", 60);
			}
			else if (viewmodeid == 5)
			{
				//do nothing, 5 is seperator line
			}
			else 
			{
				// >= 6, apply custom view mode
				viewmodeid -= 6;
				colViewId = (viewmodeid).ToString(); //
				var coldefvalues = colDefDict.Values.ToArray();
				if (viewmodeid >= coldefvalues.Length )
				{
					Debug.Print($"View mode '{colViewId}' not found in column definitions");
					return;
				}

				try
				{
					// Get column definitions for the view mode
					var columns = coldefvalues[viewmodeid];

					// Begin updating the ListView
					listView.BeginUpdate();

					// Clear existing columns
					listView.Columns.Clear();

					// Add columns based on definitions
					foreach (var colDef in columns)
					{
						// Create column with header and width
						ColumnHeader column = new ColumnHeader
						{
							Text = colDef.header,
							Width = Math.Abs(colDef.width) * 2
						};

						// Set alignment based on content
						if (colDef.content.Contains("->") || colDef.content.Contains("=tc.size", StringComparison.OrdinalIgnoreCase))
							column.TextAlign = HorizontalAlignment.Right;
						else
							column.TextAlign = HorizontalAlignment.Left;

						// Add column to ListView
						listView.Columns.Add(column);
					}

					// Finish updating
					listView.EndUpdate();
				}
				catch (Exception ex)
				{
					Debug.Print($"Error applying column configuration: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Get the current view mode for a panel
		/// </summary>
		public string GetCurrentViewMode(bool isLeftPanel)
		{
			return isLeftPanel ? currentLeftViewMode : currentRightViewMode;
		}

		public void ParseViewSwitchRule()
		{
			var section = form.configLoader.GetConfigSection("ViewModeSwitch");
			foreach (var item in section.Items)
			{
				Regex regex = new Regex(@"^(\d+)_(rules|mode)=(.*)$");
				string line = item.Key + "=" + item.Value;
				{
					Match match = regex.Match(line);
					if (match.Success)
					{
						string index = match.Groups[1].Value;
						string key = match.Groups[2].Value;
						string value = match.Groups[3].Value;

						if (!viewSwitchRules.ContainsKey(index))
							viewSwitchRules[index] = new ViewSwitchRule { rules = "", mode = "" };

						switch (key)
						{
							case "rules":
								viewSwitchRules[index].rules = value;
								break;
							case "mode":
								viewSwitchRules[index].mode = value;
								break;
						}
					}
				}
			}
		}
		public void ParseViewModeCfg()
		{
			var section = form.configLoader.GetConfigSection("ViewModes");
			foreach (var item in section.Items)
			{
				Regex regex = new Regex(@"^(\d+)_(name|icon|options)=(.*)$");

				string line = item.Key + "=" + item.Value;
				{
					Match match = regex.Match(line);
					if (match.Success)
					{
						string index = match.Groups[1].Value;
						string key = match.Groups[2].Value;
						string value = match.Groups[3].Value;

						if (!viewModes.ContainsKey(index))
						{
							viewModes[index] = new ViewMode { Name = "", Icon = "", Options = "" };
						}

						switch (key)
						{
							case "name":
								viewModes[index].Name = value;
								break;
							case "icon":
								viewModes[index].Icon = value;
								break;
							case "options":
								viewModes[index].Options = value;
								break;
						}
					}
				}
			}
		}
		public void ParseConfig()
		{
			var titles = form.configLoader.FindConfigValue("CustomFields", "Titles");
			var section = form.configLoader.GetConfigSection("CustomFields");
			List<string> headerlist = new();
			List<string> widthlist = new();
			List<string> contentlist = new();
			foreach (var item in section.Items)
			{
				if (item.Key.StartsWith("Headers"))
					headerlist.Add(item.Value);
				else if (item.Key.StartsWith("Widths"))
					widthlist.Add(item.Value);
				else if (item.Key.StartsWith("Contents"))
					contentlist.Add(item.Value);
			}
			var idx = 0;
			var heads = headerlist.ToArray();
			var widths = widthlist.ToArray();
			var contents = contentlist.ToArray();
			foreach (var t in titles.Split('|'))
			{
				colDefDict[t] = parseColDef(heads[idx], widths[idx], contents[idx]);
				idx++;
			}
		}
		private List<ColDef> parseColDef(string headers, string widths, string contents)
		{
			var result = new List<ColDef>();
			var h = ("文件名\n扩展名\n" + headers).Replace("\\n", "\n").Split('\n');
			var w = widths.Split(',').Select(int.Parse).ToArray();
			var c = ("文件名\n扩展名\n" + contents).Replace("\\n", "\n").Split('\n');
			;
			for (int i = 0; i < w.Count(); i++)
			{
				result.Add(new ColDef
				{
					header = h[i],
					width = w[i],
					content = c[i]
				});
			}
			return result;
		}
		internal string GetColViewIdByViewMode(string viewmode)
		{
			var item = viewModes[(viewmode)];
			return item.Options.Split('|')[0];
		}
		//internal string GetViewModeByColViewID(string param)
		//{
		//	foreach( var v in viewModes)
		//	{
		//		if (v.Value.Options.Split('|')[0] == param)
		//			return v.Key.ToString();
		//	}
		//	return "";
		//}
		internal List<MenuInfo> GetMenuInfoFromCustomView()
		{
			//生成自定义列视图的菜单项
			var menus = new List<MenuInfo>();
			for(var i = 0; i < ColViewNames.Count; i++)
			{
				var v = ColViewNames[i];
				var m = new MenuInfo(v);
				m.Menu = v;
				m.Cmd = "cm_srccustomviewmenu";
				m.Param = (i+6).ToString(); //bugfix: should add 6 to index, because default view modes are 0-5
				menus.Add(m);
			}
			// Add a separator for custom views
			menus.Add(new MenuInfo("-")); // Add a separator

			// add a menu item for config
			var configMenu = new MenuInfo("配置...");
			configMenu.Menu = "配置...";
			configMenu.Cmd = "cm_srccustomviewmenu";
			menus.Add(configMenu);
			return menus;
		}
		internal List<MenuInfo> GetMenuInfoFromViewModes()
		{
			var menus = new List<MenuInfo>();
			foreach (var v in viewModes)
			{
				var m = new MenuInfo(v.Value.Name);
				m.Menu = v.Value.Name;
				m.Cmd = "cm_srcviewmodelist";
				m.Param = v.Key;
				menus.Add(m);
			}
			// Add a separator for custom views
			menus.Add(new MenuInfo("-")); // Add a separator

			// add a menu item for config
			var configMenu = new MenuInfo("配置...");
			configMenu.Menu = "配置...";
			configMenu.Cmd = "cm_srcviewmodelist";
			menus.Add(configMenu);

			return menus;
		}
	}
}
