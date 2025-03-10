using System.Text;

namespace WinFormsApp1
{
    public partial class RegisterForm : Form
    {
        private readonly TextBox licenseKeyTextBox;
        private readonly Label statusLabel;
        private readonly Button validateButton;
        private readonly Button closeButton;

        public RegisterForm()
        {
            
            // ���ô����������
            Text = "��Ʒע��";
            Size = new Size(400, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            // ����ע���������
            licenseKeyTextBox = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(340, 25),
                PlaceholderText = "������ע����"
            };

            // ����״̬��ǩ
            statusLabel = new Label
            {
                Location = new Point(20, 60),
                Size = new Size(340, 40),
                Text = GetLicenseStatus()
            };

            // ������֤��ť
            validateButton = new Button
            {
                Location = new Point(100, 120),
                Size = new Size(80, 30),
                Text = "��֤"
            };
            validateButton.Click += ValidateButton_Click;

            // �����رհ�ť
            closeButton = new Button
            {
                Location = new Point(200, 120),
                Size = new Size(80, 30),
                Text = "�ر�"
            };
            closeButton.Click += (s, e) => Close();

            // ��ӿؼ�������
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
			LicenseValidator.SaveLicense(licenseKey);
            if (string.IsNullOrEmpty(licenseKey))
            {
                MessageBox.Show("������ע����", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (LicenseValidator.ValidateLicense(licenseKey))
                {
                    MessageBox.Show("ע��ɹ�!", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    statusLabel.Text = GetLicenseStatus();
                    licenseKeyTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("��Ч��ע����", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��֤ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLicenseStatus()
        {
            var status = new StringBuilder();
            status.AppendLine($"���֤״̬: {(LicenseValidator.IsLicenseValid(licenseKeyTextBox.Text.Trim()) ? "��ע��" : "δע��")}");
            
            //if (LicenseValidator.IsLicenseValid())
            //{
            //    var licenseInfo = LicenseValidator.GetLicenseInfo();
            //    status.AppendLine($"ע���û�: {licenseInfo.UserName}");
            //    status.AppendLine($"����ʱ��: {licenseInfo.ExpiryDate:yyyy-MM-dd}");
            //}

            return status.ToString();
        }
    }
}
