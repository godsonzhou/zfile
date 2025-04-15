using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemOperations
{
	public enum FileSourceOperationOptionSymLink
	{
		None = 0,
		Follow = 1,
		DontFollow = 2
	}

	public class FileSystemCalcStatisticsOperation : FileSourceCalcStatisticsOperation
    {
        private FileSourceCalcStatisticsOperationStatistics statistics; // 统计信息的本地副本
        private FileSourceOperationOptionSymLink symLinkOption;

        public FileSystemCalcStatisticsOperation(IFileSource targetFileSource, List<FileInfo> files)
            : base(targetFileSource, files)
        {
            symLinkOption = FileSourceOperationOptionSymLink.None;
        }

        public override void Initialize()
        {
            // 获取初始化的统计信息；然后我们只更改需要的内容
            statistics = RetrieveStatistics();
        }

        public override void MainExecute()
        {
            for (int currentFileIndex = 0; currentFileIndex < Files.Count; currentFileIndex++)
            {
                ProcessFile(Files[currentFileIndex]);
                CheckOperationState();
            }
        }

        private void ProcessFile(FileInfo file)
        {
            statistics.CurrentFile = file.FullName;
            UpdateStatistics(statistics);

            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                statistics.Links++;

                switch (symLinkOption)
                {
                    case FileSourceOperationOptionSymLink.Follow:
                        ProcessLink(file);
                        break;
                    case FileSourceOperationOptionSymLink.DontFollow:
                        // 不执行任何操作
                        break;
                    case FileSourceOperationOptionSymLink.None:
                        var result = AskQuestion("", 
                            $"是否跟随符号链接 {file.Name}?",
                            new[] { "是", "全部", "否", "全部跳过" },
                            "是", "否");

                        switch (result)
                        {
                            case "是":
                                ProcessLink(file);
                                break;
                            case "全部":
                                symLinkOption = FileSourceOperationOptionSymLink.Follow;
                                ProcessLink(file);
                                break;
                            case "否":
                                // 不执行任何操作
                                break;
                            case "全部跳过":
                                symLinkOption = FileSourceOperationOptionSymLink.DontFollow;
                                break;
                        }
                        break;
                }
            }
            else if ((file.Attributes & FileAttributes.Directory) != 0)
            {
                statistics.Directories++;
                ProcessSubDirs(file.FullName + Path.DirectorySeparatorChar);
            }
            else
            {
                // 在Unix上，这可能不一定是常规文件（可能是socket、FIFO、块设备、字符设备等）
                // 也许在Unix上使用FPS_ISREG()检查？

                statistics.Files++;
                statistics.Size += file.Length;
                if (file.LastWriteTime < statistics.OldestFile)
                    statistics.OldestFile = file.LastWriteTime;
                if (file.LastWriteTime > statistics.NewestFile)
                    statistics.NewestFile = file.LastWriteTime;
            }

            UpdateStatistics(statistics);
        }

        private void ProcessLink(FileInfo file)
        {
            string pathToFile = GetLinkTarget(file.FullName);
            if (!string.IsNullOrEmpty(pathToFile))
            {
                try
                {
                    var linkFile = new FileInfo(pathToFile);
                    ProcessFile(linkFile);
                }
                catch (FileNotFoundException)
                {
                    LogMessage($"无效的符号链接: {file.FullName} -> {pathToFile}", 
                        LogOptions.Error, LogMessageType.Error);
                }
            }
            else
            {
                LogMessage($"无效的符号链接: {file.FullName} -> {pathToFile}", 
                    LogOptions.Error, LogMessageType.Error);
            }
        }

        private void ProcessSubDirs(string srcPath)
        {
            try
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(srcPath, "*"))
                {
                    if (Path.GetFileName(entry) == "." || Path.GetFileName(entry) == "..")
                        continue;

                    var file = new FileInfo(entry);
                    ProcessFile(file);
                    CheckOperationState();
                }
            }
            catch (UnauthorizedAccessException)
            {
                LogMessage($"无法访问目录: {srcPath}", LogOptions.Error, LogMessageType.Error);
            }
        }

        private string GetLinkTarget(string linkPath)
        {
            try
            {
                return File.ResolveLinkTarget(linkPath, true)?.FullName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void LogMessage(string message, LogOptions options, LogMessageType msgType)
        {
            if (!ShouldLogMessage(msgType))
                return;

            if (options <= GlobalSettings.LogOptions)
            {
                // TODO: 实现日志记录
                Console.WriteLine($"[{msgType}] {message}");
            }
        }

        private bool ShouldLogMessage(LogMessageType msgType)
        {
            switch (msgType)
            {
                case LogMessageType.Error:
                    return GlobalSettings.LogOptions.HasFlag(LogOptions.Error);
                case LogMessageType.Info:
                    return GlobalSettings.LogOptions.HasFlag(LogOptions.Info);
                case LogMessageType.Success:
                    return GlobalSettings.LogOptions.HasFlag(LogOptions.Success);
                default:
                    return false;
            }
        }

        private string AskQuestion(string title, string message, string[] buttons, string defaultButton, string cancelButton)
        {
            // TODO: 实现用户交互对话框
            return defaultButton;
        }
    }

  
} 