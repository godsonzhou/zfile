using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// File source utility functions
    /// </summary>
    public static class FileSourceUtil
    {
        /// <summary>
        /// Decides what should be done when user chooses a file in a file view.
        /// This function may add/remove a file source from the view,
        /// change path, execute a file or a command, etc.
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="fileSource">File source</param>
        /// <param name="file">File</param>
        public static void ChooseFile(FileView fileView, IFileSource fileSource, FileEntry file)
        {
            // First test for file sources
            if (ChooseFileSource(fileView, fileSource, file))
                return;

            // For now work only for local files
            if (fileSource.Properties.HasFlag(FileSourceProperties.DirectAccess) || 
                fileSource.Properties.HasFlag(FileSourceProperties.LinkToLocalFiles))
            {
                // Now test if exists Open command in "extassoc.xml"
                string cmd = string.Empty;
                string parameters = string.Empty;
                string startPath = string.Empty;

                if (Globals.Extensions.GetExtActionCmd(file, "open", out cmd, out parameters, out startPath))
                {
                    try
                    {
                        // Resolve filename here since ProcessExtCommandFork doesn't do it (as of 2017)
                        // The limitation is that only one file will be opened on a FileSource of links
                        FileEntry fileCopy = null;
                        if (fileSource.Properties.HasFlag(FileSourceProperties.LinkToLocalFiles))
                        {
                            fileCopy = file.Clone();
                            fileSource.GetLocalName(fileCopy);
                        }

                        if (ProcessExtCommandFork(cmd, parameters, startPath, fileCopy))
                            return;
                    }
                    finally
                    {
                        // Cleanup if needed
                    }
                }

                if (fileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.CalcChecksum) && 
                    FileExtIsHash(file.Extension))
                {
                    ProcessExtCommandFork("cm_CheckSumVerify");
                    return;
                }
            }

            if (fileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.Execute))
            {
                try
                {
                    FileEntry fileCopy = file.Clone();
                    IFileSourceOperation operation = fileSource.CreateExecuteOperation(
                        fileCopy, fileView.CurrentPath, "open") as FileSourceExecuteOperation;

                    if (operation != null)
                    {
                        operation.Execute();
                        switch (((FileSourceExecuteOperation)operation).ExecuteOperationResult)
                        {
                            case FileSourceExecuteOperationResult.Error:
                                // Show error message
                                if (string.IsNullOrEmpty(operation.ResultString))
                                    MessageBox.Show("Error opening file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                else
                                    MessageBox.Show(operation.ResultString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;

                            case FileSourceExecuteOperationResult.Yourself:
                                // Copy out file to temp file system and execute
                                if (!ShowFileExecuteYourSelf(fileView, file, false))
                                    Debug.WriteLine("Execution error!");
                                break;

                            case FileSourceExecuteOperationResult.WithAll:
                                // Copy out all files to temp file system and execute chosen
                                if (!ShowFileExecuteYourSelf(fileView, file, true))
                                    Debug.WriteLine("Execution error!");
                                break;

                            case FileSourceExecuteOperationResult.SymLink:
                                // change directory to new path (returned in Operation.ResultString)
                                Debug.WriteLine("Change directory to " + operation.ResultString);
                                if (operation.ResultString.Contains("://"))
                                {
                                    ChooseFileSource(fileView, operation.ResultString);
                                }
                                else if (fileSource is FileSystemFileSource ||
                                        !Directory.Exists(Path.GetDirectoryName(operation.ResultString)))
                                {
                                    // Simply change path
                                    fileView.CurrentPath = operation.ResultString;
                                }
                                else
                                {
                                    // Get a new filesystem file source
                                    fileView.AddFileSource(FileSystemFileSource.GetFileSource(), operation.ResultString);
                                }
                                break;
                        }
                    }
                }
                finally
                {
                    // Cleanup if needed
                }
            }
        }

        /// <summary>
        /// Checks if choosing the given file will change to another file source,
        /// and adds this new file source to the view if it does.
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="fileSource">File source</param>
        /// <param name="file">File</param>
        /// <returns>True if the file matched any rules and a new file source was created,
        /// false otherwise, which means no action was taken.</returns>
        public static bool ChooseFileSource(FileView fileView, IFileSource fileSource, FileEntry file)
        {
            if (ChooseArchive(fileView, fileSource, file))
                return true;

            // Work only for VfsFileSource
            if (fileView.FileSource is VfsFileSource)
            {
                // Check if there is a registered WFX plugin by file system root name
                IFileSource newFileSource = FileSourceManager.Find(typeof(WfxPluginFileSource), "wfx://" + file.Name);
                if (newFileSource == null)
                    newFileSource = WfxPluginFileSource.CreateByRootName(file.Name);

                if (newFileSource == null)
                {
                    // Check if there is a registered Vfs module by file system root name
                    VfsModule vfsModule = VfsModuleList.VfsModule[file.Name];
                    if (vfsModule != null)
                    {
                        newFileSource = FileSourceManager.Find(vfsModule.FileSourceClass, file.Name);
                        if (newFileSource == null)
                            newFileSource = Activator.CreateInstance(vfsModule.FileSourceClass) as IFileSource;
                    }
                }

                if (newFileSource != null)
                {
                    fileView.AddFileSource(newFileSource, newFileSource.GetRootDir());
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses a path and returns the appropriate file source
        /// </summary>
        /// <param name="path">Path to parse</param>
        /// <param name="currentFileSource">Current file source</param>
        /// <returns>File source for the path</returns>
        public static IFileSource ParseFileSource(ref string path, IFileSource currentFileSource = null)
        {
            Type fileSourceClass = VfsModuleList.GetFileSource(path);
            // If found special FileSource for path
            if (fileSourceClass != null)
            {
                // If path is URI
                if (path.Contains("://"))
                {
                    Uri uri = new Uri(path);
                    path = NormalizePath(uri.LocalPath);
                    path = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;

                    IFileSource result = FileSourceManager.Find(
                        fileSourceClass,
                        uri.Scheme + "://" + uri.Host,
                        !string.Equals(uri.Scheme, "smb", StringComparison.OrdinalIgnoreCase));

                    if (result == null)
                    {
                        try
                        {
                            // Create new FileSource with given URI
                            result = Activator.CreateInstance(fileSourceClass, uri) as IFileSource;
                        }
                        catch
                        {
                            result = null;
                        }
                    }

                    return result;
                }
                // If found FileSource is same as current then simply change path
                else if (fileSourceClass.Name == currentFileSource?.GetType().Name)
                {
                    return currentFileSource;
                }
                // Else create new FileSource with given path
                else
                {
                    return Activator.CreateInstance(fileSourceClass) as IFileSource;
                }
            }

            return null;
        }

        /// <summary>
        /// Chooses a file source based on the path
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="path">Path</param>
        /// <param name="local">Whether the path is local</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ChooseFileSource(FileView fileView, string path, bool local = false)
        {
            string remotePath = path;
            IFileSource fileSource = ParseFileSource(ref remotePath, fileView.FileSource);

            // If found special FileSource for path
            if (fileSource != null)
            {
                // If path is URI
                if (remotePath != path)
                    fileView.AddFileSource(fileSource, remotePath);
                // If found FileSource is same as current then simply change path
                else if (fileView.FileSource.Equals(fileSource))
                    fileView.CurrentPath = path;
                // Else create new FileSource with given path
                else
                    fileView.AddFileSource(fileSource, path);
            }
            // If current FileSource has address
            else if (local && !string.IsNullOrEmpty(fileView.CurrentAddress))
                fileView.CurrentPath = path;
            // Else use FileSystemFileSource
            else
            {
                SetFileSystemPath(fileView, path);
                return Directory.Exists(path);
            }

            return true;
        }

        /// <summary>
        /// Chooses an archive file
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="fileSource">File source</param>
        /// <param name="file">File</param>
        /// <param name="force">Whether to force opening the archive</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ChooseArchive(FileView fileView, IFileSource fileSource, FileEntry file, bool force = false)
        {
            IFileSource archiveFileSource;

            try
            {
                // Check if there is a ArchiveFileSource for possible archive
                archiveFileSource = ArchiveFileSourceUtil.GetArchiveFileSource(fileSource, file, string.Empty, force, false);
            }
            catch (Exception ex)
            {
                if (ex is WcxModuleException && ((WcxModuleException)ex).ErrorCode == WcxModuleErrorCode.Handled)
                    return true;

                if (!force)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + file.FullPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }

                throw;
            }

            if (archiveFileSource != null)
            {
                if (!string.Equals(fileView.CurrentPath, file.Path, StringComparison.OrdinalIgnoreCase))
                {
                    if (fileSource.Properties.HasFlag(FileSourceProperties.DirectAccess) ||
                        fileSource.Properties.HasFlag(FileSourceProperties.LinkToLocalFiles))
                    {
                        SetFileSystemPath(fileView, file.Path);
                    }
                }

                fileView.AddFileSource(archiveFileSource, archiveFileSource.GetRootDir());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Chooses a symbolic link
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="file">File</param>
        public static void ChooseSymbolicLink(FileView fileView, FileEntry file)
        {
            if (!(fileView.FileSource is FileSystemFileSource))
            {
                fileView.ChangePathToChild(file);
                return;
            }

            string path = Path.Combine(fileView.CurrentPath, file.Name) + Path.DirectorySeparatorChar;

            try
            {
                if (Directory.Exists(path))
                {
                    fileView.CurrentPath = fileView.CurrentPath + Path.DirectorySeparatorChar + file.Name + Path.DirectorySeparatorChar;
                }
                else
                {
                    string linkTarget = ReadSymLink(file.FullPath);
                    if (!string.IsNullOrEmpty(linkTarget))
                    {
                        fileView.CurrentPath = Path.GetFullPath(Path.Combine(fileView.CurrentPath, linkTarget)) + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        MessageBox.Show(string.Format("Failed to change directory to {0}", file.FullPath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception)
            {
                // Handle exceptions
            }
        }

        /// <summary>
        /// Sets the file system path
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="path">Path</param>
        public static void SetFileSystemPath(FileView fileView, string path)
        {
            if (fileView.FileSource is FileSystemFileSource)
                fileView.CurrentPath = path;
            else
                fileView.AddFileSource(FileSystemFileSource.GetFileSource(), path);
        }

        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="fileSource">File source</param>
        /// <param name="file">File</param>
        /// <param name="newFileName">New file name</param>
        /// <param name="interactive">Whether to show interactive dialogs</param>
        /// <returns>Result of the operation</returns>
        public static SetFilePropertyResult RenameFile(IFileSource fileSource, FileEntry file, string newFileName, bool interactive)
        {
            SetFilePropertyResult result = SetFilePropertyResult.Error;

            if (fileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.SetFileProperty))
            {
                FileNameProperty newNameProperty = new FileNameProperty(newFileName);
                FileEntries files = new FileEntries();
                files.Add(file.Clone());

                try
                {
                    FileSourceSetFilePropertyOperation operation = fileSource.CreateSetFilePropertyOperation(
                        files, new FileProperty[] { newNameProperty }) as FileSourceSetFilePropertyOperation;

                    if (operation != null)
                    {
                        // Only if the operation can change file name
                        if (operation.SupportedProperties.HasFlag(FilePropertyType.Name))
                        {
                            operation.SkipErrors = !interactive;

                            if (interactive)
                            {
                                FileSourceOperationMessageBoxesUI userInterface = new FileSourceOperationMessageBoxesUI();
                                operation.AddUserInterface(userInterface);
                            }

                            operation.Execute();
                            switch (operation.Result)
                            {
                                case FileSourceOperationResult.Finished:
                                    result = SetFilePropertyResult.Success;
                                    break;
                                case FileSourceOperationResult.Aborted:
                                    result = SetFilePropertyResult.Skipped;
                                    break;
                            }
                        }
                    }
                }
                finally
                {
                    // Cleanup if needed
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the copy operation type
        /// </summary>
        /// <param name="sourceFileSource">Source file source</param>
        /// <param name="targetFileSource">Target file source</param>
        /// <param name="operationType">Output operation type</param>
        /// <returns>True if a suitable operation type was found, false otherwise</returns>
        public static bool GetCopyOperationType(IFileSource sourceFileSource, IFileSource targetFileSource, out FileSourceOperationTypes operationType)
        {
            // If same file source and address
            if (sourceFileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.Copy) &&
                targetFileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.Copy) &&
                sourceFileSource.Equals(targetFileSource) &&
                string.Equals(sourceFileSource.GetCurrentAddress(), targetFileSource.GetCurrentAddress(), StringComparison.OrdinalIgnoreCase))
            {
                operationType = FileSourceOperationTypes.Copy;
                return true;
            }
            else if (targetFileSource is FileSystemFileSource &&
                     sourceFileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.CopyOut))
            {
                operationType = FileSourceOperationTypes.CopyOut;
                return true;
            }
            else if (sourceFileSource is FileSystemFileSource &&
                     targetFileSource.GetOperationsTypes().HasFlag(FileSourceOperationTypes.CopyIn))
            {
                operationType = FileSourceOperationTypes.CopyIn;
                return true;
            }
            else
            {
                operationType = 0;
                return false;
            }
        }

        /// <summary>
        /// Normalizes path delimiters
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path</returns>
        private static string NormalizePath(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Reads a symbolic link target
        /// </summary>
        /// <param name="path">Path to the symbolic link</param>
        /// <returns>Target path</returns>
        private static string ReadSymLink(string path)
        {
            // Implementation depends on platform-specific symlink handling
            try
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                {
                    // This is a simplified implementation
                    // A real implementation would use platform-specific APIs to read the symlink target
                    return path;
                }
            }
            catch
            {
                // Ignore errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if a file extension is a hash file
        /// </summary>
        /// <param name="extension">File extension</param>
        /// <returns>True if the extension is a hash file, false otherwise</returns>
        private static bool FileExtIsHash(string extension)
        {
            // Implementation depends on which extensions are considered hash files
            string[] hashExtensions = { ".md5", ".sha1", ".sha256", ".sha512", ".crc32" };
            return hashExtensions.Contains(extension.ToLower());
        }

        /// <summary>
        /// Processes an external command
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="startPath">Start path</param>
        /// <param name="file">File</param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool ProcessExtCommandFork(string command, string parameters = "", string startPath = "", FileEntry file = null)
        {
            // Implementation depends on how external commands are executed
            try
            {
                // This is a simplified implementation
                System.Diagnostics.Process.Start(command, parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Shows the file execute yourself dialog
        /// </summary>
        /// <param name="fileView">File view</param>
        /// <param name="file">File</param>
        /// <param name="withAll">Whether to include all files</param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool ShowFileExecuteYourSelf(FileView fileView, FileEntry file, bool withAll)
        {
            // Implementation depends on how files are executed from archives
            // This is a simplified implementation
            return false;
        }
    }

}