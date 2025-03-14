using Sheng.Winform.Controls;

namespace zfile
{
	public partial class APICallerForm : ShengForm
	{
		private TextBox txtUrl;
		private TextBox txtApiKey;
		private ListView listViewParams;
		private Button btnAdd;
		private Button btnDelete;
		private Button btnConnect;
		private Button btnClose;
		private TextBox txtResult;
		private readonly string url;
		private readonly string key;
		private readonly string param;

		public APICallerForm(string url, string key, string param)
		{
			this.url = url;
			this.key = key;
			this.param = param;
			InitializeComponent();
			InitializeListView();
		}

		private void InitializeComponent()
		{
			txtUrl = new TextBox();
			txtApiKey = new TextBox();
			listViewParams = new ListView();
			btnAdd = new Button();
			btnDelete = new Button();
			btnConnect = new Button();
			btnClose = new Button();
			txtResult = new TextBox();
			SuspendLayout();
			// 
			// txtUrl
			// 
			txtUrl.Location = new Point(90, 12);
			txtUrl.Name = "txtUrl";
			txtUrl.Size = new Size(382, 23);
			txtUrl.TabIndex = 0;
			txtUrl.Text = "http://v.juhe.cn/toutiao/index";
			// 
			// txtApiKey
			// 
			txtApiKey.Location = new Point(90, 41);
			txtApiKey.Name = "txtApiKey";
			txtApiKey.Size = new Size(382, 23);
			txtApiKey.TabIndex = 1;
			txtApiKey.Text = "de73e15a67f8b359d4ec409ae3e63aed";
			// 
			// listViewParams
			// 
			listViewParams.FullRowSelect = true;
			listViewParams.GridLines = true;
			listViewParams.Location = new Point(12, 70);
			listViewParams.Name = "listViewParams";
			listViewParams.Size = new Size(460, 150);
			listViewParams.TabIndex = 2;
			listViewParams.UseCompatibleStateImageBehavior = false;
			listViewParams.View = View.Details;
			listViewParams.LabelEdit = true;
			// 
			// btnAdd
			// 
			btnAdd.Location = new Point(12, 226);
			btnAdd.Name = "btnAdd";
			btnAdd.Size = new Size(75, 23);
			btnAdd.TabIndex = 3;
			btnAdd.Text = "增加";
			btnAdd.UseVisualStyleBackColor = true;
			btnAdd.Click += new EventHandler(btnAdd_Click);
			// 
			// btnDelete
			// 
			btnDelete.Location = new Point(93, 226);
			btnDelete.Name = "btnDelete";
			btnDelete.Size = new Size(75, 23);
			btnDelete.TabIndex = 4;
			btnDelete.Text = "删除";
			btnDelete.UseVisualStyleBackColor = true;
			btnDelete.Click += new EventHandler(btnDelete_Click);
			// 
			// btnConnect
			// 
			btnConnect.Location = new Point(316, 226);
			btnConnect.Name = "btnConnect";
			btnConnect.Size = new Size(75, 23);
			btnConnect.TabIndex = 5;
			btnConnect.Text = "连接";
			btnConnect.UseVisualStyleBackColor = true;
			btnConnect.Click += new EventHandler(btnConnect_Click);
			// 
			// btnClose
			// 
			btnClose.Location = new Point(397, 226);
			btnClose.Name = "btnClose";
			btnClose.Size = new Size(75, 23);
			btnClose.TabIndex = 6;
			btnClose.Text = "关闭";
			btnClose.UseVisualStyleBackColor = true;
			btnClose.Click += new EventHandler(btnClose_Click);
			// 
			// txtResult
			// 
			txtResult.Location = new Point(12, 255);
			txtResult.Multiline = true;
			txtResult.Name = "txtResult";
			txtResult.ReadOnly = true;
			txtResult.ScrollBars = ScrollBars.Vertical;
			txtResult.Size = new Size(460, 100);
			txtResult.TabIndex = 7;
			// 
			// APICallerForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(484, 367);
			Controls.Add(txtResult);
			Controls.Add(btnClose);
			Controls.Add(btnConnect);
			Controls.Add(btnDelete);
			Controls.Add(btnAdd);
			Controls.Add(listViewParams);
			Controls.Add(txtApiKey);
			Controls.Add(txtUrl);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "APICallerForm";
			StartPosition = FormStartPosition.CenterParent;
			Text = "API调用器";
			ResumeLayout(false);
			PerformLayout();
		}

		private void InitializeListView()
		{
			_ = listViewParams.Columns.Add("参数", 220);
			_ = listViewParams.Columns.Add("值", 220);
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			ListViewItem item = new(new string[] { "param", "value" });
			_ = listViewParams.Items.Add(item);
			item.BeginEdit();
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			if (listViewParams.SelectedItems.Count > 0)
			{
				foreach (ListViewItem item in listViewParams.SelectedItems)
				{
					listViewParams.Items.Remove(item);
				}
			}
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(txtUrl.Text) || string.IsNullOrEmpty(txtApiKey.Text))
			{
				_ = MessageBox.Show("URL和APIKey不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			List<string> parameters = [];
			foreach (ListViewItem item in listViewParams.Items)
			{
				if (!string.IsNullOrEmpty(item.SubItems[0].Text) && !string.IsNullOrEmpty(item.SubItems[1].Text))
				{
					parameters.Add($"{item.SubItems[0].Text}={item.SubItems[1].Text}");
				}
			}

			string result = ((CmdProc)Tag).cm_apicaller(txtUrl.Text, txtApiKey.Text, string.Join(",", parameters));
			txtResult.Text = result;
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}