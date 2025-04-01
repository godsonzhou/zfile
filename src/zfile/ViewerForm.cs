using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace Zfile
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
		private IntPtr _pluginWindow;

		// 查看模式枚举
		private enum ViewMode
		{
			Text,
			Hex,
			Media
		}

		private ViewMode _currentViewMode = ViewMode.Text;

		// 控件
		private Panel _mainPanel;
		private Panel _imagePanel;
		private Panel _textPanel;
		private Panel _hexPanel;
		private RichTextBox _textViewer;
		private RichTextBox _hexViewer;
		private PictureBox _imageViewer;
		private ToolStrip _toolStrip;
		private StatusStrip _statusStrip;
		private MenuStrip _menuStrip;
		private Timer _animationTimer;
		private Timer _screenshotTimer;
		private bool isPluginLoaded;

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
			this.Text = "文件查看器";
			this.Size = new Size(800, 600);
			this.StartPosition = FormStartPosition.CenterScreen;

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

			// 添加控件到窗体
			_mainPanel.Controls.Add(_imagePanel);
			_mainPanel.Controls.Add(_textPanel);
			_mainPanel.Controls.Add(_hexPanel);
			this.Controls.Add(_mainPanel);
			this.Controls.Add(_menuStrip);
			this.Controls.Add(_toolStrip);
			this.Controls.Add(_statusStrip);

			// 初始化计时器
			_animationTimer = new Timer { Interval = 100 };
			_screenshotTimer = new Timer { Interval = 3000 };
		}

		public WlxModuleList InitializePlugins()
		{
			//_pluginList = new WlxModuleList();
			string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins\\wlx");
			_pluginList.LoadModulesFromDirectory(pluginPath);
			isPluginLoaded = true;
			return _pluginList;
		}

		private void InitializeFileList()
		{
			_fileList = new List<string>();
			_activeFileIndex = -1;
			_currentEncoding = Encoding.Default;
		}

		private void SetupEventHandlers()
		{
			this.KeyDown += ViewerForm_KeyDown;
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
					_currentPlugin = _pluginList.FindModuleForFile(_fileName, ref tryModuleIdx);
					if (_currentPlugin != null)
					{
						var loadsuccess = LoadWithPlugin();  //should consider load fail
						if (loadsuccess)
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
					{
						SwitchViewMode(ViewMode.Media);
					}
					else
					{
						_imagePanel.Visible = true;
					}
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

				UpdateStatusBar();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private bool LoadWithPlugin()
		{
			_isPlugin = true;

			// 隐藏所有内置查看器面板
			_textPanel.Visible = false;
			_hexPanel.Visible = false;
			_imagePanel.Visible = false;

			// HIDE ALL SUBPANEL IF SWITCH PLUG
			foreach (var p in _mainPanel.Controls)
			{
				if (p is Panel)
				{
					((Panel)p).Visible = false;
				}
			}
			// 创建隐藏的容器面板
			var container = new Panel
			{
				Dock = DockStyle.Fill,
				Visible = false
			};
			_mainPanel.Controls.Add(container);
			container.SetBounds(_mainPanel.Bounds.X, _mainPanel.Bounds.Y, _mainPanel.Bounds.Width, _mainPanel.Bounds.Height);
			// 传递容器面板的句柄作为父窗口
			_pluginWindow = _currentPlugin.CallListLoad(container.Handle, _fileName, WlxConstants.LISTPLUGIN_SHOW);
			//IntPtr bmp = IntPtr.Zero;
			//if(_pluginWindow == IntPtr.Zero)
			//	_pluginWindow = _currentPlugin.CallListGetPreviewBitmap(_fileName, _mainPanel.Bounds.Width, _mainPanel.Bounds.Height, bmp);
			//_pluginWindow = _currentPlugin.CallListLoad(this.Handle, _fileName, WlxConstants.LISTPLUGIN_SHOW);
			if (_pluginWindow != IntPtr.Zero)
			{
				// 设置窗口样式为子窗口
				NativeMethods.SetParent(_pluginWindow, container.Handle);
				NativeMethods.SetWindowLong(_pluginWindow, NativeMethods.GWL_STYLE,
					NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD);

				// 调整窗口位置和大小
				SetPluginWindowBounds(container);
				container.Visible = true;
				// 设置插件窗口位置和大小
				//SetPluginWindowBounds();
				return true;
			}
			return false;
		}
		// 在窗体Resize事件中更新位置
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (_pluginWindow != IntPtr.Zero)
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
				{
					_animationTimer.Start();
				}
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

			if (_pluginWindow != IntPtr.Zero)
			{
				_currentPlugin?.CallListCloseWindow(_pluginWindow);
				_pluginWindow = IntPtr.Zero;
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
			var prevButton = new ToolStripButton("上一个", null, (s, e) => NavigateFile(-1));
			var nextButton = new ToolStripButton("下一个", null, (s, e) => NavigateFile(1));
			var zoomInButton = new ToolStripButton("放大", null, (s, e) => ZoomImage(1.2f));
			var zoomOutButton = new ToolStripButton("缩小", null, (s, e) => ZoomImage(0.8f));
			var rotateButton = new ToolStripButton("旋转", null, (s, e) => RotateImage());
			var fullScreenButton = new ToolStripButton("全屏", null, (s, e) => ToggleFullScreen());

			_toolStrip.Items.AddRange(new ToolStripItem[] {
				openButton, new ToolStripSeparator(),
				prevButton, nextButton, new ToolStripSeparator(),
				zoomInButton, zoomOutButton, rotateButton, new ToolStripSeparator(),
				fullScreenButton
			});
		}

		private void CreateMenuStrip()
		{
			_menuStrip = new MenuStrip();

			// 文件菜单
			var fileMenu = new ToolStripMenuItem("文件(&F)");
			fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
				new ToolStripMenuItem("打开(&O)", null, (s, e) => OpenFile()),
				new ToolStripMenuItem("保存(&S)", null, (s, e) => SaveFile()),
				new ToolStripSeparator(),
				new ToolStripMenuItem("退出(&X)", null, (s, e) => Close())
			});

			// 查看菜单
			var viewMenu = new ToolStripMenuItem("查看(&V)");
			viewMenu.DropDownItems.AddRange(new ToolStripItem[] {
				new ToolStripMenuItem("放大(&I)", null, (s, e) => ZoomImage(1.2f)),
				new ToolStripMenuItem("缩小(&O)", null, (s, e) => ZoomImage(0.8f)),
				new ToolStripMenuItem("实际大小(&A)", null, (s, e) => ResetZoom()),
				new ToolStripSeparator(),
				new ToolStripMenuItem("全屏(&F)", null, (s, e) => ToggleFullScreen())
			});

			// 模式菜单
			var modeMenu = new ToolStripMenuItem("模式(&M)");
			var textModeItem = new ToolStripMenuItem("文本(&T)", null, (s, e) => SwitchViewMode(ViewMode.Text));
			var hexModeItem = new ToolStripMenuItem("16进制(&H)", null, (s, e) => SwitchViewMode(ViewMode.Hex));
			var mediaModeItem = new ToolStripMenuItem("多媒体(&M)", null, (s, e) => SwitchViewMode(ViewMode.Media));

			// 默认选中文本模式
			textModeItem.Checked = true;

			modeMenu.DropDownItems.AddRange(new ToolStripItem[] {
				textModeItem, hexModeItem, mediaModeItem
			});

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
			var pluginMenu = new ToolStripMenuItem("插件(&P)");

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
					_currentPlugin = plug;
					LoadWithPlugin();
				});
				pluginMenu.DropDownItems.Add(item);
			}
			_menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, modeMenu, encodingMenu, pluginMenu });
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
		private void ViewerForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (_isFullScreen)
					ToggleFullScreen();
				else
					Close();
			}
		}

		private void ImageViewer_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = true;
				_lastMousePosition = e.Location;
			}
		}

		private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
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

		private void ImageViewer_MouseUp(object sender, MouseEventArgs e)
		{
			_isDragging = false;
		}

		private void AnimationTimer_Tick(object sender, EventArgs e)
		{
			if (_isAnimation && _currentImage != null)
			{
				ImageAnimator.UpdateFrames(_currentImage);
				_imageViewer.Invalidate();
			}
		}

		private void ScreenshotTimer_Tick(object sender, EventArgs e)
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
					{
						return BitConverter.ToInt16(item.Value, 0) > 1;
					}
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
			this.Text = $"文件查看器 - {Path.GetFileName(_fileName)}";
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
					{
						modeText = "插件模式";
					}

					encodingLabel.Text = $"编码: {_currentEncoding.EncodingName} | {modeText}";
				}

				if (zoomLabel != null && _isImage)
				{
					zoomLabel.Text = $"缩放: {_zoomFactor:P0}";
				}
			}
		}

		private void SetPluginWindowBounds(Panel container)
		{
			if (_pluginWindow != IntPtr.Zero && container != null)
			{
				var bounds = container.ClientRectangle;
				NativeMethods.SetWindowPos(_pluginWindow, IntPtr.Zero,
					0, 0, bounds.Width, bounds.Height,
					NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
			}
		}
		private void SetPluginWindowBounds()
		{
			if (_pluginWindow != IntPtr.Zero)
			{
				var bounds = _mainPanel.ClientRectangle;
				NativeMethods.SetWindowPos(_pluginWindow, IntPtr.Zero,
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
				{
					FileName = dialog.FileName;
				}
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
				this.FormBorderStyle = FormBorderStyle.None;
				this.WindowState = FormWindowState.Maximized;
				_toolStrip.Visible = false;
				_menuStrip.Visible = false;
				_statusStrip.Visible = false;
			}
			else
			{
				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.WindowState = FormWindowState.Normal;
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
				_pluginList?.Dispose();
			}
			base.Dispose(disposing);
		}
		private void SwitchViewMode(ViewMode mode)
		{
			_currentViewMode = mode;

			// 更新菜单项选中状态
			foreach (ToolStripMenuItem item in ((ToolStripMenuItem)_menuStrip.Items[2]).DropDownItems)
			{
				if (item is ToolStripMenuItem)
				{
					item.Checked = false;
				}
			}

		((ToolStripMenuItem)((ToolStripMenuItem)_menuStrip.Items[2]).DropDownItems[(int)mode]).Checked = true;

			// 隐藏所有面板
			_textPanel.Visible = false;
			_hexPanel.Visible = false;
			_imagePanel.Visible = false;

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
					{
						_imagePanel.Visible = true;
					}
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
							{
								asciiPart.Append((char)b);
							}
							else
							{
								asciiPart.Append('.');
							}
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

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
			int x, int y, int cx, int cy, int flags);
	}

}
