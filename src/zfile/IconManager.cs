using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace WinFormsApp1
{
    public class IconManager
    {
        [DllImport("shell32.dll")]
        private static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public ImageList LoadIconsFromFile(string path)
        {
            var count = ExtractIconEx(path, -1, null, null, 0);
            var phiconLarge = new IntPtr[count];
            var phiconSmall = new IntPtr[count];
            var result = ExtractIconEx(path, 0, phiconLarge, null, count);

            ImageList imageList = new()
            {
                ImageSize = SystemInformation.IconSize
            };

            imageList.Images.AddRange(phiconLarge.Select(x => Icon.FromHandle(x).ToBitmap()).ToArray());
            phiconLarge.ToList().ForEach(x => DestroyIcon(x));

            return imageList;
        }

        public Image? LoadIcon(string iconPath)
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

        private Image? GetIconByFilenameAndIndex(string path, int index)
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