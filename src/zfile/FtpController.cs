//using System.Net.FtpClient; // 假设使用了FluentFTP库
//using FluentFTP;
using FluentFTP;
using System.Diagnostics;

namespace WinFormsApp1
{
	public class FtpController : IDisposable
	{
		private readonly Form1 parentForm;
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

		public FtpController(Form1 parentForm, FTPMGR ftpManager)
		{
			this.parentForm = parentForm;
			this.ftpManager = ftpManager;

			// 创建主面板
			mainPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 25
			};
			//mainPanel.Hide(); // 默认隐藏
			InitializeControls();// 初始化控件

			// 添加到父窗体
			parentForm.Controls.Add(mainPanel);
			UpdateStatus(false);
		}

		private void InitializeControls()
		{
			// 状态指示灯
			statusLight = new PictureBox
			{
				Size = new Size(16, 16),
				Location = new Point(2, 2),
				//Image = Properties.Resources.StatusOffline // 需要添加相应的资源
			};

			// 传输模式切换按钮
			transferModeButton = new Button
			{
				Text = "Binary",
				Location = new Point(20, 2),
				Width = 60
			};
			transferModeButton.Click += TransferModeButton_Click;

			// 断开连接按钮
			disconnectButton = new Button
			{
				Text = "Disconnect",
				Location = new Point(80, 2),
				Width = 80
			};
			disconnectButton.Click += DisconnectButton_Click;

			// 命令输入框
			commandInput = new TextBox
			{
				Location = new Point(160, 2),
				Width = 400
			};
			commandInput.KeyPress += CommandInput_KeyPress;

			// 回复列表
			replyList = new ListView
			{
				Location = new Point(560, 2),
				Width = 500,
				Height = 25,
				View = View.Details
			};
			replyList.Columns.Add("Time", 100);
			replyList.Columns.Add("Response");
			// 隐藏标题栏
			replyList.HeaderStyle = ColumnHeaderStyle.None;

			replyList.SelectedIndexChanged += ReplayList_SelectedIndexChanged;

			// 回复详情
			//replayDetail = new RichTextBox
			//{
			//	Location = new Point(750, 2),
			//	Width = 300,
			//	Height = 25,
			//	ReadOnly = true
			//};

			// 添加控件到面板
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
				// 设置FTP传输模式
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
			if (string.IsNullOrEmpty(command)) return;

			try
			{
				if (ftpManager.ActiveClient != null && isFtpConnected)
				{
					var response = ftpManager.ActiveClient.Execute(command);
					Debug.Print(response.Message);
				}
				else
				{
					// if ftp is not connected, send command to cmdproc
					parentForm.cmdProcessor.ExecCmd(command, "", parentForm.currentDirectory);
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
			if (replyList.Items.Count > 100) // 限制列表项数量
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
					// 清理托管资源
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