using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Zfile.Forms
{
    public partial class IdmForm : Form
    {
        private List<DownloadTask> downloadTasks = new List<DownloadTask>();
        private CancellationTokenSource cancellationTokenSource;
        private bool isClosing = false;
		private ToolStripMenuItem torrentDetailMenuItem;

		public IdmForm()
        {
            InitializeComponent();
            InitializeDownloadList();
            InitializeTorrentEngine();
        }
        
        /// <summary>
        /// 初始化种子下载引擎
        /// </summary>
        private async void InitializeTorrentEngine()
        {
            try
            {
                await TorrentMgr.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化BT下载引擎失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region UI初始化

        private void InitializeComponent()
        {
            this.menuStrip = new MenuStrip();
            this.taskMenuItem = new ToolStripMenuItem();
            this.fileMenuItem = new ToolStripMenuItem();
            this.downloadMenuItem = new ToolStripMenuItem();
            this.viewMenuItem = new ToolStripMenuItem();
            this.helpMenuItem = new ToolStripMenuItem();
            this.toolStrip = new ToolStrip();
            this.newTaskButton = new ToolStripButton();
            this.resumeButton = new ToolStripButton();
            this.pauseButton = new ToolStripButton();
            this.stopAllButton = new ToolStripButton();
            this.resumeAllButton = new ToolStripButton();
            this.deleteTaskButton = new ToolStripButton();
            this.deleteAllButton = new ToolStripButton();
            this.scheduleButton = new ToolStripButton();
            this.startQueueButton = new ToolStripButton();
            this.stopQueueButton = new ToolStripButton();
            this.optionsButton = new ToolStripButton();
            this.splitContainer = new SplitContainer();
            this.categoryTreeView = new TreeView();
            this.downloadListView = new ListView();
            this.statusStrip = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            this.progressBar = new ToolStripProgressBar();
            
            // 初始化右键菜单
            this.downloadListContextMenu = new ContextMenuStrip();
            this.openMenuItem = new ToolStripMenuItem();
            this.openWithMenuItem = new ToolStripMenuItem();
            this.openFolderMenuItem = new ToolStripMenuItem();
            this.moveRenameMenuItem = new ToolStripMenuItem();
            this.redownloadMenuItem = new ToolStripMenuItem();
            this.resumeDownloadMenuItem = new ToolStripMenuItem();
            this.stopDownloadMenuItem = new ToolStripMenuItem();
            this.copyUrlMenuItem = new ToolStripMenuItem();
            this.removeMenuItem = new ToolStripMenuItem();
            this.moveToQueueMenuItem = new ToolStripMenuItem();
            this.removeFromQueueMenuItem = new ToolStripMenuItem();
            this.backupMenuItem = new ToolStripMenuItem();
            this.propertiesMenuItem = new ToolStripMenuItem();
						// 在右键菜单项声明部分添加

			// 在初始化右键菜单部分添加
			this.torrentDetailMenuItem = new ToolStripMenuItem();
			this.torrentDetailMenuItem.Text = "种子下载详细信息";
			this.torrentDetailMenuItem.Click += TorrentDetailMenuItem_Click;
            // 配置右键菜单
            this.downloadListContextMenu.Items.AddRange(new ToolStripItem[] {
                this.openMenuItem,
                this.openWithMenuItem,
                this.openFolderMenuItem,
                new ToolStripSeparator(),
                this.moveRenameMenuItem,
                new ToolStripSeparator(),
                this.redownloadMenuItem,
                this.resumeDownloadMenuItem,
                this.stopDownloadMenuItem,
                new ToolStripSeparator(),
                this.copyUrlMenuItem,
                new ToolStripSeparator(),
                this.removeMenuItem,
                this.moveToQueueMenuItem,
                this.removeFromQueueMenuItem,
                new ToolStripSeparator(),
                this.backupMenuItem,
				new ToolStripSeparator(),
				this.torrentDetailMenuItem, // 添加新菜单项
                new ToolStripSeparator(),
                this.propertiesMenuItem
            });

            // 设置菜单项文本和事件
            this.openMenuItem.Text = "打开";
            this.openMenuItem.Click += OpenMenuItem_Click;
            
            this.openWithMenuItem.Text = "打开方式...";
            this.openWithMenuItem.Click += OpenWithMenuItem_Click;
            
            this.openFolderMenuItem.Text = "打开文件夹";
            this.openFolderMenuItem.Click += OpenFolderMenuItem_Click;
            
            this.moveRenameMenuItem.Text = "移动/重命名 (Ctrl+M)";
            this.moveRenameMenuItem.Click += MoveRenameMenuItem_Click;
            
            this.redownloadMenuItem.Text = "重新下载";
            this.redownloadMenuItem.Click += RedownloadMenuItem_Click;
            
            this.resumeDownloadMenuItem.Text = "继续下载";
            this.resumeDownloadMenuItem.Click += ResumeDownloadMenuItem_Click;
            
            this.stopDownloadMenuItem.Text = "停止下载";
            this.stopDownloadMenuItem.Click += StopDownloadMenuItem_Click;
            
            this.copyUrlMenuItem.Text = "复制下载地址";
            this.copyUrlMenuItem.Click += CopyUrlMenuItem_Click;
            
            this.removeMenuItem.Text = "移除";
            this.removeMenuItem.Click += RemoveMenuItem_Click;
            
            this.moveToQueueMenuItem.Text = "移动到队列";
            this.moveToQueueMenuItem.Click += MoveToQueueMenuItem_Click;
            
            this.removeFromQueueMenuItem.Text = "从队列中删除";
            this.removeFromQueueMenuItem.Click += RemoveFromQueueMenuItem_Click;
            
            this.backupMenuItem.Text = "备份";
            this.backupMenuItem.Click += BackupMenuItem_Click;
            
            this.propertiesMenuItem.Text = "属性";
            this.propertiesMenuItem.Click += PropertiesMenuItem_Click;

            // MenuStrip
            this.menuStrip.Items.AddRange(new ToolStripItem[] {
                this.taskMenuItem,
                this.fileMenuItem,
                this.downloadMenuItem,
                this.viewMenuItem,
                this.helpMenuItem
            });
            this.menuStrip.Location = new Point(0, 0);
            this.menuStrip.Size = new Size(800, 24);

            // 任务菜单
            this.taskMenuItem.Text = "任务";
            this.taskMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("新建任务", null, NewTask_Click),
                new ToolStripMenuItem("新建磁力链接下载", null, NewMagnetTask_Click),
                new ToolStripMenuItem("新建种子文件下载", null, NewTorrentTask_Click),
                new ToolStripSeparator(),
                new ToolStripMenuItem("退出", null, (s, e) => Close())
            });

            // 文件菜单
            this.fileMenuItem.Text = "文件";

            // 下载菜单
            this.downloadMenuItem.Text = "下载";
            this.downloadMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("开始", null, Resume_Click),
                new ToolStripMenuItem("暂停", null, Pause_Click),
                new ToolStripSeparator(),
                new ToolStripMenuItem("全部开始", null, ResumeAll_Click),
                new ToolStripMenuItem("全部暂停", null, StopAll_Click)
            });

            // 查看菜单
            this.viewMenuItem.Text = "查看";

            // 帮助菜单
            this.helpMenuItem.Text = "帮助";
            this.helpMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("关于", null, (s, e) => MessageBox.Show("IDM下载管理器 v1.0", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information))
            });

            // ToolStrip
            this.toolStrip.Items.AddRange(new ToolStripItem[] {
                this.newTaskButton,
                new ToolStripSeparator(),
                this.resumeButton,
                this.pauseButton,
                new ToolStripSeparator(),
                this.stopAllButton,
                this.resumeAllButton,
                new ToolStripSeparator(),
                this.deleteTaskButton,
                this.deleteAllButton,
                new ToolStripSeparator(),
                this.startQueueButton,
                this.stopQueueButton,
                new ToolStripSeparator(),
                this.optionsButton
            });
            this.toolStrip.Location = new Point(0, 24);
            this.toolStrip.Size = new Size(800, 25);

            // 工具栏按钮
            this.newTaskButton.Text = "新建任务";
            this.newTaskButton.ToolTipText = "新建下载任务";
            this.newTaskButton.Click += NewTask_Click;

            this.resumeButton.Text = "继续";
            this.resumeButton.ToolTipText = "继续选中的下载任务";
            this.resumeButton.Click += Resume_Click;

            this.pauseButton.Text = "暂停";
            this.pauseButton.ToolTipText = "暂停选中的下载任务";
            this.pauseButton.Click += Pause_Click;

            this.stopAllButton.Text = "全部暂停";
            this.stopAllButton.ToolTipText = "暂停所有下载任务";
            this.stopAllButton.Click += StopAll_Click;

            this.resumeAllButton.Text = "全部继续";
            this.resumeAllButton.ToolTipText = "继续所有下载任务";
            this.resumeAllButton.Click += ResumeAll_Click;

            this.deleteTaskButton.Text = "删除任务";
            this.deleteTaskButton.ToolTipText = "删除选中的下载任务";
            this.deleteTaskButton.Click += DeleteTask_Click;

            this.deleteAllButton.Text = "删除全部";
            this.deleteAllButton.ToolTipText = "删除所有下载任务";
            this.deleteAllButton.Click += DeleteAll_Click;

            this.startQueueButton.Text = "开始队列";
            this.startQueueButton.ToolTipText = "开始下载队列";
            this.startQueueButton.Click += StartQueue_Click;

            this.stopQueueButton.Text = "停止队列";
            this.stopQueueButton.ToolTipText = "停止下载队列";
            this.stopQueueButton.Click += StopQueue_Click;

            this.optionsButton.Text = "选项";
            this.optionsButton.ToolTipText = "设置下载选项";
            this.optionsButton.Click += Options_Click;

            // SplitContainer
            this.splitContainer.Dock = DockStyle.Fill;
            this.splitContainer.Location = new Point(0, 49);
            this.splitContainer.Size = new Size(800, 379);
            this.splitContainer.SplitterDistance = 200;
            this.splitContainer.Panel1.Controls.Add(this.categoryTreeView);
            this.splitContainer.Panel2.Controls.Add(this.downloadListView);

            // CategoryTreeView
            this.categoryTreeView.Dock = DockStyle.Fill;
            this.categoryTreeView.Location = new Point(0, 0);
            this.categoryTreeView.Size = new Size(200, 379);
            this.categoryTreeView.AfterSelect += CategoryTreeView_AfterSelect;

            // 初始化分类树
            TreeNode allFilesNode = new TreeNode("全部任务");
            allFilesNode.Nodes.Add(new TreeNode("压缩文件"));
            allFilesNode.Nodes.Add(new TreeNode("文档"));
            allFilesNode.Nodes.Add(new TreeNode("音乐"));
            allFilesNode.Nodes.Add(new TreeNode("程序"));
            allFilesNode.Nodes.Add(new TreeNode("视频"));
            this.categoryTreeView.Nodes.Add(allFilesNode);
            this.categoryTreeView.Nodes.Add(new TreeNode("未完成"));
            this.categoryTreeView.Nodes.Add(new TreeNode("已完成"));
            this.categoryTreeView.Nodes.Add(new TreeNode("站点抓取队列"));
            this.categoryTreeView.Nodes.Add(new TreeNode("队列"));
            this.categoryTreeView.ExpandAll();
            this.categoryTreeView.SelectedNode = allFilesNode;

            // DownloadListView
            this.downloadListView.Dock = DockStyle.Fill;
            this.downloadListView.Location = new Point(0, 0);
            this.downloadListView.Size = new Size(596, 379);
            this.downloadListView.View = View.Details;
            this.downloadListView.FullRowSelect = true;
            this.downloadListView.GridLines = true;
            this.downloadListView.MultiSelect = true;
            this.downloadListView.ContextMenuStrip = this.downloadListContextMenu;
            this.downloadListView.MouseUp += DownloadListView_MouseUp;

            // StatusStrip
            this.statusStrip.Items.AddRange(new ToolStripItem[] {
                this.statusLabel,
                this.progressBar
            });
            this.statusStrip.Location = new Point(0, 428);
            this.statusStrip.Size = new Size(800, 22);

            this.statusLabel.Text = "就绪";
            this.progressBar.Visible = false;

            // Form
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.statusStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Size = new Size(800, 450);
            this.Text = "IDM下载管理器";
            this.FormClosing += IdmForm_FormClosing;
        }

        private void InitializeDownloadList()
        {
            // 添加列标题
            this.downloadListView.Columns.Add("文件名", 200);
            this.downloadListView.Columns.Add("大小", 80);
            this.downloadListView.Columns.Add("状态", 80);
            this.downloadListView.Columns.Add("制作时间", 120);
            this.downloadListView.Columns.Add("传输速度", 100);
            this.downloadListView.Columns.Add("最高速度", 100);
            this.downloadListView.Columns.Add("速度", 80);
        }

		#endregion

		#region 事件处理
		private void TorrentDetailMenuItem_Click(object sender, EventArgs e)
		{
			if (downloadListView.SelectedItems.Count > 0)
			{
				var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
				if (task is TorrentDownloadTask torrentTask && !string.IsNullOrEmpty(torrentTask.TorrentId))
				{
					// 创建并显示种子详细信息窗口
					var detailForm = new TorrentDetailForm(torrentTask.TorrentId);
					detailForm.Show();
				}
				else
				{
					MessageBox.Show("选中的不是种子下载任务", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}

		private async void IdmForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            // 保存下载任务状态
            SaveDownloadTasks();
            
            // 关闭种子下载引擎
            await TorrentMgr.ShutdownAsync();
        }

        private void CategoryTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 根据选择的分类筛选显示的下载任务
            FilterDownloadTasks(e.Node.Text);
        }

        private void NewTask_Click(object sender, EventArgs e)
        {
            using (var dialog = new NewDownloadDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AddDownloadTask(dialog.Url, dialog.SavePath, dialog.Chunks);
                }
            }
        }
        
        private void NewMagnetTask_Click(object sender, EventArgs e)
        {
            using (var dialog = new TorrentDownloadDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AddTorrentDownloadTask(dialog.MagnetLink, dialog.SavePath, true, dialog.SelectedFileIndices);
                }
            }
        }
        
        private void NewTorrentTask_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "种子文件 (*.torrent)|*.torrent|所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择种子文件";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var dialog = new TorrentDownloadDialog(openFileDialog.FileName, true))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            AddTorrentDownloadTask(dialog.TorrentFilePath, dialog.SavePath, true, dialog.SelectedFileIndices);
                        }
                    }
                }
            }
        }

        private void Resume_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in downloadListView.SelectedItems)
                {
                    var task = item.Tag as DownloadTask;
                    if (task != null && task.Status != DownloadStatus.Completed)
                    {
                        ResumeDownloadTask(task);
                    }
                }
            }
        }

        private void Pause_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in downloadListView.SelectedItems)
                {
                    var task = item.Tag as DownloadTask;
                    if (task != null && task.Status == DownloadStatus.Downloading)
                    {
                        PauseDownloadTask(task);
                    }
                }
            }
        }

        private void StopAll_Click(object sender, EventArgs e)
        {
            foreach (var task in downloadTasks)
            {
                if (task.Status == DownloadStatus.Downloading)
                {
                    PauseDownloadTask(task);
                }
            }
        }

        private void ResumeAll_Click(object sender, EventArgs e)
        {
            foreach (var task in downloadTasks)
            {
                if (task.Status != DownloadStatus.Completed)
                {
                    ResumeDownloadTask(task);
                }
            }
        }

        private void DeleteTask_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("确定要删除选中的下载任务吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    List<DownloadTask> tasksToRemove = new List<DownloadTask>();

                    foreach (ListViewItem item in downloadListView.SelectedItems)
                    {
                        var task = item.Tag as DownloadTask;
                        if (task != null)
                        {
                            PauseDownloadTask(task);
                            tasksToRemove.Add(task);
                        }
                    }

                    foreach (var task in tasksToRemove)
                    {
                        RemoveDownloadTask(task);
                    }

                    UpdateDownloadListView();
                }
            }
        }

        private void DeleteAll_Click(object sender, EventArgs e)
        {
            if (downloadTasks.Count > 0)
            {
                if (MessageBox.Show("确定要删除所有下载任务吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (var task in downloadTasks.ToList())
                    {
                        PauseDownloadTask(task);
                        RemoveDownloadTask(task);
                    }

                    downloadTasks.Clear();
                    UpdateDownloadListView();
                }
            }
        }

        private void StartQueue_Click(object sender, EventArgs e)
        {
            // 实现开始队列功能
            MessageBox.Show("开始下载队列", "队列管理", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StopQueue_Click(object sender, EventArgs e)
        {
            // 实现停止队列功能
            MessageBox.Show("停止下载队列", "队列管理", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Options_Click(object sender, EventArgs e)
        {
            // 实现选项设置功能
            MessageBox.Show("下载选项设置", "选项", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 下载任务管理

        /// <summary>
        /// 添加新的种子下载任务
        /// </summary>
        /// <param name="torrentPathOrMagnet">种子文件路径或磁力链接</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="isMagnetLink">是否是磁力链接</param>
        /// <param name="selectedFileIndices">选中的文件索引</param>
        private async void AddTorrentDownloadTask(string torrentPathOrMagnet, string savePath, bool isMagnetLink, List<int> selectedFileIndices = null)
        {
            try
            {
                // 创建种子下载任务
                var task = new TorrentDownloadTask(torrentPathOrMagnet, savePath, isMagnetLink)
                {
                    SelectedFileIndices = selectedFileIndices ?? new List<int>()
                };
                
                // 添加到下载任务列表
                downloadTasks.Add(task);
                
                // 更新UI
                UpdateDownloadListView();
                
                // 开始下载
                await StartTorrentDownloadAsync(task);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加种子下载任务失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 开始种子下载任务
        /// </summary>
        private async Task StartTorrentDownloadAsync(TorrentDownloadTask task)
        {
            try
            {
                // 设置状态为下载中
                task.Status = DownloadStatus.Downloading;
                UpdateTaskUI(task);
                
                // 根据类型启动下载
                string torrentId;
                if (task.IsMagnetLink)
                {
                    // 磁力链接下载
                    torrentId = await TorrentMgr.AddMagnetLinkAsync(
                        task.Url, 
                        task.SavePath, 
                        task.CancellationTokenSource.Token,
                        (progress, speed, totalSize, chunksProgress) =>
                        {
                            if (!isClosing)
                            {
                                task.Progress = progress;
                                task.Speed = speed;
                                task.TotalSize = totalSize;
                                task.MaxSpeed = Math.Max(task.MaxSpeed, speed);
                                
                                // 获取种子信息
                                task.TorrentInfo = TorrentMgr.GetTorrentInfo(task.TorrentId);
                                if (task.TorrentInfo != null)
                                {
                                    task.UpdateFromTorrentInfo();
                                }
                                
                                // 更新UI
                                UpdateTaskUI(task);
                            }
                        });
                }
                else
                {
                    // 种子文件下载
                    torrentId = await TorrentMgr.AddTorrentFileAsync(
                        task.TorrentFilePath, 
                        task.SavePath, 
                        task.CancellationTokenSource.Token,
                        (progress, speed, totalSize, chunksProgress) =>
                        {
                            if (!isClosing)
                            {
                                task.Progress = progress;
                                task.Speed = speed;
                                task.TotalSize = totalSize;
                                task.MaxSpeed = Math.Max(task.MaxSpeed, speed);
                                
                                // 获取种子信息
                                task.TorrentInfo = TorrentMgr.GetTorrentInfo(task.TorrentId);
                                if (task.TorrentInfo != null)
                                {
                                    task.UpdateFromTorrentInfo();
                                }
                                
                                // 更新UI
                                UpdateTaskUI(task);
                            }
                        });
                }
                
                // 保存种子ID
                task.TorrentId = torrentId;
            }
            catch (OperationCanceledException)
            {
                task.Status = DownloadStatus.Paused;
                UpdateTaskUI(task);
            }
            catch (Exception ex)
            {
                task.Status = DownloadStatus.Error;
                task.ErrorMessage = ex.Message;
                UpdateTaskUI(task);
                
                MessageBox.Show($"种子下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 添加新的下载任务到下载对话框中
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径，可以为null</param>
        /// <param name="headers">HTTP请求头，可以为null</param>
        /// <param name="cookies">Cookies，可以为null</param>
        /// <param name="referrer">引用页，可以为null</param>
        public void AddNewDownload(string url, string savePath = null, Dictionary<string, string> headers = null, string cookies = null, string referrer = null)
        {
            try
            {
                Debug.WriteLine($"预填充下载信息: URL={url}, SavePath={savePath}, Headers={headers?.Count ?? 0}, Cookies={(cookies != null)}, Referrer={(referrer != null)}");
                
                // 如果URL为空，则不执行任何操作
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }
                
                // 如果保存路径为空，则尝试从URL中获取文件名
                if (string.IsNullOrEmpty(savePath))
                {
                    try
                    {
                        string fileName = Path.GetFileName(new Uri(url).LocalPath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // 使用默认下载目录
                            string downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            savePath = Path.Combine(downloadFolder, fileName);
                        }
                    }
                    catch
                    {
                        // 忽略URL解析错误
                    }
                }
                
                // 预填充URL和保存路径到界面控件
                //if (this.urlTextBox != null)
                //{
                //    this.urlTextBox.Text = url;
                //}
                
                //if (this.savePathTextBox != null && !string.IsNullOrEmpty(savePath))
                //{
                //    this.savePathTextBox.Text = savePath;
                //}
                
                // 如果有HTTP头或Cookies，可以在这里处理
                // 这里可以添加额外的UI元素来显示HTTP头和Cookies
                
                // 将信息存储到表单的Tag属性中，以便后续使用
                var downloadInfo = new Dictionary<string, object>
                {
                    { "url", url },
                    { "savePath", savePath },
                    { "headers", headers },
                    { "cookies", cookies },
                    { "referrer", referrer }
                };
                
                this.Tag = downloadInfo;
                
                Debug.WriteLine("下载信息已预填充到对话框");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"预填充下载信息失败: {ex.Message}");
                MessageBox.Show($"预填充下载信息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddDownloadTask(string url, string savePath, int chunks = 4)
        {
            var task = new DownloadTask
            {
                Url = url,
                SavePath = savePath,
                FileName = Path.GetFileName(savePath),
                Chunks = chunks,
                Status = DownloadStatus.Pending,
                CreatedTime = DateTime.Now
            };

            downloadTasks.Add(task);
            UpdateDownloadListView();
            ResumeDownloadTask(task);
        }

        private async void RemoveDownloadTask(DownloadTask task)
        {
            // 停止下载
            PauseDownloadTask(task);

            if (task is TorrentDownloadTask torrentTask && !string.IsNullOrEmpty(torrentTask.TorrentId))
            {
                // 移除种子下载任务
                await TorrentMgr.RemoveTorrentAsync(torrentTask.TorrentId, false);
            }
            else
            {
                // 删除普通下载的临时文件
                string tempFile = Path.ChangeExtension(task.SavePath, ".tmp");
                string progressFile = tempFile + ".progress";

                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);

                    if (File.Exists(progressFile))
                        File.Delete(progressFile);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"删除临时文件失败: {ex.Message}");
                }
            }

            downloadTasks.Remove(task);
        }

        private ProgressDialog progressDialog = null;

        private async void ResumeDownloadTask(DownloadTask task)
        {
            if (task.Status == DownloadStatus.Downloading)
                return;

            if (task is TorrentDownloadTask torrentTask && !string.IsNullOrEmpty(torrentTask.TorrentId))
            {
                // 恢复种子下载
                task.Status = DownloadStatus.Downloading;
                UpdateTaskUI(task);
                await TorrentMgr.ResumeTorrentAsync(torrentTask.TorrentId);
                return;
            }
            
            // 普通下载任务处理
            task.Status = DownloadStatus.Downloading;
            task.CancellationTokenSource = new CancellationTokenSource();
            UpdateDownloadListView();

            // 创建并显示进度窗口
            if (progressDialog != null)
            {
                progressDialog.Close();
                progressDialog.Dispose();
            }
            
            progressDialog = new ProgressDialog(task.Url, task.SavePath, task.Chunks, task.CancellationTokenSource);
            progressDialog.DownloadCompleted += (sender, e) => {
                // 如果是暂停状态，则更新UI
                if (task.Status == DownloadStatus.Paused)
                {
                    UpdateTaskUI(task);
                }
            };
            progressDialog.Show();

            try
            {
                await Task.Run(async () =>
                {
                    await IdmManager.StartDownloadWithProgress(task.Url, task.SavePath, task.Chunks, task.CancellationTokenSource.Token,
                        (progress, speed, totalSize, chunkProgress) =>
                        {
                            if (!isClosing)
                            {
                                task.Progress = progress;
                                task.Speed = speed;
                                task.TotalSize = totalSize;
                                task.MaxSpeed = Math.Max(task.MaxSpeed, speed);

                                // 更新UI
                                UpdateTaskUI(task);
                                
                                // 更新进度窗口
                                if (progressDialog != null && !progressDialog.IsDisposed)
                                {
                                    progressDialog.UpdateProgress(progress, speed, totalSize, chunkProgress);
                                }
                            }
                        });

                    if (!task.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        task.Status = DownloadStatus.Completed;
                        task.Progress = 100;
                        UpdateTaskUI(task);
                        
                        // 更新进度窗口为完成状态
                        if (progressDialog != null && !progressDialog.IsDisposed)
                        {
                            progressDialog.SetCompleted();
                        }
                    }
                }, task.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                task.Status = DownloadStatus.Paused;
                UpdateTaskUI(task);
            }
            catch (Exception ex)
            {
                task.Status = DownloadStatus.Error;
                task.ErrorMessage = ex.Message;
                UpdateTaskUI(task);
                
                // 更新进度窗口为错误状态
                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    progressDialog.SetError(ex.Message);
                }
                
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void PauseDownloadTask(DownloadTask task)
        {
            if (task.Status != DownloadStatus.Downloading)
                return;

            if (task is TorrentDownloadTask torrentTask && !string.IsNullOrEmpty(torrentTask.TorrentId))
            {
                // 暂停种子下载
                await TorrentMgr.PauseTorrentAsync(torrentTask.TorrentId);
                task.Status = DownloadStatus.Paused;
            }
            else
            {
                // 暂停普通下载
                task.CancellationTokenSource?.Cancel();
                task.Status = DownloadStatus.Paused;
            }
            
            UpdateTaskUI(task);
        }

        private void UpdateTaskUI(DownloadTask task)
        {
            if (isClosing) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateTaskUI(task)));
                return;
            }

            foreach (ListViewItem item in downloadListView.Items)
            {
                var itemTask = item.Tag as DownloadTask;
                if (itemTask != null && itemTask == task)
                {
                    // 更新列表项
                    item.SubItems[1].Text = FormatFileSize(task.TotalSize);
                    item.SubItems[2].Text = GetStatusText(task.Status);
                    item.SubItems[4].Text = FormatSpeed(task.Speed);
                    item.SubItems[5].Text = FormatSpeed(task.MaxSpeed);
                    item.SubItems[6].Text = $"{task.Progress:F1}%";
                    
                    // 如果是种子下载任务，更新额外信息
                    if (task is TorrentDownloadTask torrentTask)
                    {
                        // 在工具提示中显示做种/下载用户数量等
                        item.ToolTipText = $"做种: {torrentTask.Seeds}, 下载: {torrentTask.Leechs}, 连接: {torrentTask.Peers}";
                    }
                    
                    break;
                }
            }
        }

        private void UpdateDownloadListView()
        {
            if (isClosing) return;

            if (InvokeRequired)
            {
                Invoke(new Action(UpdateDownloadListView));
                return;
            }

            string selectedCategory = categoryTreeView.SelectedNode?.Text ?? "全部任务";
            FilterDownloadTasks(selectedCategory);
        }

        private void FilterDownloadTasks(string category)
        {
            if (isClosing) return;

            downloadListView.Items.Clear();

            var filteredTasks = downloadTasks;

            // 根据分类筛选
            switch (category)
            {
                case "未完成":
                    filteredTasks = downloadTasks.Where(t => t.Status != DownloadStatus.Completed).ToList();
                    break;
                case "已完成":
                    filteredTasks = downloadTasks.Where(t => t.Status == DownloadStatus.Completed).ToList();
                    break;
                case "压缩文件":
                case "文档":
                case "音乐":
                case "程序":
                case "视频":
                    filteredTasks = downloadTasks.Where(t => GetFileCategory(t.FileName) == category).ToList();
                    break;
            }

            foreach (var task in filteredTasks)
            {
                var item = new ListViewItem(task.FileName);
                item.SubItems.Add(FormatFileSize(task.TotalSize));
                item.SubItems.Add(GetStatusText(task.Status));
                item.SubItems.Add(task.CreatedTime.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(FormatSpeed(task.Speed));
                item.SubItems.Add(FormatSpeed(task.MaxSpeed));
                item.SubItems.Add($"{task.Progress:F1}%");
                item.Tag = task;

                downloadListView.Items.Add(item);
            }
        }

        private string GetFileCategory(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();

            if (new[] { ".zip", ".rar", ".7z", ".tar", ".gz" }.Contains(ext))
                return "压缩文件";
            else if (new[] { ".doc", ".docx", ".pdf", ".txt", ".xls", ".xlsx", ".ppt", ".pptx" }.Contains(ext))
                return "文档";
            else if (new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg" }.Contains(ext))
                return "音乐";
            else if (new[] { ".exe", ".msi", ".dll", ".bat", ".cmd" }.Contains(ext))
                return "程序";
            else if (new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv" }.Contains(ext))
                return "视频";
            else
                return "全部任务";
        }

        private string GetStatusText(DownloadStatus status)
        {
            switch (status)
            {
                case DownloadStatus.Pending: return "等待中";
                case DownloadStatus.Downloading: return "下载中";
                case DownloadStatus.Paused: return "已暂停";
                case DownloadStatus.Completed: return "已完成";
                case DownloadStatus.Error: return "错误";
                case DownloadStatus.Metadata: return "获取元数据";
                case DownloadStatus.Hashing: return "校验文件";
                default: return "未知";
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 0) return "未知";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F2} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0) return "--";
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F2} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
        }

        private async void SaveDownloadTasks()
        {
            // 保存下载任务到配置文件，这里简化处理
            Debug.WriteLine($"保存了 {downloadTasks.Count} 个下载任务");
        }

        #endregion

        #region 右键菜单事件处理

        private void DownloadListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // 获取右键点击位置的项
                ListViewItem clickedItem = downloadListView.GetItemAt(e.X, e.Y);
                
                // 如果点击的位置没有项，则不显示菜单
                if (clickedItem == null)
                {
                    return;
                }

                // 如果点击的项未被选中，则选中该项
                if (!clickedItem.Selected)
                {
                    foreach (ListViewItem item in downloadListView.SelectedItems)
                    {
                        item.Selected = false;
                    }
                    clickedItem.Selected = true;
                }

                // 根据选中项的状态启用或禁用菜单项
                UpdateContextMenuItems();
            }
        }

        private void UpdateContextMenuItems()
        {
            bool hasSelectedItems = downloadListView.SelectedItems.Count > 0;
            bool hasCompletedItems = false;
            bool hasDownloadingItems = false;
            bool hasPausedItems = false;
			bool hasTorrentTask = false;
			if (hasSelectedItems)
            {
                foreach (ListViewItem item in downloadListView.SelectedItems)
                {
                    var task = item.Tag as DownloadTask;
                    if (task != null)
                    {
                        if (task.Status == DownloadStatus.Completed)
                            hasCompletedItems = true;
                        else if (task.Status == DownloadStatus.Downloading)
                            hasDownloadingItems = true;
                        else if (task.Status == DownloadStatus.Paused || task.Status == DownloadStatus.Pending || task.Status == DownloadStatus.Error)
                            hasPausedItems = true;
						// 检查是否是种子下载任务
						if (task is TorrentDownloadTask torrentTask && !string.IsNullOrEmpty(torrentTask.TorrentId))
						{
							hasTorrentTask = true;
						}
					}
                }
            }

            // 启用或禁用菜单项
            openMenuItem.Enabled = hasCompletedItems;
            openWithMenuItem.Enabled = hasCompletedItems;
            openFolderMenuItem.Enabled = hasSelectedItems;
            moveRenameMenuItem.Enabled = hasCompletedItems;
            redownloadMenuItem.Enabled = hasSelectedItems;
            resumeDownloadMenuItem.Enabled = hasPausedItems;
            stopDownloadMenuItem.Enabled = hasDownloadingItems;
            copyUrlMenuItem.Enabled = hasSelectedItems;
            removeMenuItem.Enabled = hasSelectedItems;
            moveToQueueMenuItem.Enabled = hasSelectedItems;
            removeFromQueueMenuItem.Enabled = hasSelectedItems;
            backupMenuItem.Enabled = hasCompletedItems;
            propertiesMenuItem.Enabled = hasSelectedItems;
			// 启用或禁用种子详细信息菜单项
			torrentDetailMenuItem.Enabled = hasTorrentTask;
		}

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in downloadListView.SelectedItems)
            {
                var task = item.Tag as DownloadTask;
                if (task != null && task.Status == DownloadStatus.Completed && File.Exists(task.SavePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = task.SavePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenWithMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
                if (task != null && task.Status == DownloadStatus.Completed && File.Exists(task.SavePath))
                {
                    try
                    {
                        // 打开"打开方式"对话框
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "rundll32.exe",
                            Arguments = $"shell32.dll,OpenAs_RunDLL {task.SavePath}",
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(processInfo);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开'打开方式'对话框: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenFolderMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
                if (task != null)
                {
                    try
                    {
                        string folderPath = Path.GetDirectoryName(task.SavePath);
                        if (Directory.Exists(folderPath))
                        {
                            // 打开文件夹并选中文件
                            if (File.Exists(task.SavePath))
                            {
                                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{task.SavePath}\"");
                            }
                            else
                            {
                                System.Diagnostics.Process.Start("explorer.exe", folderPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件夹: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void MoveRenameMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
                if (task != null && task.Status == DownloadStatus.Completed && File.Exists(task.SavePath))
                {
                    using (SaveFileDialog dialog = new SaveFileDialog())
                    {
                        dialog.FileName = Path.GetFileName(task.SavePath);
                        dialog.Filter = "所有文件 (*.*)|*.*";
                        dialog.Title = "移动/重命名文件";
                        dialog.InitialDirectory = Path.GetDirectoryName(task.SavePath);

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                // 如果目标文件已存在，则询问是否覆盖
                                if (File.Exists(dialog.FileName) && dialog.FileName != task.SavePath)
                                {
                                    if (MessageBox.Show($"文件 {dialog.FileName} 已存在，是否覆盖？", "确认覆盖", 
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                    {
                                        return;
                                    }
                                }

                                // 移动/重命名文件
                                File.Move(task.SavePath, dialog.FileName, true);
                                
                                // 更新任务信息
                                task.SavePath = dialog.FileName;
                                task.FileName = Path.GetFileName(dialog.FileName);
                                
                                // 更新UI
                                downloadListView.SelectedItems[0].Text = task.FileName;
                                
                                MessageBox.Show("文件已成功移动/重命名", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"移动/重命名文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }

        private void RedownloadMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("确定要重新下载选中的任务吗？", "确认重新下载", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (ListViewItem item in downloadListView.SelectedItems)
                    {
                        var task = item.Tag as DownloadTask;
                        if (task != null)
                        {
                            // 停止当前下载
                            PauseDownloadTask(task);
                            
                            // 删除临时文件
                            string tempFile = Path.ChangeExtension(task.SavePath, ".tmp");
                            string progressFile = tempFile + ".progress";
                            try
                            {
                                if (File.Exists(tempFile))
                                    File.Delete(tempFile);
                                if (File.Exists(progressFile))
                                    File.Delete(progressFile);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"删除临时文件失败: {ex.Message}");
                            }
                            
                            // 重置任务状态
                            task.Progress = 0;
                            task.Speed = 0;
                            task.MaxSpeed = 0;
                            task.Status = DownloadStatus.Pending;
                            
                            // 重新开始下载
                            ResumeDownloadTask(task);
                        }
                    }
                }
            }
        }

        private void ResumeDownloadMenuItem_Click(object sender, EventArgs e)
        {
            Resume_Click(sender, e);
        }

        private void StopDownloadMenuItem_Click(object sender, EventArgs e)
        {
            Pause_Click(sender, e);
        }

        private void CopyUrlMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
                if (task != null && !string.IsNullOrEmpty(task.Url))
                {
                    try
                    {
                        Clipboard.SetText(task.Url);
                        statusLabel.Text = "下载地址已复制到剪贴板";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"复制下载地址失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RemoveMenuItem_Click(object sender, EventArgs e)
        {
            DeleteTask_Click(sender, e);
        }

        private void MoveToQueueMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                // 这里简化处理，实际应该有队列管理逻辑
                MessageBox.Show("已将选中任务移动到队列", "队列管理", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RemoveFromQueueMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                // 这里简化处理，实际应该有队列管理逻辑
                MessageBox.Show("已将选中任务从队列中删除", "队列管理", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BackupMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in downloadListView.SelectedItems)
                {
                    var task = item.Tag as DownloadTask;
                    if (task != null && task.Status == DownloadStatus.Completed && File.Exists(task.SavePath))
                    {
                        using (SaveFileDialog dialog = new SaveFileDialog())
                        {
                            dialog.FileName = Path.GetFileName(task.SavePath) + ".bak";
                            dialog.Filter = "备份文件 (*.bak)|*.bak|所有文件 (*.*)|*.*";
                            dialog.Title = "备份文件";

                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                try
                                {
                                    File.Copy(task.SavePath, dialog.FileName, true);
                                    MessageBox.Show("文件已成功备份", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"备份文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PropertiesMenuItem_Click(object sender, EventArgs e)
        {
            if (downloadListView.SelectedItems.Count > 0)
            {
                var task = downloadListView.SelectedItems[0].Tag as DownloadTask;
                if (task != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"文件名: {task.FileName}");
                    sb.AppendLine($"下载地址: {task.Url}");
                    sb.AppendLine($"保存路径: {task.SavePath}");
                    sb.AppendLine($"文件大小: {FormatFileSize(task.TotalSize)}");
                    sb.AppendLine($"状态: {GetStatusText(task.Status)}");
                    sb.AppendLine($"进度: {task.Progress:F1}%");
                    sb.AppendLine($"当前速度: {FormatSpeed(task.Speed)}");
                    sb.AppendLine($"最高速度: {FormatSpeed(task.MaxSpeed)}");
                    sb.AppendLine($"创建时间: {task.CreatedTime}");
                    sb.AppendLine($"分块数量: {task.Chunks}");
                    
                    if (!string.IsNullOrEmpty(task.ErrorMessage))
                    {
                        sb.AppendLine($"错误信息: {task.ErrorMessage}");
                    }

                    MessageBox.Show(sb.ToString(), $"{task.FileName} 属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        #endregion

        // 显示IDM下载管理器窗口
        public static void ShowIdmForm()
        {
            var form = new IdmForm();
            form.Show();
        }
    }

    // 下载任务类
    public class DownloadTask
    {
        public string Url { get; set; }
        public string SavePath { get; set; }
        public string FileName { get; set; }
        public int Chunks { get; set; }
        public DownloadStatus Status { get; set; }
        public double Progress { get; set; }
        public double Speed { get; set; }
        public double MaxSpeed { get; set; }
        public long TotalSize { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ErrorMessage { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }

    // 下载状态枚举
    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Paused,
        Completed,
        Error,
        Metadata,   // 获取种子元数据中
        Hashing     // 校验文件中
    }

    // 新建下载对话框
    public class NewDownloadDialog : Form
    {
        private TextBox urlTextBox;
        private TextBox savePathTextBox;
        private Button browseButton;
        private NumericUpDown chunksNumeric;
        private Button okButton;
        private Button cancelButton;

        public string Url { get; private set; }
        public string SavePath { get; private set; }
        public int Chunks { get; private set; }

        public NewDownloadDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.urlTextBox = new TextBox();
            this.savePathTextBox = new TextBox();
            this.browseButton = new Button();
            this.chunksNumeric = new NumericUpDown();
            this.okButton = new Button();
            this.cancelButton = new Button();
            ((ISupportInitialize)this.chunksNumeric).BeginInit();
            this.SuspendLayout();

            // urlTextBox
            this.urlTextBox.Location = new Point(100, 20);
            this.urlTextBox.Size = new Size(350, 23);

            // savePathTextBox
            this.savePathTextBox.Location = new Point(100, 60);
            this.savePathTextBox.Size = new Size(280, 23);

            // browseButton
            this.browseButton.Location = new Point(390, 60);
            this.browseButton.Size = new Size(60, 23);
            this.browseButton.Text = "浏览...";
            this.browseButton.Click += BrowseButton_Click;

            // chunksNumeric
            this.chunksNumeric.Location = new Point(100, 100);
            this.chunksNumeric.Size = new Size(60, 23);
            this.chunksNumeric.Minimum = 1;
            this.chunksNumeric.Maximum = 16;
            this.chunksNumeric.Value = 4;

            // okButton
            this.okButton.Location = new Point(290, 140);
            this.okButton.Size = new Size(75, 23);
            this.okButton.Text = "确定";
            this.okButton.DialogResult = DialogResult.OK;
            this.okButton.Click += OkButton_Click;

            // cancelButton
            this.cancelButton.Location = new Point(375, 140);
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.Text = "取消";
            this.cancelButton.DialogResult = DialogResult.Cancel;

            // 添加标签
            Label urlLabel = new Label();
            urlLabel.Text = "下载地址:";
            urlLabel.Location = new Point(20, 23);
            urlLabel.Size = new Size(70, 15);

            Label savePathLabel = new Label();
            savePathLabel.Text = "保存路径:";
            savePathLabel.Location = new Point(20, 63);
            savePathLabel.Size = new Size(70, 15);

            Label chunksLabel = new Label();
            chunksLabel.Text = "分块数量:";
            chunksLabel.Location = new Point(20, 103);
            chunksLabel.Size = new Size(70, 15);

            // 添加控件到窗体
            this.Controls.Add(urlLabel);
            this.Controls.Add(this.urlTextBox);
            this.Controls.Add(savePathLabel);
            this.Controls.Add(this.savePathTextBox);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(chunksLabel);
            this.Controls.Add(this.chunksNumeric);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);

            // 窗体设置
            this.ClientSize = new Size(470, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewDownloadDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "新建下载任务";

            ((ISupportInitialize)this.chunksNumeric).EndInit();
            this.ResumeLayout(false);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "所有文件 (*.*)|*.*";
                dialog.Title = "选择保存位置";
                
                if (!string.IsNullOrEmpty(urlTextBox.Text))
                {
                    try
                    {
                        string fileName = Path.GetFileName(new Uri(urlTextBox.Text).LocalPath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            dialog.FileName = fileName;
                        }
                    }
                    catch { }
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    savePathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(urlTextBox.Text))
            {
                MessageBox.Show("请输入下载地址", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(savePathTextBox.Text))
            {
                MessageBox.Show("请选择保存路径", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            try
            {
                Uri uri = new Uri(urlTextBox.Text);
                Url = urlTextBox.Text;
                SavePath = savePathTextBox.Text;
                Chunks = (int)chunksNumeric.Value;
            }
            catch (UriFormatException)
            {
                MessageBox.Show("下载地址格式不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}