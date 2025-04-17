namespace zfile
{
	public struct FileSourceCombineOperationStatistics
	{
		public string CurrentFileFrom;
		public string CurrentFileTo;
		public long TotalFiles;
		public long DoneFiles;
		public long TotalBytes;
		public long DoneBytes;
		public long CurrentFileTotalBytes;
		public long CurrentFileDoneBytes;
		public long BytesPerSecond;
		public DateTime RemainingTime;
	}

	public class FileSystemCombineOperation : FileSourceCombineOperation
    {
        private FileEntries? fullFilesTreeToCombine;  // 源文件，包括所有文件
        private FileSourceCombineOperationStatistics statistics; // 统计信息的本地副本
        private string? targetPath;
        private byte[] buffer;
        private bool checkFreeSpace;
        private int extensionLengthRequired;

        public FileSystemCombineOperation(IFileSource fileSource, 
            FileEntries sourceFiles, 
            string targetFile)
            : base(fileSource, sourceFiles, targetFile)
        {
            fullFilesTreeToCombine = null;
            checkFreeSpace = true;
            targetPath = Path.GetDirectoryName(targetFile);
            buffer = new byte[GlobalSettings.CopyBlockSize];
        }

        protected override void Initialize()
        {
            // 如果处于"RequireDynamicMode"，我们只有一个文件在"SourceFiles"列表中
            if (RequireDynamicMode)
            {
                // 确保".001"文件是列表中的第一个并且可用
                extensionLengthRequired = SourceFiles[0].Extension.Length;
                int maybeFileIndex = 1;
                do
                {
                    string maybeAdditionalSourceFilename = Path.Combine(
                        SourceFiles[0].Path,
                        $"{Path.GetFileNameWithoutExtension(SourceFiles[0].Name)}.{maybeFileIndex:extensionLengthRequired}");

                    if (File.Exists(maybeAdditionalSourceFilename) || maybeFileIndex == 1)
                    {
                        // 确保第一个文件可用，如果不可用则请求它
                        if (!File.Exists(maybeAdditionalSourceFilename) && maybeFileIndex == 1)
                        {
                            BegForPresenceOfThisFile(maybeAdditionalSourceFilename);
                        }

                        var maybeFile = new FileEntry(maybeAdditionalSourceFilename);
                        SourceFiles.Add(maybeFile);
                    }
                    maybeFileIndex++;
                } while (!File.Exists(Path.Combine(
                    SourceFiles[0].Path,
                    $"{Path.GetFileNameWithoutExtension(SourceFiles[0].Name)}.{maybeFileIndex:DextensionLengthRequired}")) && maybeFileIndex != 1);

                SourceFiles.RemoveAt(0); // 现在可以删除第一个文件，它可能是系列中的任何一个
            }

            // 获取初始化的统计信息；然后我们只更改需要的内容
            statistics = RetrieveStatistics();
            statistics.CurrentFileTo = TargetFile;

            FileSystemUtil.FillAndCount(SourceFiles, false, false,
                fullFilesTreeToCombine,
                ref statistics.TotalFiles,
                ref statistics.TotalBytes); // 计算文件

            // 如果处于"RequireDynamicMode"，检查是否有类似TC的摘要文件
            // 我们在标准统计之后执行此操作，以防需要根据摘要文件中的信息更正它们
            if (RequireDynamicMode && !WeGotTheCRC32VerificationFile)
            {
                TryToGetInfoFromTheCRC32VerificationFile();
            }
        }

        protected override void MainExecute()
        {
            try
            {
                // 检查磁盘空间
                if (checkFreeSpace)
                {
                    DriveInfo drive = new DriveInfo(Path.GetPathRoot(targetPath));
                    if (statistics.TotalBytes > drive.AvailableFreeSpace)
                    {
                        if (AskQuestion("", "没有足够的磁盘空间继续操作", 
                            new[] { "中止" }, "中止", "中止") == "中止")
                        {
                            RaiseAbortOperation();
                        }
                    }
                }

                FileAttributes attrs = File.GetAttributes(TargetFile);
                if (attrs != (FileAttributes)(-1))
                {
                    if ((attrs & FileAttributes.Directory) != 0)
                    {
                        if (AskQuestion($"目录已存在: {TargetFile}", "",
                            new[] { "中止" }, "中止", "中止") == "中止")
                        {
                            RaiseAbortOperation();
                        }
                    }

                    if (AskQuestion($"文件已存在: {TargetFile}", "",
                        new[] { "覆盖", "中止" }, "覆盖", "中止") != "覆盖")
                    {
                        RaiseAbortOperation();
                    }
                }

                // 创建目标文件
                using (var targetFileStream = new FileStream(TargetFile, FileMode.Create))
                {
                    int currentFileIndex = 0;
                    while (currentFileIndex < fullFilesTreeToCombine.Count ||
                           (RequireDynamicMode && statistics.DoneBytes < statistics.TotalBytes))
                    {
                        // 在"RequireDynamicMode"下，我们可能在这里时下一个文件不可用
                        // 让我们确保不是这种情况，如果是，则将其添加到当前列表
                        if (currentFileIndex >= fullFilesTreeToCombine.Count)
                        {
                            string dynamicNextFilename = Path.Combine(
                                SourceFiles[0].Path,
                                $"{Path.GetFileNameWithoutExtension(SourceFiles[0].Name)}.{(currentFileIndex + 1):extensionLengthRequired}");
                            BegForPresenceOfThisFile(dynamicNextFilename);
                            var dynamicNextFile = new FileEntry(dynamicNextFilename);
                            SourceFiles.Add(dynamicNextFile);
                            fullFilesTreeToCombine.Add(dynamicNextFile);
                        }

                        var file = fullFilesTreeToCombine[currentFileIndex];

                        statistics.CurrentFileFrom = file.Name;
                        statistics.CurrentFileTotalBytes = file.Size;
                        statistics.CurrentFileDoneBytes = 0;
                        UpdateStatistics(statistics);

                        // 与当前文件合并
                        if (!Combine(file, targetFileStream))
                            break;

                        statistics.DoneFiles++;
                        UpdateStatistics(statistics);

                        CheckOperationState();
                        currentFileIndex++;
                    }

                    if (statistics.DoneBytes != statistics.TotalBytes)
                    {
                        // 发生了一些错误，因为并非所有文件都已合并
                        // 删除未完成的目标文件
                        File.Delete(TargetFile);

                        // 在"RequireDynamicMode"下，给用户一些反馈，让他知道他不会得到他的文件
                        if (RequireDynamicMode)
                        {
                            ShowError($"错误: 文件长度不正确 [{TargetFile}]");
                        }
                    }
                    else
                    {
                        // 如果所有数据都已复制，在"RequireDynamicMode"的情况下，我们可以验证CRC32
                        // 如果可用，我们可以验证结果文件的完整性
                        if (RequireDynamicMode && ExpectedCRC32 != 0)
                        {
                            if (CurrentCRC32 != ExpectedCRC32)
                            {
                                var userAnswer = AskQuestion("", 
                                    $"错误: CRC32校验失败 [{TargetFile}]",
                                    new[] { "否", "是" }, "否", "否");
                                if (userAnswer == "否")
                                    File.Delete(TargetFile);
                                RaiseAbortOperation();
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                ShowError($"无法创建文件: {TargetFile}");
                RaiseAbortOperation();
            }
            catch (IOException ex)
            {
                ShowError($"I/O错误: {ex.Message}");
                RaiseAbortOperation();
            }
        }

        private bool Combine(FileEntry sourceFile, FileStream targetStream)
        {
            try
            {
                using (var sourceStream = sourceFile.OpenRead())
                {
                    int bytesRead;
                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        targetStream.Write(buffer, 0, bytesRead);
                        statistics.CurrentFileDoneBytes += bytesRead;
                        statistics.DoneBytes += bytesRead;
                        UpdateStatistics(statistics);
                        CheckOperationState();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowError($"合并文件时出错: {ex.Message}");
                return false;
            }
        }

        private void ShowError(string message)
        {
            // TODO: 实现错误显示
            Console.WriteLine($"错误: {message}");
        }

        private void LogMessage(string message, LogOption options, LogOption msgType)
        {
            // TODO: 实现日志记录
            Console.WriteLine($"[{msgType}] {message}");
        }

        private bool TryToGetInfoFromTheCRC32VerificationFile()
        {
            // TODO: 实现从CRC32验证文件获取信息
            return false;
        }

        private void BegForPresenceOfThisFile(string filename)
        {
            // TODO: 实现文件请求
            Console.WriteLine($"请求文件: {filename}");
        }

        private string AskQuestion(string title, string message, string[] buttons, string defaultButton, string cancelButton)
        {
            // TODO: 实现用户交互对话框
            return defaultButton;
        }
    }
} 