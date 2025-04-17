using System.Text.RegularExpressions;

namespace zfile
{
    public class OutputParser
    {
        private readonly MultiArcItem _multiArcItem;
        private readonly string _archiveFileName;
        private readonly List<Regex> _regexList;

        public event Action<ArchiveItem> OnGetArchiveItem;

        public OutputParser(MultiArcItem multiArcItem, string archiveFileName)
        {
            _multiArcItem = multiArcItem;
            _archiveFileName = archiveFileName;
            _regexList = new List<Regex>();

            InitializeRegex();
        }

        private void InitializeRegex()
        {
            // 初始化正则表达式列表
            foreach (var pattern in _multiArcItem.Format)
            {
                _regexList.Add(new Regex(pattern, RegexOptions.Compiled));
            }
        }

        public void ParseOutput(string output)
        {
            try
            {
                foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ParseLine(line);
                }
            }
            catch (Exception ex)
            {
                // 处理解析异常
                throw;
            }
        }

        private void ParseLine(string line)
        {
            foreach (var regex in _regexList)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    var archiveItem = CreateArchiveItem(match);
                    OnGetArchiveItem?.Invoke(archiveItem);
                    break;
                }
            }
        }

        private ArchiveItem CreateArchiveItem(Match match)
        {
            var item = new ArchiveItem
            {
                FileName = match.Groups["FileName"].Value,
                UnpSize = long.Parse(match.Groups["Size"].Value),
                PackSize = long.Parse(match.Groups["PackedSize"].Value),
                DateTime = DateTime.Parse(match.Groups["DateTime"].Value),
                Attributes = ParseAttributes(match.Groups["Attributes"].Value)
            };

            return item;
        }

        private FileAttributes ParseAttributes(string attributes)
        {
            var result = FileAttributes.Normal;

            if (attributes.Contains("D"))
                result |= FileAttributes.Directory;
            if (attributes.Contains("H"))
                result |= FileAttributes.Hidden;
            if (attributes.Contains("S"))
                result |= FileAttributes.System;
            if (attributes.Contains("R"))
                result |= FileAttributes.ReadOnly;

            return result;
        }
    }
} 