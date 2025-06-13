//namespace zfile
//{
//    partial class TestVfsForm
//    {
//        /// <summary>
//        /// Required designer variable.
//        /// </summary>
//        private System.ComponentModel.IContainer components = null;

//        /// <summary>
//        /// Clean up any resources being used.
//        /// </summary>
//        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//        protected override void Dispose(bool disposing)
//        {
//            if (disposing && (components != null))
//            {
//                components.Dispose();
//            }
//            base.Dispose(disposing);
//        }

//        #region Windows Form Designer generated code

//        /// <summary>
//        /// Required method for Designer support - do not modify
//        /// the contents of this method with the code editor.
//        /// </summary>
//        private void InitializeComponent()
//        {
//            this.components = new System.ComponentModel.Container();
//            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestVfsForm));
//            this.splitContainer = new System.Windows.Forms.SplitContainer();
//            this.treeViewNavigation = new System.Windows.Forms.TreeView();
//            this.imageListTree = new System.Windows.Forms.ImageList(this.components);
//            this.listViewFiles = new System.Windows.Forms.ListView();
//            this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.columnHeaderSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.columnHeaderDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.contextMenuFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
//            this.menuItemOpen = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemCopy = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemDelete = new System.Windows.Forms.ToolStripMenuItem();
//            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
//            this.menuItemEmptyRecycleBin = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemRestore = new System.Windows.Forms.ToolStripMenuItem();
//            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
//            this.menuItemExtract = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemAddFiles = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemTestArchive = new System.Windows.Forms.ToolStripMenuItem();
//            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
//            this.menuItemCreateDirectory = new System.Windows.Forms.ToolStripMenuItem();
//            this.menuItemProperties = new System.Windows.Forms.ToolStripMenuItem();
//            this.imageListFiles = new System.Windows.Forms.ImageList(this.components);
//            this.statusStrip = new System.Windows.Forms.StatusStrip();
//            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
//            this.toolStrip = new System.Windows.Forms.ToolStrip();
//            this.btnBack = new System.Windows.Forms.ToolStripButton();
//            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
//            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
//            this.splitContainer.Panel1.SuspendLayout();
//            this.splitContainer.Panel2.SuspendLayout();
//            this.splitContainer.SuspendLayout();
//            this.contextMenuFiles.SuspendLayout();
//            this.statusStrip.SuspendLayout();
//            this.toolStrip.SuspendLayout();
//            this.SuspendLayout();
//            //
//            // splitContainer
//            //
//            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splitContainer.Location = new System.Drawing.Point(0, 25);
//            this.splitContainer.Name = "splitContainer";
//            //
//            // splitContainer.Panel1
//            //
//            this.splitContainer.Panel1.Controls.Add(this.treeViewNavigation);
//            //
//            // splitContainer.Panel2
//            //
//            this.splitContainer.Panel2.Controls.Add(this.listViewFiles);
//            this.splitContainer.Size = new System.Drawing.Size(800, 403);
//            this.splitContainer.SplitterDistance = 266;
//            this.splitContainer.TabIndex = 0;
//            //
//            // treeViewNavigation
//            //
//            this.treeViewNavigation.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.treeViewNavigation.ImageIndex = 0;
//            this.treeViewNavigation.ImageList = this.imageListTree;
//            this.treeViewNavigation.Location = new System.Drawing.Point(0, 0);
//            this.treeViewNavigation.Name = "treeViewNavigation";
//            this.treeViewNavigation.SelectedImageIndex = 0;
//            this.treeViewNavigation.Size = new System.Drawing.Size(266, 403);
//            this.treeViewNavigation.TabIndex = 0;
//            this.treeViewNavigation.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeViewNavigation_AfterSelect);
//            //
//            // imageListTree
//            //
//            this.imageListTree.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTree.ImageStream")));
//            this.imageListTree.TransparentColor = System.Drawing.Color.Transparent;
//            //this.imageListTree.Images.SetKeyName(0, "folder.png");
//            //this.imageListTree.Images.SetKeyName(1, "recycle_bin.png");
//            //this.imageListTree.Images.SetKeyName(2, "archive.png");
//            ////this.imageListTree.Images.SetKeyName(3, "drive.png");
//            //this.imageListTree.Images.SetKeyName(4, "folder_open.png");
//            //this.imageListTree.Images.SetKeyName(5, "file.png");
//            //
//            // listViewFiles
//            //
//            this.listViewFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
//            this.columnHeaderName,
//            this.columnHeaderSize,
//            this.columnHeaderDate});
//            this.listViewFiles.ContextMenuStrip = this.contextMenuFiles;
//            this.listViewFiles.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.listViewFiles.FullRowSelect = true;
//            this.listViewFiles.HideSelection = false;
//            this.listViewFiles.Location = new System.Drawing.Point(0, 0);
//            this.listViewFiles.Name = "listViewFiles";
//            this.listViewFiles.Size = new System.Drawing.Size(530, 403);
//            this.listViewFiles.SmallImageList = this.imageListFiles;
//            this.listViewFiles.TabIndex = 0;
//            this.listViewFiles.UseCompatibleStateImageBehavior = false;
//            this.listViewFiles.View = System.Windows.Forms.View.Details;
//            this.listViewFiles.DoubleClick += new System.EventHandler(this.ListViewFiles_DoubleClick);
//            //
//            // columnHeaderName
//            //
//            this.columnHeaderName.Text = "名称";
//            this.columnHeaderName.Width = 200;
//            //
//            // columnHeaderSize
//            //
//            this.columnHeaderSize.Text = "大小";
//            this.columnHeaderSize.Width = 100;
//            //
//            // columnHeaderDate
//            //
//            this.columnHeaderDate.Text = "修改日期";
//            this.columnHeaderDate.Width = 150;
//            //
//            // contextMenuFiles
//            //
//            this.contextMenuFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.menuItemOpen,
//            this.menuItemCopy,
//            this.menuItemDelete,
//            this.toolStripSeparator1,
//            this.menuItemEmptyRecycleBin,
//            this.menuItemRestore,
//            this.toolStripSeparator2,
//            this.menuItemExtract,
//            this.menuItemAddFiles,
//            this.menuItemTestArchive,
//            this.toolStripSeparator3,
//            this.menuItemCreateDirectory,
//            this.menuItemProperties});
//            this.contextMenuFiles.Name = "contextMenuFiles";
//            this.contextMenuFiles.Size = new System.Drawing.Size(181, 264);
//            this.contextMenuFiles.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuFiles_Opening);
//            //
//            // menuItemOpen
//            //
//            this.menuItemOpen.Name = "menuItemOpen";
//            this.menuItemOpen.Size = new System.Drawing.Size(180, 22);
//            this.menuItemOpen.Text = "打开";
//            this.menuItemOpen.Click += new System.EventHandler(this.MenuItemOpen_Click);
//            //
//            // menuItemCopy
//            //
//            this.menuItemCopy.Name = "menuItemCopy";
//            this.menuItemCopy.Size = new System.Drawing.Size(180, 22);
//            this.menuItemCopy.Text = "复制";
//            this.menuItemCopy.Click += new System.EventHandler(this.MenuItemCopy_Click);
//            //
//            // menuItemDelete
//            //
//            this.menuItemDelete.Name = "menuItemDelete";
//            this.menuItemDelete.Size = new System.Drawing.Size(180, 22);
//            this.menuItemDelete.Text = "删除";
//            this.menuItemDelete.Click += new System.EventHandler(this.MenuItemDelete_Click);
//            //
//            // toolStripSeparator1
//            //
//            this.toolStripSeparator1.Name = "toolStripSeparator1";
//            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
//            //
//            // menuItemEmptyRecycleBin
//            //
//            this.menuItemEmptyRecycleBin.Name = "menuItemEmptyRecycleBin";
//            this.menuItemEmptyRecycleBin.Size = new System.Drawing.Size(180, 22);
//            this.menuItemEmptyRecycleBin.Text = "清空回收站";
//            this.menuItemEmptyRecycleBin.Click += new System.EventHandler(this.MenuItemEmptyRecycleBin_Click);
//            //
//            // menuItemRestore
//            //
//            this.menuItemRestore.Name = "menuItemRestore";
//            this.menuItemRestore.Size = new System.Drawing.Size(180, 22);
//            this.menuItemRestore.Text = "还原";
//            this.menuItemRestore.Click += new System.EventHandler(this.MenuItemRestore_Click);
//            //
//            // toolStripSeparator2
//            //
//            this.toolStripSeparator2.Name = "toolStripSeparator2";
//            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
//            //
//            // menuItemExtract
//            //
//            this.menuItemExtract.Name = "menuItemExtract";
//            this.menuItemExtract.Size = new System.Drawing.Size(180, 22);
//            this.menuItemExtract.Text = "解压";
//            this.menuItemExtract.Click += new System.EventHandler(this.MenuItemExtract_Click);
//            //
//            // menuItemAddFiles
//            //
//            this.menuItemAddFiles.Name = "menuItemAddFiles";
//            this.menuItemAddFiles.Size = new System.Drawing.Size(180, 22);
//            this.menuItemAddFiles.Text = "添加文件";
//            this.menuItemAddFiles.Click += new System.EventHandler(this.MenuItemAddFiles_Click);
//            //
//            // menuItemTestArchive
//            //
//            this.menuItemTestArchive.Name = "menuItemTestArchive";
//            this.menuItemTestArchive.Size = new System.Drawing.Size(180, 22);
//            this.menuItemTestArchive.Text = "测试压缩文件";
//            this.menuItemTestArchive.Click += new System.EventHandler(this.MenuItemTestArchive_Click);
//            //
//            // toolStripSeparator3
//            //
//            this.toolStripSeparator3.Name = "toolStripSeparator3";
//            this.toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
//            //
//            // menuItemCreateDirectory
//            //
//            this.menuItemCreateDirectory.Name = "menuItemCreateDirectory";
//            this.menuItemCreateDirectory.Size = new System.Drawing.Size(180, 22);
//            this.menuItemCreateDirectory.Text = "创建文件夹";
//            this.menuItemCreateDirectory.Click += new System.EventHandler(this.MenuItemCreateDirectory_Click);
//            //
//            // menuItemProperties
//            //
//            this.menuItemProperties.Name = "menuItemProperties";
//            this.menuItemProperties.Size = new System.Drawing.Size(180, 22);
//            this.menuItemProperties.Text = "属性";
//            this.menuItemProperties.Click += new System.EventHandler(this.MenuItemProperties_Click);
//            //
//            // imageListFiles
//            //
//            this.imageListFiles.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListFiles.ImageStream")));
//            this.imageListFiles.TransparentColor = System.Drawing.Color.Transparent;
//            //this.imageListFiles.Images.SetKeyName(0, "folder.png");
//            //this.imageListFiles.Images.SetKeyName(1, "recycle_bin.png");
//            //this.imageListFiles.Images.SetKeyName(2, "archive.png");
//            //this.imageListFiles.Images.SetKeyName(3, "drive.png");
//            //this.imageListFiles.Images.SetKeyName(4, "folder_open.png");
//            //this.imageListFiles.Images.SetKeyName(5, "file.png");
//            //
//            // statusStrip
//            //
//            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.toolStripStatusLabel});
//            this.statusStrip.Location = new System.Drawing.Point(0, 428);
//            this.statusStrip.Name = "statusStrip";
//            this.statusStrip.Size = new System.Drawing.Size(800, 22);
//            this.statusStrip.TabIndex = 1;
//            this.statusStrip.Text = "statusStrip1";
//            //
//            // toolStripStatusLabel
//            //
//            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
//            this.toolStripStatusLabel.Size = new System.Drawing.Size(32, 17);
//            this.toolStripStatusLabel.Text = "就绪";
//            //
//            // toolStrip
//            //
//            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.btnBack,
//            this.btnRefresh});
//            this.toolStrip.Location = new System.Drawing.Point(0, 0);
//            this.toolStrip.Name = "toolStrip";
//            this.toolStrip.Size = new System.Drawing.Size(800, 25);
//            this.toolStrip.TabIndex = 2;
//            this.toolStrip.Text = "toolStrip1";
//            //
//            // btnBack
//            //
//            this.btnBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
//            this.btnBack.Image = ((System.Drawing.Image)(resources.GetObject("btnBack.Image")));
//            this.btnBack.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.btnBack.Name = "btnBack";
//            this.btnBack.Size = new System.Drawing.Size(23, 22);
//            this.btnBack.Text = "返回上级";
//            this.btnBack.Click += new System.EventHandler(this.BtnBack_Click);
//            //
//            // btnRefresh
//            //
//            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
//            this.btnRefresh.Image = ((System.Drawing.Image)(resources.GetObject("btnRefresh.Image")));
//            this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.btnRefresh.Name = "btnRefresh";
//            this.btnRefresh.Size = new System.Drawing.Size(23, 22);
//            this.btnRefresh.Text = "刷新";
//            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
//            //
//            // TestVfsForm
//            //
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            this.ClientSize = new System.Drawing.Size(800, 450);
//            this.Controls.Add(this.splitContainer);
//            this.Controls.Add(this.toolStrip);
//            this.Controls.Add(this.statusStrip);
//            this.Name = "TestVfsForm";
//            this.Text = "VFS测试窗口";
//            this.Load += new System.EventHandler(this.TestVfsForm_Load);
//            this.splitContainer.Panel1.ResumeLayout(false);
//            this.splitContainer.Panel2.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
//            this.splitContainer.ResumeLayout(false);
//            this.contextMenuFiles.ResumeLayout(false);
//            this.statusStrip.ResumeLayout(false);
//            this.statusStrip.PerformLayout();
//            this.toolStrip.ResumeLayout(false);
//            this.toolStrip.PerformLayout();
//            this.ResumeLayout(false);
//            this.PerformLayout();

//        }

//        #endregion

//        private System.Windows.Forms.SplitContainer splitContainer;
//        private System.Windows.Forms.TreeView treeViewNavigation;
//        private System.Windows.Forms.ListView listViewFiles;
//        private System.Windows.Forms.StatusStrip statusStrip;
//        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
//        private System.Windows.Forms.ToolStrip toolStrip;
//        private System.Windows.Forms.ToolStripButton btnBack;
//        private System.Windows.Forms.ToolStripButton btnRefresh;
//        private System.Windows.Forms.ImageList imageListTree;
//        private System.Windows.Forms.ImageList imageListFiles;
//        private System.Windows.Forms.ColumnHeader columnHeaderName;
//        private System.Windows.Forms.ColumnHeader columnHeaderSize;
//        private System.Windows.Forms.ColumnHeader columnHeaderDate;
//        private System.Windows.Forms.ContextMenuStrip contextMenuFiles;
//        private System.Windows.Forms.ToolStripMenuItem menuItemOpen;
//        private System.Windows.Forms.ToolStripMenuItem menuItemCopy;
//        private System.Windows.Forms.ToolStripMenuItem menuItemDelete;
//        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
//        private System.Windows.Forms.ToolStripMenuItem menuItemEmptyRecycleBin;
//        private System.Windows.Forms.ToolStripMenuItem menuItemRestore;
//        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
//        private System.Windows.Forms.ToolStripMenuItem menuItemExtract;
//        private System.Windows.Forms.ToolStripMenuItem menuItemAddFiles;
//        private System.Windows.Forms.ToolStripMenuItem menuItemTestArchive;
//        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
//        private System.Windows.Forms.ToolStripMenuItem menuItemCreateDirectory;
//        private System.Windows.Forms.ToolStripMenuItem menuItemProperties;
//    }
//}
