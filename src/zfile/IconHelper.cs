using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinFormsApp1 
{
    public static class IconHelper
    {
        [DllImport("shell32.dll")]
        private static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons);

        public static ImageList LoadIconsFromFile(string path)
        {
            var count = ExtractIconEx(path, -1, null, null, 0);
            var phiconLarge = new IntPtr[count];
            var phiconSmall = new IntPtr[count];
            ExtractIconEx(path, 0, phiconLarge, null, count);
            
            var imageList = new ImageList {
                ImageSize = SystemInformation.IconSize
            };
            imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
            phiconLarge.ToList().ForEach(x => w32.DestroyIcon(x));
            
            return imageList;
        }
         /// <summary>
        /// 依据文件名读取图标，若指定文件不存在，则返回空值。  
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <param name="isLarge">是否返回大图标</param>
        /// <returns></returns>
        public static Icon GetIconByFileName(string fileName, bool isLarge = true)
        {
            int[] phiconLarge = new int[1];
            int[] phiconSmall = new int[1];
            //文件名 图标索引 
            Win32.ExtractIconEx(fileName, 0, phiconLarge, phiconSmall, 1);
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
                int[] phiconLarge = new int[1];
                int[] phiconSmall = new int[1];
                uint count = Win32.ExtractIconEx(fileIcon[0], Int32.Parse(fileIcon[1]), phiconLarge, phiconSmall, 1);
                IntPtr IconHnd = new IntPtr(isLarge ? phiconLarge[0] : phiconSmall[0]);
                resultIcon = Icon.FromHandle(IconHnd);
            }
            catch { }
            return resultIcon;
        }
    }
    
    class FileInfoList
    {
        public List<FileInfoWithIcon> list;
        public ImageList imageListLargeIcon;
        public ImageList imageListSmallIcon;


        /// <summary>
        /// 根据文件路径获取生成文件信息，并提取文件的图标
        /// </summary>
        /// <param name="filespath"></param>
        public FileInfoList(string[] filespath)
        {
            list = new List<FileInfoWithIcon>();
            imageListLargeIcon = new ImageList();
            imageListLargeIcon.ImageSize = new Size(32, 32);
            imageListSmallIcon = new ImageList();
            imageListSmallIcon.ImageSize = new Size(16, 16);
            foreach (string path in filespath)
            {
                FileInfoWithIcon file = new FileInfoWithIcon(path);
                imageListLargeIcon.Images.Add(file.largeIcon);
                imageListSmallIcon.Images.Add(file.smallIcon);
                file.iconIndex = imageListLargeIcon.Images.Count - 1;
                list.Add(file);
            }
        }
    }
    class FileInfoWithIcon
    {
        public FileInfo fileInfo;
        public Icon largeIcon;
        public Icon smallIcon;
        public int iconIndex;
        public FileInfoWithIcon(string path)
        {
            fileInfo = new FileInfo(path);
            largeIcon = IconHelper.GetIconByFileName(path, true);
            if (largeIcon == null)
                largeIcon = IconHelper.GetIconByFileType(Path.GetExtension(path), true);


            smallIcon = IconHelper.GetIconByFileName(path, false);
            if (smallIcon == null)
                smallIcon = IconHelper.GetIconByFileType(Path.GetExtension(path), false);
        }
    }
}
