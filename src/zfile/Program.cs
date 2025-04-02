using System.Text;

namespace Zfile
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {    
            // Register the encoding provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // 检查是否有命令行参数，用于处理Chrome扩展的启动请求
            if (args.Length > 0 && args[0] == "--chrome-extension")
            {
                // 初始化Chrome扩展支持
                try
                {
                    ApplicationConfiguration.Initialize();
                    IdmIntegration.InitializeChromeExtensionSupport();
                    
                    // 创建一个隐藏的窗体以保持应用程序运行
                    Application.Run(new Form { WindowState = FormWindowState.Minimized, ShowInTaskbar = false });
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"初始化Chrome扩展支持失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            
            // 正常启动应用程序
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}