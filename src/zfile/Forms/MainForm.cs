using Shell32;
using Sheng.Winform.Controls;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WinShell;
using zfile.Forms;
using Keys = System.Windows.Forms.Keys;

namespace zfile
{

	public partial class MainForm : Form
	{
		public class lrflag
		{
			[Flags]
			public enum LR : uint
			{
				None = 0,
				Left = 1,
				Right = 2,
				Both = 3
			}
			private LR Val;
			public bool Isleft => Val == LR.Left;
			public string GetText(LR lr)
			{
				return lr.ToString();
			}
			public LR SetByText(string str)
			{
				return (LR)Enum.Parse(typeof(LR), str, true);
			}
			public lrflag()
			{
				Val = LR.None;
			}
			public lrflag(LR val)
			{
				Val = val;
			}
			public lrflag(string str)
			{
				Val = SetByText(str);
			}
			public lrflag(bool isleft)
			{
				Val = isleft ? LR.Left : LR.Right;
			}
			public LR GetRervese()
			{
				return Val switch
				{
					LR.None => LR.Both,
					LR.Left => LR.Right,
					LR.Right => LR.Left,
					LR.Both => LR.None
				};
			}
			public void SetByFlag(LR val)
			{
				Val = val;
			}
			public void SetByFlag(bool isleft)
			{
				Val = isleft ? LR.Left : LR.Right;
			}
			public void SetByFlag(string str)
			{
				Val = SetByText(str);
			}
			public void SetByFlag(lrflag val)
			{
				Val = val.Val;
			}
			public void SetByFlag(lrflag val, bool isleft)
			{
				Val = isleft ? val.Val : val.GetRervese();
			}
			public void SetByFlag(lrflag val, string str)
			{
				Val = str == "L" ? val.Val : val.GetRervese();
			}
			public void SetByFlag(lrflag val, LR lr)
			{
				Val = lr == LR.Left ? val.Val : val.GetRervese();
			}
			public void SetByFlag(lrflag val, lrflag lr)
			{
				Val = lr.Isleft ? val.Val : val.GetRervese();
			}

		}
		// 自定义类来封装字典并实现映射
		public class FileSourceMapper
		{
			private MainForm _mainform;
			// FileSource 相关成员变量
			private IFileSource? _leftFileSource;
			private IFileSource? _rightFileSource;

			public IFileSource? LeftFileSource
			{
				get => _leftFileSource;
				set
				{
					_leftFileSource = value;
					UpdateFileSourceDict("L", value);
				}
			}
			public IFileSource? RightFileSource
			{
				get => _rightFileSource;
				set
				{
					_rightFileSource = value;
					UpdateFileSourceDict("R", value);
				}
			}

			public IFileSource? ActiveFileSource => _mainform.isleft ? _leftFileSource : _rightFileSource;
			public IFileSource? InactiveFileSource => _mainform.isleft ? _rightFileSource : _leftFileSource;
			private Dictionary<string, IFileSource?> FileSourceDict = new();
			public FileSourceMapper(MainForm mainForm)
			{
				_mainform = mainForm;
				FileSourceDict.Add("L", _leftFileSource);
				FileSourceDict.Add("R", _rightFileSource);
				FileSourceDict.Add("A", ActiveFileSource);
				FileSourceDict.Add("I", InactiveFileSource);
			}
			// 重写索引器
			public string this[string key]
			{
				get
				{
					if (FileSourceDict.TryGetValue(key, out IFileSource? value))
					{
						return value?.CurrentFullPath ?? string.Empty;
					}
					throw new KeyNotFoundException($"Key {key} not found in FileSourceDict.");
				}
				set
				{
					if (FileSourceDict.TryGetValue(key, out IFileSource? result))
					{
						if (value.Equals(result.CurrentFullPath))
							Debug.Print("WARNING: SET CURRENTFULLPATH IS NOT NEEDED!");
						else
						{
							Debug.Print($"change CURRENTFULLPATH : {result.CurrentFullPath} -> {value}");
							result.CurrentFullPath = value;
						}
					}
					else
						throw new KeyNotFoundException($"Key {key} not found in FileSourceDict.");
				}
			}
			public IFileSource? GetFileSource(string key)
			{
				if (FileSourceDict.TryGetValue(key, out IFileSource? value))
				{
					return value ?? null;
				}
				return null;
			}

			private void UpdateFileSourceDict(string key, IFileSource? fileSource)
			{
				if (FileSourceDict.ContainsKey(key))
				{
					FileSourceDict[key] = fileSource;
				}
			}
		}
		const int ILD_TRANSPARENT = 0x00000001;
		public const int LVCOL_NAME = 0;
		public const int LVCOL_SIZE = 1;
		public const int LVCOL_TYPE = 2;
		public const int LVCOL_DATE = 3;
		public const int LVCOL_ATTR = 4;
		public static MainForm Instance { get; private set; } = null!;
		public static IntPtr _Handle { get; set; }
		public static int MainThreadId { get; set; } = 0;
		public readonly FTPMGR fTPMGR;

		// FileSource 相关成员变量
		private IFileSource? LeftFileSource { get => CurrentFullpath.LeftFileSource; set => CurrentFullpath.LeftFileSource = value; }
		private IFileSource? RightFileSource { get => CurrentFullpath.RightFileSource; set => CurrentFullpath.RightFileSource = value; }

		private IFileSource? ActiveFileSource => CurrentFullpath.ActiveFileSource;
		private IFileSource? InactiveFileSource => CurrentFullpath.InactiveFileSource;
		private readonly OperationsManager _operationsManager = OperationsManager.Instance;//new OperationsManager(); //bugfix: _operationsmanager and OperationsManager.Instance are not the same instance, so we need to use _operationsManager to update the progress bar in the UI thread
		private readonly VfsModuleManager _vfsModuleManager = new VfsModuleManager();
		private readonly FileSourceManager _fileSourceManager = FileSourceManager.Instance;

		// Method to update thumbnail progress asynchronously
		//private async Task UpdateThumbnailProgressAsync(int current, int total, bool isLeft)
		//{
		//	await Task.Run(() =>
		//	{
		//		var statusStrip = isLeft ? uiManager.LeftStatusStrip : uiManager.RightStatusStrip;
		//		var progressBar = statusStrip.Items[$"thumbnailProgress{(isLeft ? 'L' : 'R')}"] as ToolStripProgressBar;
		//		var statusLabel = statusStrip.Items[$"thumbnailStatus{(isLeft ? 'L' : 'R')}"] as ToolStripStatusLabel;

		//		if (progressBar != null && statusLabel != null)
		//		{
		//			if (current == 0 && total == 0)
		//			{
		//				progressBar.Visible = false;
		//				statusLabel.Visible = false;
		//				return;
		//			}

		//			if (!progressBar.Visible)
		//			{
		//				progressBar.Visible = true;
		//				statusLabel.Visible = true;
		//			}

		//			progressBar.Maximum = total;
		//			progressBar.Value = current;
		//			statusLabel.Text = $"Generating thumbnails: {current}/{total}";

		//			if (current >= total)
		//			{
		//				progressBar.Visible = false;
		//				statusLabel.Visible = false;
		//			}
		//		}
		//	});
		//}

		public readonly LLM_Helper lLM_Helper;
		public readonly MCPClientManager mcpClientMgr;
		public readonly CFGLOADER configLoader;
		public readonly CFGLOADER ftpconfigLoader;
		public readonly CFGLOADER userConfigLoader;
		public readonly CFGLOADER cmdicons_configloader;
		public readonly ViewMgr viewMgr;
		public readonly IconManager iconManager;
		public readonly IdmManager idmManager;
		public readonly ThemeManager themeManager;
		private readonly FilePreviewManager previewManager = new();
		public readonly FileSystemManager fsManager = new();
		public readonly UIControlManager uiManager;
		public readonly ThumbnailManager thumbnailManager = new("d:\\temp\\cache", new Size(64, 64));

		private readonly BackgroundIconManager _backgroundIconManager;
		private Dictionary<Keys, string> hotkeyMappings;

		public string LRflag => uiManager.isleft ? "L" : "R";
		public string RLflag => uiManager.isleft ? "R" : "L";
		public bool isleft => uiManager.isleft;
		public bool isright => !uiManager.isleft;
		public MyListView activeListView { get => uiManager.activeListView; }
		public MyListView unactiveListView { get => uiManager.unactiveListView; }
		public TreeView activeTreeview { get => uiManager.activeTreeview; }
		public TreeView unactiveTreeview { get => uiManager.unactiveTreeview; }
		public TreeNode leftRoot, rightRoot;
		public TreeNode activeRoot { get => (isleft ? leftRoot : rightRoot); }
		public TreeNode unactiveRoot { get => (!isleft ? leftRoot : rightRoot); }

		private TreeNode thispcL, thispcR;

		private TreeNode activeFtpRoot { get => fTPMGR.ftpRootNode; }
		private TreeNode unactiveFtpRoot { get => fTPMGR.unactiveFtpRootNode; }
		public TreeNode activeThispc { get { return isleft ? thispcL : thispcR; } }
		public TreeNode unactiveThispc { get { return !isleft ? thispcL : thispcR; } }
		public TreeView? FocusedTree { get => uiManager.FocusedTree; }
		private readonly FileSystemWatcher watcher = new();

		public FileSourceMapper CurrentFullpath;

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
		private FileEntry[] draggedItems;
		private TreeNode rightClickBegin;
		private string? oldname;
		public static WcxModuleList? wcxModuleList;
		public static WlxModuleList? wlxModuleList;
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
		public Font myfont;
		private bool showFolderSize;
		private IntPtr CtrlPanel_PIDL;

		public enum TreeSearchScope
		{
			thispc = 0,
			desktop = 1,
			ftproot = 2,
			full = 3
		}
		private IFileSource UpdateFilesourceAndCurrentPath(string path, out bool filesourceChanged, out IFileSource? oldfs, out string oldpath)
		{
			// get the current filesource
			oldpath = CurrentFullpath[LRflag];
			oldfs = CurrentFullpath.GetFileSource(LRflag);
			// 使用 FileSourceManager 获取合适的 FileSource,
			IFileSource fileSource = _fileSourceManager.GetFileSourceForFullPath(path, isleft);
			filesourceChanged = (oldfs != fileSource);
			if (filesourceChanged)
			{
				Debug.Print($"file source update {oldfs}({oldfs.RootPath}) -> {fileSource}({fileSource.RootPath})");
				// 更新当前活动面板的 FileSource
				if (uiManager.isleft)
					LeftFileSource = fileSource;
				else
					RightFileSource = fileSource;
			}
			else
				Debug.Print($"WARNING: Update Filesource is not necessary!");

			// 更新当前路径
			CurrentFullpath[LRflag] = path;
			return fileSource;
		}
		// 导航到指定路径
		public void NavigateToPath(string path, bool recordHistory = true, TreeSearchScope scope = TreeSearchScope.thispc, bool isactive = true)
		{
			var searchftp = path.StartsWith("ftp://");
			var pathsep = searchftp ? '/' : '\\';
			//ftp filesystem filesource uniprocess here
			path = Helper.IncludeTrailingPathDelimiter(path, pathsep);
			if (path.Equals(CurrentFullpath[LRflag]))
				return;
			Debug.Print($"Navigate to path : {path}");
			//first change currentfilesource according to the path
			var fs = UpdateFilesourceAndCurrentPath(path, out _, out var oldfs, out var oldpath);
			if (fs is WcxArchiveFileSource wcxfs)
			{
				if (!path.StartsWith(wcxfs.ArchivePath))
					path = wcxfs.ArchivePath + path;    //if the new path is wcxfs path, 将其转化为操作系统的绝对路径，eg. d:\tmp\test.7z\
			}
			if (string.IsNullOrEmpty(path))
				return;
			//如果路径不存在，可能是虚拟节点，扩展搜索范围到桌面，
			if (path.StartsWith("\\\\"))
				scope = TreeSearchScope.desktop;
			else if (searchftp)
				scope = TreeSearchScope.ftproot;

			var searchtarget = scope switch
			{
				TreeSearchScope.thispc => isactive ? activeThispc.Nodes : unactiveThispc.Nodes,
				TreeSearchScope.full => isactive ? activeTreeview.Nodes : unactiveTreeview.Nodes,
				TreeSearchScope.desktop => isactive ? activeRoot.Nodes : unactiveRoot.Nodes,
				TreeSearchScope.ftproot => isactive ? activeFtpRoot.Nodes : unactiveFtpRoot.Nodes
			};

			var node = FindTreeNode(searchtarget, path, searchftp);
			if (node != null)
			{
				if (isactive)
				{
					if (recordHistory)
						RecordDirectoryHistory(path, oldpath);    //传入老filesource以确保跨filesource时的正确地将老路径记录到历史中
																  //else
																  //	CurrentFullpath[LRflag] = path; // 直接更新当前目录，不记录历史//already ran in previous updatefilesourceandcurrentpath, do not needed do again
					if (activeTreeview.SelectedNode != node)
						activeTreeview.SelectedNode = node;     //trigger afterselect event
					else if (searchftp)
						fTPMGR.NavigateToPath((node.Tag as FtpNodeTag)?.ConnectionName ?? "", fs.CurrentPath, activeListView);
					//RefreshPanel(activeListView);////////////////////////////////////////////whhen change selectedNode, the afterselect event will be executed and the refreshpanel operation also be run at that time, so here refreshpanel seem to be unnecessary.
				}
				else
				{
					unactiveTreeview.SelectedNode = node;
					//RefreshPanel(unactiveListView);
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
		private void GetFontFromCfgloader()
		{
			var font = configLoader.FindConfigValue("AllResolutions", "FontName");
			var fontDlg = configLoader.FindConfigValue("AllResolutions", "FontNameDialog");
			var fontWin = configLoader.FindConfigValue("AllResolutions", "FontNameWindow");
			var fontsize = configLoader.FindConfigValue("AllResolutions", "FontSize");
			var fontsizeDlg = configLoader.FindConfigValue("AllResolutions", "FontSizeDialog");
			var fontsizeWin = configLoader.FindConfigValue("AllResolutions", "FontSizeWindow");
			myfont = new Font(font ?? "Consolas", Convert.ToSingle(fontsize));
		}
		public MainForm()
		{
			Instance = this;
			env = Helper.getEnv();
			specialpaths = Helper.GetSpecFolderPaths();
			specFolderPaths = Helper.GetSpecPathFromReg(); //favarite
			configLoader = new CFGLOADER(Constants.ZfileCfgPath + "wincmd.ini");
			ftpconfigLoader = new CFGLOADER(Constants.ZfileCfgPath + "wcx_ftp.ini");
			cmdicons_configloader = new CFGLOADER(Constants.ZfileCfgPath + "wcmicons.inc");
			userConfigLoader = new CFGLOADER(Constants.ZfileCfgPath + "user.ini");
			GetFontFromCfgloader();
			//apply font
			Helper.ApplyFontToControls(this, myfont);
			viewMgr = new ViewMgr(this);
			fTPMGR = new FTPMGR(this);
			cmdProcessor = new CmdProc(this);
			mcpClientMgr = new MCPClientManager(Constants.ZfileCfgPath + "zfile_mcp_settings.json");
			lLM_Helper = new LLM_Helper(this);
			iconManager = new IconManager(this);
			idmManager = new IdmManager(this);
			InitializeComponent();
			this.Size = new Size(1920, 1080);
			_backgroundIconManager = new BackgroundIconManager(thumbnailManager, iconManager);

			// 初始化COM组件
			InitializeCOMComponents();
			keyManager = new KeyMgr();

			// 创建UIManager并初始化
			uiManager = new UIControlManager(this);
			uiManager.InitializeUI();
			CurrentFullpath = new(this);

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
			// 初始化插件模块
			WdxModuleList wdxModuleList = new WdxModuleList("");
			WfxModuleList wfxModuleList = new WfxModuleList("");
			wcxModuleList = new WcxModuleList();
			wcxModuleList.LoadConfiguration();
			wlxModuleList = new WlxModuleList();

			// 初始化 VFS 模块
			_vfsModuleManager.RegisterVirtualFileSource<FileSystemFileSource>("FileSystem", true);
			_vfsModuleManager.RegisterVirtualFileSource<RecycleBinFileSource>("RecycleBin", true);
			_vfsModuleManager.RegisterVirtualFileSource<ControlPanelFileSource>("ControlPanel", true);

			// 初始化FTP管理器扩展
			fTPMGR.Initialize();

			// 初始化 FileSourceManager
			_fileSourceManager.Initialize(wcxModuleList, fTPMGR, this);

			// 初始化默认 FileSource
			LeftFileSource = _fileSourceManager.GetFileSourceForFullPath("C:\\", true);
			RightFileSource = _fileSourceManager.GetFileSourceForFullPath("C:\\", false);

			se = new ShellExecuteHelper(this);
			ClearMemory();
			_operationsManager.AddEventListener(OperationManagerNotify);
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		private void OperationManagerNotify(object? sender, OperationEventArgs e)
		{
			// Get the active status strip based on current panel
			var statusStrip = isleft ? uiManager.LeftStatusStrip : uiManager.RightStatusStrip;

			if (e.EventType == OperationEventType.Removed)
			{
				// Hide progress information if there are no operations
				if (_operationsManager.OperationsCount == 0)
				{
					// Update status bar to show no operations are running
					if (statusStrip.Items.Count > 0)
					{
						statusStrip.Items[0].Text = "就绪";
					}
				}
			}
			else if (e.EventType == OperationEventType.Added)
			{
				// Show operation information
				if (statusStrip.Items.Count > 0)
				{
					var operation = e.Item?.Operation;
					if (operation != null)
					{
						// Update status bar to show operation is running
						statusStrip.Items[0].Text = $"正在执行: {GetOperationTypeString(operation)}";
					}
				}
			}

			// Update operation progress in UI
			UpdateOperationProgress();
		}

		private string GetOperationTypeString(FileSourceOperation operation)
		{
			// Return a user-friendly string based on operation type
			if (operation is FileSourceListOperation) return "列出文件";
			if (operation is FileSourceCopyOperation) return "复制文件";
			if (operation is FileSourceMoveOperation) return "移动文件";
			if (operation is FileSourceDeleteOperation) return "删除文件";
			if (operation is FileSourceCreateDirectoryOperation) return "创建目录";
			if (operation is FileSourceExecuteOperation) return "执行操作";

			// Default case
			return "文件操作";
		}

		private void UpdateOperationProgress()
		{
			// Get overall progress
			double progress = _operationsManager.AllProgressPoint();

			// Update status bar with progress information if operations are running
			if (_operationsManager.OperationsCount > 0)
			{
				var statusStrip = isleft ? uiManager.LeftStatusStrip : uiManager.RightStatusStrip;
				if (statusStrip.Items.Count > 0)
					statusStrip.Items[0].Text += $" - 进度: {progress:F1}%";
			}
		}
		private void InitializeCOMComponents()
		{
			// 初始化COM组件
			IntPtr deskTopPtr;
			w32.InitializeCOM();
			iDeskTop = w32.GetDesktopFolder(out deskTopPtr);
			if (iDeskTop == null)
				throw new Exception("无法初始化桌面Shell接口");
			iCtrlPanel = w32.GetControlPanelFolder(out CtrlPanel_PIDL);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// 释放其他资源
				watcher.Dispose();
				previewManager.Dispose();
				_backgroundIconManager.Dispose();
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
				{ Keys.Control | Keys.C, "cm_copytoclipboard"},
				{ Keys.Control | Keys.X, "cm_CutToClipboard"},
				{ Keys.Control | Keys.V, "cm_PasteFromClipboard"},
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
			draggedItems = [.. listView.SelectedItems.Cast<ListViewItem>().Select(item => GetListItemPath(item))];
			// 启动拖拽操作
			listView.DoDragDrop(new DataObject(DataFormats.FileDrop, draggedItems), DragDropEffects.Copy);
		}

		private string GetTreeNodePath(TreeNode node)
		{
			return Helper.getFSpathbyTree(node);
		}
		private bool IsValidTarget(TreeView? treeView, DragEventArgs e, out string targetPath)
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
			if (draggedItems == null || !IsValidTarget(treeView, e, out string targetPath)) return;

			// 使用cm_copy方法处理拖放操作
			cm_copy(null, targetPath);

			// 刷新目标视图
			RefreshPanel(treeView);
		}
		public void ListView_DragOver(object? sender, DragEventArgs e)
		{
			// 检查目标是否为有效文件系统路径
			var listView = sender as MyListView;
			if (listView == null) return;

			listView.Update();
			if (!IsValidTarget(listView, e, out string targetPath))
			{
				e.Effect = DragDropEffects.None;
				return;
			}
			Debug.Print($"{targetPath}");
			// 检查目标路径是否为FTP或压缩文件
			if (fTPMGR.IsFtpPath(targetPath) || IsArchiveFile(targetPath))
			{
				e.Effect = DragDropEffects.None;
				return;
			}

			// 获取目标项
			var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
			var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);

			if (targetItem != null)
			{
				string? itemPath = GetListItemPath(targetItem)?.FullPath;
				if (File.Exists(itemPath))
				{
					// 检查是否为可执行文件
					string ext = Path.GetExtension(itemPath).ToLower();
					if (ext == ".exe" || ext == ".com" || ext == ".bat" || ext == ".cmd")
					{
						Debug.Print($"sss{itemPath}");
						e.Effect = DragDropEffects.Copy; // 使用Link效果表示将作为参数启动程序
						return;
					}
				}
			}

			e.Effect = DragDropEffects.Copy;
		}
		public TreeView GetTreeViewByName(string name)
		{
			if (name.Equals("L", StringComparison.OrdinalIgnoreCase))
				return uiManager.LeftTree;
			else
				return uiManager.RightTree;
		}
		public MyListView GetListViewByName(string name)
		{
			if (name.Equals("L", StringComparison.OrdinalIgnoreCase))
				return uiManager.LeftList;
			else
				return uiManager.RightList;
		}
		private bool IsValidTarget(MyListView listView, DragEventArgs e, out string targetPath)
		{
			if (IsActiveFtpPanel(out var ftpnode, GetTreeViewByName(listView.Name)))
			{
				// 将屏幕坐标转换为 TreeView 控件内的坐标
				var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
				// 使用 GetNodeAt 获取目标节点
				var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);
				if (targetItem != null)
				{
					targetPath = GetListItemPath(targetItem)?.FullPath;
					return targetItem.SubItems[LVCOL_TYPE].Text.Equals("<DIR>"); //IN ftp panel, if target is a dir then return true, otherwise return false
				}
				else
				{
					var targetTree = (listView == uiManager.LeftList) ? uiManager.LeftTree : uiManager.RightTree;
					targetPath = Helper.getFSpathbyTree(targetTree.SelectedNode);
				}
				return true;
			}
			else
			{
				// 将屏幕坐标转换为 TreeView 控件内的坐标
				var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
				// 使用 GetNodeAt 获取目标节点
				var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);
				if (targetItem != null)
					targetPath = GetListItemPath(targetItem)?.FullPath;
				else
				{
					var targetTree = (listView == uiManager.LeftList) ? uiManager.LeftTree : uiManager.RightTree;
					targetPath = Helper.getFSpathbyTree(targetTree.SelectedNode);
				}
				return FileSystemManager.IsValidFileSystemPath(targetPath);
			}
		}
		public void ListView_DragDrop(object? sender, DragEventArgs e)
		{
			if (draggedItems == null) return;
			var listView = sender as MyListView;
			if (!IsValidTarget(listView, e, out string targetPath)) return;

			// 检查目标路径是否为FTP或压缩文件
			if (fTPMGR.IsFtpPath(targetPath) || IsArchiveFile(targetPath))
			{
				MessageBox.Show("不能拖放到FTP或压缩文件中", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// 获取目标项
			var clientPoint = listView.PointToClient(new Point(e.X, e.Y));
			var targetItem = listView.GetItemAt(clientPoint.X, clientPoint.Y);

			if (targetItem != null)
			{
				string itemPath = GetListItemPath(targetItem)?.FullPath;
				if (File.Exists(itemPath))
				{
					// 检查是否为可执行文件
					string ext = Path.GetExtension(itemPath).ToLower();
					if (ext == ".exe" || ext == ".com" || ext == ".bat" || ext == ".cmd")
					{
						try
						{
							// 构建启动参数
							var processInfo = new ProcessStartInfo
							{
								FileName = itemPath,
								Arguments = string.Join(" ", draggedItems.Select(path => $"\"{path}\"")),
								UseShellExecute = true,
								WorkingDirectory = Path.GetDirectoryName(itemPath)
							};
							Process.Start(processInfo);
							return;
						}
						catch (Exception ex)
						{
							MessageBox.Show($"启动程序失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
					}
				}
			}

			// 如果不是拖放到可执行文件，则执行普通的复制操作
			cm_copy(null, targetPath);

			// 刷新目标视图
			listView.Refresh();
			RefreshPanel(listView);
		}

		public void AddCurrentPathToBookmarks()
		{
			var node = activeTreeview.SelectedNode;
			if (node == null) return;
			uiManager.BookmarkManager.AddBookmark(node, isleft);
		}

		public void OpenOptions(string param)
		{
			// 打开Options窗口
			OptionsForm optionsForm = new OptionsForm(this, param);
			if (optionsForm.ShowDialog() == DialogResult.OK)
			{
				// 更新热键映射
				//hotkeyMappings = optionsForm.commandHotkeys.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
			}
		}

		public void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// 在这里可以添加自定义的菜单项
		}

		//public interface IActiveListViewChangeable
		//{
		//	void ActiveListViewChange(View view);
		//}
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

		private void MenuItemEmptyRecycleBin_Click(object sender, EventArgs e)
		{
			if (CurrentFullpath.GetFileSource(LRflag) is RecycleBinFileSource)
			{
				if (MessageBox.Show("确定要清空回收站吗?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					try
					{
						try
						{
							// Empty recycle bin using Shell API
							// Pass null for pszRootPath to empty all recycle bins
							// Use SHERB.NOCONFIRMATION to suppress the confirmation dialog
							int result = API.SHEmptyRecycleBin(
								Handle,
								null,
								(uint)SHERB.NOCONFIRMATION
							);

							if (result != 0)
							{
								Marshal.ThrowExceptionForHR(result);
							}
						}
						finally
						{

						}

						// Refresh view
						RefreshActivePanel();
						MessageBox.Show("回收站已清空", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"清空回收站出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}
		private static void Restore(string filepath)
		{
			var Shl = new Shell();
			Folder Recycler = Shl.NameSpace(10);
			var c = Recycler.Items().Count;

			var _recycler = Recycler.Items();
			for (int i = 0; i < _recycler.Count; i++)
			{
				FolderItem FI = _recycler.Item(i);
				string FileName = Recycler.GetDetailsOf(FI, 0);
				if (Path.GetExtension(FileName) == "") FileName += Path.GetExtension(FI.Path);
				//Necessary for systems with hidden file extensions.

				string FilePath = Recycler.GetDetailsOf(FI, 1);
				if (filepath == Path.Combine(FilePath, FileName))
				{
					DoVerb(FI, "还原");
					break;
				}
			}
		}

		private static bool DoVerb(FolderItem Item, string Verb)
		{
			foreach (FolderItemVerb FIVerb in Item.Verbs())
			{
				if (FIVerb.Name.Contains(Verb, StringComparison.OrdinalIgnoreCase))
				{
					FIVerb.DoIt();
					return true;
				}
			}
			return false;
		}
		private void MenuItemRestore_Click(object sender, EventArgs e)
		{
			if (CurrentFullpath.GetFileSource(LRflag) is RecycleBinFileSource && activeListView.SelectedItems.Count > 0)
			{
				try
				{
					try
					{
						bool anyRestored = false;

						foreach (ListViewItem item in activeListView.SelectedItems)
						{
							if (item.Tag is LvItemTag tag)
							{
								var file = tag.File;
								// Get original path from link property
								//string originalPath = file.LinkProperty.LinkTarget;
								string originalPath = file.FullPath;
								if (string.IsNullOrEmpty(originalPath))
								{
									MessageBox.Show($"无法还原 {file.Name}，找不到原始路径", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
									continue;
								}

								// Create directory for the file if it doesn't exist
								string? directory = Path.GetDirectoryName(originalPath);
								if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
								{
									Directory.CreateDirectory(directory);
								}
								Restore(originalPath);
								//// Get the full path to the file in the recycle bin
								//string recycleBinPath = file.LinkProperty.LinkTarget; //file.FullPath ?? string.Empty;

								//                        // Create a shell item for the file in the recycle bin
								//                        IShellItem? shellItem = null;
								//                        IntPtr pidl = API.ILCreateFromPath(recycleBinPath);

								//                        try
								//                        {
								//                            Guid iidShellItem = Guids.IID_IShellItem;
								//                            int hr = API.SHCreateItemFromIDList(pidl, ref iidShellItem, out shellItem);

								//                            if (hr != 0)
								//                            {
								//                                Marshal.ThrowExceptionForHR(hr);
								//                            }

								//                            // Get the parent folder of the file
								//                            w32.OleCheck(API.SHGetDesktopFolder(out IShellFolder desktopFolder));

								//                            // Get the context menu for the file
								//                            Guid iidContextMenu = Guids.IID_IContextMenu;
								//                            IntPtr[] pidls = [pidl];
								//                            desktopFolder.GetUIObjectOf(IntPtr.Zero, 1, pidls, ref iidContextMenu, out IntPtr contextMenuPtr);

								//                            IContextMenu contextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(contextMenuPtr);

								//                            // Execute the "Restore" verb
								//                            ContextMenuHandler.ExecuteVerb(this, "restore", string.Empty, contextMenu);

								//                            anyRestored = true;
								//                        }
								//                        finally
								//                        {
								//                            if (pidl != IntPtr.Zero)
								//                            {
								//                                API.ILFree(pidl);
								//                            }

								//                            if (shellItem != null)
								//                            {
								//                                Marshal.ReleaseComObject(shellItem);
								//                            }
								//                        }
							}
						}

						if (anyRestored)
						{
							// Refresh view
							RefreshActivePanel();
							MessageBox.Show("文件已成功还原", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					}
					finally
					{

					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"还原文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

			string path = e.Node.Text ?? string.Empty;
			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			{
				SelectedNode = e.Node;
				// 更新监视器
				watcher.Path = path;
				watcher.EnableRaisingEvents = true;
			}
		}
		public void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Nodes.Count == 1 && e.Node.FirstNode.Text == "...")  //点击+号时，加载子目录
				LoadSubDirectories(e.Node);
		}

		public void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
		{
			if (e.Node?.Tag == null) return;

			try
			{
				if (sender is TreeView treeView)
				{
					_backgroundIconManager.CancelCurrentTasks();
					// 清除所有节点的高亮状态
					ClearTreeViewHighlight(treeView);
					e.Node.BackColor = SystemColors.Highlight;
					e.Node.ForeColor = SystemColors.HighlightText;
					treeView.Refresh(); // 强制重绘

					uiManager.isleft = treeView == uiManager.LeftTree;
					// 使用 FileSourceManager 获取合适的 FileSource
					var path = Helper.getFSpathbyTree(e.Node);
					if (string.IsNullOrEmpty(path))
						return;

					var fileSource = UpdateFilesourceAndCurrentPath(path, out var fschanged, out var oldfs, out var oldpath);

					//var driveChanged = CheckDriveChange(path, oldpath);
					SelectedNode = e.Node;

					// 检查是否是FTP节点
					if (e.Node.Tag is FtpNodeTag ftpTag)
					{
						// 处理FTP节点双击事件
						fTPMGR.HandleFtpNodeDoubleClick(e.Node);
						//SelectedNode = e.Node;
						UpdatePathTextAndDriveComboBox(e.Node, CurrentFullpath[LRflag], isleft);//TODO: BUGFIX: IF ENODE IS LEFT , LRFLAG IS R, SOME THING ERROR
						uiManager.SetArgs();
						return;
					}
					e.Node.Expand();

					if (fschanged || Helper.IncludeTrailingPathDelimiter(path) != oldpath)
						RecordDirectoryHistory(path, oldpath);   // 记录目录历史, 并更新filesource的currentpath

					//if (!driveChanged)
					//{
					//如果盘符改变了，则不刷新treeview&listview, 因为在盘符改变时，会触发事件，在事件中会刷新(refreshpanel)
					// 检查节点是否已经被加载过子目录
					bool isNodeLoaded = false;
					if (e.Node.Tag is ShellItem sItem && sItem.SubNodeState == NODE_LOADED_KEY)
						isNodeLoaded = true;

					// 只有当节点没有被标记为已加载时才加载子目录
					if (!isNodeLoaded || fileSource is ShellFileSource) // shellfilesource should always loadsubdir
						LoadSubDirectories(e.Node, activeListView); //盘符不变时在这里刷新TREEVIEW/LISTVIEW

					// 无论如何都需要刷新ListView
					LoadListViewByFileSource(path, activeListView, e.Node);
					//}
					uiManager.UpdateLastVisitedPath(path);
					UpdatePathTextAndDriveComboBox(e.Node, path, isleft);    //盘符改变时在combobox事件中刷新//必须在loadsubdir之后，因为需要loadsubdir中调用pathtextbox.setchildren
					if (Directory.Exists(path))
					{
						watcher.Path = path;
						watcher.EnableRaisingEvents = true;
					}
				}
				uiManager.SetArgs();
			}
			catch (Exception ex)
			{
				Debug.Print($"TreeView_AfterSelect加载目录失败: {ex.Message}");
			}
		}
		//public static bool CheckDriveChange(string path1, string path2)
		//{
		//	if (!Directory.Exists(path1) || !Directory.Exists(path2))
		//		return false;

		//	var drive1 = Path.GetPathRoot(path1);
		//	var drive2 = Path.GetPathRoot(path2);

		//	return !string.Equals(drive1, drive2, StringComparison.OrdinalIgnoreCase);
		//}
		private bool UpdatePathTextAndDriveComboBox(TreeNode eNode, string path, bool isleft)
		{
			if (!eNode.TreeView.Name.Equals(isleft ? "L" : "R")) return false;
			var driveId = path[1] == ':' ? path.Substring(0, 2) : "";

			bool driveChanged = false;
			if (isleft)
			{
				if (ShengAddressBarStrip.FtpDrives.Contains(driveId)) //if ftp node clicked, update the pathtextbox
					uiManager.LeftPathTextBox.UpdateDrives(driveId);
				else
					uiManager.LeftPathTextBox.SetAddress(eNode);    // 调用leftpathtextbox的setaddress方法来更新路径
				if (driveId.Length > 0)
					driveChanged = SetDriveComboByValue(uiManager.LeftDriveComboBox, driveId);
			}
			else
			{
				if (ShengAddressBarStrip.FtpDrives.Contains(driveId)) //if ftp node clicked, update the pathtextbox
					uiManager.RightPathTextBox.UpdateDrives(driveId);
				else
					uiManager.RightPathTextBox.SetAddress(eNode);
				if (driveId.Length > 0)
					driveChanged = SetDriveComboByValue(uiManager.RightDriveComboBox, driveId);
			}

			uiManager.BookmarkManager.UpdateActiveBookmark(path, selectedNode, isleft);
			return driveChanged;
		}
		private static bool SetDriveComboByValue(ComboBox cb, string driveId)
		{
			var driveChanged = false;
			foreach (var i in cb.Items) //i looks like c:\
			{
				if (driveId.Equals(i.ToString()[..2], StringComparison.OrdinalIgnoreCase))
				{
					if (cb.SelectedItem?.ToString() != i.ToString())
					{
						driveChanged = true;
						cb.SelectedItem = i;
					}
					break;
				}
			}
			return driveChanged;
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
					// 加载并展开根目录
					LoadSubDirectories(rootNode);
					rootNode.Expand();
					LoadSubDirectories(activeThispc);
				}

				var node = FindTreeNode(activeThispc.Nodes, drivepath);//todo: if drivepath is ftpdrive, find treenode in ftproot
				if (node == null)
					node = FindTreeNode(fTPMGR.ftpRootNode.Nodes, drivepath);
				treeView.SelectedNode = node;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载驱动器目录失败: {ex.Message}", "错误");
			}
		}

		public void ListView_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				var v = sender as ListView;
				uiManager.isleft = v == uiManager.LeftList;
				uiManager.SetArgs();
			}
		}
		public void ListView_BeforeLabelEdit(object? sender, EventArgs e)
		{
			var listView = sender as ListView;
			if (listView?.SelectedItems.Count == 0) return;
			var item = listView?.SelectedItems[0];
			if (item?.SubItems[LVCOL_TYPE].Text == "本地磁盘")
			{
				MessageBox.Show("不能重命名本地磁盘");
				e = null;
				return;
			}
			oldname = item?.Text;
		}

		public void ListView_AfterLabelEdit(object? sender, EventArgs e)
		{
			var listView = sender as ListView;
			if (listView?.SelectedItems.Count == 0) return;
			var item = listView?.SelectedItems[0];
			string oldName = oldname;
			var labeleditEvent = e as LabelEditEventArgs;
			if (labeleditEvent.CancelEdit) return;
			var newName = labeleditEvent.Label;
			if (string.IsNullOrEmpty(newName))
			{
				MessageBox.Show("文件名不能为空");
				if (item != null) item.Text = oldName;
				return;
			}
			string oldPath = Path.Combine(CurrentFullpath[LRflag], oldName);
			string newPath = Path.Combine(CurrentFullpath[LRflag], newName);
			if (oldPath == newPath) return;
			if (File.Exists(newPath) || Directory.Exists(newPath))
			{
				MessageBox.Show("文件已存在");
				if (item != null) item.Text = oldName;
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
				if (item != null) item.Text = oldName;
			}
			RefreshPanel(listView);
		}

		public void ListView_MouseUp(object? sender, MouseEventArgs e)
		{
			if (sender is not ListView listView)
				return;
			var item = listView.GetItemAt(e.X, e.Y);
			if (item != null)
				item.Selected = true;

			if (e.Button == MouseButtons.Right)
			{
				if (item != null)
				{
					listView.FocusedItem = item;

					// 检查是否是FTP路径
					if (CurrentFullpath[LRflag].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
					{
						// 从当前目录中提取连接名称
						string connectionName = ExtractFtpConnectionName(CurrentFullpath[LRflag]);
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
						if (node.Text.Equals("回收站"))
						{
							showCtxMenuOnRecyclebin();
							return;
						}
						string iPath = (item.Tag as LvItemTag)?.File?.FullPath ?? "";       //item.SubItems[1].Text;
																							// Get corresponding TreeNode for this path
						TreeNode? targetNode = FindTreeNode(node.Nodes, item.Text);
						if (targetNode != null)
							ShowContextMenuOnTreeview(targetNode, e.Location);
						else
						{
							// If no corresponding node found, use path to show context menu
							ShowCtxMenuOnListview(iPath, e.Location);
						}
					}
				}
				return;
			}
		}
		private void showCtxMenuOnRecyclebin()
		{
			var menu = new ContextMenuStrip();
			menu.Items.Add("还原", null, MenuItemRestore_Click);
			menu.Items.Add("清空回收站", null, MenuItemEmptyRecycleBin_Click);
			menu.Show(Cursor.Position);
		}

		public void ListView_MouseDoubleClick(object? sender, MouseEventArgs e)
		{
			if (sender is not ListView listView)
				return;
			var item = listView.GetItemAt(e.X, e.Y);
			if (item != null)
				item.Selected = true;
			if (listView.SelectedItems.Count == 0) return;

			ListViewItem selectedItem = listView.SelectedItems[0];
			var oldpath = CurrentFullpath[LRflag];
			var file = (selectedItem.Tag as LvItemTag)?.File;
			var path = file?.FullPath;
			var fileSource = CurrentFullpath.GetFileSource(LRflag);
			if (selectedItem.SubItems[0].Text.Equals(".."))
			{
				cmdProcessor.cm_gotoparent();
				return;
			}
			if (selectedItem.SubItems[0].Text.Equals("."))
				return;

			// 检查是否是FTP路径
			if (CurrentFullpath[LRflag].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
			{
				// 从当前目录中提取连接名称
				string connectionName = ExtractFtpConnectionName(CurrentFullpath[LRflag]);
				if (!string.IsNullOrEmpty(connectionName))
				{
					// 处理FTP列表项双击事件
					//fTPMGR.HandleFtpListItemDoubleClick(connectionName, selectedItem, listView);
					FtpFileEntry ftpfile = new FtpFileEntry(path);  //convert '\\'  of path to '/'  by using ftpfileentry
					if (file.IsDirectory)
						fTPMGR.NavigateToPath(connectionName, ftpfile.Path, listView);
					else
					{
						// 如果是文件，查看文件
						//if (fTPMGR._ftpSources.TryGetValue(connectionName, out FtpFileSource? source))
						fTPMGR.ViewFtpFile((FtpFileSource)fileSource, path);
					}
					return;
				}
			}

			var isarchive = false;
			var isinarchive = false;
			if ((fileSource is WcxArchiveFileSource))
			{
				isarchive = true;
				isinarchive = true;
			}
			else if (IsArchiveFile(path))
			{
				isarchive = true;
			}
			if (isarchive)
			{
				// 使用 FileSourceManager 获取 WcxArchiveFileSource
				var lvItemTag = selectedItem.Tag as LvItemTag;
				var lvItemFile = lvItemTag?.File;
				if (lvItemFile != null && (lvItemFile.IsDirectory || !isinarchive))
				{
					if (!CurrentFullpath[LRflag].Equals(path))
						//由于在WCX内部，通过TREEVIEW_AFTERSELECT节点不会发生变化，所以无法记录历史，只能在LISTVIEW_DOUBLECLICK中记录历史
						RecordDirectoryHistory(path, oldpath);  // 记录目录历史

					var node = FindTreeNode(activeTreeview.SelectedNode.Nodes, Path.GetFileName(path));
					activeTreeview.SelectedNode = node;
				}
				else
				{
					// 调用wcxfilesourceexecuteoperation
					var op = fileSource?.CreateExecuteOperation(lvItemFile, fileSource.CurrentPath, "open");
					_operationsManager.AddOperation(op);
				}
				// 更新当前路径
				CurrentFullpath[LRflag] = path;
				return;
			}
			// 获取关联的TreeView
			var treeView = listView == uiManager.LeftList ? uiManager.LeftTree : uiManager.RightTree;
			if (selectedItem.SubItems[LVCOL_TYPE].Text.Equals("<DIR>") || selectedItem.SubItems[LVCOL_TYPE].Text == "本地磁盘")
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
					CurrentFullpath[LRflag] = path;    //IF ITEMPATH IS DIR, UPDATE currentDirectory[isleft], ELSE NOT
					watcher.Path = path;
					watcher.EnableRaisingEvents = true;
				}
			}
			else // 处理文件
			{
				if (File.Exists(path))
				{
					try
					{
						// 如果是可执行文件，直接执行
						if (Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
							Process.Start(path);
						else
							// 使用系统默认关联程序打开文件
							Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
			var pathpart = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
			foreach (var n in nodes)
			{
				var node = n as TreeNode;
				if (node != null && node.Text.Equals(pathpart[0], StringComparison.OrdinalIgnoreCase))
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
		public TreeNode? FindTreeNode(TreeNodeCollection nodes, string path, bool searchftp = false)
		{
			var deepSearch = path.Contains('\\');
			if (!deepSearch)
			{
				if (searchftp)
				{
					foreach (TreeNode node in nodes)
					{
						if (node.Tag is FtpNodeTag tag)
						{
							var ftpsrc = fTPMGR.GetFtpFileSourceByConnectionName(tag.ConnectionName);
							if (path.StartsWith($"ftp://{ftpsrc?.Host}"))
								return node;
						}
					}
				}
				else
				{
					//normal search
					foreach (TreeNode node in nodes)
					{
						//node.fullpath=桌面\此电脑\system (C:)\aDrive, path=c:\\
						if (path.Equals(node.Text, StringComparison.OrdinalIgnoreCase)) return node;
						//	var p = w32.GetPathByIShell(pf, pidl);      ////子节点path -> 此电脑\\迅雷下载, c:\\
						//var n = w32.GetNameByIShell(pf, pidl);    //子节点name -> 迅雷下载, system (c:)
					}
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
								continue;   // 如果是下拉菜单按钮，不执行任何操作
							else
								cmdProcessor.ExecCmd(cmd, file);    // 执行普通按钮命令
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
			// 使用系统图标索引作为键值
			iconKey = string.Empty;
			if (shellInfo.hIcon != IntPtr.Zero && shellInfo.iIcon != 0)
			{
				// 使用系统图标索引作为键值
				iconKey = $"{subItem.PIDL}_{shellInfo.iIcon}".ToLower();
				subItem.IconKey = iconKey;
				using (Icon? icon = (Icon.FromHandle(shellInfo.hIcon).Clone() as Icon))
				{
					iconManager.AddIcon(iconKey, icon, islarge);
				}
				API.DestroyIcon(shellInfo.hIcon);
				return true;
			}
			return false;
		}

		private bool getIconByShellItemPIDL1(ref ShellItem subItem, out string iconKey, bool islarge = false)
		{
			iconKey = string.Empty;

			var pidAbsolute = API.ILCombine(CtrlPanel_PIDL, subItem.PIDL);
			var shellInfo = new SHFILEINFO();
			API.SHGetFileInfoPIDL(pidAbsolute, 0, ref shellInfo, Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI.PIDL | SHGFI.DISPLAYNAME | SHGFI.ICON | SHGFI.SMALLICON);
			// SHGetFileInfo can get name and icon
			//Do something to save item name and icon
			if (shellInfo.hIcon != IntPtr.Zero)
			{
				// 使用系统图标索引作为键值
				iconKey = $"{shellInfo.hIcon}_{shellInfo.iIcon}".ToLower();
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
			IImageList? hImageList = null;

			API.SHGetImageList(islarge ? SHIL.SHIL_LARGE : SHIL.SHIL_SMALL, ref IID_IImageList, ref hImageList);
			if (hImageList != null)
			{
				// 2. 获取图标索引
				var shellInfo = new SHFILEINFO();
				var r = API.SHGetFileInfo(subItem.parsepath, 0, ref shellInfo, Marshal.SizeOf(shellInfo),
				SHGFI.SYSICONINDEX | (islarge ? SHGFI.LARGEICON : SHGFI.SMALLICON));
				if (shellInfo.iIcon > 0 || shellInfo.hIcon != 0)
				{
					// 使用系统图标索引作为键值
					iconKey = $"{subItem.PIDL}_{shellInfo.iIcon}".ToLower();
					subItem.IconKey = iconKey;
					// 3. 从系统图标列表中提取图标
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
		private void GetIconBy(ShellItem? subItem, out string iconkey, nint pidlSub)
		{
			if (!getIconByShellItem(ref subItem, out iconkey))
				if (!getIconBySysImageList(ref subItem, out iconkey))
					if (!getIconByShellItemPIDL1(ref subItem, out iconkey))
					{
						var icon = IconManager.ExtractIconFromPIDL(iCtrlPanel, pidlSub);
						if (icon != null)
							iconManager.AddIcon(pidlSub.ToString(), icon, false);
						else
							getIconByIconLocation(ref subItem, out iconkey);
					}
		}
		public List<TreeNode>? LoadSubDirectories(TreeNode node, MyListView? lv = null)
		{
			Debug.Print($"load sub dirs for treenode : {node.FullPath}");
			// 创建一个新的节点集合，用于存储需要保留的节点
			List<TreeNode> nodesToKeep = new List<TreeNode>();
			if (lv != null)
			{
				lv.SmallImageList ??= new ImageList();
				lv.LargeImageList ??= new ImageList();
				lv.Items.Clear();
			}
			if (node.Tag is not ShellItem) return null; //eg, if it is ftp virtual node, do not load subnode
			ShellItem sItem = (ShellItem)node.Tag;
			if (sItem == null) return null;
			IShellFolder root = sItem.ShellFolder;
			if (root == null) return null;
			if (node.Nodes.Count == 1 && node.Nodes[0].Text.Equals("..."))
				node.Nodes.RemoveAt(0);

			// 标记节点已经加载过子目录
			sItem.SubNodeState = NODE_LOADED_KEY;
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
			}

			// 创建一个集合，用于存储新的PIDL，以便后续比较
			HashSet<string> newPidls = new HashSet<string>();
			try
			{
				//get the config showhiddensystem
				var shcontf = SHCONTF.FOLDERS;
				if (int.TryParse(configLoader.FindConfigValue("Configuration", "ShowHiddenSystem"), out var showhiddensystem))
				{
					if ((showhiddensystem & 2) != 0)
						shcontf |= SHCONTF.INCLUDEHIDDEN;
				}

				if (root.EnumObjects(this.Handle, shcontf, out nint EnumPtr) == w32.S_OK)    // 循环查找子项
				{
					if (EnumPtr == IntPtr.Zero)  //如果node=程序和功能,则EnumPtr=0，直接返回
						return null;

					var Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
					while (Enum.Next(1, out nint pidlSub, out uint celtFetched) == 0 && celtFetched == w32.S_FALSE) //获取子节点的pidl
					{
						root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out IShellFolder iSub); //获取子节点的ishellfolder接口
						string name;
						string path = w32.GetPathByIShell(root, pidlSub);   //子节点path -> 此电脑\\迅雷下载, c:\\

						//Debug.Print(path);
						var pathPart = path.Split('\\');
						name = !pathPart[^1].Equals(string.Empty) ? pathPart[^1] : pathPart[^2];
						var subItem = new ShellItem(pidlSub, iSub, root); //子节点的tag存放pidl和ishellfolder接口

						//if (subItem.parsepath.Equals("::{26EE0668-A00A-44D7-9371-BEB064C98683}"))//控制面板
						//if (subItem.parsepath.Equals("::{645FF040-5081-101B-9F08-00AA002F954E}") )//回收站
						// 使用路径作为唯一标识符，而不是PIDL的内存地址
						string nodeKey = path;
						newPidls.Add(nodeKey);

						// 检查是否已存在相同路径的节点
						TreeNode nodeSub;
						if (existingNodes.TryGetValue(nodeKey, out TreeNode? existingNode))
						{
							// 保留现有节点
							nodeSub = existingNode;
							// 更新节点的Tag，确保使用最新的ShellItem
							nodeSub.Tag = subItem;
						}
						else
							nodeSub = new TreeNode(name) { Tag = subItem }; // 创建新节点

						// 为虚拟文件夹或非文件系统项设置特定图标
						string iconkey;
						if (subItem.IsVirtual || (subItem.GetAttributes() & SFGAO.FILESYSTEM) == 0)
						{
							GetIconBy(subItem, out iconkey, pidlSub);
							if (!string.IsNullOrEmpty(iconkey))
								iconManager.LoadIconFromCacheByKey(iconkey, node.TreeView.ImageList);

							SFGAO subattr = subItem.GetAttributes();    // 如果是文件夹且不是虚拟文件夹，则添加"..."节点
							if (subattr.HasFlag(SFGAO.FOLDER) && nodeSub.Nodes.Count == 0)
								nodeSub.Nodes.Add("...");
						}
						else
						{
							iconkey = IconManager.GetNodeIconKey(nodeSub);
							iconManager.LoadIconFromCacheByKey(iconkey, node.TreeView.ImageList);

							try
							{
								// 如果有子文件夹，则添加"..."节点
								if (Directory.Exists(path))
								{
									var dirinfo = new DirectoryInfo(path);  //压缩文件处理到此处引发异常
									var subdir = dirinfo.GetDirectories();  //windows目录CSC无权限异常
									if (subdir.Length != 0 && nodeSub.Nodes.Count == 0)
										nodeSub.Nodes.Add("...");
								}
							}
							catch (UnauthorizedAccessException)
							{
								//Debug.Print($"unauthorized access exception while try to access {path}");
							}
						}
						nodeSub.ImageKey = iconkey;
						nodeSub.SelectedImageKey = iconkey;

						// 如果是新创建的节点，才添加到父节点
						if (!existingNodes.ContainsValue(nodeSub))
							node.Nodes.Add(nodeSub);

						// 将节点添加到保留列表
						nodesToKeep.Add(nodeSub);
						if (subItem.parsepath.Equals("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}")) //"此电脑"
						{
							if (isleft)
								thispcL = nodeSub;
							else
								thispcR = nodeSub;
						}

						if (lv != null)
						{
							string[] s = ["", "", name.Contains(':') ? "本地磁盘" : "<CLS>", ""];
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
							i.Tag = new LvItemTag(null, node);   //tag存放父节点
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
				//refresh the addressbar's current node's children according to nodestokeep
				var fullFSpath = Helper.getFSpath(node.FullPath);
				var childrenpath = nodesToKeep.Select(x => Helper.getFSpath(x.FullPath)).ToList();
				if (isleft)
					uiManager.LeftPathTextBox.SetChildren(fullFSpath, childrenpath);
				else
					uiManager.RightPathTextBox.SetChildren(fullFSpath, childrenpath);
			}
			catch (Exception)
			{
				Debug.Print("exception raised in loadsubdir");
			}
			finally
			{

			}

			return nodesToKeep;
		}

		//private static bool IsChildrenExist(TreeNode node, bool includefile = false)
		//{
		//	ShellItem sItem = (ShellItem)node.Tag;
		//	if (sItem != null)
		//		return sItem.IsChildrenExist();
		//	return false;
		//}
		//private static void LoadRecycleBinbak(ListView listview)
		//{
		//	int MAX_PATH = 260;
		//	// 获取回收站中的文件和文件夹信息
		//	SHQUERYRBINFO shQueryRBInfo = new SHQUERYRBINFO();
		//	shQueryRBInfo.cbSize = Marshal.SizeOf(shQueryRBInfo);
		//	API.SHQueryRecycleBin(null, ref shQueryRBInfo);

		//	uint dwFlags = 0;
		//	StringBuilder sbDisplayName = new(MAX_PATH);
		//	StringBuilder sbOriginalPath = new(MAX_PATH);

		//	while (API.SHEnumRecycleBin(null, 0, ref dwFlags, sbDisplayName, MAX_PATH, sbOriginalPath, MAX_PATH) == 0)
		//	{
		//		// 创建 ListViewItem 并添加到 ListView 中
		//		ListViewItem item = new ListViewItem(sbDisplayName.ToString());
		//		item.SubItems.Add(sbOriginalPath.ToString());
		//		listview.Items.Add(item);
		//	}
		//}
		//public void LoadRecycleBin(ListView listView)
		//{
		//	// 使用 FileSourceManager 获取回收站 FileSource
		//	IFileSource recycleBinFileSource = _fileSourceManager.GetFileSourceForFullPath("回收站", isleft);

		//	// 更新当前面板的 FileSource
		//	if (listView == uiManager.LeftList)
		//		LeftFileSource = recycleBinFileSource;
		//	else
		//		RightFileSource = recycleBinFileSource;

		//	// 使用 FileSource 架构加载文件列表
		//	LoadListViewByFileSourceSync("回收站", listView, null);
		//}
		//public IEnumerable<string> GetRecycleBinFilenames()
		//{
		//	// 使用 Shell API 获取回收站文件
		//	var fileList = new List<string>();

		//	// 使用直接方法获取回收站文件
		//	string recycleBinPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..\\$Recycle.Bin");

		//	if (Directory.Exists(recycleBinPath))
		//	{
		//		// 获取所有用户的回收站目录
		//		foreach (var userDir in Directory.GetDirectories(recycleBinPath))
		//		{
		//			// 获取每个用户回收站中的文件
		//			try
		//			{
		//				foreach (var file in Directory.GetFiles(userDir, "*", SearchOption.AllDirectories))
		//				{
		//					// 过滤掉系统文件
		//					if (!Path.GetFileName(file).StartsWith("$"))
		//					{
		//						fileList.Add(Path.GetFileName(file) ?? string.Empty);
		//					}
		//				}
		//			}
		//			catch
		//			{
		//				// 忽略访问错误
		//			}
		//		}
		//	}

		//	// 返回收集到的文件名
		//	return fileList;
		//}
		// 在目录变更时调用此方法记录历史
		public void RecordDirectoryHistory(string newPath, string oldpath)
		{
			if (string.IsNullOrEmpty(oldpath) || oldpath.Equals(newPath)) return;
			//if (IsActiveFtpPanel(out var ftpnode))
			//	backStack.Push(oldpath);
			//else
			backStack.Push(oldpath); //同一个filesource下，压入currentpath，不同filesource下，压入老filesource.currentpath

			Debug.Print($"backstack.push: {oldpath}");
			forwardStack.Clear(); // 清除前进历史
		}
		private string? SetIconForListViewItem(ListViewItem lvItem, ListView listView, string subkey)
		{
			if (lvItem != null)
			{
				if (lvItem.SubItems[MainForm.LVCOL_TYPE].Text.Equals("<DIR>"))
				{
					iconManager.LoadIconFromCacheByKey("folder", listView.SmallImageList);
					iconManager.LoadIconFromCacheByKey("folder", listView.LargeImageList, true);
					lvItem.ImageKey = "folder";
				}
				else
				{
					//try to load from lvitem.tag.file.tag.iconkey
					var tag = (lvItem.Tag as LvItemTag);
					var file = tag?.File;
					var shellitem = file?.Tag as ShellItem;
					var key = shellitem?.IconKey;
					var itemFullName = file?.FullPath;

					if (key == null && shellitem != null)
						GetIconBy(shellitem, out key, shellitem.PIDL);
					if (string.IsNullOrEmpty(key))
						key = Path.GetExtension(itemFullName);

					// 设置默认图标
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
						// 先设置默认图标
						if (!iconManager.HasIconKey(key, true))
						{
							var icol = IconManager.GetIconByFileNameEx("FILE", itemFullName, true);
							if (icol != null)
								iconManager.AddIcon(key, icol, true);
						}
						iconManager.LoadIconFromCacheByKey(key, listView.LargeImageList, true);
						lvItem.ImageKey = key;

						// 返回文件路径，用于后续生成缩略图
						return itemFullName;
					}
				}
			}
			return null;
		}

		// 处理ListView滚动事件
		public void ListView_Scroll(object sender, EventArgs e)
		{
			if (sender is ListView listView)
			{
				// 获取当前视图模式
				var subkey = (listView.View == View.Tile ? "l" : "s");

				// 处理可见项的缩略图
				ProcessVisibleItemsForThumbnails(listView, subkey);
			}
		}

		// 获取ListView中当前可见的项目并处理缩略图
		private void ProcessVisibleItemsForThumbnails(ListView listView, string subkey)
		{
			// 如果不是大图标或平铺模式，不需要生成缩略图
			if (listView.Items.Count == 0) return;

			var itemsForJob = new List<string>();
			var lvitemsForJob = new List<ListViewItem>();
			var jobtypelist = new List<BackgroundIconManager.JobType>();
			// 获取可见区域
			var visibleRect = listView.ClientRectangle;

			// 遍历所有项目，检查是否在可见区域内
			foreach (ListViewItem item in listView.Items)
			{
				// 获取项目的边界
				var itemRect = item.Bounds;
				var isYin = itemRect.Y + itemRect.Height > visibleRect.Y;//是否进入上边界
				var isYin1 = itemRect.Y - itemRect.Height * 2 < visibleRect.Height;//是否在下边界内，最下方的两行item不处理，放宽条件
																				   // 检查项目是否在可见区域内
				if (isYin && isYin1) // (itemRect.IntersectsWith(visibleRect))// temp set to true
				{
					var file = (item.Tag as LvItemTag)?.File;
					var itemFullName = file?.FullPath;  //item.SubItems[1].Text;
					var isdir = (file?.IsDirectory) ?? false;
					if (isdir)
					{
						//if is dir, calc dir size
						if ((item.SubItems[LVCOL_SIZE].Text.Equals("0 B")) && showFolderSize)
						{
							// 检查缓存中是否已有该文件夹的大小
							if (itemFullName != null && _backgroundIconManager.HasDirSizeCache(itemFullName))
							{
								// 从缓存获取文件夹大小并更新UI
								long cachedSize = _backgroundIconManager.GetDirSizeFromCache(itemFullName);
								item.SubItems[LVCOL_SIZE].Text = FileSystemManager.FormatFileSize(cachedSize, true);
								if (file != null)
									file.Size = cachedSize;
							}
							else if (itemFullName != null)
							{
								// 如果缓存中没有，添加到任务队列
								itemsForJob.Add(itemFullName);
								lvitemsForJob.Add(item);
								jobtypelist.Add(BackgroundIconManager.JobType.DirSize);
							}
						}
					}
					else
					{
						// 检查是否是文件（不是文件夹）
						//&& item.ImageKey.StartsWith('.') if already generate thumbnail, the imagekey should be like kewkjr51643k67jakjt, otherwise imagekey should be .avi, so if imagekey start with ., indicate the item's thumbnail has not be generated yet, otherwise skip the item.
						// 检查是否已经有缩略图
						if (!string.IsNullOrEmpty(itemFullName) && subkey == "l" &&
							(item.ImageKey == Path.GetExtension(itemFullName) || string.IsNullOrEmpty(item.ImageKey)))
						{
							itemsForJob.Add(itemFullName);
							lvitemsForJob.Add(item);
							jobtypelist.Add(BackgroundIconManager.JobType.Thumbnail);
						}
					}
				}
			}

			// 如果有需要处理的项目，加入缩略图生成队列
			if (itemsForJob.Count > 0)
				_backgroundIconManager.EnqueueJob(listView, itemsForJob, lvitemsForJob, jobtypelist);

		}
		// 加载文件列表 - 使用 FileSource 架构（异步版本）
		public void LoadListViewByFileSource(string path, ListView listView, TreeNode parentnode)
		{
			if (string.IsNullOrEmpty(path)) return;

			// 确定当前面板
			bool isLeftPanel = listView == uiManager.LeftList;
			var fileSource = CurrentFullpath.GetFileSource(listView.Name);
			if (fileSource is ShellFileSource)  //如果是虚拟节点（由shellfilesource处理的节点），由于在loadsubdirectories中已经生成，所以无需再处理
				return;

			Debug.Print($"load listview by filesource [{listView.Name}]: {path}");
			try
			{
				// 创建列表操作
				var listOperation = fileSource?.CreateListOperation(path);
				if (listOperation == null)
				{
					Debug.Print($"无法为路径 {path} 创建列表操作");
					return;
				}

				// 执行列表操作
				_operationsManager.AddOperation(listOperation);
				listOperation._Thread.WaitFor();    // 等待操作完成

				// 获取文件列表结果
				if (listOperation is not FileSourceListOperation fileListOperation)
				{
					Debug.Print($"操作不是 FileSourceListOperation 类型");
					return;
				}
				var files = fileListOperation.Files;    //files include '.' '..' , for wcx: fileentry.fullpath = "\\aaa\\test.txt", is wcx internal usage
				if (files == null)
				{
					Debug.Print($"列表操作未返回文件列表");
					return;
				}

				// 更新 ListView
				listView.BeginUpdate();
				listView.Items.Clear();
				var subkey = (listView.View == View.Tile ? "l" : "s");
				showFolderSize = configLoader.FindConfigValue("Configuration", "EverythingForSize").Equals("1");

				// 应用视图管理器设置 - 根据文件夹内容自动切换视图模式
				var viewname = viewMgr.ApplyViewToListView(listView, path, fileSource);

				// 添加所有项目到 ListView
				foreach (var file in files)
				{
					var lvItem = CreateListViewItemFromFileEntry(file, showFolderSize, parentnode, viewname);
					if (lvItem != null)
					{
						var f = SetIconForListViewItem(lvItem, listView, subkey);
						listView.Items.Add(lvItem);
					}
				}

				listView.EndUpdate();
				listView.Refresh();

				// 只为可见项生成缩略图
				ProcessVisibleItemsForThumbnails(listView, subkey);

				// 添加滚动事件处理程序（如果尚未添加）
				if (listView.Tag == null && listView is MyListView myListView)
				{
					EventHandler scrollHandler = (s, e) =>
					{
						if (s != null) ListView_Scroll(s, e);
					};
					myListView.VScroll += scrollHandler;
					myListView.MouseWheel += scrollHandler;
					listView.Tag = "ScrollEventAttached";
				}

				// 更新状态栏
				var status = (listView == uiManager.LeftList) ? uiManager.LeftStatusStrip : uiManager.RightStatusStrip;
				uiManager.UpdateStatusBar(listView, status);
			}
			catch (Exception ex)
			{
				Debug.Print($"加载文件列表失败: {ex.Message}");
			}
		}

		// 创建 ListViewItem (从 FileEntry)
		private ListViewItem? CreateListViewItemFromFileEntry(FileEntry file, bool showFolderSize, TreeNode node, string viewname)
		{
			try
			{
				int viewid = 0;
				if (!viewname.Equals("默认"))
				{
					viewid = int.Parse(viewname);
				}

				if (viewid <= 5) {
					// 如果默认视图也不存在，使用硬编码的默认列
					string attrStr = GetFileAttributesString(file.Attributes);
					string[] defaultData;

					if (file.IsDirectory)
					{
						defaultData = [
							file.Name,
							showFolderSize && EverythingWrapper.IsEverythingServiceRunning() ? FileSystemManager.FormatFileSize(file.Size, true) : "",
							"<DIR>",
							file.ModificationTime.ToString("yyyy-MM-dd HH:mm"),
							attrStr
						];
					}
					else
					{
						string extension = Path.GetExtension(file.Name).ToUpperInvariant();
						defaultData = [
							file.Name,
							FileSystemManager.FormatFileSize(file.Size, true),
							extension,
							file.ModificationTime.ToString("yyyy-MM-dd HH:mm"),
							attrStr
						];
					}

					var defaultItem = new ListViewItem(defaultData);
					defaultItem.Tag = new LvItemTag(file, node);
					return defaultItem;
				} 
				else
				{
					// 获取当前视图模式的列定义
					var colDefs = viewMgr.colDefDict.Values.ToArray()[viewid - 6];
						
					// 根据列定义创建数据数组
					string[] itemData = new string[colDefs.Count];

					// 填充数据
					for (int i = 0; i < colDefs.Count; i++)
					{
						var colDef = colDefs[i];
						string content = colDef.content.Trim();

						// 根据列内容定义获取对应的数据
						if (content.Equals("文件名", StringComparison.OrdinalIgnoreCase))
						{
							itemData[i] = file.Name;
						}
						else if (content.Equals("扩展名", StringComparison.OrdinalIgnoreCase))
						{
							itemData[i] = file.IsDirectory ? "<DIR>" : Path.GetExtension(file.Name).ToUpperInvariant();
						}
						else if (content.Contains("大小", StringComparison.OrdinalIgnoreCase))
						{
							if (file.IsDirectory)
								itemData[i] = showFolderSize && EverythingWrapper.IsEverythingServiceRunning() ? FileSystemManager.FormatFileSize(file.Size, true) : "";
							else
								itemData[i] = FileSystemManager.FormatFileSize(file.Size, true);
						}
						else if (content.Contains("日期", StringComparison.OrdinalIgnoreCase) || content.Contains("时间", StringComparison.OrdinalIgnoreCase))
						{
							itemData[i] = file.ModificationTime.ToString("yyyy-MM-dd HH:mm");
						}
						else if (content.Contains("属性", StringComparison.OrdinalIgnoreCase))
						{
							itemData[i] = GetFileAttributesString(file.Attributes);
						}
						else
						{
							// 默认为空字符串
							itemData[i] = "";
						}
					}

					var ret = new ListViewItem(itemData);
					ret.Tag = new LvItemTag(file, node);
					return ret;
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"创建列表项失败: {ex.Message}");
				return null;
			}
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
						previewPanel.Text += "\r\n[文件过大，仅显示前1MB内容...]";
				}
				else
					previewPanel.Text = "[二进制文件]";
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
				string filePath = Helper.getFSpath(Path.Combine(CurrentFullpath[LRflag], selectedItem.Text));

				if (File.Exists(filePath))
					await PreviewFileAsync(filePath, previewPanel);
			}
			uiManager.SetArgs();
		}

		public void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
		{
			if (sender is not ListView listView) return;

			// 如果点击的是同一列，切换排序顺序
			if (e.Column == sortColumn)
			{
				sortOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
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
		private class ListViewItemComparer : IComparer
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

		//private void InitializeThemeToggleButton()
		//{
		//	ToolStripButton themeToggleButton = new ToolStripButton
		//	{
		//		Text = "切换主题",
		//		DisplayStyle = ToolStripItemDisplayStyle.Text
		//	};
		//	themeToggleButton.Click += ThemeToggleButton_Click;
		//	uiManager.toolbarManager.DynamicToolStrip.Items.Add(themeToggleButton);
		//}

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
			cm_list();
		}
		private List<FileEntry> GetFileListByViewOrParam(string param, bool isViaTemp = true)
		{
			if (!string.IsNullOrWhiteSpace(param))
			{
				var ret = se.PrepareParameter(param, new string[] { }, "");
				if (ret != null && ret.Count > 0)
				{
					return ret.Select(x => new FileEntry(x)).ToList();
				}
			}

			List<FileEntry> result = new();
			if (activeListView.SelectedItems.Count == 0) return result;

			// 获取原始文件列表
			var originalFiles = new List<FileEntry>();
			foreach (ListViewItem item in activeListView.SelectedItems)
			{
				var fileEntry = GetListItemPath(item);
				if (fileEntry != null)
				{
					originalFiles.Add(fileEntry);
				}
			}

			// 检查是否是FTP路径，并且是需要下载的操作（cm_edit或cm_list）
			var filesource = CurrentFullpath.GetFileSource(LRflag);
			if ((filesource is FtpFileSource || filesource is WcxArchiveFileSource) && isViaTemp)
			{
				// 从当前目录中提取连接名称
				//string connectionName = ExtractFtpConnectionName(CurrentFullpath[LRflag]);
				//if (!string.IsNullOrEmpty(connectionName) && fTPMGR.ftpSources.TryGetValue(connectionName, out var ftpSource))
				{
					// 使用FtpCopyOutOperation下载文件到临时目录
					var tempFiles = DownloadFilesToTemp(filesource, originalFiles);
					if (tempFiles.Count > 0)
						return tempFiles;
				}
			}

			// 非FTP路径或FTP处理失败，或者是不需要下载的操作（cm_copy, cm_renmov, cm_delete），使用原来的逻辑
			return originalFiles;
		}

		/// <summary>
		/// 将FTP文件下载到临时目录
		/// </summary>
		/// <param name="ftpSource">FTP文件源</param>
		/// <param name="sourceFiles">源文件列表</param>
		/// <returns>临时文件列表</returns>
		private List<FileEntry> DownloadFilesToTemp(IFileSource? ftpSource, List<FileEntry> sourceFiles)
		{
			try
			{
				// 创建临时文件系统
				ITempFileSystemFileSource tempFileSource = new TempFileSystemFileSource();
				string tempPath = tempFileSource.FileSystemRoot;

				// 创建文件条目列表
				var fileEntries = FileSourceUtil.FileEntryListToFileEntries(sourceFiles, ftpSource is FtpFileSource);
				if (fileEntries.Count == 0)
					return [];

				// 创建FTP复制出操作
				var copyOutOperation = ftpSource?.CreateCopyOutOperation(
					tempFileSource,
					fileEntries,
					tempPath);

				if (copyOutOperation != null)
				{
					// 添加操作到管理器并执行
					_operationsManager.AddOperation(copyOutOperation);
					var opitem = _operationsManager.GetItemByOperation(copyOutOperation);
					opitem?.OperationThread.WaitFor();

					Debug.Print($"now check the copyout operation result{copyOutOperation.Result}");

					// 检查操作是否成功完成
					if (copyOutOperation.Result == FileSourceOperationResult.Finished)
					{
						// 获取临时目录中的所有文件
						List<FileEntry> tempFiles = new List<FileEntry>();
						foreach (var file in fileEntries)
						{
							string tempFilePath = Path.Combine(tempPath, file.Name);
							if (File.Exists(tempFilePath))
							{
								var tempFile = FileSystemFileSource.CreateFileFromFile(tempFilePath);
								tempFiles.Add(tempFile);
							}
						}
						return tempFiles;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"下载FTP文件到临时目录失败: {ex.Message}");
				MessageBox.Show($"下载FTP文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return [];
		}

		public void cm_list(string param = "")
		{
			// 编辑按钮点击处理逻辑
			var filePaths = GetFileListByViewOrParam(param);
			if (filePaths.Count == 0) return;
			Form viewerForm = new ViewerForm(filePaths.Select(x => x.FullPath).ToList(), wlxModuleList)
			{
				Text = $"查看文件 - {filePaths}",
				Size = new Size(800, 600)
			};
			viewerForm.Show();
		}
		public void EditButton_Click(object? sender, EventArgs e)
		{
			cm_edit();
		}
		public void cm_edit(string param = "")
		{
			//读取配置文件的editor, 首先调用用户自定义外部编辑器
			var user_edit = Helper.GetPathByEnv(configLoader.FindConfigValue("Configuration", "Editor"));
			Debug.Print(user_edit);
			if (!string.IsNullOrWhiteSpace(user_edit))
			{
				myShellExe(user_edit);
				//cmdProcessor.cm_executedos1(user_edit);
				return;
			}

			var files = GetFileListByViewOrParam(param);
			// 检测文件类型，如果是2进制文件则不打开
			//if (!Helper.IsTextFile(files[0]))
			//{
			//	MessageBox.Show($"无法打开二进制文件{files[0]}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//	return;
			//}
			var editorForm = new NewEditorForm(files.Select(x => x.FullPath).ToList())
			{
				Text = $"编辑文件 - {files[0]}",
				Size = new Size(800, 600)
			};
			editorForm.Show();
		}
		public void CopyButton_Click(object? sender, EventArgs e)
		{
			cm_copy();
		}
		public void DeleteButton_Click(object? sender, EventArgs e)
		{
			cm_delete();
		}

		public void FolderButton_Click(object? sender, EventArgs e)
		{
			cm_mkdir();
		}
		// 创建新文件夹
		public void cm_mkdir(string? folderName = null)
		{
			if (string.IsNullOrEmpty(folderName))
				folderName = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称: eg. dir1,dir2\\dir3", "新建文件夹", "新建文件夹");
			if (string.IsNullOrWhiteSpace(folderName)) return;
			var dirs = folderName.Split(',');
			var path = CurrentFullpath[LRflag];

			try
			{
				// 使用 FileSourceManager 获取合适的 FileSource
				IFileSource fileSource = _fileSourceManager.GetFileSourceForFullPath(path, isleft);

				// 使用 FileSource 架构创建目录
				foreach (var dir in dirs)
				{
					var operation = fileSource.CreateCreateDirectoryOperation(path, dir);
					if (operation != null)
					{
						_operationsManager.AddOperation(operation);
						operation._Thread.WaitFor();
					}
					else
					{
						// 如果无法创建操作，使用传统方法
						if (fTPMGR.IsFtpPath(path))
						{
							// FTP创建文件夹
							var ftpSource = fTPMGR.GetFtpSource(path);
							if (ftpSource != null)
							{
								string newFolderPath = Path.Combine(path, dir).Replace("\\", "/");
								ftpSource.CreateDirectory(newFolderPath);
							}
						}
						else
						{
							// 本地创建文件夹
							var newFolderPath = Path.Combine(path, dir);
							FileSystemManager.CreateDirectory(newFolderPath);
						}
					}
				}

				RefreshPanel(activeListView);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建文件夹失败: {ex.Message}", "错误");
			}
		}
		public void MoveButton_Click(object? sender, EventArgs e)
		{
			cm_renmov();
		}

		public void RefreshTreeViewAndListView(MyListView listView, string path)
		{
			if (listView == null) return;
			var node = listView == uiManager.LeftList ? uiManager.LeftTree.SelectedNode : uiManager.RightTree.SelectedNode;
			LoadSubDirectories(node, listView);

			// 使用 FileSource 架构加载文件列表
			LoadListViewByFileSource(path, listView, node);
		}
		public void RefreshPanel(TreeView treeView)
		{
			if (treeView == null) return;
			RefreshPanel(treeView == uiManager.LeftTree ? RefreshPanelMode.Left : RefreshPanelMode.Right);
		}
		public void RefreshPanel(ListView? listView)
		{
			if (listView == null) return;
			RefreshPanel(listView == uiManager.LeftList ? RefreshPanelMode.Left : RefreshPanelMode.Right);
		}

		public void RefreshActivePanel()
		{
			RefreshPanel(isleft);
		}
		public void RefreshPanel(bool isleft)
		{
			RefreshPanel(isleft ? RefreshPanelMode.Left : RefreshPanelMode.Right);
		}
		public void RefreshPanel(RefreshPanelMode mode)
		{
			if (mode.HasFlag(RefreshPanelMode.Left))
			{
				string path = CurrentFullpath["L"];
				if (!string.IsNullOrEmpty(path))
				{
					//refresh the treeview
					var node = uiManager.LeftTree.SelectedNode;
					LoadSubDirectories(node, uiManager.LeftList);
					var fs = CurrentFullpath.GetFileSource("L");///////////////////////////////////
					if (LeftFileSource != fs)
					{
						Debug.Print($"WARNING: filesource CHANGED in refreshpanel {LeftFileSource} -> {fs}");
						LeftFileSource = fs;
					}
					else
						Debug.Print("unnecessary filesource assignment in refreshpanel");

					// 使用 FileSource 架构刷新左面板
					LoadListViewByFileSource(path, uiManager.LeftList, uiManager.LeftTree.SelectedNode);
				}
				else if (IsFtpPanel(out var ftpnode, "L") && ftpnode != null)
				{
					RefreshTreeViewAndListView(uiManager.LeftList, ftpnode.Path);
				}
				else if (uiManager.LeftTree.SelectedNode?.Tag is ShellItem shellItem)
				{
					RefreshTreeViewAndListView(uiManager.LeftList, shellItem.parsepath);
				}
			}

			if (mode.HasFlag(RefreshPanelMode.Right))
			{
				string path = CurrentFullpath["R"];
				if (!string.IsNullOrEmpty(path))
				{
					//refresh the treeview
					var node = uiManager.RightTree.SelectedNode;
					LoadSubDirectories(node, uiManager.RightList);
					RightFileSource = CurrentFullpath.GetFileSource("R");///////////////////////////////////

					// 使用 FileSource 架构刷新右面板
					LoadListViewByFileSource(path, uiManager.RightList, uiManager.RightTree.SelectedNode);
				}
				else if (IsFtpPanel(out var ftpnode, "R") && ftpnode != null)
				{
					RefreshTreeViewAndListView(uiManager.RightList, ftpnode.Path);
				}
				else if (uiManager.RightTree.SelectedNode?.Tag is ShellItem shellItem)
				{
					RefreshTreeViewAndListView(uiManager.RightList, shellItem.parsepath);
				}
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

		public void MenuItem_Click(object? sender, EventArgs e)
		{
			if (sender is ToolStripMenuItem menuItem)
			{
				if (menuItem != null && menuItem.Tag != null)
				{
					if (menuItem.Tag is MenuInfo mi) //new calling method with MenuInfo
					{
						Debug.Print($"点击了菜单项: {menuItem.Text} , cmd : {mi.Cmd}");
						cmdProcessor.ExecCmdByMenuInfo(mi);
					}
					else
					{
						var cmd = (string)menuItem.Tag;//51 or cm_xx, legacy calling method
						Debug.Print($"点击了菜单项: {menuItem.Text} , cmd : {cmd}");
						cmdProcessor.ExecCmd(cmd);
					}
				}
			}
		}

		public void SetViewMode(View viewMode)
		{
			if (viewMode == activeListView.View) return;
			var needupdate = viewMode == View.Tile || activeListView.View == View.Tile;
			activeListView.View = viewMode;
			if (needupdate)
				RefreshActivePanel();//update imagekey
		}

		/// <summary>
		/// 切换视图模式 - 实现cm_switchviewmode命令
		/// </summary>
		public void cm_switchviewmode()
		{
			// 创建视图模式选择对话框
			using var form = new Form
			{
				Text = "选择视图模式",
				Size = new Size(400, 500),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			};

			// 创建TabControl用于分类显示不同类型的视图
			var tabControl = new TabControl
			{
				Dock = DockStyle.Fill,
				Padding = new Point(10, 10)
			};

			// 系统视图选项卡
			var systemTab = new TabPage("系统视图");
			var systemListView = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				HideSelection = false
			};
			systemListView.Columns.Add("视图名称", 150);
			systemListView.Columns.Add("描述", 200);

			// 添加系统视图选项
			var systemViews = new[]
			{
				new { Name = "详细信息", View = View.Details, Description = "显示文件的详细信息（名称、大小、类型等）" },
				new { Name = "列表", View = View.List, Description = "以简单列表形式显示文件" },
				new { Name = "平铺", View = View.Tile, Description = "以平铺方式显示文件图标和信息" },
				new { Name = "大图标", View = View.LargeIcon, Description = "显示大图标" },
				new { Name = "小图标", View = View.SmallIcon, Description = "显示小图标" }
			};

			foreach (var view in systemViews)
			{
				var item = new ListViewItem(view.Name);
				item.SubItems.Add(view.Description);
				item.Tag = view.View;
				systemListView.Items.Add(item);
			}

			// 自定义视图选项卡
			var customTab = new TabPage("自定义视图");
			var customListView = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				HideSelection = false
			};
			customListView.Columns.Add("视图名称", 150);
			customListView.Columns.Add("列配置", 200);

			// 添加自定义视图选项
			foreach (var viewMode in viewMgr.colDefDict)
			{
				var item = new ListViewItem(viewMode.Key);
				item.SubItems.Add(viewMgr.GetColDef(viewMode.Key));
				item.Tag = viewMode.Key;
				customListView.Items.Add(item);
			}

			// 添加控件到选项卡
			systemTab.Controls.Add(systemListView);
			customTab.Controls.Add(customListView);

			// 添加选项卡到TabControl
			tabControl.TabPages.Add(systemTab);
			tabControl.TabPages.Add(customTab);

			// 添加按钮面板
			var buttonPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 50
			};

			var okButton = new Button
			{
				Text = "确定",
				DialogResult = DialogResult.OK,
				Location = new Point(form.Width - 180, 15),
				Width = 75
			};

			var cancelButton = new Button
			{
				Text = "取消",
				DialogResult = DialogResult.Cancel,
				Location = new Point(form.Width - 90, 15),
				Width = 75
			};

			buttonPanel.Controls.Add(okButton);
			buttonPanel.Controls.Add(cancelButton);

			// 添加控件到表单
			form.Controls.Add(tabControl);
			form.Controls.Add(buttonPanel);
			form.AcceptButton = okButton;
			form.CancelButton = cancelButton;

			// 显示对话框并处理结果
			if (form.ShowDialog() == DialogResult.OK)
			{
				// 根据选择的选项卡应用不同的视图
				if (tabControl.SelectedTab == systemTab && systemListView.SelectedItems.Count > 0 && systemListView.SelectedItems[0].Tag != null)
				{
					// 应用系统视图
					if (systemListView.SelectedItems[0].Tag is View selectedView)
					{
						SetViewMode(selectedView);
					}
				}
				else if (tabControl.SelectedTab == customTab && customListView.SelectedItems.Count > 0 && customListView.SelectedItems[0].Tag != null)
				{
					// 应用自定义视图
					string? selectedViewName = customListView.SelectedItems[0].Tag as string;
					if (selectedViewName == null) return;

					// 设置为详细信息视图以显示列
					activeListView.View = View.Details;

					// 应用自定义视图配置
					var path = CurrentFullpath[LRflag];
					var fileSource = CurrentFullpath.GetFileSource(LRflag);

					// 清除现有列并应用新视图
					activeListView.BeginUpdate();
					activeListView.Columns.Clear();

					// 手动应用列配置
					if (viewMgr.colDefDict.TryGetValue(selectedViewName, out var colDefs) && colDefs != null)
					{
						foreach (var colDef in colDefs)
						{
							var column = new ColumnHeader
							{
								Text = colDef.header,
								Width = colDef.width
							};

							// 设置对齐方式
							if (colDef.content.Contains("->]") || colDef.content.Contains("=tc.大小"))
								column.TextAlign = HorizontalAlignment.Right;
							else
								column.TextAlign = HorizontalAlignment.Left;

							activeListView.Columns.Add(column);
						}
					}

					// 刷新列表视图以应用新的视图
					RefreshActivePanel();
					activeListView.EndUpdate();
				}
			}
		}
		public bool IsArchiveFile(string filePath)
		{
			return _fileSourceManager.IsArchiveFile(filePath);
		}

		public bool OpenArchive(string archivePath, OpenMode openMode = OpenMode.PK_OM_LIST)
		{
			if (openArchives.ContainsKey(archivePath)) return true;
			string ext = Path.GetExtension(archivePath).ToLower();
			// 获取所有支持该后缀名的插件
			var modules = wcxModuleList._modules.Where(m => m.DetectStrings.Contains(ext.TrimStart('.')));
			if (!modules.Any())
			{
				// 如果没有找到支持该后缀名的插件，尝试使用GetModuleByExt方法
				var wcxModule = wcxModuleList.GetModuleByExt(ext);
				if (wcxModule != null)
					modules = new[] { wcxModule };
			}

			if (!modules.Any()) return false;

			// 尝试所有支持该后缀名的插件
			foreach (var wcxModule in modules)
			{
				if (!wcxModule.CanYouHandleThisFile(archivePath)) continue; // 如果插件不能处理该文件，尝试下一个插件

				IntPtr handle = wcxModule.OpenArchiveHandle(archivePath, (int)openMode, out var openResult);
				if (handle == IntPtr.Zero) continue; // 如果打开失败，尝试下一个插件

				openArchives[archivePath] = handle;
				return true; // 成功打开文件，返回true
			}
			return false; // 所有插件都无法处理该文件，返回false
		}
		//public void CloseAllArchives()
		//{
		//	foreach (var archive in openArchives.Keys.ToList())
		//		CloseArchive(archive);
		//}
		private void CloseArchive(string archivePath)
		{
			if (!openArchives.ContainsKey(archivePath)) return;

			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule != null)
			{
				wcxModule.CloseArchive(openArchives[archivePath]);
				openArchives.Remove(archivePath);
				archivePaths.Remove(archivePath);
			}
		}

		//public List<ListViewItem> LoadArchiveContents(string archivePath)
		//{
		//	List<ListViewItem> items = new List<ListViewItem>();
		//	string ext = Path.GetExtension(archivePath).ToLower();
		//	var wcxModule = wcxModuleList.GetModuleByExt(ext);
		//	if (wcxModule == null || !openArchives.ContainsKey(archivePath)) return items;

		//	IntPtr handle = openArchives[archivePath];
		//	//THeaderDataExW headerData = new THeaderDataExW();
		//	var headerData = new WcxHeader();
		//	while (wcxModule.ReadHeader(handle, out headerData))
		//	{
		//		var item = new ListViewItem(headerData.FileName);
		//		var lvitem = item.Tag as LvItemTag;
		//		item.Tag = new ArchNodeTag(lvitem.File, lvitem.Node) { Path = "", Handler = handle };
		//		item.SubItems.Add(archivePath + "\\" + headerData.FileName); // file name with full path
		//																	 // 将 vhigh 左移32位，然后与 vlow 进行按位或运算
		//		var isdir = (int)headerData.FileAttr == 16;
		//		var ext1 = Path.GetExtension(headerData.FileName);
		//		var UnpSize = headerData.UnpSize; // ((ulong)headerData.UnpSizeHigh << 32) | headerData.UnpSizeLow;
		//		item.SubItems.Add(FileSystemManager.FormatFileSize(UnpSize, true));
		//		item.SubItems.Add(isdir ? "<DIR>" : ext1.TrimStart('.')); // <dir> / <ext>
		//		item.SubItems.Add(DateTime.FromFileTime(headerData.FileTime).ToString());
		//		//item.SubItems.Add(headerData.Method.ToString());

		//		item.SubItems.Add(UnpSize.ToString()); // origin size
		//		var attrstr = GetFileAttributesString((FileAttributes)headerData.FileAttr);
		//		item.SubItems.Add(attrstr); // ACDHS
		//		items.Add(item);

		//		wcxModule.ProcessFile(handle, ProcessMode.PK_SKIP, "", ""); // Skip file
		//	}
		//	CloseArchive(archivePath);
		//	return items;
		//}

		public bool ExtractArchiveFile(string archivePath, string fileName, string destPath)
		{
			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null || !openArchives.ContainsKey(archivePath)) return false;

			IntPtr handle = openArchives[archivePath];
			//THeaderDataExW headerData = new THeaderDataExW();
			//WcxHeader headerData;
			while (wcxModule.ReadHeader(handle, out var headerData))
			{
				if (headerData.FileName == fileName)
					return wcxModule.ProcessFile(handle, ProcessMode.PK_EXTRACT, destPath, fileName) == 0;// 0 SKIP, 1 TEST, 2 EXTRACT
				wcxModule.ProcessFile(handle, ProcessMode.PK_SKIP, "", ""); // 0 Skip file
			}
			CloseArchive(archivePath);
			return false;
		}

		//public bool AddToArchive(string archivePath, string[] files)
		//{
		//	string ext = Path.GetExtension(archivePath).ToLower();
		//	var wcxModule = wcxModuleList.GetModuleByExt(ext);
		//	if (wcxModule == null)
		//		return false;
		//	if ((wcxModule.PluginCapabilities & (int)PackerCaps.PK_CAPS_MODIFY) == 0)
		//	{
		//		MessageBox.Show("该插件不支持修改压缩文件内容", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//		return false;
		//	}
		//	//string fileList = string.Join("\n", files); //TODO:Each string in fileList is zero-delimited (ends in zero), and the fileList string ends with an extra zero byte, i.e. there are two zero bytes at the end of AddList.
		//	//string fileList = string.Join("\0", files) + "\0\0";
		//	var packfilesflags = PackFilesFlags.PK_PACK_SAVE_PATHS;
		//	return wcxModule.PackFiles(archivePath, "", Path.GetDirectoryName(files[0]), ConvertStringArrayToStringSeperateWithZeroDelimiter(files), (int)packfilesflags) == 0;
		//}

		public static string ConvertStringArrayToStringSeperateWithZeroDelimiter(string[] input)
		{
			string result = "";
			// Filenames must be relative to archive root and shouldn't start with path delimiter.
			// TC ends paths to directories to be deleted with '\*.*'
			// (which means delete this directory and all files in it).
			foreach (string str in input)
				result += (str) + '\0';
			result += '\0';
			return result;
		}

		//public bool DeleteFromArchive(string archivePath, string[] files)
		//{
		//	string ext = Path.GetExtension(archivePath).ToLower();
		//	var wcxModule = wcxModuleList.GetModuleByExt(ext);
		//	if (wcxModule == null)
		//		return false;
		//	if ((wcxModule.PluginCapabilities & (int)PackerCaps.PK_CAPS_DELETE) == 0)
		//	{
		//		MessageBox.Show("该插件不支持删除文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//		return false;
		//	}
		//	//OpenArchive(archivePath, UnpackFlags.PK_OM_EXTRACT);
		//	//string fileList = string.Join("\0", files) + "\0\0";
		//	string fileList = ConvertStringArrayToStringSeperateWithZeroDelimiter(files);
		//	Encoding utf8Encoding = Encoding.UTF8;
		//	int byteCount = utf8Encoding.GetByteCount(fileList);
		//	Debug.Print($"字符串在 UTF - 8 编码下的字节数: {byteCount}");

		//	Encoding asciiEncoding = Encoding.ASCII;
		//	byteCount = asciiEncoding.GetByteCount(fileList);
		//	Debug.Print($"字符串在 ASCII 编码下的字节数: {byteCount}");
		//	return wcxModule.DeleteFiles(archivePath, fileList) == 0; // archivepath should be full path and name of the the archive.
		//}

		// 用于标记节点是否已经加载过子目录的键
		private const string NODE_LOADED_KEY = "SubDirsLoaded";

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			Control.CheckForIllegalCrossThreadCalls = false;//设置该属性 为false

			try
			{
				// 确定哪个面板正在显示变化的目录
				var selectedDrive = uiManager.LeftDriveComboBox.SelectedItem?.ToString();
				var isLeftPanel = selectedDrive != null && watcher.Path.StartsWith(selectedDrive);
				var treeView = isLeftPanel ? uiManager.LeftTree : uiManager.RightTree;
				var listView = isLeftPanel ? uiManager.LeftList : uiManager.RightList;

				// 找到对应的节点
				TreeNode? affectedNode = null;
				if (treeView.SelectedNode != null && treeView.SelectedNode.Tag is ShellItem sItem)
				{
					string nodePath = sItem.parsepath;
					if (string.Equals(nodePath, watcher.Path, StringComparison.OrdinalIgnoreCase))
					{
						affectedNode = treeView.SelectedNode;
					}
				}

				// 如果找到了受影响的节点，清除其加载标记
				if (affectedNode != null && affectedNode.Tag is ShellItem shellItem)
				{
					// 清除节点的加载标记，以便下次访问时重新加载子目录
					shellItem.SubNodeState = "";

					// 如果变化是创建或删除目录，则刷新TreeView
					if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Deleted)
					{
						if (Directory.Exists(e.FullPath) || e.ChangeType == WatcherChangeTypes.Deleted)
						{
							// 在UI线程上执行刷新操作
							this.BeginInvoke(new Action(() =>
							{
								LoadSubDirectories(affectedNode, listView);
								// 刷新ListView
								LoadListViewByFileSource(watcher.Path, listView, affectedNode);

								// 清除文件夹统计缓存，以便重新计算
								FolderStatistics.ClearCache();
							}));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"Watcher_Changed error: {ex.Message}");
			}
		}

		public FileEntry? GetListItemPath(ListViewItem item)
		{
			//if (item.Tag is ArchNodeTag archNode)
			//	return Path.Combine(archNode.Path, item.Text);
			//// 检查是否是FTP节点 // 对于FTP项，直接使用SubItems[1]中存储的完整路径 // 对于本地文件系统
			//if (uiManager.activeTreeview.SelectedNode.Tag is FtpNodeTag)
			//	//bugfix: 如果使用平铺模式，无法从树节点的路径获取到ITEM的真实完整路径，所以只能从ITEM.SUBITEM[1]中获取
			//	return item.SubItems[1].Text;
			var lvitemtag = item.Tag as LvItemTag;
			if (lvitemtag?.File != null)
				return lvitemtag.File;
			return null;
		}
		public void ToolbarStrip_Click(object sender, EventArgs e)
		{
			if (e is MouseEventArgs mouse_event && mouse_event.Button == MouseButtons.Right)
			{
				if (sender is ToolStrip toolStrip)
				{
					if (toolStrip == uiManager.toolbarManager.DynamicToolStrip)
					{
						uiManager.toolbarManager.EditToolbar();
					}
					else if (toolStrip == uiManager.vtoolbarManager.DynamicToolStrip)
					{
						uiManager.vtoolbarManager.EditToolbar();
					}
				}
			}
		}

		// 获取路径访问历史
		public List<string> GetPathHistory()
		{
			// 清理超过100条的旧记录
			if (pathAccessHistory.Count > MAX_HISTORY_COUNT)
			{
				var oldestPaths = pathAccessHistory
					.OrderBy(x => x.Value.lastAccess)
					.Take(pathAccessHistory.Count - MAX_HISTORY_COUNT)
					.Select(x => x.Key)
					.ToList();

				foreach (var path in oldestPaths)
				{
					pathAccessHistory.Remove(path);
				}
			}

			return pathAccessHistory.Keys.ToList();
		}

		// 可选：添加一个清理历史记录的方法
		public void ClearPathHistory()
		{
			pathAccessHistory.Clear();
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
		}   // 复制选中的文件
		public bool cm_copy(string? param = null, string? targetPath = null)
		{
			string? srcPath;
			FileEntry[] sourceFiles;
			ListView targetlist;

			if (!string.IsNullOrEmpty(param)) // if param exist, indicate that use clipboard to copy/move file, so the actpanel is targetpanel, otherwise is normal operation, the actpanel is srcpanel.
			{
				sourceFiles = GetFileListByViewOrParam(param).ToArray();
				srcPath = Path.GetDirectoryName(sourceFiles[0].FullPath) ?? "";
				targetlist = uiManager.activeListView;
			}
			else
			{
				var listView = activeListView;
				if (listView == null || listView.SelectedItems.Count <= 0) return false;

				// 获取文件列表
				List<FileEntry> fileList = [];
				foreach (ListViewItem item in listView.SelectedItems)
				{
					var fileEntry = GetListItemPath(item);
					if (fileEntry != null)
						fileList.Add(fileEntry);
				}
				sourceFiles = [.. fileList];
				srcPath = uiManager.srcDir;//todo: need add wcx virtual folder to shengfilesystemnode's child, 然后才能从srcdir获取到正确的srcpath

				// 如果没有指定目标路径，则使用非活动面板的路径作为目标
				if (string.IsNullOrEmpty(targetPath))
					targetPath = CurrentFullpath[unactiveTreeview.Name]; //Helper.getFSpath(unactiveTreeview.SelectedNode.FullPath);
				targetlist = uiManager.unactiveListView;
			}

			try
			{
				if (targetPath != null)
				{
					// 使用 FileSourceManager 获取源和目标 FileSource
					var sourceFileSource = CurrentFullpath.GetFileSource(LRflag);   //fullpath.getfilesource is faster , about 2ms
					var targetFileSource = _fileSourceManager.GetFileSourceForFullPath(targetPath, !isleft);    //if pastefromclipboard, the targetpath is not unactive, so calc it is necessary, slower, about 21ms, 10x times slower than the previous method

					// 创建文件条目列表
					var fileEntries = new FileEntries();
					foreach (var file in sourceFiles)
						fileEntries.Add(file);

					// 使用 FileSourceManager 创建适合的复制操作
					FileSourceOperation? operation = FileSourceManager.CreateCopyOperation(
						sourceFileSource,
						targetFileSource,
						fileEntries,
						targetPath);

					// 特殊情况：如果源和目标都是WcxArchiveFileSource，需要通过临时文件系统进行复制
					if (operation == null)
						return CopyViaTemporaryDirectory(sourceFileSource, targetFileSource, fileEntries, targetPath);

					_operationsManager.AddOperation(operation);
					operation._Thread.WaitFor();

					// 刷新目标面板
					RefreshPanel(targetlist);
					return true;
				}

				// 如果无法使用 FileSource 架构，使用传统方法
				// 确定源路径和目标路径的类型
				//bool isSourceArchive = !string.IsNullOrEmpty(srcPath) && IsArchiveFile(srcPath);//bugfix: the srcpath is the dir in which the arch file located, so always return false, it should use vfs to process the arch file as virtual dir
				//if (isSourceArchive)
				//	OpenArchive(srcPath, OpenMode.PK_OM_EXTRACT);
				//bool isTargetArchive = targetPath != null && IsArchiveFile(targetPath);
				//if (isTargetArchive && targetPath != null)
				//	OpenArchive(targetPath, OpenMode.PK_OM_EXTRACT);
				//bool isSourceFtp = fTPMGR.IsFtpPath(srcPath);
				//bool isTargetFtp = targetPath != null && fTPMGR.IsFtpPath(targetPath);

				//// 场景1: FTP -> FTP
				//if (isSourceFtp && isTargetFtp)
				//{
				//	var sourceFtp = fTPMGR.GetFtpSource(srcPath);
				//	var targetFtp = fTPMGR.GetFtpSource(targetPath);
				//	if (sourceFtp != null && targetFtp != null)
				//	{
				//		foreach (var remotePath in sourceFiles)
				//		{
				//			// 先下载到临时目录
				//			string tempFile = sourceFtp.DownloadFile(remotePath.FullPath);
				//			if (!string.IsNullOrEmpty(tempFile))
				//			{
				//				try
				//				{
				//					// 再上传到目标FTP
				//					string fileName = Path.GetFileName(remotePath.FullPath);
				//					string targetRemotePath = Path.Combine(targetPath, fileName).Replace("\\", "/");
				//					targetFtp.UploadFile(tempFile, targetRemotePath);
				//				}
				//				finally
				//				{
				//					// 清理临时文件
				//					if (File.Exists(tempFile))
				//						File.Delete(tempFile);
				//				}
				//			}
				//		}
				//	}
				//}
				//// 场景2: FTP -> LOCAL
				//else if (isSourceFtp && !isTargetFtp && !isTargetArchive)
				//{
				//	// 从FTP下载到本地
				//	var ftpSource = fTPMGR.GetFtpSource(srcPath);
				//	if (ftpSource != null)
				//	{
				//		foreach (var remotePath in sourceFiles)
				//		{
				//			string fileName = Path.GetFileName(remotePath.FullPath);
				//			string localPath = Path.Combine(targetPath, fileName);
				//			string tempFile = ftpSource.DownloadFile(remotePath.FullPath);
				//			if (!string.IsNullOrEmpty(tempFile))
				//			{
				//				try
				//				{
				//					File.Copy(tempFile, localPath, true);
				//				}
				//				finally
				//				{
				//					// 清理临时文件
				//					if (File.Exists(tempFile))
				//						File.Delete(tempFile);
				//				}
				//			}
				//		}
				//	}
				//}
				//// 场景3: FTP -> ARCHIVE
				//else if (isSourceFtp && !isTargetFtp && isTargetArchive)
				//{
				//	var ftpSource = fTPMGR.GetFtpSource(srcPath);
				//	if (ftpSource != null)
				//	{
				//		List<string> tempFiles = new List<string>();
				//		try
				//		{
				//			// 先将文件从FTP下载到临时目录
				//			foreach (var remotePath in sourceFiles)
				//			{
				//				string tempFile = ftpSource.DownloadFile(remotePath.FullPath);
				//				if (!string.IsNullOrEmpty(tempFile))
				//					tempFiles.Add(tempFile);
				//			}

				//			// 然后添加到压缩文件
				//			if (tempFiles.Count > 0)
				//				AddToArchive(targetPath, tempFiles.ToArray());
				//		}
				//		finally
				//		{
				//			// 清理临时文件
				//			foreach (var tempFile in tempFiles)
				//			{
				//				if (File.Exists(tempFile))
				//					File.Delete(tempFile);
				//			}
				//		}
				//	}
				//}
				//// 场景4: LOCAL -> FTP
				//else if (!isSourceFtp && !isSourceArchive && isTargetFtp)
				//{
				//	// 从本地上传到FTP
				//	var ftpTarget = fTPMGR.GetFtpSource(targetPath);
				//	if (ftpTarget != null)
				//	{
				//		foreach (var localFile in sourceFiles)
				//		{
				//			string fullSourcePath = Path.Combine(srcPath, localFile.FullPath);
				//			string fileName = Path.GetFileName(localFile.FullPath);
				//			string remotePath = Path.Combine(ftpTarget.CurrentPath, fileName).Replace("\\", "/");
				//			if (Directory.Exists(fullSourcePath))
				//				fTPMGR.UploadDirectory(ftpTarget.Client, fullSourcePath, remotePath);
				//			else
				//				ftpTarget.UploadFile(fullSourcePath, remotePath);
				//		}
				//	}
				//}
				//// 场景5: LOCAL -> LOCAL
				//else if (!isSourceFtp && !isSourceArchive && !isTargetFtp && !isTargetArchive)
				//{
				//	// 本地文件之间的复制
				//	string[] fullPaths = sourceFiles.Select(f => Path.Combine(srcPath, f.FullPath)).ToArray();
				//	FileSystemManager.CopyFilesAndDirectories(fullPaths, targetPath);
				//}
				//// 场景6: LOCAL -> ARCHIVE
				//else if (!isSourceFtp && !isSourceArchive && !isTargetFtp && isTargetArchive)
				//{
				//	string[] fullPaths = sourceFiles.Select(f => Path.Combine(srcPath, f.FullPath)).ToArray();
				//	AddToArchive(targetPath, fullPaths);
				//}
				//// 场景7: ARCHIVE -> FTP
				//else if (!isSourceFtp && isSourceArchive && isTargetFtp)
				//{
				//	var ftpTarget = fTPMGR.GetFtpSource(targetPath);
				//	if (ftpTarget != null)
				//	{
				//		foreach (var fileName in sourceFiles)
				//		{
				//			// 先解压到临时目录
				//			string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				//			Directory.CreateDirectory(tempDir);
				//			try
				//			{
				//				string tempFile = Path.Combine(tempDir, fileName.FullPath);
				//				if (ExtractArchiveFile(srcPath, fileName.FullPath, tempDir))
				//				{
				//					// 上传到FTP
				//					string remotePath = Path.Combine(targetPath, fileName.FullPath).Replace("\\", "/");
				//					ftpTarget.UploadFile(tempFile, remotePath);
				//				}
				//			}
				//			finally
				//			{
				//				// 清理临时目录
				//				if (Directory.Exists(tempDir))
				//					Directory.Delete(tempDir, true);
				//			}
				//		}
				//	}
				//}
				//// 场景8: ARCHIVE -> LOCAL
				//else if (!isSourceFtp && isSourceArchive && !isTargetFtp && !isTargetArchive)
				//{
				//	foreach (var fileName in sourceFiles)
				//		ExtractArchiveFile(srcPath, fileName.FullPath, targetPath);
				//}
				//// 场景9: ARCHIVE -> ARCHIVE
				//else if (!isSourceFtp && isSourceArchive && !isTargetFtp && isTargetArchive)
				//{
				//	string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				//	Directory.CreateDirectory(tempDir);
				//	try
				//	{
				//		// 先从源压缩文件解压
				//		foreach (var fileName in sourceFiles)
				//			ExtractArchiveFile(srcPath, fileName.FullPath, tempDir);

				//		// 再添加到目标压缩文件
				//		string[] tempFiles = Directory.GetFiles(tempDir);
				//		if (tempFiles.Length > 0)
				//			AddToArchive(targetPath, tempFiles);
				//	}
				//	finally
				//	{
				//		// 清理临时目录
				//		if (Directory.Exists(tempDir))
				//			Directory.Delete(tempDir, true);
				//	}
				//}

				//RefreshPanel(targetlist);
				//return true;
				return false;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制文件失败: {ex.Message}", "错误");
				return false;
			}
		}
		// 移动选中的文件
		public void cm_renmov(string? param = null, string? targetPath = null)
		{
			string srcpath;
			var sourceFiles = GetFileListByViewOrParam(param);
			if (sourceFiles.Count == 0) return;

			if (!string.IsNullOrEmpty(param)) // when use clipboard, the targetpath is actpanel dir, so use srcdir, and the srcpath is determined by the filenames in the clipboard, so use the first sourcefile dir, TODO: the sourcefiles with many directories
			{
				srcpath = Path.GetDirectoryName(sourceFiles[0].FullPath) ?? "";
				targetPath = uiManager.srcDir;
			}
			else
			{
				srcpath = uiManager.srcDir;
				targetPath = uiManager.targetDir;
			}

			if (string.IsNullOrEmpty(targetPath))
			{
				MessageBox.Show("无效的目标路径", "错误");
				return;
			}

			if (srcpath.Equals(targetPath)) return;     //if srcpath eq targetpath, do not need move, do rename

			try
			{
				// 使用 FileSourceManager 获取源和目标 FileSource
				IFileSource sourceFileSource = _fileSourceManager.GetFileSourceForFullPath(srcpath, isleft);
				IFileSource targetFileSource = _fileSourceManager.GetFileSourceForFullPath(targetPath, !isleft);

				// 创建文件条目列表
				var fileEntries = new FileEntries();
				foreach (var fileEntry in sourceFiles)
					fileEntries.Add(fileEntry);

				// 创建移动操作
				FileSourceOperation? operation;

				// 如果源和目标是同一个 FileSource，使用 CreateMoveOperation
				if (sourceFileSource.GetType() == targetFileSource.GetType())
				{
					operation = sourceFileSource.CreateMoveOperation(fileEntries, targetPath);
					_operationsManager.AddOperation(operation);
					operation._Thread.WaitFor();    //waiting for operation to finish
													// 刷新面板
					RefreshPanel(activeListView);
					RefreshPanel(unactiveListView);
					return;
				}
				else
				{
					// 如果不是同一类型的 FileSource，先复制后删除
					// 使用 FileSourceManager 创建复制操作
					var copyOperation = FileSourceManager.CreateCopyOperation(sourceFileSource, targetFileSource, fileEntries, targetPath);
					if (copyOperation != null)
					{
						_operationsManager.AddOperation(copyOperation);
						copyOperation._Thread.WaitFor();

						// 复制成功后删除源文件
						operation = sourceFileSource.CreateDeleteOperation(fileEntries);
					}
					else
					{
						operation = null;
					}

					if (operation != null)
					{
						_operationsManager.AddOperation(operation);
						operation._Thread.WaitFor();

						// 刷新面板
						RefreshPanel(activeListView);
						RefreshPanel(unactiveListView);
						return;
					}
				}

				// 如果无法使用 FileSource 架构，使用传统方法
				// 检查源路径和目标路径是否为FTP路径
				//bool isSourceFtp = fTPMGR.IsFtpPath(srcpath);
				//bool isTargetFtp = fTPMGR.IsFtpPath(targetPath);

				//if (isSourceFtp || isTargetFtp)
				//{
				//	// 如果涉及FTP，先复制后删除
				//	if (cm_copy(param, targetPath))
				//	{
				//		// 如果源是FTP，使用FTP删除
				//		if (isSourceFtp)
				//		{
				//			var ftpSource = fTPMGR.GetFtpSource(srcpath);
				//			if (ftpSource != null)
				//				foreach (var remotePath in sourceFiles)
				//					ftpSource.DeleteFile(remotePath.FullPath);
				//		}
				//		else
				//			cm_delete(param, false); // 源是本地文件，使用本地删除
				//	}
				//}
				//else if (cm_copy(param, targetPath)) // 本地文件之间的移动
				//	cm_delete(param, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"移动文件失败: {ex.Message}", "错误");
			}
		}

		// 删除选中的文件
		public void cm_delete(string? param = null, bool needConfirm = true)
		{
			if (FocusedTree != null)
			{
				//if the focus is on the tree instead of listviewitem, then directly remove the dir of treenode if possible
				if (FocusedTree.SelectedNode != null)
				{
					if (FocusedTree.SelectedNode.Tag is FtpNodeTag ftpnode)
					{
						var ftpSource = fTPMGR.GetFtpFileSourceByConnectionName(ftpnode.ConnectionName);
						if (ftpSource != null)
						{
							ftpSource.DeleteDirectory(ftpnode.Path);
							RefreshPanel(FocusedTree);
						}
					}
					else
					{
						var dir = Helper.getFSpath(FocusedTree.SelectedNode.FullPath);
						if (Directory.Exists(dir))
						{
							Directory.Delete(dir, true);
							FocusedTree.SelectedNode = FocusedTree.SelectedNode.Parent;//bugfix: if delete a node from tree, will lead to refreshpanel exception occurs, so change the selected node to its parent
							RefreshPanel(FocusedTree);
						}
					}
				}
				return;
			}

			var files = GetFileListByViewOrParam(param, false);
			if (files.Count == 0) return;

			var currentPath = CurrentFullpath[LRflag];
			var result = DialogResult.Yes;
			if (needConfirm)
			{
				result = MessageBox.Show(
					$"确定要删除选中的 {files.Count} 个文件吗？",
					"确认删除",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);
			}

			if (result == DialogResult.Yes)
			{
				try
				{
					// 使用 FileSourceManager 获取合适的 FileSource
					var path = CurrentFullpath[LRflag];
					IFileSource fileSource = _fileSourceManager.GetFileSourceForFullPath(path, isleft);

					// 创建文件条目列表
					var fileEntries = new FileEntries();
					foreach (var fileEntry in files)
						fileEntries.Add(fileEntry);

					// 创建删除操作
					var operation = fileSource.CreateDeleteOperation(fileEntries);
					if (operation != null)
					{
						_operationsManager.AddOperation(operation);
						operation._Thread.WaitFor();

						// 刷新面板
						RefreshPanel(activeListView);
						if (!string.IsNullOrEmpty(param))
							RefreshPanel(unactiveListView);
						return;
					}

					// 如果无法使用 FileSource 架构，使用传统方法
					//if (IsArchiveFile(CurrentFullpath[LRflag]))
					//{
					//	if (DeleteFromArchive(CurrentFullpath[LRflag], files.Select(x => x.FullPath).ToArray()))
					//	{
					//		var items = LoadArchiveContents(CurrentFullpath[LRflag]);
					//		activeListView.Items.Clear();
					//		activeListView.Items.AddRange(items.ToArray());
					//	}
					//	return;
					//}

					//// 检查是否为FTP路径
					//if (IsActiveFtpPanel(out var ftpnode))
					//{
					//	var ftpSource = fTPMGR.GetFtpFileSourceByConnectionName(ftpnode.ConnectionName);
					//	if (ftpSource != null)
					//	{
					//		foreach (var remotePath in files)
					//			ftpSource.DeleteFile(remotePath.FullPath);
					//	}
					//}
					//else
					//{
					//	// 本地文件删除
					//	foreach (var file in files)
					//		FileSystemManager.DeleteFile(file.FullPath);
					//}

					//RefreshPanel(activeListView);
					//if (!string.IsNullOrEmpty(param))
					//	RefreshPanel(unactiveListView);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"删除文件失败: {ex.Message}", "错误");
				}
			}
		}

		// 重命名选中的文件或文件夹
		public void cm_renameonly()
		{
			var listView = activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;
			var selectedItem = listView.SelectedItems[0];
			// 启用编辑模式
			selectedItem.BeginEdit();
		}

		/// <summary>
		/// 通过临时文件系统复制文件，用于在两个压缩文件之间复制文件
		/// </summary>
		/// <param name="sourceFileSource">源文件源</param>
		/// <param name="targetFileSource">目标文件源</param>
		/// <param name="sourceFiles">要复制的文件</param>
		/// <param name="targetPath">目标路径</param>
		/// <returns>操作是否成功</returns>
		private bool CopyViaTemporaryDirectory(
			IFileSource sourceFileSource,
			IFileSource targetFileSource,
			FileEntries sourceFiles,
			string targetPath)
		{
			bool result = false;

			try
			{
				// 创建临时文件系统
				ITempFileSystemFileSource tempFileSource = new TempFileSystemFileSource();
				string tempPath = tempFileSource.FileSystemRoot;

				// 第一步：从源压缩文件复制到临时文件系统
				var copyOutOperation = sourceFileSource.CreateCopyOutOperation(
					tempFileSource,
					sourceFiles,
					tempPath);

				if (copyOutOperation != null)
				{
					// 添加操作到管理器并执行
					_operationsManager.AddOperation(copyOutOperation);
					copyOutOperation._Thread.WaitFor();

					// 检查操作是否成功完成
					if (copyOutOperation.Result == FileSourceOperationResult.Finished)
					{
						// 创建临时文件系统中的文件列表
						var tempFiles = new FileEntries(tempPath);

						// 获取临时目录中的所有文件
						foreach (var file in sourceFiles)
						{
							string tempFilePath = Path.Combine(tempPath, file.Name);
							if (File.Exists(tempFilePath) || Directory.Exists(tempFilePath))
							{
								var tempFile = FileSystemFileSource.CreateFileFromFile(tempFilePath);
								tempFiles.Add(tempFile);
							}
						}

						// 第二步：从临时文件系统复制到目标压缩文件
						var copyInOperation = targetFileSource.CreateCopyInOperation(tempFileSource, tempFiles, targetPath);

						if (copyInOperation != null)
						{
							// 添加操作到管理器并执行
							_operationsManager.AddOperation(copyInOperation);
							copyInOperation._Thread.WaitFor();

							// 操作成功
							result = (copyInOperation.Result == FileSourceOperationResult.Finished);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制文件失败: {ex.Message}", "错误");
			}

			// 刷新目标面板
			RefreshPanel(activeListView);
			RefreshPanel(unactiveListView);

			return result;
		}

		//private void HandleRegistryContextMenuItems(string path)
		//{
		//	string[] registryPaths = new[]
		//	{
		//		@"*\shellex\ContextMenuHandlers",
		//		@"Directory\shell\",
		//		@"Directory\shellex\ContextMenuHandlers",
		//		@"Folder\shell",
		//		@"Folder\shellex\ContextMenuHandlers"
		//	};

		//	foreach (string registryPath in registryPaths)
		//	{
		//		Guid? guid = ContextMenuHandler.GetContextMenuHandlerGuid(registryPath);
		//		if (guid.HasValue)
		//		{
		//			object? comObject = ContextMenuHandler.CreateComObject(guid.Value);
		//			if (comObject != null)
		//				ContextMenuHandler.InvokeComMethod(comObject, "InvokeCommand", path);
		//		}
		//	}
		//}

		public void myShellExe(string pathWithArgs = "c:\\windows\\system32")
		{
			pathWithArgs = se.PrepareParameter(pathWithArgs, [uiManager.srcfiles], Path.GetDirectoryName(pathWithArgs))[0];
			// 获取运行参数
			string cmd;
			string arg;
			(cmd, arg) = Helper.SplitCommand(pathWithArgs);
			// 获取可执行文件路径
			string executablePath = Path.GetFullPath(Regex.Match(cmd, @"^.*?\.exe").Value);
			API.ShellExecute(IntPtr.Zero, "open", executablePath, arg.Replace("\"", ""), "", (int)SW.SHOWNORMAL);
			//cmdProcessor.cm_executedos1(executablePath + " -p " + arg.Replace("\"", ""));
			//var p = new ProcessStartInfo(executablePath) {
			//	Arguments = "-p " + arg.Replace("\"", ""),
			//	UseShellExecute = false
			//};
			//Process.Start(p);
			//Window wnd = Window.GetWindow(this); //获取当前窗口
			//var wih = new WindowInteropHelper(wnd); //该类支持获取hWnd
			//IntPtr hWnd = wih.Handle;    //获取窗口句柄
			//var result = ShellExecute(hWnd, "open", "需要打开的路径如C:\\Users\\Desktop\\xx.exe", null, null, (int)ShowWindowCommands.SW_SHOW);
		}

		public void cm_options()
		{
			var optionsForm = new OptionsForm(this, "");
			optionsForm.ShowDialog();
		}

		public void cm_testviewmgr()
		{
			var testForm = new TestViewMgrForm(this);
			testForm.Show();
		}
		public bool IsActiveFtpPanel(out FtpNodeTag? ftpnode, TreeView? treeview = null)
		{
			ftpnode = null;
			if ((treeview ?? activeTreeview).SelectedNode.Tag is FtpNodeTag _ftpnode)
			{
				ftpnode = _ftpnode;
				return true;
			}
			return false;
		}
		public bool IsFtpPanel(out FtpNodeTag? ftpnode, string leftright)
		{
			ftpnode = null;
			if (GetTreeViewByName(leftright).SelectedNode.Tag is FtpNodeTag _ftpnode)
			{
				ftpnode = _ftpnode;
				return true;
			}
			return false;
		}
		public static void ClearMemory()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
		}
		[DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
		public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
	}
}