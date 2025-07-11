using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Reflection;

namespace zfile.Forms
{
    public class ViewerPanel : UserControl
    {
        // 预留字段和属性，后续迁移 ViewerForm 的核心成员
        private string _fileName;
        private List<string> _fileList;
        private int _activeFileIndex;
        private bool _isAnimation;
        private bool _isImage;
        private bool _isPlugin;
        private bool _isTextMode;
        private Encoding _currentEncoding;
        private float _zoomFactor = 1.0f;
        private Point _lastMousePosition;
        private bool _isDragging;
        private Image _currentImage;
        private WlxModuleList _pluginList;
        private WlxModule _currentPlugin;
        private nint _pluginWindow;

        // 视图模式枚举
        private enum ViewMode
        {
            Text,
            Hex,
            Media,
			Plugin
        }
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
        private ContextMenuStrip _menuStrip;
        private System.Windows.Forms.Timer _animationTimer;
        private System.Windows.Forms.Timer _screenshotTimer;

        public ViewerPanel()
        {
            InitializeComponent();
            InitializeFileList();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            // 主面板
            _mainPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(_mainPanel);

            // 图像查看面板
            _imagePanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            _imageViewer = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            _imagePanel.Controls.Add(_imageViewer);

            // 文本查看面板
            _textPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            _textViewer = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, WordWrap = true, Font = new Font("Consolas", 10) };
            _textPanel.Controls.Add(_textViewer);

            // 16进制查看面板
            _hexPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            _hexViewer = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, WordWrap = false, Font = new Font("Consolas", 10), BackColor = Color.White, ForeColor = Color.Black };
            _hexPanel.Controls.Add(_hexViewer);

            // 隐藏的容器面板（为插件预留）
            container = new Panel { Dock = DockStyle.Fill, Visible = false };

            // 添加控件到主面板
            _mainPanel.Controls.Add(_imagePanel);
            _mainPanel.Controls.Add(_textPanel);
            _mainPanel.Controls.Add(_hexPanel);
            _mainPanel.Controls.Add(container);
        }

        private void InitializeFileList()
        {
            _fileList = new List<string>();
            _currentEncoding = Encoding.Default;
        }

        private void SetupEventHandlers()
        {
            // 绑定图片拖拽等事件
            _imageViewer.MouseDown += ImageViewer_MouseDown;
            _imageViewer.MouseMove += ImageViewer_MouseMove;
            _imageViewer.MouseUp += ImageViewer_MouseUp;
        }

        // --- 预览核心方法 ---
        public void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;
			if (!File.Exists(fileName))
				if (Directory.Exists(fileName))
					fileName = Helper.IncludeTrailingPathDelimiter(fileName);
				else
					return;

			_fileName = fileName;
            try
            {
                CleanupCurrentView();
                EnsurePluginList();
                int tryModuleIdx = -1;
				while (tryModuleIdx < _pluginList._configDict.Count)
				{
					var plugin = _pluginList?.FindModuleForFile(_fileName, ref tryModuleIdx);
					if (plugin != null && LoadWithPlugin(plugin))
						return;
				}
                string extension = Path.GetExtension(_fileName).ToLower();
                if (IsImageFile(extension))
                {
                    _isImage = true;
                    LoadImage();
                    SwitchViewModeInternal(ViewMode.Media);
                }
                else if (IsMediaFile(extension))
                {
                    // 预留音视频播放接口
                    SwitchViewModeInternal(ViewMode.Media);
                }
                else
                {
                    LoadText();
                    SwitchViewModeInternal(ViewMode.Text);
                }
            }
            catch { /* 可加日志 */ }
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
                _currentEncoding = DetectEncoding(_fileName);
                string content = File.ReadAllText(_fileName, _currentEncoding);
                _textViewer.Text = content;
            }
        }
        public void SwitchMode(string mode)
        {
            if (mode == "hex")
                SwitchViewModeInternal(ViewMode.Hex);
            else if (mode == "media")
                SwitchViewModeInternal(ViewMode.Media);
            else
                SwitchViewModeInternal(ViewMode.Text);
        }
        private void SwitchViewModeInternal(ViewMode mode)
        {
            _currentViewMode = mode;
            _textPanel.Visible = false;
            _hexPanel.Visible = false;
            _imagePanel.Visible = false;
            container.Visible = false;
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
                        SwitchViewModeInternal(ViewMode.Text);
                    break;
            }
        }
        private void LoadHex()
        {
            _hexPanel.Visible = true;
            _textPanel.Visible = false;
            _imagePanel.Visible = false;
            try
            {
                _hexViewer.Clear();
                byte[] fileBytes = File.ReadAllBytes(_fileName);
                StringBuilder hexContent = new StringBuilder();
                const int bytesPerLine = 16;
                for (int i = 0; i < fileBytes.Length; i += bytesPerLine)
                {
                    hexContent.AppendFormat("{0:X8}:  ", i);
                    StringBuilder hexPart = new StringBuilder();
                    StringBuilder asciiPart = new StringBuilder();
                    for (int j = 0; j < bytesPerLine; j++)
                    {
                        if (i + j < fileBytes.Length)
                        {
                            byte b = fileBytes[i + j];
                            hexPart.AppendFormat("{0:X2} ", b);
                            if (b >= 32 && b <= 126)
                                asciiPart.Append((char)b);
                            else
                                asciiPart.Append('.');
                        }
                        else
                        {
                            hexPart.Append("   ");
                            asciiPart.Append(" ");
                        }
                    }
                    hexContent.AppendFormat("{0}  {1}\r\n", hexPart.ToString(), asciiPart.ToString());
                }
                _hexViewer.Text = hexContent.ToString();
            }
            catch { }
        }
        private void CleanupCurrentView()
        {
            if (_currentImage != null)
            {
                _currentImage.Dispose();
                _currentImage = null;
            }
            _isImage = false;
            _isPlugin = false;
            _isAnimation = false;
            if (_imageViewer != null)
                _imageViewer.Image = null;
        }
        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            return Array.IndexOf(imageExtensions, extension) != -1;
        }
        private Encoding DetectEncoding(string fileName)
        {
            using (var reader = new StreamReader(fileName, Encoding.Default, true))
            {
                reader.Peek();
                return reader.CurrentEncoding;
            }
        }
        // 图片拖拽事件（可选）
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
        public void Unload()
        {
            CleanupCurrentView();
        }
        // 插件支持
        private void EnsurePluginList()
        {
            if (_pluginList == null)
                _pluginList = new WlxModuleList();
        }

        // 菜单支持
        private void CreateMenuStrip()
        {
            if (_menuStrip != null) return;
            _menuStrip = new ContextMenuStrip();
            // 模式切换
            var textMode = new ToolStripMenuItem("文本模式", null, (s, e) => SwitchMode("text"));
            var hexMode = new ToolStripMenuItem("16进制模式", null, (s, e) => SwitchMode("hex"));
            var mediaMode = new ToolStripMenuItem("图片/多媒体", null, (s, e) => SwitchMode("media"));
            _menuStrip.Items.AddRange(new ToolStripItem[] { textMode, hexMode, mediaMode });
            // 插件菜单
            EnsurePluginList();
            if (_pluginList != null && _pluginList.Modules.Count > 0)
            {
                var pluginMenu = new ToolStripMenuItem("插件");
                foreach (var plug in _pluginList.Modules)
                {
                    var item = new ToolStripMenuItem(plug.Name, null, (s, e) => LoadWithPlugin(plug));
                    pluginMenu.DropDownItems.Add(item);
                }
                _menuStrip.Items.Add(pluginMenu);
            }
            // 右键弹出
            this.ContextMenuStrip = _menuStrip;
        }

        public override void Refresh()
        {
            base.Refresh();
            CreateMenuStrip();
        }

        public void SetPluginList(WlxModuleList pluginList)
        {
            _pluginList = pluginList;
            CreateMenuStrip();
        }

        private bool LoadWithPlugin(WlxModule plugin)
        {
            _isPlugin = true;
            _textPanel.Visible = false;
            _hexPanel.Visible = false;
            _imagePanel.Visible = false;
            foreach (var p in _mainPanel.Controls)
                if (p is Panel pnl) pnl.Visible = false;
            if (_currentPlugin == null)
                _currentPlugin = plugin;
            else if (_currentPlugin != plugin)
            {
                if (_pluginWindow != IntPtr.Zero)
                {
                    _currentPlugin.CallListCloseWindow(_pluginWindow);
                    _pluginWindow = IntPtr.Zero;
                }
                _currentPlugin = plugin;
            }
            if (!container.IsHandleCreated)
                _ = container.Handle;

            // 检查 container 的实际大小
            var rect = container.DisplayRectangle;
            if (rect.Width < 10 || rect.Height < 10)
            {
                // 挂接一次性 Resize 事件，等有实际大小再创建插件窗口
                container.Resize -= DelayedPluginLoad_Resize;
                container.Resize += DelayedPluginLoad_Resize;
                container.Visible = true;
                return false;
            }

            CreatePluginWindow(plugin, rect);
            return _pluginWindow != IntPtr.Zero;
        }

        private void DelayedPluginLoad_Resize(object? sender, EventArgs e)
        {
            var rect = container.DisplayRectangle;
            if (rect.Width >= 10 && rect.Height >= 10)
            {
                container.Resize -= DelayedPluginLoad_Resize;
                if (_currentPlugin != null && _pluginWindow == IntPtr.Zero)
                {
                    CreatePluginWindow(_currentPlugin, rect);
                }
            }
        }

        private void CreatePluginWindow(WlxModule plugin, Rectangle rect)
        {
            int initW = rect.Width >= 10 ? rect.Width : 800;
            int initH = rect.Height >= 10 ? rect.Height : 600;
            Form pluginHostForm = new Form();
            pluginHostForm.Size = new Size(initW, initH);
            pluginHostForm.StartPosition = FormStartPosition.Manual;
            pluginHostForm.Location = container.PointToScreen(Point.Empty);
            pluginHostForm.ShowInTaskbar = false;
            pluginHostForm.FormBorderStyle = FormBorderStyle.None;
            pluginHostForm.Visible = false;
            IntPtr parentWin = pluginHostForm.Handle;
            _pluginWindow = plugin.CallListLoad(parentWin, _fileName, 1);

            if (_pluginWindow != IntPtr.Zero)
            {
                NativeMethods.SetParent(_pluginWindow, container.Handle);
                NativeMethods.SetWindowLong(_pluginWindow, NativeMethods.GWL_STYLE, NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD);
                SetPluginWindowBounds(container);
                container.Visible = true;
                // 多重事件保证后续自适应
                container.Resize -= Container_ResizeForPlugin;
                container.Resize += Container_ResizeForPlugin;
                container.ParentChanged -= Container_ResizeForPlugin;
                container.ParentChanged += Container_ResizeForPlugin;
                container.VisibleChanged -= Container_ResizeForPlugin;
                container.VisibleChanged += Container_ResizeForPlugin;
                container.Layout -= Container_ResizeForPlugin;
                container.Layout += Container_ResizeForPlugin;

                // 多次延迟强制同步插件窗口大小
                for (int i = 1; i <= 3; i++)
                {
                    int delay = i * 100;
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = delay;
                    timer.Tick += (s, e) =>
                    {
                        SetPluginWindowBounds(container);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }
        private void Container_ResizeForPlugin(object? sender, EventArgs e)
        {
            SetPluginWindowBounds(container);
        }
        private void SetPluginWindowBounds(Panel container)
        {
            if (_pluginWindow != IntPtr.Zero && container != null)
            {
                var bounds = container.ClientRectangle;
                NativeMethods.SetWindowPos(_pluginWindow, IntPtr.Zero, 0, 0, bounds.Width, bounds.Height, NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
            }
        }
        private bool IsMediaFile(string extension)
        {
            string[] mediaExtensions = { ".mp3", ".wav", ".mp4", ".avi", ".wmv", ".mov", ".flv", ".mkv" };
            return Array.IndexOf(mediaExtensions, extension) != -1;
        }
    }
} 