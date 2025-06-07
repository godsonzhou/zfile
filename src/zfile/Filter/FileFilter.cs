using System.Text.RegularExpressions;

namespace zfile.Filter
{
    /// <summary>
    /// 过滤模式枚举
    /// </summary>
    public enum FilterMode
    {
        None,
        ByName,
        ByExtension,
        BySize,
        ByDate,
        ByAttributes
    }

    /// <summary>
    /// 比较类型枚举
    /// </summary>
    public enum ComparisonType
    {
        Equal,
        Greater,
        Less,
        Between
    }

    /// <summary>
    /// 日期类型枚举
    /// </summary>
    public enum DateType
    {
        Modified,
        Created,
        Accessed
    }

    /// <summary>
    /// 文件过滤器类
    /// </summary>
    public class FileFilter
    {
        // 过滤模式
        public FilterMode FilterMode { get; set; } = FilterMode.None;

        // 名称过滤
        public string NamePattern { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; } = false;
        public bool UseRegex { get; set; } = false;

        // 扩展名过滤
        public string Extensions { get; set; } = string.Empty;
        public bool ExcludeExtensions { get; set; } = false;

        // 大小过滤
        public ComparisonType SizeComparisonType { get; set; } = ComparisonType.Equal;
        public long MinSize { get; set; } = 0;
        public long MaxSize { get; set; } = 0;

        // 日期过滤
        public ComparisonType DateComparisonType { get; set; } = ComparisonType.Equal;
        public DateType DateType { get; set; } = DateType.Modified;
        public DateTime MinDate { get; set; } = DateTime.Now;
        public DateTime MaxDate { get; set; } = DateTime.Now;

        // 属性过滤
        public bool IncludeDirectories { get; set; } = true;
        public bool IncludeHidden { get; set; } = true;
        public bool IncludeSystem { get; set; } = true;
        public bool IncludeReadOnly { get; set; } = true;

        /// <summary>
        /// 判断文件是否符合过滤条件
        /// </summary>
        public bool MatchesFilter(FileEntry file)
        {
            // 如果没有过滤，则所有文件都符合条件
            if (FilterMode == FilterMode.None)
                return true;

            // 根据文件类型过滤
            if (file.IsDirectory && !IncludeDirectories)
                return false;

            // 根据文件属性过滤
            if (FilterMode == FilterMode.ByAttributes || FilterMode == FilterMode.None)
            {
                if (file.IsHidden && !IncludeHidden)
                    return false;

                if (file.IsSysFile && !IncludeSystem)
                    return false;

                if (file.IsReadOnly && !IncludeReadOnly)
                    return false;

                // 如果是按属性过滤模式，到这里就可以返回true了
                if (FilterMode == FilterMode.ByAttributes)
                    return true;
            }

            // 如果是目录且不是按属性过滤，则始终显示目录
            if (file.IsDirectory && FilterMode != FilterMode.ByAttributes)
                return true;

            // 根据过滤模式进行过滤
            switch (FilterMode)
            {
                case FilterMode.ByName:
                    return MatchesNameFilter(file.Name);

                case FilterMode.ByExtension:
                    return MatchesExtensionFilter(file.Extension);

                case FilterMode.BySize:
                    return MatchesSizeFilter(file.Size);

                case FilterMode.ByDate:
                    return MatchesDateFilter(file);

                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断文件名是否符合过滤条件
        /// </summary>
        private bool MatchesNameFilter(string fileName)
        {
            if (string.IsNullOrEmpty(NamePattern))
                return true;

            if (UseRegex)
            {
                try
                {
                    RegexOptions options = CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    Regex regex = new Regex(NamePattern, options);
                    return regex.IsMatch(fileName);
                }
                catch
                {
                    // 正则表达式错误，返回false
                    return false;
                }
            }
            else
            {
                // 使用通配符匹配
                string pattern = WildcardToRegex(NamePattern);
                RegexOptions options = CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                Regex regex = new Regex(pattern, options);
                return regex.IsMatch(fileName);
            }
        }

        /// <summary>
        /// 将通配符转换为正则表达式
        /// </summary>
        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
        }

        /// <summary>
        /// 判断文件扩展名是否符合过滤条件
        /// </summary>
        private bool MatchesExtensionFilter(string extension)
        {
            if (string.IsNullOrEmpty(Extensions))
                return true;

            // 获取扩展名列表
            string[] extensionList = Extensions.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (extensionList.Length == 0)
                return true;

            // 检查扩展名是否在列表中
            bool inList = extensionList.Any(ext => 
                extension.Equals(ext, StringComparison.OrdinalIgnoreCase) || 
                extension.Equals("." + ext, StringComparison.OrdinalIgnoreCase));

            // 根据是否排除扩展名返回结果
            return ExcludeExtensions ? !inList : inList;
        }

        /// <summary>
        /// 判断文件大小是否符合过滤条件
        /// </summary>
        private bool MatchesSizeFilter(long fileSize)
        {
            switch (SizeComparisonType)
            {
                case ComparisonType.Equal:
                    return fileSize == MinSize;

                case ComparisonType.Greater:
                    return fileSize > MinSize;

                case ComparisonType.Less:
                    return fileSize < MinSize;

                case ComparisonType.Between:
                    return fileSize >= MinSize && fileSize <= MaxSize;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断文件日期是否符合过滤条件
        /// </summary>
        private bool MatchesDateFilter(FileEntry file)
        {
            DateTime fileDate;

            // 获取对应的日期
            switch (DateType)
            {
                case DateType.Created:
                    fileDate = file.CreationTime;
                    break;

                case DateType.Accessed:
                    fileDate = file.LastAccessTime;
                    break;

                case DateType.Modified:
                default:
                    fileDate = file.ModificationTime;
                    break;
            }

            // 比较日期
            switch (DateComparisonType)
            {
                case ComparisonType.Equal:
                    return fileDate.Date == MinDate.Date;

                case ComparisonType.Greater:
                    return fileDate.Date > MinDate.Date;

                case ComparisonType.Less:
                    return fileDate.Date < MinDate.Date;

                case ComparisonType.Between:
                    return fileDate.Date >= MinDate.Date && fileDate.Date <= MaxDate.Date;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 创建一个新的过滤器实例
        /// </summary>
        public FileFilter Clone()
        {
            return new FileFilter
            {
                FilterMode = this.FilterMode,
                NamePattern = this.NamePattern,
                CaseSensitive = this.CaseSensitive,
                UseRegex = this.UseRegex,
                Extensions = this.Extensions,
                ExcludeExtensions = this.ExcludeExtensions,
                SizeComparisonType = this.SizeComparisonType,
                MinSize = this.MinSize,
                MaxSize = this.MaxSize,
                DateComparisonType = this.DateComparisonType,
                DateType = this.DateType,
                MinDate = this.MinDate,
                MaxDate = this.MaxDate,
                IncludeDirectories = this.IncludeDirectories,
                IncludeHidden = this.IncludeHidden,
                IncludeSystem = this.IncludeSystem,
                IncludeReadOnly = this.IncludeReadOnly
            };
        }
    }
}