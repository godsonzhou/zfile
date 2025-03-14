﻿using System.Data;
namespace zfile
{
	public class AddWlxMappingForm : Form
	{
		private ComboBox pluginCombo;
		private TextBox extensionBox;
		public string SelectedPlugin => pluginCombo.SelectedItem?.ToString() ?? "";
		public string Extension => extensionBox.Text;

		public AddWlxMappingForm(WlxModuleList wlxModules)
		{
			Text = "添加WLX插件映射";
			Size = new Size(300, 150);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;

			TableLayoutPanel layout = new()
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 3,
				Padding = new Padding(10)
			};

			layout.Controls.Add(new Label { Text = "插件:" }, 0, 0);
			pluginCombo = new ComboBox { Dock = DockStyle.Fill };
			pluginCombo.Items.AddRange(wlxModules._modules.Select(m => m.Name).ToArray());
			layout.Controls.Add(pluginCombo, 1, 0);

			layout.Controls.Add(new Label { Text = "扩展名:" }, 0, 1);
			extensionBox = new TextBox { Dock = DockStyle.Fill };
			layout.Controls.Add(extensionBox, 1, 1);

			FlowLayoutPanel buttonPanel = new()
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft
			};

			Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
			Button btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK };
			buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

			layout.Controls.Add(buttonPanel, 1, 2);

			Controls.Add(layout);
		}
	}
	// 添加插件映射对话框
	public class AddPluginMappingForm : Form
	{
		public ComboBox pluginCombo;
		public TextBox extensionBox;
		public string SelectedPlugin => pluginCombo.SelectedItem?.ToString() ?? "";
		public string Extension => extensionBox.Text;
		// WLX插件映射对话框
	
		public AddPluginMappingForm(WcxModuleList wcxModules)
		{
			Text = "添加插件映射";
			Size = new Size(400, 200);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;

			TableLayoutPanel layout = new()
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 3,
				Padding = new Padding(10)
			};

			layout.Controls.Add(new Label { Text = "插件:" }, 0, 0);
			pluginCombo = new ComboBox { Dock = DockStyle.Fill };
			pluginCombo.Items.AddRange(wcxModules._modules.Select(m => m.Name).ToArray());
			layout.Controls.Add(pluginCombo, 1, 0);

			layout.Controls.Add(new Label { Text = "扩展名:" }, 0, 1);
			extensionBox = new TextBox { Dock = DockStyle.Fill };
			layout.Controls.Add(extensionBox, 1, 1);

			FlowLayoutPanel buttonPanel = new()
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft
			};

			Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
			Button btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK };
			buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

			layout.Controls.Add(buttonPanel, 1, 2);

			Controls.Add(layout);
		}
	}
	public partial class OptionsForm : Form
    {
        private Panel optionPanel;
        private Panel fontPanel;
        private ComboBox fontComboBox;
        private Form1 mainForm;
		public Dictionary<string, KeyDef> commandHotkeys = new();
		private Dictionary<string, Label> commandLabels;
		private Dictionary<string, ComboBox> commandComboBoxes; 
		// 添加修饰键的复选框字典
		private Dictionary<string, CheckBox> ctrlCheckBoxes = new();
		private Dictionary<string, CheckBox> altCheckBoxes = new();
		private Dictionary<string, CheckBox> shiftCheckBoxes = new();
		private Dictionary<string, CheckBox> winCheckBoxes = new();
		private bool hasConflict = false;  // 添加冲突标志
		private readonly Color conflictColor = Color.Red;
		private readonly Color normalColor = SystemColors.Window;
		// 在构造函数中初始化ToolTip
		private readonly ToolTip toolTip;
		// 添加插件配置面板
		private Panel pluginPanel;
		private WcxModuleList wcxModuleList;
		public OptionsForm(Form1 mainForm)
        {
            InitializeComponent();
            //this.commandHotkeys = commandHotkeys;
            this.mainForm = mainForm;
			this.wcxModuleList = mainForm.wcxModuleList;
			// 初始化ToolTip
			toolTip = new ToolTip
			{
				InitialDelay = 0,
				ReshowDelay = 0,
				ShowAlways = true
			};
			InitializeOptionPanel();
            InitializeFontPanel();
            InitializeTreeView();
			InitializePluginPanel();  // 添加插件配置面板初始化
		}
		private void InitializePluginPanel()
		{
			pluginPanel = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true
			};

			// 创建标签页控件
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};

			// 添加WCX/WDX/WLX/WFX标签页
			string[] pluginTypes = { "WCX", "WDX", "WLX", "WFX" };
			foreach (var type in pluginTypes)
			{
				TabPage tabPage = new TabPage(type);
				if (type == "WCX")
				{
					InitializeWcxTab(tabPage);
				}
				else if (type == "WLX")
				{
					InitializeWlxTab(tabPage);
				}
				tabControl.TabPages.Add(tabPage);
			}

			pluginPanel.Controls.Add(tabControl);
			splitContainer1.Panel2.Controls.Add(pluginPanel);
			pluginPanel.Visible = false;
		}
		private void InitializeWlxTab(TabPage tabPage)
		{
			// 创建DataGridView显示插件配置
			DataGridView grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = true,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false
			};

			// 添加列
			grid.Columns.Add("PluginName", "插件名称");
			grid.Columns.Add("Extension", "文件扩展名");

			// 添加按钮面板
			FlowLayoutPanel buttonPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				FlowDirection = FlowDirection.RightToLeft,
				Height = 40,
				Padding = new Padding(5)
			};

			Button btnAdd = new Button { Text = "添加", Width = 80 };
			Button btnDelete = new Button { Text = "删除", Width = 80 };
			Button btnMoveUp = new Button { Text = "上移", Width = 80 };
			Button btnMoveDown = new Button { Text = "下移", Width = 80 };

			buttonPanel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnMoveUp, btnMoveDown });

			// 加载现有配置
			foreach (var config in mainForm.wlxModuleList._configDict)
			{
				grid.Rows.Add(config.Value, config.Key);
			}

			// 添加事件处理
			btnAdd.Click += (s, e) => AddWlxMapping(grid);
			btnDelete.Click += (s, e) => DeleteWlxMapping(grid);
			btnMoveUp.Click += (s, e) => MoveWlxMapping(grid, -1);
			btnMoveDown.Click += (s, e) => MoveWlxMapping(grid, 1);

			tabPage.Controls.Add(grid);
			tabPage.Controls.Add(buttonPanel);
		}

		private void AddWlxMapping(DataGridView grid)
		{
			using var addForm = new AddWlxMappingForm(mainForm.wlxModuleList);
			if (addForm.ShowDialog() == DialogResult.OK)
			{
				grid.Rows.Add(addForm.SelectedPlugin, addForm.Extension);
				// 更新配置
				UpdateWlxConfiguration(grid);
			}
		}

		private void DeleteWlxMapping(DataGridView grid)
		{
			if (grid.SelectedRows.Count > 0)
			{
				grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
				// 更新配置
				UpdateWlxConfiguration(grid);
			}
		}

		private void MoveWlxMapping(DataGridView grid, int offset)
		{
			if (grid.SelectedRows.Count == 0) return;

			int currentIndex = grid.SelectedRows[0].Index;
			int newIndex = currentIndex + offset;

			if (newIndex >= 0 && newIndex < grid.Rows.Count)
			{
				DataGridViewRow row = grid.Rows[currentIndex];
				grid.Rows.RemoveAt(currentIndex);
				grid.Rows.Insert(newIndex, row);
				grid.ClearSelection();
				grid.Rows[newIndex].Selected = true;
				// 更新配置
				UpdateWlxConfiguration(grid);
			}
		}

		private void UpdateWlxConfiguration(DataGridView grid)
		{
			// 清除现有配置
			mainForm.wlxModuleList._configDict.Clear();

			// 从grid重建配置
			foreach (DataGridViewRow row in grid.Rows)
			{
				string ext = row.Cells["Extension"].Value?.ToString() ?? "";
				string pluginName = row.Cells["PluginName"].Value?.ToString() ?? "";

				var module = mainForm.wlxModuleList.FindModuleByName(pluginName);
				if (module != null)
				{
					mainForm.wlxModuleList._configDict[ext] = module.Name;
				}
			}
			mainForm.wlxModuleList.isConfigChanged = true;
		}
		private void InitializeWcxTab(TabPage tabPage)
		{
			// 创建DataGridView显示插件配置
			DataGridView grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = true,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				EditMode = DataGridViewEditMode.EditProgrammatically,
				MultiSelect = false
			};

			// 添加列
			grid.Columns.Add("PluginName", "插件名称");
			grid.Columns.Add("Extension", "文件扩展名");

			// 添加按钮面板
			FlowLayoutPanel buttonPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				FlowDirection = FlowDirection.RightToLeft,
				Height = 40,
				Padding = new Padding(5)
			};

			Button btnEdit = new Button { Text = "编辑", Width = 80 };
			Button btnAdd = new Button { Text = "添加", Width = 80 };
			Button btnDelete = new Button { Text = "删除", Width = 80 };
			Button btnMoveUp = new Button { Text = "上移", Width = 80 };
			Button btnMoveDown = new Button { Text = "下移", Width = 80 };


			buttonPanel.Controls.AddRange(new Control[] { btnEdit, btnAdd, btnDelete, btnMoveUp, btnMoveDown });

			// 加载现有配置
			foreach (var ext in wcxModuleList._exts)
			{
				grid.Rows.Add(ext.Value.Name, ext.Key);
			}

			// 添加事件处理
			btnEdit.Click += (s, e) => EditWcxMapping(grid);
			btnAdd.Click += (s, e) => AddWcxMapping(grid);
			btnDelete.Click += (s, e) => DeleteWcxMapping(grid);
			btnMoveUp.Click += (s, e) => MoveWcxMapping(grid, -1);
			btnMoveDown.Click += (s, e) => MoveWcxMapping(grid, 1);

			tabPage.Controls.Add(grid);
			tabPage.Controls.Add(buttonPanel);
		}
		private void AddWcxMapping(DataGridView grid)
		{
			using var addForm = new AddPluginMappingForm(wcxModuleList);
			if (addForm.ShowDialog() == DialogResult.OK)
			{
				grid.Rows.Add(addForm.SelectedPlugin, addForm.Extension);
			}
			UpdateWcxConfiguration(grid);
		}

		private void DeleteWcxMapping(DataGridView grid)
		{
			if (grid.SelectedRows.Count > 0)
			{
				grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
			}
			UpdateWcxConfiguration(grid);
		}
		private void EditWcxMapping(DataGridView grid)
		{
			if (grid.SelectedRows.Count == 0) return;
			var selectedRow = grid.SelectedRows[0];
			string pluginName = selectedRow.Cells["PluginName"].Value?.ToString() ?? "";
			string extension = selectedRow.Cells["Extension"].Value?.ToString() ?? "";
			using var editForm = new AddPluginMappingForm(wcxModuleList)
			{
				extensionBox = { Text = extension }
			};
			editForm.pluginCombo.SelectedItem = pluginName;
			if (editForm.ShowDialog() == DialogResult.OK)
			{
				selectedRow.Cells["PluginName"].Value = editForm.SelectedPlugin;
				selectedRow.Cells["Extension"].Value = editForm.Extension;
			}
			UpdateWcxConfiguration(grid);

		}

		private void MoveWcxMapping(DataGridView grid, int offset)
		{
			if (grid.SelectedRows.Count == 0) return;

			int currentIndex = grid.SelectedRows[0].Index;
			int newIndex = currentIndex + offset;

			if (newIndex >= 0 && newIndex < grid.Rows.Count)
			{
				DataGridViewRow row = grid.Rows[currentIndex];
				grid.Rows.RemoveAt(currentIndex);
				grid.Rows.Insert(newIndex, row);
				grid.ClearSelection();
				grid.Rows[newIndex].Selected = true;
			}
			UpdateWcxConfiguration(grid);
		}
		private void UpdateWcxConfiguration(DataGridView grid)
		{
			// 清除现有配置
			mainForm.wcxModuleList._cfg.Clear();

			// 从grid重建配置
			foreach (DataGridViewRow row in grid.Rows)
			{
				string ext = row.Cells["Extension"].Value?.ToString() ?? "";
				string pluginName = row.Cells["PluginName"].Value?.ToString() ?? "";

				var module = mainForm.wcxModuleList.FindModuleByName(pluginName);
				if (module != null)
				{
					//mainForm.wcxModuleList._configDict[ext] = module.Name;
					mainForm.wcxModuleList._cfg.Add($"{ext}=0,{module.FilePath}");
				}
			}
			mainForm.wcxModuleList.isConfigChanged = true;
		}
		// 重写FormClosing事件，防止有冲突时关闭窗口
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing && hasConflict)
			{
				if (MessageBox.Show("存在快捷键冲突，是否放弃更改？", "警告",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
				{
					e.Cancel = true;
				}
			}
			base.OnFormClosing(e);
		}
		private void InitializeOptionPanel()
        {
            optionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            commandLabels = new Dictionary<string, Label>();
            commandComboBoxes = new Dictionary<string, ComboBox>();

			var cmdtable = mainForm.cmdProcessor.cmdTable;
			var cmdmap = mainForm.keyManager.cmdmap;
			
			foreach (var cmd in cmdtable.GetAll())
			{
				KeyDef keydef = null;
				//将keymap的内容补充到commandhotkeys中
				if (!cmdmap.ContainsKey(cmd.CmdName))
					continue;
				keydef = cmdmap[cmd.CmdName];  //C+8 代表 ctrl+8, S+A 代表 shift+A A+8 代表 ALT+8 AS+0 代表 ALT+SHIFT+0	
			
				if (!commandHotkeys.ContainsKey(cmd.CmdName))
				{
					commandHotkeys[cmd.CmdName] = keydef;
				}
			}
			int y = 10;
            foreach (var cmd in commandHotkeys)
            {
				var keydef = cmd.Value;
				//if(cmd.Value == Keys.None) continue;
				Label label = new Label
                {
                    Text = mainForm.cmdProcessor.cmdTable.GetByCmdName(keydef.Cmd)?.Description ?? cmd.Key,
					Location = new Point(10, y),
                    AutoSize = true,
					Width = 180
				};
                optionPanel.Controls.Add(label);
				commandLabels[keydef.Cmd] = label;
				
				// 添加修饰键复选框
				int checkBoxX = 200;
				var ctrlBox = CreateModifierCheckBox("Ctrl", checkBoxX, y, keydef.HasCtrl);
				var altBox = CreateModifierCheckBox("Alt", checkBoxX + 60, y, keydef.HasAlt);
				var shiftBox = CreateModifierCheckBox("Shift", checkBoxX + 120, y, keydef.HasShift);
				var winBox = CreateModifierCheckBox("Win", checkBoxX + 180, y, keydef.HasWin);

				ctrlCheckBoxes[keydef.Cmd] = ctrlBox;
				altCheckBoxes[keydef.Cmd] = altBox;
				shiftCheckBoxes[keydef.Cmd] = shiftBox;
				winCheckBoxes[keydef.Cmd] = winBox;
				optionPanel.Controls.AddRange([ctrlBox, altBox, shiftBox, winBox]);

				var comboBox = new ComboBox
				{
					Location = new Point(250 + checkBoxX, y),
					Width = 80,
					DropDownStyle = ComboBoxStyle.DropDownList
				};
                comboBox.Items.AddRange(Enum.GetNames(typeof(Keys)));
				// 解析修饰键和主键
				var keys = keydef.Key.Split('+', StringSplitOptions.RemoveEmptyEntries);
				var mainkey = keys[^1];
				comboBox.SelectedItem = Helper.ConvertStringToKey(mainkey);
				comboBox.SelectedIndexChanged += (sender, e) => UpdateHotkey(cmd.Key, comboBox);
                optionPanel.Controls.Add(comboBox);
                commandComboBoxes[keydef.Cmd] = comboBox;

                y += 26;
            }
		
			splitContainer1.Panel2.Controls.Add(optionPanel);
        }
		//private CheckBox CreateModifierCheckBox(string text, int x, int y, bool isChecked)
		//{
		//	return new CheckBox
		//	{
		//		Text = text,
		//		Location = new Point(x, y),
		//		AutoSize = true,
		//		Checked = isChecked
		//	};
		//}
		// 添加修饰键变化事件
		private CheckBox CreateModifierCheckBox(string text, int x, int y, bool isChecked)
		{
			var checkBox = new CheckBox
			{
				Text = text,
				Location = new Point(x, y),
				AutoSize = true,
				Checked = isChecked
			};

			// 添加修饰键变化事件处理
			checkBox.CheckedChanged += (sender, e) =>
			{
				var cmd = commandHotkeys.First(c =>
					ctrlCheckBoxes.ContainsKey(c.Key) &&
					(ctrlCheckBoxes[c.Key] == checkBox ||
					 altCheckBoxes[c.Key] == checkBox ||
					 shiftCheckBoxes[c.Key] == checkBox ||
					 winCheckBoxes[c.Key] == checkBox)).Key;

				if (commandComboBoxes.TryGetValue(cmd, out var comboBox))
				{
					UpdateHotkey(cmd, comboBox);
				}
			};

			return checkBox;
		}
		private void InitializeFontPanel()
        {
            fontPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            Label fontLabel = new Label
            {
                Text = "字体设置",
                Location = new Point(10, 10),
                AutoSize = true
            };
            fontPanel.Controls.Add(fontLabel);

            fontComboBox = new ComboBox
            {
                Location = new Point(10, 40),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            fontComboBox.Items.AddRange(FontFamily.Families.Select(f => f.Name).ToArray());
            fontComboBox.SelectedIndexChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontComboBox);

            Label fontSizeLabel = new Label
            {
                Text = "字体大小",
                Location = new Point(10, 80),
                AutoSize = true
            };
            fontPanel.Controls.Add(fontSizeLabel);

            NumericUpDown fontSizeNumeric = new NumericUpDown
            {
                Location = new Point(10, 110),
                Width = 100,
                Minimum = 6,
                Maximum = 72,
                Value = 10
            };
            fontSizeNumeric.ValueChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontSizeNumeric);

            splitContainer1.Panel2.Controls.Add(fontPanel);
            fontPanel.Visible = false; // 初始隐藏
        }

        private void FontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fontComboBox.SelectedItem != null)
            {
                string selectedFont = fontComboBox.SelectedItem.ToString();
                float fontSize = (float)(fontPanel.Controls.OfType<NumericUpDown>().FirstOrDefault()?.Value ?? 10);
                Font newFont = new Font(selectedFont, fontSize);
                ApplyFontToControls(this, newFont);
                ApplyFontToControls(mainForm, newFont);
            }
        }

        private void ApplyFontToControls(Control control, Font font)
        {
            control.Font = font;
            foreach (Control child in control.Controls)
            {
                ApplyFontToControls(child, font);
            }
        }

        private void InitializeTreeView()
        {
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                ShowLines = true,
                ShowRootLines = true,
                ShowPlusMinus = true
            };

            TreeNode rootNode = new TreeNode("设置");
            TreeNode hotkeyNode = new TreeNode("快捷键设置");
            TreeNode fontNode = new TreeNode("字体设置");
			TreeNode pluginNode = new TreeNode("插件设置");  // 新增插件设置节点


			rootNode.Nodes.Add(hotkeyNode);
            rootNode.Nodes.Add(fontNode);
			rootNode.Nodes.Add(pluginNode);  // 添加到树中
			treeView.Nodes.Add(rootNode);
			treeView.SelectedNode = hotkeyNode;

			treeView.AfterSelect += TreeView_AfterSelect;

            splitContainer1.Panel1.Controls.Clear();
            splitContainer1.Panel1.Controls.Add(treeView);
			rootNode.Expand();
            treeView.BringToFront();
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
				// 修改现有的选择处理逻辑，添加插件设置面板的显示控制
				optionPanel.Visible = e.Node.Text == "快捷键设置";
				fontPanel.Visible = e.Node.Text == "字体设置";
				pluginPanel.Visible = e.Node.Text == "插件设置";
			}
        }

        private void UpdateHotkey(string cmdName, ComboBox comboBox)
        {
			//if (comboBox.SelectedItem != null && Enum.TryParse(comboBox.SelectedItem.ToString(), out Keys newKey))
			//{
			//    commandHotkeys[cmdName] = newKey;
			//}
			if (!commandComboBoxes.ContainsKey(cmdName)) return;

			//var comboBox = commandComboBoxes[cmdName];
			if (comboBox.SelectedItem == null) return;

			// 构建热键字符串
			string modifiers = "";
			if (winCheckBoxes[cmdName].Checked) modifiers += "#";
			if (ctrlCheckBoxes[cmdName].Checked) modifiers += "C";
			if (altCheckBoxes[cmdName].Checked) modifiers += "A";
			if (shiftCheckBoxes[cmdName].Checked) modifiers += "S";

			string keyStr = comboBox.SelectedItem.ToString();
			string fullKeyStr = modifiers.Length > 0 ? $"{modifiers}+{keyStr}" : keyStr;
			// 检查快捷键冲突
			var conflicts = CheckHotkeyConflicts(cmdName, fullKeyStr);
			if (conflicts.Any())
			{
				comboBox.BackColor = conflictColor;
				comboBox.FlatStyle = FlatStyle.Flat;
				hasConflict = true;
				string conflictCommands = string.Join(", ", conflicts);
				toolTip.SetToolTip(comboBox, $"快捷键冲突与: {conflictCommands}");
			}
			else
			{
				comboBox.BackColor = normalColor;
				comboBox.FlatStyle = FlatStyle.Standard;
				hasConflict = false;
				toolTip.SetToolTip(comboBox, "");
				mainForm.keyManager.UpdateKeyMapping(cmdName, fullKeyStr);
			}

			// 更新确定按钮状态
			UpdateOkButtonState();
			// 更新到mainForm的keyManager
			//mainForm.keyManager.UpdateKeyMapping(cmdName, fullKeyStr);
		}
		// 添加冲突检测方法
		private List<string> CheckHotkeyConflicts(string currentCmd, string hotkey)
		{
			var conflicts = new List<string>();
			foreach (var kvp in commandHotkeys)
			{
				if (kvp.Key == currentCmd) continue; // 跳过当前命令

				// 检查修饰键和主键是否完全匹配
				var otherHotkey = kvp.Value.Key;
				if (string.Equals(hotkey, otherHotkey, StringComparison.OrdinalIgnoreCase))
				{
					var cmdInfo = mainForm.cmdProcessor.cmdTable.GetByCmdName(kvp.Key);
					conflicts.Add(cmdInfo?.Description ?? kvp.Key);
				}
			}
			return conflicts;
		}

		// 更新确定按钮状态
		private void UpdateOkButtonState()
		{
			if (Okbutton != null)  // 假设你的确定按钮名称为buttonOk
			{
				Okbutton.Enabled = !hasConflict;
			}
		}
		private void button1_Click(object sender, EventArgs e)
        {
            // 关闭此表单
            this.Close();
        }

        private void Okbutton_Click(object sender, EventArgs e)
        {
			// 保存设置
			mainForm.keyManager.SaveKeyMappingToConfigFile();
			// 保存WLX配置
			mainForm.wlxModuleList.SaveConfiguration();
			mainForm.wcxModuleList.SaveConfiguration();
			this.Close();
        }

        // 其他代码...
    }
}
