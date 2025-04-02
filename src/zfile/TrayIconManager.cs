using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Zfile
{
    /// <summary>
    /// 系统托盘图标管理器，用于显示下载管理器状态
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private bool _disposed = false;
        private static TrayIconManager _instance;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// 获取TrayIconManager的单例实例
        /// </summary>
        public static TrayIconManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new TrayIconManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private TrayIconManager()
        {
            InitializeNotifyIcon();
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = false,
                Icon = GetApplicationIcon(),
                Text = "ZFile IDM下载管理器 - 监听中"
            };

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            
            // 添加菜单项 - 打开下载管理器
            var openItem = new ToolStripMenuItem("打开下载管理器");
            openItem.Click += (sender, e) => IdmIntegration.ShowIdmManager();
            contextMenu.Items.Add(openItem);
            
            // 添加分隔线
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // 添加菜单项 - 退出
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (sender, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // 双击打开下载管理器
            _notifyIcon.DoubleClick += (sender, e) => IdmIntegration.ShowIdmManager();
        }

        /// <summary>
        /// 获取应用程序图标
        /// </summary>
        private Icon GetApplicationIcon()
        {
            try
            {
                // 尝试从Chrome扩展图标加载
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChromeExtension", "images", "icon48.png");
                if (File.Exists(iconPath))
                {
                    using (var bitmap = new Bitmap(iconPath))
                    {
                        return Icon.FromHandle(bitmap.GetHicon());
                    }
                }
                
                // 如果找不到扩展图标，使用应用程序图标
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图标失败: {ex.Message}");
                return SystemIcons.Application; // 使用系统默认图标
            }
        }

        /// <summary>
        /// 显示系统托盘图标
        /// </summary>
        /// <param name="message">气泡提示消息（可选）</param>
        public void Show(string message = null)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                
                // 如果提供了消息，显示气泡提示
                if (!string.IsNullOrEmpty(message))
                {
                    _notifyIcon.ShowBalloonTip(3000, "ZFile IDM下载管理器", message, ToolTipIcon.Info);
                }
            }
        }

        /// <summary>
        /// 隐藏系统托盘图标
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        /// <summary>
        /// 更新托盘图标提示文本
        /// </summary>
        /// <param name="text">新的提示文本</param>
        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text;
            }
        }

        /// <summary>
        /// 显示气泡提示
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="icon">图标类型</param>
        public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, icon);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }
                }

                _disposed = true;
            }
        }
    }
}