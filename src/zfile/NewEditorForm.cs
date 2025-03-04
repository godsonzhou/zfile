using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System.Text.RegularExpressions;

namespace zfile
{
	public class SettingsForm : Form
	{
		private ComboBox fontFamilyComboBox;
		private ComboBox fontSizeComboBox;
		private RadioButton lightModeRadio;
		private RadioButton darkModeRadio;
		private Button okButton;
		private Button cancelButton;

		public Font SelectedFont { get; private set; }
		public bool IsDarkMode { get; private set; }

		public SettingsForm(Font currentFont, bool isDarkMode)
		{
			InitializeComponents();
			InitializeValues(currentFont, isDarkMode);
		}

		private void InitializeComponents()
		{
			this.Text = "编辑器设置";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Size = new Size(400, 300);
			this.StartPosition = FormStartPosition.CenterParent;

			// 字体选择
			var fontLabel = new Label
			{
				Text = "字体:",
				Location = new Point(20, 20)
			};

			fontFamilyComboBox = new ComboBox
			{
				Location = new Point(120, 20),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			// 字体大小
			var fontSizeLabel = new Label
			{
				Text = "字体大小:",
				Location = new Point(20, 60)
			};

			fontSizeComboBox = new ComboBox
			{
				Location = new Point(120, 60),
				Width = 100,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			// 主题模式
			var themeGroupBox = new GroupBox
			{
				Text = "主题",
				Location = new Point(20, 100),
				Size = new Size(340, 80)
			};

			lightModeRadio = new RadioButton
			{
				Text = "浅色模式",
				Location = new Point(20, 30),
				Checked = true
			};

			darkModeRadio = new RadioButton
			{
				Text = "深色模式",
				Location = new Point(180, 30)
			};

			themeGroupBox.Controls.AddRange(new Control[] { lightModeRadio, darkModeRadio });

			// 按钮
			okButton = new Button
			{
				Text = "确定",
				DialogResult = DialogResult.OK,
				Location = new Point(200, 220)
			};

			cancelButton = new Button
			{
				Text = "取消",
				DialogResult = DialogResult.Cancel,
				Location = new Point(290, 220)
			};

			// 添加控件
			this.Controls.AddRange(new Control[] {
				fontLabel, fontFamilyComboBox,
				fontSizeLabel, fontSizeComboBox,
				themeGroupBox,
				okButton, cancelButton
			});

			// 加载字体列表
			foreach (var family in FontFamily.Families)
			{
				fontFamilyComboBox.Items.Add(family.Name);
			}

			// 加载字体大小列表
			int[] sizes = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36 };
			foreach (int size in sizes)
			{
				fontSizeComboBox.Items.Add(size);
			}

			this.AcceptButton = okButton;
			this.CancelButton = cancelButton;

			okButton.Click += OkButton_Click;
		}

		private void InitializeValues(Font currentFont, bool isDarkMode)
		{
			// 设置当前字体
			fontFamilyComboBox.SelectedItem = currentFont.FontFamily.Name;
			fontSizeComboBox.SelectedItem = (int)currentFont.Size;

			// 设置当前主题模式
			lightModeRadio.Checked = !isDarkMode;
			darkModeRadio.Checked = isDarkMode;
		}

		private void OkButton_Click(object sender, EventArgs e)
		{
			try
			{
				string fontFamily = fontFamilyComboBox.SelectedItem?.ToString();
				float fontSize = Convert.ToInt32(fontSizeComboBox.SelectedItem);

				if (!string.IsNullOrEmpty(fontFamily))
				{
					SelectedFont = new Font(fontFamily, fontSize);
					IsDarkMode = darkModeRadio.Checked;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("设置字体时发生错误：" + ex.Message, "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.DialogResult = DialogResult.None;
			}
		}
	}
	public class FindReplaceForm : Form
	{
		private TextEditorControl editor;
		private Label findLabel;
		private Label replaceLabel;
		private TextBox findTextBox;
		private TextBox replaceTextBox;
		private CheckBox matchCaseCheckBox;
		private CheckBox useRegexCheckBox;
		private CheckBox wholeWordCheckBox;
		private Button findNextButton;
		private Button replaceButton;
		private Button replaceAllButton;
		private Button closeButton;
		private Label statusLabel;
		private int lastSearchPosition = 0;

		public FindReplaceForm(TextEditorControl editor)
		{
			this.editor = editor;
			InitializeComponents();
		}

		private void InitializeComponents()
		{
			this.Text = "查找和替换";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.ShowInTaskbar = false;
			this.Size = new System.Drawing.Size(450, 250);
			this.StartPosition = FormStartPosition.CenterParent;

			// 创建控件
			findLabel = new Label { Text = "查找内容:", Location = new System.Drawing.Point(10, 15) };
			findTextBox = new TextBox { Location = new System.Drawing.Point(120, 12), Width = 200 };

			replaceLabel = new Label { Text = "替换为:", Location = new System.Drawing.Point(10, 45) };
			replaceTextBox = new TextBox { Location = new System.Drawing.Point(120, 42), Width = 200 };

			matchCaseCheckBox = new CheckBox
			{
				Text = "区分大小写",
				Location = new System.Drawing.Point(10, 80)
			};

			useRegexCheckBox = new CheckBox
			{
				Text = "使用正则表达式",
				Location = new System.Drawing.Point(120, 80)
			};

			wholeWordCheckBox = new CheckBox
			{
				Text = "全字匹配",
				Location = new System.Drawing.Point(240, 80)
			};

			findNextButton = new Button
			{
				Text = "查找下一个",
				Location = new System.Drawing.Point(330, 12),
				Width = 90
			};

			replaceButton = new Button
			{
				Text = "替换",
				Location = new System.Drawing.Point(290, 42),
				Width = 90
			};

			replaceAllButton = new Button
			{
				Text = "全部替换",
				Location = new System.Drawing.Point(290, 72),
				Width = 90
			};

			closeButton = new Button
			{
				Text = "关闭",
				Location = new System.Drawing.Point(330, 102),
				Width = 90
			};

			statusLabel = new Label
			{
				Location = new System.Drawing.Point(10, 150),
				Width = 370,
				Height = 40,
				AutoSize = false
			};

			// 添加事件处理
			findNextButton.Click += FindNext_Click;
			replaceButton.Click += Replace_Click;
			replaceAllButton.Click += ReplaceAll_Click;
			closeButton.Click += (s, e) => Close();
			findTextBox.TextChanged += (s, e) => statusLabel.Text = "";

			// 添加控件到窗体
			Controls.AddRange(new Control[] {
				findLabel, findTextBox,
				replaceLabel, replaceTextBox,
				matchCaseCheckBox, useRegexCheckBox, wholeWordCheckBox,
				findNextButton, replaceButton, replaceAllButton, closeButton,
				statusLabel
			});
		}

		public void ShowFind()
		{
			replaceTextBox.Visible = false;
			//Controls.Find("替换为:", false)[0].Visible = false;
			replaceLabel.Visible = false;
			replaceButton.Visible = false;
			replaceAllButton.Visible = false;
			this.Text = "查找";
			Show();
			findTextBox.Focus();
		}

		public void ShowReplace()
		{
			replaceTextBox.Visible = true;
			//Controls.Find("替换为:", false)[0].Visible = true;
			replaceLabel.Visible = true;
			replaceButton.Visible = true;
			replaceAllButton.Visible = true;
			this.Text = "替换";
			Show();
			findTextBox.Focus();
		}

		private void FindNext_Click(object sender, EventArgs e)
		{
			Find(false);
		}

		private void Replace_Click(object sender, EventArgs e)
		{
			if (editor.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
			{
				editor.Document.Replace(editor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Offset,
									 editor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Length,
									 replaceTextBox.Text);
			}
			Find(false);
		}

		private void ReplaceAll_Click(object sender, EventArgs e)
		{
			int count = 0;
			lastSearchPosition = 0;

			while (Find(true))
			{
				editor.Document.Replace(editor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Offset,
									 editor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Length,
									 replaceTextBox.Text);
				count++;
			}

			statusLabel.Text = $"完成替换，共替换了 {count} 处文本";
		}

		private bool Find(bool silent)
		{
			if (string.IsNullOrEmpty(findTextBox.Text))
			{
				if (!silent) MessageBox.Show("请输入要查找的文本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			string textContent = editor.Document.TextContent;
			int startIndex = lastSearchPosition;

			try
			{
				if (useRegexCheckBox.Checked)
				{
					RegexOptions options = RegexOptions.None;
					if (!matchCaseCheckBox.Checked) options |= RegexOptions.IgnoreCase;

					var regex = new Regex(findTextBox.Text, options);
					Match match = regex.Match(textContent, startIndex);

					if (match.Success)
					{
						SelectText(match.Index, match.Length);
						lastSearchPosition = match.Index + 1;
						return true;
					}
				}
				else
				{
					StringComparison comparison = matchCaseCheckBox.Checked ?
						StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

					int index = textContent.IndexOf(findTextBox.Text, startIndex, comparison);

					if (index >= 0)
					{
						if (wholeWordCheckBox.Checked)
						{
							bool isWholeWord = IsWholeWord(textContent, index, findTextBox.Text.Length);
							if (!isWholeWord)
							{
								lastSearchPosition = index + 1;
								return Find(silent);
							}
						}

						SelectText(index, findTextBox.Text.Length);
						lastSearchPosition = index + 1;
						return true;
					}
				}

				if (startIndex > 0 && !silent)
				{
					if (MessageBox.Show("已到达文档末尾，是否从头开始搜索？", "提示",
						MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						lastSearchPosition = 0;
						return Find(silent);
					}
				}

				if (!silent) statusLabel.Text = "找不到指定文本";
				return false;
			}
			catch (ArgumentException ex)
			{
				if (!silent) MessageBox.Show("无效的正则表达式：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
		}

		private bool IsWholeWord(string text, int startIndex, int length)
		{
			bool isWordStart = startIndex == 0 || !char.IsLetterOrDigit(text[startIndex - 1]);
			bool isWordEnd = (startIndex + length) >= text.Length ||
							!char.IsLetterOrDigit(text[startIndex + length]);

			return isWordStart && isWordEnd;
		}

		private void SelectText(int offset, int length)
		{
			editor.ActiveTextAreaControl.SelectionManager.SetSelection(
				editor.Document.OffsetToPosition(offset),
				editor.Document.OffsetToPosition(offset + length)
			);
			editor.ActiveTextAreaControl.ScrollToCaret();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
	public class NewEditorForm : Form
    {
        private TextEditorControl textEditor;
        private string currentFilePath;
        private Encoding currentEncoding;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private FindReplaceForm findReplaceDialog;
		private StatusStrip statusStrip;
		private ToolStripStatusLabel encodingLabel;
		private ToolStripStatusLabel lineColumnLabel;
		private ToolStripStatusLabel fileTypeLabel;
		private bool isDarkMode = false;

		public NewEditorForm()
        {
            InitializeComponents();
            InitializeEditor();
            SetupMenus();
            SetupToolbar();
        }
		public NewEditorForm(string filename): this()
		{
			OpenFile(filename);
		}

        private void InitializeComponents()
        {
            this.Size = new Size(800, 600);
            this.Text = "新建文档 - 文本编辑器";

            menuStrip = new MenuStrip();
            toolStrip = new ToolStrip();
            textEditor = new TextEditorControl();

            this.MainMenuStrip = menuStrip;
            
            // 设置控件布局
            menuStrip.Dock = DockStyle.Top;
            toolStrip.Dock = DockStyle.Top;
            textEditor.Dock = DockStyle.Fill;

            this.Controls.Add(textEditor);
            this.Controls.Add(toolStrip);
            this.Controls.Add(menuStrip);
			// 初始化状态栏
			statusStrip = new StatusStrip();
			encodingLabel = new ToolStripStatusLabel();
			lineColumnLabel = new ToolStripStatusLabel();
			fileTypeLabel = new ToolStripStatusLabel();

			// 设置状态栏项的初始值和宽度
			encodingLabel.AutoSize = false;
			encodingLabel.Width = 150;
			encodingLabel.Text = "UTF-8";

			lineColumnLabel.AutoSize = false;
			lineColumnLabel.Width = 150;
			lineColumnLabel.Text = "行 1, 列 1";

			fileTypeLabel.AutoSize = false;
			fileTypeLabel.Width = 100;
			fileTypeLabel.Text = "文本文件";

			statusStrip.Items.AddRange(new ToolStripItem[] {
				encodingLabel,
				lineColumnLabel,
				fileTypeLabel
			});

			// 在最后添加状态栏
			this.Controls.Add(statusStrip);
			statusStrip.Dock = DockStyle.Bottom;
		}

        private void InitializeEditor()
        {
            textEditor.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy("Default");
            //textEditor.Document.UndoStack.s = 1000;
            textEditor.ShowInvalidLines = false;
            textEditor.ShowSpaces = false;
            textEditor.ShowTabs = false;
            textEditor.ShowEOLMarkers = false;
            textEditor.AllowDrop = true;
            textEditor.ShowLineNumbers = true;
            
            // 绑定文件拖放事件
            textEditor.DragEnter += TextEditor_DragEnter;
            textEditor.DragDrop += TextEditor_DragDrop;
			// 添加光标位置变化事件处理
			textEditor.ActiveTextAreaControl.TextArea.Caret.PositionChanged += Caret_PositionChanged;

			// 设置默认字体
			textEditor.Font = new Font("Consolas", 11f);

			// 应用默认主题
			ApplyTheme(isDarkMode);
		}

		private void Caret_PositionChanged(object sender, EventArgs e)
		{
			UpdateCaretPosition();
		}

		private void UpdateCaretPosition()
		{
			var caret = textEditor.ActiveTextAreaControl.Caret;
			lineColumnLabel.Text = $"行 {caret.Line + 1}, 列 {caret.Column + 1}";
		}

		private void SetupMenus()
        {
            // 文件菜单
            var fileMenu = new ToolStripMenuItem("文件(&F)");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("新建(&N)", null, NewFile_Click, Keys.Control | Keys.N),
                new ToolStripMenuItem("打开(&O)...", null, OpenFile_Click, Keys.Control | Keys.O),
                new ToolStripMenuItem("保存(&S)", null, SaveFile_Click, Keys.Control | Keys.S),
                new ToolStripMenuItem("另存为(&A)...", null, SaveFileAs_Click),
                new ToolStripSeparator(),
                new ToolStripMenuItem("退出(&X)", null, Exit_Click)
            });

            // 编辑菜单
            var editMenu = new ToolStripMenuItem("编辑(&E)");
            editMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("撤销(&U)", null, Undo_Click, Keys.Control | Keys.Z),
                new ToolStripMenuItem("重做(&R)", null, Redo_Click, Keys.Control | Keys.Y),
                new ToolStripSeparator(),
                new ToolStripMenuItem("剪切(&T)", null, Cut_Click, Keys.Control | Keys.X),
                new ToolStripMenuItem("复制(&C)", null, Copy_Click, Keys.Control | Keys.C),
                new ToolStripMenuItem("粘贴(&P)", null, Paste_Click, Keys.Control | Keys.V),
                new ToolStripSeparator(),
                new ToolStripMenuItem("查找(&F)...", null, Find_Click, Keys.Control | Keys.F),
                new ToolStripMenuItem("替换(&H)...", null, Replace_Click, Keys.Control | Keys.H)
            });
			// 添加查看菜单
			var viewMenu = new ToolStripMenuItem("查看(&V)");
			viewMenu.DropDownItems.AddRange(new ToolStripItem[] {
				new ToolStripMenuItem("设置(&S)...", null, Settings_Click)
			});
			menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu });
        }
		private void Settings_Click(object sender, EventArgs e)
		{
			using (var settingsForm = new SettingsForm(textEditor.Font, isDarkMode))
			{
				if (settingsForm.ShowDialog() == DialogResult.OK)
				{
					// 应用字体设置
					textEditor.Font = settingsForm.SelectedFont;

					// 应用主题设置
					bool newDarkMode = settingsForm.IsDarkMode;
					if (newDarkMode != isDarkMode)
					{
						ApplyTheme(newDarkMode);
					}
				}
			}
		}

		private void ApplyTheme(bool darkMode)
		{
			isDarkMode = darkMode;
			if (darkMode)
			{
				// 深色主题
				textEditor.BackColor = Color.FromArgb(30, 30, 30);
				textEditor.ForeColor = Color.White;
				textEditor.Document.FoldingManager.FoldingStrategy = null; // 重置折叠策略以更新颜色

				// 设置深色主题的语法高亮颜色
				var darkColors = new Dictionary<string, Color>
				{
					{"Default", Color.White},
					{"Comment", Color.Green},
					{"Keyword", Color.LightBlue},
					{"String", Color.Orange},
					{"Numbers", Color.LightGreen}
				};

				foreach (var color in darkColors)
				{
					var highlightColor = textEditor.Document.HighlightingStrategy.GetColorFor(color.Key);
					highlightColor = new HighlightColor(highlightColor.BackgroundColor, color.Value, highlightColor.Bold, highlightColor.Italic);
				}
			}
			else
			{
				// 浅色主题
				textEditor.BackColor = Color.White;
				textEditor.ForeColor = Color.Black;
				textEditor.Document.FoldingManager.FoldingStrategy = null;

				// 重置为默认的语法高亮颜色
				var lightColors = new Dictionary<string, Color>
				{
					{"Default", Color.Black},
					{"Comment", Color.Green},
					{"Keyword", Color.Blue},
					{"String", Color.Brown},
					{"Numbers", Color.DarkGreen}
				};

				foreach (var color in lightColors)
				{
					var highlightColor = textEditor.Document.HighlightingStrategy.GetColorFor(color.Key);
					highlightColor = new HighlightColor(highlightColor.BackgroundColor, color.Value, highlightColor.Bold, highlightColor.Italic);
				}
			}

			textEditor.Refresh();
		}
		private void SetupToolbar()
        {
            toolStrip.Items.AddRange(new ToolStripItem[] {
                new ToolStripButton("新建", null, NewFile_Click),
                new ToolStripButton("打开", null, OpenFile_Click),
                new ToolStripButton("保存", null, SaveFile_Click),
                new ToolStripSeparator(),
                new ToolStripButton("剪切", null, Cut_Click),
                new ToolStripButton("复制", null, Copy_Click),
                new ToolStripButton("粘贴", null, Paste_Click),
                new ToolStripSeparator(),
                new ToolStripButton("撤销", null, Undo_Click),
                new ToolStripButton("重做", null, Redo_Click)
            });
        }

        #region 文件操作
        private void NewFile_Click(object sender, EventArgs e)
        {
            if (CheckSaveChanges())
            {
                textEditor.Document.TextContent = string.Empty;
                currentFilePath = null;
				currentEncoding = Encoding.UTF8;
				this.Text = "新建文档 - 文本编辑器";

				// 重置状态栏
				encodingLabel.Text = "UTF-8";
				fileTypeLabel.Text = "文本文件";
				UpdateCaretPosition();
			}
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            if (!CheckSaveChanges()) return;

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "所有文件(*.*)|*.*|文本文件(*.txt)|*.txt";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(dlg.FileName);
                }
            }
        }

        private void SaveFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs_Click(sender, e);
            }
            else
            {
                SaveFile(currentFilePath);
            }
        }

        private void SaveFileAs_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    SaveFile(dlg.FileName);
                }
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

		private Encoding DetectFileEncoding(string filePath)
		{
			// 读取文件的前几个字节来检测编码
			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				if (fs.Length >= 4)
				{
					byte[] buffer = new byte[4];
					fs.Read(buffer, 0, 4);

					// 检测 UTF-8 BOM
					if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
						return Encoding.UTF8;

					// 检测 UTF-32 LE
					if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0 && buffer[3] == 0)
						return Encoding.UTF32;

					// 检测 UTF-32 BE
					if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xFE && buffer[3] == 0xFF)
						return Encoding.GetEncoding(12001);

					// 检测 UTF-16 LE
					if (buffer[0] == 0xFF && buffer[1] == 0xFE)
						return Encoding.Unicode;

					// 检测 UTF-16 BE
					if (buffer[0] == 0xFE && buffer[1] == 0xFF)
						return Encoding.BigEndianUnicode;
				}

				// 尝试检测UTF-8（无BOM）
				fs.Position = 0;
				var reader = new StreamReader(fs, Encoding.Default, true);
				reader.ReadToEnd();

				return reader.CurrentEncoding;
			}
		}
		private void OpenFile(string filePath)
        {
            try
            {
				// 检测文件编码
				currentEncoding = DetectFileEncoding(filePath);
				using (var reader = new StreamReader(filePath, currentEncoding))
                {
                    //currentEncoding = reader.CurrentEncoding;
                    textEditor.Document.TextContent = reader.ReadToEnd();
                }
				
				currentFilePath = filePath;
                this.Text = Path.GetFileName(filePath) + " - 文本编辑器";
				// 更新状态栏信息
				UpdateStatusBar(filePath);
				// 根据文件扩展名设置语法高亮
				string extension = Path.GetExtension(filePath).ToLower();
                string highlighting = GetHighlightingByExtension(extension);
                textEditor.Document.HighlightingStrategy = 
                    HighlightingStrategyFactory.CreateHighlightingStrategy(highlighting);
				textEditor.Refresh();
			}
            catch (Exception ex)
            {
                MessageBox.Show("打开文件时发生错误：" + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

		private void UpdateStatusBar(string filePath)
		{
			// 更新编码信息
			encodingLabel.Text = currentEncoding?.EncodingName ?? "UTF-8";

			// 更新文件类型信息
			string extension = Path.GetExtension(filePath).ToLower();
			string fileType = GetFileTypeDescription(extension);
			fileTypeLabel.Text = fileType;

			// 更新光标位置
			UpdateCaretPosition();
		}

		private string GetFileTypeDescription(string extension)
		{
			switch (extension)
			{
				case ".txt": return "文本文件";
				case ".cs": return "C# 源文件";
				case ".js": return "JavaScript";
				case ".html":
				case ".htm": return "HTML";
				case ".xml": return "XML";
				case ".css": return "CSS";
				case ".json": return "JSON";
				case ".md": return "Markdown";
				default: return "文本文件";
			}
		}
		private void SaveFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, currentEncoding ?? Encoding.UTF8))
                {
                    writer.Write(textEditor.Document.TextContent);
                }

                currentFilePath = filePath;
                this.Text = Path.GetFileName(filePath) + " - 文本编辑器";
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存文件时发生错误：" + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CheckSaveChanges()
        {
            if (textEditor.Document.UndoStack.UndoItemCount > 0)
            {
                DialogResult result = MessageBox.Show("文档已修改，是否保存？", "保存文件",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveFile_Click(this, EventArgs.Empty);
                    return true;
                }
                return result == DialogResult.No;
            }
            return true;
        }
        #endregion

        #region 编辑操作
        private void Undo_Click(object sender, EventArgs e)
        {
            if (textEditor.EnableUndo)
                textEditor.Undo();
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            if (textEditor.EnableRedo)
                textEditor.Redo();
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            textEditor.ActiveTextAreaControl.TextArea.ClipboardHandler.Cut(null, null);
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            textEditor.ActiveTextAreaControl.TextArea.ClipboardHandler.Copy(null, null);
        }

        private void Paste_Click(object sender, EventArgs e)
        {
            textEditor.ActiveTextAreaControl.TextArea.ClipboardHandler.Paste(null, null);
        }

        private void Find_Click(object sender, EventArgs e)
        {
            if (findReplaceDialog == null || findReplaceDialog.IsDisposed)
            {
                findReplaceDialog = new FindReplaceForm(textEditor);
            }
            findReplaceDialog.ShowFind();
        }

        private void Replace_Click(object sender, EventArgs e)
        {
            if (findReplaceDialog == null || findReplaceDialog.IsDisposed)
            {
                findReplaceDialog = new FindReplaceForm(textEditor);
            }
            findReplaceDialog.ShowReplace();
        }
        #endregion

        #region 拖放处理
        private void TextEditor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void TextEditor_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && CheckSaveChanges())
            {
                OpenFile(files[0]);
            }
        }
        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckSaveChanges())
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        private string GetHighlightingByExtension(string extension)
        {
            switch (extension)
            {
                case ".cs":
                    return "C#";
                case ".js":
                    return "JavaScript";
                case ".xml":
                case ".config":
                    return "XML";
                case ".html":
                case ".htm":
                    return "HTML";
                case ".css":
                    return "CSS";
                case ".cpp":
                case ".h":
                    return "C++";
                case ".java":
                    return "Java";
                case ".py":
                    return "Python";
                case ".php":
                    return "PHP";
                case ".sql":
                    return "SQL";
                case ".json":
                    return "JSON";
                case ".md":
                case ".markdown":
                    return "Markdown";
                case ".yaml":
                case ".yml":
                    return "YAML";
                case ".ps1":
                case ".psm1":
                    return "PowerShell";
                case ".bat":
                case ".cmd":
                    return "Batch";
                case ".sh":
                    return "Shell";
                default:
                    return "Default";
            }
        }
    }
}