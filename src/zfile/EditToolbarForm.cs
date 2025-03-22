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
        
        // 缓冲区，用于存储临时更改
        private List<ToolbarButton> toolbarButtonsBuffer = new List<ToolbarButton>();
        private Dictionary<string, List<MenuInfo>> toolbarsDictBuffer = new Dictionary<string, List<MenuInfo>>();
        private bool isUpdatingUI = false; // 防止UI更新和数据更新之间的循环

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
            
            // 添加复制按钮
            var btnCopy = new Button
            {
                Text = "复制(&C)",
                Location = new Point(190, 145),
                Width = 80,
                Enabled = false
            };
            btnCopy.Click += BtnCopy_Click;
            Controls.Add(btnCopy);

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
            
            // 初始化缓冲区
            toolbarButtonsBuffer.Clear();
            toolbarsDictBuffer.Clear();
            
            if (target.Equals("default"))
            {
                // 创建工具栏按钮的深拷贝到缓冲区
                foreach (var button in toolbarManager.toolbarButtons)
                {
                    toolbarButtonsBuffer.Add(new ToolbarButton(
                        button.name,
                        button.cmd,
                        button.icon,
                        button.path,
                        button.param,
                        button.iconic
                    ));
                    AddToolbarButtonToPanel(button);
                }
            }
            else
            {
                // 确保目标键存在于字典中
                if (!toolbarsDictBuffer.ContainsKey(target))
                {
                    toolbarsDictBuffer[target] = new List<MenuInfo>();
                }
                
                // 创建菜单信息的深拷贝到缓冲区
                foreach (var menuInfo in toolbarManager.toolbarsDict[target])
                {
                    toolbarsDictBuffer[target].Add(menuInfo.Clone());
                    AddToolbarButtonToPanel(menuInfo);
                }
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
                Margin = new Padding(2),
                FlatStyle = FlatStyle.Standard
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
				Margin = new Padding(2),
				FlatStyle = FlatStyle.Standard
			};
			btn.Click += ToolbarButton_Click;
			toolbarPanel.Controls.Add(btn);
		}
		private void ToolbarButton_Click(object? sender, EventArgs e)
        {
			// 清除所有按钮的高亮显示
			foreach (Control control in toolbarPanel.Controls)
			{
				if (control is Button panelBtn)
				{
					panelBtn.FlatStyle = FlatStyle.Standard;
					panelBtn.FlatAppearance.BorderSize = 1;
				}
			}

			if (sender is Button btn)
			{
				// 高亮显示当前选中的按钮
				btn.FlatStyle = FlatStyle.Flat;
				btn.FlatAppearance.BorderSize = 2;
				btn.FlatAppearance.BorderColor = Color.Blue;

                // 设置标志，防止UI更新触发PropertyChanged事件
                isUpdatingUI = true;
                
				if (btn.Tag is ToolbarButton button)
				{
					// 更新当前选中的按钮
					currentButton = button;
					btnDelete.Enabled = true;
					// 启用复制按钮
					var btnCopy = Controls.OfType<Button>().FirstOrDefault(b => b.Text == "复制(&C)");
					if (btnCopy != null) btnCopy.Enabled = true;

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
					// 启用复制按钮
					var btnCopy = Controls.OfType<Button>().FirstOrDefault(b => b.Text == "复制(&C)");
					if (btnCopy != null) btnCopy.Enabled = true;

					// 更新属性显示
					cmdTextBox.Text = x.Cmd;
					paramTextBox.Text = x.Param;
					pathTextBox.Text = x.Path;
					iconFileTextBox.Text = x.Button;
					tooltipTextBox.Text = x.Menu;

					// 更新图标预览
					iconPreview.Image = toolbarManager.form.iconManager.LoadIcon(x.Button);
				}
                
                // 重置标志
                isUpdatingUI = false;
			}
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
			if (_target.Equals("default"))
			{
				var newButton = new ToolbarButton("新按钮", "", "", "", "", "0");
				// 添加到缓冲区而不是直接修改原始数据
				toolbarButtonsBuffer.Add(newButton);
				AddToolbarButtonToPanel(newButton);
			}
			else
			{
				var newButton = new MenuInfo("新按钮", "", "", "", "", 0, "");
				// 确保目标键存在于缓冲区字典中
				if (!toolbarsDictBuffer.ContainsKey(_target))
				{
					toolbarsDictBuffer[_target] = new List<MenuInfo>();
				}
				// 添加到缓冲区而不是直接修改原始数据
				toolbarsDictBuffer[_target].Add(newButton);
				AddToolbarButtonToPanel(newButton);
			}
			IsModified = true;
		}

		private void BtnDelete_Click(object? sender, EventArgs e)
        {
			if (currentButton != null && _target.Equals("default"))
			{
				// 从缓冲区中删除按钮，而不是直接修改原始数据
				toolbarButtonsBuffer.Remove((ToolbarButton)currentButton);
				// 更新UI显示
				isUpdatingUI = true;
				LoadToolbarButtons(_target);
				isUpdatingUI = false;
				
				currentButton = null;
				btnDelete.Enabled = false;
				ClearProperties();
				IsModified = true; 
				return;
			}
			if(currentMenuInfo != null && !_target.Equals("default"))
			{
				// 确保目标键存在于缓冲区字典中
				if (!toolbarsDictBuffer.ContainsKey(_target))
				{
					toolbarsDictBuffer[_target] = new List<MenuInfo>();
				}
				// 从缓冲区中删除按钮，而不是直接修改原始数据
				toolbarsDictBuffer[_target].Remove(currentMenuInfo);
				// 更新UI显示
				isUpdatingUI = true;
				LoadToolbarButtons(_target);
				isUpdatingUI = false;
				
				currentMenuInfo = null;
				btnDelete.Enabled = false;
				ClearProperties();
				IsModified = true;
			}
        }

		private void PropertyChanged(object? sender, EventArgs e)
		{
			// 如果是UI更新触发的事件，不处理以避免循环
			if (isUpdatingUI)
				return;

			if (currentButton != null && _target.Equals("default"))
			{
				// 查找缓冲区中对应的按钮
				var index = toolbarButtonsBuffer.FindIndex(b => b == currentButton);
				if (index >= 0)
				{
					// 直接修改现有对象的属性，而不是创建新对象
					var button = toolbarButtonsBuffer[index];
					button.name = tooltipTextBox.Text;
					button.cmd = cmdTextBox.Text;
					button.icon = iconFileTextBox.Text;
					button.path = pathTextBox.Text;
					button.param = paramTextBox.Text;
					// iconic保持不变

					// 更新UI显示
					isUpdatingUI = true;
					LoadToolbarButtons(_target);
					isUpdatingUI = false;

					IsModified = true;
				}
				return;
			}

			if (currentMenuInfo != null && !_target.Equals("default"))
			{
				// 确保目标键存在于缓冲区字典中
				if (!toolbarsDictBuffer.ContainsKey(_target))
				{
					toolbarsDictBuffer[_target] = new List<MenuInfo>();
				}

				// 查找缓冲区中对应的菜单信息
				var index = toolbarsDictBuffer[_target].FindIndex(m => m == currentMenuInfo);
				if (index >= 0)
				{
					// 直接修改现有对象的属性，而不是创建新对象
					var menuInfo = toolbarsDictBuffer[_target][index];
					menuInfo.Menu = tooltipTextBox.Text;
					menuInfo.Button = iconFileTextBox.Text;
					menuInfo.Cmd = cmdTextBox.Text;
					menuInfo.Param = paramTextBox.Text;
					menuInfo.Path = pathTextBox.Text;
					// Iconic保持不变

					// 更新UI显示
					isUpdatingUI = true;
					LoadToolbarButtons(_target);
					isUpdatingUI = false;

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
                // 将缓冲区中的更改应用到原始数据中
                if (_target.Equals("default"))
                {
                    // 清空原始数据并添加缓冲区中的所有按钮
                    toolbarManager.toolbarButtons.Clear();
                    foreach (var button in toolbarButtonsBuffer)
                    {
                        toolbarManager.toolbarButtons.Add(new ToolbarButton(
                            button.name,
                            button.cmd,
                            button.icon,
                            button.path,
                            button.param,
                            button.iconic
                        ));
                    }
                }
                else
                {
                    // 确保目标键存在于原始字典中
                    if (!toolbarManager.toolbarsDict.ContainsKey(_target))
                    {
                        toolbarManager.toolbarsDict[_target] = new List<MenuInfo>();
                    }
                    
                    // 清空原始数据并添加缓冲区中的所有菜单信息
                    toolbarManager.toolbarsDict[_target].Clear();
                    if (toolbarsDictBuffer.ContainsKey(_target))
                    {
                        foreach (var menuInfo in toolbarsDictBuffer[_target])
                        {
                            toolbarManager.toolbarsDict[_target].Add(menuInfo.Clone());
                        }
                    }
                }
                
                DialogResult = DialogResult.OK;
				//写入配置文件，并刷新工具栏
				toolbarManager.SaveToconfig(_target);
				toolbarManager.GenerateDynamicToolbar();
			}
			Close();
        }

		private void BtnCancel_Click(object? sender, EventArgs eventArgs)
		{
			// 放弃缓冲区中的更改，直接关闭窗口
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void BtnCopy_Click(object? sender, EventArgs e)
		{
			if (currentButton != null && _target.Equals("default"))
			{
				// 创建新按钮并复制当前按钮的属性
				var newButton = new ToolbarButton(
					currentButton?.name + " 副本",
					currentButton?.cmd,
					currentButton?.icon,
					currentButton?.path,
					currentButton?.param,
					currentButton?.iconic
				);
				// 添加到缓冲区而不是直接修改原始数据
				toolbarButtonsBuffer.Add(newButton);
				AddToolbarButtonToPanel(newButton);
				IsModified = true;
			}
			else if (currentMenuInfo != null && !_target.Equals("default"))
			{
				// 创建新按钮并复制当前按钮的属性
				var newButton = new MenuInfo(
					currentMenuInfo.Menu + " 副本",
					currentMenuInfo.Button,
					currentMenuInfo.Cmd,
					currentMenuInfo.Param,
					currentMenuInfo.Path,
					currentMenuInfo.Iconic,
					currentMenuInfo.Menu + " 副本"
				);
				// 确保目标键存在于缓冲区字典中
				if (!toolbarsDictBuffer.ContainsKey(_target))
				{
					toolbarsDictBuffer[_target] = new List<MenuInfo>();
				}
				// 添加到缓冲区而不是直接修改原始数据
				toolbarsDictBuffer[_target].Add(newButton);
				AddToolbarButtonToPanel(newButton);
				IsModified = true;
			}
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