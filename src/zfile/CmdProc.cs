using System.Text;
using WinFormsApp1;

namespace CmdProcessor
{
	public struct CmdTableItem
    {
        public string CmdName;
        public int CmdId;
        public string Description;
        public string ZhDesc;

        public CmdTableItem(string cmdName, int cmdId, string description, string zhDesc)
        {
            CmdName = cmdName;
            CmdId = cmdId;
            Description = description;
            ZhDesc = zhDesc;
        }
    }

    public class CmdTable
    {
        private readonly Dictionary<string, CmdTableItem> _cmdNameDict = new();
        private readonly Dictionary<int, CmdTableItem> _cmdIdDict = new();

        public void Add(CmdTableItem item)
        {
            _cmdNameDict[item.CmdName] = item;
            _cmdIdDict[item.CmdId] = item;
        }

        public CmdTableItem? GetByCmdName(string cmdName)
        {
            return _cmdNameDict.TryGetValue(cmdName.ToLower(), out var item) ? item : null;
        }

        public CmdTableItem? GetByCmdId(int cmdId)
        {
            return _cmdIdDict.TryGetValue(cmdId, out var item) ? item : null;
        }
    }

    public static class ConfigLoader
    {
        public static CmdTable LoadCmdTable(string totalCmdPath, string wcmIconsPath)
        {
            var cmdTable = new CmdTable();
            var zhDescDict = LoadZhDesc(wcmIconsPath);

            using (var reader = new StreamReader(totalCmdPath, Encoding.GetEncoding("GB2312")))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("cm_"))
                    {
                        var parts = line.Split(';');
                        var cmdParts = parts[0].Split('=');
                        if (cmdParts.Length == 2 && int.TryParse(cmdParts[1], out var cmdId))
                        {
                            var cmdName = cmdParts[0].ToLower();//bugfix: cm_SrcThumbs in cfgfile�� but in toolbarstrip, the button trigger cmd is cm_srcthumbs, so we need to convert cm_SrcThumbs to cm_srcthumbs
							var description = parts.Length > 1 ? parts[1] : string.Empty;
                            var zhDesc = zhDescDict.TryGetValue(cmdId, out var desc) ? desc : string.Empty;
                            var cmdItem = new CmdTableItem(cmdName, cmdId, description, zhDesc);
                            cmdTable.Add(cmdItem);
                        }
                    }
                }
            }

            return cmdTable;
        }

        private static Dictionary<int, string> LoadZhDesc(string wcmIconsPath)
        {
            var zhDescDict = new Dictionary<int, string>();

            using (var reader = new StreamReader(wcmIconsPath, Encoding.GetEncoding("GB2312")))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Contains('='))
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2 && int.TryParse(parts[0], out var cmdId))
                        {
                            zhDescDict[cmdId] = parts[1];
                        }
                    }
                }
            }

            return zhDescDict;
        }
    }
	public class KeyMgr
	{
		private Dictionary<string, string> keymap = new Dictionary<string, string>();
		public KeyMgr()
		{
			loadFromConfig("wincmd.ini", "Shortcuts");
			loadFromConfig("wincmd.ini", "ShortcutsWin");
		}
		public void Add(string key, string value)
		{
			keymap[key] = value;
		}
		public string GetByKeyCode(System.Windows.Forms.Keys k)
		{
			return Get(k.ToString());
		}
		public string Get(string key)
		{
			if (keymap.TryGetValue(key, out _)) 
				return keymap[key];
			return string.Empty;
		}
		public bool Contains(string key)
		{
			return keymap.ContainsKey(key);
		}
		public void Remove(string key)
		{
			keymap.Remove(key);
		}
		public void Clear()
		{
			keymap.Clear();
		}
		public int Count()
		{
			return keymap.Count;
		}
		public string[] GetKeys()
		{
			return keymap.Keys.ToArray();
		}
		private void loadFromConfig(string path, string section)
		{
			// ��ȡ�����ļ��еĿ�ݼ�ӳ�䣬λ��section����
			// ���磺[Shortcuts]
			// cm_copy=Ctrl+C
			// [ShortcutsWin]
			// em_py=Ctrl+Insert
			var cfg = Helper.ReadSectionContent(Constants.ZfilePath + path, section);
			foreach (var line in cfg)
			{
				if (line.Contains('='))
				{
					var parts = line.Split('=');
					if (parts.Length == 2)
					{
						Add(parts[0], parts[1]);
					}
				}
			}
		}
	}
    public class CmdProc
    {
        public CmdTable cmdTable;
        private Form1 owner;

        public CmdProc(Form1 owner)
        {
            cmdTable = new CmdTable();
            InitializeCmdTable(Constants.ZfilePath + "TOTALCMD.INC", Constants.ZfilePath+"WCMD_CHN.INC");
            this.owner = owner;
        }

        public void InitializeCmdTable(string totalCmdPath, string wcmIconsPath)
        {
            cmdTable = ConfigLoader.LoadCmdTable(totalCmdPath, wcmIconsPath);
        }

        public CmdTableItem? GetCmdByName(string cmdName)
        {
			//if (cmdName[0] == '"') 
			//	cmdName = cmdName.TrimStart('"').TrimEnd('"');
			return cmdTable.GetByCmdName(cmdName);
        }

        public CmdTableItem? GetCmdById(int cmdId)
        {
            return cmdTable.GetByCmdId(cmdId);
        }
        // �����ɲ˵����͹���������Ķ���
        public void ExecCmdByName(string cmdName)
        {
            if (cmdName.StartsWith("cm_"))
            {
                var cmdItem = cmdTable.GetByCmdName(cmdName);
                if (cmdItem != null)
                {
                    Console.WriteLine($"Processing command: {cmdItem}");
					// ��������Ӵ���������߼�
					ExecCmdByID(cmdItem.Value.CmdId);
                }
                else
                {
                    throw new KeyNotFoundException("Command name does not exist.");
                }
            }
            else
            {
                var parts = cmdName.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out var cmdId))
                {
                    ExecCmdByID(cmdId);
                }
            }
        }
        public void ExecCmdByID(int cmdId)
        {
            if (cmdTable.GetByCmdId(cmdId) != null)
            {
                Console.WriteLine($"Processing command: {cmdTable.GetByCmdId(cmdId)}");
                // ��������Ӵ���������߼�
                switch (cmdId)
                {
                    case 101: // cm_copy
                        CopySelectedFiles();
                        break;
                    case 102: // cm_move
                        MoveSelectedFiles();
                        break;
                    case 103: // cm_delete
                        DeleteSelectedFiles();
                        break;
                    case 104: // cm_newfolder
                        CreateNewFolder();
                        break;
                    case 105: // cm_rename
                        RenameSelected();
                        break;
                    case 106: // cm_search
                        SearchFiles();
                        break;
                    case 107: // cm_properties
                        ShowFileProperties();
                        break;
                    case 108: // cm_compare
                        CompareFiles();
                        break;
                    case 109: // cm_pack
                        PackFiles();
                        break;
                    case 110: // cm_unpack
                        UnpackFiles();
                        break;
                    case 269:   //cm_srcthumbs
                        owner.SetViewMode(View.Tile);
                        break;
                    case 301:
                        owner.SetViewMode(View.List);
                        break;
                    case 302:
                        owner.SetViewMode(View.Details);
                        break;
                    case 490:
                        owner.OpenOptions();
                        break;
                    case 511: // cm_executedos
                        owner.OpenCommandPrompt();
                        break;
                    case 903: //cm_list
                        owner.do_cm_list();
                        break;
                    case 904: //cm_edit
                        owner.do_cm_edit();
                        break;
                    case 2950:
                        owner.ThemeToggle();
                        break;
                    case 3001:  //add new bookmark
                        owner.AddCurrentPathToBookmarks();
                        break;
                    case 3012:  //lock the bookmark
                        owner.uiManager.BookmarkManager.ToggleCurrentBookmarkLock(owner.uiManager.isleft);
                        break;
                    case 24340:
                        Form1.ExitApp();
                        break;
                    default:
                        MessageBox.Show($"����ID = {cmdId} ��δʵ��", "��ʾ");
                        break;
                }
            }
            else
            {
                throw new KeyNotFoundException("����ID������");
            }
        }

        // ����ѡ�е��ļ�
        private void CopySelectedFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Path.Combine(owner.currentDirectory, item.Text))
                .ToArray();

            // TODO: ��ʾ���ƶԻ������û�ѡ��Ŀ��·��
            var targetPath = owner.uiManager.isleft ? owner.uiManager.RightList.Tag?.ToString() : owner.uiManager.LeftList.Tag?.ToString();
            if (string.IsNullOrEmpty(targetPath))
            {
                MessageBox.Show("����ѡ��Ŀ��·��", "��ʾ");
                return;
            }

            try
            {
                foreach (var file in sourceFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var targetFile = Path.Combine(targetPath, fileName);
                    File.Copy(file, targetFile, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�����ļ�ʧ��: {ex.Message}", "����");
            }
        }

        // �ƶ�ѡ�е��ļ�
        private void MoveSelectedFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Path.Combine(owner.currentDirectory, item.Text))
                .ToArray();

            var targetPath = owner.uiManager.isleft ? owner.uiManager.RightList.Tag?.ToString() : owner.uiManager.LeftList.Tag?.ToString();
            if (string.IsNullOrEmpty(targetPath))
            {
                MessageBox.Show("����ѡ��Ŀ��·��", "��ʾ");
                return;
            }

            try
            {
                foreach (var file in sourceFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var targetFile = Path.Combine(targetPath, fileName);
                    File.Move(file, targetFile, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�ƶ��ļ�ʧ��: {ex.Message}", "����");
            }
        }

        // ɾ��ѡ�е��ļ�
        private void DeleteSelectedFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var files = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Path.Combine(owner.currentDirectory, item.Text))
                .ToArray();

            var result = MessageBox.Show(
                $"ȷ��Ҫɾ��ѡ�е� {files.Length} ���ļ���",
                "ȷ��ɾ��",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    foreach (var file in files)
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                        else if (Directory.Exists(file))
                            Directory.Delete(file, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ɾ���ļ�ʧ��: {ex.Message}", "����");
                }
            }
        }

        // �������ļ���
        private void CreateNewFolder()
        {
            var folderName = "�½��ļ���";
            var path = owner.currentDirectory;
            var newFolderPath = Path.Combine(path, folderName);
            var counter = 1;

            while (Directory.Exists(newFolderPath))
            {
                folderName = $"�½��ļ��� ({counter})";
                newFolderPath = Path.Combine(path, folderName);
                counter++;
            }

            try
            {
                Directory.CreateDirectory(newFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�����ļ���ʧ��: {ex.Message}", "����");
            }
        }

        // ������ѡ�е��ļ����ļ���
        private void RenameSelected()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var selectedItem = listView.SelectedItems[0];
            var oldPath = Path.Combine(owner.currentDirectory, selectedItem.Text);

            // ���ñ༭ģʽ
            selectedItem.BeginEdit();
        }

        // �����ļ�
        private void SearchFiles()
        {
            var searchForm = new Form
            {
                Text = "�����ļ�",
                Size = new Size(400, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var searchBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(360, 20),
                PlaceholderText = "���������ؼ���"
            };

            var searchButton = new Button
            {
                Text = "����",
                Location = new Point(150, 100),
                DialogResult = DialogResult.OK
            };

            searchForm.Controls.AddRange(new Control[] { searchBox, searchButton });
            searchForm.AcceptButton = searchButton;

            if (searchForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(searchBox.Text))
            {
                var searchPattern = searchBox.Text;
                var searchPath = owner.currentDirectory;

                try
                {
                    var files = Directory.GetFiles(searchPath, $"*{searchPattern}*", SearchOption.AllDirectories);
                    var results = new Form
                    {
                        Text = "�������",
                        Size = new Size(600, 400),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    var resultList = new ListView
                    {
                        Dock = DockStyle.Fill,
                        View = View.Details
                    };

                    resultList.Columns.Add("�ļ���", 200);
                    resultList.Columns.Add("·��", 350);

                    foreach (var file in files)
                    {
                        var item = new ListViewItem(Path.GetFileName(file));
                        item.SubItems.Add(Path.GetDirectoryName(file));
                        resultList.Items.Add(item);
                    }

                    results.Controls.Add(resultList);
                    results.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�����ļ�ʱ����: {ex.Message}", "����");
                }
            }
        }

        // ��ʾ�ļ�����
        private void ShowFileProperties()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var selectedItem = listView.SelectedItems[0];
            var filePath = Path.Combine(owner.currentDirectory, selectedItem.Text);

            try
            {
                var info = new FileInfo(filePath);
                var sb = new StringBuilder();
                sb.AppendLine($"����: {info.Name}");
                sb.AppendLine($"����: {(info.Attributes.HasFlag(FileAttributes.Directory) ? "�ļ���" : "�ļ�")}");
                sb.AppendLine($"λ��: {info.DirectoryName}");
                sb.AppendLine($"��С: {FormatFileSize(info.Length)}");
                sb.AppendLine($"����ʱ��: {info.CreationTime}");
                sb.AppendLine($"�޸�ʱ��: {info.LastWriteTime}");
                sb.AppendLine($"����ʱ��: {info.LastAccessTime}");
                sb.AppendLine($"����: {info.Attributes}");

                MessageBox.Show(sb.ToString(), "�ļ�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�޷���ȡ�ļ�����: {ex.Message}", "����");
            }
        }

        // ��ʽ���ļ���С
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        // �Ƚ��ļ�
        private void CompareFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count != 2)
            {
                MessageBox.Show("��ѡ�������ļ����бȽ�", "��ʾ");
                return;
            }

            var file1 = Path.Combine(owner.currentDirectory, listView.SelectedItems[0].Text);
            var file2 = Path.Combine(owner.currentDirectory, listView.SelectedItems[1].Text);

            try
            {
                if (!File.Exists(file1) || !File.Exists(file2))
                {
                    MessageBox.Show("��ѡ�ļ�������", "����");
                    return;
                }

                var form = new Form
                {
                    Text = "�ļ��Ƚ�",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterScreen
                };

                var splitContainer = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal
                };

                var textBox1 = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Font = new Font("Consolas", 10)
                };

                var textBox2 = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Font = new Font("Consolas", 10)
                };

                splitContainer.Panel1.Controls.Add(textBox1);
                splitContainer.Panel2.Controls.Add(textBox2);
                form.Controls.Add(splitContainer);

                // ��ȡ�ļ�����
                textBox1.Text = File.ReadAllText(file1);
                textBox2.Text = File.ReadAllText(file2);

                // ������ʾ����
                HighlightDifferences(textBox1, textBox2);

                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�Ƚ��ļ�ʱ����: {ex.Message}", "����");
            }
        }

        // ������ʾ�ı�����
        private void HighlightDifferences(RichTextBox box1, RichTextBox box2)
        {
            var lines1 = box1.Text.Split('\n');
            var lines2 = box2.Text.Split('\n');

            box1.Text = "";
            box2.Text = "";

            for (int i = 0; i < Math.Max(lines1.Length, lines2.Length); i++)
            {
                var line1 = i < lines1.Length ? lines1[i] : "";
                var line2 = i < lines2.Length ? lines2[i] : "";

                if (line1 != line2)
                {
                    box1.SelectionBackColor = Color.LightPink;
                    box2.SelectionBackColor = Color.LightPink;
                }
                else
                {
                    box1.SelectionBackColor = Color.White;
                    box2.SelectionBackColor = Color.White;
                }

                box1.AppendText(line1 + "\n");
                box2.AppendText(line2 + "\n");
            }
        }

        // ����ļ�
        private void PackFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count == 0) return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "ZIP �ļ�|*.zip|�����ļ�|*.*",
                Title = "ѡ�񱣴�λ��"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var files = listView.SelectedItems.Cast<ListViewItem>()
                        .Select(item => Path.Combine(owner.currentDirectory, item.Text))
                        .ToArray();

                    System.IO.Compression.ZipFile.CreateFromDirectory(
                        owner.currentDirectory,
                        saveDialog.FileName,
                        System.IO.Compression.CompressionLevel.Optimal,
                        true);

                    MessageBox.Show("�ļ�������", "��ʾ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"����ļ�ʱ����: {ex.Message}", "����");
                }
            }
        }

        // ��ѹ�ļ�
        private void UnpackFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var zipPath = Path.Combine(owner.currentDirectory, selectedItem.Text);

            if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("��ѡ�� ZIP �ļ�", "��ʾ");
                return;
            }

            var folderDialog = new FolderBrowserDialog
            {
                Description = "ѡ���ѹĿ���ļ���"
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(
                        zipPath,
                        folderDialog.SelectedPath,
                        System.Text.Encoding.GetEncoding("GB2312"),
                        true);

                    MessageBox.Show("�ļ���ѹ���", "��ʾ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"��ѹ�ļ�ʱ����: {ex.Message}", "����");
                }
            }
        }
    }

}

