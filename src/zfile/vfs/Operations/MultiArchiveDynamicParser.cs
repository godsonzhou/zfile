namespace zfile
{
    public class MultiArchiveDynamicParser : MultiArchiveParser
    {
        private readonly List<string> _lines;
        private readonly List<string> _masks;
        private readonly List<string> _unparsedLines;
        private string _blockSize;
        private string _packBlockSize;
        private string _fileName;
        private string _fileExt;
        private string _day;
        private string _month;
        private string _threeMonth;
        private string _year;
        private string _hours;
        private string _hoursModif;
        private string _minutes;
        private string _seconds;
        private string _size;
        private string _packSize;
        private string _attributes;

        public MultiArchiveDynamicParser(MultiArcItem multiArcItem)
            : base(multiArcItem)
        {
            _lines = new List<string>();
            _masks = multiArcItem.Format;
            _unparsedLines = new List<string>();
        }

        public override void Prepare()
        {
            _lines.Clear();
            _unparsedLines.Clear();
        }

        private void ClearStore()
        {
            _blockSize = string.Empty;
            _packBlockSize = string.Empty;
            _fileName = string.Empty;
            _fileExt = string.Empty;
            _day = string.Empty;
            _month = string.Empty;
            _threeMonth = string.Empty;
            _year = string.Empty;
            _hours = string.Empty;
            _hoursModif = string.Empty;
            _minutes = string.Empty;
            _seconds = string.Empty;
            _size = string.Empty;
            _packSize = string.Empty;
            _attributes = string.Empty;
        }

        private int ParseByMask(string value, string nextValue, string mask)
        {
            ClearStore();
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(mask))
                return 0;

            int iValue = 0;
            int iMask = 0;
            int result = 1;
            char lastMaskC = ' ';

            while (iMask < mask.Length)
            {
                if (mask[iMask] != '+' && iValue >= value.Length)
                {
                    return 0;
                }

                char maskC = mask[iMask];
                if (iValue >= value.Length)
                    return result;

                char c = value[iValue];

                switch (maskC)
                {
                    case 'n':
                        _fileName += c;
                        break;
                    case 'e':
                        _fileExt += c;
                        break;
                    case 'd':
                        _day += c;
                        break;
                    case 't':
                        _month += c;
                        break;
                    case 'T':
                        _threeMonth += c;
                        break;
                    case 'y':
                        _year += c;
                        break;
                    case 'h':
                        _hours += c;
                        break;
                    case 'H':
                        _hoursModif += c;
                        break;
                    case 'm':
                        _minutes += c;
                        break;
                    case 's':
                        _seconds += c;
                        break;
                    case 'z':
                        _size += c;
                        break;
                    case 'p':
                        _packSize += c;
                        break;
                    case 'a':
                        _attributes += c;
                        break;
                    case 'x':
                        if (c != ' ')
                            return 0;
                        break;
                    case '+':
                        string s = string.Empty;
                        if (lastMaskC == 'n' || lastMaskC == 'e')
                        {
                            if (iMask == mask.Length - 1)
                                s = value.Substring(iValue);
                            else
                            {
                                while (iValue < value.Length)
                                {
                                    if (value[iValue] == ' ')
                                        break;
                                    s += value[iValue];
                                    iValue++;
                                }
                            }
                            if (lastMaskC == 'n')
                                _fileName += s;
                            else
                                _fileExt += s;
                        }
                        else
                        {
                            while (iValue < value.Length)
                            {
                                if (!char.IsDigit(value[iValue]))
                                    break;
                                s += value[iValue];
                                iValue++;
                            }
                            if (lastMaskC == 'z')
                                _size += s;
                            else if (lastMaskC == 'p')
                                _packSize += s;
                        }
                        iValue--;
                        break;
                    case '*':
                        while (iValue < value.Length)
                        {
                            if (value[iValue] == ' ')
                                break;
                            iValue++;
                        }
                        while (iValue < value.Length)
                        {
                            if (value[iValue] != ' ')
                                break;
                            iValue++;
                        }
                        iValue--;
                        break;
                    case '$':
                        while (iValue < value.Length)
                        {
                            if (value[iValue] != ' ' && value[iValue] != '\t')
                                break;
                            iValue++;
                        }
                        iValue--;
                        break;
                    case '=':
                        if (lastMaskC == 'z' || lastMaskC == 'p')
                        {
                            s = string.Empty;
                            while (iMask < mask.Length)
                            {
                                if (!char.IsDigit(mask[iMask]))
                                    break;
                                s += mask[iMask];
                                iMask++;
                            }
                            iMask--;
                            if (lastMaskC == 'z')
                                _blockSize = s;
                            else
                                _packBlockSize = s;
                        }
                        break;
                    case '\\':
                        value = nextValue;
                        iValue = -1;
                        result = 2;
                        break;
                    case '?':
                        break;
                }
                iValue++;
                iMask++;
                lastMaskC = maskC;
            }
            return result;
        }

        private bool CheckValues()
        {
            if (string.IsNullOrEmpty(_fileName))
                return false;

            if (!string.IsNullOrEmpty(_day))
            {
                _day = _day.Trim();
                if (!int.TryParse(_day, out int x) || x < 1 || x > 31)
                    return false;
            }

            if (!string.IsNullOrEmpty(_month))
            {
                _month = _month.Trim();
                if (!int.TryParse(_month, out int x) || x < 1 || x > 12)
                    return false;
            }

            if (!string.IsNullOrEmpty(_hours))
            {
                _hours = _hours.Trim();
                if (!int.TryParse(_hours, out int x) || x < 0 || x > 24)
                    return false;
            }

            if (!string.IsNullOrEmpty(_hoursModif))
            {
                if (!"aApP".Contains(_hoursModif[0]))
                    return false;
            }

            if (!string.IsNullOrEmpty(_minutes))
            {
                _minutes = _minutes.Trim();
                if (!int.TryParse(_minutes, out int x) || x < 0 || x > 59)
                    return false;
            }

            if (!string.IsNullOrEmpty(_seconds))
            {
                _seconds = _seconds.Trim();
                if (!int.TryParse(_seconds, out int x) || x < 0 || x > 59)
                    return false;
            }

            if (!string.IsNullOrEmpty(_size))
            {
                _size = _size.Trim();
                foreach (char c in _size)
                {
                    if (!char.IsDigit(c))
                        return false;
                }
            }

            if (!string.IsNullOrEmpty(_threeMonth))
            {
                int month = MonthToNumber(_threeMonth);
                if (month == 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(_year))
            {
                _year = _year.Trim();
                if (!int.TryParse(_year, out int x))
                    return false;

                switch (_year.Length)
                {
                    case 4:
                        if (x <= 1900 || x >= 2100)
                            return false;
                        break;
                    case 2:
                        if (x < 0 || x > 99)
                            return false;
                        break;
                    case 3:
                        if (x < 100 || x > 110)
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private void FillRecord(ArchiveItem value)
        {
            value.FileName = Path.GetFileName(_fileName);
            value.FileExt = Path.GetFileName(_fileExt);
            value.PackSize = long.TryParse(_packSize, out long packSize) ? packSize : -1;
            value.UnpSize = long.TryParse(_size, out long size) ? size : -1;
            value.Year = YearShortToLong(int.TryParse(_year, out int year) ? year : 0);
            value.Month = int.TryParse(_month, out int month) ? month : 1;
            value.Day = int.TryParse(_day, out int day) ? day : 1;
            value.Hour = int.TryParse(_hours, out int hour) ? hour : 0;
            value.Minute = int.TryParse(_minutes, out int minute) ? minute : 0;
            value.Second = int.TryParse(_seconds, out int second) ? second : 0;
            value.Attributes = GetFileAttr(_attributes);

            if (!string.IsNullOrEmpty(_threeMonth))
            {
                value.Month = MonthToNumber(_threeMonth);
            }

            if (!string.IsNullOrEmpty(_hoursModif))
            {
                value.Hour = TwelveToTwentyFour(value.Hour, _hoursModif);
            }

            if (!string.IsNullOrEmpty(_blockSize))
            {
                if (int.TryParse(_blockSize, out int blockSize))
                {
                    value.UnpSize *= blockSize;
                }
            }

            if (!string.IsNullOrEmpty(_packBlockSize))
            {
                if (int.TryParse(_packBlockSize, out int packBlockSize))
                {
                    value.PackSize *= packBlockSize;
                }
            }
        }

        public override void ParseLines()
        {
            int n = 0;
            while (n < _lines.Count)
            {
                string s = n == _lines.Count - 1 ? string.Empty : _lines[n + 1];
                bool b = false;
                int x = 0;

                foreach (string mask in _masks)
                {
                    x = ParseByMask(_lines[n], s, mask);
                    if (x > 0 && CheckValues())
                    {
                        if (OnGetArchiveItem != null)
                        {
                            var archiveItem = new ArchiveItem();
                            FillRecord(archiveItem);
                            UpdateFileName();
                            OnGetArchiveItem(archiveItem);
                        }
                        b = true;
                        break;
                    }
                }

                if (!b)
                {
                    _unparsedLines.Add(_lines[n]);
                }

                n++;
                if (x > 1)
                {
                    n += x - 1;
                }
            }
        }

        public override void AddLine(string str)
        {
            _lines.Add(str);
        }

        public static bool NeedDynamic(List<string> format)
        {
            foreach (string line in format)
            {
                if (line.Contains("+") && line.IndexOf('+') < line.Length - 1)
                    return true;
                if (ContainsOneOf(line, "$x=\\"))
                    return true;
            }
            return false;
        }

        private static bool ContainsOneOf(string str, string chars)
        {
            foreach (char c in chars)
            {
                if (str.Contains(c))
                    return true;
            }
            return false;
        }

        private static int MonthToNumber(string month)
        {
            // 实现月份名称到数字的转换
            return 0;
        }

        private static int YearShortToLong(int year)
        {
            // 实现短年份到长年份的转换
            return year;
        }

        private static int TwelveToTwentyFour(int hour, string modifier)
        {
            // 实现12小时制到24小时制的转换
            return hour;
        }
    }
} 