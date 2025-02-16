using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
using System.Collections.Generic;

namespace WinFormsApp1
{

	public static class IconManager
	{
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

		private static readonly Dictionary<string, Icon> iconCache = new Dictionary<string, Icon>();

		public static Icon ExtractIconFromFile(string file, int iconIndex)
		{
			IntPtr hIcon = ExtractIcon(IntPtr.Zero, file, iconIndex);
			if (hIcon == IntPtr.Zero)
				return null;

			Icon icon = Icon.FromHandle(hIcon);
			return icon;
		}
		public static ImageList LoadIconsFromFile(string path, bool islarge = true)
		{
			var count = API.ExtractIconEx(path, -1, null, null, 0);
			var phiconLarge = new IntPtr[count];
			var phiconSmall = new IntPtr[count];
			var result = API.ExtractIconEx(path, 0, phiconLarge, phiconSmall, count);

			ImageList imageList = new();
			if (islarge)
			{
				imageList.ImageSize = new Size(64, 64);//SystemInformation.IconSize;
				imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			}
			else
			{
				imageList.ImageSize = new Size(16, 16);
				imageList.Images.AddRange(phiconSmall.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			}
			phiconLarge.ToList().ForEach(x => API.DestroyIcon(x));
			phiconSmall.ToList().ForEach(x => API.DestroyIcon(x));
			return imageList;
		}
		public static ImageList LoadIconsFromFile1(string path)
		{
			var count = API.ExtractIconEx(path, -1, null, null, 0);
			var phiconLarge = new IntPtr[count];
			var phiconSmall = new IntPtr[count];
			API.ExtractIconEx(path, 0, phiconLarge, phiconSmall, count);

			var imageList = new ImageList
			{
				ImageSize = SystemInformation.IconSize
			};
			imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			phiconLarge.ToList().ForEach(x => API.DestroyIcon(x));
			phiconSmall.ToList().ForEach(x => API.DestroyIcon(x));
			return imageList;
		}

		public static Icon GetIconByFileName(string fileName, bool isLarge = true)
		{
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			//文件名 图标索引 
			API.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
			IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

			if (IconHnd.ToString() == "0")
				return null;
			return Icon.FromHandle(IconHnd);
		}
		/// <summary>  
		/// 根据文件扩展名（如:.*），返回与之关联的图标。
		/// 若不以"."开头则返回文件夹的图标。  
		/// </summary>  
		/// <param name="fileType">文件扩展名</param>  
		/// <param name="isLarge">是否返回大图标</param>  
		/// <returns></returns>  
		public static Icon GetIconByFileType(string fileType, bool isLarge)
		{
			if (fileType == null || fileType.Equals(string.Empty)) return null;

			RegistryKey regVersion = null;
			string regFileType = null;
			string regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";

			if (fileType[0] == '.')
			{
				//读系统注册表中文件类型信息  
				regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
				if (regVersion != null)
				{
					regFileType = regVersion.GetValue("") as string;
					regVersion.Close();
					regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
					if (regVersion != null)
					{
						regIconString = regVersion.GetValue("") as string;
						regVersion.Close();
					}
				}
				if (regIconString == null)
				{
					//没有读取到文件类型注册信息，指定为未知文件类型的图标  
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//直接指定为文件夹图标  
				regIconString = systemDirectory + "shell32.dll,3";
			}
			string[] fileIcon = regIconString.Split(new char[] { ',' });
			if (fileIcon.Length != 2)
			{
				//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标  
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
			}
			Icon resultIcon = null;
			try
			{
				//调用API方法读取图标  
				IntPtr[] phiconLarge = new IntPtr[1];
				IntPtr[] phiconSmall = new IntPtr[1];
				uint count = API.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
				IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);
				resultIcon = Icon.FromHandle(IconHnd);
			}
			catch { }
			return resultIcon;
		}
		/// <summary>
		/// 通过文件名称获取文件图标
		/// </summary>
		/// <param name="tcType">指定参数tcFullName的类型: FILE/DIR</param>
		/// <param name="tcFullName">需要获取图片的全路径文件名</param>
		/// <param name="tlIsLarge">是否获取大图标(32*32)</param>
		/// <returns></returns>
		public static Icon GetIconByFileNameEx(string tcType, string tcFullName, bool tlIsLarge = false)
		{
			Icon ico = null;

			string fileType = tcFullName.Contains(".") ? tcFullName.Substring(tcFullName.LastIndexOf('.')).ToLower() : string.Empty;

			RegistryKey regVersion = null;
			string regFileType = null;
			string regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			IntPtr hIcon = IntPtr.Zero;
			uint rst = 0;

			if (tcType == "FILE")
			{
				//含图标的文件，优先使用文件中自带图标
				if (".exe.ico".Contains(fileType))
				{
					//文件名 图标索引
					phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
					rst = API.ExtractIconEx(tcFullName, 0, phiconLarge, phiconSmall, 1);
					hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
					ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
					if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
					if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
					if (ico != null)
						return ico;
				}

				//通过文件扩展名读取图标
				regVersion = Registry.ClassesRoot.OpenSubKey(fileType, false);
				if (regVersion != null)
				{
					regFileType = regVersion.GetValue("") as string;
					regVersion.Close();
					regVersion = Registry.ClassesRoot.OpenSubKey(regFileType + @"\DefaultIcon", false);
					if (regVersion != null)
					{
						regIconString = regVersion.GetValue("") as string;
						regVersion.Close();
					}
				}
				if (regIconString == null)
				{
					//没有读取到文件类型注册信息，指定为未知文件类型的图标
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//直接指定为文件夹图标
				regIconString = systemDirectory + "shell32.dll,3";
			}

			string[] fileIcon = regIconString.Split(new char[] { ',' });
			//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
			fileIcon = fileIcon.Length == 2 ? fileIcon : new string[] { systemDirectory + "shell32.dll", "2" };

			phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
			rst = API.ExtractIconEx(fileIcon[0].Trim('\"'), Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
			hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
			ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
			if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
			if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			if (ico != null)
				return ico;

			// 对于文件，如果提取文件图标失败，则重新使用可执行文件通用图标
			if (tcType == "FILE")
			{
				//系统注册表中注册的标图不能直接提取，则返回可执行文件的通用图标
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
				phiconLarge = new IntPtr[1];
				phiconSmall = new IntPtr[1];
				rst = API.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
				hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
				ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
				if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
				if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			}

			return ico;
		}
		public static void InitializeIcons(ImageList l, bool islarge = false)
		{
			var imageresPath = Path.Combine(Environment.SystemDirectory, "imageres.dll");
			l.ColorDepth = ColorDepth.Depth32Bit;
			if (islarge) 
			{ 
				l.ImageSize = new Size(64, 64);
			}
			else 
				{ l.ImageSize = new Size(16, 16); }

			// 使用IconManager加载系统图标
			var imageList = LoadIconsFromFile(imageresPath, islarge);

			// 添加系统图标到treeViewImageList
			//l.Images.Add("desktop", LoadIcon($"{imageresPath},174")); // 桌面
			//l.Images.Add("computer", LoadIcon($"{imageresPath},104")); // 此电脑
			//l.Images.Add("network", LoadIcon($"{imageresPath},20")); // 网上邻居
			//l.Images.Add("controlPanel", LoadIcon($"{imageresPath},22")); // 控制面板
			//l.Images.Add("recyclebin", LoadIcon($"{imageresPath},49")); // 回收站49,empty recyclebin=50
			//l.Images.Add("documents", LoadIcon($"{imageresPath},107")); // 文档
			//l.Images.Add("drives", LoadIcon($"{imageresPath},27")); // 磁盘驱动器
			//l.Images.Add("linux", LoadIcon($"{imageresPath},27")); // 
			//l.Images.Add("downloads", LoadIcon($"{imageresPath},175")); // 
			//l.Images.Add("music", LoadIcon($"{imageresPath},103")); // 
			//l.Images.Add("pictures", LoadIcon($"{imageresPath},108")); // 
			//l.Images.Add("videos", LoadIcon($"{imageresPath},178")); // 
			//l.Images.Add("home", LoadIcon($"{imageresPath},83")); // 
			l.Images.Add("desktop", imageList.Images[174]); // 桌面
			l.Images.Add("computer", imageList.Images[104]); // 此电脑
			l.Images.Add("network", imageList.Images[20]); // 网上邻居
			l.Images.Add("controlPanel", imageList.Images[22]); // 控制面板
			l.Images.Add("recyclebin", imageList.Images[49]); // 回收站49,empty recyclebin=50
			l.Images.Add("documents", imageList.Images[107]); // 文档
			l.Images.Add("drives", imageList.Images[27]); // 磁盘驱动器
			l.Images.Add("linux", imageList.Images[27]); // 
			l.Images.Add("downloads", imageList.Images[175]); // 
			l.Images.Add("music", imageList.Images[103]); // 
			l.Images.Add("pictures", imageList.Images[108]); // 
			l.Images.Add("videos", imageList.Images[178]); // 
			l.Images.Add("home", imageList.Images[83]); // 

			// 添加默认文件夹图标
			Icon folderIcon = GetIconByFileType("folder", false);
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
			//Debug.Print("search icon tree key {0} -> {1}", node.Text, ico);
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
            else if (File.Exists(iconPath))
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

        public static bool HasIconKey(string key)
        {
            return iconCache.ContainsKey(key);
        }

        public static void AddIcon(string key, Icon icon)
        {
            if (!iconCache.ContainsKey(key))
            {
                iconCache[key] = icon.Clone() as Icon;
            }
        }
    }
} 