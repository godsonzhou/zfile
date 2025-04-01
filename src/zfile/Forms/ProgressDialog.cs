using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Zfile.Forms
{
    /// <summary>
    /// 下载进度显示窗口
    /// </summary>
    public partial class ProgressDialog : Form
    {
        private string _url;
        private string _savePath;
        private string _fileName;
        private long _totalSize;
        private int _chunks;
        private double _progress;
        private double _speed;
        private double _maxSpeed;
        private DownloadStatus _status;
        private CancellationTokenSource _cancellationTokenSource;
        private Dictionary<long, long> _chunkProgress = new Dictionary<long, long>();
        private Dictionary<long, double> _chunkSpeeds = new Dictionary<long, double>();
        private Dictionary<long, long> _lastChunkBytes = new Dictionary<long, long>();
        private Dictionary<long, long> _lastChunkTime = new Dictionary<long, long>();
		private Panel infoPanel;
		private Label fileNameLabel;
		private Label fileSizeLabel;
		private Label statusLabel;
		private Label speedLabel;
		private Label timeLeftLabel;
		private Panel progressPanel;
		private ProgressBar totalProgressBar;
		private Label progressLabel;
		private Panel chunksPanel;
		private ListView chunksListView;
		private Panel buttonPanel;
		private Button hideButton;
		private Button cancelButton;
		private Button pauseButton;
		private Button resumeButton;

		// 委托定义，用于在下载完成或取消时通知主窗体
		public delegate void DownloadCompletedEventHandler(object sender, EventArgs e);
        public event DownloadCompletedEventHandler DownloadCompleted;

        public ProgressDialog(string url, string savePath, int chunks, CancellationTokenSource cancellationTokenSource)
        {
            _url = url;
            _savePath = savePath;
            _fileName = Path.GetFileName(savePath);
            _chunks = chunks;
            _cancellationTokenSource = cancellationTokenSource;
            _status = DownloadStatus.Pending;

            InitializeComponent();
            InitializeChunkProgress();
        }

        #region UI初始化

        private void InitializeComponent()
        {
            this.infoPanel = new Panel();
            this.fileNameLabel = new Label();
            this.fileSizeLabel = new Label();
            this.statusLabel = new Label();
            this.speedLabel = new Label();
            this.timeLeftLabel = new Label();
            this.progressPanel = new Panel();
            this.totalProgressBar = new ProgressBar();
            this.progressLabel = new Label();
            this.chunksPanel = new Panel();
            this.chunksListView = new ListView();
            this.buttonPanel = new Panel();
            this.hideButton = new Button();
            this.cancelButton = new Button();
            this.pauseButton = new Button();

            // 设置窗体属性
            this.Text = "下载进度";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.FormClosing += ProgressDialog_FormClosing;

            // 信息面板
            this.infoPanel.Dock = DockStyle.Top;
            this.infoPanel.Height = 120;
            this.infoPanel.Padding = new Padding(10);

            // 文件名标签
            this.fileNameLabel.AutoSize = true;
            this.fileNameLabel.Location = new Point(10, 15);
            this.fileNameLabel.Text = $"文件名: {_fileName}";
            this.fileNameLabel.Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold);

            // 文件大小标签
            this.fileSizeLabel.AutoSize = true;
            this.fileSizeLabel.Location = new Point(10, 40);
            this.fileSizeLabel.Text = "文件大小: 计算中...";

            // 状态标签
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new Point(10, 65);
            this.statusLabel.Text = "状态: 准备中...";

            // 速度标签
            this.speedLabel.AutoSize = true;
            this.speedLabel.Location = new Point(10, 90);
            this.speedLabel.Text = "传输速度: 0 KB/s";

            // 剩余时间标签
            this.timeLeftLabel.AutoSize = true;
            this.timeLeftLabel.Location = new Point(300, 90);
            this.timeLeftLabel.Text = "剩余时间: 计算中...";

            // 添加控件到信息面板
            this.infoPanel.Controls.Add(this.fileNameLabel);
            this.infoPanel.Controls.Add(this.fileSizeLabel);
            this.infoPanel.Controls.Add(this.statusLabel);
            this.infoPanel.Controls.Add(this.speedLabel);
            this.infoPanel.Controls.Add(this.timeLeftLabel);

            // 进度面板
            this.progressPanel.Dock = DockStyle.Top;
            this.progressPanel.Height = 50;
            this.progressPanel.Padding = new Padding(10);

            // 总进度条
            this.totalProgressBar.Location = new Point(10, 10);
            this.totalProgressBar.Size = new Size(460, 25);
            this.totalProgressBar.Minimum = 0;
            this.totalProgressBar.Maximum = 100;
            this.totalProgressBar.Value = 0;

            // 进度百分比标签
            this.progressLabel.AutoSize = true;
            this.progressLabel.Location = new Point(480, 15);
            this.progressLabel.Text = "0%";

            // 添加控件到进度面板
            this.progressPanel.Controls.Add(this.totalProgressBar);
            this.progressPanel.Controls.Add(this.progressLabel);

            // 分块面板
            this.chunksPanel.Dock = DockStyle.Fill;
            this.chunksPanel.Padding = new Padding(10);

            // 分块列表视图
            this.chunksListView.Dock = DockStyle.Fill;
            this.chunksListView.View = View.Details;
            this.chunksListView.FullRowSelect = true;
            this.chunksListView.GridLines = true;

            // 添加列标题
            this.chunksListView.Columns.Add("序号", 50);
            this.chunksListView.Columns.Add("已下载", 100);
            this.chunksListView.Columns.Add("信息", 150);
            this.chunksListView.Columns.Add("传输速度", 100);
            this.chunksListView.Columns.Add("进度", 170);

            // 添加控件到分块面板
            this.chunksPanel.Controls.Add(this.chunksListView);

            // 按钮面板
            this.buttonPanel.Dock = DockStyle.Bottom;
            this.buttonPanel.Height = 50;
            this.buttonPanel.Padding = new Padding(10);

            // 隐藏按钮
            this.hideButton.Text = "隐藏窗口";
            this.hideButton.Location = new Point(310, 10);
            this.hideButton.Size = new Size(80, 30);
            this.hideButton.Click += HideButton_Click;

            // 取消按钮
            this.cancelButton.Text = "取消";
            this.cancelButton.Location = new Point(400, 10);
            this.cancelButton.Size = new Size(80, 30);
            this.cancelButton.Click += CancelButton_Click;

            // 暂停/继续按钮
            this.pauseButton.Text = "暂停";
            this.pauseButton.Location = new Point(490, 10);
            this.pauseButton.Size = new Size(80, 30);
            this.pauseButton.Click += PauseButton_Click;

            // 添加控件到按钮面板
            this.buttonPanel.Controls.Add(this.hideButton);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Controls.Add(this.pauseButton);

            // 添加面板到窗体
            this.Controls.Add(this.chunksPanel);
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.progressPanel);
            this.Controls.Add(this.infoPanel);
        }

        private void InitializeChunkProgress()
        {
            // 初始化分块进度列表
            chunksListView.Items.Clear();
            for (int i = 0; i < _chunks; i++)
            {
                ListViewItem item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add("0 KB");
                item.SubItems.Add("正在初始化...");
                item.SubItems.Add("0 KB/s");
                item.SubItems.Add("[                    ] 0%");
                chunksListView.Items.Add(item);

                // 初始化分块进度数据
                _chunkProgress[i] = 0;
                _chunkSpeeds[i] = 0;
                _lastChunkBytes[i] = 0;
                _lastChunkTime[i] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }

        #endregion

        #region 事件处理

        private void ProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 如果是用户关闭窗口，则隐藏而不是关闭
            if (e.CloseReason == CloseReason.UserClosing && _status == DownloadStatus.Downloading)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void HideButton_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要取消下载吗？", "确认取消", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _cancellationTokenSource?.Cancel();
                _status = DownloadStatus.Paused;
                UpdateStatus();
                DownloadCompleted?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (_status == DownloadStatus.Downloading)
            {
                _cancellationTokenSource?.Cancel();
                _status = DownloadStatus.Paused;
                pauseButton.Text = "继续";
                UpdateStatus();
                DownloadCompleted?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
            else if (_status == DownloadStatus.Paused)
            {
                // 通知主窗体继续下载
                _status = DownloadStatus.Pending;
                pauseButton.Text = "暂停";
                UpdateStatus();
                DownloadCompleted?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
        }

        #endregion

        #region 进度更新

        /// <summary>
        /// 更新下载进度
        /// </summary>
        /// <param name="progress">总进度百分比</param>
        /// <param name="speed">下载速度(bytes/s)</param>
        /// <param name="totalSize">文件总大小</param>
        /// <param name="chunkProgress">各分块的进度</param>
        public void UpdateProgress(double progress, double speed, long totalSize, Dictionary<long, long> chunkProgress = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(progress, speed, totalSize, chunkProgress)));
                return;
            }

            _progress = progress;
            _speed = speed;
            _totalSize = totalSize;
            _maxSpeed = Math.Max(_maxSpeed, speed);
            _status = DownloadStatus.Downloading;

            // 更新总进度
            totalProgressBar.Value = (int)progress;
            progressLabel.Text = $"{progress:F1}%";

            // 更新文件信息
            fileSizeLabel.Text = $"文件大小: {FormatFileSize(totalSize)}";
            speedLabel.Text = $"传输速度: {FormatSpeed(speed)}";

            // 计算剩余时间
            if (speed > 0)
            {
                long bytesLeft = totalSize - (long)(totalSize * progress / 100);
                double secondsLeft = bytesLeft / speed;
                timeLeftLabel.Text = $"剩余时间: {FormatTimeSpan(secondsLeft)}";
            }
            else
            {
                timeLeftLabel.Text = "剩余时间: 计算中...";
            }

            // 更新状态
            UpdateStatus();

            // 更新分块进度
            if (chunkProgress != null)
            {
                UpdateChunkProgress(chunkProgress);
            }
        }

        /// <summary>
        /// 更新分块下载进度
        /// </summary>
        private void UpdateChunkProgress(Dictionary<long, long> chunkProgress)
        {
            // 计算每个分块的大小
            long chunkSize = _totalSize / _chunks;

            // 更新每个分块的进度
            int i = 0;
            foreach (var entry in chunkProgress)
            {
                long startPos = entry.Key;
                long bytesDownloaded = entry.Value;
                int chunkIndex = (int)(startPos / chunkSize);

                if (chunkIndex < 0 || chunkIndex >= _chunks) continue;

                // 计算分块的结束位置
                long endPos = (chunkIndex == _chunks - 1) ? _totalSize - 1 : startPos + chunkSize - 1;
                long chunkTotalSize = endPos - startPos + 1;
                double _chunkProgress = (double)bytesDownloaded / chunkTotalSize * 100;

                // 计算分块的下载速度
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long timeElapsed = currentTime - _lastChunkTime[chunkIndex];
                if (timeElapsed > 500) // 每500毫秒更新一次速度
                {
                    long bytesChange = bytesDownloaded - _lastChunkBytes[chunkIndex];
                    _chunkSpeeds[chunkIndex] = bytesChange * 1000.0 / timeElapsed; // bytes per second
                    _lastChunkTime[chunkIndex] = currentTime;
                    _lastChunkBytes[chunkIndex] = bytesDownloaded;
                }

                // 更新列表项
                if (chunksListView.Items.Count > chunkIndex)
                {
                    ListViewItem item = chunksListView.Items[chunkIndex];
                    item.SubItems[1].Text = FormatFileSize(bytesDownloaded);
                    item.SubItems[2].Text = $"{FormatFileSize(startPos)} - {FormatFileSize(endPos)}";
                    item.SubItems[3].Text = FormatSpeed(_chunkSpeeds[chunkIndex]);

                    // 创建进度条文本 [====    ] 40%
                    int progressChars = (int)(_chunkProgress / 5); // 20个字符表示100%
                    string progressBar = "[" + new string('=', progressChars) + new string(' ', 20 - progressChars) + "] " + $"{_chunkProgress:F1}%";
                    item.SubItems[4].Text = progressBar;
                }

                i++;
            }
        }

        /// <summary>
        /// 更新下载状态
        /// </summary>
        private void UpdateStatus()
        {
            switch (_status)
            {
                case DownloadStatus.Pending:
                    statusLabel.Text = "状态: 准备中...";
                    break;
                case DownloadStatus.Downloading:
                    statusLabel.Text = "状态: 正在下载...";
                    break;
                case DownloadStatus.Paused:
                    statusLabel.Text = "状态: 已暂停";
                    break;
                case DownloadStatus.Completed:
                    statusLabel.Text = "状态: 已完成";
                    pauseButton.Enabled = false;
                    cancelButton.Text = "关闭";
                    break;
                case DownloadStatus.Error:
                    statusLabel.Text = "状态: 下载出错";
                    break;
            }
        }

        /// <summary>
        /// 设置下载完成状态
        /// </summary>
        public void SetCompleted()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetCompleted));
                return;
            }

            _status = DownloadStatus.Completed;
            _progress = 100;
            totalProgressBar.Value = 100;
            progressLabel.Text = "100%";
            timeLeftLabel.Text = "剩余时间: 0秒";
            UpdateStatus();

            // 通知主窗体下载完成
            DownloadCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 设置下载错误状态
        /// </summary>
        public void SetError(string errorMessage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetError(errorMessage)));
                return;
            }

            _status = DownloadStatus.Error;
            statusLabel.Text = $"状态: 下载出错 - {errorMessage}";
            pauseButton.Enabled = false;
            cancelButton.Text = "关闭";

            // 通知主窗体下载出错
            DownloadCompleted?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F2} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        /// <summary>
        /// 格式化下载速度
        /// </summary>
        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F2} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
        }

        /// <summary>
        /// 格式化时间
        /// </summary>
        private string FormatTimeSpan(double seconds)
        {
            if (seconds < 60) return $"{seconds:F0}秒";
            if (seconds < 3600) return $"{(int)(seconds / 60)}分{(int)(seconds % 60)}秒";
            if (seconds < 86400) return $"{(int)(seconds / 3600)}小时{(int)((seconds % 3600) / 60)}分";
            return $"{(int)(seconds / 86400)}天{(int)((seconds % 86400) / 3600)}小时";
        }

        #endregion
    }
}