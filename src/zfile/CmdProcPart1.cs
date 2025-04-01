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
using System.Windows.Forms;


namespace Zfile
{
	public partial class CmdProc
	{
		private void cm_listExternal(string param)
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
		private void cm_dirtabsshowmenu()
		{
			owner.uiManager.BookmarkManager.OnRightClick();
		}
		private void cm_visdirtabs()
		{
			owner.uiManager.BookmarkManager.ToggleHidePanel(owner.uiManager.isleft);
		}
		private void cm_gotoprevornextselected(bool isprevious = true)
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

		private void cm_gotofirstfile()
		{
			var firstfile = owner.activeListView.Items.Cast<ListViewItem>().FirstOrDefault(item => !item.SubItems[3].Text.Equals("<DIR>"));
			//firstfile.Selected = true;
			var idx = owner.activeListView.Items.IndexOf(firstfile);
			owner.activeListView.EnsureVisible(idx);
		}
		private void cm_openrecycled()
		{
			owner.NavigateToPath("回收站", scope: MainForm.TreeSearchScope.desktop);
		}
		private void cm_openfonts()
		{

		}

		private void cm_opencontrols()
		{
			owner.NavigateToPath("控制面板", scope: MainForm.TreeSearchScope.desktop);
		}

		private void cm_opennetwork()
		{
			owner.NavigateToPath("网络", scope: MainForm.TreeSearchScope.desktop);
		}
		private void cm_opendrives()
		{
			owner.NavigateToPath("此电脑", scope: MainForm.TreeSearchScope.desktop);
		}
		private void cm_opendesktop()
		{
			owner.NavigateToPath("桌面", scope: MainForm.TreeSearchScope.full);
		}
		public void cm_gotoroot()
		{
			//if (owner.IsActiveFtpPanel(out var ftpnode)) {
			//	owner.fTPMGR.NavigateToPath(ftpnode.ConnectionName, "/", owner.activeListView);
			//}
			//else
			//{
			//	var currentpath = owner.uiManager.srcDir;
			//	var parts = currentpath.Split('\\');
			//	owner.NavigateToPath(parts[0]);
			//}
			if (owner.IsActiveFtpPanel(out var ftpnode))
				owner.fTPMGR.NavigateToPath(ftpnode.ConnectionName, "/", owner.activeListView);
			else
				owner.NavigateToPath(Helper.GetPathByEnv(Path.GetPathRoot(owner.uiManager.srcDir)));
		}
		private void do_cm_gotodrive(string drive)
		{
			owner.NavigateToPath(drive);
		}
		private void cm_gotodrivec()
		{
			do_cm_gotodrive("c:");
		}
		private void cm_gotodrived()
		{
			do_cm_gotodrive("d:");
		}
		private void cm_gotodrivee()
		{
			do_cm_gotodrive("e:");
		}
		private void cm_gotodrivef()
		{
			do_cm_gotodrive("f:");
		}
		private void cm_gotodriveg()
		{
			do_cm_gotodrive("g:");
		}
		private void cm_gotodriveh()
		{
			do_cm_gotodrive("h:");
		}
		private void cm_gotodrivez()
		{
			do_cm_gotodrive("z:");
		}
		private void cm_gotodrivea()
		{
			do_cm_gotodrive("a:");
		}
		private void cm_gotodriveb()
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
				3,      //the third col is extension
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
			owner.cm_edit(Constants.ZfileCfgPath + "wincmd.ini");
		}
		private void do_cm_configsavesettings()
		{
			//save ftp config
			owner.fTPMGR.SaveToCfgloader();
		}
		private void cm_llm_helper(string param)
		{
			List<string> filePaths;
			if (param.Equals(string.Empty))
			{
				filePaths = new List<string>();
				foreach (var i in owner.activeListView.SelectedItems.Cast<ListViewItem>())
					filePaths.Add(i.SubItems[1].Text);  //对于展开所有子目录的文件，路径应该读取第2个subitem（存放真实路径)
			}
			else
				filePaths = owner.se.PrepareParameter(param, new string[] { }, "");

			// 使用异步方式处理，避免UI线程阻塞
			_ = Task.Run(async () =>
			{
				try
				{
					if (!owner.lLM_Helper.IsPrepared)
						await owner.lLM_Helper.Prepare().ConfigureAwait(false);
					//var response = await owner.lLM_Helper.CallOllamaApiAsync("介绍一下你自己。").ConfigureAwait(false);
					//// 使用Invoke确保在UI线程上显示消息框
					//owner.Invoke(() =>
					//{
					//	Debug.Print($"{owner.lLM_Helper.currentModel}: {response}");
					//});
					var response = await ShowAIassistDialog(filePaths, "开始执行：\r\n", false);
					Debug.Print(response);
				}
				catch (Exception ex)
				{
					owner.Invoke(() =>
					{
						MessageBox.Show($"Ollama操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					});
				}
			});
		}
		private void do_cm_netConnect()     //调用操作系统命令来映射网上邻居的共享文件夹到虚拟盘符
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
				var filePath = Path.Combine(owner.CurrentDir[owner.LRflag], item.Text);
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
					var path = Path.Combine(owner.CurrentDir[owner.LRflag], item.Text);
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
				var directory = owner.CurrentDir[owner.LRflag];
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
					.Select(item => Path.Combine(owner.CurrentDir[owner.LRflag], item.Text))
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

			splitForm.Controls.AddRange(new Control[] { sizeLabel, sizeCombo, splitButton });
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
						//TODO: IF FTP REMOTE FILE, DOWNLOAD IT FIRST, THEN CALL SPLITFILE
						if (filePath.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase))
						{
							var file = Path.GetFileName(filePath);
							var localfile = $"{owner.uiManager.targetDir}{file}";
							var ftpsrc = owner.fTPMGR.GetFtpSource(filePath);
							owner.fTPMGR.ActiveClient.DownloadFile(localfile, filePath);
						}
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
	}
}
