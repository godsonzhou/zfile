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
            return _cmdNameDict.TryGetValue(cmdName, out var item) ? item : null;
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
                            var cmdName = cmdParts[0];
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
            return cmdTable.GetByCmdName(cmdName);
        }

        public CmdTableItem? GetCmdById(int cmdId)
        {
            return cmdTable.GetByCmdId(cmdId);
        }
        // 处理由菜单栏和工具栏发起的动作
        public void processCmdByName(string cmdName)
        {
            if (cmdName.StartsWith("cm_"))
            {
                var cmdItem = cmdTable.GetByCmdName(cmdName);
                if (cmdItem != null)
                {
                    Console.WriteLine($"Processing command: {cmdItem}");
					// 在这里添加处理命令的逻辑
					processCmdByID(cmdItem.Value.CmdId);
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
                    processCmdByID(cmdId);
                }
            }
        }
        public void processCmdByID(int cmdId)
        {
            if (cmdTable.GetByCmdId(cmdId) != null)
            {
                Console.WriteLine($"Processing command: {cmdTable.GetByCmdId(cmdId)}");
                // 在这里添加处理命令的逻辑
                switch (cmdId)
                {
                    case 301:
                        // 若owner是IActiveListViewChangeable的实例，则调用activeListViewChange方法
                        //if (owner is IActiveListViewChangeable changeableOwner)
                        owner.ActiveListViewChange(View.List);
                        break;
                    case 302:
                        owner.ActiveListViewChange(View.Details);
                        break;
                    case 490:
                        owner.OpenOptions();
                        break;
					case 511: // 添加对 cmdID 为 511 的处理
						owner.OpenCommandPrompt();
						break;
					case 903:
						owner.do_cm_list();
						break;
					case 2950:
                        owner.ThemeToggle();
                        break;
					case 3012:  //lock the bookmark
						owner.uiManager.BookmarkManager.ToggleCurrentBookmarkLock(owner.uiManager.isleft);
						break;
                    case 24340:
                        owner.ExitApp();
                        break;
					//否则do nothing
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

