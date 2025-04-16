namespace zfile
{
    public class VfsListOperation : FileSourceListOperation
    {
        private readonly IVfsFileSource _vfsFileSource;

        public VfsListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new FileEntries(path);
            _vfsFileSource = fileSource as IVfsFileSource;
        }

        protected override void MainExecute()
        {
            Files.Clear();

            // 处理VFS文件列表
            for (int i = 0; i < _vfsFileSource.VfsFileEntries.Count; i++)
            {
                CheckOperationState();
                if (_vfsFileSource.VfsFileEntries.Enabled[i])
                {
                    var file = VfsFileSource.CreateFile(Path);
                    file.Name = _vfsFileSource.VfsFileEntries.Name[i];
                    file.Attributes = FileAttributes.Normal | FileAttributes.Virtual;
                    file.LinkProperty.LinkTo = Path.GetFullPath(_vfsFileSource.VfsFileEntries.FileName[i]);
                    Files.Add(file);
                }
            }

            // 处理VFS模块列表
            for (int i = 0; i < GlobalSettings.VfsModuleList.Count; i++)
            {
                CheckOperationState();
                var vfsModule = (VfsModule)GlobalSettings.VfsModuleList.Objects[i];
                if (vfsModule.Visible)
                {
                    var file = VfsFileSource.CreateFile(Path);
                    file.Name = GlobalSettings.VfsModuleList[i];
                    string path;
                    if (vfsModule.FileSourceClass.GetMainIcon(out path))
                    {
                        file.LinkProperty.LinkTo = Path.GetFullPath(path);
                        file.Attributes = FileAttributes.Offline | FileAttributes.Virtual;
                    }
                    Files.Add(file);
                }
            }
        }
    }
} 