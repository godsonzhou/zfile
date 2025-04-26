using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Shell32;
using WinShell;
namespace zfile
{
    public partial class TestVfsForm : Form
    {
        private IFileSource? _currentFileSource;
        private string? _currentPath;
        private FileEntries? _currentFiles;
        private readonly OperationsManager _operationsManager;

        public TestVfsForm()
        {
            InitializeComponent();
            _operationsManager = new OperationsManager();
            _currentPath = string.Empty;
            _currentFiles = new FileEntries();
            InitializeTreeView();
        }


        private void TestVfsForm_Load(object sender, EventArgs e)
        {
            // Initialize file sources
            InitializeFileSources();
        }

        private void InitializeTreeView()
        {
            // Add root nodes
            var fileSystemNode = treeViewNavigation.Nodes.Add("文件系统", "文件系统", 0, 0);
            var recycleBinNode = treeViewNavigation.Nodes.Add("回收站", "回收站", 1, 1);
            treeViewNavigation.Nodes.Add("压缩文件", "压缩文件", 2, 2);
            var controlPanelNode = treeViewNavigation.Nodes.Add("控制面板", "控制面板", 6, 6);

            // Add drives to file system node
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string driveName = string.IsNullOrEmpty(drive.VolumeLabel) ?
                        drive.Name : $"{drive.VolumeLabel} ({drive.Name})";

                    TreeNode driveNode = fileSystemNode.Nodes.Add(drive.Name, driveName, 3, 3);
                    driveNode.Tag = new NodeTag { Path = drive.Name, FileSourceType = FileSourceType.FileSystem };
                }
            }

            // Set tag for recycle bin node
            recycleBinNode.Tag = new NodeTag { Path = string.Empty, FileSourceType = FileSourceType.RecycleBin };

            // Set tag for control panel node
            controlPanelNode.Tag = new NodeTag { Path = string.Empty, FileSourceType = FileSourceType.ControlPanel };

            // 为控制面板节点添加一个占位子节点，以便显示展开图标
            controlPanelNode.Nodes.Add("...");

            // 注册TreeView的BeforeExpand事件
            treeViewNavigation.BeforeExpand += TreeViewNavigation_BeforeExpand;

            // Expand file system node
            fileSystemNode.Expand();
        }

        private void InitializeFileSources()
        {
            // Select the first drive node by default
            if (treeViewNavigation.Nodes[0].Nodes.Count > 0)
            {
                treeViewNavigation.SelectedNode = treeViewNavigation.Nodes[0].Nodes[0];
            }
        }

        private void TreeViewNavigation_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e?.Node?.Tag is NodeTag tag)
            {
                switch (tag.FileSourceType)
                {
                    case FileSourceType.FileSystem:
                        NavigateToFileSystem(tag.Path);
                        break;
                    case FileSourceType.RecycleBin:
                        NavigateToRecycleBin();
                        break;
                    case FileSourceType.WcxArchive:
                        NavigateToArchive(tag.Path);
                        break;
                    case FileSourceType.ControlPanel:
                        NavigateToControlPanel(tag.Path);
                        break;
                }
            }
        }

        private void NavigateToFileSystem(string path)
        {
            try
            {
                _currentFileSource = new FileSystemFileSource();
                _currentPath = path;

                if (_currentFileSource == null || _currentPath == null) return;
                var listOperation = _currentFileSource.CreateListOperation(_currentPath);
                if (listOperation != null)
                {
                    listOperation.Execute();
                    _currentFiles = ((FileSourceListOperation)listOperation).Files;
                    DisplayFiles(_currentFiles);
                }



                // Update status bar
                toolStripStatusLabel.Text = $"文件系统: {_currentPath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到文件系统出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NavigateToRecycleBin()
        {
            try
            {
                _currentFileSource = new RecycleBinFileSource();
                _currentPath = new string(Path.DirectorySeparatorChar, 3) + Resources.VfsRecycleBin + Path.DirectorySeparatorChar;

                if (_currentFileSource == null || _currentPath == null) return;
                var listOperation = _currentFileSource.CreateListOperation(_currentPath);
                if (listOperation != null)
                {
                    listOperation.Execute();
                    _currentFiles = ((FileSourceListOperation)listOperation).Files;
                    DisplayFiles(_currentFiles);
                }



                // Update status bar
                toolStripStatusLabel.Text = "回收站";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到回收站出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NavigateToControlPanel(string path)
        {
            try
            {
                _currentFileSource = new ControlPanelFileSource();
                _currentPath = path;

                if (_currentFileSource == null) return;
                var listOperation = _currentFileSource.CreateListOperation(_currentPath);
                if (listOperation != null)
                {
                    listOperation.Execute();
                    _currentFiles = ((FileSourceListOperation)listOperation).Files;
                    DisplayFiles(_currentFiles);
                }

                // Update status bar
                toolStripStatusLabel.Text = "控制面板";

                // 如果是首次点击控制面板节点，展开所有子节点
                if (string.IsNullOrEmpty(path) && treeViewNavigation.SelectedNode != null && !treeViewNavigation.SelectedNode.IsExpanded)
                {
                    ExpandControlPanelNode(treeViewNavigation.SelectedNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到控制面板出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TreeViewNavigation_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is NodeTag tag)
            {
                // 如果是控制面板节点或其子节点
                if (tag.FileSourceType == FileSourceType.ControlPanel)
                {
                    // 如果是根控制面板节点
                    if (e.Node.Text == "控制面板")
                    {
                        ExpandControlPanelNode(e.Node);
                    }
                    // 如果是控制面板的子节点
                    else if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "...")
                    {
                        ExpandControlPanelSubNode(e.Node);
                    }
                }
            }
        }

        private void ExpandControlPanelSubNode(TreeNode node)
        {
            try
            {
                if (node.Tag is NodeTag tag && !string.IsNullOrEmpty(tag.Path))
                {
                    // 清除占位节点
                    node.Nodes.Clear();

                    // 获取控制面板文件夹
                    IShellFolder controlPanelFolder = w32.GetControlPanelFolder(out _);

                    // 解析PIDL
                    uint attributes = 0;
                    controlPanelFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, node.Text, out _, out IntPtr pidl, ref attributes);

                    if (pidl != IntPtr.Zero)
                    {
                        try
                        {
                            // 绑定到子文件夹
                            var shellFolderGuid = typeof(IShellFolder).GUID;
                            w32.OleCheck(API.SHBindToParent(pidl, ref shellFolderGuid, out object? folderObj, out _));
                            IShellFolder subFolder = (IShellFolder)folderObj!;

                            // 枚举子项目
                            var flags = SHCONTF.FOLDERS | SHCONTF.NONFOLDERS;
                            subFolder.EnumObjects(IntPtr.Zero, flags, out IntPtr enumPtr);

                            if (enumPtr != IntPtr.Zero)
                            {
                                IEnumIDList enumIdList = (IEnumIDList)Marshal.GetObjectForIUnknown(enumPtr);

                                while (enumIdList.Next(1, out IntPtr childPidl, out uint fetched) == 0 && fetched == 1)
                                {
                                    try
                                    {
                                        // 获取项目名称
                                        string name = w32.GetDisplayName(subFolder, childPidl, SHGDN.INFOLDER);

                                        // 获取项目属性
                                        SFGAO childAttributes = 0;
                                        subFolder.GetAttributesOf(1, [childPidl], ref childAttributes);

                                        // 创建子节点
                                        var childNode = new TreeNode(name)
                                        {
                                            Tag = new NodeTag
                                            {
                                                Path = childPidl.ToString(), // 使用PIDL作为路径标识
                                                FileSourceType = FileSourceType.ControlPanel
                                            }
                                        };

                                        // 设置图标
                                        Icon? icon = IconManager.ExtractIconFromPIDL(subFolder, childPidl);
                                        if (icon != null)
                                        {
                                            // 添加图标到ImageList
                                            string iconKey = $"cp_{node.Text}_{name}";
                                            if (!treeViewNavigation.ImageList.Images.ContainsKey(iconKey))
                                            {
                                                treeViewNavigation.ImageList.Images.Add(iconKey, icon);
                                            }
                                            childNode.ImageKey = iconKey;
                                            childNode.SelectedImageKey = iconKey;
                                        }
                                        else
                                        {
                                            // 使用默认图标
                                            childNode.ImageIndex = 6;
                                            childNode.SelectedImageIndex = 6;
                                        }

                                        // 如果是文件夹，添加占位子节点
                                        if ((childAttributes & SFGAO.FOLDER) != 0)
                                        {
                                            childNode.Nodes.Add("...");
                                        }

                                        node.Nodes.Add(childNode);
                                    }
                                    finally
                                    {
                                        if (childPidl != IntPtr.Zero)
                                        {
                                            API.ILFree(childPidl);
                                        }
                                    }
                                }

                                Marshal.ReleaseComObject(enumIdList);
                            }

                            Marshal.ReleaseComObject(subFolder);
                        }
                        finally
                        {
                            API.ILFree(pidl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"展开控制面板子节点时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExpandControlPanelNode(TreeNode node)
        {
            try
            {
                // 清除现有子节点
                node.Nodes.Clear();

                // 获取控制面板文件夹
                IShellFolder controlPanelFolder = w32.GetControlPanelFolder(out _);

                // 枚举控制面板项目
                var flags = SHCONTF.FOLDERS | SHCONTF.NONFOLDERS;
                controlPanelFolder.EnumObjects(IntPtr.Zero, flags, out IntPtr enumPtr);

                if (enumPtr != IntPtr.Zero)
                {
                    IEnumIDList enumIdList = (IEnumIDList)Marshal.GetObjectForIUnknown(enumPtr);

                    while (enumIdList.Next(1, out IntPtr pidl, out uint fetched) == 0 && fetched == 1)
                    {
                        try
                        {
                            // 获取项目名称
                            string name = w32.GetDisplayName(controlPanelFolder, pidl, SHGDN.INFOLDER);

                            // 获取项目属性
                            SFGAO attributes = 0;
                            controlPanelFolder.GetAttributesOf(1, [pidl], ref attributes);

                            // 创建子节点
                            var childNode = new TreeNode(name)
                            {
                                Tag = new NodeTag
                                {
                                    Path = pidl.ToString(), // 使用PIDL作为路径标识
                                    FileSourceType = FileSourceType.ControlPanel
                                }
                            };

                            // 设置图标
                            Icon? icon = IconManager.ExtractIconFromPIDL(controlPanelFolder, pidl);
                            if (icon != null)
                            {
                                // 添加图标到ImageList
                                string iconKey = $"cp_{name}";
                                if (!treeViewNavigation.ImageList.Images.ContainsKey(iconKey))
                                {
                                    treeViewNavigation.ImageList.Images.Add(iconKey, icon);
                                }
                                childNode.ImageKey = iconKey;
                                childNode.SelectedImageKey = iconKey;
                            }
                            else
                            {
                                // 使用默认图标
                                childNode.ImageIndex = 6;
                                childNode.SelectedImageIndex = 6;
                            }

                            // 如果是文件夹，添加占位子节点
                            if ((attributes & SFGAO.FOLDER) != 0)
                            {
                                childNode.Nodes.Add("...");
                            }

                            node.Nodes.Add(childNode);
                        }
                        finally
                        {
                            if (pidl != IntPtr.Zero)
                            {
                                API.ILFree(pidl);
                            }
                        }
                    }

                    Marshal.ReleaseComObject(enumIdList);
                }

                // 展开节点
                node.Expand();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"展开控制面板节点时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NavigateToArchive(string archivePath)
        {
            try
            {
                // Get file extension
                string ext = Path.GetExtension(archivePath).TrimStart('.');

                // Find WCX module for this extension
                // Get WCX module for this extension
                WcxModule? wcxModule = null;
                foreach (var module in WcxPlugins.FileName)
                {
                    var loadedModule = WcxPlugins.LoadModule(module);
                    if (loadedModule != null && loadedModule.DetectStrings.Contains(ext))
                    {
                        wcxModule = loadedModule;
                        break;
                    }
                }

                if (wcxModule != null && !string.IsNullOrEmpty(wcxModule.FilePath))
                {
                    // Create archive file source
                    var fileSystemFileSource = new FileSystemFileSource();
                    _currentFileSource = new WcxArchiveFileSource(fileSystemFileSource, archivePath, wcxModule.FilePath, wcxModule.PluginCapabilities);
                    _currentPath = Path.DirectorySeparatorChar.ToString();

                    if (_currentFileSource == null || _currentPath == null) return;
                    var listOperation = _currentFileSource.CreateListOperation(_currentPath);
                    if (listOperation != null)
                    {
                        listOperation.Execute();
                        _currentFiles = ((FileSourceListOperation)listOperation).Files;
                        DisplayFiles(_currentFiles);
                    }



                    // Update status bar
                    toolStripStatusLabel.Text = $"压缩文件: {archivePath}";
                }
                else
                {
                    MessageBox.Show($"没有找到支持 {ext} 格式的压缩插件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到压缩文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayFiles(FileEntries files)
        {
            listViewFiles.Items.Clear();

            foreach (var file in files)
            {
                var item = new ListViewItem(file.Name);

                // Add subitems
                item.SubItems.Add(file.IsDirectory ? "<DIR>" : FormatFileSize(file.Size));
                item.SubItems.Add(file.ModificationTime.ToString());

                // Set icon based on file type
                item.ImageIndex = file.IsDirectory ? 4 : 5;

                // Store file entry in tag
                item.Tag = file;

                listViewFiles.Items.Add(item);
            }
        }

        private static string FormatFileSize(long size)
        {
            var sizes = new[] { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void ListViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewFiles.SelectedItems[0];
                if (selectedItem.Tag is FileEntry file)


                {
                    if (file.IsDirectory)
                    {
                        // Navigate to directory
                        if (file.Name == "..")
                        {
                            // Go to parent directory
                            string? parentPath = _currentPath != null ? Path.GetDirectoryName(_currentPath) : null;
                            if (!string.IsNullOrEmpty(parentPath))
                            {
                                _currentPath = parentPath;
                                RefreshCurrentView();
                            }
                        }
                        else
                        {
                            // Go to subdirectory
                            _currentPath = _currentPath != null ? Path.Combine(_currentPath, file.Name) : file.Name;
                            RefreshCurrentView();
                        }
                    }
                    else if (_currentFileSource is FileSystemFileSource)
                    {
                        // Check if file is an archive
                        string ext = Path.GetExtension(file.Name).TrimStart('.');
                        // Get WCX module for this extension
                        WcxModule? wcxModule = null;
                        foreach (var module in WcxPlugins.FileName)
                        {
                            var loadedModule = WcxPlugins.LoadModule(module);
                            if (loadedModule != null && loadedModule.DetectStrings.Contains(ext))
                            {
                                wcxModule = loadedModule;
                                break;
                            }
                        }

                        if (wcxModule != null && !string.IsNullOrEmpty(wcxModule.FilePath))
                        {
                            // Navigate to archive
                            string archivePath = _currentPath != null ? Path.Combine(_currentPath, file.Name) : file.Name;

                            // Add to tree view if not already there
                            TreeNode archivesNode = treeViewNavigation.Nodes[2]; // "压缩文件" node

                            // Check if archive is already in tree
                            bool found = false;
                            foreach (TreeNode node in archivesNode.Nodes)
                            {
                                if (node.Tag is NodeTag tag && tag.Path == archivePath)
                                {
                                    treeViewNavigation.SelectedNode = node;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                // Add archive to tree
                                TreeNode archiveNode = archivesNode.Nodes.Add(archivePath, file.Name, 2, 2);
                                archiveNode.Tag = new NodeTag { Path = archivePath, FileSourceType = FileSourceType.WcxArchive };
                                treeViewNavigation.SelectedNode = archiveNode;
                                archivesNode.Expand();
                            }
                        }
                        else
                        {
                            // Try to execute file
                            try
                            {
                                if (_currentFileSource != null && _currentPath != null)
                                {
                                    var executeOperation = _currentFileSource.CreateExecuteOperation(file, _currentPath, string.Empty);
                                    executeOperation.Execute();
                                }

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"执行文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }

        private void RefreshCurrentView()
        {
            try
            {
                if (_currentFileSource == null || _currentPath == null) return;
                var listOperation = _currentFileSource.CreateListOperation(_currentPath);
                if (listOperation != null)
                {
                    listOperation.Execute();
                    _currentFiles = ((FileSourceListOperation)listOperation).Files;
                    DisplayFiles(_currentFiles);
                }



                // Update status bar
                if (_currentFileSource is RecycleBinFileSource)
                {
                    toolStripStatusLabel.Text = "回收站";
                }
                else if (_currentFileSource is WcxArchiveFileSource wcxArchiveFileSource)
                {
                    toolStripStatusLabel.Text = $"压缩文件: {wcxArchiveFileSource.ArchiveFileName}";
                }
                else
                {
                    toolStripStatusLabel.Text = $"文件系统: {_currentPath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新视图出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshCurrentView();
        }

        private void ContextMenuFiles_Opening(object sender, CancelEventArgs e)
        {
            // Enable/disable menu items based on current file source and selection
            bool hasSelection = listViewFiles.SelectedItems.Count > 0;

            // Common operations
            menuItemOpen.Enabled = hasSelection;
            menuItemCopy.Enabled = hasSelection;
            menuItemDelete.Enabled = hasSelection;

            // RecycleBin specific operations
            menuItemEmptyRecycleBin.Visible = _currentFileSource is RecycleBinFileSource;
            menuItemRestore.Visible = _currentFileSource is RecycleBinFileSource && hasSelection;

            // WcxArchive specific operations
            menuItemExtract.Visible = _currentFileSource is WcxArchiveFileSource && hasSelection;
            menuItemAddFiles.Visible = _currentFileSource is WcxArchiveFileSource;
            menuItemTestArchive.Visible = _currentFileSource is WcxArchiveFileSource;

            // FileSystem specific operations
            menuItemCreateDirectory.Visible = _currentFileSource is FileSystemFileSource;
            menuItemProperties.Enabled = hasSelection;
        }

        private void MenuItemOpen_Click(object sender, EventArgs e)
        {
            ListViewFiles_DoubleClick(sender, e);
        }

        private void MenuItemCopy_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                // Create a list of selected files
                var selectedFiles = new FileEntries();
                foreach (ListViewItem item in listViewFiles.SelectedItems)
                {
                    if (item.Tag is FileEntry file)
                    {
                        selectedFiles.Add(file);
                    }
                }

                // Show folder browser dialog
                var dialog = new FolderBrowserDialog
                {
                    Description = "选择目标文件夹"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create target file source
                        IFileSource targetFileSource = new FileSystemFileSource();

                        // Create copy operation
                        // Execute operation
                        FileSourceOperation? operation = null;

                        if (_currentFileSource is WcxArchiveFileSource)
                        {
                            // Copy out from archive
                            operation = _currentFileSource.CreateCopyOutOperation(targetFileSource, selectedFiles, dialog.SelectedPath);
                        }
                        else if (_currentFileSource != null)
                        {
                            // Regular copy
                            operation = _currentFileSource.CreateCopyOperation(selectedFiles, dialog.SelectedPath);
                        }

                        if (operation != null)
                        {
                            _operationsManager.AddOperation(operation);
                            operation.Execute();
                        }

                        MessageBox.Show("复制操作完成", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"复制文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MenuItemDelete_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                // Create a list of selected files
                var selectedFiles = new FileEntries();
                foreach (ListViewItem item in listViewFiles.SelectedItems)
                {
                    if (item.Tag is FileEntry file)
                    {
                        selectedFiles.Add(file);
                    }
                }

                // Confirm deletion
                string message = selectedFiles.Count == 1
                    ? $"确定要删除 {selectedFiles[0].Name} 吗?"
                    : $"确定要删除选中的 {selectedFiles.Count} 个项目吗?";

                if (MessageBox.Show(message, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        // Create delete operation
                        if (_currentFileSource != null)
                        {
                            var operation = _currentFileSource.CreateDeleteOperation(selectedFiles);
                            if (operation != null)
                            {
                                // Execute operation
                                _operationsManager.AddOperation(operation);
                                operation.Execute();
                            }
                        }

                        // Refresh view
                        RefreshCurrentView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MenuItemEmptyRecycleBin_Click(object sender, EventArgs e)
        {
            if (_currentFileSource is RecycleBinFileSource)
            {
                if (MessageBox.Show("确定要清空回收站吗?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        // Initialize COM
                        w32.InitializeCOM();

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
                            // Uninitialize COM
                            w32.UninitializeCOM();
                        }

                        // Refresh view
                        RefreshCurrentView();

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
			//object[] args = (object[])param;
			//string filename = (string)args[0];
			//string filepath = (string)args[1];

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
            if (_currentFileSource is RecycleBinFileSource && listViewFiles.SelectedItems.Count > 0)
            {
                try
                {
                    // Initialize COM
                    w32.InitializeCOM();

                    try
                    {
                        bool anyRestored = false;

                        foreach (ListViewItem item in listViewFiles.SelectedItems)
                        {
                            if (item.Tag is FileEntry file)
                            {
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
                            RefreshCurrentView();
                            MessageBox.Show("文件已成功还原", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    finally
                    {
                        // Uninitialize COM
                        w32.UninitializeCOM();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"还原文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
		public static Shell32.Folder GetShell32Folder(object folder, Object shell, Type shellAppType)
		{
			return (Shell32.Folder)shellAppType.InvokeMember("NameSpace",
			System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { folder });
		}
		public static void GetRecycleBinFilenames()
		{
			Console.WriteLine("\n[+]系统回收站里的文件列表如下：");
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			Object? shell = Activator.CreateInstance(shellAppType);
			Folder recycleBin = GetShell32Folder(10, shell, shellAppType);
			foreach (FolderItem2 recfile in recycleBin.Items())
			{
				Console.WriteLine($"\t【文件名]:" + recfile.Name + "，[文件路径]:" + recfile.Path);
				Console.WriteLine($"\t[文件恢复］:move" + recfile.Path + "D:\\" + recfile.Name);
				Console.WriteLine($"\n");
			}
			Marshal.FinalReleaseComObject(shell); 
		}

		private void MenuItemExtract_Click(object sender, EventArgs e)
        {
            if (_currentFileSource is WcxArchiveFileSource && listViewFiles.SelectedItems.Count > 0)
            {
                // Create a list of selected files
                var selectedFiles = new FileEntries();
                foreach (ListViewItem item in listViewFiles.SelectedItems)
                {
                    if (item.Tag is FileEntry file)
                    {
                        selectedFiles.Add(file);
                    }
                }

                // Show folder browser dialog
                var dialog = new FolderBrowserDialog
                {
                    Description = "选择解压目标文件夹"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create target file source
                        IFileSource targetFileSource = new FileSystemFileSource();

                        // Create copy out operation
                        var operation = _currentFileSource.CreateCopyOutOperation(targetFileSource, selectedFiles, dialog.SelectedPath);

                        // Execute operation
                        _operationsManager.AddOperation(operation);
                        operation.Execute();

                        MessageBox.Show("解压操作完成", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"解压文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MenuItemAddFiles_Click(object sender, EventArgs e)
        {
            if (_currentFileSource is WcxArchiveFileSource wcxArchiveFileSource)
            {
                // Show open file dialog
                var dialog = new OpenFileDialog
                {
                    Multiselect = true,
                    Title = "选择要添加到压缩文件的文件"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create source file source
                        IFileSource sourceFileSource = new FileSystemFileSource();

                        // Create file entries for selected files
                        var sourceFiles = new FileEntries();
                        foreach (string fileName in dialog.FileNames)
                        {
                            FileEntry file = FileSystemFileSource.CreateFileFromFile(fileName);
                            sourceFiles.Add(file);
                        }

                        // Create copy in operation
                        if (_currentPath != null)
                        {
                            var operation = wcxArchiveFileSource.CreateCopyInOperation(sourceFileSource, sourceFiles, _currentPath);
                            if (operation != null)
                            {
                                // Execute operation
                                _operationsManager.AddOperation(operation);
                                operation.Execute();
                            }
                        }

                        // Refresh view
                        RefreshCurrentView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MenuItemTestArchive_Click(object sender, EventArgs e)
        {
            if (_currentFileSource is WcxArchiveFileSource wcxArchiveFileSource)
            {
                try
                {
                    // Create test archive operation
                    if (_currentFiles != null)
                    {
                        var operation = wcxArchiveFileSource.CreateTestArchiveOperation(_currentFiles);
                        if (operation != null)
                        {
                            // Execute operation
                            _operationsManager.AddOperation(operation);
                            operation.Execute();
                        }
                    }

                    MessageBox.Show("压缩文件测试完成", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"测试压缩文件出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MenuItemCreateDirectory_Click(object sender, EventArgs e)
        {
            if (_currentFileSource is FileSystemFileSource)
            {
                // Show input dialog
                string directoryName = Interaction.InputBox("请输入新文件夹名称:", "创建文件夹", "");

                if (!string.IsNullOrEmpty(directoryName))
                {
                    try
                    {
                        // Create directory operation
                        if (_currentPath != null)
                        {
                            var operation = _currentFileSource.CreateCreateDirectoryOperation(_currentPath, directoryName);
                            if (operation != null)
                            {
                                // Execute operation
                                _operationsManager.AddOperation(operation);
                                operation.Execute();
                            }
                        }

                        // Refresh view
                        RefreshCurrentView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"创建文件夹出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MenuItemProperties_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewFiles.SelectedItems[0];
                if (selectedItem.Tag is FileEntry file)


                {
                    // Show properties dialog
                    var properties = new StringBuilder();
                    properties.AppendLine($"名称: {file.Name}");
                    properties.AppendLine($"路径: {file.FullPath}");
                    properties.AppendLine($"大小: {FormatFileSize(file.Size)}");
                    properties.AppendLine($"修改时间: {file.ModificationTime}");
                    properties.AppendLine($"创建时间: {file.CreationTime}");
                    properties.AppendLine($"属性: {file.Attributes}");

                    if (file.LinkProperty != null && !string.IsNullOrEmpty(file.LinkProperty.LinkTarget))
                    {
                        properties.AppendLine($"链接目标: {file.LinkProperty.LinkTarget}");
                    }

                    MessageBox.Show(properties.ToString(), $"{file.Name} 的属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            // Go to parent directory
            if (!string.IsNullOrEmpty(_currentPath) && _currentPath != Path.DirectorySeparatorChar.ToString())
            {
                string? parentPath = _currentPath != null ? Path.GetDirectoryName(_currentPath) : null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    _currentPath = parentPath;
                    RefreshCurrentView();
                }
            }
        }
    }

    public enum FileSourceType
    {
        FileSystem,
        RecycleBin,
        WcxArchive,
        ControlPanel
    }

    public class NodeTag
    {
        public string Path { get; set; } = string.Empty;
        public FileSourceType FileSourceType { get; set; }
    }
}
