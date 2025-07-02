using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Data;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;

namespace zfile
{
	public partial class FileCompareForm : Form
	{
		private string leftFilePath = "";
		private string rightFilePath = "";
		private bool hexMode = false;
		private bool caseSensitive = false;
		private bool ignoreWhitespace = false;
		private bool ignoreCommonLines = false;
		private bool editMode = false;
		private int bytesPerLine = 16;
		private Encoding currentEncoding = Encoding.Default;
		private List<DiffPiece> leftDiffLines;
		private List<DiffPiece> rightDiffLines;
		private List<HexDiff> hexDiffs = new List<HexDiff>();
		private bool isScrolling = false; // 防止滚动递归

		public FileCompareForm()
		{
			//InitializeComponent();
			InitializeUI();
		}
		public FileCompareForm(string leftfile, string rightfile) : this()
		{
			leftFilePath = leftfile;
			rightFilePath = rightfile;
			leftFilePathBox.Text = leftFilePath;
			rightFilePathBox.Text = rightFilePath;
			if (!string.IsNullOrEmpty(leftFilePath) && !string.IsNullOrEmpty(rightFilePath))
			{
				CompareFiles();
			}
		}
		private void InitializeUI()
		{
			// 设置主窗体
			this.Text = "文件差异比较工具";
			this.Size = new Size(1200, 800);
			this.StartPosition = FormStartPosition.CenterScreen;

			// 创建工具栏
			var toolStrip = new ToolStrip();
			toolStrip.Items.AddRange(new ToolStripItem[] {
				new ToolStripButton("比较", null, BtnCompare_Click),
				new ToolStripButton("下一个差异", null, BtnNextDiff_Click),
				new ToolStripButton("上一个差异", null, BtnPrevDiff_Click),
				new ToolStripButton("字体", null, BtnFont_Click),
				new ToolStripButton("16进制模式", null, BtnHexMode_Click) { CheckOnClick = true },
				new ToolStripComboBox("bytesPerLine") {
					Items = { "8", "16", "32", "64" },
					SelectedIndex = 1,
					DropDownStyle = ComboBoxStyle.DropDownList
				},
				new ToolStripButton("区分大小写", null, BtnCaseSensitive_Click) { CheckOnClick = true },
				new ToolStripButton("忽略空格", null, BtnIgnoreWhitespace_Click) { CheckOnClick = true },
				new ToolStripButton("忽略常见行", null, BtnIgnoreCommonLines_Click) { CheckOnClick = true },
				new ToolStripButton("编辑模式", null, BtnEditMode_Click) { CheckOnClick = true },
				new ToolStripButton("复制到右侧", null, BtnCopyToRight_Click),
				new ToolStripButton("复制到左侧", null, BtnCopyToLeft_Click),
				new ToolStripButton("撤销编辑", null, BtnUndo_Click),
				new ToolStripButton("Unicode/ANSI", null, BtnEncoding_Click),
				new ToolStripButton("查找", null, BtnFind_Click),
				new ToolStripButton("查找下一个", null, BtnFindNext_Click)
			});

			// 保存按钮面板
			var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 30 };
			var btnSaveLeft = new Button { Text = "保存左侧", Dock = DockStyle.Left, Width = 100 };
			var btnSaveRight = new Button { Text = "保存右侧", Dock = DockStyle.Right, Width = 100 };
			btnSaveLeft.Click += BtnSaveLeft_Click;
			btnSaveRight.Click += BtnSaveRight_Click;
			buttonPanel.Controls.Add(btnSaveLeft);
			buttonPanel.Controls.Add(btnSaveRight);

			// 状态栏
			var statusBar = new StatusStrip();
			statusBar.Items.Add(new ToolStripStatusLabel("就绪"));
			statusBar.Items.Add(new ToolStripStatusLabel("差异数: 0") { Name = "lblDiffCount" });

			// 主分割容器 - 左右面板各占50%
			var mainSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical
			};
			mainSplit.SplitterWidth = 1;

			// 左侧面板
			var leftPanel = new Panel { Dock = DockStyle.Fill };

			// 左侧内容区 - 水平分割（行号列 + 内容）
			var leftContentSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				SplitterWidth = 1,
			};
			leftLineNumbers = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("新宋体", 9),
				BackColor = Color.LightGray,
				ScrollBars = RichTextBoxScrollBars.None,
				WordWrap = false
			};
			leftContent = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("新宋体", 9),
				ScrollBars = RichTextBoxScrollBars.Both,
				WordWrap = false
			};
			leftContentSplit.Panel1.Controls.Add(leftLineNumbers);
			leftContentSplit.Panel2.Controls.Add(leftContent);
			leftPanel.Controls.Add(leftContentSplit);

			leftFilePathBox = new TextBox { Dock = DockStyle.Top, ReadOnly = true, Height = 25 };
			leftPanel.Controls.Add(leftFilePathBox);

			// 右侧面板
			var rightPanel = new Panel { Dock = DockStyle.Fill };

			// 右侧内容区 - 水平分割（行号列 + 内容）
			var rightContentSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				SplitterWidth = 1,
				
			};
			rightLineNumbers = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("新宋体", 9),
				BackColor = Color.LightGray,
				ScrollBars = RichTextBoxScrollBars.None,
				WordWrap = false
			};
			rightContent = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("新宋体", 9),
				ScrollBars = RichTextBoxScrollBars.Both,
				WordWrap = false
			};
			rightContentSplit.Panel1.Controls.Add(rightLineNumbers);
			rightContentSplit.Panel2.Controls.Add(rightContent);
			rightPanel.Controls.Add(rightContentSplit);

			rightFilePathBox = new TextBox { Dock = DockStyle.Top, ReadOnly = true, Height = 25 };
			rightPanel.Controls.Add(rightFilePathBox);

			mainSplit.Panel1.Controls.Add(leftPanel);
			mainSplit.Panel2.Controls.Add(rightPanel);

			// 用于跟踪滚动定时器
			System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer { Interval = 150, Enabled = false };
			//scrollTimer.Tick += (s, e) => 
			//{
			//	scrollTimer.Stop();
			//	if (hexMode)
			//	{
			//		DisplayHexDiffsForVisibleArea();
			//	}
			//};

			// 添加滚动同步事件（修复递归问题）
			leftContent.VScroll += (s, e) =>
			{
				if (!isScrolling)
				{
					isScrolling = true;
					SyncScroll(leftContent, rightContent);
					SyncLineNumbersScroll(leftContent, leftLineNumbers);
					SyncLineNumbersScroll(leftContent, rightLineNumbers); // 新增：同步右侧行号
					
					// 16进制模式下，滚动时更新可见区域
				//if (hexMode)
				//{
				//	// 检查是否滚动到底部
				//	CheckScrollPosition(leftContent);
					
				//	// 重置定时器，只在滚动停止后更新
				//	scrollTimer.Stop();
				//	scrollTimer.Start();
				//}
					
					isScrolling = false;
				}
			};

			rightContent.VScroll += (s, e) =>
			{
				if (!isScrolling)
				{
					isScrolling = true;
					SyncScroll(rightContent, leftContent);
					SyncLineNumbersScroll(rightContent, rightLineNumbers);
					SyncLineNumbersScroll(rightContent, leftLineNumbers); // 新增：同步左侧行号
					
					// 16进制模式下，滚动时更新可见区域
				//if (hexMode)
				//{
				//	// 检查是否滚动到底部
				//	CheckScrollPosition(rightContent);
					
				//	// 重置定时器，只在滚动停止后更新
				//	scrollTimer.Stop();
				//	scrollTimer.Start();
				//}
					
					isScrolling = false;
				}
			};

			leftContent.HScroll += (s, e) =>
			{
				if (!isScrolling)
				{
					isScrolling = true;
					SyncHorizontalScroll(leftContent, rightContent);
					isScrolling = false;
				}
			};

			rightContent.HScroll += (s, e) =>
			{
				if (!isScrolling)
				{
					isScrolling = true;
					SyncHorizontalScroll(rightContent, leftContent);
					isScrolling = false;
				}
			};

			// 添加控件到窗体
			this.Controls.Add(mainSplit);
			this.Controls.Add(toolStrip);
			this.Controls.Add(buttonPanel);
			this.Controls.Add(statusBar);
			// 在 InitializeUI 末尾添加
			this.Load += (s, e) =>
			{
				mainSplit.SplitterDistance = mainSplit.Width / 2;
			};
			
			// 检查是否滚动到底部，如果是则立即更新显示
			//void CheckScrollPosition(RichTextBox textBox)
			//{
			//	if (!hexMode) return;
				
			//	// 计算当前可见区域
			//	int firstVisibleLine = textBox.GetCharIndexFromPosition(new Point(0, 0));
			//	firstVisibleLine = textBox.GetLineFromCharIndex(firstVisibleLine);
				
			//	int linesPerPage = textBox.Height / textBox.Font.Height;
			//	int lastVisibleLine = firstVisibleLine + linesPerPage;
				
			//	// 计算文件的总行数
			//	int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes?.Length ?? 0, rightBytes?.Length ?? 0) / bytesPerLine);
				
			//	// 检查是否接近底部
			//	if (lastVisibleLine + linesPerPage >= maxLines)
			//	{
			//		// 如果接近底部，立即更新显示
			//		scrollTimer.Stop(); // 停止定时器，避免重复更新
			//		DisplayHexDiffsForVisibleArea();
			//	}
			//}
	
            this.Shown += (s, e) =>
            {
                leftContentSplit.SplitterDistance = leftLineNumbers.Width / 4; // 行号列宽度
                rightContentSplit.SplitterDistance = rightLineNumbers.Width / 4; // 行号列宽度
            };	
		}

		#region UI Controls
		private TextBox leftFilePathBox;
		private TextBox rightFilePathBox;
		private RichTextBox leftContent;
		private RichTextBox rightContent;
		private RichTextBox leftLineNumbers;
		private RichTextBox rightLineNumbers;
		#endregion

		#region 核心功能
		private void CompareFiles()
		{
			if (string.IsNullOrEmpty(leftFilePath)) return;
			if (string.IsNullOrEmpty(rightFilePath)) return;

			if (hexMode)
			{
				CompareHexFiles();
			}
			else
			{
				CompareTextFiles();
			}
		}

		private void CompareTextFiles()
		{
			try
			{
				var leftText = File.ReadAllText(leftFilePath, currentEncoding);
				var rightText = File.ReadAllText(rightFilePath, currentEncoding);

				var diffBuilder = new SideBySideDiffBuilder(new Differ());
				var diffResult = diffBuilder.BuildDiffModel(leftText, rightText,
					ignoreWhitespace, caseSensitive);

				leftDiffLines = diffResult.OldText.Lines;
				rightDiffLines = diffResult.NewText.Lines;

				DisplayTextDiffs();
				UpdateDiffCount();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"比较文件时出错: {ex.Message}");
			}
		}

		private byte[] leftBytes;
		private byte[] rightBytes;
		private int visibleStartLine = 0;
		private int visibleEndLine = 0;
		private int totalHexDiffCount = 0;
		private CancellationTokenSource? hexComparisonCts;

		private void CompareHexFiles()
		{
			try
			{
				// 取消之前的任务（如果有）
				hexComparisonCts?.Cancel();
				hexComparisonCts = new CancellationTokenSource();

				// 显示加载指示器
				UpdateStatusBar("正在加载文件...");
				
				// 清空显示
				leftContent.Clear();
				rightContent.Clear();
				leftLineNumbers.Clear();
				rightLineNumbers.Clear();
				hexDiffs.Clear();

				// 在后台线程加载文件
				Task.Run(() => 
				{
					try 
					{
						var t = DateTime.Now;
						leftBytes = File.ReadAllBytes(leftFilePath);
						rightBytes = File.ReadAllBytes(rightFilePath);

						// 计算可见区域和总行数
						int linesPerPage = leftContent.Height / leftContent.Font.Height;
						int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes.Length, rightBytes.Length) / bytesPerLine);
						visibleStartLine = 0;
						visibleEndLine = Math.Min(linesPerPage * 3, maxLines); // 增加初始显示行数
						
						Debug.Print($"初始化: 总行数={maxLines}, 显示区域={visibleStartLine}-{visibleEndLine}");

						// 计算可见区域的差异
						CalculateHexDiffs(visibleStartLine, visibleEndLine, hexComparisonCts.Token);

						// 在UI线程更新显示
						//this.BeginInvoke(() => 
						//{
						//	DisplayHexDiffsForVisibleArea();
						//	Debug.Print($"Initial hex display took: {DateTime.Now - t}");
						//});

						// 在后台继续计算剩余部分的差异
						Task.Run(() => 
						{
							try 
							{
								int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes.Length, rightBytes.Length) / bytesPerLine);
								CalculateHexDiffs(visibleEndLine, maxLines, hexComparisonCts.Token);
								Debug.Print($"Full hex comparison took: {DateTime.Now - t}");
							}
							catch (OperationCanceledException) { /* 任务被取消 */ }
							catch (Exception ex) 
							{
								this.BeginInvoke(() => MessageBox.Show($"计算差异时出错: {ex.Message}"));
							}
						}, hexComparisonCts.Token);
					}
					catch (OperationCanceledException) { /* 任务被取消 */ }
					catch (Exception ex)
					{
						this.BeginInvoke(() => MessageBox.Show($"比较文件时出错: {ex.Message}"));
					}
				}, hexComparisonCts.Token);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"比较文件时出错: {ex.Message}");
			}
		}

		private void CalculateHexDiffs(int startLine, int endLine, CancellationToken cancellationToken)
		{
			// 确保endLine不超过文件的实际行数
			int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes.Length, rightBytes.Length) / bytesPerLine);
			endLine = Math.Min(endLine, maxLines);
			
			Debug.Print($"计算差异: {startLine}-{endLine}, 总行数: {maxLines}");
			
			for (int line = startLine; line < endLine; line++)
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				int lineStart = line * bytesPerLine;
				bool isDiff = false;
				bool hasContent = false; // 检查行是否有内容

				for (int j = 0; j < bytesPerLine; j++)
				{
					int pos = lineStart + j;
					
					// 检查是否超出文件范围
					if (pos >= leftBytes.Length && pos >= rightBytes.Length)
					{
						break;
					}
					
					// 只要有一个文件在这个位置有内容，就标记为有内容
					if (pos < leftBytes.Length || pos < rightBytes.Length)
					{
						hasContent = true;
					}

					byte leftByte = pos < leftBytes.Length ? leftBytes[pos] : (byte)0;
					byte rightByte = pos < rightBytes.Length ? rightBytes[pos] : (byte)0;

					if (leftByte != rightByte)
					{
						isDiff = true;
						lock (hexDiffs)
						{
							hexDiffs.Add(new HexDiff { Line = line, Position = j });
							totalHexDiffCount++;
						}
					}
				}

				// 只有当行有内容且有差异时才标记整行
				if (isDiff && hasContent)
				{
					lock (hexDiffs)
					{
						hexDiffs.Add(new HexDiff { Line = line, Position = -1 }); // Mark whole line
					}
				}
			}
			
			// 如果计算到了文件末尾，记录一下
			if (endLine >= maxLines)
			{
				Debug.Print($"已计算到文件末尾: {maxLines}行");
			}
		}

		private void DisplayTextDiffs()
		{
			leftContent.Clear();
			rightContent.Clear();
			leftLineNumbers.Clear();
			rightLineNumbers.Clear();

			int lineNum = 1;	//for left
			int lineNum1 = 1;	//for right
			int diffCount = 0;

			for (int i = 0; i < Math.Max(leftDiffLines.Count, rightDiffLines.Count); i++)
			{
				// 左侧行
				if (i < leftDiffLines.Count)
				{
					var piece = leftDiffLines[i];
					AppendWithColor(leftContent, piece.Text, GetDiffColor(piece.Type));
					if (piece.Type != ChangeType.Imaginary)
					{
						leftLineNumbers.AppendText($"{lineNum}\n");
						lineNum++;
					}
					else
					{
						leftLineNumbers.AppendText("\n");
						Debug.Print($"left empty line insert. {i} {lineNum} {piece.Type} {piece.Text}");
					}
				}
				else
				{
					leftLineNumbers.AppendText("\n");
					Debug.Print($"left empty line insert. {i} {lineNum} {leftDiffLines.Count} ");
				}

				// 右侧行
				if (i < rightDiffLines.Count)
				{
					var piece = rightDiffLines[i];
					AppendWithColor(rightContent, piece.Text, GetDiffColor(piece.Type));
					if (piece.Type != ChangeType.Imaginary)
					{
						rightLineNumbers.AppendText($"{lineNum1}\n");
						lineNum1++;
					}
					else
					{
						rightLineNumbers.AppendText("\n");
						Debug.Print($"right empty line insert. {i} {lineNum1} {piece.Type} {piece.Text}");

					}
				}
				else
				{
					rightLineNumbers.AppendText("\n");
					Debug.Print($"right empty line insert. {i} {lineNum1} {rightDiffLines.Count} ");
				}

				// 处理差异计数
				if (i < leftDiffLines.Count && i < rightDiffLines.Count)
				{
					if (leftDiffLines[i].Type != ChangeType.Unchanged ||
						rightDiffLines[i].Type != ChangeType.Unchanged)
					{
						diffCount++;
					}
				}
				else
				{
					diffCount++;
				}

				//lineNum++;
			}

			UpdateStatusBar($"差异数: {diffCount}");
		}

		// 用于跟踪上次可见区域的变量
		private int lastFirstVisibleLine = -1;
		private int lastLastVisibleLine = -1;

		//private void DisplayHexDiffsForVisibleArea()
		//{
		//	// 防止重入
		//	if (isScrolling) return;
		//	isScrolling = true;

		//	try
		//	{
		//		// 计算当前可见区域
		//		int firstVisibleLine = leftContent.GetCharIndexFromPosition(new Point(0, 0));
		//		firstVisibleLine = leftContent.GetLineFromCharIndex(firstVisibleLine);

		//		int linesPerPage = leftContent.Height / leftContent.Font.Height;
		//		int lastVisibleLine = firstVisibleLine + linesPerPage;

		//		// 计算文件的总行数
		//		int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes.Length, rightBytes.Length) / bytesPerLine);

		//		// 检查是否滚动到底部
		//		bool isNearBottom = (lastVisibleLine + linesPerPage >= maxLines);

		//		// 检查可见区域是否真的变化了（避免微小变化导致的重绘）
		//		if (Math.Abs(firstVisibleLine - lastFirstVisibleLine) > 2 || 
		//		    Math.Abs(lastVisibleLine - lastLastVisibleLine) > 2 ||
		//		    isNearBottom) // 接近底部时总是更新
		//		{
		//			// 更新上次可见区域记录
		//			lastFirstVisibleLine = firstVisibleLine;
		//			lastLastVisibleLine = lastVisibleLine;

		//			// 更新可见区域范围，增加缓冲区
		//			visibleStartLine = Math.Max(0, firstVisibleLine - linesPerPage / 2);
					
		//			// 如果接近底部，确保显示到文件末尾
		//			if (isNearBottom)
		//			{
		//				visibleEndLine = maxLines;
		//			}
		//			else
		//			{
		//				visibleEndLine = lastVisibleLine + linesPerPage / 2;
		//			}

		//			// 在后台计算新可见区域的差异
		//			Task.Run(() =>
		//			{
		//				try
		//				{
		//					// 确保这个区域的差异已经计算
		//					if (hexComparisonCts != null && !hexComparisonCts.IsCancellationRequested)
		//					{
		//						CalculateHexDiffs(visibleStartLine, visibleEndLine, hexComparisonCts.Token);
								
		//						// 在UI线程更新显示
		//						this.BeginInvoke(DisplayVisibleHexContent);
		//					}
		//				}
		//				catch (OperationCanceledException) { /* 任务被取消 */ }
		//				catch (Exception ex)
		//				{
		//					this.BeginInvoke(() => Debug.Print($"更新可见区域时出错: {ex.Message}"));
		//				}
		//			});
		//		}
		//	}
		//	finally
		//	{
		//		isScrolling = false;
		//	}
		//}

		//private void DisplayVisibleHexContent()
		//{
		//	// 保存滚动位置
		//	int scrollPos = NativeMethods.GetScrollPos(leftContent.Handle, NativeMethods.SB_VERT);

		//	// 使用双缓冲减少闪烁
		//	SetDoubleBuffered(leftContent, true);
		//	SetDoubleBuffered(rightContent, true);
		//	SetDoubleBuffered(leftLineNumbers, true);
		//	SetDoubleBuffered(rightLineNumbers, true);

		//	leftContent.SuspendLayout();
		//	rightContent.SuspendLayout();
		//	leftLineNumbers.SuspendLayout();
		//	rightLineNumbers.SuspendLayout();

		//	// 使用StringBuilder构建内容，减少字符串连接操作
		//	StringBuilder leftContentText = new StringBuilder();
		//	StringBuilder rightContentText = new StringBuilder();
		//	StringBuilder leftLineNumbersText = new StringBuilder();
		//	StringBuilder rightLineNumbersText = new StringBuilder();

		//	int diffCount = 0;
			
		//	// 计算文件的总行数
		//	int maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes.Length, rightBytes.Length) / bytesPerLine);
			
		//	// 确保visibleEndLine不超过总行数
		//	visibleEndLine = Math.Min(visibleEndLine, maxLines);
			
		//	// 调试信息
		//	Debug.Print($"显示区域: {visibleStartLine}-{visibleEndLine}, 总行数: {maxLines}");

		//	// 只显示可见区域的内容
		//	for (int line = visibleStartLine; line < visibleEndLine; line++)
		//	{
		//		int lineStart = line * bytesPerLine;

		//		// 左侧显示
		//		StringBuilder hexLeft = new StringBuilder();
		//		StringBuilder asciiLeft = new StringBuilder();
		//		for (int i = 0; i < bytesPerLine; i++)
		//		{
		//			int pos = lineStart + i;
		//			if (pos < leftBytes.Length)
		//			{
		//				hexLeft.Append($"{leftBytes[pos]:X2} ");
		//				asciiLeft.Append(GetAsciiChar(leftBytes[pos]));
		//			}
		//			else
		//			{
		//				hexLeft.Append("   ");
		//				asciiLeft.Append(" ");
		//			}
		//		}

		//		// 右侧显示
		//		StringBuilder hexRight = new StringBuilder();
		//		StringBuilder asciiRight = new StringBuilder();
		//		for (int i = 0; i < bytesPerLine; i++)
		//		{
		//			int pos = lineStart + i;
		//			if (pos < rightBytes.Length)
		//			{
		//				hexRight.Append($"{rightBytes[pos]:X2} ");
		//				asciiRight.Append(GetAsciiChar(rightBytes[pos]));
		//			}
		//			else
		//			{
		//				hexRight.Append("   ");
		//				asciiRight.Append(" ");
		//			}
		//		}

		//		// 检查差异
		//		bool hasDiff = false;
		//		lock (hexDiffs)
		//		{
		//			hasDiff = hexDiffs.Any(d => d.Line == line && d.Position == -1);
		//		}
		//		if (hasDiff) diffCount++;

		//		// 添加行号
		//		leftLineNumbersText.AppendLine($"{line + 1}");
		//		rightLineNumbersText.AppendLine($"{line + 1}");

		//		// 添加内容（记录差异行以便后续批量设置颜色）
		//		leftContentText.AppendLine($"{hexLeft} | {asciiLeft}");
		//		rightContentText.AppendLine($"{hexRight} | {asciiRight}");
		//	}

		//	// 批量更新内容
		//	leftContent.Clear();
		//	rightContent.Clear();
		//	leftLineNumbers.Clear();
		//	rightLineNumbers.Clear();

		//	leftContent.Text = leftContentText.ToString();
		//	rightContent.Text = rightContentText.ToString();
		//	leftLineNumbers.Text = leftLineNumbersText.ToString();
		//	rightLineNumbers.Text = rightLineNumbersText.ToString();

		//	// 高亮显示差异行
		//	HighlightHexDiffs();

		//	leftContent.ResumeLayout();
		//	rightContent.ResumeLayout();
		//	leftLineNumbers.ResumeLayout();
		//	rightLineNumbers.ResumeLayout();

		//	// 计算文件的总行数
		//	maxLines = (int)Math.Ceiling((double)Math.Max(leftBytes?.Length ?? 0, rightBytes?.Length ?? 0) / bytesPerLine);
			
		//	// 检查是否需要调整滚动位置（确保底部内容可见）
		//	int linesPerPage = leftContent.Height / leftContent.Font.Height;
		//	int lastVisibleLine = scrollPos + linesPerPage;
			
		//	// 如果接近底部，确保显示到文件末尾
		//	if (lastVisibleLine + linesPerPage >= maxLines && maxLines > linesPerPage)
		//	{
		//		// 调整滚动位置以显示底部
		//		scrollPos = Math.Max(0, maxLines - linesPerPage);
		//		Debug.Print($"调整滚动位置: {scrollPos}, 总行数: {maxLines}");
		//	}
			
		//	// 恢复滚动位置
		//	NativeMethods.SetScrollPos(leftContent.Handle, NativeMethods.SB_VERT, scrollPos, true);
		//	NativeMethods.SendMessage(leftContent.Handle, NativeMethods.WM_VSCROLL,
		//		NativeMethods.SB_THUMBPOSITION + 0x10000 * scrollPos, 0);

		//	// 同步右侧滚动
		//	SyncScroll(leftContent, rightContent);
		//	SyncLineNumbersScroll(leftContent, leftLineNumbers);
		//	SyncLineNumbersScroll(rightContent, rightLineNumbers);

		//	// 更新状态栏
		//	UpdateStatusBar($"差异数: {totalHexDiffCount} (显示区域: {diffCount}, 总行数: {maxLines})");

		//	// 恢复双缓冲设置
		//	SetDoubleBuffered(leftContent, false);
		//	SetDoubleBuffered(rightContent, false);
		//	SetDoubleBuffered(leftLineNumbers, false);
		//	SetDoubleBuffered(rightLineNumbers, false);
			
		//	// 调试信息
		//	Debug.Print($"显示完成: 总行数={maxLines}, 显示区域={visibleStartLine}-{visibleEndLine}, 滚动位置={scrollPos}");
		//}

		private char GetAsciiChar(byte b)
		{
			return b >= 32 && b <= 126 ? (char)b : '.';
		}

		private Color GetDiffColor(ChangeType type)
		{
			switch (type)
			{
				case ChangeType.Inserted: return Color.LightGreen;
				case ChangeType.Deleted: return Color.LightPink;
				case ChangeType.Modified: return Color.LightYellow;
				default: return Color.White;
			}
		}

		/// <summary>
		/// 设置控件的双缓冲属性，减少闪烁
		/// </summary>
		private void SetDoubleBuffered(Control control, bool enabled)
		{
			// 获取控件的Type
			Type controlType = control.GetType();

			// 获取DoubleBuffered属性
			PropertyInfo pi = controlType.GetProperty("DoubleBuffered", 
				BindingFlags.Instance | BindingFlags.NonPublic);

			// 设置DoubleBuffered属性值
			pi?.SetValue(control, enabled, null);

			// 对于RichTextBox，还可以设置其他减少闪烁的属性
			if (control is RichTextBox rtb)
			{
				// 设置WM_ERASEBKGND消息处理
				if (enabled)
				{
					// 使用反射调用SetStyle方法
					MethodInfo method = controlType.GetMethod("SetStyle", 
						BindingFlags.Instance | BindingFlags.NonPublic);

					if (method != null)
					{
						// 减少重绘次数
						method.Invoke(rtb, new object[] { ControlStyles.OptimizedDoubleBuffer, enabled });
						method.Invoke(rtb, new object[] { ControlStyles.AllPaintingInWmPaint, enabled });
					}
				}
			}
		}

		/// <summary>
		/// 高亮显示十六进制差异行
		/// </summary>
		private void HighlightHexDiffs()
		{
			// 获取当前可见区域的差异行
			List<int> diffLines = new List<int>();
			lock (hexDiffs)
			{
				diffLines = hexDiffs
					.Where(d => d.Line >= visibleStartLine && d.Line < visibleEndLine && d.Position == -1)
					.Select(d => d.Line)
					.Distinct()
					.ToList();
			}

			// 批量高亮差异行
			foreach (int line in diffLines)
			{
				// 计算行在文本中的位置
				int lineIndex = line - visibleStartLine;
				if (lineIndex < 0) continue;

				// 获取行的起始和结束位置
				int startPos = leftContent.GetFirstCharIndexFromLine(lineIndex);
				if (startPos < 0) continue;

				int endPos;
				if (lineIndex < leftContent.Lines.Length - 1)
					endPos = leftContent.GetFirstCharIndexFromLine(lineIndex + 1) - 1;
				else
					endPos = leftContent.TextLength;

				// 高亮左侧内容
				leftContent.SelectionStart = startPos;
				leftContent.SelectionLength = endPos - startPos;
				leftContent.SelectionBackColor = Color.LightPink;

				// 高亮右侧内容
				if (lineIndex < rightContent.Lines.Length)
				{
					startPos = rightContent.GetFirstCharIndexFromLine(lineIndex);
					if (startPos >= 0)
					{
						if (lineIndex < rightContent.Lines.Length - 1)
							endPos = rightContent.GetFirstCharIndexFromLine(lineIndex + 1) - 1;
						else
							endPos = rightContent.TextLength;

						rightContent.SelectionStart = startPos;
						rightContent.SelectionLength = endPos - startPos;
						rightContent.SelectionBackColor = Color.LightPink;
					}
				}
			}

			// 重置选择
			leftContent.SelectionStart = 0;
			leftContent.SelectionLength = 0;
			rightContent.SelectionStart = 0;
			rightContent.SelectionLength = 0;
		}

		private void AppendWithColor(RichTextBox rtb, string text, Color color)
		{
			rtb.SelectionStart = rtb.TextLength;
			rtb.SelectionLength = 0;
			rtb.SelectionBackColor = color;
			rtb.AppendText(text + "\n");
			rtb.SelectionBackColor = rtb.BackColor;
		}

		private void SyncScroll(RichTextBox source, RichTextBox target)
		{
			// 同步垂直滚动
			NativeMethods.SetScrollPos(target.Handle, NativeMethods.SB_VERT,
				NativeMethods.GetScrollPos(source.Handle, NativeMethods.SB_VERT), true);
			NativeMethods.SendMessage(target.Handle, NativeMethods.WM_VSCROLL,
				NativeMethods.SB_THUMBPOSITION + 0x10000 * NativeMethods.GetScrollPos(source.Handle, NativeMethods.SB_VERT), 0);
		}

		private void SyncLineNumbersScroll(RichTextBox content, RichTextBox lineNumbers)
		{
			// 同步行号列的垂直滚动
			NativeMethods.SetScrollPos(lineNumbers.Handle, NativeMethods.SB_VERT,
				NativeMethods.GetScrollPos(content.Handle, NativeMethods.SB_VERT), true);
			NativeMethods.SendMessage(lineNumbers.Handle, NativeMethods.WM_VSCROLL,
				NativeMethods.SB_THUMBPOSITION + 0x10000 * NativeMethods.GetScrollPos(content.Handle, NativeMethods.SB_VERT), 0);
		}

		private void SyncHorizontalScroll(RichTextBox source, RichTextBox target)
		{
			// 同步水平滚动
			NativeMethods.SetScrollPos(target.Handle, NativeMethods.SB_HORZ,
				NativeMethods.GetScrollPos(source.Handle, NativeMethods.SB_HORZ), true);
			NativeMethods.SendMessage(target.Handle, NativeMethods.WM_HSCROLL,
				NativeMethods.SB_THUMBPOSITION + 0x10000 * NativeMethods.GetScrollPos(source.Handle, NativeMethods.SB_HORZ), 0);
		}
		#endregion

		#region 按钮事件处理
		private void BtnCompare_Click(object? sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Multiselect = true;
				dialog.Title = "选择要比较的两个文件";

				if (dialog.ShowDialog() == DialogResult.OK && dialog.FileNames.Length == 2)
				{
					leftFilePath = dialog.FileNames[0];
					rightFilePath = dialog.FileNames[1];

					leftFilePathBox.Text = leftFilePath;
					rightFilePathBox.Text = rightFilePath;

					CompareFiles();
				}
			}
		}

		private void BtnNextDiff_Click(object? sender, EventArgs e)
		{
			// 查找下一个差异
			FindNextDiff(true);
		}

		private void BtnPrevDiff_Click(object? sender, EventArgs e)
		{
			// 查找上一个差异
			FindNextDiff(false);
		}

		private void FindNextDiff(bool forward)
		{
			if (hexMode)
			{
				FindNextHexDiff(forward);
			}
			else
			{
				FindNextTextDiff(forward);
			}
		}

		private void FindNextTextDiff(bool forward)
		{
			int startLine = leftContent.GetLineFromCharIndex(leftContent.SelectionStart);
			int nextDiffLine = -1;

			if (forward)
			{
				for (int i = startLine + 1; i < leftDiffLines.Count; i++)
				{
					if (leftDiffLines[i].Type != ChangeType.Unchanged)
					{
						nextDiffLine = i;
						break;
					}
				}
			}
			else
			{
				for (int i = startLine - 1; i >= 0; i--)
				{
					if (leftDiffLines[i].Type != ChangeType.Unchanged)
					{
						nextDiffLine = i;
						break;
					}
				}
			}

			if (nextDiffLine >= 0)
			{
				ScrollToLine(nextDiffLine);
			}
		}

		private void FindNextHexDiff(bool forward)
		{
			int startLine = leftContent.GetLineFromCharIndex(leftContent.SelectionStart);
			int nextDiffLine = -1;

			if (forward)
			{
				for (int i = startLine + 1; i < hexDiffs.Count; i++)
				{
					if (hexDiffs[i].Position != -1)
					{
						nextDiffLine = hexDiffs[i].Line;
						break;
					}
				}
			}
			else
			{
				for (int i = startLine - 1; i >= 0; i--)
				{
					if (hexDiffs[i].Position != -1)
					{
						nextDiffLine = hexDiffs[i].Line;
						break;
					}
				}
			}

			if (nextDiffLine >= 0)
			{
				ScrollToLine(nextDiffLine);
			}
		}

		private void ScrollToLine(int line)
		{
			// 滚动到指定行
			int charIndex = leftContent.GetFirstCharIndexFromLine(line);
			leftContent.SelectionStart = charIndex;
			leftContent.SelectionLength = 0;
			leftContent.ScrollToCaret();
		}

		private void BtnFont_Click(object? sender, EventArgs e)
		{
			using (var fontDialog = new FontDialog())
			{
				if (fontDialog.ShowDialog() == DialogResult.OK)
				{
					leftContent.Font = fontDialog.Font;
					rightContent.Font = fontDialog.Font;
					leftLineNumbers.Font = fontDialog.Font;
					rightLineNumbers.Font = fontDialog.Font;
				}
			}
		}

		private void BtnHexMode_Click(object? sender, EventArgs e)
		{
			hexMode = ((ToolStripButton)sender).Checked;
			
			// 取消之前的任务（如果有）
			hexComparisonCts?.Cancel();
			hexComparisonCts = null;
			
			if (!string.IsNullOrEmpty(leftFilePath)) CompareFiles();
		}

		private void BtnCaseSensitive_Click(object? sender, EventArgs e)
		{
			caseSensitive = ((ToolStripButton)sender).Checked;
			if (!string.IsNullOrEmpty(leftFilePath)) CompareFiles();
		}

		private void BtnIgnoreWhitespace_Click(object? sender, EventArgs e)
		{
			ignoreWhitespace = ((ToolStripButton)sender).Checked;
			if (!string.IsNullOrEmpty(leftFilePath)) CompareFiles();
		}

		private void BtnIgnoreCommonLines_Click(object? sender, EventArgs e)
		{
			ignoreCommonLines = ((ToolStripButton)sender).Checked;
			if (!string.IsNullOrEmpty(leftFilePath)) CompareFiles();
		}

		private void BtnEditMode_Click(object? sender, EventArgs e)
		{
			editMode = ((ToolStripButton)sender).Checked;
			leftContent.ReadOnly = !editMode;
			rightContent.ReadOnly = !editMode;
		}

		private void BtnCopyToRight_Click(object? sender, EventArgs e)
		{
			// 复制选中内容到右侧
			if (leftContent.SelectionLength > 0)
			{
				int startLine = leftContent.GetLineFromCharIndex(leftContent.SelectionStart);
				int endLine = leftContent.GetLineFromCharIndex(leftContent.SelectionStart + leftContent.SelectionLength);

				for (int i = startLine; i <= endLine; i++)
				{
					if (i < leftDiffLines.Count && leftDiffLines[i].Type == ChangeType.Deleted)
					{
						// 处理删除行的复制
						rightDiffLines[i] = new DiffPiece(leftDiffLines[i].Text, ChangeType.Unchanged);
					}
				}

				DisplayTextDiffs();
			}
		}

		private void BtnCopyToLeft_Click(object? sender, EventArgs e)
		{
			// 复制选中内容到左侧
			if (rightContent.SelectionLength > 0)
			{
				int startLine = rightContent.GetLineFromCharIndex(rightContent.SelectionStart);
				int endLine = rightContent.GetLineFromCharIndex(rightContent.SelectionStart + rightContent.SelectionLength);

				for (int i = startLine; i <= endLine; i++)
				{
					if (i < rightDiffLines.Count && rightDiffLines[i].Type == ChangeType.Inserted)
					{
						// 处理新增行的复制
						leftDiffLines[i] = new DiffPiece(rightDiffLines[i].Text, ChangeType.Unchanged);
					}
				}

				DisplayTextDiffs();
			}
		}

		private void BtnUndo_Click(object? sender, EventArgs e)
		{
			// 撤销编辑
			leftContent.Undo();
			rightContent.Undo();
		}

		private void BtnEncoding_Click(object? sender, EventArgs e)
		{
			currentEncoding = (currentEncoding == Encoding.Default) ?
				Encoding.UTF8 : Encoding.Default;

			if (!string.IsNullOrEmpty(leftFilePath)) CompareFiles();
		}

		private void BtnFind_Click(object? sender, EventArgs e)
		{
			// 查找功能
			MessageBox.Show("查找功能");
		}

		private void BtnFindNext_Click(object? sender, EventArgs e)
		{
			// 查找下一个
			MessageBox.Show("查找下一个功能");
		}

		private void BtnSaveLeft_Click(object? sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(leftFilePath)) return;

			try
			{
				File.WriteAllText(leftFilePath, leftContent.Text, currentEncoding);
				MessageBox.Show("左侧文件保存成功！");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存文件时出错: {ex.Message}");
			}
		}

		private void BtnSaveRight_Click(object? sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(rightFilePath)) return;

			try
			{
				File.WriteAllText(rightFilePath, rightContent.Text, currentEncoding);
				MessageBox.Show("右侧文件保存成功！");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存文件时出错: {ex.Message}");
			}
		}
		#endregion

		#region 辅助方法
		private void UpdateDiffCount()
		{
			int count = 0;
			if (leftDiffLines != null)
			{
				count = leftDiffLines.Count(p => p.Type != ChangeType.Unchanged);
			}
			UpdateStatusBar($"差异数: {count}");
		}

		private void UpdateStatusBar(string message)
		{
			foreach (ToolStripItem item in ((StatusStrip)this.Controls[this.Controls.Count - 1]).Items)
			{
				if (item.Name == "lblDiffCount")
				{
					item.Text = message;
					return;
				}
			}
		}
		#endregion

		#region 内部类
		private class HexDiff
		{
			public int Line { get; set; }
			public int Position { get; set; } // -1表示整行
		}

		internal static class NativeMethods
		{
			public const int WM_VSCROLL = 0x115;
			public const int WM_HSCROLL = 0x114;
			public const int SB_VERT = 1;
			public const int SB_HORZ = 0;
			public const int SB_THUMBPOSITION = 4;
			public const int SB_THUMBTRACK = 5;

			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

			[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
			public static extern int GetScrollPos(IntPtr hWnd, int nBar);

			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
		}
		#endregion

		private void InitializeComponent()
		{

		}
	}
}