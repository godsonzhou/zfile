using Shell32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinShell;
using Keys = System.Windows.Forms.Keys;
namespace zfile
{
	public partial class Form1 : Form
	{
		const int ILD_TRANSPARENT = 0x00000001;
		public readonly AsyncFTPMGR fTPMGR;
		public readonly LLM_Helper lLM_Helper;
		public readonly CFGLOADER configLoader;
		public readonly CFGLOADER ftpconfigLoader;
		public readonly CFGLOADER cmdicons_configloader;
		public readonly IconManager iconManager = new();
		private readonly ThemeManager themeManager;
		private readonly FilePreviewManager previewManager = new();
		public readonly FileSystemManager fsManager = new();
		public readonly UIControlManager uiManager;
		public readonly ThumbnailManager thumbnailManager = new("d:\\temp\\cache", new Size(64, 64));
		private Dictionary<Keys, string> hotkeyMappings;
		private bool isSelecting = false;
		private Rectangle selectionRectangle;
		public bool isleft => uiManager.isleft;
		public ListView activeListView { get => uiManager.activeListView; }
		public ListView unactiveListView { get => uiManager.unactiveListView; }
		public TreeView activeTreeview { get => uiManager.activeTreeview; }
		public TreeView unactiveTreeview { get => uiManager.unactiveTreeview; }
		public TreeNode leftRoot, rightRoot;
		public TreeNode activeRoot { get => (isleft ? leftRoot : rightRoot); }
		public TreeNode unactiveRoot { get => (!isleft ? leftRoot : rightRoot); }
		private TreeNode thispcL, thispcR;
		public TreeNode activeThispc { get { return isleft ? thispcL : thispcR; } }
		public TreeNode unactiveThispc { get { return !isleft ? thispcL : thispcR; } }

		private readonly FileSystemWatcher watcher = new();
		public string currentDirectory = "";
		private TreeNode? selectedNode = null;
		private int sortColumn = -1;
		private SortOrder sortOrder = SortOrder.None;
		private readonly ContextMenuStrip contextMenuStrip = new();
		public CmdProc cmdProcessor;
		public KeyMgr keyManager;
		private IShellFolder iDeskTop, iCtrlPanel;
		private Dictionary<string, string> specFolderPaths = new();
		private string[] draggedItems;
		private TreeNode rightClickBegin;
		private string oldname;
		public WcxModuleList wcxModuleList;
		public WlxModuleList wlxModuleList;
		private Dictionary<string, IntPtr> openArchives = new Dictionary<string, IntPtr>();
		private Dictionary<string, string> archivePaths = new Dictionary<string, string>();
		// 添加目录历史导航相关的字段
		public Stack<string> backStack = new();    // 后退历史
		public Stack<string> forwardStack = new(); // 前进历史
		private string lastDirectory = string.Empty; // 上一次访问的目录
		public ShellExecuteHelper se;
		private bool shiftKeyPressed, altKeyPressed, ctrlKeyPressed, winKeyPressed;
		public IEnumerable<string> GetRecycleBinFilenames()
		{
			Shell shell = new Shell();
			Folder recycleBin = shell.NameSpace(10);//hell.NameSpace(ShellSpecialFolderConstants.ssfBITBUCKET)

			foreach (FolderItem2 recfile in recycleBin.Items())
			{
				// Filename
				yield return recfile.Name;

				// full recyclepath
				// yield return recfile.Path;
			}

			Marshal.FinalReleaseComObject(shell);
		}
		// 在目录变更时调用此方法记录历史
		public void RecordDirectoryHistory(string newPath)
		{
			if (string.IsNullOrEmpty(currentDirectory) || newPath == currentDirectory)
				return;

			backStack.Push(currentDirectory);
			forwardStack.Clear(); // 清除前进历史
			currentDirectory = newPath;
		}
		public enum TreeSearchScope
		{
			thispc = 0,
			desktop = 1,
			full = 2
		}
		// 导航到指定路径
		public void NavigateToPath(string path, bool recordHistory = true, TreeSearchScope scope = TreeSearchScope.thispc , bool isactive = true)
		{
			//scope : thispc, desktop, full
			if (string.IsNullOrEmpty(path))
				return;
			if (scope == TreeSearchScope.thispc && !Directory.Exists(path))
				return;
			var searchtarget = scope switch
			{
				TreeSearchScope.thispc => isactive ? activeThispc.Nodes : unactiveThispc.Nodes,
				TreeSearchScope.full => isactive ? activeTreeview.Nodes : unactiveTreeview.Nodes,
				TreeSearchScope.desktop => isactive ? activeRoot.Nodes : unactiveRoot.Nodes
			};
			
			var node = FindTreeNode(searchtarget, path);
			if (node != null)
			{
				if (isactive)
				{
					if (recordHistory)
						RecordDirectoryHistory(path);
					else
					{
						// 直接更新当前目录，不记录历史
						currentDirectory = path;
					}
				}
				if (isactive)
				{
					activeTreeview.SelectedNode = node;
					RefreshPanel(activeListView);
				}else
				{
					unactiveTreeview.SelectedNode = node;
					RefreshPanel(unactiveListView);
				}
			}
			// 更新最后访问路径
			if(isactive)
				uiManager.UpdateLastVisitedPath(path);
		}

		public Form1()
		{
			configLoader = new CFGLOADER(Constants.ZfileCfgPath+"wincmd.ini");
			ftpconfigLoader = new CFGLOADER(Constants.ZfileCfgPath + "wcx_ftp.ini");
			cmdicons_configloader = new CFGLOADER(Constants.ZfileCfgPath + "wcmicons.inc");
			fTPMGR = new AsyncFTPMGR(this);
		    cmdProcessor = new CmdProc(this);
			lLM_Helper = new LLM_Helper(this);
			InitializeComponent();
		    this.Size = new Size(1920, 1080);

		    // 初始化COM组件
		    InitializeCOMComponents();
		    keyManager = new KeyMgr();
		    
		    // 创建UIManager并初始化
		    uiManager = new UIControlManager(this);
		    uiManager.InitializeUI();
		    
		    // 创建默认书签
		    uiManager.BookmarkManager.CreateDefaultBookmarks();

			// 设置活动视图
			//isleft = true;
            thumbnailManager.RegisterProvider(ThumbnailGenerator.GetThumbnail);
    
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
			WdxModuleList wdxModuleList = new WdxModuleList("");
			WfxModuleList wfxModuleList = new WfxModuleList("");
			wcxModuleList = new WcxModuleList();
			wcxModuleList.LoadConfiguration();
			wlxModuleList = new WlxModuleList();

			// 初始化FTP管理器扩展
			fTPMGR.Initialize();

			se = new ShellExecuteHelper(this);
		}
         private void InitializeCOMComponents()
        {
            // 初始化COM组件
            IntPtr deskTopPtr;
            w32.InitializeCOM();
            iDeskTop = w32.GetDesktopFolder(out deskTopPtr);
            if (iDeskTop == null)
                throw new Exception("无法初始化桌面Shell接口");
			iCtrlPanel = w32.GetControlPanelFolder();
		}

		protected override void Dispose(bool disposing)
		{
		    if (disposing)
		    {   
		        // 释放其他资源
		        watcher.Dispose();
		        previewManager.Dispose();
		        thumbnailManager.Dispose();
				iconManager.Dispose();
		        uiManager.Dispose();
		        themeManager.Dispose();
		        contextMenuStrip.Dispose();

                // 释放打开的压缩文件句柄
				foreach (var archive in openArchives)
					CloseArchive(archive.Key);
				openArchives.Clear();
				archivePaths.Clear();

				unInitializeHotkeys();
                // 释放托管资源
                if (components != null)
                    components.Dispose();
                
                if (iDeskTop != null)
                {
                    Marshal.ReleaseComObject(iDeskTop);
                    iDeskTop = null;
                }
				if (iCtrlPanel != null)
				{
					Marshal.ReleaseComObject(iCtrlPanel);
					iCtrlPanel = null;
				}
                
                w32.UninitializeCOM();
			}
			base.Dispose(disposing);
		}
		private void unInitializeHotkeys()
		{
			this.KeyDown -= Form1_KeyDown;
			this.KeyUp -= Form1_KeyUp;
		}
		private void InitializeHotkeys()
        {
            hotkeyMappings = new Dictionary<Keys, string>
            {
				{ Keys.F2, "cm_RenameOnly" },
				{ Keys.F3, "cm_List" },
                { Keys.F4, "cm_Edit" },
                { Keys.F5, "cm_Copy" },
                { Keys.F6, "cm_renmov" },
                { Keys.F7, "cm_mkdir" },
                { Keys.F8, "cm_Delete" },
				{ Keys.Delete, "cm_Delete" },
				{ Keys.F9, "cm_ExecuteDOS" },
				{ Keys.Escape, "cm_ClearAll"},
                { Keys.Alt | Keys.X, "cm_Exit" }
            };

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
			this.KeyUp += new KeyEventHandler(Form1_KeyUp);
        }
		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			if(e.Shift)	
				shiftKeyPressed = false;
			if (e.Alt)
				altKeyPressed = false;
			if (e.Control)
				ctrlKeyPressed = false;
			if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
				winKeyPressed = false;
		}
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
			if(e.Shift) 
				shiftKeyPressed = true;
			if (e.Alt) 
				altKeyPressed = true;
			if(e.Control) 
				ctrlKeyPressed = true;
			if(e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
				winKeyPressed = true;
			
			var specKey = (winKeyPressed ? "#" : "") + (altKeyPressed ? "A" : "") + (ctrlKeyPressed ? "C" : "") + (shiftKeyPressed ? "S" : "");
			var mainKey = Helper.ConvertKeyToString(e.KeyCode);
			if (mainKey.Equals(string.Empty))
			{
				e.Handled = true;
				return;
			}
			var cmd = keyManager.GetCmdByKey(specKey.Length != 0 ? $"{specKey}+{mainKey}" : mainKey);
			if(!cmd.Equals(string.Empty))
				cmdProcessor.ExecCmd(cmd);
			else if (hotkeyMappings.TryGetValue(e.KeyData, out string cmdName))
				cmdProcessor.ExecCmd(cmdName);
			e.Handled = true;
		}
        public void ListView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            var listView = sender as ListView;
            if (listView?.SelectedItems.Count == 0) return;

            // 收集拖拽项路径
            draggedItems = listView.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => Helper.GetListItemPath(item))
                .ToArray();
            // 启动拖拽操作
            listView.DoDragDrop(new DataObject(DataFormats.FileDrop, draggedItems), DragDropEffects.Copy);
        }

        private string GetTreeNodePath(TreeNode node)
        {
            return Helper.getFSpathbyTree(node);
        }
		private bool IsValidTarget(TreeView treeView, DragEventArgs e, out string targetPath)
		{
			targetPath = string.Empty;
			if (treeView == null) return false;
			// 将屏幕坐标转换为 TreeView 控件内的坐标
			var clientPoint = treeView.PointToClient(new Point(e.X, e.Y));
			// 使用 GetNodeAt 获取目标节点
			var targetNode = treeView.GetNodeAt(clientPoint);
			if (targetNode == null) {  return false; }
			targetPath = GetTreeNodePath(targetNode);
			return FileSystemManager.IsValidFileSystemPath(targetPath);
		}
        public void TreeView_DragOver(object? sender, DragEventArgs e)
        {
            // 检查目标是否为有效文件系统路径
            var treeView = sender as TreeView;
            treeView?.Update();
            e.Effect = IsValidTarget(treeView, e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
            return;
        }
        public void TreeView_DragDrop(object? sender, DragEventArgs e)
        {
            var treeView = sender as TreeView;     
			if (treeView == null) { return; }
            //if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    draggedItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            //}
            if (draggedItems == null || !IsValidTarget(treeView, e, out string targetPath)) return;
            // 复制文件/目录到目标路径
            foreach (var sourcePath in draggedItems)
            {
                try
                {
                    var destPath = Path.Combine(targetPath, Path.GetFileName(sourcePath));
                    if (Directory.Exists(sourcePath))
                        FileSystemManager.CopyDirectory(sourcePath, destPath);
                    else
                        File.Copy(sourcePath, destPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            // 刷新目标视图
			RefreshPanel(treeView);
        }
		public void ListView_DragOver(object? sender, DragEventArgs e)
		{
			// 检查目标是否为有效文件系统路径
			var listView = sender as ListView;
			listView.Update();
			e.Effect = IsValidTarget(listView, e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
			return;
		}
		private bool IsValidTarget(ListView listView, DragEventArgs e, out string targetPath)
		{
			// 将屏幕坐标转换为 TreeView 控件内的坐标
			var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
			// 使用 GetNodeAt 获取目标节点
			var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);
			if (targetItem != null)
				targetPath = Helper.GetListItemPath(targetItem);
			else
			{
				var targetTree = (listView == uiManager.LeftList) ? uiManager.LeftTree : uiManager.RightTree;
				targetPath = Helper.getFSpathbyTree(targetTree.SelectedNode);
			}
			return FileSystemManager.IsValidFileSystemPath(targetPath);
		}
		public void ListView_DragDrop(object? sender, DragEventArgs e)
        {
			if (draggedItems == null) return;
			var listView = sender as ListView;
			if (!IsValidTarget(listView, e, out string targetPath)) return;
			
			// 复制文件/目录到目标路径
			foreach (var sourcePath in draggedItems)
				FileSystemManager.CopyFilesAndDirectories(sourcePath, targetPath);

            // 刷新目标视图
            listView.Refresh();
			RefreshPanel(listView);
        }
        public void AddCurrentPathToBookmarks()
        {
			//if (string.IsNullOrEmpty(currentDirectory)) return;
			var node = activeTreeview.SelectedNode;
			if(node == null) return;
			uiManager.BookmarkManager.AddBookmark(node, isleft);
        }

        public void OpenOptions()
        {
            // 打开Options窗口
            OptionsForm optionsForm = new OptionsForm(this);
            if (optionsForm.ShowDialog() == DialogResult.OK)
            {
                // 更新热键映射
                //hotkeyMappings = optionsForm.commandHotkeys.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
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
				var fileList = new FileInfoList(filespath);
				InitListView(fileList);
			}
		}
		private void InitListView(FileInfoList fileList)
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
        public void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView Tree1 = sender as TreeView;
                rightClickBegin = Tree1.GetNodeAt(e.X, e.Y);
				if (Tree1.SelectedNode != rightClickBegin)
					Tree1.SelectedNode = rightClickBegin;
            }
        }

        public void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView treeView = sender as TreeView;
                TreeNode node = treeView.GetNodeAt(e.X, e.Y);
                if (node != null && node == rightClickBegin)
                {
                    treeView.SelectedNode = node;
                    ShowContextMenuOnTreeview(node, e.Location);
                }
            }
        }

        private void ShowCtxMenuOnListview(string path, Point location)
        {
            // 先获取路径的父目录
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
				try
				{
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
				finally
				{
					Marshal.ReleaseComObject(iContextMenu);
					if (iContextMenuPtr != IntPtr.Zero)
						Marshal.Release(iContextMenuPtr);
				}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行命令时出错: {ex.Message}", "错误");
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                    API.ILFree(pidl);
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

        private void ShowContextMenuOnTreeview(TreeNode node, Point location)
        {
			if(node.Tag is not ShellItem)
			{
				//ftp node process
				return;
			}
			//获得当前节点的 PIDL
			ShellItem sItem = (ShellItem)node.Tag;
            IntPtr PIDL = sItem.PIDL;

            //获得父节点的 IShellFolder 接口
            IShellFolder IParent = iDeskTop;
            if (node.Parent != null)
                IParent = ((ShellItem)node.Parent.Tag).ShellFolder;
            else
            {
                //桌面的真实路径的 PIDL
                string path = w32.GetSpecialFolderPath(this.Handle, ShellSpecialFolders.DESKTOPDIRECTORY);
                w32.GetShellFolder(iDeskTop, path, out PIDL);
            }

            //存放 PIDL 的数组
            IntPtr[] pidls = [PIDL];

			//得到 IContextMenu 接口
			IntPtr iContextMenuPtr = IntPtr.Zero;
            iContextMenuPtr = IParent.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);
            IContextMenu iContextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);
			try
			{
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
			finally
			{
				Marshal.ReleaseComObject(iContextMenu);
				if (iContextMenuPtr != IntPtr.Zero)
					Marshal.Release(iContextMenuPtr);
			}
        }

        public void myShellExe(string path = "c:\\windows\\system32")
        {
            API.ShellExecute(IntPtr.Zero, "open", path, "", path, (int)SW.SHOWNORMAL);
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
            //try
            {
                string path = e.Node.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
					//Debug.Print("TreeView_NodeMouseClick：{0}", path);
					// 如果path是文件夹，则加载子目录
					var treeView = sender as TreeView;
                    var listView = treeView == uiManager.LeftTree ? uiManager.LeftList : uiManager.RightList;
					currentDirectory = path;
                    selectedNode = e.Node;
                    // 更新监视器
                    watcher.Path = path;
                    watcher.EnableRaisingEvents = true;
                }
            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"TreeView_NodeMouseClick加载目录失败: {ex.Message}", "错误");
            //}
        }
        public void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
			if (e.Node.Nodes.Count == 1 && e.Node.FirstNode.Text == "...")
			{
				LoadSubDirectories(e.Node);
			}
        }
		private bool ftpNodeSelect(TreeNode eNode)
		{
			// 检查是否是FTP节点
			if (eNode.Tag is FtpNodeTag ftpTag)
			{
				// 处理FTP节点双击事件
				fTPMGR.HandleFtpNodeDoubleClick( eNode, activeListView);
				activeListView.Refresh();
				// 更新当前目录和路径显示
				currentDirectory = $"ftp://{ftpTag.ConnectionName}{ftpTag.Path}";
				//if (isleft)
				//	uiManager.LeftPathTextBox.Text = currentDirectory;
				//else
				//	uiManager.RightPathTextBox.Text = currentDirectory;

				selectedNode = eNode;
				//uiManager.BookmarkManager.UpdateActiveBookmark(currentDirectory, selectedNode, isleft);
				UpdatePathTextAndDriveComboBox(eNode, currentDirectory, isleft);
				uiManager.setArgs();
				return true;
			}
			return false;
		}
        public void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;

            //try
            {
                if (sender is TreeView treeView)
                {
                    // 清除所有节点的高亮状态
                    ClearTreeViewHighlight(treeView);
                    e.Node.BackColor = SystemColors.Highlight;
                    e.Node.ForeColor = SystemColors.HighlightText;
                    treeView.Refresh(); // 强制重绘
					uiManager.isleft = treeView == uiManager.LeftTree;

					if (ftpNodeSelect(e.Node)) return;

					LoadSubDirectories(e.Node, activeListView);
					e.Node.Expand();
                    var path = Helper.getFSpathbyTree(e.Node);
                    if (string.IsNullOrEmpty(path)) return;

					// 记录目录历史
					RecordDirectoryHistory(path);
					if (path == "回收站")
						LoadRecycleBin(activeListView); //加载回收站内容
					else
						LoadListViewByFilesystem(path, activeListView, e.Node); //如果未点击回收站

					currentDirectory = path;
					//uiManager.lastVisitedPaths[path.Substring(0,2)] = path;
					uiManager.UpdateLastVisitedPath(path);
                    selectedNode = e.Node;
                    if (Directory.Exists(path))
                    {
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }
					UpdatePathTextAndDriveComboBox(e.Node, path, isleft);
				}
            }
			uiManager.setArgs();
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"TreeView_AfterSelect加载目录失败: {ex.Message}", "错误");
            //}
        }
		private void UpdatePathTextAndDriveComboBox(TreeNode eNode, string path, bool isleft)
		{
			if (isleft)
			{
				uiManager.LeftPathTextBox.SetAddress(eNode);    // 调用leftpathtextbox的setaddress方法来更新路径
				SetDriveComboByValue(uiManager.LeftDriveComboBox, eNode.FullPath);
			}
			else
			{
				uiManager.RightPathTextBox.SetAddress(eNode);
				SetDriveComboByValue(uiManager.RightDriveComboBox, eNode.FullPath);
			}

			uiManager.BookmarkManager.UpdateActiveBookmark(path, selectedNode, isleft);
		}
		private void SetDriveComboByValue(ComboBox cb, string value)
		{
			foreach (var i in cb.Items)
			{
				if (value.Contains(i.ToString().Substring(0,2)))
				{
					cb.SelectedItem = i;
					return;
				}
			}
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
       
        public void LoadDriveIntoTree(TreeView treeView, string drivepath)
        {
            try
            {
                //treeView.BeginUpdate();
                //treeView.Nodes.Clear();
				if(treeView.Nodes.Count == 0)
				{
					//获得桌面 PIDL
					IntPtr deskTopPtr;
					iDeskTop = w32.GetDesktopFolder(out deskTopPtr);
					TreeNode rootNode = new("桌面")
					{
						Tag = new ShellItem(deskTopPtr, iDeskTop, null) { IconKey = "桌面" },
						ImageKey = "桌面", // 设置图标
						SelectedImageKey = "桌面" // 设置选中图标
					};
					treeView.Nodes.Add(rootNode);
					uiManager.isleft = (treeView == uiManager.LeftTree);
					if(isleft)
						leftRoot = rootNode;
					else
						rightRoot = rootNode;
					//treeView.SelectedNode = rootNode;
					// 加载并展开根目录
					LoadSubDirectories(rootNode);
					rootNode.Expand();
					LoadSubDirectories(activeThispc);
				}

				var node = FindTreeNode(activeThispc.Nodes, drivepath);//todo: if drivepath is ftpdrive, find treenode in ftproot
				if (node == null)
					node = FindTreeNode(fTPMGR.ftpRootNode.Nodes, drivepath);
				treeView.SelectedNode = node;
				//treeView.EndUpdate();
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
				var v = sender as ListView;
				uiManager.isleft = v == uiManager.LeftList;
				uiManager.setArgs();
            }
        }
		public void ListView_BeforeLabelEdit(object sender, EventArgs e)
		{
			ListView listView = sender as ListView;
			if (listView.SelectedItems.Count == 0) return;
			ListViewItem item = listView.SelectedItems[0];
			if (item.SubItems[3].Text == "本地磁盘")
			{
				MessageBox.Show("不能重命名本地磁盘");
				e = null;
				return;
			}
			oldname = item.Text;
		}

		public void ListView_AfterLabelEdit(object sender, EventArgs e)
		{
			ListView listView = sender as ListView;
			if (listView.SelectedItems.Count == 0) return;
			ListViewItem item = listView.SelectedItems[0];
			string oldName = oldname;
			var labeleditEvent = e as LabelEditEventArgs;
			if (labeleditEvent.CancelEdit) return;
			string newName = labeleditEvent.Label;
			if (string.IsNullOrEmpty(newName))
			{
				MessageBox.Show("文件名不能为空");
				item.Text = oldName;
				return;
			}
			string oldPath = Path.Combine(currentDirectory, oldName);
			string newPath = Path.Combine(currentDirectory, newName);
			if (oldPath == newPath) return;
			if (File.Exists(newPath) || Directory.Exists(newPath))
			{
				MessageBox.Show("文件已存在");
				item.Text = oldName;
				return;
			}
			try
			{
				if (File.Exists(oldPath))
					File.Move(oldPath, newPath);
				else
					Directory.Move(oldPath, newPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"重命名失败: {ex.Message}", "错误");
				item.Text = oldName;
			}
			RefreshPanel(listView);
		}
		public void ListView_MouseMove(object sender, MouseEventArgs e)
        {
         
        }
        /// <summary>
        /// 从FTP路径中提取连接名称
        /// </summary>
        private string ExtractFtpConnectionName(string ftpPath)
        {
            // 从FTP路径中提取主机名
            if (ftpPath.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                string host = ftpPath.Substring(6).Split('/')[0];
                
                // 查找匹配的连接名称
                var connections = fTPMGR.GetConnections();
                foreach (var conn in connections)
                {
                    if (conn.Host.Equals(host, StringComparison.OrdinalIgnoreCase))
                    {
                        return conn.Name;
                    }
                }
            }
            return string.Empty;
        }
        
        public void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            //if (isSelecting)
            //{
            //    isSelecting = false;
            //    if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
            //        SelectItemsInRectangle(activeListView, selectionRectangle);
            //    activeListView.Invalidate();
            //    selectionRectangle = Rectangle.Empty;
            //}

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

                    // 检查是否是FTP路径
                    if (currentDirectory.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                    {
                        // 从当前目录中提取连接名称
                        string connectionName = ExtractFtpConnectionName(currentDirectory);
                        if (!string.IsNullOrEmpty(connectionName))
                        {
                            // 显示FTP右键菜单
                            fTPMGR.ShowFtpContextMenu(connectionName, item, e.Location);
                            return;
                        }
                    }

                    var tree1 = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;
                    // Find corresponding TreeNode for the clicked ListView item
                    TreeNode? node = tree1.SelectedNode;
                    if (node != null)
                    {
                        // Get the full path by combining current directory and selected item name
                        //string iPath = Path.Combine(currentDirectory, item.Text);
						string iPath = item.SubItems[1].Text;
                        // Get corresponding TreeNode for this path
                        TreeNode? targetNode = FindTreeNode(node.Nodes, item.Text);
                        if (targetNode != null)
                        {
                            ShowContextMenuOnTreeview(targetNode, e.Location);
                        }
                        else
                        {
                            // If no corresponding node found, use path to show context menu
                            //TreeNode? parentNode = (TreeNode)item.Tag;
                            ShowCtxMenuOnListview(iPath, e.Location);
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
            //Debug.Print("listview_mousedoubleclick:{0}, currentDir={1}", selectedItem.Text, currentDirectory);
            
            // 检查是否是FTP路径
            if (currentDirectory.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                // 从当前目录中提取连接名称
                string connectionName = ExtractFtpConnectionName(currentDirectory);
                if (!string.IsNullOrEmpty(connectionName))
                {
                    // 处理FTP列表项双击事件
                    fTPMGR.HandleFtpListItemDoubleClick(connectionName, selectedItem, listView);
                    return;
                }
            }
            
            string path = Path.Combine(currentDirectory, selectedItem.Text);
			if (IsArchiveFile(path))
			{
				if (OpenArchive(path))
				{
					archivePaths[path] = currentDirectory;
					var items = LoadArchiveContents(path);
					listView.Items.Clear();
					listView.Items.AddRange(items.ToArray());
					currentDirectory = path;
					return;
				}
			}
			// 获取关联的TreeView
			TreeView treeView = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;
			if (selectedItem.SubItems[3].Text.Equals("<DIR>") || selectedItem.SubItems[3].Text == "本地磁盘")
            {
                //try
                {
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
						RefreshPanel(listView);
                    }

                    // 更新监视器
                    if (Directory.Exists(path))
                    {
                        currentDirectory = path;    //IF ITEMPATH IS DIR, UPDATE CURRENTDIRECTORY, ELSE NOT
                        watcher.Path = path;
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
                path = Helper.getFSpath(path);
                if (File.Exists(path))
                {
                    try
                    {
                        // 如果是可执行文件，直接执行
                        if (Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(path);
                        }
                        else
                        {
                            // 使用系统默认关联程序打开文件
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件: {ex.Message}", "错误");
                    }
				}
				else
				{
					//is virtual node open
					treeView.SelectedNode.Expand();
					TreeNode? node = FindTreeNode(treeView.SelectedNode.Nodes, selectedItem.Text);
					Process.Start(new ProcessStartInfo(((ShellItem)node.Tag).parsepath) { UseShellExecute = true });
				}
            }
        }

        public TreeNode? FindTreeNode(TreeNodeCollection nodes, string path)
        {
			//Debug.Print("FindTreeNode -> {0}", path);
			//if (nodes.Count == 1 && nodes[0].Text.Equals("..."))
			//{
			//	LoadSubDirectories(nodes[0]);
			//	nodes[0].Expand();
			//}
			var deepSearch = path.Contains("\\");
			if (!deepSearch)
			{
				foreach (TreeNode node in nodes)
				{
					//Debug.Print("FindTreeNode -> node: {0}, {1}", node.Text, node.FullPath);
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

						if (!(p.Equals("此电脑") && path.Contains(':')))
						{
							if (!path.Contains(p))
								continue;
						}
					}
					LoadSubDirectories(node);
					node.Expand();

					TreeNode? foundNode = FindTreeNode(node.Nodes, path);
					if (foundNode != null)
					{
						//Debug.Print("FindTreeNode -> foundNode: {0}", foundNode.Text);
						return foundNode;
					}
				}
			}
			else
			{   // Get the first part of the path, find the node, expand it, and call FindTreeNode recursively
				//TODO : 优化查找算法
				var pathpart = Helper.getFSpath(path).Split('\\', StringSplitOptions.RemoveEmptyEntries);
				foreach(var n in nodes)
				{
					var node = n as TreeNode;
					if (node.Text == pathpart[0])
					{
						if (pathpart.Length == 1)
							return node;
						LoadSubDirectories(node);
						node.Expand();
						TreeNode? foundNode = FindTreeNode(node.Nodes, path.Substring(path.IndexOf('\\') + 1));
						if (foundNode != null)
						{
							//Debug.Print("FindTreeNode -> foundNode: {0}", foundNode.Text);
							return foundNode;
						}
					}
				}
			}
			return null;
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
                    bar.GenerateDynamicToolbar();
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

        public static void ExitApp()
        {
            Application.Exit();
        }
		private bool getIconByShellItem(ref ShellItem subItem, out string iconKey, bool islarge = false)
		{
			var shellInfo = new SHFILEINFO();
			// 首先获取系统图标索引
			var result = API.SHGetFileInfoPIDL(subItem.PIDL, 0, ref shellInfo, Marshal.SizeOf(typeof(SHFILEINFO)),
				((islarge ? SHGFI.LARGEICON : SHGFI.SMALLICON) | SHGFI.ICON | SHGFI.PIDL));
			//Debug.Print($"Virtual 1folder：result={result} name: {subItem.Name} Path: {subItem.parsepath}, Icon:{shellInfo.hIcon} Index: {shellInfo.iIcon}");
			// 使用系统图标索引作为键值
			iconKey = string.Empty;
			if (shellInfo.hIcon != IntPtr.Zero && shellInfo.iIcon != 0)
			{
				// 使用系统图标索引作为键值
				iconKey = $"{subItem.PIDL}_{shellInfo.iIcon}".ToLower();
				subItem.IconKey = iconKey;
				using (Icon icon = Icon.FromHandle(shellInfo.hIcon))
				{
					iconManager.AddIcon(iconKey, icon, islarge);
				}
				API.DestroyIcon(shellInfo.hIcon);
				return true;
			}
			return false;
		}
		private bool getIconBySysImageList(ref ShellItem subItem, out string iconKey, bool islarge = false)
		{
			var IID_IImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
			IImageList hImageList = null;
			
			API.SHGetImageList(islarge ? SHIL.SHIL_LARGE : SHIL.SHIL_SMALL, ref IID_IImageList, ref hImageList);
			if (hImageList != null)
			{
				// 2. 获取图标索引
				var shellInfo = new SHFILEINFO();
				var r = API.SHGetFileInfo(subItem.parsepath, 0, ref shellInfo, Marshal.SizeOf(shellInfo),
				SHGFI.SYSICONINDEX | (islarge ? SHGFI.LARGEICON : SHGFI.SMALLICON));
				Debug.Print($"Virtual 2folder：result={r} name: {subItem.Name} Path: {subItem.parsepath}, Icon:{shellInfo.hIcon} Index: {shellInfo.iIcon}");
				if (shellInfo.iIcon > 0)
				{
					// 使用系统图标索引作为键值
					iconKey = $"{subItem.PIDL}_{shellInfo.iIcon}".ToLower();
					subItem.IconKey = iconKey;
					// 3. 从系统图标列表中提取图标
					// IntPtr hIcon = API.ImageList_GetIcon(hImageList, shellInfo.iIcon, 0);
					IntPtr hIcon = IntPtr.Zero;
					hImageList.GetIcon(shellInfo.iIcon, 0, ref hIcon);
					if (hIcon != IntPtr.Zero)
					{
						try
						{
							using (Icon icon = Icon.FromHandle(hIcon))
							{
								iconManager.AddIcon(iconKey, icon, islarge);
							}
						}
						finally
						{
							API.DestroyIcon(hIcon);
						}
						return true;
					}
				}
			}
			iconKey = string.Empty;
			return false;
		}
		private bool getIconByIconLocation(ref ShellItem subItem, out string iconKey, bool islarge = false)
		{
			var shellInfo = new SHFILEINFO();
			//使用shgfi.iconlocation获取图标文件名和图标索引
			var result = API.SHGetFileInfo(subItem.parsepath, 0, ref shellInfo, Marshal.SizeOf(typeof(SHFILEINFO)), (islarge ? SHGFI.LARGEICON : SHGFI.SMALLICON | SHGFI.ICONLOCATION | SHGFI.ATTRIBUTES));
			Debug.Print($"Virtual 3folder：result={result} name: {subItem.Name} Path: {subItem.parsepath}, Icon:{shellInfo.hIcon} Index: {shellInfo.iIcon}, location:{shellInfo.szDisplayName}");
			if (shellInfo.szDisplayName != string.Empty)
			{
				iconKey = ($"{shellInfo.szTypeName}_{shellInfo.iIcon}").ToLower();
				subItem.IconKey = iconKey;
				//iconKey += islarge ? "l" : "s";
				if (!iconManager.HasIconKey(iconKey, islarge))
				{
					var icon = IconManager.ExtractIconFromFile(shellInfo.szTypeName, shellInfo.iIcon);
					iconManager.AddIcon(iconKey, icon, islarge);
				}
			
				return true;
			}
			iconKey = string.Empty;
			return false;
		}
		public void LoadSubDirectories(TreeNode node, ListView? lv = null)
		{
			if (lv != null)
			{
				lv.SmallImageList ??= new ImageList();
				lv.LargeImageList ??= new ImageList();
				//lv.BeginUpdate();
				lv.Items.Clear();
			}
			if (node.Tag is not ShellItem) return; //eg, if it is ftp virtual node, do not load subnode
			ShellItem sItem = (ShellItem)node.Tag;
			if (sItem == null) return;
			IShellFolder root = sItem.ShellFolder;
			if (root == null ) return;
			
			if(node.Nodes.Count == 1 && node.Nodes[0].Text.Equals("...")) 
				node.Nodes.RemoveAt(0);
			// 保存现有节点的引用，以便后续比较
			Dictionary<string, TreeNode> existingNodes = new Dictionary<string, TreeNode>();
			foreach (TreeNode existingNode in node.Nodes)
			{
				if (existingNode.Tag is ShellItem existingItem)
				{
					// 使用路径作为唯一标识符，而不是PIDL的内存地址
					string path = w32.GetPathByIShell(existingItem.ParentShellFolder, existingItem.PIDL);
					existingNodes[path] = existingNode;
				}
				else if (existingNode.Tag is FtpRootNodeTag)
				{
					existingNodes["ftproot"] = existingNode;
				}
				else if (existingNode.Tag is FtpNodeTag ftptag)
				{

				}
			}
			
			// 创建一个新的节点集合，用于存储需要保留的节点
			List<TreeNode> nodesToKeep = new List<TreeNode>();
			// 创建一个集合，用于存储新的PIDL，以便后续比较
			HashSet<string> newPidls = new HashSet<string>();
			IEnumIDList Enum;
			
			if (root.EnumObjects(this.Handle, SHCONTF.FOLDERS, out nint EnumPtr) == w32.S_OK)    // 循环查找子项
			{
				if (EnumPtr == IntPtr.Zero)  //如果node=程序和功能,则EnumPtr=0，直接返回
					return;
				try
				{
					Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
					while (Enum.Next(1, out nint pidlSub, out uint celtFetched) == 0 && celtFetched == w32.S_FALSE) //获取子节点的pidl
					{
						root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out IShellFolder iSub); //获取子节点的ishellfolder接口
						string name;
						string path = w32.GetPathByIShell(root, pidlSub);   //子节点path -> 此电脑\\迅雷下载, c:\\
						var pathPart = path.Split('\\');
						name = !pathPart[^1].Equals(string.Empty) ? pathPart[^1] : pathPart[^2];
						var subItem = new ShellItem(pidlSub, iSub, root); //子节点的tag存放pidl和ishellfolder接口
						
						// 使用路径作为唯一标识符，而不是PIDL的内存地址
						string nodeKey = path;
						newPidls.Add(nodeKey);
						
						// 检查是否已存在相同路径的节点
						TreeNode nodeSub;
						if (existingNodes.TryGetValue(nodeKey, out TreeNode existingNode))
						{
							// 保留现有节点
							nodeSub = existingNode;
							// 更新节点的Tag，确保使用最新的ShellItem
							nodeSub.Tag = subItem;
							// 将节点添加到保留列表
							nodesToKeep.Add(nodeSub);
						}
						else
						{
							// 创建新节点
							nodeSub = new TreeNode(name)
							{
								Tag = subItem
							};
						}
						// 为虚拟文件夹或非文件系统项设置特定图标
						string iconkey;
						if (subItem.IsVirtual || (subItem.GetAttributes() & SFGAO.FILESYSTEM) == 0)
						{
							if (!getIconByShellItem(ref subItem, out iconkey))
								if(!getIconBySysImageList(ref subItem, out iconkey))
								{
									var icon = IconManager.ExtractIconFromPIDL(iCtrlPanel, pidlSub);
									if(icon != null)
										iconManager.AddIcon(pidlSub.ToString(), icon, false);
									else
										getIconByIconLocation(ref subItem, out iconkey);
								}
										
							iconManager.LoadIconFromCacheByKey(iconkey, node.TreeView.ImageList);
						
							SFGAO subattr = subItem.GetAttributes();    // 如果是文件夹且不是虚拟文件夹，则添加"..."节点
							if (subattr.HasFlag(SFGAO.FOLDER) && nodeSub.Nodes.Count == 0)
								nodeSub.Nodes.Add("...");
						}
						else
						{
							iconkey = IconManager.GetNodeIconKey(nodeSub);
							iconManager.LoadIconFromCacheByKey(iconkey, node.TreeView.ImageList);
							
							// 如果有子文件夹，则添加"..."节点
							if (Directory.Exists(path))
							{
								try
								{
									var dirinfo = new DirectoryInfo(path);  //压缩文件处理到此处引发异常
									var subdir = dirinfo.GetDirectories();	//windows目录CSC无权限异常
									if (subdir.Length != 0 && nodeSub.Nodes.Count == 0)
										nodeSub.Nodes.Add("...");
								}
								catch { }
							}
						}
						nodeSub.ImageKey = iconkey;
						nodeSub.SelectedImageKey = iconkey;
						
						// 如果是新创建的节点，才添加到父节点
						if (!existingNodes.ContainsValue(nodeSub))
						{
							node.Nodes.Add(nodeSub);
						}
						
						// 将节点添加到保留列表
						nodesToKeep.Add(nodeSub);
						if(nodeSub.Text.Equals("此电脑"))
						{
							if (isleft)
								thispcL = nodeSub;
							else
								thispcR = nodeSub;
						}

						if (lv != null)
						{
							string[] s = ["", name, "", name.Contains(':') ? "本地磁盘" : "<CLS>", "", ""];
							var i = new ListViewItem(s);
							var ico = IconManager.GetIconKey(subItem);
							if(lv.View == View.Tile)
							{
								getIconByShellItem(ref subItem, out ico, true);
								iconManager.LoadIconFromCacheByKey(ico, lv.LargeImageList, true);
							}
							iconManager.LoadIconFromCacheByKey(ico, lv.SmallImageList);
							i.ImageKey = ico;
							i.Text = name;
							i.Tag = node;   //tag存放父节点
							lv.Items.Add(i);
						}
					}
					// 处理需要删除的节点
					// 找出所有不在新路径集合中的现有节点，这些节点需要被删除
					foreach (var existingPair in existingNodes)
					{
						// 使用路径作为唯一标识符进行比较
						if (!newPidls.Contains(existingPair.Key) && !existingPair.Key.Equals("ftproot"))
						{
							// 从父节点中移除不再存在的节点
							node.Nodes.Remove(existingPair.Value);
						}
					}
					
					//lv?.EndUpdate();
				}
				finally
				{
				}
            }
        }
       
        private static bool IsChildrenExist(TreeNode node, bool includefile = false)
        {
            ShellItem sItem = (ShellItem)node.Tag;
			if (sItem != null)
			{
				return sItem.IsChildrenExist();
			}
			return false;
        }
		private static void LoadRecycleBinbak(ListView listview)
		{
			int MAX_PATH = 260;
			// 获取回收站中的文件和文件夹信息
			SHQUERYRBINFO shQueryRBInfo = new SHQUERYRBINFO();
			shQueryRBInfo.cbSize = Marshal.SizeOf(shQueryRBInfo);
			API.SHQueryRecycleBin(null, ref shQueryRBInfo);

			uint dwFlags = 0;
			StringBuilder sbDisplayName = new (MAX_PATH);
			StringBuilder sbOriginalPath = new (MAX_PATH);

			while (API.SHEnumRecycleBin(null, 0, ref dwFlags, sbDisplayName, MAX_PATH, sbOriginalPath, MAX_PATH) == 0)
			{
				// 创建 ListViewItem 并添加到 ListView 中
				ListViewItem item = new ListViewItem(sbDisplayName.ToString());
				item.SubItems.Add(sbOriginalPath.ToString());
				listview.Items.Add(item);
			}
		}
		public void LoadRecycleBin(ListView listView)
		{
			IntPtr ppidlRecycleBin;
			API.SHGetSpecialFolderLocation(IntPtr.Zero, CSIDL.BITBUCKET, out ppidlRecycleBin);

			IShellFolder desktopFolder;
			API.SHGetDesktopFolder(out desktopFolder);

			IShellFolder recycleBinFolder;
			desktopFolder.BindToObject(ppidlRecycleBin, IntPtr.Zero, ref Guids.IID_IShellFolder, out recycleBinFolder);

			IEnumIDList enumIDList;
			recycleBinFolder.EnumObjects(IntPtr.Zero, SHCONTF.FOLDERS | SHCONTF.NONFOLDERS | SHCONTF.INCLUDEHIDDEN, out nint enumIDs);
			enumIDList = (IEnumIDList)Marshal.GetObjectForIUnknown(enumIDs);
			var files = GetRecycleBinFilenames();
			foreach (var originalPath in files) 
			{ 
				//Debug.Print(file.ToString()); 
				var originalName = Path.GetFileName(originalPath);
				var extension = Path.GetExtension(originalPath);

				ListViewItem item = new ListViewItem(new string[] {
						originalName,                    // 原始文件名
						originalPath,                    // 原始完整路径
						"",                        // 文件大小
						extension,                       // 扩展名
						"",                     // 最后修改时间
						""					 // 原始文件大小
					});

				// 设置图标
				SetIconForListViewItem(item, listView, (listView.View == View.Tile ? "l" : "s"));
				listView.Items.Add(item);
			}
			return;
			// 遍历回收站中的文件和文件夹
			while (enumIDList.Next(1, out nint pidl, out uint fetched) == 0)
			{
				try 
				{
					SHFILEINFO shfi = new();
					// 获取文件基本信息
					API.SHGetFileInfoPIDL(pidl, 0, ref shfi, Marshal.SizeOf(shfi), 
						SHGFI.PIDL | SHGFI.DISPLAYNAME | SHGFI.TYPENAME | SHGFI.ATTRIBUTES | 
						SHGFI.ICON | SHGFI.SMALLICON);

					// 获取原始路径
					IShellItem shellItem;
					API.SHCreateItemFromIDList(pidl, ref Guids.IID_IShellItem, out shellItem);
					
					string originalPath = string.Empty;
					if (shellItem != null)
					{
						IntPtr pszName;
						shellItem.GetDisplayName(SIGDN.FILESYSPATH, out pszName);
						if (pszName != IntPtr.Zero)
						{
							originalPath = Marshal.PtrToStringAuto(pszName);
							Marshal.FreeCoTaskMem(pszName);
						}
					}

					// 获取文件大小和日期信息
					WIN32_FIND_DATA findData = new WIN32_FIND_DATA();
					IntPtr findHandle = API.FindFirstFile(originalPath, out findData);
					
					string fileSize = "<未知>";
					string lastModified = "<未知>";
					
					if (findHandle != new IntPtr(-1))
					{
						if ((findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
						{
							long size = ((long)findData.nFileSizeHigh << 32) | (uint)findData.nFileSizeLow;
							fileSize = FileSystemManager.FormatFileSize(size);
						}
						else
						{
							fileSize = "<DIR>";
						}
						
						lastModified = DateTime.FromFileTime(
							((long)findData.ftLastWriteTime.dwHighDateTime << 32) | 
							(uint)findData.ftLastWriteTime.dwLowDateTime).ToString("yyyy-MM-dd HH:mm:ss");
						
						API.FindClose(findHandle);
					}

					// 创建 ListViewItem 并添加信息
					var originalName = Path.GetFileName(originalPath);
					var extension = Path.GetExtension(originalPath);
					
					ListViewItem item = new ListViewItem(new string[] {
						originalName,                    // 原始文件名
						originalPath,                    // 原始完整路径
						fileSize,                        // 文件大小
						extension,                       // 扩展名
						lastModified,                     // 最后修改时间
						fileSize					 // 原始文件大小
					});

					// 设置图标
					SetIconForListViewItem(item, listView, (listView.View == View.Tile ? "l" : "s"));
					listView.Items.Add(item);

					if (shellItem != null)
						Marshal.ReleaseComObject(shellItem);
				}
				finally 
				{
					if (pidl != IntPtr.Zero)
						Marshal.FreeCoTaskMem(pidl);
				}
			}

			Marshal.FreeCoTaskMem(ppidlRecycleBin);
		}
		private void SetIconForListViewItem(ListViewItem lvItem, ListView listView, string subkey)
		{
			if (lvItem != null)
			{
				if (lvItem.SubItems[3].Text.Equals("<DIR>"))
				{
					iconManager.LoadIconFromCacheByKey("folder", listView.SmallImageList);
					iconManager.LoadIconFromCacheByKey("folder", listView.LargeImageList, true);
					lvItem.ImageKey = "folder";
				}
				else
				{
					var itemFullName = lvItem.SubItems[1].Text;
					var key = Path.GetExtension(itemFullName);
					if (subkey == "s")
					{
						if (!iconManager.HasIconKey(key, false))
						{
							var ico = IconManager.GetIconByFileNameEx("FILE", itemFullName);
							if (ico != null)
								iconManager.AddIcon(key, ico, false);
						}
						iconManager.LoadIconFromCacheByKey(key, listView.SmallImageList);
						lvItem.ImageKey = key;
					}
					else
					{
						var thumb = thumbnailManager.CreatePreview(itemFullName, out string md5key);
						if (thumb != null)
						{
							Debug.Print("thumb generated: {0}, {1}", itemFullName, md5key);
							listView.LargeImageList.Images.Add(md5key, thumb);
							lvItem.ImageKey = md5key;
						}
						else
						{
							if (!iconManager.HasIconKey(key, true))
							{
								var icol = IconManager.GetIconByFileNameEx("FILE", itemFullName, true);
								if (icol != null)
									iconManager.AddIcon(key, icol, true);
							}
							iconManager.LoadIconFromCacheByKey(key, listView.LargeImageList, true);
							lvItem.ImageKey = key;
						}
					}
				}
				
			}
		}
		// 加载文件列表
		private async Task LoadListViewByFilesystem(string path, ListView listView, TreeNode parentnode)
        {
			if(ftpNodeSelect(parentnode)) return;

			var sitem = (ShellItem)parentnode.Tag;
			if (sitem.IsVirtual) return;
            if (string.IsNullOrEmpty(path)) return;
            if (!path.Contains(':')) return;
			path = Helper.getFSpath(path);
            if (path.EndsWith(':')) path += "\\";
			if (IsArchiveFile(path))
			{
				if (OpenArchive(path))
				{
					archivePaths[path] = currentDirectory;
					var items = LoadArchiveContents(path);
					listView.Items.Clear();
					listView.Items.AddRange(items.ToArray());
					currentDirectory = path;
				}
				return;
			}
			//try
			{
                var items = await Task.Run(() => fsManager.GetDirectoryContents(path));
                listView.BeginUpdate();
                listView.Items.Clear();
				var subkey = (listView.View == View.Tile ? "l" : "s");
				foreach (var item in items)
                {
                    if ((item.Attributes & FileAttributes.Hidden) != 0) continue;
                    var lvItem = CreateListViewItem(item);
					SetIconForListViewItem(lvItem, listView, subkey);
					lvItem.Tag = parentnode;
					listView.Items.Add(lvItem);
				}
                listView.EndUpdate();
				listView.Refresh();
            }
			var status = (listView == uiManager.LeftList) ? uiManager.LeftStatusStrip : uiManager.RightStatusStrip;
			uiManager.UpdateStatusBar(listView, status);
			
			//catch (Exception ex)
			//{
			//    MessageBox.Show($"加载文件列表失败: {ex.Message}", "错误",
			//        MessageBoxButtons.OK, MessageBoxIcon.Error);
			//}
		}
      
        // 将文件属性转换为RAHSC格式的字符串
        private string GetFileAttributesString(FileAttributes attributes)
        {
            StringBuilder sb = new StringBuilder("-----");
            
            // 检查各种属性并设置对应的字符
            if ((attributes & FileAttributes.ReadOnly) != 0)
                sb[0] = 'R';
            if ((attributes & FileAttributes.Hidden) != 0)
                sb[1] = 'H';
            if ((attributes & FileAttributes.System) != 0)
                sb[2] = 'S';
            if ((attributes & FileAttributes.Compressed) != 0)
                sb[4] = 'C';
            if ((attributes & FileAttributes.Archive) != 0)
                sb[3] = 'A';
                
            return sb.ToString();
        }
        
        private ListViewItem? CreateListViewItem(FileSystemInfo item)
        {
            try
            {
                string[] itemData;
                if (item is DirectoryInfo)
                {
					var showFolderSize = configLoader.FindConfigValue("Configuration", "EverythingForSize").Equals("1");
					var size = showFolderSize ? EverythingWrapper.CalculateDirectorySize(item.FullName) : 0;
					
					// 获取目录属性并格式化为RAHSC格式
					string attrStr = GetFileAttributesString(item.Attributes);
					
					itemData = new[]
                    {
                        item.Name,
                        item.FullName,
                        showFolderSize && EverythingWrapper.IsEverythingServiceRunning() ? FileSystemManager.FormatFileSize(size, true) : "",
                        "<DIR>",
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
						size.ToString(),
						attrStr
					};
                }
                else if (item is FileInfo fileInfo)
                {
                    // 获取文件属性并格式化为RAHSC格式
                    string attrStr = GetFileAttributesString(item.Attributes);
                    
                    itemData = new[]
                    {
                        item.Name,
                        item.FullName,	//真实完整路径
                        FileSystemManager.FormatFileSize(fileInfo.Length, true),
                        fileInfo.Extension.ToUpperInvariant(),
                        item.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
						fileInfo.Length.ToString(),
						attrStr
					};
                }
                else
                    return null;

				var i = new ListViewItem(itemData);
				return i;
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
                if (FileSystemManager.IsTextFile(Path.GetExtension(filePath)))
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
                    await PreviewFileAsync(filePath, previewPanel);
            }
			Debug.Print("selection index changed");
			uiManager.setArgs();
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
                themeManager.ApplyDarkTheme();
            else
                themeManager.ApplyLightTheme();
        }

        // 查看按钮点击处理逻辑
        public void ViewButton_Click(object? sender, EventArgs e)
        {
            do_cm_list();
        }
		private List<string> GetFileListByViewOrParam(string param)
		{
			if (!param.Equals(string.Empty))
				return se.PrepareParameter(param, new string[] { }, "");
			List<string> result = new();
			if (activeListView.SelectedItems.Count == 0)
				return result;
			
			// 检查是否是FTP路径
			if (currentDirectory.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
			{
				// 从当前目录中提取连接名称
				string connectionName = ExtractFtpConnectionName(currentDirectory);
				if (!string.IsNullOrEmpty(connectionName) && fTPMGR.ftpSources.TryGetValue(connectionName, out AsyncFtpFileSource source))
				{
					// 对于FTP文件，先下载到本地临时目录
					return activeListView.SelectedItems.Cast<ListViewItem>()
						.Where(i => i.SubItems[3].Text != "<DIR>") // 排除目录
						.Select(i => {
							string remotePath = i.SubItems[1].Text;
							return source.DownloadFile(remotePath);
						})
						.Where(path => !string.IsNullOrEmpty(path)) // 排除下载失败的文件
						.ToList();
				}
			}
			
			// 非FTP路径或FTP处理失败，使用原来的逻辑
			return activeListView.SelectedItems.Cast<ListViewItem>().Select(i => i.SubItems[1].Text).ToList();
		}
        public void do_cm_edit(string param = "")
        {

			//var listView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
			//if (listView.SelectedItems.Count == 0) return;

			//var selectedItem = listView.SelectedItems[0];
			//var filePath = Helper.getFSpath(Path.Combine(currentDirectory, selectedItem.Text));
			var files = GetFileListByViewOrParam(param);
            //if (File.Exists(filePath))
            {
				//Form viewerForm = new Form
				//{
				//    Text = $"查看文件 - {selectedItem.Text}",
				//    Size = new Size(800, 600)
				//};

				//Control viewerControl = previewManager.CreatePreviewControl(filePath);
				//viewerForm.Controls.Add(viewerControl);
				//viewerForm.Show();
				var editorForm = new NewEditorForm(files)
				{
					Text = $"编辑文件 - {files[0]}",
					Size = new Size(800, 600)
				};
				editorForm.Show();
			}
        }
		public void do_cm_list(string param="")
		{
			// 编辑按钮点击处理逻辑
			// OPEN VIEWERFORM
			//string filePath;
			//List<string> filePaths;
			//ListView listView;
			//if (param.Equals(string.Empty))
			//{
			//	var listView = uiManager.LeftList.Focused ? uiManager.LeftList : uiManager.RightList;
			//	if (listView.SelectedItems.Count == 0) return;
			//	var selectedItemText = listView.SelectedItems[0].Text;
			//	filePath = Helper.getFSpath(Path.Combine(currentDirectory, selectedItemText));
			//	if (File.Exists(filePath))
			//	{
			//		Form viewerForm = new ViewerForm(filePath, wlxModuleList)
			//		{
			//			Text = $"查看文件 - {filePath}",
			//			Size = new Size(800, 600)
			//		};
			//		viewerForm.Show();
			//		//Control viewerControl = previewManager.CreatePreviewControl(filePath);
			//		//viewerForm.Controls.Add(viewerControl);
			//	}
			//}
			//else
			//{
			//	//se = new ShellExecuteHelper(this);
			//	filePaths = se.PrepareParameter(param, new string[] { }, "");
			//	Form viewerForm = new ViewerForm(filePaths, wlxModuleList)
			//	{
			//		Text = $"查看文件 - {filePaths}",
			//		Size = new Size(800, 600)
			//	};
			//	viewerForm.Show();
			//	//Control viewerControl = previewManager.CreatePreviewControl(filePath);
			//	//viewerForm.Controls.Add(viewerControl);
			//}
			var filePaths = GetFileListByViewOrParam(param);
			if (filePaths.Count == 0) return;
			Form viewerForm = new ViewerForm(filePaths, wlxModuleList)
			{
				Text = $"查看文件 - {filePaths}",
				Size = new Size(800, 600)
			};
			viewerForm.Show();
		}
		public void EditButton_Click(object? sender, EventArgs e)
        {
			do_cm_edit();
		}

        public void CopyButton_Click(object? sender, EventArgs e)
        {
			cmdProcessor.ExecCmd("cm_copy");
        }
        public void DeleteButton_Click(object? sender, EventArgs e)
        {
			cmdProcessor.ExecCmd("cm_delete");
        }

        public void FolderButton_Click(object? sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称: eg. dir1,dir2\\dir3", "新建文件夹", "新建文件夹");
            if (string.IsNullOrWhiteSpace(input)) return;
			var dirs = input.Split(',');
			foreach(var dir in dirs)
			{
				string newFolderPath = Path.Combine(currentDirectory, dir);
				FileSystemManager.CreateDirectory(newFolderPath);
			}
			RefreshPanel(activeListView);
        }

        public void MoveButton_Click(object? sender, EventArgs e)
        {
			cmdProcessor.ExecCmd("cm_renmov");
        }

        public void RefreshTreeViewAndListView(ListView listView, string path)
        {
			if(listView == null) return;
			
			var node = listView == uiManager.LeftList ? uiManager.LeftTree.SelectedNode : uiManager.RightTree.SelectedNode;
			
            LoadSubDirectories(node, listView);
			if (node.Text == "回收站")
				LoadRecycleBin(listView);
			else if (node.Tag is FtpNodeTag)
			{
				ftpNodeSelect(node);
			} 
			else
				LoadListViewByFilesystem(path, listView, node);
        }
		public void RefreshPanel(TreeView treeView)
		{
			if (treeView == null) return;
			RefreshPanel(treeView==uiManager.LeftTree? RefreshPanelMode.Left: RefreshPanelMode.Right);
		}
		public void RefreshPanel(ListView listView)
		{
			if (listView == null) return;
			RefreshPanel(listView == uiManager.LeftList ? RefreshPanelMode.Left : RefreshPanelMode.Right);
		}
		public void RefreshPanel()
		{
			RefreshPanel(isleft);
		}
		public void RefreshPanel(bool isleft)
		{
			RefreshPanel(isleft? RefreshPanelMode.Left: RefreshPanelMode.Right);
		}
		public void RefreshPanel(RefreshPanelMode mode)
		{
			if (mode.HasFlag(RefreshPanelMode.Left)) 
			{
				RefreshTreeViewAndListView(uiManager.LeftList, uiManager.LeftPathTextBox.CurrentNode.UniqueID);
				//Debug.Print("refresh left panel");
			}
			if (mode.HasFlag(RefreshPanelMode.Right))	
			{
				if(uiManager.RightTree.SelectedNode.Tag is ShellItem)
					RefreshTreeViewAndListView(uiManager.RightList, ((ShellItem)uiManager.RightTree.SelectedNode.Tag).parsepath);
				//Debug.Print("refresh right panel");
			}
		}
        public void TerminalButton_Click(object? sender, EventArgs e)
        {
            // 终端按钮点击处理逻辑
        }

        public void ExitButton_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void OpenCommandPrompt(string cmdstring = "", string cmdMode = "/k", bool isRunas = true)
        {
            try
            {
                //Process.Start("cmd.exe");
                //w32.ShellExecute(IntPtr.Zero, "open", "notepad.exe", "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
                //WinExec(path, 1);
                //System.Diagnostics.Process.Start(path);
                //System.Diagnostics.Process.Start("explorer.exe", path);
                //Process.Start("cmd.exe", "/c start explorer.exe /select,");
				var processInfo = new ProcessStartInfo("cmd.exe")
				{
					Arguments = $"{cmdMode} {cmdstring}",
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					Verb = isRunas ? "runas" : string.Empty
				};
				//Process.Start("cmd.exe", $"{cmdMode} {cmdstring}");
				Process.Start(processInfo);
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
				if (menuItem != null && menuItem.Tag != null)
				{
					var cmd = (string)menuItem.Tag;//51 or cm_xx
					Debug.Print($"点击了菜单项: {menuItem.Text} , cmd : {cmd}");
					cmdProcessor.ExecCmd(cmd);
				}
            }
        }
		
		public void SetViewMode(View viewMode)
		{
			if(viewMode == activeListView.View) return;
			var needupdate = viewMode == View.Tile || activeListView.View == View.Tile;
			activeListView.View = viewMode;
			if (needupdate) 
				RefreshPanel();//update imagekey 
		}
		public bool IsArchiveFile(string filePath)
		{
			string ext = Path.GetExtension(filePath).ToLower();
			return wcxModuleList.GetModuleByExt(ext) != null;
		}

		private bool OpenArchive(string archivePath)
		{
			if (openArchives.ContainsKey(archivePath))
				return true;

			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null)
				return false;
			if(!wcxModule.CanYouHandleThisFile(archivePath))
				return false;
			IntPtr handle = wcxModule.OpenArchive(archivePath, 0, out var openResult);//TODO: BUGFIX: zip.wcx 的函数openarchive 对于zip压缩文件为何不起作用
			if (handle == IntPtr.Zero)
				return false;

			openArchives[archivePath] = handle;
			return true;
		}
		public void CloseAllArchives()
		{
			foreach (var archive in openArchives.Keys.ToList())
			{
				CloseArchive(archive);
			}
		}
		private void CloseArchive(string archivePath)
		{
			if (!openArchives.ContainsKey(archivePath))
				return;

			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule != null)
			{
				wcxModule.CloseArchive(openArchives[archivePath]);
				openArchives.Remove(archivePath);
				archivePaths.Remove(archivePath);
			}
		}

		public List<ListViewItem> LoadArchiveContents(string archivePath)
		{
			List<ListViewItem> items = new List<ListViewItem>();
			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null || !openArchives.ContainsKey(archivePath))
				return items;

			IntPtr handle = openArchives[archivePath];
			THeaderDataExW headerData = new THeaderDataExW();

			while (wcxModule.ReadHeader(handle, out headerData))
			{
				var item = new ListViewItem(headerData.FileName);
				// 将 vhigh 左移32位，然后与 vlow 进行按位或运算
				ulong UnpSize = ((ulong)headerData.UnpSizeHigh << 32) | headerData.UnpSizeLow;
				item.SubItems.Add(UnpSize.ToString());
				item.SubItems.Add(DateTime.FromFileTime(headerData.FileTime).ToString());
				item.SubItems.Add(headerData.Method.ToString());
				items.Add(item);

				wcxModule.ProcessFile(handle, 0, "", ""); // Skip file
			}

			return items;
		}

		public bool ExtractArchiveFile(string archivePath, string fileName, string destPath)
		{
			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null || !openArchives.ContainsKey(archivePath))
				return false;

			IntPtr handle = openArchives[archivePath];
			THeaderDataExW headerData = new THeaderDataExW();

			while (wcxModule.ReadHeader(handle, out headerData))
			{
				if (headerData.FileName == fileName)
				{
					return wcxModule.ProcessFile(handle, 1, destPath, fileName) == 0;
				}
				wcxModule.ProcessFile(handle, 0, "", ""); // Skip file
			}

			return false;
		}

		public bool AddToArchive(string archivePath, string[] files)
		{
			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null)
				return false;

			string fileList = string.Join("\n", files);
			return wcxModule.PackFiles(archivePath, "", Path.GetDirectoryName(files[0]), fileList, 0) == 0;
		}

		public bool DeleteFromArchive(string archivePath, string[] files)
		{
			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null)
				return false;

			string fileList = string.Join("\n", files);
			return wcxModule.DeleteFiles(archivePath, fileList) == 0;
		}
		
		private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;//设置该属性 为false

            var selectedDrive = uiManager.LeftDriveComboBox.SelectedItem?.ToString();
            var listView = selectedDrive != null && watcher.Path.StartsWith(selectedDrive) ? uiManager.LeftList : uiManager.RightList;
			//RefreshPanel(listView);//TODO:BUGFIX 线程异常操作，
        }
    }
}
