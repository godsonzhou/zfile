using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinShell;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;

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
			//iconCache.Clear();
			var imageresPath = Path.Combine(Environment.SystemDirectory, "imageres.dll");
			var imageList = LoadIconsFromFile(imageresPath, islarge);
			AddIcon("drive" + (islarge ? 'l' : 's'), ConvertImageToIcon(imageList.Images[27]));
			AddIcon("folder" + (islarge ? 'l' : 's'), ConvertImageToIcon(imageList.Images[3]));
			AddIcon("桌面" + (islarge ? 'l' : 's'), ConvertImageToIcon(imageList.Images[174]));
			//var idx = 0;
			//foreach (Image image in imageList.Images) 
			//{
			//	AddIcon(($"{imageresPath}_{idx}") + (islarge ? 'L' : 'S'), ConvertImageToIcon(image));
			//	idx ++;
			//}
		}

		public bool HasIconKey(string key)
		{
			return iconCache.ContainsKey(key.ToLower());
		}

		public void ClearCache()
		{
			foreach (var icon in iconCache.Values)
			{
				icon.Dispose();
			}
			iconCache.Clear();
		}

		public void AddIcon(string key, Icon icon)
		{
			if (icon == null) return;
			
			key = key.ToLower();
			if (!iconCache.ContainsKey(key))
			{
				// 创建完全独立的图标副本（增加异常处理）
				//iconCache.Add(key, icon.Clone() as Icon);
				try
				{
					using (var ms = new MemoryStream())
					{
						icon.Save(ms);
						ms.Position = 0;
						iconCache[key] = new Icon(ms);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"图标缓存失败: {ex.Message}");
				}
			}
		}

	
		public Icon GetIcon(string key)
		{
			return iconCache[key.ToLower()];
		}

	
		public void LoadIconFromCacheByKey(string key, ImageList l)
		{
			if (HasIconKey(key) && !l.Images.ContainsKey(key))
			{
				l.Images.Add(key, iconCache[key]);
			}
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
			//�ļ��� ͼ������ 
			API.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
			IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);

			if (IconHnd.ToString() == "0")
				return null;
			return Icon.FromHandle(IconHnd);
		}
		/// <summary>  
		/// �����ļ���չ������:.*����������֮������ͼ�ꡣ
		/// ������"."��ͷ�򷵻��ļ��е�ͼ�ꡣ  
		/// </summary>  
		/// <param name="fileType">�ļ���չ��</param>  
		/// <param name="isLarge">�Ƿ񷵻ش�ͼ��</param>  
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
				//��ϵͳע������ļ�������Ϣ  
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
					//û�ж�ȡ���ļ�����ע����Ϣ��ָ��Ϊδ֪�ļ����͵�ͼ��  
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//ֱ��ָ��Ϊ�ļ���ͼ��  
				regIconString = systemDirectory + "shell32.dll,3";
			}
			string[] fileIcon = regIconString.Split(new char[] { ',' });
			if (fileIcon.Length != 2)
			{
				//ϵͳע�����ע��ı�ͼ����ֱ����ȡ���򷵻ؿ�ִ���ļ���ͨ��ͼ��  
				fileIcon = new string[] { systemDirectory + "shell32.dll", "2" };
			}
			Icon resultIcon = null;
			try
			{
				//����API������ȡͼ��  
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
		/// ͨ���ļ����ƻ�ȡ�ļ�ͼ��
		/// </summary>
		/// <param name="tcType">ָ������tcFullName������: FILE/DIR</param>
		/// <param name="tcFullName">��Ҫ��ȡͼƬ��ȫ·���ļ���</param>
		/// <param name="tlIsLarge">�Ƿ��ȡ��ͼ��(32*32)</param>
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
				//��ͼ����ļ�������ʹ���ļ����Դ�ͼ��
				if (".exe.ico".Contains(fileType))
				{
					//�ļ��� ͼ������
					phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
					rst = API.ExtractIconEx(tcFullName, 0, phiconLarge, phiconSmall, 1);
					hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
					ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
					if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
					if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
					if (ico != null)
						return ico;
				}

				//ͨ���ļ���չ����ȡͼ��
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
					//û�ж�ȡ���ļ�����ע����Ϣ��ָ��Ϊδ֪�ļ����͵�ͼ��
					regIconString = systemDirectory + "shell32.dll,0";
				}
			}
			else
			{
				//ֱ��ָ��Ϊ�ļ���ͼ��
				regIconString = systemDirectory + "shell32.dll,3";
			}

			string[] fileIcon = regIconString.Split(new char[] { ',' });
			//ϵͳע�����ע��ı�ͼ����ֱ����ȡ���򷵻ؿ�ִ���ļ���ͨ��ͼ��
			fileIcon = fileIcon.Length == 2 ? fileIcon : new string[] { systemDirectory + "shell32.dll", "2" };

			phiconLarge[0] = phiconSmall[0] = IntPtr.Zero;
			rst = API.ExtractIconEx(fileIcon[0].Trim('\"'), Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
			hIcon = tlIsLarge ? phiconLarge[0] : phiconSmall[0];
			ico = hIcon == IntPtr.Zero ? null : Icon.FromHandle(hIcon).Clone() as Icon;
			if (phiconLarge[0] != IntPtr.Zero) API.DestroyIcon(phiconLarge[0]);
			if (phiconSmall[0] != IntPtr.Zero) API.DestroyIcon(phiconSmall[0]);
			if (ico != null)
				return ico;

			// �����ļ��������ȡ�ļ�ͼ��ʧ�ܣ�������ʹ�ÿ�ִ���ļ�ͨ��ͼ��
			if (tcType == "FILE")
			{
				//ϵͳע�����ע��ı�ͼ����ֱ����ȡ���򷵻ؿ�ִ���ļ���ͨ��ͼ��
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
		private static Icon ConvertImageToIcon(Image image)
		{
			using (Bitmap bmp = new Bitmap(image))
			{
				IntPtr hIcon = bmp.GetHicon();
				try
				{
					return Icon.FromHandle(hIcon);
				}
				finally
				{
					API.DestroyIcon(hIcon);
				}
			}
		}
	
		public static void InitializeIcons(ImageList l, bool islarge = false)
		{
			// ����ϵͳͼ�굽treeViewImageList
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
			return GetIconKey((ShellItem)node.Tag) + "s";   //treenode icon key is always use small icon, so append 's' to the pure key
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

	}
}