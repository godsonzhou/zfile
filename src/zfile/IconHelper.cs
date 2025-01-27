namespace WinFormsApp1
{
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
            largeIcon = Helper.GetIconByFileName(path, true);
            if (largeIcon == null)
                largeIcon = Helper.GetIconByFileType(Path.GetExtension(path), true);


            smallIcon = Helper.GetIconByFileName(path, false);
            if (smallIcon == null)
                smallIcon = Helper.GetIconByFileType(Path.GetExtension(path), false);
        }
    }
}
