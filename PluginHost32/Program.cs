using System.Diagnostics;

namespace PluginHost32
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				// To customize application configuration such as set high DPI settings or default font,
				// see https://aka.ms/applicationconfiguration.
				ApplicationConfiguration.Initialize();
				//接收2个argument, 用于初始化pluginhost
				//var arg = Environment.GetCommandLineArgs();
				if (args.Length == 2)
				{
					Console.WriteLine($"pluginhost32: arg0={args[0]} arg1={args[1]}");
					Application.Run(new PluginHost(args[0], args[1]));
				}
			}
			catch (Exception ex)
			{
				// 捕获异常并显示错误信息
				MessageBox.Show($"插件加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Debug.Print($"PluginHost32: {ex}");
				Environment.Exit(1);
			}
		}
	}
}