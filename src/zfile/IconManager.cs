using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;
using System.Text;

namespace WinFormsApp1
{

	public class IconManager : IDisposable
	{
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

		private readonly Dictionary<string, Icon> iconCache = new Dictionary<string, Icon>();
		private bool disposed = false;
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
		public IconManager()
		{
			InitIconCache(true);
			InitIconCache(false);
		}
		public void InitIconCache(bool islarge)
		{
			var imageresPath = Path.Combine(Environment.SystemDirectory, "imageres.dll");
			var imageList = LoadIconsFromFile(imageresPath, islarge);
			// 标准化图标尺寸
			var targetSize = islarge ? new Size(64, 64) : new Size(16, 16);

			AddIcon("drive", ResizeIcon(ConvertImageToIcon(imageList.Images[27]), targetSize), islarge);
			AddIcon("folder", ResizeIcon(ConvertImageToIcon(imageList.Images[3]), targetSize), islarge);
			AddIcon("桌面",	ResizeIcon(ConvertImageToIcon(imageList.Images[174]), targetSize), islarge);
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

		public void AddIcon(string key, Icon icon, bool islarge)
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
						newIcon.ToBitmap().Save("d:\\temp\\" + key + ".png", System.Drawing.Imaging.ImageFormat.Png);

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

		public void LoadIconFromCacheByKey(string key, ImageList l, bool islarge = false)
		{
			if (HasIconKey(key, islarge) && !l.Images.ContainsKey(key))
			{
				l.Images.Add(key, GetIcon(key, islarge));
				//Debug.Print(key + " added to imagelist : " + l.Images.Count);
			}
		}
	
		public static Icon ConvertImageToIcon(Image image)
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

		private static Icon ResizeIcon(Icon icon, Size targetSize)
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
			if (item.IsVirtual || !item.GetAttributes().HasFlag(SFGAO.FILESYSTEM))
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
			return GetIconKey((ShellItem)node.Tag);   //treenode icon key is always use small icon, so append 's' to the pure key
		}

		public static Image? LoadIcon(string iconPath)
		{
			if (string.IsNullOrEmpty(iconPath))
				return null;

			if (iconPath.ToLower().StartsWith("wcmicon"))
				iconPath = Constants.ZfilePath + iconPath;

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
			API.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
			IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

			if (IconHnd.ToString() == "0")
				return null;
			return Icon.FromHandle(IconHnd);
		}
		
		public static Icon GetIconByFileType(string fileType, bool isLarge)
		{
			if (fileType == null || fileType.Equals(string.Empty)) return null;

			RegistryKey regVersion = null;
			string regFileType = null;
			string regIconString = null;
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
			Icon resultIcon = null;
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
		private static Image? GetIconByFilenameAndIndex(string path, int index)
		{
			ImageList images = LoadIconsFromFile(path);
			if (images != null && index < images.Images.Count)
			{
				return images.Images[index];
			}
			return null;
		}
		public static Icon ExtractIconFromPIDL(IShellFolder folder, IntPtr pidl)
		{
			try
			{
				if (folder == null || pidl == IntPtr.Zero)
					return null;
				
				//Guid iExtractIconGuid = new Guid("000214EB-0000-0000-C000-000000000046");
				// 获取节点的IExtractIcon接口
				Guid iExtractIconGuid = typeof(IExtractIcon).GUID;
				IntPtr pExtractIcon;
				folder.GetUIObjectOf(IntPtr.Zero, 1, new IntPtr[] { pidl }, iExtractIconGuid, out pExtractIcon);

				if (pExtractIcon != IntPtr.Zero)
				{
					IExtractIconW extractIcon = (IExtractIconW)Marshal.GetObjectForIUnknown(pExtractIcon);
					StringBuilder iconPath = new StringBuilder(260);
					int iconIndex;
					ExtractIconFlags flags;
					extractIcon.GetIconLocation(0, iconPath, iconPath.Capacity, out iconIndex, out flags);

					IntPtr hIcon;
					extractIcon.Extract(iconPath.ToString(), (uint)iconIndex, out hIcon, out _, 0x00010000);

					if (hIcon != IntPtr.Zero)
					{
						Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
						API.DestroyIcon(hIcon);
						return icon;
					}
				}
			}
			catch { }
			return null;
		}
		
	}
}