using System;
using System.IO;

namespace zfile
{
    public class DateOutOfRangeException : Exception
    {
        public DateTime DateTime { get; }
        public DateOutOfRangeException(DateTime dateTime) : base($"日期超出范围: {dateTime}")
        {
            DateTime = dateTime;
        }
    }

    public class FileSystemSetFilePropertyOperation : FileSourceSetFilePropertyOperation
    {
        private FileEntries? fullFilesTree;
        private FileSourceSetFilePropertyOperationStatistics? statistics;
        private Description? description;
        // Unused but kept for compatibility
        private readonly FileSourceOperationSymLinkOption symLinkOption;
        private FileSourceOperationUIResponse fileExistsOption;
        private FileSourceOperationUIResponse dirExistsOption;
        private FileEntry? currentFile;
        private string? currentTargetFilePath;

        public FileSystemSetFilePropertyOperation(IFileSource targetFileSource, FileEntries targetFiles, FileProperties newProperties)
            : base(targetFileSource, targetFiles, newProperties)
        {
            symLinkOption = FileSourceOperationSymLinkOption.None;
            fullFilesTree = null;

            // 在调用基类构造函数后赋值
            SupportedProperties = FilePropertiesTypes.Name |
#if UNIX
                // 在设置MODE之前设置所有者/组，因为它会清除SUID位
                FilePropertyType.Owner,
#endif
                FilePropertiesTypes.Attributes |
                FilePropertiesTypes.ModificationTime |
                FilePropertiesTypes.CreationTime |
                FilePropertiesTypes.LastAccessTime;

            if (GlobalSettings.ProcessComments)
            {
                description = new Description(false);
            }
        }

        ~FileSystemSetFilePropertyOperation()
        {
            if (Recursive)
            {
                fullFilesTree?.Clear();
            }

            if (description != null)
            {
                description.SaveDescription();
                description.Dispose();
            }
        }

        protected override void Initialize()
        {
            // 获取初始化的统计信息
            statistics = RetrieveStatistics();

            if (!Recursive)
            {
                fullFilesTree = TargetFiles;
                statistics.TotalFiles = fullFilesTree.Count;
            }
            else
            {
                long totalBytes;
                FileSystemUtil.FillAndCount(TargetFiles, true, false,
                            out fullFilesTree,
                            out statistics.TotalFiles,
                            out totalBytes);     // 获取完整的文件列表（递归）
            }
        }

        protected override void MainExecute()
        {
            if (fullFilesTree == null || statistics == null)
                return;

            for (int currentFileIndex = 0; currentFileIndex < fullFilesTree.Count; currentFileIndex++)
            {
                var file = fullFilesTree[currentFileIndex];

                statistics.CurrentFile = file.FullPath;
                UpdateStatistics(statistics);

                FileEntry? templateFile = null;
                if (TemplateFiles != null && currentFileIndex < TemplateFiles.Count)
                {
                    templateFile = TemplateFiles[currentFileIndex];
                }

                SetProperties(currentFileIndex, file, templateFile);

                statistics.DoneFiles++;
                UpdateStatistics(statistics);

                CheckOperationState();
            }
        }

        protected override SetFilePropertyResult SetNewProperty(FileEntry file, FileProperty templateProperty)
        {
            var result = SetFilePropertyResult.Success;

            try
            {
                switch (templateProperty.ID)
                {
                    case FilePropertiesTypes.Name:
                        if ((templateProperty as FileNameProperty)?.Value != file.Name)
                        {
                            result = RenameFile(
                                file,
                                (templateProperty as FileNameProperty)?.Value ?? string.Empty);

                            if (result == SetFilePropertyResult.Success && GlobalSettings.ProcessComments)
                            {
                                description?.Rename(file.FullPath, (templateProperty as FileNameProperty)?.Value ?? string.Empty);
                            }
                        }
                        else
                        {
                            result = SetFilePropertyResult.Skipped;
                        }
                        break;

                    case FilePropertiesTypes.Attributes:
                        if ((templateProperty as FileAttributesProperty)?.Value !=
                            (file.Properties[FilePropertiesTypes.Attributes] as FileAttributesProperty)?.Value)
                        {
                            if (!FileSystemUtil.SetAttributesUAC(
                                file.FullPath,
                                (templateProperty as FileAttributesProperty)?.Value))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else
                        {
                            result = SetFilePropertyResult.Skipped;
                        }
                        break;

                    case FilePropertiesTypes.ModificationTime:
                        if ((templateProperty as FileModificationDateTimeProperty)?.Value !=
                            (file.Properties[FilePropertiesTypes.ModificationTime] as FileModificationDateTimeProperty)?.Value)
                        {
                            if (!FileSystemUtil.SetTimeExUAC(
                                file.FullPath,
                                DateTimeToFileTimeEx((templateProperty as FileModificationDateTimeProperty)?.Value),
                                null,
                                null))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else
                        {
                            result = SetFilePropertyResult.Skipped;
                        }
                        break;

                    case FilePropertiesTypes.CreationTime:
                        if ((templateProperty as FileCreationDateTimeProperty)?.Value !=
                            (file.Properties[FilePropertiesTypes.CreationTime] as FileCreationDateTimeProperty)?.Value)
                        {
                            if (!FileSystemUtil.SetTimeExUAC(
                                file.FullPath,
                                null,
                                DateTimeToFileTimeEx((templateProperty as FileCreationDateTimeProperty)?.Value),
                                null))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else
                        {
                            result = SetFilePropertyResult.Skipped;
                        }
                        break;

                    case FilePropertiesTypes.LastAccessTime:
                        if ((templateProperty as FileLastAccessDateTimeProperty)?.Value !=
                            (file.Properties[FilePropertiesTypes.LastAccessTime] as FileLastAccessDateTimeProperty)?.Value)
                        {
                            if (!FileSystemUtil.SetTimeExUAC(
                                file.FullPath,
                                null,
                                null,
                                DateTimeToFileTimeEx((templateProperty as FileLastAccessDateTimeProperty)?.Value)))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else
                        {
                            result = SetFilePropertyResult.Skipped;
                        }
                        break;

#if UNIX
                    case FilePropertyType.Owner:
                        if (FileSystemUtil.SetOwner(file.FullPath,
                            (templateProperty as FileOwnerProperty).Owner,
                            (templateProperty as FileOwnerProperty).Group) != 0)
                        {
                            result = SetFilePropertyResult.Error;
                        }
                        break;
#endif

                    default:
                        throw new Exception("尝试设置不支持的属性");
                }
            }
            catch (DateOutOfRangeException ex)
            {
                if (!GlobalSettings.SkipFileOpError)
                {
                    switch (AskQuestion(Resources.MsgLogError + string.Format(Resources.MsgErrDateNotSupported, ex.DateTime.ToString()), "",
                        new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                        FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort))
                    {
                        case FileSourceOperationUIResponse.Skip:
                            result = SetFilePropertyResult.Skipped;
                            break;
                        case FileSourceOperationUIResponse.Abort:
                            RaiseAbortOperation();
                            break;
                    }
                }
            }
            catch (FormatException ex)
            {
                if (!GlobalSettings.SkipFileOpError)
                {
                    switch (AskQuestion(Resources.MsgLogError + ex.Message, "",
                        new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                        FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort))
                    {
                        case FileSourceOperationUIResponse.Skip:
                            result = SetFilePropertyResult.Skipped;
                            break;
                        case FileSourceOperationUIResponse.Abort:
                            RaiseAbortOperation();
                            break;
                    }
                }
            }

            return result;
        }

        private void QuestionActionHandler(FileSourceOperationUIResponse action)
        {
            if (action == FileSourceOperationUIResponse.CompareAction && currentFile != null && !string.IsNullOrEmpty(currentTargetFilePath))
            {
                ShowCompareFilesUI(currentFile, currentTargetFilePath);
            }
        }

        private SetFilePropertyResult RenameFile(FileEntry file, string newName)
        {
            var oldName = file.FullPath;
            FileAttributeData newAttr;

            FileSourceOperationUIResponse OverwriteOlder()
            {
                if (file.ModificationTime > FileTimeToDateTime(newAttr.LastWriteTime))
                    return FileSourceOperationUIResponse.Overwrite;
                return FileSourceOperationUIResponse.Skip;
            }

            FileSourceOperationUIResponse OverwriteSmaller()
            {
                if (file.Size > newAttr.Size)
                    return FileSourceOperationUIResponse.Overwrite;
                return FileSourceOperationUIResponse.Skip;
            }

            FileSourceOperationUIResponse OverwriteLarger()
            {
                if (file.Size < newAttr.Size)
                    return FileSourceOperationUIResponse.Overwrite;
                return FileSourceOperationUIResponse.Skip;
            }

            FileSourceOperationUIResponse AskIfOverwrite()
            {
                if (FileSystemUtil.IsDirectory(newAttr.Attr))
                {
                    if (dirExistsOption != FileSourceOperationUIResponse.Invalid)
                        return dirExistsOption;

                    var result = AskQuestion(string.Format(Resources.MsgErrDirExists, newName), "",
                        new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.SkipAll, FileSourceOperationUIResponse.Abort },
                        FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort);

                    if (result == FileSourceOperationUIResponse.SkipAll)
                    {
                        dirExistsOption = FileSourceOperationUIResponse.Skip;
                        return dirExistsOption;
                    }
                    return result;
                }
                else
                {
                    switch (fileExistsOption)
                    {
                        case FileSourceOperationUIResponse.None:
                        case FileSourceOperationUIResponse.Invalid:
                            currentFile = file;
                            currentTargetFilePath = newName;
                            var question = FileExistsMessage(newName, file.FullPath, file.Size, file.ModificationTime);
                            var result = AskQuestion(question, "",
                                new[] {
                                    FileSourceOperationUIResponse.Overwrite,
                                    FileSourceOperationUIResponse.Skip,
                                    FileSourceOperationUIResponse.OverwriteSmaller,
                                    FileSourceOperationUIResponse.OverwriteAll,
                                    FileSourceOperationUIResponse.SkipAll,
                                    FileSourceOperationUIResponse.OverwriteLarger,
                                    FileSourceOperationUIResponse.OverwriteOlder,
                                    FileSourceOperationUIResponse.Abort,
                                    FileSourceOperationUIResponse.CompareAction
                                },
                                FileSourceOperationUIResponse.Overwrite,
                                FileSourceOperationUIResponse.Abort,
                                new FileSourceOperationUIActionHandlerAdapter(QuestionActionHandler));

                            switch (result)
                            {
                                case FileSourceOperationUIResponse.OverwriteAll:
                                    result = FileSourceOperationUIResponse.Overwrite;
                                    fileExistsOption = result;
                                    break;
                                case FileSourceOperationUIResponse.SkipAll:
                                    result = FileSourceOperationUIResponse.Skip;
                                    fileExistsOption = result;
                                    break;
                                case FileSourceOperationUIResponse.OverwriteOlder:
                                    fileExistsOption = OverwriteOlder();
                                    result = fileExistsOption;
                                    break;
                                case FileSourceOperationUIResponse.OverwriteSmaller:
                                    fileExistsOption = FileSourceOperationUIResponse.OverwriteSmaller;
                                    result = OverwriteSmaller();
                                    break;
                                case FileSourceOperationUIResponse.OverwriteLarger:
                                    fileExistsOption = FileSourceOperationUIResponse.OverwriteLarger;
                                    result = OverwriteLarger();
                                    break;
                            }
                            return result;
                        case FileSourceOperationUIResponse.OverwriteOlder:
                            return OverwriteOlder();
                        case FileSourceOperationUIResponse.OverwriteSmaller:
                            return OverwriteSmaller();
                        case FileSourceOperationUIResponse.OverwriteLarger:
                            return OverwriteLarger();
                        default:
                            return fileExistsOption;
                    }
                }
            }

            if (FileSource.GetPathType(newName) != PathType.Absolute)
            {
                var dirName = Path.GetDirectoryName(oldName);
                if (!string.IsNullOrEmpty(dirName))
                {
                    newName = Path.Combine(dirName, Path.GetFileName(newName));
                }
            }

            if (oldName == newName)
                return SetFilePropertyResult.Skipped;

#if UNIX
            FileAttributeData oldAttr;
            // 检查目标文件是否存在
            if (FileSystemUtil.GetAttributesUAC(newName, out newAttr))
            {
                // 不能将文件覆盖为目录，反之亦然
                if (FileSystemUtil.IsDirectory(newAttr.Attr) != file.IsDirectory)
                    return SetFilePropertyResult.Error;

                // 特殊情况：文件名仅大小写不同
                if (oldName.ToLower() != newName.ToLower())
                {
                    oldAttr.Inode = ~newAttr.Inode;
                }
                else
                {
                    if (!FileSystemUtil.GetAttributesUAC(oldName, out oldAttr))
                        return SetFilePropertyResult.Error;
                }

                // 检查源文件和目标文件是否是同一个文件（相同的inode和相同的设备）
                if (oldAttr.Inode == newAttr.Inode &&
                    oldAttr.Device == newAttr.Device &&
                    // 检查链接数，如果为1，则源文件和目标文件名很可能在大小写不敏感的文件系统上仅大小写不同
                    ((newAttr.LinkCount == 1) || FileSystemUtil.IsDirectory(newAttr.Attr)))
                {
                    // 文件名在大小写不敏感的文件系统上仅大小写不同
                }
                else
                {
                    switch (AskIfOverwrite())
                    {
                        case FileSourceOperationUIResponse.Overwrite:
                            break; // 继续
                        case FileSourceOperationUIResponse.Skip:
                            return SetFilePropertyResult.Skipped;
                        case FileSourceOperationUIResponse.Abort:
                            RaiseAbortOperation();
                            break;
                    }
                }
            }
#else
            // Windows不允许两个文件名仅大小写不同（即使在NTFS上）
            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                if (FileSystemUtil.GetAttributesUAC(newName, out newAttr))  // 如果目标文件存在
                {
                    // 不能将文件覆盖为目录，反之亦然
                    if (FileSystemUtil.IsDirectory(newAttr.Attr) != file.IsDirectory)
                        return SetFilePropertyResult.Error;

                    switch (AskIfOverwrite())
                    {
                        case FileSourceOperationUIResponse.Overwrite:
                            break; // 继续
                        case FileSourceOperationUIResponse.Skip:
                            return SetFilePropertyResult.Skipped;
                        case FileSourceOperationUIResponse.Abort:
                            RaiseAbortOperation();
                            break;
                    }
                }
            }
#endif

            if (FileSystemUtil.RenameFileUAC(oldName, newName))
                return SetFilePropertyResult.Success;
            else
                return SetFilePropertyResult.Error;
        }

        private string FileExistsMessage(string newName, string _, long size, DateTime modificationTime)
        {
            if (!FileSystemUtil.GetAttributesUAC(newName, out var newAttr))
                return string.Format("File {0} exists, overwrite?", newName);

            string msg = string.Format("File {0} exists, overwrite?", newName);
            msg += "\n\n" + "Overwrite:";

            // 添加目标文件信息
            msg += "\n" + string.Format("{0} bytes, {1}", newAttr.Size,
                FileTimeToDateTime(newAttr.LastWriteTime).ToString());

            // 添加源文件信息
            msg += "\n\n" + "With file:";
            msg += "\n" + string.Format("{0} bytes, {1}", size, modificationTime.ToString());

            return msg;
        }

        protected void ShowCompareFilesUI(FileEntry sourceFile, string targetFilePath)
        {
            if (string.IsNullOrEmpty(targetFilePath))
                return;

            var dirName = Path.GetDirectoryName(targetFilePath);
            if (string.IsNullOrEmpty(dirName))
                return;

            var targetFile = FileSource.CreateFile(dirName);
            try
            {
                targetFile.Name = Path.GetFileName(targetFilePath);
                PrepareToolData(FileSource, sourceFile, FileSource, targetFile, ShowDifferByGlobList, true);
            }
            finally
            {
                targetFile.Dispose();
            }
        }

        /// <summary>
        /// Delegate for handling file comparison
        /// </summary>
        /// <param name="fileList">List of files to compare</param>
        /// <param name="waitData">Wait data</param>
        /// <param name="modal">Whether to show modal dialog</param>
        private delegate void ShowDifferByGlobListDelegate(System.Collections.Specialized.StringCollection fileList, object? waitData, bool modal = false);

        /// <summary>
        /// Shows differ for comparing files
        /// </summary>
        /// <param name="fileList">List of files to compare</param>
        /// <param name="waitData">Wait data</param>
        /// <param name="modal">Whether to show modal dialog</param>
        private void ShowDifferByGlobList(System.Collections.Specialized.StringCollection fileList, object? waitData, bool modal = false)
        {
            if (fileList != null && fileList.Count >= 2)
            {
                // 调用外部比较工具或内部比较器
                // 这里简化实现，实际应该调用外部比较工具
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fileList[0]}\"");
            }
        }

        private static void PrepareToolData(IFileSource _, FileEntry sourceFile, IFileSource __, FileEntry targetFile, ShowDifferByGlobListDelegate showDifferCallback, bool modal)
        {
            if (sourceFile == null || targetFile == null)
                return;

            var fileList = new System.Collections.Specialized.StringCollection
            {
                sourceFile.FullPath,
                targetFile.FullPath
            };

            showDifferCallback(fileList, null, modal);
        }
    }
}