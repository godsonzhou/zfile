using SharpCompress.Compressors.Explode;
using Sheng.Winform.Controls;
using System.Diagnostics;
using System.Text;
using WinShell;
using ZstdSharp.Unsafe;
using static System.Net.Mime.MediaTypeNames;

namespace WinFormsApp1
{
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
		public TreeView LeftTree { get; } = new() { Name = "LeftTree" };
		public TreeView RightTree { get; } = new() { Name = "RightTree" };
		public ListView LeftList { get; } = new() { Name = "LeftList" };
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
		public FtpController ftpController;
		public bool isleft { get; set; }
		public string leftDir => LeftPathTextBox?.CurrentNode?.UniqueID ;
		public string rightDir => RightPathTextBox?.CurrentNode?.UniqueID;
		public string leftfiles => string.Join("|", LeftList.SelectedItems.Cast<ListViewItem>()?.Select(item => item.SubItems[0].Text));
		public string rightfiles => string.Join("|", RightList.SelectedItems.Cast<ListViewItem>()?.Select(item => item.SubItems[0].Text));
		public string targetDir => isleft ? rightDir : leftDir;
		public string srcDir => isleft ? leftDir : rightDir;
		public string targetfiles => isleft ? rightfiles : leftfiles;
		public string srcfiles => isleft ? leftfiles : rightfiles; 
		public Dictionary<string, string> args = new();
		
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
			
			setArgs();
		}
		public void setArgs()
		{
			/*
			 * ? 为 第一个 参数时，启动程序前显示对话框，列出其余参数，允许你修改，甚至中止程序运行
			%P 插入来源路径，以反斜杠() 结尾。
			%N 插入光标所在的文件名。
			%T 插入当前目标路径，对压缩程序尤其有用。
			%M 插入目标文件夹的当前文件名。
			%O 插入当前文件名，不含扩展名。
			%E 插入当前文件的 扩展名 （无前导句号）。
			%S 插入所有选中文件的文件名。包含空格的名字放在双引号中。请注意命令行最大长度是32767个字符。
			%S10 插入（最多）前10个选中文件的文件名。这样可以限定传递给程序的文件名个数。可指定其它数字。
			%P%S 插入所有选中文件的全路径文件名。包含空格的名字将放在双引号中。不要自己在%P%S前后加双引号！
			注释: %N 和 %M 插入长文件名，而%n 和 %m 插入8.3 DOS文件名。%P 和 %T 插入长路径名，%p 和 %t 插入短路径名。（%o，%e和%s同样）
			如果直接在%S或%s前写%P，%p，%T或%t，将插入每个文件的路径名+文件名。例如：%P%S代表所有选中文件的长路径名和长文件名。
			%% 插入百分号。
			%L, %l, %F, %f, %D, %d 在TEMP文件夹创建包含选定文件和文件夹名字的列表文件，并插入该文件的名字。列表文件在调用程序退出后自动删除。可创建以下6种列表文件：
			%L 包含完整路径的长文件名，如，c:\Program Files\Long name.exe
			%l (小写L) 包含完整路径的短文件名，如，C:\PROGRA1\LONGNA1.EXE
			%F 不含路径的长文件名，如，Long name.exe
			%f 不含路径的短文件名，如，LONGNA~1.EXE
			%D 包含完整路径的短文件名，重音（accent）使用DOS字符集。
			%d 不含路径的短文件名，重音（accent）使用DOS字符集。
			仅用于命令别名：
			%A 插入已输入的命令行的其余部分。
			%A1..%A9 插入第1至第9个参数。
			例如，别名op代表命令：totalcmd.exe 参数：/L=%A1 /R=%A2
			-> 命令行：op c:\dir1 d:\dir2 等同于: totalcmd.exe /L=c:\dir1 /R=d:\dir2
			 */
			args["%T"] = targetDir;
			args["%N"] = srcfiles;
			args["%P"] = srcDir;
			args["%M"] = targetfiles;
			args["%O"] = Path.GetFileNameWithoutExtension(srcfiles);
			args["%E"] = Path.GetExtension(srcfiles);
			args["%S"] = srcfiles;
			args["%F"] = srcfiles;
			Debug.Print($"args update>>> \r\n [T]:{targetDir}, \r\n[P]:{srcDir}, \r\n[N]:{srcfiles}, \r\n[M]:{targetfiles}");
		}
		private void updateArg(string arg, string value)
		{
			args[arg] = value;
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
			InitializeDynamicMenu();
			InitializeDynamicToolbar();
			InitializeFtpController();
			InitializeStatusStrips();
			InitializeToolStrip();
			InitializeBookmarkLists();
		}
		private void InitializeFtpController()
		{
			ftpController = new FtpController(form, form.fTPMGR);
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
			// 创建状态栏项
			var totalFilesLabel = new ToolStripStatusLabel();
			var selectedFilesLabel = new ToolStripStatusLabel();
			var spacerLabel = new ToolStripStatusLabel { Spring = true }; // 弹性空间

			// 添加到左侧状态栏
			LeftStatusStrip.Items.AddRange(new ToolStripItem[] {
				totalFilesLabel,
				spacerLabel,
				selectedFilesLabel
			});
			LeftStatusStrip.Dock = DockStyle.Bottom;
			LeftPanel.Panel2.Controls.Add(LeftStatusStrip);

			// 为右侧状态栏创建相同的项
			var rightTotalFilesLabel = new ToolStripStatusLabel();
			var rightSelectedFilesLabel = new ToolStripStatusLabel();
			var rightSpacerLabel = new ToolStripStatusLabel { Spring = true };

			RightStatusStrip.Items.AddRange(new ToolStripItem[] {
				rightTotalFilesLabel,
				rightSpacerLabel,
				rightSelectedFilesLabel
			});
			RightStatusStrip.Dock = DockStyle.Bottom;
			RightPanel.Panel2.Controls.Add(RightStatusStrip);
			// 添加事件处理
			LeftList.ItemSelectionChanged += (s, e) => UpdateStatusBar(LeftList, LeftStatusStrip);
			RightList.ItemSelectionChanged += (s, e) => UpdateStatusBar(RightList, RightStatusStrip);

			// 更新初始状态
			UpdateStatusBar(LeftList, LeftStatusStrip);
			UpdateStatusBar(RightList, RightStatusStrip);
		}

		public void UpdateStatusBar(ListView listView, StatusStrip statusStrip)
		{
			var totalStats = CalculateStats(listView.Items.Cast<ListViewItem>());
			var selectedStats = CalculateStats(listView.SelectedItems.Cast<ListViewItem>());

			// 更新总计信息
			statusStrip.Items[0].Text = FormatStatsText(totalStats, "总计");

			// 更新选中信息
			statusStrip.Items[2].Text = FormatStatsText(selectedStats, "已选择");
		}

		private (int files, int folders, long totalSize) CalculateStats(IEnumerable<ListViewItem> items)
		{
			int fileCount = 0;
			int folderCount = 0;
			long totalSize = 0;

			foreach (var item in items)
			{
				if (item.SubItems.Count >= 3) // 确保有足够的子项
				{
					// 检查是否是文件夹
					bool isFolder = item.SubItems[3].Text.Equals("<DIR>", StringComparison.OrdinalIgnoreCase);
					if (isFolder)
						folderCount++;
					else
						fileCount++;
					// 解析文件大小
					if (long.TryParse(item.SubItems[5]?.Text.Replace(",", ""), out long size))
						totalSize += size;
				}
			}

			return (fileCount, folderCount, totalSize);
		}

		private string FormatStatsText((int files, int folders, long totalSize) stats, string prefix)
		{
			if (stats.files == 0 && stats.folders == 0)
				return $"{prefix}: 无项目";

			var parts = new List<string>();
			if (stats.folders > 0)
				parts.Add($"{stats.folders} 个文件夹");
			if (stats.files > 0)
				parts.Add($"{stats.files} 个文件");

			string sizeStr = FileSystemManager.FormatFileSize(stats.totalSize, true);
			return $"{prefix}: {string.Join(", ", parts)}, {sizeStr}";
		}

		//private string FormatFileSize(long bytes)
		//{
		//	string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		//	int order = 0;
		//	double size = bytes;
		//	while (size >= 1024 && order < sizes.Length - 1)
		//	{
		//		order++;
		//		size = size / 1024;
		//	}
		//	return $"总大小: {size:0.##} {sizes[order]}";
		//}

		// 在ListView内容改变时调用此方法
		public void RefreshStatusBar(ListView listView)
		{
			var statusStrip = listView == LeftList ? LeftStatusStrip : RightStatusStrip;
			UpdateStatusBar(listView, statusStrip);
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
					form.cmdProcessor.ExecCmd(cmd);
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
			var bar = form.configLoader.FindConfigValue("ButtonBar", "Buttonbar");
			var bar1 = form.configLoader.FindConfigValue("ButtonbarVertical", "Buttonbar");
			toolbarManager = new ToolbarManager(form, bar, false);//"DEFAULT.BAR"
			vtoolbarManager = new ToolbarManager(form, bar1, true);//"VERTICAL.BAR"
		}
		public void InitializeDynamicMenu()
		{
			var menu = form.configLoader.FindConfigValue("Configuration", "Mainmenu");
			string menuFilePath = Constants.ZfileCfgPath + menu;// "WCMD_CHN.MNU";
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