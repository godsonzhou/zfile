namespace zfile
{
    public class MultiListListOperation : FileSourceListOperation
    {
        private readonly IMultiListFileSource _fileSource;

        public MultiListListOperation(IFileSource fileSource, string path)
            : base(fileSource, path)
        {
            Files = new List<FileInfo>();
            _fileSource = fileSource as IMultiListFileSource;
        }

        public override void MainExecute()
        {
            Files.Clear();

            bool isRootPath = FileSource.IsPathAtRoot(Path);
            var currentNode = _fileSource.FileList;
            string currentPath = FileSource.GetRootDir();

            // 在给定路径中搜索文件
            while (Path != currentPath && IsInPath(currentPath, Path, true, false))
            {
                CheckOperationState();
                bool found = false;
                for (int i = 0; i < currentNode.SubNodes.Count; i++)
                {
                    if (IsInPath(Path.Combine(currentPath, currentNode.SubNodes[i].TheFile.Name),
                                Path, true, false))
                    {
                        currentNode = currentNode.SubNodes[i];
                        found = true;
                        break;
                    }
                }
                if (!found)
                    break;
            }

            if (!isRootPath)
            {
                var file = FileSource.CreateFile(Path);
                file.Name = "..";
                if (file.SupportedProperties.HasFlag(FilePropertiesTypes.Attributes))
                    file.Attributes = FileAttributes.Directory;
                Files.Add(file);
            }

            if (Path == currentPath)
            {
                for (int i = 0; i < currentNode.SubNodes.Count; i++)
                {
                    CheckOperationState();
                    var file = currentNode.SubNodes[i].TheFile;
                    Files.Add(file);
                }
            }
        }

        private bool IsInPath(string path1, string path2, bool allowPartial, bool caseSensitive)
        {
            // 实现路径比较逻辑
            return path1.StartsWith(path2, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
    }
} 