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
					case 269:   //cm_srcthumbs
						owner.SetViewMode(View.Tile);
						break;
					case 301:
                        // ��owner��IActiveListViewChangeable��ʵ���������activeListViewChange����
                        //if (owner is IActiveListViewChangeable changeableOwner)
                        owner.SetViewMode(View.List);
                        break;
                    case 302:
                        owner.SetViewMode(View.Details);
                        break;
                    case 490:
                        owner.OpenOptions();
                        break;
					case 511: // ��Ӷ� cmdID Ϊ 511 �Ĵ���
						owner.OpenCommandPrompt();
						break;
					case 903:
						owner.do_cm_list();
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
					//����do nothing
					default:
						MessageBox.Show("cmd id = {0} has not been implemented yet", cmdId.ToString());
						break;
                }
            }
            else
            {
                throw new KeyNotFoundException("Command ID does not exist.");
            }
        }
    }

}

