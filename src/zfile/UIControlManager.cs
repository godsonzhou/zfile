using SharpCompress.Compressors.Explode;
using Sheng.Winform.Controls;
using System.Diagnostics;
using System.Text;
using WinShell;
using static System.Net.Mime.MediaTypeNames;
namespace WinFormsApp1
{
	public struct ToolbarButton
	{
		public string name;
		public string cmd;
		public string icon;
		public string path;
		public string param;
		public string iconic;
		public ToolbarButton(string _name, string _cmd, string _icon, string _path, string _param, string _iconic)
		{
			name = _name;
			cmd = _cmd;
			icon = _icon;
			path = _path;
			param = _param;
			iconic = _iconic;
		}
	}
	public class ToolbarManager : IDisposable
	{
		private Form1 form;
		//private UIControlManager uiControlManager;

		private ToolStrip dynamicToolStrip;
		public ToolStrip DynamicToolStrip => dynamicToolStrip;
		public List<ToolbarButton> toolbarButtons = new List<ToolbarButton>();
		public int ButtonCount => toolbarButtons.Count;
		private string configfile;
		// 添加上下文菜单属性
		private readonly ContextMenuStrip buttonContextMenu;
		private ToolStripButton? currentButton;
		private bool disposed = false;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 取消事件订阅
					dynamicToolStrip.DragEnter -= form.ToolbarButton_DragEnter;
					dynamicToolStrip.DragDrop -= form.ToolbarButton_DragDrop;
					// 取消DriveBox事件订阅
					// 取消所有按钮的事件订阅
					foreach (ToolStripItem item in dynamicToolStrip.Items)
					{
						if (item is ToolStripButton button)
						{
							button.MouseUp -= Button_MouseUp;
						}
						else if (item is ToolStripDropDownButton dropDownButton)
						{
							dropDownButton.MouseUp -= Button_MouseUp;
						}
					}
					// 释放上下文菜单
					buttonContextMenu.Dispose();
				}

				// 释放非托管资源
				disposed = true;
			}
		}

		~ToolbarManager()
		{
			Dispose(false);
		}
		public ToolbarManager(Form1 form, string configfile, bool isVertical)
		{
			// 加载配置文件中的工具栏按钮信息并初始化控件,实现逻辑参照 initializeDynamicToolbar
			dynamicToolStrip = new ToolStrip();
			this.form = form;
			this.configfile = configfile;
			// 初始化上下文菜单
			buttonContextMenu = new ContextMenuStrip();
			var deleteItem = new ToolStripMenuItem("删除按钮");
			var copyItem = new ToolStripMenuItem("复制按钮");

			deleteItem.Click += DeleteButton_Click;
			copyItem.Click += CopyButton_Click;

			buttonContextMenu.Items.Add(deleteItem);
			buttonContextMenu.Items.Add(copyItem);

			Init(configfile);
			GenerateDynamicToolbar();

			if (isVertical)
			{
				dynamicToolStrip.Dock = DockStyle.Left;
				dynamicToolStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
				//form.uiManager.MainContainer.Panel2.Controls.Add(dynamicToolStrip);
				//将vertical dynamictoolstrip移动到rightuppanel的左边
				form.uiManager.RightUpperPanel.Controls.Add(dynamicToolStrip);
			}
			else
			{
				form.Controls.Add(dynamicToolStrip);
			}
			dynamicToolStrip.AllowDrop = true;
			dynamicToolStrip.DragEnter += form.ToolbarButton_DragEnter;
			dynamicToolStrip.DragDrop += form.ToolbarButton_DragDrop;
		}
	
		public void AddButton(string name, string cmd, string icon, string path, string param, string iconic)
		{
			toolbarButtons.Add(new ToolbarButton(name, cmd, icon, path, param, iconic));
		}
		public void RemoveButton(int index)
		{
			toolbarButtons.RemoveAt(index);
		}
		public void RemoveButton(string name)
		{
			if (string.IsNullOrEmpty(name)) return;

			// 删除指定名称的所有工具栏按钮
			toolbarButtons.RemoveAll(b => b.name == name);
		}
		public void SaveToconfig()
		{
			try
			{
				string configPath = Path.Combine(Constants.ZfileCfgPath, configfile);
				using (StreamWriter writer = new StreamWriter(configPath, false, Encoding.GetEncoding("GB2312")))
				{
					writer.WriteLine("[Buttonbar]");
					writer.WriteLine($"Buttoncount={toolbarButtons.Count}");

					for (int i = 0; i < toolbarButtons.Count; i++)
					{
						int buttonNumber = i + 1;
						ToolbarButton button = toolbarButtons[i];

						writer.WriteLine($"button{buttonNumber}={button.icon}");
						writer.WriteLine($"cmd{buttonNumber}={button.cmd}");
						writer.WriteLine($"iconic{buttonNumber}={button.iconic}");
						writer.WriteLine($"menu{buttonNumber}={button.name}");
						writer.WriteLine($"path{buttonNumber}={button.path}");
						writer.WriteLine($"param{buttonNumber}={button.param}");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存工具栏配置失败：{ex.Message}", "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public void GenerateDynamicToolbar()
		{
			// 遍历toolbarButtons列表，为每个按钮创建ToolStripButton或ToolStripDropDownButton，并添加到dynamicToolStrip中
			// 如果按钮的cmd属性以"openbar "开头，则创建ToolStripDropDownButton，并调用InitializeDropdownMenu方法初始化下拉菜单
			dynamicToolStrip.Items.Clear();
			for (int i = 0; i < toolbarButtons.Count; i++)
			{
				ToolbarButton b = toolbarButtons[i];
				// var zhdesc = form.cmdProcessor.cmdTable.GetByCmdName(cmd)?.ZhDesc ?? "";
				ToolStripButton button = new ToolStripButton
				{
					Text = "",  //menuText,
					ToolTipText = b.name,
					Image = IconManager.LoadIcon(b.icon),
					Tag = b.cmd
				};

				if (b.cmd.StartsWith("openbar"))
				{
					string dropdownFilePath = b.cmd.Substring("openbar ".Length);
					ToolStripDropDownButton dropdownButton = new ToolStripDropDownButton
					{
						Text = "", //menuText,
						ToolTipText = b.name,
						Image = IconManager.LoadIcon(b.icon)
					};
					// 为下拉按钮添加右键菜单
					dropdownButton.MouseUp += Button_MouseUp;
					form.uiManager.InitializeDropdownMenu(dropdownButton, dropdownFilePath);
					dynamicToolStrip.Items.Add(dropdownButton);
				}
				else
				{
					button.Click += form.uiManager.ToolbarButton_Click;
					// 为普通按钮添加右键菜单
					button.MouseUp += Button_MouseUp;
					dynamicToolStrip.Items.Add(button);
				}
			}
			dynamicToolStrip.Refresh();

		}
		// 处理按钮的鼠标事件
		private void Button_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && sender is ToolStripItem button)
			{
				currentButton = button as ToolStripButton;

				//buttonContextMenu.Show(dynamicToolStrip.PointToScreen(new Point(e.X, e.Y)));
				// 获取鼠标的屏幕坐标
				Point screenPoint = Cursor.Position;
				buttonContextMenu.Show(screenPoint);
			}
		}

		// 删除按钮
		private void DeleteButton_Click(object? sender, EventArgs e)
		{
			if (currentButton != null)
			{
				int index = dynamicToolStrip.Items.IndexOf(currentButton);
				if (index >= 0)
				{
					toolbarButtons.RemoveAt(index);
					SaveToconfig();
					GenerateDynamicToolbar();
				}
			}
		}

		// 复制按钮
		private void CopyButton_Click(object? sender, EventArgs e)
		{
			if (currentButton != null)
			{
				int index = dynamicToolStrip.Items.IndexOf(currentButton);
				if (index >= 0 && index < toolbarButtons.Count)
				{
					var button = toolbarButtons[index];
					AddButton(button.name, button.cmd, button.icon, button.path, button.param, button.iconic);
					SaveToconfig();
					GenerateDynamicToolbar();
				}
			}
		}
		public void Init(string path)
		{
			//load from config file
			string toolbarFilePath = Path.Combine(Constants.ZfileCfgPath, path);
			if (!File.Exists(toolbarFilePath))
			{
				MessageBox.Show("工具栏配置文件不存在" + toolbarFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var zfile_path = Path.Combine(Constants.ZfileCfgPath, "WCMIcon3.dll");
			//var iconManager = form.iconManager;
			var iconList = IconManager.LoadIconsFromFile(zfile_path);
			var fileInfoList = new FileInfoList(new string[] { zfile_path });

			using (StreamReader reader = new StreamReader(toolbarFilePath, Encoding.GetEncoding("GB2312")))
			{
				// dynamicToolStrip = new ToolStrip();
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
							AddButton(menuText, cmd, buttonIcon, pathText, paramText, iconic);
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
			}
		}
	}

	public class UIControlManager : IDisposable
	{
		private readonly Form1 form;
		private readonly ImageList listViewImageListL;
		private readonly ImageList thumbnailImageListL;
		private readonly ImageList listViewImageListR;
		private readonly ImageList thumbnailImageListR;

		#region Container Controls
		public SplitContainer MainContainer { get; } = new();
		public SplitContainer LeftPanel { get; } = new();
		public SplitContainer RightPanel { get; } = new();
		public SplitContainer LeftTreeListSplitter { get; } = new();
		public SplitContainer RightTreeListSplitter { get; } = new();
		#endregion

		#region Panel Controls
		public Panel LeftUpperPanel { get; } = new();
		public Panel RightUpperPanel { get; } = new();
		public Panel LeftDrivePanel { get; } = new();
		public Panel RightDrivePanel { get; } = new();
		#endregion

		#region Drive Controls
		public ComboBox LeftDriveBox { get; } = new();
		public ComboBox RightDriveBox { get; } = new();
		public ShengAddressBarStrip LeftPathTextBox { get; } = new();
		public ShengAddressBarStrip RightPathTextBox { get; } = new();
		#endregion

		#region View Controls
		public TreeView LeftTree { get; } = new() { Name = "LeftTree"};
		public TreeView RightTree { get; } = new() { Name = "RightTree"};
		public ListView LeftList { get; } = new() { Name = "LeftList"};
		public ListView RightList { get; } = new() { Name = "RightList" };
		#endregion

		#region Preview Controls
		public TextBox LeftPreview { get; } = new();
		public TextBox RightPreview { get; } = new();
		#endregion

		#region Status Controls
		public StatusStrip LeftStatusStrip { get; } = new();
		public StatusStrip RightStatusStrip { get; } = new();
		#endregion

		#region Bookmark Controls
		public readonly FlowLayoutPanel leftBookmarkPanel = new();
		public readonly FlowLayoutPanel rightBookmarkPanel = new();
		public BookmarkManager BookmarkManager { get; private set; }
		#endregion

		#region Menu Controls
		public MenuStrip dynamicMenuStrip = new();
		#endregion
		public ToolbarManager toolbarManager;
		public ToolbarManager vtoolbarManager;
		public bool isleft;
		public Dictionary<string, string> lastVisitedPaths = new ();
		private bool disposed = false;

		public UIControlManager(Form1 form)
		{
			this.form = form;
			listViewImageListL = new ImageList();
			listViewImageListR = new ImageList();
			thumbnailImageListL = new ImageList();
			thumbnailImageListR = new ImageList();
			BookmarkManager = new BookmarkManager(form, leftBookmarkPanel, rightBookmarkPanel);
			
			LeftPathTextBox.SelectionChange += LeftPathTextBox_PathChanged;
			RightPathTextBox.SelectionChange += RightPathTextBox_PathChanged;
		}

		private void LeftPathTextBox_PathChanged(object? sender, EventArgs e)
		{
			UpdateTreeViewSelection(LeftTree, LeftPathTextBox.CurrentNode);
		}

		private void RightPathTextBox_PathChanged(object? sender, EventArgs e)
		{
			UpdateTreeViewSelection(RightTree, RightPathTextBox.CurrentNode);
		}

		private void UpdateTreeViewSelection(TreeView treeView, IShengAddressNode snode)
		{
			TreeNode? node = null;
			if (snode.tNode != null)
				node = snode.tNode;
			else
				node = form.FindTreeNode(form.thispc.Nodes, snode.UniqueID);  //应该用绝对路径查找，而不是相对路径，否则遇到相同名称的文件夫目录会出现问题
			if (node != null)
			{
				treeView.SelectedNode = node;
				node.EnsureVisible();
			}
		}
		public void InitializeUI()
		{
			InitializeLayout();
			InitializeTreeViews();
			InitializeTreeViewIcons();
			InitializeListViews();
			InitializeDriveComboBoxes();
			InitializePreviewPanels();
			InitializeStatusStrips();
			InitializeToolStrip();
			InitializeDynamicMenu();
			InitializeDynamicToolbar();
			InitializeBookmarkLists();
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

			MainContainer.Dock = DockStyle.Fill;
			MainContainer.Orientation = Orientation.Vertical;

			//int halfWidth = (form.ClientSize.Width - MainContainer.SplitterWidth) / 2;
			//MainContainer.SplitterDistance = halfWidth;
			MainContainer.SplitterMoved += MainContainer_SplitterMoved;

			containerPanel.Controls.Add(MainContainer);

			ConfigurePanel(LeftPanel, MainContainer.Panel1);
			ConfigurePanel(RightPanel, MainContainer.Panel2);

			ConfigureUpperPanel(LeftUpperPanel, LeftDrivePanel, LeftPanel.Panel1);
			ConfigureUpperPanel(RightUpperPanel, RightDrivePanel, RightPanel.Panel1);
		}

		private void MainContainer_SplitterMoved(object? sender, SplitterEventArgs e)
		{
			int halfWidth = (form.ClientSize.Width - MainContainer.SplitterWidth) / 2;
			if (Math.Abs(MainContainer.SplitterDistance - halfWidth) > 5)
			{
				MainContainer.SplitterDistance = halfWidth;
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
			ConfigureDriveBox(LeftDriveBox, LeftDrivePanel, LeftPathTextBox);
			ConfigureDriveBox(RightDriveBox, RightDrivePanel, RightPathTextBox);

			var rootNode = new ShengFileSystemNode();
			LeftPathTextBox.InitializeRoot(rootNode);
			RightPathTextBox.InitializeRoot(rootNode);

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
					LeftDriveBox.Items.Add(drive.Name);
					RightDriveBox.Items.Add(drive.Name);
				}
			}

			if (LeftDriveBox.Items.Count > 0)
			{
				LeftDriveBox.SelectedIndex = 0;
				RightDriveBox.SelectedIndex = 0;
			}
		}
	
		private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (sender is not ComboBox comboBox) return;

			var treeView = comboBox == LeftDriveBox ? LeftTree : RightTree;
			var listView = comboBox == LeftDriveBox ? LeftList : RightList;

			if (comboBox.SelectedItem is string drivePath)
			{
				if (lastVisitedPaths.TryGetValue(drivePath, out var lastPath)) 
					form.NavigateToPath(lastPath);
				else
					form.LoadDriveIntoTree(treeView, drivePath);
			}
		}
		// 添加方法用于更新最后访问路径
		public void UpdateLastVisitedPath(string currentPath)
		{
			if (string.IsNullOrEmpty(currentPath)) return;

			// 获取路径的根目录（盘符）
			string root = Path.GetPathRoot(currentPath) ?? "";
			if (!string.IsNullOrEmpty(root))
			{
				lastVisitedPaths[root] = currentPath;
			}
		}
		public void InitializeTreeViews()
		{
			ConfigureTreeListSplitter(LeftTreeListSplitter, LeftUpperPanel, LeftTree, LeftList);
			ConfigureTreeListSplitter(RightTreeListSplitter, RightUpperPanel, RightTree, RightList);
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

		// 为两个ListView设置ImageList
		public void InitializeListViewIcons()
		{
			IconManager.InitializeIcons(listViewImageListL);
			IconManager.InitializeIcons(listViewImageListR);
			IconManager.InitializeIcons(thumbnailImageListL, true);
			IconManager.InitializeIcons(thumbnailImageListR, true);
			LeftList.SmallImageList = listViewImageListL;
			RightList.SmallImageList = listViewImageListR;
			LeftList.LargeImageList = thumbnailImageListL;
			RightList.LargeImageList = thumbnailImageListR;
		}

		public void InitializeTreeViewIcons()
		{
			// 确保先清理旧资源
			CleanupTreeViewResources();

			// 创建新的ImageList实例
			LeftTree.ImageList = new ImageList {
				ColorDepth = ColorDepth.Depth32Bit,
				ImageSize = new Size(16, 16)
			};
			RightTree.ImageList = new ImageList {
				ColorDepth = ColorDepth.Depth32Bit,
				ImageSize = new Size(16, 16)
			};

			// 初始化图标时使用新的ImageList
			//IconManager.InitializeIcons(LeftTree.ImageList);
			//IconManager.InitializeIcons(RightTree.ImageList);

			// 强制刷新所有节点图标
			RefreshAllNodeIcons(LeftTree.Nodes);
			RefreshAllNodeIcons(RightTree.Nodes);
		}

		private void RefreshAllNodeIcons(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				UpdateNodeIcon(node);
				RefreshAllNodeIcons(node.Nodes);
			}
		}

		public void CleanupTreeViewResources()
		{
			// 先清除节点图标引用
			ClearAllNodeIcons(LeftTree.Nodes);
			ClearAllNodeIcons(RightTree.Nodes);

			// 释放ImageList资源
			if (LeftTree.ImageList != null)
			{
				LeftTree.ImageList.Dispose();
				LeftTree.ImageList = null;
			}
			if (RightTree.ImageList != null)
			{
				RightTree.ImageList.Dispose();
				RightTree.ImageList = null;
			}
		}

		private void ClearAllNodeIcons(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				node.ImageKey = null;
				node.SelectedImageKey = null;
				ClearAllNodeIcons(node.Nodes);
			}
		}

		private void UpdateNodeIcon(TreeNode node)
		{
			string iconKey = IconManager.GetNodeIconKey(node);
			node.ImageKey = iconKey;
			node.SelectedImageKey = iconKey; // 确保选中状态使用相同图标

			// 递归更新所有子节点
			foreach (TreeNode childNode in node.Nodes)
			{
				if (childNode.Text != "...")
				{
					UpdateNodeIcon(childNode);
				}
			}
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
			treeView.AllowDrop = true;

			treeView.DragOver += form.TreeView_DragOver;
			treeView.DragDrop += form.TreeView_DragDrop;
			treeView.DrawNode += form.TreeView_DrawNode;
			treeView.MouseUp += form.TreeView_MouseUp;
			treeView.AfterSelect += form.TreeView_AfterSelect;
			treeView.NodeMouseClick += form.TreeView_NodeMouseClick;
			treeView.BeforeExpand += form.TreeView_BeforeExpand;
			treeView.MouseDown += form.TreeView_MouseDown;
			treeView.AfterExpand += (s, e) => UpdateNodeIcon(e.Node);
		}
		private void UnregisterTreeViewEvents(TreeView treeView)
		{
			treeView.DragDrop -= form.TreeView_DragDrop;
			treeView.DrawNode -= form.TreeView_DrawNode;
			treeView.MouseUp -= form.TreeView_MouseUp;
			treeView.AfterSelect -= form.TreeView_AfterSelect;
			treeView.NodeMouseClick -= form.TreeView_NodeMouseClick;
			treeView.BeforeExpand -= form.TreeView_BeforeExpand;
			treeView.MouseDown -= form.TreeView_MouseDown;
			treeView.AfterExpand -= (s, e) => UpdateNodeIcon(e.Node);
			treeView.BeforeExpand -= form.TreeView_BeforeExpand;
		}
		public void InitializeListViews()
		{
			ConfigureListView(LeftList, LeftTreeListSplitter.Panel2);
			ConfigureListView(RightList, RightTreeListSplitter.Panel2);
			InitializeListViewIcons();
		}

		private void ConfigureListView(ListView listView, Control parent)
		{
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = false;
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
			listView.AllowDrop = true;
			listView.ItemDrag += form.ListView_ItemDrag;
			listView.DragOver += form.ListView_DragOver;
			listView.DragDrop += form.ListView_DragDrop;
			// 添加双击事件
			listView.MouseDoubleClick += form.ListView_MouseDoubleClick;
			listView.ColumnClick += form.ListView_ColumnClick;
			listView.SelectedIndexChanged += form.ListView_SelectedIndexChanged;
			listView.MouseUp += form.ListView_MouseUp;
			listView.MouseDown += form.ListView_MouseDown;
			listView.MouseMove += form.ListView_MouseMove;
			listView.BeforeLabelEdit += form.ListView_BeforeLabelEdit;
			listView.AfterLabelEdit += form.ListView_AfterLabelEdit;
			parent.Controls.Add(listView);
			listView.BringToFront();
		}
		private void UnregisterListViewEvents(ListView listView){
			listView.ItemDrag -= form.ListView_ItemDrag;
			listView.DragOver -= form.ListView_DragOver;
			listView.DragDrop -= form.ListView_DragDrop;
			// 添加双击事件
			listView.MouseDoubleClick -= form.ListView_MouseDoubleClick;
			listView.ColumnClick -= form.ListView_ColumnClick;
			listView.SelectedIndexChanged -= form.ListView_SelectedIndexChanged;
			listView.MouseUp -= form.ListView_MouseUp;
			listView.MouseDown -= form.ListView_MouseDown;
			listView.MouseMove -= form.ListView_MouseMove;
			listView.BeforeLabelEdit -= form.ListView_BeforeLabelEdit;
			listView.AfterLabelEdit -= form.ListView_AfterLabelEdit;
		}

		public void InitializePreviewPanels()
		{
			LeftPreview.Dock = DockStyle.Fill;
			LeftPreview.Multiline = true;
			LeftPreview.ReadOnly = true;
			LeftPreview.ScrollBars = ScrollBars.Both;
			LeftPanel.Panel2.Controls.Add(LeftPreview);

			RightPreview.Dock = DockStyle.Fill;
			RightPreview.Multiline = true;
			RightPreview.ReadOnly = true;
			RightPreview.ScrollBars = ScrollBars.Both;
			RightPanel.Panel2.Controls.Add(RightPreview);
		}

		public void InitializeStatusStrips()
		{
			LeftStatusStrip.Dock = DockStyle.Bottom;
			LeftPanel.Panel2.Controls.Add(LeftStatusStrip);

			RightStatusStrip.Dock = DockStyle.Bottom;
			RightPanel.Panel2.Controls.Add(RightStatusStrip);
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
			//leftBookmarkPanel.Dock = DockStyle.Top;
			var leftlines = Helper.GetFlowLayoutPanelLineCount(leftBookmarkPanel);
			leftBookmarkPanel.Height = 20 * leftlines;
			//leftBookmarkPanel.WrapContents = true;
			//leftBookmarkPanel.DoubleClick += BookmarkPanel_DoubleClick;
			LeftPanel.Panel2.Controls.Add(leftBookmarkPanel);

			// 初始化右侧书签Panel
			var rightlines = Helper.GetFlowLayoutPanelLineCount(rightBookmarkPanel);
			//rightBookmarkPanel.Dock = DockStyle.Top;
			rightBookmarkPanel.Height = 20 * rightlines;
			//rightBookmarkPanel.WrapContents = true;
			//rightBookmarkPanel.DoubleClick += BookmarkPanel_DoubleClick;
			RightPanel.Panel2.Controls.Add(rightBookmarkPanel);

			// 调整布局顺序
			LeftPanel.Panel2.Controls.SetChildIndex(leftBookmarkPanel, 0);
			LeftPanel.Panel2.Controls.SetChildIndex(LeftPreview, 1);
			RightPanel.Panel2.Controls.SetChildIndex(rightBookmarkPanel, 0);
			RightPanel.Panel2.Controls.SetChildIndex(RightPreview, 1);
		}
		//private void BookmarkPanel_DoubleClick(object? sender,  EventArgs e)
		//{
		//	Debug.Print("书签双击1");
		//	var s = sender as FlowLayoutPanel;
		//	isleft = s == leftBookmarkPanel;
		//	BookmarkManager.AddBookmark(form.currentDirectory, form.activeTreeview.SelectedNode, isleft);
		//}
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
			//TODO: ADD MOUSE RIGHT CLICK TO EDIT BUTTON
			if (sender is ToolStripItem item)
			{
				if (item.Tag is string cmd)
				{
					Debug.Print($"执行命令: {cmd} <信息");
					form.cmdProcessor.ExecCmdByName(cmd);
				}
				else if (item.Tag is MenuInfo mi){
					form.cmdProcessor.ExecCmdByMenuInfo(mi);
					Debug.Print($"执行命令: {mi.Cmd} <信息");
				}
			}
		}
		public void InitializeDropdownMenu(ToolStripDropDownButton dropdownButton, string dropdownFilePath)
		{
			var commanderPath = GetCommanderPath();
			if (string.IsNullOrEmpty(commanderPath))
			{
				return;
			}
			dropdownFilePath = dropdownFilePath.ToUpper().Replace("%COMMANDER_PATH%", Constants.ZfileCfgPath );//commanderPath + "\\..\\..\\..\\..\\config"
			if (!File.Exists(dropdownFilePath))
			{
				MessageBox.Show("下拉菜单配置文件不存在" + dropdownFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			//try
			//{
			//	using (StreamReader reader = new StreamReader(dropdownFilePath, Encoding.GetEncoding("GB2312")))
			//	{
			//		string? line;
			//		while ((line = reader.ReadLine()) != null)
			//		{
			//			line = line.Trim();
			//			if (line.StartsWith("button"))
			//			{
			//				string menuText = reader.ReadLine()?.Trim() ?? string.Empty;
			//				string cmd = reader.ReadLine()?.Trim() ?? string.Empty;

			//				ToolStripMenuItem menuItem = new ToolStripMenuItem
			//				{
			//					Text = menuText,

			//					Tag = cmd
			//				};
			//				menuItem.Click += ToolbarButton_Click;
			//				dropdownButton.DropDownItems.Add(menuItem);
			//			}
			//		}
			//	}
			//}
			//catch (Exception ex)
			//{
			//	MessageBox.Show($"加载下拉菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//}
			var menulist = Helper.ReadButtonbarFile(dropdownFilePath);
			foreach (var item in menulist)
			{
				ToolStripMenuItem menuItem = new ToolStripMenuItem
				{
					Text = item.Menu,
					Tag = item,
					Image = IconManager.LoadIcon(item.Button)
				};
				menuItem.Click += ToolbarButton_Click;
				dropdownButton.DropDownItems.Add(menuItem);
			}

		}
		public void InitializeDynamicToolbar()
		{
			toolbarManager = new ToolbarManager(form, "DEFAULT.BAR", false);
			vtoolbarManager = new ToolbarManager(form, "VERTICAL.BAR", true);
		}
		public void InitializeDynamicMenu()
		{
			string menuFilePath = Constants.ZfileCfgPath+"WCMD_CHN.MNU";
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
								var lineSplit = line.Split(',');
								var menutxt = lineSplit[0].Trim('"');
								ToolStripMenuItem menuItem = new ToolStripMenuItem(menutxt);
								menuItem.Tag = lineSplit[1];
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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void ReleaseTreeNodes(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag is ShellItem shellItem)
					shellItem.Dispose();
				ReleaseTreeNodes(node.Nodes);
			}
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 取消事件订阅
					LeftPathTextBox.SelectionChange -= LeftPathTextBox_PathChanged;
					RightPathTextBox.SelectionChange -= RightPathTextBox_PathChanged;
					MainContainer.SplitterMoved -= MainContainer_SplitterMoved;
					LeftDriveBox.SelectedIndexChanged -= DriveComboBox_SelectedIndexChanged;
					RightDriveBox.SelectedIndexChanged -= DriveComboBox_SelectedIndexChanged;

					// 取消书签面板事件订阅
					//if (leftBookmarkPanel != null)
					//	leftBookmarkPanel.MouseDoubleClick -= BookmarkPanel_MouseDoubleClick;
					//if (rightBookmarkPanel != null)
					//	rightBookmarkPanel.MouseDoubleClick -= BookmarkPanel_MouseDoubleClick;
					UnregisterTreeViewEvents(LeftTree);
					UnregisterTreeViewEvents(RightTree);
					UnregisterListViewEvents(LeftList);
					UnregisterListViewEvents(RightList);
					
					// 释放所有 TreeView 节点中的 ShellItem
					ReleaseTreeNodes(LeftTree.Nodes);
					ReleaseTreeNodes(RightTree.Nodes);
					// 释放托管资源
					LeftList.SmallImageList?.Dispose();
					LeftList.LargeImageList?.Dispose();
					RightList.SmallImageList?.Dispose();
					RightList.LargeImageList?.Dispose();
					LeftList?.Dispose();
					RightList?.Dispose();

					CleanupTreeViewResources();
					LeftTree?.Dispose();
					RightTree?.Dispose();

					LeftPreview?.Dispose();
					RightPreview?.Dispose();
					LeftStatusStrip?.Dispose();
					RightStatusStrip?.Dispose();
					toolbarManager?.Dispose();
					vtoolbarManager?.Dispose();
					dynamicMenuStrip?.Dispose();
					BookmarkManager?.Dispose(); 
				}

				// 释放非托管资源
				disposed = true;
			}
		}

		~UIControlManager()
		{
			Dispose(false);
		}
	}
}