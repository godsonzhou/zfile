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
			var keydef = new KeyDef(iswin ? "#" + key : key, cmd);
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

	public class CmdProc
	{
		public CmdTable cmdTable;
		private Form1 owner;
		private List<MenuInfo> emCmds;

		public CmdProc(Form1 owner)
		{
			cmdTable = new CmdTable();
			InitializeCmdTable(Constants.ZfileCfgPath + "TOTALCMD.INC", Constants.ZfileCfgPath + "WCMD_CHN.INC");//读取cm_开头的内部命令与ID的对应关系
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
		// 处理由菜单栏和工具栏发起的动作
		public void ExecCmd(string cmdName, string param = "", string workingdir = "")
		{
			cmdName = cmdName.Trim();
			if (cmdName.Equals(string.Empty)) return;
			//support cm_xx, em_xx, "xx, cmdid", regedit.exe, control.exe xxx.cpl, cmdid
			if (cmdName.StartsWith("em_"))
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
					// 在这里添加处理命令的逻辑
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
					//可能是可执行文件名称,比如regedit.exe, 直接运行
					//if (Path.GetExtension(cmdName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
					//{
					//	Process.Start(cmdName);//insufficient permission, bugfix
					//}
					//else
					//{
					//	// 使用系统默认关联程序打开文件
					//	Process.Start(new ProcessStartInfo(cmdName) { UseShellExecute = true });
					//}
					try
					{
						var args = owner.se.PrepareParameter(param, null, "");
						foreach(var arg in args) 
						{
							// 使用 ProcessStartInfo 设置启动进程的详细信息
							var startInfo = new ProcessStartInfo
							{
								FileName = Helper.GetPathByEnv(cmdName),
								UseShellExecute = true,
								Arguments = arg,
								Verb = "runas" // 请求管理员权限
							};
							if (workingdir != "")
								startInfo.WorkingDirectory = workingdir;
							if (cmdName.StartsWith("control.exe", StringComparison.OrdinalIgnoreCase))
								owner.OpenCommandPrompt(cmdName);   //TODO: SHELLEXECUTEHELPER.EXECUTECOMMAND合并（增加了参数的处理）
							else
								Process.Start(startInfo);

						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"无法启动进程: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				// 在这里添加处理命令的逻辑
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
					case 562: // cm_encode
						do_cm_encode(param);
						break;

					case 563: // cm_decode
						do_cm_decode(param);
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
					case 2924:  //命令ID=2924,Name=cm_commandbrowser尚未实现
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
						MessageBox.Show($"命令ID = {cmdId}, Name = {cmdItem?.CmdName} 尚未实现", "提示");
						break;
				}
			}
			else
			{
				throw new KeyNotFoundException("命令ID不存在");
			}
		}
		private void do_cm_decode(string param)
		{
			if (param.Equals(string.Empty))
			{
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请先选择要解码的文件", "提示");
					return;
				}

				using var dialog = new EncodeDialog();
				//if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory, item.Text);
					string decodedContent;
					try
					{
						string encodedContent = File.ReadAllText(sourcePath);
						decodedContent = Path.GetExtension(sourcePath) switch
						{
							".B64" => DecodeBase64(encodedContent),
							".UUE" => DecodeUUEncode(encodedContent),
							".XXE" => DecodeXXEncode(encodedContent),
							_ => throw new InvalidOperationException("仅支持编码格式B64/UUE/XXE")
						};

						var targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(item.Text));
						File.WriteAllText(targetFile, decodedContent);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"解码文件 {item.Text} 时出错: {ex.Message}", "错误");
					}
				}
			}
			else
			{
				var files = owner.se.PrepareParameter(param, null, "");
				var targetPath = owner.currentDirectory;
				foreach(var file in files)
				{
					var sourcePath = Path.Combine(owner.currentDirectory, file);
					string decodedContent;
					try
					{
						string encodedContent = File.ReadAllText(sourcePath);
						decodedContent = Path.GetExtension(sourcePath) switch
						{
							"B64" => DecodeBase64(encodedContent),
							"UUE" => DecodeUUEncode(encodedContent),
							"XXE" => DecodeXXEncode(encodedContent),
							_ => throw new InvalidOperationException("仅支持编码格式B64/UUE/XXE")
						};

						var targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(file));
						File.WriteAllText(targetFile, decodedContent);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"解码文件 {file} 时出错: {ex.Message}", "错误");
					}
				}

			}
			owner.RefreshPanel();
		}

		private string DecodeBase64(string encodedContent)
		{
			byte[] bytes = Convert.FromBase64String(encodedContent);
			return Encoding.UTF8.GetString(bytes);
		}

		private string DecodeUUEncode(string encodedContent)
		{
			// 实现 UUEncode 解码逻辑
			var decodedBytes = new List<byte>();
			var lines = encodedContent.Split('\n');

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line == "end" || line == "`")
					continue;

				int length = line[0] - 32;
				if (length <= 0)
					continue;

				for (int i = 1; i < line.Length; i += 4)
				{
					byte a = (byte)(line[i] - 32);
					byte b = (byte)(line[i + 1] - 32);
					byte c = (byte)(line[i + 2] - 32);
					byte d = (byte)(line[i + 3] - 32);

					byte e = (byte)((a << 2) | (b >> 4));
					byte f = (byte)((b << 4) | (c >> 2));
					byte g = (byte)((c << 6) | d);

					decodedBytes.Add(e);
					if (decodedBytes.Count < length) decodedBytes.Add(f);
					if (decodedBytes.Count < length) decodedBytes.Add(g);
				}
			}

			return Encoding.UTF8.GetString(decodedBytes.ToArray());
		}

		private string DecodeXXEncode(string encodedContent)
		{
			// 实现 XXEncode 解码逻辑
			var decodedBytes = new List<byte>();
			var lines = encodedContent.Split('\n');

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line == "end" || line == "+")
					continue;

				int length = line[0] - 32;
				if (length <= 0)
					continue;

				for (int i = 1; i < line.Length; i += 4)
				{
					byte a = (byte)(line[i] - 42);
					byte b = (byte)(line[i + 1] - 42);
					byte c = (byte)(line[i + 2] - 42);
					byte d = (byte)(line[i + 3] - 42);

					byte e = (byte)((a << 2) | (b >> 4));
					byte f = (byte)((b << 4) | (c >> 2));
					byte g = (byte)((c << 6) | d);

					decodedBytes.Add(e);
					if (decodedBytes.Count < length) decodedBytes.Add(f);
					if (decodedBytes.Count < length) decodedBytes.Add(g);
				}
			}

			return Encoding.UTF8.GetString(decodedBytes.ToArray());
		}
		private void encode_files(string sourcePath)
		{
		
		}
		private void do_cm_encode(string param)
		{
			if (param.Equals(string.Empty))
			{
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请先选择要编码的文件", "提示");
					return;
				}
				using var dialog = new EncodeDialog();
				if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory, item.Text);
					string extension = dialog.SelectedEncoding switch
					{
						"MIME (Base64)" => ".B64",
						"UUEncode" => ".UUE",
						"XXEncode" => ".XXE",
						_ => ".B64"
					};

					var targetFile = Path.Combine(targetPath,
						Path.GetFileNameWithoutExtension(item.Text) + extension);

					try
					{
						byte[] fileContent = File.ReadAllBytes(sourcePath);
						string encodedContent = dialog.SelectedEncoding switch
						{
							"MIME (Base64)" => Convert.ToBase64String(fileContent),
							"UUEncode" => UUEncode(fileContent),
							"XXEncode" => XXEncode(fileContent),
							_ => Convert.ToBase64String(fileContent)
						};

						// 添加文件头
						var header = string.Empty;// $"begin {Path.GetFileName(sourcePath)}\n";

						if (dialog.FileSize > 0 || dialog.LineCount > 0)
						{
							// 分割编码后的内容
							var parts = SplitEncodedContent(encodedContent, dialog.FileSize, dialog.LineCount);
							for (int i = 0; i < parts.Count; i++)
							{
								var partFile = i == 0 ? targetFile :
									Path.Combine(targetPath,
										Path.GetFileNameWithoutExtension(targetFile) +
										$"_{i + 1}{extension}");
								File.WriteAllText(partFile, header + parts[i]);
							}
						}
						else
						{
							File.WriteAllText(targetFile, header + encodedContent);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"编码文件 {item.Text} 时出错: {ex.Message}", "错误");
					}
				}
			}
			else {
				using var dialog = new EncodeDialog();
				if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory : dialog.TargetPath;

				var files = owner.se.PrepareParameter(param, null, "");
				foreach (var file in files) {
					var sourcePath = Path.Combine(owner.currentDirectory, file);
					string extension = dialog.SelectedEncoding switch
					{
						"MIME (Base64)" => ".B64",
						"UUEncode" => ".UUE",
						"XXEncode" => ".XXE",
						_ => ".B64"
					};

					var targetFile = Path.Combine(targetPath,
						Path.GetFileNameWithoutExtension(file) + extension);

					try
					{
						byte[] fileContent = File.ReadAllBytes(sourcePath);
						string encodedContent = dialog.SelectedEncoding switch
						{
							"MIME (Base64)" => Convert.ToBase64String(fileContent),
							"UUEncode" => UUEncode(fileContent),
							"XXEncode" => XXEncode(fileContent),
							_ => Convert.ToBase64String(fileContent)
						};

						// 添加文件头
						var header = $"begin {Path.GetFileName(sourcePath)}\n";

						if (dialog.FileSize > 0 || dialog.LineCount > 0)
						{
							// 分割编码后的内容
							var parts = SplitEncodedContent(encodedContent, dialog.FileSize, dialog.LineCount);
							for (int i = 0; i < parts.Count; i++)
							{
								var partFile = i == 0 ? targetFile :
									Path.Combine(targetPath,
										Path.GetFileNameWithoutExtension(targetFile) +
										$"_{i + 1}{extension}");
								File.WriteAllText(partFile, header + parts[i]);
							}
						}
						else
						{
							File.WriteAllText(targetFile, header + encodedContent);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"编码文件 {file} 时出错: {ex.Message}", "错误");
					}
				}
			}
		

			owner.RefreshPanel();
		}

		private string UUEncode(byte[] data)
		{
			var sb = new StringBuilder();
			const int lineLength = 45;

			for (int i = 0; i < data.Length; i += lineLength)
			{
				int blockLength = Math.Min(lineLength, data.Length - i);
				sb.Append((char)(blockLength + 32));

				for (int j = 0; j < blockLength; j += 3)
				{
					byte a = data[i + j];
					byte b = j + 1 < blockLength ? data[i + j + 1] : (byte)0;
					byte c = j + 2 < blockLength ? data[i + j + 2] : (byte)0;

					byte d = (byte)((a >> 2) & 0x3f);
					byte e = (byte)(((a << 4) | ((b >> 4) & 0xf)) & 0x3f);
					byte f = (byte)(((b << 2) | ((c >> 6) & 0x3)) & 0x3f);
					byte g = (byte)(c & 0x3f);

					sb.Append((char)(d + 32));
					sb.Append((char)(e + 32));
					if (j + 1 < blockLength) sb.Append((char)(f + 32));
					if (j + 2 < blockLength) sb.Append((char)(g + 32));
				}
				sb.AppendLine();
			}
			sb.AppendLine("`");
			return sb.ToString();
		}

		private string XXEncode(byte[] data)
		{
			var sb = new StringBuilder();
			const int lineLength = 45;

			for (int i = 0; i < data.Length; i += lineLength)
			{
				int blockLength = Math.Min(lineLength, data.Length - i);
				sb.Append((char)(blockLength + 32));

				for (int j = 0; j < blockLength; j += 3)
				{
					byte a = data[i + j];
					byte b = j + 1 < blockLength ? data[i + j + 1] : (byte)0;
					byte c = j + 2 < blockLength ? data[i + j + 2] : (byte)0;

					byte d = (byte)((a >> 2) & 0x3f);
					byte e = (byte)(((a << 4) | ((b >> 4) & 0xf)) & 0x3f);
					byte f = (byte)(((b << 2) | ((c >> 6) & 0x3)) & 0x3f);
					byte g = (byte)(c & 0x3f);

					sb.Append(XXEncodeChar(d));
					sb.Append(XXEncodeChar(e));
					if (j + 1 < blockLength) sb.Append(XXEncodeChar(f));
					if (j + 2 < blockLength) sb.Append(XXEncodeChar(g));
				}
				sb.AppendLine();
			}
			sb.AppendLine("+");
			return sb.ToString();
		}

		private char XXEncodeChar(byte b)
		{
			return (char)(b + 42);
		}

		private List<string> SplitEncodedContent(string content, int maxSize, int maxLines)
		{
			var result = new List<string>();
			var lines = content.Split('\n');

			if (maxLines > 0)
			{
				for (int i = 0; i < lines.Length; i += maxLines)
				{
					result.Add(string.Join("\n",
						lines.Skip(i).Take(maxLines)));
				}
			}
			else if (maxSize > 0)
			{
				var currentPart = new StringBuilder();
				foreach (var line in lines)
				{
					if (currentPart.Length + line.Length > maxSize)
					{
						result.Add(currentPart.ToString());
						currentPart.Clear();
					}
					currentPart.AppendLine(line);
				}
				if (currentPart.Length > 0)
				{
					result.Add(currentPart.ToString());
				}
			}
			else
			{
				result.Add(content);
			}

			return result;
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

		private void ShowCommandBrowser()
		{
			var form = new Form
			{
				Text = "命令浏览器",
				Size = new Size(800, 600),
				StartPosition = FormStartPosition.CenterParent,
				MinimizeBox = false,
				MaximizeBox = false
			};

			// 创建搜索面板
			var searchPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 40
			};

			var searchBox = new TextBox
			{
				Location = new Point(10, 10),
				Width = 200,
				PlaceholderText = "搜索命令..."
			};

			//var searchTypeCombo = new ComboBox
			//{
			//	Location = new Point(220, 10),
			//	Width = 120,
			//	DropDownStyle = ComboBoxStyle.DropDownList
			//};
			//searchTypeCombo.Items.AddRange(new string[] { "按ID搜索", "按名称搜索", "按描述搜索" });
			//searchTypeCombo.SelectedIndex = 0;

			searchPanel.Controls.AddRange(new Control[] { searchBox });//, searchTypeCombo

			// 创建ListView用于显示命令
			var listView = new ListView
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

			// 获取所有命令并填充ListView
			var commands = cmdTable.GetAll();
			foreach (var cmd in commands)
			{
				var item = new ListViewItem(cmd.CmdId.ToString());
				item.SubItems.Add(cmd.CmdName);
				item.SubItems.Add(cmd.Description);
				item.SubItems.Add(cmd.ZhDesc);
				listView.Items.Add(item);
			}

			// 添加搜索功能
			searchBox.TextChanged += (s, e) =>
			{
				string searchText = searchBox.Text.ToLower();
				bool foundMatch = false;
				foreach (ListViewItem item in listView.Items)
				{
					bool match = false;
					//switch (searchTypeCombo.SelectedIndex)
					//{
						//case 0: // ID
					match = item.Text.ToLower().Contains(searchText);
					//break;
					//case 1: // 名称
					if (!match)
					{
						match = item.SubItems[1].Text.ToLower().Contains(searchText);
						//break;
						//case 2: // 描述
						if (!match)
							match = item.SubItems[2].Text.ToLower().Contains(searchText) ||
								   item.SubItems[3].Text.ToLower().Contains(searchText);
						//break;
					}
					//}
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
			};

			// 双击命令时复制命令名称
			listView.DoubleClick += (s, e) =>
			{
				if (listView.SelectedItems.Count > 0)
				{
					string cmdName = listView.SelectedItems[0].SubItems[1].Text;
					Clipboard.SetText(cmdName);
					MessageBox.Show($"命令 {cmdName} 已复制到剪贴板", "提示",
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			};

			// 添加右键菜单
			var contextMenu = new ContextMenuStrip();
			var copyMenuItem = new ToolStripMenuItem("复制命令名称");
			var execMenuItem = new ToolStripMenuItem("执行命令");
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

			// 添加控件到窗体
			form.Controls.Add(listView);
			form.Controls.Add(searchPanel);
			// 设置初始焦点
			form.Load += (s, e) => searchBox.Focus();
			// 显示窗体
			form.ShowDialog();
		}

		// 添加新方法实现刷新功能
		private void do_cm_rereadsource()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			try
			{
				// 刷新当前面板
				owner.RefreshPanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"刷新目录失败: {ex.Message}", "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void do_cm_DirBranch()
		{
			owner.fsManager.isDirBranchMode = !owner.fsManager.isDirBranchMode;
			owner.RefreshPanel();
		}
		// 全选
		private void do_cm_SelectAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = true;
			}
			owner.uiManager.setArgs();
		}

		// 取消全选
		private void do_cm_ClearAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = false;
			}
			owner.uiManager.setArgs();
		}

		// 反选
		private void do_cm_InvertSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = !item.Selected;
			}
			owner.uiManager.setArgs();
		}

		// 选择相同扩展名文件
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
			owner.uiManager.setArgs();
		}

		// 存储的选择集合
		private List<string> savedSelection = new();

		// 保存选择
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

		// 恢复选择
		private void do_cm_RestoreSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;

			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = savedSelection.Contains(item.Text);
			}
			owner.uiManager.setArgs();
		}

		// 复制文件名到剪贴板
		private void do_cm_CopyNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var names = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>().Select(i => i.Text));
			Clipboard.SetText(names);
		}

		// 复制完整路径到剪贴板
		private void do_cm_CopyFullNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var paths = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>()
					.Select(i => Path.Combine(owner.currentDirectory, i.Text)));
			Clipboard.SetText(paths);
		}

		// 复制文件详细信息
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

		// 复制文件详细信息及完整路径
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
		// 添加导航命令的实现
		private void do_cm_gotopreviousdir()
		{
			if (owner.backStack.Count > 0)
			{
				// 将当前目录存入前进栈
				owner.forwardStack.Push(owner.currentDirectory);
				// 从后退栈获取上一个目录
				string previousPath = owner.backStack.Pop();
				// 导航到该目录，但不记录到历史（避免重复记录）
				owner.NavigateToPath(previousPath, false);
			}
		}

		private void do_cm_gotonextdir()
		{
			if (owner.forwardStack.Count > 0)
			{
				// 将当前目录存入后退栈
				owner.backStack.Push(owner.currentDirectory);
				// 从前进栈获取下一个目录
				string nextPath = owner.forwardStack.Pop();
				// 导航到该目录，但不记录到历史（避免重复记录）
				owner.NavigateToPath(nextPath, false);
			}
		}

		private void do_cm_gotoparent()
		{
			string? parentPath = Path.GetDirectoryName(owner.currentDirectory);
			if (!string.IsNullOrEmpty(parentPath))
			{
				// 记录当前目录到历史
				owner.RecordDirectoryHistory(parentPath);
				// 导航到父目录
				owner.NavigateToPath(parentPath);
			}
		}
		// 复制选中的文件
		private bool CopySelectedFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return false;

			var srcPath = Helper.getFSpath(!owner.uiManager.isleft ? owner.uiManager.RightTree.SelectedNode.FullPath : owner.uiManager.LeftTree.SelectedNode.FullPath);

			var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => Helper.GetListItemPath(item))
				.ToArray();

			// TODO: 显示复制对话框，让用户选择目标路径
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
				MessageBox.Show($"复制文件失败: {ex.Message}", "错误");
				return false;
			}
		}

		// 移动选中的文件
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
				MessageBox.Show("无效的目标路径", "错误");
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
				MessageBox.Show($"移动文件失败: {ex.Message}", "错误");
			}
		}

		// 删除选中的文件
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
					$"确定要删除选中的 {files.Length} 个文件吗？",
					"确认删除",
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
					MessageBox.Show($"删除文件失败: {ex.Message}", "错误");
				}
			}
		}

		// 创建新文件夹
		private void CreateNewFolder(string folderName = "新建文件夹")
		{
			var path = owner.currentDirectory;
			var newFolderPath = Path.Combine(path, folderName);

			FileSystemManager.CreateDirectory(newFolderPath);
			owner.RefreshPanel(owner.activeListView);
		}

		// 重命名选中的文件或文件夹
		private void RenameSelected()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var selectedItem = listView.SelectedItems[0];

			// 启用编辑模式
			selectedItem.BeginEdit();
		}

		// 搜索文件
		private void SearchFiles()
		{
			var searchForm = new Form
			{
				Text = "搜索文件",
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
				PlaceholderText = "输入搜索关键词"
			};

			var searchButton = new Button
			{
				Text = "搜索",
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
						Text = "搜索结果",
						Size = new Size(600, 400),
						StartPosition = FormStartPosition.CenterParent
					};

					var resultList = new ListView
					{
						Dock = DockStyle.Fill,
						View = View.Details
					};

					resultList.Columns.Add("文件名", 200);
					resultList.Columns.Add("路径", 350);

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
					MessageBox.Show($"搜索文件时出错: {ex.Message}", "错误");
				}
			}
		}

		// 显示文件属性
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
				sb.AppendLine($"名称: {info.Name}");
				sb.AppendLine($"类型: {(info.Attributes.HasFlag(FileAttributes.Directory) ? "文件夹" : "文件")}");
				sb.AppendLine($"位置: {info.DirectoryName}");
				sb.AppendLine($"大小: {FormatFileSize(info.Length)}");
				sb.AppendLine($"创建时间: {info.CreationTime}");
				sb.AppendLine($"修改时间: {info.LastWriteTime}");
				sb.AppendLine($"访问时间: {info.LastAccessTime}");
				sb.AppendLine($"属性: {info.Attributes}");

				MessageBox.Show(sb.ToString(), "文件属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"无法获取文件属性: {ex.Message}", "错误");
			}
		}

		// 格式化文件大小
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

		// 比较文件
		private void CompareFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count != 2)
			{
				MessageBox.Show("请选择两个文件进行比较", "提示");
				return;
			}

			var file1 = Path.Combine(owner.currentDirectory, listView.SelectedItems[0].Text);
			var file2 = Path.Combine(owner.currentDirectory, listView.SelectedItems[1].Text);

			try
			{
				if (!File.Exists(file1) || !File.Exists(file2))
				{
					MessageBox.Show("所选文件不存在", "错误");
					return;
				}

				var form = new Form
				{
					Text = "文件比较",
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

				// 读取文件内容
				textBox1.Text = File.ReadAllText(file1);
				textBox2.Text = File.ReadAllText(file2);

				// 高亮显示差异
				HighlightDifferences(textBox1, textBox2);

				form.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"比较文件时出错: {ex.Message}", "错误");
			}
		}

		// 高亮显示文本差异
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

		// 打包文件
		private void PackFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0) return;
			var targetfile = Path.Combine(owner.currentDirectory, listView.SelectedItems[0].Text) + ".zip";
			if (File.Exists(targetfile))
			{
				if (MessageBox.Show($"{targetfile} 已存在，是否替换？", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
					return;
				else
					File.Delete(targetfile);    //delete the old zip file
			}
			//var saveDialog = new SaveFileDialog
			//{
			//    Filter = "ZIP 文件|*.zip|所有文件|*.*",
			//    Title = "选择保存位置"
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

					MessageBox.Show("文件打包完成", "提示");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"打包文件时出错: {ex.Message}", "错误");
				}
			}
			owner.RefreshPanel(listView);
		}

		// 解压文件
		private void UnpackFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0) return;

			var selectedItem = listView.SelectedItems[0];
			var zipPath = Path.Combine(owner.currentDirectory, selectedItem.Text);

			if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))      //TODO:其他压缩格式的支持，使用插件
			{
				MessageBox.Show("请选择 ZIP 文件", "提示");
				return;
			}

			var folderDialog = new FolderBrowserDialog
			{
				Description = "选择解压目标文件夹"
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

					MessageBox.Show("文件解压完成", "提示");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"解压文件时出错: {ex.Message}", "错误");
				}
			}
		}

		private void ShowMultiRenameDialog()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("没有选中文件");
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

