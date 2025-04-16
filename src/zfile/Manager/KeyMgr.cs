namespace zfile
{
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
			if (keymap.TryGetValue(key.ToUpper(), out var keydef))
				return keydef.Cmd;
			return "";
		}
		public void Add(string key, string cmd, bool iswin)
		{
			var k = iswin ? "#" + key : key;
			var keydef = new KeyDef(k, cmd);
			keymap[k] = keydef;
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
			// 读取配置文件中的快捷键映射，位于section段内
			// 例如：[Shortcuts]
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
			// 更新快捷键映射
			// 如果命令已存在，则更新快捷键
			// 如果快捷键已存在，则更新命令
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
			// 保存快捷键映射到配置文件
			// 例如：[Shortcuts]
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
}