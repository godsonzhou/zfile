using System.IO;
namespace zfile
{
    public class FileSystemListOperation : FileSourceListOperation
    {
        public FileSystemListOperation(IFileSource fileSource, string path) 
            : base(fileSource, path)
        {
            Files = new FileEntries();
        }

        private void FlatView(string path)
        {
            try
            {
                var files = Directory.GetFileSystemEntries(path, "*");
                foreach (var file in files)
                {
                    CheckOperationState();

                    var fileName = System.IO.Path.GetFileName(file);
                    if (fileName == "." || fileName == "..")
                        continue;

                    if (Directory.Exists(file))
                    {
                        FlatView(System.IO.Path.Combine(file, System.IO.Path.DirectorySeparatorChar.ToString()));
                    }
                    else
                    {
                        var FileEntry = FileSystemFileSource.CreateFileFromFile(file);
                        Files.Add(FileEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Error in FlatView: {ex.Message}", LogOption.Error);
            }
        }

        protected override void MainExecute()
        {
            Files.Clear();

            if (FlatView)
            {
                FlatView(Path);
                return;
            }

            var isRootPath = FileSource.IsPathAtRoot(Path);

            try
            {
                var files = Directory.GetFileSystemEntries(Path, "*");
                if (files.Length == 0)
                {
                    // 没有找到文件
                    if (!isRootPath)
                    {
                        var parentFile = FileSystemFileSource.CreateFile(Path);
                        parentFile.Name = "..";
                        parentFile.Attributes = FileAttributes.Directory;
                        Files.Add(parentFile);
                    }
                }
                else
                {
                    foreach (var file in files)
                    {
                        CheckOperationState();

                        var fileName = System.IO.Path.GetFileName(file);
                        if (fileName == ".")
                            continue;

                        // 在根目录中不包含".."
                        if (fileName == ".." && isRootPath)
                            continue;

                        var FileEntry = FileSystemFileSource.CreateFileFromFile(file);
                        Files.Add(FileEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Error in MainExecute: {ex.Message}", LogOption.Error);
            }
        }

        private void CheckOperationState()
        {
            if (OperationState == OperationState.Cancelled)
                throw new OperationCanceledException();
        }
    }
} 