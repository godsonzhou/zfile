using System.Text.RegularExpressions;

namespace zfile
{
    public class SearchTemplate : IDisposable
    {
        private readonly string _searchTemplate;
        private readonly bool _caseSensitive;
        private readonly bool _regExp;
        private readonly bool _wholeWords;
        private readonly bool _searchInPath;
        private readonly bool _searchInContent;
        private readonly string _contentEncoding;
        private readonly string _contentPattern;
        private readonly long _minFileSize;
        private readonly long _maxFileSize;
        private readonly DateTime _minFileDate;
        private readonly DateTime _maxFileDate;
        private readonly bool _attributesSet;
        private readonly FileAttributes _attributes;
        private readonly bool _attributesUnset;
        private readonly FileAttributes _attributesUnsetMask;
        private readonly string _excludeDirectories = string.Empty;
        internal int SearchDepth = -1;

        /// <summary>
        /// 获取或设置搜索模板的名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        public SearchTemplate(
            string searchTemplate,
            bool caseSensitive = false,
            bool regExp = false,
            bool wholeWords = false,
            bool searchInPath = true,
            bool searchInContent = false,
            string contentEncoding = null,
            string contentPattern = null,
            long minFileSize = 0,
            long maxFileSize = 0,
            DateTime minFileDate = default,
            DateTime maxFileDate = default,
            bool attributesSet = false,
            FileAttributes attributes = 0,
            bool attributesUnset = false,
            FileAttributes attributesUnsetMask = 0,
            string excludeDirectories = null)
        {
            _searchTemplate = searchTemplate;
            _caseSensitive = caseSensitive;
            _regExp = regExp;
            _wholeWords = wholeWords;
            _searchInPath = searchInPath;
            _searchInContent = searchInContent;
            _contentEncoding = contentEncoding;
            _contentPattern = contentPattern;
            _minFileSize = minFileSize;
            _maxFileSize = maxFileSize;
            _minFileDate = minFileDate;
            _maxFileDate = maxFileDate;
            _attributesSet = attributesSet;
            _attributes = attributes;
            _attributesUnset = attributesUnset;
            _attributesUnsetMask = attributesUnsetMask;
            _excludeDirectories = excludeDirectories ?? string.Empty;
        }

        public bool Check(FileEntry file)
        {
            if (file == null)
                return false;

            // Check file size
            if (_minFileSize > 0 && file.Size < _minFileSize)
                return false;
            if (_maxFileSize > 0 && file.Size > _maxFileSize)
                return false;

            // Check file date
            if (_minFileDate != default && file.ModificationTime < _minFileDate)
                return false;
            if (_maxFileDate != default && file.ModificationTime > _maxFileDate)
                return false;

            // Check attributes
            if (_attributesSet && (file.Attributes & _attributes) != _attributes)
                return false;
            if (_attributesUnset && (file.Attributes & _attributesUnsetMask) != 0)
                return false;

            // Check name/path
            if (_searchInPath)
            {
                string textToSearch = _caseSensitive ? file.Name : file.Name.ToLower();
                string searchPattern = _caseSensitive ? _searchTemplate : _searchTemplate.ToLower();

                if (_regExp)
                {
                    try
                    {
                        var regex = new Regex(searchPattern, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        if (!regex.IsMatch(textToSearch))
                            return false;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else if (_wholeWords)
                {
                    if (!textToSearch.Contains(searchPattern))
                        return false;
                }
                else
                {
                    if (!textToSearch.Contains(searchPattern))
                        return false;
                }
            }

            // Check content
            if (_searchInContent && !string.IsNullOrEmpty(_contentPattern))
            {
                try
                {
                    using (var stream = file.OpenRead())
                    using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(_contentEncoding)))
                    {
                        string content = reader.ReadToEnd();
                        if (_caseSensitive)
                        {
                            if (!content.Contains(_contentPattern))
                                return false;
                        }
                        else
                        {
                            if (!content.ToLower().Contains(_contentPattern.ToLower()))
                                return false;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }

        internal bool CheckDirectoryName(string name)
        {
            // 检查目录名是否匹配排除目录列表
            return !MatchesMaskList(name, _excludeDirectories);
        }

        internal bool CheckDirectoryNameEx(string fullPath, string rootDir)
        {
            // 检查完整路径是否匹配排除目录列表
            if (string.IsNullOrEmpty(_excludeDirectories))
                return true;

            foreach (var path in _excludeDirectories.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pathType = GetPathType(path);
                if (pathType == PathType.Relative)
                {
                    // 检查相对于根目录的路径
                    string relativePath = ExtractDirLevel(rootDir, fullPath);
                    if (MatchesMask(relativePath, path))
                        return false;
                }
                else if (pathType == PathType.Absolute)
                {
                    // 检查绝对路径
                    if (MatchesMask(fullPath, path))
                        return false;
                }
            }
            return true;
        }

        // 辅助方法：检查路径类型
        private enum PathType { Relative, Absolute }

        private PathType GetPathType(string path)
        {
            // 判断路径是相对路径还是绝对路径
            return Path.IsPathRooted(path) ? PathType.Absolute : PathType.Relative;
        }

        // 辅助方法：提取相对于根目录的路径
        private string ExtractDirLevel(string basePath, string fullPath)
        {
            // 从完整路径中提取相对于基础路径的部分
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = fullPath.Substring(basePath.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    relativePath = relativePath.Substring(1);
                return relativePath;
            }
            return fullPath;
        }

        // 辅助方法：检查是否匹配掩码列表
        private bool MatchesMaskList(string name, string maskList)
        {
            if (string.IsNullOrEmpty(maskList))
                return false;

            foreach (var mask in maskList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (MatchesMask(name, mask))
                    return true;
            }
            return false;
        }

        // 辅助方法：检查是否匹配掩码
        private bool MatchesMask(string name, string mask)
        {
            if (_regExp)
            {
                try
                {
                    var regex = new Regex(mask, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                    return regex.IsMatch(name);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // 简单的通配符匹配
                return new Regex("^" + Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".") + "$", 
                    _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase).IsMatch(name);
            }
        }
    }
}