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
            Media
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
        private MenuStrip _menuStrip;
        private Timer _animationTimer;
        private Timer _screenshotTimer;

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
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return;
            _fileName = fileName;
            try
            {
                CleanupCurrentView();
                string extension = Path.GetExtension(_fileName).ToLower();
                if (IsImageFile(extension))
                {
                    _isImage = true;
                    LoadImage();
                    SwitchViewModeInternal(ViewMode.Media);
                }
                else
                {
                    // 默认文本模式
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
        public void SetPluginList(WlxModuleList pluginList)
        {
            _pluginList = pluginList;
        }
    }
} 