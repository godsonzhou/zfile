using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MonoTorrent;
using MonoTorrent.Client;

namespace Zfile.Forms
{
    /// <summary>
    /// 种子下载对话框，用于添加磁力链接或种子文件下载
    /// </summary>
    public partial class TorrentDownloadDialog : Form
    {
        private string _magnetLink;
        private string _torrentFilePath;
        private bool _isMagnetLink;
        private Torrent _loadedTorrent;
        private MagnetLink _parsedMagnetLink;

        public string SavePath { get; private set; }
        public string TorrentFilePath => _torrentFilePath;
        public string MagnetLink => _magnetLink;
        public bool IsMagnetLink => _isMagnetLink;
        public List<int> SelectedFileIndices { get; private set; } = new List<int>();

        /// <summary>
        /// 创建磁力链接下载对话框
        /// </summary>
        /// <param name="magnetLink">磁力链接</param>
        public TorrentDownloadDialog(string magnetLink)
        {
            _magnetLink = magnetLink;
            _isMagnetLink = true;
            InitializeComponent();
            magnetLinkTextBox.Text = magnetLink;
            tabControl.SelectedIndex = 0; // 选择磁力链接标签页
        }

        /// <summary>
        /// 创建种子文件下载对话框
        /// </summary>
        /// <param name="torrentFilePath">种子文件路径</param>
        public TorrentDownloadDialog(string torrentFilePath, bool isTorrentFile)
        {
            if (isTorrentFile)
            {
                _torrentFilePath = torrentFilePath;
                _isMagnetLink = false;
            }
            else
            {
                _magnetLink = torrentFilePath;
                _isMagnetLink = true;
            }
            
            InitializeComponent();
            
            if (_isMagnetLink)
            {
                magnetLinkTextBox.Text = _magnetLink;
                tabControl.SelectedIndex = 0; // 选择磁力链接标签页
            }
            else
            {
                torrentFilePathTextBox.Text = _torrentFilePath;
                tabControl.SelectedIndex = 1; // 选择种子文件标签页
                LoadTorrentFileInfo();
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TorrentDownloadDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.tabControl = new TabControl();
            this.magnetLinkTab = new TabPage();
            this.torrentFileTab = new TabPage();
            this.magnetLinkTextBox = new TextBox();
            this.magnetLinkLabel = new Label();
            this.torrentFilePathTextBox = new TextBox();
            this.torrentFileLabel = new Label();
            this.browseTorrentButton = new Button();
            this.savePathTextBox = new TextBox();
            this.savePathLabel = new Label();
            this.browseSavePathButton = new Button();
            this.fileListView = new ListView();
            this.fileListLabel = new Label();
            this.selectAllButton = new Button();
            this.deselectAllButton = new Button();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.torrentInfoGroupBox = new GroupBox();
            this.torrentNameLabel = new Label();
            this.torrentSizeLabel = new Label();
            this.torrentFilesCountLabel = new Label();
            this.torrentNameValueLabel = new Label();
            this.torrentSizeValueLabel = new Label();
            this.torrentFilesCountValueLabel = new Label();

            this.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.magnetLinkTab.SuspendLayout();
            this.torrentFileTab.SuspendLayout();
            this.torrentInfoGroupBox.SuspendLayout();

            // TabControl
            this.tabControl.Controls.Add(this.magnetLinkTab);
            this.tabControl.Controls.Add(this.torrentFileTab);
            this.tabControl.Location = new Point(12, 12);
            this.tabControl.Size = new Size(560, 100);
            this.tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // MagnetLinkTab
            this.magnetLinkTab.Controls.Add(this.magnetLinkTextBox);
            this.magnetLinkTab.Controls.Add(this.magnetLinkLabel);
            this.magnetLinkTab.Location = new Point(4, 24);
            this.magnetLinkTab.Size = new Size(552, 72);
            this.magnetLinkTab.Text = "磁力链接";
            this.magnetLinkTab.UseVisualStyleBackColor = true;

            // MagnetLinkLabel
            this.magnetLinkLabel.AutoSize = true;
            this.magnetLinkLabel.Location = new Point(10, 15);
            this.magnetLinkLabel.Size = new Size(100, 15);
            this.magnetLinkLabel.Text = "磁力链接:";

            // MagnetLinkTextBox
            this.magnetLinkTextBox.Location = new Point(10, 35);
            this.magnetLinkTextBox.Size = new Size(530, 23);
            this.magnetLinkTextBox.TextChanged += MagnetLinkTextBox_TextChanged;

            // TorrentFileTab
            this.torrentFileTab.Controls.Add(this.torrentFilePathTextBox);
            this.torrentFileTab.Controls.Add(this.torrentFileLabel);
            this.torrentFileTab.Controls.Add(this.browseTorrentButton);
            this.torrentFileTab.Location = new Point(4, 24);
            this.torrentFileTab.Size = new Size(552, 72);
            this.torrentFileTab.Text = "种子文件";
            this.torrentFileTab.UseVisualStyleBackColor = true;

            // TorrentFileLabel
            this.torrentFileLabel.AutoSize = true;
            this.torrentFileLabel.Location = new Point(10, 15);
            this.torrentFileLabel.Size = new Size(100, 15);
            this.torrentFileLabel.Text = "种子文件:";

            // TorrentFilePathTextBox
            this.torrentFilePathTextBox.Location = new Point(10, 35);
            this.torrentFilePathTextBox.Size = new Size(450, 23);
            this.torrentFilePathTextBox.ReadOnly = true;

            // BrowseTorrentButton
            this.browseTorrentButton.Location = new Point(470, 35);
            this.browseTorrentButton.Size = new Size(70, 23);
            this.browseTorrentButton.Text = "浏览...";
            this.browseTorrentButton.UseVisualStyleBackColor = true;
            this.browseTorrentButton.Click += BrowseTorrentButton_Click;

            // SavePathLabel
            this.savePathLabel.AutoSize = true;
            this.savePathLabel.Location = new Point(16, 125);
            this.savePathLabel.Size = new Size(100, 15);
            this.savePathLabel.Text = "保存位置:";

            // SavePathTextBox
            this.savePathTextBox.Location = new Point(16, 145);
            this.savePathTextBox.Size = new Size(470, 23);

            // BrowseSavePathButton
            this.browseSavePathButton.Location = new Point(496, 145);
            this.browseSavePathButton.Size = new Size(70, 23);
            this.browseSavePathButton.Text = "浏览...";
            this.browseSavePathButton.UseVisualStyleBackColor = true;
            this.browseSavePathButton.Click += BrowseSavePathButton_Click;

            // TorrentInfoGroupBox
            this.torrentInfoGroupBox.Controls.Add(this.torrentNameLabel);
            this.torrentInfoGroupBox.Controls.Add(this.torrentSizeLabel);
            this.torrentInfoGroupBox.Controls.Add(this.torrentFilesCountLabel);
            this.torrentInfoGroupBox.Controls.Add(this.torrentNameValueLabel);
            this.torrentInfoGroupBox.Controls.Add(this.torrentSizeValueLabel);
            this.torrentInfoGroupBox.Controls.Add(this.torrentFilesCountValueLabel);
            this.torrentInfoGroupBox.Location = new Point(16, 180);
            this.torrentInfoGroupBox.Size = new Size(550, 100);
            this.torrentInfoGroupBox.Text = "种子信息";

            // TorrentNameLabel
            this.torrentNameLabel.AutoSize = true;
            this.torrentNameLabel.Location = new Point(20, 25);
            this.torrentNameLabel.Size = new Size(80, 15);
            this.torrentNameLabel.Text = "名称:";

            // TorrentNameValueLabel
            this.torrentNameValueLabel.AutoSize = true;
            this.torrentNameValueLabel.Location = new Point(120, 25);
            this.torrentNameValueLabel.Size = new Size(400, 15);
            this.torrentNameValueLabel.Text = "未知";

            // TorrentSizeLabel
            this.torrentSizeLabel.AutoSize = true;
            this.torrentSizeLabel.Location = new Point(20, 50);
            this.torrentSizeLabel.Size = new Size(80, 15);
            this.torrentSizeLabel.Text = "大小:";

            // TorrentSizeValueLabel
            this.torrentSizeValueLabel.AutoSize = true;
            this.torrentSizeValueLabel.Location = new Point(120, 50);
            this.torrentSizeValueLabel.Size = new Size(400, 15);
            this.torrentSizeValueLabel.Text = "未知";

            // TorrentFilesCountLabel
            this.torrentFilesCountLabel.AutoSize = true;
            this.torrentFilesCountLabel.Location = new Point(20, 75);
            this.torrentFilesCountLabel.Size = new Size(80, 15);
            this.torrentFilesCountLabel.Text = "文件数:";

            // TorrentFilesCountValueLabel
            this.torrentFilesCountValueLabel.AutoSize = true;
            this.torrentFilesCountValueLabel.Location = new Point(120, 75);
            this.torrentFilesCountValueLabel.Size = new Size(400, 15);
            this.torrentFilesCountValueLabel.Text = "未知";

            // FileListLabel
            this.fileListLabel.AutoSize = true;
            this.fileListLabel.Location = new Point(16, 290);
            this.fileListLabel.Size = new Size(100, 15);
            this.fileListLabel.Text = "文件列表:";

            // FileListView
            this.fileListView.CheckBoxes = true;
            this.fileListView.FullRowSelect = true;
            this.fileListView.HideSelection = false;
            this.fileListView.Location = new Point(16, 310);
            this.fileListView.Size = new Size(550, 150);
            this.fileListView.View = View.Details;
            this.fileListView.Columns.Add("文件名", 350);
            this.fileListView.Columns.Add("大小", 100);
            this.fileListView.Columns.Add("路径", 100);

            // SelectAllButton
            this.selectAllButton.Location = new Point(16, 470);
            this.selectAllButton.Size = new Size(80, 23);
            this.selectAllButton.Text = "全选";
            this.selectAllButton.UseVisualStyleBackColor = true;
            this.selectAllButton.Click += SelectAllButton_Click;

            // DeselectAllButton
            this.deselectAllButton.Location = new Point(106, 470);
            this.deselectAllButton.Size = new Size(80, 23);
            this.deselectAllButton.Text = "全不选";
            this.deselectAllButton.UseVisualStyleBackColor = true;
            this.deselectAllButton.Click += DeselectAllButton_Click;

            // OkButton
            this.okButton.Location = new Point(400, 470);
            this.okButton.Size = new Size(80, 23);
            this.okButton.Text = "确定";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.DialogResult = DialogResult.OK;
            this.okButton.Click += OkButton_Click;

            // CancelButton
            this.cancelButton.Location = new Point(486, 470);
            this.cancelButton.Size = new Size(80, 23);
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.DialogResult = DialogResult.Cancel;

            // Form
            this.AcceptButton = this.okButton;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new Size(584, 511);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.savePathLabel);
            this.Controls.Add(this.savePathTextBox);
            this.Controls.Add(this.browseSavePathButton);
            this.Controls.Add(this.torrentInfoGroupBox);
            this.Controls.Add(this.fileListLabel);
            this.Controls.Add(this.fileListView);
            this.Controls.Add(this.selectAllButton);
            this.Controls.Add(this.deselectAllButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TorrentDownloadDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "BT下载";
            this.Load += TorrentDownloadDialog_Load;

            this.tabControl.ResumeLayout(false);
            this.magnetLinkTab.ResumeLayout(false);
            this.magnetLinkTab.PerformLayout();
            this.torrentFileTab.ResumeLayout(false);
            this.torrentFileTab.PerformLayout();
            this.torrentInfoGroupBox.ResumeLayout(false);
            this.torrentInfoGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TorrentDownloadDialog_Load(object sender, EventArgs e)
        {
            // 设置默认保存路径
            savePathTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "Downloads");

            // 如果已经有磁力链接，尝试解析
            if (!string.IsNullOrEmpty(_magnetLink))
            {
                ParseMagnetLink();
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            _isMagnetLink = tabControl.SelectedIndex == 0;
            UpdateFileList();
        }

        private void MagnetLinkTextBox_TextChanged(object sender, EventArgs e)
        {
            _magnetLink = magnetLinkTextBox.Text;
            ParseMagnetLink();
        }

        private void BrowseTorrentButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "种子文件 (*.torrent)|*.torrent|所有文件 (*.*)|*.*";
                dialog.Title = "选择种子文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _torrentFilePath = dialog.FileName;
                    torrentFilePathTextBox.Text = _torrentFilePath;
                    LoadTorrentFileInfo();
                }
            }
        }

        private void BrowseSavePathButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择保存位置";
                dialog.UseDescriptionForTitle = true;
                dialog.SelectedPath = savePathTextBox.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    savePathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in fileListView.Items)
            {
                item.Checked = true;
            }
        }

        private void DeselectAllButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in fileListView.Items)
            {
                item.Checked = false;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (_isMagnetLink && string.IsNullOrWhiteSpace(_magnetLink))
            {
                MessageBox.Show("请输入磁力链接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            if (!_isMagnetLink && string.IsNullOrWhiteSpace(_torrentFilePath))
            {
                MessageBox.Show("请选择种子文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(savePathTextBox.Text))
            {
                MessageBox.Show("请选择保存位置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            // 保存选择的文件索引
            SelectedFileIndices.Clear();
            for (int i = 0; i < fileListView.Items.Count; i++)
            {
                if (fileListView.Items[i].Checked)
                {
                    SelectedFileIndices.Add(i);
                }
            }

            // 如果没有选择任何文件，提示用户
            if (SelectedFileIndices.Count == 0 && fileListView.Items.Count > 0)
            {
                var result = MessageBox.Show("您没有选择任何文件下载，是否继续？", "确认", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            // 保存路径
            SavePath = savePathTextBox.Text;
        }

        private async void LoadTorrentFileInfo()
        {
            if (string.IsNullOrEmpty(_torrentFilePath) || !File.Exists(_torrentFilePath))
                return;

            try
            {
                // 加载种子文件
                _loadedTorrent = await Torrent.LoadAsync(_torrentFilePath);

                // 更新种子信息
                UpdateTorrentInfo(_loadedTorrent);

                // 更新文件列表
                UpdateFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载种子文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ParseMagnetLink()
        {
            if (string.IsNullOrEmpty(_magnetLink))
                return;

            try
            {
                // 解析磁力链接
                var magnetLinkParser = new MagnetLinkParser();
                _parsedMagnetLink = magnetLinkParser.Parse(_magnetLink);

                // 更新种子信息
                torrentNameValueLabel.Text = _parsedMagnetLink.Name ?? "未知";
                torrentSizeValueLabel.Text = "未知 (需要连接到网络获取)";
                torrentFilesCountValueLabel.Text = "未知 (需要连接到网络获取)";

                // 清空文件列表
                fileListView.Items.Clear();
            }
            catch (Exception ex)
            {
                // 解析失败，不显示错误，等用户点击确定时再验证
            }
        }

        private void UpdateTorrentInfo(Torrent torrent)
        {
            if (torrent == null)
                return;

            torrentNameValueLabel.Text = torrent.Name;
            torrentSizeValueLabel.Text = FormatFileSize(torrent.Size);
            torrentFilesCountValueLabel.Text = torrent.Files.Count.ToString();
        }

        private void UpdateFileList()
        {
            fileListView.Items.Clear();

            if (_isMagnetLink)
            {
                // 磁力链接无法预先获取文件列表
                return;
            }

            if (_loadedTorrent == null)
                return;

            foreach (var file in _loadedTorrent.Files)
            {
                var item = new ListViewItem(Path.GetFileName(file.Path));
                item.SubItems.Add(FormatFileSize(file.Length));
                item.SubItems.Add(Path.GetDirectoryName(file.Path));
                item.Checked = true; // 默认选中所有文件
                fileListView.Items.Add(item);
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

        // 控件声明
        private TabControl tabControl;
        private TabPage magnetLinkTab;
        private TabPage torrentFileTab;
        private TextBox magnetLinkTextBox;
        private Label magnetLinkLabel;
        private TextBox torrentFilePathTextBox;
        private Label torrentFileLabel;
        private Button browseTorrentButton;
        private TextBox savePathTextBox;
        private Label savePathLabel;
        private Button browseSavePathButton;
        private ListView fileListView;
        private Label fileListLabel;
        private Button selectAllButton;
        private Button deselectAllButton;
        private Button okButton;
        private Button cancelButton;
        private GroupBox torrentInfoGroupBox;
        private Label torrentNameLabel;
        private Label torrentSizeLabel;
        private Label torrentFilesCountLabel;
        private Label torrentNameValueLabel;
        private Label torrentSizeValueLabel;
        private Label torrentFilesCountValueLabel;
    }
}