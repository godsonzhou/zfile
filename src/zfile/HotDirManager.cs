using System.Text;

namespace Zfile
{
    public class HotDirManager
    {
        private readonly MainForm form;
        private readonly Dictionary<string, string> hotDirs = new(); // 键为文件夹名称，值为完整路径
        private bool isConfigChanged = false;
        private const string CONFIG_SECTION = "HotDirs";
        public Dictionary<string, string> HotDirs => hotDirs;


        public HotDirManager(MainForm form)
        {
            this.form = form;
            LoadFromCfg();
        }

        public void AddFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            string name = Path.GetFileName(path.TrimEnd('\\'));
            if (string.IsNullOrEmpty(name))
                name = path; // 如果是根目录，使用完整路径作为名称

            // 如果已存在相同名称的文件夹，添加数字后缀
            string originalName = name;
            int counter = 1;
            while (hotDirs.ContainsKey(name))
            {
                name = $"{originalName}_{counter++}";
            }

            hotDirs[name] = path;
            isConfigChanged = true;
            SaveToCfg();
        }

        public void DeleteFolder(string name)
        {
            if (hotDirs.Remove(name))
            {
                isConfigChanged = true;
                SaveToCfg();
            }
        }

        public void SaveToCfg()
        {
            if (!isConfigChanged)
                return;

            try
            {
                // 创建配置项列表
                var items = new List<ConfigItem>();
                int index = 1;
                foreach (var dir in hotDirs)
                {
                    items.Add(new ConfigItem
                    {
                        Key = $"name{index}",
                        Value = dir.Key
                    });
                    items.Add(new ConfigItem
                    {
                        Key = $"path{index}",
                        Value = dir.Value
                    });
                    index++;
                }

                // 更新配置
                form.configLoader.AddOrUpdateSection(CONFIG_SECTION, items);
                form.configLoader.SaveConfig();
                isConfigChanged = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存常用文件夹配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadFromCfg()
        {
            hotDirs.Clear();
            var section = form.configLoader.GetConfigSection(CONFIG_SECTION);
            if (section == null)
                return;

            try
            {
                // 获取所有配置项
                var items = section.Items;
                int maxIndex = items.Count / 2; // 每个文件夹有name和path两个配置项

                for (int i = 1; i <= maxIndex; i++)
                {
                    var name = items.FirstOrDefault(item => item.Key == $"name{i}")?.Value;
                    var path = items.FirstOrDefault(item => item.Key == $"path{i}")?.Value;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                    {
                        hotDirs[name] = path;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载常用文件夹配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ShowConfigDialog()
        {
            // 创建临时字典用于存储编辑过程中的更改
            var tempHotDirs = new Dictionary<string, string>(hotDirs);

            using var form = new Form
            {
                Text = "常用文件夹配置",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog
            };

            var listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill,
                MultiSelect = false
            };

            listView.Columns.Add("名称", 150);
            listView.Columns.Add("路径", 400);

            // 添加按钮面板
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            var deleteButton = new Button
            {
                Text = "删除",
                Width = 75,
                Location = new Point(10, 8)
            };

            var editButton = new Button
            {
                Text = "编辑",
                Width = 75,
                Location = new Point(95, 8)
            };

            var addButton = new Button
            {
                Text = "添加",
                Width = 75,
                Location = new Point(180, 8)
            };

            var okButton = new Button
            {
                Text = "确定",
                Width = 75,
                Location = new Point(400, 8),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "取消",
                Width = 75,
                Location = new Point(485, 8),
                DialogResult = DialogResult.Cancel
            };

            // 刷新列表视图
            void RefreshListView()
            {
                listView.Items.Clear();
                foreach (var dir in tempHotDirs)
                {
                    var item = new ListViewItem(dir.Key);
                    item.SubItems.Add(dir.Value);
                    listView.Items.Add(item);
                }
            }

            // 初始加载数据
            RefreshListView();

            // 删除按钮点击事件
            deleteButton.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count > 0)
                {
                    var name = listView.SelectedItems[0].Text;
                    if (MessageBox.Show($"确定要删除 {name} 吗？", "确认删除",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        tempHotDirs.Remove(name);
                        RefreshListView();
                    }
                }
            };

            // 编辑按钮点击事件
            editButton.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count > 0)
                {
                    var item = listView.SelectedItems[0];
                    var name = item.Text;
                    var path = item.SubItems[1].Text;

                    using var editForm = new Form
                    {
                        Text = "编辑文件夹",
                        Size = new Size(400, 150),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MinimizeBox = false,
                        MaximizeBox = false
                    };

                    var nameLabel = new Label { Text = "名称:", Location = new Point(10, 15) };
                    var nameTextBox = new TextBox
                    {
                        Text = name,
                        Location = new Point(70, 12),
                        Width = 300
                    };

                    var pathLabel = new Label { Text = "路径:", Location = new Point(10, 45) };
                    var pathTextBox = new TextBox
                    {
                        Text = path,
                        Location = new Point(70, 42),
                        Width = 300
                    };

                    var okButton = new Button
                    {
                        Text = "确定",
                        DialogResult = DialogResult.OK,
                        Location = new Point(200, 75)
                    };

                    var cancelButton = new Button
                    {
                        Text = "取消",
                        DialogResult = DialogResult.Cancel,
                        Location = new Point(290, 75)
                    };

                    editForm.Controls.AddRange(new Control[] { nameLabel, nameTextBox, pathLabel, pathTextBox, okButton, cancelButton });
                    editForm.AcceptButton = okButton;
                    editForm.CancelButton = cancelButton;

                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        tempHotDirs.Remove(name);
                        tempHotDirs[nameTextBox.Text] = pathTextBox.Text;
                        RefreshListView();
                    }
                }
            };

            // 添加按钮点击事件
            addButton.Click += (s, e) =>
            {
                using var folderBrowser = new FolderBrowserDialog();
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    string path = folderBrowser.SelectedPath;
                    string name = Path.GetFileName(path.TrimEnd('\\'));
                    if (string.IsNullOrEmpty(name))
                        name = path;

                    // 如果已存在相同名称的文件夹，添加数字后缀
                    string originalName = name;
                    int counter = 1;
                    while (tempHotDirs.ContainsKey(name))
                    {
                        name = $"{originalName}_{counter++}";
                    }

                    tempHotDirs[name] = path;
                    RefreshListView();
                }
            };

            // 确定按钮点击事件
            okButton.Click += (s, e) =>
            {
                hotDirs.Clear();
                foreach (var item in tempHotDirs)
                {
                    hotDirs[item.Key] = item.Value;
                }
                isConfigChanged = true;
                SaveToCfg();
                form.DialogResult = DialogResult.OK;
            };

            // 取消按钮点击事件
            cancelButton.Click += (s, e) =>
            {
                form.DialogResult = DialogResult.Cancel;
            };

            buttonPanel.Controls.AddRange(new Control[] { deleteButton, editButton, addButton, okButton, cancelButton });
            form.Controls.Add(listView);
            form.Controls.Add(buttonPanel);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            form.ShowDialog();
        }
    }
}