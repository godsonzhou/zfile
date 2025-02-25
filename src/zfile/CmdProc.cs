using System.Diagnostics;
using System.Text;
using WinFormsApp1;

namespace CmdProcessor
{
    public struct CmdTableItem(string cmdName, int cmdId, string description, string zhDesc)
    {
        public string CmdName = cmdName;
        public int CmdId = cmdId;
        public string Description = description;
        public string ZhDesc = zhDesc;
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
		public List<CmdTableItem> GetAll() 
		{ 
			return _cmdNameDict.Values.ToList();
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
        public Dictionary<string, string> keymap = new Dictionary<string, string>();
		public Dictionary<string,string> keymapReverse = new Dictionary<string, string>();
		public KeyMgr()
        {
            loadFromConfig("wincmd.ini", "Shortcuts");
            loadFromConfig("wincmd.ini", "ShortcutsWin");
        }
        public void Add(string key, string cmd)
        {
            keymap[key] = cmd;
			keymapReverse[cmd] = key;
        }
        public string GetByKeyCode(Keys k)
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
			var cmd = Get(key);
            keymap.Remove(key);
			keymapReverse.Remove(cmd);
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
		public string[] GetCmds()
		{
			return keymap.Values.ToArray();
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
                        Add(parts[0], parts[1].ToLower());
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
            InitializeCmdTable(Constants.ZfilePath + "TOTALCMD.INC", Constants.ZfilePath + "WCMD_CHN.INC");
            this.owner = owner;
        }

        public void InitializeCmdTable(string totalCmdPath, string wcmIconsPath)
        {
            cmdTable = ConfigLoader.LoadCmdTable(totalCmdPath, wcmIconsPath);
        }

        public CmdTableItem? GetCmdByName(string cmdName)
        {
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
					case 540: // cm_rereadsource
						do_cm_rereadsource();
						break;
					case 905: // cm_copy
                        CopySelectedFiles();
                        break;
                    case 906: // cm_renmov
                        MoveSelectedFiles();
                        break;
                    case 908: // cm_delete
                        DeleteSelectedFiles();
                        break;
                    case 907: // cm_mkdir
                        CreateNewFolder();
                        break;
                    case 501: // cm_searchfor
                        SearchFiles();
                        break;
                    case 1002: // cm_renameonly
                        RenameSelected();
                        break;
                    case 1003: // cm_properties
                        ShowFileProperties();
                        break;
                    case 2022: // cm_comparefilesbycontent
                        CompareFiles();
                        break;
                    case 508: // cm_packfiles
                        PackFiles();
                        break;
                    case 509: // cm_unpackfiles
                        UnpackFiles();
                        break;
                    case 269:   //cm_srcthumbs
                        owner.SetViewMode(View.Tile);
                        break;
                    case 301:	//cm_srcshort
                        owner.SetViewMode(View.List);
                        break;
                    case 302:	//cm_srclong
                        owner.SetViewMode(View.Details);
                        break;
                    case 490:	//cm_config
                        owner.OpenOptions();
                        break;
                    case 511: // cm_executedos
                        owner.OpenCommandPrompt();
                        break;
					case 523: // cm_SelectAll
						do_cm_SelectAll();
						break;
					case 524: // cm_ClearAll  
						do_cm_ClearAll();
						break;
					case 525: // cm_InvertSelection
						do_cm_InvertSelection();
						break;
					case 527: // cm_SelectByExt
						do_cm_SelectByExt();
						break;
					case 530: // cm_SaveSelection
						do_cm_SaveSelection();
						break;
					case 529: // cm_RestoreSelection  
						do_cm_RestoreSelection();
						break;
					case 2017: // cm_CopyNamesToClip
						do_cm_CopyNamesToClip();
						break;
					case 2018: // cm_CopyFullNamesToClip 
						do_cm_CopyFullNamesToClip();
						break;
					case 2036: // cm_CopyDetailsToClip
						do_cm_CopyDetailsToClip();
						break;
					case 2037: // cm_CopyFullDetailsToClip
						do_cm_CopyFullDetailsToClip();
						break;
					case 570:
						do_cm_gotopreviousdir();
						break;
					case 571:
						do_cm_gotonextdir();
						break;
					case 2002:
						do_cm_gotoparent();
						break;

                    case 903: //cm_list
                        owner.do_cm_list();
                        break;
                    case 904: //cm_edit
                        owner.do_cm_edit();
                        break;
					case 2026:
						do_cm_DirBranch();
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
		// ����·���ʵ��ˢ�¹���
		private void do_cm_rereadsource()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			try
			{
				// ˢ�µ�ǰ���
				owner.RefreshPanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"ˢ��Ŀ¼ʧ��: {ex.Message}", "����",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void do_cm_DirBranch()
		{
			owner.fsManager.isDirBranchMode = !owner.fsManager.isDirBranchMode;
			owner.RefreshPanel();
		}
		// ȫѡ
		private void do_cm_SelectAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = true;
			}
		}

		// ȡ��ȫѡ
		private void do_cm_ClearAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = false;
			}
		}

		// ��ѡ
		private void do_cm_InvertSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = !item.Selected;
			}
		}

		// ѡ����ͬ��չ���ļ�
		private void do_cm_SelectByExt()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var ext = Path.GetExtension(lv.SelectedItems[0].Text);
			foreach (ListViewItem item in lv.Items)
			{
				if (Path.GetExtension(item.Text).Equals(ext, StringComparison.OrdinalIgnoreCase))
				{
					item.Selected = true;
				}
			}
		}

		// �洢��ѡ�񼯺�
		private List<string> savedSelection = new();

		// ����ѡ��
		private void do_cm_SaveSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;

			savedSelection.Clear();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				savedSelection.Add(item.Text);
			}
		}

		// �ָ�ѡ��
		private void do_cm_RestoreSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;

			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = savedSelection.Contains(item.Text);
			}
		}

		// �����ļ�����������
		private void do_cm_CopyNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var names = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>().Select(i => i.Text));
			Clipboard.SetText(names);
		}

		// ��������·����������
		private void do_cm_CopyFullNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var paths = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>()
					.Select(i => Path.Combine(owner.currentDirectory, i.Text)));
			Clipboard.SetText(paths);
		}

		// �����ļ���ϸ��Ϣ
		private void do_cm_CopyDetailsToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var details = new StringBuilder();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				details.AppendLine(string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(si => si.Text)));
			}
			Clipboard.SetText(details.ToString());
		}

		// �����ļ���ϸ��Ϣ������·��
		private void do_cm_CopyFullDetailsToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var details = new StringBuilder();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				details.AppendLine(Path.Combine(owner.currentDirectory, item.Text) + "\t" +
					string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Skip(1).Select(si => si.Text)));
			}
			Clipboard.SetText(details.ToString());
		}
		// ��ӵ��������ʵ��
		private void do_cm_gotopreviousdir()
		{
			if (owner.backStack.Count > 0)
			{
				// ����ǰĿ¼����ǰ��ջ
				owner.forwardStack.Push(owner.currentDirectory);
				// �Ӻ���ջ��ȡ��һ��Ŀ¼
				string previousPath = owner.backStack.Pop();
				// ��������Ŀ¼��������¼����ʷ�������ظ���¼��
				owner.NavigateToPath(previousPath, false);
			}
		}

		private void do_cm_gotonextdir()
		{
			if (owner.forwardStack.Count > 0)
			{
				// ����ǰĿ¼�������ջ
				owner.backStack.Push(owner.currentDirectory);
				// ��ǰ��ջ��ȡ��һ��Ŀ¼
				string nextPath = owner.forwardStack.Pop();
				// ��������Ŀ¼��������¼����ʷ�������ظ���¼��
				owner.NavigateToPath(nextPath, false);
			}
		}

		private void do_cm_gotoparent()
		{
			string? parentPath = Path.GetDirectoryName(owner.currentDirectory);
			if (!string.IsNullOrEmpty(parentPath))
			{
				// ��¼��ǰĿ¼����ʷ
				owner.RecordDirectoryHistory(parentPath);
				// ��������Ŀ¼
				owner.NavigateToPath(parentPath);
			}
		}
		// ����ѡ�е��ļ�
		private bool CopySelectedFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return false;

            var srcPath = Helper.getFSpath(!owner.uiManager.isleft ? owner.uiManager.RightTree.SelectedNode.FullPath : owner.uiManager.LeftTree.SelectedNode.FullPath);

            var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Helper.GetListItemPath(item))
                .ToArray();

            // TODO: ��ʾ���ƶԻ������û�ѡ��Ŀ��·��
            var targetTree = owner.uiManager.isleft ? owner.uiManager.RightTree : owner.uiManager.LeftTree;
            var targetPath = Helper.getFSpath(targetTree.SelectedNode.FullPath);
            var isSamePath = targetPath.Equals(srcPath);

            var targetlist = owner.uiManager.isleft ? owner.uiManager.RightList : owner.uiManager.LeftList;
            try
            {
                if (owner.IsArchiveFile(srcPath))
                {
                    foreach (string fileName in sourceFiles)
                    {
                        owner.ExtractArchiveFile(srcPath, fileName, targetPath);
                    }
                    return true;
                }

                if (owner.IsArchiveFile(targetPath))
                {
                    string[] files = sourceFiles.Select(f => Path.Combine(srcPath, f)).ToArray();
                    owner.AddToArchive(targetPath, files);
                    var items = owner.LoadArchiveContents(targetPath);
                    var targetListView = (owner.uiManager.isleft ? owner.uiManager.RightList : owner.uiManager.LeftList);
                    targetListView.Items.Clear();
                    targetListView.Items.AddRange(items.ToArray());
                    return true;
                }

                FileSystemManager.CopyFilesAndDirectories(sourceFiles, targetPath);

                owner.RefreshPanel(targetlist);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�����ļ�ʧ��: {ex.Message}", "����");
                return false;
            }
        }

        // �ƶ�ѡ�е��ļ�
        public void MoveSelectedFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var srcpath = Helper.getFSpath(owner.activeTreeview.SelectedNode.FullPath);
            var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Helper.GetListItemPath(item))
                .ToArray();

            var targettree = owner.uiManager.isleft ? owner.uiManager.RightTree : owner.uiManager.LeftTree;
            var targetPath = Helper.getFSpath(targettree.SelectedNode.FullPath);
            if (string.IsNullOrEmpty(targetPath))
            {
                MessageBox.Show("��Ч��Ŀ��·��", "����");
                return;
            }
            if (srcpath.Equals(targetPath))
            {
                return;     //if srcpath eq targetpath, do not need move, do rename 
            }

            try
            {
                if (CopySelectedFiles())
                    DeleteSelectedFiles(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�ƶ��ļ�ʧ��: {ex.Message}", "����");
            }
        }

        // ɾ��ѡ�е��ļ�
        private void DeleteSelectedFiles(bool needConfirm = true)
        {
            Debug.Print("Delete files : >>");
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var files = listView.SelectedItems.Cast<ListViewItem>()
                .Select(item => Helper.GetListItemPath(item))
                .ToArray();
            var result = DialogResult.Yes;
            if (needConfirm)
            {
                result = MessageBox.Show(
                    $"ȷ��Ҫɾ��ѡ�е� {files.Length} ���ļ���",
                    "ȷ��ɾ��",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
            }
            if (result == DialogResult.Yes)
            {
                try
                {
                    if (owner.IsArchiveFile(owner.currentDirectory))
                    {
                        if (owner.DeleteFromArchive(owner.currentDirectory, files.ToArray()))
                        {
                            var items = owner.LoadArchiveContents(owner.currentDirectory);
                            owner.activeListView.Items.Clear();
                            owner.activeListView.Items.AddRange(items.ToArray());
                        }
                        return;
                    }
                    foreach (var file in files)
                    {
                        FileSystemManager.DeleteFile(file);
                    }
                    owner.RefreshPanel(listView);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ɾ���ļ�ʧ��: {ex.Message}", "����");
                }
            }
        }

        // �������ļ���
        private void CreateNewFolder(string folderName = "�½��ļ���")
        {
            var path = owner.currentDirectory;
            var newFolderPath = Path.Combine(path, folderName);

            FileSystemManager.CreateDirectory(newFolderPath);
            owner.RefreshPanel(owner.activeListView);
        }

        // ������ѡ�е��ļ����ļ���
        private void RenameSelected()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count <= 0) return;

            var selectedItem = listView.SelectedItems[0];

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
            var targetfile = Path.Combine(owner.currentDirectory, listView.SelectedItems[0].Text) + ".zip";
            if (File.Exists(targetfile))
            {
                if (MessageBox.Show($"{targetfile} �Ѵ��ڣ��Ƿ��滻��", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
                else
                    File.Delete(targetfile);    //delete the old zip file
            }
            //var saveDialog = new SaveFileDialog
            //{
            //    Filter = "ZIP �ļ�|*.zip|�����ļ�|*.*",
            //    Title = "ѡ�񱣴�λ��"
            //};

            //if (saveDialog.ShowDialog() == DialogResult.OK)
            //File.Delete(targetfile);	//delete the old zip file
            {
                try
                {
                    var files = listView.SelectedItems.Cast<ListViewItem>()
                        .Select(item => Helper.GetListItemPath(item))
                        .ToArray();

                    System.IO.Compression.ZipFile.CreateFromDirectory(
                        owner.currentDirectory,
                        //saveDialog.FileName,
                        targetfile,
                        System.IO.Compression.CompressionLevel.Optimal,
                        true);

                    MessageBox.Show("�ļ�������", "��ʾ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"����ļ�ʱ����: {ex.Message}", "����");
                }
            }
            owner.RefreshPanel(listView);
        }

        // ��ѹ�ļ�
        private void UnpackFiles()
        {
            var listView = owner.activeListView;
            if (listView == null || listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var zipPath = Path.Combine(owner.currentDirectory, selectedItem.Text);

            if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))		//TODO:����ѹ����ʽ��֧�֣�ʹ�ò��
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
                        Encoding.GetEncoding("GB2312"),
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

