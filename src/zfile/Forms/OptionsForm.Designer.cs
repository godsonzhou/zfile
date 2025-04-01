namespace Zfile
{
    partial class OptionsForm
    {
        private System.Windows.Forms.TreeView treeView; // Add this line

        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Button Okbutton;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

		private void InitializeComponent()
		{
			splitContainer1 = new SplitContainer();
			splitContainer2 = new SplitContainer();
			treeView = new TreeView();
			CancelButton = new Button();
			Okbutton = new Button();
			buttonPanel = new Panel();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
			splitContainer2.Panel1.SuspendLayout();
			splitContainer2.Panel2.SuspendLayout();
			splitContainer2.SuspendLayout();
			buttonPanel.SuspendLayout();
			SuspendLayout();
			// 
			// splitContainer1
			// 
			splitContainer1.Dock = DockStyle.Fill;
			splitContainer1.Location = new Point(0, 0);
			splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			splitContainer1.Panel1.Controls.Add(treeView);
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.Controls.Add(splitContainer2);
			splitContainer1.Size = new Size(700, 478);
			splitContainer1.SplitterDistance = 126;
			splitContainer1.TabIndex = 0;
			// 
			// splitContainer2
			// 
			splitContainer2.Dock = DockStyle.Fill;
			splitContainer2.Location = new Point(0, 0);
			splitContainer2.Name = "splitContainer2";
			splitContainer2.Orientation = Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			splitContainer2.Panel1.AutoScroll = true;
			// 
			// splitContainer2.Panel2
			// 
			splitContainer2.Panel2.Controls.Add(buttonPanel);
			splitContainer2.Size = new Size(570, 478);
			splitContainer2.SplitterDistance = 428;
			splitContainer2.TabIndex = 2;
			// 
			// buttonPanel
			// 
			buttonPanel.Dock = DockStyle.Fill;
			buttonPanel.Location = new Point(0, 0);
			buttonPanel.Name = "buttonPanel";
			buttonPanel.Size = new Size(570, 46);
			buttonPanel.TabIndex = 0;
			buttonPanel.Controls.Add(CancelButton);
			buttonPanel.Controls.Add(Okbutton);
			// 
			// treeView
			// 
			treeView.Dock = DockStyle.Fill;
			treeView.Location = new Point(0, 0);
			treeView.Name = "treeView";
			treeView.Size = new Size(126, 478);
			treeView.TabIndex = 0;
			// 
			// CancelButton
			// 
			CancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			CancelButton.Location = new Point(480, 10);
			CancelButton.Name = "CancelButton";
			CancelButton.Size = new Size(80, 26);
			CancelButton.TabIndex = 0;
			CancelButton.Text = "取消";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += button1_Click;
			// 
			// Okbutton
			// 
			Okbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			Okbutton.Location = new Point(390, 10);
			Okbutton.Name = "Okbutton";
			Okbutton.Size = new Size(80, 26);
			Okbutton.TabIndex = 1;
			Okbutton.Text = "确定";
			Okbutton.UseVisualStyleBackColor = true;
			Okbutton.Click += Okbutton_Click;
			// 
			// OptionsForm
			// 
			AutoScaleDimensions = new SizeF(7F, 17F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(700, 478);
			Controls.Add(splitContainer1);
			Name = "OptionsForm";
			Text = "OptionsForm";
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			splitContainer2.Panel1.ResumeLayout(false);
			splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
			splitContainer2.ResumeLayout(false);
			buttonPanel.ResumeLayout(false);
			ResumeLayout(false);

		}
	}
}
