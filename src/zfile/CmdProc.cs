using MCPSharp;
using MCPSharp.Model;
using MCPSharp.Model.Schemas;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Net;

namespace zfile
{

	public partial class CmdProc
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
					case 498: // 命令ID = 498, Name = cm_buttonconfig
						owner.uiManager.toolbarManager.EditToolbar();
						break;
					case 583: // cm_buttonconfig2
						owner.uiManager.vtoolbarManager.EditToolbar();
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
					case 700: // cm_ChangeStartMenu
						owner.uiManager.EditMenu(0);//set to 1 to change mainmenu
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
	
	}
}

