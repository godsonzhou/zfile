using System.Text.RegularExpressions;

namespace Zfile.Forms
{
    public partial class MultiRenameForm : Form
    {
        private readonly ListView sourceListView;
        private readonly string currentDirectory;
        private readonly Dictionary<string, string> renameMap = new();
        private readonly ListBox previewList;
        private readonly TextBox nameTemplateBox;
        private readonly TextBox extensionTemplateBox;
        private readonly TextBox findBox;
        private readonly TextBox replaceBox;
        private readonly NumericUpDown counterStartBox;
        private readonly NumericUpDown counterStepBox;
        private readonly NumericUpDown counterDigitsBox;
        private readonly CheckBox caseSensitiveBox;
        private readonly CheckBox regexBox;
        private readonly ComboBox nameStyleBox;
        private readonly ComboBox extensionStyleBox;
        private readonly Button okButton;
        private readonly Button cancelButton;
        private readonly Button insertNameButton;
        private readonly Button insertExtButton;

        public MultiRenameForm(ListView sourceList, string currentDir)
        {
            sourceListView = sourceList;
            currentDirectory = currentDir;

            // 初始化窗体
            Text = "批量重命名";
            Size = new Size(1200, 768);
            StartPosition = FormStartPosition.CenterParent;

            // 创建表格布局
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            // 设置列宽度比例
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 600));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 创建左侧预览面板
            previewList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                HorizontalScrollbar = true
            };

            // 创建一个容器Panel来包含previewList
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Width = 600,
                MinimumSize = new Size(600, 0)
            };
            leftPanel.Controls.Add(previewList);

            // 创建右侧控制面板
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 添加面板到表格布局
            tableLayout.Controls.Add(leftPanel, 0, 0);
            tableLayout.Controls.Add(rightPanel, 1, 0);
            Controls.Add(tableLayout);

            // 创建控件
            var templateGroup = new GroupBox
            {
                Text = "名称模板",
                Dock = DockStyle.Top,
                Height = 120
            };

            nameTemplateBox = new TextBox { Width = 300 };
            extensionTemplateBox = new TextBox { Width = 300 };
            insertNameButton = new Button { Text = "插入", Width = 60 };
            insertExtButton = new Button { Text = "插入", Width = 60 };

            templateGroup.Controls.AddRange(new Control[] {
                new Label { Text = "文件名:", Location = new Point(10, 20) },
                nameTemplateBox,
                insertNameButton,
                new Label { Text = "扩展名:", Location = new Point(10, 50) },
                extensionTemplateBox,
                insertExtButton
				//new Label { Text = "注: [N][C][E][Y][M][D]"}
            });

            nameTemplateBox.Location = new Point(170, 20);
            insertNameButton.Location = new Point(480, 20);
            extensionTemplateBox.Location = new Point(170, 50);
            insertExtButton.Location = new Point(480, 50);

            var searchGroup = new GroupBox
            {
                Text = "查找替换",
                Dock = DockStyle.Top,
                Height = 120,
                Top = 130
            };

            findBox = new TextBox { Width = 300 };
            replaceBox = new TextBox { Width = 300 };
            caseSensitiveBox = new CheckBox { Text = "区分大小写" };
            regexBox = new CheckBox { Text = "使用正则表达式" };

            searchGroup.Controls.AddRange(new Control[] {
                new Label { Text = "查找:", Location = new Point(10, 20) },
                findBox,
                new Label { Text = "替换为:", Location = new Point(10, 50) },
                replaceBox,
                caseSensitiveBox,
                regexBox
            });

            findBox.Location = new Point(170, 20);
            replaceBox.Location = new Point(170, 50);
            caseSensitiveBox.Location = new Point(170, 80);
            regexBox.Location = new Point(300, 80);

            var counterGroup = new GroupBox
            {
                Text = "计数器",
                Dock = DockStyle.Top,
                Height = 120,
                Top = 260
            };

            counterStartBox = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 999999 };
            counterStepBox = new NumericUpDown { Width = 80, Minimum = 1, Maximum = 100 };
            counterDigitsBox = new NumericUpDown { Width = 80, Minimum = 1, Maximum = 10 };

            counterGroup.Controls.AddRange(new Control[] {
                new Label { Text = "起始值:", Location = new Point(10, 20) },
                counterStartBox,
                new Label { Text = "步长:", Location = new Point(10, 50) },
                counterStepBox,
                new Label { Text = "位数:", Location = new Point(10, 80) },
                counterDigitsBox
            });

            counterStartBox.Location = new Point(170, 20);
            counterStepBox.Location = new Point(170, 50);
            counterDigitsBox.Location = new Point(170, 80);

            var styleGroup = new GroupBox
            {
                Text = "样式",
                Dock = DockStyle.Top,
                Height = 100,
                Top = 390
            };

            nameStyleBox = new ComboBox { Width = 150 };
            extensionStyleBox = new ComboBox { Width = 150 };

            var styles = new[] { "不改变", "全部大写", "全部小写", "首字母大写", "每个单词首字母大写" };
            nameStyleBox.Items.AddRange(styles);
            extensionStyleBox.Items.AddRange(styles);
            nameStyleBox.SelectedIndex = 0;
            extensionStyleBox.SelectedIndex = 0;

            styleGroup.Controls.AddRange(new Control[] {
                new Label { Text = "文件名:", Location = new Point(10, 20) },
                nameStyleBox,
                new Label { Text = "扩展名:", Location = new Point(10, 50) },
                extensionStyleBox
            });

            nameStyleBox.Location = new Point(170, 20);
            extensionStyleBox.Location = new Point(170, 50);

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            okButton = new Button
            {
                Text = "重命名",
                DialogResult = DialogResult.OK,
                Width = 80
            };

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Width = 80
            };

            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton });
            okButton.Location = new Point(buttonPanel.Width - 170, 10);
            cancelButton.Location = new Point(buttonPanel.Width - 80, 10);

            rightPanel.Controls.AddRange(new Control[] {
                templateGroup,
                searchGroup,
                counterGroup,
                styleGroup,
                buttonPanel
            });

            // 绑定事件
            nameTemplateBox.TextChanged += UpdatePreview;
            extensionTemplateBox.TextChanged += UpdatePreview;
            findBox.TextChanged += UpdatePreview;
            replaceBox.TextChanged += UpdatePreview;
            counterStartBox.ValueChanged += UpdatePreview;
            counterStepBox.ValueChanged += UpdatePreview;
            counterDigitsBox.ValueChanged += UpdatePreview;
            caseSensitiveBox.CheckedChanged += UpdatePreview;
            regexBox.CheckedChanged += UpdatePreview;
            nameStyleBox.SelectedIndexChanged += UpdatePreview;
            extensionStyleBox.SelectedIndexChanged += UpdatePreview;
            insertNameButton.Click += InsertNameTemplate;
            insertExtButton.Click += InsertExtTemplate;
            okButton.Click += OkButton_Click;

            // 初始化预览
            LoadFiles();
            UpdatePreview(this, EventArgs.Empty);
        }

        private void LoadFiles()
        {
            foreach (ListViewItem item in sourceListView.SelectedItems)
            {
                string oldName = item.Text;
                previewList.Items.Add($"{oldName} -> {oldName}");
                renameMap[oldName] = oldName;
            }
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            previewList.Items.Clear();
            int counter = (int)counterStartBox.Value;
            int step = (int)counterStepBox.Value;
            int digits = (int)counterDigitsBox.Value;

            foreach (ListViewItem item in sourceListView.SelectedItems)
            {
                string oldName = item.Text;
                string newName = GenerateNewName(oldName, counter);
                counter += step;

                previewList.Items.Add($"{oldName} -> {newName}");
                renameMap[oldName] = newName;
            }
        }

        private string GenerateNewName(string oldName, int counter)
        {
            string fileName = Path.GetFileNameWithoutExtension(oldName);
            string extension = Path.GetExtension(oldName);

            // 处理文件名模板
            string newName = nameTemplateBox.Text;
            if (string.IsNullOrEmpty(newName))
                newName = fileName;
            else
            {
                newName = newName.Replace("[N]", fileName)
                               .Replace("[C]", counter.ToString($"D{counterDigitsBox.Value}"))
                               .Replace("[E]", extension.TrimStart('.'))
                               .Replace("[Y]", DateTime.Now.Year.ToString())
                               .Replace("[M]", DateTime.Now.Month.ToString("D2"))
                               .Replace("[D]", DateTime.Now.Day.ToString("D2"));
            }

            // 处理扩展名模板
            string newExt = extensionTemplateBox.Text;
            if (string.IsNullOrEmpty(newExt))
                newExt = extension;
            else
            {
                newExt = newExt.Replace("[E]", extension.TrimStart('.'));
                if (!newExt.StartsWith("."))
                    newExt = "." + newExt;
            }

            // 查找替换
            if (!string.IsNullOrEmpty(findBox.Text))
            {
                if (regexBox.Checked)
                {
                    try
                    {
                        var options = caseSensitiveBox.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
                        newName = Regex.Replace(newName, findBox.Text, replaceBox.Text, options);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"正则表达式错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    var comparison = caseSensitiveBox.Checked ?
                        StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    newName = newName.Replace(findBox.Text, replaceBox.Text, comparison);
                }
            }

            // 应用样式
            newName = ApplyStyle(newName, nameStyleBox.SelectedIndex);
            newExt = ApplyStyle(newExt, extensionStyleBox.SelectedIndex);

            return newName + newExt;
        }

        private string ApplyStyle(string text, int styleIndex)
        {
            return styleIndex switch
            {
                1 => text.ToUpper(),
                2 => text.ToLower(),
                3 => char.ToUpper(text[0]) + text.Substring(1).ToLower(),
                4 => string.Join(" ", text.Split(' ').Select(w =>
                    w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : w)),
                _ => text
            };
        }

        private void InsertNameTemplate(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            var items = new[]
            {
                new { Text = "原文件名 [N]", Tag = "[N]" },
                new { Text = "计数器 [C]", Tag = "[C]" },
                new { Text = "扩展名 [E]", Tag = "[E]" },
                new { Text = "年 [Y]", Tag = "[Y]" },
                new { Text = "月 [M]", Tag = "[M]" },
                new { Text = "日 [D]", Tag = "[D]" }
            };

            foreach (var item in items)
            {
                menu.Items.Add(item.Text).Click += (s, e) =>
                {
                    nameTemplateBox.SelectedText = item.Tag.ToString();
                    nameTemplateBox.Focus();
                };
            }

            menu.Show(insertNameButton, new Point(0, insertNameButton.Height));
        }

        private void InsertExtTemplate(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("原扩展名 [E]").Click += (s, e) =>
            {
                extensionTemplateBox.SelectedText = "[E]";
                extensionTemplateBox.Focus();
            };

            menu.Show(insertExtButton, new Point(0, insertExtButton.Height));
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var duplicates = renameMap.Values.GroupBy(x => x)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key);

            if (duplicates.Any())
            {
                MessageBox.Show("存在重复的文件名:\n" + string.Join("\n", duplicates),
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                foreach (var kvp in renameMap)
                {
                    if (kvp.Key != kvp.Value)
                    {
                        string oldPath = Path.Combine(currentDirectory, kvp.Key);
                        string newPath = Path.Combine(currentDirectory, kvp.Value);

                        if (File.Exists(oldPath))
                            File.Move(oldPath, newPath);
                        else if (Directory.Exists(oldPath))
                            Directory.Move(oldPath, newPath);
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}