namespace Zfile
{
    public partial class LicenseGeneratorForm : Form
    {
        private readonly TextBox hardwareIdTextBox;
        private readonly TextBox licenseKeyTextBox;
        private readonly Button generateButton;
        private readonly Button copyButton;
        private readonly Button getHardwareIdButton;
        private readonly Button saveButton;
        private readonly Button closeButton;
        private readonly Label statusLabel;

        public LicenseGeneratorForm()
        {
            // 设置窗体基本属性
            Text = "授权码生成工具";
            Size = new Size(600, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            // 创建控件
            var hardwareIdLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(100, 20),
                Text = "硬件ID:"
            };

            hardwareIdTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(540, 60),
                Multiline = true
            };

            getHardwareIdButton = new Button
            {
                Location = new Point(20, 115),
                Size = new Size(150, 30),
                Text = "获取当前机器硬件ID"
            };
            getHardwareIdButton.Click += GetHardwareIdButton_Click;

            var licenseKeyLabel = new Label
            {
                Location = new Point(20, 155),
                Size = new Size(100, 20),
                Text = "生成的授权码:"
            };

            licenseKeyTextBox = new TextBox
            {
                Location = new Point(20, 180),
                Size = new Size(540, 60),
                Multiline = true,
                ReadOnly = true
            };

            generateButton = new Button
            {
                Location = new Point(20, 250),
                Size = new Size(120, 30),
                Text = "生成授权码"
            };
            generateButton.Click += GenerateButton_Click;

            copyButton = new Button
            {
                Location = new Point(150, 250),
                Size = new Size(120, 30),
                Text = "复制授权码"
            };
            copyButton.Click += CopyButton_Click;

            saveButton = new Button
            {
                Location = new Point(280, 250),
                Size = new Size(120, 30),
                Text = "保存授权码"
            };
            saveButton.Click += SaveButton_Click;

            statusLabel = new Label
            {
                Location = new Point(20, 290),
                Size = new Size(540, 20),
                Text = "准备就绪"
            };

            closeButton = new Button
            {
                Location = new Point(440, 320),
                Size = new Size(120, 30),
                Text = "关闭"
            };
            closeButton.Click += (s, e) => Close();

            // 添加控件到窗体
            Controls.AddRange(new Control[] {
                hardwareIdLabel,
                hardwareIdTextBox,
                getHardwareIdButton,
                licenseKeyLabel,
                licenseKeyTextBox,
                generateButton,
                copyButton,
                saveButton,
                statusLabel,
                closeButton
            });

            // 检查私钥文件是否存在
            CheckPrivateKeyExists();
        }

        private void CheckPrivateKeyExists()
        {
            if (!File.Exists(Constants.ZfilePath+"private.pem"))
            {
                statusLabel.Text = "警告: private.pem 文件不存在，无法生成授权码!";
                generateButton.Enabled = false;
            }
        }

        private void GetHardwareIdButton_Click(object sender, EventArgs e)
        {
            try
            {
                string hardwareId = LicenseValidator.GenerateHardwareId();
                hardwareIdTextBox.Text = hardwareId;
                statusLabel.Text = "已获取当前机器的硬件ID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取硬件ID失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "获取硬件ID失败";
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            string hardwareId = hardwareIdTextBox.Text.Trim();
            if (string.IsNullOrEmpty(hardwareId))
            {
                MessageBox.Show("请输入硬件ID", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string licenseKey = LicenseValidator.GenerateLicenseKey(hardwareId);
                licenseKeyTextBox.Text = licenseKey;
                statusLabel.Text = "授权码生成成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成授权码失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "生成授权码失败";
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
			//if (!string.IsNullOrEmpty(licenseKeyTextBox.Text))
			//{
			//    Clipboard.SetText(licenseKeyTextBox.Text);
			//    statusLabel.Text = "授权码已复制到剪贴板";
			//}
			//else
			//{
			//    MessageBox.Show("没有可复制的授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//}
			if (!string.IsNullOrEmpty(licenseKeyTextBox.Text))
			{
				bool success = false;
				int retryCount = 5;
				while (!success && retryCount > 0)
				{
					try
					{
						Clipboard.SetText(licenseKeyTextBox.Text);
						success = true;
					}
					catch (System.Runtime.InteropServices.ExternalException)
					{
						retryCount--;
						System.Threading.Thread.Sleep(100); // Wait before retrying
					}
				}

				if (success)
				{
					statusLabel.Text = "授权码已复制到剪贴板";
				}
				else
				{
					MessageBox.Show("无法复制授权码到剪贴板，请稍后再试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				MessageBox.Show("没有可复制的授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(licenseKeyTextBox.Text))
            {
                MessageBox.Show("没有可保存的授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                saveDialog.Title = "保存授权码";
                saveDialog.FileName = "license_key.txt";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveDialog.FileName, licenseKeyTextBox.Text);
                        statusLabel.Text = $"授权码已保存到: {saveDialog.FileName}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存授权码失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // 用于测试验证授权码是否有效
        private bool ValidateLicense(string hardwareId, string licenseKey)
        {
            try
            {
                // 这里需要实现验证逻辑，可以调用LicenseValidator中的方法
                return LicenseValidator.ValidateLicense(licenseKey);
            }
            catch
            {
                return false;
            }
        }
    }
}