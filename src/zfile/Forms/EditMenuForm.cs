using System.Text;

namespace Zfile.Forms
{
    public partial class EditMenuForm : Form
    {
        private ComboBox menuTypeComboBox;
        private ListBox menuItemsListBox;
        private Button addItemButton;
        private Button addSubmenuButton;
        private Button deleteItemButton;
        private Button editTitleButton;
        private Button okButton;
        private Button cancelButton;
        private MainForm mainForm;
        private string currentMenuType;
        private List<string> menuItems = new();
        private int menu_id;	//0-usermenu, 1-mainmenu
        
        // 添加新控件
        private TextBox cmdTextBox;
        private TextBox paramTextBox;
        private TextBox pathTextBox;
        private Label cmdLabel;
        private Label paramLabel;
        private Label pathLabel;
		private List<MenuInfo> userMenuInfos = new();
		private List<MenuInfo> mainMenuInfos = new(); //
		private bool usermenu_changed = false;
		private bool mainmenu_changed = false;
	
		public EditMenuForm(MainForm form, int menuid)
        {
            mainForm = form;
            menu_id = menuid;
            currentMenuType = menuid == 0 ? "usermenu" : "mainmenu";
            InitializeComponent();
            menuTypeComboBox.SelectedIndex = menu_id;
            LoadMenuItems();
        }

        private void InitializeComponent()
        {
            Text = "编辑菜单";
            Size = new Size(500, 450);
            StartPosition = FormStartPosition.CenterParent;

            // 创建菜单类型选择ComboBox
            menuTypeComboBox = new ComboBox
            {
                Location = new Point(10, 10),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            menuTypeComboBox.Items.AddRange(new object[] { "usermenu", "mainmenu" });
            menuTypeComboBox.SelectedIndexChanged += MenuTypeComboBox_SelectedIndexChanged;

            // 创建菜单项ListBox
            menuItemsListBox = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(460, 250),
                SelectionMode = SelectionMode.One
            };
            menuItemsListBox.SelectedIndexChanged += MenuItemsListBox_SelectedIndexChanged;

            // 创建按钮
            addItemButton = new Button
            {
                Text = "添加项目(&A)",
                Location = new Point(10, 300),
                Size = new Size(100, 30)
            };
            addItemButton.Click += AddItemButton_Click;

            addSubmenuButton = new Button
            {
                Text = "添加子菜单(&U)",
                Location = new Point(120, 300),
                Size = new Size(100, 30)
            };
            addSubmenuButton.Click += AddSubmenuButton_Click;

            deleteItemButton = new Button
            {
                Text = "删除项目(&D)",
                Location = new Point(230, 300),
                Size = new Size(100, 30)
            };
            deleteItemButton.Click += DeleteItemButton_Click;

            editTitleButton = new Button
            {
                Text = "更改标题(&I)",
                Location = new Point(340, 300),
                Size = new Size(100, 30)
            };
            editTitleButton.Click += EditTitleButton_Click;

            okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(290, 370),
                Size = new Size(75, 30)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(375, 370),
                Size = new Size(75, 30)
            };

            // 添加命令、参数、路径输入框和标签
            cmdLabel = new Label
            {
                Text = "命令:",
                Location = new Point(10, 340),
                Size = new Size(40, 20)
            };

            cmdTextBox = new TextBox
            {
                Location = new Point(50, 340),
                Size = new Size(100, 20)
            };
            cmdTextBox.TextChanged += TextBox_TextChanged;

            paramLabel = new Label
            {
                Text = "参数:",
                Location = new Point(160, 340),
                Size = new Size(40, 20)
            };

            paramTextBox = new TextBox
            {
                Location = new Point(200, 340),
                Size = new Size(100, 20)
            };
            paramTextBox.TextChanged += TextBox_TextChanged;

            pathLabel = new Label
            {
                Text = "路径:",
                Location = new Point(310, 340),
                Size = new Size(40, 20)
            };

            pathTextBox = new TextBox
            {
                Location = new Point(350, 340),
                Size = new Size(100, 20)
            };
            pathTextBox.TextChanged += TextBox_TextChanged;

            // 添加控件到窗体
            Controls.AddRange(new Control[] {
                menuTypeComboBox,
                menuItemsListBox,
                addItemButton,
                addSubmenuButton,
                deleteItemButton,
                editTitleButton,
                cmdLabel,
                cmdTextBox,
                paramLabel,
                paramTextBox,
                pathLabel,
                pathTextBox,
                okButton,
                cancelButton
            });
        }

        private void LoadMenuItems()
        {
            menuItems.Clear();
            menuItemsListBox.Items.Clear();
			cmdTextBox.Text = "";
			paramTextBox.Text = "";
			pathTextBox.Text = "";

            if (currentMenuType == "usermenu")
            {
                var userSection = mainForm.userConfigLoader.GetConfigSection("User");
                if (userSection != null)
                {
                    List<string> str = new();
                    foreach (var item in userSection.Items)
                        str.Add(item.Key + "=" + item.Value);
                    userMenuInfos = Helper.GetMenuInfoFromList(str.ToArray());
                    foreach (var m in userMenuInfos)
                    {
                        menuItems.Add(m.Menu);
                        menuItemsListBox.Items.Add(m.Menu);
                    }
                }
            }
            else
            {
                // 加载主菜单
                var menu = mainForm.configLoader.FindConfigValue("Configuration", "Mainmenu");
                string menuFilePath = Constants.ZfileCfgPath + menu;
                if (File.Exists(menuFilePath))
                {
                    using (StreamReader reader = new StreamReader(menuFilePath, Encoding.GetEncoding("GB2312")))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
								var linepart = line.Replace("MENUITEM ", "").Replace("END_POPUP", "--").Replace("POPUP ", "-").Replace("\"", "").Replace("SEPARATOR", "-").Split(',');
								menuItems.Add(linepart[0]);
								menuItemsListBox.Items.Add(linepart[0]);
								mainMenuInfos.Add(new MenuInfo { Menu = linepart[0], Cmd = linepart[1] });
							}
                        }
                    }
                }
            }
        }

        private void MenuTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentMenuType = menuTypeComboBox.SelectedItem.ToString();
            LoadMenuItems();
        }

        private void MenuItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
			cmdTextBox.Text = "";
			paramTextBox.Text = "";
			pathTextBox.Text = "";
			if (menuItemsListBox.SelectedIndex != -1)
            {
                int selectedIndex = menuItemsListBox.SelectedIndex;
                string selectedItem = menuItems[selectedIndex];

                // 启用或禁用编辑按钮
                editTitleButton.Enabled = !selectedItem.StartsWith("--");

                // 根据菜单类型和选中项更新输入框状态
                if (currentMenuType == "usermenu")
                {
                    bool isNotEndItem = !selectedItem.StartsWith("--");
                    cmdTextBox.Enabled = isNotEndItem;
                    paramTextBox.Enabled = isNotEndItem;
                    pathTextBox.Enabled = isNotEndItem;

                    if (isNotEndItem && selectedIndex < userMenuInfos.Count)
                    {
                        // 直接从userMenuInfos获取选中项的信息
                        var menuInfo = userMenuInfos[selectedIndex];
                        cmdTextBox.Text = menuInfo.Cmd;
                        paramTextBox.Text = menuInfo.Param;
                        pathTextBox.Text = menuInfo.Path;
                    }
                }
                
                else
                {
                    // 主菜单项，禁用参数和路径输入框
                    cmdTextBox.Enabled = !selectedItem.StartsWith("--");
                    paramTextBox.Enabled = false;
                    pathTextBox.Enabled = false;
					//MenuInfo cmditem;
					if (mainForm.uiManager.usermenuMap.TryGetValue(selectedItem.TrimStart('-'), out var cmditem))
					{
						//cmditem = mainForm.uiManager.usermenuMap[selectedItem.TrimStart('-')];
						if (!selectedItem.StartsWith("--"))
							cmdTextBox.Text = cmditem.Cmd;
					}
                }
            }
            else
            {
                // 没有选中项时，禁用所有控件并清空文本框
                editTitleButton.Enabled = false;
                cmdTextBox.Enabled = false;
                paramTextBox.Enabled = false;
                pathTextBox.Enabled = false;
            }
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog("添加菜单项", "请输入菜单项标题:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int insertIndex = menuItemsListBox.SelectedIndex;
                    if (insertIndex == -1)
                        insertIndex = menuItems.Count - 1;
                    if (insertIndex < -1) insertIndex = -1;
                    
                    string newItem = dialog.InputText;
                    menuItems.Insert(insertIndex + 1, newItem);
                    menuItemsListBox.Items.Insert(insertIndex + 1, newItem);
                    
                    // 如果是用户菜单，创建新的MenuInfo对象
                    if (currentMenuType == "usermenu")
                    {
                        MenuInfo newMenuInfo = new MenuInfo
                        {
                            Menu = newItem,
                            Cmd = "%COMMANDER_PATH%\\Tools\\Swoff.exe", // 默认命令
                            Param = "/锁定 /wait:0" // 默认参数
                        };
                        
                        // 在相应位置插入新的MenuInfo
                        userMenuInfos.Insert(insertIndex + 1, newMenuInfo);
                        usermenu_changed = true;
                    }
                    else
                    {
                        mainmenu_changed = true;
                    }
                    
                    menuItemsListBox.SelectedIndex = insertIndex + 1;
                }
            }
        }

        private void AddSubmenuButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog("添加子菜单", "请输入子菜单标题:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int insertIndex = menuItemsListBox.SelectedIndex;
                    if (insertIndex == -1)
                        insertIndex = menuItems.Count - 1;

                    string startItem, endItem;
                    startItem = $"-{dialog.InputText}";
                    endItem = "--";
                    if (insertIndex < -1) insertIndex = -1;
                    menuItems.Insert(insertIndex + 1, startItem);
                    menuItems.Insert(insertIndex + 2, endItem);
                    menuItemsListBox.Items.Insert(insertIndex + 1, startItem);
                    menuItemsListBox.Items.Insert(insertIndex + 2, endItem);
                    menuItemsListBox.SelectedIndex = insertIndex + 1;
					if (currentMenuType == "mainmenu")
						mainmenu_changed = true;
					else
						usermenu_changed = true;
				}
            }
        }

        private void DeleteItemButton_Click(object sender, EventArgs e)
        {
            if (menuItemsListBox.SelectedIndex == -1) return;

            int selectedIndex = menuItemsListBox.SelectedIndex;
            string selectedItem = menuItems[selectedIndex];

			if (selectedItem.Equals("--"))
			{
				// 删除子菜单结束项及其起始项
				int startIndex = FindSubmenuStart(selectedIndex);
				if (startIndex != -1)
				{
					menuItems.RemoveAt(selectedIndex);
					menuItems.RemoveAt(startIndex);
					menuItemsListBox.Items.RemoveAt(selectedIndex);
					menuItemsListBox.Items.RemoveAt(startIndex);
                    
                    // 如果是用户菜单，同时删除对应的MenuInfo
                    if (currentMenuType == "usermenu")
                    {
                        if (selectedIndex < userMenuInfos.Count)
                            userMenuInfos.RemoveAt(selectedIndex);
                        if (startIndex < userMenuInfos.Count)
                            userMenuInfos.RemoveAt(startIndex);
                    }
				}
			}
			else if (selectedItem.StartsWith("-") && selectedItem.Length != 1) // - is separator, so exclude it
            {
                // 删除子菜单起始项及其结束项
                int endIndex = FindSubmenuEnd(selectedIndex);
                if (endIndex != -1)
                {
                    menuItems.RemoveAt(endIndex);
                    menuItems.RemoveAt(selectedIndex);
                    menuItemsListBox.Items.RemoveAt(endIndex);
                    menuItemsListBox.Items.RemoveAt(selectedIndex);
                    
                    // 如果是用户菜单，同时删除对应的MenuInfo
                    if (currentMenuType == "usermenu")
                    {
                        if (endIndex < userMenuInfos.Count)
                            userMenuInfos.RemoveAt(endIndex);
                        if (selectedIndex < userMenuInfos.Count)
                            userMenuInfos.RemoveAt(selectedIndex);
                    }
                }
            }
            else
            {
                // 删除普通菜单项
                menuItems.RemoveAt(selectedIndex);
                menuItemsListBox.Items.RemoveAt(selectedIndex);
                
                // 如果是用户菜单，同时删除对应的MenuInfo
                if (currentMenuType == "usermenu" && selectedIndex < userMenuInfos.Count)
                {
                    userMenuInfos.RemoveAt(selectedIndex);
                }
            }
			if (currentMenuType == "mainmenu")
				mainmenu_changed = true;
			else
				usermenu_changed = true;
		}

        private void EditTitleButton_Click(object sender, EventArgs e)
        {
            if (menuItemsListBox.SelectedIndex == -1) return;

            int selectedIndex = menuItemsListBox.SelectedIndex;
            string selectedItem = menuItems[selectedIndex];

            using (var dialog = new InputDialog("编辑标题", "请输入新标题:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string newTitle = dialog.InputText;
                    if (selectedItem.StartsWith("-") && !selectedItem.StartsWith("--"))
                    {
                        string newItem = $"-{newTitle}";
                        menuItems[selectedIndex] = newItem;
                        menuItemsListBox.Items[selectedIndex] = newItem;
                    }
                    else if (!selectedItem.StartsWith("--"))
                    {
                        menuItems[selectedIndex] = newTitle;
                        menuItemsListBox.Items[selectedIndex] = newTitle;
                        
                        // 如果是用户菜单，同时更新对应MenuInfo的Menu属性
                        if (currentMenuType == "usermenu" && selectedIndex < userMenuInfos.Count)
                        {
                            userMenuInfos[selectedIndex].Menu = newTitle;
                        }
                    }
					if (currentMenuType == "mainmenu")
						mainmenu_changed = true;
					else
						usermenu_changed = true;
				}
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // 当文本框内容变化时，实时更新userMenuInfos
            int selectedIndex = menuItemsListBox.SelectedIndex;
            if (currentMenuType == "usermenu" && selectedIndex >= 0 && selectedIndex < userMenuInfos.Count)
            {
                if (sender == cmdTextBox)
                {
                    userMenuInfos[selectedIndex].Cmd = cmdTextBox.Text;
                }
                else if (sender == paramTextBox)
                {
                    userMenuInfos[selectedIndex].Param = paramTextBox.Text;
                }
                else if (sender == pathTextBox)
                {
                    userMenuInfos[selectedIndex].Path = pathTextBox.Text;
                }
                usermenu_changed = true;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (usermenu_changed)
            {
                // 保存用户菜单
                var userSection = mainForm.userConfigLoader.GetConfigSection("User");
                if (userSection != null)
                {
                    userSection.Items.Clear();
                    
                    // 不需要在这里更新当前选中项的值，因为已经在TextChanged事件中实时更新了
                    
                    // 按照指定格式保存配置
                    for (int i = 0; i < userMenuInfos.Count; i++)
                    {
                        int menuNumber = i + 1;
                        var menuInfo = userMenuInfos[i];
                        
                        // 添加menu项
                        userSection.Items.Add(new ConfigItem { 
                            Key = $"menu{menuNumber}", 
                            Value = menuInfo.Menu 
                        });
                        
                        // 添加cmd项
                        userSection.Items.Add(new ConfigItem { 
                            Key = $"cmd{menuNumber}", 
                            Value = menuInfo.Cmd 
                        });
                        
                        // 添加param项
                        userSection.Items.Add(new ConfigItem { 
                            Key = $"param{menuNumber}", 
                            Value = menuInfo.Param 
                        });
                    }                    
                    mainForm.userConfigLoader.SaveConfig();
				}
            }
            //else
			if(mainmenu_changed)
			{
                // 保存主菜单
                var menu = mainForm.configLoader.FindConfigValue("Configuration", "Mainmenu");
                string menuFilePath = Constants.ZfileCfgPath + menu;
                try
                {
                    List<string> mainMenuItems = new List<string>();
                    int selectedIndex = menuItemsListBox.SelectedIndex;
                    
                    for (int i = 0; i < menuItems.Count; i++)
                    {
                        string item = menuItems[i];
                        if (item == "-")
                            mainMenuItems.Add("SEPARATOR");
                        else if (item.StartsWith("-") && !item.StartsWith("--"))
                            mainMenuItems.Add($"POPUP \"{item.Substring(1)}\"");
                        else if (item == "--")
                            mainMenuItems.Add("END_POPUP");
                        else if (item.Contains("startmenu", StringComparison.OrdinalIgnoreCase)||item.Contains("help_break", StringComparison.OrdinalIgnoreCase))
							mainMenuItems.Add($"{item}");
						else
							mainMenuItems.Add($"MENUITEM \"{item}\", {(selectedIndex == i ? cmdTextBox.Text : mainMenuInfos[i].Cmd)}");
                    }

                    File.WriteAllLines(menuFilePath, mainMenuItems.ToArray(), Encoding.GetEncoding("GB2312"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
		}

		private int FindSubmenuEnd(int startIndex)
        {
            int nestedLevel = 1;
            for (int i = startIndex + 1; i < menuItems.Count; i++)
            {
                string item = menuItems[i];
                if (item.StartsWith("-") && !item.StartsWith("--"))
                {
                    nestedLevel++;
                }
                else if (item.StartsWith("--"))
                {
                    nestedLevel--;
                    if (nestedLevel == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private int FindSubmenuStart(int endIndex)
        {
            int nestedLevel = 1;
            for (int i = endIndex - 1; i >= 0; i--)
            {
                string item = menuItems[i];
                if (item.StartsWith("--"))
                {
                    nestedLevel++;
                }
                else if (item.StartsWith("-") && !item.StartsWith("--"))
                {
                    nestedLevel--;
                    if (nestedLevel == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    public class InputDialog : Form
    {
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;
        private Label label;

        public string InputText => textBox.Text;

        public InputDialog(string title, string prompt)
        {
            Text = title;
            Size = new Size(300, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            label = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(260, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(260, 20)
            };

            okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 70),
                Size = new Size(75, 30)
            };

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(195, 70),
                Size = new Size(75, 30)
            };

            Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}