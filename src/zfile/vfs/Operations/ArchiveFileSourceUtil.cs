namespace zfile
{
    /// <summary>
    /// Archive file source utility functions
    /// </summary>
    public static class ArchiveFileSourceUtil
    {
        /// <summary>
        /// Gets an archive file source for the specified archive file
        /// </summary>
        /// <param name="sourceFileSource">Source file source</param>
        /// <param name="archiveFile">Archive file</param>
        /// <param name="archiveType">Archive type</param>
        /// <param name="archiveSign">Whether to check by archive signature</param>
        /// <param name="includeHidden">Whether to include hidden files</param>
        /// <returns>Archive file source if successful, null otherwise</returns>
        public static IArchiveFileSource GetArchiveFileSource(
            IFileSource sourceFileSource,
            FileEntry archiveFile,
            string archiveType,
            bool archiveSign,
            bool includeHidden)
        {
            if (sourceFileSource.Properties.HasFlag(FileSourceProperties.DirectAccess))
            {
                return GetArchiveFileSourceDirect(sourceFileSource, archiveFile.FullPath, archiveType, archiveSign, includeHidden);
            }

            IArchiveFileSource result = null;

            if (sourceFileSource.Properties.HasFlag(FileSourceProperties.LinkToLocalFiles))
            {
                FileEntry localArchiveFile = archiveFile.Clone();
                try
                {
                    if (sourceFileSource.GetLocalName(ref localArchiveFile))
                    {
                        ITempFileSystemFileSource tempFS = new TempFileSystemFileSource(localArchiveFile.Path);
                        // Source FileSource manages the files, not the TempFileSource
                        tempFS.DeleteOnDestroy = false;
                        // The files on temp file source are valid as long as source FileSource is valid
                        tempFS.ParentFileSource = sourceFileSource;
                        result = GetArchiveFileSourceDirect(tempFS, localArchiveFile.FullPath, archiveType, archiveSign, includeHidden);
                        // If not successful will try to get files through CopyOut below
                    }
                }
                finally
                {
                    localArchiveFile = null;
                }
            }

            if (result == null && sourceFileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.CopyOut))
            {
                // If checking by extension we don't have to unpack files yet
                // First check if there is a registered plugin for the archive extension
                if (!archiveSign &&
                    !(WcxArchiveFileSource.CheckPluginByName(archiveFile.Name) ||
                      MultiArchiveFileSource.CheckAddonByName(archiveFile.Name)))
                {
                    // No registered handlers for the archive extension
                    return null;
                }
                // else either there is a handler for the archive extension
                // or we have to unpack files first to check
                // (if creating file source by archive signature)

                try
                {
                    ITempFileSystemFileSource tempFS = new TempFileSystemFileSource();
                    FileEntries files = new FileEntries();
                    files.Add(archiveFile.Clone());

                    FileSourceOperation operation = sourceFileSource.CreateCopyOutOperation(
                        tempFS, files, tempFS.FileSystemRoot);

                    if (operation != null)
                    {
                        OperationsManager.Instance.AddOperationModal(operation);

                        if (operation.Result == FileSourceOperationResult.Finished)
                        {
                            result = GetArchiveFileSourceDirect(
                                tempFS,
                                Path.Combine(tempFS.FileSystemRoot, archiveFile.Name),
                                archiveType,
                                archiveSign,
                                includeHidden);
                        }
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an archive file source for the specified archive file (direct access only)
        /// </summary>
        /// <param name="sourceFileSource">Source file source</param>
        /// <param name="archiveFileName">Archive file name</param>
        /// <param name="archiveType">Archive type</param>
        /// <param name="archiveSign">Whether to check by archive signature</param>
        /// <param name="includeHidden">Whether to include hidden files</param>
        /// <returns>Archive file source if successful, null otherwise</returns>
        private static IArchiveFileSource GetArchiveFileSourceDirect(
            IFileSource sourceFileSource,
            string archiveFileName,
            string archiveType,
            bool archiveSign,
            bool includeHidden)
        {
            if (!sourceFileSource.Properties.HasFlag(FileSourceProperties.DirectAccess))
                return null;

            // Check if there is a registered WCX plugin for possible archive
            IArchiveFileSource? result = FileSourceManager.Instance.Find(typeof(WcxArchiveFileSource), archiveFileName) as IArchiveFileSource;
            if (result == null)
            {
                if (archiveSign)
                    result = WcxArchiveFileSource.CreateByArchiveSign(sourceFileSource, archiveFileName);
                else if (string.IsNullOrEmpty(archiveType))
                    result = WcxArchiveFileSource.CreateByArchiveName(sourceFileSource, archiveFileName);
                else
                    result = WcxArchiveFileSource.CreateByArchiveType(sourceFileSource, archiveFileName, archiveType, includeHidden);
            }

            // Check if there is a registered MultiArc addon for possible archive
            if (result == null)
            {
                result = FileSourceManager.Instance.Find(typeof(MultiArchiveFileSource), archiveFileName) as IArchiveFileSource;
                if (result == null)
                {
                    if (archiveSign)
                        result = MultiArchiveFileSource.CreateByArchiveSign(sourceFileSource, archiveFileName);
                    else if (string.IsNullOrEmpty(archiveType))
                        result = MultiArchiveFileSource.CreateByArchiveName(sourceFileSource, archiveFileName);
                    else
                        result = MultiArchiveFileSource.CreateByArchiveType(sourceFileSource, archiveFileName, archiveType);
                }
            }

            return result;
        }

        /// <summary>
        /// Tests the specified archive files
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="files">Files to test</param>
        /// <param name="queueIdentifier">Queue identifier</param>
        public static void TestArchive(FileView fileView, FileEntries files, int queueIdentifier)
        {
            try
            {
                // If in archive
                if (fileView.ActiveFileSource is IArchiveFileSource)
                {
                    FileEntries filesToTest = files.Clone();
                    if (fileView.ActiveFileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.TestArchive))
                    {
                        FileSourceOperation operation = fileView.ActiveFileSource.CreateTestArchiveOperation(filesToTest);

                        if (operation != null)
                        {
                            // Start operation
                            //var operationsManager = new OperationsManager();
                            OperationsManager.Instance.AddOperation(operation, queueIdentifier, false, true);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Not implemented", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Operation not supported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                // If filesystem
                else if (fileView.ActiveFileSource is FileSystemFileSource)
                {
                    // If archives count > 1 then put to queue
                    int queueId;
                    if (files.Count > 1 && queueIdentifier == OperationsManager.FreeOperationsQueueId)
                        queueId = OperationsManager.Instance.GetNewQueueIdentifier();
                    else
                        queueId = queueIdentifier;

                    for (int i = 0; i < files.Count; i++) // Test all selected archives
                    {
                        try
                        {
                            // Check if there is a ArchiveFileSource for possible archive
                            IArchiveFileSource archiveFileSource = GetArchiveFileSource(fileView.ActiveFileSource, files[i], string.Empty, false, true);

                            if (archiveFileSource != null)
                            {
                                // Check if List and TestArchive are supported
                                if (archiveFileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.List) &&
                                    archiveFileSource.OperationsTypes.HasFlag(FileSourceOperationTypes.TestArchive))
                                {
                                    // Get files to test
                                    FileEntries filesToTest = archiveFileSource.GetFiles(archiveFileSource.GetRootDir());

                                    if (filesToTest != null)
                                    {
                                        try
                                        {
                                            // Test all files
                                            FileSourceOperation operation = archiveFileSource.CreateTestArchiveOperation(filesToTest);

                                            if (operation != null)
                                            {
                                                // Start operation
                                                //var operationsManager = new OperationsManager();
                                                OperationsManager.Instance.AddOperation(operation, queueId, false, true);
                                            }
                                            else
                                            {
                                                MessageBox.Show("Not implemented", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            }
                                        }
                                        finally
                                        {
                                            filesToTest = null;
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Operation not supported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + Environment.NewLine + files[i].FullPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Operation not supported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                // Cleanup if needed
            }
        }

        /// <summary>
        /// Checks if the specified file is an archive
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>True if the file is an archive, false otherwise</returns>
        public static bool FileIsArchive(string fileName)
        {
            return WcxArchiveFileSource.CheckPluginByName(fileName) ||
                   MultiArchiveFileSource.CheckAddonByName(fileName);
        }

        /// <summary>
        /// Fills the file list and counts files and sizes
        /// </summary>
        /// <param name="files">Source files</param>
        /// <param name="newFiles">Output files list</param>
        /// <param name="filesCount">Output files count</param>
        /// <param name="filesSize">Output files size</param>
        public static void FillAndCount(FileEntries files, out FileEntries newFiles, out long filesCount, out long filesSize)
        {
            filesSize = 0;
            filesCount = 0;
            List<string> folderList = new List<string>();
            FileEntries folderFiles = new FileEntries();
            newFiles = new FileEntries(files.Path);

            // Process first level files
            foreach (FileEntry file in files)
            {
                if (file.IsLink)
                {
                    newFiles.Add(file.Clone());
                }
                else if (file.IsDirectory)
                {
                    folderList.Add(Path.Combine(file.Path, file.Name) + Path.DirectorySeparatorChar);
                    folderFiles.Add(file.Clone());
                }
                else
                {
                    filesCount++;
                    newFiles.Add(file.Clone());
                    filesSize += file.Size; // In first level we know file size -> use it
                }
            }

            // Add folders to the list
            foreach (FileEntry folder in folderFiles)
            {
                newFiles.Add(folder);
            }

            // Process directories recursively
            foreach (string folder in folderList)
            {
                FillAndCountRecursive(folder, newFiles, ref filesCount, ref filesSize);
            }
        }

        /// <summary>
        /// Recursively fills the file list and counts files and sizes
        /// </summary>
        /// <param name="srcPath">Source path</param>
        /// <param name="newFiles">Output files list</param>
        /// <param name="filesCount">Output files count</param>
        /// <param name="filesSize">Output files size</param>
        private static void FillAndCountRecursive(string srcPath, FileEntries newFiles, ref long filesCount, ref long filesSize)
        {
            List<string> folders = new List<string>();
            FileEntries folderFiles = new FileEntries();

            try
            {
                foreach (string filePath in Directory.GetFileSystemEntries(srcPath, "*"))
                {
                    string fileName = Path.GetFileName(filePath);
                    if (fileName == "." || fileName == "..") continue;

					//FileEntry file = new FileEntry(srcPath, fileName);
					var fileInfo = new FileInfo(filePath);
					var sr = new SearchRec
					{
						Name = fileName,
						Attributes = File.GetAttributes(filePath),
						Size = File.Exists(fileInfo.FullName) ? fileInfo.Length : 0,
						Time = fileInfo.LastWriteTime,
						PlatformTime = fileInfo.CreationTime,
						LastAccessTime = fileInfo.LastAccessTime
					};
					//var file = FileSystemFileSource.CreateFile(filePath, sr);//bugfix: 
					var file = FileSystemFileSource.CreateFile(srcPath, sr);
					if (file.IsLink)
                    {
                        newFiles.Add(file.Clone());
                    }
                    else if (file.IsDirectory)
                    {
                        folders.Add(Path.Combine(srcPath, fileName) + Path.DirectorySeparatorChar);
                        folderFiles.Add(file);
                    }
                    else
                    {
                        filesCount++;
                        newFiles.Add(file);
                        filesSize += file.Size;
                    }
                }

                // Add folders to the list
                foreach (FileEntry folder in folderFiles)
                {
                    newFiles.Add(folder);
                }

                // Process directories recursively
                foreach (string folder in folders)
                {
                    FillAndCountRecursive(folder, newFiles, ref filesCount, ref filesSize);
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Installs a plugin from the specified file
        /// </summary>
        /// <param name="fileName">Plugin file name</param>
        public static void InstallPlugin(string fileName)
        {
            if (FileIsArchive(fileName))
            {
                try
                {
                    FileEntry file = new FileEntry(Path.GetDirectoryName(fileName));
                    file.Name = Path.GetFileName(fileName);
                    // Check if there is a ArchiveFileSource for possible archive
                    IArchiveFileSource fileSource = GetArchiveFileSource(new FileSystemFileSource(), file, string.Empty, false, false);

                    if (fileSource == null)
                        throw new Exception("Error");

                    FileEntries files = fileSource.GetFiles(Path.DirectorySeparatorChar.ToString());
                    try
                    {
                        foreach (FileEntry archiveFile in files)
                        {
                            string pluginFile = archiveFile.Name;

                            if (pluginFile.Length == 12 && string.Compare(pluginFile, "pluginst.inf", true) == 0)
                            {
                                FileEntries sourceFiles = [archiveFile.Clone()];
                                ITempFileSystemFileSource temp = TempFileSystemFileSource.GetFileSource();

                                ArchiveCopyOutOperation? operation = fileSource.CreateCopyOutOperation(
                                    temp, sourceFiles, temp.GetRootDir()) as ArchiveCopyOutOperation;
                                try
                                {
									OperationsManager.Instance.AddOperation(operation);
								}
                                finally
                                {
                                    operation?.Dispose();
                                }

                                if (File.Exists(Path.Combine(temp.GetRootDir(), pluginFile)))
                                {
                                    // Read plugin installation info
                                    string iniFile = Path.Combine(temp.GetRootDir(), pluginFile);
                                    string pluginFileName = ReadIniValue(iniFile, "PluginInstall", "File", string.Empty);
                                    string extension = ReadIniValue(iniFile, "PluginInstall", "DefaultExtension", string.Empty);
                                    string pluginType = ReadIniValue(iniFile, "PluginInstall", "Type", string.Empty).ToLower();
                                    string defaultDir = Path.GetFileName(ReadIniValue(iniFile, "PluginInstall", "DefaultDir", Path.GetFileNameWithoutExtension(pluginFileName)));

                                    // Determine plugins path
                                    string pluginsPath;
                                    if (GlobalSettings.UseConfigInProgramDir)
                                        pluginsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                                    else
                                        pluginsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "plugins");

                                    string installDir = Path.Combine(Path.Combine(pluginsPath, pluginType), defaultDir);

                                    // Create plugin target directory
                                    if (Directory.CreateDirectory(installDir).Exists)
                                    {
                                        operation = fileSource.CreateCopyOutOperation(
                                            new FileSystemFileSource(), files, installDir) as ArchiveCopyOutOperation;
                                        try
                                        {
											OperationsManager.Instance.AddOperation(operation);
                                            if (operation.Result == FileSourceOperationResult.Aborted)
                                                return;
                                        }
                                        finally
                                        {
                                            operation?.Dispose();
                                        }

                                        string plugin = Path.Combine(installDir, pluginFileName);

                                        if (!CheckPlugin(plugin))
                                        {
                                            // Delete directory if plugin check failed
                                            Directory.Delete(installDir, true);
                                            return;
                                        }

                                        int result = -1;

                                        if (pluginType == "wcx")
                                        {
                                            WcxModule wcxModule = WcxPlugins.LoadModule(plugin);
                                            if (wcxModule != null)
                                            {
                                                int flags = wcxModule.PluginCapabilities;
                                                foreach (string ext in extension.Split(','))
                                                {
                                                    result = WcxPlugins.Add(ext, flags, plugin);
                                                    WcxPlugins.FileName[result] = GetPluginFilenameToSave(plugin);
                                                }
                                            }
                                        }
                                        else if (pluginType == "wdx")
                                        {
                                            result = WdxPlugins.Add(plugin);
                                            WdxPlugins.GetWdxModule(result).FileName = GetPluginFilenameToSave(plugin);
                                        }
                                        else if (pluginType == "wfx")
                                        {
                                            WfxModule wfxModule = WfxPlugins.LoadModule(plugin);
                                            if (wfxModule != null)
                                            {
                                                string rootName = wfxModule.VFSRootName;
                                                if (string.IsNullOrEmpty(rootName))
                                                {
                                                    rootName = Path.GetFileNameWithoutExtension(pluginFileName);
                                                }
                                                result = WfxPlugins.Add(rootName, plugin);
                                                WfxPlugins.SetFileName(result, GetPluginFilenameToSave(plugin));
                                            }
                                        }
                                        else if (pluginType == "wlx")
                                        {
                                            result = WlxPlugins.Add(plugin);
                                            WlxPlugins.GetWlxModule(result).FileName = GetPluginFilenameToSave(plugin);
                                        }

                                        if (result >= 0)
                                        {
                                            ShowOptions("TfrmOptionsPlugins" + char.ToUpper(pluginType[0]) + pluginType.Substring(1));
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    finally
                    {
                        files = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            MessageBox.Show("Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Reads a value from an INI file
        /// </summary>
        /// <param name="iniFile">INI file path</param>
        /// <param name="section">Section name</param>
        /// <param name="key">Key name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Value read from the INI file</returns>
        private static string ReadIniValue(string iniFile, string section, string key, string defaultValue)
        {
            try
            {
                string[] lines = File.ReadAllLines(iniFile);
                bool inSection = false;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        string sectionName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        inSection = (string.Compare(sectionName, section, true) == 0);
                    }
                    else if (inSection)
                    {
                        int equalPos = trimmedLine.IndexOf('=');
                        if (equalPos > 0)
                        {
                            string keyName = trimmedLine.Substring(0, equalPos).Trim();
                            if (string.Compare(keyName, key, true) == 0)
                                return trimmedLine.Substring(equalPos + 1).Trim();
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a plugin is valid
        /// </summary>
        /// <param name="plugin">Plugin path</param>
        /// <returns>True if the plugin is valid, false otherwise</returns>
        private static bool CheckPlugin(string plugin)
        {
            // Implementation depends on platform-specific plugin validation
            return File.Exists(plugin);
        }

        /// <summary>
        /// Gets the plugin filename to save in configuration
        /// </summary>
        /// <param name="plugin">Plugin path</param>
        /// <returns>Plugin filename to save</returns>
        private static string GetPluginFilenameToSave(string plugin)
        {
            // Implementation depends on how plugin paths are stored in configuration
            return plugin;
        }

        /// <summary>
        /// Shows the options dialog for the specified plugin type
        /// </summary>
        /// <param name="optionsFormName">Options form name</param>
        private static void ShowOptions(string optionsFormName)
        {
            // Implementation depends on how options are shown in the application
            // For example: OptionsForm.ShowPluginOptions(optionsFormName);
        }
    }
}