//using System.Net.FtpClient; // ����ʹ����FluentFTP��
//using FluentFTP;
using FluentFTP;
using System.Diagnostics;

namespace zfile
{
	public class FtpController : IDisposable
	{
		private readonly Form1 parentForm;
		private readonly AsyncFTPMGR asyncFtpManager;
		private readonly FTPMGR ftpManager;

		// UI Controls
		private Panel mainPanel;
		private PictureBox statusLight;
		private Button transferModeButton;
		private Button disconnectButton;
		private TextBox commandInput;
		private ListView replyList;
		private RichTextBox replayDetail;
		private bool isFtpConnected;
		private bool isBinaryMode = true;
		private bool disposed = false;
		private bool isPanelShow = false;
		private List<string> commandHistory = new List<string>();
		private int cmdHistoryIndex = 0;

		public FtpController(Form1 parentForm, AsyncFTPMGR asyncFtpManager, FTPMGR ftpManager)
		{
			this.parentForm = parentForm;
			this.ftpManager = ftpManager;
			this.asyncFtpManager = asyncFtpManager;

			// ���������
			mainPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 25
			};
			//mainPanel.Hide(); // Ĭ������
			InitializeControls();// ��ʼ���ؼ�

			// ���ӵ�������
			parentForm.Controls.Add(mainPanel);
			UpdateStatus(false);
		}

		private void InitializeControls()
		{
			// ״ָ̬ʾ��
			statusLight = new PictureBox
			{
				Size = new Size(16, 16),
				Location = new Point(2, 2),
				//Image = Properties.Resources.StatusOffline // ��Ҫ������Ӧ����Դ
			};

			// ����ģʽ�л���ť
			transferModeButton = new Button
			{
				Text = "Binary",
				Location = new Point(20, 2),
				Width = 60
			};
			transferModeButton.Click += TransferModeButton_Click;

			// �Ͽ����Ӱ�ť
			disconnectButton = new Button
			{
				Text = "Disconnect",
				Location = new Point(80, 2),
				Width = 80
			};
			disconnectButton.Click += DisconnectButton_Click;

			// ���������
			commandInput = new TextBox
			{
				Location = new Point(160, 2),
				Width = 400
			};
			commandInput.KeyPress += CommandInput_KeyPress;

			// �ظ��б�
			replyList = new ListView
			{
				Location = new Point(560, 2),
				Width = 500,
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

			// ���ӿؼ������
			mainPanel.Controls.AddRange(new Control[] {
				statusLight,
				transferModeButton,
				disconnectButton,
				commandInput,
				replyList
				//replayDetail
			});
		}
		public string GetCmdHistory(int i)
		{
			if (commandHistory.Count == 0) {
				return string.Empty;
			}

			// -1 表示获取上一条命令（向上浏览历史）
			if (i == -1) {
				if (cmdHistoryIndex > 0) {
					cmdHistoryIndex--;
				}
			}
			// 1 表示获取下一条命令（向下浏览历史）
			else if (i == 1) {
				if (cmdHistoryIndex < commandHistory.Count - 1) {
					cmdHistoryIndex++;
				}
			}

			// 返回当前索引位置的命令
			return commandHistory[cmdHistoryIndex];
		}
		public void SetCmdLine(string cmdLine)
		{
			commandInput.Text = cmdLine;
		}
		public void SetFocusCmdline()
		{
			commandInput.Focus();
		}
		public void TogglePanel()
		{
			if (isPanelShow)
				mainPanel.Hide();
			else
				mainPanel.Show();
			isPanelShow = !isPanelShow;
		}
		private void TransferModeButton_Click(object? sender, EventArgs e)
		{
			isBinaryMode = !isBinaryMode;
			transferModeButton.Text = isBinaryMode ? "Binary Mode" : "ASCII Mode";

			if (ftpManager.ActiveClient != null)
			{
				// ����FTP����ģʽ
				//ftpManager.ActiveClient.(isBinaryMode ? FtpDataType.ASCII : FtpDataType.Binary);
				//SendCommand(isBinaryMode ? "bin" : "asc");
				if (isBinaryMode)
				{
					ftpManager.ActiveClient.Config.DownloadDataType = FtpDataType.Binary;
					ftpManager.ActiveClient.Config.UploadDataType = FtpDataType.Binary;
					ftpManager.ActiveClient.Config.ListingDataType = FtpDataType.Binary;
				}
				else
				{
					ftpManager.ActiveClient.Config.DownloadDataType = FtpDataType.ASCII;
					ftpManager.ActiveClient.Config.UploadDataType = FtpDataType.ASCII;
					ftpManager.ActiveClient.Config.ListingDataType = FtpDataType.ASCII;
				}
			}
		}

		private void DisconnectButton_Click(object? sender, EventArgs e)
		{
			parentForm.fTPMGR.CloseConnection();
		}
		public void SetPrevCmd(){
			if (commandHistory.Count > 0)
			{
				string prevCmd = GetCmdHistory(-1);
				if (!string.IsNullOrEmpty(prevCmd))
				{
					SetCmdLine(prevCmd);
				}
			}
		}
		public void SetNextCmd(){
		if (commandHistory.Count > 0 && cmdHistoryIndex < commandHistory.Count)
				{
					string nextCmd = GetCmdHistory(1);
					if (!string.IsNullOrEmpty(nextCmd))
					{
						SetCmdLine(nextCmd);
					}
					else
					{
						// 如果已经到达历史记录的末尾，清空输入框
						SetCmdLine(string.Empty);
					}
				}
		}
		private void CommandInput_KeyPress(object? sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				string lastcmd = string.Empty;
				SendCommand(commandInput.Text);
				if(commandHistory.Count > 0) 
					lastcmd = commandHistory.Last();
				if (!string.IsNullOrEmpty(commandInput.Text) && !lastcmd.Equals(commandInput.Text))
					commandHistory.Add(commandInput.Text);
				
				// 添加新命令后，将历史索引重置到最新位置
				cmdHistoryIndex = commandHistory.Count;
				commandInput.Clear();
				e.Handled = true;
			}
			else if (e.KeyChar == (char)Keys.Up || e.KeyChar == 38) // 上箭头键
			{
				SetPrevCmd();
				e.Handled = true;
			}
			else if (e.KeyChar == (char)Keys.Down || e.KeyChar == 40) // 下箭头键
			{
				SetNextCmd();
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
			if (string.IsNullOrEmpty(command)) return;

			try
			{
				if (ftpManager.ActiveClient != null && isFtpConnected)
				{
					var response = ftpManager.ActiveClient.Execute(command);
					//Debug.Print(response.Message);
				}
				else
				{
					// if ftp is not connected, send command to cmdproc
					var cmdparts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					var param = cmdparts.Length > 1 ? string.Join(' ', cmdparts[1..]) : string.Empty;
					parentForm.cmdProcessor.ExecCmd(cmdparts[0], param, parentForm.currentDirectory[parentForm.isleft]);
				}
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
			isFtpConnected = isConnected;
			statusLight.Image = isConnected ?
				parentForm.iconManager.ImageList.Images[233] :
				parentForm.iconManager.ImageList.Images[230];
			if (!isConnected) 
			{ 
				statusLight.Hide();
				disconnectButton.Hide();
				transferModeButton.Hide();
				replyList.Hide();
				//replayDetail.Hide();
			}
			else 
			{
				statusLight.Show();
				disconnectButton.Show();
				transferModeButton.Show();
				replyList.Show();
				//replayDetail.Show();
			}
			transferModeButton.Enabled = isConnected;
			disconnectButton.Enabled = isConnected;
			//commandInput.Enabled = isConnected;
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