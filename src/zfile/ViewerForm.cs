using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace WinFormsApp1
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

        // 控件
        private Panel _mainPanel;
        private Panel _imagePanel;
        private Panel _textPanel;
        private RichTextBox _textViewer;
        private PictureBox _imageViewer;
        private ToolStrip _toolStrip;
        private StatusStrip _statusStrip;
        private MenuStrip _menuStrip;
        private Timer _animationTimer;
        private Timer _screenshotTimer;

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
            InitializeComponent();
            InitializePlugins();
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

            // 创建工具栏
            CreateToolStrip();

            // 创建菜单栏
            CreateMenuStrip();

            // 创建状态栏
            CreateStatusStrip();

            // 添加控件到窗体
            _mainPanel.Controls.Add(_imagePanel);
            _mainPanel.Controls.Add(_textPanel);
            this.Controls.Add(_mainPanel);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_menuStrip);
            this.Controls.Add(_statusStrip);

            // 初始化计时器
            _animationTimer = new Timer { Interval = 100 };
            _screenshotTimer = new Timer { Interval = 3000 };
        }

        private void InitializePlugins()
        {
            _pluginList = new WlxModuleList();
            string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (Directory.Exists(pluginPath))
            {
                _pluginList.LoadModulesFromDirectory(pluginPath);
            }
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
                _currentPlugin = _pluginList.FindModuleForFile(_fileName);
                if (_currentPlugin != null)
                {
                    LoadWithPlugin();
                    return;
                }

                // 检查文件类型
                string extension = Path.GetExtension(_fileName).ToLower();
                if (IsImageFile(extension))
                {
                    LoadImage();
                }
                else
                {
                    LoadText();
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWithPlugin()
        {
            _isPlugin = true;
            _pluginWindow = _currentPlugin.CallListLoad(this.Handle, _fileName, WlxConstants.LISTPLUGIN_SHOW);
            if (_pluginWindow != IntPtr.Zero)
            {
                // 设置插件窗口位置和大小
                SetPluginWindowBounds();
            }
        }

        private void LoadImage()
        {
            _isImage = true;
            _imagePanel.Visible = true;
            _textPanel.Visible = false;

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

            // 编码菜单
            var encodingMenu = new ToolStripMenuItem("编码(&E)");
            foreach (var enc in Encoding.GetEncodings())
            {
                var encoding = enc.GetEncoding();
                var menuItem = new ToolStripMenuItem(encoding.EncodingName, null, (s, e) => {
                    _currentEncoding = encoding;
                    LoadText();
                });
                encodingMenu.DropDownItems.Add(menuItem);
            }

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, encodingMenu });
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
            if (e.KeyCode == Keys.Escape && _isFullScreen)
            {
                ToggleFullScreen();
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
                    fileInfo.Text = $"大小: {FormatFileSize(fileSize)}";
                }

                if (encodingLabel != null)
                {
                    encodingLabel.Text = $"编码: {_currentEncoding.EncodingName}";
                }

                if (zoomLabel != null && _isImage)
                {
                    zoomLabel.Text = $"缩放: {_zoomFactor:P0}";
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
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
    }

    internal static class NativeMethods
    {
        public const int SWP_NOZORDER = 0x0004;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, int flags);
    }
} 