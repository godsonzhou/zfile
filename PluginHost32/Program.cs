using System.Diagnostics;

namespace PluginHost32
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();
			//接收2个argument, 用于初始化pluginhost
			var arg = Environment.GetCommandLineArgs();
			Debug.Print($"pluginhost32: arg0={arg[0]} arg1={arg[1]}");
			
			Application.Run(new PluginHost(arg[0], arg[1]));
		}
	}
}