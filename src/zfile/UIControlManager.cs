using Sheng.Winform.Controls;
using System.Text;

namespace WinFormsApp1
{
	public class UIControlManager
    {
        private readonly Form1 form;
        private readonly ImageList treeViewImageList;
        private readonly SplitContainer mainContainer;
        private readonly SplitContainer leftPanel;
        private readonly SplitContainer rightPanel;
        private readonly Panel leftUpperPanel;
        private readonly Panel rightUpperPanel;
        private readonly Panel leftDrivePanel;
        private readonly Panel rightDrivePanel;
        private readonly ComboBox leftDriveBox;
        private readonly ComboBox rightDriveBox;
        private readonly TreeView leftTree;
        private readonly ListView leftList;
        private readonly TextBox leftPreview;
        
        private readonly TreeView rightTree;
        private readonly ListView rightList;
        private readonly TextBox rightPreview;
        
        private readonly ShengAddressBarStrip leftPathTextBox;
        private readonly ShengAddressBarStrip rightPathTextBox;
        private readonly StatusStrip leftStatusStrip;
        private readonly StatusStrip rightStatusStrip;
        private readonly SplitContainer leftTreeListSplitter;
        private readonly SplitContainer rightTreeListSplitter;
		public readonly FlowLayoutPanel leftBookmarkPanel = new();
		public readonly FlowLayoutPanel rightBookmarkPanel = new();
		public MenuStrip dynamicMenuStrip = new();
		public ToolStrip dynamicToolStrip = new();
		public UIControlManager(Form1 form, 
            SplitContainer mainContainer,
            SplitContainer leftPanel,
            SplitContainer rightPanel,
            Panel leftUpperPanel,
            Panel rightUpperPanel,
            Panel leftDrivePanel,
            Panel rightDrivePanel,
            ComboBox leftDriveBox,
            ComboBox rightDriveBox,
            TreeView leftTree,
            ListView leftList,
            TextBox leftPreview,
            TreeView rightTree,
            ListView rightList,
            TextBox rightPreview,
            ShengAddressBarStrip leftPathTextBox,
            ShengAddressBarStrip rightPathTextBox,
            StatusStrip leftStatusStrip,
            StatusStrip rightStatusStrip,
            SplitContainer leftTreeListSplitter,
            SplitContainer rightTreeListSplitter)
        {
            this.form = form;
            this.mainContainer = mainContainer;
            this.leftPanel = leftPanel;
            this.rightPanel = rightPanel;
            this.leftUpperPanel = leftUpperPanel;
            this.rightUpperPanel = rightUpperPanel;
            this.leftDrivePanel = leftDrivePanel;
            this.rightDrivePanel = rightDrivePanel;
            this.leftDriveBox = leftDriveBox;
            this.rightDriveBox = rightDriveBox;
            this.leftTree = leftTree;
            this.leftList = leftList;
            this.leftPreview = leftPreview;
            this.rightTree = rightTree;
            this.rightList = rightList;
            this.rightPreview = rightPreview;
            this.leftPathTextBox = leftPathTextBox;
            this.rightPathTextBox = rightPathTextBox;
            this.leftStatusStrip = leftStatusStrip;
            this.rightStatusStrip = rightStatusStrip;
            this.leftTreeListSplitter = leftTreeListSplitter;
            this.rightTreeListSplitter = rightTreeListSplitter;

            treeViewImageList = new ImageList();
            treeViewImageList.ImageSize = new Size(16, 16);
        }

        public void InitializeLayout()
        {
            int topHeight = 0;
            Panel containerPanel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, topHeight, 0, 0)
            };
            form.Controls.Add(containerPanel);

            mainContainer.Dock = DockStyle.Fill;
            mainContainer.Orientation = Orientation.Vertical;

            int halfWidth = (form.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            mainContainer.SplitterDistance = halfWidth;
            mainContainer.SplitterMoved += MainContainer_SplitterMoved;

            containerPanel.Controls.Add(mainContainer);

            ConfigurePanel(leftPanel, mainContainer.Panel1);
            ConfigurePanel(rightPanel, mainContainer.Panel2);

            ConfigureUpperPanel(leftUpperPanel, leftDrivePanel, leftPanel.Panel1);
            ConfigureUpperPanel(rightUpperPanel, rightDrivePanel, rightPanel.Panel1);
        }

        private void MainContainer_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            int halfWidth = (form.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            if (Math.Abs(mainContainer.SplitterDistance - halfWidth) > 5)
            {
                mainContainer.SplitterDistance = halfWidth;
            }
        }

        private void ConfigurePanel(SplitContainer panel, Control parent)
        {
            panel.Dock = DockStyle.Fill;
            panel.Orientation = Orientation.Horizontal;
            panel.SplitterDistance = (int)((parent.Width) * 0.5);
            parent.Controls.Add(panel);
        }

        private void ConfigureUpperPanel(Panel upperPanel, Panel drivePanel, Control parent)
        {
            upperPanel.Dock = DockStyle.Fill;
            upperPanel.Padding = new Padding(0, 30, 0, 0);

            drivePanel.Dock = DockStyle.Top;
            drivePanel.Height = 30;

            parent.Controls.Add(upperPanel);
            parent.Controls.Add(drivePanel);
            drivePanel.BringToFront();
        }

        public void InitializeDriveComboBoxes()
        {
            ConfigureDriveBox(leftDriveBox, leftDrivePanel, leftPathTextBox);
            ConfigureDriveBox(rightDriveBox, rightDrivePanel, rightPathTextBox);

            var rootNode = new ShengFileSystemNode();
            leftPathTextBox.InitializeRoot(rootNode);
            rightPathTextBox.InitializeRoot(rootNode);

            LoadDrives();
        }

        private void ConfigureDriveBox(ComboBox driveBox, Panel parent, ShengAddressBarStrip pathTextBox)
        {
            driveBox.Dock = DockStyle.Left;
            driveBox.DropDownStyle = ComboBoxStyle.DropDownList;
            driveBox.SelectedIndexChanged += DriveComboBox_SelectedIndexChanged;

            pathTextBox.Dock = DockStyle.Fill;

            parent.Controls.Add(pathTextBox);
            parent.Controls.Add(driveBox);
        }

        private void LoadDrives()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    leftDriveBox.Items.Add(drive.Name);
                    rightDriveBox.Items.Add(drive.Name);
                }
            }

            if (leftDriveBox.Items.Count > 0)
            {
                leftDriveBox.SelectedIndex = 0;
                rightDriveBox.SelectedIndex = 0;
            }
        }

        private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var treeView = comboBox == leftDriveBox ? leftTree : rightTree;
            var listView = comboBox == leftDriveBox ? leftList : rightList;

            if (comboBox.SelectedItem is string drivePath)
            {
                form.LoadDriveIntoTree(treeView, drivePath);
            }
        }

        public void InitializeTreeViews()
        {
            ConfigureTreeListSplitter(leftTreeListSplitter, leftUpperPanel, leftTree, leftList);
            ConfigureTreeListSplitter(rightTreeListSplitter, rightUpperPanel, rightTree, rightList);
        }

        private void ConfigureTreeListSplitter(SplitContainer splitter, Panel parent, TreeView treeView, ListView listView)
        {
            splitter.Dock = DockStyle.Fill;
            splitter.Orientation = Orientation.Vertical;
			// 设置合理的最小尺寸
			splitter.Panel1MinSize = 100;
			splitter.Panel2MinSize = 100;
			// 调用函数执行treeview绑定事件
			ConfigureTreeView(treeView);
			splitter.Panel1.Controls.Add(treeView);
            splitter.Panel2.Controls.Add(listView);
            parent.Controls.Add(splitter);
			// 设置初始分割位置
			if (parent.Width > 0)
			{
				int desiredDistance = parent.Width / 3;
				// 确保分割线位置在有效范围内
				splitter.SplitterDistance = Math.Max(
					splitter.Panel1MinSize,
					Math.Min(desiredDistance, parent.Width - splitter.Panel2MinSize)
				);
			}

			// 处理父容器大小改变事件
			parent.SizeChanged += (s, e) =>
			{
				if (parent.Width > 0)
				{
					int desiredDistance = parent.Width / 3;
					try
					{
						splitter.SplitterDistance = Math.Max(
							splitter.Panel1MinSize,
							Math.Min(desiredDistance, parent.Width - splitter.Panel2MinSize)
						);
					}
					catch (ArgumentException)
					{
						// 忽略可能的参数异常
					}
				}
			};
		}

        public void InitializeTreeViewIcons()
        {
            Icon folderIcon = Helper.GetIconByFileType("folder", false);
            if (folderIcon != null)
            {
                treeViewImageList.Images.Add("folder", folderIcon);
            }

            ConfigureTreeView(leftTree);
            ConfigureTreeView(rightTree);
        }

        public void ConfigureTreeView(TreeView treeView)
        {
            treeView.Dock = DockStyle.Fill;
            treeView.ShowLines = true;
            treeView.HideSelection = false;
            treeView.ShowPlusMinus = true;
            treeView.ShowRootLines = true;
            treeView.PathSeparator = "\\";
            treeView.FullRowSelect = true;
            treeView.ItemHeight = 20;
            treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView.ImageList = treeViewImageList;

            treeView.DrawNode += form.TreeView_DrawNode;
            treeView.MouseUp += form.TreeView_MouseUp;
            treeView.AfterSelect += form.TreeView_AfterSelect;
            treeView.NodeMouseClick += form.TreeView_NodeMouseClick;
            treeView.BeforeExpand += form.TreeView_BeforeExpand;
            treeView.MouseDown += form.TreeView_MouseDown;
        }

        public void InitializeListViews()
        {
            ConfigureListView(leftList, leftPanel.Panel2);
            ConfigureListView(rightList, rightPanel.Panel2);
        }

        private void ConfigureListView(ListView listView, Control parent)
        {
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.AllowColumnReorder = true;
            listView.LabelEdit = true;
            listView.MultiSelect = true;
            listView.HideSelection = false;
			listView.Sorting = SortOrder.Ascending;
			listView.Columns.Clear();
			listView.Columns.Add("名称", 250); // 新增图标列
			listView.Columns.Add("名称", 0); // 隐藏名称列
			listView.Columns.Add("大小", 100);
			listView.Columns.Add("类型", 80);
			listView.Columns.Add("修改日期", 150);
			// 添加双击事件
			listView.MouseDoubleClick += form.ListView_MouseDoubleClick;
			listView.ColumnClick += form.ListView_ColumnClick;
			listView.SelectedIndexChanged += form.ListView_SelectedIndexChanged;
			listView.MouseUp += form.ListView_MouseUp;
			listView.MouseDown += form.ListView_MouseDown;
			listView.MouseMove += form.ListView_MouseMove;
			parent.Controls.Add(listView);
			listView.BringToFront();
		}

		public void InitializePreviewPanels()
        {
            leftPreview.Dock = DockStyle.Fill;
            leftPreview.Multiline = true;
            leftPreview.ReadOnly = true;
            leftPreview.ScrollBars = ScrollBars.Both;
            leftPanel.Panel2.Controls.Add(leftPreview);

            rightPreview.Dock = DockStyle.Fill;
            rightPreview.Multiline = true;
            rightPreview.ReadOnly = true;
            rightPreview.ScrollBars = ScrollBars.Both;
            rightPanel.Panel2.Controls.Add(rightPreview);
        }

        public void InitializeStatusStrips()
        {
            leftStatusStrip.Dock = DockStyle.Bottom;
            leftPanel.Panel2.Controls.Add(leftStatusStrip);

            rightStatusStrip.Dock = DockStyle.Bottom;
            rightPanel.Panel2.Controls.Add(rightStatusStrip);
		}
		private ToolStripButton CreateToolStripButton(string text, Keys shortcutKeys, EventHandler onClick)
		{
			var button = new ToolStripButton
			{
				Text = $"{text} ({shortcutKeys})",
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			button.Click += onClick;
			return button;
		}
		public void InitializeToolStrip()
		{
			ToolStrip toolStrip = new ToolStrip
			{
				Dock = DockStyle.Bottom
			};

			// 添加按钮
			toolStrip.Items.Add(CreateToolStripButton("查看", Keys.F3, form.ViewButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("编辑", Keys.F4, form.EditButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("复制", Keys.F5, form.CopyButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("移动", Keys.F6, form.MoveButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("文件夹", Keys.F7, form.FolderButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("删除", Keys.F8, form.DeleteButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("终端", Keys.F9, form.TerminalButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("退出", Keys.Alt | Keys.X, form.ExitButton_Click));

			// 将工具栏添加到窗体
			form.Controls.Add(toolStrip);
		}
		public void InitializeBookmarkLists()
		{
			// 初始化左侧书签Panel
			leftBookmarkPanel.Dock = DockStyle.Top;
			leftBookmarkPanel.FlowDirection = FlowDirection.LeftToRight;
			leftBookmarkPanel.WrapContents = false;
			leftBookmarkPanel.AutoScroll = true;
			leftPanel.Panel2.Controls.Add(leftBookmarkPanel);

			// 初始化右侧书签Panel
			rightBookmarkPanel.Dock = DockStyle.Top;
			rightBookmarkPanel.FlowDirection = FlowDirection.LeftToRight;
			rightBookmarkPanel.WrapContents = false;
			rightBookmarkPanel.AutoScroll = true;
			rightPanel.Panel2.Controls.Add(rightBookmarkPanel);

			// 调整布局顺序
			leftPanel.Panel2.Controls.SetChildIndex(leftBookmarkPanel, 0);
			leftPanel.Panel2.Controls.SetChildIndex(leftPreview, 1);
			rightPanel.Panel2.Controls.SetChildIndex(rightBookmarkPanel, 0);
			rightPanel.Panel2.Controls.SetChildIndex(rightPreview, 1);
		}
		//从环境变量获取%COMMANDER_PATH%
		private string GetCommanderPath()
		{
			string commanderPath = Environment.GetEnvironmentVariable("COMMANDER_PATH") ?? string.Empty;
			//if (string.IsNullOrEmpty(commanderPath))
			{
				//MessageBox.Show("未设置COMMANDER_PATH环境变量", "warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//var bb = Environment.CurrentDirectory;
				//var cc = AppDomain.CurrentDomain.BaseDirectory;
				//var dd = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				return Directory.GetCurrentDirectory(); ;
			}
			return commanderPath;
		}


		public void ToolbarButton_Click(object? sender, EventArgs e)
		{
			if (sender is ToolStripItem item && item.Tag is string cmd)
			{
				MessageBox.Show($"执行命令: {cmd}", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
		public void InitializeDropdownMenu(ToolStripDropDownButton dropdownButton, string dropdownFilePath)
		{
			var commanderPath = GetCommanderPath();
			if (string.IsNullOrEmpty(commanderPath))
			{
				return;
			}
			dropdownFilePath = dropdownFilePath.ToUpper().Replace("%COMMANDER_PATH%", commanderPath + "\\..\\..\\..\\..\\config");
			if (!File.Exists(dropdownFilePath))
			{
				MessageBox.Show("下拉菜单配置文件不存在" + dropdownFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				using (StreamReader reader = new StreamReader(dropdownFilePath, Encoding.GetEncoding("GB2312")))
				{
					string? line;
					while ((line = reader.ReadLine()) != null)
					{
						line = line.Trim();
						if (line.StartsWith("button"))
						{
							string menuText = reader.ReadLine()?.Trim() ?? string.Empty;
							string cmd = reader.ReadLine()?.Trim() ?? string.Empty;

							ToolStripMenuItem menuItem = new ToolStripMenuItem
							{
								Text = menuText,
								Tag = cmd
							};
							menuItem.Click += ToolbarButton_Click;
							dropdownButton.DropDownItems.Add(menuItem);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载下拉菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public void InitializeDynamicToolbar()
		{
			string toolbarFilePath = Path.Combine(Constants.ZfilePath, "DEFAULT.BAR");
			if (!File.Exists(toolbarFilePath))
			{
				MessageBox.Show("工具栏配置文件不存在" + toolbarFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var zfile_path = Path.Combine(Constants.ZfilePath, "WCMIcon3.dll");
			var iconManager = form.iconManager;
			var iconList = iconManager.LoadIconsFromFile(zfile_path);
			var fileInfoList = new FileInfoList(new string[] { zfile_path });

			using (StreamReader reader = new StreamReader(toolbarFilePath, Encoding.GetEncoding("GB2312")))
			{
				dynamicToolStrip = new ToolStrip();
				string? line;
				int buttonCount = 0;
				int buttonIndex;
				string buttonIcon = "";
				string cmd = "";
				string menuText = "";
				string pathText = "";
				string iconic = "";
				string paramText = "";
				List<int> emptybuttons = new List<int>();

				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if (line.StartsWith("Buttoncount="))
					{
						buttonCount = int.Parse(line.Substring("Buttoncount=".Length));
						continue;
					}
					else if (line.StartsWith("iconic"))
					{
						var _buttonIndex = int.Parse(line.Substring(6, line.IndexOf('=') - 6));
						if (emptybuttons.Contains(_buttonIndex))  //如果emptybuttons中存在_buttonIndex，则跳过
							continue;
						iconic = line.Substring(line.IndexOf('=') + 1);
					}
					else if (line.StartsWith("cmd"))
					{
						int _buttonIndex = int.Parse(line.Substring(3, line.IndexOf('=') - 3));
						if (emptybuttons.Contains(_buttonIndex))
							continue;

						cmd = line.Substring(line.IndexOf('=') + 1);
					}
					else if (line.StartsWith("menu"))
					{
						int _buttonIndex = int.Parse(line.Substring(4, line.IndexOf('=') - 4));
						if (emptybuttons.Contains(_buttonIndex))
							continue;
						menuText = line.Substring(line.IndexOf('=') + 1);
					}
					else if (line.StartsWith("path"))
					{
						int _buttonIndex = int.Parse(line.Substring(4, line.IndexOf('=') - 4));
						if (emptybuttons.Contains(_buttonIndex))
							continue;
						pathText = line.Substring(line.IndexOf('=') + 1);
					}
					else if (line.StartsWith("param"))
					{
						int _buttonIndex = int.Parse(line.Substring(5, line.IndexOf('=') - 5));
						if (emptybuttons.Contains(_buttonIndex))
							continue;
						paramText = line.Substring(line.IndexOf('=') + 1);
					}
					else if (line.StartsWith("button"))
					{
						if (!cmd.Equals(""))
						{
							var zhdesc = form.cmdProcessor.cmdTable.GetByCmdName(cmd)?.ZhDesc ?? "";
							ToolStripButton button = new ToolStripButton
							{
								Text = menuText,
								ToolTipText = zhdesc,
								Image = iconManager.LoadIcon(buttonIcon),
								Tag = cmd
							};

							if (cmd.StartsWith("openbar"))
							{
								string dropdownFilePath = cmd.Substring("openbar ".Length);
								ToolStripDropDownButton dropdownButton = new ToolStripDropDownButton
								{
									Text = menuText,
									ToolTipText = menuText,
									Image = iconManager.LoadIcon(buttonIcon)
								};
								InitializeDropdownMenu(dropdownButton, dropdownFilePath);
								dynamicToolStrip.Items.Add(dropdownButton);
							}
							else
							{
								button.Click += ToolbarButton_Click;
								dynamicToolStrip.Items.Add(button);
							}
							menuText = "";
							pathText = "";
							cmd = "";
							iconic = "";
							paramText = "";
						}

						buttonIndex = int.Parse(line.Substring(6, line.IndexOf('=') - 6));
						buttonIcon = line.Substring(line.IndexOf('=') + 1);
						//如果buttonIcon为空，则读取下一行，并记录当前buttonIndex,忽略下面所有编号为buttonIndex的行
						if (string.IsNullOrEmpty(buttonIcon))
						{
							emptybuttons.Add(buttonIndex);
							continue;
						}
					}
				}

				form.Controls.Add(dynamicToolStrip);
			}


		}
		public void InitializeDynamicMenu()
		{
			string menuFilePath = "C:\\Users\\zhouy\\source\\repos\\WinFormsApp1\\src\\config\\WCMD_CHN.MNU";
			if (!File.Exists(menuFilePath))
			{
				MessageBox.Show("菜单文件不存在" + menuFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				using (StreamReader reader = new StreamReader(menuFilePath, Encoding.GetEncoding("GB2312")))
				{
					dynamicMenuStrip = new MenuStrip();
					Stack<ToolStripMenuItem> menuStack = new Stack<ToolStripMenuItem>();
					ToolStripMenuItem? currentPopup = null;

					string? line;
					while ((line = reader.ReadLine()) != null)
					{
						line = line.Trim();
						if (line.StartsWith("POPUP"))
						{
							string menuItemText = line.Substring(6).Trim().Trim('"');
							ToolStripMenuItem menuItem = new ToolStripMenuItem(menuItemText);
							if (menuStack.Count > 0)
							{
								menuStack.Peek().DropDownItems.Add(menuItem);
							}
							else
							{
								dynamicMenuStrip.Items.Add(menuItem);
							}
							menuStack.Push(menuItem);
							currentPopup = menuItem;
						}
						else if (line.StartsWith("END_POPUP"))
						{
							if (menuStack.Count > 0)
							{
								menuStack.Pop();
							}
							currentPopup = menuStack.Count > 0 ? menuStack.Peek() : null;
						}
						else if (currentPopup != null)
						{
							line = line.TrimStart().Substring(9);
							if (line.StartsWith("SEPARATOR"))
							{
								if (currentPopup != null)
								{
									currentPopup.DropDownItems.Add(new ToolStripSeparator());
								}
							}
							else
							{
								ToolStripMenuItem menuItem = new ToolStripMenuItem(line);
								menuItem.Click += form.MenuItem_Click;
								currentPopup.DropDownItems.Add(menuItem);
							}
						}
					}

					form.MainMenuStrip = dynamicMenuStrip;
					form.Controls.Add(dynamicMenuStrip);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
} 