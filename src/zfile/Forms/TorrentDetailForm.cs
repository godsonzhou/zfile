using System.Text;

namespace Zfile.Forms
{
    /// <summary>
    /// 种子下载详细信息窗口，用于显示种子下载的详细状态和调试信息
    /// </summary>
    public partial class TorrentDetailForm : Form
    {
        private string _torrentId;
        private System.Windows.Forms.Timer _updateTimer;
        private StringBuilder _logBuilder = new StringBuilder();
        private const int MaxLogLines = 1000;

        public TorrentDetailForm(string torrentId)
        {
            _torrentId = torrentId;
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000; // 1秒更新一次
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateTorrentInfo();
        }

        private void InitializeComponent()
        {
            this.tabControl = new TabControl();
            this.generalTab = new TabPage();
            this.peersTab = new TabPage();
            this.trackersTab = new TabPage();
            this.filesTab = new TabPage();
            this.logTab = new TabPage();
            this.dhtTab = new TabPage();

            // 常规信息面板
            this.generalInfoPanel = new Panel();
            this.torrentNameLabel = new Label();
            this.torrentSizeLabel = new Label();
            this.downloadSpeedLabel = new Label();
            this.uploadSpeedLabel = new Label();
            this.progressLabel = new Label();
            this.statusLabel = new Label();
            this.downloadedLabel = new Label();
            this.uploadedLabel = new Label();
            this.timeLeftLabel = new Label();
            this.hashLabel = new Label();
            this.progressBar = new ProgressBar();

            // Peers列表
            this.peersListView = new ListView();
            this.peerAddressColumn = new ColumnHeader();
            this.peerClientColumn = new ColumnHeader();
            this.peerDownSpeedColumn = new ColumnHeader();
            this.peerUpSpeedColumn = new ColumnHeader();
            this.peerProgressColumn = new ColumnHeader();
            this.peerStatusColumn = new ColumnHeader();

            // Trackers列表
            this.trackersListView = new ListView();
            this.trackerUrlColumn = new ColumnHeader();
            this.trackerStatusColumn = new ColumnHeader();
            this.trackerUpdateColumn = new ColumnHeader();
            this.trackerSeedsColumn = new ColumnHeader();
            this.trackerPeersColumn = new ColumnHeader();

            // 文件列表
            this.filesListView = new ListView();
            this.fileNameColumn = new ColumnHeader();
            this.fileSizeColumn = new ColumnHeader();
            this.fileProgressColumn = new ColumnHeader();
            this.filePriorityColumn = new ColumnHeader();

            // 日志
            this.logTextBox = new TextBox();
            this.clearLogButton = new Button();
            this.saveLogButton = new Button();
            this.verboseLoggingCheckBox = new CheckBox();

            // DHT信息
            this.dhtInfoPanel = new Panel();
            this.dhtStatusLabel = new Label();
            this.dhtNodesLabel = new Label();
            this.dhtListView = new ListView();
            this.dhtNodeColumn = new ColumnHeader();
            this.dhtStatusColumn = new ColumnHeader();
            this.dhtLastSeenColumn = new ColumnHeader();

            // 设置控件
            this.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.generalTab.SuspendLayout();
            this.peersTab.SuspendLayout();
            this.trackersTab.SuspendLayout();
            this.filesTab.SuspendLayout();
            this.logTab.SuspendLayout();
            this.dhtTab.SuspendLayout();

            // TabControl
            this.tabControl.Controls.Add(this.generalTab);
            this.tabControl.Controls.Add(this.peersTab);
            this.tabControl.Controls.Add(this.trackersTab);
            this.tabControl.Controls.Add(this.filesTab);
            this.tabControl.Controls.Add(this.dhtTab);
            this.tabControl.Controls.Add(this.logTab);
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Location = new Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new Size(800, 450);
            this.tabControl.TabIndex = 0;

            // 常规标签页
            this.generalTab.Controls.Add(this.generalInfoPanel);
            this.generalTab.Location = new Point(4, 24);
            this.generalTab.Name = "generalTab";
            this.generalTab.Padding = new Padding(3);
            this.generalTab.Size = new Size(792, 422);
            this.generalTab.TabIndex = 0;
            this.generalTab.Text = "常规";
            this.generalTab.UseVisualStyleBackColor = true;

            // 常规信息面板
            this.generalInfoPanel.Dock = DockStyle.Fill;
            this.generalInfoPanel.Location = new Point(3, 3);
            this.generalInfoPanel.Name = "generalInfoPanel";
            this.generalInfoPanel.Size = new Size(786, 416);
            this.generalInfoPanel.TabIndex = 0;

            // 种子名称标签
            this.torrentNameLabel.AutoSize = true;
            this.torrentNameLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            this.torrentNameLabel.Location = new Point(10, 10);
            this.torrentNameLabel.Name = "torrentNameLabel";
            this.torrentNameLabel.Size = new Size(100, 16);
            this.torrentNameLabel.TabIndex = 0;
            this.torrentNameLabel.Text = "名称: 未知";
            this.generalInfoPanel.Controls.Add(this.torrentNameLabel);

            // 种子大小标签
            this.torrentSizeLabel.AutoSize = true;
            this.torrentSizeLabel.Location = new Point(10, 40);
            this.torrentSizeLabel.Name = "torrentSizeLabel";
            this.torrentSizeLabel.Size = new Size(100, 15);
            this.torrentSizeLabel.TabIndex = 1;
            this.torrentSizeLabel.Text = "大小: 未知";
            this.generalInfoPanel.Controls.Add(this.torrentSizeLabel);

            // 下载速度标签
            this.downloadSpeedLabel.AutoSize = true;
            this.downloadSpeedLabel.Location = new Point(10, 70);
            this.downloadSpeedLabel.Name = "downloadSpeedLabel";
            this.downloadSpeedLabel.Size = new Size(100, 15);
            this.downloadSpeedLabel.TabIndex = 2;
            this.downloadSpeedLabel.Text = "下载速度: 0 KB/s";
            this.generalInfoPanel.Controls.Add(this.downloadSpeedLabel);

            // 上传速度标签
            this.uploadSpeedLabel.AutoSize = true;
            this.uploadSpeedLabel.Location = new Point(10, 100);
            this.uploadSpeedLabel.Name = "uploadSpeedLabel";
            this.uploadSpeedLabel.Size = new Size(100, 15);
            this.uploadSpeedLabel.TabIndex = 3;
            this.uploadSpeedLabel.Text = "上传速度: 0 KB/s";
            this.generalInfoPanel.Controls.Add(this.uploadSpeedLabel);

            // 进度标签
            this.progressLabel.AutoSize = true;
            this.progressLabel.Location = new Point(10, 130);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new Size(100, 15);
            this.progressLabel.TabIndex = 4;
            this.progressLabel.Text = "进度: 0%";
            this.generalInfoPanel.Controls.Add(this.progressLabel);

            // 进度条
            this.progressBar.Location = new Point(10, 150);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(766, 23);
            this.progressBar.TabIndex = 5;
            this.generalInfoPanel.Controls.Add(this.progressBar);

            // 状态标签
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new Point(10, 180);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(100, 15);
            this.statusLabel.TabIndex = 6;
            this.statusLabel.Text = "状态: 未知";
            this.generalInfoPanel.Controls.Add(this.statusLabel);

            // 已下载标签
            this.downloadedLabel.AutoSize = true;
            this.downloadedLabel.Location = new Point(10, 210);
            this.downloadedLabel.Name = "downloadedLabel";
            this.downloadedLabel.Size = new Size(100, 15);
            this.downloadedLabel.TabIndex = 7;
            this.downloadedLabel.Text = "已下载: 0 MB";
            this.generalInfoPanel.Controls.Add(this.downloadedLabel);

            // 已上传标签
            this.uploadedLabel.AutoSize = true;
            this.uploadedLabel.Location = new Point(10, 240);
            this.uploadedLabel.Name = "uploadedLabel";
            this.uploadedLabel.Size = new Size(100, 15);
            this.uploadedLabel.TabIndex = 8;
            this.uploadedLabel.Text = "已上传: 0 MB";
            this.generalInfoPanel.Controls.Add(this.uploadedLabel);

            // 剩余时间标签
            this.timeLeftLabel.AutoSize = true;
            this.timeLeftLabel.Location = new Point(10, 270);
            this.timeLeftLabel.Name = "timeLeftLabel";
            this.timeLeftLabel.Size = new Size(100, 15);
            this.timeLeftLabel.TabIndex = 9;
            this.timeLeftLabel.Text = "剩余时间: 未知";
            this.generalInfoPanel.Controls.Add(this.timeLeftLabel);

            // 哈希值标签
            this.hashLabel.AutoSize = true;
            this.hashLabel.Location = new Point(10, 300);
            this.hashLabel.Name = "hashLabel";
            this.hashLabel.Size = new Size(100, 15);
            this.hashLabel.TabIndex = 10;
            this.hashLabel.Text = "哈希值: 未知";
            this.generalInfoPanel.Controls.Add(this.hashLabel);

            // Peers标签页
            this.peersTab.Controls.Add(this.peersListView);
            this.peersTab.Location = new Point(4, 24);
            this.peersTab.Name = "peersTab";
            this.peersTab.Padding = new Padding(3);
            this.peersTab.Size = new Size(792, 422);
            this.peersTab.TabIndex = 1;
            this.peersTab.Text = "Peers";
            this.peersTab.UseVisualStyleBackColor = true;

            // Peers列表视图
            this.peersListView.Columns.AddRange(new ColumnHeader[] {
                this.peerAddressColumn,
                this.peerClientColumn,
                this.peerDownSpeedColumn,
                this.peerUpSpeedColumn,
                this.peerProgressColumn,
                this.peerStatusColumn
            });
            this.peersListView.Dock = DockStyle.Fill;
            this.peersListView.FullRowSelect = true;
            this.peersListView.GridLines = true;
            this.peersListView.HideSelection = false;
            this.peersListView.Location = new Point(3, 3);
            this.peersListView.Name = "peersListView";
            this.peersListView.Size = new Size(786, 416);
            this.peersListView.TabIndex = 0;
            this.peersListView.UseCompatibleStateImageBehavior = false;
            this.peersListView.View = View.Details;

            // Peers列表列
            this.peerAddressColumn.Text = "地址";
            this.peerAddressColumn.Width = 150;
            this.peerClientColumn.Text = "客户端";
            this.peerClientColumn.Width = 150;
            this.peerDownSpeedColumn.Text = "下载速度";
            this.peerDownSpeedColumn.Width = 100;
            this.peerUpSpeedColumn.Text = "上传速度";
            this.peerUpSpeedColumn.Width = 100;
            this.peerProgressColumn.Text = "进度";
            this.peerProgressColumn.Width = 100;
            this.peerStatusColumn.Text = "状态";
            this.peerStatusColumn.Width = 100;

            // Trackers标签页
            this.trackersTab.Controls.Add(this.trackersListView);
            this.trackersTab.Location = new Point(4, 24);
            this.trackersTab.Name = "trackersTab";
            this.trackersTab.Padding = new Padding(3);
            this.trackersTab.Size = new Size(792, 422);
            this.trackersTab.TabIndex = 2;
            this.trackersTab.Text = "Trackers";
            this.trackersTab.UseVisualStyleBackColor = true;

            // Trackers列表视图
            this.trackersListView.Columns.AddRange(new ColumnHeader[] {
                this.trackerUrlColumn,
                this.trackerStatusColumn,
                this.trackerUpdateColumn,
                this.trackerSeedsColumn,
                this.trackerPeersColumn
            });
            this.trackersListView.Dock = DockStyle.Fill;
            this.trackersListView.FullRowSelect = true;
            this.trackersListView.GridLines = true;
            this.trackersListView.HideSelection = false;
            this.trackersListView.Location = new Point(3, 3);
            this.trackersListView.Name = "trackersListView";
            this.trackersListView.Size = new Size(786, 416);
            this.trackersListView.TabIndex = 0;
            this.trackersListView.UseCompatibleStateImageBehavior = false;
            this.trackersListView.View = View.Details;

            // Trackers列表列
            this.trackerUrlColumn.Text = "URL";
            this.trackerUrlColumn.Width = 300;
            this.trackerStatusColumn.Text = "状态";
            this.trackerStatusColumn.Width = 100;
            this.trackerUpdateColumn.Text = "更新时间";
            this.trackerUpdateColumn.Width = 150;
            this.trackerSeedsColumn.Text = "做种数";
            this.trackerSeedsColumn.Width = 100;
            this.trackerPeersColumn.Text = "Peer数";
            this.trackerPeersColumn.Width = 100;

            // 文件标签页
            this.filesTab.Controls.Add(this.filesListView);
            this.filesTab.Location = new Point(4, 24);
            this.filesTab.Name = "filesTab";
            this.filesTab.Padding = new Padding(3);
            this.filesTab.Size = new Size(792, 422);
            this.filesTab.TabIndex = 3;
            this.filesTab.Text = "文件";
            this.filesTab.UseVisualStyleBackColor = true;

            // 文件列表视图
            this.filesListView.Columns.AddRange(new ColumnHeader[] {
                this.fileNameColumn,
                this.fileSizeColumn,
                this.fileProgressColumn,
                this.filePriorityColumn
            });
            this.filesListView.Dock = DockStyle.Fill;
            this.filesListView.FullRowSelect = true;
            this.filesListView.GridLines = true;
            this.filesListView.HideSelection = false;
            this.filesListView.Location = new Point(3, 3);
            this.filesListView.Name = "filesListView";
            this.filesListView.Size = new Size(786, 416);
            this.filesListView.TabIndex = 0;
            this.filesListView.UseCompatibleStateImageBehavior = false;
            this.filesListView.View = View.Details;

            // 文件列表列
            this.fileNameColumn.Text = "文件名";
            this.fileNameColumn.Width = 300;
            this.fileSizeColumn.Text = "大小";
            this.fileSizeColumn.Width = 100;
            this.fileProgressColumn.Text = "进度";
            this.fileProgressColumn.Width = 100;
            this.filePriorityColumn.Text = "优先级";
            this.filePriorityColumn.Width = 100;

            // DHT标签页
            this.dhtTab.Controls.Add(this.dhtInfoPanel);
            this.dhtTab.Location = new Point(4, 24);
            this.dhtTab.Name = "dhtTab";
            this.dhtTab.Padding = new Padding(3);
            this.dhtTab.Size = new Size(792, 422);
            this.dhtTab.TabIndex = 4;
            this.dhtTab.Text = "DHT";
            this.dhtTab.UseVisualStyleBackColor = true;

            // DHT信息面板
            this.dhtInfoPanel.Dock = DockStyle.Fill;
            this.dhtInfoPanel.Location = new Point(3, 3);
            this.dhtInfoPanel.Name = "dhtInfoPanel";
            this.dhtInfoPanel.Size = new Size(786, 416);
            this.dhtInfoPanel.TabIndex = 0;

            // DHT状态标签
            this.dhtStatusLabel.AutoSize = true;
            this.dhtStatusLabel.Location = new Point(10, 10);
            this.dhtStatusLabel.Name = "dhtStatusLabel";
            this.dhtStatusLabel.Size = new Size(100, 15);
            this.dhtStatusLabel.TabIndex = 0;
            this.dhtStatusLabel.Text = "DHT状态: 未知";
            this.dhtInfoPanel.Controls.Add(this.dhtStatusLabel);

            // DHT节点数标签
            this.dhtNodesLabel.AutoSize = true;
            this.dhtNodesLabel.Location = new Point(10, 40);
            this.dhtNodesLabel.Name = "dhtNodesLabel";
            this.dhtNodesLabel.Size = new Size(100, 15);
            this.dhtNodesLabel.TabIndex = 1;
            this.dhtNodesLabel.Text = "DHT节点数: 0";
            this.dhtInfoPanel.Controls.Add(this.dhtNodesLabel);

            // DHT节点列表视图
            this.dhtListView.Columns.AddRange(new ColumnHeader[] {
                this.dhtNodeColumn,
                this.dhtStatusColumn,
                this.dhtLastSeenColumn
            });
            this.dhtListView.FullRowSelect = true;
            this.dhtListView.GridLines = true;
            this.dhtListView.HideSelection = false;
            this.dhtListView.Location = new Point(10, 70);
            this.dhtListView.Name = "dhtListView";
            this.dhtListView.Size = new Size(766, 336);
            this.dhtListView.TabIndex = 2;
            this.dhtListView.UseCompatibleStateImageBehavior = false;
            this.dhtListView.View = View.Details;
            this.dhtInfoPanel.Controls.Add(this.dhtListView);

            // DHT节点列表列
            this.dhtNodeColumn.Text = "节点";
            this.dhtNodeColumn.Width = 300;
            this.dhtStatusColumn.Text = "状态";
            this.dhtStatusColumn.Width = 100;
            this.dhtLastSeenColumn.Text = "最后活动时间";
            this.dhtLastSeenColumn.Width = 150;

            // 日志标签页
            this.logTab.Controls.Add(this.logTextBox);
            this.logTab.Controls.Add(this.clearLogButton);
            this.logTab.Controls.Add(this.saveLogButton);
            this.logTab.Controls.Add(this.verboseLoggingCheckBox);
            this.logTab.Location = new Point(4, 24);
            this.logTab.Name = "logTab";
            this.logTab.Padding = new Padding(3);
            this.logTab.Size = new Size(792, 422);
            this.logTab.TabIndex = 5;
            this.logTab.Text = "日志";
            this.logTab.UseVisualStyleBackColor = true;

            // 日志文本框
            this.logTextBox.Location = new Point(6, 6);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = ScrollBars.Both;
            this.logTextBox.Size = new Size(780, 381);
            this.logTextBox.TabIndex = 0;

            // 清除日志按钮
            this.clearLogButton.Location = new Point(6, 393);
            this.clearLogButton.Name = "clearLogButton";
            this.clearLogButton.Size = new Size(75, 23);
            this.clearLogButton.TabIndex = 1;
            this.clearLogButton.Text = "清除日志";
            this.clearLogButton.UseVisualStyleBackColor = true;
            this.clearLogButton.Click += ClearLogButton_Click;

            // 保存日志按钮
            this.saveLogButton.Location = new Point(87, 393);
            this.saveLogButton.Name = "saveLogButton";
            this.saveLogButton.Size = new Size(75, 23);
            this.saveLogButton.TabIndex = 2;
            this.saveLogButton.Text = "保存日志";
            this.saveLogButton.UseVisualStyleBackColor = true;
            this.saveLogButton.Click += SaveLogButton_Click;

            // 详细日志复选框
            this.verboseLoggingCheckBox.AutoSize = true;
            this.verboseLoggingCheckBox.Location = new Point(168, 397);
            this.verboseLoggingCheckBox.Name = "verboseLoggingCheckBox";
            this.verboseLoggingCheckBox.Size = new Size(99, 19);
            this.verboseLoggingCheckBox.TabIndex = 3;
            this.verboseLoggingCheckBox.Text = "启用详细日志";
            this.verboseLoggingCheckBox.UseVisualStyleBackColor = true;
            this.verboseLoggingCheckBox.CheckedChanged += VerboseLoggingCheckBox_CheckedChanged;

            // 窗体
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 450);
            this.Controls.Add(this.tabControl);
            this.Name = "TorrentDetailForm";
            this.Text = "种子下载详细信息";
            this.FormClosing += TorrentDetailForm_FormClosing;
            this.Load += TorrentDetailForm_Load;

            this.tabControl.ResumeLayout(false);
            this.generalTab.ResumeLayout(false);
            this.peersTab.ResumeLayout(false);
            this.trackersTab.ResumeLayout(false);
            this.filesTab.ResumeLayout(false);
            this.logTab.ResumeLayout(false);
            this.logTab.PerformLayout();
            this.dhtTab.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void TorrentDetailForm_Load(object sender, EventArgs e)
        {
            UpdateTorrentInfo();
            AddLog("详细信息窗口已打开");
            AddLog($"种子ID: {_torrentId}");
        }

        private void TorrentDetailForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _updateTimer.Stop();
            _updateTimer.Dispose();
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            _logBuilder.Clear();
            logTextBox.Clear();
            AddLog("日志已清除");
        }

        private void SaveLogButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                dialog.Title = "保存日志";
                dialog.FileName = $"Torrent_{_torrentId}_{DateTime.Now:yyyyMMdd_HHmmss}.log";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dialog.FileName, logTextBox.Text);
                        AddLog($"日志已保存到: {dialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存日志失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void VerboseLoggingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AddLog($"详细日志已{(verboseLoggingCheckBox.Checked ? "启用" : "禁用")}");
        }

        private void UpdateTorrentInfo()
        {
            var torrentInfo = TorrentMgr.GetTorrentInfo(_torrentId);
            if (torrentInfo == null)
            {
                AddLog("获取种子信息失败，可能已被删除");
                return;
            }

            // 更新常规信息
            UpdateGeneralInfo(torrentInfo);

            // 更新文件列表
            UpdateFilesList(torrentInfo);

            // 获取详细信息（需要修改TorrentManager类以提供这些信息）
            var detailedInfo = TorrentMgr.GetDetailedTorrentInfo(_torrentId);
            if (detailedInfo != null)
            {
                // 更新Peers列表
                UpdatePeersList(detailedInfo);

                // 更新Trackers列表
                UpdateTrackersList(detailedInfo);

                // 更新DHT信息
                UpdateDhtInfo(detailedInfo);

                // 记录详细日志
                if (verboseLoggingCheckBox.Checked)
                {
                    AddDetailedLog(detailedInfo);
                }
            }
        }

        private void UpdateGeneralInfo(TorrentInfo info)
        {
            torrentNameLabel.Text = $"名称: {info.Name}";
            torrentSizeLabel.Text = $"大小: {FormatFileSize(info.Size)}";
            downloadSpeedLabel.Text = $"下载速度: {FormatSpeed(info.DownloadSpeed)}";
            uploadSpeedLabel.Text = $"上传速度: {FormatSpeed(info.UploadSpeed)}";
            progressLabel.Text = $"进度: {info.Progress:F2}%";
            statusLabel.Text = $"状态: {info.State}";
            downloadedLabel.Text = $"已下载: {FormatFileSize(info.DownloadedBytes)}";
            uploadedLabel.Text = $"已上传: {FormatFileSize(info.UploadedBytes)}";
            hashLabel.Text = $"哈希值: {_torrentId}";

            // 计算剩余时间
            if (info.DownloadSpeed > 0)
            {
                long remainingBytes = info.Size - info.DownloadedBytes;
                double remainingSeconds = remainingBytes / info.DownloadSpeed;
                TimeSpan timeLeft = TimeSpan.FromSeconds(remainingSeconds);
                timeLeftLabel.Text = $"剩余时间: {FormatTimeSpan(timeLeft)}";
            }
            else
            {
                timeLeftLabel.Text = "剩余时间: 未知";
            }

            // 更新进度条
            progressBar.Value = (int)info.Progress;
        }

        private void UpdateFilesList(TorrentInfo info)
        {
            filesListView.BeginUpdate();
            filesListView.Items.Clear();

            foreach (var file in info.Files)
            {
                var item = new ListViewItem(Path.GetFileName(file.Path));
                item.SubItems.Add(FormatFileSize(file.Length));
                item.SubItems.Add($"{file.Progress:F2}%");
                item.SubItems.Add(file.Priority);
                filesListView.Items.Add(item);
            }

            filesListView.EndUpdate();
        }

        private void UpdatePeersList(TorrentDetailedInfo detailedInfo)
        {
            peersListView.BeginUpdate();
            peersListView.Items.Clear();

            foreach (var peer in detailedInfo.Peers)
            {
                var item = new ListViewItem(peer.Address);
                item.SubItems.Add(peer.ClientSoftware);
                item.SubItems.Add(FormatSpeed(peer.DownloadSpeed));
                item.SubItems.Add(FormatSpeed(peer.UploadSpeed));
                item.SubItems.Add($"{peer.Progress:F2}%");
                item.SubItems.Add(peer.Status);
                peersListView.Items.Add(item);
            }

            peersListView.EndUpdate();
        }

        private void UpdateTrackersList(TorrentDetailedInfo detailedInfo)
        {
            trackersListView.BeginUpdate();
            trackersListView.Items.Clear();

            foreach (var tracker in detailedInfo.Trackers)
            {
                var item = new ListViewItem(tracker.Url);
                item.SubItems.Add(tracker.Status);
                item.SubItems.Add(tracker.LastUpdated.ToString());
                item.SubItems.Add(tracker.Seeds.ToString());
                item.SubItems.Add(tracker.Peers.ToString());
                trackersListView.Items.Add(item);
            }

            trackersListView.EndUpdate();
        }

        private void UpdateDhtInfo(TorrentDetailedInfo detailedInfo)
        {
            dhtStatusLabel.Text = $"DHT状态: {detailedInfo.DhtStatus}";
            dhtNodesLabel.Text = $"DHT节点数: {detailedInfo.DhtNodes.Count}";

            dhtListView.BeginUpdate();
            dhtListView.Items.Clear();

            foreach (var node in detailedInfo.DhtNodes)
            {
                var item = new ListViewItem(node.Address);
                item.SubItems.Add(node.Status);
                item.SubItems.Add(node.LastSeen.ToString());
                dhtListView.Items.Add(item);
            }

            dhtListView.EndUpdate();
        }

        private void AddDetailedLog(TorrentDetailedInfo detailedInfo)
        {
            AddLog($"[详细] 连接的Peers数: {detailedInfo.Peers.Count}");
            AddLog($"[详细] 活动的Trackers数: {detailedInfo.Trackers.Count}");
            AddLog($"[详细] DHT节点数: {detailedInfo.DhtNodes.Count}");
            AddLog($"[详细] 下载速度: {FormatSpeed(detailedInfo.DownloadSpeed)}");
            AddLog($"[详细] 上传速度: {FormatSpeed(detailedInfo.UploadSpeed)}");
            AddLog($"[详细] 已下载数据: {FormatFileSize(detailedInfo.DownloadedBytes)}");
            AddLog($"[详细] 已上传数据: {FormatFileSize(detailedInfo.UploadedBytes)}");
            AddLog($"[详细] 当前状态: {detailedInfo.State}");
            AddLog($"[详细] 错误信息: {detailedInfo.ErrorMessage}");
        }

        private void AddLog(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            // 添加到日志构建器
            _logBuilder.AppendLine(logEntry);
            
            // 限制日志行数
            string[] lines = _logBuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > MaxLogLines)
            {
                _logBuilder.Clear();
                for (int i = lines.Length - MaxLogLines; i < lines.Length; i++)
                {
                    _logBuilder.AppendLine(lines[i]);
                }
            }
            
            // 更新文本框
            logTextBox.Text = _logBuilder.ToString();
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
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
            if (bytesPerSecond < 0) return "未知";
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F2} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}天 {timeSpan.Hours}小时";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}小时 {timeSpan.Minutes}分钟";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}分钟 {timeSpan.Seconds}秒";
            else
                return $"{(int)timeSpan.TotalSeconds}秒";
        }

        // 控件声明
        private TabControl tabControl;
        private TabPage generalTab;
        private TabPage peersTab;
        private TabPage trackersTab;
        private TabPage filesTab;
        private TabPage logTab;
        private TabPage dhtTab;
        private Panel generalInfoPanel;
        private Label torrentNameLabel;
        private Label torrentSizeLabel;
        private Label downloadSpeedLabel;
        private Label uploadSpeedLabel;
        private Label progressLabel;
        private Label statusLabel;
        private Label downloadedLabel;
        private Label uploadedLabel;
        private Label timeLeftLabel;
        private Label hashLabel;
        private ProgressBar progressBar;
        private ListView peersListView;
        private ColumnHeader peerAddressColumn;
        private ColumnHeader peerClientColumn;
        private ColumnHeader peerDownSpeedColumn;
        private ColumnHeader peerUpSpeedColumn;
        private ColumnHeader peerProgressColumn;
        private ColumnHeader peerStatusColumn;
        private ListView trackersListView;
        private ColumnHeader trackerUrlColumn;
        private ColumnHeader trackerStatusColumn;
        private ColumnHeader trackerUpdateColumn;
        private ColumnHeader trackerSeedsColumn;
        private ColumnHeader trackerPeersColumn;
        private ListView filesListView;
        private ColumnHeader fileNameColumn;
        private ColumnHeader fileSizeColumn;
        private ColumnHeader fileProgressColumn;
        private ColumnHeader filePriorityColumn;
        private TextBox logTextBox;
        private Button clearLogButton;
        private Button saveLogButton;
        private CheckBox verboseLoggingCheckBox;
        private Panel dhtInfoPanel;
        private Label dhtStatusLabel;
        private Label dhtNodesLabel;
        private ListView dhtListView;
        private ColumnHeader dhtNodeColumn;
        private ColumnHeader dhtStatusColumn;
        private ColumnHeader dhtLastSeenColumn;
    }
}