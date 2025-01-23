using CmdProcessor;
using Microsoft.Win32; // Add this namespace
using System.Runtime.InteropServices; // Add this namespace
using System.Text;
using Keys = System.Windows.Forms.Keys;//引入CmdProcessor命名空间
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using LibVLCSharp.Shared;
using SharpCompress.Archives;
using WinShell;
using CSCore.Streams.SampleConverter;

namespace WinFormsApp1
{
	public static class constant_value
	{
		public const string zfilePath = "D:\\gitrepos\\Files\\config\\";
	}

	public partial class Form1 : Form
    {
		// 声明新的 TextBox 控件
		private readonly TextBox leftPathTextBox = new();
		private readonly TextBox rightPathTextBox = new();

		private bool isSelecting = false;
        private Point selectionStart;
        private Rectangle selectionRectangle;
        private ListView activeListView;
		private TreeView activeTreeview;

		private readonly FileSystemWatcher watcher = new();
		private string currentDirectory = "";// @"";

        // 声明控件为私有字段
        private readonly SplitContainer mainContainer = new();
        private readonly SplitContainer leftPanel = new();
        private readonly SplitContainer rightPanel = new();

        private readonly Panel leftUpperPanel = new();
        private readonly Panel rightUpperPanel = new();
        private readonly Panel leftDrivePanel = new();
        private readonly Panel rightDrivePanel = new();

        private readonly ComboBox leftDriveBox = new();
        private readonly ComboBox rightDriveBox = new();

        private readonly TreeView leftTree = new();
        private readonly ListView leftList = new();
        private readonly TextBox leftPreview = new();

        private readonly TreeView rightTree = new();
        private readonly ListView rightList = new();
        private readonly TextBox rightPreview = new();

        private TreeNode? selectedNode = null; // 添加可空标记

        // 添加排序状态追踪
        private int sortColumn = -1;
        private SortOrder sortOrder = SortOrder.None;

        // 添加缓存机制，避免频繁刷新
        private readonly Dictionary<string, List<FileSystemInfo>> _directoryCache = new();
        private readonly int _cacheTimeout = 5000; // 缓存超时时间(毫秒)
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        private readonly SplitContainer leftTreeListSplitter = new();
        private readonly SplitContainer rightTreeListSplitter = new();

        // 声明状态栏控件
        private readonly StatusStrip leftStatusStrip = new();
        private readonly StatusStrip rightStatusStrip = new();

        //private readonly CmdTable cmdTable = new();
        private readonly List<Icon> iconList = new();
        FileInfoList fileList;

        private readonly ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        private CmdProc cmdProcessor;
        MenuStrip dynamicMenuStrip = new();
        ToolStrip dynamicToolStrip = new();
		private ImageList treeViewImageList;
		private WinShell.IShellFolder iDeskTop;

		public Form1()
        {
            InitializeComponent();
            InitializeContextMenu();
            this.Size = new Size(1200, 800);
            InitializeLayout();
            InitializeDriveComboBoxes();
            InitializeTreeViews();
            InitializeListViews();
            activeListView = leftList;  //default active view is left list view
			activeTreeview = leftTree;
			InitializePreviewPanels();
            InitializeStatusStrips(); // 初始化状态栏
            InitializeFileSystemWatcher();
            InitializeThemeToggleButton(); // 初始化主题切换按钮
            InitializeToolStrip(); // 初始化工具栏
            InitializeDynamicMenu();
            cmdProcessor = new CmdProc(this);
            InitializeDynamicToolbar();
			InitializeTreeViewIcons(); // 初始化TreeView图标
		}
        private void InitializeTreeViewIcons()
        {
            treeViewImageList = new ImageList();
            treeViewImageList.ImageSize = new Size(16, 16);

            // 获取系统默认文件夹图标
            Icon folderIcon = GetSystemIcon.GetIconByFileType("folder", false);
            if (folderIcon != null)
            {
                treeViewImageList.Images.Add("folder", folderIcon);
            }

            //// 获取系统默认驱动器图标
            //Icon driveIcon = GetSystemIcon.GetIconByFileType("drive", false);
            //if (driveIcon != null)
            //{
            //    treeViewImageList.Images.Add("drive", driveIcon);
            //}

            //// 获取系统默认文件图标
            //Icon fileIcon = GetSystemIcon.GetIconByFileType("file", false);
            //if (fileIcon != null)
            //{
            //    treeViewImageList.Images.Add("file", fileIcon);
            //}

            // 将ImageList分配给TreeView
            leftTree.ImageList = treeViewImageList;
            rightTree.ImageList = treeViewImageList;
        }
		private void InitializeContextMenu()
        {
            // 初始化ContextMenuStrip
            contextMenuStrip.Opening += ContextMenuStrip_Opening;
        }
		public void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// 在这里可以添加自定义的菜单项
		}
		private void 加载文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            //if (dlg.ShowDialog() == DialogResult.OK)
            {
                string[] filespath = Directory.GetFiles(dlg.SelectedPath);
                fileList = new FileInfoList(filespath);
                InitListView();
            }
        }

        private void InitListView()
        {
            activeListView.Items.Clear();
            this.activeListView.BeginUpdate();
            foreach (FileInfoWithIcon file in fileList.list)
            {
                ListViewItem item = new ListViewItem();
                item.Text = file.fileInfo.Name.Split('.')[0];
                item.ImageIndex = file.iconIndex;
                item.SubItems.Add(file.fileInfo.LastWriteTime.ToString());
                item.SubItems.Add(file.fileInfo.Extension.Replace(".", ""));
                item.SubItems.Add(string.Format(("{0:N0}"), file.fileInfo.Length));
                activeListView.Items.Add(item);
            }
            activeListView.LargeImageList = fileList.imageListLargeIcon;
            activeListView.SmallImageList = fileList.imageListSmallIcon;
            activeListView.Show();
            this.activeListView.EndUpdate();
        }

        public interface IActiveListViewChangeable
        {
            void ActiveListViewChange(View view);
        }
        public void ActiveListViewChange(View v)
        {
            activeListView.View = v; // View.LargeIcon;
        }

        private void InitializeLayout()
        {
            // 计算菜单和工具栏占用的总高度
            int topHeight = 0; // menuStrip2.Height + toolStrip1.Height;

            // 创建一个容器面板来持有主分割容器
            Panel containerPanel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, topHeight, 0, 0) // 为顶部控件留出空间
            };
            this.Controls.Add(containerPanel);

            // 主分割容器 (左右)
            mainContainer.Dock = DockStyle.Fill;
            mainContainer.Orientation = Orientation.Vertical;

            // 明确设置分割位置为窗体宽度的一半
            int halfWidth = (this.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            mainContainer.SplitterDistance = halfWidth;
            mainContainer.SplitterMoved += MainContainer_SplitterMoved; // 添加分割条移动事件

            containerPanel.Controls.Add(mainContainer);

            // 左右面板基本布局
            ConfigurePanel(leftPanel, mainContainer.Panel1);
            ConfigurePanel(rightPanel, mainContainer.Panel2);

            // 上部面板布局
            ConfigureUpperPanel(leftUpperPanel, leftDrivePanel, leftPanel.Panel1);
            ConfigureUpperPanel(rightUpperPanel, rightDrivePanel, rightPanel.Panel1);

        }

        // 分割条移动事件处理
        private void MainContainer_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            // 如果分割位置偏离中心过多，则重置到中心位置
            int halfWidth = (this.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            if (Math.Abs(mainContainer.SplitterDistance - halfWidth) > 5) // 允许5像素的误差
            {
                mainContainer.SplitterDistance = halfWidth;
            }
        }

        // 修改窗体大小改变事件处理
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (mainContainer != null && this.WindowState != FormWindowState.Minimized)
            {
                // 计算新的中心位置
                int halfWidth = (this.ClientSize.Width - mainContainer.SplitterWidth) / 2;
                mainContainer.SplitterDistance = halfWidth;
            }
        }

        /// <summary>
        /// 配置并初始化分割面板。
        /// </summary>
        /// <param name="panel">要配置的SplitContainer对象。</param>
        /// <param name="parent">SplitContainer的父控件。</param>
        private void ConfigurePanel(SplitContainer panel, Control parent)
        {
            // 设置分割面板填充其父控件
            panel.Dock = DockStyle.Fill;
            // 设置分割面板的分割方向为水平
            panel.Orientation = Orientation.Horizontal;
            // 设置分割面板的分割线位置，使其位于父控件宽度的50%处
            panel.SplitterDistance = (int)((parent.Width) * 0.5);
            // 将分割面板添加到父控件的控件集合中
            parent.Controls.Add(panel);
        }

        private void ConfigureUpperPanel(Panel upperPanel, Panel drivePanel, Control parent)
        {
            // 修改上部面板布局
            upperPanel.Dock = DockStyle.Fill;
            upperPanel.Padding = new Padding(0, 30, 0, 0); // 为驱动器面板留出空间

            // 修改驱动器面板布局
            drivePanel.Dock = DockStyle.Top;
            drivePanel.Height = 30;

            // 调整添加顺序
            parent.Controls.Add(upperPanel);
            parent.Controls.Add(drivePanel);
            drivePanel.BringToFront(); // 确保驱动器面板在最上层
        }

        private void InitializeDriveComboBoxes()
        {
			//ConfigureDriveBox(leftDriveBox, leftDrivePanel);
			//ConfigureDriveBox(rightDriveBox, rightDrivePanel);
			ConfigureDriveBox(leftDriveBox, leftDrivePanel, leftPathTextBox);
			ConfigureDriveBox(rightDriveBox, rightDrivePanel, rightPathTextBox);


			LoadDrives();
        }
		private void ConfigureDriveBox(ComboBox driveBox, Panel parent, TextBox pathTextBox)
		{
			driveBox.Dock = DockStyle.Left;
			driveBox.DropDownStyle = ComboBoxStyle.DropDownList;
			driveBox.SelectedIndexChanged += DriveComboBox_SelectedIndexChanged;

			pathTextBox.Dock = DockStyle.Fill;
			pathTextBox.ReadOnly = true;

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

        private void InitializeTreeViews()
        {
            ConfigureTreeListSplitter(leftTreeListSplitter, leftUpperPanel, leftTree, leftList);// 配置左侧树列表分割容器
            ConfigureTreeListSplitter(rightTreeListSplitter, rightUpperPanel, rightTree, rightList);// 配置右侧树列表分割容器
        }
		private void TreeView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				TreeView Tree1 = sender as TreeView;
				Tree1.SelectedNode = Tree1.GetNodeAt(e.X, e.Y);
			}
		}
        private void ConfigureTreeView(TreeView treeView)
        {
            treeView.Dock = DockStyle.Fill;

            // 修改TreeView的基本属性和样式
            treeView.ShowLines = true;
            treeView.HideSelection = false;
            treeView.ShowPlusMinus = true;
            treeView.ShowRootLines = true;
            treeView.PathSeparator = "\\";

            // 添加这些关键设置以启用自定义绘制
            treeView.FullRowSelect = true;  // 允许整行选择
            treeView.ItemHeight = 20;       // 设置节点高度
            treeView.DrawMode = TreeViewDrawMode.OwnerDrawText; // 使用自定义绘制

            treeView.DrawNode += TreeView_DrawNode; // 添加绘制事件处理
            treeView.MouseUp += TreeView_MouseUp;
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseClick += TreeView_NodeMouseClick;
            treeView.BeforeExpand += TreeView_BeforeExpand;
            treeView.MouseDown += TreeView_MouseDown;
        }
		private void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView treeView = sender as TreeView;
                TreeNode node = treeView.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    treeView.SelectedNode = node;
					//ShowContextMenu(treeView, node.Tag.ToString(), e.Location);
					ShowContextMenu1(treeView, node, e.Location);
				}
            }
        }
		private void ShowContextMenu1(TreeView Tree1, TreeNode node, Point location)
		{
			//获得当前节点的 PIDL
			ShellItem sItem = (ShellItem)Tree1.SelectedNode.Tag;
			IntPtr PIDL = sItem.PIDL;

			//获得父节点的 IShellFolder 接口
			WinShell.IShellFolder IParent = iDeskTop;
			if (Tree1.SelectedNode.Parent != null)
			{
				IParent = ((ShellItem)Tree1.SelectedNode.Parent.Tag).ShellFolder;
			}
			else
			{
				//桌面的真实路径的 PIDL
				string path = API.GetSpecialFolderPath(this.Handle, ShellSpecialFolders.DESKTOPDIRECTORY);
				API.GetShellFolder(iDeskTop, path, out PIDL);
			}

			//存放 PIDL 的数组
			IntPtr[] pidls = new IntPtr[1];
			pidls[0] = PIDL;

			//得到 IContextMenu 接口
			IntPtr iContextMenuPtr = IntPtr.Zero;
			iContextMenuPtr = IParent.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length,
				pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);
			WinShell.IContextMenu iContextMenu = (WinShell.IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);

			//提供一个弹出式菜单的句柄
			IntPtr contextMenu = API.CreatePopupMenu();
			iContextMenu.QueryContextMenu(contextMenu, 0,
				API.CMD_FIRST, API.CMD_LAST, CMF.NORMAL | CMF.EXPLORE);

			//弹出菜单
			uint cmd = API.TrackPopupMenuEx(contextMenu, TPM.RETURNCMD,
				MousePosition.X, MousePosition.Y, this.Handle, IntPtr.Zero);

			//获取命令序号，执行菜单命令
			if (cmd >= API.CMD_FIRST)
			{
				var invoke = new CMINVOKECOMMANDINFOEX();
				invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
				invoke.lpVerb = (IntPtr)(cmd - 1);
				invoke.lpDirectory = string.Empty;
				invoke.fMask = 0;
				invoke.ptInvoke = new POINT(MousePosition.X, MousePosition.Y);
				invoke.nShow = 1;
				iContextMenu.InvokeCommand(ref invoke);
			}
		}

        private void ShowContextMenu(Control control, string path, Point location)
        {
			path = getFSpath(path);
            if (File.Exists(path) || Directory.Exists(path))
            {
                IntPtr menu = IntPtr.Zero;
                try
                {
                    menu = CreateContextMenu(path);
                    if (menu != IntPtr.Zero)
                    {
                        contextMenuStrip.Items.Clear();
                        int count = w32.GetMenuItemCount(menu);
                        for (int i = 0; i < count; i++)
                        {
                            MENUITEMINFO mii = new();
                            mii.cbSize = (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
                            mii.fMask = MIIM.ID | MIIM.STRING | MIIM.SUBMENU;
                            mii.dwTypeData = new string('\0', 256);
                            mii.cch = (uint)mii.dwTypeData.Length;

                            if (w32.GetMenuItemInfo(menu, (uint)i, true, ref mii))
                            {
                                string text = mii.dwTypeData;
                                if (string.IsNullOrEmpty(text))
                                    contextMenuStrip.Items.Add(new ToolStripSeparator());
                                else
                                {
                                    ToolStripMenuItem item = new ToolStripMenuItem(text);
                                    if (mii.hSubMenu != IntPtr.Zero)
                                        AddSubMenuItems(item, mii.hSubMenu);
                                    else
                                        item.Click += (s, e) => InvokeCommand(path, mii.wID);
                                    contextMenuStrip.Items.Add(item);
                                }
                            }
                            else
                                MessageBox.Show("无法获取菜单项信息", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        contextMenuStrip.Show(control, location);
                    }
                }
                finally
                {
                    if (menu != IntPtr.Zero)
                        w32.DestroyMenu(menu);
                }
            }
        }

        private void AddSubMenuItems(ToolStripMenuItem parentItem, IntPtr hSubMenu)
        {
            int subMenuCount = w32.GetMenuItemCount(hSubMenu);
            for (int j = 0; j < subMenuCount; j++)
            {
                MENUITEMINFO subMii = new();
                subMii.cbSize = (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
                subMii.fMask = MIIM.ID | MIIM.STRING | MIIM.SUBMENU;
                subMii.dwTypeData = new string('\0', 256);
                subMii.cch = (uint)subMii.dwTypeData.Length;

                if (w32.GetMenuItemInfo(hSubMenu, (uint)j, true, ref subMii))
                {
                    string subText = subMii.dwTypeData;
                    if (string.IsNullOrEmpty(subText))
                    {
                        parentItem.DropDownItems.Add(new ToolStripSeparator());
                    }
                    else
                    {
                        ToolStripMenuItem subItem = new ToolStripMenuItem(subText);
                        if (subMii.hSubMenu != IntPtr.Zero)
                        {
                            AddSubMenuItems(subItem, subMii.hSubMenu);
                        }
                        else
                        {
                            subItem.Click += (s, e) => InvokeCommand(parentItem.Tag.ToString(), subMii.wID);
                        }
                        parentItem.DropDownItems.Add(subItem);
                    }
                }
                else
                {
                    MessageBox.Show("无法获取子菜单项信息", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private IntPtr CreateContextMenu(string path)
        {
            IntPtr menu = w32.CreatePopupMenu();
            if (menu != IntPtr.Zero)
            {
                IntPtr pidl = w32.ILCreateFromPath(path);
                if (pidl != IntPtr.Zero)
                {
                    IntPtr parentPidl = w32.ILClone(pidl);
					w32.ILRemoveLastID(parentPidl);
                    IShellFolder desktopFolder;
					w32.SHGetDesktopFolder(out desktopFolder);
                    IShellFolder parentFolder;
                    Guid iidShellFolder = w32.IID_IShellFolder;
                    desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref iidShellFolder, out parentFolder);
                    IntPtr[] pidls = new IntPtr[] { w32.ILFindLastID(pidl) };
                    IContextMenu contextMenu;
                    Guid iidContextMenu = w32.IID_IContextMenu;
                    parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref iidContextMenu, IntPtr.Zero, out contextMenu);
                    contextMenu.QueryContextMenu(menu, 0, 1, 0x7FFF, CMF.NORMAL);
                    Marshal.ReleaseComObject(contextMenu);
                    Marshal.ReleaseComObject(parentFolder);
                    Marshal.ReleaseComObject(desktopFolder);
					w32.ILFree(pidl);
					w32.ILFree(parentPidl);
                }
            }
            return menu;
        }

        private void InvokeCommand(string path, uint id)
        {
            IntPtr pidl = w32.ILCreateFromPath(path);
            if (pidl != IntPtr.Zero)
            {
                IntPtr parentPidl = w32.ILClone(pidl);
                w32.ILRemoveLastID(parentPidl);
                IShellFolder desktopFolder;
                w32.SHGetDesktopFolder(out desktopFolder);
                IShellFolder parentFolder;
                Guid iid_IShellFolder = w32.IID_IShellFolder;
                desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref iid_IShellFolder, out parentFolder);
                IntPtr[] pidls = new IntPtr[] { w32.ILFindLastID(pidl) };
                IContextMenu contextMenu;
                Guid iid_IContextMenu = w32.IID_IContextMenu;
                parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref iid_IContextMenu, IntPtr.Zero, out contextMenu);
                CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
                invoke.lpVerb = (IntPtr)(id - 1);
				invoke.lpDirectory = string.Empty;
                invoke.nShow = w32.SW_SHOWNORMAL;
				invoke.fMask = 0;	// CMIC.CMIC_MASK_UNICODE; // Ensure the fMask is set correctly
				invoke.ptInvoke = new POINT(MousePosition.X, MousePosition.Y);
				contextMenu.InvokeCommand(ref invoke);
                Marshal.ReleaseComObject(contextMenu);
                Marshal.ReleaseComObject(parentFolder);
                Marshal.ReleaseComObject(desktopFolder);
                w32.ILFree(pidl);
                w32.ILFree(parentPidl);
            }
        }
		private void InvokeCommand1(string path, uint id)
		{
			//ShellExecute(IntPtr.Zero, "open", path, "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
			//WinExec(path, 1);
			//System.Diagnostics.Process.Start(path);
			//System.Diagnostics.Process.Start("explorer.exe", path);
			//System.Diagnostics.Process.Start("cmd.exe", "/c " + path);
			//System.Diagnostics.Process.Start("cmd.exe", "/c start " + path);
			//System.Diagnostics.Process.Start("cmd.exe", "/c start explorer.exe " + path);
			//System.Diagnostics.Process.Start("cmd.exe", "/c start explorer.exe /select," + path);
			try
			{
				// 使用File.App.Utils.Shell中的contextmenu类的相关方法，完成执行右键菜单的各种功能
				w32.ShellExecute(IntPtr.Zero, "open", path, "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"无法执行命令: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public void myShellExe()
        {
            w32.ShellExecute(IntPtr.Zero, "open", "cmd.exe", "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
            //Window wnd = Window.GetWindow(this); //获取当前窗口
            //var wih = new WindowInteropHelper(wnd); //该类支持获取hWnd
            //IntPtr hWnd = wih.Handle;    //获取窗口句柄
            //var result = ShellExecute(hWnd, "open", "需要打开的路径如C:\\Users\\Desktop\\xx.exe", null, null, (int)ShowWindowCommands.SW_SHOW);
        }

        private void TreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            // 获取节点的绘制区域
            Rectangle bounds = e.Bounds;

            // 使用节点的背景色和前景色
            Color backColor = e.Node.BackColor;
            Color foreColor = e.Node.ForeColor;

            if ((e.State & TreeNodeStates.Selected) != 0 && backColor == SystemColors.Window)
            {
                backColor = SystemColors.Highlight;
                foreColor = SystemColors.HighlightText;
            }

            // 绘制节点背景
            using (SolidBrush backgroundBrush = new(backColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, bounds);
            }

            // 计算文本的垂直居中位置，并向下偏移2像素
            int textY = bounds.Y + (bounds.Height - e.Node.TreeView.ItemHeight) / 2 + 2;

            // 绘制节点文本
            TextRenderer.DrawText(
                e.Graphics,
                e.Node.Text,
                e.Node.TreeView.Font,
                new Point(bounds.X + 2, textY),
                foreColor,
                TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.SingleLine
            );

            // 如果节点处于焦点状态，绘制焦点矩形
            if ((e.State & TreeNodeStates.Focused) != 0)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, bounds);
            }

            e.DrawDefault = false;
        }

        private void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag == null) return;

            try
            {
                string path = e.Node.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    // 如果path是文件夹，则加载子目录
                    LoadSubDirectories(e.Node);

                    // 更新ListView显示
                    if (sender is TreeView treeView)
                    {
                        var listView = treeView == leftTree ? leftList : rightList;
                        LoadListView(e.Node, listView);
                        currentDirectory = path;
                        selectedNode = e.Node;

                        // 更新监视器
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }

                    // 展开节点
                    e.Node.Expand();
                }
				else
				{
					//如果不是文件夹，而是比如我的电脑/网上邻居等，则通过其他方式打开

				}
			}
            catch (Exception ex)
            {
                MessageBox.Show($"TreeView_NodeMouseClick加载目录失败: {ex.Message}", "错误");
            }
        }
        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.FirstNode.Text == "...")
            {
                LoadSubDirectories(e.Node);
            }
        }
		private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
		{
			if (e.Node?.Tag == null) return;

			try
			{
				if (sender is TreeView treeView)
				{
					// 清除所有节点的高亮状态
					ClearTreeViewHighlight(treeView);

					// 设置当前节点的高亮状态
					e.Node.BackColor = SystemColors.Highlight;
					e.Node.ForeColor = SystemColors.HighlightText;
					treeView.Refresh(); // 强制重绘

					var listView = treeView == leftTree ? leftList : rightList;
					LoadListView(e.Node, listView);
					var path = GetFullPath(e.Node);
					LoadListViewByFilesystem(path, listView);
					currentDirectory = path;
					selectedNode = e.Node;

					// 更新监视器
					if (Directory.Exists(path))
					{
						watcher.Path = path;
						watcher.EnableRaisingEvents = true;
					}

					// 更新路径 TextBox
					if (treeView == leftTree)
					{
						leftPathTextBox.Text = path;
					}
					else
					{
						rightPathTextBox.Text = path;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"TreeView_AfterSelect加载目录失败: {ex.Message}", "错误");
			}
		}

		private string GetFullPath(TreeNode node)
		{
			List<string> pathParts = new List<string>();
			while (node != null)
			{
				pathParts.Insert(0, node.Text);
				node = node.Parent;
			}
			return Path.Combine(pathParts.ToArray());
		}


		private void ClearTreeViewHighlight(TreeView treeView)
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                ClearNodeHighlight(node);
            }
        }

        private void ClearNodeHighlight(TreeNode node)
        {
            node.BackColor = SystemColors.Window;
            node.ForeColor = SystemColors.WindowText;
            foreach (TreeNode childNode in node.Nodes)
            {
                ClearNodeHighlight(childNode);
            }
        }

        private void LoadSubDirectories(TreeNode parentNode)
        {
            ShellItem sItem = (ShellItem)parentNode.Tag;
            WinShell.IShellFolder root = sItem.ShellFolder;

            // 清除现有子节点，避免重复添加
            parentNode.Nodes.Clear();

            // 循环查找子项
            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;
            uint celtFetched;

            if (root.EnumObjects(this.Handle, SHCONTF.FOLDERS, out EnumPtr) == API.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == API.S_FALSE)
                {
                    string name = API.GetNameByIShell(root, pidlSub);
                    WinShell.IShellFolder iSub;
                    try
                    {
                        root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);
                    }
                    catch (COMException ex)
                    {
                        MessageBox.Show($"Failed to bind to object: {ex.Message}");
                    }
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);

                    TreeNode nodeSub = new TreeNode(name);
                    nodeSub.Tag = new ShellItem(pidlSub, iSub);

                    // 根据项类型设置图标
                    SFGAO attributes = SFGAO.FOLDER;
                    root.GetAttributesOf(1, new IntPtr[] { pidlSub }, ref attributes);
                    if ((attributes & SFGAO.FILESYSTEM) != 0)
                    {
                        nodeSub.ImageKey = "file";
                        nodeSub.SelectedImageKey = "file";
                    }
                    else if ((attributes & SFGAO.FOLDER) != 0)
                    {
                        nodeSub.ImageKey = "folder";
                        nodeSub.SelectedImageKey = "folder";
                    }
                    else
                    {
                        nodeSub.ImageKey = "drive";
                        nodeSub.SelectedImageKey = "drive";
                    }

                    nodeSub.Nodes.Add("...");
                    parentNode.Nodes.Add(nodeSub);
                }
            }
        }

        private void LoadDriveIntoTree(TreeView treeView, string drivePath)
        {
            try
            {
                treeView.BeginUpdate();
                treeView.Nodes.Clear();

                //获得桌面 PIDL
                IntPtr deskTopPtr;
                iDeskTop = API.GetDesktopFolder(out deskTopPtr);

                TreeNode rootNode = new TreeNode("桌面")
                {
                    Tag = new ShellItem(deskTopPtr, (WinShell.IShellFolder)iDeskTop),
                    ImageKey = "desktop", // 设置图标
                    SelectedImageKey = "desktop" // 设置选中图标
                };
                treeView.Nodes.Add(rootNode);

                // 加载并展开根目录
                LoadSubDirectories(rootNode);
                rootNode.Expand();

                treeView.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载驱动器目录失败: {ex.Message}", "错误");
            }
        }

        private void ConfigureListView(ListView listView, Panel parent)
        {
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.MultiSelect = true;
            listView.Sorting = SortOrder.Ascending;

            // 配置列
            listView.Columns.Clear();
            listView.Columns.Add("名称", 250); // 新增图标列
            listView.Columns.Add("名称", 0); // 隐藏名称列
            listView.Columns.Add("大小", 100);
            listView.Columns.Add("类型", 80);
            listView.Columns.Add("修改日期", 150);

            // 添加双击事件
            listView.MouseDoubleClick += ListView_MouseDoubleClick;
            listView.ColumnClick += ListView_ColumnClick;
            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            listView.MouseUp += ListView_MouseUp;
            listView.MouseDown += ListView_MouseDown;
            listView.MouseMove += ListView_MouseMove;
        }
        private void ListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                selectionStart = e.Location;
                activeListView = sender as ListView;
                activeListView.SelectedItems.Clear();
				if (activeListView == leftList)
					activeTreeview = leftTree;
				else
					activeTreeview = rightTree;
			}
        }

        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectionRectangle = new Rectangle(
                    Math.Min(selectionStart.X, e.X),
                    Math.Min(selectionStart.Y, e.Y),
                    Math.Abs(selectionStart.X - e.X),
                    Math.Abs(selectionStart.Y - e.Y));

                activeListView.Invalidate();
            }
        }
        private void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;
                SelectItemsInRectangle(activeListView, selectionRectangle);
                activeListView.Invalidate();
            }

            if (sender is not ListView listView)
                return;
			ListViewItem item = listView.GetItemAt(e.X, e.Y);
			if (item != null)
				item.Selected = true;

			if (e.Button == MouseButtons.Right)
            {    
                if (item != null)
                {
                    listView.FocusedItem = item;
					var p = Path.Combine(currentDirectory, item.Text);
					if (p.Contains(':'))
						ShowContextMenu(listView, p, e.Location);
					else
						ShowContextMenu1(leftTree, selectedNode, e.Location);
				}
                else
                {
                    // Show context menu for the ListView itself
                    ShowContextMenu(listView, currentDirectory, e.Location);
                }
				return;
            }

            if (listView.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            string itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            if (selectedItem.SubItems[3].Text.ToUpper() == "<DIR>" || selectedItem.SubItems[3].Text == "本地磁盘")  //|| selectedItem.SubItems[2].Text.Contains(":")
			{
                try
                {
                    // 获取关联的TreeView
                    TreeView treeView = listView == leftList ? leftTree : rightTree;

                    // 查找并选择对应的TreeNode
                    TreeNode? node = FindTreeNode(treeView.Nodes, itemPath);
                    if (node != null)
                    {
                        // 设置选中状态并高亮显示
                        treeView.SelectedNode = node;
                        ClearTreeViewHighlight(treeView);
                        node.BackColor = SystemColors.Highlight;
                        node.ForeColor = SystemColors.HighlightText;
                        treeView.Refresh(); // 强制重绘
                        node.EnsureVisible(); // 确保节点可见
                        node.Expand();

                        // 更新当前目录和ListView
                        currentDirectory = itemPath;
                        selectedNode = node;
                        RefreshTreeViewAndListView(treeView, listView, itemPath);
                    }
                    else
                    {
                        // 如果在树中找不到节点，直接更新ListView
                        currentDirectory = itemPath;
                    }

                    // 更新监视器
                    if (Directory.Exists(itemPath))
                    {
                        watcher.Path = itemPath;
                        watcher.EnableRaisingEvents = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"访问文件夹失败: {ex.Message}", "错误");
                }
            }
            else // 处理文件
            {
				itemPath = getFSpath(itemPath);
                if (File.Exists(itemPath))
                {
                    try
                    {
                        // 如果是可执行文件，直接执行
                        if (Path.GetExtension(itemPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Process.Start(itemPath);
                        }
                        else
                        {
                            // 使用系统默认关联程序打开文件
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(itemPath) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件: {ex.Message}", "错误");
                    }
                }
            }
        }
        private void SelectItemsInRectangle(ListView listView, Rectangle rect)
        {
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Bounds.IntersectsWith(rect))
                {
                    item.Selected = true;					
                }
            }
        }

        private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (isSelecting && e.Bounds.IntersectsWith(selectionRectangle))
            {
                e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
            }
            e.DrawDefault = true;
        }
        private void ListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (sender is not ListView listView) return;

            if (listView.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            string itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            if (Directory.Exists(itemPath))
            {
                try
                {
                    // 获取关联的 TreeView
                    TreeView treeView = listView == leftList ? leftTree : rightTree;

                    // 查找并选择对应的 TreeNode
                    TreeNode? node = FindTreeNode(treeView.Nodes, itemPath);
                    if (node != null)
                    {
                        // 设置选中状态并高亮显示
                        treeView.SelectedNode = node;
                        ClearTreeViewHighlight(treeView);
                        node.BackColor = SystemColors.Highlight;
                        node.ForeColor = SystemColors.HighlightText;
                        treeView.Refresh(); // 强制重绘
                        node.EnsureVisible(); // 确保节点可见
                        node.Expand();

                        // 更新当前目录和 ListView
                        currentDirectory = itemPath;
                        selectedNode = node;
                        RefreshTreeViewAndListView(treeView, listView, itemPath);
                    }
                    else
                    {
                        // 如果在树中找不到节点，直接更新 ListView
                        currentDirectory = itemPath;
                        LoadListViewByFilesystem(itemPath, listView);
                    }

                    // 更新监视器
                    if (Directory.Exists(itemPath))
                    {
                        watcher.Path = itemPath;
                        watcher.EnableRaisingEvents = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"访问文件夹失败: {ex.Message}", "错误");
                }
            }
            else if (File.Exists(itemPath))
            {
                try
                {
                    // 如果是可执行文件，直接执行
                    if (Path.GetExtension(itemPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Process.Start(itemPath);
                    }
                    else
                    {
                        // 使用系统默认关联程序打开文件
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(itemPath) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开文件: {ex.Message}", "错误");
                }
            }
        }

		private TreeNode? FindTreeNode(TreeNodeCollection nodes, string fullPath)
        {
            foreach (TreeNode node in nodes)
            {
				//string p1;
				//if (node.Text.Contains(':'))
				//{
				//	//读取 ':'的前一个字符
				//	var p = node.Text.IndexOf(':');
				//	p1 = node.Text.Substring(p - 1, 1);
				//	//if (fullPath.StartsWith(p1 + ":"))
				//	if(fullPath.Contains(node.Text))
				//		return node;
				//}
				//else
				//	p1 = node.Text;

				//if (node.Text == fullPath)
				//            {
				//                return node;
				//            }
				//if (node.Text.Contains(':'))	
				if(fullPath.EndsWith(node.Text))
				{
					return node;
				}

				var i = (ShellItem)node.Tag;
				//var p1 = API.GetNameByIShell(iDeskTop, i.PIDL);
				//var p2 = API.GetPathByIShell(iDeskTop, i.PIDL);
				//var p3 = API.SHGetFileInfo(p2, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), SHGFI_ICON | SHGFI_SMALLICON);
				//var p3 = API.GetNameByPIDL(i.PIDL);
				// 如果当前节点的路径是目标路径的父路径，则展开并递归搜索
				
				if (fullPath.Contains(node.Text))
                {
                    LoadSubDirectories(node); // 确保子节点已加载
                    node.Expand();
                    TreeNode? found = FindTreeNode(node.Nodes, fullPath);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private void ConfigureTreeListSplitter(SplitContainer splitter, Panel parent, TreeView tree, ListView list)
        {
            // 配置分割容器基本属性
            splitter.Dock = DockStyle.Fill;
            splitter.Orientation = Orientation.Vertical;

            // 设置合理的最小尺寸
            splitter.Panel1MinSize = 100;
            splitter.Panel2MinSize = 100;

            // 调用函数执行treeview绑定事件
            ConfigureTreeView(tree);


            // 添加控件到分割容器
            splitter.Panel1.Controls.Add(tree);
            splitter.Panel2.Controls.Add(list);

            // 将分割容器添加到父面板
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

        private void InitializeListViews()
        {
            // 只需配置ListView的属性，不需要添加到面板
            ConfigureListView(leftList, leftPanel.Panel2);
            ConfigureListView(rightList, rightPanel.Panel2);
        }

        private void InitializePreviewPanels()
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

        private void InitializeStatusStrips()
        {
            // 配置左侧状态栏
            leftStatusStrip.Dock = DockStyle.Bottom;
            leftPanel.Panel2.Controls.Add(leftStatusStrip);

            // 配置右侧状态栏
            rightStatusStrip.Dock = DockStyle.Bottom;
            rightPanel.Panel2.Controls.Add(rightStatusStrip);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var selectedDrive = leftDriveBox.SelectedItem?.ToString();
            var listView = selectedDrive != null && watcher.Path.StartsWith(selectedDrive) ? leftList : rightList;
            //LoadListView(watcher.Path, listView);
        }
        public void OpenOptions()
        {
            //打开Options窗口
            OptionsForm optionsForm = new();
            optionsForm.ShowDialog();
        }
        public void ExitApp()
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("关于此应用程序zFile ver0.0.1d", "关于");
        }

        // 驱动器选择变更事件处理
        private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var treeView = comboBox == leftDriveBox ? leftTree : rightTree;
            var listView = comboBox == leftDriveBox ? leftList : rightList;

            if (comboBox.SelectedItem is string drivePath)
            {
                LoadDriveIntoTree(treeView, drivePath);
                //LoadListView(drivePath, listView);
            }
        }
        private void LoadListView(TreeNode node, ListView listView)
        {
            if (listView == null) return;
            if (listView.SmallImageList == null)
            {
                listView.SmallImageList = new ImageList();
            }
            ShellItem sItem = (ShellItem)node.Tag;
            WinShell.IShellFolder root = sItem.ShellFolder;

            // 循环查找子项
            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;
            uint celtFetched;
            listView.BeginUpdate();
            listView.Items.Clear();

            // 加载文件夹和文件
            if (root.EnumObjects(this.Handle, SHCONTF.FOLDERS, out EnumPtr) == API.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == API.S_FALSE)
                {
                    string name = API.GetNameByIShell(root, pidlSub);
                    string pth = API.GetPathByIShell(root, pidlSub);
                    WinShell.IShellFolder iSub;
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);

					// 获取图标
					//Icon icon = GetSystemIcon.GetIconByFileType(name.Contains(':') ? "folder" : Path.GetExtension(name), false);
					var fiwi = new FileInfoWithIcon(name);
					var icon = fiwi.smallIcon != null ? fiwi.smallIcon : GetIconByFileName("FILE", name);
					int iconIndex = listView.SmallImageList.Images.Count;
                    listView.SmallImageList.Images.Add(icon);

                    string[] s = { "", name, "", name.Contains(':') ? "本地磁盘" : "<DIR>", "" };
                    var i = new ListViewItem(s);
                    i.ImageIndex = iconIndex;
					i.Text = name;
                    listView.Items.Add(i);
                }
            }

            listView.EndUpdate();
        }
		private string getFSpath(string path)
		{
			if (path.Contains(':'))
			{
				var pathParts = path.Split(':');    // path = 桌面\\此电脑\\system (c:)\\windows\\system32 -> c:\\windows\\system32
													//get the last char of pathparts[0] to get the drive letter
				var len = pathParts[0].Length;
				var drive = pathParts[0].Substring(len - 1, 1);
				return drive + ":" + pathParts[1].TrimStart(')');
			}
			return path;
		}
		// 加载文件列表
		    private void LoadListViewByFilesystem(string path, ListView listView)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = getFSpath(path);

            try
            {
                var currentTime = DateTime.Now;
                var needsUpdate = !_directoryCache.ContainsKey(path) ||
                                (currentTime - _lastCacheUpdate).TotalMilliseconds > _cacheTimeout;

                List<FileSystemInfo> items;
                if (needsUpdate)
                {
                    items = GetDirectoryContents(path);
                    _directoryCache[path] = items;
                    _lastCacheUpdate = currentTime;
                }
                else
                {
                    items = _directoryCache[path];
                }

                listView.BeginUpdate();
                //listView.Items.Clear();

                foreach (var item in items)
                {
                    if ((item.Attributes & FileAttributes.Hidden) != 0) continue;

                    var lvItem = CreateListViewItem(item, listView);
                    if (lvItem != null)
                    {
                        listView.Items.Add(lvItem);
                    }
                }

                listView.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件列表失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 优化获取目录内容的方法
        private List<FileSystemInfo> GetDirectoryContents(string path, bool includeFolder = false)
        {
            var result = new List<FileSystemInfo>();
            var dirInfo = new DirectoryInfo(path);
			if (Directory.Exists(path))
			{
				try
				{
					// 并行处理目录和文件
					if (includeFolder)
					{
						var directories = dirInfo.GetDirectories()
							.Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
						result.AddRange(directories);
					}
					//var directories = dirInfo.GetDirectories()
					//	.Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
					var files = dirInfo.GetFiles()
						.Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

					//result.AddRange(directories);
					result.AddRange(files);
				}
				catch (UnauthorizedAccessException)
				{
					// 忽略访问受限的目录
				}
			}

			return result;
        }

        // 优化ListViewItem创建
        private ListViewItem? CreateListViewItem(FileSystemInfo item, ListView listView)
        {
            try
            {
                string[] itemData;
                Icon icon;
                if (item is DirectoryInfo)
                {
                    itemData = new[]
                    {
                        "",
                        item.Name,
                        "<DIR>",
                        "文件夹",
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    };
                    icon = GetSystemIcon.GetIconByFileType("folder", false);
                }
                else if (item is FileInfo fileInfo)
                {
                    itemData = new[]
                    {
                        "",
                        item.Name,
                        FormatFileSize(fileInfo.Length),
                        fileInfo.Extension.ToUpperInvariant(),
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    };
                    icon = GetSystemIcon.GetIconByFileType(fileInfo.Extension, false);
                }
                else
                {
                    return null;
                }

                int iconIndex = listView.SmallImageList.Images.Count;
                listView.SmallImageList.Images.Add(icon);

                var lvItem = new ListViewItem(itemData)
                {
                    ImageIndex = iconIndex
                };
				lvItem.Text = item.Name;
				return lvItem;
            }
            catch
            {
                return null;
            }
        }

        // 预览文件内容
        private async Task PreviewFileAsync(string filePath, TextBox previewPanel)
        {
            if (!File.Exists(filePath))
            {
                previewPanel.Clear();
                return;
            }

            try
            {
                if (IsTextFile(Path.GetExtension(filePath)))
                {
                    using var stream = new StreamReader(filePath);
                    // 仅读取前1MB内容
                    var buffer = new char[1024 * 1024];
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    previewPanel.Text = new string(buffer, 0, read);
                    if (stream.Peek() != -1)
                    {
                        previewPanel.Text += "\r\n[文件过大，仅显示前1MB内容...]";
                    }
                }
                else
                {
                    previewPanel.Text = "[二进制文件]";
                }
            }
            catch (Exception ex)
            {
                previewPanel.Text = $"无法预览文件: {ex.Message}";
            }
        }

        private async void ListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ListView listView) return;
            var previewPanel = listView == leftList ? leftPreview : rightPreview;

            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView.SelectedItems[0];
                string filePath = getFSpath(Path.Combine(currentDirectory, selectedItem.Text));

                if (File.Exists(filePath))
                {
                    await PreviewFileAsync(filePath, previewPanel);
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (sender is not ListView listView) return;

            // 如果点击的是同一列，切换排序顺序
            if (e.Column == sortColumn)
            {
                sortOrder = sortOrder == SortOrder.Ascending ?
                           SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sortColumn = e.Column;
                sortOrder = SortOrder.Ascending;
            }

            // 应用排序
            listView.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder);
        }

        // 排序比较器类
        private class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int column;
            private readonly SortOrder order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                this.column = column;
                this.order = order;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem item1 || y is not ListViewItem item2)
                    return 0;

                int result;

                // 根据列类型进行比较
                switch (column)
                {
                    case 1: // 名称列
                        result = string.Compare(item1.SubItems[column].Text,
                                             item2.SubItems[column].Text);
                        break;

                    case 2: // 大小列
                        var size1 = item1.SubItems[column].Text;
                        var size2 = item2.SubItems[column].Text;
                        if (size1 == "<DIR>" && size2 == "<DIR>")
                            result = 0;
                        else if (size1 == "<DIR>")
                            result = -1;
                        else if (size2 == "<DIR>")
                            result = 1;
                        else
                            result = CompareFileSize(size1, size2);
                        break;

                    case 4: // 日期列
                        result = DateTime.Compare(
                            DateTime.Parse(item1.SubItems[column].Text),
                            DateTime.Parse(item2.SubItems[column].Text));
                        break;

                    default: // 其他列
                        result = string.Compare(item1.SubItems[column].Text,
                                             item2.SubItems[column].Text);
                        break;
                }

                // 根据排序顺序返回结果
                return order == SortOrder.Ascending ? result : -result;
            }

            private int CompareFileSize(string size1, string size2)
            {
                try
                {
                    var s1 = ParseFileSize(size1);
                    var s2 = ParseFileSize(size2);
                    return s1.CompareTo(s2);
                }
                catch
                {
                    return string.Compare(size1, size2);
                }
            }

            private double ParseFileSize(string size)
            {
                var parts = size.Split(' ');
                if (parts.Length != 2) return 0;

                var value = double.Parse(parts[0]);
                var unit = parts[1].ToUpper();

                return unit switch
                {
                    "B" => value,
                    "KB" => value * 1024,
                    "MB" => value * 1024 * 1024,
                    "GB" => value * 1024 * 1024 * 1024,
                    "TB" => value * 1024 * 1024 * 1024 * 1024,
                    _ => 0
                };
            }
        }

        // 判断文件是否为文本文件
        private bool IsTextFile(string extension)
        {
            string[] textFileExtensions = { ".txt", ".cs", ".html", ".htm", ".xml", ".json", ".css", ".js", ".md" };
            return textFileExtensions.Contains(extension.ToLower());
        }

        // 优化文件系统监视器配置
        private void InitializeFileSystemWatcher()
        {
            watcher.NotifyFilter = NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = false;
        }

        private void InitializeThemeToggleButton()
        {
            // 创建主题切换按钮
            ToolStripButton themeToggleButton = new ToolStripButton
            {
                Text = "切换主题",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            themeToggleButton.Click += ThemeToggleButton_Click;

            // 将按钮添加到工具栏
            dynamicToolStrip.Items.Add(themeToggleButton);
        }

        private void ThemeToggleButton_Click(object? sender, EventArgs e)
        {
            ThemeToggle();
        }
        public void ThemeToggle()
        {
            // 切换主题
            if (BackColor == SystemColors.Control)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
        }

        private void ApplyDarkTheme()
        {
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            dynamicToolStrip.BackColor = Color.FromArgb(28, 28, 28);
            dynamicToolStrip.ForeColor = Color.White;
            dynamicMenuStrip.BackColor = Color.FromArgb(28, 28, 28);//MenuStrip dynamicMenuStrip 
            dynamicMenuStrip.ForeColor = Color.White;
            leftTree.BackColor = Color.FromArgb(37, 37, 38);
            leftTree.ForeColor = Color.White;
            rightTree.BackColor = Color.FromArgb(37, 37, 38);
            rightTree.ForeColor = Color.White;
            leftList.BackColor = Color.FromArgb(37, 37, 38);
            leftList.ForeColor = Color.White;
            rightList.BackColor = Color.FromArgb(37, 37, 38);
            rightList.ForeColor = Color.White;
            leftPreview.BackColor = Color.FromArgb(37, 37, 38);
            leftPreview.ForeColor = Color.White;
            rightPreview.BackColor = Color.FromArgb(37, 37, 38);
            rightPreview.ForeColor = Color.White;
            leftStatusStrip.BackColor = Color.FromArgb(28, 28, 28);
            leftStatusStrip.ForeColor = Color.White;
            rightStatusStrip.BackColor = Color.FromArgb(28, 28, 28);
            rightStatusStrip.ForeColor = Color.White;
        }

        private void ApplyLightTheme()
        {
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;
            dynamicToolStrip.BackColor = SystemColors.Control;
            dynamicToolStrip.ForeColor = SystemColors.ControlText;
            dynamicMenuStrip.BackColor = SystemColors.Control;
            dynamicMenuStrip.ForeColor = SystemColors.ControlText;
            leftTree.BackColor = SystemColors.Window;
            leftTree.ForeColor = SystemColors.WindowText;
            rightTree.BackColor = SystemColors.Window;
            rightTree.ForeColor = SystemColors.WindowText;
            leftList.BackColor = SystemColors.Window;
            leftList.ForeColor = SystemColors.WindowText;
            rightList.BackColor = SystemColors.Window;
            rightList.ForeColor = SystemColors.WindowText;
            leftPreview.BackColor = SystemColors.Window;
            leftPreview.ForeColor = SystemColors.WindowText;
            rightPreview.BackColor = SystemColors.Window;
            rightPreview.ForeColor = SystemColors.WindowText;
            leftStatusStrip.BackColor = SystemColors.Control;
            leftStatusStrip.ForeColor = SystemColors.ControlText;
            rightStatusStrip.BackColor = SystemColors.Control;
            rightStatusStrip.ForeColor = SystemColors.ControlText;
        }

        private void InitializeToolStrip()
        {
            ToolStrip toolStrip = new ToolStrip
            {
                Dock = DockStyle.Bottom
            };

            // 添加按钮
            toolStrip.Items.Add(CreateToolStripButton("查看", Keys.F3, ViewButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("编辑", Keys.F4, EditButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("复制", Keys.F5, CopyButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("移动", Keys.F6, MoveButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("文件夹", Keys.F7, FolderButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("删除", Keys.F8, DeleteButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("终端", Keys.F9, TerminalButton_Click));
            toolStrip.Items.Add(CreateToolStripButton("退出", Keys.Alt | Keys.X, ExitButton_Click));

            // 将工具栏添加到窗体
            Controls.Add(toolStrip);
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

        // 按钮点击事件处理

        // 查看按钮点击处理逻辑
        private void ViewButton_Click(object? sender, EventArgs e)
        {
            var listView = leftList.Focused ? leftList : rightList;
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var filePath = Path.Combine(currentDirectory, selectedItem.Text);

            if (File.Exists(filePath))
            {
                var extension = Path.GetExtension(filePath).ToLower();
                Form viewerForm = new Form
                {
                    Text = $"查看文件 - {selectedItem.Text}",
                    Size = new Size(800, 600)
                };

                Control viewerControl = extension switch
                {
                    ".txt" or ".cs" or ".html" or ".htm" or ".xml" or ".json" or ".css" or ".js" or ".md" => CreateTextViewer(filePath),
                    ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => CreateOfficeViewer(filePath),
                    ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" => CreateImageViewer(filePath),
                    ".mp3" or ".wav" or ".wma" or ".aac" => CreateAudioPlayer(filePath),
                    ".mp4" or ".avi" or ".mkv" or ".mov" => CreateVideoPlayer(filePath),
                    ".zip" or ".rar" or ".7z" => CreateArchiveViewer(filePath),
                    _ => new Label { Text = "不支持的文件格式", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }
                };

                viewerForm.Controls.Add(viewerControl);
                viewerForm.Show();
            }
        }

        private Control CreateTextViewer(string filePath)
        {
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Text = File.ReadAllText(filePath)
            };
            return textBox;
        }

        private Control CreateOfficeViewer(string filePath)
        {
            // 使用WebBrowser控件来查看Office文档
            var webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                Url = new Uri(filePath)
            };
            return webBrowser;
        }

        private Control CreateImageViewer(string filePath)
        {
            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = Image.FromFile(filePath),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            return pictureBox;
        }

        private Control CreateAudioPlayer(string filePath)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var playButton = new Button { Text = "Play", Dock = DockStyle.Top };
            var stopButton = new Button { Text = "Stop", Dock = DockStyle.Top };

            try
            {
                var waveOut = new CSCore.SoundOut.WaveOut();
                IWaveSource audioFile;

                // 根据文件扩展名选择合适的解码器
                switch (Path.GetExtension(filePath).ToLower())
                {
                    case ".wav":
                        audioFile = new CSCore.Codecs.WAV.WaveFileReader(filePath);
                        break;
                    case ".mp3":
                        audioFile = new CSCore.Codecs.MP3.Mp3MediafoundationDecoder(filePath);
                        break;
                    case ".flac":
                        audioFile = new CSCore.Codecs.FLAC.FlacFile(filePath);
                        break;
                    case ".wma":
                        audioFile = new CSCore.Codecs.WMA.WmaDecoder(filePath);
                        break;
                    case ".aac":
                        audioFile = new CSCore.Codecs.AAC.AacDecoder(filePath);
                        break;
                    // case ".ogg":
                    //     audioFile = new CSCore.Codecs.OGG.OggVorbisDecoder(filePath);
                    //     break;
                    default:
                        throw new NotSupportedException("不支持的音频格式");
                }

                waveOut.Initialize(audioFile);

                playButton.Click += (s, e) => waveOut.Play();
                stopButton.Click += (s, e) => waveOut.Stop();

                panel.Controls.Add(stopButton);
                panel.Controls.Add(playButton);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法播放音频文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return panel;
        }

        // ...

        private Control CreateVideoPlayer(string filePath)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var playButton = new Button { Text = "Play", Dock = DockStyle.Top };
            var stopButton = new Button { Text = "Stop", Dock = DockStyle.Top };

            // Specify the path to the LibVLC native libraries
            string libVlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(libVlcPath); // Initialize LibVLC with the specified path

            using var libVLC = new LibVLC();
            var mediaPlayer = new MediaPlayer(libVLC); // Create MediaPlayer instance
            mediaPlayer.Play(new Media(libVLC, filePath, FromType.FromPath));

            var videoView = new LibVLCSharp.WinForms.VideoView
            {
                MediaPlayer = mediaPlayer,
                Dock = DockStyle.Fill
            };

            playButton.Click += (s, e) => mediaPlayer.Play();
            stopButton.Click += (s, e) => mediaPlayer.Stop();

            panel.Controls.Add(stopButton);
            panel.Controls.Add(playButton);
            panel.Controls.Add(videoView);

            return panel;
        }

        // ... other code ...

        private Control CreateArchiveViewer(string filePath)
        {
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill
            };

            try
            {
                using (var archive = ArchiveFactory.Open(filePath)) // Use ArchiveFactory from SharpCompress
                {
                    var sb = new StringBuilder();
                    foreach (var entry in archive.Entries)
                    {
                        sb.AppendLine($"{entry.Key} ({entry.Size} bytes)");
                    }
                    textBox.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                textBox.Text = $"无法读取压缩文件: {ex.Message}";
            }

            return textBox;
        }


        private void EditButton_Click(object? sender, EventArgs e)
        {
            // 编辑按钮点击处理逻辑
        }


        private void CopyButton_Click(object? sender, EventArgs e)
        {
            var sourceListView = leftList.Focused ? leftList : rightList;
            var targetTreeView = leftList.Focused ? rightTree : leftTree;
            var targetListView = leftList.Focused ? rightList : leftList;

            if (sourceListView.SelectedItems.Count == 0 || targetTreeView.SelectedNode == null) return;

            var selectedItem = sourceListView.SelectedItems[0];
            var sourcePath = Path.Combine(currentDirectory, selectedItem.Text);
            var targetPath = Path.Combine(targetTreeView.SelectedNode.Tag.ToString() ?? string.Empty, selectedItem.Text);

            try
            {
                if (selectedItem.SubItems[3].Text == "<DIR>")
                {
                    CopyDirectory(sourcePath, targetPath);
                }
                else
                {
                    File.Copy(sourcePath, targetPath);
                }

                // Refresh both panels
                RefreshTreeViewAndListView(leftTree, leftList, leftDriveBox.SelectedItem?.ToString() ?? string.Empty);
                RefreshTreeViewAndListView(rightTree, rightList, rightDriveBox.SelectedItem?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFilePath);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var targetDirectoryPath = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetDirectoryPath);
            }
        }


        private void MoveButton_Click(object? sender, EventArgs e)
        {
            var sourceListView = leftList.Focused ? leftList : rightList;
            var targetTreeView = leftList.Focused ? rightTree : leftTree;
            var targetListView = leftList.Focused ? rightList : leftList;

            if (sourceListView.SelectedItems.Count == 0 || targetTreeView.SelectedNode == null) return;

            var selectedItem = sourceListView.SelectedItems[0];
            var sourcePath = Path.Combine(currentDirectory, selectedItem.Text);
            var targetPath = Path.Combine(targetTreeView.SelectedNode.Tag.ToString() ?? string.Empty, selectedItem.Text);

            try
            {
                if (selectedItem.SubItems[3].Text == "<DIR>")
                {
                    Directory.Move(sourcePath, targetPath);
                }
                else
                {
                    File.Move(sourcePath, targetPath);
                }

                // Refresh both panels
                RefreshTreeViewAndListView(leftTree, leftList, leftDriveBox.SelectedItem?.ToString() ?? string.Empty);
                RefreshTreeViewAndListView(rightTree, rightList, rightDriveBox.SelectedItem?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void FolderButton_Click(object? sender, EventArgs e)
        {
            var listView = leftList.Focused ? leftList : rightList;
            var treeView = leftList.Focused ? leftTree : rightTree;

            if (selectedNode == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称:", "新建文件夹", "新文件夹");
            if (string.IsNullOrWhiteSpace(input)) return;

            string newFolderPath = Path.Combine(currentDirectory, input);

            try
            {
                Directory.CreateDirectory(newFolderPath);
                RefreshTreeViewAndListView(treeView, listView, currentDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建文件夹失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTreeViewAndListView(TreeView treeView, ListView listView, string path)
        {
            if (selectedNode != null)
            {
                LoadSubDirectories(selectedNode);
                selectedNode.Expand();
            }
            LoadListView(selectedNode, listView);
			LoadListViewByFilesystem(path, listView);
		}

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            var listView = leftList.Focused ? leftList : rightList;
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            //var confirmResult = MessageBox.Show($"确定要删除 {selectedItem.Text} 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            //if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    if (selectedItem.SubItems[3].Text == "<DIR>")
                    {
                        Directory.Delete(itemPath, true);
                    }
                    else
                    {
                        File.Delete(itemPath);
                    }

                    listView.Items.Remove(selectedItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void TerminalButton_Click(object? sender, EventArgs e)
        {
            // 终端按钮点击处理逻辑
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }
        private string getCurrentPath()
        {
            // 获取当前exe文件所在目录
            string currentDirectory = Directory.GetCurrentDirectory();
            return currentDirectory;
        }
        public static string ConvertGB2312ToUTF8(string str)
        {
            Encoding utf8;
            Encoding gb2312;
            utf8 = Encoding.GetEncoding("UTF-8");
            gb2312 = Encoding.GetEncoding("GB2312");
            byte[] gb = gb2312.GetBytes(str);
            gb = Encoding.Convert(gb2312, utf8, gb);
            return utf8.GetString(gb);
        }
        private void InitializeDynamicMenu()
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
                                menuItem.Click += MenuItem_Click;
                                currentPopup.DropDownItems.Add(menuItem);
                            }
                        }
                    }

                    this.MainMenuStrip = dynamicMenuStrip;
                    this.Controls.Add(dynamicMenuStrip);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                MessageBox.Show($"点击了菜单项: {menuItem.Text}", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cmdProcessor.processCmdByName(menuItem.Text);
            }
        }

        private void InitializeDynamicToolbar()
        {
            string toolbarFilePath = constant_value.zfilePath+"DEFAULT.BAR";
            if (!File.Exists(toolbarFilePath))
            {
                MessageBox.Show("工具栏配置文件不存在" + toolbarFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

			//读取config文件夹中wcmdicons.dll文件中所有图标到列表iconlist中
			var zfile_path = constant_value.zfilePath+"WCMIcon3.dll";   //"C:\\Users\\zhouy\\source\\repos\\WinFormsApp1\\src\\config\\wcmdicons3.dll"

			icons_Load(zfile_path);
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
                            var zhdesc = cmdProcessor.cmdTable.GetByCmdName(cmd)?.ZhDesc ?? "";
                            ToolStripButton button = new ToolStripButton
                            {
                                Text = menuText,
                                ToolTipText = zhdesc,
                                Image = LoadIcon(buttonIcon),
                                Tag = cmd
                            };

                            if (cmd.StartsWith("openbar"))
                            {
                                string dropdownFilePath = cmd.Substring("openbar ".Length);
                                ToolStripDropDownButton dropdownButton = new ToolStripDropDownButton
                                {
                                    Text = menuText,
                                    ToolTipText = menuText,
                                    Image = LoadIcon(buttonIcon)
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

                this.Controls.Add(dynamicToolStrip);
            }


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
        private void InitializeDropdownMenu(ToolStripDropDownButton dropdownButton, string dropdownFilePath)
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

        private void ToolbarButton_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem item && item.Tag is string cmd)
            {
                MessageBox.Show($"执行命令: {cmd}", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Image? LoadIcon(string iconPath)
        {
			if (iconPath == null)
			{
				return null;
			}
			if (iconPath.ToLower().StartsWith("wcmicon"))
			{
				iconPath = constant_value.zfilePath + iconPath;
			}
			if (iconPath.Contains(","))
            {
                string[] parts = iconPath.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out int iconIndex))
                {
                    var iconFilePath = parts[0];
                    var idx = int.Parse(parts[1]);
                    //根据icon文件名和iconindex,调用相应函数读取图标并返回icon结果
                    return GetIconByFilenameAndIdx(iconFilePath, idx);
                }
                else if (parts.Length == 1)
                {
                    var iconFilePath = parts[0];
                    using (var icon = Icon.ExtractAssociatedIcon(parts[0]))
                    {
                        return icon?.ToBitmap();
                    }
                }
            }
            else
            {
                if (File.Exists(iconPath))
                {
                    //return new Icon(iconPath).ToBitmap();
                    return GetIconByFilenameAndIdx(iconPath, 0);
                }

            }
            return null;
        }
        [DllImport("shell32.dll")]
        private static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons);


        private Image GetIconByFilenameAndIdx(string path, int index)
        {
            ImageList images = icons_Load(path);
            //返回images中的第index个图标
            if (images != null && index < images.Images.Count)
            {
                return images.Images[index];
            }
            return null;
        }

        private ImageList icons_Load(string path)
        {
            var count = ExtractIconEx(path, -1, null, null, 0);// old value="shell32.dll"
            var phiconLarge = new IntPtr[count];
            var phiconSmall = new IntPtr[count];
            var result = ExtractIconEx(path, 0, phiconLarge, null, count);// old value="shell32.dll"
            ImageList imagelist1 = new ImageList();
            imagelist1.ImageSize = SystemInformation.IconSize;
            imagelist1.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
            // var listView1 = new ListView();
            // listView1.LargeImageList = imagelist1;
            // listView1.Dock = DockStyle.Fill;
            // listView1.View = View.LargeIcon;
            // listView1.Items.AddRange(
            //     Enumerable.Range(0, (int)count)
            //     .Select(x => new ListViewItem(x.ToString(), x)).ToArray());
            // this.Controls.Add(listView1);
            phiconLarge.ToList().ForEach(x => w32.DestroyIcon(x));
            return imagelist1;
        }

        /// <summary>
        /// 通过文件名称获取文件图标
        /// </summary>
        /// <param name="tcType">指定参数tcFullName的类型: FILE/DIR</param>
        /// <param name="tcFullName">需要获取图片的全路径文件名</param>
        /// <param name="tlIsLarge">是否获取大图标(32*32)</param>
        /// <returns></returns>
        private Icon GetIconByFileName(string tcType, string tcFullName, bool tlIsLarge = false)
        {
            Icon ico = null;

            string fileType = tcFullName.Contains(".") ? tcFullName.Substring(tcFullName.LastIndexOf('.')).ToLower() : string.Empty;

            RegistryKey regVersion = null;
            string regFileType = null;
            string regIconString = null;
            string systemDirectory = Environment.SystemDirectory + "\\";
            IntPtr[] phiconLarge = new IntPtr[1];
            IntPtr[] phiconSmall = new IntPtr[1];
            IntPtr hIcon = IntPtr.Zero;
            uint rst = 0;

            if (tcType == "FILE")
            {
                //含图标的文件，优先使用文件中自带图标
                if (".exe.ico".Contains(fileType))
                {
                    //文件名 图标索引
                    phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
                    rst = ExtractIconEx(tcFullName, 0, phiconLarge, phiconSmall, 1);
                    hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
                    ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
                    if (phiconLarge[0] != IntPtr.Zero) w32.DestroyIcon(phiconLarge[0]);
                    if (phiconSmall[0] != IntPtr.Zero) w32.DestroyIcon(phiconSmall[0]);
                    if (ico != null)
                    {
                        return ico;
                    }
                }

                //通过文件扩展名读取图标
                regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
                if (regVersion != null)
                {
                    regFileType = regVersion.GetValue("") as string;
                    regVersion.Close();
                    regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
                    if (regVersion != null)
                    {
                        regIconString = regVersion.GetValue("") as string;
                        regVersion.Close();
                    }
                }
                if (regIconString == null)
                {
                    //没有读取到文件类型注册信息，指定为未知文件类型的图标
                    regIconString = systemDirectory + "shell32.dll,0";
                }
            }
            else
            {
                //直接指定为文件夹图标
                regIconString = systemDirectory + "shell32.dll,3";
            }

            string[] fileIcon = regIconString.Split(new char[] { ',' });
            //系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
            fileIcon = fileIcon.Length == 2 ? fileIcon : new string[] { systemDirectory + "shell32.dll", "2" };

            phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
            rst = ExtractIconEx(fileIcon[0].Trim('\"'), Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
            hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
            ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
            if (phiconLarge[0] != IntPtr.Zero) w32.DestroyIcon(phiconLarge[0]);
            if (phiconSmall[0] != IntPtr.Zero) w32.DestroyIcon(phiconSmall[0]);
            if (ico != null)
            {
                return ico;
            }

            // 对于文件，如果提取文件图标失败，则重新使用可执行文件通用图标
            if (tcType == "FILE")
            {
                //系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
                fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
                phiconLarge = new IntPtr[1];
                phiconSmall = new IntPtr[1];
                rst = ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
                hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
                ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
                if (phiconLarge[0] != IntPtr.Zero) w32.DestroyIcon(phiconLarge[0]);
                if (phiconSmall[0] != IntPtr.Zero) w32.DestroyIcon(phiconSmall[0]);
            }

            return ico;
        }
    }
}

class FileInfoList
{
    public List<FileInfoWithIcon> list;
    public ImageList imageListLargeIcon;
    public ImageList imageListSmallIcon;


    /// <summary>
    /// 根据文件路径获取生成文件信息，并提取文件的图标
    /// </summary>
    /// <param name="filespath"></param>
    public FileInfoList(string[] filespath)
    {
        list = new List<FileInfoWithIcon>();
        imageListLargeIcon = new ImageList();
        imageListLargeIcon.ImageSize = new Size(32, 32);
        imageListSmallIcon = new ImageList();
        imageListSmallIcon.ImageSize = new Size(16, 16);
        foreach (string path in filespath)
        {
            FileInfoWithIcon file = new FileInfoWithIcon(path);
            imageListLargeIcon.Images.Add(file.largeIcon);
            imageListSmallIcon.Images.Add(file.smallIcon);
            file.iconIndex = imageListLargeIcon.Images.Count - 1;
            list.Add(file);
        }
    }
}
class FileInfoWithIcon
{
    public FileInfo fileInfo;
    public Icon largeIcon;
    public Icon smallIcon;
    public int iconIndex;
    public FileInfoWithIcon(string path)
    {
        fileInfo = new FileInfo(path);
        largeIcon = GetSystemIcon.GetIconByFileName(path, true);
        if (largeIcon == null)
            largeIcon = GetSystemIcon.GetIconByFileType(Path.GetExtension(path), true);


        smallIcon = GetSystemIcon.GetIconByFileName(path, false);
        if (smallIcon == null)
            smallIcon = GetSystemIcon.GetIconByFileType(Path.GetExtension(path), false);
    }
}
public static class GetSystemIcon
{
    /// <summary>
    /// 依据文件名读取图标，若指定文件不存在，则返回空值。  
    /// </summary>
    /// <param name="fileName">文件路径</param>
    /// <param name="isLarge">是否返回大图标</param>
    /// <returns></returns>
    public static Icon GetIconByFileName(string fileName, bool isLarge = true)
    {
        int[] phiconLarge = new int[1];
        int[] phiconSmall = new int[1];
        //文件名 图标索引 
        Win32.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
        IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

        if (IconHnd.ToString() == "0")
            return null;
        return Icon.FromHandle(IconHnd);
    }


    /// <summary>  
    /// 根据文件扩展名（如:.*），返回与之关联的图标。
    /// 若不以"."开头则返回文件夹的图标。  
    /// </summary>  
    /// <param name="fileType">文件扩展名</param>  
    /// <param name="isLarge">是否返回大图标</param>  
    /// <returns></returns>  
    public static Icon GetIconByFileType(string fileType, bool isLarge)
    {
        if (fileType == null || fileType.Equals(string.Empty)) return null;


        RegistryKey regVersion = null;
        string regFileType = null;
        string regIconString = null;
        string systemDirectory = Environment.SystemDirectory + "\\";


        if (fileType[0] == '.')
        {
            //读系统注册表中文件类型信息  
            regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
            if (regVersion != null)
            {
                regFileType = regVersion.GetValue("") as string;
                regVersion.Close();
                regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
                if (regVersion != null)
                {
                    regIconString = regVersion.GetValue("") as string;
                    regVersion.Close();
                }
            }
            if (regIconString == null)
            {
                //没有读取到文件类型注册信息，指定为未知文件类型的图标  
                regIconString = systemDirectory + "shell32.dll,0";
            }
        }
        else
        {
            //直接指定为文件夹图标  
            regIconString = systemDirectory + "shell32.dll,3";
        }
        string[] fileIcon = regIconString.Split(new char[] { ',' });
        if (fileIcon.Length != 2)
        {
            //系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标  
            fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
        }
        Icon resultIcon = null;
        try
        {
            //调用API方法读取图标  
            int[] phiconLarge = new int[1];
            int[] phiconSmall = new int[1];
            uint count = Win32.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
            IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);
            resultIcon = Icon.FromHandle(IconHnd);
        }
        catch { }
        return resultIcon;
    }
}


/// <summary>  
/// 定义调用的API方法  
/// </summary>  
class Win32
{
    [DllImport("shell32.dll")]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, int[] phiconLarge, int[] phiconSmall, uint nIcons);
}