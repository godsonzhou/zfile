//using System.Net.FtpClient; // ����ʹ����FluentFTP��
//using FluentFTP;
using FluentFTP;

namespace WinFormsApp1
{
	public class FtpController : IDisposable
	{
		private readonly Form parentForm;
		private readonly FTPMGR ftpManager;

		// UI Controls
		private Panel mainPanel;
		private PictureBox statusLight;
		private Button transferModeButton;
		private Button disconnectButton;
		private TextBox commandInput;
		private ListView replyList;
		private RichTextBox replayDetail;

		private bool isBinaryMode = true;
		private bool disposed = false;

		public FtpController(Form parentForm, FTPMGR ftpManager)
		{
			this.parentForm = parentForm;
			this.ftpManager = ftpManager;

			// ���������
			mainPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 25
			};
			mainPanel.Hide(); // Ĭ������
			InitializeControls();// ��ʼ���ؼ�

			// ��ӵ�������
			parentForm.Controls.Add(mainPanel);
		}

		private void InitializeControls()
		{
			// ״ָ̬ʾ��
			statusLight = new PictureBox
			{
				Size = new Size(16, 16),
				Location = new Point(2, 2),
				//Image = Properties.Resources.StatusOffline // ��Ҫ�����Ӧ����Դ
			};

			// ����ģʽ�л���ť
			transferModeButton = new Button
			{
				Text = "Binary",
				Location = new Point(20, 2),
				Width = 50
			};
			transferModeButton.Click += TransferModeButton_Click;

			// �Ͽ����Ӱ�ť
			disconnectButton = new Button
			{
				Text = "Disconnect",
				Location = new Point(70, 2),
				Width = 80
			};
			disconnectButton.Click += DisconnectButton_Click;

			// ���������
			commandInput = new TextBox
			{
				Location = new Point(150, 2),
				Width = 300
			};
			commandInput.KeyPress += CommandInput_KeyPress;

			// �ظ��б�
			replyList = new ListView
			{
				Location = new Point(450, 2),
				Width = 400,
				Height = 25,
				View = View.Details
			};
			replyList.Columns.Add("Time", 100);
			replyList.Columns.Add("Response");
			// ���ر�����
			replyList.HeaderStyle = ColumnHeaderStyle.None;

			replyList.SelectedIndexChanged += ReplayList_SelectedIndexChanged;

			// �ظ�����
			//replayDetail = new RichTextBox
			//{
			//	Location = new Point(750, 2),
			//	Width = 300,
			//	Height = 25,
			//	ReadOnly = true
			//};

			// ��ӿؼ������
			mainPanel.Controls.AddRange(new Control[] {
				statusLight,
				transferModeButton,
				disconnectButton,
				commandInput,
				replyList
				//replayDetail
			});
		}

		private void TransferModeButton_Click(object? sender, EventArgs e)
		{
			isBinaryMode = !isBinaryMode;
			transferModeButton.Text = isBinaryMode ? "Binary Mode" : "ASCII Mode";

			if (ftpManager.ActiveClient != null)
			{
				// ����FTP����ģʽ
				//ftpManager.ActiveClient.(isBinaryMode ? FtpDataType.ASCII : FtpDataType.Binary);
			}
		}

		private void DisconnectButton_Click(object? sender, EventArgs e)
		{
			if (ftpManager.ActiveClient != null)
			{
				ftpManager.ActiveClient.Disconnect();
				UpdateStatus(false);
			}
		}

		private void CommandInput_KeyPress(object? sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				SendCommand(commandInput.Text);
				commandInput.Clear();
				e.Handled = true;
			}
		}

		private void ReplayList_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (replyList.SelectedItems.Count > 0)
			{
				var item = replyList.SelectedItems[0];
				replayDetail.Text = item.Tag as string ?? string.Empty;
			}
		}

		private async void SendCommand(string command)
		{
			if (ftpManager.ActiveClient == null || string.IsNullOrEmpty(command)) return;

			try
			{
				var response = ftpManager.ActiveClient.Execute(command);
				//AddReplayToList(command, response);
			}
			catch (Exception ex)
			{
				AddReplayToList(command, ex.Message);
			}
		}

		private void AddReplayToList(string command, string response)
		{
			var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
			var firstLine = response.Split('\n').FirstOrDefault() ?? response;
			item.SubItems.Add(firstLine);
			item.Tag = $"Command: {command}\n\nResponse:\n{response}";

			replyList.Items.Insert(0, item);
			if (replyList.Items.Count > 100) // �����б�������
			{
				replyList.Items.RemoveAt(replyList.Items.Count - 1);
			}
		}

		public void UpdateStatus(bool isConnected)
		{
			//statusLight.Image = isConnected ?
			//	Properties.Resources.StatusOnline :
			//	Properties.Resources.StatusOffline;

			transferModeButton.Enabled = isConnected;
			disconnectButton.Enabled = isConnected;
			commandInput.Enabled = isConnected;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// �����й���Դ
					statusLight.Dispose();
					transferModeButton.Dispose();
					disconnectButton.Dispose();
					commandInput.Dispose();
					replyList.Dispose();
					replayDetail.Dispose();
					mainPanel.Dispose();
				}
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}