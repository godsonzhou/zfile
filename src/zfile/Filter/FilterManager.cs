using System.Text;
using zfile.Forms;

namespace zfile.Filter
{
    /// <summary>
    /// 过滤器管理器类，用于管理文件过滤器和提供UI接口
    /// </summary>
    public class FilterManager
    {
        private static FilterManager? _instance;
		internal bool ShowOnlySelected;

		/// <summary>
		/// 当前活动的过滤器
		/// </summary>
		public List<FileFilter> CurrentFilters { get; set; }
		public HashSet<string> AllFilterNames { get; set; }
		public List<string> CurrentFilterNames => CurrentFilters.Select(f => f.Name).ToList();
		public List<string> AvailableFilterNames { get
			{
				// 返回所有可用的过滤器名称，不包括当前过滤器
				return AllFilterNames.Except(CurrentFilterNames).ToList();
			}
		}
		/// <summary>
		/// 是否启用过滤
		/// </summary>
		public bool IsFilterEnabled { get; set; }
		public bool CurrentFilterAndOr { get; set; } = false;
		/// <summary>
		/// 获取FilterManager的单例实例
		/// </summary>
		public static FilterManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FilterManager();
                return _instance;
            }
        }

		/// <summary>
		/// 构造函数
		/// </summary>
		private FilterManager()
		{
			CurrentFilters = new();
			IsFilterEnabled = false;
			AllFilterNames =  LoadAllTemplate();
		}

		private HashSet<string> LoadAllTemplate()
		{
			HashSet<string> templateNames = [];

			var searchesSection = MainForm.Instance.configLoader.GetConfigSection("Searches");
			if (searchesSection != null)
			{
				// 提取所有搜索模板名称
				foreach (var item in searchesSection.Items)
				{
					string key = item.Key;
					if (key.Contains("_SearchFlags") || key.Contains("_SearchFor") ||
						key.Contains("_SearchIn") || key.Contains("_SearchText"))
					{
						string templateName = key.Substring(0, key.IndexOf('_'));
						templateNames.Add(templateName);
					}
				}
			}
			return templateNames;
		}
		// 加载搜索模板列表到ListBox控件
		public void LoadSearchTemplates(ListBox listBox, bool useavailable = true)
		{	
			listBox.Items.Clear();

			// 添加到列表框
			foreach (var name in useavailable ? AvailableFilterNames : CurrentFilterNames)
				listBox.Items.Add(name);
		}

		// 更新或添加配置项
		public void UpdateOrAddConfigItem(ConfigSection section, string key, string value)
		{
			var item = section.Items.FirstOrDefault(i => i.Key == key);
			if (item != null)
				item.Value = value;
			else
				section.Items.Add(new ConfigItem { Key = key, Value = value });
		}

		// 删除搜索模板
		public void DeleteSearchTemplate(string templateName, ListBox listBox)
		{
			if (string.IsNullOrEmpty(templateName))
			{
				MessageBox.Show("请先选择一个搜索模板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (MessageBox.Show($"确定要删除搜索模板 '{templateName}' 吗？", "确认删除",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				//if (owner is MainForm mainForm)
				MainForm mainForm = MainForm.Instance;
				{
					var searchesSection = mainForm.configLoader.GetConfigSection("Searches");
					if (searchesSection != null)
					{
						// 删除与模板名称匹配的所有配置项
						searchesSection.Items.RemoveAll(item => item.Key.StartsWith(templateName + "_"));

						// 保存配置
						mainForm.configLoader.SaveConfig();

						// 刷新模板列表
						LoadSearchTemplates(listBox);

						MessageBox.Show($"搜索模板 '{templateName}' 已删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			}
		}
		// 从配置中解析搜索模板
		public Dictionary<string, string> GetSearchTemplateFromCfg(string templateName)
		{
			Dictionary<string, string> templateData = [];

			var mainForm = MainForm.Instance;
			{
				var searchesSection = mainForm.configLoader.GetConfigSection("Searches");
				if (searchesSection != null)
				{
					// 查找与模板名称匹配的所有配置项
					foreach (var item in searchesSection.Items)
					{
						if (item.Key.StartsWith(templateName + "_"))
						{
							templateData[item.Key] = item.Value;
						}
					}
				}
			}

			return templateData;
		}

        /// <summary>
        /// 应用过滤器到文件列表
        /// </summary>
        public FileEntries ApplyFilter(FileEntries files)
        {
            if ((!IsFilterEnabled || CurrentFilters.Count == 0) && !ShowOnlySelected)
                return files;

            FileEntries result = new FileEntries();
            result.Path = files.Path;

            foreach (var file in files)
            {
				if (ShowOnlySelected)
				{
					if (MainForm.Instance.SelectedItems.Contains(file.Name))
						result.Add(file);
				}
				else
				{
					bool ismatch = CurrentFilterAndOr;
					foreach (var filter in CurrentFilters)  //bugfix: 遍历所有过滤器，判断文件是否符合任意一个过滤器的条件
					{
						if (GetFlagByAndOr(filter.MatchesFilter(file)))
						{
							ismatch = !CurrentFilterAndOr;
							break;
						}
					}

					if (ismatch)
						result.Add(file);
				}
			}

			return result;
        }

		private bool GetFlagByAndOr(bool v)
		{
			return !CurrentFilterAndOr ? v : !v;
		}

		/// <summary>
		/// 显示过滤器对话框
		/// </summary>
		public bool ShowFilterDialog(Form owner = null)
        {
            using (FilterDialog dialog = new FilterDialog(owner as MainForm))
            {
                // 显示对话框
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    IsFilterEnabled = true;
					return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 清除当前过滤器
        /// </summary>
        public void ClearFilter()
        {
			CurrentFilters.Clear();
            IsFilterEnabled = false;
        }

        /// <summary>
        /// 设置过滤器
        /// </summary>
        public void SetFilter(List<FileFilter>? filter, bool enabled = true)
        {
            CurrentFilters = filter ?? new List<FileFilter>();
            IsFilterEnabled = enabled;
        }

		public List<FileFilter>? LoadSearchTemplate(string templateName)
		{
			if (string.IsNullOrEmpty(templateName))
			{
				MessageBox.Show("请先选择一个搜索模板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return null;
			}

			var templateData = Filter.FilterManager.Instance.GetSearchTemplateFromCfg(templateName);
			if (templateData.Count == 0)
			{
				MessageBox.Show("无法加载搜索模板", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
	
			var result = createFilterBySearchTemplte(templateData);

			//MessageBox.Show($"已加载搜索模板: {templateName}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return result;
		}

		private List<FileFilter> createFilterBySearchTemplte(Dictionary<string, string> templateData)
		{
			var result = new List<FileFilter>();
			if (templateData.Count == 0)
				return result;

			// 获取模板名称（从任意一个键中提取）
			string templateName = templateData.Keys.First().Split('_')[0];

			// 获取搜索设置
			string searchFlags = templateData.ContainsKey($"{templateName}_SearchFlags") ?
				templateData[$"{templateName}_SearchFlags"] : "";
			string searchFor = templateData.ContainsKey($"{templateName}_SearchFor") ?
				templateData[$"{templateName}_SearchFor"] : "";
			string searchIn = templateData.ContainsKey($"{templateName}_SearchIn") ?
				templateData[$"{templateName}_SearchIn"] : "";
			string searchText = templateData.ContainsKey($"{templateName}_SearchText") ?
				templateData[$"{templateName}_SearchText"] : "";

			// 创建文件过滤器
			var filter = new FileFilter(templateName);

			// 解析 SearchFlags (格式: flag1|flag2|flag3|date|comp|type|sizeop|size|sizeunit|attr|reserved)
			string[] flagParts = searchFlags.Split('|');
			if (flagParts.Length > 5)
			{
				// 基本标志位解析
				string baseFlags = flagParts[0];

				// 解析扩展名过滤
				if (!string.IsNullOrEmpty(searchFor))
				{
					filter.FilterMode |= FilterMode.ByExtension;
					filter.Extensions = searchFor;
					// 检查是否是排除模式
					filter.ExcludeExtensions = baseFlags.Contains("exclude");
				}

				// 解析日期过滤
				//ion文件1_SearchFlags=0|002002000020|2025/06/08 21:16:31|9999/01/01 00:00:00||||||12220|0000|||
				// 2： datefrom
				// 3: dateto
				if (!string.IsNullOrEmpty(flagParts[2]))
				{
					filter.FilterMode |= FilterMode.ByDate;
					filter.DateType = DateType.Modified; // 默认使用修改时间
					if (DateTime.TryParse(flagParts[2], out DateTime dateFrom))
					{
						filter.MinDate = dateFrom;
						filter.DateComparisonType = ComparisonType.Greater; // 不早于
					}
					if(DateTime.TryParse(flagParts[3], out var dateto))
					{
						filter.MaxDate = dateto;
						filter.DateComparisonType = ComparisonType.Less; // 不晚于
					}
				}
				//今日新文件_SearchFlags=0|00000200| | |1|1| | | | |0000
				//本周新文件_SearchFlags=0|00000200| | |7|1| | | | |0000
				//4 = 不早于的时间数
				//5 = 不早于的时间单位 -1分钟 0小时 1天 2周 3月 4年
				//ion文件1_SearchFlags=0|002002000020| | | | | | | | |0000| |45|3
				//12： 早于的时间数
				//13： 早于的时间单位 -1分钟 0小时 1天 2周 3月 4年
				if (!string.IsNullOrEmpty(flagParts[4]))
				{
					if (int.TryParse(flagParts[4], out var days) && days > 0)
					{
						filter.FilterMode |= FilterMode.ByDate;
						filter.DateComparisonType = ComparisonType.Greater;
						filter.DateType = DateType.Modified; // 默认使用创建时间
						if (int.TryParse(flagParts[5], out int timeUnit))
						{
							// 根据时间单位设置最小日期
							switch (timeUnit)
							{
								case -1: // 分钟
									filter.MinDate = DateTime.Now.AddMinutes(-days);
									break;
								case 0: // 小时
									filter.MinDate = DateTime.Now.AddHours(-days);
									break;
								case 1: // 天
									filter.MinDate = DateTime.Now.AddDays(-days);
									break;
								case 2: // 周
									filter.MinDate = DateTime.Now.AddDays(-days * 7);
									break;
								case 3: // 月
									filter.MinDate = DateTime.Now.AddMonths(-days);
									break;
								case 4: // 年
									filter.MinDate = DateTime.Now.AddYears(-days);
									break;
							}
						}
					}
				}
				if (flagParts.Length > 13 && !string.IsNullOrEmpty(flagParts[12]))
				{
					if (int.TryParse(flagParts[12], out var days) && days > 0)
					{
						filter.FilterMode |= FilterMode.ByDate;
						filter.DateComparisonType = ComparisonType.Less;
						filter.DateType = DateType.Modified; // 默认使用创建时间
						if (int.TryParse(flagParts[13], out int timeUnit))
						{
							// 根据时间单位设置最小日期
							switch (timeUnit)
							{
								case -1: // 分钟
									filter.MaxDate = DateTime.Now.AddMinutes(-days);
									break;
								case 0: // 小时
									filter.MaxDate = DateTime.Now.AddHours(-days);
									break;
								case 1: // 天
									filter.MaxDate = DateTime.Now.AddDays(-days);
									break;
								case 2: // 周
									filter.MaxDate = DateTime.Now.AddDays(-days * 7);
									break;
								case 3: // 月
									filter.MaxDate = DateTime.Now.AddMonths(-days);
									break;
								case 4: // 年
									filter.MaxDate = DateTime.Now.AddYears(-days);
									break;
							}
						}
					}
				}
				// 0 字节文件_SearchFlags=0|00000200| | | | |0|0|0|22220|0000
				// 6 = sizeop, 0:= 1:> 2:<
				// 7 = size,
				// 8 = sizeunit, 0:B 1:KB 2:MB 3:GB 4:TB
				// 解析大小过滤
				if (!string.IsNullOrEmpty(flagParts[6]))
				{
					if (int.TryParse(flagParts[7], out int size) &&
						int.TryParse(flagParts[8], out int sizeUnit))
					{
						filter.FilterMode |= FilterMode.BySize;
						// sizeUnit: 1=KB, 2=MB, 3=GB
						long multiplier = sizeUnit switch
						{
							0 => 1L, // 字节
							1 => 1024L,
							2 => 1024L * 1024L,
							3 => 1024L * 1024L * 1024L,
							_ => 1L
						};

						if (flagParts[6].Equals("0")) // 等于
						{
							filter.SizeComparisonType = ComparisonType.Equal;
							filter.MinSize = filter.MaxSize = size * multiplier;
						}
						else if (flagParts[6] == "1") // 大于
						{
							filter.SizeComparisonType = ComparisonType.Greater;
							filter.MinSize = size * multiplier;
						}
						else if (flagParts[6] == "2") // 小于
						{
							filter.SizeComparisonType = ComparisonType.Less;
							filter.MaxSize = size * multiplier;
						}
					}
				}

				// 解析文件属性 (第10个参数，5位数字代表: 存档|只读|隐藏|系统|目录)
				//系统（按属性）_SearchFlags=0|00000200| | | | | | | |22212|0000
				// 隐藏文件_SearchFlags=0|00000200||||||||22122|0000
				//文件夹_SearchFlags=0|00000200||||||||22221|0000
				//0 : 不选 ， 1：选中， 2：保留
				if (flagParts.Length > 9 && flagParts[9].Length == 5)
				{
					string attrs = flagParts[9];
					filter.IncludeDirectories = (IncludeType)int.Parse(attrs[4].ToString());
					filter.IncludeSystem = (IncludeType)int.Parse(attrs[3].ToString());
					filter.IncludeHidden = (IncludeType)int.Parse(attrs[2].ToString());
					filter.IncludeReadOnly = (IncludeType)int.Parse(attrs[1].ToString());
					//if(filter.IncludeSystem || filter.IncludeHidden || filter.IncludeReadOnly)
					filter.FilterMode |= FilterMode.ByAttributes;
				}
			}

			if (filter.FilterMode != FilterMode.None)
			{
				result.Add(filter);
			}

			return result;
		}


		/// <summary>
		/// 获取过滤器状态描述
		/// </summary>
		public string GetFilterStatusDescription()
        {
			return (IsFilterEnabled && FilterManager.Instance.CurrentFilters.Count != 0) ? "已启用过滤 : " + string.Join(", ", FilterManager.Instance.CurrentFilterNames) : "";
        }

		internal void AddFilter(List<FileFilter>? filter)
		{
			if(filter != null)
				CurrentFilters.AddRange(filter);
		}

		internal void RemoveFilter(List<FileFilter>? filter)
		{
			if (filter == null) return;
			foreach(var f in filter)
			{
				//todo: bugfix: 应该依据过滤器名称来删除，而不是对象引用
				var existingFilter = CurrentFilters.FirstOrDefault(x => x.Name == f.Name);
				if (existingFilter != null)	
					CurrentFilters.Remove(existingFilter);
			}
		}

		internal object GetFilterByName(string v)
		{
			throw new NotImplementedException();
		}
	}
}