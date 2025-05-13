using System;
using System.Windows.Forms;

namespace zfile
{
    /// <summary>
    /// 压缩文件属性对话框
    /// </summary>
    public class PackInfoDialog : Form
    {
        private Button btnClose;
        private Button btnUnpackAndExec;
        private Button btnUnpackAllAndExec;
        private Label lblAttributes;
        private Label lblCompressionRatio;
        private Label lblDate;
        private Label lblMethod;
        private Label lblOriginalSize;
        private Label lblPackedFile;
        private Label lblPackedSize;
        private Label lblPacker;
        private Label lblTime;
        private Label lblPackedAttr;
        private Label lblPackedCompression;
        private Label lblPackedDate;
        private TextBox edtPackedFile;
        private Label lblPackedMethod;
        private Label lblPackedOrgSize;
        private Label lblPackedPackedSize;
        private Label lblPackedPacker;
        private Label lblPackedTime;
        private Panel pnlInfoProperties;
        private Panel pnlInfoFile;
        private Panel pnlInfo;
        private Panel pnlButtons;

        private IArchiveFileSource _fileSource;
        private FileEntry _file;

        /// <summary>
        /// 显示压缩文件属性对话框
        /// </summary>
        /// <param name="fileSource">归档文件源</param>
        /// <param name="file">文件条目</param>
        /// <returns>操作结果</returns>
        public static FileSourceExecuteOperationResult Show(IWcxArchiveFileSource fileSource, FileEntry file)
        {
            using (var dialog = new PackInfoDialog(fileSource, file))
            {
                var result = dialog.ShowDialog();

                switch (result)
                {
                    case DialogResult.Cancel:
                        return FileSourceExecuteOperationResult.Cancelled;
                    case DialogResult.OK:
                        return FileSourceExecuteOperationResult.YourSelf;
                    case DialogResult.Yes: // 使用Yes作为"全部"按钮的结果
                        return FileSourceExecuteOperationResult.WithAll;
                    default:
                        return FileSourceExecuteOperationResult.Success;
                }
            }
        }

        private PackInfoDialog(IArchiveFileSource fileSource, FileEntry file)
        {
            _fileSource = fileSource;
            _file = file;

            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            // 设置窗口标题
            Text = "压缩文件属性";

            // 设置按钮状态
            btnUnpackAndExec.Enabled = _fileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.CopyOut);
            btnUnpackAllAndExec.Enabled = _fileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.List) &&
                                         _fileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.CopyOut);

            // 设置文件信息
            edtPackedFile.Text = _file.FullPath;
            lblPackedPacker.Text = _fileSource.Packer;

            // 设置文件大小信息的可见性
            lblPackedOrgSize.Visible = !_file.IsDirectory;
            lblPackedPackedSize.Visible = !_file.IsDirectory;
            lblPackedCompression.Visible = false;
            lblPackedMethod.Visible = false;

            // 如果不是目录，显示文件大小信息
            if (!_file.IsDirectory)
            {
                lblPackedOrgSize.Text = _file.Size.ToString();
                // 压缩后大小可能需要从插件获取
            }

            // 设置文件日期和时间
            if (_file.ModificationTime != DateTime.MinValue)
            {
                lblPackedDate.Text = _file.ModificationTime.ToShortDateString();
                lblPackedTime.Text = _file.ModificationTime.ToShortTimeString();
            }

            // 设置文件属性
            lblPackedAttr.Text = _file.Attributes.ToString();
        }

        private void InitializeComponent()
        {
            // 设置窗口属性
            ClientSize = new System.Drawing.Size(400, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // 创建面板
            pnlInfo = new Panel
            {
                Dock = DockStyle.Fill
            };

            pnlInfoFile = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60
            };

            pnlInfoProperties = new Panel
            {
                Dock = DockStyle.Fill
            };

            pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            // 创建文件信息控件
            lblPackedFile = new Label
            {
                Text = "压缩文件:",
                Location = new System.Drawing.Point(10, 15),
                AutoSize = true
            };

            edtPackedFile = new TextBox
            {
                Location = new System.Drawing.Point(100, 12),
                Width = 280,
                ReadOnly = true
            };

            // 创建属性标签
            lblPacker = new Label
            {
                Text = "压缩器:",
                Location = new System.Drawing.Point(10, 20),
                AutoSize = true
            };

            lblPackedPacker = new Label
            {
                Location = new System.Drawing.Point(100, 20),
                AutoSize = true
            };

            lblOriginalSize = new Label
            {
                Text = "原始大小:",
                Location = new System.Drawing.Point(10, 45),
                AutoSize = true
            };

            lblPackedOrgSize = new Label
            {
                Location = new System.Drawing.Point(100, 45),
                AutoSize = true
            };

            lblPackedSize = new Label
            {
                Text = "压缩后大小:",
                Location = new System.Drawing.Point(10, 70),
                AutoSize = true
            };

            lblPackedPackedSize = new Label
            {
                Location = new System.Drawing.Point(100, 70),
                AutoSize = true
            };

            lblCompressionRatio = new Label
            {
                Text = "压缩比率:",
                Location = new System.Drawing.Point(10, 95),
                AutoSize = true,
                Visible = false
            };

            lblPackedCompression = new Label
            {
                Location = new System.Drawing.Point(100, 95),
                AutoSize = true,
                Visible = false
            };

            lblMethod = new Label
            {
                Text = "压缩方法:",
                Location = new System.Drawing.Point(10, 120),
                AutoSize = true,
                Visible = false
            };

            lblPackedMethod = new Label
            {
                Location = new System.Drawing.Point(100, 120),
                AutoSize = true,
                Visible = false
            };

            lblDate = new Label
            {
                Text = "日期:",
                Location = new System.Drawing.Point(10, 145),
                AutoSize = true
            };

            lblPackedDate = new Label
            {
                Location = new System.Drawing.Point(100, 145),
                AutoSize = true
            };

            lblTime = new Label
            {
                Text = "时间:",
                Location = new System.Drawing.Point(10, 170),
                AutoSize = true
            };

            lblPackedTime = new Label
            {
                Location = new System.Drawing.Point(100, 170),
                AutoSize = true
            };

            lblAttributes = new Label
            {
                Text = "属性:",
                Location = new System.Drawing.Point(10, 195),
                AutoSize = true
            };

            lblPackedAttr = new Label
            {
                Location = new System.Drawing.Point(100, 195),
                AutoSize = true
            };

            // 创建按钮
            btnClose = new Button
            {
                Text = "关闭",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(310, 10),
                Size = new System.Drawing.Size(80, 25)
            };

            btnUnpackAndExec = new Button
            {
                Text = "解压并执行",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(120, 25)
            };

            btnUnpackAllAndExec = new Button
            {
                Text = "全部解压并执行",
                DialogResult = DialogResult.Yes,
                Location = new System.Drawing.Point(140, 10),
                Size = new System.Drawing.Size(160, 25)
            };

            // 添加控件到面板
            pnlInfoFile.Controls.Add(lblPackedFile);
            pnlInfoFile.Controls.Add(edtPackedFile);

            pnlInfoProperties.Controls.Add(lblPacker);
            pnlInfoProperties.Controls.Add(lblPackedPacker);
            pnlInfoProperties.Controls.Add(lblOriginalSize);
            pnlInfoProperties.Controls.Add(lblPackedOrgSize);
            pnlInfoProperties.Controls.Add(lblPackedSize);
            pnlInfoProperties.Controls.Add(lblPackedPackedSize);
            pnlInfoProperties.Controls.Add(lblCompressionRatio);
            pnlInfoProperties.Controls.Add(lblPackedCompression);
            pnlInfoProperties.Controls.Add(lblMethod);
            pnlInfoProperties.Controls.Add(lblPackedMethod);
            pnlInfoProperties.Controls.Add(lblDate);
            pnlInfoProperties.Controls.Add(lblPackedDate);
            pnlInfoProperties.Controls.Add(lblTime);
            pnlInfoProperties.Controls.Add(lblPackedTime);
            pnlInfoProperties.Controls.Add(lblAttributes);
            pnlInfoProperties.Controls.Add(lblPackedAttr);

            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Controls.Add(btnUnpackAndExec);
            pnlButtons.Controls.Add(btnUnpackAllAndExec);

            pnlInfo.Controls.Add(pnlInfoProperties);
            pnlInfo.Controls.Add(pnlInfoFile);

            // 添加面板到窗体
            Controls.Add(pnlInfo);
            Controls.Add(pnlButtons);

            // 设置默认按钮和取消按钮
            AcceptButton = btnUnpackAndExec;
            CancelButton = btnClose;
        }
    }
}
