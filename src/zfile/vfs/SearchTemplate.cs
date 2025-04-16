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
            FileAttributes attributesUnsetMask = 0)
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
    }
} 