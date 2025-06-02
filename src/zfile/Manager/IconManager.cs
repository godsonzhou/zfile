using Microsoft.Win32;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinShell;

namespace zfile
{

	public class IconManager : IDisposable
	{
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

		private readonly Dictionary<string, Icon> iconCache = new Dictionary<string, Icon>();
		public ImageList ImageList { get; private set; }
		private Dictionary<string, ImageList> iconsCache = new();
		private bool disposed = false;
		private Size largesize = new Size(64, 64);
		private Size smallsize = new Size(16, 16);
		private MainForm form;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 取消事件订阅
				}
				// 释放图标缓存
				ClearCache();
				// 释放非托管资源
				disposed = true;
			}
		}

		~IconManager()
		{
			Dispose(false);
		}
		public IconManager(MainForm form)
		{
			this.form = form;
			InitIconCache(true);
			InitIconCache(false);
		}
		public void InitIconCache(bool islarge)
		{
			var imageresPath = Path.Combine(Environment.SystemDirectory, "imageres.dll");
			ImageList = LoadIconsFromFile(imageresPath, islarge);
			// 标准化图标尺寸
			var targetSize = islarge ? largesize : smallsize;

			CacheIcon("drive", ResizeIcon(ConvertImageToIcon(ImageList.Images[27]), targetSize), islarge);
			CacheIcon("folder", ResizeIcon(ConvertImageToIcon(ImageList.Images[3]), targetSize), islarge);
			CacheIcon("桌面",	ResizeIcon(ConvertImageToIcon(ImageList.Images[105]), targetSize), islarge);

			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\0", ResizeIcon(ConvertImageToIcon(ImageList.Images[22]), targetSize), islarge);//0 所有控制面板项
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\1", ResizeIcon(ConvertImageToIcon(ImageList.Images[186]), targetSize), islarge);//1 外观和个性化
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\2", ResizeIcon(ConvertImageToIcon(ImageList.Images[185]), targetSize), islarge);//2 硬件和声音
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\3", ResizeIcon(ConvertImageToIcon(ImageList.Images[114]), targetSize), islarge);//3 parsepath	"网络和Internet"	
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\5", ResizeIcon(ConvertImageToIcon(ImageList.Images[184]), targetSize), islarge);//5 系统和安全
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\6", ResizeIcon(ConvertImageToIcon(ImageList.Images[138]), targetSize), islarge);//6 时钟和区域
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\7", ResizeIcon(ConvertImageToIcon(ImageList.Images[81]), targetSize), islarge);//7 轻松使用
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\8", ResizeIcon(ConvertImageToIcon(ImageList.Images[82]), targetSize), islarge);//8 程序
			CacheIcon("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\9", ResizeIcon(ConvertImageToIcon(ImageList.Images[83]), targetSize), islarge);//9 用户账户

			//var idx = 0;
			//foreach (Image image in imageList.Images) 
			//{
			//	AddIcon(($"{imageresPath}_{idx}") + (islarge ? 'L' : 'S'), ConvertImageToIcon(image));
			//	idx ++;
			//}
		}

		public bool HasIconKey(string key, bool islarge)
		{
			var subkey = islarge ? "l" : "s";
			return iconCache.ContainsKey($"{key}___{subkey}".ToLower());
		}

		public void ClearCache()
		{
			foreach (var icon in iconCache.Values)
			{
				icon.Dispose();
			}
			iconCache.Clear();
		}

		public void CacheIcon(string key, Icon icon, bool islarge)
		{
			if (icon == null) return;

			var subkey = islarge ? "___l" : "___s";
			key = (key+subkey).ToLower();
			if (!iconCache.ContainsKey(key))
			{
				try
				{
					// 使用Bitmap保持透明通道
					using (var bitmap = icon.ToBitmap())
					{
						if (bitmap == null)
						{
							Debug.WriteLine("图标缓存失败: 无法将图标转换为Bitmap");
							return;
						}

						// 检查Bitmap的像素格式
						if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
						{
							Debug.WriteLine("图标缓存失败: Bitmap的像素格式无效");
							return;
						}

						IntPtr hIcon = bitmap.GetHicon();
						if (hIcon == IntPtr.Zero)
						{
							Debug.WriteLine("图标缓存失败: 无法从Bitmap获取图标句柄");
							return;
						}

						Icon newIcon = Icon.FromHandle(hIcon);
						if (newIcon == null)
						{
							Debug.WriteLine("图标缓存失败: 无法从句柄创建图标");
							return;
						}
						//save current icon to png file
						//newIcon.ToBitmap().Save("d:\\temp\\" + key + ".png", System.Drawing.Imaging.ImageFormat.Png);

						iconCache[key] = (Icon)newIcon.Clone();
						API.DestroyIcon(hIcon); // 确保销毁图标句柄
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"图标缓存失败: {ex.Message}");
				}
			}
		}

		public Icon GetIcon(string key, bool islarge)
		{
			var subkey = islarge ? "___l" : "___s";
			return iconCache[(key + subkey).ToLower()];
		}

		public void LoadIconFromCacheByKey(string key, ImageList? l, bool islarge = false)
		{
			if (l == null) return;
			if (HasIconKey(key, islarge) && !l.Images.ContainsKey(key))
			{
				l.Images.Add(key, GetIcon(key, islarge));
				//Debug.Print(key + " added to imagelist : " + l.Images.Count);
			}
		}
	
		public static Icon? ConvertImageToIcon(Image image)
		{
			// 创建32位ARGB格式的Bitmap保持透明通道
			//Debug.Print(image.Width + " " + image.Height);
			using (Bitmap srcBmp = new Bitmap(image)) 
			using (Bitmap argbBmp = new Bitmap(srcBmp.Width, srcBmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) 
			{
				using (Graphics g = Graphics.FromImage(argbBmp))
				{
					g.DrawImage(srcBmp, new Rectangle(0, 0, argbBmp.Width, argbBmp.Height));
				}

				IntPtr hIcon = argbBmp.GetHicon();
				try
				{
					return Icon.FromHandle(hIcon).Clone() as Icon;
				}
				finally
				{
					API.DestroyIcon(hIcon);
				}
			}
		}

		private static Icon? ResizeIcon(Icon icon, Size targetSize)
		{
			if (icon.Size == targetSize)
				return icon;

			using (Bitmap bitmap = new Bitmap(targetSize.Width, targetSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			{
				using (Graphics g = Graphics.FromImage(bitmap))
				{
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
					g.DrawImage(icon.ToBitmap(), new Rectangle(0, 0, targetSize.Width, targetSize.Height));
				}

				IntPtr hIcon = bitmap.GetHicon();
				try
				{
					return Icon.FromHandle(hIcon).Clone() as Icon;
				}
				finally
				{
					API.DestroyIcon(hIcon);
				}
			}
		}

		public static void InitializeIcons(ImageList l, bool islarge = false)
		{
			l.ColorDepth = ColorDepth.Depth32Bit;
			if (islarge)
				l.ImageSize = new Size(64, 64);
			else
				l.ImageSize = new Size(16, 16);
		}
		public static string GetIconKey(ShellItem item)
		{
			if(item==null) return string.Empty;
			if (item.IsVirtual || !item.attr.HasFlag(SFGAO.FILESYSTEM))
				return item.IconKey;
			else
			{
				if (item.Name.Contains(':'))
					return "drive";
				return "folder";
			}
		}

		public static string GetNodeIconKey(TreeNode node)
		{
			if(node.Tag is ShellItem)
				return GetIconKey((ShellItem)node.Tag);   //treenode icon key is always use small icon, so append 's' to the pure key
			return string.Empty;
		}
	
		public Image? LoadIcon(string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			if (path.ToLower().StartsWith("wcmicon"))
				path = Constants.ZfileCfgPath + path;

			if (path.Contains(","))
			{
				string[] parts = path.Split(',');
				if (parts.Length == 2 && int.TryParse(parts[1], out int iconIndex))
					return GetIconByFilenameAndIndex(parts[0], iconIndex);
				Debug.Print("icon path.length > 2?! pls check");
			}
			if (File.Exists(path))
			{
				//return GetIconByFilenameAndIndex(iconPath, 0);
				using var icon = Icon.ExtractAssociatedIcon(path);
				return icon?.ToBitmap();
			}
			
			//TODO: check windows\system32 from env:
			var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
			path = Path.Combine(system32, path);
			if (File.Exists(path))
			{
				//return GetIconByFilenameAndIndex(iconPath, 0);
				using var icon = Icon.ExtractAssociatedIcon(path);
				return icon?.ToBitmap();
			}
			return null;
		}
		public static Icon ExtractIconFromFile(string file, int iconIndex)
		{
			IntPtr hIcon = ExtractIcon(IntPtr.Zero, file, iconIndex);
			if (hIcon == IntPtr.Zero)
				return null;

			Icon icon = Icon.FromHandle(hIcon);
			return icon;
		}
		public ImageList LoadIconsFromFile(string path, bool islarge = true)
		{
			var pathkey = path + (islarge ? "|L" : "|S");
			if (iconsCache.TryGetValue(pathkey, out var icons)) { return icons; }
			var count = API.ExtractIconEx(path, -1, null, null, 0);
			var phiconLarge = new IntPtr[count];
			var phiconSmall = new IntPtr[count];
			var result = API.ExtractIconEx(path, 0, phiconLarge, phiconSmall, count);

			ImageList imageList = new();
			if (islarge)
			{
				imageList.ImageSize = largesize;//SystemInformation.IconSize;
				imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			}
			else
			{
				imageList.ImageSize = smallsize;
				imageList.Images.AddRange(phiconSmall.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
			}
			phiconLarge.ToList().ForEach(x => API.DestroyIcon(x));
			phiconSmall.ToList().ForEach(x => API.DestroyIcon(x));
			iconsCache[pathkey] = imageList;
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
		public static Icon[] ExtractIconsFromFile(string filePath)
		{
			// 这里需要实现从DLL或EXE文件中提取图标的逻辑
			// 可以使用Win32 API或第三方库
			// 简化起见，这里返回一个空数组
			List<Icon> icons = new();
			var imagelist = LoadIconsFromFile1(filePath);
			foreach (var i in imagelist.Images.Cast<Image>())
			{
				var icon = ConvertImageToIcon(i);
				icons.Add(icon);
			}
			return icons.ToArray();
		}

		public static Icon? GetIconByFileName(string fileName, bool isLarge = true)
		{
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			API.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
			IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

			if (IconHnd.ToString() == "0")
				return null;
			return Icon.FromHandle(IconHnd);
		}
		
		public static Icon? GetIconByFileType(string fileType, bool isLarge)
		{
			if (fileType == null || fileType.Equals(string.Empty)) return null;

			RegistryKey? regVersion = null;
			string? regFileType = null;
			string? regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";

			if (fileType[0] == '.')
			{
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
					regIconString = systemDirectory + "shell32.dll,0";
			}
			else
				regIconString = systemDirectory + "shell32.dll,3";
			string[] fileIcon = regIconString.Split(new char[] { ',' });
			if (fileIcon.Length != 2)
			{
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
			}
			Icon? resultIcon = null;
			try
			{
				IntPtr[] phiconLarge = new IntPtr[1];
				IntPtr[] phiconSmall = new IntPtr[1];
				uint count = API.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
				IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);
				resultIcon = Icon.FromHandle(IconHnd);
			}
			catch { }
			return resultIcon;
		}
		
		public static Icon? GetIconByFileNameEx(string tcType, string tcFullName, bool tlIsLarge = false)
		{
			Icon? ico = null;

			string fileType = tcFullName.Contains(".") ? tcFullName.Substring(tcFullName.LastIndexOf('.')).ToLower() : string.Empty;

			RegistryKey? regVersion = null;
			string? regFileType = null;
			string? regIconString = null;
			string systemDirectory = Environment.SystemDirectory + "\\";
			IntPtr[] phiconLarge = new IntPtr[1];
			IntPtr[] phiconSmall = new IntPtr[1];
			IntPtr hIcon = IntPtr.Zero;
			uint rst = 0;

			if (tcType == "FILE")
			{
				if (".exe.ico".Contains(fileType))
				{
					phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
					rst = API.ExtractIconEx(tcFullName, 0, phiconLarge, phiconSmall, 1);
					hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
					ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
					if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
					if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
					if (ico != null)
						return ico;
				}

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
					regIconString = systemDirectory + "shell32.dll,0";
			}
			else
			{
				regIconString = systemDirectory + "shell32.dll,3";
			}

			string[] fileIcon = regIconString.Split(new char[] { ',' });
			fileIcon = fileIcon.Length == 2 ? fileIcon : new string[] { systemDirectory + "shell32.dll", "2" };

			phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
			rst = API.ExtractIconEx(fileIcon[0].Trim('\"'), Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
			hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
			ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
			if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
			if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			if (ico != null)
				return ico;

			if (tcType == "FILE")
			{
				fileIcon = [ systemDirectory + "shell32.dll", "2" ];
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
		private Image? GetIconByFilenameAndIndex(string path, int index)
		{
			if (iconsCache.TryGetValue(path, out ImageList imglst))
				if(index < imglst.Images.Count)
					return imglst.Images[index];
			ImageList images = LoadIconsFromFile(path);
			if (images != null )
			{
				iconsCache[path] = images;
				if(index < images.Images.Count)
					return images.Images[index];
			}
			return null;
		}
		public static Icon? ExtractIconFromPIDL(IShellFolder folder, IntPtr pidl, out string iconkey)
		{
			iconkey = string.Empty;
			try
			{
				if (folder == null || pidl == IntPtr.Zero)
					return null;
				
				//Guid iExtractIconGuid = new Guid("000214EB-0000-0000-C000-000000000046");
				// 获取节点的IExtractIcon接口
				Guid iExtractIconGuid = typeof(IExtractIcon).GUID;
				IntPtr[] pidls = new IntPtr[] { pidl };
				//IntPtr pExtractIcon;
				var hr = folder.GetUIObjectOf(IntPtr.Zero, 1, pidls, ref iExtractIconGuid, out nint pExtractIcon);

				if (hr == 0 && pExtractIcon != IntPtr.Zero)
				{
					IExtractIcon extractIcon = (IExtractIcon)Marshal.GetObjectForIUnknown(pExtractIcon);
					StringBuilder iconPath = new (260);
					//int iconIndex;
					//ExtractIconFlags flags;
					extractIcon.GetIconLocation(0, iconPath, iconPath.Capacity, out int iconIndex, out uint flags);

					//IntPtr hIcon;
					extractIcon.Extract(iconPath.ToString(), iconIndex, out nint hIcon, out var hIconSmall, 0x00010000);

					if (hIcon != IntPtr.Zero)
					{
						iconkey = $"{(hIcon != 0 ? hIcon : hIconSmall)}_{iconIndex}";
						Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
						API.DestroyIcon(hIcon);
						return icon;
					}
				}
			}
			catch { }
			return null;
		}
		//调用.NET内部提供的ExtractAssociatedIcon方法，只能从文件获取一种规格的ICON图标，一般是Size(32,32)
		public static System.Drawing.Icon? GetIconFromFile(string fileName)
		{
			if (System.IO.File.Exists(fileName) == false)
				return null;
			return System.Drawing.Icon.ExtractAssociatedIcon(fileName);
		}
		//调用Win32 API提供的ExtractIconEx方法，可以从文件获取多种规格的ICON图标
		[DllImport("shell32.dll")]
		public static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

		/// <summary>
		/// 从应用程序文件中获取Icon图标
		/// </summary>
		/// <param name="appPath">应用程序文件名</param>
		/// <returns>返回获取到的Icon图标集，顺序为图标A Size(32,32)、图标A Size(16,16)、图标B Size(32,32)、图标B Size(16,16)....</returns>
		public static System.Drawing.Icon[] GetIconFromAPP(string appPath)
		{
			int count = ExtractIconEx(appPath, -1, null, null, 0);
			IntPtr[] largeIcons = new IntPtr[count];
			IntPtr[] smallIcons = new IntPtr[count];
			ExtractIconEx(appPath, 0, largeIcons, smallIcons, count);
			System.Drawing.Icon[] icons = new System.Drawing.Icon[count * 2];
			for (int i = 0; i < count; i++)
			{
				icons[i * 2] = System.Drawing.Icon.FromHandle(largeIcons[i]);
				icons[i * 2 + 1] = System.Drawing.Icon.FromHandle(smallIcons[i]);
			}
			return icons;
		}
		
		/// <summary>
		 /// 获取文件的图标索引号
		 /// </summary>
		 /// <param name="fileName">文件名</param>
		 /// <returns>图标索引号</returns>
		public static int GetIconIndex(string fileName)
		{
			//从API调用SHGetFileInfo获取图标索引号，调用SHGetImageList获取图标列表，再从图标列表中取指定索引号的图标。可获取文件或文件夹的图标，有6种不同参数，目前可提取4种规格图标【Size(16,16)、Size(32,32)、Size(48,48)、Size(256,256)】，最全面的方式，推荐
			SHFILEINFO info = new SHFILEINFO();
			IntPtr iconIntPtr = API.SHGetFileInfo(fileName, 0, ref info, Marshal.SizeOf(info), (SHGFI.SYSICONINDEX | SHGFI.OPENICON));
			if (iconIntPtr == IntPtr.Zero)
				return -1;
			return info.iIcon;
		}

		/// <summary>
		/// 根据图标索引号获取图标
		/// </summary>
		/// <param name="iIcon">图标索引号</param>
		/// <param name="flag">图标尺寸标识</param>
		/// <returns></returns>
		public static System.Drawing.Icon GetIcon(int iIcon, SHIL flag)
		{
			IImageList? list = null;
			Guid theGuid = Guids.IID_IIMAGELIST; //new Guid(IID_IImageList);//目前所知用IID_IImageList2也是一样的
			API.SHGetImageList(flag, ref theGuid, ref list);//获取系统图标列表
			IntPtr hIcon = IntPtr.Zero;
			int r = list.GetIcon(iIcon, Constants.ILD_TRANSPARENT | Constants.ILD_IMAGE, ref hIcon);//获取指定索引号的图标句柄
			return System.Drawing.Icon.FromHandle(hIcon);
		}

		/// <summary>
		///  方法3：从文件获取Icon图标
		/// </summary>
		/// <param name="fileName">文件名称</param>
		/// <param name="flag">图标尺寸标识</param>
		/// <returns></returns>
		public static System.Drawing.Icon GetIconFromFile(string fileName, SHIL flag)
		{
			return GetIcon(GetIconIndex(fileName), flag);
		}/// <summary>
		 /// 根据文件名得到系统图标（经修改参数后文件夹也可以）
		 /// </summary>
		 /// <param name="fileName">文件名</param>
		 /// <param name="largeIcon">图标的大小</param>
		 /// <returns></returns>
		public static System.Drawing.Icon GetFileIcon(string fileName, bool largeIcon)
		{
			//直接从API调用SHGetFileInfo获取文件或文件夹的图标，只能获取2种规格【Size(16,16)、Size(32,32)】
			SHFILEINFO info = new SHFILEINFO();
			int size = Marshal.SizeOf(info);
			SHGFI flags;
			if (largeIcon)
				flags = SHGFI.ICON | SHGFI.LARGEICON;//| SHGFI.UseFileAttributes;网上都有加这项导致只对文件有效，去掉后文件夹也可以。
			else
				flags = SHGFI.ICON | SHGFI.SMALLICON;//| SHGFI.UseFileAttributes;网上都有加这项导致只对文件有效，去掉后文件夹也可以。
			IntPtr iconIntPtr = API.SHGetFileInfo(fileName, 0, ref info, size, flags);
			if (iconIntPtr.Equals(IntPtr.Zero))
				return null;
			return System.Drawing.Icon.FromHandle(info.hIcon);
		}
	}
}