using System.Diagnostics;
using System.IO;
using System.Text;
using WinFormsApp1;
using WinShell;
using zfile;
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

	public class KeyDef(string key, string cmd)
	{
		public string Key { get; set; } = key;
		public string Cmd { get; set; } = cmd;
		public bool HasShift => Key.Contains('+') && Key.Split('+')[0].Contains("S", StringComparison.OrdinalIgnoreCase);

		public bool HasCtrl => Key.Contains('+') && Key.Split('+')[0].Contains("C", StringComparison.OrdinalIgnoreCase);
		public bool HasAlt => Key.Contains('+') && Key.Split('+')[0].Contains("A", StringComparison.OrdinalIgnoreCase);
		public bool HasWin => Key.Contains('+') && Key.Split('+')[0].Contains("#", StringComparison.OrdinalIgnoreCase);
	}

	public class KeyMgr
    {
        public Dictionary<string, KeyDef> keymap = [];
		public Dictionary<string, KeyDef> cmdmap = [];
		private bool keymapChanged = false;
		public KeyMgr()
        {
            loadFromConfig("wincmd.ini", "Shortcuts", false);
            loadFromConfig("wincmd.ini", "ShortcutsWin", true);
        }
		public string GetCmdByKey(string key)
		{
			if (keymap.TryGetValue(key, out var keydef))
				return keydef.Cmd;
			return "";
		}
		public void Add(string key, string cmd, bool iswin)
        {
			var keydef = new KeyDef(iswin ? "#"+key : key, cmd);
            keymap[key] = keydef;
			cmdmap[cmd] = keydef;
        }
        public KeyDef? GetByKey(string key)
        {
            if (keymap.TryGetValue(key, out var result))
                return result;
            return null;
        }
		public KeyDef? GetByCmd(string cmd)
		{
			if (cmdmap.TryGetValue(cmd, out var result))
				return result;
			return null;
		}
        public void Remove(string cmd)
        {
			var keydef = GetByCmd(cmd);
			keymap.Remove(keydef.Key);
			cmdmap.Remove(keydef.Cmd);
		}
		public void Clear()
        {
            keymap.Clear();
			cmdmap.Clear();
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
			return cmdmap.Keys.ToArray();
		}
        private void loadFromConfig(string path, string section, bool iswin)
        {
            // ��ȡ�����ļ��еĿ�ݼ�ӳ�䣬λ��section����
            // ���磺[Shortcuts]
            // cm_copy=Ctrl+C
            // [ShortcutsWin]
            // em_py=Ctrl+Insert
            var cfg = Helper.ReadSectionContent(Constants.ZfileCfgPath + path, section);
            foreach (var line in cfg)
            {
                if (line.Contains('='))
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        Add(parts[0], parts[1].ToLower(), iswin);
                    }
                }
            }
        }
		public void UpdateKeyMapping(string cmd, string key)
		{
			// ���¿�ݼ�ӳ��
			// ��������Ѵ��ڣ�����¿�ݼ�
			// �����ݼ��Ѵ��ڣ����������
			if (cmdmap.TryGetValue(cmd, out var keydef))
			{
				cmdmap.Remove(cmd);
			}
			if (keymap.TryGetValue(key, out _))
			{
				 keymap.Remove(key);
			}
			Add(key, cmd, key.Contains('#'));
			keymapChanged = true;
		}
		public void SaveKeyMappingToConfigFile()
		{
			// �����ݼ�ӳ�䵽�����ļ�
			// ���磺[Shortcuts]
			// cm_copy=Ctrl+C
			// [ShortcutsWin]
			// em_py=Ctrl+Insert
			if (!keymapChanged) return;

			var shortcuts = new List<string>();
			var shortcutsWin = new List<string>();
			foreach (var key in keymap)
			{
				var cmd = key.Value.Cmd;
				var keydef = key.Value.Key;
				if (keydef.Contains("#"))
				{
					shortcutsWin.Add($"{keydef}={cmd}");
				}
				else
				{
					shortcuts.Add($"{keydef}={cmd}");
				}
			}
			Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "Shortcuts", shortcuts);
			Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ShortcutsWin", shortcutsWin);
		}
	}

    public class CmdProc
    {
        public CmdTable cmdTable;
        private Form1 owner;
		private List<MenuInfo> emCmds;

		public CmdProc(Form1 owner)
        {
            cmdTable = new CmdTable();
            InitializeCmdTable(Constants.ZfileCfgPath + "TOTALCMD.INC", Constants.ZfileCfgPath + "WCMD_CHN.INC");//��ȡcm_��ͷ���ڲ�������ID�Ķ�Ӧ��ϵ
			emCmds = Helper.ReadConfigFromFile(Constants.ZfileCfgPath + "Wcmd_chn.ini");
			this.owner = owner;
        }

        public void InitializeCmdTable(string totalCmdPath, string wcmIconsPath)
        {
            cmdTable = CFGLOADER.LoadCmdTable(totalCmdPath, wcmIconsPath);
        }

        public CmdTableItem? GetCmdByName(string cmdName)
        {
            return cmdTable.GetByCmdName(cmdName);
        }

        public CmdTableItem? GetCmdById(int cmdId)
        {
            return cmdTable.GetByCmdId(cmdId);
        }
		public void ExecCmdByMenuInfo(MenuInfo mi)
		{
			ExecCmd(mi.Cmd, mi.Param, mi.Path);
		}
        // �����ɲ˵����͹���������Ķ���
        public void ExecCmd(string cmdName, string param = "", string workingdir = "")
        {
			cmdName = cmdName.Trim();
			if (cmdName.Equals(string.Empty)) return;
			//support cm_xx, em_xx, "xx, cmdid", regedit.exe, control.exe xxx.cpl, cmdid
			if(cmdName.StartsWith("em_"))
			{
				var emCmd = emCmds.Find(x => x.Name.Equals(cmdName));
				if (emCmd != null)
				{
					ExecCmdByMenuInfo(emCmd);
					return;
				}
			}
			if (cmdName.StartsWith("cm_")) //TODO: add more prefix em_
			{
				var cmdparts = cmdName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				cmdName = cmdparts[0];
				if (cmdparts.Length > 1)
					//join the rest parts except the first one
					param += string.Join(' ', cmdparts.Skip(1));

				var cmdItem = cmdTable.GetByCmdName(cmdName);
                if (cmdItem != null)
                {
                    Console.WriteLine($"Processing command: {cmdItem}");
                    // ���������Ӵ���������߼�
                    ExecCmdByID(cmdItem.Value.CmdId, param);
					return;
                }
                Debug.Print($"Command name {cmdName} does not exist.");
            }
            else
            {
				var parts = cmdName.Split(',');
				if (parts.Length == 2 && int.TryParse(parts[1], out var cmdId))
				{
					ExecCmdByID(cmdId, param);
				}
				else
				{
					if (int.TryParse(cmdName, out cmdId)) { ExecCmdByID(cmdId, param); return; }
					//�����ǿ�ִ���ļ�����,����regedit.exe, ֱ������
					//if (Path.GetExtension(cmdName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
					//{
					//	Process.Start(cmdName);//insufficient permission, bugfix
					//}
					//else
					//{
					//	// ʹ��ϵͳĬ�Ϲ���������ļ�
					//	Process.Start(new ProcessStartInfo(cmdName) { UseShellExecute = true });
					//}
					try
					{
						// ʹ�� ProcessStartInfo �����������̵���ϸ��Ϣ
						var startInfo = new ProcessStartInfo
						{
							FileName = cmdName,
							UseShellExecute = true,
							Verb = "runas" // �������ԱȨ��
						};
						if (workingdir != "")
							startInfo.WorkingDirectory = workingdir;
						if (cmdName.StartsWith("control.exe", StringComparison.OrdinalIgnoreCase))
							owner.OpenCommandPrompt(cmdName);   //TODO: SHELLEXECUTEHELPER.EXECUTECOMMAND�ϲ��������˲����Ĵ�����
						else
							Process.Start(startInfo);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"�޷���������: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
            }
        }
        public void ExecCmdByID(int cmdId, string param = "")
        {
			var cmdItem = cmdTable.GetByCmdId(cmdId);

			if (cmdItem != null)
            {
                Console.WriteLine($"Processing command: {cmdItem}");
                // ���������Ӵ���������߼�
                switch (cmdId)
                {
					case 269:   //cm_srcthumbs
						owner.SetViewMode(View.Tile);
						break;
					case 301:   //cm_srcshort
						owner.SetViewMode(View.List);
						break;
					case 302:   //cm_srclong
						owner.SetViewMode(View.Details);
						break;

					case 490:   //cm_config
						owner.OpenOptions();
						break;
					case 500:   //cm_cdtree
						ShowDirectoryTreeSearch();
						break;
					case 501: // cm_searchfor
                        SearchFiles();
                        break;
                    case 508: // cm_packfiles
                        PackFiles();
                        break;
                    case 509: // cm_unpackfiles
                        UnpackFiles();
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
					case 529: // cm_RestoreSelection  
						do_cm_RestoreSelection();
						break;
					case 530: // cm_SaveSelection
						do_cm_SaveSelection();
						break;
					case 540: // cm_rereadsource
						do_cm_rereadsource();
						break;
					case 570:
						do_cm_gotopreviousdir();
						break;
					case 571:
						do_cm_gotonextdir();
						break;

					case 903: //cm_list
						owner.do_cm_list(param);
						break;
					case 904: //cm_edit
						owner.do_cm_edit();
						break;
					case 905: // cm_copy
						CopySelectedFiles();
						break;
					case 906: // cm_renmov
						MoveSelectedFiles();
						break;
					case 907: // cm_mkdir
						CreateNewFolder();
						break;
					case 908: // cm_delete
						DeleteSelectedFiles();
						break;

					case 1002: // cm_renameonly
						RenameSelected();
						break;
					case 1003: // cm_properties
						ShowFileProperties();
						break;

					case 2002:
						do_cm_gotoparent();
						break;
					case 2017: // cm_CopyNamesToClip
						do_cm_CopyNamesToClip();
						break;
					case 2018: // cm_CopyFullNamesToClip 
						do_cm_CopyFullNamesToClip();
						break;
					case 2020: // cm_filesync
						ShowSyncDirsDialog();
						break;
					case 2022: // cm_comparefilesbycontent
						CompareFiles();
						break;
					case 2026:
						do_cm_DirBranch();
						break;
					case 2036: // cm_CopyDetailsToClip
						do_cm_CopyDetailsToClip();
						break;
					case 2037: // cm_CopyFullDetailsToClip
						do_cm_CopyFullDetailsToClip();
						break;
					case 2400: // cm_multirename
						ShowMultiRenameDialog();
						break;
					case 2924:  //����ID=2924,Name=cm_commandbrowser��δʵ��
						ShowCommandBrowser();
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
						MessageBox.Show($"����ID = {cmdId}, Name = {cmdItem?.CmdName} ��δʵ��", "��ʾ");
                        break;
                }
            }
            else
            {
                throw new KeyNotFoundException("����ID������");
            }
        }
		private void ShowCommandBrowser()
		{
			var form = new Form
			{
				Text = "���������",
				Size = new Size(800, 600),
				StartPosition = FormStartPosition.CenterParent,
				MinimizeBox = false,
				MaximizeBox = false
			};

			// �����������
			var searchPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 40
			};

			var searchBox = new TextBox
			{
				Location = new Point(10, 10),
				Width = 200,
				PlaceholderText = "��������..."
			};

			var searchTypeCombo = new ComboBox
			{
				Location = new Point(220, 10),
				Width = 120,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			searchTypeCombo.Items.AddRange(new string[] { "��ID����", "����������", "����������" });
			searchTypeCombo.SelectedIndex = 0;

			searchPanel.Controls.AddRange(new Control[] { searchBox, searchTypeCombo });

			// ����ListView������ʾ����
			var listView = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				GridLines = true,
				MultiSelect = false
			};

			// ������
			listView.Columns.Add("ID", 80);
			listView.Columns.Add("��������", 200);
			listView.Columns.Add("����", 250);
			listView.Columns.Add("��������", 250);

			// ��ȡ����������ListView
			var commands = cmdTable.GetAll();
			foreach (var cmd in commands)
			{
				var item = new ListViewItem(cmd.CmdId.ToString());
				item.SubItems.Add(cmd.CmdName);
				item.SubItems.Add(cmd.Description);
				item.SubItems.Add(cmd.ZhDesc);
				listView.Items.Add(item);
			}

			// ������������
			searchBox.TextChanged += (s, e) =>
			{
				string searchText = searchBox.Text.ToLower();
				bool foundMatch = false;
				foreach (ListViewItem item in listView.Items)
				{
					bool match = false;
					switch (searchTypeCombo.SelectedIndex)
					{
						case 0: // ID
							match = item.Text.ToLower().Contains(searchText);
							break;
						case 1: // ����
							match = item.SubItems[1].Text.ToLower().Contains(searchText);
							break;
						case 2: // ����
							match = item.SubItems[2].Text.ToLower().Contains(searchText) ||
								   item.SubItems[3].Text.ToLower().Contains(searchText);
							break;
					}
					item.ForeColor = match || string.IsNullOrEmpty(searchText) ?
						SystemColors.WindowText : SystemColors.GrayText;
					if (match && !foundMatch)
					{
						// �ҵ���һ��ƥ����
						foundMatch = true;
						item.Selected = true;
						item.EnsureVisible(); // �������ɼ�����
					}
					else
					{
						item.Selected = false;
					}
				}
			};

			// ˫������ʱ������������
			listView.DoubleClick += (s, e) =>
			{
				if (listView.SelectedItems.Count > 0)
				{
					string cmdName = listView.SelectedItems[0].SubItems[1].Text;
					Clipboard.SetText(cmdName);
					MessageBox.Show($"���� {cmdName} �Ѹ��Ƶ�������", "��ʾ",
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};

			// �����Ҽ��˵�
			var contextMenu = new ContextMenuStrip();
			var copyMenuItem = new ToolStripMenuItem("������������");
			var execMenuItem = new ToolStripMenuItem("ִ������");
			copyMenuItem.Click += (s, e) =>
			{
				if (listView.SelectedItems.Count > 0)
				{
					string cmdName = listView.SelectedItems[0].SubItems[1].Text;
					Clipboard.SetText(cmdName);
				}
			}; 
			execMenuItem.Click += (s, e) =>
			{
				if (listView.SelectedItems.Count > 0)
				{
					string cmdName = listView.SelectedItems[0].SubItems[1].Text;
					ExecCmd(cmdName);
					form.Close();
				}
			};
			//contextMenu.Items.Add(copyMenuItem);
			contextMenu.Items.AddRange(new ToolStripItem[] { copyMenuItem, execMenuItem });
			listView.ContextMenuStrip = contextMenu;

			// ���ӿؼ�������
			form.Controls.Add(listView);
			form.Controls.Add(searchPanel);
			// ���ó�ʼ����
			form.Load += (s, e) => searchBox.Focus();
			// ��ʾ����
			form.ShowDialog();
		}

		// �����·���ʵ��ˢ�¹���
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

		private void ShowDirectoryTreeSearch()
		{
			// 获取当前驱动器
			string currentDrive = Path.GetPathRoot(owner.currentDirectory);
			if (string.IsNullOrEmpty(currentDrive))
				currentDrive = "C:\\";

			// 创建并显示目录树查找窗口
			var treeSearchForm = new DirectoryTreeSearchForm(owner, currentDrive);
			treeSearchForm.ShowDialog();
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
		// ���ӵ��������ʵ��
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

        private void ShowMultiRenameDialog()
        {
            var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("û��ѡ���ļ�");
				return;
			}
            using var dialog = new MultiRenameForm(listView, owner.currentDirectory);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                owner.RefreshPanel(listView);
            }
        }

        private void ShowSyncDirsDialog()
        {
            var leftPath = owner.uiManager.LeftTree.SelectedNode != null
                ? Helper.getFSpathbyTree(owner.uiManager.LeftTree.SelectedNode)
                : string.Empty;
            var rightPath = owner.uiManager.RightTree.SelectedNode != null
                ? Helper.getFSpathbyTree(owner.uiManager.RightTree.SelectedNode)
                : string.Empty;

            var syncDlg = new SyncDirsDlg(leftPath, rightPath);
            syncDlg.Show();
        }
    }

}

