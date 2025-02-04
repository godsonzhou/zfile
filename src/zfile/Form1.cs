using CmdProcessor;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
using Keys = System.Windows.Forms.Keys;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {

        private readonly ThemeManager themeManager;
        private readonly FilePreviewManager previewManager = new();
        private readonly FileSystemManager fsManager = new();
        public readonly UIControlManager uiManager;

        private Dictionary<Keys, string> hotkeyMappings;
        private bool isSelecting = false;
        private Point selectionStart;
        private Rectangle selectionRectangle;
        private ListView activeListView;
        private TreeView activeTreeview;
        private readonly FileSystemWatcher watcher = new();
        private string currentDirectory = "";
        private TreeNode? selectedNode = null;
        private int sortColumn = -1;
        private SortOrder sortOrder = SortOrder.None;
        private readonly ContextMenuStrip contextMenuStrip = new();
        public CmdProc cmdProcessor;
        private IShellFolder iDeskTop;
        private Dictionary<string, string> specFolderPaths = new();
        private string[] draggedItems;

        public Form1()
        {
            InitializeComponent();
            this.Size = new Size(1200, 800);

            cmdProcessor = new CmdProc(this);

            // 创建UIManager并初始化
            uiManager = new UIControlManager(this);
            uiManager.InitializeUI();

            // 设置活动视图
            activeListView = uiManager.LeftList;
            activeTreeview = uiManager.LeftTree;

            // 其他初始化
            InitializeFileSystemWatcher();
            InitializeHotkeys();

            // 初始化主题管理器
            themeManager = new ThemeManager(
                this,
                uiManager.toolbarManager.DynamicToolStrip,
				uiManager.vtoolbarManager.DynamicToolStrip,
				uiManager.dynamicMenuStrip,
                uiManager.LeftTree,
                uiManager.RightTree,
                uiManager.LeftList,
                uiManager.RightList,
                uiManager.LeftPreview,
                uiManager.RightPreview,
                uiManager.LeftStatusStrip,
                uiManager.RightStatusStrip
            );
            specFolderPaths = Helper.GetSpecFolderPaths();
            uiManager.LeftList.ItemDrag += ListView_ItemDrag;
            uiManager.RightList.ItemDrag += ListView_ItemDrag;
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
        public void ListView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            var listView = sender as ListView;
            if (listView?.SelectedItems.Count == 0) return;

            // 收集拖拽项路径
            draggedItems = listView.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => Path.Combine(currentDirectory, item.Text))
                .ToArray();

            // 启动拖拽操作
            listView.DoDragDrop(new DataObject(DataFormats.FileDrop, draggedItems), DragDropEffects.Copy);
        }

        private string GetTreeNodePath(TreeNode node)
        {
            return Helper.getFSpathbyTree(node);
        }
        public void TreeView_DragOver(object? sender, DragEventArgs e)
        {
            // 检查目标是否为有效文件系统路径
            var treeView = sender as TreeView;
            treeView.Update();
            // 将屏幕坐标转换为 TreeView 控件内的坐标
            var clientPoint = treeView.PointToClient(new Point(e.X, e.Y));
            // 使用 GetNodeAt 获取目标节点
            var targetNode = treeView.GetNodeAt(clientPoint);
            if (targetNode != null)
            {
                Debug.Print("target node :{0} ", targetNode.FullPath);
                var targetPath = GetTreeNodePath(targetNode);
                if (!targetPath.Equals(string.Empty)) Debug.Print("targetpath : {0}", targetPath);
                bool isValid = Helper.IsValidFileSystemPath(targetPath);
                e.Effect = isValid ? DragDropEffects.Copy : DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.None;
        }
        private string GetListItemPath(ListViewItem item)
        {
            var path = Path.Combine(currentDirectory, item.Text);      //todo: debug it
            Debug.Print(path);
            return path;
        }
        public void ListView_DragOver(object? sender, DragEventArgs e)
        {
            // 检查目标是否为有效文件系统路径
            var listView = sender as ListView;
            listView.Update();
            // 将屏幕坐标转换为 TreeView 控件内的坐标
            var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
            // 使用 GetNodeAt 获取目标节点
            var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);
            if (targetItem != null)
            {
                //Debug.Print("target node :{0} ", targetItem.FullPath);
                var targetPath = GetListItemPath(targetItem);
                if (!targetPath.Equals(string.Empty)) Debug.Print("targetpath : {0}", targetPath);
                bool isValid = Helper.IsValidFileSystemPath(targetPath);
                e.Effect = isValid ? DragDropEffects.Copy : DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.None;
        }

        public void TreeView_DragDrop(object? sender, DragEventArgs e)
        {
            Debug.Print("treeview_dragdrop");

            var treeView = sender as TreeView;
            var clientPoint = treeView.PointToClient(new Point(e.X, e.Y));
            var targetNode = treeView.GetNodeAt(clientPoint);
            if (targetNode == null) return;
            var targetPath = GetTreeNodePath(targetNode);
            if (targetNode == null || !Helper.IsValidFileSystemPath(targetPath)) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                draggedItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            }
            if (draggedItems == null || !Helper.IsValidFileSystemPath(targetPath)) return;

            // 复制文件/目录到目标路径
            foreach (var sourcePath in draggedItems)
            {
                try
                {
                    var destPath = Path.Combine(targetPath, Path.GetFileName(sourcePath));
                    if (Directory.Exists(sourcePath))
                        fsManager.CopyDirectory(sourcePath, destPath);
                    else
                        File.Copy(sourcePath, destPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // 刷新目标视图
            var listView = treeView == uiManager.LeftTree ? uiManager.LeftList : uiManager.RightList;
            LoadListView(targetNode, listView);
        }
        public void ListView_DragDrop(object? sender, DragEventArgs e)
        {
            Debug.Print("listview_dragdrop");

            var listView = sender as ListView;
            var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
            var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);
            if (targetItem == null) return;
            var targetPath = GetListItemPath(targetItem);
            if (targetItem == null || !Helper.IsValidFileSystemPath(targetPath)) return;
            //if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //	draggedItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            //}
            if (draggedItems == null || !Helper.IsValidFileSystemPath(targetPath)) return;

            // 复制文件/目录到目标路径
            foreach (var sourcePath in draggedItems)
            {
                try
                {
                    var destPath = Path.Combine(targetPath, Path.GetFileName(sourcePath));
                    if (Directory.Exists(sourcePath))
                        fsManager.CopyDirectory(sourcePath, destPath);
                    else
                        File.Copy(sourcePath, destPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // 刷新目标视图
            listView.Refresh();
        }
        private void AddCurrentPathToBookmarks()
        {
            if (string.IsNullOrEmpty(currentDirectory)) return;
            var bookmarkPanel = activeTreeview == uiManager.LeftTree ? uiManager.leftBookmarkPanel : uiManager.rightBookmarkPanel;

            if (!bookmarkPanel.Controls.OfType<Label>().Any(label => label.Text == currentDirectory))
            {
                var bookmarkLabel = new Label
                {
                    Text = currentDirectory,
                    AutoSize = true,
                    Padding = new Padding(2),
                    BorderStyle = BorderStyle.FixedSingle
                };
                bookmarkLabel.MouseClick += BookmarkLabel_MouseClick;

                bookmarkPanel.Controls.Add(bookmarkLabel);
                bookmarkPanel.Refresh();
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


        private void InitializeContextMenu()
        {
            // 初始化ContextMenuStrip
            contextMenuStrip.Opening += ContextMenuStrip_Opening;
        }
        public void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 在这里可以添加自定义的菜单项
        }
        // private void 加载文件ToolStripMenuItem_Click(object sender, EventArgs e)
        // {
        //     FolderBrowserDialog dlg = new FolderBrowserDialog();
        //     //if (dlg.ShowDialog() == DialogResult.OK)
        //     {
        //         string[] filespath = Directory.GetFiles(dlg.SelectedPath);
        //         fileList = new FileInfoList(filespath);
        //         InitListView();
        //     }
        // }
        // private void InitListView()
        // {
        //     activeListView.Items.Clear();
        //     this.activeListView.BeginUpdate();
        //     foreach (FileInfoWithIcon file in fileList.list)
        //     {
        //         ListViewItem item = new ListViewItem();
        //         item.Text = file.fileInfo.Name.Split('.')[0];
        //         item.ImageIndex = file.iconIndex;
        //         item.SubItems.Add(file.fileInfo.LastWriteTime.ToString());
        //         item.SubItems.Add(file.fileInfo.Extension.Replace(".", ""));
        //         item.SubItems.Add(string.Format(("{0:N0}"), file.fileInfo.Length));
        //         activeListView.Items.Add(item);
        //     }
        //     activeListView.LargeImageList = fileList.imageListLargeIcon;
        //     activeListView.SmallImageList = fileList.imageListSmallIcon;
        //     activeListView.Show();
        //     this.activeListView.EndUpdate();
        // }

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
                Debug.Print("treeview_right mouse button up:");
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
            Debug.Print("showctxmenu:");
            // 先获取路径的父目录
            //path = Helper.getFSpath(path);

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                MessageBox.Show("文件或目录不存在: " + path);
                return;
            }
            //HandleRegistryContextMenuItems(path);
            var parentFolder = iDeskTop;
            IntPtr pidl;
            var strpath = string.Empty;
            if (Directory.Exists(path))
            {
                // 如果是文件夹,直接获取其 PIDL
                pidl = API.ILCreateFromPath(path);
                strpath = path;
            }
            else
            {
                // 如果是文件,先获取其父文件夹
                var parentPath = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                parentFolder = w32.GetParentFolder(parentPath);
                w32.GetShellFolder(parentFolder, fileName, out pidl, false);
                strpath = parentPath;
            }

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
                iContextMenuPtr = parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);
                if (iContextMenuPtr == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取上下文菜单接口");
                    return;
                }

                IContextMenu iContextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);
                // 提供一个弹出式菜单的句柄
                IntPtr contextMenu = API.CreatePopupMenu();
                iContextMenu.QueryContextMenu(contextMenu, 0,
                    w32.CMD_FIRST, w32.CMD_LAST, CMF.NORMAL | CMF.EXPLORE);
                //var str = new StringBuilder(256);
                //iContextMenu.GetCommandString(w32.CMD_FIRST, GetCommandStringInformations.VERB, IntPtr.Zero, str, 0);
                //Debug.Print("cmdstr:{0}", str);
                // 弹出菜单
                uint cmd = API.TrackPopupMenuEx(contextMenu, TPM.RETURNCMD,
                    MousePosition.X, MousePosition.Y, this.Handle, IntPtr.Zero);

                // 获取命令序号,执行菜单命令
                if (cmd >= w32.CMD_FIRST)
                    ContextMenuHandler.InvokeCommand(iContextMenu, cmd, strpath, new POINT(MousePosition.X, MousePosition.Y));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行命令时出错: {ex.Message}", "错误");
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                {
                    API.ILFree(pidl);
                }
            }
        }
        private void HandleRegistryContextMenuItems(string path)
        {
            string[] registryPaths = new[]
            {
                @"*\shellex\ContextMenuHandlers",
                @"Directory\shell\",
                @"Directory\shellex\ContextMenuHandlers",
                @"Folder\shell",
                @"Folder\shellex\ContextMenuHandlers"
            };

            foreach (string registryPath in registryPaths)
            {
                Guid? guid = ContextMenuHandler.GetContextMenuHandlerGuid(registryPath);
                if (guid.HasValue)
                {
                    object? comObject = ContextMenuHandler.CreateComObject(guid.Value);
                    if (comObject != null)
                    {
                        ContextMenuHandler.InvokeComMethod(comObject, "InvokeCommand", path);
                    }
                }
            }
        }

        private void ShowContextMenu1(TreeNode node, Point location)
        {
            Debug.Print("showcontextmenu1:{0}", node.Text);
            //获得当前节点的 PIDL
            ShellItem sItem = (ShellItem)node.Tag;
            IntPtr PIDL = sItem.PIDL;

            //获得父节点的 IShellFolder 接口
            IShellFolder IParent = iDeskTop;
            if (node.Parent != null)
            {
                IParent = ((ShellItem)node.Parent.Tag).ShellFolder;
            }
            else
            {
                //桌面的真实路径的 PIDL
                string path = w32.GetSpecialFolderPath(this.Handle, ShellSpecialFolders.DESKTOPDIRECTORY);
                w32.GetShellFolder(iDeskTop, path, out PIDL);
            }

            //存放 PIDL 的数组
            IntPtr[] pidls = new IntPtr[1];
            pidls[0] = PIDL;

            //得到 IContextMenu 接口
            IntPtr iContextMenuPtr = IntPtr.Zero;
            iContextMenuPtr = IParent.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);
            IContextMenu iContextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);

            //提供一个弹出式菜单的句柄
            IntPtr contextMenu = API.CreatePopupMenu();
            iContextMenu.QueryContextMenu(contextMenu, 0, w32.CMD_FIRST, w32.CMD_LAST, CMF.NORMAL | CMF.EXPLORE);

            //弹出菜单
            uint cmd = API.TrackPopupMenuEx(contextMenu, TPM.RETURNCMD, MousePosition.X, MousePosition.Y, this.Handle, IntPtr.Zero);

            //获取命令序号，执行菜单命令
            if (cmd >= w32.CMD_FIRST)
            {
                var strpath = Helper.getFSpathbyTree(node);
                ContextMenuHandler.InvokeCommand(iContextMenu, cmd, strpath, new POINT(MousePosition.X, MousePosition.Y));
            }
        }

        public void myShellExe(string path = "c:\\windows\\system32")
        {
            API.ShellExecute(IntPtr.Zero, "open", "cmd.exe", "", path, (int)SW.SHOWNORMAL);
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
                Debug.Print("TreeView_NodeMouseClick：{0}", path);
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    // 如果path是文件夹，则加载子目录
                    LoadSubDirectories(e.Node);

                    // 更新ListView显示
                    if (sender is TreeView treeView)
                    {
                        var listView = treeView == uiManager.LeftTree ? uiManager.LeftList : uiManager.RightList;
                        LoadListView(e.Node, listView, true);
                        currentDirectory = path;
                        selectedNode = e.Node;
                        // 更新监视器
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }
                    e.Node.Expand();
                }
                else
                {
                    //如果不是文件夹，而是比如我的电脑/网上邻居/控制面板等，则通过其他方式打开
                    Debug.Print(GetNodeType(e.Node));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TreeView_NodeMouseClick加载目录失败: {ex.Message}", "错误");
            }
        }
        public void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            Debug.Print("TreeView_BeforeExpand");
            if (e.Node.Nodes.Count == 1 && e.Node.FirstNode.Text == "...")
                LoadSubDirectories(e.Node);
        }
        public void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            Debug.Print("TreeView_AfterSelect");
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

                    var listView = treeView == uiManager.LeftTree ? uiManager.LeftList : uiManager.RightList;
                    LoadListView(e.Node, listView);
                    //var path = GetFullPath(e.Node);	//bugfix: d:资料->d:\"my document", convert some display name to real path
                    var path = Helper.getFSpathbyTree(e.Node);
                    if (string.IsNullOrEmpty(path)) return;
                    LoadListViewByFilesystem(path, listView, e.Node);
                    currentDirectory = path;
                    selectedNode = e.Node;

                    // 更新监视器
                    if (Directory.Exists(path))
                    {
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }

                    // 调用leftpathtextbox的setaddress方法来更新路径
                    Debug.Print("treeview afterselect , set addr: {0}", path);
                    if (treeView == uiManager.LeftTree)
                        uiManager.LeftPathTextBox.SetAddress(path);
                    else
                        uiManager.RightPathTextBox.SetAddress(path);
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
        public void LoadSubDirectories(TreeNode parentNode)
        {
            Debug.Print("LoadSubDirectories:{0}", parentNode.Text);
            ShellItem sItem = (ShellItem)parentNode.Tag;
            if (sItem == null) return;
            IShellFolder root = sItem.ShellFolder;  //需要加载子目录的treenode的ishellfoler接口，用于enumobjects其所有下层子目录和文件
                                                    //bug to be fixed: 增加root是否为正常的folder, 比如 root=迅雷下载时，系统会报异常
            if (root == null || parentNode.Text.Equals("迅雷下载")) return;
            //var p = w32.GetPathByIShell(iDeskTop, sItem.PIDL);
            //var n = w32.GetNameByIShell(iDeskTop, sItem.PIDL);

            // 清除现有子节点，避免重复添加
            parentNode.Nodes.Clear();

            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;		//子节点的pidl
            uint celtFetched;
            if (root.EnumObjects(this.Handle, SHCONTF.FOLDERS, out EnumPtr) == w32.S_OK)    // 循环查找子项
            {
                if (EnumPtr == IntPtr.Zero)  //如果node=程序和功能,则EnumPtr=0，直接返回
                {
                    return;
                }
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == w32.S_FALSE)
                {
                    string name = w32.GetNameByIShell(root, pidlSub);   //子节点name -> 迅雷下载, system (c:)
                    string path = w32.GetPathByIShell(root, pidlSub);   //子节点path -> 此电脑\\迅雷下载, c:\\
                                                                        //Debug.Print(path);
                    IShellFolder iSub;//子节点的ishellfolder接口
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);

                    TreeNode nodeSub = new TreeNode(name);
                    nodeSub.Tag = new ShellItem(pidlSub, iSub); //子节点的tag存放pidl和ishellfolder接口
                    nodeSub.ImageKey = IconManager.GetNodeIconKey(nodeSub);

                    //if(IsChildrenExist(nodeSub))
                    nodeSub.Nodes.Add("...");
                    parentNode.Nodes.Add(nodeSub);
                }
            }
        }
        private void printattr(SFGAO attr)
        {
            //根据attr的枚举类型,强制转换成整形数，再转换成2进制，最后返回字符串类型
            //

        }
        private string GetNodeType(TreeNode node)
        {
            var type = string.Empty;
            if (node.Tag is ShellItem shellItem)
            {
                //SFGAO attributes = SFGAO.FOLDER | SFGAO.FILESYSTEM;
                //shellItem.ShellFolder.GetAttributesOf(1, new[] { shellItem.PIDL }, ref attributes);

                //if ((attributes & SFGAO.FILESYSTEM) != 0)
                //	type += "drives";
                //if ((attributes & SFGAO.FOLDER) != 0)
                //	type += "folder";
                //if ((attributes & SFGAO.LINK) != 0)
                //	type += "link";
                //if ((attributes & SFGAO.STORAGE) != 0)
                //	type += "storage";
                //type += ((uint)attributes).ToString();

            }
            return type;
        }
        public void LoadDriveIntoTree(TreeView treeView, string drivePath)
        {
            Debug.Print("LoadDriveIntoTree");
            try
            {
                treeView.BeginUpdate();
                treeView.Nodes.Clear();

                //获得桌面 PIDL
                IntPtr deskTopPtr;
                iDeskTop = w32.GetDesktopFolder(out deskTopPtr);

                TreeNode rootNode = new TreeNode("桌面")
                {
                    Tag = new ShellItem(deskTopPtr, iDeskTop),
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
                if (activeListView == uiManager.LeftList)
                    activeTreeview = uiManager.LeftTree;
                else
                    activeTreeview = uiManager.RightTree;
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
            Debug.Print("listview_mouseup:");
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

                    var tree1 = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;
                    // Find corresponding TreeNode for the clicked ListView item
                    TreeNode? node = tree1.SelectedNode;
                    if (node != null)
                    {
                        // Get the full path by combining current directory and selected item name
                        string iPath = Path.Combine(currentDirectory, item.Text);

                        // Get corresponding TreeNode for this path
                        TreeNode? targetNode = FindTreeNode(node.Nodes, item.Text);
                        if (targetNode != null)
                        {
                            ShowContextMenu1(targetNode, e.Location);
                        }
                        else
                        {
                            // If no corresponding node found, use path to show context menu
                            TreeNode? parentNode = (TreeNode)item.Tag;
                            showCtxMenu(parentNode, iPath, e.Location);
                        }
                    }
                }

                return;
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
            if (sender is not ListView listView)
                return;
            ListViewItem item = listView.GetItemAt(e.X, e.Y);
            if (item != null)
                item.Selected = true;
            if (listView.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            Debug.Print("listview_mousedoubleclick:{0}, currentDir={1}", selectedItem.Text, currentDirectory);
            string itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            if (selectedItem.SubItems[3].Text.Equals("<DIR>") || selectedItem.SubItems[3].Text == "本地磁盘")
            {
                //try
                {
                    // 获取关联的TreeView
                    TreeView treeView = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;

                    // 查找并选择对应的TreeNode
                    treeView.SelectedNode.Expand();
                    TreeNode? node = FindTreeNode(treeView.SelectedNode.Nodes, selectedItem.Text);
                    //TreeNode? node = (TreeNode)selectedItem.Tag;
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
                        selectedNode = node;
                        RefreshTreeViewAndListView(treeView, listView, itemPath);
                    }

                    // 更新监视器
                    if (Directory.Exists(itemPath))
                    {
                        currentDirectory = itemPath;    //IF ITEMPATH IS DIR, UPDATE CURRENTDIRECTORY, ELSE NOT
                        watcher.Path = itemPath;
                        watcher.EnableRaisingEvents = true;
                    }
                }
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"访问文件夹失败741: {ex.Message}", "错误");
                //}
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
                            Process.Start(itemPath);
                        }
                        else
                        {
                            // 使用系统默认关联程序打开文件
                            Process.Start(new ProcessStartInfo(itemPath) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件: {ex.Message}", "错误");
                    }
                }
            }
        }

        public TreeNode? FindTreeNode(TreeNodeCollection nodes, string path, bool deepSearch = false)
        {
            Debug.Print("FindTreeNode -> {0}", path);
            foreach (TreeNode node in nodes)
            {
                Debug.Print("FindTreeNode -> node: {0}, {1}", node.Text, node.FullPath);
                //bug fix: node.fullpath=桌面\此电脑\system (C:)\aDrive, path=c:\\
                if (path.Equals(node.Text, StringComparison.OrdinalIgnoreCase)) return node;
                if (!deepSearch) continue;
                if (node.Parent != null && node.Tag != null)
                {
                    var pidl = ((ShellItem)node.Tag).PIDL;
                    var pf = ((ShellItem)(node.Parent.Tag)).ShellFolder;
                    var p = w32.GetPathByIShell(pf, pidl);      ////子节点path -> 此电脑\\迅雷下载, c:\\
                                                                //var n = w32.GetNameByIShell(pf, pidl);    //子节点name -> 迅雷下载, system (c:)
                    if (p.Equals(path, StringComparison.OrdinalIgnoreCase))
                        return node;

                    if (!(p.Equals("此电脑") && path.Contains(":")))
                    {
                        if (!path.Contains(p))
                            continue;
                    }
                }
                LoadSubDirectories(node);
                node.Expand();//todo: 算法改进，这样效率太低，而且会展开之前所有的无关节点

                TreeNode? foundNode = FindTreeNode(node.Nodes, path, deepSearch);
                if (foundNode != null)
                {
                    Debug.Print("FindTreeNode -> foundNode: {0}", foundNode.Text);
                    return foundNode;
                }
            }
            return null;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;//设置该属性 为false

            var selectedDrive = uiManager.LeftDriveBox.SelectedItem?.ToString();
            var listView = selectedDrive != null && watcher.Path.StartsWith(selectedDrive) ? uiManager.LeftList : uiManager.RightList;
            //LoadListView(watcher.Path, listView);
        }
        public void ToolbarButton_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // 只允许可执行文件或目录
                if (files.Any(f => File.Exists(f) && (Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Directory.Exists(f))))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        public void ToolbarButton_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var strip = sender as ToolStrip;//文件可以拖动到按钮上
                if (strip != null && uiManager != null)
                {
                    var bar = strip?.LayoutStyle == ToolStripLayoutStyle.VerticalStackWithOverflow
                                ? uiManager.vtoolbarManager
                                : uiManager.toolbarManager;
                    foreach (string file in files)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            // 获取文件显示名称
                            string displayName = Path.GetFileNameWithoutExtension(file);

                            // 设置按钮参数
                            //var buttonParams = new Dictionary<string, object>
                            //{
                            //    {"Command", "Execute"},
                            //    {"Path", file},
                            //    {"WorkingDirectory", fi.DirectoryName},
                            //    {"Icon", Icon.ExtractAssociatedIcon(file)?.ToBitmap()}
                            //};

                            // 添加或更新工具栏按钮
                            //uiManager.toolbarManager.AddButton(
                            //    displayName,
                            //    buttonParams,
                            //    (s, args) => ExecuteToolbarCommand(file)
                            //);

                            //TODO: 如何判定当前是toolbarmanager还是vtoolbarmanager?
                            // 通过工具栏的停靠方向判断是水平还是垂直工具栏
                            
                            bar.AddButton(displayName, file, file + ",0", "", "", "0");

                            // 设置按钮显示属性
                            //button.Text = displayName;
                            //button.ToolTipText = $"启动 {displayName}";
                            //if (buttonParams["Icon"] is Image icon)
                            //{
                            //    button.Image = icon;
                            //}
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"添加工具栏按钮失败: {ex.Message}");
                        }
                    }

                    // 刷新工具栏
                    //uiManager.toolbarManager.GenerateDynamicToolbar();
                    bar.GenerateDynamicToolbar();
                    //uiManager.dynamicToolStrip.Invalidate();
                    return;
                }
                var button1 = sender as ToolStripButton;//文件可以拖动到按钮上
                if (button1 != null && uiManager != null)
                {
                    //拖到了一个按钮上，执行用这个按钮的CMD 并将选中的文件作为参数传入的逻辑 TODO

                }
            }
        }

        //private void ExecuteToolbarCommand(string filePath)
        //{
        //    try
        //    {
        //        if (Directory.Exists(filePath))
        //        {
        //            Process.Start("explorer.exe", filePath);
        //        }
        //        else if (File.Exists(filePath))
        //        {
        //            Process.Start(new ProcessStartInfo(filePath)
        //            {
        //                UseShellExecute = true,
        //                WorkingDirectory = Path.GetDirectoryName(filePath)
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"执行失败: {ex.Message}", "错误",
        //            MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}
        //}



        public void ExitApp()
        {
            Application.Exit();
        }


        // 驱动器选择变更事件处理
        private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var treeView = comboBox == uiManager.LeftDriveBox ? uiManager.LeftTree : uiManager.RightTree;
            var listView = comboBox == uiManager.LeftDriveBox ? uiManager.LeftList : uiManager.RightList;

            if (comboBox.SelectedItem is string drivePath)
            {
                LoadDriveIntoTree(treeView, drivePath);
                //LoadListView(drivePath, listView);
            }
        }
        //加载选定树节点的子文件夹和文件到listview中
        private void LoadListView(TreeNode node, ListView listView, bool includefile = false)
        {
            Debug.Print("LoadListView");
            if (listView == null) return;
            if (listView.SmallImageList == null)
            {
                listView.SmallImageList = new ImageList();
            }
            ShellItem sItem = (ShellItem)node.Tag;
            IShellFolder root = sItem.ShellFolder;

            // 循环查找子项
            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;
            uint celtFetched;
            listView.BeginUpdate();
            listView.Items.Clear();

            var flag = includefile ? SHCONTF.FOLDERS | SHCONTF.NONFOLDERS : SHCONTF.FOLDERS;
            // 加载文件夹和文件
            if (root.EnumObjects(this.Handle, flag, out EnumPtr) == w32.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == w32.S_FALSE)
                {
                    string name = w32.GetNameByIShell(root, pidlSub);
                    string pth = w32.GetPathByIShell(root, pidlSub);
                    Debug.Print(pth);
                    IShellFolder iSub;
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);
                    //TODO: 目录的图标不正确 bug
                    //Icon icon = IconHelper.GetIconByFileType(name.Contains(':') ? "folder" : Path.GetExtension(name), false);
                    var fiwi = new FileInfoWithIcon(name);
                    //var icon = fiwi.smallIcon != null ? fiwi.smallIcon : Helper.GetIconByFileName("FILE", name);
                    //int iconIndex = listView.SmallImageList.Images.Count;
                    //listView.SmallImageList.Images.Add(icon);

                    string[] s = { "", name, "", name.Contains(':') ? "本地磁盘" : "<DIR>", "" };
                    var i = new ListViewItem(s);
                    //i.ImageIndex = iconIndex;
                    var ico = IconManager.GetIconKey(name);
                    Debug.Print("search ico list{0} - > {1}", name, ico);
                    i.ImageKey = ico;
                    i.Text = name;
                    i.Tag = node;   //tag存放父节点
                    listView.Items.Add(i);
                }
            }

            listView.EndUpdate();
        }
        private bool IsChildrenExist(TreeNode node, bool includefile = false)
        {
            ShellItem sItem = (ShellItem)node.Tag;
            IShellFolder root = sItem.ShellFolder;

            // 循环查找子项
            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;
            uint celtFetched;

            var flag = includefile ? SHCONTF.FOLDERS | SHCONTF.NONFOLDERS : SHCONTF.FOLDERS;
            // 加载文件夹和文件
            if (root.EnumObjects(this.Handle, flag, out EnumPtr) == w32.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == w32.S_FALSE)
                {
                    return true;
                }
            }
            return false;
        }
        // 加载文件列表
        private void LoadListViewByFilesystem(string path, ListView listView, TreeNode parentnode)
        {
            Debug.Print("LoadListViewByFilesystem:{0}", path);
            if (string.IsNullOrEmpty(path)) return;
            if (!path.Contains(':')) return;
            path = Helper.getFSpath(path);
            if (path.EndsWith(':'))
                path += "\\";

            try
            {
                //path = Helper.getFSpathbyList(path);
                var items = fsManager.GetDirectoryContents(path);

                listView.BeginUpdate();
                listView.Items.Clear();

                foreach (var item in items)
                {
                    if ((item.Attributes & FileAttributes.Hidden) != 0) continue;

                    var lvItem = CreateListViewItem(item);//TODO: ADD ICON
                    if (lvItem != null)
                    {
                        lvItem.ImageKey = lvItem.SubItems[3].Text.Equals("<DIR>") ? "folder" : "";
                        Debug.Print("file add to listview ：{0}", item.FullName);
                        lvItem.Tag = parentnode;
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
                        "",
                        "<DIR>",
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
            var previewPanel = listView == uiManager.LeftList ? uiManager.LeftPreview : uiManager.RightPreview;

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
            uiManager.toolbarManager.DynamicToolStrip.Items.Add(themeToggleButton);
        }

        private void ThemeToggleButton_Click(object? sender, EventArgs e)
        {
            ThemeToggle();
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
            var listView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
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
            var sourceListView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
            var targetTreeView = uiManager.LeftList.Focused ? uiManager.RightTree : uiManager.LeftTree;
            var targetListView = uiManager.LeftList.Focused ? uiManager.RightList : uiManager.LeftList;

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

                RefreshTreeViewAndListView(uiManager.LeftTree, uiManager.LeftList, uiManager.LeftDriveBox.SelectedItem?.ToString() ?? string.Empty);
                RefreshTreeViewAndListView(uiManager.RightTree, uiManager.RightList, uiManager.RightDriveBox.SelectedItem?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void DeleteButton_Click(object? sender, EventArgs e)
        {
            var listView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            fsManager.DeleteFile(itemPath);
            listView.Items.Remove(selectedItem);
        }

        public void FolderButton_Click(object? sender, EventArgs e)
        {
            var listView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
            var treeView = uiManager.LeftList.Focused ? uiManager.LeftTree : uiManager.RightTree;

            if (selectedNode == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称:", "新建文件夹", "新文件夹");
            if (string.IsNullOrWhiteSpace(input)) return;

            string newFolderPath = Path.Combine(currentDirectory, input);
            fsManager.CreateDirectory(newFolderPath);
            RefreshTreeViewAndListView(treeView, listView, currentDirectory);
        }

        public void MoveButton_Click(object? sender, EventArgs e)
        {
            var sourceListView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
            var targetTreeView = uiManager.LeftList.Focused ? uiManager.RightTree : uiManager.LeftTree;

            if (sourceListView.SelectedItems.Count == 0 || targetTreeView.SelectedNode == null) return;

            var selectedItem = sourceListView.SelectedItems[0];
            var sourcePath = Path.Combine(currentDirectory, selectedItem.Text);
            var targetPath = Path.Combine(targetTreeView.SelectedNode.Tag.ToString() ?? string.Empty, selectedItem.Text);

            fsManager.MoveFileOrDirectory(sourcePath, targetPath);
            RefreshTreeViewAndListView(uiManager.LeftTree, uiManager.LeftList, uiManager.LeftDriveBox.SelectedItem?.ToString() ?? string.Empty);
            RefreshTreeViewAndListView(uiManager.RightTree, uiManager.RightList, uiManager.RightDriveBox.SelectedItem?.ToString() ?? string.Empty);
        }

        private void RefreshTreeViewAndListView(TreeView treeView, ListView listView, string path)
        {
            Debug.Print("RefreshTreeViewAndListView:{0}", path);
            if (selectedNode != null)
            {
                LoadSubDirectories(selectedNode);
                selectedNode.Expand();
            }
            LoadListView(selectedNode, listView);
            LoadListViewByFilesystem(path, listView, selectedNode);
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
                Process.Start("cmd.exe");
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
