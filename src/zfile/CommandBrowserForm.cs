namespace zfile
{
    public class CommandBrowserForm : Form
    {
        private Panel searchPanel;
        private TextBox searchBox;
        private ListView listView;
        private Button btnNew;
        private Button btnEdit;
        private Button btnDel;
        private Button btnCopy;
        private Button btnRename;
        private CmdProc cmdProcessor;

        public CommandBrowserForm(CmdProc cmdProcessor)
        {
            this.cmdProcessor = cmdProcessor;
            InitializeComponent();
            LoadCommands();
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            this.Text = "命令浏览器";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            // 创建搜索面板
            searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            var btnWidth = 100;
            searchBox = new TextBox
            {
                Location = new Point(10, 10),
                Width = 200,
                PlaceholderText = "搜索命令..."
            };

            btnNew = new Button
            {
                Location = new Point(300, 10),
                Width = btnWidth,
                Text = "New"
            };

            btnEdit = new Button
            {
                Location = new Point(400, 10),
                Width = btnWidth,
                Text = "Edit"
            };

            btnDel = new Button
            {
                Location = new Point(500, 10),
                Width = btnWidth,
                Text = "Delete"
            };

            btnCopy = new Button
            {
                Location = new Point(600, 10),
                Width = btnWidth,
                Text = "Copy"
            };

            btnRename = new Button
            {
                Location = new Point(700, 10),
                Width = btnWidth,
                Text = "Rename"
            };

            searchPanel.Controls.AddRange(new Control[] { searchBox, btnNew, btnEdit, btnDel, btnCopy, btnRename });

            // 创建ListView用于显示命令
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            // 添加列
            listView.Columns.Add("ID", 80);
            listView.Columns.Add("命令名称", 200);
            listView.Columns.Add("描述", 250);
            listView.Columns.Add("中文描述", 250);

            // 添加控件到窗体
            this.Controls.Add(listView);
            this.Controls.Add(searchPanel);

            // 设置初始焦点
            this.Load += (s, e) => searchBox.Focus();

            // 添加事件处理
            searchBox.TextChanged += SearchBox_TextChanged;
            listView.DoubleClick += ListView_DoubleClick;
            btnNew.Click += (s, e) => { };
            btnEdit.Click += (s, e) => { };
            btnDel.Click += (s, e) => { };
            btnCopy.Click += (s, e) => { };
            btnRename.Click += (s, e) => { };

            // 添加右键菜单
            var contextMenu = new ContextMenuStrip();
            var copyMenuItem = new ToolStripMenuItem("复制命令名称");
            var execMenuItem = new ToolStripMenuItem("执行命令");

            copyMenuItem.Click += CopyMenuItem_Click;
            execMenuItem.Click += ExecMenuItem_Click;

            contextMenu.Items.AddRange(new ToolStripItem[] { copyMenuItem, execMenuItem });
            listView.ContextMenuStrip = contextMenu;
        }

        private void LoadCommands()
        {
            // 获取所有命令并填充ListView
            var commands = cmdProcessor.cmdTable.GetAll();
            foreach (var cmd in commands)
            {
                var item = new ListViewItem(cmd.CmdId.ToString());
                item.SubItems.Add(cmd.CmdName);
                item.SubItems.Add(cmd.Description);
                item.SubItems.Add(cmd.ZhDesc);
                listView.Items.Add(item);
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchBox.Text.ToLower();
            bool foundMatch = false;

            foreach (ListViewItem item in listView.Items)
            {
                bool match = false;
                
                // 搜索ID
                match = item.Text.ToLower().Contains(searchText);
                
                // 搜索名称
                if (!match)
                {
                    match = item.SubItems[1].Text.ToLower().Contains(searchText);
                    
                    // 搜索描述
                    if (!match)
                        match = item.SubItems[2].Text.ToLower().Contains(searchText) ||
                               item.SubItems[3].Text.ToLower().Contains(searchText);
                }
                
                item.ForeColor = match || string.IsNullOrEmpty(searchText) ?
                    SystemColors.WindowText : SystemColors.GrayText;
                
                if (match && !foundMatch)
                {
                    // 找到第一个匹配项
                    foundMatch = true;
                    item.Selected = true;
                    item.EnsureVisible(); // 滚动到可见区域
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                Clipboard.SetText(cmdName);
                MessageBox.Show($"命令 {cmdName} 已复制到剪贴板", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                Clipboard.SetText(cmdName);
            }
        }

        private void ExecMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string cmdName = listView.SelectedItems[0].SubItems[1].Text;
                cmdProcessor.ExecCmd(cmdName);
                this.Close();
            }
        }
    }
}