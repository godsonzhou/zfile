namespace WinFormsApp1
{
    partial class OptionsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            TreeNode treeNode1 = new TreeNode("language");
            TreeNode treeNode2 = new TreeNode("options", new TreeNode[] { treeNode1 });
            splitContainer1 = new SplitContainer();
            treeView1 = new TreeView();
            button1 = new Button();
            Okbutton = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            groupBox1 = new GroupBox();
            tabPage2 = new TabPage();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
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
            splitContainer1.Panel1.Controls.Add(treeView1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(button1);
            splitContainer1.Panel2.Controls.Add(Okbutton);
            splitContainer1.Panel2.Controls.Add(tabControl1);
            splitContainer1.Size = new Size(1000, 761);
            splitContainer1.SplitterDistance = 333;
            splitContainer1.TabIndex = 0;
            // 
            // treeView1
            // 
            treeView1.Location = new Point(11, 14);
            treeView1.Name = "treeView1";
            treeNode1.Name = "节点1";
            treeNode1.Tag = "language";
            treeNode1.Text = "language";
            treeNode2.Checked = true;
            treeNode2.Name = "节点0";
            treeNode2.Text = "options";
            treeView1.Nodes.AddRange(new TreeNode[] { treeNode2 });
            treeView1.Size = new Size(306, 726);
            treeView1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(372, 704);
            button1.Name = "button1";
            button1.Size = new Size(127, 33);
            button1.TabIndex = 3;
            button1.Text = "Cancel";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Okbutton
            // 
            Okbutton.Location = new Point(243, 704);
            Okbutton.Name = "Okbutton";
            Okbutton.Size = new Size(127, 33);
            Okbutton.TabIndex = 2;
            Okbutton.Text = "OK";
            Okbutton.UseVisualStyleBackColor = true;
            Okbutton.Click += Okbutton_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(12, 88);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(621, 601);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Location = new Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(613, 571);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Location = new Point(33, 97);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(450, 224);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "groupBox1";
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 26);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(613, 571);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 761);
            Controls.Add(splitContainer1);
            Name = "OptionsForm";
            Text = "Form2";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private GroupBox groupBox1;
        private TabPage tabPage2;
        private TreeView treeView1;
        private Button button1;
        private Button Okbutton;
    }
}