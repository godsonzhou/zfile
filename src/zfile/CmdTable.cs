namespace zfile
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
}
