using System.Text;
using WinShell;

namespace WinFormsApp1
{
    public static class FileSystemHelper
    {
        public static void getSpecPathFromReg()
        {
            RegistryKey folders;
            folders = OpenRegistryPath(Registry.CurrentUser, @"\software\microsoft\windows\currentversion\explorer\shell folders");
            //Windows用户桌面路径
            string desktopPath = folders.GetValue("Desktop").ToString();
            //Windows用户字体目录路径
            string fontsPath = folders.GetValue("Fonts").ToString();
            //Windows用户网络邻居路径
            string nethoodPath = folders.GetValue("Nethood").ToString();
            //Windows用户我的文档路径
            string personalPath = folders.GetValue("Personal").ToString();
            //Windows用户开始菜单程序路径
            string programsPath = folders.GetValue("Programs").ToString();
            //Windows用户存放用户最近访问文档快捷方式的目录路径
            string recentPath = folders.GetValue("Recent").ToString();
            //Windows用户发送到目录路径
            string sendtoPath = folders.GetValue("Sendto").ToString();
            //Windows用户开始菜单目录路径
            string startmenuPath = folders.GetValue("Start menu").ToString();
            //Windows用户开始菜单启动项目录路径
            string startupPath = folders.GetValue("Startup").ToString();
            //Windows用户收藏夹目录路径
            string favoritesPath = folders.GetValue("Favorites").ToString();
            //Windows用户网页历史目录路径
            string historyPath = folders.GetValue("History").ToString();
            //Windows用户Cookies目录路径
            string cookiesPath = folders.GetValue("Cookies").ToString();
            //Windows用户Cache目录路径
            string cachePath = folders.GetValue("Cache").ToString();
            //Windows用户应用程式数据目录路径
            string appdataPath = folders.GetValue("Appdata").ToString();
            //Windows用户打印目录路径
            string printhoodPath = folders.GetValue("Printhood").ToString();
            String Path = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);//返回收藏夹位置
            Console.WriteLine(Path);
        }
        public static RegistryKey OpenRegistryPath(RegistryKey root, string s)
        {
            s = s.Remove(0, 1) + @"\";
            while (s.IndexOf(@"\") != -1)
            {
                root = root.OpenSubKey(s.Substring(0, s.IndexOf(@"\")));
                s = s.Remove(0, s.IndexOf(@"\") + 1);
            }
            return root;
        }

        public static void getEnv()
        {
            //把环境变量中所有的值取出来，放到变量environment中
            IDictionary environment = Environment.GetEnvironmentVariables();
            //打印表头
            Console.WriteLine("环境变量名\t=\t环境变量值");
            //遍历environment中所有键值
            foreach (string environmentKey in environment.Keys)
            {
                //打印出所有环境变量的名称和值
                //Console.WriteLine("(0}\t=\t{(1}", environmentKey, environment[environmentKey].ToString());
            }
        }
        public static List<FileSystemInfo> GetDirectoryContents(string path, bool includeFolder = false)
        {
            var result = new List<FileSystemInfo>();
            if (!Directory.Exists(path)) return result;

            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (includeFolder)
                {
                    var directories = dirInfo.GetDirectories()
                        .Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
                    result.AddRange(directories);
                }
                var files = dirInfo.GetFiles()
                        .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);
                result.AddRange(files);
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略访问受限的目录
            }
            return result;
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        public static string getFSpath(string path)
        {
            if (path.Contains(':'))
            {
                var pathParts = path.Split(':');
                var len = pathParts[0].Length;
                var drive = pathParts[0].Substring(len - 1, 1);
                return drive + ":" + pathParts[1].TrimStart(')');
            }
            return path;
        }
		public static string getFSpathbyTree(TreeNode Node)
		{
			if (Node.Parent == null)
			{
				//top node process, does not need to process listviewbyfilesystem
				return string.Empty;
			}
			var parentfolder = ((ShellItem)Node.Parent.Tag).ShellFolder;
			var pidl = ((ShellItem)Node.Tag).PIDL;
			return API.GetPathByIShell(parentfolder, pidl);
		}
		public static string getFSpathbyList(string path)
		    {			
			var parentfolder = w32.GetParentFolder(path);
			var pidl = w32.ILCreateFromPath(path);
			return API.GetPathByIShell(parentfolder, pidl);
		}
	}
}
