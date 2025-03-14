using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
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
		private string url;
		private string key;
		private string param;

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
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.listViewParams = new System.Windows.Forms.ListView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtUrl
            // 
            this.txtUrl.Location = new System.Drawing.Point(90, 12);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(382, 23);
            this.txtUrl.TabIndex = 0;
            this.txtUrl.Text = "http://v.juhe.cn/toutiao/index";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Location = new System.Drawing.Point(90, 41);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(382, 23);
            this.txtApiKey.TabIndex = 1;
            this.txtApiKey.Text = "de73e15a67f8b359d4ec409ae3e63aed";
            // 
            // listViewParams
            // 
            this.listViewParams.FullRowSelect = true;
            this.listViewParams.GridLines = true;
            this.listViewParams.Location = new System.Drawing.Point(12, 70);
            this.listViewParams.Name = "listViewParams";
            this.listViewParams.Size = new System.Drawing.Size(460, 150);
            this.listViewParams.TabIndex = 2;
            this.listViewParams.UseCompatibleStateImageBehavior = false;
            this.listViewParams.View = System.Windows.Forms.View.Details;
            this.listViewParams.LabelEdit = true;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 226);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "增加";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(93, 226);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 4;
            this.btnDelete.Text = "删除";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(316, 226);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 5;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(397, 226);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(12, 255);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Size = new System.Drawing.Size(460, 100);
            this.txtResult.TabIndex = 7;
            // 
            // APICallerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 367);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.listViewParams);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.txtUrl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "APICallerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "API调用器";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeListView()
        {
            listViewParams.Columns.Add("参数", 220);
            listViewParams.Columns.Add("值", 220);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ListViewItem item = new ListViewItem(new string[] { "param", "value" });
            listViewParams.Items.Add(item);
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
                MessageBox.Show("URL和APIKey不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> parameters = new List<string>();
            foreach (ListViewItem item in listViewParams.Items)
            {
                if (!string.IsNullOrEmpty(item.SubItems[0].Text) && !string.IsNullOrEmpty(item.SubItems[1].Text))
                {
                    parameters.Add($"{item.SubItems[0].Text}={item.SubItems[1].Text}");
                }
            }

            string result = ((CmdProc)this.Tag).cm_apicaller(txtUrl.Text, txtApiKey.Text, string.Join(",", parameters));
            txtResult.Text = result;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}