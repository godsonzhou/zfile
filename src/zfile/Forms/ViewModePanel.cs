using System.Data;
namespace zfile
{
    public class ViewModePanel : Panel
    {
        private DataGridView grid;
        private MainForm mainForm;

        // 新增：保存控件引用
        private ComboBox viewTypeCombo;
        private ComboBox sortMethodCombo;
        private TextBox additionalSortTextBox;
        private ComboBox labelColorCombo;
        private ComboBox bgColorCombo;
        private ComboBox evenRowColorCombo;
        private CheckBox priorityCheckBox;
        private TextBox autoCommandTextBox;
        // 保存初始viewModes副本
        private Dictionary<string, string> originalOptions = new();
        private bool isLoading = false;

        public ViewModePanel(MainForm mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponents();
            LoadViewModes();
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;

            // 创建DataGridView显示视图模式配置
            grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 200,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // 添加列
            grid.Columns.Add("ViewName", "视图模式名称");
            grid.Columns.Add("Description", "描述");

            // 添加按钮面板
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            Button btnAdd = new Button { Text = "添加(A)...", Width = 80 };
            Button btnDelete = new Button { Text = "删除(D)...", Width = 80 };
            Button btnChange = new Button { Text = "更改标题(C)...", Width = 100 };

            buttonPanel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnChange });

            // 添加事件处理
            btnAdd.Click += (s, e) => AddViewMode();
            btnDelete.Click += (s, e) => DeleteViewMode();
            btnChange.Click += (s, e) => ChangeViewMode();

            // 创建设置面板
            Panel settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 列视图设置
            Label viewTypeLabel = new Label { Text = "列视图(Q):", AutoSize = true, Location = new Point(10, 10) };
            viewTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 10)
            };
			//todo: get the combobox items from the mainForm.viewmgr.coldefdict
			//viewTypeCombo.Items.AddRange(new object[] { "默认", "系统", "程序", "图片", "音频", "视频", "源码", "文档" });
			var viewmodes = mainForm.viewMgr.colDefDict.Keys.ToArray();
			viewTypeCombo.Items.AddRange(viewmodes);
            viewTypeCombo.SelectedIndex = 0;

            // 排序方式设置
            Label sortMethodLabel = new Label { Text = "排序方式(S):", AutoSize = true, Location = new Point(10, 40) };
            sortMethodCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 40)
            };
            sortMethodCombo.Items.AddRange(new object[] { "不变", "文件名", "扩展名", "大小", "时间", "类型" });
            sortMethodCombo.SelectedIndex = 0;

            // 附加排序设置
            Label additionalSortLabel = new Label { Text = "附加排序(I):", AutoSize = true, Location = new Point(10, 70) };
            additionalSortTextBox = new TextBox { Width = 320, Location = new Point(220, 70) };
            Button additionalSortButton = new Button { Text = "+", Width = 30, Location = new Point(550, 70) };

            // 标签颜色设置
            Label labelColorLabel = new Label { Text = "标签颜色和图标(T):", AutoSize = true, Location = new Point(10, 100) };
            labelColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 100)
            };
            labelColorCombo.Items.AddRange(new object[] { "默认色", "红色", "绿色", "蓝色", "黄色", "紫色" });
            labelColorCombo.SelectedIndex = 0;
            Button labelColorButton1 = new Button { Text = ">>", Width = 30, Location = new Point(550, 100) };
            Button labelColorButton2 = new Button { Text = ">>", Width = 30, Location = new Point(590, 100) };

            // 背景颜色设置
            Label bgColorLabel = new Label { Text = "背景颜色(B):", AutoSize = true, Location = new Point(10, 130) };
            bgColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 130)
            };
            bgColorCombo.Items.AddRange(new object[] { "默认色", "白色", "灰色", "黑色", "浅蓝", "浅绿" });
            bgColorCombo.SelectedIndex = 0;
            Button bgColorButton = new Button { Text = ">>", Width = 30, Location = new Point(550, 130) };
            priorityCheckBox = new CheckBox { Text = "优先(P)", AutoSize = true, Location = new Point(590, 130), Checked = true };

            // 偶数行背景颜色设置
            Label evenRowColorLabel = new Label { Text = "偶数行背景颜色(2):", AutoSize = true, Location = new Point(10, 160) };
            evenRowColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 160)
            };
            evenRowColorCombo.Items.AddRange(new object[] { "默认色", "白色", "灰色", "浅蓝", "浅绿" });
            evenRowColorCombo.SelectedIndex = 0;
            Button evenRowColorButton = new Button { Text = ">>", Width = 30, Location = new Point(550, 160) };

            // 自动运行命令设置
            Label autoCommandLabel = new Label { Text = "自动运行命令:", AutoSize = true, Location = new Point(10, 190) };
            autoCommandTextBox = new TextBox { Width = 320, Location = new Point(220, 190) };
            Button autoCommandButton = new Button { Text = "-", Width = 30, Location = new Point(550, 190) };

            // 添加控件到设置面板
            settingsPanel.Controls.AddRange(new Control[] {
                viewTypeLabel, viewTypeCombo,
                sortMethodLabel, sortMethodCombo,
                additionalSortLabel, additionalSortTextBox, additionalSortButton,
                labelColorLabel, labelColorCombo, labelColorButton1, labelColorButton2,
                bgColorLabel, bgColorCombo, bgColorButton, priorityCheckBox,
                evenRowColorLabel, evenRowColorCombo, evenRowColorButton,
                autoCommandLabel, autoCommandTextBox, autoCommandButton
            });

            // 添加控件到面板
            Controls.Add(settingsPanel);
            Controls.Add(buttonPanel);
            Controls.Add(grid);

            // 新增：事件绑定
            grid.SelectionChanged += Grid_SelectionChanged;
            viewTypeCombo.SelectedIndexChanged += Controls_Changed;
            sortMethodCombo.SelectedIndexChanged += Controls_Changed;
            additionalSortTextBox.TextChanged += Controls_Changed;
            labelColorCombo.SelectedIndexChanged += Controls_Changed;
            bgColorCombo.SelectedIndexChanged += Controls_Changed;
            evenRowColorCombo.SelectedIndexChanged += Controls_Changed;
            priorityCheckBox.CheckedChanged += Controls_Changed;
            autoCommandTextBox.TextChanged += Controls_Changed;
        }

        private void LoadViewModes()
        {
            // 加载默认视图模式
            grid.Rows.Clear();
            originalOptions.Clear();
            foreach(var v in mainForm.viewMgr.viewModes.Values)
            {
                grid.Rows.Add(v.Name, v.Options);
                originalOptions[v.Name] = v.Options; // 以Name为key保存原始option
            }
            if (grid.Rows.Count > 0)
                grid.Rows[0].Selected = true;
        }

        private void AddViewMode()
        {
            // 添加新的视图模式
            string newName = $"视图模式{grid.Rows.Count + 1}";
            grid.Rows.Add(newName, "新建视图模式");
        }

        private void DeleteViewMode()
        {
            // 删除选中的视图模式
            if (grid.SelectedRows.Count > 0)
            {
                grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
            }
        }

        private void ChangeViewMode()
        {
            // 修改选中的视图模式
            if (grid.SelectedRows.Count > 0)
            {
                var row = grid.SelectedRows[0];
                string currentName = row.Cells["ViewName"].Value.ToString();
                string currentDesc = row.Cells["Description"].Value.ToString();

                // 这里可以弹出对话框进行编辑
                using var inputBox = new InputBox("修改视图模式", "视图模式名称:", currentName);
                if (inputBox.ShowDialog() == DialogResult.OK)
                {
                    row.Cells["ViewName"].Value = inputBox.InputText;
                }
            }
        }

        // 新增：表格行选中时刷新控件
        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) return;
            isLoading = true;
            var row = grid.SelectedRows[0];
            string option = row.Cells["Description"].Value?.ToString() ?? "";
            ParseOptionToControls(option);
            isLoading = false;
        }

        // 新增：控件变更时同步option
        private void Controls_Changed(object sender, EventArgs e)
        {
            if (isLoading) return;
            if (grid.SelectedRows.Count == 0) return;
            var row = grid.SelectedRows[0];
            string newOption = GenerateOptionFromControls(row.Cells["Description"].Value?.ToString() ?? "");
            row.Cells["Description"].Value = newOption;
        }

        // 解析option字符串到控件
        private void ParseOptionToControls(string option)
        {
            var parts = option.Split('|');
            // 列视图编号
            if (parts.Length > 0)
            {
                if (int.TryParse(parts[0], out int viewId))
                {
                    // 只处理自定义列视图（6及以上）
                    int idx = viewId - 6;
                    if (idx >= 0 && idx < mainForm.viewMgr.colDefDict.Count)
                        viewTypeCombo.SelectedIndex = idx;
                    else
                        viewTypeCombo.SelectedIndex = 0;
                }
                else
                {
                    viewTypeCombo.SelectedIndex = 0;
                }
            }
            // 排序方式
            if (parts.Length > 1)
            {
                int idx = 0;
                switch (parts[1])
                {
                    case "-1": idx = 0; break;
                    case "0": idx = 1; break;
                    case "1": idx = 2; break;
                    case "2": idx = 3; break;
                    case "3": idx = 4; break;
                    case "4": idx = 5; break;
                }
                sortMethodCombo.SelectedIndex = idx;
            }
            // 附加排序列号
            if (parts.Length > 3)
                additionalSortTextBox.Text = parts[3];
            // 标签颜色
            if (parts.Length > 4)
            {
                int idx = 0;
                if (int.TryParse(parts[4], out int colorVal))
                {
                    switch (colorVal)
                    {
                        case -1: idx = 0; break;
                        case 255: idx = 1; break;
                        case 65280: idx = 2; break;
                        case 16711680: idx = 3; break;
                        case 65535: idx = 4; break;
                        case 8388736: idx = 5; break;
                        default: idx = 0; break;
                    }
                }
                labelColorCombo.SelectedIndex = idx;
            }
            // 背景颜色
            if (parts.Length > 8)
            {
                int idx = 0;
                if (int.TryParse(parts[8], out int bgVal))
                {
                    switch (bgVal)
                    {
                        case -1: idx = 0; break;
                        case 0: idx = 1; break;
                        case 8421504: idx = 2; break;
                        case 16777215: idx = 3; break;
                        case 12639424: idx = 4; break;
                        case 8454016: idx = 5; break;
                        default: idx = 0; break;
                    }
                }
                bgColorCombo.SelectedIndex = idx;
            }
            // 偶数行背景色
            if (parts.Length > 8)
            {
                evenRowColorCombo.SelectedIndex = 0; // 可扩展
            }
            // 优先
            priorityCheckBox.Checked = true; // 可扩展
            // 自动命令
            if (parts.Length > 9)
                autoCommandTextBox.Text = parts[9];
            else
                autoCommandTextBox.Text = "";
        }

        // 生成option字符串
        private string GenerateOptionFromControls(string oldOption)
        {
            var parts = oldOption.Split('|');
            // 列视图编号：combobox索引+6
            int colViewIdNum = viewTypeCombo.SelectedIndex + 6;
            string colViewId = colViewIdNum.ToString();
            // 排序方式
            string sortMethod = sortMethodCombo.SelectedIndex switch
            {
                0 => "-1",
                1 => "0",
                2 => "1",
                3 => "2",
                4 => "3",
                5 => "4",
                _ => "-1"
            };
            // 升降序
            string order = parts.Length > 2 ? parts[2] : "0";
            // 附加排序列号
            string addSort = additionalSortTextBox.Text;
            // 标签颜色
            string labelColor = labelColorCombo.SelectedIndex switch
            {
                0 => "-1",
                1 => "255",
                2 => "65280",
                3 => "16711680",
                4 => "65535",
                5 => "8388736",
                _ => "-1"
            };
            // ？|？|？|？ 保留原样
            string q1 = parts.Length > 5 ? parts[5] : "-1";
            string q2 = parts.Length > 6 ? parts[6] : "-1";
            string q3 = parts.Length > 7 ? parts[7] : "-1";
            // 背景色
            string bgColor = bgColorCombo.SelectedIndex switch
            {
                0 => "-1",
                1 => "0",
                2 => "8421504",
                3 => "16777215",
                4 => "12639424",
                5 => "8454016",
                _ => "-1"
            };
            // 拼接
            return string.Join("|", new[] { colViewId, sortMethod, order, addSort, labelColor, q1, q2, q3, bgColor });
        }

        // 对外：应用更改
        public void ApplyChanges()
        {
            // 遍历表格，将option写回viewModes
            foreach (DataGridViewRow row in grid.Rows)
            {
                string name = row.Cells["ViewName"].Value?.ToString() ?? "";
                string option = row.Cells["Description"].Value?.ToString() ?? "";
                // 找到对应viewMode
                foreach (var kv in mainForm.viewMgr.viewModes)
                {
                    if (kv.Value.Name == name)
                    {
                        kv.Value.Options = option;
                        break;
                    }
                }
            }
            // 更新原始副本
            foreach (var kv in mainForm.viewMgr.viewModes)
            {
                originalOptions[kv.Value.Name] = kv.Value.Options;
            }
        }

        // 对外：恢复初始状态
        public void Reload()
        {
            // 恢复viewModes为originalOptions
            foreach (var kv in mainForm.viewMgr.viewModes)
            {
                if (originalOptions.TryGetValue(kv.Value.Name, out var opt))
                    kv.Value.Options = opt;
            }
            LoadViewModes();
        }
    }

    // 简单的输入对话框
    public class InputBox : Form
    {
        private TextBox textBox;
        public string InputText => textBox.Text;

        public InputBox(string title, string prompt, string defaultValue = "")
        {
            Text = title;
            Size = new Size(400, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            Label label = new Label
            {
                Text = prompt,
                AutoSize = true,
                Location = new Point(10, 20)
            };

            textBox = new TextBox
            {
                Text = defaultValue,
                Width = 370,
                Location = new Point(10, 50)
            };

            Button okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 80)
            };

            Button cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(300, 80)
            };

            Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}