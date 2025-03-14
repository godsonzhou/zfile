using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentFTP;
using MCPSharp;
using MCPSharp.Model.Schemas;
using MCPSharp.Model;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.Extensions.AI;
using WinShell;

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

	public class CmdProc
	{
		public CmdTable cmdTable;
		public Form1 owner;
		public List<MenuInfo> emCmds;
		private int targetIndex = 0;

		public CmdProc(Form1 owner)
		{
			cmdTable = new CmdTable();
			InitializeCmdTable(Constants.ZfileCfgPath + "TOTALCMD.INC", Constants.ZfileCfgPath + "WCMD_CHN.INC");//读取cm_开头的内部命令与ID的对应关系
			emCmds = Helper.ReadConfigFromFile(Constants.ZfileCfgPath + "Wcmd_chn.ini");
			this.owner = owner;
		}
		public void SaveEmCmdCfg() {
			Helper.WriteConfigToFile(Constants.ZfileCfgPath + "Wcmd_chn.ini", emCmds);
		}

		public void InitializeCmdTable(string totalCmdPath, string wcmIconsPath)
		{
			cmdTable = CFGLOADER.LoadCmdTable(totalCmdPath, wcmIconsPath);
		}

		public CmdTableItem? GetCmdByName(string cmdName)
		{
			return cmdTable.GetByCmdName(cmdName);
		}
		public MenuInfo? GetEmdByName(string emdName)
		{
			return emCmds.Find(e => e.Name.Equals(emdName));
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
			//var cmdItem = cmdTable.GetByCmdId(cmdId);

			//if (cmdItem != null)
			{
				//Console.WriteLine($"Processing command: {cmdItem}");
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

					case 321: // cm_srcbyname
						do_cm_srcbyname();
						break;
					case 322: // cm_srcbyext
						do_cm_srcbyext();
						break;
					case 323: // cm_srcbysize
						do_cm_srcbysize();
						break;
					case 324: // cm_srcbydatetime
						do_cm_srcbydatetime();
						break;
					case 325: // cm_srcunsorted
						do_cm_srcunsorted();
						break;
					case 330: // cm_srcnegorder
						do_cm_srcnegorder();
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
					case 512: // cm_netConnect
						do_cm_netConnect();
						break;
					case 513: // cm_netDisconnect
						do_cm_netDisconnect();
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

					case 532: // cm_matchsrc
						do_cm_matchsrc();
						break;
					case 540: // cm_rereadsource
						do_cm_rereadsource();
						break;

					case 550: // cm_ftpconnect
						ShowFtpConnectionManager();
						break;
					case 551: //命令ID=551，Name=cm_ftpnew
						do_cm_ftpnew();
						break;
					case 552: //命令ID=552,Name=cm_ftpdisconnect
						do_cm_ftpdisconnect();
						break;
					case 553: //命令ID=553，Name=cm_ftphiddenfiles 显示隐藏文件
						do_cm_ftphiddenfiles();
						break;
					case 554: // cm_ftpabort
						do_cm_ftpabort();
						break;
					case 555: // cm_ftpresumedownload
						do_cm_ftpresumedownload();
						break;
					case 556: // cm_ftpselecttransfermode
						do_cm_ftpselectransfermode();
						break;
					case 557: // cm_ftpaddtolist
						do_cm_ftpaddtolist();
						break;
					case 558: // cm_ftpdownloadlist
						do_cm_ftpdownloadlist();
						break;

					case 560: // cm_split
						do_cm_split(param);
						break;
					case 561: // cm_combine
						do_cm_combine(param);
						break;
					case 562: // cm_encode
						do_cm_encode(param);
						break;
					case 563: // cm_decode
						do_cm_decode(param);
						break;
					case 564:   // cm_crccreate
						do_cm_crccreate(param);
						break;
					case 565:   // cm_crccheck
						do_cm_crccheck(param);
						break;

					case 570:
						do_cm_gotopreviousdir();
						break;
					case 571:
						do_cm_gotonextdir();
						break;
					case 580:
						do_cm_configsavesettings();
						break;
					case 581:
						do_cm_configchangeinifiles();
						break;
					case 630:
						do_cm_register();
						break;
					case 690: // cm_about
						MessageBox.Show("Zfile v0.1.3.14 \r\n Author: zhou yaping \r\n Email: zhouyaping822@gmail.com");
						break;
					case 903: //cm_list
						owner.do_cm_list(param);
						break;
					case 904: //cm_edit
						owner.do_cm_edit(param);
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

					case 2001:
						do_cm_gotoroot();
						break;
					case 2002:
						do_cm_gotoparent();
						break;
					case 2004:
						do_cm_clearcommand();
						break;
					case 2005:
						do_cm_nextcommand();
						break;
					case 2006:
						do_cm_prevcommand();
						break;

					case 2011: //命令ID=2011,Name=cmswitchhidsy
						do_cm_switchhidsys();
						break;

					case 2017: // cm_CopyNamesToClip
						do_cm_CopyNamesToClip();
						break;
					case 2018: // cm_CopyFullNamesToClip 
						do_cm_CopyFullNamesToClip();
						break;
					case 2019: // 命令ID = 2019, Name = cm_addpathtocmdline
						do_cm_addpathtocmdline();
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

					case 2050: // 命令ID=2050,Name = cm_gotofirstfile
						do_cm_gotofirstfile();
						break;
					case 2053:
						do_cmgotoprevornextselected(false);
						break;
					case 2054: //命令ID=2054,Name = cmgotoprevselected
						do_cmgotoprevornextselected();
						break;
					case 2061:
						do_cm_gotodrivea();
						break;
					case 2062:
						do_cm_gotodriveb();
						break;
					case 2063:
						do_cm_gotodrivec();
						break;
					case 2064:
						do_cm_gotodrived();
						break;
					case 2065:
						do_cm_gotodrivee();
						break;
					case 2066:
						do_cm_gotodrivef();
						break;
					case 2067:
						do_cm_gotodriveg();
						break;
					case 2068:
						do_cm_gotodriveh();
						break;
					case 2086:
						do_cm_gotodrivez();
						break;

					case 2121:
						do_cm_opendesktop();
						break;
					case 2122:
						do_cm_opendrives();
						break;
					case 2123:
						do_cm_opencontrols();
						break;
					case 2124:
						do_cm_openfonts();
						break;
					case 2125:
						do_cm_opennetwork();
						break;
					case 2127:
						do_cm_openrecycled();
						break;

					case 2400: // cm_multirename
						ShowMultiRenameDialog();
						break;
					case 2901:
						do_cm_visbuttonbar();
						break;
					case 2902: //命令ID=2902,Name=cmvisdrivebuttons
						do_cm_visdrivebuttons();
						break;
					case 2906:
						do_cm_visdrivecombo();
						break;
					case 2909:
						do_cm_visstatusbar();
						break;

					case 2910:
						do_cm_viscmdline();
						break;
					case 2911: // 命令ID=2911,Name=cm_viskeybuttons
						do_cm_viskeybuttons();
						break;
					case 2916: // 命令ID=2916,Name=cm_visdirtabs
						do_cm_visdirtabs();
						break;
					case 2917: // 命令ID=2917，Name=cmswitchoverlayicons
						do_cm_switchoverlayicons();
						break;

					case 2924:  //命令ID=2924,Name=cm_commandbrowser尚未实现
						ShowCommandBrowser();
						break;
					case 2944:
						do_cm_visbuttonbar2();
						break;

					case 2950:
						owner.ThemeToggle();
						break;

					case 3001:  //add new bookmark
						owner.AddCurrentPathToBookmarks();
						break;
					case 3005: // 命令ID=3005，Name=cm switchtonexttab
						owner.uiManager.BookmarkManager.SwitchToPrevOrNextTab(false);
						break;
					case 3006: // 命令ID=3006，Name=cm switchtoprevioustab
						owner.uiManager.BookmarkManager.SwitchToPrevOrNextTab(true);
						break;
					case 3007: // 命令ID=3007,Name =cm_closecurrenttab
						owner.uiManager.BookmarkManager.CloseCurrentTab();
						break;
					case 3008: // 命令ID=3008,Name=cm_closealltabs
						owner.uiManager.BookmarkManager.CloseAllTabs();
						break;
					case 3009: // 命令ID=3009,Name=cm_dirtabsshowmenu
						do_cm_dirtabsshowmenu();
						break;
					case 3010: // 命令ID=3010Name=cmtogglelockcurrenttab
						owner.uiManager.BookmarkManager.ToggleCurrentBookmarkLock(owner.uiManager.isleft);
						break;
					case 3012:  //lock the bookmark
						owner.uiManager.BookmarkManager.ToggleCurrentBookmarkLock(owner.uiManager.isleft);
						break;

					case 3026: // cm_listExternal
						do_cm_listExternal(param);
						break;
					case 4003:
						do_cm_focuscmdline();
						break;

					case 5001: // 命令ID=5001,Name =cm_srcactivatetab1
					case 5002:
					case 5003:
					case 5004:
					case 5005:
					case 5006:
					case 5007:
					case 5008:
					case 5009:// 命令ID=5009,Name =cm_srcactivatetab9
						owner.uiManager.BookmarkManager.SwitchToNthTab(cmdId - 5000);
						break;
					case 5101: //命令ID = 5101, Name = cm_trgactivatetab1
					case 5102:
					case 5103:
					case 5104:
					case 5105:
					case 5106:
					case 5107:
					case 5108:
					case 5109:
						owner.uiManager.BookmarkManager.SwitchToNthTab(cmdId - 5100, true);
						break;

					case 11434: //命令ID=11434,Name=cm_ollama
						do_cm_llm_helper(param);
						break;
					case 11435: //网络爬虫
						do_cm_netCrawler(param);
						break;
					case 11436: //动态网页爬虫，利用chromedriver和selenium
						do_cm_ChromeCrawler(param);
						break;
					case 11437: // API caller
						var parameters = param.Split(' ');
						if (parameters.Length < 3)
							MessageBox.Show("the parameters should contain url key other at least");
						else
							do_cm_apicaller(parameters[0], parameters[1], parameters[2]);	//
						break;
					case 11438: // mcp client
						do_cm_mcpclient(param);
						break;
					case 11439: // mcp client with mcpsharp
						var lst = Task.Run(async () => { await do_cm_mcpclient1(param); });
						break;
					case 11440: // launch mcp server
						Task.Run(async () => { await do_cm_mcpserver(param); } );
						break;
					case 24340:
						Form1.ExitApp();
						break;
					case 34567:
						var licensegen = new LicenseGeneratorForm();
						licensegen.ShowDialog();
						break;

					default:
						var cmdItem = cmdTable.GetByCmdId(cmdId);
						if (cmdItem != null)
							MessageBox.Show($"命令ID = {cmdId}, Name = {cmdItem?.CmdName} 尚未实现", "提示");
						else
							MessageBox.Show($"命令ID = {cmdId} 尚未实现", "提示");
						break;
				}
			}
			//else
			//{
			//	throw new KeyNotFoundException("命令ID不存在");
			//}
		}
		private async Task do_cm_mcpserver(string param)
		{
			MCPServer.AddToolHandler(new Tool()
			{
				Name = "dynamicTool",
				Description = "A dynamic tool",
				InputSchema = new InputSchema
				{
					Type = "object",
					Required = ["input"],
					Properties = new Dictionary<string, ParameterSchema>{
						{"input", new ParameterSchema{Type="string", Description="Input value"}}
					}
				}
			}, (string input) => { return $"You provided: {input}"; });

			// Register with MCPServer
			MCPServer.Register<MySkillClass>();
			await MCPServer.StartAsync(param, "1.0.0");
		}
		private async Task<IList<AIFunction>> do_cm_mcpclient1(string param)
		{
			// Client-side integration
			MCPClient client = new("AIClient", "1.0", "path/to/mcp/server");
			IList<AIFunction> functions = await client.GetFunctionsAsync();
			return functions;
		}
		private void do_cm_mcpclient(string param)
		{
			Task.Run(async () => { await MCP.Launcher([param]); });
		}
		private void do_cm_switchoverlayicons()
		{

		}
		private void do_cm_visstatusbar()
		{

		}
		private void do_cm_visdrivecombo()
		{

		}
		private void do_cm_visdrivebuttons()
		{

		}
		private void do_cm_switchhidsys()
		{

		}
		private void do_cm_prevcommand()
		{
			owner.uiManager.ftpController.SetPrevCmd();
		}
		private void do_cm_nextcommand()
		{
			owner.uiManager.ftpController.SetNextCmd();
		}
		private void do_cm_clearcommand()
		{
			owner.uiManager.ftpController.SetCmdLine("");
		}
		private void do_cm_addpathtocmdline()
		{
			owner.uiManager.ftpController.SetCmdLine(owner.uiManager.ActivePathTextBox.CurrentNode.UniqueID);
		}
		private void do_cm_viskeybuttons()
		{
			owner.uiManager.ToggleToolStrip();
		}
		private void do_cm_visbuttonbar()
		{
			owner.uiManager.toolbarManager.TogglePanel();
		}
		private void do_cm_visbuttonbar2()
		{
			owner.uiManager.vtoolbarManager.TogglePanel();
		}
		private void do_cm_viscmdline()
		{
			owner.uiManager.ftpController.TogglePanel();
		}
		private void do_cm_focuscmdline()
		{
			owner.uiManager.ftpController.SetFocusCmdline();
		}
		private void do_cm_apicaller(string url, string key, string param){
			APICallerForm form = new APICallerForm(url, key, param);
			form.Tag = this;
			form.ShowDialog();
		}
		public string cm_apicaller(string url = "http://v.juhe.cn/toutiao/index", string apiKey = "de73e15a67f8b359d4ec409ae3e63aed", string param = "type=keji")
		{
			//string url = "http://v.juhe.cn/toutiao/index";
			//string apiKey = "您申请的调用APIkey";

			Dictionary<string, string> data = new Dictionary<string, string>();
			data.Add("key", apiKey);
			if (!param.Equals(string.Empty))
			{
				var _params = param.Split(',');
				foreach (var _param in _params)
				{
					//data.Add("type", "top");
					//data.Add("page", "20");
					//data.Add("page_size", "");
					//data.Add("is_filter", "");
					var x = _param.Split('=');
					data.Add(x[0], x[1]);
				}
			}
			using (WebClient client = new WebClient())
			{
				string fullUrl = url + "?" + string.Join("&", data.Select(x => x.Key + "=" + x.Value));

				try
				{
					string responseContent = client.DownloadString(fullUrl);
					dynamic responseData = JsonConvert.DeserializeObject(responseContent);

					if (responseData != null)
					{
						Debug.Print("Return Code: " + responseData["error_code"]);
						Debug.Print("Return Message: " + responseData["reason"]);
						return responseData["reason"];
					}
					else
					{
						Debug.Print("json解析异常！");
					}
				}
				catch (Exception)
				{
					Debug.Print("请检查其它错误");
				}
			}
			return string.Empty;
		}
		private string do_cm_ChromeCrawler(string param)
		{
			try
			{
				// 设置 ChromeDriver 的路径，需要根据实际情况修改
				//string chromeDriverPath = @"C:\Users\zhouy\Documents\Files\src\zfile\bin\Debug";
				ChromeOptions options = new ChromeOptions();
				// 可以选择无头模式，不显示浏览器窗口
				options.AddArgument("--headless");

				using (IWebDriver driver = new ChromeDriver(options))
				{
					// 要爬取的网址
					string url = param;
					driver.Navigate().GoToUrl(url);

					// 等待页面加载完成，可以根据实际情况调整等待时间
					Thread.Sleep(5000);

					// 获取页面源代码
					string content = driver.PageSource;
					Debug.Print(content);
					return content;
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"发生错误: {ex.Message}");
			}
			return string.Empty;
		}
		private void do_cm_netCrawler(string param)
		{
			Task.Run(async () => { await netCrawler(param); } );
			Debug.Print("crawler run started...");
		}
		static async Task netCrawler(string url)
		{
			try
			{
				// 要爬取的网址
				//string url = "https://www.example.com";
				string content = await FetchWebContent(url);
				Debug.Print(content);
			}
			catch (Exception ex)
			{
				Debug.Print($"发生错误: {ex.Message}");
			}
		}

		static async Task<string> FetchWebContent(string url)
		{
			using (HttpClient client = new HttpClient())
			{
				// 发送HTTP请求获取响应
				HttpResponseMessage response = await client.GetAsync(url);
				// 确保请求成功
				response.EnsureSuccessStatusCode();
				// 读取响应内容
				return await response.Content.ReadAsStringAsync();
			}
		}
		private void do_cm_listExternal(string param)
		{
			string externalViewer = owner.configLoader.FindConfigValue("Configuration", "Viewer");
			var str = externalViewer.Split(' ');
			var str1 = string.Join(" ", str[1..]);
			ExecCmd(str[0], param);
		}
		private void do_cm_ftpdownloadlist()
		{
			// 下载列表中的文件
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 显示下载列表并开始下载
			owner.fTPMGR.ProcessDownloadList();
		}

		private void do_cm_ftpaddtolist()
		{
			// 添加到下载列表
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 获取当前选中的文件或文件夹
			if (owner.activeListView.SelectedItems.Count > 0)
			{
				var selectedItem = owner.activeListView.SelectedItems[0];
				bool isDirectory = selectedItem.SubItems[3].Text == "<DIR>";
				string path = selectedItem.SubItems[1].Text;

				// 获取当前连接名称
				string connectionName = "";
				foreach (var node in owner.fTPMGR.ftpRootNode.Nodes)
				{
					if (node is TreeNode treeNode && treeNode.Tag is FtpNodeTag tag && tag.Path == path)
					{
						connectionName = tag.ConnectionName;
						break;
					}
				}

				if (!string.IsNullOrEmpty(connectionName) && owner.fTPMGR.ftpSources.TryGetValue(connectionName, out var source))
				{
					// 调用添加到下载列表方法
					owner.fTPMGR.AddToDownloadList(source, path, isDirectory);
				}
			}
			else
			{
				MessageBox.Show("请先选择要添加到下载列表的文件或文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void do_cm_ftpselectransfermode()
		{
			// 选择传输模式（ASCII/Binary）
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 创建传输模式选择对话框
			var dialog = new Form
			{
				Text = "选择传输模式",
				Size = new Size(300, 150),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var asciiRadio = new RadioButton
			{
				Text = "ASCII模式（文本文件）",
				Location = new Point(20, 20),
				Width = 250,
				Checked = owner.fTPMGR.ActiveClient.Config.DownloadDataType == FtpDataType.ASCII
			};

			var binaryRadio = new RadioButton
			{
				Text = "二进制模式（图像、压缩文件等）",
				Location = new Point(20, 50),
				Width = 250,
				Checked = owner.fTPMGR.ActiveClient.Config.DownloadDataType == FtpDataType.Binary
			};

			var okButton = new Button
			{
				Text = "确定",
				DialogResult = DialogResult.OK,
				Location = new Point(120, 80)
			};

			dialog.Controls.AddRange(new Control[] { asciiRadio, binaryRadio, okButton });
			dialog.AcceptButton = okButton;

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				// 设置传输模式
				owner.fTPMGR.ActiveClient.Config.DownloadDataType = asciiRadio.Checked ? FtpDataType.ASCII : FtpDataType.Binary;
				owner.fTPMGR.ActiveClient.Config.UploadDataType = asciiRadio.Checked ? FtpDataType.ASCII : FtpDataType.Binary;
				owner.fTPMGR.ActiveClient.Config.ListingDataType = asciiRadio.Checked ? FtpDataType.ASCII : FtpDataType.Binary;
				MessageBox.Show($"已切换到{(asciiRadio.Checked ? "ASCII" : "二进制")}传输模式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void do_cm_ftpresumedownload()
		{
			// 恢复下载
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 检查是否有中断的下载任务
			if (owner.fTPMGR.HasPendingDownloads())
			{
				// 恢复下载任务
				owner.fTPMGR.ResumeDownload();
			}
			else
			{
				MessageBox.Show("没有可恢复的下载任务", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void do_cm_ftpabort()
		{
			// 中断下载
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 检查是否有正在进行的下载任务
			if (owner.fTPMGR.IsDownloading())
			{
				// 中断下载任务
				owner.fTPMGR.AbortDownload();
				MessageBox.Show("已中断下载任务", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
			{
				MessageBox.Show("没有正在进行的下载任务", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void do_cm_ftphiddenfiles()
		{
			// 显示/隐藏隐藏文件
			if (owner.fTPMGR.ActiveClient == null || !owner.fTPMGR.ActiveClient.IsConnected)
			{
				MessageBox.Show("请先连接到FTP服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 切换显示隐藏文件的状态
			//bool currentState = owner.fTPMGR.ActiveClient.Config.ListHiddenFiles;
			//owner.fTPMGR.ActiveClient.Config.ListHiddenFiles = !currentState;
			
			owner.fTPMGR.ListOption ^= FtpListOption.AllFiles;

			// 刷新当前目录
			if (owner.fTPMGR.ftpRootNode.Nodes.Count > 0 && owner.fTPMGR.ftpRootNode.Nodes[0].Tag is FtpNodeTag tag)
			{
				owner.fTPMGR.LoadFtpDirectory(tag.ConnectionName, tag.Path, owner.activeListView);
				//MessageBox.Show($"已{(currentState ? "隐藏" : "显示")}隐藏文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
		private void do_cm_register()
		{
			var registerForm = new RegisterForm();
			registerForm.ShowDialog();
		}
		private void do_cm_dirtabsshowmenu()
		{
			owner.uiManager.BookmarkManager.OnRightClick();
		}
		private void do_cm_visdirtabs()
		{
			owner.uiManager.BookmarkManager.ToggleHidePanel(owner.uiManager.isleft);
		}
		private void do_cmgotoprevornextselected(bool isprevious = true)
		{
			//var selidxs = owner.activeListView.SelectedIndices;
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedIndices.Count == 0) return;
			
			// 获取当前选中项的索引
			var currentIndex = listView.SelectedIndices[targetIndex];
			int target;
			if (isprevious)
			{
				// 查找前一个选中项
				targetIndex -= 1;
				if (targetIndex < 0)
					targetIndex += listView.SelectedIndices.Count;
				target = listView.SelectedIndices[targetIndex];
			}
			else 
			{
				// 查找下一个选中项
				// 如果没有找到下一个选中项，则跳转到第一个选中项
				targetIndex += 1;
				if (targetIndex >= listView.SelectedIndices.Count)
					targetIndex -= listView.SelectedIndices.Count;
				target = listView.SelectedIndices[targetIndex];
			}
			// 如果找到了目标项，则将其设为焦点并确保可见
			listView.Items[currentIndex].Focused = false;
			listView.Items[target].Focused = true;
			listView.EnsureVisible(target);
		}
	
		private void do_cm_gotofirstfile()
		{
			var firstfile = owner.activeListView.Items.Cast<ListViewItem>().FirstOrDefault(item => !item.SubItems[3].Text.Equals("<DIR>"));
			//firstfile.Selected = true;
			var idx = owner.activeListView.Items.IndexOf(firstfile);
			owner.activeListView.EnsureVisible(idx);
		}
		private void do_cm_openrecycled()
		{
			owner.NavigateToPath("回收站", scope: Form1.TreeSearchScope.desktop);
		}
		private void do_cm_openfonts()
		{

		}

		private void do_cm_opencontrols()
		{
			owner.NavigateToPath("控制面板", scope: Form1.TreeSearchScope.desktop);
		}

		private void do_cm_opennetwork()
		{
			owner.NavigateToPath("网络", scope: Form1.TreeSearchScope.desktop);
		}
		private void do_cm_opendrives()
		{
			owner.NavigateToPath("此电脑", scope: Form1.TreeSearchScope.desktop);
		}
		private void do_cm_opendesktop()
		{
			owner.NavigateToPath("桌面", scope: Form1.TreeSearchScope.full);
		}
		private void do_cm_gotoroot()
		{
			var currentpath = owner.uiManager.srcDir;
			var parts = currentpath.Split('\\');
			owner.NavigateToPath(parts[0]);
		}
		private void do_cm_gotodrive(string drive)
		{
			owner.NavigateToPath(drive);
		}
		private void do_cm_gotodrivec()
		{
			do_cm_gotodrive("c:");
		}
		private void do_cm_gotodrived()
		{
			do_cm_gotodrive("d:");
		}
		private void do_cm_gotodrivee()
		{
			do_cm_gotodrive("e:");
		}
		private void do_cm_gotodrivef()
		{
			do_cm_gotodrive("f:");
		}
		private void do_cm_gotodriveg()
		{
			do_cm_gotodrive("g:");
		}
		private void do_cm_gotodriveh()
		{
			do_cm_gotodrive("h:");
		}
		private void do_cm_gotodrivez()
		{
			do_cm_gotodrive("z:");
		}
		private void do_cm_gotodrivea()
		{
			do_cm_gotodrive("a:");
		}
		private void do_cm_gotodriveb()
		{
			do_cm_gotodrive("b:");
		}
		// 添加一个 ListViewItemComparer 类来处理排序
		private class ListViewItemComparer : System.Collections.IComparer
		{
			private int _columnIndex;
			private bool _ascending;
			private readonly Func<ListViewItem, ListViewItem, int> _customComparer;

			public ListViewItemComparer(int columnIndex, bool ascending = true,
				Func<ListViewItem, ListViewItem, int> customComparer = null)
			{
				_columnIndex = columnIndex;
				_ascending = ascending;
				_customComparer = customComparer;
			}

			public void ReverseOrder()
			{
				_ascending = !_ascending;
			}

			public int Compare(object x, object y)
			{
				var item1 = (ListViewItem)x;
				var item2 = (ListViewItem)y;

				int result;
				if (_customComparer != null)
				{
					result = _customComparer(item1, item2);
				}
				else
				{
					result = string.Compare(
						item1.SubItems[_columnIndex].Text,
						item2.SubItems[_columnIndex].Text,
						StringComparison.OrdinalIgnoreCase
					);
				}

				return _ascending ? result : -result;
			}
		}
		// 按名称排序
		private void do_cm_srcbyname()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			listView.ListViewItemSorter = new ListViewItemComparer(0, true); // 0 表示第一列（文件名）
			listView.Sort();
		}

		// 按扩展名排序
		private void do_cm_srcbyext()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			listView.ListViewItemSorter = new ListViewItemComparer(
				3,		//the third col is extension
				true,
				(x, y) => string.Compare(
					Path.GetExtension(x.Text),
					Path.GetExtension(y.Text),
					StringComparison.OrdinalIgnoreCase
				)
			);
			listView.Sort();
		}

		// 按大小排序
		private void do_cm_srcbysize()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			// 大小信息在第5列
			listView.ListViewItemSorter = new ListViewItemComparer(5, true, (x, y) =>
			{
				if (long.TryParse(x.SubItems[1].Text, out long size1) &&
					long.TryParse(y.SubItems[1].Text, out long size2))
				{
					return size1.CompareTo(size2);
				}
				return string.Compare(x.SubItems[1].Text, y.SubItems[1].Text);
			});
			listView.Sort();
		}

		// 按日期时间排序
		private void do_cm_srcbydatetime()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			// 日期时间信息在第3列
			listView.ListViewItemSorter = new ListViewItemComparer(4, true, (x, y) =>
			{
				if (DateTime.TryParse(x.SubItems[2].Text, out DateTime date1) &&
					DateTime.TryParse(y.SubItems[2].Text, out DateTime date2))
				{
					return date1.CompareTo(date2);
				}
				return string.Compare(x.SubItems[2].Text, y.SubItems[2].Text);
			});
			listView.Sort();
		}

		// 取消排序
		private void do_cm_srcunsorted()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			listView.ListViewItemSorter = null;
			owner.RefreshPanel(); // 刷新面板以恢复默认顺序
		}

		// 反向排序
		private void do_cm_srcnegorder()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			// 如果当前有排序器，反转其排序方向
			if (listView.ListViewItemSorter is ListViewItemComparer comparer)
			{
				comparer.ReverseOrder();
				listView.Sort();
			}
		}
		private void do_cm_configchangeinifiles()
		{
			owner.do_cm_edit(Constants.ZfileCfgPath + "wincmd.ini");
		}
		private void do_cm_configsavesettings()
		{
			//save ftp config
			owner.fTPMGR.SaveToCfgloader();
		}
		private void do_cm_llm_helper(string param)
		{
			List<string> filePaths;
			if (param.Equals(string.Empty))
			{
				filePaths = new List<string>();
				foreach(var i in owner.activeListView.SelectedItems.Cast<ListViewItem>())
					filePaths.Add(i.SubItems[1].Text);	//对于展开所有子目录的文件，路径应该读取第2个subitem（存放真实路径)
			}
			else
				filePaths = owner.se.PrepareParameter(param, new string[] { }, "");

			// 使用异步方式处理，避免UI线程阻塞
			Task.Run(async () => {
				try
				{
					if (!owner.lLM_Helper.IsPrepared)
						await owner.lLM_Helper.Prepare().ConfigureAwait(false);
					var response = await owner.lLM_Helper.CallOllamaApiAsync("介绍一下你自己。").ConfigureAwait(false);
					// 使用Invoke确保在UI线程上显示消息框
					owner.Invoke(() => {
						Debug.Print($"{owner.lLM_Helper.currentModel}: {response}");
					});
					response = await ShowAIassistDialog(filePaths, "请描述以下内容：\r\n", false);
					Debug.Print(response);
				}
				catch (Exception ex)
				{
					owner.Invoke(() => {
						MessageBox.Show($"Ollama操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					});
				}
			});
		}
		private void do_cm_netConnect()		//调用操作系统命令来映射网上邻居的共享文件夹到虚拟盘符
		{

		}
		private void do_cm_netDisconnect()
		{

		}
		private void do_cm_matchsrc()
		{
			//Form1.TreeSearchScope scope;
			//ShellItem shitem;
			//string path = owner.uiManager.srcDir;
			//var tag = owner.activeTreeview.SelectedNode.Tag;
			var fullpath = owner.activeTreeview.SelectedNode.FullPath;
			//if (tag is ShellItem)
			//{
			//	shitem = (ShellItem)tag;
			//	if (shitem.IsVirtual)
			//	{
			//		if (shitem.parsepath.Equals("::{00021400-0000-0000-C000-000000000046}"))	// is desktop
			//			scope = Form1.TreeSearchScope.full;
			//		else					
			//			scope = Form1.TreeSearchScope.desktop;
			//		path = owner.activeTreeview.SelectedNode.Text;
			//	}
			//	else
			//		scope = Form1.TreeSearchScope.thispc;
			//}
			//else if (tag is FtpRootNodeTag)
			//{
			//	scope = Form1.TreeSearchScope.desktop;
			//	path = owner.activeTreeview.SelectedNode.Text;
			//}
			//else //is ftpnode
			//	scope = Form1.TreeSearchScope.ftproot;

			//owner.NavigateToPath(path, true, scope, false);
			var node = owner.FindTreeNodeByFullPath(owner.unactiveTreeview.Nodes, fullpath);
			owner.unactiveTreeview.SelectedNode = node;
			owner.RefreshPanel(owner.unactiveListView);
		}
		private void ShowFtpConnectionManager()
		{
			// 显示FTP连接管理器
			owner.fTPMGR.ShowFtpConnectionForm();
			
			// 初始化FTP管理器扩展
			//owner.fTPMGR.Initialize();
		}
		private void do_cm_ftpnew()
		{
			owner.fTPMGR.EditConnectionDialog();
		}
		private void do_cm_ftpdisconnect()
		{
			owner.fTPMGR.CloseConnection();
		}
		private void do_cm_crccheck(string param)
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("请选择要校验的文件", "提示");
				return;
			}

			foreach (ListViewItem item in listView.SelectedItems)
			{
				var filePath = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
				var extension = Path.GetExtension(filePath).ToLower();

				if (extension == ".sfv" || extension == ".md5" || extension == ".sha1" || extension == ".sha256" || extension == ".sha512")
				{
					ValidateChecksumFile(filePath, extension);
				}
				else
				{
					MessageBox.Show($"不支持的校验文件格式: {extension}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ValidateChecksumFile(string checksumFilePath, string extension)
		{
			var lines = File.ReadAllLines(checksumFilePath);
			var errors = new List<string>();

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
					continue;

				var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
					continue;

				var expectedChecksum = parts[0];
				var filePath = Path.Combine(Path.GetDirectoryName(checksumFilePath)!, parts[1].Replace("/", "\\"));

				if (!File.Exists(filePath))
				{
					errors.Add($"文件不存在: {filePath}");
					continue;
				}

				var actualChecksum = ComputeChecksum(filePath, extension);
				if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
				{
					errors.Add($"校验失败: {filePath} (预期: {expectedChecksum}, 实际: {actualChecksum})");
				}
			}

			if (errors.Count > 0)
			{
				MessageBox.Show(string.Join(Environment.NewLine, errors), "校验结果", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show("所有文件校验通过", "校验结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

	
		private void do_cm_crccreate(string param)
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("请选择要生成校验和的文件或文件夹", "提示");
				return;
			}

			var optionsForm = new Form
			{
				Text = "生成校验和文件",
				Size = new Size(400, 300),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var singleFileCheckBox = new CheckBox
			{
				Text = "为每个文件创建单独的校验和文件 (S)",
				Location = new Point(10, 10),
				AutoSize = true
			};

			var singleFolderCheckBox = new CheckBox
			{
				Text = "为每个文件夹单独创建校验和文件",
				Location = new Point(10, 40),
				AutoSize = true
			};

			var utf8CheckBox = new CheckBox
			{
				Text = "为校验文件使用UTF-8编码 (A)",
				Location = new Point(10, 70),
				AutoSize = true
			};

			var unixFormatCheckBox = new CheckBox
			{
				Text = "使用Unix格式的换行符和路径分隔符 (U)",
				Location = new Point(10, 100),
				AutoSize = true
			};

			var hashAlgorithmLabel = new Label
			{
				Text = "选择校验算法：",
				Location = new Point(10, 130),
				AutoSize = true
			};

			var hashAlgorithmComboBox = new ComboBox
			{
				Location = new Point(10, 160),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			hashAlgorithmComboBox.Items.AddRange(new string[]
			{
				"CRC32 (SFV)", "SHA224", "SHA3_224", "MD5", "SHA256", "SHA3_256",
				"SHA1", "SHA384", "SHA3_384", "BLAKE3", "SHA512", "SHA3_512"
			});
			hashAlgorithmComboBox.SelectedIndex = 0;

			var generateButton = new Button
			{
				Text = "生成",
				Location = new Point(150, 200),
				DialogResult = DialogResult.OK
			};

			optionsForm.Controls.AddRange(new Control[]
			{
				singleFileCheckBox, singleFolderCheckBox, utf8CheckBox, unixFormatCheckBox,
				hashAlgorithmLabel, hashAlgorithmComboBox, generateButton
			});
			optionsForm.AcceptButton = generateButton;

			if (optionsForm.ShowDialog() == DialogResult.OK)
			{
				var selectedAlgorithm = hashAlgorithmComboBox.SelectedItem.ToString();
				var useUtf8 = utf8CheckBox.Checked;
				var useUnixFormat = unixFormatCheckBox.Checked;
				var createSingleFile = singleFileCheckBox.Checked;
				var createSingleFolder = singleFolderCheckBox.Checked;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var path = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
					if (Directory.Exists(path) && createSingleFolder)
					{
						GenerateChecksumForDirectory(path, selectedAlgorithm, useUtf8, useUnixFormat);
					}
					else if (File.Exists(path) && createSingleFile)
					{
						GenerateChecksumForFile(path, selectedAlgorithm, useUtf8, useUnixFormat);
					}
				}

				owner.RefreshPanel();
			}
		}

		private void GenerateChecksumForFile(string filePath, string algorithm, bool useUtf8, bool useUnixFormat)
		{
			var checksum = ComputeChecksum(filePath, algorithm);
			var checksumFileName = $"{filePath}.{algorithm.ToLower()}";
			var encoding = useUtf8 ? Encoding.UTF8 : Encoding.Default;
			var lineEnding = useUnixFormat ? "\n" : "\r\n";
			var pathSeparator = useUnixFormat ? "/" : "\\";

			var checksumContent = $"{checksum} {filePath.Replace("\\", pathSeparator)}{lineEnding}";
			File.WriteAllText(checksumFileName, checksumContent, encoding);
		}

		private void GenerateChecksumForDirectory(string directoryPath, string algorithm, bool useUtf8, bool useUnixFormat)
		{
			var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
			var encoding = useUtf8 ? Encoding.UTF8 : Encoding.Default;
			var lineEnding = useUnixFormat ? "\n" : "\r\n";
			var pathSeparator = useUnixFormat ? "/" : "\\";

			var checksumFileName = $"{directoryPath}.{algorithm.ToLower()}";
			using var writer = new StreamWriter(checksumFileName, false, encoding);

			foreach (var file in files)
			{
				var checksum = ComputeChecksum(file, algorithm);
				var relativePath = file.Substring(directoryPath.Length + 1).Replace("\\", pathSeparator);
				writer.WriteLine($"{checksum} {relativePath}");
			}
		}
	
		private string ComputeChecksum(string filePath, string algorithm)
		{
			using var stream = File.OpenRead(filePath);
			HashAlgorithm hashAlgorithm = algorithm switch
			{
				//"CRC32 (SFV)" => new Crc32(),
				//"SHA224" => SHA224.Create(),
				//"SHA3_224" => SHA3_224.Create(),
				"MD5" => MD5.Create(),
				"SHA256" => SHA256.Create(),
				"SHA3_256" => SHA3_256.Create(),
				"SHA1" => SHA1.Create(),
				"SHA384" => SHA384.Create(),
				"SHA3_384" => SHA3_384.Create(),
				//"BLAKE3" => Blake3.Create(),
				"SHA512" => SHA512.Create(),
				"SHA3_512" => SHA3_512.Create(),
				_ => throw new InvalidOperationException("不支持的校验算法")
			};

			var hash = hashAlgorithm.ComputeHash(stream);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}
		private void do_cm_combine(string param = "")
		{
			List<string> filesToCombine;
			if (string.IsNullOrEmpty(param))
			{
				// 如果没有参数，使用当前选中的第一个文件
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请选择要合并的文件", "提示");
					return;
				}

				// 获取选中的第一个文件
				var firstFile = listView.SelectedItems[0].Text;
				var directory = owner.currentDirectory[owner.isleft];
				var fileNameWithoutExt = Path.GetFileNameWithoutExtension(firstFile);

				// 如果文件名包含.part，则去掉.part及后面的数字
				if (fileNameWithoutExt.Contains(".part"))
				{
					fileNameWithoutExt = fileNameWithoutExt.Substring(0, fileNameWithoutExt.LastIndexOf(".part"));
				}

				// 获取所有匹配的文件
				filesToCombine = Directory.GetFiles(directory, $"{fileNameWithoutExt}.part*")
					.OrderBy(f => f)
					.ToList();

				if (filesToCombine.Count == 0)
				{
					MessageBox.Show("未找到可合并的文件", "提示");
					return;
				}
			}
			else
			{
				// 使用参数解析文件列表
				filesToCombine = owner.se.PrepareParameter(param, null, "")
					.OrderBy(f => f)
					.ToList();
			}

			try
			{
				// 获取第一个文件的基本信息
				var firstFile = filesToCombine[0];
				var directory = Path.GetDirectoryName(firstFile);
				var extension = Path.GetExtension(firstFile);
				var fileNameWithoutPartNum = Path.GetFileNameWithoutExtension(firstFile);

				// 如果文件名包含.part，则去掉.part及后面的数字
				if (fileNameWithoutPartNum.Contains(".part"))
				{
					fileNameWithoutPartNum = fileNameWithoutPartNum.Substring(0, fileNameWithoutPartNum.LastIndexOf(".part"));
				}

				// 构造目标文件路径
				var targetFile = Path.Combine(directory!, $"{fileNameWithoutPartNum}{extension}");

				// 确认是否覆盖已存在的文件
				if (File.Exists(targetFile))
				{
					var result = MessageBox.Show(
						$"文件 {Path.GetFileName(targetFile)} 已存在，是否覆盖？",
						"确认覆盖",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question
					);

					if (result != DialogResult.Yes)
						return;
				}

				// 合并文件
				using (var outputStream = new FileStream(targetFile, FileMode.Create))
				{
					var buffer = new byte[8192]; // 8KB buffer
					foreach (var file in filesToCombine)
					{
						using var inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);
						int bytesRead;
						while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
						{
							outputStream.Write(buffer, 0, bytesRead);
						}
					}
				}

				// 询问是否删除源文件
				var deleteResult = MessageBox.Show(
					"文件合并完成。是否删除源文件？",
					"合并完成",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);

				if (deleteResult == DialogResult.Yes)
				{
					foreach (var file in filesToCombine)
					{
						File.Delete(file);
					}
				}

				// 刷新当前面板
				owner.RefreshPanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"合并文件时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void do_cm_split(string param = "")
		{
			List<string> filesToSplit;
			if (string.IsNullOrEmpty(param))
			{
				// 如果没有参数，使用当前选中的文件
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请选择要拆分的文件", "提示");
					return;
				}
				filesToSplit = listView.SelectedItems.Cast<ListViewItem>()
					.Select(item => Path.Combine(owner.currentDirectory[owner.isleft], item.Text))
					.ToList();
			}
			else
			{
				// 使用参数解析文件列表
				filesToSplit = owner.se.PrepareParameter(param, null, "");
			}

			// 创建拆分对话框
			var splitForm = new Form
			{
				Text = "文件拆分",
				Size = new Size(400, 150),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var sizeLabel = new Label
			{
				Text = "选择拆分大小：",
				Location = new Point(10, 20),
				AutoSize = true
			};

			var sizeCombo = new ComboBox
			{
				Location = new Point(120, 17),
				Width = 150,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			//var sizeInput = new TextBox
			//{
			//	Location = new Point(120, 37),
			//	Width = 150
			//};

			var m = 1024 * 1024;
			var g = 1024 * m;
			// 添加常用大小选项
			var sizeOptions = new Dictionary<string, long>
			{
				{"650 MB (CD)", 650L * m},
				{"700 MB (CD)", 700L * m},
				{"4.7 GB (DVD)", (long)(4.7 * g)},
				{"8.5 GB (DVD)", (long)(8.5 * g)},
				{"自定义...", 0}
			};

			sizeCombo.Items.AddRange(sizeOptions.Keys.ToArray());
			sizeCombo.SelectedIndex = 0;

			var splitButton = new Button
			{
				Text = "开始拆分",
				Location = new Point(150, 70),
				DialogResult = DialogResult.OK
			};

			splitForm.Controls.AddRange(new Control[] { sizeLabel, sizeCombo,  splitButton });
			splitForm.AcceptButton = splitButton;

			if (splitForm.ShowDialog() == DialogResult.OK)
			{
				var selectedSize = sizeOptions[sizeCombo.SelectedItem.ToString()!];

				// 如果选择自定义大小，弹出输入框
				if (selectedSize == 0)
				{
					var input = Microsoft.VisualBasic.Interaction.InputBox(
						"请输入拆分大小(MB)：",
						"自定义大小",
						"100");

					if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long customSize))
						return;

					selectedSize = customSize * 1024 * 1024; // 转换为字节
				}

				// 处理每个选中的文件
				foreach (var filePath in filesToSplit)
				{
					try
					{
						SplitFile(filePath, selectedSize);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"拆分文件 {Path.GetFileName(filePath)} 时出错：{ex.Message}", "错误");
					}
				}

				// 刷新当前面板
				owner.RefreshPanel();
			}
		}

		private void SplitFile(string filePath, long splitSize)
		{
			using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			var fileName = Path.GetFileNameWithoutExtension(filePath);
			var extension = Path.GetExtension(filePath);
			var directory = Path.GetDirectoryName(filePath);

			var buffer = new byte[8192]; // 8KB buffer
			var partNumber = 1;
			long bytesRead;
			long currentPartSize = 0;

			FileStream? outputStream = null;

			try
			{
				while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					if (outputStream == null || currentPartSize >= splitSize)
					{
						outputStream?.Dispose();
						var partPath = Path.Combine(directory!,
							$"{fileName}.part{partNumber:D3}{extension}");
						outputStream = new FileStream(partPath, FileMode.Create);
						currentPartSize = 0;
						partNumber++;
					}

					outputStream.Write(buffer, 0, (int)bytesRead);
					currentPartSize += bytesRead;
				}
			}
			finally
			{
				outputStream?.Dispose();
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
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
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
				var targetPath = owner.currentDirectory[owner.isleft];
				foreach(var file in files)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], file);
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
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
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
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				var files = owner.se.PrepareParameter(param, null, "");
				foreach (var file in files) {
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], file);
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
			string currentDrive = Path.GetPathRoot(owner.currentDirectory[owner.isleft]);
			if (string.IsNullOrEmpty(currentDrive))
				currentDrive = "C:\\";

			// 创建并显示目录树查找窗口
			var treeSearchForm = new DirectoryTreeSearchForm(owner, currentDrive);
			treeSearchForm.ShowDialog();
		}

		private void ShowCommandBrowser()
		{
			// 显示窗体
			var form = new CommandBrowserForm(owner.cmdProcessor);
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
					.Select(i => Path.Combine(owner.currentDirectory[owner.isleft], i.Text)));
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
				details.AppendLine(Path.Combine(owner.currentDirectory[owner.isleft], item.Text) + "\t" +
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
				owner.forwardStack.Push(owner.currentDirectory[owner.isleft]);
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
				owner.backStack.Push(owner.currentDirectory[owner.isleft]);
				// 从前进栈获取下一个目录
				string nextPath = owner.forwardStack.Pop();
				// 导航到该目录，但不记录到历史（避免重复记录）
				owner.NavigateToPath(nextPath, false);
			}
		}

		private void do_cm_gotoparent()
		{
			string? parentPath = Path.GetDirectoryName(owner.currentDirectory[owner.isleft]);
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
					if (owner.IsArchiveFile(owner.currentDirectory[owner.isleft]))
					{
						if (owner.DeleteFromArchive(owner.currentDirectory[owner.isleft], files.ToArray()))
						{
							var items = owner.LoadArchiveContents(owner.currentDirectory[owner.isleft]);
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
			var path = owner.currentDirectory[owner.isleft];
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
				var searchPath = owner.currentDirectory[owner.isleft];

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
			var filePath = Path.Combine(owner.currentDirectory[owner.isleft], selectedItem.Text);

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

			var file1 = Path.Combine(owner.currentDirectory[owner.isleft], listView.SelectedItems[0].Text);
			var file2 = Path.Combine(owner.currentDirectory[owner.isleft], listView.SelectedItems[1].Text);

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
			var targetfile = Path.Combine(owner.currentDirectory[owner.isleft], listView.SelectedItems[0].Text) + ".zip";
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
						owner.currentDirectory[owner.isleft],
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
			var zipPath = Path.Combine(owner.currentDirectory[owner.isleft], selectedItem.Text);

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
			using var dialog = new MultiRenameForm(listView, owner.currentDirectory[owner.isleft]);
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
		private async Task<string> ShowAIassistDialog(List<string> filePaths, string prompt, bool isBackground = true)
		{
			string response = string.Empty;
			if (!isBackground)
			{
				var aiDlg = new AIassistDlg(filePaths, owner.lLM_Helper, owner.cmdProcessor);
				owner.Invoke(() => { aiDlg.Show(); });
			}
			else
			{
				foreach (var file in filePaths)
				{
					//read all text from file if it is a text file
					var result = Helper.Getfiletype(file);
					Debug.Print($"{file} => {result}");

					if (!result.Contains("text", StringComparison.OrdinalIgnoreCase)) continue;

					var content = File.ReadAllText(file);
					response = await owner.lLM_Helper.CallOllamaApiAsync(prompt + content);
				}
			}
			return response;
		}
	}
}

