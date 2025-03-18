using MCPSharp;
using MCPSharp.Model;
using MCPSharp.Model.Schemas;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Net;
using System.Drawing;
using System.IO;
using System.Security.Policy;

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
						if(!owner.uiManager.isThumbs)
							owner.SetViewMode(View.Tile);
						else
							owner.SetViewMode(View.Details);
						owner.uiManager.isThumbs = !owner.uiManager.isThumbs;
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
					case 533: // 命令ID=533,Name=cm_comparedirs
						do_cm_comparedirs();
						break;
					case 534: // 命令ID=534，Name=cm_dirmatch
						do_cm_dirmatch();
						break;
					case 536: // cm_CompareDirsWithSubdirs
						do_cm_CompareDirsWithSubdirs(param);
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
					case 4001: // 命令ID = 4001, Name = cm_focusleft
						owner.uiManager.LeftList.Focus();
						break;
					case 4002: // cm_focusright
						owner.uiManager.RightList.Focus();
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
						var paramcount = param.Length;
						string url = "http://v.juhe.cn/toutiao/index", key = "de73e15a67f8b359d4ec409ae3e63aed", par = "type=keji";
						
						if(paramcount> 0) url = parameters[0];
						if(paramcount> 1) key = parameters[1];
						if(paramcount> 2) par = parameters[2];
						do_cm_apicaller(url, key, par);   
						break;

					case 11438: // mcp client
						do_cm_mcpclient(param);
						break;
					case 11439: // mcp client with mcpsharp
						var lst = Task.Run(async () => { await do_cm_mcpclient1(param); });
						break;
					case 11440: // launch mcp server
						Task.Run(async () => { await do_cm_mcpserver(param); } ); // param is servername
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
		private void do_cm_comparedirs()
		{
			do_cm_dirmatch(false);
		}
		private void do_cm_dirmatch(bool hideIdentical = true)
		{
			// 获取左右面板的ListView
			ListView leftList = owner.uiManager.LeftList;
			ListView rightList = owner.uiManager.RightList;

			// 创建文件名集合
			HashSet<string> leftFiles = new HashSet<string>();
			HashSet<string> rightFiles = new HashSet<string>();

			// 收集左面板文件名
			foreach (ListViewItem item in leftList.Items)
			{
				if (!item.SubItems[3].Text.Equals("<DIR>"))
					leftFiles.Add(item.Text);
			}

			// 收集右面板文件名
			foreach (ListViewItem item in rightList.Items)
			{
				if (!item.SubItems[3].Text.Equals("<DIR>"))
					rightFiles.Add(item.Text);
			}

			// 遍历左面板文件，标记差异
			foreach (ListViewItem item in leftList.Items)
			{
				if (item.SubItems[3].Text.Equals("<DIR>")) continue;
				if (!rightFiles.Contains(item.Text))
				{
					// 文件只存在于左面板，高亮显示
					item.BackColor = Color.LightPink;
					item.Selected = true;
				}
				else if (hideIdentical)
				{
					// 如果需要隐藏相同文件
					item.ForeColor = Color.LightGray;
				}
				else
				{
					// 重置颜色
					item.BackColor = SystemColors.Window;
					item.ForeColor = SystemColors.WindowText;
				}
			}

			// 遍历右面板文件，标记差异
			foreach (ListViewItem item in rightList.Items)
			{
				if (item.SubItems[3].Text.Equals("<DIR>")) continue;
				if (!leftFiles.Contains(item.Text))
				{
					// 文件只存在于右面板，高亮显示
					item.BackColor = Color.LightPink;
					item.Selected = true;
				}
				else if (hideIdentical)
				{
					// 如果需要隐藏相同文件
					item.ForeColor = Color.LightGray;
				}
				else
				{
					// 重置颜色
					item.BackColor = SystemColors.Window;
					item.ForeColor = SystemColors.WindowText;
				}
			}
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
		public string cm_apicaller(string url, string apiKey, string param)
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
					var responseContent = client.DownloadString(fullUrl);
					//dynamic responseData = JsonConvert.DeserializeObject(responseContent);

					if (responseContent != null)
					{
						//Debug.Print("Return Code: " + responseData["error_code"]);
						//Debug.Print("Return Message: " + responseData["reason"]);//-> success
						return responseContent;
					}
					else
						Debug.Print("json解析异常！");
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
		private void do_cm_CompareDirsWithSubdirs(string param)
		{
			/*
			 * 根据传入的参数param确定比较模式：
				0: 选择在另一侧不存在、或更新、或时间戳相同但大小不同的文件
				1: 仅选择在另一侧不存在的文件
				2: 选择大小不同的文件以及在另一侧不存在的文件
				主要实现步骤：
				获取左右面板的ListView和对应的目录路径
				创建两个字典来存储文件信息（包括完整路径、大小和修改时间）
				使用GetAllFiles辅助方法递归获取两个目录下所有文件的信息
				清除现有的文件选择
				分别遍历左右面板的文件列表进行比较
				文件比较逻辑：
				对于每个文件，首先检查是否在另一侧存在
				如果不存在，将其标记为粉色背景并选中
				如果存在，根据不同的比较模式进行比较：
				模式0：检查修改时间和文件大小
				模式1：仅检查文件是否存在
				模式2：检查文件大小
				符合条件的文件会被选中并标记为黄色背景
				视觉效果：
				不存在的文件：粉色背景
				有差异的文件：黄色背景
				正常文件：保持原样
				辅助方法GetAllFiles：
				递归遍历目录及其子目录
				收集所有文件的信息（路径、大小、修改时间）
				使用相对路径作为字典的键，以便于比较
			 */
			// 获取左右面板的ListView
			ListView leftList = owner.uiManager.LeftList;
			ListView rightList = owner.uiManager.RightList;

			// 获取左右面板的路径
			string leftPath = owner.uiManager.leftDir;
			string rightPath = owner.uiManager.rightDir;

			// 解析参数
			int compareMode = 0;
			if (!string.IsNullOrEmpty(param))
			{
				int.TryParse(param, out compareMode);
			}

			// 创建文件信息字典，键为相对路径
			Dictionary<string, (string FullPath, long Size, DateTime ModTime)> leftFiles = new();
			Dictionary<string, (string FullPath, long Size, DateTime ModTime)> rightFiles = new();

			// 递归获取所有文件信息
			GetAllFiles(leftPath, leftPath, leftFiles);
			GetAllFiles(rightPath, rightPath, rightFiles);

			// 清除所有现有选择
			leftList.SelectedItems.Clear();
			rightList.SelectedItems.Clear();

			// 比较文件并选择符合条件的文件
			foreach (ListViewItem leftItem in leftList.Items)
			{
				if (leftItem.SubItems[3].Text.Equals("<DIR>")) continue;
				string fileName = leftItem.Text;
				string leftFullPath = Path.Combine(leftPath, fileName);
				FileInfo leftFileInfo = new FileInfo(leftFullPath);

				if (rightFiles.ContainsKey(fileName))
				{
					var rightInfo = rightFiles[fileName];
					bool select = false;

					switch (compareMode)
					{
						case 0: // 不存在或更新或大小不同
							if (leftFileInfo.LastWriteTime != rightInfo.ModTime)
							{
								if (leftFileInfo.LastWriteTime > rightInfo.ModTime)
									select = true;
							}
							else if (leftFileInfo.Length != rightInfo.Size)
							{
								select = true;
							}
							break;
						case 1: // 仅选择不存在的文件
							select = false;
							break;
						case 2: // 选择大小不同的文件
							if (leftFileInfo.Length != rightInfo.Size)
							{
								select = true;
							}
							break;
					}

					if (select)
					{
						leftItem.Selected = true;
						leftItem.BackColor = Color.LightYellow;
					}
				}
				else // 文件在右边不存在
				{
					leftItem.Selected = true;
					leftItem.BackColor = Color.LightPink;
				}
			}

			foreach (ListViewItem rightItem in rightList.Items)
			{
				if (rightItem.SubItems[3].Text.Equals("<DIR>")) continue;
				string fileName = rightItem.Text;
				string rightFullPath = Path.Combine(rightPath, fileName);
				FileInfo rightFileInfo = new FileInfo(rightFullPath);

				if (leftFiles.ContainsKey(fileName))
				{
					var leftInfo = leftFiles[fileName];
					bool select = false;

					switch (compareMode)
					{
						case 0: // 不存在或更新或大小不同
							if (rightFileInfo.LastWriteTime != leftInfo.ModTime)
							{
								if (rightFileInfo.LastWriteTime > leftInfo.ModTime)
									select = true;
							}
							else if (rightFileInfo.Length != leftInfo.Size)
							{
								select = true;
							}
							break;
						case 1: // 仅选择不存在的文件
							select = false;
							break;
						case 2: // 选择大小不同的文件
							if (rightFileInfo.Length != leftInfo.Size)
							{
								select = true;
							}
							break;
					}

					if (select)
					{
						rightItem.Selected = true;
						rightItem.BackColor = Color.LightYellow;
					}
				}
				else // 文件在左边不存在
				{
					rightItem.Selected = true;
					rightItem.BackColor = Color.LightPink;
				}
			}
		}

		private void GetAllFiles(string basePath, string currentPath, Dictionary<string, (string FullPath, long Size, DateTime ModTime)> files)
		{
			try
			{
				// 获取当前目录下的所有文件
				foreach (string file in Directory.GetFiles(currentPath))
				{
					FileInfo fileInfo = new FileInfo(file);
					string relativePath = Path.GetRelativePath(basePath, file);
					files[relativePath] = (file, fileInfo.Length, fileInfo.LastWriteTime);
				}

				// 递归处理子目录
				foreach (string dir in Directory.GetDirectories(currentPath))
				{
					GetAllFiles(basePath, dir, files);
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"获取文件列表时出错: {ex.Message}");
			}
		}
	}
}

