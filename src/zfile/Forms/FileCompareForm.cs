using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
namespace zfile
{
    public class FileCompareForm : Form
    {
        // Win32 API 函数声明
        [DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        
        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        
        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        
        private TextBox txtLeftFile;
        private TextBox txtRightFile;
        private Button btnSelectLeft;
        private Button btnSelectRight;
        private ToolStrip toolStrip;
        private RichTextBox txtLeftContent;
        private RichTextBox txtRightContent;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private bool isHexMode;
        private bool isCaseSensitive;
        private bool ignoreWhitespace;
        private bool ignoreCommonLines;
        private bool isEditMode;
        private int bytesPerLine = 16;
        private List<Difference> differences;
        private int currentDifferenceIndex = -1;
        private bool isDarkMode;
        private bool isLeftModified;
        private bool isRightModified;
        private string leftFilePath;
        private string rightFilePath;
        private Font currentFont;
        private string searchText;
        private int currentSearchIndex = -1;
        private bool isScrolling = false;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel topPanel;
        private TableLayoutPanel contentPanel;
        private TableLayoutPanel bottomPanel;
        private RichTextBox leftLineNumbers;
        private RichTextBox rightLineNumbers;
        private ToolStripComboBox bytesPerLineCombo;
        private bool isHighlighting = false;
		//private RichTextBox _syncSource = null;
		private HashSet<RichTextBox> _syncing = new();
		private bool isSuppressingSyncScroll = false;
		private string[] alignedLeftLineNumbersArr;
		private string[] alignedRightLineNumbersArr;
		public FileCompareForm(string leftFile, string rightFile)
        {
            InitializeComponents();
            leftFilePath = leftFile;
            rightFilePath = rightFile;
            txtLeftFile.Text = leftFile;
            txtRightFile.Text = rightFile;
            LoadFiles();
        }

        private void InitializeComponents()
        {
            Text = "文件比较";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;

            // 创建主布局
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // 创建顶部文件选择区域
            topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // 左侧文件选择区域
            var leftFilePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            leftFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            leftFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtLeftFile = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            btnSelectLeft = new Button
            {
                Text = "选择文件"
                //Dock = DockStyle.Fill
            };
            btnSelectLeft.Click += BtnSelectLeft_Click;

            leftFilePanel.Controls.Add(txtLeftFile, 0, 0);
            leftFilePanel.Controls.Add(btnSelectLeft, 1, 0);

            // 右侧文件选择区域
            var rightFilePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            rightFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            rightFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtRightFile = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            btnSelectRight = new Button
            {
                Text = "选择文件"
                //Dock = DockStyle.Fill
            };
            btnSelectRight.Click += BtnSelectRight_Click;

            rightFilePanel.Controls.Add(txtRightFile, 0, 0);
            rightFilePanel.Controls.Add(btnSelectRight, 1, 0);

            topPanel.Controls.Add(leftFilePanel, 0, 0);
            topPanel.Controls.Add(rightFilePanel, 1, 0);

            // 创建工具栏
            toolStrip = new ToolStrip();
            bytesPerLineCombo = new ToolStripComboBox("每行字节数")
            {
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "16", "24", "32" }
            };
            bytesPerLineCombo.SelectedIndex = 0;
            bytesPerLineCombo.SelectedIndexChanged += BytesPerLineCombo_SelectedIndexChanged;
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("比较", null, BtnCompare_Click),
                new ToolStripButton("下一个差异", null, BtnNextDiff_Click),
                new ToolStripButton("上一个差异", null, BtnPrevDiff_Click),
                new ToolStripButton("字体", null, BtnFont_Click),
                new ToolStripButton("16进制模式", null, BtnHexMode_Click) { CheckOnClick = true },
                bytesPerLineCombo,
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

            // 创建内容区域
            contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // 创建左侧内容区域
            var leftContentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            leftContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            leftContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            leftLineNumbers = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("新宋体", 9),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            txtLeftContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("新宋体", 9),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                HideSelection = false
            };

            leftContentPanel.Controls.Add(leftLineNumbers, 0, 0);
            leftContentPanel.Controls.Add(txtLeftContent, 1, 0);

            // 创建右侧内容区域
            var rightContentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            rightContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            rightContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            rightLineNumbers = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("新宋体", 9),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            txtRightContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("新宋体", 9),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                HideSelection = false
            };

            rightContentPanel.Controls.Add(rightLineNumbers, 0, 0);
            rightContentPanel.Controls.Add(txtRightContent, 1, 0);

            contentPanel.Controls.Add(leftContentPanel, 0, 0);
            contentPanel.Controls.Add(rightContentPanel, 1, 0);

            // 创建底部按钮区域
            bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var btnSaveLeft = new Button
            {
                Text = "保存左侧",
                Dock = DockStyle.Fill
            };
            btnSaveLeft.Click += BtnSaveLeft_Click;

            var btnSaveRight = new Button
            {
                Text = "保存右侧",
                Dock = DockStyle.Fill
            };
            btnSaveRight.Click += BtnSaveRight_Click;

            bottomPanel.Controls.Add(btnSaveLeft, 0, 0);
            bottomPanel.Controls.Add(btnSaveRight, 1, 0);

            // 创建状态栏
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            statusStrip.Items.Add(lblStatus);

            // 添加所有控件到主布局
            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.Controls.Add(toolStrip, 0, 1);
            mainLayout.Controls.Add(contentPanel, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            Controls.Add(mainLayout);
            Controls.Add(statusStrip);

            // 设置默认字体
            currentFont = new Font("新宋体", 9);
            txtLeftContent.Font = currentFont;
            txtRightContent.Font = currentFont;
            leftLineNumbers.Font = currentFont;
            rightLineNumbers.Font = currentFont;

            // 注册事件
            txtLeftContent.VScroll += (s, e) => SyncScroll(txtLeftContent, txtRightContent, leftLineNumbers);
            txtRightContent.VScroll += (s, e) => SyncScroll(txtRightContent, txtLeftContent, rightLineNumbers);

			txtLeftContent.TextChanged += (s, e) => UpdateLineNumbers(leftLineNumbers, txtLeftContent);
            txtRightContent.TextChanged += (s, e) => UpdateLineNumbers(rightLineNumbers, txtRightContent);
            FormClosing += FileCompareForm_FormClosing;
        }

        private void BytesPerLineCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(bytesPerLineCombo.SelectedItem.ToString(), out int newBytesPerLine))
            {
                bytesPerLine = newBytesPerLine;
                if (isHexMode)
                {
                    LoadFiles();
                }
            }
        }

        private int lastFirstVisibleLine = -1;
        private Dictionary<RichTextBox, int> lastVisibleLines = new Dictionary<RichTextBox, int>();

		private void SyncScroll(RichTextBox source, RichTextBox target, RichTextBox sourceLineNumbers)
		{
			if (isSuppressingSyncScroll) return;
			if (!this.IsHandleCreated)
				return;
			if (isScrolling) return;
			try
			{
				isScrolling = true;
				BeginInvoke(new Action(() =>
				{
					int sourceMax = GetScrollMax(source.Handle, 1);
					int targetMax = GetScrollMax(target.Handle, 1);

					int sourcePos = GetScrollPos(source.Handle, 1);
					int targetPos = GetScrollPos(target.Handle, 1);

					int syncPos = targetMax == 0 ? 0 : (int)Math.Round(sourcePos * (targetMax / (double)Math.Max(1, sourceMax)));

					// 只在差异大于2像素时才同步
					if (Math.Abs(targetPos - syncPos) > 2)
					{
						syncPos = Math.Max(0, Math.Min(targetMax, syncPos));
						SetScrollPos(target.Handle, 1, syncPos, true);
						PostMessage(target.Handle, 0x115, 4 + 0x10000 * syncPos, 0);
					}

					UpdateLineNumbers(sourceLineNumbers, source);
				}));
			}
			finally
			{
				isScrolling = false;
			}
		}
		//private void SyncScroll(RichTextBox source, RichTextBox target, RichTextBox sourceLineNumbers)
		//{
		//	if (isSuppressingSyncScroll) return;
		//	if (!this.IsHandleCreated)
		//		return;
		//	if (isScrolling) return;
		//	try
		//	{
		//		isScrolling = true;
		//		BeginInvoke(new Action(() =>
		//		{
		//			// 获取source的首个可见行号
		//			int firstVisibleLine = source.GetLineFromCharIndex(source.GetCharIndexFromPosition(new Point(0, 0)));
		//			// 让target滚动到同样的行号
		//			int firstCharIndex = target.GetFirstCharIndexFromLine(firstVisibleLine);
		//			if (firstCharIndex >= 0)
		//			{
		//				target.SelectionStart = firstCharIndex;
		//				target.ScrollToCaret();
		//			}
		//			// 行号列同步
		//			UpdateLineNumbers(sourceLineNumbers, source);
		//		}));
		//	}
		//	finally
		//	{
		//		isScrolling = false;
		//	}
		//}

		// 新增：获取滚动条最大值
		[DllImport("user32.dll")]
		private static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

		private int GetScrollMax(IntPtr hWnd, int nBar)
		{
			int min, max;
			GetScrollRange(hWnd, nBar, out min, out max);
			return max;
		}
		private void HighlightDifferences()
        {
            if (isHighlighting || differences == null) return;
            try
            {
                isHighlighting = true;
				isSuppressingSyncScroll = true; // 禁用滚动同步
												// 使用SuspendLayout/ResumeLayout减少闪烁
				txtLeftContent.SuspendLayout();
                txtRightContent.SuspendLayout();

                // 清除现有高亮
                txtLeftContent.SelectAll();
                txtLeftContent.SelectionBackColor = SystemColors.Window;
                txtRightContent.SelectAll();
                txtRightContent.SelectionBackColor = SystemColors.Window;

                // 高亮差异
                foreach (var diff in differences)
                {
                    // 高亮左侧
                    int leftStart = txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart);
                    int leftEnd = diff.LeftStart + 1 < txtLeftContent.Lines.Length ?
                        txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart + 1) - 1 :
                        txtLeftContent.TextLength;
                    if (leftStart >= 0 && leftEnd >= leftStart)
                    {
                        txtLeftContent.Select(leftStart, leftEnd - leftStart);
                        txtLeftContent.SelectionBackColor = Color.LightPink;
                    }

                    // 高亮右侧
                    int rightStart = txtRightContent.GetFirstCharIndexFromLine(diff.RightStart);
                    int rightEnd = diff.RightStart + 1 < txtRightContent.Lines.Length ?
                        txtRightContent.GetFirstCharIndexFromLine(diff.RightStart + 1) - 1 :
                        txtRightContent.TextLength;
                    if (rightStart >= 0 && rightEnd >= rightStart)
                    {
                        txtRightContent.Select(rightStart, rightEnd - rightStart);
                        txtRightContent.SelectionBackColor = Color.LightPink;
                    }
                }

                // 恢复原始选择
                txtLeftContent.SelectionLength = 0;
                txtRightContent.SelectionLength = 0;
                
                // 恢复布局
                txtLeftContent.ResumeLayout();
                txtRightContent.ResumeLayout();
            }
            finally
            {
                isHighlighting = false;
				isSuppressingSyncScroll = false; // 恢复滚动同步
			}
        }
		private void BuildAlignedTextAndLineNumbers(
	out string[] alignedLeftLines, out string[] alignedRightLines,
	out string[] leftLineNumbers, out string[] rightLineNumbers)
		{
			var leftLines = File.Exists(leftFilePath) ? File.ReadAllLines(leftFilePath) : Array.Empty<string>();
			var rightLines = File.Exists(rightFilePath) ? File.ReadAllLines(rightFilePath) : Array.Empty<string>();

			var diffBuilder = new InlineDiffBuilder(new Differ());
			var diffResult = diffBuilder.BuildDiffModel(
				string.Join("\n", leftLines),
				string.Join("\n", rightLines)
			);

			var leftList = new List<string>();
			var rightList = new List<string>();
			var leftNumList = new List<string>();
			var rightNumList = new List<string>();

			int lIdx = 0, rIdx = 0;
			foreach (var line in diffResult.Lines)
			{
				switch (line.Type)
				{
					case ChangeType.Inserted:
						leftList.Add(""); // 左侧补空行
						rightList.Add(line.Text);
						leftNumList.Add(""); // 行号为空
						rightNumList.Add((rIdx + 1).ToString());
						rIdx++;
						break;
					case ChangeType.Deleted:
						leftList.Add(line.Text);
						rightList.Add(""); // 右侧补空行
						leftNumList.Add((lIdx + 1).ToString());
						rightNumList.Add("");
						lIdx++;
						break;
					default:
						leftList.Add(line.Text);
						rightList.Add(line.Text);
						leftNumList.Add((lIdx + 1).ToString());
						rightNumList.Add((rIdx + 1).ToString());
						lIdx++;
						rIdx++;
						break;
				}
			}

			alignedLeftLines = leftList.ToArray();
			alignedRightLines = rightList.ToArray();
			leftLineNumbers = leftNumList.ToArray();
			rightLineNumbers = rightNumList.ToArray();
		}
		private void LoadFiles()
		{
			try
			{
				if (File.Exists(leftFilePath))
				{
					if (isHexMode)
					{
						var leftBytes = File.ReadAllBytes(leftFilePath);
						var rightBytes = File.Exists(rightFilePath) ? File.ReadAllBytes(rightFilePath) : Array.Empty<byte>();
						int leftLines = (leftBytes.Length + bytesPerLine - 1) / bytesPerLine;
						int rightLines = (rightBytes.Length + bytesPerLine - 1) / bytesPerLine;
						int maxLines = Math.Max(leftLines, rightLines);

						txtLeftContent.Text = GetHexContentWithLines(leftBytes, maxLines);
						txtRightContent.Text = GetHexContentWithLines(rightBytes, maxLines);
					}
					else
					{
						// ...文本模式下...
						BuildAlignedTextAndLineNumbers(
							out var alignedLeftLines, out var alignedRightLines,
							out var leftLineNumbersArr, out var rightLineNumbersArr);

						txtLeftContent.Text = string.Join("\n", alignedLeftLines);
						txtRightContent.Text = string.Join("\n", alignedRightLines);

						// 缓存行号数组
						alignedLeftLineNumbersArr = leftLineNumbersArr;
						alignedRightLineNumbersArr = rightLineNumbersArr;

						// 初始显示可见行号
						UpdateLineNumbers(leftLineNumbers, txtLeftContent, alignedLeftLineNumbersArr);
						UpdateLineNumbers(rightLineNumbers, txtRightContent, alignedRightLineNumbersArr);
					}
				}
				if (File.Exists(rightFilePath) && !isHexMode)
				{
					txtRightContent.Text = File.ReadAllText(rightFilePath);
				}
				CompareFiles();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载文件时出错: {ex.Message}", "错误");
			}
		}
		private void UpdateLineNumbers(RichTextBox lineNumbers, RichTextBox content, string[] lineNumbersArr)
		{
			var firstVisibleLine = content.GetLineFromCharIndex(content.GetCharIndexFromPosition(new Point(0, 0)));
			if (lastVisibleLines.TryGetValue(content, out int lastLine) && lastLine == firstVisibleLine)
				return;
			lastVisibleLines[content] = firstVisibleLine;
			var visibleLines = content.Height / content.Font.Height;
			var sb = new StringBuilder();
			for (int i = firstVisibleLine; i <= firstVisibleLine + visibleLines + 1; i++)
			{
				if (i < lineNumbersArr.Length)
					sb.AppendLine(lineNumbersArr[i]);
			}
			lineNumbers.SuspendLayout();
			lineNumbers.Text = sb.ToString();
			lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
			lineNumbers.ResumeLayout();
		}
	
		private void UpdateLineNumbers(RichTextBox lineNumbers, RichTextBox content)
		{
			if (!isHexMode)
			{
				// 文本模式下，使用缓存的行号数组
				string[] lineNumbersArr = (lineNumbers == leftLineNumbers) ? alignedLeftLineNumbersArr : alignedRightLineNumbersArr;
				if (lineNumbersArr == null) return;

				var firstVisibleLine = content.GetLineFromCharIndex(content.GetCharIndexFromPosition(new Point(0, 0)));
				if (lastVisibleLines.TryGetValue(content, out int lastLine) && lastLine == firstVisibleLine)
					return;
				lastVisibleLines[content] = firstVisibleLine;
				var visibleLines = content.Height / content.Font.Height;
				var sb = new StringBuilder();
				for (int i = firstVisibleLine; i <= firstVisibleLine + visibleLines + 1; i++)
				{
					if (i < lineNumbersArr.Length)
						sb.AppendLine(lineNumbersArr[i]);
				}
				lineNumbers.SuspendLayout();
				lineNumbers.Text = sb.ToString();
				lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
				lineNumbers.ResumeLayout();
				return;
			}

			// 16进制模式下，原有逻辑
			var firstVisible = content.GetLineFromCharIndex(content.GetCharIndexFromPosition(new Point(0, 0)));
			if (lastVisibleLines.TryGetValue(content, out int lastL) && lastL == firstVisible)
				return;
			lastVisibleLines[content] = firstVisible;
			var visible = content.Height / content.Font.Height;
			var sbHex = new StringBuilder();
			for (int i = firstVisible; i <= firstVisible + visible + 1; i++)
			{
				if (i < content.Lines.Length)
					sbHex.AppendLine($"{i + 1,4}");
			}
			lineNumbers.SuspendLayout();
			lineNumbers.Text = sbHex.ToString();
			lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
			lineNumbers.ResumeLayout();
		}
		// 新增方法
		private string GetHexContentWithLines(byte[] bytes, int totalLines)
		{
			var sb = new StringBuilder();
			int actualLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;
			for (int i = 0; i < totalLines; i++)
			{
				if (i < actualLines)
					sb.AppendLine(GetHexLine(bytes, i * bytesPerLine));
				else
					sb.AppendLine(""); // 补空行
			}
			return sb.ToString();
		}

        private string GetHexLine(byte[] bytes, int startIndex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytesPerLine && startIndex + i < bytes.Length; i++)
            {
                sb.Append($"{bytes[startIndex + i]:X2} ");
            }
            return sb.ToString().TrimEnd();
        }

        private void CompareFiles()
        {
            differences = new List<Difference>();
            if (isHexMode)
                CompareHexMode();
            else
                CompareTextMode();
            UpdateStatus();
            HighlightDifferences();
            if (differences.Count > 0)
            {
                currentDifferenceIndex = 0;
                NavigateToDifference(currentDifferenceIndex);
            }
        }
	
		private void CompareHexMode()
		{
			var leftBytes = File.ReadAllBytes(leftFilePath);
			var rightBytes = File.ReadAllBytes(rightFilePath);
			int maxLength = Math.Max(leftBytes.Length, rightBytes.Length);

			differences = new List<Difference>();
			for (int i = 0; i < maxLength; i++)
			{
				byte leftByte = i < leftBytes.Length ? leftBytes[i] : (byte)0;
				byte rightByte = i < rightBytes.Length ? rightBytes[i] : (byte)0;
				bool leftExists = i < leftBytes.Length;
				bool rightExists = i < rightBytes.Length;

				if (!leftExists || !rightExists || leftByte != rightByte)
				{
					differences.Add(new Difference
					{
						LeftStart = leftExists ? i : -1,
						RightStart = rightExists ? i : -1,
						LeftLength = leftExists ? 1 : 0,
						RightLength = rightExists ? 1 : 0
					});
				}
			}
		}

		private void CompareTextMode()
		{
			var leftLines = txtLeftContent.Lines;
			var rightLines = txtRightContent.Lines;

			// 使用 DiffPlex 进行行级 diff
			var diffBuilder = new InlineDiffBuilder(new Differ());
			var diffResult = diffBuilder.BuildDiffModel(
				string.Join("\n", leftLines),
				string.Join("\n", rightLines)
			);

			differences = new List<Difference>();
			int leftLine = 0, rightLine = 0;
			foreach (var line in diffResult.Lines)
			{
				switch (line.Type)
				{
					case ChangeType.Inserted:
						differences.Add(new Difference
						{
							LeftStart = leftLine - 1 >= 0 ? leftLine - 1 : 0,
							RightStart = rightLine,
							LeftLength = 0,
							RightLength = 1
						});
						rightLine++;
						break;
					case ChangeType.Deleted:
						differences.Add(new Difference
						{
							LeftStart = leftLine,
							RightStart = rightLine - 1 >= 0 ? rightLine - 1 : 0,
							LeftLength = 1,
							RightLength = 0
						});
						leftLine++;
						break;
					case ChangeType.Modified:
						differences.Add(new Difference
						{
							LeftStart = leftLine,
							RightStart = rightLine,
							LeftLength = 1,
							RightLength = 1
						});
						leftLine++;
						rightLine++;
						break;
					default:
						leftLine++;
						rightLine++;
						break;
				}
			}
		}

        private void NavigateToDifference(int index)
        {
            if (index < 0 || index >= differences.Count) return;

            try
            {
                isScrolling = true;
                var diff = differences[index];
                
                // 暂停布局以减少闪烁
                txtLeftContent.SuspendLayout();
                txtRightContent.SuspendLayout();
                
                // 导航到左侧差异
                int leftPos = txtLeftContent.GetFirstCharIndexFromLine(diff.LeftStart);
                if (leftPos >= 0)
                {
                    txtLeftContent.SelectionStart = leftPos;
                    int line = diff.LeftStart;
                    int pos = line * txtLeftContent.Font.Height;
                    SetScrollPos(txtLeftContent.Handle, 1, pos, true);
                    PostMessage(txtLeftContent.Handle, 0x115, 4 + 0x10000 * pos, 0);
                }
                
                // 导航到右侧差异
                int rightPos = txtRightContent.GetFirstCharIndexFromLine(diff.RightStart);
                if (rightPos >= 0)
                {
                    txtRightContent.SelectionStart = rightPos;
                    int line = diff.RightStart;
                    int pos = line * txtRightContent.Font.Height;
                    SetScrollPos(txtRightContent.Handle, 1, pos, true);
                    PostMessage(txtRightContent.Handle, 0x115, 4 + 0x10000 * pos, 0);
                }
                
                // 更新行号
                UpdateLineNumbers(leftLineNumbers, txtLeftContent);
                UpdateLineNumbers(rightLineNumbers, txtRightContent);
                
                // 恢复布局
                txtLeftContent.ResumeLayout();
                txtRightContent.ResumeLayout();
            }
            finally
            {
                isScrolling = false;
            }
        }

        private void UpdateStatus()
        {
            lblStatus.Text = $"共发现 {differences.Count} 个差异";
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            CompareFiles();
        }

        private void BtnNextDiff_Click(object sender, EventArgs e)
        {
            if (currentDifferenceIndex < differences.Count - 1)
            {
                currentDifferenceIndex++;
                NavigateToDifference(currentDifferenceIndex);
            }
        }

        private void BtnPrevDiff_Click(object sender, EventArgs e)
        {
            if (currentDifferenceIndex > 0)
            {
                currentDifferenceIndex--;
                NavigateToDifference(currentDifferenceIndex);
            }
        }

        private void BtnFont_Click(object sender, EventArgs e)
        {
            using var fontDialog = new FontDialog { Font = currentFont };
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                currentFont = fontDialog.Font;
                txtLeftContent.Font = currentFont;
                txtRightContent.Font = currentFont;
                leftLineNumbers.Font = currentFont;
                rightLineNumbers.Font = currentFont;
            }
        }

        private void BtnHexMode_Click(object sender, EventArgs e)
        {
            isHexMode = !isHexMode;
            LoadFiles();
        }

        private void BtnCaseSensitive_Click(object sender, EventArgs e)
        {
            isCaseSensitive = !isCaseSensitive;
            CompareFiles();
        }

        private void BtnIgnoreWhitespace_Click(object sender, EventArgs e)
        {
            ignoreWhitespace = !ignoreWhitespace;
            CompareFiles();
        }

        private void BtnIgnoreCommonLines_Click(object sender, EventArgs e)
        {
            ignoreCommonLines = !ignoreCommonLines;
            CompareFiles();
        }

        private void BtnEditMode_Click(object sender, EventArgs e)
        {
            isEditMode = !isEditMode;
            txtLeftContent.ReadOnly = !isEditMode;
            txtRightContent.ReadOnly = !isEditMode;
        }

        private void BtnCopyToRight_Click(object sender, EventArgs e)
        {
            if (txtLeftContent.SelectionLength > 0)
            {
                txtRightContent.SelectedText = txtLeftContent.SelectedText;
                isRightModified = true;
            }
        }

        private void BtnCopyToLeft_Click(object sender, EventArgs e)
        {
            if (txtRightContent.SelectionLength > 0)
            {
                txtLeftContent.SelectedText = txtRightContent.SelectedText;
                isLeftModified = true;
            }
        }

        private void BtnUndo_Click(object sender, EventArgs e)
        {
            txtLeftContent.Undo();
            txtRightContent.Undo();
        }

        private void BtnEncoding_Click(object sender, EventArgs e)
        {
            // 实现编码切换逻辑
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            using var findDialog = new Form
            {
                Text = "查找",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent
            };

            var txtSearch = new TextBox
            {
                Location = new Point(10, 10),
                Width = 260
            };

            var btnFind = new Button
            {
                Text = "查找",
                Location = new Point(10, 40),
                DialogResult = DialogResult.OK
            };

            findDialog.Controls.AddRange(new Control[] { txtSearch, btnFind });

            if (findDialog.ShowDialog() == DialogResult.OK)
            {
                searchText = txtSearch.Text;
                currentSearchIndex = -1;
                BtnFindNext_Click(sender, e);
            }
        }

        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(searchText)) return;

            var searchStart = currentSearchIndex + 1;
            var leftIndex = txtLeftContent.Text.IndexOf(searchText, searchStart);
            var rightIndex = txtRightContent.Text.IndexOf(searchText, searchStart);

            if (leftIndex >= 0 || rightIndex >= 0)
            {
                currentSearchIndex = Math.Min(leftIndex >= 0 ? leftIndex : int.MaxValue,
                    rightIndex >= 0 ? rightIndex : int.MaxValue);

                txtLeftContent.SelectionStart = currentSearchIndex;
                txtLeftContent.SelectionLength = searchText.Length;
                txtRightContent.SelectionStart = currentSearchIndex;
                txtRightContent.SelectionLength = searchText.Length;
            }
        }

        private void BtnSelectLeft_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                leftFilePath = dialog.FileName;
                txtLeftFile.Text = leftFilePath;
                LoadFiles();
            }
        }

        private void BtnSelectRight_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                rightFilePath = dialog.FileName;
                txtRightFile.Text = rightFilePath;
                LoadFiles();
            }
        }

        private void BtnSaveLeft_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(leftFilePath, txtLeftContent.Text);
                isLeftModified = false;
                MessageBox.Show("左侧文件保存成功", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存左侧文件时出错: {ex.Message}", "错误");
            }
        }

        private void BtnSaveRight_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(rightFilePath, txtRightContent.Text);
                isRightModified = false;
                MessageBox.Show("右侧文件保存成功", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存右侧文件时出错: {ex.Message}", "错误");
            }
        }

        private void FileCompareForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isLeftModified || isRightModified)
            {
                var result = MessageBox.Show(
                    "文件已修改，是否保存更改？",
                    "确认",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    if (isLeftModified) BtnSaveLeft_Click(sender, e);
                    if (isRightModified) BtnSaveRight_Click(sender, e);
                }
            }
        }

        private class Difference
        {
            public int LeftStart { get; set; }
            public int RightStart { get; set; }
            public int LeftLength { get; set; }
            public int RightLength { get; set; }
        }
    }
}