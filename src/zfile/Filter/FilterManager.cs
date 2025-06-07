using zfile.Forms;

namespace zfile.Filter
{
    /// <summary>
    /// 过滤器管理器类，用于管理文件过滤器和提供UI接口
    /// </summary>
    public class FilterManager
    {
        private static FilterManager _instance;

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
		// 加载搜索模板列表到ListBox控件
		public HashSet<string> LoadSearchTemplates(ListBox listBox)
		{
			HashSet<string> templateNames = new HashSet<string>();
			listBox.Items.Clear();

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

				// 添加到列表框
				foreach (var name in templateNames)
				{
					listBox.Items.Add(name);
				}
			}
			return templateNames;
		}

		// 更新或添加配置项
		public void UpdateOrAddConfigItem(ConfigSection section, string key, string value)
		{
			var item = section.Items.FirstOrDefault(i => i.Key == key);
			if (item != null)
			{
				item.Value = value;
			}
			else
			{
				section.Items.Add(new ConfigItem { Key = key, Value = value });
			}
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
		/// 当前活动的过滤器
		/// </summary>
		public List<FileFilter> CurrentFilters { get; set; }

        /// <summary>
        /// 是否启用过滤
        /// </summary>
        public bool IsFilterEnabled { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        private FilterManager()
        {
            CurrentFilters = new ();
            IsFilterEnabled = false;
        }

        /// <summary>
        /// 应用过滤器到文件列表
        /// </summary>
        public FileEntries ApplyFilter(FileEntries files)
        {
            if (!IsFilterEnabled) // || CurrentFilters.FilterMode == FilterMode.None)
                return files;

            FileEntries result = new FileEntries();
            result.Path = files.Path;

            foreach (var file in files)
            {
				bool ismatch = true;
				foreach (var filter in CurrentFilters)
				{
					if (!filter.MatchesFilter(file))
					{
						ismatch = false;
						break;
					}
				}
				if(ismatch)
					result.Add(file);
			}

			return result;
        }

        /// <summary>
        /// 显示过滤器对话框
        /// </summary>
        public List<FileFilter>? ShowFilterDialog(Form owner = null)
        {
            using (FilterDialog dialog = new FilterDialog(owner as MainForm))
            {
                // 显示对话框
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    IsFilterEnabled = true;
                    return FilterManager.Instance.CurrentFilters;
                }

                return null;
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
        public void SetFilter(List<FileFilter> filter, bool enabled)
        {
            CurrentFilters = filter ?? new List<FileFilter>();
            IsFilterEnabled = enabled;
        }

		public void LoadSearchTemplate(string templateName)
		{
			if (string.IsNullOrEmpty(templateName))
			{
				MessageBox.Show("请先选择一个搜索模板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			var templateData = Filter.FilterManager.Instance.GetSearchTemplateFromCfg(templateName);
			if (templateData.Count == 0)
			{
				MessageBox.Show("无法加载搜索模板", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// 解析SearchFlags
			string searchFlags = templateData.ContainsKey(templateName + "_SearchFlags") ?
				templateData[templateName + "_SearchFlags"] : "";

			// 解析SearchFor (搜索模式)
			string searchFor = templateData.ContainsKey(templateName + "_SearchFor") ?
				templateData[templateName + "_SearchFor"] : "";

			// 解析SearchIn (搜索位置)
			string searchIn = templateData.ContainsKey(templateName + "_SearchIn") ?
				templateData[templateName + "_SearchIn"] : "";

			// 解析SearchText (搜索文本)
			string searchText = templateData.ContainsKey(templateName + "_SearchText") ?
				templateData[templateName + "_SearchText"] : "";

			// 解析SearchFlags并设置相应的UI控件
			if (!string.IsNullOrEmpty(searchFlags))
			{
				string[] flagParts = searchFlags.Split('|');
				if (flagParts.Length > 1)
				{
					

					// 如果有日期类型 (第6个参数)
					if (flagParts.Length > 5 && !string.IsNullOrEmpty(flagParts[5]))
					{
						int dateType;
						if (int.TryParse(flagParts[5], out dateType))
						{
							// 1=修改日期, 2=创建日期, 3=访问日期
							// 这里可以设置相应的UI控件，如果有的话
						}
					}
				}
			}

			MessageBox.Show($"已加载搜索模板: {templateName}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		/// <summary>
		/// 获取过滤器状态描述
		/// </summary>
		public string GetFilterStatusDescription()
        {
            if (!IsFilterEnabled)
                return "无过滤";

            //switch (CurrentFilters.FilterMode)
            //{
            //    case FilterMode.ByName:
            //        return $"按名称过滤: {CurrentFilters.NamePattern}";

            //    case FilterMode.ByExtension:
            //        return $"按扩展名过滤: {CurrentFilters.Extensions}";

            //    case FilterMode.BySize:
            //        return "按大小过滤";

            //    case FilterMode.ByDate:
            //        return "按日期过滤";

            //    case FilterMode.ByAttributes:
            //        return "按属性过滤";

            //    default:
                    return "已启用过滤";
            //}
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
				CurrentFilters.Remove(f);
		}
	}
}