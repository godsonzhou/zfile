namespace Sample3
{
    partial class Sample3
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.Tree1 = new System.Windows.Forms.TreeView();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Tree1
            // 
            this.Tree1.Location = new System.Drawing.Point(12, 48);
            this.Tree1.Name = "Tree1";
            this.Tree1.Size = new System.Drawing.Size(294, 335);
            this.Tree1.TabIndex = 0;
            this.Tree1.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tree1_BeforeExpand);
            this.Tree1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Tree1_MouseUp);
            this.Tree1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Tree1_MouseDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "右键点击节点";
            // 
            // Sample3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(325, 395);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Tree1);
            this.Name = "Sample3";
            this.Text = "Sample3";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Sample3_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView Tree1;
        private System.Windows.Forms.Label label1;
    }
}

