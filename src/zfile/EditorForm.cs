using System.Text;
using System.Text.RegularExpressions;

namespace zfile
{
    public class EditorForm : Form
    {
        #region 字段和属性
        private string _fileName;
        private bool _isModified;
        private Encoding _currentEncoding = Encoding.Default;
        private string _originalText;
        private List<UndoRedoItem> _undoStack;
        private List<UndoRedoItem> _redoStack;
        private bool _isReadOnly;
        private int _maxUndoSteps = 1000;
        private bool _isAdministrator;
        private bool _autoSave;
        private int _autoSaveInterval = 300000; // 5分钟
        private List<CaretPosition> _multiCarets;
        private string _searchPattern;
        private bool _searchRegex;
        private bool _searchCaseSensitive;
        private bool _searchWholeWord;
        private List<string> _searchHistory;
        private List<string> _replaceHistory;

        // 控件
        private RichTextBox _editor;
        private MenuStrip _menuStrip;
        private ToolStrip _toolStrip;
        private StatusStrip _statusStrip;
        private System.Windows.Forms.Timer _autoSaveTimer;
        private Panel _lineNumberPanel;
        private ToolStripStatusLabel _encodingLabel;
        private ToolStripStatusLabel _positionLabel;
        private ToolStripStatusLabel _modifiedLabel;
        private ToolStripStatusLabel _lineEndingLabel;
		//TODO: ADD LANGUAGE HIGH LIGHTER SUPPORT

        public bool IsModified
        {
            get => _isModified;
            private set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    UpdateTitle();
                    UpdateStatusBar();
                }
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                UpdateTitle();
                if (!string.IsNullOrEmpty(value))
                {
                    LoadFile();
                }
            }
        }

        #endregion

        #region 构造函数和初始化
        public EditorForm()
        {
            InitializeComponent();
            InitializeEditor();
            SetupEventHandlers();
        }

        public EditorForm(string fileName) : this()
        {
            FileName = fileName;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建菜单栏
            CreateMenuStrip();

            // 创建工具栏
            CreateToolStrip();

            // 创建编辑器
            _editor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                AcceptsTab = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                DetectUrls = true
            };
			
			// 创建行号面板
			_lineNumberPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 40,
                BackColor = SystemColors.Control
            };
            _lineNumberPanel.Paint += LineNumberPanel_Paint;

            // 初始化自动保存计时器
            _autoSaveTimer = new System.Windows.Forms.Timer
            {
                Interval = _autoSaveInterval,
                Enabled = _autoSave
            };

            // 初始化撤销重做栈
            _undoStack = new List<UndoRedoItem>();
            _redoStack = new List<UndoRedoItem>();

            // 初始化多光标位置列表
            _multiCarets = new List<CaretPosition>();

            // 初始化搜索历史
            _searchHistory = new List<string>();
            _replaceHistory = new List<string>();

			// 创建状态栏
			CreateStatusStrip();

			// 添加控件到窗体
			var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(_editor);
            container.Controls.Add(_lineNumberPanel);

            this.Controls.Add(container);
            this.Controls.Add(_menuStrip);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_statusStrip);
        }

        private void InitializeEditor()
        {
            _currentEncoding = Encoding.UTF8;
            _isModified = false;
            _searchRegex = false;
            _searchCaseSensitive = false;
            _searchWholeWord = false;
        }

        private void SetupEventHandlers()
        {
            this.FormClosing += EditorForm_FormClosing;
            _editor.TextChanged += Editor_TextChanged;
            _editor.SelectionChanged += Editor_SelectionChanged;
            _editor.VScroll += Editor_VScroll;
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _editor.KeyDown += Editor_KeyDown;
        }
        #endregion

        #region 菜单和工具栏
        private void CreateMenuStrip()
        {
            _menuStrip = new MenuStrip();

            // 文件菜单
            var fileMenu = new ToolStripMenuItem("文件(&F)");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("新建(&N)", null, (s, e) => NewFile(), Keys.Control | Keys.N),
                new ToolStripMenuItem("打开(&O)", null, (s, e) => OpenFile(), Keys.Control | Keys.O),
                new ToolStripMenuItem("保存(&S)", null, (s, e) => SaveFile(), Keys.Control | Keys.S),
                new ToolStripMenuItem("另存为(&A)", null, (s, e) => SaveFileAs()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("退出(&X)", null, (s, e) => Close())
            });

            // 编辑菜单
            var editMenu = new ToolStripMenuItem("编辑(&E)");
            editMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("撤销(&U)", null, (s, e) => Undo(), Keys.Control | Keys.Z),
                new ToolStripMenuItem("重做(&R)", null, (s, e) => Redo(), Keys.Control | Keys.Y),
                new ToolStripSeparator(),
                new ToolStripMenuItem("剪切(&T)", null, (s, e) => Cut(), Keys.Control | Keys.X),
                new ToolStripMenuItem("复制(&C)", null, (s, e) => Copy(), Keys.Control | Keys.C),
                new ToolStripMenuItem("粘贴(&P)", null, (s, e) => Paste(), Keys.Control | Keys.V),
                new ToolStripMenuItem("删除(&D)", null, (s, e) => Delete(), Keys.Delete),
                new ToolStripSeparator(),
                new ToolStripMenuItem("全选(&A)", null, (s, e) => SelectAll(), Keys.Control | Keys.A)
            });

            // 搜索菜单
            var searchMenu = new ToolStripMenuItem("搜索(&S)");
            searchMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("查找(&F)", null, (s, e) => ShowFindDialog(), Keys.Control | Keys.F),
                new ToolStripMenuItem("替换(&R)", null, (s, e) => ShowReplaceDialog(), Keys.Control | Keys.H),
                new ToolStripMenuItem("转到(&G)", null, (s, e) => ShowGoToDialog(), Keys.Control | Keys.G)
            });

            // 视图菜单
            var viewMenu = new ToolStripMenuItem("视图(&V)");
            viewMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("自动换行(&W)", null, (s, e) => ToggleWordWrap()),
                new ToolStripMenuItem("显示行号(&L)", null, (s, e) => ToggleLineNumbers()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("放大(&I)", null, (s, e) => ZoomIn(), Keys.Control | Keys.Add),
                new ToolStripMenuItem("缩小(&O)", null, (s, e) => ZoomOut(), Keys.Control | Keys.Subtract)
            });

            // 编码菜单
            var encodingMenu = new ToolStripMenuItem("编码(&E)");
            foreach (var enc in Encoding.GetEncodings())
            {
                var encoding = enc.GetEncoding();
                var menuItem = new ToolStripMenuItem(encoding.EncodingName, null, (s, e) => {
                    _currentEncoding = encoding;
                    ReloadFile();
                });
                encodingMenu.DropDownItems.Add(menuItem);
            }

            _menuStrip.Items.AddRange(new ToolStripItem[] {
                fileMenu, editMenu, searchMenu, viewMenu, encodingMenu
            });
        }

        private void CreateToolStrip()
        {
            _toolStrip = new ToolStrip();

            // 添加工具栏按钮
            var newButton = new ToolStripButton("新建", null, (s, e) => NewFile());
            var openButton = new ToolStripButton("打开", null, (s, e) => OpenFile());
            var saveButton = new ToolStripButton("保存", null, (s, e) => SaveFile());
            var undoButton = new ToolStripButton("撤销", null, (s, e) => Undo());
            var redoButton = new ToolStripButton("重做", null, (s, e) => Redo());
            var findButton = new ToolStripButton("查找", null, (s, e) => ShowFindDialog());
            var replaceButton = new ToolStripButton("替换", null, (s, e) => ShowReplaceDialog());

            _toolStrip.Items.AddRange(new ToolStripItem[] {
                newButton, openButton, saveButton,
                new ToolStripSeparator(),
                undoButton, redoButton,
                new ToolStripSeparator(),
                findButton, replaceButton
            });
        }

        private void CreateStatusStrip()
        {
            _statusStrip = new StatusStrip();

            _encodingLabel = new ToolStripStatusLabel();
            _positionLabel = new ToolStripStatusLabel();
            _modifiedLabel = new ToolStripStatusLabel();
            _lineEndingLabel = new ToolStripStatusLabel();

            _statusStrip.Items.AddRange(new ToolStripItem[] {
                _encodingLabel, _positionLabel, _modifiedLabel, _lineEndingLabel
            });

            UpdateStatusBar();
        }
        #endregion

        #region 文件操作
        private void NewFile()
        {
            if (CheckSave())
            {
                _editor.Clear();
                _fileName = null;
                _isModified = false;
                _undoStack.Clear();
                _redoStack.Clear();
                UpdateTitle();
            }
        }

        private void OpenFile()
        {
            if (CheckSave())
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "文本文件|*.txt|所有文件|*.*";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        FileName = dialog.FileName;
                    }
                }
            }
        }

        private void LoadFile()
        {
            try
            {
                if (File.Exists(_fileName))
                {
                    // 保存原始文本用于比较
                    _originalText = File.ReadAllText(_fileName, _currentEncoding);
                    _editor.Text = _originalText;
                    _isModified = false;
                    _undoStack.Clear();
                    _redoStack.Clear();
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                SaveFileAs();
                return;
            }

            try
            {
                File.WriteAllText(_fileName, _editor.Text, _currentEncoding);
                _isModified = false;
                _originalText = _editor.Text;
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFileAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "文本文件|*.txt|所有文件|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _fileName = dialog.FileName;
                    SaveFile();
                    UpdateTitle();
                }
            }
        }

        private void ReloadFile()
        {
            if (!string.IsNullOrEmpty(_fileName))
            {
                LoadFile();
            }
        }

        private bool CheckSave()
        {
            if (_isModified)
            {
                var result = MessageBox.Show(
                    "文件已修改，是否保存？",
                    "保存确认",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        SaveFile();
                        return true;
                    case DialogResult.No:
                        return true;
                    default:
                        return false;
                }
            }
            return true;
        }
        #endregion

        #region 编辑操作
        private void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var item = _undoStack[_undoStack.Count - 1];
                _undoStack.RemoveAt(_undoStack.Count - 1);
                _redoStack.Add(new UndoRedoItem(_editor.Text, _editor.SelectionStart));
                _editor.Text = item.Text;
                _editor.SelectionStart = item.CaretPosition;
                UpdateStatusBar();
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var item = _redoStack[_redoStack.Count - 1];
                _redoStack.RemoveAt(_redoStack.Count - 1);
                _undoStack.Add(new UndoRedoItem(_editor.Text, _editor.SelectionStart));
                _editor.Text = item.Text;
                _editor.SelectionStart = item.CaretPosition;
                UpdateStatusBar();
            }
        }

        private void Cut()
        {
            if (_editor.SelectionLength > 0)
            {
                _editor.Cut();
            }
        }

        private void Copy()
        {
            if (_editor.SelectionLength > 0)
            {
                _editor.Copy();
            }
        }

        private void Paste()
        {
            if (Clipboard.ContainsText())
            {
                _editor.Paste();
            }
        }

        private void Delete()
        {
            if (_editor.SelectionLength > 0)
            {
                _editor.SelectedText = string.Empty;
            }
        }

        private void SelectAll()
        {
            _editor.SelectAll();
        }
        #endregion

        #region 搜索和替换
        private void ShowFindDialog()
        {
            using (var dialog = new FindDialog())
            {
                dialog.SearchPattern = _searchPattern;
                dialog.UseRegex = _searchRegex;
                dialog.CaseSensitive = _searchCaseSensitive;
                dialog.WholeWord = _searchWholeWord;
                dialog.SearchHistory = _searchHistory;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _searchPattern = dialog.SearchPattern;
                    _searchRegex = dialog.UseRegex;
                    _searchCaseSensitive = dialog.CaseSensitive;
                    _searchWholeWord = dialog.WholeWord;

                    if (!string.IsNullOrEmpty(_searchPattern))
                    {
                        FindNext();
                        if (!_searchHistory.Contains(_searchPattern))
                        {
                            _searchHistory.Add(_searchPattern);
                        }
                    }
                }
            }
        }

        private void ShowReplaceDialog()
        {
            using (var dialog = new ReplaceDialog())
            {
                dialog.SearchPattern = _searchPattern;
                dialog.UseRegex = _searchRegex;
                dialog.CaseSensitive = _searchCaseSensitive;
                dialog.WholeWord = _searchWholeWord;
                dialog.SearchHistory = _searchHistory;
                dialog.ReplaceHistory = _replaceHistory;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _searchPattern = dialog.SearchPattern;
                    _searchRegex = dialog.UseRegex;
                    _searchCaseSensitive = dialog.CaseSensitive;
                    _searchWholeWord = dialog.WholeWord;

                    if (!string.IsNullOrEmpty(dialog.ReplacePattern))
                    {
                        ReplaceAll(dialog.ReplacePattern);
                        if (!_replaceHistory.Contains(dialog.ReplacePattern))
                        {
                            _replaceHistory.Add(dialog.ReplacePattern);
                        }
                    }
                }
            }
        }

        private void ShowGoToDialog()
        {
            using (var dialog = new GoToDialog())
            {
                dialog.MaxLineNumber = _editor.Lines.Length;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    GoToLine(dialog.LineNumber);
                }
            }
        }

        private void FindNext()
        {
            if (string.IsNullOrEmpty(_searchPattern)) return;

            string text = _editor.Text;
            int startIndex = _editor.SelectionStart + _editor.SelectionLength;

            if (_searchRegex)
            {
                var options = RegexOptions.None;
                if (!_searchCaseSensitive) options |= RegexOptions.IgnoreCase;
                var regex = new Regex(_searchPattern, options);
                var match = regex.Match(text, startIndex);
                if (!match.Success && startIndex > 0)
                {
                    match = regex.Match(text, 0); // 从头开始搜索
                }
                if (match.Success)
                {
                    _editor.Select(match.Index, match.Length);
                    _editor.ScrollToCaret();
                }
            }
            else
            {
                StringComparison comparison = _searchCaseSensitive ?
                    StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int index = text.IndexOf(_searchPattern, startIndex, comparison);
                if (index == -1 && startIndex > 0)
                {
                    index = text.IndexOf(_searchPattern, 0, comparison); // 从头开始搜索
                }
                if (index != -1)
                {
                    _editor.Select(index, _searchPattern.Length);
                    _editor.ScrollToCaret();
                }
            }
        }

        private void ReplaceAll(string replacePattern)
        {
            if (string.IsNullOrEmpty(_searchPattern)) return;

            string text = _editor.Text;
            if (_searchRegex)
            {
                var options = RegexOptions.None;
                if (!_searchCaseSensitive) options |= RegexOptions.IgnoreCase;
                text = Regex.Replace(text, _searchPattern, replacePattern, options);
            }
            else
            {
                StringComparison comparison = _searchCaseSensitive ?
                    StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                text = text.Replace(_searchPattern, replacePattern, comparison);
            }

            _editor.Text = text;
        }

        private void GoToLine(int lineNumber)
        {
            if (lineNumber > 0 && lineNumber <= _editor.Lines.Length)
            {
                int position = _editor.GetFirstCharIndexFromLine(lineNumber - 1);
                _editor.SelectionStart = position;
                _editor.ScrollToCaret();
            }
        }
        #endregion

        #region 视图操作
        private void ToggleWordWrap()
        {
            _editor.WordWrap = !_editor.WordWrap;
        }

        private void ToggleLineNumbers()
        {
            _lineNumberPanel.Visible = !_lineNumberPanel.Visible;
            if (_lineNumberPanel.Visible)
            {
                UpdateLineNumbers();
            }
        }

        private void ZoomIn()
        {
            if (_editor.ZoomFactor < 3.0f)
            {
                _editor.ZoomFactor += 0.1f;
            }
        }

        private void ZoomOut()
        {
            if (_editor.ZoomFactor > 0.5f)
            {
                _editor.ZoomFactor -= 0.1f;
            }
        }
        #endregion

        #region 事件处理
        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !CheckSave();
        }

        private void Editor_TextChanged(object sender, EventArgs e)
        {
            if (!_isModified && _editor.Text != _originalText)
            {
                IsModified = true;
            }

            if (_undoStack.Count >= _maxUndoSteps)
            {
                _undoStack.RemoveAt(0);
            }
            _undoStack.Add(new UndoRedoItem(_editor.Text, _editor.SelectionStart));
            _redoStack.Clear();

            UpdateLineNumbers();
        }

        private void Editor_SelectionChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void Editor_VScroll(object sender, EventArgs e)
        {
            _lineNumberPanel.Invalidate();
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowFindDialog();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                ShowReplaceDialog();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                ShowGoToDialog();
                e.Handled = true;
            }
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (_autoSave && _isModified && !string.IsNullOrEmpty(_fileName))
            {
                SaveFile();
            }
        }

        private void LineNumberPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_editor == null) return;

            int firstVisibleLine = _editor.GetLineFromCharIndex(_editor.GetCharIndexFromPosition(new Point(0, 0)));
            int lastVisibleLine = _editor.GetLineFromCharIndex(_editor.GetCharIndexFromPosition(new Point(0, _editor.Height)));

            using (var font = new Font(_editor.Font.FontFamily, _editor.Font.Size))
            {
                for (int i = firstVisibleLine; i <= lastVisibleLine + 1; i++)
                {
                    if (i < _editor.Lines.Length)
                    {
                        int lineY = _editor.GetPositionFromCharIndex(_editor.GetFirstCharIndexFromLine(i)).Y;
                        string lineNumber = (i + 1).ToString();
                        e.Graphics.DrawString(lineNumber, font, Brushes.Gray,
                            _lineNumberPanel.Width - e.Graphics.MeasureString(lineNumber, font).Width - 5,
                            lineY);
                    }
                }
            }
        }
        #endregion

        #region 辅助方法
        private void UpdateTitle()
        {
            string title = "文本编辑器";
            if (!string.IsNullOrEmpty(_fileName))
            {
                title += $" - {Path.GetFileName(_fileName)}";
            }
            if (_isModified)
            {
                title += "*";
            }
            this.Text = title;
        }

        private void UpdateStatusBar()
        {
            int line = _editor.GetLineFromCharIndex(_editor.SelectionStart) + 1;
            int column = _editor.SelectionStart - _editor.GetFirstCharIndexFromLine(line - 1) + 1;
            _positionLabel.Text = $"行 {line}, 列 {column}";
            _encodingLabel.Text = $"编码: {_currentEncoding.EncodingName}";
            _modifiedLabel.Text = _isModified ? "已修改" : "未修改";
            _lineEndingLabel.Text = "CRLF"; // 可以添加行尾检测逻辑
        }

        private void UpdateLineNumbers()
        {
            if (_lineNumberPanel.Visible)
            {
                int maxLineNumber = _editor.Lines.Length;
                int width = (maxLineNumber.ToString().Length * 10) + 10;
                if (width != _lineNumberPanel.Width)
                {
                    _lineNumberPanel.Width = width;
                }
                _lineNumberPanel.Invalidate();
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoSaveTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class UndoRedoItem
    {
        public string Text { get; }
        public int CaretPosition { get; }

        public UndoRedoItem(string text, int caretPosition)
        {
            Text = text;
            CaretPosition = caretPosition;
        }
    }

    public class CaretPosition
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class FindDialog : Form
    {
        public string SearchPattern { get; set; }
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public List<string> SearchHistory { get; set; }

        public FindDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 实现查找对话框的界面
        }
    }

    public class ReplaceDialog : Form
    {
        public string SearchPattern { get; set; }
        public string ReplacePattern { get; set; }
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public List<string> SearchHistory { get; set; }
        public List<string> ReplaceHistory { get; set; }

        public ReplaceDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 实现替换对话框的界面
        }
    }

    public class GoToDialog : Form
    {
        public int LineNumber { get; set; }
        public int MaxLineNumber { get; set; }

        public GoToDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 实现跳转对话框的界面
        }
    }
} 