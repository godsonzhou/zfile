using System;
using System.Drawing;
using System.Windows.Forms;
using Sheng.Winform.Controls;
using WinShell;

namespace WinFormsApp1
{
    public class UIControlManager
    {
        private readonly Form1 form;
        private readonly ImageList treeViewImageList;
        private readonly SplitContainer mainContainer;
        private readonly SplitContainer leftPanel;
        private readonly SplitContainer rightPanel;
        private readonly Panel leftUpperPanel;
        private readonly Panel rightUpperPanel;
        private readonly Panel leftDrivePanel;
        private readonly Panel rightDrivePanel;
        private readonly ComboBox leftDriveBox;
        private readonly ComboBox rightDriveBox;
        private readonly TreeView leftTree;
        private readonly ListView leftList;
        private readonly TextBox leftPreview;
        private readonly ListBox leftBookmarkList;
        private readonly TreeView rightTree;
        private readonly ListView rightList;
        private readonly TextBox rightPreview;
        private readonly ListBox rightBookmarkList;
        private readonly ShengAddressBarStrip leftPathTextBox;
        private readonly ShengAddressBarStrip rightPathTextBox;
        private readonly StatusStrip leftStatusStrip;
        private readonly StatusStrip rightStatusStrip;
        private readonly SplitContainer leftTreeListSplitter;
        private readonly SplitContainer rightTreeListSplitter;

        public UIControlManager(Form1 form, 
            SplitContainer mainContainer,
            SplitContainer leftPanel,
            SplitContainer rightPanel,
            Panel leftUpperPanel,
            Panel rightUpperPanel,
            Panel leftDrivePanel,
            Panel rightDrivePanel,
            ComboBox leftDriveBox,
            ComboBox rightDriveBox,
            TreeView leftTree,
            ListView leftList,
            TextBox leftPreview,
            ListBox leftBookmarkList,
            TreeView rightTree,
            ListView rightList,
            TextBox rightPreview,
            ListBox rightBookmarkList,
            ShengAddressBarStrip leftPathTextBox,
            ShengAddressBarStrip rightPathTextBox,
            StatusStrip leftStatusStrip,
            StatusStrip rightStatusStrip,
            SplitContainer leftTreeListSplitter,
            SplitContainer rightTreeListSplitter)
        {
            this.form = form;
            this.mainContainer = mainContainer;
            this.leftPanel = leftPanel;
            this.rightPanel = rightPanel;
            this.leftUpperPanel = leftUpperPanel;
            this.rightUpperPanel = rightUpperPanel;
            this.leftDrivePanel = leftDrivePanel;
            this.rightDrivePanel = rightDrivePanel;
            this.leftDriveBox = leftDriveBox;
            this.rightDriveBox = rightDriveBox;
            this.leftTree = leftTree;
            this.leftList = leftList;
            this.leftPreview = leftPreview;
            this.leftBookmarkList = leftBookmarkList;
            this.rightTree = rightTree;
            this.rightList = rightList;
            this.rightPreview = rightPreview;
            this.rightBookmarkList = rightBookmarkList;
            this.leftPathTextBox = leftPathTextBox;
            this.rightPathTextBox = rightPathTextBox;
            this.leftStatusStrip = leftStatusStrip;
            this.rightStatusStrip = rightStatusStrip;
            this.leftTreeListSplitter = leftTreeListSplitter;
            this.rightTreeListSplitter = rightTreeListSplitter;

            treeViewImageList = new ImageList();
            treeViewImageList.ImageSize = new Size(16, 16);
        }

        public void InitializeLayout()
        {
            int topHeight = 0;
            Panel containerPanel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, topHeight, 0, 0)
            };
            form.Controls.Add(containerPanel);

            mainContainer.Dock = DockStyle.Fill;
            mainContainer.Orientation = Orientation.Vertical;

            int halfWidth = (form.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            mainContainer.SplitterDistance = halfWidth;
            mainContainer.SplitterMoved += MainContainer_SplitterMoved;

            containerPanel.Controls.Add(mainContainer);

            ConfigurePanel(leftPanel, mainContainer.Panel1);
            ConfigurePanel(rightPanel, mainContainer.Panel2);

            ConfigureUpperPanel(leftUpperPanel, leftDrivePanel, leftPanel.Panel1);
            ConfigureUpperPanel(rightUpperPanel, rightDrivePanel, rightPanel.Panel1);
        }

        private void MainContainer_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            int halfWidth = (form.ClientSize.Width - mainContainer.SplitterWidth) / 2;
            if (Math.Abs(mainContainer.SplitterDistance - halfWidth) > 5)
            {
                mainContainer.SplitterDistance = halfWidth;
            }
        }

        private void ConfigurePanel(SplitContainer panel, Control parent)
        {
            panel.Dock = DockStyle.Fill;
            panel.Orientation = Orientation.Horizontal;
            panel.SplitterDistance = (int)((parent.Width) * 0.5);
            parent.Controls.Add(panel);
        }

        private void ConfigureUpperPanel(Panel upperPanel, Panel drivePanel, Control parent)
        {
            upperPanel.Dock = DockStyle.Fill;
            upperPanel.Padding = new Padding(0, 30, 0, 0);

            drivePanel.Dock = DockStyle.Top;
            drivePanel.Height = 30;

            parent.Controls.Add(upperPanel);
            parent.Controls.Add(drivePanel);
            drivePanel.BringToFront();
        }

        public void InitializeDriveComboBoxes()
        {
            ConfigureDriveBox(leftDriveBox, leftDrivePanel, leftPathTextBox);
            ConfigureDriveBox(rightDriveBox, rightDrivePanel, rightPathTextBox);

            var rootNode = new ShengFileSystemNode();
            leftPathTextBox.InitializeRoot(rootNode);
            rightPathTextBox.InitializeRoot(rootNode);

            LoadDrives();
        }

        private void ConfigureDriveBox(ComboBox driveBox, Panel parent, ShengAddressBarStrip pathTextBox)
        {
            driveBox.Dock = DockStyle.Left;
            driveBox.DropDownStyle = ComboBoxStyle.DropDownList;
            driveBox.SelectedIndexChanged += DriveComboBox_SelectedIndexChanged;

            pathTextBox.Dock = DockStyle.Fill;

            parent.Controls.Add(pathTextBox);
            parent.Controls.Add(driveBox);
        }

        private void LoadDrives()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    leftDriveBox.Items.Add(drive.Name);
                    rightDriveBox.Items.Add(drive.Name);
                }
            }

            if (leftDriveBox.Items.Count > 0)
            {
                leftDriveBox.SelectedIndex = 0;
                rightDriveBox.SelectedIndex = 0;
            }
        }

        private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var treeView = comboBox == leftDriveBox ? leftTree : rightTree;
            var listView = comboBox == leftDriveBox ? leftList : rightList;

            if (comboBox.SelectedItem is string drivePath)
            {
                form.LoadDriveIntoTree(treeView, drivePath);
            }
        }

        public void InitializeTreeViews()
        {
            ConfigureTreeListSplitter(leftTreeListSplitter, leftUpperPanel, leftTree, leftList);
            ConfigureTreeListSplitter(rightTreeListSplitter, rightUpperPanel, rightTree, rightList);
        }

        private void ConfigureTreeListSplitter(SplitContainer splitter, Panel parent, TreeView treeView, ListView listView)
        {
            splitter.Dock = DockStyle.Fill;
            splitter.Orientation = Orientation.Vertical;
            splitter.Panel1.Controls.Add(treeView);
            splitter.Panel2.Controls.Add(listView);
            parent.Controls.Add(splitter);
        }

        public void InitializeTreeViewIcons()
        {
            Icon folderIcon = IconHelper.GetIconByFileType("folder", false);
            if (folderIcon != null)
            {
                treeViewImageList.Images.Add("folder", folderIcon);
            }

            ConfigureTreeView(leftTree);
            ConfigureTreeView(rightTree);
        }

        public void ConfigureTreeView(TreeView treeView)
        {
            treeView.Dock = DockStyle.Fill;
            treeView.ShowLines = true;
            treeView.HideSelection = false;
            treeView.ShowPlusMinus = true;
            treeView.ShowRootLines = true;
            treeView.PathSeparator = "\\";
            treeView.FullRowSelect = true;
            treeView.ItemHeight = 20;
            treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView.ImageList = treeViewImageList;

            treeView.DrawNode += form.TreeView_DrawNode;
            treeView.MouseUp += form.TreeView_MouseUp;
            treeView.AfterSelect += form.TreeView_AfterSelect;
            treeView.NodeMouseClick += form.TreeView_NodeMouseClick;
            treeView.BeforeExpand += form.TreeView_BeforeExpand;
            treeView.MouseDown += form.TreeView_MouseDown;
        }

        public void InitializeListViews()
        {
            ConfigureListView(leftList, leftPanel.Panel2);
            ConfigureListView(rightList, rightPanel.Panel2);
        }

        private void ConfigureListView(ListView listView, Control parent)
        {
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.AllowColumnReorder = true;
            listView.LabelEdit = true;
            listView.MultiSelect = false;
            listView.HideSelection = false;

            listView.Columns.Add("名称", 200);
            listView.Columns.Add("大小", 100);
            listView.Columns.Add("类型", 100);
            listView.Columns.Add("修改日期", 150);

            parent.Controls.Add(listView);
        }

        public void InitializePreviewPanels()
        {
            leftPreview.Dock = DockStyle.Fill;
            leftPreview.Multiline = true;
            leftPreview.ReadOnly = true;
            leftPreview.ScrollBars = ScrollBars.Both;
            leftPanel.Panel2.Controls.Add(leftPreview);

            rightPreview.Dock = DockStyle.Fill;
            rightPreview.Multiline = true;
            rightPreview.ReadOnly = true;
            rightPreview.ScrollBars = ScrollBars.Both;
            rightPanel.Panel2.Controls.Add(rightPreview);
        }

        public void InitializeStatusStrips()
        {
            leftStatusStrip.Dock = DockStyle.Bottom;
            leftPanel.Panel2.Controls.Add(leftStatusStrip);

            rightStatusStrip.Dock = DockStyle.Bottom;
            rightPanel.Panel2.Controls.Add(rightStatusStrip);
        }
    }
} 