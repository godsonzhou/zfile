using Sheng.Winform.Controls;
using System.Diagnostics;
using System.Text;
//using System.Windows.Controls;
using WinShell;

namespace zfile
{
	public class UIControlManager : IDisposable
	{
		private readonly Form1 form;
		private readonly ImageList listViewImageListL;
		private readonly ImageList thumbnailImageListL;
		private readonly ImageList listViewImageListR;
		private readonly ImageList thumbnailImageListR;

		#region Container Controls
		public SplitContainer MainContainer { get; } = new();
		public SplitContainer LeftPanel { get; } = new();
		public SplitContainer RightPanel { get; } = new();
		public SplitContainer LeftTreeListSplitter { get; } = new();
		public SplitContainer RightTreeListSplitter { get; } = new();
		#endregion

		#region Panel Controls
		public Panel LeftUpperPanel { get; } = new();
		public Panel RightUpperPanel { get; } = new();
		public Panel LeftDrivePanel { get; } = new();
		public Panel RightDrivePanel { get; } = new();
		#endregion

		#region Drive Controls
		public ComboBox LeftDriveComboBox { get; } = new() { Name = "LeftDriveCombo" };
		public ComboBox RightDriveComboBox { get; } = new() { Name = "RightDriveCombo" };
		public ShengAddressBarStrip LeftPathTextBox { get; } = new() { Name = "LeftPath" };
		public ShengAddressBarStrip RightPathTextBox { get; } = new() { Name = "RightPath" };
		public ShengAddressBarStrip ActivePathTextBox { get => (isleft ? LeftPathTextBox : RightPathTextBox); }
		#endregion
		// 添加导航按钮控件
		public ToolStrip LeftNavigationStrip { get; } = new() { Name = "LeftNav", Width = 100 };
		public ToolStrip RightNavigationStrip { get; } = new() { Name = "RightNav", Width = 100 };

		#region View Controls
		public TreeView LeftTree { get; } = new() { Name = "LeftTree" };
		public TreeView RightTree { get; } = new() { Name = "RightTree" };
		public ListView LeftList { get; } = new() { Name = "LeftList" };
		public ListView RightList { get; } = new() { Name = "RightList" };
		#endregion

		#region Preview Controls
		public TextBox LeftPreview { get; } = new() { Name = "LeftPreview" };
		public TextBox RightPreview { get; } = new() { Name = "RightPreview" };
		#endregion

		#region Status Controls
		public StatusStrip LeftStatusStrip { get; } = new() { Name = "LeftStatus" };
		public StatusStrip RightStatusStrip { get; } = new() { Name = "RightStatus" };
		#endregion

		#region Bookmark Controls
		public readonly FlowLayoutPanel leftBookmarkPanel = new() { Name = "LeftBookmark" };
		public readonly FlowLayoutPanel rightBookmarkPanel = new() { Name = "RightBookmark" };
		public BookmarkManager BookmarkManager { get; private set; }
		#endregion

		#region Menu Controls
		public MenuStrip dynamicMenuStrip = new();
		#endregion
		public ListView activeListView { get => (isleft ? LeftList : RightList); }
		public ListView unactiveListView { get => (!isleft ? LeftList : RightList); }
		public TreeView activeTreeview { get => (isleft ? LeftTree : RightTree); }
		public TreeView unactiveTreeview { get => (!isleft ? LeftTree : RightTree); }

		public ToolbarManager toolbarManager;
		public ToolbarManager vtoolbarManager;
		public FtpController ftpController;
		public ToolStrip toolStrip;
		private bool isToolStripHidden;

		public bool isleft { get; set; } = true;
		public string leftDir => LeftPathTextBox?.CurrentNode?.UniqueID + "\\";
		public string rightDir => RightPathTextBox?.CurrentNode?.UniqueID + "\\";
		public string leftfiles => string.Join("|", LeftList.SelectedItems.Cast<ListViewItem>()?.Select(item => item.SubItems[0].Text));
		public string rightfiles => string.Join("|", RightList.SelectedItems.Cast<ListViewItem>()?.Select(item => item.SubItems[0].Text));
		public string targetDir => isleft ? rightDir : leftDir;
		public string srcDir => isleft ? leftDir : rightDir;
		public string targetfiles => isleft ? rightfiles : leftfiles;
		public string srcfiles => isleft ? leftfiles : rightfiles;

		public Dictionary<string, string> args = new();
		public Dictionary<string, string> lastVisitedPaths = new();
		public Dictionary<string, MenuInfo> usermenuMap = new();
		public bool isThumbs;
		private bool disposed = false;

		public UIControlManager(Form1 form)
		{
			this.form = form;
			listViewImageListL = new ImageList();
			listViewImageListR = new ImageList();
			thumbnailImageListL = new ImageList();
			thumbnailImageListR = new ImageList();
			BookmarkManager = new BookmarkManager(form, leftBookmarkPanel, rightBookmarkPanel);

			LeftPathTextBox.SelectionChange += LeftPathTextBox_PathChanged;
			RightPathTextBox.SelectionChange += RightPathTextBox_PathChanged;

			setArgs();
		}
		/*
		注意： 所有参数现在都支持下面表单中的子字段：~开始位置，长度。例如：％N:~2,5 或 ％N:~-8,5。要在长度值之后直接追加数字，请使用另一个 "~" 字符，例如：％N:~2,5~2。负值从字符串的末端开始计算。示例：％P:~0,-1 表示从路径中去除反斜杠。
		开始位置数值 -0 具有特殊意义：%N:~-0,20 表示复制文件名中前20个字符（不含扩展名），%N:~-0,-20 表示复制扩展名的前 20个字符（不含文件名）。
		特殊参数：
		? 作为 第一个 参数时，启动程序前显示 对话框 ，列出下列其他参数。您可以在启动程序之前更改参数。甚至可以阻止程序执行。
		%P 插入来源路径，以反斜杠 (\) 结尾。
		%N 插入光标所在的文件名。
		%T 插入当前目标路径，对压缩程序尤其有用。
		%M 插入目标文件夹的当前文件名。
		%O 插入当前文件名，不含扩展名。
		%E 插入当前文件的 扩展名，无前导句号 (.)。
		%B, %B0..%B9
		从路径中添加文件夹（目录）名称（包括来自分支视图的相对路径或搜索结果）。
		%B 或 %B0 = 上级文件夹（父目录）, %B1 = 上两级文件夹（父目录的上级目录），以此类推。
		%BT, %BT0..%BT9
		从目标路径添加文件夹（目录）名称（不包括来自分支视图的相对路径）。
		%BT 或 %BT0 = 上级文件夹（父目录）, %BT1 = 上两级文件夹（父目录的上级目录），以此类推。
		%B-, %B-0..%B-9
		从路径中添加文件夹（目录）名称（不包括来自分支视图的相对路径和空的搜索结果）。
		%B- 或 %B-0 = 上级文件夹（父目录）, %B-1 = 上两级文件夹（父目录的上级目录），以此类推。
		%B+, %B+0..%B+9
		从路径添加文件夹（目录）名称（包括分支视图），从驱动器/服务器名称开始计算：
		%B+ = 包含 ":" 符号在内的驱动器符，%B+0 = 不包含 ":" 符号在内的驱动器符，%B+1 = 第一个文件夹（目录）或共享，%B+2 = 第二个文件夹（目录），以此类推。
		%S 在命令行中插入所有选中文件的文件名。带有空格的文件名将放在双引号 ("") 中。请注意命令行最大长度是 32767 个字符。
		%S10
		在命令行中插入（最多）前 10 个选中文件的文件名。这样可以限定传递给程序的文件名个数。可指定其它数字。
		%P%S
		将所有选定文件的名称插入命令行，带有完整路径。包含空格的名称将被放入到双引号 ("") 中。请不要自行在 %P%S 前后放置引号！
		%R 与 %S 类似，但插入的是目标面板中选定文件的文件名。
		注意：%N 和 %M 插入长文件名，而 %n 和 %m 插入 8.3 DOS 文件名。%P 和 %T 插入长路径名，%p 和 %t 插入短路径名。（%o，%e 和 %s 也是一样）。
		如果直接在 %S 或 %s 前加上 %P 或 %p，%T 或 %t，每个文件的路径名将和文件名一起插入。例如：%P%S 表示插入所有选中文件的长路径名和长文件名。
		%C1 类似于「比较文件内容」功能的第一个参数：第一个选定的文件，或光标下的文件
		%C2 类似于「比较文件内容」功能的第二个参数：第二个选定的文件，或在目标面板中第一个选定的文件，或者是在目标面板中包含相同名称的文件。注意：如果右侧面板处于活动状态并且选定少于2个文件，则 ％C1 和 ％C2 参数会逆转。
		%C3..%C9
		来源面板中选定的第 3 到 9 个文件，如果没有选定足够的文件，则为空。
		%c1..%c9
		类似于 %C1..%C9，但适用于 8.3 (DOS) 格式的文件和路径
		%% 插入百分号 (%)。
		%L、%l、%F、%f、%D、%d、%WL、%WF、%UL、%UF 在临时文件夹中创建包含选定文件和文件夹名字的列表文件，并在命令行中插入该文件的文件名。列表文件在调用程序退出后自动删除。每个命令仅支持一个列表。可创建以下 10 种类型的列表文件：
		%L 包含完整路径的长文件名，例如：c:\Program Files\Long name.exe
		%l （小写 L）包含完整路径的短文件名，例如：C:\PROGRA~1\LONGNA~1.EXE
		%F 不含路径的长文件名，例如：Long name.exe
		%f 不含路径的短文件名，例如：LONGNA~1.EXE
		%D 包含完整路径的短文件名，但重音使用 DOS 字符集。
		%d 不含路径的短文件名，但重音使用 DOS 字符集。
		%Q 当名称包含空格时，关闭某些参数（如：%P%N）的自动引号。然后用户必须自行放置它们。
		%UL, %UF 与 %L 和 %F 类似，但列表文件是 UTF-8 格式（带有字节顺序标志 BOM）。
		%WL, %WF 与 %L 和 %F 类似，但列表文件是 UTF-16 格式（带有字节顺序标志 BOM）。
		%v 在 "虚拟面板" 等文件系统插件中插入虚拟文件名，其中 %N 粘贴为入口点的真实文件（在文件系统中）的名称
		%V 类似于 %v，但包括完整路径（包括插件名称）
		%X 将本参数后面的下列参数解释为左/右面板而非来源/目标面板的的参数：
		  %P、%p （左侧路径）；%T、%t （右侧路径）；%N、%n （左侧文件名）；%M、%m （右侧文件名）；
		  %S、%s （左侧选定文件）；%R、%r （右侧选定文件）
		  示例： %X%P %T  传递左侧路径和右侧路径到外部同步工具等程序
		%x 将本参数后面的参数仍解释为来源/目标面板参数。
		  示例： %X%P %x%P 传递左侧和来源面板的路径到将要调用的程序
		%Y 参数中的任何位置：使用某个像 %L 这样的列表参数时将空列表传递给程序。否则，将传递光标下的文件。
		%Z 参数中的任何位置：进入压缩文件时，%P 或 %T 代表压缩文件名，并作为路径参数传递给外部程序。
		  示例： %Z%P 将压缩文件名传递给外部工具（当 TC 显示压缩文件内容时）。
		*/
		public void setArgs()
		{
			// 基本参数设置
			args["%P"] = srcDir;
			args["%N"] = srcfiles;
			args["%T"] = targetDir;
			args["%M"] = targetfiles;
			args["%O"] = Path.GetFileNameWithoutExtension(srcfiles);
			args["%E"] = Path.GetExtension(srcfiles)?.TrimStart('.');
			args["%S"] = srcfiles;
			args["%R"] = targetfiles;
			args["%%"] = "%";

			// 处理小写版本（短文件名）
			args["%p"] = GetShortPath(srcDir);
			args["%n"] = GetShortFileName(srcfiles);
			args["%t"] = GetShortPath(targetDir);
			args["%m"] = GetShortFileName(targetfiles);
			args["%o"] = GetShortFileName(Path.GetFileNameWithoutExtension(srcfiles));
			args["%e"] = GetShortFileName(Path.GetExtension(srcfiles)?.TrimStart('.'));
			args["%s"] = GetShortFileName(srcfiles);
			args["%r"] = GetShortFileName(targetfiles);

			// 处理目录参数
			ProcessDirectoryParams(srcDir, "%B", "%B-", "%B+");
			ProcessDirectoryParams(targetDir, "%BT", null, null);

			// 处理比较文件参数
			ProcessCompareParams();

			// 处理列表文件参数
			// 注意：实际创建列表文件的操作应在需要时执行
			// 这里只是设置参数占位符
			args["%L"] = "<长文件名列表文件>";
			args["%l"] = "<短文件名列表文件>";
			args["%F"] = "<不含路径的长文件名列表文件>";
			args["%f"] = "<不含路径的短文件名列表文件>";
			args["%D"] = "<DOS字符集的长文件名列表文件>";
			args["%d"] = "<DOS字符集的短文件名列表文件>";
			args["%UL"] = "<UTF-8格式的长文件名列表文件>";
			args["%UF"] = "<UTF-8格式的不含路径的长文件名列表文件>";
			args["%WL"] = "<UTF-16格式的长文件名列表文件>";
			args["%WF"] = "<UTF-16格式的不含路径的长文件名列表文件>";

			// 其他特殊参数
			args["%Q"] = "<关闭自动引号>";
			args["%v"] = "<虚拟文件名>";
			args["%V"] = "<完整路径的虚拟文件名>";
			args["%X"] = "<切换为左/右面板参数>";
			args["%x"] = "<切换回来源/目标面板参数>";
			args["%Y"] = "<空列表或光标下文件>";
			args["%Z"] = "<压缩文件名作为路径参数>";

			// 组合参数
			args["%P%S"] = CombinePaths(srcDir, srcfiles);
			args["%p%s"] = CombinePaths(GetShortPath(srcDir), GetShortFileName(srcfiles));
			args["%T%R"] = CombinePaths(targetDir, targetfiles);
			args["%t%r"] = CombinePaths(GetShortPath(targetDir), GetShortFileName(targetfiles));
		}

		// 处理子字段语法：~开始位置,长度
		public string ProcessSubfield(string value, string subfieldSpec)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(subfieldSpec) || !subfieldSpec.StartsWith("~"))
				return value;

			try
			{
				// 移除开头的~
				subfieldSpec = subfieldSpec.Substring(1);

				// 处理多个~分隔的部分
				string[] parts = subfieldSpec.Split('~');
				string result = value;

				foreach (string part in parts)
				{
					if (string.IsNullOrEmpty(part))
						continue;

					// 解析开始位置和长度
					string[] parameters = part.Split(',');
					if (parameters.Length < 1)
						continue;

					// 解析开始位置
					if (!int.TryParse(parameters[0], out int startPos))
						continue;

					// 解析长度（如果提供）
					int length = result.Length - Math.Abs(startPos);
					if (parameters.Length > 1 && int.TryParse(parameters[1], out int specifiedLength))
						length = specifiedLength;

					// 处理特殊情况：-0表示不含扩展名的文件名或仅扩展名
					if (startPos == -0)
					{
						// 获取不含扩展名的文件名
						string fileNameWithoutExt = Path.GetFileNameWithoutExtension(result);

						// 如果长度为负数，则获取扩展名
						if (length < 0)
						{
							string extension = Path.GetExtension(result).TrimStart('.');
							result = extension.Substring(0, Math.Min(Math.Abs(length), extension.Length));
						}
						else
						{
							result = fileNameWithoutExt.Substring(0, Math.Min(length, fileNameWithoutExt.Length));
						}
					}
					else
					{
						// 处理正常的子字段
						if (startPos < 0) // 从末尾开始计算
						{
							startPos = result.Length + startPos;
							if (startPos < 0)
								startPos = 0;
						}

						if (startPos >= result.Length)
							result = "";
						else if (length < 0) // 负长度表示从末尾向前取
						{
							int endPos = result.Length + length;
							if (endPos <= startPos)
								result = "";
							else
								result = result.Substring(startPos, endPos - startPos);
						}
						else // 正常长度
						{
							if (startPos + length > result.Length)
								length = result.Length - startPos;

							result = result.Substring(startPos, length);
						}
					}
				}

				return result;
			}
			catch
			{
				// 出现任何异常，返回原始值
				return value;
			}
		}

		// 获取参数值，支持子字段处理
		public string GetArgValue(string arg)
		{
			if (string.IsNullOrEmpty(arg))
				return string.Empty;

			// 检查是否包含子字段规范
			int colonPos = arg.IndexOf(':');
			if (colonPos <= 0)
			{
				// 没有子字段，直接返回参数值
				return args.TryGetValue(arg, out string value) ? value : string.Empty;
			}

			// 提取基本参数和子字段规范
			string baseArg = arg.Substring(0, colonPos);
			string subfieldSpec = arg.Substring(colonPos + 1);

			// 获取基本参数值
			string baseValue = args.TryGetValue(baseArg, out string val) ? val : string.Empty;

			// 应用子字段处理
			return ProcessSubfield(baseValue, subfieldSpec);
		}

		// 获取短路径名
		private string GetShortPath(string path)
		{
			// 实际实现应使用Windows API获取短文件名
			// 这里简化处理
			return path;
		}

		// 获取短文件名
		private string GetShortFileName(string fileName)
		{
			// 实际实现应使用Windows API获取短文件名
			// 这里简化处理
			return fileName;
		}

		// 处理目录参数
		private void ProcessDirectoryParams(string path, string basePrefix, string noRelativePrefix, string drivePrefix)
		{
			if (string.IsNullOrEmpty(path))
				return;

			try
			{
				// 分割路径为目录部分
				string[] parts = path.TrimEnd('\\').Split('\\');

				// 处理基本目录参数 (%B, %B0..%B9)
				if (!string.IsNullOrEmpty(basePrefix))
				{
					// %B 或 %B0 = 上级文件夹（父目录）
					args[$"{basePrefix}"] = args[$"{basePrefix}0"] = parts.Length > 1 ? parts[parts.Length - 2] : "";

					// %B1..%B9 = 上两级文件夹及以上
					for (int i = 1; i <= 9; i++)
					{
						int index = parts.Length - 2 - i;
						args[$"{basePrefix}{i}"] = index >= 0 ? parts[index] : "";
					}
				}

				// 处理无相对路径的目录参数 (%B-, %B-0..%B-9)
				if (!string.IsNullOrEmpty(noRelativePrefix))
				{
					// %B- 或 %B-0 = 上级文件夹（父目录）
					args[$"{noRelativePrefix}"] = args[$"{noRelativePrefix}0"] = parts.Length > 1 ? parts[parts.Length - 2] : "";

					// %B-1..%B-9 = 上两级文件夹及以上
					for (int i = 1; i <= 9; i++)
					{
						int index = parts.Length - 2 - i;
						args[$"{noRelativePrefix}{i}"] = index >= 0 ? parts[index] : "";
					}
				}

				// 处理从驱动器开始的目录参数 (%B+, %B+0..%B+9)
				if (!string.IsNullOrEmpty(drivePrefix))
				{
					// 处理驱动器部分
					if (parts.Length > 0 && parts[0].EndsWith(":"))
					{
						// %B+ = 包含 ":" 符号在内的驱动器符
						args[$"{drivePrefix}"] = parts[0];

						// %B+0 = 不包含 ":" 符号在内的驱动器符
						args[$"{drivePrefix}0"] = parts[0].TrimEnd(':');
					}
					else
					{
						args[$"{drivePrefix}"] = "";
						args[$"{drivePrefix}0"] = "";
					}

					// %B+1..%B+9 = 第一个文件夹及以上
					for (int i = 1; i <= 9; i++)
					{
						int index = i;
						args[$"{drivePrefix}{i}"] = index < parts.Length ? parts[index] : "";
					}
				}
			}
			catch
			{
				// 出现异常时，设置空值
				if (!string.IsNullOrEmpty(basePrefix))
				{
					args[$"{basePrefix}"] = args[$"{basePrefix}0"] = "";
					for (int i = 1; i <= 9; i++)
						args[$"{basePrefix}{i}"] = "";
				}

				if (!string.IsNullOrEmpty(noRelativePrefix))
				{
					args[$"{noRelativePrefix}"] = args[$"{noRelativePrefix}0"] = "";
					for (int i = 1; i <= 9; i++)
						args[$"{noRelativePrefix}{i}"] = "";
				}

				if (!string.IsNullOrEmpty(drivePrefix))
				{
					args[$"{drivePrefix}"] = args[$"{drivePrefix}0"] = "";
					for (int i = 1; i <= 9; i++)
						args[$"{drivePrefix}{i}"] = "";
				}
			}
		}

		// 处理比较文件参数
		private void ProcessCompareParams()
		{
			try
			{
				// 获取源面板中选中的文件
				var selectedFiles = isleft ? LeftList.SelectedItems : RightList.SelectedItems;
				var targetSelectedFiles = !isleft ? LeftList.SelectedItems : RightList.SelectedItems;

				// 如果右侧面板处于活动状态并且选定少于2个文件，则 %C1 和 %C2 参数会逆转
				bool reverseParams = !isleft && selectedFiles.Count < 2;

				// 处理 %C1..%C9 参数
				for (int i = 1; i <= 9; i++)
				{
					string paramName = $"%C{i}";
					string shortParamName = $"%c{i}";

					if (i == 1 && reverseParams) // 第一个参数，可能需要逆转
					{
						// 使用目标面板的第一个选中文件
						args[paramName] = targetSelectedFiles.Count > 0 ?
							((ListViewItem)targetSelectedFiles[0]).SubItems[0].Text : "";
						args[shortParamName] = GetShortFileName(args[paramName]);
					}
					else if (i == 2 && reverseParams) // 第二个参数，可能需要逆转
					{
						// 使用源面板的第一个选中文件
						args[paramName] = selectedFiles.Count > 0 ?
							((ListViewItem)selectedFiles[0]).SubItems[0].Text : "";
						args[shortParamName] = GetShortFileName(args[paramName]);
					}
					else // 正常处理
					{
						// 获取源面板中第i个选中的文件
						int index = i - 1;
						args[paramName] = index < selectedFiles.Count ?
							((ListViewItem)selectedFiles[index]).SubItems[0].Text : "";
						args[shortParamName] = GetShortFileName(args[paramName]);
					}
				}
			}
			catch
			{
				// 出现异常时，设置空值
				for (int i = 1; i <= 9; i++)
				{
					args[$"%C{i}"] = "";
					args[$"%c{i}"] = "";
				}
			}
		}

		// 组合路径和文件名
		private string CombinePaths(string path, string files)
		{
			if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(files))
				return string.Empty;

			try
			{
				// 确保路径以反斜杠结尾
				if (!path.EndsWith("\\"))
					path += "\\";

				// 处理多个文件名（以|分隔）
				string[] fileNames = files.Split('|');
				var combinedPaths = new List<string>();

				foreach (string fileName in fileNames)
				{
					if (!string.IsNullOrEmpty(fileName))
					{
						string fullPath = Path.Combine(path, fileName);

						// 如果路径包含空格，添加引号
						if (fullPath.Contains(" "))
							fullPath = $"\"{fullPath}\"";

						combinedPaths.Add(fullPath);
					}
				}

				return string.Join(" ", combinedPaths);
			}
			catch
			{
				return string.Empty;
			}
		}

		private void LeftPathTextBox_PathChanged(object? sender, EventArgs e)
		{
			isleft = true;
			UpdateTreeViewSelection(LeftTree, LeftPathTextBox.CurrentNode);
		}

		private void RightPathTextBox_PathChanged(object? sender, EventArgs e)
		{
			isleft = false;
			UpdateTreeViewSelection(RightTree, RightPathTextBox.CurrentNode);
		}

		private void UpdateTreeViewSelection(TreeView treeView, IShengAddressNode snode)
		{
			TreeNode? node = null;
			if (snode.tNode != null)
				node = snode.tNode;
			else
			{
				if (snode.UniqueID.Equals(string.Empty))
					node = form.FindTreeNode(form.activeRoot.Nodes, snode.DisplayName);  //应该用绝对路径查找，而不是相对路径，否则遇到相同名称的文件夫目录会出现问题
				else
					node = form.FindTreeNode(form.activeThispc.Nodes, snode.UniqueID);
			}
			if (node != null)
			{
				treeView.SelectedNode = node;
				Debug.Print($"set {treeView.Name} selected node to {node.FullPath}");
				node.EnsureVisible();
			}
			else
				Debug.Print($"can not find {treeView.Name} node for {snode.UniqueID}");
		}
		public void InitializeUI()
		{
			InitializeLayout();
			InitializeTreeViews();
			InitializeTreeViewIcons();
			InitializeListViews();
			InitializeDriveComboBoxes();
			InitializePreviewPanels();
			InitializeDynamicMenu();
			InitializeDynamicToolbar();
			InitializeFtpController();
			InitializeStatusStrips();
			InitializeToolStrip();
			InitializeBookmarkLists();
		}
		private void InitializeFtpController()
		{
			ftpController = new FtpController(form, form.asyncfTPMGR, form.fTPMGR);
		}
		public void InitializeLayout()
		{
			int topHeight = 0;
			Panel containerPanel = new()
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(0, topHeight, 0, 0)
			};
			form.Controls.Add(containerPanel);

			MainContainer.Dock = DockStyle.Fill;
			MainContainer.Orientation = Orientation.Vertical;

			//int halfWidth = (form.ClientSize.Width - MainContainer.SplitterWidth) / 2;
			//MainContainer.SplitterDistance = halfWidth;
			MainContainer.SplitterMoved += MainContainer_SplitterMoved;

			containerPanel.Controls.Add(MainContainer);

			ConfigurePanel(LeftPanel, MainContainer.Panel1);
			ConfigurePanel(RightPanel, MainContainer.Panel2);

			ConfigureUpperPanel(LeftUpperPanel, LeftDrivePanel, LeftPanel.Panel1);
			ConfigureUpperPanel(RightUpperPanel, RightDrivePanel, RightPanel.Panel1);
		}

		private void MainContainer_SplitterMoved(object? sender, SplitterEventArgs e)
		{
			int halfWidth = (form.ClientSize.Width - MainContainer.SplitterWidth) / 2;
			if (Math.Abs(MainContainer.SplitterDistance - halfWidth) > 5)
			{
				MainContainer.SplitterDistance = halfWidth;
			}
		}

		private void ConfigurePanel(SplitContainer panel, Control parent)
		{
			panel.Dock = DockStyle.Fill;
			panel.Orientation = Orientation.Horizontal;
			panel.SplitterDistance = (int)((parent.Width) * 0.5);
			parent.Controls.Add(panel);
		}

		private void ConfigureUpperPanel(Panel upperPanel, Panel drivePanel, Control parent)
		{
			upperPanel.Dock = DockStyle.Fill;
			upperPanel.Padding = new Padding(0, 30, 0, 0);

			drivePanel.Dock = DockStyle.Top;
			drivePanel.Height = 30;

			parent.Controls.Add(upperPanel);
			parent.Controls.Add(drivePanel);
			drivePanel.BringToFront();
		}

		public void InitializeDriveComboBoxes()
		{
			ConfigureDriveBox(LeftDriveComboBox, LeftDrivePanel, LeftPathTextBox);
			ConfigureDriveBox(RightDriveComboBox, RightDrivePanel, RightPathTextBox);

			var rootNode = new ShengFileSystemNode();
			LeftPathTextBox.InitializeRoot(rootNode);
			RightPathTextBox.InitializeRoot(rootNode);

			LoadDrives();
		}

		private void ConfigureDriveBox(ComboBox driveBox, Panel parent, ShengAddressBarStrip pathTextBox)
		{
			driveBox.Dock = DockStyle.Left;
			driveBox.DropDownStyle = ComboBoxStyle.DropDownList;
			driveBox.SelectedIndexChanged += DriveComboBox_SelectedIndexChanged;
			// 创建并配置导航按钮工具栏
			var isleft = pathTextBox == LeftPathTextBox;
			var navigationStrip = isleft ? LeftNavigationStrip : RightNavigationStrip;
			navigationStrip.Dock = DockStyle.Right;
			navigationStrip.GripStyle = ToolStripGripStyle.Hidden;

			// 添加导航按钮
			var gotoParentDir = new ToolStripButton("..", null, NavigationButton_Click)
			{
				ToolTipText = "转到父目录",
				Tag = "parentdir"
			};
			var gotoHomeDir = new ToolStripButton("~", null, NavigationButton_Click)
			{
				ToolTipText = "转到用户主目录",
				Tag = "homedir"
			};
			var gotoRootDir = new ToolStripButton("\\", null, NavigationButton_Click)
			{
				ToolTipText = "转到根目录",
				Tag = "rootdir"
			};
			var gotoFrequentDir = new ToolStripButton("*", null, NavigationButton_Click)
			{
				ToolTipText = "常用目录",
				Tag = "frequentdir"
			};
			var gotoHistoryDir = new ToolStripButton("^", null, NavigationButton_Click)
			{
				ToolTipText = "历史目录",
				Tag = "historydir"
			};
			var gotoAnotherDir = new ToolStripButton(isleft ? "<" : ">", null, NavigationButton_Click)
			{
				ToolTipText = "转到对面面板当前目录",
				Tag = "anotherdir"
			};
			navigationStrip.Items.AddRange(new ToolStripItem[] {
				gotoParentDir, gotoHomeDir, gotoRootDir, gotoFrequentDir, gotoHistoryDir,gotoAnotherDir
			});
			pathTextBox.Dock = DockStyle.Fill;

			parent.Controls.Add(navigationStrip);
			parent.Controls.Add(pathTextBox);
			parent.Controls.Add(driveBox);
			//parent.Controls.SetChildIndex(driveBox, 0);
			//parent.Controls.SetChildIndex(navigationStrip, 1);
			//parent.Controls.SetChildIndex(pathTextBox, 2);
		}
		private void NavigationButton_Click(object? sender, EventArgs e)
		{
			if (sender is not ToolStripButton button) return;

			var isLeft = button.GetCurrentParent() == LeftNavigationStrip;
			isleft = isLeft;

			switch (button.Tag?.ToString())
			{
				case "homedir":
					form.NavigateToPath(Helper.GetPathByEnv("%userprofile%"));
					break;
				case "rootdir":
					form.NavigateToPath(Helper.GetPathByEnv("\\"));
					break;
				case "parentdir":
					var currentPath = isLeft ? LeftPathTextBox.CurrentNode?.UniqueID : RightPathTextBox.CurrentNode?.UniqueID;
					if (!string.IsNullOrEmpty(currentPath))
					{
						var parentPath = Path.GetDirectoryName(currentPath);
						if (!string.IsNullOrEmpty(parentPath))
							form.NavigateToPath(parentPath);
					}
					break;
				case "historydir":
					ShowHistoryMenu(button, isLeft);
					break;
				case "frequentdir":
					break;
				case "anotherdir":
					form.NavigateToPath(isleft ? RightPathTextBox.CurrentNode?.UniqueID : LeftPathTextBox.CurrentNode?.UniqueID);
					break;
			}
		}

		private void ShowHistoryMenu(ToolStripButton button, bool isLeft)
		{
			var historyMenu = new ContextMenuStrip();
			var pathHistory = form.GetPathHistory();

			// 第一部分：高频访问的文件夹
			var frequentPaths = pathHistory
				.GroupBy(p => p)
				.Select(g => new { Path = g.Key, Count = g.Count() })
				.OrderByDescending(x => x.Count)
				.Take(10); // 显示前10个高频访问的文件夹

			foreach (var item in frequentPaths)
			{
				var menuItem = new ToolStripMenuItem(
					$"{Path.GetFileName(item.Path.TrimEnd('\\'))} ({item.Count}次)")
				{
					Tag = item.Path,
					ToolTipText = item.Path
				};
				menuItem.Click += (s, e) => form.NavigateToPath(item.Path);
				historyMenu.Items.Add(menuItem);
			}

			// 添加分隔线
			if (frequentPaths.Any())
			{
				historyMenu.Items.Add(new ToolStripSeparator());
			}

			// 第二部分：最近访问的文件夹
			var recentPaths = pathHistory
				.Distinct()
				.Reverse() // 最新的记录在前面
				.Take(15); // 显示最近15个访问的文件夹

			foreach (var path in recentPaths)
			{
				// 避免重复显示已经在高频列表中的项目
				if (!frequentPaths.Any(f => f.Path == path))
				{
					var menuItem = new ToolStripMenuItem(Path.GetFileName(path.TrimEnd('\\')))
					{
						Tag = path,
						ToolTipText = path
					};
					menuItem.Click += (s, e) => form.NavigateToPath(path);
					historyMenu.Items.Add(menuItem);
				}
			}

			// 如果菜单有项目，则显示
			if (historyMenu.Items.Count > 0)
			{
				// 获取按钮在屏幕上的位置
				var buttonPos = button.Owner.PointToScreen(new Point(button.Bounds.Left, button.Bounds.Bottom));
				historyMenu.Show(buttonPos);
			}
		}

		private void LoadDrives()
		{
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				if (drive.IsReady)
				{
					LeftDriveComboBox.Items.Add(drive.Name);
					RightDriveComboBox.Items.Add(drive.Name);
				}
			}

			if (LeftDriveComboBox.Items.Count > 0)
			{
				LeftDriveComboBox.SelectedIndex = 0;
				RightDriveComboBox.SelectedIndex = 0;
			}
		}

		private void DriveComboBox_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (sender is not ComboBox comboBox) return;
			isleft = comboBox == LeftDriveComboBox;
			if (comboBox.SelectedItem is string drivePath)
			{
				if (lastVisitedPaths.TryGetValue(drivePath, out var lastPath))
					form.NavigateToPath(lastPath);
				else
					form.LoadDriveIntoTree(form.activeTreeview, drivePath);
			}
		}
		// 添加方法用于更新最后访问路径
		public void UpdateLastVisitedPath(string currentPath)
		{
			if (string.IsNullOrEmpty(currentPath)) return;

			// 获取路径的根目录（盘符）
			string root = Path.GetPathRoot(currentPath) ?? "";
			if (!string.IsNullOrEmpty(root))
			{
				lastVisitedPaths[root] = currentPath;
			}
		}
		public void InitializeTreeViews()
		{
			ConfigureTreeListSplitter(LeftTreeListSplitter, LeftUpperPanel, LeftTree, LeftList);
			ConfigureTreeListSplitter(RightTreeListSplitter, RightUpperPanel, RightTree, RightList);
		}

		private void ConfigureTreeListSplitter(SplitContainer splitter, Panel parent, TreeView treeView, ListView listView)
		{
			splitter.Dock = DockStyle.Fill;
			splitter.Orientation = Orientation.Vertical;
			// 设置合理的最小尺寸
			splitter.Panel1MinSize = 100;
			splitter.Panel2MinSize = 100;
			// 调用函数执行treeview绑定事件
			ConfigureTreeView(treeView);
			splitter.Panel1.Controls.Add(treeView);
			splitter.Panel2.Controls.Add(listView);
			parent.Controls.Add(splitter);
			// 设置初始分割位置
			if (parent.Width > 0)
			{
				int desiredDistance = parent.Width / 3;
				// 确保分割线位置在有效范围内
				splitter.SplitterDistance = Math.Max(
					splitter.Panel1MinSize,
					Math.Min(desiredDistance, parent.Width - splitter.Panel2MinSize)
				);
			}

			// 处理父容器大小改变事件
			parent.SizeChanged += (s, e) =>
			{
				if (parent.Width > 0)
				{
					int desiredDistance = parent.Width / 3;
					try
					{
						splitter.SplitterDistance = Math.Max(
							splitter.Panel1MinSize,
							Math.Min(desiredDistance, parent.Width - splitter.Panel2MinSize)
						);
					}
					catch (ArgumentException)
					{
						// 忽略可能的参数异常
					}
				}
			};
		}

		// 为两个ListView设置ImageList
		public void InitializeListViewIcons()
		{
			IconManager.InitializeIcons(listViewImageListL);
			IconManager.InitializeIcons(listViewImageListR);
			IconManager.InitializeIcons(thumbnailImageListL, true);
			IconManager.InitializeIcons(thumbnailImageListR, true);
			LeftList.SmallImageList = listViewImageListL;
			RightList.SmallImageList = listViewImageListR;
			LeftList.LargeImageList = thumbnailImageListL;
			RightList.LargeImageList = thumbnailImageListR;
		}

		public void InitializeTreeViewIcons()
		{
			// 确保先清理旧资源
			CleanupTreeViewResources();

			// 创建新的ImageList实例
			LeftTree.ImageList = new ImageList
			{
				ColorDepth = ColorDepth.Depth32Bit,
				ImageSize = new Size(16, 16)
			};
			RightTree.ImageList = new ImageList
			{
				ColorDepth = ColorDepth.Depth32Bit,
				ImageSize = new Size(16, 16)
			};

			// 初始化图标时使用新的ImageList
			//IconManager.InitializeIcons(LeftTree.ImageList);
			//IconManager.InitializeIcons(RightTree.ImageList);

			// 强制刷新所有节点图标
			RefreshAllNodeIcons(LeftTree.Nodes);
			RefreshAllNodeIcons(RightTree.Nodes);
		}

		private void RefreshAllNodeIcons(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				UpdateNodeIcon(node);
				RefreshAllNodeIcons(node.Nodes);
			}
		}

		public void CleanupTreeViewResources()
		{
			// 先清除节点图标引用
			ClearAllNodeIcons(LeftTree.Nodes);
			ClearAllNodeIcons(RightTree.Nodes);

			// 释放ImageList资源
			if (LeftTree.ImageList != null)
			{
				LeftTree.ImageList.Dispose();
				LeftTree.ImageList = null;
			}
			if (RightTree.ImageList != null)
			{
				RightTree.ImageList.Dispose();
				RightTree.ImageList = null;
			}
		}

		private void ClearAllNodeIcons(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				node.ImageKey = null;
				node.SelectedImageKey = null;
				ClearAllNodeIcons(node.Nodes);
			}
		}

		private void UpdateNodeIcon(TreeNode node)
		{
			string iconKey = IconManager.GetNodeIconKey(node);
			node.ImageKey = iconKey;
			node.SelectedImageKey = iconKey; // 确保选中状态使用相同图标

			// 递归更新所有子节点
			foreach (TreeNode childNode in node.Nodes)
			{
				if (childNode.Text != "...")
				{
					UpdateNodeIcon(childNode);
				}
			}
		}

		public void ConfigureTreeView(TreeView treeView)
		{
			treeView.Dock = DockStyle.Fill;
			treeView.ShowLines = true;
			treeView.HideSelection = false;
			treeView.ShowPlusMinus = true;
			treeView.ShowRootLines = true;
			treeView.PathSeparator = "\\";
			treeView.FullRowSelect = true;
			treeView.ItemHeight = 20;
			treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
			treeView.AllowDrop = true;

			treeView.DragOver += form.TreeView_DragOver;
			treeView.DragDrop += form.TreeView_DragDrop;
			treeView.DrawNode += form.TreeView_DrawNode;
			treeView.MouseUp += form.TreeView_MouseUp;
			treeView.AfterSelect += form.TreeView_AfterSelect;
			treeView.NodeMouseClick += form.TreeView_NodeMouseClick;
			treeView.BeforeExpand += form.TreeView_BeforeExpand;
			treeView.MouseDown += form.TreeView_MouseDown;
			treeView.AfterExpand += (s, e) => UpdateNodeIcon(e.Node);
		}
		private void UnregisterTreeViewEvents(TreeView treeView)
		{
			treeView.DragDrop -= form.TreeView_DragDrop;
			treeView.DrawNode -= form.TreeView_DrawNode;
			treeView.MouseUp -= form.TreeView_MouseUp;
			treeView.AfterSelect -= form.TreeView_AfterSelect;
			treeView.NodeMouseClick -= form.TreeView_NodeMouseClick;
			treeView.BeforeExpand -= form.TreeView_BeforeExpand;
			treeView.MouseDown -= form.TreeView_MouseDown;
			treeView.AfterExpand -= (s, e) => UpdateNodeIcon(e.Node);
			treeView.BeforeExpand -= form.TreeView_BeforeExpand;
		}
		public void InitializeListViews()
		{
			ConfigureListView(LeftList, LeftTreeListSplitter.Panel2);
			ConfigureListView(RightList, RightTreeListSplitter.Panel2);
			InitializeListViewIcons();
		}

		private void ConfigureListView(ListView listView, Control parent)
		{
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = false;
			listView.GridLines = true;
			listView.AllowColumnReorder = true;
			listView.LabelEdit = true;
			listView.MultiSelect = true;
			listView.HideSelection = false;
			listView.Sorting = SortOrder.Ascending;
			listView.Columns.Clear();
			listView.Columns.Add("名称", 250); // 新增图标列
			listView.Columns.Add("名称", 0); // 隐藏名称列
			listView.Columns.Add("大小", 100);
			listView.Columns.Add("类型", 80);
			listView.Columns.Add("修改日期", 150);
			listView.Columns.Add("大小", 0); //
			listView.Columns.Add("属性", 80); // 新增属性列，显示RAHSC或L777格式
			listView.AllowDrop = true;
			listView.ItemDrag += form.ListView_ItemDrag;
			listView.DragOver += form.ListView_DragOver;
			listView.DragDrop += form.ListView_DragDrop;
			// 添加双击事件
			listView.MouseDoubleClick += form.ListView_MouseDoubleClick;
			listView.ColumnClick += form.ListView_ColumnClick;
			listView.SelectedIndexChanged += form.ListView_SelectedIndexChanged;
			listView.MouseUp += form.ListView_MouseUp;
			listView.MouseDown += form.ListView_MouseDown;
			listView.MouseMove += form.ListView_MouseMove;
			listView.BeforeLabelEdit += form.ListView_BeforeLabelEdit;
			listView.AfterLabelEdit += form.ListView_AfterLabelEdit;
			parent.Controls.Add(listView);
			listView.BringToFront();
		}
		private void UnregisterListViewEvents(ListView listView)
		{
			listView.ItemDrag -= form.ListView_ItemDrag;
			listView.DragOver -= form.ListView_DragOver;
			listView.DragDrop -= form.ListView_DragDrop;
			// 添加双击事件
			listView.MouseDoubleClick -= form.ListView_MouseDoubleClick;
			listView.ColumnClick -= form.ListView_ColumnClick;
			listView.SelectedIndexChanged -= form.ListView_SelectedIndexChanged;
			listView.MouseUp -= form.ListView_MouseUp;
			listView.MouseDown -= form.ListView_MouseDown;
			listView.MouseMove -= form.ListView_MouseMove;
			listView.BeforeLabelEdit -= form.ListView_BeforeLabelEdit;
			listView.AfterLabelEdit -= form.ListView_AfterLabelEdit;
		}

		public void InitializePreviewPanels()
		{
			LeftPreview.Dock = DockStyle.Fill;
			LeftPreview.Multiline = true;
			LeftPreview.ReadOnly = true;
			LeftPreview.ScrollBars = ScrollBars.Both;
			LeftPanel.Panel2.Controls.Add(LeftPreview);

			RightPreview.Dock = DockStyle.Fill;
			RightPreview.Multiline = true;
			RightPreview.ReadOnly = true;
			RightPreview.ScrollBars = ScrollBars.Both;
			RightPanel.Panel2.Controls.Add(RightPreview);
		}

		public void InitializeStatusStrips()
		{
			// 创建状态栏项
			var totalFilesLabel = new ToolStripStatusLabel();
			var selectedFilesLabel = new ToolStripStatusLabel();
			var spacerLabel = new ToolStripStatusLabel { Spring = true }; // 弹性空间

			// 添加到左侧状态栏
			LeftStatusStrip.Items.AddRange(new ToolStripItem[] {
				totalFilesLabel,
				spacerLabel,
				selectedFilesLabel
			});
			LeftStatusStrip.Dock = DockStyle.Bottom;
			LeftPanel.Panel2.Controls.Add(LeftStatusStrip);

			// 为右侧状态栏创建相同的项
			var rightTotalFilesLabel = new ToolStripStatusLabel();
			var rightSelectedFilesLabel = new ToolStripStatusLabel();
			var rightSpacerLabel = new ToolStripStatusLabel { Spring = true };

			RightStatusStrip.Items.AddRange(new ToolStripItem[] {
				rightTotalFilesLabel,
				rightSpacerLabel,
				rightSelectedFilesLabel
			});
			RightStatusStrip.Dock = DockStyle.Bottom;
			RightPanel.Panel2.Controls.Add(RightStatusStrip);
			// 添加事件处理
			LeftList.ItemSelectionChanged += (s, e) => UpdateStatusBar(LeftList, LeftStatusStrip);
			RightList.ItemSelectionChanged += (s, e) => UpdateStatusBar(RightList, RightStatusStrip);

			// 更新初始状态
			UpdateStatusBar(LeftList, LeftStatusStrip);
			UpdateStatusBar(RightList, RightStatusStrip);
		}

		public void UpdateStatusBar(ListView listView, StatusStrip statusStrip)
		{
			var totalStats = CalculateStats(listView.Items.Cast<ListViewItem>());
			var selectedStats = CalculateStats(listView.SelectedItems.Cast<ListViewItem>());

			// 更新总计信息
			statusStrip.Items[0].Text = FormatStatsText(totalStats, "总计");

			// 更新选中信息
			statusStrip.Items[2].Text = FormatStatsText(selectedStats, "已选择");
		}

		private (int files, int folders, long totalSize) CalculateStats(IEnumerable<ListViewItem> items)
		{
			int fileCount = 0;
			int folderCount = 0;
			long totalSize = 0;

			foreach (var item in items)
			{
				if (item.SubItems.Count >= 3) // 确保有足够的子项
				{
					// 检查是否是文件夹
					bool isFolder = item.SubItems[3].Text.Equals("<DIR>", StringComparison.OrdinalIgnoreCase);
					if (isFolder)
						folderCount++;
					else
						fileCount++;
					// 解析文件大小
					if (long.TryParse(item.SubItems[5]?.Text.Replace(",", ""), out long size))
						totalSize += size;
				}
			}

			return (fileCount, folderCount, totalSize);
		}

		private string FormatStatsText((int files, int folders, long totalSize) stats, string prefix)
		{
			if (stats.files == 0 && stats.folders == 0)
				return $"{prefix}: 无项目";

			var parts = new List<string>();
			if (stats.folders > 0)
				parts.Add($"{stats.folders} 个文件夹");
			if (stats.files > 0)
				parts.Add($"{stats.files} 个文件");

			string sizeStr = FileSystemManager.FormatFileSize(stats.totalSize, true);
			return $"{prefix}: {string.Join(", ", parts)}, {sizeStr}";
		}

		//private string FormatFileSize(long bytes)
		//{
		//	string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		//	int order = 0;
		//	double size = bytes;
		//	while (size >= 1024 && order < sizes.Length - 1)
		//	{
		//		order++;
		//		size = size / 1024;
		//	}
		//	return $"总大小: {size:0.##} {sizes[order]}";
		//}

		// 在ListView内容改变时调用此方法
		public void RefreshStatusBar(ListView listView)
		{
			var statusStrip = listView == LeftList ? LeftStatusStrip : RightStatusStrip;
			UpdateStatusBar(listView, statusStrip);
		}
		private ToolStripButton CreateToolStripButton(string text, Keys shortcutKeys, EventHandler onClick)
		{
			var button = new ToolStripButton
			{
				Text = $"{text} ({shortcutKeys})",
				DisplayStyle = ToolStripItemDisplayStyle.Text
			};
			button.Click += onClick;
			return button;
		}
		public void ToggleToolStrip()
		{
			if (isToolStripHidden)
				toolStrip.Show();
			else toolStrip.Hide();
			isToolStripHidden = !isToolStripHidden;
		}
		public void InitializeToolStrip()
		{
			toolStrip = new ToolStrip
			{
				Dock = DockStyle.Bottom
			};

			// 添加按钮
			toolStrip.Items.Add(CreateToolStripButton("查看", Keys.F3, form.ViewButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("编辑", Keys.F4, form.EditButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("复制", Keys.F5, form.CopyButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("移动", Keys.F6, form.MoveButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("文件夹", Keys.F7, form.FolderButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("删除", Keys.F8, form.DeleteButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("终端", Keys.F9, form.TerminalButton_Click));
			toolStrip.Items.Add(CreateToolStripButton("退出", Keys.Alt | Keys.X, form.ExitButton_Click));

			// 将工具栏添加到窗体
			form.Controls.Add(toolStrip);
		}
		public void InitializeBookmarkLists()
		{
			// 初始化左侧书签Panel
			//leftBookmarkPanel.Dock = DockStyle.Top;
			var leftlines = Helper.GetFlowLayoutPanelLineCount(leftBookmarkPanel);
			leftBookmarkPanel.Height = 20 * leftlines;
			//leftBookmarkPanel.WrapContents = true;
			//leftBookmarkPanel.DoubleClick += BookmarkPanel_DoubleClick;
			LeftPanel.Panel2.Controls.Add(leftBookmarkPanel);

			// 初始化右侧书签Panel
			var rightlines = Helper.GetFlowLayoutPanelLineCount(rightBookmarkPanel);
			//rightBookmarkPanel.Dock = DockStyle.Top;
			rightBookmarkPanel.Height = 20 * rightlines;
			//rightBookmarkPanel.WrapContents = true;
			//rightBookmarkPanel.DoubleClick += BookmarkPanel_DoubleClick;
			RightPanel.Panel2.Controls.Add(rightBookmarkPanel);

			// 调整布局顺序
			LeftPanel.Panel2.Controls.SetChildIndex(leftBookmarkPanel, 0);
			LeftPanel.Panel2.Controls.SetChildIndex(LeftPreview, 1);
			RightPanel.Panel2.Controls.SetChildIndex(rightBookmarkPanel, 0);
			RightPanel.Panel2.Controls.SetChildIndex(RightPreview, 1);
		}
		//from环境变量获取%COMMANDER_PATH%
		public string GetCommanderPath()
		{
			string commanderPath = Environment.GetEnvironmentVariable("COMMANDER_PATH") ?? string.Empty;
			//if (string.IsNullOrEmpty(commanderPath))
			{
				//MessageBox.Show("未设置COMMANDER_PATH环境变量", "warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//var bb = Environment.CurrentDirectory;
				//var cc = AppDomain.CurrentDomain.BaseDirectory;
				//var dd = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				return Directory.GetCurrentDirectory(); ;
			}
			return commanderPath;
		}

		public void ToolbarButton_Click(object? sender, EventArgs e)
		{
			//TODO: ADD MOUSE RIGHT CLICK TO EDIT BUTTON
			if (sender is ToolStripItem item)
			{
				if (item.Tag is string cmd)
				{
					Debug.Print($"执行命令: {cmd} <信息");
					form.cmdProcessor.ExecCmd(cmd);
				}
				else if (item.Tag is MenuInfo mi)
				{
					form.cmdProcessor.ExecCmdByMenuInfo(mi);
					Debug.Print($"执行命令: {mi.Cmd} <信息");
				}
			}
		}

		public void InitializeDynamicToolbar()
		{
			var bar = form.configLoader.FindConfigValue("ButtonBar", "Buttonbar");
			var bar1 = form.configLoader.FindConfigValue("ButtonbarVertical", "Buttonbar");
			toolbarManager = new ToolbarManager(form, bar, false);//"DEFAULT.BAR"
			vtoolbarManager = new ToolbarManager(form, bar1, true);//"VERTICAL.BAR"
		}
		public void InitializeDynamicMenu()
		{

			var menu = form.configLoader.FindConfigValue("Configuration", "Mainmenu");
			string menuFilePath = Constants.ZfileCfgPath + menu;// "WCMD_CHN.MNU";
			if (!File.Exists(menuFilePath))
			{
				MessageBox.Show("菜单文件不存在" + menuFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				using (StreamReader reader = new StreamReader(menuFilePath, Encoding.GetEncoding("GB2312")))
				{
					dynamicMenuStrip = new MenuStrip();
					Stack<ToolStripMenuItem> menuStack = new Stack<ToolStripMenuItem>();
					ToolStripMenuItem? currentPopup = null;

					string? line;
					while ((line = reader.ReadLine()) != null)
					{
						line = line.Trim();
						if (line.StartsWith("POPUP"))
						{
							string menuItemText = line.Substring(6).Trim().Trim('"');
							ToolStripMenuItem menuItem = new ToolStripMenuItem(menuItemText);
							if (menuStack.Count > 0)
							{
								menuStack.Peek().DropDownItems.Add(menuItem);
							}
							else
							{
								dynamicMenuStrip.Items.Add(menuItem);
							}
							menuStack.Push(menuItem);
							currentPopup = menuItem;
						}
						else if (line.StartsWith("END_POPUP"))
						{
							if (menuStack.Count > 0)
							{
								menuStack.Pop();
							}
							currentPopup = menuStack.Count > 0 ? menuStack.Peek() : null;
						}
						else if (currentPopup != null)
						{
							line = line.TrimStart().Substring(9);
							if (line.StartsWith("SEPARATOR"))
							{
								if (currentPopup != null)
								{
									currentPopup.DropDownItems.Add(new ToolStripSeparator());
								}
							}
							else
							{
								var lineSplit = line.Split(',');
								var menutxt = lineSplit[0].Trim('"');
								ToolStripMenuItem menuItem = new ToolStripMenuItem(menutxt);

								var cmdid = lineSplit[1].Trim();
								if (int.TryParse(cmdid, out var id))
								{
									var cti = form.cmdProcessor.GetCmdById(id);
									if (cti != null && !menutxt.Equals(string.Empty))
										usermenuMap[menutxt] = new MenuInfo("", "", ((CmdTableItem)cti).CmdName, "", "", 0, "");
								}
								else
								{
									var emd = form.cmdProcessor.GetEmdByName(cmdid);
									if (emd != null && !menutxt.Equals(string.Empty))
										usermenuMap[menutxt] = emd;
								}
								menuItem.Tag = cmdid;
								menuItem.Click += form.MenuItem_Click;
								var iconidx = form.cmdicons_configloader.FindConfigValue("mappings", cmdid);
								if (iconidx != null)
									menuItem.Image = form.iconManager.LoadIcon($"wcmicon2.dll,{iconidx}");
								currentPopup.DropDownItems.Add(menuItem);
							}
						}
						else if (!line.Trim().Equals(string.Empty))
						{
							//Debug.Print($"user menu: {line}");
							if (line == "STARTMENU")
							{
								string UserMenuName = line;
								if (!line.Contains("menu", StringComparison.OrdinalIgnoreCase))
									continue;
								if (UserMenuName == "STARTMENU") UserMenuName = "User";
								var usermenu = form.userConfigLoader.GetConfigSection(UserMenuName);
								//CONSTRUCT MENU BY USING CONFIGSECTION NAMED "USER"
								if (usermenu != null)
								{
									var userpop = new ToolStripMenuItem("开始");
									dynamicMenuStrip.Items.Add(userpop);
									List<string> str = new();
									foreach (var i in usermenu.Items)
										str.Add(i.Key + "=" + i.Value);
									var ms = Helper.GetMenuInfoFromList(str.ToArray());
									foreach (var m in ms)
									{
										if (m.Menu.Equals("-"))
											userpop.DropDownItems.Add(new ToolStripSeparator());
										else
										{
											if (m.Menu.StartsWith("-") && !m.Menu.StartsWith("--"))
											{
												// 处理子菜单起始项
												var submenu = new ToolStripMenuItem(m.Menu.Substring(1));
												submenu.Tag = m;
												usermenuMap[submenu.Text] = m;
												userpop.DropDownItems.Add(submenu);
												currentPopup = submenu;
											}
											else if (m.Menu.Equals("--"))
											{
												// 处理子菜单结束项
												currentPopup = userpop;
											}
											else
											{
												var ddi = new ToolStripMenuItem(m.Menu);
												usermenuMap[m.Menu] = m;
												ddi.Click += form.MenuItem_Click;
												ddi.Tag = m;
												if (currentPopup != null)
													currentPopup.DropDownItems.Add(ddi);
												else
													userpop.DropDownItems.Add(ddi);
											}
										}
									}
								}
							}
						}
					}

					form.MainMenuStrip = dynamicMenuStrip;
					form.Controls.Add(dynamicMenuStrip);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载菜单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public void EditMenu(int menuID)
		{
			using (var editMenuForm = new EditMenuForm(form, menuID))
			{
				if (editMenuForm.ShowDialog() == DialogResult.OK)
				{
					// 重新初始化菜单
					form.Controls.Remove(dynamicMenuStrip);
					dynamicMenuStrip.Dispose();
					InitializeDynamicMenu();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void ReleaseTreeNodes(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag is ShellItem shellItem)
					shellItem.Dispose();
				ReleaseTreeNodes(node.Nodes);
			}
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 取消事件订阅
					LeftPathTextBox.SelectionChange -= LeftPathTextBox_PathChanged;
					RightPathTextBox.SelectionChange -= RightPathTextBox_PathChanged;
					MainContainer.SplitterMoved -= MainContainer_SplitterMoved;
					LeftDriveComboBox.SelectedIndexChanged -= DriveComboBox_SelectedIndexChanged;
					RightDriveComboBox.SelectedIndexChanged -= DriveComboBox_SelectedIndexChanged;

					// 取消书签面板事件订阅
					//if (leftBookmarkPanel != null)
					//	leftBookmarkPanel.MouseDoubleClick -= BookmarkPanel_MouseDoubleClick;
					//if (rightBookmarkPanel != null)
					//	rightBookmarkPanel.MouseDoubleClick -= BookmarkPanel_MouseDoubleClick;
					UnregisterTreeViewEvents(LeftTree);
					UnregisterTreeViewEvents(RightTree);
					UnregisterListViewEvents(LeftList);
					UnregisterListViewEvents(RightList);

					// 释放所有 TreeView 节点中的 ShellItem
					ReleaseTreeNodes(LeftTree.Nodes);
					ReleaseTreeNodes(RightTree.Nodes);
					// 释放托管资源
					LeftList.SmallImageList?.Dispose();
					LeftList.LargeImageList?.Dispose();
					RightList.SmallImageList?.Dispose();
					RightList.LargeImageList?.Dispose();
					LeftList?.Dispose();
					RightList?.Dispose();

					CleanupTreeViewResources();
					LeftTree?.Dispose();
					RightTree?.Dispose();
					// 释放导航按钮工具栏
					LeftNavigationStrip?.Dispose();
					RightNavigationStrip?.Dispose();
					LeftPreview?.Dispose();
					RightPreview?.Dispose();
					LeftStatusStrip?.Dispose();
					RightStatusStrip?.Dispose();
					toolbarManager?.Dispose();
					vtoolbarManager?.Dispose();
					dynamicMenuStrip?.Dispose();
					BookmarkManager?.Dispose();
				}

				// 释放非托管资源
				disposed = true;
			}
		}

		~UIControlManager()
		{
			Dispose(false);
		}
	}
}