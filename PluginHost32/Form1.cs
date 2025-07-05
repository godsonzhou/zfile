using System.Diagnostics;
using System.Runtime.InteropServices;
using zfile;
namespace PluginHost32
{
	public class PluginHost : Form
	{
		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		private WlxModule _plugin;
		private IntPtr _pluginWindow;
		private string _pluginPath;
		private string _filePath;

		public PluginHost() { }
		public PluginHost(string pluginPath, string filePath)
		{
			_pluginPath = pluginPath;
			_filePath = filePath;
			Debug.Print($"pluginhost.ctr: {pluginPath} {filePath}");
			Load += PluginHost_Load;
		}

		private void PluginHost_Load(object sender, EventArgs e)
		{
			try
			{
				// 加载插件
				_plugin = new WlxModule() 
				{ 
					FilePath = _pluginPath,
					Name = Path.GetFileNameWithoutExtension(_pluginPath)
				};
				_plugin.LoadModule();

				// 创建插件窗口
				_pluginWindow = _plugin.CallListLoad(Handle, _filePath, WlxConstants.LISTPLUGIN_SHOW);

				if (_pluginWindow != IntPtr.Zero)
				{
					// 设置窗口样式
					NativeMethods.SetWindowLong(_pluginWindow, NativeMethods.GWL_STYLE,
						NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD);

					// 调整窗口大小
					NativeMethods.SetWindowPos(_pluginWindow, IntPtr.Zero,
						0, 0, ClientSize.Width, ClientSize.Height,
						NativeMethods.SWP_NOZORDER);

					// 将插件窗口句柄发送给主进程
					Console.WriteLine(_pluginWindow.ToString());
				}
				else
				{
					Console.WriteLine("ERROR:Failed to create plugin window");
					Environment.Exit(1);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR:{ex.Message}");
				Environment.Exit(1);
			}
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (_pluginWindow != IntPtr.Zero)
			{
				NativeMethods.SetWindowPos(_pluginWindow, IntPtr.Zero,
					0, 0, ClientSize.Width, ClientSize.Height,
					NativeMethods.SWP_NOZORDER);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (_pluginWindow != IntPtr.Zero)
			{
				_plugin.CallListCloseWindow(_pluginWindow);
			}
		}
	}

	internal static class NativeMethods
	{
		public const int GWL_STYLE = -16;
		public const int WS_CHILD = 0x40000000;
		public const int WS_VISIBLE = 0x10000000;
		public const int SWP_NOZORDER = 0x0004;

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
			int x, int y, int cx, int cy, int flags);
	}
}
