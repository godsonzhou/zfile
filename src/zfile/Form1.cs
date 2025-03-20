using Shell32;
using System.Collections;
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
		public readonly FTPMGR fTPMGR;
		public readonly AsyncFTPMGR asyncfTPMGR;
		public readonly LLM_Helper lLM_Helper;
		public readonly CFGLOADER configLoader;
		public readonly CFGLOADER ftpconfigLoader;
		public readonly CFGLOADER userConfigLoader;
		public readonly CFGLOADER cmdicons_configloader;
		public readonly IconManager iconManager;
		public readonly ThemeManager themeManager;
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
		//private TreeNode ftprootL, ftprootR;
		private TreeNode activeFtpRoot { get => fTPMGR.ftpRootNode; }
		private TreeNode unactiveFtpRoot { get => fTPMGR.unactiveFtpRootNode; }
		public TreeNode activeThispc { get { return isleft ? thispcL : thispcR; } }
		public TreeNode unactiveThispc { get { return !isleft ? thispcL : thispcR; } }

		private readonly FileSystemWatcher watcher = new();
		public Dictionary<bool, string> currentDirectory = new();
		private TreeNode? selectedNode = null;
		public TreeNode? SelectedNode
		{
			get { return selectedNode; }
			set
			{
				selectedNode = value;
				if (value != null && value.Tag is ShellItem sitem)
				{
					if (Directory.Exists(sitem.parsepath))
						updateNavHistory(sitem.parsepath);
				}
			}
		}
		private int sortColumn = -1;
		private SortOrder sortOrder = SortOrder.None;
		private readonly ContextMenuStrip contextMenuStrip = new();
		public CmdProc cmdProcessor;
		public KeyMgr keyManager;
		private IShellFolder iDeskTop, iCtrlPanel;
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
		public IDictionary env;
		public Dictionary<string, string> specialpaths = new();
		private Dictionary<string, string> specFolderPaths = new();
		// 添加一个新的字段来存储路径访问历史
		private readonly Dictionary<string, (int count, DateTime lastAccess)> pathAccessHistory = new();
		private const int MAX_HISTORY_COUNT = 100; // 限制历史记录数量

	
		public enum TreeSearchScope
		{
			thispc = 0,
			desktop = 1,
			ftproot = 2,
			full = 3
		}
		// 导航到指定路径
		public void NavigateToPath(string path, bool recordHistory = true, TreeSearchScope scope = TreeSearchScope.thispc, bool isactive = true)
		{
			//Debug.Print($"start to navigate to path {path}");
			//scope : thispc, desktop, full
			if (string.IsNullOrEmpty(path))
				return;
			if (scope == TreeSearchScope.thispc && !Directory.Exists(path))
				return;
			var searchtarget = scope switch
			{
				TreeSearchScope.thispc => isactive ? activeThispc.Nodes : unactiveThispc.Nodes,
				TreeSearchScope.full => isactive ? activeTreeview.Nodes : unactiveTreeview.Nodes,
				TreeSearchScope.desktop => isactive ? activeRoot.Nodes : unactiveRoot.Nodes,
				TreeSearchScope.ftproot => isactive ? activeFtpRoot.Nodes : unactiveFtpRoot.Nodes
			};

			var node = FindTreeNode(searchtarget, path);
			if (node != null)
			{
				if (isactive)
				{
					if (recordHistory)
						RecordDirectoryHistory(path);
					else
						currentDirectory[isleft] = path; // 直接更新当前目录，不记录历史
				}
				if (isactive)
				{
					activeTreeview.SelectedNode = node;
					RefreshPanel(activeListView);
					//Debug.Print($" for {activeListView.Name}");
				}
				else
				{
					unactiveTreeview.SelectedNode = node;
					RefreshPanel(unactiveListView);
					//Debug.Print($" for {unactiveListView.Name}");
				}
			}
			// 更新最后访问路径
			if (isactive)
				uiManager.UpdateLastVisitedPath(path);

			// 更新路径访问历史
			if (recordHistory && Directory.Exists(path))
				updateNavHistory(path);
		}
		private void updateNavHistory(string path)
		{
			string normalizedPath = Path.GetFullPath(path).TrimEnd('\\'); //todo: manual input dir may be insert into history redunantly
			if (pathAccessHistory.ContainsKey(normalizedPath))
			{
				var (count, _) = pathAccessHistory[normalizedPath];
				pathAccessHistory[normalizedPath] = (count + 1, DateTime.Now);
			}
			else
				pathAccessHistory[normalizedPath] = (1, DateTime.Now);
		}
		public Form1()
		{
			env = Helper.getEnv();
			specialpaths = Helper.GetSpecFolderPaths();
			specFolderPaths = Helper.GetSpecPathFromReg(); //favarite
			configLoader = new CFGLOADER(Constants.ZfileCfgPath + "wincmd.ini");
			ftpconfigLoader = new CFGLOADER(Constants.ZfileCfgPath + "wcx_ftp.ini");
			cmdicons_configloader = new CFGLOADER(Constants.ZfileCfgPath + "wcmicons.inc");
			userConfigLoader = new CFGLOADER(Constants.ZfileCfgPath + "user.ini");
			fTPMGR = new FTPMGR(this);
			cmdProcessor = new CmdProc(this);
			lLM_Helper = new LLM_Helper(this);
			iconManager = new IconManager(this);
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
			if (e.Shift)
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
			if (e.Shift)
				shiftKeyPressed = true;
			if (e.Alt)
				altKeyPressed = true;
			if (e.Control)
				ctrlKeyPressed = true;
			if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
				winKeyPressed = true;

			var specKey = (winKeyPressed ? "#" : "") + (altKeyPressed ? "A" : "") + (ctrlKeyPressed ? "C" : "") + (shiftKeyPressed ? "S" : "");
			var mainKey = Helper.ConvertKeyToString(e.KeyCode);
			if (mainKey.Equals(string.Empty))
			{
				e.Handled = true;
				return;
			}
			var cmd = keyManager.GetCmdByKey(specKey.Length != 0 ? $"{specKey}+{mainKey}" : mainKey);
			if (!cmd.Equals(string.Empty))
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
				.Select(item => GetListItemPath(item))
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
			if (targetNode == null) { return false; }
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
				targetPath = GetListItemPath(targetItem);
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

			// 检查源路径是否是 FTP 路径

			bool isSourceFtp = draggedItems[0].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
			// 检查目标路径是否是 FTP 路径
			bool isTargetFtp = targetPath.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);

			try
			{
				if (isSourceFtp && isTargetFtp)
				{
					// FTP 到 FTP 的传输
					string sourceConnection = ExtractFtpConnectionName(draggedItems[0]);
					string targetConnection = ExtractFtpConnectionName(targetPath);

					if (!string.IsNullOrEmpty(sourceConnection) && !string.IsNullOrEmpty(targetConnection))
					{
						fTPMGR.HandleFtpToFtpTransfer(sourceConnection, targetPath, draggedItems);
					}
				}
				else if (isSourceFtp)
				{
					// FTP 到本地的传输
					string sourceConnection = ExtractFtpConnectionName(draggedItems[0]);
					if (!string.IsNullOrEmpty(sourceConnection))
					{
						fTPMGR.HandleFtpToLocalTransfer(draggedItems[0], targetPath, draggedItems);
					}
				}
				else if (isTargetFtp)
				{
					// 本地到 FTP 的传输
					string targetConnection = ExtractFtpConnectionName(targetPath);
					if (!string.IsNullOrEmpty(targetConnection))
					{
						fTPMGR.HandleLocalToFtpTransfer(draggedItems[0], targetPath, draggedItems);
					}
				}
				else
				{
					// 本地到本地的传输
					foreach (var sourcePath in draggedItems)
					{
						FileSystemManager.CopyFilesAndDirectories(sourcePath, targetPath);
					}
				}

				// 刷新目标视图
				listView.Refresh();
				RefreshPanel(listView);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"传输失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	
		public void AddCurrentPathToBookmarks()
		{
			//if (string.IsNullOrEmpty(currentDirectory[isleft])) return;
			var node = activeTreeview.SelectedNode;
			if (node == null) return;
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
	

		private void ShowContextMenuOnTreeview(TreeNode node, Point location)
		{
			if (node.Tag is not ShellItem)
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
					currentDirectory[isleft] = path;
					SelectedNode = e.Node;
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

					if (!currentDirectory.TryGetValue(isleft, out string p))
						currentDirectory[isleft] = path;
					else if (!currentDirectory[isleft].Equals(path))
						// 记录目录历史
						RecordDirectoryHistory(path);
					if (path == "回收站")
						LoadRecycleBin(activeListView); //加载回收站内容
					else
						LoadListViewByFilesystem(path, activeListView, e.Node); //如果未点击回收站

					//uiManager.lastVisitedPaths[path.Substring(0,2)] = path;
					uiManager.UpdateLastVisitedPath(path);
					SelectedNode = e.Node;
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
				if (value.Contains(i.ToString().Substring(0, 2)))
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
				if (treeView.Nodes.Count == 0)
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
					if (isleft)
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
			string oldPath = Path.Combine(currentDirectory[isleft], oldName);
			string newPath = Path.Combine(currentDirectory[isleft], newName);
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
					if (currentDirectory[isleft].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
					{
						// 从当前目录中提取连接名称
						string connectionName = ExtractFtpConnectionName(currentDirectory[isleft]);
						if (!string.IsNullOrEmpty(connectionName))
						{
							// 显示FTP右键菜单
							fTPMGR.ShowFtpContextMenu(connectionName, item);
							return;
						}
					}

					var tree1 = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;
					// Find corresponding TreeNode for the clicked ListView item
					TreeNode? node = tree1.SelectedNode;
					if (node != null)
					{
						// Get the full path by combining current directory and selected item name
						//string iPath = Path.Combine(currentDirectory[isleft], item.Text);
						string iPath = item.SubItems[1].Text;
						// Get corresponding TreeNode for this path
						TreeNode? targetNode = FindTreeNode(node.Nodes, item.Text);
						if (targetNode != null)
							ShowContextMenuOnTreeview(targetNode, e.Location);
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
					item.Selected = true;
			}
		}

		private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			if (isSelecting && e.Bounds.IntersectsWith(selectionRectangle))
				e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
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
			//Debug.Print("listview_mousedoubleclick:{0}, currentDir={1}", selectedItem.Text, currentDirectory[isleft]);

			// 检查是否是FTP路径
			if (currentDirectory[isleft].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
			{
				// 从当前目录中提取连接名称
				string connectionName = ExtractFtpConnectionName(currentDirectory[isleft]);
				if (!string.IsNullOrEmpty(connectionName))
				{
					// 处理FTP列表项双击事件
					fTPMGR.HandleFtpListItemDoubleClick(connectionName, selectedItem, listView);
					return;
				}
			}

			string path = Path.Combine(currentDirectory[isleft], selectedItem.Text);
			if (IsArchiveFile(path))
			{
				if (OpenArchive(path))
				{
					archivePaths[path] = currentDirectory[isleft];
					var items = LoadArchiveContents(path);
					listView.Items.Clear();
					listView.Items.AddRange(items.ToArray());
					currentDirectory[isleft] = path;
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
						SelectedNode = node;
						RefreshPanel(listView);
					}

					// 更新监视器
					if (Directory.Exists(path))
					{
						currentDirectory[isleft] = path;    //IF ITEMPATH IS DIR, UPDATE currentDirectory[isleft], ELSE NOT
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
		public TreeNode? FindTreeNodeByFullPath(TreeNodeCollection nodes, string path)
		{
			//var pathpart = Helper.getFSpath(path).Split('\\', StringSplitOptions.RemoveEmptyEntries);
			var pathpart = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
			foreach (var n in nodes)
			{
				var node = n as TreeNode;
				if (node.Text.Equals(pathpart[0], StringComparison.OrdinalIgnoreCase))
				{
					if (pathpart.Length == 1)
						return node;
					LoadSubDirectories(node);
					node.Expand();
					TreeNode? foundNode = FindTreeNodeByFullPath(node.Nodes, path.Substring(path.IndexOf('\\') + 1));
					if (foundNode != null)
					{
						//Debug.Print("FindTreeNode -> foundNode: {0}", foundNode.Text);
						return foundNode;
					}
				}
			}
			return null;
		}
		public TreeNode? FindTreeNode(TreeNodeCollection nodes, string path)
		{
			var deepSearch = path.Contains("\\");
			if (!deepSearch)
			{
				foreach (TreeNode node in nodes)
				{
					//Debug.Print("FindTreeNode -> node: {0}, {1}", node.Text, node.FullPath);
					//bug fix: node.fullpath=桌面\此电脑\system (C:)\aDrive, path=c:\\
					if (path.Equals(node.Text, StringComparison.OrdinalIgnoreCase)) return node;
					//if (!deepSearch) continue;
					//if (node.Parent != null && node.Tag != null)
					//{
					//	var pidl = ((ShellItem)node.Tag).PIDL;
					//	var pf = ((ShellItem)(node.Parent.Tag)).ShellFolder;
					//	var p = w32.GetPathByIShell(pf, pidl);      ////子节点path -> 此电脑\\迅雷下载, c:\\
					//var n = w32.GetNameByIShell(pf, pidl);    //子节点name -> 迅雷下载, system (c:)
				}
			}
			else
			{   // Get the first part of the path, find the node, expand it, and call FindTreeNode recursively
				TreeNode? foundNode = FindTreeNodeByFullPath(nodes, path);
				if (foundNode != null) return foundNode;
			}
			return null;
		}

		public void ToolbarButton_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[])e.Data.GetData(DataFormats.FileDrop);
				// 只允许可执行文件或目录
				//if (files.Any(f => File.Exists(f) && (Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Directory.Exists(f))))
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

				// 首先检查是否拖放到按钮上
				var button = sender as ToolStripButton;
				if (button != null && uiManager != null)
				{
					string cmd = button.Tag?.ToString() ?? "";
					if (!string.IsNullOrEmpty(cmd))
					{
						foreach (string file in files)
						{
							// 执行按钮命令，将拖拽的文件作为参数
							if (cmd.StartsWith("openbar"))
							{
								// 如果是下拉菜单按钮，不执行任何操作
								continue;
							}
							else
							{
								// 执行普通按钮命令
								cmdProcessor.ExecCmd(cmd, file);
							}
						}
						return;
					}
				}

				// 如果不是拖放到按钮上，则处理拖放到工具栏的情况
				var strip = sender as ToolStrip;
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
							string displayName = Path.GetFileNameWithoutExtension(file);
							bar.AddButton(displayName, file, file + ",0", "", "", "0");
						}
						catch (Exception ex)
						{
							Debug.Print($"添加工具栏按钮失败: {ex.Message}");
						}
					}
					bar.GenerateDynamicToolbar();
				}
			}
		}

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
				lv.Items.Clear();
			}
			if (node.Tag is not ShellItem) return; //eg, if it is ftp virtual node, do not load subnode
			ShellItem sItem = (ShellItem)node.Tag;
			if (sItem == null) return;
			IShellFolder root = sItem.ShellFolder;
			if (root == null) return;

			if (node.Nodes.Count == 1 && node.Nodes[0].Text.Equals("..."))
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
					existingNodes["ftproot"] = existingNode;
				else if (existingNode.Tag is FtpNodeTag ftptag)
				{
				}
			}

			// 创建一个新的节点集合，用于存储需要保留的节点
			List<TreeNode> nodesToKeep = new List<TreeNode>();
			// 创建一个集合，用于存储新的PIDL，以便后续比较
			HashSet<string> newPidls = new HashSet<string>();
			IEnumIDList Enum;
			try
			{
				//get the config showhiddensystem 
				var shcontf = SHCONTF.FOLDERS;
				if (int.TryParse(configLoader.FindConfigValue("Configuration", "ShowHiddenSystem"), out var showhiddensystem))
				{
					if ((showhiddensystem & 2) != 0)
					{
						shcontf |= SHCONTF.INCLUDEHIDDEN;
					}
				}

				if (root.EnumObjects(this.Handle, shcontf, out nint EnumPtr) == w32.S_OK)    // 循环查找子项
				{
					if (EnumPtr == IntPtr.Zero)  //如果node=程序和功能,则EnumPtr=0，直接返回
						return;

					Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
					while (Enum.Next(1, out nint pidlSub, out uint celtFetched) == 0 && celtFetched == w32.S_FALSE) //获取子节点的pidl
					{
						root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out IShellFolder iSub); //获取子节点的ishellfolder接口
						string name;
						string path = w32.GetPathByIShell(root, pidlSub);   //子节点path -> 此电脑\\迅雷下载, c:\\
						var pathPart = path.Split('\\');
						name = !pathPart[^1].Equals(string.Empty) ? pathPart[^1] : pathPart[^2];
						var subItem = new ShellItem(pidlSub, iSub, root); //子节点的tag存放pidl和ishellfolder接口
						if (subItem.parsepath.Equals("::{645FF040-5081-101B-9F08-00AA002F954E}") || subItem.parsepath.Equals("::{26EE0668-A00A-44D7-9371-BEB064C98683}"))//回收站||控制面板，还有些问题, 忽略不做处理
							continue;
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
							nodeSub = new TreeNode(name) { Tag = subItem };
						}
						// 为虚拟文件夹或非文件系统项设置特定图标
						string iconkey;
						if (subItem.IsVirtual || (subItem.GetAttributes() & SFGAO.FILESYSTEM) == 0)
						{
							if (!getIconByShellItem(ref subItem, out iconkey))
								if (!getIconBySysImageList(ref subItem, out iconkey))
								{
									var icon = IconManager.ExtractIconFromPIDL(iCtrlPanel, pidlSub);
									if (icon != null)
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
								var dirinfo = new DirectoryInfo(path);  //压缩文件处理到此处引发异常
								var subdir = dirinfo.GetDirectories();  //windows目录CSC无权限异常
								if (subdir.Length != 0 && nodeSub.Nodes.Count == 0)
									nodeSub.Nodes.Add("...");
							}
						}
						nodeSub.ImageKey = iconkey;
						nodeSub.SelectedImageKey = iconkey;

						// 如果是新创建的节点，才添加到父节点
						if (!existingNodes.ContainsValue(nodeSub))
							node.Nodes.Add(nodeSub);

						// 将节点添加到保留列表
						nodesToKeep.Add(nodeSub);
						if (subItem.parsepath.Equals("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"))
						//if(nodeSub.Text.Equals("此电脑"))
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
							if (lv.View == View.Tile)
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
					// 处理需要删除的节点, 找出所有不在新路径集合中的现有节点，这些节点需要被删除
					foreach (var existingPair in existingNodes)
					{
						// 使用路径作为唯一标识符进行比较
						if (!newPidls.Contains(existingPair.Key) && !existingPair.Key.Equals("ftproot"))
						{
							Debug.Print(existingPair.Key.ToString() + " removed");
							node.Nodes.Remove(existingPair.Value);// 从父节点中移除不再存在的节点
						}
					}
				}
			}
			catch (Exception)
			{
				Debug.Print("exception raised in loadsubdir");
			}
			finally
			{

			}
		}
	
	}
}
