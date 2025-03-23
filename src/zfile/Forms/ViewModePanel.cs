using System.Data;
namespace zfile
{
    public class ViewModePanel : Panel
    {
        private DataGridView grid;
        private Form1 mainForm;

        public ViewModePanel(Form1 mainForm)
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
            ComboBox viewTypeCombo = new ComboBox
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
            ComboBox sortMethodCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 40)
            };
            sortMethodCombo.Items.AddRange(new object[] { "不变" });
            sortMethodCombo.SelectedIndex = 0;

            // 附加排序设置
            Label additionalSortLabel = new Label { Text = "附加排序(I):", AutoSize = true, Location = new Point(10, 70) };
            TextBox additionalSortTextBox = new TextBox { Width = 320, Location = new Point(220, 70) };
            Button additionalSortButton = new Button { Text = "+", Width = 30, Location = new Point(550, 70) };

            // 标签颜色设置
            Label labelColorLabel = new Label { Text = "标签颜色和图标(T):", AutoSize = true, Location = new Point(10, 100) };
            ComboBox labelColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 100)
            };
            labelColorCombo.Items.AddRange(new object[] { "默认色" });
            labelColorCombo.SelectedIndex = 0;
            Button labelColorButton1 = new Button { Text = ">>", Width = 30, Location = new Point(550, 100) };
            Button labelColorButton2 = new Button { Text = ">>", Width = 30, Location = new Point(590, 100) };

            // 背景颜色设置
            Label bgColorLabel = new Label { Text = "背景颜色(B):", AutoSize = true, Location = new Point(10, 130) };
            ComboBox bgColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 130)
            };
            bgColorCombo.Items.AddRange(new object[] { "默认色" });
            bgColorCombo.SelectedIndex = 0;
            Button bgColorButton = new Button { Text = ">>", Width = 30, Location = new Point(550, 130) };
            CheckBox priorityCheckBox = new CheckBox { Text = "优先(P)", AutoSize = true, Location = new Point(590, 130), Checked = true };

            // 偶数行背景颜色设置
            Label evenRowColorLabel = new Label { Text = "偶数行背景颜色(2):", AutoSize = true, Location = new Point(10, 160) };
            ComboBox evenRowColorCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Location = new Point(220, 160)
            };
            evenRowColorCombo.Items.AddRange(new object[] { "默认色" });
            evenRowColorCombo.SelectedIndex = 0;
            Button evenRowColorButton = new Button { Text = ">>", Width = 30, Location = new Point(550, 160) };

            // 自动运行命令设置
            Label autoCommandLabel = new Label { Text = "自动运行命令:", AutoSize = true, Location = new Point(10, 190) };
            TextBox autoCommandTextBox = new TextBox { Width = 320, Location = new Point(220, 190) };
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
        }

        private void LoadViewModes()
        {
            // 加载默认视图模式
            //grid.Rows.Add("默认", "默认视图模式");
            //grid.Rows.Add("系统", "系统文件视图模式");
            //grid.Rows.Add("程序", "程序文件视图模式");
            //grid.Rows.Add("图片", "图片文件视图模式");
            //grid.Rows.Add("音频", "音频文件视图模式");
            //grid.Rows.Add("视频", "视频文件视图模式");
            //grid.Rows.Add("源码", "源代码文件视图模式");
            //grid.Rows.Add("文档", "文档文件视图模式");
			foreach(var v in mainForm.viewMgr.viewModes.Values)
			{
				grid.Rows.Add(v.Name, v.Options);
			}
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