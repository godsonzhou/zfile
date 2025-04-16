using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace zfile
{
    public class FileCompareForm : Form
    {
        private TextBox txtLeftFile;
        private TextBox txtRightFile;
        private Button btnSelectLeft;
        private Button btnSelectRight;
        private ToolStrip toolStrip;
        private RichTextBox txtLeftContent;
        private RichTextBox txtRightContent;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private bool isHexMode;
        private bool isCaseSensitive;
        private bool ignoreWhitespace;
        private bool ignoreCommonLines;
        private bool isEditMode;
        private int bytesPerLine = 16;
        private List<Difference> differences;
        private int currentDifferenceIndex = -1;
        private bool isDarkMode;
        private bool isLeftModified;
        private bool isRightModified;
        private string leftFilePath;
        private string rightFilePath;
        private Font currentFont;
        private string searchText;
        private int currentSearchIndex = -1;
        private bool isScrolling = false;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel topPanel;
        private TableLayoutPanel contentPanel;
        private TableLayoutPanel bottomPanel;
        private RichTextBox leftLineNumbers;
        private RichTextBox rightLineNumbers;
        private ToolStripComboBox bytesPerLineCombo;
        private bool isHighlighting = false;

        public FileCompareForm(string leftFile, string rightFile)
        {
            InitializeComponents();
            leftFilePath = leftFile;
            rightFilePath = rightFile;
            txtLeftFile.Text = leftFile;
            txtRightFile.Text = rightFile;
            LoadFiles();
        }

        private void InitializeComponents()
        {
            Text = "文件比较";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;

            // 创建主布局
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // 创建顶部文件选择区域
            topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // 左侧文件选择区域
            var leftFilePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            leftFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            leftFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtLeftFile = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            btnSelectLeft = new Button
            {
                Text = "选择文件"
                //Dock = DockStyle.Fill
            };
            btnSelectLeft.Click += BtnSelectLeft_Click;

            leftFilePanel.Controls.Add(txtLeftFile, 0, 0);
            leftFilePanel.Controls.Add(btnSelectLeft, 1, 0);

            // 右侧文件选择区域
            var rightFilePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            rightFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            rightFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtRightFile = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            btnSelectRight = new Button
            {
                Text = "选择文件"
                //Dock = DockStyle.Fill
            };
            btnSelectRight.Click += BtnSelectRight_Click;

            rightFilePanel.Controls.Add(txtRightFile, 0, 0);
            rightFilePanel.Controls.Add(btnSelectRight, 1, 0);

            topPanel.Controls.Add(leftFilePanel, 0, 0);
            topPanel.Controls.Add(rightFilePanel, 1, 0);

            // 创建工具栏
            toolStrip = new ToolStrip();
            bytesPerLineCombo = new ToolStripComboBox("每行字节数")
            {
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "16", "24", "32" }
            };
            bytesPerLineCombo.SelectedIndex = 0;
            bytesPerLineCombo.SelectedIndexChanged += BytesPerLineCombo_SelectedIndexChanged;
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("比较", null, BtnCompare_Click),
                new ToolStripButton("下一个差异", null, BtnNextDiff_Click),
                new ToolStripButton("上一个差异", null, BtnPrevDiff_Click),
                new ToolStripButton("字体", null, BtnFont_Click),
                new ToolStripButton("16进制模式", null, BtnHexMode_Click) { CheckOnClick = true },
                bytesPerLineCombo,
                new ToolStripButton("区分大小写", null, BtnCaseSensitive_Click) { CheckOnClick = true },
                new ToolStripButton("忽略空格", null, BtnIgnoreWhitespace_Click) { CheckOnClick = true },
                new ToolStripButton("忽略常见行", null, BtnIgnoreCommonLines_Click) { CheckOnClick = true },
                new ToolStripButton("编辑模式", null, BtnEditMode_Click) { CheckOnClick = true },
                new ToolStripButton("复制到右侧", null, BtnCopyToRight_Click),
                new ToolStripButton("复制到左侧", null, BtnCopyToLeft_Click),
                new ToolStripButton("撤销编辑", null, BtnUndo_Click),
                new ToolStripButton("Unicode/ANSI", null, BtnEncoding_Click),
                new ToolStripButton("查找", null, BtnFind_Click),
                new ToolStripButton("查找下一个", null, BtnFindNext_Click)
            });

            // 创建内容区域
            contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // 创建左侧内容区域
            var leftContentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            leftContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            leftContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            leftLineNumbers = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("Consolas", 10),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            txtLeftContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                HideSelection = false
            };

            leftContentPanel.Controls.Add(leftLineNumbers, 0, 0);
            leftContentPanel.Controls.Add(txtLeftContent, 1, 0);

            // 创建右侧内容区域
            var rightContentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            rightContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            rightContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            rightLineNumbers = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("Consolas", 10),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            txtRightContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                HideSelection = false
            };

            rightContentPanel.Controls.Add(rightLineNumbers, 0, 0);
            rightContentPanel.Controls.Add(txtRightContent, 1, 0);

            contentPanel.Controls.Add(leftContentPanel, 0, 0);
            contentPanel.Controls.Add(rightContentPanel, 1, 0);

            // 创建底部按钮区域
            bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var btnSaveLeft = new Button
            {
                Text = "保存左侧",
                Dock = DockStyle.Fill
            };
            btnSaveLeft.Click += BtnSaveLeft_Click;

            var btnSaveRight = new Button
            {
                Text = "保存右侧",
                Dock = DockStyle.Fill
            };
            btnSaveRight.Click += BtnSaveRight_Click;

            bottomPanel.Controls.Add(btnSaveLeft, 0, 0);
            bottomPanel.Controls.Add(btnSaveRight, 1, 0);

            // 创建状态栏
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            statusStrip.Items.Add(lblStatus);

            // 添加所有控件到主布局
            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.Controls.Add(toolStrip, 0, 1);
            mainLayout.Controls.Add(contentPanel, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            Controls.Add(mainLayout);
            Controls.Add(statusStrip);

            // 设置默认字体
            currentFont = new Font("Consolas", 10);
            txtLeftContent.Font = currentFont;
            txtRightContent.Font = currentFont;
            leftLineNumbers.Font = currentFont;
            rightLineNumbers.Font = currentFont;

            // 注册事件
            txtLeftContent.VScroll += (s, e) => SyncScroll(txtLeftContent, txtRightContent, leftLineNumbers);
            txtRightContent.VScroll += (s, e) => SyncScroll(txtRightContent, txtLeftContent, rightLineNumbers);
            txtLeftContent.TextChanged += (s, e) => UpdateLineNumbers(leftLineNumbers, txtLeftContent);
            txtRightContent.TextChanged += (s, e) => UpdateLineNumbers(rightLineNumbers, txtRightContent);
            FormClosing += FileCompareForm_FormClosing;
        }

        private void BytesPerLineCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(bytesPerLineCombo.SelectedItem.ToString(), out int newBytesPerLine))
            {
                bytesPerLine = newBytesPerLine;
                if (isHexMode)
                {
                    LoadFiles();
                }
            }
        }

        private void UpdateLineNumbers(RichTextBox lineNumbers, RichTextBox content)
        {
            var firstVisibleLine = content.GetLineFromCharIndex(content.GetCharIndexFromPosition(new Point(0, 0)));
            var visibleLines = content.Height / content.Font.Height;
            var sb = new StringBuilder();

            for (int i = firstVisibleLine; i <= firstVisibleLine + visibleLines + 1; i++)
            {
                if (i < content.Lines.Length)
                {
                    sb.AppendLine($"{i + 1,4}");
                }
            }

            lineNumbers.Text = sb.ToString();
            lineNumbers.SelectAll();
            lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        }

        private void SyncScroll(RichTextBox source, RichTextBox target, RichTextBox sourceLineNumbers)
        {
            if (isScrolling) return;
            try
            {
                isScrolling = true;
                int firstVisibleLine = source.GetLineFromCharIndex(source.GetCharIndexFromPosition(new Point(0, 0)));
                int firstCharIndex = target.GetFirstCharIndexFromLine(firstVisibleLine);
                target.SelectionStart = firstCharIndex;
                target.ScrollToCaret();
                UpdateLineNumbers(sourceLineNumbers, source);
                UpdateLineNumbers(target == txtLeftContent ? leftLineNumbers : rightLineNumbers, target);
            }
            finally
            {
                isScrolling = false;
            }
        }

        private void HighlightDifferences()
        {
            if (isHighlighting || differences == null) return;
            try
            {
                isHighlighting = true;

                // 清除现有高亮
                txtLeftContent.SelectAll();
                txtLeftContent.SelectionBackColor = SystemColors.Window;
                txtRightContent.SelectAll();
                txtRightContent.SelectionBackColor = SystemColors.Window;

                // 高亮差异
                foreach (var diff in differences)
                {
                    // 高亮左侧
                    int leftStart = txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart);
                    int leftEnd = diff.LeftStart + 1 < txtLeftContent.Lines.Length ?
                        txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart + 1) - 1 :
                        txtLeftContent.TextLength;
                    if (leftStart >= 0 && leftEnd >= leftStart)
                    {
                        txtLeftContent.Select(leftStart, leftEnd - leftStart);
                        txtLeftContent.SelectionBackColor = Color.LightPink;
                    }

                    // 高亮右侧
                    int rightStart = txtRightContent.GetFirstCharIndexFromLine(diff.RightStart);
                    int rightEnd = diff.RightStart + 1 < txtRightContent.Lines.Length ?
                        txtRightContent.GetFirstCharIndexFromLine(diff.RightStart + 1) - 1 :
                        txtRightContent.TextLength;
                    if (rightStart >= 0 && rightEnd >= rightStart)
                    {
                        txtRightContent.Select(rightStart, rightEnd - rightStart);
                        txtRightContent.SelectionBackColor = Color.LightPink;
                    }
                }

                // 恢复原始选择
                txtLeftContent.SelectionLength = 0;
                txtRightContent.SelectionLength = 0;
            }
            finally
            {
                isHighlighting = false;
            }
        }

        private void LoadFiles()
        {
            try
            {
                if (File.Exists(leftFilePath))
                {
                    if (isHexMode)
                    {
                        var bytes = File.ReadAllBytes(leftFilePath);
                        txtLeftContent.Text = GetHexContent(bytes);
                    }
                    else
                    {
                        txtLeftContent.Text = File.ReadAllText(leftFilePath);
                    }
                }
                if (File.Exists(rightFilePath))
                {
                    if (isHexMode)
                    {
                        var bytes = File.ReadAllBytes(rightFilePath);
                        txtRightContent.Text = GetHexContent(bytes);
                    }
                    else
                    {
                        txtRightContent.Text = File.ReadAllText(rightFilePath);
                    }
                }
                CompareFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件时出错: {ex.Message}", "错误");
            }
        }

        private string GetHexContent(byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += bytesPerLine)
            {
                sb.AppendLine(GetHexLine(bytes, i));
            }
            return sb.ToString();
        }

        private string GetHexLine(byte[] bytes, int startIndex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytesPerLine && startIndex + i < bytes.Length; i++)
            {
                sb.Append($"{bytes[startIndex + i]:X2} ");
            }
            return sb.ToString().TrimEnd();
        }

        private void CompareFiles()
        {
            differences = new List<Difference>();
            if (isHexMode)
            {
                CompareHexMode();
            }
            else
            {
                CompareTextMode();
            }
            UpdateStatus();
            HighlightDifferences();
            if (differences.Count > 0)
            {
                currentDifferenceIndex = 0;
                NavigateToDifference(currentDifferenceIndex);
            }
        }

        private void CompareHexMode()
        {
            var leftBytes = File.ReadAllBytes(leftFilePath);
            var rightBytes = File.ReadAllBytes(rightFilePath);
            int maxLength = Math.Max(leftBytes.Length, rightBytes.Length);

            for (int i = 0; i < maxLength; i += bytesPerLine)
            {
                var leftLine = GetHexLine(leftBytes, i);
                var rightLine = GetHexLine(rightBytes, i);
                if (leftLine != rightLine)
                {
                    differences.Add(new Difference
                    {
                        LeftStart = i / bytesPerLine,
                        RightStart = i / bytesPerLine,
                        LeftLength = 1,
                        RightLength = 1
                    });
                }
            }
        }

        private void CompareTextMode()
        {
            var leftLines = txtLeftContent.Lines;
            var rightLines = txtRightContent.Lines;
            int maxLines = Math.Max(leftLines.Length, rightLines.Length);

            for (int i = 0; i < maxLines; i++)
            {
                var leftLine = i < leftLines.Length ? leftLines[i] : "";
                var rightLine = i < rightLines.Length ? rightLines[i] : "";

                if (!CompareLines(leftLine, rightLine))
                {
                    differences.Add(new Difference
                    {
                        LeftStart = i,
                        RightStart = i,
                        LeftLength = 1,
                        RightLength = 1
                    });
                }
            }
        }

        private bool CompareLines(string left, string right)
        {
            if (!isCaseSensitive)
            {
                left = left.ToLower();
                right = right.ToLower();
            }

            if (ignoreWhitespace)
            {
                left = string.Join(" ", left.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                right = string.Join(" ", right.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return left == right;
        }

        private void NavigateToDifference(int index)
        {
            if (index < 0 || index >= differences.Count) return;

            var diff = differences[index];
            txtLeftContent.SelectionStart = txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart);
            txtLeftContent.ScrollToCaret();
            txtRightContent.SelectionStart = txtRightContent.GetFirstCharIndexFromLine(diff.RightStart);
            txtRightContent.ScrollToCaret();
        }

        private void UpdateStatus()
        {
            lblStatus.Text = $"共发现 {differences.Count} 个差异";
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            CompareFiles();
        }

        private void BtnNextDiff_Click(object sender, EventArgs e)
        {
            if (currentDifferenceIndex < differences.Count - 1)
            {
                currentDifferenceIndex++;
                NavigateToDifference(currentDifferenceIndex);
            }
        }

        private void BtnPrevDiff_Click(object sender, EventArgs e)
        {
            if (currentDifferenceIndex > 0)
            {
                currentDifferenceIndex--;
                NavigateToDifference(currentDifferenceIndex);
            }
        }

        private void BtnFont_Click(object sender, EventArgs e)
        {
            using var fontDialog = new FontDialog { Font = currentFont };
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                currentFont = fontDialog.Font;
                txtLeftContent.Font = currentFont;
                txtRightContent.Font = currentFont;
                leftLineNumbers.Font = currentFont;
                rightLineNumbers.Font = currentFont;
            }
        }

        private void BtnHexMode_Click(object sender, EventArgs e)
        {
            isHexMode = !isHexMode;
            LoadFiles();
        }

        private void BtnCaseSensitive_Click(object sender, EventArgs e)
        {
            isCaseSensitive = !isCaseSensitive;
            CompareFiles();
        }

        private void BtnIgnoreWhitespace_Click(object sender, EventArgs e)
        {
            ignoreWhitespace = !ignoreWhitespace;
            CompareFiles();
        }

        private void BtnIgnoreCommonLines_Click(object sender, EventArgs e)
        {
            ignoreCommonLines = !ignoreCommonLines;
            CompareFiles();
        }

        private void BtnEditMode_Click(object sender, EventArgs e)
        {
            isEditMode = !isEditMode;
            txtLeftContent.ReadOnly = !isEditMode;
            txtRightContent.ReadOnly = !isEditMode;
        }

        private void BtnCopyToRight_Click(object sender, EventArgs e)
        {
            if (txtLeftContent.SelectionLength > 0)
            {
                txtRightContent.SelectedText = txtLeftContent.SelectedText;
                isRightModified = true;
            }
        }

        private void BtnCopyToLeft_Click(object sender, EventArgs e)
        {
            if (txtRightContent.SelectionLength > 0)
            {
                txtLeftContent.SelectedText = txtRightContent.SelectedText;
                isLeftModified = true;
            }
        }

        private void BtnUndo_Click(object sender, EventArgs e)
        {
            txtLeftContent.Undo();
            txtRightContent.Undo();
        }

        private void BtnEncoding_Click(object sender, EventArgs e)
        {
            // 实现编码切换逻辑
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            using var findDialog = new Form
            {
                Text = "查找",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent
            };

            var txtSearch = new TextBox
            {
                Location = new Point(10, 10),
                Width = 260
            };

            var btnFind = new Button
            {
                Text = "查找",
                Location = new Point(10, 40),
                DialogResult = DialogResult.OK
            };

            findDialog.Controls.AddRange(new Control[] { txtSearch, btnFind });

            if (findDialog.ShowDialog() == DialogResult.OK)
            {
                searchText = txtSearch.Text;
                currentSearchIndex = -1;
                BtnFindNext_Click(sender, e);
            }
        }

        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(searchText)) return;

            var searchStart = currentSearchIndex + 1;
            var leftIndex = txtLeftContent.Text.IndexOf(searchText, searchStart);
            var rightIndex = txtRightContent.Text.IndexOf(searchText, searchStart);

            if (leftIndex >= 0 || rightIndex >= 0)
            {
                currentSearchIndex = Math.Min(leftIndex >= 0 ? leftIndex : int.MaxValue,
                    rightIndex >= 0 ? rightIndex : int.MaxValue);

                txtLeftContent.SelectionStart = currentSearchIndex;
                txtLeftContent.SelectionLength = searchText.Length;
                txtRightContent.SelectionStart = currentSearchIndex;
                txtRightContent.SelectionLength = searchText.Length;
            }
        }

        private void BtnSelectLeft_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                leftFilePath = dialog.FileName;
                txtLeftFile.Text = leftFilePath;
                LoadFiles();
            }
        }

        private void BtnSelectRight_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                rightFilePath = dialog.FileName;
                txtRightFile.Text = rightFilePath;
                LoadFiles();
            }
        }

        private void BtnSaveLeft_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(leftFilePath, txtLeftContent.Text);
                isLeftModified = false;
                MessageBox.Show("左侧文件保存成功", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存左侧文件时出错: {ex.Message}", "错误");
            }
        }

        private void BtnSaveRight_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(rightFilePath, txtRightContent.Text);
                isRightModified = false;
                MessageBox.Show("右侧文件保存成功", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存右侧文件时出错: {ex.Message}", "错误");
            }
        }

        private void FileCompareForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isLeftModified || isRightModified)
            {
                var result = MessageBox.Show(
                    "文件已修改，是否保存更改？",
                    "确认",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    if (isLeftModified) BtnSaveLeft_Click(sender, e);
                    if (isRightModified) BtnSaveRight_Click(sender, e);
                }
            }
        }

        private class Difference
        {
            public int LeftStart { get; set; }
            public int RightStart { get; set; }
            public int LeftLength { get; set; }
            public int RightLength { get; set; }
        }
    }
}