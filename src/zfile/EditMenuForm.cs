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
        private string currentMenuType = "usermenu";
        private List<string> menuItems = new();
		private int menu_id;	//0-usermenu, 1-mainmenu

        public EditMenuForm(Form1 form, int menuid)
        {
            mainForm = form;
			menu_id = menuid;
            InitializeComponent();
            LoadMenuItems();
        }

        private void InitializeComponent()
        {
            this.Text = "编辑菜单";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            // 创建菜单类型选择ComboBox
            menuTypeComboBox = new ComboBox
            {
                Location = new Point(10, 10),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            menuTypeComboBox.Items.AddRange(new string[] { "usermenu", "mainmenu" });
            menuTypeComboBox.SelectedIndex = menu_id;
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
                Location = new Point(290, 340),
                Size = new Size(75, 30)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(375, 340),
                Size = new Size(75, 30)
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] {
                menuTypeComboBox,
                menuItemsListBox,
                addItemButton,
                addSubmenuButton,
                deleteItemButton,
                editTitleButton,
                okButton,
                cancelButton
            });
        }

        private void LoadMenuItems()
        {
            menuItems.Clear();
            menuItemsListBox.Items.Clear();

            if (currentMenuType == "usermenu")
            {
                // 加载用户菜单
                var userMenu = mainForm.userConfigLoader.GetConfigSection("User");
                if (userMenu != null)
                {
                    //foreach (var item in userMenu.Items)
                    //{
                    //    menuItems.Add(item.Key + "=" + item.Value);
                    //    menuItemsListBox.Items.Add(item.Value);
                    //}
					List<string> str = new();
					foreach (var i in userMenu.Items)
						str.Add(i.Key + "=" + i.Value);
					var ms = Helper.GetMenuInfoFromList(str.ToArray());
					foreach (var m in ms)
					{
						//var ddi = new ToolStripMenuItem(m.Menu);
						//ddi.Tag = m.Cmd + " " + m.Param;
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
            currentMenuType = menuTypeComboBox.SelectedItem.ToString() ?? "usermenu";
            LoadMenuItems();
        }

        private void MenuItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (menuItemsListBox.SelectedItem != null)
            {
                string selectedItem = menuItemsListBox.SelectedItem.ToString() ?? "";
                editTitleButton.Enabled = !selectedItem.StartsWith("--");
            }
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            int insertIndex = menuItemsListBox.SelectedIndex;
            if (insertIndex == -1) insertIndex = menuItemsListBox.Items.Count - 1;

            string newItem;
            if (currentMenuType == "usermenu")
            {
                newItem = "新菜单项=cmd";
                menuItems.Insert(insertIndex + 1, newItem);
                menuItemsListBox.Items.Insert(insertIndex + 1, "新菜单项");
            }
            else
            {
                newItem = "MENUITEM \"新菜单项\", cm_test";
                menuItems.Insert(insertIndex + 1, newItem);
                menuItemsListBox.Items.Insert(insertIndex + 1, newItem);
            }
            menuItemsListBox.SelectedIndex = insertIndex + 1;
        }

        private void AddSubmenuButton_Click(object sender, EventArgs e)
        {
            int insertIndex = menuItemsListBox.SelectedIndex;
            if (insertIndex == -1) insertIndex = menuItemsListBox.Items.Count - 1;

            if (currentMenuType == "usermenu")
            {
                string startItem = "-子菜单=cmd";
                string endItem = "--=";
                menuItems.Insert(insertIndex + 1, startItem);
                menuItems.Insert(insertIndex + 2, endItem);
                menuItemsListBox.Items.Insert(insertIndex + 1, "-子菜单");
                menuItemsListBox.Items.Insert(insertIndex + 2, "--");
            }
            else
            {
                string startItem = "POPUP \"-子菜单\"";
                string endItem = "END_POPUP";
                menuItems.Insert(insertIndex + 1, startItem);
                menuItems.Insert(insertIndex + 2, endItem);
                menuItemsListBox.Items.Insert(insertIndex + 1, startItem);
                menuItemsListBox.Items.Insert(insertIndex + 2, endItem);
            }
            menuItemsListBox.SelectedIndex = insertIndex + 1;
        }

        private void DeleteItemButton_Click(object sender, EventArgs e)
        {
            if (menuItemsListBox.SelectedIndex == -1) return;

            int selectedIndex = menuItemsListBox.SelectedIndex;
            string selectedItem = menuItemsListBox.SelectedItem.ToString() ?? "";

            if (currentMenuType == "usermenu")
            {
                if (selectedItem.StartsWith("-") && !selectedItem.StartsWith("--"))
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
                else if (selectedItem.StartsWith("--"))
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
                else
                {
                    // 删除普通菜单项
                    menuItems.RemoveAt(selectedIndex);
                    menuItemsListBox.Items.RemoveAt(selectedIndex);
                }
            }
            else
            {
                if (selectedItem.StartsWith("POPUP"))
                {
                    // 删除子菜单起始项及其结束项
                    int endIndex = FindMainMenuEnd(selectedIndex);
                    if (endIndex != -1)
                    {
                        menuItems.RemoveAt(endIndex);
                        menuItems.RemoveAt(selectedIndex);
                        menuItemsListBox.Items.RemoveAt(endIndex);
                        menuItemsListBox.Items.RemoveAt(selectedIndex);
                    }
                }
                else if (selectedItem.StartsWith("END_POPUP"))
                {
                    // 删除子菜单结束项及其起始项
                    int startIndex = FindMainMenuStart(selectedIndex);
                    if (startIndex != -1)
                    {
                        menuItems.RemoveAt(selectedIndex);
                        menuItems.RemoveAt(startIndex);
                        menuItemsListBox.Items.RemoveAt(selectedIndex);
                        menuItemsListBox.Items.RemoveAt(startIndex);
                    }
                }
                else
                {
                    // 删除普通菜单项
                    menuItems.RemoveAt(selectedIndex);
                    menuItemsListBox.Items.RemoveAt(selectedIndex);
                }
            }
        }

        private void EditTitleButton_Click(object sender, EventArgs e)
        {
            if (menuItemsListBox.SelectedIndex == -1) return;

            int selectedIndex = menuItemsListBox.SelectedIndex;
            string selectedItem = menuItems[selectedIndex];

            using (var inputDialog = new InputDialog("更改标题", "请输入新标题:"))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string newTitle = inputDialog.InputText;
                    if (currentMenuType == "usermenu")
                    {
                        string[] parts = selectedItem.Split('=');
                        if (parts.Length == 2)
                        {
                            if (selectedItem.StartsWith("-") && !selectedItem.StartsWith("--"))
                            {
                                newTitle = "-" + newTitle;
                            }
                            string newItem = $"{newTitle}={parts[1]}";
                            menuItems[selectedIndex] = newItem;
                            menuItemsListBox.Items[selectedIndex] = newTitle;
                        }
                    }
                    else
                    {
                        if (selectedItem.StartsWith("POPUP"))
                        {
                            string newItem = $"POPUP \"{newTitle}\"";
                            menuItems[selectedIndex] = newItem;
                            menuItemsListBox.Items[selectedIndex] = newItem;
                        }
                        else if (selectedItem.StartsWith("MENUITEM"))
                        {
                            string[] parts = selectedItem.Split(',');
                            if (parts.Length == 2)
                            {
                                string newItem = $"MENUITEM \"{newTitle}\",{parts[1]}";
                                menuItems[selectedIndex] = newItem;
                                menuItemsListBox.Items[selectedIndex] = newItem;
                            }
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
                    foreach (string item in menuItems)
                    {
                        string[] parts = item.Split('=');
                        if (parts.Length == 2)
                        {
                            //userSection.Items[parts[0]] = parts[1];
                        }
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
                    File.WriteAllLines(menuFilePath, menuItems, Encoding.GetEncoding("GB2312"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存菜单文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int FindSubmenuEnd(int startIndex)
        {
            for (int i = startIndex + 1; i < menuItems.Count; i++)
            {
                if (menuItemsListBox.Items[i].ToString()?.StartsWith("--") == true)
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindSubmenuStart(int endIndex)
        {
            for (int i = endIndex - 1; i >= 0; i--)
            {
                string item = menuItemsListBox.Items[i].ToString() ?? "";
                if (item.StartsWith("-") && !item.StartsWith("--"))
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindMainMenuEnd(int startIndex)
        {
            for (int i = startIndex + 1; i < menuItems.Count; i++)
            {
                if (menuItems[i].StartsWith("END_POPUP"))
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindMainMenuStart(int endIndex)
        {
            for (int i = endIndex - 1; i >= 0; i--)
            {
                if (menuItems[i].StartsWith("POPUP"))
                {
                    return i;
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