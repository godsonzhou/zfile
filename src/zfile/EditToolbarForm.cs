using System.Drawing;
using System.Windows.Forms;

namespace zfile
{
    public class EditToolbarForm : Form
    {
        private ToolbarManager toolbarManager;
        private FlowLayoutPanel toolbarPanel;
        private Button btnAdd;
        private Button btnDelete;
        private TextBox cmdTextBox;
        private TextBox paramTextBox;
        private TextBox pathTextBox;
        private TextBox iconFileTextBox;
        private PictureBox iconPreview;
        private TextBox tooltipTextBox;
        private Button btnOK;
        private Button btnCancel;
        private ToolbarButton? currentButton;
		private MenuInfo? currentMenuInfo;
		private string _target;
        public bool IsModified { get; private set; }

        public EditToolbarForm(ToolbarManager manager, string target)
        {
			_target = Helper.GetPathByEnv(target);
            toolbarManager = manager;
            InitializeComponents();
            LoadToolbarButtons(_target);
        }

        private void InitializeComponents()
        {
            Text = "编辑工具栏";
            Size = new Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // 工具栏面板
            var toolbarLabel = new Label
            {
                Text = "工具栏(&B):",
                Location = new Point(10, 10),
                AutoSize = true
            };
            Controls.Add(toolbarLabel);

            toolbarPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 35),
                Size = new Size(760, 100),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            Controls.Add(toolbarPanel);

            // 按钮面板
            btnAdd = new Button
            {
                Text = "添加(&A)",
                Location = new Point(10, 145),
                Width = 80
            };
            btnAdd.Click += BtnAdd_Click;
            Controls.Add(btnAdd);

            btnDelete = new Button
            {
                Text = "删除(&D)",
                Location = new Point(100, 145),
                Width = 80,
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;
            Controls.Add(btnDelete);

            // 属性面板
            var propertiesGroup = new GroupBox
            {
                Text = "按钮属性",
                Location = new Point(10, 180),
                Size = new Size(760, 320)
            };

            // 命令
            var cmdLabel = new Label { Text = "命令(&C):", Location = new Point(10, 25), AutoSize = true };
            cmdTextBox = new TextBox
            {
                Location = new Point(120, 22),
                Width = 300
                //DropDownStyle = ComboBoxStyle.DropDownList
            };
			var cmdSelButton = new Button() { Text = "..." , Location = new Point(450, 22)};
			cmdSelButton.Click += CmdSelButton_Click;
            //cmdTextBox.SelectedIndexChanged += PropertyChanged;
            propertiesGroup.Controls.AddRange(new Control[] { cmdLabel, cmdTextBox, cmdSelButton });

            // 参数
            var paramLabel = new Label { Text = "参数(&P):", Location = new Point(10, 60), AutoSize = true };
            paramTextBox = new TextBox { Location = new Point(120, 57), Width = 600 };
            paramTextBox.TextChanged += PropertyChanged;
            propertiesGroup.Controls.AddRange(new Control[] { paramLabel, paramTextBox });

            // 启动路径
            var pathLabel = new Label { Text = "启动路径(&S):", Location = new Point(10, 95), AutoSize = true };
            pathTextBox = new TextBox { Location = new Point(120, 92), Width = 600 };
            pathTextBox.TextChanged += PropertyChanged;
            propertiesGroup.Controls.AddRange(new Control[] { pathLabel, pathTextBox });

            // 图标文件
            var iconFileLabel = new Label { Text = "图标文件(&F):", Location = new Point(10, 130), AutoSize = true };
            iconFileTextBox = new TextBox { Location = new Point(120, 127), Width = 560 };
            var browseIconBtn = new Button { Text = "...", Location = new Point(690, 126), Width = 30 };
            iconFileTextBox.TextChanged += PropertyChanged;
            browseIconBtn.Click += BrowseIcon_Click;
            propertiesGroup.Controls.AddRange(new Control[] { iconFileLabel, iconFileTextBox, browseIconBtn });

            // 图标预览
            var iconLabel = new Label { Text = "图标:", Location = new Point(10, 165), AutoSize = true };
            iconPreview = new PictureBox
            {
                Location = new Point(120, 162),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.FixedSingle
            };
            propertiesGroup.Controls.AddRange(new Control[] { iconLabel, iconPreview });

            // 提示
            var tooltipLabel = new Label { Text = "提示(&T):", Location = new Point(10, 210), AutoSize = true };
            tooltipTextBox = new TextBox { Location = new Point(120, 207), Width = 600 };
            tooltipTextBox.TextChanged += PropertyChanged;
            propertiesGroup.Controls.AddRange(new Control[] { tooltipLabel, tooltipTextBox });

            Controls.Add(propertiesGroup);

            // 底部按钮
            btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(600, 520),
                Width = 80
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(690, 520),
                Width = 80
            };
			btnCancel.Click += BtnCancel_Click;

            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            // 加载命令列表
            LoadCommands();
        }

		private void CmdSelButton_Click(object? sender, EventArgs e)
		{
			//throw new NotImplementedException();
			var selcmdform = new CommandBrowserForm(toolbarManager.form.cmdProcessor, true);
			if (selcmdform.ShowDialog() == DialogResult.OK) 
			{ 
				cmdTextBox.Text = selcmdform.CmdRet;
			}
		}

		private void LoadCommands()
        {
            // 从toolbarManager.form.cmdProcessor.cmdTable获取命令列表
            var cmdTable = toolbarManager.form.cmdProcessor.cmdTable;
            foreach (var cmd in cmdTable.GetAll())
            {
                //cmdTextBox.Items.Add(cmd.CmdName);
            }
        }

        private void LoadToolbarButtons(string target)
        {
            toolbarPanel.Controls.Clear();
			if(target.Equals("default"))
				foreach (var button in toolbarManager.toolbarButtons)
					AddToolbarButtonToPanel(button);
			else
			{
				foreach (var button in toolbarManager.toolbarsDict[target])
					AddToolbarButtonToPanel(button);
			}
        }

        private void AddToolbarButtonToPanel(ToolbarButton button)
        {
            var btn = new Button
            {
                Image = toolbarManager.form.iconManager.LoadIcon(button.icon),
                ImageAlign = ContentAlignment.MiddleCenter,
                Size = new Size(32, 32),
                Tag = button,
                Margin = new Padding(2)
            };
            btn.Click += ToolbarButton_Click;
            toolbarPanel.Controls.Add(btn);
        }
		private void AddToolbarButtonToPanel(MenuInfo button)
		{
			var btn = new Button
			{
				Image = toolbarManager.form.iconManager.LoadIcon(button.Button),
				ImageAlign = ContentAlignment.MiddleCenter,
				Size = new Size(32, 32),
				Tag = button,
				Margin = new Padding(2)
			};
			btn.Click += ToolbarButton_Click;
			toolbarPanel.Controls.Add(btn);
		}
		private void ToolbarButton_Click(object? sender, EventArgs e)
        {
			if (sender is Button btn)
			{
				if (btn.Tag is ToolbarButton button)
				{
					// 更新当前选中的按钮
					currentButton = button;
					btnDelete.Enabled = true;

					// 更新属性显示
					cmdTextBox.Text = button.cmd;
					paramTextBox.Text = button.param;
					pathTextBox.Text = button.path;
					iconFileTextBox.Text = button.icon;
					tooltipTextBox.Text = button.name;

					// 更新图标预览
					iconPreview.Image = toolbarManager.form.iconManager.LoadIcon(button.icon);
				}
				else if (btn.Tag is MenuInfo x)
				{
					// 更新当前选中的按钮
					currentMenuInfo = x;
					btnDelete.Enabled = true;

					// 更新属性显示
					cmdTextBox.Text = x.Cmd;
					paramTextBox.Text = x.Param;
					pathTextBox.Text = x.Path;
					iconFileTextBox.Text = x.Button;
					tooltipTextBox.Text = x.Menu;

					// 更新图标预览
					iconPreview.Image = toolbarManager.form.iconManager.LoadIcon(x.Button);
				}
			}
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
			if (_target.Equals("default"))
			{
				var newButton = new ToolbarButton("新按钮", "", "", "", "", "0");
				toolbarManager.toolbarButtons.Add(newButton);
				AddToolbarButtonToPanel(newButton);
			}
			else
			{
				var newButton = new MenuInfo("新按钮", "", "", "", "", 0, "");
				toolbarManager.toolbarsDict[_target].Add(newButton);
				AddToolbarButtonToPanel(newButton);
			}
			IsModified = true;
		}

		private void BtnDelete_Click(object? sender, EventArgs e)
        {
			if (currentButton != null && _target.Equals("default"))
			{
				toolbarManager.toolbarButtons.Remove((ToolbarButton)currentButton);
				LoadToolbarButtons(_target);
				currentButton = null;
				btnDelete.Enabled = false;
				ClearProperties();
				IsModified = true; 
				return;
			}
			if(currentMenuInfo != null && !_target.Equals("default"))
			{
				toolbarManager.toolbarsDict[_target].Remove(currentMenuInfo);
				LoadToolbarButtons(_target);
				currentMenuInfo = null;
				btnDelete.Enabled = false;
				ClearProperties();
				IsModified = true;
			}
        }

        private void PropertyChanged(object? sender, EventArgs e)
        {
            if (currentButton != null)
            {
                var index = toolbarManager.toolbarButtons.IndexOf((ToolbarButton)currentButton);
                if (index >= 0)
                {
                    toolbarManager.toolbarButtons[index] = new ToolbarButton(
                        tooltipTextBox.Text,
                        cmdTextBox.Text,
                        iconFileTextBox.Text,
                        pathTextBox.Text,
                        paramTextBox.Text,
                        "0" // 这里可以根据需要修改iconic值
                    );
                    LoadToolbarButtons(_target);
                    IsModified = true;
                }
				return;
            }
			if (currentMenuInfo != null)
			{
				var index = toolbarManager.toolbarsDict[_target].IndexOf(currentMenuInfo);
				if (index >= 0)
				{
					toolbarManager.toolbarsDict[_target][index] = new MenuInfo(
						tooltipTextBox.Text,
						cmdTextBox.Text,
						iconFileTextBox.Text,
						pathTextBox.Text,
						paramTextBox.Text,
						0, // 这里可以根据需要修改iconic值
						""
					);
					LoadToolbarButtons(_target);
					IsModified = true;
				}
			}
		}

        private void BrowseIcon_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "图标文件 (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                iconFileTextBox.Text = dialog.FileName;
                if (currentButton != null)
                {
                    iconPreview.Image = toolbarManager.form.iconManager.LoadIcon(dialog.FileName);
                }
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (IsModified)
            {
                // 更新将在ToolbarManager中处理
                DialogResult = DialogResult.OK;
            }
            Close();
        }

		private void BtnCancel_Click(object? sender, EventArgs eventArgs)
		{
			Close();
		}

		private void ClearProperties()
        {
			//cmdTextBox.SelectedIndex = -1;
			cmdTextBox.Text = "";
            paramTextBox.Text = "";
            pathTextBox.Text = "";
            iconFileTextBox.Text = "";
            tooltipTextBox.Text = "";
            iconPreview.Image = null;
        }
    }
} 