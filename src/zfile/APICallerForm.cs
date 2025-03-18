using Newtonsoft.Json;
using Sheng.Winform.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zfile
{
	public class Data
	{
		public string uniquekey { get; set; }
		public string title { get; set; }
		public string date { get; set; }
		public string category { get; set; }
		public string author_name { get; set; }
		public string url { get; set; }
		public string thumbnail_pic_s { get; set; }
		public string thumbnail_pic_s02 { get; set; }
		public string thumbnail_pic_s03 { get; set; }
		public string is_content { get; set; }
	}

	public class Result
	{
		public string stat { get; set; }
		public List<Data> data { get; set; }
		public string page { get; set; }
		public string pageSize { get; set; }
	}

	public class RootObject
	{
		public string reason { get; set; }
		public Result result { get; set; }
		public int error_code { get; set; }
	}
	public partial class APICallerForm : ShengForm
	{
		private TextBox txtUrl;
		private TextBox txtApiKey;
		private ListView listViewParams;
		private Button btnAdd;
		private Button btnDelete;
		private Button btnEdit;
		private Button btnConnect;
		private Button btnClose;
		//private TextBox txtResult;
		private ListView txtResult;
		private readonly string url;
		private readonly string key;
		private readonly string param;

		private Label lblUrl;
		private Label lblApiKey;

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
			btnEdit = new Button();
			btnConnect = new Button();
			btnClose = new Button();
			txtResult = new ListView();
			lblUrl = new Label();
			lblApiKey = new Label();
			SuspendLayout();
			// 
			// lblUrl
			// 
			lblUrl.AutoSize = true;
			lblUrl.Location = new Point(12, 15);
			lblUrl.Name = "lblUrl";
			lblUrl.Size = new Size(72, 15);
			lblUrl.TabIndex = 8;
			lblUrl.Text = "API地址：";
			// 
			// lblApiKey
			// 
			lblApiKey.AutoSize = true;
			lblApiKey.Location = new Point(12, 44);
			lblApiKey.Name = "lblApiKey";
			lblApiKey.Size = new Size(72, 15);
			lblApiKey.TabIndex = 9;
			lblApiKey.Text = "API密钥：";
			// 
			// txtUrl
			// 
			txtUrl.Location = new Point(90, 12);
			txtUrl.Name = "txtUrl";
			txtUrl.Size = new Size(382, 23);
			txtUrl.TabIndex = 0;
			txtUrl.Text = url;
			// 
			// txtApiKey
			// 
			txtApiKey.Location = new Point(90, 41);
			txtApiKey.Name = "txtApiKey";
			txtApiKey.Size = new Size(382, 23);
			txtApiKey.TabIndex = 1;
			txtApiKey.Text = key;
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
			// btnEdit
			// 
			btnEdit.Location = new Point(174, 226);
			btnEdit.Name = "btnEdit";
			btnEdit.Size = new Size(75, 23);
			btnEdit.TabIndex = 5;
			btnEdit.Text = "编辑";
			btnEdit.UseVisualStyleBackColor = true;
			btnEdit.Click += new EventHandler(btnEdit_Click);
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
			//txtResult.Multiline = true;
			txtResult.Name = "txtResult";
			//txtResult.ReadOnly = true;
			//txtResult.ScrollBars = ScrollBars.Vertical;
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
			Controls.Add(btnEdit);
			Controls.Add(listViewParams);
			Controls.Add(txtApiKey);
			Controls.Add(txtUrl);
			Controls.Add(lblUrl);
			Controls.Add(lblApiKey);
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
			listViewParams.LabelEdit = true;
			listViewParams.AfterLabelEdit += listViewParams_AfterLabelEdit;
		}

		private void listViewParams_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null)
			{
				e.CancelEdit = true;
				return;
			}
			
			ListViewItem item = listViewParams.Items[e.Item];
			if (item.SubItems.Count > 1)
			{
				item.SubItems[0].Text = e.Label;
			}
			e.CancelEdit = true;
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

			var result = ((CmdProc)Tag).cm_apicaller(txtUrl.Text, txtApiKey.Text, string.Join(",", parameters));
			processResult(result);
			txtResult.Text = result;
		}

		private void processResult(string json)
		{
			// 解析JSON数据
			var root = JsonConvert.DeserializeObject<RootObject>(json);

			// 配置ListView
			txtResult.View = View.Details;
			txtResult.Columns.Add("Title", 200);
			txtResult.Columns.Add("Date", 150);
			txtResult.Columns.Add("Category", 100);
			txtResult.Columns.Add("Author Name", 150);

			// 填充ListView
			foreach (var item in root.result.data)
			{
				var listItem = new ListViewItem(item.title);
				listItem.SubItems.Add(item.date);
				listItem.SubItems.Add(item.category);
				listItem.SubItems.Add(item.author_name);
				listItem.Tag = item.url;
				txtResult.Items.Add(listItem);
			}

			// 绑定点击事件
			txtResult.ItemActivate += ListView1_ItemActivate;
		}
		private void ListView1_ItemActivate(object sender, EventArgs e)
		{
			if (txtResult.SelectedItems.Count > 0)
			{
				string url = txtResult.SelectedItems[0].Tag as string;
				if (!string.IsNullOrEmpty(url))
				{
					var args = new ProcessStartInfo() {FileName=url,UseShellExecute=true};
					Process.Start(args);
				}
			}
		}
		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnEdit_Click(object sender, EventArgs e)
		{
			if (listViewParams.SelectedItems.Count > 0)
			{
				var dialog = new InputDialog("参数", "请输入参数值");
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					ListViewItem item = listViewParams.SelectedItems[0];
					if (item.SubItems.Count > 1)
					{
						item.SubItems[1].Text = dialog.InputText;
						//item.BeginEdit();
					}
				}
			}
		}
	}
}