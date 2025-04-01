namespace Zfile.Forms
{
    public class SyncOptionsForm : Form
    {
        public List<SyncFileInfo> SelectedFiles { get; private set; }
        public SyncDirection SyncDirection { get; private set; }

        private ListView fileListView;
        private RadioButton rbLeftToRight;
        private RadioButton rbRightToLeft;
        private CheckBox chkSelectAll;
        private Button btnOK;
        private Button btnCancel;
        private Label lblStatus;

        private List<SyncFileInfo> allFiles;

        public SyncOptionsForm(List<SyncFileInfo> files)
        {
            allFiles = files;
            SelectedFiles = new List<SyncFileInfo>();
            InitializeComponents();
            InitializeEvents();
            LoadFiles();
        }

        private void InitializeComponents()
        {
            Text = "同步选项";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            rbLeftToRight = new RadioButton
            {
                Text = "左 → 右",
                Location = new Point(10, 10),
                Checked = true
            };

            rbRightToLeft = new RadioButton
            {
                Text = "右 → 左",
                Location = new Point(150, 10)
            };

            chkSelectAll = new CheckBox
            {
                Text = "全选",
                Location = new Point(300, 10)
            };
			btnOK = new Button
			{
				Text = "确定",
				DialogResult = DialogResult.OK,
				Location = new Point(400, 10),
				Size = new Size(75, 23)
			};

			btnCancel = new Button
			{
				Text = "取消",
				DialogResult = DialogResult.Cancel,
				Location = new Point(500, 10),
				Size = new Size(75, 23)
			};
			panel.Controls.AddRange(new Control[] { rbLeftToRight, rbRightToLeft, chkSelectAll, btnOK, btnCancel });

            fileListView = new ListView
            {
                Location = new Point(10, 50),
                Size = new Size(565, 360),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true
            };

            fileListView.Columns.Add("文件名", 200);
            fileListView.Columns.Add("状态", 100);
            fileListView.Columns.Add("大小", 100);
            fileListView.Columns.Add("修改时间", 150);

            lblStatus = new Label
            {
                Location = new Point(10, 420),
                Size = new Size(565, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.AddRange(new Control[] { 
                panel, fileListView, lblStatus
            });
        }

        private void InitializeEvents()
        {
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
            fileListView.ItemChecked += FileListView_ItemChecked;
            FormClosing += SyncOptionsForm_FormClosing;
            rbLeftToRight.CheckedChanged += DirectionRadioButton_CheckedChanged;
            rbRightToLeft.CheckedChanged += DirectionRadioButton_CheckedChanged;
        }

        private void LoadFiles()
        {
            fileListView.BeginUpdate();
            foreach (var file in allFiles)
            {
                if (file.State != SyncFileState.Equal)
                {
                    var item = new ListViewItem(Path.GetFileName(file.RelativePath));
                    item.SubItems.Add(GetStateText(file.State));
                    item.SubItems.Add(file.IsDirectory ? "<DIR>" : FileSystemManager.FormatFileSize(file.Size));
                    item.SubItems.Add(file.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.Tag = file;

                    // 根据同步方向自动选择需要同步的文件
                    bool shouldCheck = ShouldCheckFile(file);
                    item.Checked = shouldCheck;

                    // 设置颜色
                    switch (file.State)
                    {
                        case SyncFileState.NotEqual:
                            item.BackColor = Color.Yellow;
                            break;
                        case SyncFileState.LeftOnly:
                            item.BackColor = Color.LightBlue;
                            break;
                        case SyncFileState.RightOnly:
                            item.BackColor = Color.LightGreen;
                            break;
                        case SyncFileState.LeftNewer:
                            item.BackColor = Color.LightPink;
                            break;
                        case SyncFileState.RightNewer:
                            item.BackColor = Color.LightCoral;
                            break;
                    }

                    fileListView.Items.Add(item);
                }
            }
            fileListView.EndUpdate();
            UpdateStatus();
        }

        private bool ShouldCheckFile(SyncFileInfo file)
        {
            if (rbLeftToRight.Checked)
            {
                return file.State == SyncFileState.LeftOnly ||
                       file.State == SyncFileState.LeftNewer ||
                       file.State == SyncFileState.NotEqual;
            }
            else
            {
                return file.State == SyncFileState.RightOnly ||
                       file.State == SyncFileState.RightNewer ||
                       file.State == SyncFileState.NotEqual;
            }
        }

        private string GetStateText(SyncFileState state)
        {
            return state switch
            {
                SyncFileState.NotEqual => "不同",
                SyncFileState.LeftOnly => "仅左侧",
                SyncFileState.RightOnly => "仅右侧",
                SyncFileState.LeftNewer => "左侧较新",
                SyncFileState.RightNewer => "右侧较新",
                _ => string.Empty
            };
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            fileListView.BeginUpdate();
            foreach (ListViewItem item in fileListView.Items)
            {
                item.Checked = chkSelectAll.Checked;
            }
            fileListView.EndUpdate();
            UpdateStatus();
        }

        private void FileListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateStatus();
        }

        private void DirectionRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SyncDirection = rbLeftToRight.Checked ? 
                SyncDirection.LeftToRight : 
                SyncDirection.RightToLeft;

            // 重新加载文件列表，根据新的同步方向自动选择文件
            fileListView.Items.Clear();
            LoadFiles();
        }

        private void UpdateStatus()
        {
            var checkedCount = fileListView.CheckedItems.Count;
            var totalSize = 0L;
            foreach (ListViewItem item in fileListView.CheckedItems)
            {
                var file = (SyncFileInfo)item.Tag;
                if (!file.IsDirectory)
                    totalSize += file.Size;
            }

            lblStatus.Text = $"已选择 {checkedCount} 个项目，总大小: {FileSystemManager.FormatFileSize(totalSize)}";
            btnOK.Enabled = checkedCount > 0;
        }

        private void SyncOptionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                SelectedFiles.Clear();
                foreach (ListViewItem item in fileListView.CheckedItems)
                {
                    SelectedFiles.Add((SyncFileInfo)item.Tag);
                }

                if (SelectedFiles.Count == 0)
                {
                    MessageBox.Show("请至少选择一个文件进行同步", "提示");
                    e.Cancel = true;
                    return;
                }

                SyncDirection = rbLeftToRight.Checked ? 
                    SyncDirection.LeftToRight : 
                    SyncDirection.RightToLeft;
            }
        }
    }
} 