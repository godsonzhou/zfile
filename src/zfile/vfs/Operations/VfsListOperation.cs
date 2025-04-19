using System.Reflection;

namespace zfile
{
    public class VfsListOperation : FileSourceListOperation
    {
        private readonly IVfsFileSource? _vfsFileSource;

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
            if (_vfsFileSource != null)
            {
                WfxModuleList? entries = null;
                try
                {
                    entries = _vfsFileSource.VfsFileEntries;
                }
                catch
                {
                    // Ignore any errors accessing VfsFileEntries
                }

                if (entries != null)
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        CheckOperationState();
                        if (entries.Enabled[i])
                        {
                            var file = CreateFileEntry(Path);
                            file.Name = entries.Name[i];
                            file.Attributes = System.IO.FileAttributes.Normal;
                            file.LinkProperty.LinkTarget = System.IO.Path.GetFullPath(entries.FileName[i]);
                            Files.Add(file);
                        }
                    }
                }
            }

            // 处理VFS模块列表
            var moduleList = GlobalSettings.VfsModuleList;
            for (int i = 0; i < moduleList.Count; i++)
            {
                CheckOperationState();
                var vfsModule = moduleList.Objects[i];
                if (vfsModule != null)
                {
                    bool isVisible = false;
                    try
                    {
                        isVisible = vfsModule.Visible;
                    }
                    catch
                    {
                        // Ignore any errors accessing Visible property
                    }

                    if (isVisible)
                    {
                        var file = CreateFileEntry(Path);
                        file.Name = moduleList[i];

                        // Try to get icon path using reflection
                        string iconPath = string.Empty;
                        try
                        {
                            Type? fileSourceClass = null;
                            try
                            {
                                fileSourceClass = vfsModule.FileSourceClass;
                            }
                            catch
                            {
                                // Ignore any errors accessing FileSourceClass property
                            }

                            if (fileSourceClass != null)
                            {
                                // Create an instance of the file source class
                                if (System.Activator.CreateInstance(fileSourceClass) is FileSourceBase fileSourceInstance)
                                {
                                    // Try to call GetMainIcon method if it exists
                                    var method = fileSourceClass.GetMethod("GetMainIcon",
                                        BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                                    if (method != null)
                                    {
                                        if (method.GetParameters().Length == 1 &&
                                            method.GetParameters()[0].ParameterType == typeof(string).MakeByRefType())
                                        {
                                            // Method with out parameter
                                            var parameters = new object[] { iconPath };
                                            var result = method.Invoke(fileSourceInstance, parameters);
                                            if (result is bool boolResult && boolResult && parameters[0] != null)
                                            {
                                                iconPath = parameters[0]?.ToString() ?? string.Empty;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore any reflection errors
                        }

                        if (!string.IsNullOrEmpty(iconPath))
                        {
                            file.LinkProperty.LinkTarget = System.IO.Path.GetFullPath(iconPath);
                            file.Attributes = System.IO.FileAttributes.Offline;
                        }
                        else
                        {
                            file.Attributes = System.IO.FileAttributes.Directory;
                        }

                        Files.Add(file);
                    }
                }
            }
        }

        private static FileEntry CreateFileEntry(string path)
        {
            // Create a new file entry
            return new FileEntry(path);
        }
    }
}