namespace zfile
{
    partial class OptionsForm
    {
        private System.Windows.Forms.TreeView treeView; // Add this line

        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.SplitContainer splitContainer1;
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
			treeView = new TreeView();
			CancelButton = new Button();
			Okbutton = new Button();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
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
			splitContainer1.Panel2.Controls.Add(CancelButton);
			splitContainer1.Panel2.Controls.Add(Okbutton);
			splitContainer1.Size = new Size(700, 478);
			splitContainer1.SplitterDistance = 126;
			splitContainer1.TabIndex = 0;
			// 
			// treeView
			// 
			treeView.Dock = DockStyle.Fill;
			treeView.Location = new Point(0, 0);
			treeView.Name = "treeView";
			treeView.Size = new Size(126, 478);
			treeView.TabIndex = 0;
			// 
			// button1
			// 
			CancelButton.Location = new Point(624, 3);
			CancelButton.Name = "button1";
			CancelButton.Size = new Size(66, 24);
			CancelButton.TabIndex = 0;
			CancelButton.Text = "Cancel";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += button1_Click;
			// 
			// Okbutton
			// 
			Okbutton.Location = new Point(553, 3);
			Okbutton.Name = "Okbutton";
			Okbutton.Size = new Size(66, 24);
			Okbutton.TabIndex = 1;
			Okbutton.Text = "OK";
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
			ResumeLayout(false);

		}
	}
}
