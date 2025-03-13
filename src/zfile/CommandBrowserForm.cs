namespace zfile
{
    public class CommandBrowserForm : Form
    {
        private Panel searchPanel;
        private TextBox searchBox;
        private ListView listView;
        private Button btnNew;
        private Button btnEdit;
        private Button btnDel;
        private Button btnCopy;
        private Button btnRename;
		private Button btnSave;
		private Button btnChoose;
		private Button btnClose;
        private CmdProc cmdProcessor;
        private bool isChooseMode;
		public string CmdRet;

        public CommandBrowserForm(CmdProc cmdProcessor, bool isChooseMode = false)
        {
            this.isChooseMode = isChooseMode;
            this.cmdProcessor = cmdProcessor;
            InitializeComponent();
            LoadCommands();
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            this.Text = "命令浏览器";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            // 创建搜索面板
            searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            var btnWidth = 70;
			var startx = 300;
            searchBox = new TextBox
            {
                Location = new Point(10, 10),
                Width = 200,
                PlaceholderText = "搜索命令..."
            };
			if (isChooseMode)
			{
				btnChoose = new Button
				{
					Location = new Point(startx, 10),
					Width = btnWidth,
					Text = "Choose"
				};
				btnClose = new Button
				{
					Location = new Point(startx+btnWidth, 10),
					Width = btnWidth,
					Text = "Close"
				};
			    searchPanel.Controls.AddRange(new Control[] { searchBox, btnChoose, btnClose });
				btnChoose.Click += BtnChoose_Click;
				btnClose.Click += BtnClose_Click;
			}
			else
			{
				btnNew = new Button
				{
					Location = new Point(startx, 10),
					Width = btnWidth,
					Text = "New"
				};

				btnEdit = new Button
				{
					Location = new Point(startx+btnWidth, 10),
					Width = btnWidth,
					Text = "Edit"
				};

				btnDel = new Button
				{
					Location = new Point(startx + btnWidth*2, 10),
					Width = btnWidth,
					Text = "Delete"
				};

				btnCopy = new Button
				{
					Location = new Point(startx + btnWidth*3, 10),
					Width = btnWidth,
					Text = "Copy"
				};

				btnRename = new Button
				{
					Location = new Point(startx + btnWidth*4, 10),
					Width = btnWidth,
					Text = "Rename"
				};
                btnSave = new Button
                {
                    Location = new Point(startx + btnWidth*5, 10),
                    Width = btnWidth,
                    Text = "Save"
                };
                
		    	searchPanel.Controls.AddRange(new Control[] { searchBox, btnNew, btnEdit, btnDel, btnCopy, btnRename, btnSave });
                btnNew.Click += BtnNew_Click;
                btnEdit.Click += BtnEdit_Click;
                btnDel.Click += BtnDel_Click;
                btnCopy.Click += BtnCopy_Click;
                btnRename.Click += BtnRename_Click;
                btnSave.Click += BtnSave_Click;
            }

            // 创建ListView用于显示命令
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            // 添加列
            listView.Columns.Add("ID", 80);
            listView.Columns.Add("命令名称", 200);
            listView.Columns.Add("描述", 250);
            listView.Columns.Add("中文描述", 250);

            // 添加控件到窗体
            this.Controls.Add(listView);
            this.Controls.Add(searchPanel);

            // 设置初始焦点
            this.Load += (s, e) => searchBox.Focus();

            // 添加事件处理
            searchBox.TextChanged += SearchBox_TextChanged;
            listView.DoubleClick += ListView_DoubleClick;
			listView.Click += ListView_Click;
        

            // 添加右键菜单
            var contextMenu = new ContextMenuStrip();
            var copyMenuItem = new ToolStripMenuItem("复制命令名称");
            var execMenuItem = new ToolStripMenuItem("执行命令");

            copyMenuItem.Click += CopyMenuItem_Click;
            execMenuItem.Click += ExecMenuItem_Click;

            contextMenu.Items.AddRange(new ToolStripItem[] { copyMenuItem, execMenuItem });
            listView.ContextMenuStrip = contextMenu;
        }

		private void ListView_Click(object? sender, EventArgs e)
		{
			CmdRet = listView.SelectedItems[0].SubItems[1].Text;
		}

		private void BtnClose_Click(object? sender, EventArgs e)
		{
			CmdRet = string.Empty;
			DialogResult = DialogResult.None;
			this.Close();
		}

		private void BtnChoose_Click(object? sender, EventArgs e)
		{
			//设置dialogresult = ok
			if (listView.SelectedItems.Count == 0)
				return;
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void LoadCommands()
        {
			if (isChooseMode)
			{
				// 获取所有命令并填充ListView
				var commands = cmdProcessor.cmdTable.GetAll();
				foreach (var cmd in commands)
				{
					var item = new ListViewItem(cmd.CmdId.ToString());
					item.SubItems.Add(cmd.CmdName);
					item.SubItems.Add(cmd.Description);
					item.SubItems.Add(cmd.ZhDesc);
					listView.Items.Add(item);
				}
			}else
			{
                // 从cmdprocessor.emcmds获取所有em命令并填充ListView
                var emcmds = cmdProcessor.emCmds;
                foreach (var cmd in emcmds)
                {
                    var item = new ListViewItem(" ");
                    item.SubItems.Add(cmd.Name);
                    item.SubItems.Add(cmd.Menu);
                    item.SubItems.Add(cmd.Menu);
                    listView.Items.Add(item);
                }
			}
		}

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchBox.Text.ToLower();
            bool foundMatch = false;

            foreach (ListViewItem item in listView.Items)
            {
                bool match = false;
                
                // 搜索ID
                match = item.Text.ToLower().Contains(searchText);
                
                // 搜索名称
                if (!match)
                {
                    match = item.SubItems[1].Text.ToLower().Contains(searchText);
                    
                    // 搜索描述
                    if (!match)
                        match = item.SubItems[2].Text.ToLower().Contains(searchText) ||
                               item.SubItems[3].Text.ToLower().Contains(searchText);
                }
                
                item.ForeColor = match || string.IsNullOrEmpty(searchText) ?
                    SystemColors.WindowText : SystemColors.GrayText;
                
                if (match && !foundMatch)
                {
                    // 找到第一个匹配项
                    foundMatch = true;
                    item.Selected = true;
                    item.EnsureVisible(); // 滚动到可见区域
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                Clipboard.SetText(cmdName);
                MessageBox.Show($"命令 {cmdName} 已复制到剪贴板", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                Clipboard.SetText(cmdName);
            }
        }

        private void ExecMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                cmdProcessor.ExecCmd(cmdName);
                this.Close();
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
			// 创建新命令
			var newName = InputCommandName(false);
			if (newName == null) return;

            ShowCommandEditDialog(new MenuInfo(newName), false);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            // 编辑选中的命令
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                var cmdItem = cmdProcessor.GetEmdByName(cmdName);
                if (cmdItem != null)
                {
                    ShowCommandEditDialog(cmdItem, true);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个命令", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存命令
            cmdProcessor.SaveEmCmdCfg();
        }
	
        private void BtnDel_Click(object sender, EventArgs e)
        {
            // 删除选中的命令
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                var result = MessageBox.Show($"确定要删除命令 {cmdName} 吗？", "确认删除",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
					// 这里应该添加删除命令的逻辑
					var cmd = cmdProcessor.emCmds.Find(x => x.Name.Equals(cmdName));
					cmdProcessor.emCmds.Remove(cmd);
                    // 删除后刷新列表
                    listView.Items.Remove(listView.SelectedItems[0]);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个命令", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            // 复制选中的命令
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                var cmdItem = cmdProcessor.GetEmdByName(cmdName);
                if (cmdItem != null)
                {
                    // 创建一个新的命令，基于选中的命令
                    var newCmdItem = cmdItem.Clone();
                    newCmdItem.Name = $"{newCmdItem.Name}_copy";
					//cmdProcessor.emCmds.Add(newCmdItem);
                    ShowCommandEditDialog(newCmdItem, false);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个命令", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
	
		private string InputCommandName(bool isRename = true)
		{
			string newName = null;
			string cmdName = string.Empty;
			MenuInfo? cmdItem = null;
			if (listView.SelectedItems.Count > 0) {
				cmdName = listView.SelectedItems[0].SubItems[1].Text;
				cmdItem = cmdProcessor.GetEmdByName(cmdName);
			}
			// 重命名选中的命令
			if (isRename && listView.SelectedItems.Count == 0)
				return null;
			
			// 显示重命名对话框
			var inputDialog = new Form
			{
				Width = 400,
				Height = 150,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				Text = isRename ? "重命名命令" : "新命令名称",
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var textBox = new TextBox
			{
				Left = 50,
				Top = 20,
				Width = 300,
				Text = isRename ? cmdName : ""
			};

			var okButton = new Button
			{
				Text = "确定",
				Left = 100,
				Width = 100,
				Top = 70,
				DialogResult = DialogResult.OK
			};

			var cancelButton = new Button
			{
				Text = "取消",
				Left = 220,
				Width = 100,
				Top = 70,
				DialogResult = DialogResult.Cancel
			};

			inputDialog.Controls.Add(textBox);
			inputDialog.Controls.Add(okButton);
			inputDialog.Controls.Add(cancelButton);
			inputDialog.AcceptButton = okButton;
			inputDialog.CancelButton = cancelButton;

			var result = inputDialog.ShowDialog();
			if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
			{
				newName = textBox.Text;
				if (isRename)
				{
					// 这里应该添加重命名命令的逻辑
					cmdItem.Name = newName;
					// 重命名后刷新列表
					listView.SelectedItems[0].SubItems[1].Text = newName;
				}
			}
			
			
			return newName;
		}
        private void BtnRename_Click(object sender, EventArgs e)
        {
			InputCommandName();
        }

        private void ShowCommandEditDialog(MenuInfo? cmdItem, bool isRename)
        {
            // 创建并显示命令编辑对话框
            var editDialog = new CommandEditDialog(cmdProcessor, cmdItem);
            if (editDialog.ShowDialog() == DialogResult.OK)
            {
                // 获取编辑结果
                string command = editDialog.Command;
                string parameters = editDialog.Parameters;
                string workingDir = editDialog.WorkingDirectory;
                string iconFile = editDialog.IconFile;
                string tooltip = editDialog.Tooltip;
                int iconIndex = editDialog.SelectedIconIndex;

                // 保存命令
                SaveCommand(cmdItem, command, parameters, workingDir, iconFile, iconIndex, tooltip, isRename);

                // 刷新命令列表
                RefreshCommandList();
            }
        }

        private void SaveCommand(MenuInfo? cmd, string command, string parameters, string workingDir, string iconFile, int iconIndex, string tooltip, bool isRename)
        {
			// 这里应该实现保存命令到配置文件的逻辑
			// 例如，将命令保存到Wcmd_chn.ini文件中
			// 简化起见，这里只显示一个消息框
			//var cmd = cmdProcessor.emCmds.Find(e => e.Name.Equals(command));
			if (cmd == null)
				cmd = new MenuInfo(command);

			cmd.Menu = tooltip;
			cmd.Param = parameters;
			cmd.Path = workingDir;
			cmd.Button = iconFile;
			cmd.Iconic = iconIndex;
			cmd.Cmd = command;
			if(!isRename) 
				cmdProcessor.emCmds.Add(cmd);
			//MessageBox.Show($"命令 {command} 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void RefreshCommandList()
        {
            // 清空并重新加载命令列表
            listView.Items.Clear();
            LoadCommands();
        }
    }
}