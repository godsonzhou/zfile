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

namespace WinFormsApp1
{
	public static class constant_value
	{
		public const string zfilePath = "D:\\gitrepos\\Files\\config\\";
	}

	public partial class Form1 : Form
    {
        private bool isSelecting = false;
        private Point selectionStart;
        private Rectangle selectionRectangle;
        private ListView activeListView;

        private readonly FileSystemWatcher watcher = new();
        private string currentDirectory = @"C:\";

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

			// 将ImageList分配给TreeView
			leftTree.ImageList = treeViewImageList;
			rightTree.ImageList = treeViewImageList;
		}
		private void InitializeContextMenu()
        {
            // 初始化ContextMenuStrip
            contextMenuStrip.Opening += ContextMenuStrip_Opening;
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

            // 确保菜单和工具栏在最上层
            //menuStrip2.BringToFront();
            //toolStrip1.BringToFront();
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
            ConfigureDriveBox(leftDriveBox, leftDrivePanel);
            ConfigureDriveBox(rightDriveBox, rightDrivePanel);

            LoadDrives();
        }

        private void ConfigureDriveBox(ComboBox driveBox, Panel parent)
        {
            driveBox.Dock = DockStyle.Fill;
            driveBox.DropDownStyle = ComboBoxStyle.DropDownList;
            driveBox.SelectedIndexChanged += DriveComboBox_SelectedIndexChanged;
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
            // 配置左侧树列表分割容器
            ConfigureTreeListSplitter(leftTreeListSplitter, leftUpperPanel, leftTree, leftList);
            // 配置右侧树列表分割容器
            ConfigureTreeListSplitter(rightTreeListSplitter, rightUpperPanel, rightTree, rightList);
        }

        private void ConfigureTreeView(TreeView treeView)
        {
            treeView.Dock = DockStyle.Fill;
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseClick += TreeView_NodeMouseClick;

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
                    ShowContextMenu(treeView, node.Tag.ToString(), e.Location);
                }
            }
        }


        private void ShowContextMenu(Control control, string path, Point location)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                IntPtr menu = IntPtr.Zero;
                try
                {
                    menu = CreateContextMenu(path);
                    if (menu != IntPtr.Zero)
                    {
                        contextMenuStrip.Items.Clear();
                        int count = GetMenuItemCount(menu);
                        for (int i = 0; i < count; i++)
                        {
                            MENUITEMINFO mii = new();
                            mii.cbSize = (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
                            mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_STRING | MIIM.MIIM_SUBMENU;
                            mii.dwTypeData = new string('\0', 256);
                            mii.cch = (uint)mii.dwTypeData.Length;

                            if (GetMenuItemInfo(menu, (uint)i, true, ref mii))
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
                        DestroyMenu(menu);
                }
            }
        }

        private void AddSubMenuItems(ToolStripMenuItem parentItem, IntPtr hSubMenu)
        {
            int subMenuCount = GetMenuItemCount(hSubMenu);
            for (int j = 0; j < subMenuCount; j++)
            {
                MENUITEMINFO subMii = new();
                subMii.cbSize = (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
                subMii.fMask = MIIM.MIIM_ID | MIIM.MIIM_STRING | MIIM.MIIM_SUBMENU;
                subMii.dwTypeData = new string('\0', 256);
                subMii.cch = (uint)subMii.dwTypeData.Length;

                if (GetMenuItemInfo(hSubMenu, (uint)j, true, ref subMii))
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
            IntPtr menu = CreatePopupMenu();
            if (menu != IntPtr.Zero)
            {
                IntPtr pidl = ILCreateFromPath(path);
                if (pidl != IntPtr.Zero)
                {
                    IntPtr parentPidl = ILClone(pidl);
                    ILRemoveLastID(parentPidl);
                    IShellFolder desktopFolder;
                    SHGetDesktopFolder(out desktopFolder);
                    IShellFolder parentFolder;
                    Guid iidShellFolder = IID_IShellFolder;
                    desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref iidShellFolder, out parentFolder);
                    IntPtr[] pidls = new IntPtr[] { ILFindLastID(pidl) };
                    IContextMenu contextMenu;
                    Guid iidContextMenu = IID_IContextMenu;
                    parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref iidContextMenu, IntPtr.Zero, out contextMenu);
                    contextMenu.QueryContextMenu(menu, 0, 1, 0x7FFF, CMF.CMF_NORMAL);
                    Marshal.ReleaseComObject(contextMenu);
                    Marshal.ReleaseComObject(parentFolder);
                    Marshal.ReleaseComObject(desktopFolder);
                    ILFree(pidl);
                    ILFree(parentPidl);
                }
            }
            return menu;
        }

        private void InvokeCommand(string path, uint id)
        {
            IntPtr pidl = ILCreateFromPath(path);
            if (pidl != IntPtr.Zero)
            {
                IntPtr parentPidl = ILClone(pidl);
                ILRemoveLastID(parentPidl);
                IShellFolder desktopFolder;
                SHGetDesktopFolder(out desktopFolder);
                IShellFolder parentFolder;
                Guid iid_IShellFolder = IID_IShellFolder;
                desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref iid_IShellFolder, out parentFolder);
                IntPtr[] pidls = new IntPtr[] { ILFindLastID(pidl) };
                IContextMenu contextMenu;
                Guid iid_IContextMenu = IID_IContextMenu;
                parentFolder.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, pidls, ref iid_IContextMenu, IntPtr.Zero, out contextMenu);
                CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
                invoke.lpVerb = (IntPtr)(id - 1);
				invoke.lpDirectory = string.Empty;
                invoke.nShow = SW_SHOWNORMAL;
				invoke.fMask = 0;	// CMIC.CMIC_MASK_UNICODE; // Ensure the fMask is set correctly
				invoke.ptInvoke = new POINT(MousePosition.X, MousePosition.Y);
				contextMenu.InvokeCommand(ref invoke);
                Marshal.ReleaseComObject(contextMenu);
                Marshal.ReleaseComObject(parentFolder);
                Marshal.ReleaseComObject(desktopFolder);
                ILFree(pidl);
                ILFree(parentPidl);
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
				ShellExecute(IntPtr.Zero, "open", path, "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"无法执行命令: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public void myShellExe()
        {
            ShellExecute(IntPtr.Zero, "open", "cmd.exe", "", "", (int)ShowWindowCommands.SW_SHOWNORMAL);
            //Window wnd = Window.GetWindow(this); //获取当前窗口
            //var wih = new WindowInteropHelper(wnd); //该类支持获取hWnd
            //IntPtr hWnd = wih.Handle;    //获取窗口句柄
            //var result = ShellExecute(hWnd, "open", "需要打开的路径如C:\\Users\\Desktop\\xx.exe", null, null, (int)ShowWindowCommands.SW_SHOW);
        }
        [DllImport("shell32.dll")]
        public static extern IntPtr ShellExecute(IntPtr hwnd, //窗口句柄
             string lpOperation, //指定要进行的操作
             string lpFile,  //要执行的程序、要浏览的文件夹或者网址
             string lpParameters, //若lpFile参数是一个可执行程序，则此参数指定命令行参数
             string lpDirectory, //指定默认目录
             int nShowCmd   //若lpFile参数是一个可执行程序，则此参数指定程序窗口的初始显示方式(参考如下枚举)
         );
        [DllImport("kernel32.dll")]
        public static extern int WinExec(string programPath, int operType);
        public enum ShowWindowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,  //显示一个窗口，同时令其进入活动状态
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_MAX = 10
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition, ref MENUITEMINFO lpmii);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ILCreateFromPath(string pszPath);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr ILClone(IntPtr pidl);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void ILRemoveLastID(IntPtr pidl);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr ILFindLastID(IntPtr pidl);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MENUITEMINFO
        {
            public uint cbSize;
            public MIIM fMask;
            public uint fType;
            public uint fState;
            public uint wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public string dwTypeData;
            public uint cch;
            public IntPtr hbmpItem;
        }

        [Flags]
        private enum MIIM : uint
        {
            MIIM_STATE = 0x00000001,
            MIIM_ID = 0x00000002,
            MIIM_SUBMENU = 0x00000004,
            MIIM_CHECKMARKS = 0x00000008,
            MIIM_TYPE = 0x00000010,
            MIIM_DATA = 0x00000020,
            MIIM_STRING = 0x00000040,
            MIIM_BITMAP = 0x00000080,
            MIIM_FTYPE = 0x00000100
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CMINVOKECOMMANDINFOEX
        {
            public int cbSize;
            public CMIC fMask;
            public IntPtr hwnd;
            public IntPtr lpVerb;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public int dwHotKey;
            public IntPtr hIcon;
            public string lpTitle;
            public IntPtr lpVerbW;
            public string lpParametersW;
            public string lpDirectoryW;
            public string lpTitleW;
            public POINT ptInvoke;
        }

        [Flags]
        private enum CMIC : uint
        {
            CMIC_MASK_ICON = 0x00000010,
            CMIC_MASK_HOTKEY = 0x00000020,
            CMIC_MASK_NOASYNC = 0x00000100,
            CMIC_MASK_FLAG_NO_UI = 0x00000400,
            CMIC_MASK_UNICODE = 0x00004000,
            CMIC_MASK_NO_CONSOLE = 0x00008000,
            CMIC_MASK_ASYNCOK = 0x00100000,
            CMIC_MASK_NOZONECHECKS = 0x00800000,
            CMIC_MASK_SHIFT_DOWN = 0x10000000,
            CMIC_MASK_CONTROL_DOWN = 0x40000000,
            CMIC_MASK_FLAG_LOG_USAGE = 0x04000000,
            CMIC_MASK_PTINVOKE = 0x20000000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
			public POINT(int x, int y)
			{
				this.x = x;
				this.y = y;
			}

		}

        [Flags]
        private enum CMF : uint
        {
            CMF_NORMAL = 0x00000000,
            CMF_DEFAULTONLY = 0x00000001,
            CMF_VERBSONLY = 0x00000002,
            CMF_EXPLORE = 0x00000004,
            CMF_NOVERBS = 0x00000008,
            CMF_CANRENAME = 0x00000010,
            CMF_NODEFAULT = 0x00000020,
            CMF_INCLUDESTATIC = 0x00000040,
            CMF_ITEMMENU = 0x00000080,
            CMF_EXTENDEDVERBS = 0x00000100,
            CMF_DISABLEDVERBS = 0x00000200,
            CMF_ASYNCVERBSTATE = 0x00000400,
            CMF_OPTIMIZEFORINVOKE = 0x00000800,
            CMF_SYNCCASCADEMENU = 0x00001000,
            CMF_DONOTPICKDEFAULT = 0x00002000,
            CMF_RESERVED = 0xffff0000
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E6-0000-0000-C000-000000000046")]
        private interface IShellFolder
        {
            void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
            void EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList);
            void BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IShellFolder ppv);
            void BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
            [PreserveSig]
            int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
            void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref SFGAO rgfInOut);
            void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref Guid riid, IntPtr rgfReserved, out IContextMenu ppv);
            void GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out STRRET pName);
            void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, SHCONTF uFlags, out IntPtr ppidlOut);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E4-0000-0000-C000-000000000046")]
        private interface IContextMenu
        {
            [PreserveSig]
            int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, CMF uFlags);
            void InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
            void GetCommandString(uint idCmd, GCS uType, IntPtr pReserved, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName, uint cchMax);
        }

        [Flags]
        private enum SHCONTF
        {
            SHCONTF_CHECKING_FOR_CHILDREN = 0x00010,
            SHCONTF_FOLDERS = 0x00020,
            SHCONTF_NONFOLDERS = 0x00040,
            SHCONTF_INCLUDEHIDDEN = 0x00080,
            SHCONTF_INIT_ON_FIRST_NEXT = 0x00100,
            SHCONTF_NETPRINTERSRCH = 0x00200,
            SHCONTF_SHAREABLE = 0x00400,
            SHCONTF_STORAGE = 0x00800,
            SHCONTF_NAVIGATION_ENUM = 0x01000,
            SHCONTF_FASTITEMS = 0x02000,
            SHCONTF_FLATLIST = 0x04000,
            SHCONTF_ENABLE_ASYNC = 0x08000,
            SHCONTF_INCLUDESUPERHIDDEN = 0x10000
        }

        [Flags]
        private enum SFGAO : uint
        {
            SFGAO_CANCOPY = 0x1,
            SFGAO_CANMOVE = 0x2,
            SFGAO_CANLINK = 0x4,
            SFGAO_STORAGE = 0x00000008,
            SFGAO_CANRENAME = 0x00000010,
            SFGAO_CANDELETE = 0x00000020,
            SFGAO_HASPROPSHEET = 0x00000040,
            SFGAO_DROPTARGET = 0x00000100,
            SFGAO_CAPABILITYMASK = 0x00000177,
            SFGAO_ENCRYPTED = 0x00002000,
            SFGAO_ISSLOW = 0x00004000,
            SFGAO_GHOSTED = 0x00008000,
            SFGAO_LINK = 0x00010000,
            SFGAO_SHARE = 0x00020000,
            SFGAO_READONLY = 0x00040000,
            SFGAO_HIDDEN = 0x00080000,
            SFGAO_DISPLAYATTRMASK = 0x000FC000,
            SFGAO_FILESYSANCESTOR = 0x10000000,
            SFGAO_FOLDER = 0x20000000,
            SFGAO_FILESYSTEM = 0x40000000,
            SFGAO_HASSUBFOLDER = 0x80000000,
            SFGAO_CONTENTSMASK = 0x80000000,
            SFGAO_VALIDATE = 0x01000000,
            SFGAO_REMOVABLE = 0x02000000,
            SFGAO_COMPRESSED = 0x04000000,
            SFGAO_BROWSABLE = 0x08000000,
            SFGAO_NONENUMERATED = 0x00100000,
            SFGAO_NEWCONTENT = 0x00200000,
            SFGAO_CANMONIKER = 0x00400000,
            SFGAO_HASSTORAGE = 0x00400000,
            SFGAO_STREAM = 0x00400000,
            SFGAO_STORAGEANCESTOR = 0x00800000,
            SFGAO_STORAGECAPMASK = 0x70C50008,
            SFGAO_PKEYSFGAOMASK = 0x81044000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct STRRET
        {
            public uint uType;
            public IntPtr pOleStr;
        }

        [Flags]
        private enum SHGDN : uint
        {
            SHGDN_NORMAL = 0x0000,
            SHGDN_INFOLDER = 0x0001,
            SHGDN_FOREDITING = 0x1000,
            SHGDN_FORADDRESSBAR = 0x4000,
            SHGDN_FORPARSING = 0x8000
        }

        [Flags]
        private enum GCS : uint
        {
            GCS_VERBA = 0x00000000,
            GCS_HELPTEXTA = 0x00000001,
            GCS_VALIDATEA = 0x00000002,
            GCS_VERBW = 0x00000004,
            GCS_HELPTEXTW = 0x00000005,
            GCS_VALIDATEW = 0x00000006,
            GCS_UNICODE = 0x00000004
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F2-0000-0000-C000-000000000046")]
        private interface IEnumIDList
        {
            [PreserveSig]
            int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
            void Skip(uint celt);
            void Reset();
            void Clone(out IEnumIDList ppenum);
        }

        private static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
        private static readonly Guid IID_IContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");
        private const int SW_SHOWNORMAL = 1;

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 在这里可以添加自定义的菜单项
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
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
                string path = e.Node.Tag.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    // 加载子目录
                    LoadSubDirectories(e.Node);

                    // 更新ListView显示
                    if (sender is TreeView treeView)
                    {
                        var listView = treeView == leftTree ? leftList : rightList;
                        LoadListView(path, listView);
                        currentDirectory = path;
                        selectedNode = e.Node;

                        // 更新监视器
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }

                    // 展开节点
                    e.Node.Expand();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录失败: {ex.Message}", "错误");
            }
        }

        private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;

            try
            {
                string path = e.Node.Tag.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
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
                        LoadListView(path, listView);
                        currentDirectory = path;
                        selectedNode = e.Node;

                        // 更新监视器
                        watcher.Path = path;
                        watcher.EnableRaisingEvents = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录失败: {ex.Message}", "错误");
            }
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
            if (parentNode?.Tag == null) return;

            try
            {
                string path = parentNode.Tag.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    parentNode.Nodes.Clear();
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        try
                        {
                            DirectoryInfo dirInfo = new(dir);
                            if ((dirInfo.Attributes & FileAttributes.Hidden) == 0)
                            {
                                TreeNode node = new(dirInfo.Name)
                                {
                                    Tag = dir,
									ImageKey = "folder", // 设置图标
									SelectedImageKey = "folder" // 设置选中图标
								};
                                parentNode.Nodes.Add(node);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载子目录失败: {ex.Message}", "错误");
            }
        }

        private void LoadDriveIntoTree(TreeView treeView, string drivePath)
        {
            try
            {
                treeView.BeginUpdate();
                treeView.Nodes.Clear();

                TreeNode rootNode = new(drivePath)
                {
                    Tag = drivePath,
					ImageKey = "folder", // 设置图标
					SelectedImageKey = "folder" // 设置选中图标
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
            // 不再使用单独的容器Panel
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.MultiSelect = true;
            listView.Sorting = SortOrder.Ascending;

            // 配置列
            listView.Columns.Clear();
            listView.Columns.Add("名称", 200);
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
                return;
            }
            if (e.Button == MouseButtons.Right)
            {
                ListView listView = sender as ListView;
                ListViewItem item = listView.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    listView.FocusedItem = item;
                    string itemPath = Path.Combine(currentDirectory, item.Text);
                    ShowContextMenu(listView, itemPath, e.Location);
                }
                else
                {
                    // Show context menu for the ListView itself
                    ShowContextMenu(listView, currentDirectory, e.Location);
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
            if (sender is not ListView listView || listView.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            string itemPath = Path.Combine(currentDirectory, selectedItem.Text);

            if (selectedItem.SubItems[1].Text == "<DIR>") // 确认是文件夹
            {
                if (Directory.Exists(itemPath))
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
                        }
                        else
                        {
                            // 如果在树中找不到节点，直接更新ListView
                            LoadListView(itemPath, listView);
                            currentDirectory = itemPath;
                        }

                        // 更新监视器
                        watcher.Path = itemPath;
                        watcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"访问文件夹失败: {ex.Message}", "错误");
                    }
                }
            }
            else // 处理文件
            {
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

        private TreeNode? FindTreeNode(TreeNodeCollection nodes, string fullPath)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag?.ToString() == fullPath)
                {
                    return node;
                }

                // 如果当前节点的路径是目标路径的父路径，则展开并递归搜索
                var nodeTag = node.Tag?.ToString();
                if (nodeTag != null && fullPath.StartsWith(nodeTag))
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
            LoadListView(watcher.Path, listView);
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
                LoadListView(drivePath, listView);
            }
        }

        // 加载文件列表
        private void LoadListView(string path, ListView listView)
        {
            if (string.IsNullOrEmpty(path)) return;

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

        // 优化获取目录内容的方法
        private List<FileSystemInfo> GetDirectoryContents(string path)
        {
            var result = new List<FileSystemInfo>();
            var dirInfo = new DirectoryInfo(path);

            try
            {
                // 并行处理目录和文件
                var directories = dirInfo.GetDirectories()
                    .Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
                var files = dirInfo.GetFiles()
                    .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

                result.AddRange(directories);
                result.AddRange(files);
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略访问受限的目录
            }

            return result;
        }

        // 优化ListViewItem创建
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
                        FormatFileSize(fileInfo.Length),
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

        private async void ListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ListView listView) return;
            var previewPanel = listView == leftList ? leftPreview : rightPreview;

            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView.SelectedItems[0];
                string filePath = Path.Combine(currentDirectory, selectedItem.Text);
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
                    case 0: // 名称列
                        result = string.Compare(item1.SubItems[column].Text,
                                             item2.SubItems[column].Text);
                        break;

                    case 1: // 大小列
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

                    case 3: // 日期列
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
                if (selectedItem.SubItems[1].Text == "<DIR>")
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
                if (selectedItem.SubItems[1].Text == "<DIR>")
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
            LoadListView(path, listView);
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
                    if (selectedItem.SubItems[1].Text == "<DIR>")
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
            if (string.IsNullOrEmpty(commanderPath))
            {
                //MessageBox.Show("未设置COMMANDER_PATH环境变量", "warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                var aa = Directory.GetCurrentDirectory();
                //var bb = Environment.CurrentDirectory;
                //var cc = AppDomain.CurrentDomain.BaseDirectory;
                //var dd = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                return aa;

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
            phiconLarge.ToList().ForEach(x => DestroyIcon(x));
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
                    if (phiconLarge[0] != IntPtr.Zero) DestroyIcon(phiconLarge[0]);
                    if (phiconSmall[0] != IntPtr.Zero) DestroyIcon(phiconSmall[0]);
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
            if (phiconLarge[0] != IntPtr.Zero) DestroyIcon(phiconLarge[0]);
            if (phiconSmall[0] != IntPtr.Zero) DestroyIcon(phiconSmall[0]);
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
                if (phiconLarge[0] != IntPtr.Zero) DestroyIcon(phiconLarge[0]);
                if (phiconSmall[0] != IntPtr.Zero) DestroyIcon(phiconSmall[0]);
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