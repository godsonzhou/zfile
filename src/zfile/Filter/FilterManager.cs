using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using zfile.Forms;
//using zfile.vfs.FileSources;

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

        /// <summary>
        /// 当前活动的过滤器
        /// </summary>
        public FileFilter CurrentFilter { get; set; }

        /// <summary>
        /// 是否启用过滤
        /// </summary>
        public bool IsFilterEnabled { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        private FilterManager()
        {
            CurrentFilter = new FileFilter();
            IsFilterEnabled = false;
        }

        /// <summary>
        /// 应用过滤器到文件列表
        /// </summary>
        public FileEntries ApplyFilter(FileEntries files)
        {
            if (!IsFilterEnabled || CurrentFilter.FilterMode == FilterMode.None)
                return files;

            FileEntries result = new FileEntries();
            result.Path = files.Path;

            foreach (var file in files)
            {
                if (CurrentFilter.MatchesFilter(file))
                {
                    result.Add(file);
                }
            }

            return result;
        }

        /// <summary>
        /// 显示过滤器对话框
        /// </summary>
        public bool ShowFilterDialog()
        {
            using (FilterDialog dialog = new FilterDialog())
            {
                // 设置对话框的过滤器为当前过滤器的副本
                //dialog.Filter = CurrentFilter.Clone();
                //dialog.IsFilterEnabled = IsFilterEnabled;

                // 显示对话框
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 用户点击了确定，更新当前过滤器
                    //CurrentFilter = dialog.Filter;
                    //IsFilterEnabled = dialog.IsFilterEnabled;
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
            CurrentFilter = new FileFilter();
            IsFilterEnabled = false;
        }

        /// <summary>
        /// 设置过滤器
        /// </summary>
        public void SetFilter(FileFilter filter, bool enabled)
        {
            CurrentFilter = filter ?? new FileFilter();
            IsFilterEnabled = enabled;
        }

        /// <summary>
        /// 获取过滤器状态描述
        /// </summary>
        public string GetFilterStatusDescription()
        {
            if (!IsFilterEnabled || CurrentFilter.FilterMode == FilterMode.None)
                return "无过滤";

            switch (CurrentFilter.FilterMode)
            {
                case FilterMode.ByName:
                    return $"按名称过滤: {CurrentFilter.NamePattern}";

                case FilterMode.ByExtension:
                    return $"按扩展名过滤: {CurrentFilter.Extensions}";

                case FilterMode.BySize:
                    return "按大小过滤";

                case FilterMode.ByDate:
                    return "按日期过滤";

                case FilterMode.ByAttributes:
                    return "按属性过滤";

                default:
                    return "已启用过滤";
            }
        }
    }
}