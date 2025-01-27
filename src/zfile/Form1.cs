using CmdProcessor;
using Sheng.Winform.Controls;
using System.Runtime.InteropServices; // Add this namespace
using WinShell;
using Keys = System.Windows.Forms.Keys;//引入CmdProcessor命名空间

namespace WinFormsApp1
{
	public partial class Form1 : Form
    {
        public readonly IconManager iconManager = new();
        private readonly ThemeManager themeManager;
        private readonly FilePreviewManager previewManager = new();
        private readonly FileSystemManager fsManager = new();
        private readonly UIControlManager uiManager;
   

        private Dictionary<Keys, string> hotkeyMappings;
        // 声明新的 TextBox 控件
        private readonly ShengAddressBarStrip leftPathTextBox = new();
        private readonly ShengAddressBarStrip rightPathTextBox = new();

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
        //private readonly ListBox leftBookmarkList = new();

        private readonly TreeView rightTree = new();
        private readonly ListView rightList = new();
        private readonly TextBox rightPreview = new();
        //private readonly ListBox rightBookmarkList = new();

        private TreeNode? selectedNode = null; // 添加可空标记

        // 添加排序状态追踪
        private int sortColumn = -1;
        private SortOrder sortOrder = SortOrder.None;

        private readonly SplitContainer leftTreeListSplitter = new();
        private readonly SplitContainer rightTreeListSplitter = new();

        // 声明状态栏控件
        private readonly StatusStrip leftStatusStrip = new();
        private readonly StatusStrip rightStatusStrip = new();

        FileInfoList fileList;

        private readonly ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        public CmdProc cmdProcessor;
        
        
        private ImageList treeViewImageList;
        private WinShell.IShellFolder iDeskTop;

        // Form1
        // ├── mainContainer(SplitContainer)
        // │   ├── leftPanel(SplitContainer)
        // │   │   ├── leftUpperPanel(Panel)
        // │   │   │   ├── leftDriveBox(ComboBox)
        // │   │   │   └── leftPathTextBox(ShengAddressBarStrip)
        // │   │   ├── leftTreeListSplitter(SplitContainer)
        // │   │   │   ├── leftTree(TreeView)
        // │   │   │   └── leftList(ListView)
        // │   │   ├── leftBookmarkPanel(FlowLayoutPanel)
        // │   │   └── leftPreview(TextBox)
        // │   └── rightPanel(SplitContainer)
        // │       ├── rightUpperPanel(Panel)
        // │       │   ├── rightDriveBox(ComboBox)
        // │       │   └── rightPathTextBox(ShengAddressBarStrip)
        // │       ├── rightTreeListSplitter(SplitContainer)
        // │       │   ├── rightTree(TreeView)
        // │       │   └── rightList(ListView)
        // │       ├── rightBookmarkPanel(FlowLayoutPanel)
        // │       └── rightPreview(TextBox)
        // ├── leftStatusStrip(StatusStrip)
        // └── rightStatusStrip(StatusStrip)

        public Form1()
        {
            InitializeComponent();
            InitializeContextMenu();
            this.Size = new Size(1200, 800);

            uiManager = new UIControlManager(
                this,
                mainContainer,
                leftPanel,
                rightPanel,
                leftUpperPanel,
                rightUpperPanel,
                leftDrivePanel,
                rightDrivePanel,
                leftDriveBox,
                rightDriveBox,
                leftTree,
                leftList,
                leftPreview,
                rightTree,
                rightList,
                rightPreview,
                leftPathTextBox,
                rightPathTextBox,
                leftStatusStrip,
                rightStatusStrip,
                leftTreeListSplitter,
                rightTreeListSplitter
            );

            uiManager.InitializeLayout();
            uiManager.InitializeDriveComboBoxes();
            uiManager.InitializeTreeViews();
            uiManager.InitializeListViews();
            activeListView = leftList;  //default active view is left list view
            activeTreeview = leftTree;
            uiManager.InitializePreviewPanels();
            uiManager.InitializeStatusStrips();
            InitializeFileSystemWatcher();
            InitializeThemeToggleButton();
            uiManager.InitializeToolStrip();
            uiManager.InitializeDynamicMenu();
            cmdProcessor = new CmdProc(this);
            uiManager.InitializeDynamicToolbar();
            uiManager.InitializeTreeViewIcons();
            InitializeHotkeys();
            uiManager.InitializeBookmarkLists();
            Helper.GetSpecPathFromReg();
            Helper.getEnv();

            // 初始化ThemeManager
            themeManager = new ThemeManager(
                this, uiManager.dynamicToolStrip, uiManager.dynamicMenuStrip,
                leftTree, rightTree,
                leftList, rightList,
                leftPreview, rightPreview,
                leftStatusStrip, rightStatusStrip
            );
        }
      
        private void InitializeHotkeys()
        {
            hotkeyMappings = new Dictionary<Keys, string>
            {
                { Keys.F3, "cm_List" },
                { Keys.F4, "cm_Edit" },
                { Keys.F5, "cm_Copy" },
                { Keys.F6, "cm_Move" },
                { Keys.F7, "cm_NewFolder" },
                { Keys.F8, "cm_Delete" },
                { Keys.F9, "cm_ExecuteDOS" },
                { Keys.Alt | Keys.X, "cm_Exit" }
            };

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (hotkeyMappings.TryGetValue(e.KeyData, out string cmdName))
            {
                cmdProcessor.processCmdByName(cmdName);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.T)
            {
                AddCurrentPathToBookmarks();
                e.Handled = true;
            }
        }

        private void AddCurrentPathToBookmarks()
        {
            if (string.IsNullOrEmpty(currentDirectory)) return;
            var bookmarkPanel = activeTreeview == leftTree ? uiManager.leftBookmarkPanel : uiManager.rightBookmarkPanel;

            if (!bookmarkPanel.Controls.OfType<Label>().Any(label => label.Text == currentDirectory))
            {
                var bookmarkLabel = new Label
                {
                    Text = currentDirectory,
                    AutoSize = true,
                    Padding = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle
                };
                bookmarkLabel.MouseClick += BookmarkLabel_MouseClick;

                bookmarkPanel.Controls.Add(bookmarkLabel);
                bookmarkPanel.Refresh(); // 确保控件刷新
            }
        }

        private void BookmarkLabel_MouseClick(object? sender, MouseEventArgs e)
        {
            if (sender is Label bookmarkLabel)
            {
                if (e.Button == MouseButtons.Left)
                {
                    // 左键点击 - 处理书签点击事件
                    MessageBox.Show($"书签点击: {bookmarkLabel.Text}", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // 右键点击 - 删除书签
                    var bookmarkPanel = bookmarkLabel.Parent as FlowLayoutPanel;
                    bookmarkPanel?.Controls.Remove(bookmarkLabel);
                }
            }
        }

     

        public void OpenOptions()
        {
            // 打开Options窗口
            OptionsForm optionsForm = new OptionsForm(hotkeyMappings.ToDictionary(kvp => kvp.Value.ToString(), kvp => kvp.Key), this);
            if (optionsForm.ShowDialog() == DialogResult.OK)
            {
                // 更新热键映射
                hotkeyMappings = optionsForm.commandHotkeys.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            }
        }

        private void InitializeTreeViewIcons()
        {
            treeViewImageList = new ImageList();
            treeViewImageList.ImageSize = new Size(16, 16);

            Icon folderIcon = Helper.GetIconByFileType("folder", false);
            if (folderIcon != null)
            {
                treeViewImageList.Images.Add("folder", folderIcon);
            }
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

        public void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView Tree1 = sender as TreeView;
                Tree1.SelectedNode = Tree1.GetNodeAt(e.X, e.Y);
            }
        }

        public void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView treeView = sender as TreeView;
                TreeNode node = treeView.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    treeView.SelectedNode = node;
                    //ShowContextMenu(treeView, node.Tag.ToString(), e.Location);
                    ShowContextMenu1(node, e.Location);
                }
            }
        }
        private void showCtxMenu(TreeNode parentNode, string path, Point location)
        {
            // 先获取路径的父目录
            path = Helper.getFSpath(path);

            var parentFolder = iDeskTop;
            IntPtr pidl;
            if (Directory.Exists(path))
            {
                // 如果是文件夹,直接获取其 PIDL
                pidl = w32.ILCreateFromPath(path);
            }
            else
            {
                // 如果是文件,先获取其父文件夹
                var parentPath = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                parentFolder = w32.GetParentFolder(parentPath);
                API.GetShellFolder(parentFolder, fileName, out pidl, false);
            }
            //}

            if (pidl == IntPtr.Zero)
            {
                MessageBox.Show("无法获取文件 PIDL");
                return;
            }

            // 存放 PIDL 的数组
            IntPtr[] pidls = new IntPtr[1];
            pidls[0] = pidl;

            try
            {
                // 得到 IContextMenu 接口
                IntPtr iContextMenuPtr = IntPtr.Zero;
                iContextMenuPtr = parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length,
                    pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);

                if (iContextMenuPtr == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取上下文菜单接口");
                    return;
                }

                WinShell.IContextMenu iContextMenu = (WinShell.IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);

                // 提供一个弹出式菜单的句柄
                IntPtr contextMenu = API.CreatePopupMenu();
                iContextMenu.QueryContextMenu(contextMenu, 0,
                    API.CMD_FIRST, API.CMD_LAST, CMF.NORMAL | CMF.EXPLORE);

                // 弹出菜单
                uint cmd = API.TrackPopupMenuEx(contextMenu, TPM.RETURNCMD,
                    MousePosition.X, MousePosition.Y, this.Handle, IntPtr.Zero);

                // 获取命令序号,执行菜单命令
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
            finally
            {
                if (pidl != IntPtr.Zero)
                {
                    w32.ILFree(pidl);
                }
            }
        }
        private void ShowContextMenu1(TreeNode node, Point location)
        {
            //获得当前节点的 PIDL
            ShellItem sItem = (ShellItem)node.Tag;
            IntPtr PIDL = sItem.PIDL;

            //获得父节点的 IShellFolder 接口
            WinShell.IShellFolder IParent = iDeskTop;
            if (node.Parent != null)
            {
                IParent = ((ShellItem)node.Parent.Tag).ShellFolder;
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

        public void myShellExe()
        {
            w32.ShellExecute(IntPtr.Zero, "open", "cmd.exe", "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
            //Window wnd = Window.GetWindow(this); //获取当前窗口
            //var wih = new WindowInteropHelper(wnd); //该类支持获取hWnd
            //IntPtr hWnd = wih.Handle;    //获取窗口句柄
            //var result = ShellExecute(hWnd, "open", "需要打开的路径如C:\\Users\\Desktop\\xx.exe", null, null, (int)ShowWindowCommands.SW_SHOW);
        }
        public void TreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
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

        public void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
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
                        LoadListView(e.Node, listView, true);
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
        public void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.FirstNode.Text == "...")
            {
                LoadSubDirectories(e.Node);
            }
        }
        public void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
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
                    //var path = GetFullPath(e.Node);	//bugfix: d:资料->d:\"my document", convert some display name to real path

                    var path = Helper.getFSpathbyTree(e.Node);
                    if (string.IsNullOrEmpty(path)) return;
                    LoadListViewByFilesystem(path, listView);
                    currentDirectory = path;
                    selectedNode = e.Node;

                    // 更新监视器
                    if (Directory.Exists(path))
                    {
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }

                    // 调用leftpathtextbox的setaddress方法来更新路径


                    if (treeView == leftTree)
                        leftPathTextBox.SetAddress(path);
                    else
                        rightPathTextBox.SetAddress(path);
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
                ClearNodeHighlight(node);
        }
        private void ClearNodeHighlight(TreeNode node)
        {
            node.BackColor = SystemColors.Window;
            node.ForeColor = SystemColors.WindowText;
            foreach (TreeNode childNode in node.Nodes)
                ClearNodeHighlight(childNode);
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

        public void LoadDriveIntoTree(TreeView treeView, string drivePath)
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
     
        public void ListView_MouseDown(object sender, MouseEventArgs e)
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

        public void ListView_MouseMove(object sender, MouseEventArgs e)
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
        public void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;
                if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
                    SelectItemsInRectangle(activeListView, selectionRectangle);
                activeListView.Invalidate();
                selectionRectangle = Rectangle.Empty;
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

                    var tree1 = listView == leftList ? leftTree : rightTree;
                    // Find corresponding TreeNode for the clicked ListView item
                    TreeNode? node = tree1.SelectedNode;
                    if (node != null)
                    {
                        // Get the full path by combining current directory and selected item name
                        string iPath = Path.Combine(currentDirectory, item.Text);

                        // Get corresponding TreeNode for this path
                        TreeNode? targetNode = FindTreeNode(tree1.Nodes, iPath);
                        if (targetNode != null)
                        {
                            // Show context menu for this node
                            ShowContextMenu1(targetNode, e.Location);
                        }
                        else
                        {
                            // If no corresponding node found, use path to show context menu
                            TreeNode? parentNode = FindTreeNode(tree1.Nodes, currentDirectory);
                            showCtxMenu(parentNode, iPath, e.Location);
                        }
                    }
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
                itemPath = Helper.getFSpath(itemPath);
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
        public void ListView_MouseDoubleClick(object? sender, MouseEventArgs e)
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
                if (fullPath.EndsWith(node.Text))
                {
                    return node;
                }

                var i = (ShellItem)node.Tag;
                if (fullPath.Contains(node.Text) || node.Text.Equals("桌面"))
                {
                    LoadSubDirectories(node);
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


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var selectedDrive = leftDriveBox.SelectedItem?.ToString();
            var listView = selectedDrive != null && watcher.Path.StartsWith(selectedDrive) ? leftList : rightList;
            //LoadListView(watcher.Path, listView);
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
        private void LoadListView(TreeNode node, ListView listView, bool includefile = false)
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

            var flag = includefile ? SHCONTF.FOLDERS | SHCONTF.NONFOLDERS : SHCONTF.FOLDERS;
            // 加载文件夹和文件
            if (root.EnumObjects(this.Handle, flag, out EnumPtr) == API.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == API.S_FALSE)
                {
                    string name = API.GetNameByIShell(root, pidlSub);
                    string pth = API.GetPathByIShell(root, pidlSub);
                    WinShell.IShellFolder iSub;
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);
                    //Icon icon = IconHelper.GetIconByFileType(name.Contains(':') ? "folder" : Path.GetExtension(name), false);
                    var fiwi = new FileInfoWithIcon(name);
                    var icon = fiwi.smallIcon != null ? fiwi.smallIcon : Helper.GetIconByFileName("FILE", name);
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
    
        // 加载文件列表
        private void LoadListViewByFilesystem(string path, ListView listView)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!path.Contains(':')) return;
            path = Helper.getFSpath(path);
            if (path.EndsWith(':'))
                path += "\\";

            try
            {
                path = Helper.getFSpathbyList(path);
                var items = fsManager.GetDirectoryContents(path);

                listView.BeginUpdate();
                listView.Items.Clear();

                foreach (var item in items)
                {
                    if ((item.Attributes & FileAttributes.Hidden) != 0) continue;

                    var lvItem = CreateListViewItem(item);
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
		private async Task LoadListViewByFilesystemAsync(string path, ListView listView)
		{
			if (string.IsNullOrEmpty(path)) return;
			if (!path.Contains(':')) return;
			path = Helper.getFSpath(path);
			if (path.EndsWith(':'))
				path += "\\";

			try
			{
				path = Helper.getFSpathbyList(path);
				var items = await Task.Run(() => fsManager.GetDirectoryContents(path));

				listView.BeginUpdate();
				listView.Items.Clear();

				foreach (var item in items)
				{
					if ((item.Attributes & FileAttributes.Hidden) != 0) continue;

					var lvItem = CreateListViewItem(item);
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
		private ListViewItem? CreateListViewItem(FileSystemInfo item)
        {
            try
            {
                string[] itemData;
                if (item is DirectoryInfo)
                {
                    itemData = new[]
                    {
                        item.Name,
                        "",
                        "<DIR>",
                        "文件夹",
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    };
                }
                else if (item is FileInfo fileInfo)
                {
                    itemData = new[]
                    {
                        item.Name,
                        "",
                        fsManager.FormatFileSize(fileInfo.Length),
                        fileInfo.Extension.ToUpperInvariant(),
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    };
                }
                else
                {
                    return null;
                }

                return new ListViewItem(itemData);
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
        public async void ListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ListView listView) return;
            var previewPanel = listView == leftList ? leftPreview : rightPreview;

            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView.SelectedItems[0];
                string filePath = Helper.getFSpath(Path.Combine(currentDirectory, selectedItem.Text));

                if (File.Exists(filePath))
                {
                    await PreviewFileAsync(filePath, previewPanel);
                }
            }
        }

        public void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
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
            ToolStripButton themeToggleButton = new ToolStripButton
            {
                Text = "切换主题",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            themeToggleButton.Click += ThemeToggleButton_Click;
            uiManager.dynamicToolStrip.Items.Add(themeToggleButton);
        }

        private void ThemeToggleButton_Click(object? sender, EventArgs e)
        {
            if (BackColor == SystemColors.Control)
            {
                themeManager.ApplyDarkTheme();
            }
            else
            {
                themeManager.ApplyLightTheme();
            }
        }
        public void ThemeToggle()
        {
            if (BackColor == SystemColors.Control)
            {
                themeManager.ApplyDarkTheme();
            }
            else
            {
                themeManager.ApplyLightTheme();
            }
        }

        // 查看按钮点击处理逻辑
        public void ViewButton_Click(object? sender, EventArgs e)
        {
            do_cm_list();
        }

        public void do_cm_list()
        {
            var listView = leftList.Focused ? leftList : rightList;
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var filePath = Helper.getFSpath(Path.Combine(currentDirectory, selectedItem.Text));

            if (File.Exists(filePath))
            {
                Form viewerForm = new Form
                {
                    Text = $"查看文件 - {selectedItem.Text}",
                    Size = new Size(800, 600)
                };

                Control viewerControl = previewManager.CreatePreviewControl(filePath);
                viewerForm.Controls.Add(viewerControl);
                viewerForm.Show();
            }
        }

        public void EditButton_Click(object? sender, EventArgs e)
        {
            // 编辑按钮点击处理逻辑
        }

        public void CopyButton_Click(object? sender, EventArgs e)
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
                    fsManager.CopyDirectory(sourcePath, targetPath);
                }
                else
                {
                    File.Copy(sourcePath, targetPath);
                }

                RefreshTreeViewAndListView(leftTree, leftList, leftDriveBox.SelectedItem?.ToString() ?? string.Empty);
                RefreshTreeViewAndListView(rightTree, rightList, rightDriveBox.SelectedItem?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void DeleteButton_Click(object? sender, EventArgs e)
        {
            var listView = leftList.Focused ? leftList : rightList;
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            fsManager.DeleteFile(itemPath);
            listView.Items.Remove(selectedItem);
        }

        public void FolderButton_Click(object? sender, EventArgs e)
        {
            var listView = leftList.Focused ? leftList : rightList;
            var treeView = leftList.Focused ? leftTree : rightTree;

            if (selectedNode == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称:", "新建文件夹", "新文件夹");
            if (string.IsNullOrWhiteSpace(input)) return;

            string newFolderPath = Path.Combine(currentDirectory, input);
            fsManager.CreateDirectory(newFolderPath);
            RefreshTreeViewAndListView(treeView, listView, currentDirectory);
        }

        public void MoveButton_Click(object? sender, EventArgs e)
        {
            var sourceListView = leftList.Focused ? leftList : rightList;
            var targetTreeView = leftList.Focused ? rightTree : leftTree;

            if (sourceListView.SelectedItems.Count == 0 || targetTreeView.SelectedNode == null) return;

            var selectedItem = sourceListView.SelectedItems[0];
            var sourcePath = Path.Combine(currentDirectory, selectedItem.Text);
            var targetPath = Path.Combine(targetTreeView.SelectedNode.Tag.ToString() ?? string.Empty, selectedItem.Text);

            fsManager.MoveFileOrDirectory(sourcePath, targetPath);
            RefreshTreeViewAndListView(leftTree, leftList, leftDriveBox.SelectedItem?.ToString() ?? string.Empty);
            RefreshTreeViewAndListView(rightTree, rightList, rightDriveBox.SelectedItem?.ToString() ?? string.Empty);
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

        public void TerminalButton_Click(object? sender, EventArgs e)
        {
            // 终端按钮点击处理逻辑
        }

        public void ExitButton_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void OpenCommandPrompt()
        {
            try
            {
                System.Diagnostics.Process.Start("cmd.exe");
                //w32.ShellExecute(IntPtr.Zero, "open", "notepad.exe", "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
                //WinExec(path, 1);
                //System.Diagnostics.Process.Start(path);
                //System.Diagnostics.Process.Start("explorer.exe", path);
                //System.Diagnostics.Process.Start("cmd.exe", "/c start explorer.exe /select," + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开命令提示符: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    

        public void MenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                MessageBox.Show($"点击了菜单项: {menuItem.Text}", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cmdProcessor.processCmdByName(menuItem.Text);
            }
        }
     
    }
}
