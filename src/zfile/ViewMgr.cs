using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zfile
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
		private List<ColDef> colDefs = new ();
		public Dictionary<string, List<ColDef>> colDefDict = new();
		public Dictionary<string, ViewMode> viewModes = new ();
		public Dictionary<string, ViewSwitchRule> viewSwitchRules = new ();

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
			var result = new List<ColDef> ();
			var h = ("文件名\n扩展名\n"+headers).Replace("\\n","\n").Split('\n');
			var w = widths.Split(',').Select(int.Parse).ToArray();
			var c = ("文件名\n扩展名\n" + contents).Replace("\\n","\n").Split('\n');
;
			for (int i = 0; i < w.Count(); i++) {
				result.Add(new ColDef
				{
					header = h[i],
					width = w[i],
					content = c[i]
				});
			}
			return result;
		}
	} 
}
