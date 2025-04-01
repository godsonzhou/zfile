using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
namespace Zfile.Forms
{
    public partial class IdmForm : Form
    {
        private List<DownloadTask> downloadTasks = new List<DownloadTask>();
        private CancellationTokenSource cancellationTokenSource;
        private bool isClosing = false;

        public IdmForm()
        {
            InitializeComponent();
            InitializeDownloadList();
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

        private void IdmForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            // 保存下载任务状态
            SaveDownloadTasks();
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

        private void RemoveDownloadTask(DownloadTask task)
        {
            // 停止下载
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

            downloadTasks.Remove(task);
        }

        private async void ResumeDownloadTask(DownloadTask task)
        {
            if (task.Status == DownloadStatus.Downloading)
                return;

            task.Status = DownloadStatus.Downloading;
            task.CancellationTokenSource = new CancellationTokenSource();
            UpdateDownloadListView();

            try
            {
                await Task.Run(async () =>
                {
                    await IdmManager.StartDownloadWithProgress(task.Url, task.SavePath, task.Chunks, task.CancellationTokenSource.Token,
                        (progress, speed, totalSize) =>
                        {
                            if (!isClosing)
                            {
                                task.Progress = progress;
                                task.Speed = speed;
                                task.TotalSize = totalSize;
                                task.MaxSpeed = Math.Max(task.MaxSpeed, speed);

                                // 更新UI
                                UpdateTaskUI(task);
                            }
                        });

                    if (!task.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        task.Status = DownloadStatus.Completed;
                        task.Progress = 100;
                        UpdateTaskUI(task);
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
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PauseDownloadTask(DownloadTask task)
        {
            if (task.Status != DownloadStatus.Downloading)
                return;

            task.CancellationTokenSource?.Cancel();
            task.Status = DownloadStatus.Paused;
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

        private void SaveDownloadTasks()
        {
            // 保存下载任务到配置文件，这里简化处理
            Debug.WriteLine($"保存了 {downloadTasks.Count} 个下载任务");
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
        Error
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