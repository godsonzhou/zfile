using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;

namespace WinFormsApp1
{

	public static class IconManager
    {
		public static void InitializeIcons(ImageList l)
		{
			l.ColorDepth = ColorDepth.Depth32Bit;
			l.ImageSize = new Size(16, 16);
			var imageresPath = Path.Combine(Environment.SystemDirectory, "imageres.dll");
			//var iconManager = form.iconManager; // 使用form中的IconManager实例

			// 使用IconManager加载系统图标
			var imageList = LoadIconsFromFile(imageresPath);

			// 添加系统图标到treeViewImageList
			l.Images.Add("desktop", LoadIcon($"{imageresPath},174")); // 桌面
			l.Images.Add("computer", LoadIcon($"{imageresPath},104")); // 此电脑
			l.Images.Add("network", LoadIcon($"{imageresPath},20")); // 网上邻居
			l.Images.Add("controlPanel", LoadIcon($"{imageresPath},22")); // 控制面板
			l.Images.Add("recyclebin", LoadIcon($"{imageresPath},49")); // 回收站49,empty recyclebin=50
			l.Images.Add("documents", LoadIcon($"{imageresPath},107")); // 文档
			l.Images.Add("drives", LoadIcon($"{imageresPath},27")); // 磁盘驱动器
			l.Images.Add("linux", LoadIcon($"{imageresPath},27")); // 
			l.Images.Add("downloads", LoadIcon($"{imageresPath},175")); // 
			l.Images.Add("music", LoadIcon($"{imageresPath},103")); // 
			l.Images.Add("pictures", LoadIcon($"{imageresPath},108")); // 
			l.Images.Add("videos", LoadIcon($"{imageresPath},178")); // 
			l.Images.Add("home", LoadIcon($"{imageresPath},83")); // 

			// 添加默认文件夹图标
			Icon folderIcon = Helper.GetIconByFileType("folder", false);
			if (folderIcon != null)
			{
				l.Images.Add("folder", folderIcon);
			}
		}
		public static string GetIconKey(string Text)
		{
			// 基于节点文本和属性确定图标键值
			if (Text.Equals("桌面", StringComparison.OrdinalIgnoreCase)) return "desktop";
			if (Text.Equals("此电脑", StringComparison.OrdinalIgnoreCase)) return "computer";
			if (Text.Equals("网络", StringComparison.OrdinalIgnoreCase)) return "network";
			if (Text.Equals("控制面板", StringComparison.OrdinalIgnoreCase)) return "controlPanel";
			if (Text.Equals("回收站", StringComparison.OrdinalIgnoreCase)) return "recyclebin";
			if (Text.Contains("文档", StringComparison.OrdinalIgnoreCase)) return "documents";
			if (Text.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "linux";
			if (Text.Contains("下载", StringComparison.OrdinalIgnoreCase)) return "downloads";
			if (Text.Contains("音乐", StringComparison.OrdinalIgnoreCase)) return "music";
			if (Text.Contains("图片", StringComparison.OrdinalIgnoreCase)) return "pictures";
			if (Text.Contains("视频", StringComparison.OrdinalIgnoreCase)) return "videos";
			if (Text.Contains("主文件夹", StringComparison.OrdinalIgnoreCase)) return "home";
			// 检查是否为驱动器
			if (Text.Contains(":")) return "drives";
			return "folder"; // 默认返回文件夹图标
		}
		public static string GetNodeIconKey(TreeNode node)
		{
			var ico = GetIconKey(node.Text);
			Debug.Print("search icon tree key {0} -> {1}", node.Text, ico);
			return ico;

			// 如果节点包含Tag并且是ShellItem类型，可以进一步判断
			if (node.Tag is ShellItem shellItem)
			{
				SFGAO attributes = SFGAO.FOLDER;
				if ((attributes & SFGAO.FILESYSTEM) != 0)
				{
					return "folder";
				}
			}

			return "folder"; // 默认返回文件夹图标
		}
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

		public static Icon ExtractIconFromFile(string file, int iconIndex)
		{
			IntPtr hIcon = ExtractIcon(IntPtr.Zero, file, iconIndex);
			if (hIcon == IntPtr.Zero)
				return null;

			Icon icon = Icon.FromHandle(hIcon);
			return icon;
		}
		public static ImageList LoadIconsFromFile(string path)
        {
            var count = API.ExtractIconEx(path, -1, null, null, 0);
            var phiconLarge = new IntPtr[count];
            var phiconSmall = new IntPtr[count];
            var result = API.ExtractIconEx(path, 0, phiconLarge, null, count);

            ImageList imageList = new()
            {
                ImageSize = SystemInformation.IconSize
            };

            imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
            phiconLarge.ToList().ForEach(x => API.DestroyIcon(x));

            return imageList;
        }

        public static Image? LoadIcon(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                return null;
            }

            if (iconPath.ToLower().StartsWith("wcmicon"))
            {
                iconPath = Constants.ZfilePath + iconPath;
            }

            if (iconPath.Contains(","))
            {
                string[] parts = iconPath.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out int iconIndex))
                {
                    return GetIconByFilenameAndIndex(parts[0], iconIndex);
                }
                else if (parts.Length == 1)
                {
                    using var icon = Icon.ExtractAssociatedIcon(parts[0]);
                    return icon?.ToBitmap();
                }
            }
            else if (System.IO.File.Exists(iconPath))
            {
                return GetIconByFilenameAndIndex(iconPath, 0);
            }

            return null;
        }

        private static Image? GetIconByFilenameAndIndex(string path, int index)
        {
            ImageList images = LoadIconsFromFile(path);
            if (images != null && index < images.Images.Count)
            {
                return images.Images[index];
            }
            return null;
        }
    }
} 