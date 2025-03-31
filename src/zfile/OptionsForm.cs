using System.Data;
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

		private void InitializeComponent()
		{

		}
	}
	// 添加插件映射对话框
	// AddPluginMappingForm类已移至单独的文件中
	public partial class OptionsForm : Form
    {
		private Panel BasicPanel;
        private Panel HotKeyPanel;
        private Panel fontPanel;
        private ComboBox fontComboBox;
		private NumericUpDown fontSizeNumeric;
		private MainForm mainForm;
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
		// 添加压缩程序配置面板
		private Panel compressPanel;
		private Dictionary<string, Panel> compressPanels = new();
        // 添加自定义视图配置面板
        private Panel customViewPanel;
        // 添加视图模式配置面板
        private Panel viewModePanel;
        // 添加视图自动切换规则配置面板
        private Panel autoSwitchViewPanel;
		private string _node;
		private TreeNode rootNode;
		public OptionsForm(MainForm mainForm, string node)
        {
			_node = node;
            InitializeComponent();
			Size = new Size(1024, 768);
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
			InitializeBasicPanel();
			InitializeHotkeyPanel();
            InitializeFontPanel();
            InitializeTreeView();
			InitializePluginPanel();  // 添加插件配置面板初始化
			InitializeCompressPanel();  // 添加压缩程序配置面板初始化
            InitializeCustomViewPanel();  // 添加自定义视图配置面板初始化
            InitializeViewModePanel();  // 添加视图模式配置面板初始化
            InitializeAutoSwitchViewPanel();  // 添加视图自动切换规则配置面板初始化
			Helper.ApplyFontToControls(this, mainForm.myfont); // 设置字体
			if(!string.IsNullOrEmpty(node))
				SelectNodeByName(node);
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
			splitContainer2.Panel1.Controls.Add(pluginPanel);
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
		private void InitializeBasicPanel()
		{
			BasicPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
			var F4Label = new Label
			{
				Text = "F4键打开命令行",
				Location = new Point(10, 10),
				AutoSize = true
			};
			var F4textbox = new TextBox
			{
				Location = new Point(200, 10),
				Width = 400,
				Text = mainForm.configLoader.FindConfigValue("Configuration", "Editor")
			};
			BasicPanel.Controls.AddRange([F4Label,F4textbox]);
			splitContainer2.Panel1.Controls.Add(BasicPanel);
		}
		private void InitializeHotkeyPanel()
        {
            HotKeyPanel = new Panel
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
                HotKeyPanel.Controls.Add(label);
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
				HotKeyPanel.Controls.AddRange([ctrlBox, altBox, shiftBox, winBox]);

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
                HotKeyPanel.Controls.Add(comboBox);
                commandComboBoxes[keydef.Cmd] = comboBox;

                y += 26;
            }
		
			splitContainer2.Panel1.Controls.Add(HotKeyPanel);
        }
		
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
			
			fontComboBox.Text = mainForm.myfont.Name;
			
			fontComboBox.SelectedIndexChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontComboBox);

            Label fontSizeLabel = new Label
            {
                Text = "字体大小",
                Location = new Point(10, 80),
                AutoSize = true
            };
            fontPanel.Controls.Add(fontSizeLabel);
		
			fontSizeNumeric = new NumericUpDown
            {
                Location = new Point(10, 110),
                Width = 100,
                Minimum = 6,
                Maximum = 72,
                Value = Convert.ToDecimal((mainForm.myfont.Size))
			};
            fontSizeNumeric.ValueChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontSizeNumeric);

            splitContainer2.Panel1.Controls.Add(fontPanel);
            fontPanel.Visible = false; // 初始隐藏
        }

        private void FontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        
        }

		private void updateFont()
		{
			if (fontComboBox.SelectedItem != null)
			{
				string selectedFont = fontComboBox.SelectedItem.ToString();
				float fontSize = (float)(fontPanel.Controls.OfType<NumericUpDown>().FirstOrDefault()?.Value ?? 10);
				Font newFont = new Font(selectedFont, fontSize);
				Helper.ApplyFontToControls(this, newFont);
				Helper.ApplyFontToControls(mainForm, newFont);
				mainForm.myfont = newFont;
				mainForm.configLoader.SetConfigValue("AllResolutions", "FontName", fontComboBox.Text);
				mainForm.configLoader.SetConfigValue("AllResolutions", "FontSize", fontSizeNumeric.Value.ToString());
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

            rootNode = new TreeNode("设置");
			TreeNode BasicNode = new TreeNode("基本设置");
			TreeNode hotkeyNode = new TreeNode("快捷键设置");
            TreeNode fontNode = new TreeNode("字体设置");
            TreeNode pluginNode = new TreeNode("插件设置");
            TreeNode compressNode = new TreeNode("压缩程序");  // 新增压缩程序节点
            TreeNode customViewNode = new TreeNode("自定义视图");  // 新增自定义视图节点
            TreeNode viewModeNode = new TreeNode("视图模式");  // 新增视图模式节点
            TreeNode autoSwitchViewNode = new TreeNode("视图自动切换规则");  // 新增视图自动切换规则节点

            // 添加压缩程序的子节点
            compressNode.Nodes.Add(new TreeNode("ARJ"));
            compressNode.Nodes.Add(new TreeNode("LHA"));
            compressNode.Nodes.Add(new TreeNode("RAR"));
            compressNode.Nodes.Add(new TreeNode("UC2"));
            compressNode.Nodes.Add(new TreeNode("ACE"));
            compressNode.Nodes.Add(new TreeNode("TAR"));
            compressNode.Nodes.Add(new TreeNode("其他压缩程序"));

			rootNode.Nodes.Add(BasicNode);
            rootNode.Nodes.Add(hotkeyNode);
            rootNode.Nodes.Add(fontNode);
            rootNode.Nodes.Add(pluginNode);
            rootNode.Nodes.Add(compressNode);  // 添加到树中
            rootNode.Nodes.Add(customViewNode);  // 添加自定义视图节点到树中
            rootNode.Nodes.Add(viewModeNode);  // 添加视图模式节点到树中
            rootNode.Nodes.Add(autoSwitchViewNode);  // 添加视图自动切换规则节点到树中
            treeView.Nodes.Add(rootNode);
            treeView.SelectedNode = hotkeyNode;

            treeView.AfterSelect += TreeView_AfterSelect;

            splitContainer1.Panel1.Controls.Clear();
            splitContainer1.Panel1.Controls.Add(treeView);
            rootNode.Expand();
            treeView.BringToFront();
        }
		private void SelectNodeByName(string nodeName)
		{
			foreach(var node in rootNode.Nodes.Cast<TreeNode>())
			{
				if (node.Text == nodeName)
				{
					treeView.SelectedNode = node;
					break;
				}
			}
		}

		private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                // 首先隐藏所有面板
				BasicPanel.Visible = false;
				HotKeyPanel.Visible = false;
                fontPanel.Visible = false;
                pluginPanel.Visible = false;
                compressPanel.Visible = false;
                customViewPanel.Visible = false;
                viewModePanel.Visible = false;
                autoSwitchViewPanel.Visible = false;

                // 隐藏所有压缩程序子面板
                foreach (var panel in compressPanels.Values)
                    panel.Visible = false;

                // 根据选中的节点显示相应的面板
                switch (e.Node.Text)
                {
					case "基本设置":
						BasicPanel.Visible = true;
						break;
					case "快捷键设置":
                        HotKeyPanel.Visible = true;
                        break;
                    case "字体设置":
                        fontPanel.Visible = true;
                        break;
                    case "插件设置":
                        pluginPanel.Visible = true;
                        break;
                    case "压缩程序":
                        compressPanel.Visible = true;
                        break;
                    case "自定义视图":
                        customViewPanel.Visible = true;
                        break;
					case "视图模式":
						viewModePanel.Visible = true;
						break;
                    case "视图自动切换规则":
                        autoSwitchViewPanel.Visible = true;
                        break;
					default:
                        // 处理压缩程序的子节点
                        if (e.Node.Parent?.Text == "压缩程序")
                        {
                            compressPanel.Visible = true;
                            if (compressPanels.TryGetValue(e.Node.Text, out var panel))
                            {
                                panel.Parent = compressPanel;
                                panel.Visible = true;
                                panel.BringToFront();
                            }
                        }
                        break;
                }
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
		private void UpdateBasicSettings()
		{
			// 更新基本设置
			// 更新F4键打开命令行
			var F4textbox = BasicPanel.Controls.OfType<TextBox>().FirstOrDefault();
			if (F4textbox != null)
				mainForm.configLoader.SetConfigValue("Configuration", "Editor", F4textbox.Text);
		}

		private void Okbutton_Click(object sender, EventArgs e)
        {
			// 保存设置
			mainForm.keyManager.SaveKeyMappingToConfigFile();
			// 保存WLX配置
			mainForm.wlxModuleList.SaveConfiguration();
			mainForm.wcxModuleList.SaveConfiguration();
			// save font
			updateFont();
			UpdateBasicSettings();
			// 应用视图自动切换规则的更改
			if (autoSwitchViewPanel.Visible && autoSwitchViewPanel.Controls.Count > 0)
			{
				var autoSwitchViewPanelControl = autoSwitchViewPanel.Controls[0] as AutoSwitchViewPanel;
				if (autoSwitchViewPanelControl != null)
				{
					autoSwitchViewPanelControl.ApplyChanges();
				}
			}
			
			this.Close();
        }

        // 添加压缩程序配置面板
        private void InitializeCompressPanel()
        {
            compressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Visible = false
            };

            // 通用选项
            var commonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10)
            };

            var chkFolderProcess = new CheckBox
            {
                Text = "将压缩文件按文件夹处理(&D)",
                AutoSize = true,
                Location = new Point(10, 10)
            };
            commonPanel.Controls.Add(chkFolderProcess);
            compressPanel.Controls.Add(commonPanel);

            // 初始化各压缩程序的面板
            InitializeArjPanel();
            InitializeLhaPanel();
            InitializeRarPanel();
            InitializeUc2Panel();
            InitializeAcePanel();
            InitializeTarPanel();
            InitializeOtherCompressPanel();

            // 添加按钮面板
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            //var btnHelp = new Button { Text = "帮助", Width = 80 };
            //var btnApply = new Button { Text = "应用", Width = 80 };
            //var btnCancel = new Button { Text = "取消", Width = 80, DialogResult = DialogResult.Cancel };
            //var btnOK = new Button { Text = "确定", Width = 80, DialogResult = DialogResult.OK };

            //buttonPanel.Controls.AddRange(new Control[] { btnOK, btnCancel, btnApply, btnHelp });
            compressPanel.Controls.Add(buttonPanel);

            splitContainer2.Panel1.Controls.Add(compressPanel);
        }

        private void InitializeArjPanel()
        {
            var panel = CreateCompressPanel("ARJ");

            var pathLabel = new Label { Text = "ARJ压缩程序(A):", AutoSize = true, Location = new Point(10, 10) };
            var pathBox = new TextBox { Text = "arj32.exe", Width = 300, Location = new Point(150, 10) };
            var browseBtn = new Button { Text = "...", Width = 30, Location = new Point(460, 10) };
            browseBtn.Click += (s, e) => BrowseCompressFile(pathBox);

            var chkInternalArj = new CheckBox
            {
                Text = "是否尽量使用内部ARJ解压缩程序(U)",
                AutoSize = true,
                Location = new Point(10, 40)
            };

            var chkLongNames = new CheckBox
            {
                Text = "是否向 ARJ 传递长文件名(需要 ARJ 2.55 或更新的版本）(P)",
                AutoSize = true,
                Location = new Point(10, 70)
            };

            panel.Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn, chkInternalArj, chkLongNames });
            compressPanels["ARJ"] = panel;
        }

        private void InitializeLhaPanel()
        {
            var panel = CreateCompressPanel("LHA");

            var pathLabel = new Label { Text = "LHA 压缩程序(L):", AutoSize = true, Location = new Point(10, 10) };
            var pathBox = new TextBox { Text = "lha32.exe", Width = 300, Location = new Point(150, 10) };
            var browseBtn = new Button { Text = "...", Width = 30, Location = new Point(460, 10) };
            browseBtn.Click += (s, e) => BrowseCompressFile(pathBox);

            var chkInternalLzh = new CheckBox
            {
                Text = "是否尽量使用内部LZH解压缩程序(I)",
                AutoSize = true,
                Location = new Point(10, 40)
            };

            panel.Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn, chkInternalLzh });
            compressPanels["LHA"] = panel;
        }

        private void InitializeRarPanel()
        {
            var panel = CreateCompressPanel("RAR");

            var pathLabel = new Label { Text = "RAR压缩程序(R):", AutoSize = true, Location = new Point(10, 10) };
            var pathBox = new TextBox { Text = "%COMMANDER_PATH%\\Plugins\\Wcx\\Rar\\Rar.exe", Width = 300, Location = new Point(150, 10) };
            var browseBtn = new Button { Text = "...", Width = 30, Location = new Point(460, 10) };
            browseBtn.Click += (s, e) => BrowseCompressFile(pathBox);

            var chkInternalRar = new CheckBox
            {
                Text = "是否尽量使用内部RAR解压缩程序(U)",
                AutoSize = true,
                Location = new Point(10, 40)
            };

            panel.Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn, chkInternalRar });
            compressPanels["RAR"] = panel;
        }

        private void InitializeUc2Panel()
        {
            var panel = CreateCompressPanel("UC2");

            var pathLabel = new Label { Text = "UC2 压缩程序(2):", AutoSize = true, Location = new Point(10, 10) };
            var pathBox = new TextBox { Text = "uc.exe", Width = 300, Location = new Point(150, 10) };
            var browseBtn = new Button { Text = "...", Width = 30, Location = new Point(460, 10) };
            browseBtn.Click += (s, e) => BrowseCompressFile(pathBox);

            panel.Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn });
            compressPanels["UC2"] = panel;
        }

        private void InitializeAcePanel()
        {
            var panel = CreateCompressPanel("ACE");

            var pathLabel = new Label { Text = "ACE (>= 2.04):", AutoSize = true, Location = new Point(10, 10) };
            var pathBox = new TextBox { Text = "winace.exe", Width = 300, Location = new Point(150, 10) };
            var browseBtn = new Button { Text = "...", Width = 30, Location = new Point(460, 10) };
            browseBtn.Click += (s, e) => BrowseCompressFile(pathBox);

            var chkInternalAce = new CheckBox
            {
                Text = "是否尽量使用内部ACE解压缩程序(U)",
                AutoSize = true,
                Location = new Point(10, 40)
            };

            panel.Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn, chkInternalAce });
            compressPanels["ACE"] = panel;
        }

        private void InitializeTarPanel()
        {
            var panel = CreateCompressPanel("TAR");

            var chkLinuxFormat = new CheckBox
            {
                Text = "是否创建 Linux 格式的 TAR 压缩文件(不选：SunOS 格式）(T)",
                AutoSize = true,
                Location = new Point(10, 10)
            };

            panel.Controls.Add(chkLinuxFormat);
            compressPanels["TAR"] = panel;
        }

        private void InitializeOtherCompressPanel()
        {
            var panel = CreateCompressPanel("其他压缩程序");

            var configBtn = new Button
            {
                Text = "配置压缩插件(C)",
                AutoSize = true,
                Location = new Point(10, 10)
            };

            panel.Controls.Add(configBtn);
            compressPanels["其他压缩程序"] = panel;
        }

        private Panel CreateCompressPanel(string name)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Tag = name
            };
        }

        private void BrowseCompressFile(TextBox pathBox)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                pathBox.Text = dialog.FileName;
            }
        }

        // 初始化自定义视图配置面板
        private void InitializeCustomViewPanel()
        {
            customViewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // 创建自定义视图表单
            var customViewForm = new CustomViewForm(mainForm);
            customViewForm.TopLevel = false;
            customViewForm.FormBorderStyle = FormBorderStyle.None;
            customViewForm.Dock = DockStyle.Fill;
            customViewForm.Visible = true;

            // 添加到面板中
            customViewPanel.Controls.Add(customViewForm);
            splitContainer2.Panel1.Controls.Add(customViewPanel);
            customViewPanel.Visible = false;
        }

        // 初始化视图模式配置面板
        private void InitializeViewModePanel()
        {
            viewModePanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // 创建视图模式面板
            var viewModePanelControl = new ViewModePanel(mainForm);
            viewModePanelControl.Dock = DockStyle.Fill;
            viewModePanelControl.Visible = true;

            // 添加到面板中
            viewModePanel.Controls.Add(viewModePanelControl);
            splitContainer2.Panel1.Controls.Add(viewModePanel);
            viewModePanel.Visible = false;
        }

        // 初始化视图自动切换规则配置面板
        private void InitializeAutoSwitchViewPanel()
        {
            autoSwitchViewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // 创建视图自动切换规则面板
            var autoSwitchViewPanelControl = new AutoSwitchViewPanel(mainForm);
            autoSwitchViewPanelControl.Dock = DockStyle.Fill;
            autoSwitchViewPanelControl.Visible = true;

            // 添加到面板中
            autoSwitchViewPanel.Controls.Add(autoSwitchViewPanelControl);
            splitContainer2.Panel1.Controls.Add(autoSwitchViewPanel);
            autoSwitchViewPanel.Visible = false;
        }
    }
}
