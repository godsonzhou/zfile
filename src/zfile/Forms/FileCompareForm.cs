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

            // 创建顶部文件选择区域
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            txtLeftFile = new TextBox
            {
                Location = new Point(5, 5),
                Width = 400,
                ReadOnly = true
            };

            btnSelectLeft = new Button
            {
                Text = "选择文件",
                Location = new Point(410, 4),
                Width = 80
            };
            btnSelectLeft.Click += BtnSelectLeft_Click;

            txtRightFile = new TextBox
            {
                Location = new Point(500, 5),
                Width = 400,
                ReadOnly = true
            };

            btnSelectRight = new Button
            {
                Text = "选择文件",
                Location = new Point(905, 4),
                Width = 80
            };
            btnSelectRight.Click += BtnSelectRight_Click;

            topPanel.Controls.AddRange(new Control[] { txtLeftFile, btnSelectLeft, txtRightFile, btnSelectRight });

            // 创建工具栏
            toolStrip = new ToolStrip();
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("比较", null, BtnCompare_Click),
                new ToolStripButton("下一个差异", null, BtnNextDiff_Click),
                new ToolStripButton("上一个差异", null, BtnPrevDiff_Click),
                new ToolStripButton("字体", null, BtnFont_Click),
                new ToolStripButton("16进制模式", null, BtnHexMode_Click) { CheckOnClick = true },
                new ToolStripComboBox("每行字节数") { Width = 60 },
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

            // 创建主分割容器
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            // 创建内容区域
            var contentPanel = new Panel { Dock = DockStyle.Fill };
            txtLeftContent = new RichTextBox
            {
                Dock = DockStyle.Left,
                Width = 600,
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };
            txtRightContent = new RichTextBox
            {
                Dock = DockStyle.Right,
                Width = 600,
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };
            contentPanel.Controls.AddRange(new Control[] { txtLeftContent, txtRightContent });

            // 创建底部按钮区域
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };

            var btnSaveLeft = new Button
            {
                Text = "保存左侧",
                Location = new Point(5, 5),
                Width = 80
            };
            btnSaveLeft.Click += BtnSaveLeft_Click;

            var btnSaveRight = new Button
            {
                Text = "保存右侧",
                Location = new Point(95, 5),
                Width = 80
            };
            btnSaveRight.Click += BtnSaveRight_Click;

            bottomPanel.Controls.AddRange(new Control[] { btnSaveLeft, btnSaveRight });

            // 创建状态栏
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            statusStrip.Items.Add(lblStatus);

            // 添加所有控件到窗体
            Controls.AddRange(new Control[] { topPanel, toolStrip, contentPanel, bottomPanel, statusStrip });

            // 设置默认字体
            currentFont = new Font("Consolas", 10);
            txtLeftContent.Font = currentFont;
            txtRightContent.Font = currentFont;

            // 注册事件
            txtLeftContent.VScroll += (s, e) => SyncScroll(txtLeftContent, txtRightContent);
            txtRightContent.VScroll += (s, e) => SyncScroll(txtRightContent, txtLeftContent);
            FormClosing += FileCompareForm_FormClosing;
        }

        private void LoadFiles()
        {
            try
            {
                if (File.Exists(leftFilePath))
                {
                    txtLeftContent.Text = File.ReadAllText(leftFilePath);
                }
                if (File.Exists(rightFilePath))
                {
                    txtRightContent.Text = File.ReadAllText(rightFilePath);
                }
                CompareFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件时出错: {ex.Message}", "错误");
            }
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

        private string GetHexLine(byte[] bytes, int startIndex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytesPerLine && startIndex + i < bytes.Length; i++)
            {
                sb.Append($"{bytes[startIndex + i]:X2} ");
            }
            return sb.ToString().TrimEnd();
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

        private void SyncScroll(RichTextBox source, RichTextBox target)
        {
            target.ScrollToCaret();
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
            }
        }

        private void BtnHexMode_Click(object sender, EventArgs e)
        {
            isHexMode = !isHexMode;
            CompareFiles();
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