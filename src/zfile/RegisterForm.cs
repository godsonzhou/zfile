using System.Text;

namespace zfile
{
    public partial class RegisterForm : Form
    {
        private readonly TextBox licenseKeyTextBox;
        private readonly Label statusLabel;
        private readonly Button validateButton;
        private readonly Button closeButton;

        public RegisterForm()
        {
            
            // 设置窗体基本属性
            Text = "产品注册";
            Size = new Size(600, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            // 创建注册码输入框
            licenseKeyTextBox = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(540, 425),
                PlaceholderText = "请输入注册码",
				Multiline = true
            };

            // 创建状态标签
            statusLabel = new Label
            {
                Location = new Point(20, 460),
                Size = new Size(540, 40),
                Text = GetLicenseStatus()
            };

            // 创建验证按钮
            validateButton = new Button
            {
                Location = new Point(200, 520),
                Size = new Size(80, 30),
                Text = "验证"
            };
            validateButton.Click += ValidateButton_Click;

            // 创建关闭按钮
            closeButton = new Button
            {
                Location = new Point(300, 520),
                Size = new Size(80, 30),
                Text = "关闭"
            };
            closeButton.Click += (s, e) => Close();

            // 添加控件到窗体
            Controls.AddRange(new Control[] {
                licenseKeyTextBox,
                statusLabel,
                validateButton,
                closeButton
            });
        }

        private void ValidateButton_Click(object? sender, EventArgs e)
        {
            string licenseKey = licenseKeyTextBox.Text.Trim();
			//File.WriteAllText(Constants.ZfileCfgPath + "user.lic", licenseKey);
            if (string.IsNullOrEmpty(licenseKey))
            {
                MessageBox.Show("请输入注册码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (LicenseValidator.ValidateLicense(licenseKey))
                {
                    MessageBox.Show("注册成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
					LicenseValidator.SaveLicense(licenseKey);
					statusLabel.Text = GetLicenseStatus();
                    licenseKeyTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("无效的注册码", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLicenseStatus()
        {
            var status = new StringBuilder();
            status.AppendLine($"许可证状态: {(LicenseValidator.IsLicenseValid(licenseKeyTextBox.Text.Trim()) ? "已注册" : "未注册")}");
            
            //if (LicenseValidator.IsLicenseValid())
            //{
            //    var licenseInfo = LicenseValidator.GetLicenseInfo();
            //    status.AppendLine($"注册用户: {licenseInfo.UserName}");
            //    status.AppendLine($"到期时间: {licenseInfo.ExpiryDate:yyyy-MM-dd}");
            //}

            return status.ToString();
        }
    }
}
