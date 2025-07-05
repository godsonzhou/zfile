using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace zfile.Forms
{
	public class ViewerForm : Form
	{
		#region 字段和属性
		private string _fileName;
		private List<string> _fileList;
		private int _activeFileIndex;
		private bool _isAnimation;
		private bool _isImage;
		private bool _isPlugin;
		private bool _isFullScreen;
		private bool _isTextMode;
		private Encoding _currentEncoding;
		private float _zoomFactor = 1.0f;
		private Point _lastMousePosition;
		private bool _isDragging;
		private Image _currentImage;
		private WlxModuleList _pluginList;
		private WlxModule _currentPlugin;
		private nint _pluginWindow;

		// 查看模式枚举
		private enum ViewMode
		{
			Text,
			Hex,
			Media
		}

		#region 编辑功能
		private string _lastSearchText = "";
		private bool _lastMatchCase = false;
		private bool _lastWholeWord = false;
		private bool _lastSearchUp = false;
		private bool _lastHexSearch = false;

		/// <summary>
		/// 复制选中的文本到剪贴板
		/// </summary>
		private void CopySelectedText()
		{
			if (_isPlugin && _pluginWindow != nint.Zero && _currentPlugin != null)
			{
				// 使用插件的复制功能
				_currentPlugin.CallListSendCommand(_pluginWindow, 1, 0); // lc_copy = 1
			}
			else if (_currentViewMode == ViewMode.Text && _textViewer.SelectionLength > 0)
			{
				// 复制文本查看器中的选中文本
				string selectedText = _textViewer.SelectedText;
				// 将所有字符串结束标志 (#0) 转换为空格 (#32)
				selectedText = selectedText.Replace('\0', ' ');
				Clipboard.SetText(selectedText);
			}
			else if (_currentViewMode == ViewMode.Hex && _hexViewer.SelectionLength > 0)
			{
				// 复制十六进制查看器中的选中文本
				string selectedText = _hexViewer.SelectedText;
				// 将所有字符串结束标志 (#0) 转换为空格 (#32)
				selectedText = selectedText.Replace('\0', ' ');
				Clipboard.SetText(selectedText);
			}
		}

		/// <summary>
		/// 全选文本
		/// </summary>
		private void SelectAllText()
		{
			if (_isPlugin && _pluginWindow != nint.Zero && _currentPlugin != null)
			{
				// 使用插件的全选功能
				_currentPlugin.CallListSendCommand(_pluginWindow, 3, 0); // lc_selectall = 3
			}
			else if (_currentViewMode == ViewMode.Text)
			{
				// 全选文本查看器中的文本
				_textViewer.SelectAll();
			}
			else if (_currentViewMode == ViewMode.Hex)
			{
				// 全选十六进制查看器中的文本
				_hexViewer.SelectAll();
			}
		}

		/// <summary>
		/// 显示查找对话框
		/// </summary>
		private void ShowFindDialog()
		{
			if (_isPlugin && _pluginWindow != nint.Zero && _currentPlugin != null)
			{
				// 尝试使用插件的查找对话框
				int result = _currentPlugin.CallListSearchDialog(_pluginWindow, 0);
				if (result == WlxConstants.LISTPLUGIN_OK)
					return; // 插件处理了查找对话框
			}

			// 使用自定义查找对话框
			string initialSearchText = "";
			
			// 如果有选中的文本，使用它作为初始搜索文本
			if (_currentViewMode == ViewMode.Text && _textViewer.SelectionLength > 0)
			{
				initialSearchText = _textViewer.SelectedText;
			}
			else if (_currentViewMode == ViewMode.Hex && _hexViewer.SelectionLength > 0)
			{
				initialSearchText = _hexViewer.SelectedText;
			}
			else if (!string.IsNullOrEmpty(_lastSearchText))
			{
				initialSearchText = _lastSearchText;
			}

			using (var dialog = new SearchDialog(initialSearchText))
			{
				// 设置上次的搜索选项
				if (!string.IsNullOrEmpty(_lastSearchText))
				{
					dialog._matchCaseCheckBox.Checked = _lastMatchCase;
					dialog._wholeWordCheckBox.Checked = _lastWholeWord;
					dialog._searchUpCheckBox.Checked = _lastSearchUp;
					dialog._hexSearchCheckBox.Checked = _lastHexSearch;
				}

				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					// 保存搜索选项
					_lastSearchText = dialog.SearchText;
					_lastMatchCase = dialog.MatchCase;
					_lastWholeWord = dialog.WholeWord;
					_lastSearchUp = dialog.SearchUp;
					_lastHexSearch = dialog.HexSearch;

					// 执行搜索
					PerformSearch(dialog.GetProcessedSearchText(), dialog.GetSearchParameters(), _lastHexSearch);
				}
			}
		}

		/// <summary>
		/// 查找下一个匹配项
		/// </summary>
		/// <param name="reverse">是否反向搜索</param>
		private void FindNext(bool reverse = false)
		{
			if (string.IsNullOrEmpty(_lastSearchText))
			{
				// 如果没有上次的搜索文本，显示查找对话框
				ShowFindDialog();
				return;
			}

			if (_isPlugin && _pluginWindow != nint.Zero && _currentPlugin != null && !_lastHexSearch)
			{
				// 使用插件的查找功能
				int searchParameter = 0; // 继续搜索

				if (_lastMatchCase)
					searchParameter |= 2; // lcs_matchcase

				if (_lastWholeWord)
					searchParameter |= 4; // lcs_wholewords

				if (_lastSearchUp || reverse)
					searchParameter |= 8; // lcs_backwards

				_currentPlugin.CallListSearchText(_pluginWindow, _lastSearchText, searchParameter);
			}
			else
			{
				// 使用内置查找功能
				int searchParameter = 0; // 继续搜索

				if (_lastMatchCase)
					searchParameter |= 2; // lcs_matchcase

				if (_lastWholeWord)
					searchParameter |= 4; // lcs_wholewords

				if (_lastSearchUp || reverse)
					searchParameter |= 8; // lcs_backwards

				PerformSearch(_lastSearchText, searchParameter, _lastHexSearch);
			}
		}

		/// <summary>
		/// 执行搜索
		/// </summary>
		/// <param name="searchText">搜索文本</param>
		/// <param name="searchParameter">搜索参数</param>
		private void PerformSearch(string searchText, int searchParameter, bool hexsearch = false)
		{
			if (_isPlugin && _pluginWindow != nint.Zero && _currentPlugin != null && !hexsearch)
			{
				// 使用插件的搜索功能
				if (WlxConstants.LISTPLUGIN_OK == _currentPlugin.CallListSearchText(_pluginWindow, searchText, searchParameter))
					return;
			}

			bool matchCase = (searchParameter & 2) != 0; // lcs_matchcase
			bool wholeWord = (searchParameter & 4) != 0; // lcs_wholewords
			bool backwards = (searchParameter & 8) != 0; // lcs_backwards
			bool findFirst = (searchParameter & 1) != 0; // lcs_findfirst

			if (_currentViewMode == ViewMode.Text)
			{
				// 在文本查看器中搜索
				RichTextBoxFinds options = RichTextBoxFinds.None;

				if (matchCase)
					options |= RichTextBoxFinds.MatchCase;

				if (wholeWord)
					options |= RichTextBoxFinds.WholeWord;

				if (backwards)
					options |= RichTextBoxFinds.Reverse;

				int startPosition;
				if (findFirst)
				{
					// 从当前行的开始位置搜索
					int lineIndex = _textViewer.GetLineFromCharIndex(_textViewer.SelectionStart);
					startPosition = _textViewer.GetFirstCharIndexFromLine(lineIndex);
				}
				else if (backwards)
				{
					// 反向搜索时，从选择的开始位置搜索
					startPosition = _textViewer.SelectionStart;
				}
				else
				{
					// 正向搜索时，从选择的结束位置搜索
					startPosition = _textViewer.SelectionStart + _textViewer.SelectionLength;
				}

				int foundIndex = _textViewer.Find(searchText, startPosition, options);

				if (foundIndex == -1 && !findFirst)
				{
					// 如果没有找到，从头/尾开始搜索
					foundIndex = _textViewer.Find(searchText, backwards ? _textViewer.TextLength : 0, options);
				}

				if (foundIndex != -1)
				{
					// 找到了匹配项，滚动到该位置
					_textViewer.Select(foundIndex, searchText.Length);
					_textViewer.ScrollToCaret();
				}
				else
				{
					MessageBox.Show($"找不到 \"{searchText}\"", "查找", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			else if (_currentViewMode == ViewMode.Hex)
			{
				// 在十六进制查看器中搜索
				// 十六进制查看器的搜索实现类似于文本查看器
				RichTextBoxFinds options = RichTextBoxFinds.None;

				if (matchCase)
					options |= RichTextBoxFinds.MatchCase;

				if (wholeWord)
					options |= RichTextBoxFinds.WholeWord;

				if (backwards)
					options |= RichTextBoxFinds.Reverse;

				int startPosition;
				if (findFirst)
				{
					// 从当前行的开始位置搜索
					int lineIndex = _hexViewer.GetLineFromCharIndex(_hexViewer.SelectionStart);
					startPosition = _hexViewer.GetFirstCharIndexFromLine(lineIndex);
				}
				else if (backwards)
				{
					// 反向搜索时，从选择的开始位置搜索
					startPosition = _hexViewer.SelectionStart;
				}
				else
				{
					// 正向搜索时，从选择的结束位置搜索
					startPosition = _hexViewer.SelectionStart + _hexViewer.SelectionLength;
				}

				int foundIndex = _hexViewer.Find(searchText, startPosition, options);

				if (foundIndex == -1 && !findFirst)
				{
					// 如果没有找到，从头/尾开始搜索
					foundIndex = _hexViewer.Find(searchText, backwards ? _hexViewer.TextLength : 0, options);
				}

				if (foundIndex != -1)
				{
					// 找到了匹配项，滚动到该位置
					_hexViewer.Select(foundIndex, searchText.Length);
					_hexViewer.ScrollToCaret();
				}
				else
				{
					MessageBox.Show($"找不到 \"{searchText}\"", "查找", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}
		#endregion

		private ViewMode _currentViewMode = ViewMode.Text;

		// 控件
		private Panel _mainPanel;
		private Panel _imagePanel;
		private Panel _textPanel;
		private Panel _hexPanel;
		private Panel container;
		private RichTextBox _textViewer;
		private RichTextBox _hexViewer;
		private PictureBox _imageViewer;
		private ToolStrip _toolStrip;
		private StatusStrip _statusStrip;
		private MenuStrip _menuStrip;
		private Timer _animationTimer;
		private Timer _screenshotTimer;
		//private bool isPluginLoaded;

		public string FileName
		{
			get => _fileName;
			set
			{
				_fileName = value;
				UpdateTitle();
				LoadFile();
			}
		}

		#endregion

		#region 构造函数和初始化
		public ViewerForm()
		{
			init();
		}
		public ViewerForm(string fileName, WlxModuleList wlxModuleList)
		{
			_pluginList = wlxModuleList;
			init();
			FileName = fileName;
		}
		public ViewerForm(List<string> files, WlxModuleList wlxModuleList)
		{
			_pluginList = wlxModuleList;
			init();
			_fileList.AddRange(files);
			FileName = _fileList[0];
		}
		private void init()
		{
			InitializePlugins();        //load all wlx plugins
			InitializeComponent();
			InitializeFileList();
			SetupEventHandlers();
		}

		private void InitializeComponent()
		{
			// 设置窗体属性
			Text = "文件查看器";
			Size = new Size(800, 600);
			StartPosition = FormStartPosition.CenterScreen;

			// 创建主面板
			_mainPanel = new Panel
			{
				Dock = DockStyle.Fill
			};

			// 创建图像查看面板
			_imagePanel = new Panel
			{
				Dock = DockStyle.Fill,
				Visible = false
			};

			_imageViewer = new PictureBox
			{
				Dock = DockStyle.Fill,
				SizeMode = PictureBoxSizeMode.Zoom
			};
			_imagePanel.Controls.Add(_imageViewer);

			// 创建文本查看面板
			_textPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Visible = false
			};

			_textViewer = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				WordWrap = true,
				Font = new Font("Consolas", 10)
			};
			_textPanel.Controls.Add(_textViewer);

			// 创建16进制查看面板
			_hexPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Visible = false
			};

			_hexViewer = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				WordWrap = false,
				Font = new Font("Consolas", 10),
				BackColor = Color.White,
				ForeColor = Color.Black
			};
			_hexPanel.Controls.Add(_hexViewer);

			// 创建工具栏
			CreateToolStrip();

			// 创建菜单栏
			CreateMenuStrip();

			// 创建状态栏
			CreateStatusStrip();

			// 创建隐藏的容器面板
			container = new Panel
			{
				Dock = DockStyle.Fill,
				Visible = false
			};
		
			// 添加控件到窗体
			_mainPanel.Controls.Add(_imagePanel);
			_mainPanel.Controls.Add(_textPanel);
			_mainPanel.Controls.Add(_hexPanel);
			_mainPanel.Controls.Add(container); //must be add after all subpanel, otherwise the plugin container will be set to a very small size when double click the window
			Controls.Add(_mainPanel);
			Controls.Add(_toolStrip);
			Controls.Add(_menuStrip);
			Controls.Add(_statusStrip);

			// 初始化计时器
			_animationTimer = new Timer { Interval = 100 };
			_screenshotTimer = new Timer { Interval = 3000 };

			container.SetBounds(_mainPanel.Bounds.X, _mainPanel.Bounds.Y, _mainPanel.Bounds.Width, _mainPanel.Bounds.Height);
		}

		public WlxModuleList InitializePlugins()
		{
			string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins\\wlx");
			_pluginList.LoadModulesFromDirectory(pluginPath);
			return _pluginList;
		}

		private void InitializeFileList()
		{
			_fileList = [];
			_currentEncoding = Encoding.Default;
		}

		private void SetupEventHandlers()
		{
			KeyDown += ViewerForm_KeyDown;
			_imageViewer.MouseDown += ImageViewer_MouseDown;
			_imageViewer.MouseMove += ImageViewer_MouseMove;
			_imageViewer.MouseUp += ImageViewer_MouseUp;
			_animationTimer.Tick += AnimationTimer_Tick;
			_screenshotTimer.Tick += ScreenshotTimer_Tick;
		}
		#endregion

		#region 文件加载和显示
		private void LoadFile()
		{
			if (string.IsNullOrEmpty(_fileName) || !File.Exists(_fileName))
			{
				MessageBox.Show("文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				// 清理当前资源
				CleanupCurrentView();

				// 检查是否有插件可以处理
				int tryModuleIdx = -1; //依次尝试所有的module
				while (tryModuleIdx < _pluginList._modules.Count) {
					var _currentPlugin = _pluginList.FindModuleForFile(_fileName, ref tryModuleIdx);
					if (_currentPlugin != null)
					{
						if (LoadWithPlugin(_currentPlugin))		//should consider load fail
							return;
					}
				}
				// 检查文件类型
				string extension = Path.GetExtension(_fileName).ToLower();
				if (IsImageFile(extension))
				{
					_isImage = true;
					LoadImage();
					// 如果是图像文件，自动切换到多媒体模式
					if (_currentViewMode != ViewMode.Media)
						SwitchViewMode(ViewMode.Media);
					else
						_imagePanel.Visible = true;
				}
				else
				{
					// 根据当前模式加载文件
					switch (_currentViewMode)
					{
						case ViewMode.Text:
							LoadText();
							break;
						case ViewMode.Hex:
							LoadHex();
							break;
						case ViewMode.Media:
							// 如果不是图像文件但选择了多媒体模式，默认使用文本模式
							SwitchViewMode(ViewMode.Text);
							break;
					}
				}
				setButtonStateForImageMode(_isImage);
				UpdateStatusBar();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void setButtonStateForImageMode(bool flag)
		{
			UIControlManager.GetToolStripButtonByName("放大", _toolStrip.Items).Visible = flag;
			UIControlManager.GetToolStripButtonByName("缩小", _toolStrip.Items).Visible = flag;
			UIControlManager.GetToolStripButtonByName("旋转", _toolStrip.Items).Visible = flag;
			UIControlManager.GetToolStripButtonByName("全屏", _toolStrip.Items).Visible = flag;
		}
		private void SetMenuItemCheckedState(string pluginName, bool flag)
		{
			var menuitem = UIControlManager.GetToolStripMenuItemByName(pluginName, _menuStrip.Items);
			if (menuitem != null)
				menuitem.Checked = flag;
		}
		private bool LoadWithPlugin(WlxModule plugin)
		{
			//if(_currentPlugin != null)
				//SetMenuItemCheckedState(_currentPlugin.Name, false);
			setCheckedMenuStateByNameToId("模式");    //关闭模式菜单下所有勾选
			setCheckedMenuStateByNameToId("插件");

			_isPlugin = true;

			// 隐藏所有内置查看器面板
			_textPanel.Visible = false;
			_hexPanel.Visible = false;
			_imagePanel.Visible = false;

			// HIDE ALL SUBPANEL IF SWITCH PLUG
			foreach (var p in _mainPanel.Controls)
			{
				if (p is Panel pnl) pnl.Visible = false;
			}

			if (_currentPlugin == null)
				_currentPlugin = plugin;
			else if (_currentPlugin == plugin)
			{ }
			else
			{
				if (_pluginWindow != IntPtr.Zero)
				{
					_currentPlugin.CallListCloseWindow(_pluginWindow);  //关闭原有plugin window
					_pluginWindow = IntPtr.Zero;
				}
				_currentPlugin = plugin;
			}

			// 调用插件前执行, 在 C# 调用插件前设置低地址分配偏好，保持32位插件兼容性，解决fileinfo.wlx64报错"映射文件地址>4GB"
			/*
			 * 因为在FILEINFO.WLX64插件中使用了如下判断：
			 * if (((DWORD64)m_pMemoryMappedFileBase) >> 32)
				{
					UnmapViewOfFile( m_pMemoryMappedFileBase );
					m_pMemoryMappedFileBase = 0;
					CloseHandle(m_hFileMapping);
					m_hFileMapping = 0;
					CloseHandle(m_hFile);
					m_hFile = INVALID_HANDLE_VALUE;
					m_errCode = errMMF_MapView;
					AfxMessageBox("Address of MappedFile > 4GB", MB_OK|MB_ICONERROR);
					return;	
				}
			 */
			//try
			//{
			//	const int SET_WS_SET = 0x1;
			//	NativeMethods.SetProcessWorkingSetSizeEx(
			//		Process.GetCurrentProcess().Handle,
			//		(IntPtr)(100 * 1024 * 1024),  // 100MB
			//		(IntPtr)(300 * 1024 * 1024),  // 300MB
			//		SET_WS_SET
			//	);
				
			//	// 可选：预先分配低地址内存1MB，帮助确保后续分配在低地址空间
			//	IntPtr lowMem = Marshal.AllocHGlobal(0x100000);
			//}
			//catch (Exception ex)
			//{
			//	// 如果设置失败，记录但不阻止插件加载
			//	System.Diagnostics.Debug.WriteLine($"设置低地址分配偏好失败: {ex.Message}");
			//}
	
			// 传递容器面板的句柄作为父窗口
			if(_pluginWindow == IntPtr.Zero)
				_pluginWindow = _currentPlugin.CallListLoad(container.Handle, _fileName, WlxConstants.LISTPLUGIN_SHOW);
			//IntPtr bmp = IntPtr.Zero;
			//if(_pluginWindow == IntPtr.Zero)
			//	_pluginWindow = _currentPlugin.CallListGetPreviewBitmap(_fileName, _mainPanel.Bounds.Width, _mainPanel.Bounds.Height, bmp);
			//_pluginWindow = _currentPlugin.CallListLoad(this.Handle, _fileName, WlxConstants.LISTPLUGIN_SHOW);
			if (_pluginWindow != nint.Zero)
			{
				// 设置窗口样式为子窗口
				NativeMethods.SetParent(_pluginWindow, container.Handle);
				NativeMethods.SetWindowLong(_pluginWindow, NativeMethods.GWL_STYLE, NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD);

				// 调整窗口位置和大小
				SetPluginWindowBounds(container);
				container.Visible = true;
				SetMenuItemCheckedState(_currentPlugin.Name, true); //将相应的插件菜单项设为checked状态
				setButtonStateForImageMode(false);
				return true;
			}
			return false;
		}
		// 在窗体Resize事件中更新位置
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (_pluginWindow != nint.Zero)
			{
				//var container = _mainPanel.Controls.OfType<Panel>().FirstOrDefault();
				foreach (var container in _mainPanel.Controls.OfType<Panel>())
					SetPluginWindowBounds(container);
			}
		}
		private void LoadImage()
		{
			_isImage = true;
			_imagePanel.Visible = true;
			_textPanel.Visible = false;
			_hexPanel.Visible = false;

			using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
			{
				_currentImage = Image.FromStream(stream);
				_imageViewer.Image = _currentImage;

				// 检查是否是动画GIF
				_isAnimation = IsAnimatedGif(_currentImage);
				if (_isAnimation)
					_animationTimer.Start();
			}
		}

		private void LoadText()
		{
			_isTextMode = true;
			_textPanel.Visible = true;
			_hexPanel.Visible = false;
			_imagePanel.Visible = false;

			try
			{
				string content = File.ReadAllText(_fileName, _currentEncoding);
				_textViewer.Text = content;
			}
			catch
			{
				// 如果使用当前编码失败，尝试自动检测编码
				_currentEncoding = DetectEncoding(_fileName);
				string content = File.ReadAllText(_fileName, _currentEncoding);
				_textViewer.Text = content;
			}
		}

		private void CleanupCurrentView()
		{
			if (_currentImage != null)
			{
				_currentImage.Dispose();
				_currentImage = null;
			}

			if (_pluginWindow != nint.Zero)
			{
				_currentPlugin?.CallListCloseWindow(_pluginWindow);
				_pluginWindow = nint.Zero;
			}

			_isImage = false;
			_isPlugin = false;
			_isAnimation = false;
			_animationTimer.Stop();
		}
		#endregion

		#region 工具栏和菜单
		private void CreateToolStrip()
		{
			_toolStrip = new ToolStrip();

			// 添加工具栏按钮
			var openButton = new ToolStripButton("打开", null, (s, e) => OpenFile());
			var prevButton = new ToolStripButton("上一个文件", null, (s, e) => NavigateFile(-1));
			var nextButton = new ToolStripButton("下一个文件", null, (s, e) => NavigateFile(1));
			var zoomInButton = new ToolStripButton("放大", null, (s, e) => ZoomImage(1.2f));
			var zoomOutButton = new ToolStripButton("缩小", null, (s, e) => ZoomImage(0.8f));
			var rotateButton = new ToolStripButton("旋转", null, (s, e) => RotateImage());
			var fullScreenButton = new ToolStripButton("全屏", null, (s, e) => ToggleFullScreen());

			_toolStrip.Items.AddRange([
				openButton, new ToolStripSeparator(),
				prevButton, nextButton, new ToolStripSeparator(),
				zoomInButton, zoomOutButton, rotateButton, new ToolStripSeparator(),
				fullScreenButton
			]);
		}

		private void CreateMenuStrip()
		{
			_menuStrip = new MenuStrip();

			// 文件菜单
			var fileMenu = new ToolStripMenuItem("文件(&F)");
			fileMenu.DropDownItems.AddRange([
				new ToolStripMenuItem("打开(&O)", null, (s, e) => OpenFile()),
				new ToolStripMenuItem("保存(&S)", null, (s, e) => SaveFile()),
				new ToolStripSeparator(),
				new ToolStripMenuItem("退出(&X)", null, (s, e) => Close())
			]);

			// 编辑菜单
			var editMenu = new ToolStripMenuItem("编辑(&E)");
			editMenu.DropDownItems.AddRange([
				new ToolStripMenuItem("将选择文本复制到剪贴板(&C)", null, (s, e) => CopySelectedText()) { ShortcutKeys = Keys.Control | Keys.C },
				new ToolStripMenuItem("全部选择(&A)", null, (s, e) => SelectAllText()) { ShortcutKeys = Keys.Control | Keys.A },
				new ToolStripSeparator(),
				new ToolStripMenuItem("查找(&F)...", null, (s, e) => ShowFindDialog()) { ShortcutKeys = Keys.F7 },
				new ToolStripMenuItem("查找下一个(&N)", null, (s, e) => FindNext()) { ShortcutKeys = Keys.F5 }
			]);

			// 查看菜单
			var viewMenu = new ToolStripMenuItem("查看(&V)");
			viewMenu.DropDownItems.AddRange([
				new ToolStripMenuItem("放大(&I)", null, (s, e) => ZoomImage(1.2f)),
				new ToolStripMenuItem("缩小(&O)", null, (s, e) => ZoomImage(0.8f)),
				new ToolStripMenuItem("实际大小(&A)", null, (s, e) => ResetZoom()),
				new ToolStripSeparator(),
				new ToolStripMenuItem("全屏(&F)", null, (s, e) => ToggleFullScreen())
			]);

			// 模式菜单
			var modeMenu = new ToolStripMenuItem("模式(&M)") { Name = "模式"};
			var textModeItem = new ToolStripMenuItem("文本(&T)", null, (s, e) => SwitchViewMode(ViewMode.Text));
			var hexModeItem = new ToolStripMenuItem("16进制(&H)", null, (s, e) => SwitchViewMode(ViewMode.Hex));
			var mediaModeItem = new ToolStripMenuItem("多媒体(&M)", null, (s, e) => SwitchViewMode(ViewMode.Media));

			// 默认选中文本模式
			textModeItem.Checked = true;
			modeMenu.DropDownItems.AddRange([textModeItem, hexModeItem, mediaModeItem]);

			// 编码菜单
			var encodingMenu = new ToolStripMenuItem("编码(&E)");
			foreach (var enc in Encoding.GetEncodings())
			{
				var encoding = enc.GetEncoding();
				var menuItem = new ToolStripMenuItem(encoding.EncodingName, null, (s, e) =>
				{
					_currentEncoding = encoding;
					LoadText();
				});
				encodingMenu.DropDownItems.Add(menuItem);
			}

			// plugin menu
			var pluginMenu = new ToolStripMenuItem("插件(&P)") { Name = "插件"};

			// 添加内置查看器选项
			var builtInViewerItem = new ToolStripMenuItem("内置查看器", null, (s, e) =>
			{
				_currentPlugin = null;
				LoadFile();
			});
			pluginMenu.DropDownItems.Add(builtInViewerItem);
			pluginMenu.DropDownItems.Add(new ToolStripSeparator());
			foreach (var plug in _pluginList.Modules)
			{
				var item = new ToolStripMenuItem(plug.Name, null, (s, e) =>
				{
					LoadWithPlugin(plug);
				});
				pluginMenu.DropDownItems.Add(item);
			}
			_menuStrip.Items.AddRange([ fileMenu, editMenu, viewMenu, modeMenu, encodingMenu, pluginMenu ]);
		}

		private void CreateStatusStrip()
		{
			_statusStrip = new StatusStrip();

			var fileInfoLabel = new ToolStripStatusLabel();
			var encodingLabel = new ToolStripStatusLabel();
			var zoomLabel = new ToolStripStatusLabel();

			_statusStrip.Items.AddRange(new ToolStripItem[] {
				fileInfoLabel, encodingLabel, zoomLabel
			});
		}
		#endregion

		#region 事件处理
		private void ViewerForm_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (_isFullScreen)
					ToggleFullScreen();
				else
					Close();
			}
			else if (e.Control && e.KeyCode == Keys.C)
			{
				CopySelectedText();
				e.Handled = true;
			}
			else if (e.Control && e.KeyCode == Keys.A)
			{
				SelectAllText();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.F7)
			{
				ShowFindDialog();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.F5 || e.KeyCode == Keys.F3 || (e.Shift && e.KeyCode == Keys.F7))
			{
				FindNext();
				e.Handled = true;
			}
			else if ((e.Control && e.KeyCode == Keys.F3) || (e.Control && e.KeyCode == Keys.F5))
			{
				FindNext(true); // 反向搜索
				e.Handled = true;
			}
			else if ((e.Shift && e.KeyCode == Keys.F3) || (e.Shift && e.KeyCode == Keys.F5))
			{
				FindNext(true); // 反向搜索
				e.Handled = true;
			}
		}

		private void ImageViewer_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = true;
				_lastMousePosition = e.Location;
			}
		}

		private void ImageViewer_MouseMove(object? sender, MouseEventArgs e)
		{
			if (_isDragging)
			{
				int deltaX = e.X - _lastMousePosition.X;
				int deltaY = e.Y - _lastMousePosition.Y;

				_imageViewer.Left += deltaX;
				_imageViewer.Top += deltaY;

				_lastMousePosition = e.Location;
			}
		}

		private void ImageViewer_MouseUp(object? sender, MouseEventArgs e)
		{
			_isDragging = false;
		}

		private void AnimationTimer_Tick(object? sender, EventArgs e)
		{
			if (_isAnimation && _currentImage != null)
			{
				ImageAnimator.UpdateFrames(_currentImage);
				_imageViewer.Invalidate();
			}
		}

		private void ScreenshotTimer_Tick(object? sender, EventArgs e)
		{
			_screenshotTimer.Stop();
			CaptureScreenshot();
		}
		#endregion

		#region 辅助方法
		private bool IsImageFile(string extension)
		{
			string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
			return Array.IndexOf(imageExtensions, extension) != -1;
		}

		private bool IsAnimatedGif(Image image)
		{
			if (image.RawFormat.Guid == ImageFormat.Gif.Guid)
			{
				foreach (PropertyItem item in image.PropertyItems)
				{
					if (item.Id == 0x5100) // FrameCount
						return BitConverter.ToInt16(item.Value, 0) > 1;
				}
			}
			return false;
		}

		private Encoding DetectEncoding(string fileName)
		{
			using (var reader = new StreamReader(fileName, Encoding.Default, true))
			{
				reader.Peek(); // 触发编码检测
				return reader.CurrentEncoding;
			}
		}

		private void UpdateTitle()
		{
			Text = $"文件查看器 - {Path.GetFileName(_fileName)}";
		}

		private void UpdateStatusBar()
		{
			if (_statusStrip.Items.Count >= 3)
			{
				var fileInfo = _statusStrip.Items[0] as ToolStripStatusLabel;
				var encodingLabel = _statusStrip.Items[1] as ToolStripStatusLabel;
				var zoomLabel = _statusStrip.Items[2] as ToolStripStatusLabel;

				if (fileInfo != null)
				{
					var fileSize = new FileInfo(_fileName).Length;
					fileInfo.Text = $"大小: {FileSystemManager.FormatFileSize(fileSize, true)}";
				}

				if (encodingLabel != null)
				{
					string modeText = "";
					switch (_currentViewMode)
					{
						case ViewMode.Text:
							modeText = "文本模式";
							break;
						case ViewMode.Hex:
							modeText = "16进制模式";
							break;
						case ViewMode.Media:
							modeText = "多媒体模式";
							break;
					}

					if (_isPlugin)
						modeText = "插件模式";

					encodingLabel.Text = $"编码: {_currentEncoding.EncodingName} | {modeText}";
				}

				if (zoomLabel != null && _isImage)
					zoomLabel.Text = $"缩放: {_zoomFactor:P0}";
			}
		}

		private void SetPluginWindowBounds(Panel container)
		{
			if (_pluginWindow != nint.Zero && container != null)
			{
				var bounds = container.ClientRectangle;
				NativeMethods.SetWindowPos(_pluginWindow, nint.Zero, 0, 0, bounds.Width, bounds.Height, NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
			}
		}
		private void SetPluginWindowBounds()
		{
			if (_pluginWindow != nint.Zero)
			{
				var bounds = _mainPanel.ClientRectangle;
				NativeMethods.SetWindowPos(_pluginWindow, nint.Zero,
					bounds.Left, bounds.Top, bounds.Width, bounds.Height,
					NativeMethods.SWP_NOZORDER);
			}
		}
		#endregion

		#region 命令处理
		private void OpenFile()
		{
			using (var dialog = new OpenFileDialog())
			{
				if (dialog.ShowDialog() == DialogResult.OK)
					FileName = dialog.FileName;
			}
		}

		private void SaveFile()
		{
			if (_isImage && _currentImage != null)
			{
				using (var dialog = new SaveFileDialog())
				{
					dialog.Filter = "PNG文件|*.png|JPEG文件|*.jpg|所有文件|*.*";
					if (dialog.ShowDialog() == DialogResult.OK)
					{
						_currentImage.Save(dialog.FileName);
					}
				}
			}
		}

		private void NavigateFile(int direction)
		{
			if (_fileList.Count == 0) return;

			_activeFileIndex = (_activeFileIndex + direction + _fileList.Count) % _fileList.Count;
			FileName = _fileList[_activeFileIndex];
		}

		private void ZoomImage(float factor)
		{
			if (!_isImage) return;

			_zoomFactor *= factor;
			_imageViewer.Size = new Size(
				(int)(_currentImage.Width * _zoomFactor),
				(int)(_currentImage.Height * _zoomFactor)
			);
			UpdateStatusBar();
		}

		private void ResetZoom()
		{
			if (!_isImage) return;

			_zoomFactor = 1.0f;
			_imageViewer.Size = _currentImage.Size;
			UpdateStatusBar();
		}

		private void RotateImage()
		{
			if (!_isImage || _currentImage == null) return;

			_currentImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
			_imageViewer.Image = _currentImage;
			_imageViewer.Invalidate();
		}

		private void ToggleFullScreen()
		{
			if (!_isFullScreen)
			{
				FormBorderStyle = FormBorderStyle.None;
				WindowState = FormWindowState.Maximized;
				_toolStrip.Visible = false;
				_menuStrip.Visible = false;
				_statusStrip.Visible = false;
			}
			else
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				WindowState = FormWindowState.Normal;
				_toolStrip.Visible = true;
				_menuStrip.Visible = true;
				_statusStrip.Visible = true;
			}
			_isFullScreen = !_isFullScreen;
		}

		private void CaptureScreenshot()
		{
			if (!_isImage) return;

			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = "PNG文件|*.png";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					using (var bitmap = new Bitmap(_imageViewer.Width, _imageViewer.Height))
					{
						_imageViewer.DrawToBitmap(bitmap, _imageViewer.ClientRectangle);
						bitmap.Save(dialog.FileName, ImageFormat.Png);
					}
				}
			}
		}
		#endregion

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CleanupCurrentView();
				_animationTimer?.Dispose();
				_screenshotTimer?.Dispose();
				//_pluginList?.Dispose();	//bugfix: inied.wlx关闭时导致主程序意外退出；同时开多个cudalister.wlx，关闭其中一个导致主程序意外退出
			}
			base.Dispose(disposing);
		}
		private void setCheckedMenuStateByNameToId(string name, int checkedId = -1)
		{
			var viewmodeIndex = _menuStrip.Items.IndexOfKey(name);
			foreach (var item in ((ToolStripMenuItem)_menuStrip.Items[viewmodeIndex]).DropDownItems)
			{
				if (item is ToolStripMenuItem menuitem) 
					menuitem.Checked = false;
			}
			if(checkedId >= 0)
				((ToolStripMenuItem)((ToolStripMenuItem)_menuStrip.Items[viewmodeIndex]).DropDownItems[checkedId]).Checked = true;
		}
		private void SwitchViewMode(ViewMode mode)
		{
			_currentViewMode = mode;
			// 更新插件菜单的内置查看器为选中状态
			setCheckedMenuStateByNameToId("插件", 0);
			setCheckedMenuStateByNameToId("模式", (int)mode);

			// 隐藏所有面板
			_textPanel.Visible = false;
			_hexPanel.Visible = false;
			_imagePanel.Visible = false;
			container.Visible = false;
			// 根据模式显示相应面板
			switch (mode)
			{
				case ViewMode.Text:
					LoadText();
					_textPanel.Visible = true;
					break;
				case ViewMode.Hex:
					LoadHex();
					_hexPanel.Visible = true;
					break;
				case ViewMode.Media:
					if (_isImage)
						_imagePanel.Visible = true;
					else
					{
						// 如果不是图像，默认回到文本模式
						SwitchViewMode(ViewMode.Text);
					}
					break;
			}

			UpdateStatusBar();
		}
	
		private void LoadHex()
		{
			_hexPanel.Visible = true;
			_textPanel.Visible = false;
			_imagePanel.Visible = false;

			try
			{
				// 清空现有内容
				_hexViewer.Clear();

				// 读取文件内容
				byte[] fileBytes = File.ReadAllBytes(_fileName);
				StringBuilder hexContent = new StringBuilder();

				// 设置字体和颜色
				_hexViewer.Font = new Font("Consolas", 10);

				// 每行显示16个字节
				const int bytesPerLine = 16;

				for (int i = 0; i < fileBytes.Length; i += bytesPerLine)
				{
					// 添加偏移量
					hexContent.AppendFormat("{0:X8}:  ", i);

					// 添加16进制内容
					StringBuilder hexPart = new StringBuilder();
					StringBuilder asciiPart = new StringBuilder();

					for (int j = 0; j < bytesPerLine; j++)
					{
						if (i + j < fileBytes.Length)
						{
							byte b = fileBytes[i + j];

							// 添加16进制值
							hexPart.AppendFormat("{0:X2} ", b);

							// 添加ASCII字符（如果可打印）
							if (b >= 32 && b <= 126)
								asciiPart.Append((char)b);
							else
								asciiPart.Append('.');
						}
						else
						{
							// 填充空白
							hexPart.Append("   ");
							asciiPart.Append(" ");
						}
					}

					// 组合一行
					hexContent.AppendFormat("{0}  {1}\r\n", hexPart.ToString(), asciiPart.ToString());
				}

				_hexViewer.Text = hexContent.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载16进制视图失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	internal static class NativeMethods
	{
		public const int SWP_NOZORDER = 0x0004;
		public const int SWP_NOACTIVATE = 0x0010;
		// 新增窗口样式常量
		public const int GWL_STYLE = -16;
		public const int WS_CHILD = 0x40000000;
		public const int WS_VISIBLE = 0x10000000;
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr VirtualAlloc(
		 IntPtr lpAddress,
		 IntPtr dwSize,
		 uint flAllocationType,
		 uint flProtect);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualFree(
			IntPtr lpAddress,
			IntPtr dwSize,
			uint dwFreeType);

		public const uint MEM_COMMIT = 0x1000;
		public const uint MEM_RESERVE = 0x2000;
		public const uint MEM_RELEASE = 0x8000;
		public const uint PAGE_READWRITE = 0x04;
		[DllImport("kernel32.dll")]
		public static extern int SetProcessWorkingSetSizeEx(
			IntPtr hProcess,
			IntPtr dwMinimumWorkingSetSize,
			IntPtr dwMaximumWorkingSetSize,
			int Flags
		);
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern nint SetParent(nint hWndChild, nint hWndNewParent);
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
			int x, int y, int cx, int cy, int flags);
	}

}
