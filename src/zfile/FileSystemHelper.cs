using System.Text;
using WinShell;

namespace WinFormsApp1
{
    public static class FileSystemHelper
    {
		public static async Task<List<FileSystemInfo>> GetDirectoryContentsAsync(string path)
		{
			return await Task.Run(() => GetDirectoryContents(path));
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
