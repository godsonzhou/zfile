using System.Diagnostics;
using System.Text;

namespace Zfile
{
    public partial class DirectoryTreeSearchForm : Form
    {
        private TreeView treeView;
        private TextBox searchBox;
		private Button nextButton;
        private Button refreshButton;
        private Button okButton;
        private Button cancelButton;
        private string currentDrive;
        private MainForm ownerForm;
        private Dictionary<string, TreeNode> directoryNodes = new Dictionary<string, TreeNode>();
		List<TreeNode> matchingNodes = new();
		private int idx;
		// 在类的字段部分添加
		private FlowLayoutPanel driveFlowLayoutPanel;
		private Color originalNodeForeColor;
		private TreeNode? lastHighlightedNode;

		public DirectoryTreeSearchForm(MainForm owner, string drive)
        {
            ownerForm = owner;
            currentDrive = drive;
			originalNodeForeColor = SystemColors.WindowText;
			InitializeComponent();
			LoadDriveButtons();
			LoadDirectoryTree();
        }

        private void InitializeComponent()
        {
            this.Text = "目录树查找";
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

			// 创建最上层的面板来容纳所有控件
			Panel mainPanel = new Panel
			{
				Dock = DockStyle.Fill
			};

			// 在 InitializeComponent 方法中添加 driveFlowLayoutPanel 的初始化代码，放在 topPanel 之前
			driveFlowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 35,
				Padding = new Padding(5),
				AutoScroll = true
			};
			// 创建搜索框和按钮面板
			Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            searchBox = new TextBox
            {
                Location = new Point(10, 10),
                Width = 350,
                PlaceholderText = "输入目录名称进行搜索..."
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            refreshButton = new Button
            {
                Text = "刷新",
                Location = new Point(370, 9),
                Width = 80
            };
            refreshButton.Click += RefreshButton_Click;

			nextButton = new Button
			{
				Text = "下一个",
				Location = new Point(470, 9),
				Width = 80
			};
			nextButton.Click += NextButton_Click;

			topPanel.Controls.Add(searchBox);
			topPanel.Controls.Add(nextButton);
			topPanel.Controls.Add(refreshButton);

            // 创建目录树
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true
            };
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;

            // 创建底部按钮面板
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(400, 10),
                Width = 80
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(490, 10),
                Width = 80
            };

            bottomPanel.Controls.Add(okButton);
            bottomPanel.Controls.Add(cancelButton);

			// 添加控件到窗体
			// 按照从下到上的顺序添加控件到主面板
			mainPanel.Controls.Add(treeView);         // 先添加树视图（Fill）
			mainPanel.Controls.Add(topPanel);         // 再添加搜索面板（Top）
			mainPanel.Controls.Add(driveFlowLayoutPanel); // 最后添加驱动器面板（Top）

			//this.Controls.Add(driveFlowLayoutPanel);
			//this.Controls.Add(treeView);
			//         this.Controls.Add(topPanel);
			this.Controls.Add(bottomPanel);
            this.Controls.Add(mainPanel);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

		// 添加加载驱动器按钮的方法
		private void LoadDriveButtons()
		{
			driveFlowLayoutPanel.Controls.Clear();

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				if (drive.IsReady)
				{
					Button driveButton = new Button
					{
						Text = $"{drive.Name} ({drive.VolumeLabel})",
						Tag = drive.Name,
						Width = 100,
						Height = 25,
						Margin = new Padding(2)
					};

					driveButton.Click += DriveButton_Click;

					// 高亮显示当前选中的驱动器
					if (drive.Name.Equals(currentDrive, StringComparison.OrdinalIgnoreCase))
					{
						driveButton.BackColor = SystemColors.Highlight;
						driveButton.ForeColor = Color.White;
					}

					driveFlowLayoutPanel.Controls.Add(driveButton);
				}
			}
		}

		// 添加驱动器按钮点击事件处理
		private void DriveButton_Click(object sender, EventArgs e)
		{
			if (sender is Button button)
			{
				string selectedDrive = button.Tag.ToString();
				if (selectedDrive != currentDrive)
				{
					currentDrive = selectedDrive;
					LoadDirectoryTree();

					// 更新按钮外观
					foreach (Control control in driveFlowLayoutPanel.Controls)
					{
						if (control is Button driveBtn)
						{
							bool isSelected = driveBtn.Tag.ToString() == currentDrive;
							driveBtn.BackColor = isSelected ? SystemColors.Highlight : SystemColors.Control;
							driveBtn.ForeColor = isSelected ? Color.White : SystemColors.ControlText;
						}
					}
				}
			}
		}
		private void LoadDirectoryTree()
        {
            treeView.Nodes.Clear();
            directoryNodes.Clear();
            Cursor = Cursors.WaitCursor;

            try
            {
				// 创建根节点
				TreeNode rootNode = new TreeNode(currentDrive)
				{
					Tag = currentDrive
				};
				treeView.Nodes.Add(rootNode);
				directoryNodes[currentDrive.ToLower()] = rootNode;

				var nfo = $"treeinfo{currentDrive.Substring(0, 1)}.wc";
				if (File.Exists(nfo))
				{
					//load dirs from file
					LoadDirectoriesFromFile(nfo, rootNode);
				}
				else 
				{
					// 使用Everything SDK生成目录结构
					if (EverythingWrapper.IsEverythingServiceRunning())
					{
						LoadDirectoriesUsingEverything(rootNode);
					}
					else
					{
						// 如果Everything服务未运行，使用传统方法加载目录
						LoadDirectoriesRecursively(rootNode, currentDrive);
					}
					savetofile(nfo);
				}
				rootNode.Expand();
			}
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录树时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
		private void savetofile(string filename)
		{
			//save 
			try
			{
				using var writer = new StreamWriter(filename, false, Encoding.UTF8);
				// 写入保存时间作为文件头
				writer.WriteLine($"#SaveTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

				// 使用队列进行广度优先遍历，保存目录结构
				var queue = new Queue<TreeNode>();
				queue.Enqueue(treeView.Nodes[0]); // 根节点

				while (queue.Count > 0)
				{
					var node = queue.Dequeue();
					string path = node.Tag?.ToString() ?? string.Empty;
					// 每行格式：目录完整路径|目录名称
					writer.WriteLine($"{path}|{node.Text}");

					// 将子节点加入队列
					foreach (TreeNode childNode in node.Nodes)
					{
						queue.Enqueue(childNode);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"保存目录树结构时出错: {ex.Message}");
				// 这里我们只是记录错误，不抛出异常，因为这不是关键功能
			}
		}
		private void LoadDirectoriesFromFile(string filename, TreeNode rootNode)
		{
			try
			{
				string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
				if (lines.Length == 0)
					return;

				// 检查文件是否过期（超过24小时）
				if (lines[0].StartsWith("#SaveTime:"))
				{
					string timeStr = lines[0].Substring(10);
					if (DateTime.TryParse(timeStr, out DateTime saveTime))
					{
						if ((DateTime.Now - saveTime).TotalHours > 24)
						{
							// 文件过期，返回false以触发重新扫描
							return;
						}
					}
				}

				// 使用字典记录父节点路径和对应的TreeNode
				var pathNodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase)
				{
					[currentDrive.ToLower()] = rootNode
				};

				// 从第二行开始处理（跳过时间戳行）
				for (int i = 1; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line)) continue;

					string[] parts = line.Split('|');
					if (parts.Length != 2) continue;

					string fullPath = parts[0];
					string dirName = parts[1];

					// 获取父目录路径
					string parentPath = Path.GetDirectoryName(fullPath)?.ToLower() ?? string.Empty;

					// 如果能找到父节点，就创建当前节点
					if (pathNodes.TryGetValue(parentPath, out TreeNode parentNode))
					{
						TreeNode newNode = new TreeNode(dirName)
						{
							Tag = fullPath
						};
						parentNode.Nodes.Add(newNode);
						pathNodes[fullPath.ToLower()] = newNode;
						directoryNodes[fullPath.ToLower()] = newNode;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"加载目录树缓存文件时出错: {ex.Message}");
				// 出错时清空节点，以便后续重新扫描
				rootNode.Nodes.Clear();
			}
		}

		private void LoadDirectoriesUsingEverything(TreeNode rootNode)
        {
            // 设置搜索参数
            EverythingWrapper.Everything_SetSearchW($"{currentDrive}*");
            EverythingWrapper.Everything_SetRequestFlags(
                EverythingWrapper.EVERYTHING_REQUEST_PATH | 
                EverythingWrapper.EVERYTHING_REQUEST_FILE_NAME);
            EverythingWrapper.Everything_SetMatchPath(true);
            EverythingWrapper.Everything_SetMatchCase(false);
            EverythingWrapper.Everything_SetMatchWholeWord(false);
            EverythingWrapper.Everything_SetRegex(false);
            EverythingWrapper.Everything_SetSort(EverythingWrapper.EVERYTHING_SORT_PATH_ASCENDING);

            // 执行查询
            EverythingWrapper.Everything_QueryW(true);

            // 检查错误
            uint errorCode = EverythingWrapper.Everything_GetLastError();
            if (errorCode != 0)
            {
                throw new Exception($"Everything查询失败，错误码: {errorCode}");
            }

            // 获取结果数量
            uint numResults = EverythingWrapper.Everything_GetNumResults();
            Dictionary<string, TreeNode> pathNodes = new Dictionary<string, TreeNode>();
            pathNodes[currentDrive.ToLower()] = rootNode;

			// 处理结果
			int j = 0;
            for (uint i = 0; i < numResults; i++)
            {
                if (EverythingWrapper.Everything_IsFolderResult(i))
                {
                    StringBuilder pathBuilder = new StringBuilder(260);
                    EverythingWrapper.Everything_GetResultFullPathName(i, pathBuilder, 260);
                    string fullPath = pathBuilder.ToString();

                    // 跳过根目录自身
                    if (string.Equals(fullPath, currentDrive, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 创建目录节点
                    if(CreateDirectoryNode(fullPath, pathNodes))
						j++;
                }
            }
			Debug.Print($"{j} nodes created.");
        }

        private bool CreateDirectoryNode(string fullPath, Dictionary<string, TreeNode> pathNodes)
        {
            string parentPath = Path.GetDirectoryName(fullPath);
            if (parentPath == null) return false;

            parentPath = parentPath.ToLower();
            string dirName = Path.GetFileName(fullPath);

            // 如果父路径节点不存在，先创建父路径节点
            if (!pathNodes.ContainsKey(parentPath))
            {
                CreateDirectoryNode(parentPath, pathNodes);
            }

            // 获取父节点并添加当前目录节点
            if (pathNodes.TryGetValue(parentPath, out TreeNode parentNode))
            {
                TreeNode dirNode = new TreeNode(dirName)
                {
                    Tag = fullPath
                };
                parentNode.Nodes.Add(dirNode);
                pathNodes[fullPath.ToLower()] = dirNode;
                directoryNodes[fullPath.ToLower()] = dirNode;
            }
			return true;
        }

        private void LoadDirectoriesRecursively(TreeNode parentNode, string path, int depth = 0)
        {
            // 限制递归深度，避免过深的递归
            if (depth > 5) return;

            try
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    try
                    {
                        // 跳过隐藏文件夹
                        if ((File.GetAttributes(directory) & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        string dirName = Path.GetFileName(directory);
                        TreeNode dirNode = new TreeNode(dirName)
                        {
                            Tag = directory
                        };
                        parentNode.Nodes.Add(dirNode);
                        directoryNodes[directory.ToLower()] = dirNode;

                        // 递归加载子目录
                        LoadDirectoriesRecursively(dirNode, directory, depth + 1);
                    }
                    catch (UnauthorizedAccessException)
                    {
						Debug.Print(" 忽略无权限访问的目录");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
				Debug.Print(" 忽略无权限访问的目录");
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
			// 清除上一次的高亮显示
			if (lastHighlightedNode != null)
			{
				lastHighlightedNode.ForeColor = originalNodeForeColor;
				lastHighlightedNode.BackColor = Color.Transparent;
			}

			string searchText = searchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 如果搜索框为空，恢复所有节点
                ResetTreeView();
                return;
            }

            // 搜索匹配的目录
            matchingNodes = new List<TreeNode>();
            foreach (var kvp in directoryNodes)
            {
                string path = kvp.Key;
                TreeNode node = kvp.Value;

                if (path.Contains(searchText) || node.Text.ToLower().Contains(searchText))
                {
                    matchingNodes.Add(node);
                }
            }

            // 如果找到匹配项，选择第一个并确保可见
            if (matchingNodes.Count > 0)
            {
                TreeNode firstMatch = matchingNodes[0];
                treeView.SelectedNode = firstMatch;
                firstMatch.EnsureVisible();

				// 高亮显示匹配节点
				firstMatch.ForeColor = Color.White;
				firstMatch.BackColor = SystemColors.Highlight;
				lastHighlightedNode = firstMatch;
			}
			idx = 0;
        }

        private void ResetTreeView()
        {
            // 恢复树视图的默认状态
            treeView.SelectedNode = treeView.Nodes[0];
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadDirectoryTree();
        }
		private void NextButton_Click(Object sender, EventArgs e)
		{
			if (matchingNodes.Count > 0)
			{
				// 清除上一次的高亮显示
				if (lastHighlightedNode != null)
				{
					lastHighlightedNode.ForeColor = originalNodeForeColor;
					lastHighlightedNode.BackColor = Color.Transparent;
				}
				idx++;
				idx %= matchingNodes.Count;
				TreeNode firstMatch = matchingNodes[idx];
				treeView.SelectedNode = firstMatch;
				firstMatch.EnsureVisible();
				// 高亮显示当前匹配节点
				firstMatch.ForeColor = Color.White;
				firstMatch.BackColor = SystemColors.Highlight;
				lastHighlightedNode = firstMatch;
				treeView.Refresh();
			}
		}
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 可以在这里添加选择节点后的逻辑
        }

        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // 双击节点时直接确认选择
            if (e.Node != null)
            {
                NavigateToSelectedDirectory();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            NavigateToSelectedDirectory();
        }

        private void NavigateToSelectedDirectory()
        {
            if (treeView.SelectedNode != null)
            {
                string selectedPath = treeView.SelectedNode.Tag as string;
                if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                {
                    // 导航到选定的目录
                    ownerForm.NavigateToPath(selectedPath);
                }
            }
        }
	}
}