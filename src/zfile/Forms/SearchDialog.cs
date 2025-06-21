using System.Text;

namespace zfile.Forms
{
    public class SearchDialog : Form
    {
        #region 字段和属性
        private TextBox _searchTextBox;
        public CheckBox _matchCaseCheckBox;
        public CheckBox _wholeWordCheckBox;
        public CheckBox _searchUpCheckBox;
        public CheckBox _hexSearchCheckBox;
        private Button _searchButton;
        private Button _cancelButton;
        private Button _helpButton;

        public string SearchText => _searchTextBox.Text;
        public bool MatchCase => _matchCaseCheckBox.Checked;
        public bool WholeWord => _wholeWordCheckBox.Checked;
        public bool SearchUp => _searchUpCheckBox.Checked;
        public bool HexSearch => _hexSearchCheckBox.Checked;
        #endregion

        public SearchDialog()
        {
            InitializeComponent();
        }

        public SearchDialog(string initialSearchText)
        {
            InitializeComponent();
            _searchTextBox.Text = initialSearchText;
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            Text = "查找";
            Size = new Size(400, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // 创建控件
            var searchLabel = new Label
            {
                Text = "输入查找文本:",
                Location = new Point(20, 20),
                Size = new Size(100, 20)
            };

            _searchTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(350, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _matchCaseCheckBox = new CheckBox
            {
                Text = "区分大小写",
                Location = new Point(20, 80),
                Size = new Size(150, 20)
            };

            _wholeWordCheckBox = new CheckBox
            {
                Text = "全字匹配",
                Location = new Point(20, 105),
                Size = new Size(150, 20)
            };

            _searchUpCheckBox = new CheckBox
            {
                Text = "向上查找",
                Location = new Point(200, 80),
                Size = new Size(150, 20)
            };

            _hexSearchCheckBox = new CheckBox
            {
                Text = "查找十六进制串",
                Location = new Point(200, 105),
                Size = new Size(150, 20)
            };

            _searchButton = new Button
            {
                Text = "查找",
                Location = new Point(100, 140),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };

            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(200, 140),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            _helpButton = new Button
            {
                Text = "帮助",
                Location = new Point(300, 140),
                Size = new Size(80, 30)
            };

            // 添加事件处理
            _searchButton.Click += (s, e) => DialogResult = DialogResult.OK;
            _cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;
            _helpButton.Click += HelpButton_Click;
            _hexSearchCheckBox.CheckedChanged += HexSearchCheckBox_CheckedChanged;

            // 添加控件到窗体
            Controls.AddRange(new Control[] {
                searchLabel,
                _searchTextBox,
                _matchCaseCheckBox,
                _wholeWordCheckBox,
                _searchUpCheckBox,
                _hexSearchCheckBox,
                _searchButton,
                _cancelButton,
                _helpButton
            });

            // 设置默认按钮和取消按钮
            AcceptButton = _searchButton;
            CancelButton = _cancelButton;
        }

        private void HexSearchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // 如果选中十六进制搜索，禁用全字匹配选项
            _wholeWordCheckBox.Enabled = !_hexSearchCheckBox.Checked;
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            string helpText = "搜索时用到的特殊字符：\n" +
                             "\t - 制表符\n" +
                             "\n - 换行符\n" +
                             "\\ - 单个反斜杠\n\n" +
                             "支持正则表达式！\n\n" +
                             "十六进制搜索格式示例：\n" +
                             "00 FF 12 - 纯十六进制\n" +
                             "\"PK\"0102 - 混合方式";

            MessageBox.Show(helpText, "查找帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 处理搜索文本，转换特殊字符和十六进制
        /// </summary>
        /// <returns>处理后的搜索文本</returns>
        public string GetProcessedSearchText()
        {
            string text = _searchTextBox.Text;

            if (_hexSearchCheckBox.Checked)
            {
                // 处理十六进制搜索
                return ProcessHexString(text);
            }
            else
            {
                // 处理特殊字符
                return ProcessSpecialCharacters(text);
            }
        }

        private string ProcessSpecialCharacters(string text)
        {
            // 替换特殊字符序列
            text = text.Replace("\\t", "\t");
            text = text.Replace("\\n", "\n");
            text = text.Replace("\\\\", "\\");

            return text;
        }

        private string ProcessHexString(string hexString)
        {
            // 支持混合模式，如 "PK"0102
            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < hexString.Length)
            {
                if (hexString[i] == '"')
                {
                    // 处理引号内的文本部分
                    int endQuote = hexString.IndexOf('"', i + 1);
                    if (endQuote > i)
                    {
                        string textPart = hexString.Substring(i + 1, endQuote - i - 1);
                        result.Append(textPart);
                        i = endQuote + 1;
                    }
                    else
                    {
                        // 未找到结束引号，将剩余部分作为文本
                        result.Append(hexString.Substring(i + 1));
                        break;
                    }
                }
                else if (char.IsWhiteSpace(hexString[i]))
                {
                    // 跳过空白字符
                    i++;
                }
                else if (IsHexChar(hexString[i]))
                {
                    // 尝试读取两个十六进制字符
                    if (i + 1 < hexString.Length && IsHexChar(hexString[i + 1]))
                    {
                        string hexPair = hexString.Substring(i, 2);
                        try
                        {
                            byte b = Convert.ToByte(hexPair, 16);
                            result.Append((char)b);
                            i += 2;
                        }
                        catch
                        {
                            // 如果转换失败，跳过这个字符
                            i++;
                        }
                    }
                    else
                    {
                        // 只有一个十六进制字符，跳过
                        i++;
                    }
                }
                else
                {
                    // 其他字符，直接添加
                    result.Append(hexString[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        private bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        /// <summary>
        /// 获取搜索参数标志
        /// </summary>
        /// <returns>搜索参数标志</returns>
        public int GetSearchParameters()
        {
            int parameters = 1; // lcs_findfirst 默认从当前位置开始查找

            if (MatchCase)
                parameters |= 2; // lcs_matchcase

            if (WholeWord)
                parameters |= 4; // lcs_wholewords

            if (SearchUp)
                parameters |= 8; // lcs_backwards

            return parameters;
        }
    }
}