using System;
using System.Drawing;
using System.Windows.Forms;

namespace zfile.Forms
{
    public class PackOptionDialog : Form
    {
        public CheckBox chkIncludePath = null!;
        public CheckBox chkRecursive = null!;
        public CheckBox chkMultiVolume = null!;
        public CheckBox chkMoveToArchive = null!;
        public CheckBox chkSelfExtract = null!;
        public CheckBox chkSeparateArchives = null!;
        public CheckBox chkExcludeBaseDir = null!;
        public CheckBox chkEncrypt = null!;
        public CheckBox chkTarBefore = null!;
        public ComboBox cboCompressMethod = null!;
        public Button btnOK = null!;
        public Button btnCancel = null!;
        public Button btnConfig = null!;

        public bool IncludePath => chkIncludePath.Checked;
        public bool Recursive => chkRecursive.Checked;
        public bool MultiVolume => chkMultiVolume.Checked;
        public bool MoveToArchive => chkMoveToArchive.Checked;
        public bool SelfExtract => chkSelfExtract.Checked;
        public bool SeparateArchives => chkSeparateArchives.Checked;
        public bool ExcludeBaseDir => chkExcludeBaseDir.Checked;
        public bool Encrypt => chkEncrypt.Checked;
        public bool TarBefore => chkTarBefore.Checked;
        public string CompressMethod => cboCompressMethod.SelectedItem?.ToString() ?? "ZIP";

        public PackOptionDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "压缩选项";
            Size = new Size(400, 550);
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

            chkTarBefore = new CheckBox
            {
                Text = "先打包成TAR",
                Location = new Point(20, 260),
                AutoSize = true
            };

            // 创建压缩方式选择下拉框
            var lblCompressMethod = new Label
            {
                Text = "压缩方式选择（带*需要外部压缩程序）：",
                Location = new Point(20, 290),
                AutoSize = true
            };

            cboCompressMethod = new ComboBox
            {
                Location = new Point(20, 320),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboCompressMethod.Items.AddRange(new[]
            {
                "ZIP", "RAR*", "TAR", "ARJ*", "UC2*", "GZ", "LHA*", "ACE*", "TGZ", "压缩插件"
            });
            cboCompressMethod.SelectedIndex = 0;

            // 创建配置按钮
            btnConfig = new Button
            {
                Text = "配置",
                Location = new Point(20, 360),
                Width = 80
            };
            btnConfig.Click += BtnConfig_Click;

            // 创建按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(0, 400),
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

            buttonPanel.Controls.AddRange(new[] { btnCancel, btnOK });

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
                chkTarBefore,
                lblCompressMethod,
                cboCompressMethod,
                btnConfig,
                buttonPanel
            });

            Controls.Add(panel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            // 默认禁用一些选项，等待根据插件能力启用
            chkEncrypt.Enabled = false;
            chkMultiVolume.Enabled = false;
            chkSelfExtract.Enabled = false;
            chkTarBefore.Enabled = false;
            btnConfig.Enabled = false;
        }

        private void BtnConfig_Click(object sender, EventArgs e)
        {
            try
            {
                string archiveType = CompressMethod.ToLower();
                if (archiveType.EndsWith('*'))
                    archiveType = archiveType.TrimEnd('*');

                var wcxModule = WcxPlugins._moduleList?.GetModuleByExt(archiveType);
                if (wcxModule != null)
                {
                    // 调用WCX插件的配置界面
                    wcxModule.VFSConfigure(Handle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 根据插件能力启用或禁用选项
        public void EnableEncrypt(bool enable)
        {
            chkEncrypt.Enabled = enable;
            if (!enable)
                chkEncrypt.Checked = false;
        }

        public void EnableMultiple(bool enable)
        {
            // 如果不支持多文件，则强制使用单独压缩
            if (!enable)
            {
                chkSeparateArchives.Checked = true;
                chkSeparateArchives.Enabled = false;
            }
        }

        public static void EnableModify(bool enable)
        {
            // 修改能力不直接对应UI控件，但可能影响其他选项
        }

        public static void EnableDelete(bool enable)
        {
            // 删除能力不直接对应UI控件，但可能影响其他选项
        }

        public void EnableOptions(bool enable)
        {
            btnConfig.Enabled = enable;
        }

        public static void EnableMemPack(bool enable)
        {
            // 内存打包能力不直接对应UI控件
        }

        public void EnableSeparateArchives(bool enable)
        {
            chkSeparateArchives.Enabled = enable;
        }

        public void SetSeparateArchives(bool value)
        {
            chkSeparateArchives.Checked = value;
        }

        public void EnableTarBefore(bool enable)
        {
            chkTarBefore.Enabled = enable;
        }

        public void SetTarBefore(bool value)
        {
            chkTarBefore.Checked = value;
        }
    }
}