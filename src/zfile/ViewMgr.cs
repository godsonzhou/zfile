using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zfile;

namespace Zfile
{
	// 定义 ColDef 类
	public class ColDef
	{
		public string header;
		public int width;
		public string content;
	}
	public class ViewMgr
	{
		private Form1 form;
		private List<ColDef> colDefs = new ();
		private Dictionary<string, List<ColDef>> colDefDict = new();

		public ViewMgr(Form1 form)
		{
			this.form = form;
			ParseConfig();
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
