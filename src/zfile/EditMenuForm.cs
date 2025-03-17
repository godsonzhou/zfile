using System.Text;
using System.Windows.Forms;

namespace zfile
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
        private Form1 mainForm;
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

        public EditMenuForm(Form1 form, int menuid)
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
            this.Text = "编辑菜单";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;

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

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] {
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
                    {
                        str.Add(item.Key + "=" + item.Value);
                    }
                    var ms = Helper.GetMenuInfoFromList(str.ToArray());
                    foreach (var m in ms)
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

                    if (isNotEndItem)
                    {
						//string[] parts = selectedItem.Split('=');
						//if (parts.Length == 2)
						if( mainForm.uiManager.usermenuMap.TryGetValue(selectedItem, out var menuinfo))
                        {
                            cmdTextBox.Text = menuinfo.Cmd;
                            paramTextBox.Text = menuinfo.Param;
                            pathTextBox.Text = menuinfo.Path;
                        }
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

                    string newItem = dialog.InputText;
                    if (insertIndex < -1) insertIndex = -1;
                    menuItems.Insert(insertIndex + 1, newItem);
                    menuItemsListBox.Items.Insert(insertIndex + 1, newItem);
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
                }
            }
            else
            {
                // 删除普通菜单项
                menuItems.RemoveAt(selectedIndex);
                menuItemsListBox.Items.RemoveAt(selectedIndex);
            }
         
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
                    if (currentMenuType == "usermenu")
                    {
                        if (selectedItem.Contains("="))
                        {
                            string[] parts = selectedItem.Split('=');
                            if (parts.Length == 2)
                            {
                                if (selectedItem.StartsWith("-") && !selectedItem.StartsWith("--"))
                                {
                                    string newItem = $"-{newTitle}";
                                    menuItems[selectedIndex] = newItem;
                                    menuItemsListBox.Items[selectedIndex] = newItem;
                                }
                                else if (!selectedItem.StartsWith("--"))
                                {
                                    string newItem = $"{newTitle}";
                                    menuItems[selectedIndex] = newItem;
                                    menuItemsListBox.Items[selectedIndex] = newItem;
                                }
                            }
                        }
                        else
                        {
                            if (selectedItem.StartsWith("-") && !selectedItem.StartsWith("--"))
                            {
                                string newItem = $"-{newTitle}";
                                menuItems[selectedIndex] = newItem;
                                menuItemsListBox.Items[selectedIndex] = newItem;
                            }
                            else if (!selectedItem.StartsWith("--"))
                            {
                                string newItem = $"{newTitle}";
                                menuItems[selectedIndex] = newItem;
                                menuItemsListBox.Items[selectedIndex] = newItem;
                            }
                        }
                    }
                    else
                    {
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
                        }
                    }
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SaveMenuItems();
        }

        private void SaveMenuItems()
        {
            if (currentMenuType == "usermenu")
            {
                // 保存用户菜单
                var userSection = mainForm.userConfigLoader.GetConfigSection("User");
                if (userSection != null)
                {
                    userSection.Items.Clear();
                    for (int i = 0; i < menuItems.Count; i++)
                    {
                        string item = menuItems[i];
                        if (item.StartsWith("--"))
                            continue;

                        string menuText = item;
                        string cmdLine = "cmd";

                        if (item.Contains("="))
                        {
                            string[] parts = item.Split('=');
                            if (parts.Length == 2)
                            {
                                menuText = parts[0];
                                cmdLine = parts[1];
                            }
                        }

                        if (menuItemsListBox.SelectedIndex >= 0 && i == menuItemsListBox.SelectedIndex)
                        {
                            // 使用当前输入框的值
                            var cmdParts = new List<string>();
                            if (!string.IsNullOrEmpty(cmdTextBox.Text))
                                cmdParts.Add(cmdTextBox.Text);
                            if (!string.IsNullOrEmpty(paramTextBox.Text))
                                cmdParts.Add(paramTextBox.Text);
                            if (!string.IsNullOrEmpty(pathTextBox.Text))
                                cmdParts.Add(pathTextBox.Text);

                            cmdLine = string.Join(" ", cmdParts);
                            if (string.IsNullOrEmpty(cmdLine))
                                cmdLine = "cmd";
                        }

                        userSection.Items.Add(new ConfigItem { Key = menuText, Value = cmdLine });
                    }
                    mainForm.userConfigLoader.SaveConfig();
                }
            }
            else
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
                        else
                            mainMenuItems.Add($"MENUITEM \"{item}\", {(selectedIndex == i ? cmdTextBox.Text : "cm_test")}");
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
            this.Text = title;
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

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

            this.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
} 