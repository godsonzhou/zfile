using System;
using System.Drawing;
using System.Windows.Forms;

namespace zfile
{
    public class PackOptionDialog : Form
    {
        private CheckBox chkIncludePath;
        private CheckBox chkRecursive;
        private CheckBox chkMultiVolume;
        private CheckBox chkMoveToArchive;
        private CheckBox chkSelfExtract;
        private CheckBox chkSeparateArchives;
        private CheckBox chkExcludeBaseDir;
        private CheckBox chkEncrypt;
        private ComboBox cboCompressMethod;
        private Button btnOK;
        private Button btnCancel;

        public bool IncludePath => chkIncludePath.Checked;
        public bool Recursive => chkRecursive.Checked;
        public bool MultiVolume => chkMultiVolume.Checked;
        public bool MoveToArchive => chkMoveToArchive.Checked;
        public bool SelfExtract => chkSelfExtract.Checked;
        public bool SeparateArchives => chkSeparateArchives.Checked;
        public bool ExcludeBaseDir => chkExcludeBaseDir.Checked;
        public bool Encrypt => chkEncrypt.Checked;
        public string CompressMethod => cboCompressMethod.SelectedItem?.ToString() ?? "ZIP";

        public PackOptionDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "压缩选项";
            Size = new Size(400, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 创建选项复选框
            chkIncludePath = new CheckBox
            {
                Text = "包括路径名(P)",
                Location = new Point(20, 20),
                AutoSize = true
            };

            chkRecursive = new CheckBox
            {
                Text = "递归压缩子文件夹(S)",
                Location = new Point(20, 50),
                AutoSize = true
            };

            chkMultiVolume = new CheckBox
            {
                Text = "多卷压缩",
                Location = new Point(20, 80),
                AutoSize = true
            };

            chkMoveToArchive = new CheckBox
            {
                Text = "移动到压缩文件(M)",
                Location = new Point(20, 110),
                AutoSize = true
            };

            chkSelfExtract = new CheckBox
            {
                Text = "创建自解压缩文件",
                Location = new Point(20, 140),
                AutoSize = true
            };

            chkSeparateArchives = new CheckBox
            {
                Text = "为每个项目创建单独的压缩文件(N)",
                Location = new Point(20, 170),
                AutoSize = true
            };

            chkExcludeBaseDir = new CheckBox
            {
                Text = "压缩文件夹时不包括基文件夹",
                Location = new Point(20, 200),
                AutoSize = true
            };

            chkEncrypt = new CheckBox
            {
                Text = "加密",
                Location = new Point(20, 230),
                AutoSize = true
            };

            // 创建压缩方式选择下拉框
            var lblCompressMethod = new Label
            {
                Text = "压缩方式选择（带*需要外部压缩程序）：",
                Location = new Point(20, 270),
                AutoSize = true
            };

            cboCompressMethod = new ComboBox
            {
                Location = new Point(20, 300),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboCompressMethod.Items.AddRange(new string[]
            {
                "ZIP", "RAR*", "TAR", "ARJ*", "UC2*", "GZ", "LHA*", "ACE*", "TGZ", "压缩插件"
            });
            cboCompressMethod.SelectedIndex = 0;

            // 创建按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(0, 350),
                Width = 380,
                Height = 40
            };

            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Width = 80
            };

            btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Width = 80
            };

            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

            // 添加所有控件到面板
            panel.Controls.AddRange(new Control[]
            {
                chkIncludePath,
                chkRecursive,
                chkMultiVolume,
                chkMoveToArchive,
                chkSelfExtract,
                chkSeparateArchives,
                chkExcludeBaseDir,
                chkEncrypt,
                lblCompressMethod,
                cboCompressMethod,
                buttonPanel
            });

            Controls.Add(panel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
} 