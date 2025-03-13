using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace zfile
{
    public class CommandEditDialog : Form
    {
        private TextBox txtCommand;
        private Button btnSelectCommand;
        private TextBox txtParams;
        private TextBox txtWorkingDir;
        private TextBox txtIconFile;
        private Button btnSelectIconFile;
        private FlowLayoutPanel iconPanel;
        private TextBox txtTooltip;
        private Button btnOK;
        private Button btnCancel;
        private CmdProc cmdProcessor;
		private MenuInfo? _cmdItem;

        public string Command { get; private set; }
        public string Parameters { get; private set; }
        public string WorkingDirectory { get; private set; }
        public string IconFile { get; private set; }
        public string Tooltip { get; private set; }
        public int SelectedIconIndex { get; private set; } = -1;

        public CommandEditDialog(CmdProc cmdProcessor, MenuInfo? cmdItem = null)
        {
			this._cmdItem = cmdItem;
            this.cmdProcessor = cmdProcessor;
            InitializeComponents();

            // 如果是编辑现有命令，则填充表单
            if (cmdItem != null)
            {
                txtCommand.Text = cmdItem.Cmd;
                txtParams.Text = cmdItem.Param;
                txtWorkingDir.Text = cmdItem.Path;
                txtTooltip.Text = cmdItem.Menu;
                // 加载图标文件
                if (!string.IsNullOrEmpty((string)cmdItem.Button))
                {
					txtIconFile.Text = cmdItem.Button;
					LoadIconsFromFile((string)cmdItem.Button);
                }
            }
        }

        private void InitializeComponents()
        {
            // 设置窗体属性
            this.Text = "编辑命令" + (_cmdItem != null ? _cmdItem.Name : "");
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 创建标签和输入控件
            var lblCommand = new Label { Text = "命令:", Location = new Point(20, 20), Width = 80 };
            txtCommand = new TextBox { Location = new Point(120, 20), Width = 350 };
            btnSelectCommand = new Button { Text = "选择命令", Location = new Point(480, 20), Width = 80 };

            var lblParams = new Label { Text = "参数:", Location = new Point(20, 60), Width = 80 };
            txtParams = new TextBox { Location = new Point(120, 60), Width = 440 };

            var lblWorkingDir = new Label { Text = "启动路径:", Location = new Point(20, 100), Width = 80 };
            txtWorkingDir = new TextBox { Location = new Point(120, 100), Width = 440 };

            var lblIconFile = new Label { Text = "图标文件:", Location = new Point(20, 140), Width = 80 };
            txtIconFile = new TextBox { Location = new Point(120, 140), Width = 350 };
            btnSelectIconFile = new Button { Text = "选择文件", Location = new Point(480, 140), Width = 80 };

            var lblIcon = new Label { Text = "图标:", Location = new Point(20, 180), Width = 80 };
            iconPanel = new FlowLayoutPanel
            {
                Location = new Point(120, 180),
                Size = new Size(440, 120),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            var lblTooltip = new Label { Text = "提示:", Location = new Point(20, 320), Width = 80 };
            txtTooltip = new TextBox { Location = new Point(120, 320), Width = 440 };

            // 创建按钮
            btnOK = new Button
            {
                Text = "确定",
                Location = new Point(200, 400),
                Width = 80,
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(300, 400),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            // 添加事件处理
            btnSelectCommand.Click += BtnSelectCommand_Click;
            btnSelectIconFile.Click += BtnSelectIconFile_Click;
            btnOK.Click += BtnOK_Click;

            // 添加控件到窗体
            this.Controls.AddRange(new Control[]
            {
                lblCommand, txtCommand, btnSelectCommand,
                lblParams, txtParams,
                lblWorkingDir, txtWorkingDir,
                lblIconFile, txtIconFile, btnSelectIconFile,
                lblIcon, iconPanel,
                lblTooltip, txtTooltip,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnSelectCommand_Click(object sender, EventArgs e)
        {
            // 打开命令浏览器
            var commandBrowser = new CommandBrowserForm(cmdProcessor);
            if (commandBrowser.ShowDialog() == DialogResult.OK)
            {
                // 获取选中的命令
                // 这里需要修改CommandBrowserForm以支持返回选中的命令
            }
        }

        private void BtnSelectIconFile_Click(object sender, EventArgs e)
        {
            // 打开文件选择对话框
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图标文件 (*.ico;*.dll;*.exe)|*.ico;*.dll;*.exe|所有文件 (*.*)|*.*",
                Title = "选择图标文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtIconFile.Text = openFileDialog.FileName;
                LoadIconsFromFile(openFileDialog.FileName);
            }
        }

        private void LoadIconsFromFile(string filePath)
        {
            iconPanel.Controls.Clear();
            SelectedIconIndex = -1;

            try
            {
                if (File.Exists(filePath))
                {
                    // 获取文件中的所有图标
                    Icon[] icons = null;
                    
                    if (Path.GetExtension(filePath).ToLower() == ".ico")
                    {
                        // 单个图标文件
                        icons = new Icon[] { new Icon(filePath) };
                    }
                    else
                    {
                        // DLL或EXE文件中的图标
                        icons = IconManager.ExtractIconsFromFile(filePath);
                    }

                    if (icons != null && icons.Length > 0)
                    {
                        for (int i = 0; i < icons.Length; i++)
                        {
                            var icon = icons[i];
                            var pictureBox = new PictureBox
                            {
                                Image = icon.ToBitmap(),
                                SizeMode = PictureBoxSizeMode.AutoSize,
                                Margin = new Padding(5),
                                Tag = i,
                                BorderStyle = BorderStyle.FixedSingle,
                                Cursor = Cursors.Hand
                            };

                            pictureBox.Click += (s, e) =>
                            {
                                // 选中图标
                                foreach (PictureBox pb in iconPanel.Controls)
                                {
                                    pb.BorderStyle = BorderStyle.FixedSingle;
                                }
                                ((PictureBox)s).BorderStyle = BorderStyle.Fixed3D;
                                SelectedIconIndex = (int)((PictureBox)s).Tag;
                            };

                            iconPanel.Controls.Add(pictureBox);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图标时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

     

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtCommand.Text))
            {
                MessageBox.Show("请输入命令名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // 保存输入值
            Command = txtCommand.Text;
            Parameters = txtParams.Text;
            WorkingDirectory = txtWorkingDir.Text;
            IconFile = txtIconFile.Text;
            Tooltip = txtTooltip.Text;

            // 关闭对话框
            DialogResult = DialogResult.OK;
        }
    }
}