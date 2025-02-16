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

			// ʹ��IconManager����ϵͳͼ��
			var imageList = LoadIconsFromFile(imageresPath, islarge);

			// ���ϵͳͼ�굽treeViewImageList
			//l.Images.Add("desktop", LoadIcon($"{imageresPath},174")); // ����
			//l.Images.Add("computer", LoadIcon($"{imageresPath},104")); // �˵���
			//l.Images.Add("network", LoadIcon($"{imageresPath},20")); // �����ھ�
			//l.Images.Add("controlPanel", LoadIcon($"{imageresPath},22")); // �������
			//l.Images.Add("recyclebin", LoadIcon($"{imageresPath},49")); // ����վ49,empty recyclebin=50
			//l.Images.Add("documents", LoadIcon($"{imageresPath},107")); // �ĵ�
			//l.Images.Add("drives", LoadIcon($"{imageresPath},27")); // ����������
			//l.Images.Add("linux", LoadIcon($"{imageresPath},27")); // 
			//l.Images.Add("downloads", LoadIcon($"{imageresPath},175")); // 
			//l.Images.Add("music", LoadIcon($"{imageresPath},103")); // 
			//l.Images.Add("pictures", LoadIcon($"{imageresPath},108")); // 
			//l.Images.Add("videos", LoadIcon($"{imageresPath},178")); // 
			//l.Images.Add("home", LoadIcon($"{imageresPath},83")); // 
			l.Images.Add("desktop", imageList.Images[174]); // ����
			l.Images.Add("computer", imageList.Images[104]); // �˵���
			l.Images.Add("network", imageList.Images[20]); // �����ھ�
			l.Images.Add("controlPanel", imageList.Images[22]); // �������
			l.Images.Add("recyclebin", imageList.Images[49]); // ����վ49,empty recyclebin=50
			l.Images.Add("documents", imageList.Images[107]); // �ĵ�
			l.Images.Add("drives", imageList.Images[27]); // ����������
			l.Images.Add("linux", imageList.Images[27]); // 
			l.Images.Add("downloads", imageList.Images[175]); // 
			l.Images.Add("music", imageList.Images[103]); // 
			l.Images.Add("pictures", imageList.Images[108]); // 
			l.Images.Add("videos", imageList.Images[178]); // 
			l.Images.Add("home", imageList.Images[83]); // 

			// ���Ĭ���ļ���ͼ��
			Icon folderIcon = GetIconByFileType("folder", false);
			if (folderIcon != null)
			{
				l.Images.Add("folder", folderIcon);
			}
		}
		public static string GetIconKey(string Text)
		{
			// ���ڽڵ��ı�������ȷ��ͼ���ֵ
			if (Text.Equals("����", StringComparison.OrdinalIgnoreCase)) return "desktop";
			if (Text.Equals("�˵���", StringComparison.OrdinalIgnoreCase)) return "computer";
			if (Text.Equals("����", StringComparison.OrdinalIgnoreCase)) return "network";
			if (Text.Equals("�������", StringComparison.OrdinalIgnoreCase)) return "controlPanel";
			if (Text.Equals("����վ", StringComparison.OrdinalIgnoreCase)) return "recyclebin";
			if (Text.Contains("�ĵ�", StringComparison.OrdinalIgnoreCase)) return "documents";
			if (Text.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "linux";
			if (Text.Contains("����", StringComparison.OrdinalIgnoreCase)) return "downloads";
			if (Text.Contains("����", StringComparison.OrdinalIgnoreCase)) return "music";
			if (Text.Contains("ͼƬ", StringComparison.OrdinalIgnoreCase)) return "pictures";
			if (Text.Contains("��Ƶ", StringComparison.OrdinalIgnoreCase)) return "videos";
			if (Text.Contains("���ļ���", StringComparison.OrdinalIgnoreCase)) return "home";
			// ����Ƿ�Ϊ������
			if (Text.Contains(":")) return "drives";
			return "folder"; // Ĭ�Ϸ����ļ���ͼ��
		}
		public static string GetNodeIconKey(TreeNode node)
		{
			var ico = GetIconKey(node.Text);
			//Debug.Print("search icon tree key {0} -> {1}", node.Text, ico);
			return ico;

			// ����ڵ����Tag������ShellItem���ͣ����Խ�һ���ж�
			if (node.Tag is ShellItem shellItem)
			{
				SFGAO attributes = SFGAO.FOLDER;
				if ((attributes & SFGAO.FILESYSTEM) != 0)
				{
					return "folder";
				}
			}

			return "folder"; // Ĭ�Ϸ����ļ���ͼ��
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