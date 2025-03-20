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
			StringBuilder sbDisplayName = new(MAX_PATH);
			StringBuilder sbOriginalPath = new(MAX_PATH);

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
			if (string.IsNullOrEmpty(currentDirectory[isleft]) || currentDirectory[isleft].Equals(newPath))
				return;

			backStack.Push(currentDirectory[isleft]);
			forwardStack.Clear(); // 清除前进历史
			currentDirectory[isleft] = newPath;
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
			if (ftpNodeSelect(parentnode)) return;

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
					archivePaths[path] = currentDirectory[isleft];
					var items = LoadArchiveContents(path);
					listView.Items.Clear();
					listView.Items.AddRange(items.ToArray());
					currentDirectory[isleft] = path;
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
				Debug.Print("exception in createlistview item");
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
				string filePath = Helper.getFSpath(Path.Combine(currentDirectory[isleft], selectedItem.Text));

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
			cm_list();
		}
		private List<string> GetFileListByViewOrParam(string param)
		{
			if (!param.Equals(string.Empty))
				return se.PrepareParameter(param, new string[] { }, "");
			List<string> result = new();
			if (activeListView.SelectedItems.Count == 0)
				return result;

			// 检查是否是FTP路径
			if (currentDirectory[isleft].StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
			{
				// 从当前目录中提取连接名称
				string connectionName = ExtractFtpConnectionName(currentDirectory[isleft]);
				if (!string.IsNullOrEmpty(connectionName) && fTPMGR.ftpSources.TryGetValue(connectionName, out FtpFileSource source))
				{
					// 对于FTP文件，先下载到本地临时目录
					return activeListView.SelectedItems.Cast<ListViewItem>()
						.Where(i => i.SubItems[3].Text != "<DIR>") // 排除目录
						.Select(i =>
						{
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

		public void cm_list(string param = "")
		{
			// 编辑按钮点击处理逻辑
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
			cm_edit();
		}
		public void cm_edit(string param = "")
		{
			var files = GetFileListByViewOrParam(param);
			var editorForm = new NewEditorForm(files)
			{
				Text = $"编辑文件 - {files[0]}",
				Size = new Size(800, 600)
			};
			editorForm.Show();
		}
		public void CopyButton_Click(object? sender, EventArgs e)
		{
			//cmdProcessor.ExecCmd("cm_copy");
			cm_copy();
		}
		public void DeleteButton_Click(object? sender, EventArgs e)
		{
			//cmdProcessor.ExecCmd("cm_delete");
			cm_delete();
		}

		public void FolderButton_Click(object? sender, EventArgs e)
		{
			string input = Microsoft.VisualBasic.Interaction.InputBox("请输入新文件夹名称: eg. dir1,dir2\\dir3", "新建文件夹", "新建文件夹");
			if (string.IsNullOrWhiteSpace(input)) return;
			var dirs = input.Split(',');
			foreach (var dir in dirs)
			{
				string newFolderPath = Path.Combine(currentDirectory[isleft], dir);
				FileSystemManager.CreateDirectory(newFolderPath);
			}
			RefreshPanel(activeListView);
		}

		public void MoveButton_Click(object? sender, EventArgs e)
		{
			//cmdProcessor.ExecCmd("cm_renmov");
			cm_renmov();
		}

		public void RefreshTreeViewAndListView(ListView listView, string path)
		{
			if (listView == null) return;

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
			RefreshPanel(treeView == uiManager.LeftTree ? RefreshPanelMode.Left : RefreshPanelMode.Right);
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
			RefreshPanel(isleft ? RefreshPanelMode.Left : RefreshPanelMode.Right);
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
				if (uiManager.RightTree.SelectedNode.Tag is ShellItem)
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
			if (viewMode == activeListView.View) return;
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

		public bool OpenArchive(string archivePath)
		{
			if (openArchives.ContainsKey(archivePath))
				return true;

			string ext = Path.GetExtension(archivePath).ToLower();
			var wcxModule = wcxModuleList.GetModuleByExt(ext);
			if (wcxModule == null)
				return false;
			if (!wcxModule.CanYouHandleThisFile(archivePath))
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
		public string GetListItemPath(ListViewItem item)
		{
			if (item.Tag is TreeNode node)
			{
				// 对于本地文件系统
				var path = Helper.getFSpath(node?.FullPath);
				return Path.Combine(path, item.Text);
			}

			// 检查是否是FTP节点
			//if (item?.Tag is FtpNodeTag || item?.Tag is FtpRootNodeTag)
			if (uiManager.activeTreeview.SelectedNode.Tag is FtpNodeTag ftpnode)
			{
				// 对于FTP项，直接使用SubItems[1]中存储的完整路径
				return item.SubItems[1].Text;
			}
			return string.Empty;
		}
		public void ToolbarStrip_Click(object sender, EventArgs e)
		{
			var mouse_event = e as MouseEventArgs;
			if (mouse_event.Button == MouseButtons.Right)
			{
				if (sender is ToolStrip)
				{
					ToolStrip toolStrip = sender as ToolStrip;
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
		public bool cm_copy(string param = null)
		{
			var files1 = GetFileListByViewOrParam(param);
			var listView = activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return false;

			var srcPath = Helper.getFSpath(!uiManager.isleft ? uiManager.RightTree.SelectedNode.FullPath : uiManager.LeftTree.SelectedNode.FullPath);
			var targetTree = uiManager.isleft ? uiManager.RightTree : uiManager.LeftTree;
			var targetPath = Helper.getFSpath(targetTree.SelectedNode.FullPath);
			var isSamePath = targetPath.Equals(srcPath);

			var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => GetListItemPath(item))
				.ToArray();

			var targetlist = uiManager.isleft ? uiManager.RightList : uiManager.LeftList;
			try
			{
				// 处理压缩文件的情况
				if (IsArchiveFile(srcPath))
				{
					foreach (string fileName in sourceFiles)
					{
						ExtractArchiveFile(srcPath, fileName, targetPath);
					}
					return true;
				}

				if (IsArchiveFile(targetPath))
				{
					string[] files = sourceFiles.Select(f => Path.Combine(srcPath, f)).ToArray();
					AddToArchive(targetPath, files);
					var items = LoadArchiveContents(targetPath);
					var targetListView = (uiManager.isleft ? uiManager.RightList : uiManager.LeftList);
					targetListView.Items.Clear();
					targetListView.Items.AddRange(items.ToArray());
					return true;
				}

				// 检查源路径和目标路径是否为FTP路径
				bool isSourceFtp = fTPMGR.IsFtpPath(srcPath);
				bool isTargetFtp = fTPMGR.IsFtpPath(targetPath);

				if (isSourceFtp && !isTargetFtp)
				{
					// 从FTP下载到本地
					var ftpSource = fTPMGR.GetFtpSource(srcPath);
					if (ftpSource != null)
					{
						foreach (string remotePath in sourceFiles)
						{
							string fileName = Path.GetFileName(remotePath);
							string localPath = Path.Combine(targetPath, fileName);
							string tempFile = ftpSource.DownloadFile(remotePath);
							if (!string.IsNullOrEmpty(tempFile))
							{
								try
								{
									File.Copy(tempFile, localPath, true);
								}
								finally
								{
									// 清理临时文件
									if (File.Exists(tempFile))
										File.Delete(tempFile);
								}
							}
						}
					}
				}
				else if (!isSourceFtp && isTargetFtp)
				{
					// 从本地上传到FTP
					var ftpTarget = fTPMGR.GetFtpSource(targetPath);
					if (ftpTarget != null)
					{
						foreach (string localFile in sourceFiles)
						{
							string fileName = Path.GetFileName(localFile);
							string remotePath = Path.Combine(targetPath, fileName).Replace("\\", "/");
							ftpTarget.UploadFile(localFile, remotePath);
						}
					}
				}
				else if (isSourceFtp && isTargetFtp)
				{
					// FTP到FTP的复制
					var sourceFtp = fTPMGR.GetFtpSource(srcPath);
					var targetFtp = fTPMGR.GetFtpSource(targetPath);
					if (sourceFtp != null && targetFtp != null)
					{
						foreach (string remotePath in sourceFiles)
						{
							// 先下载到临时目录
							string tempFile = sourceFtp.DownloadFile(remotePath);
							if (!string.IsNullOrEmpty(tempFile))
							{
								try
								{
									// 再上传到目标FTP
									string fileName = Path.GetFileName(remotePath);
									string targetRemotePath = Path.Combine(targetPath, fileName).Replace("\\", "/");
									targetFtp.UploadFile(tempFile, targetRemotePath);
								}
								finally
								{
									// 清理临时文件
									if (File.Exists(tempFile))
										File.Delete(tempFile);
								}
							}
						}
					}
				}
				else
				{
					// 本地文件之间的复制
					FileSystemManager.CopyFilesAndDirectories(sourceFiles, targetPath);
				}

				RefreshPanel(targetlist);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制文件失败: {ex.Message}", "错误");
				return false;
			}
		}
		// 移动选中的文件
		public void cm_renmov()
		{
			var listView = activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var srcpath = Helper.getFSpath(activeTreeview.SelectedNode.FullPath);
			var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => GetListItemPath(item))
				.ToArray();

			var targettree = uiManager.isleft ? uiManager.RightTree : uiManager.LeftTree;
			var targetPath = Helper.getFSpath(targettree.SelectedNode.FullPath);
			if (string.IsNullOrEmpty(targetPath))
			{
				MessageBox.Show("无效的目标路径", "错误");
				return;
			}
			if (srcpath.Equals(targetPath))
			{
				return;     //if srcpath eq targetpath, do not need move, do rename 
			}

			try
			{
				// 检查源路径和目标路径是否为FTP路径
				bool isSourceFtp = fTPMGR.IsFtpPath(srcpath);
				bool isTargetFtp = fTPMGR.IsFtpPath(targetPath);

				if (isSourceFtp || isTargetFtp)
				{
					// 如果涉及FTP，先复制后删除
					if (cm_copy())
					{
						// 如果源是FTP，使用FTP删除
						if (isSourceFtp)
						{
							var ftpSource = fTPMGR.GetFtpSource(srcpath);
							if (ftpSource != null)
							{
								foreach (string remotePath in sourceFiles)
								{
									ftpSource.DeleteFile(remotePath);
								}
							}
						}
						else
						{
							// 源是本地文件，使用本地删除
							cm_delete(false);
						}
					}
				}
				else
				{
					// 本地文件之间的移动
					if (cm_copy())
						cm_delete(false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"移动文件失败: {ex.Message}", "错误");
			}
		}

		// 删除选中的文件
		public void cm_delete(bool needConfirm = true)
		{
			//Debug.Print("Delete files : >>");
			var listView = activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var files = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => GetListItemPath(item))
				.ToArray();

			var currentPath = currentDirectory[isleft];
			var result = DialogResult.Yes;
			if (needConfirm)
			{
				result = MessageBox.Show(
					$"确定要删除选中的 {files.Length} 个文件吗？",
					"确认删除",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);
			}
			if (result == DialogResult.Yes)
			{
				try
				{
					if (IsArchiveFile(currentDirectory[isleft]))
					{
						if (DeleteFromArchive(currentDirectory[isleft], files.ToArray()))
						{
							var items = LoadArchiveContents(currentDirectory[isleft]);
							activeListView.Items.Clear();
							activeListView.Items.AddRange(items.ToArray());
						}
						return;
					}

					// 检查是否为FTP路径
					if (fTPMGR.IsFtpPath(currentPath))
					{
						var ftpSource = fTPMGR.GetFtpSource(currentPath);
						if (ftpSource != null)
						{
							foreach (string remotePath in files)
							{
								ftpSource.DeleteFile(remotePath);
							}
						}
					}
					else
					{
						// 本地文件删除
						foreach (var file in files)
						{
							FileSystemManager.DeleteFile(file);
						}
					}
					RefreshPanel(listView);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"删除文件失败: {ex.Message}", "错误");
				}
			}
		}

		// 创建新文件夹
		public void cm_mkdir(string folderName = "新建文件夹")
		{
			var path = currentDirectory[isleft];

			try
			{
				if (fTPMGR.IsFtpPath(path))
				{
					// FTP创建文件夹
					var ftpSource = fTPMGR.GetFtpSource(path);
					if (ftpSource != null)
					{
						string newFolderPath = Path.Combine(path, folderName).Replace("\\", "/");
						if (ftpSource.CreateDirectory(newFolderPath))
						{
							RefreshPanel(activeListView);
						}
					}
				}
				else
				{
					// 本地创建文件夹
					var newFolderPath = Path.Combine(path, folderName);
					FileSystemManager.CreateDirectory(newFolderPath);
					RefreshPanel(activeListView);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建文件夹失败: {ex.Message}", "错误");
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
		private bool ftpNodeSelect(TreeNode eNode)
		{
			// 检查是否是FTP节点
			if (eNode.Tag is FtpNodeTag ftpTag)
			{
				// 处理FTP节点双击事件
				fTPMGR.HandleFtpNodeDoubleClick(eNode, activeListView);
				activeListView.Refresh();
				// 更新当前目录和路径显示
				var ftpsrc = fTPMGR.GetFtpFileSourceByConnectionName(ftpTag.ConnectionName);
				//currentDirectory[isleft] = $"ftp://{ftpTag.ConnectionName}{ftpTag.Path}";
				currentDirectory[isleft] = $"ftp://{ftpsrc?.Host}{ftpTag.Path}";        //bugfix: currentdir can not be set to connection name, use host instead,
				//if (isleft)
				//	uiManager.LeftPathTextBox.Text = currentDirectory[isleft];
				//else
				//	uiManager.RightPathTextBox.Text = currentDirectory[isleft];

				SelectedNode = eNode;
				//uiManager.BookmarkManager.UpdateActiveBookmark(currentDirectory[isleft], selectedNode, isleft);
				UpdatePathTextAndDriveComboBox(eNode, currentDirectory[isleft], isleft);
				uiManager.setArgs();
				return true;
			}
			return false;
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
		//private void 加载文件ToolStripMenuItem_Click(object sender, EventArgs e)
		//{
		//	FolderBrowserDialog dlg = new FolderBrowserDialog();
		//	//if (dlg.ShowDialog() == DialogResult.OK)
		//	{
		//		string[] filespath = Directory.GetFiles(dlg.SelectedPath);
		//		var fileList = new FileInfoList(filespath);
		//		InitListView(fileList);
		//	}
		//}
		//private void InitListView(FileInfoList fileList)
		//{
		//	activeListView.Items.Clear();
		//	this.activeListView.BeginUpdate();
		//	foreach (FileInfoWithIcon file in fileList.list)
		//	{
		//		ListViewItem item = new ListViewItem();
		//		item.Text = file.fileInfo.Name.Split('.')[0];
		//		item.ImageIndex = file.iconIndex;
		//		item.SubItems.Add(file.fileInfo.LastWriteTime.ToString());
		//		item.SubItems.Add(file.fileInfo.Extension.Replace(".", ""));
		//		item.SubItems.Add(string.Format(("{0:N0}"), file.fileInfo.Length));
		//		activeListView.Items.Add(item);
		//	}
		//	activeListView.LargeImageList = fileList.imageListLargeIcon;
		//	activeListView.SmallImageList = fileList.imageListSmallIcon;
		//	activeListView.Show();
		//	this.activeListView.EndUpdate();
		//}
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
		public void myShellExe(string path = "c:\\windows\\system32")
		{
			API.ShellExecute(IntPtr.Zero, "open", path, "", path, (int)SW.SHOWNORMAL);
			//Window wnd = Window.GetWindow(this); //获取当前窗口
			//var wih = new WindowInteropHelper(wnd); //该类支持获取hWnd
			//IntPtr hWnd = wih.Handle;    //获取窗口句柄
			//var result = ShellExecute(hWnd, "open", "需要打开的路径如C:\\Users\\Desktop\\xx.exe", null, null, (int)ShowWindowCommands.SW_SHOW);
		}
	}
}
