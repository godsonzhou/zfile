using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP列表操作，用于获取FTP目录内容
    /// </summary>
    public class FtpListOperation : FileSourceListOperation
    {
        private readonly FtpFileSource _ftpFileSource;
        private readonly FtpListOption _listOption;

        /// <summary>
        /// 创建FTP列表操作
        /// </summary>
        /// <param name="fileSource">FTP文件源</param>
        /// <param name="path">目标路径</param>
        public FtpListOperation(FtpFileSource fileSource, string path)
            : base(fileSource, path)
        {
            _ftpFileSource = fileSource;
            _listOption = FtpListOption.Auto;
        }

        /// <summary>
        /// 设置列表选项
        /// </summary>
        /// <param name="option">FTP列表选项</param>
        public void SetListOption(FtpListOption option)
        {
            _listOption = option;
        }

        /// <summary>
        /// 执行主操作
        /// </summary>
        protected override void MainExecute()
        {
            Files.Clear();

            try
            {
                // 获取FTP目录列表
                var listing = _ftpFileSource.Client.GetListing(Path, _listOption);

                // 处理上级目录
                if (!string.IsNullOrEmpty(Path) && Path != "/" && Path != "\\")
                {
                    var parentFile = _ftpFileSource.CreateFile(Path);
                    parentFile.Name = "..";
                    parentFile.Attributes = System.IO.FileAttributes.Directory;
                    Files.Add(parentFile);
                }

                foreach (var item in listing)
                {
                    CheckOperationState();

                    if (item.Name == "." || item.Name == "..")
                        continue;

                    var fileEntry = CreateFileEntry(item);
                    Files.Add(fileEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FTP列表操作失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从FTP列表项创建文件条目
        /// </summary>
        /// <param name="item">FTP列表项</param>
        /// <returns>文件条目</returns>
        private FileEntry CreateFileEntry(FtpListItem item)
        {
            var fileEntry = _ftpFileSource.CreateFile(item.FullName);
            
            // 设置文件属性
            if (item.Type == FtpObjectType.Directory)
            {
                fileEntry.Attributes = System.IO.FileAttributes.Directory;
            }
            else
            {
                fileEntry.Size = item.Size;
            }

            // 设置时间属性
            fileEntry.LastWriteTime = item.Modified;
            
            // 设置权限属性
            var attributes = new FileAttributesProperty();
            attributes.OwnerPermissions = (int)item.OwnerPermissions;
            attributes.GroupPermissions = (int)item.GroupPermissions;
            attributes.OthersPermissions = (int)item.OthersPermissions;
            fileEntry.Properties.Add(attributes);

            return fileEntry;
        }
    }
}
