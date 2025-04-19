using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
namespace zfile
{
    unsafe public struct FileSourceSplitOperationStatistics
    {
        public string CurrentFileFrom;
        public string CurrentFileTo;
        public long TotalBytes;
        public long DoneBytes;
        public long CurrentFileTotalBytes;
        public long CurrentFileDoneBytes;
        public int TotalFiles;
        public int DoneFiles;
        public DateTime RemainingTime;
        public long BytesPerSecond;
    }


    unsafe public class FileSystemSplitOperation : FileSourceSplitOperation
    {
        private FileSourceSplitOperationStatistics statistics;
        private string targetPath;
        private IntPtr buffer;
        private uint bufferSize;
        private bool checkFreeSpace;
        private uint currentCRC32;

        public FileSystemSplitOperation(IFileSource fileSource, FileEntry sourceFile, string targetPath)
            : base(fileSource, sourceFile, targetPath)
        {
            checkFreeSpace = true;
            this.targetPath = Path.Combine(targetPath, "");
            bufferSize = GlobalSettings.CopyBlockSize;
            buffer = Marshal.AllocHGlobal((int)bufferSize);
        }

        ~FileSystemSplitOperation()
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }
        }

        private string ConvertStringToTCStringUTF8CharReplacedByUnderscore(string str)
        {
            return Encoding.Default.GetString(Encoding.UTF8.GetBytes(str)).Replace("?", "_");
        }

        protected override void Initialize()
        {
            // 获取初始化的统计信息
            statistics = RetrieveStatistics();

            statistics.CurrentFileFrom = SourceFile.FullPath;
            statistics.TotalFiles = VolumeNumber;
            statistics.TotalBytes = SourceFile.Size;
        }

        protected override void MainExecute()
        {
            try
            {
                if (!AutomaticSplitMode)
                {
                    // 检查磁盘空间
                    if (checkFreeSpace)
                    {
                        long freeSpace, totalSpace;
                        FileSystemUtil.GetDiskFreeSpace(TargetPath, out freeSpace, out totalSpace);
                        if (statistics.TotalBytes > freeSpace)
                        {
                            AskQuestion("", Resources.MsgNoFreeSpaceCont,
                                new[] { FileSourceOperationUIResponse.Abort },
                                FileSourceOperationUIResponse.Abort,
                                FileSourceOperationUIResponse.Abort);
                            RaiseAbortOperation();
                        }
                    }
                }

                // 打开源文件
                using (var sourceFileStream = new FileStream(SourceFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // 计算扩展名长度
                    int extLength = 3; // 最小长度3个字符
                    if (!AutomaticSplitMode)
                    {
                        int fileIndexForExt = statistics.TotalFiles / 1000;
                        while (fileIndexForExt >= 1)
                        {
                            fileIndexForExt /= 10;
                            extLength++;
                        }
                    }

                    // 使用while循环而不是for循环，以防文件数量计算错误
                    int currentFileIndex = 1;
                    while ((currentFileIndex <= statistics.TotalFiles || AutomaticSplitMode) &&
                           statistics.TotalBytes > statistics.DoneBytes)
                    {
                        // 确定下一个输出文件的文件名
                        string targetFilename;
                        if (RequireACRC32VerificationFile)
                        {
                            targetFilename = Path.Combine(targetPath,
                                SourceFile.NameNoExt + "." + currentFileIndex.ToString($"D{extLength}"));
                        }
                        else
                        {
                            targetFilename = Path.Combine(targetPath,
                                SourceFile.Name + "." + currentFileIndex.ToString($"D{extLength}"));
                        }

                        if (AutomaticSplitMode)
                        {
                            FileSourceOperationUIResponse respAutomaticSwapDisk;
                            do
                            {
                                long freeSpace, totalSpace;
                                FileSystemUtil.GetDiskFreeSpace(TargetPath, out freeSpace, out totalSpace);
                                VolumeSize = freeSpace - (64 * 1024); // 在复制后保留64KB的可用空间
                                if (VolumeSize < (64 * 1024))
                                {
                                    respAutomaticSwapDisk = AskQuestion("",
                                        string.Format(Resources.MsgInsertNextDisk, targetFilename,
                                            statistics.TotalBytes - statistics.DoneBytes),
                                        new[] { FileSourceOperationUIResponse.Ok, FileSourceOperationUIResponse.Abort },
                                        FileSourceOperationUIResponse.Ok,
                                        FileSourceOperationUIResponse.Abort);
                                    if (respAutomaticSwapDisk == FileSourceOperationUIResponse.Abort)
                                        RaiseAbortOperation();
                                    if (respAutomaticSwapDisk == FileSourceOperationUIResponse.Ok)
                                        VolumeSize = 1 * 1024 * 1024; // 调试用
                                }
                            } while (VolumeSize < (64 * 1024));
                            statistics.TotalFiles++;
                        }

                        // 最后一个文件可能小于卷大小
                        if (statistics.TotalBytes - statistics.DoneBytes < VolumeSize)
                            VolumeSize = statistics.TotalBytes - statistics.DoneBytes;

                        statistics.CurrentFileTo = targetFilename;
                        statistics.CurrentFileTotalBytes = VolumeSize;
                        statistics.CurrentFileDoneBytes = 0;
                        UpdateStatistics(statistics);

                        // 分割当前文件
                        if (!Split(sourceFileStream, targetFilename))
                            break;

                        statistics.DoneFiles++;
                        UpdateStatistics(statistics);

                        CheckOperationState();
                        currentFileIndex++;
                    }

                    if (statistics.DoneBytes != statistics.TotalBytes)
                    {
                        // 发生错误，因为并非所有文件都已创建
                        // 删除未完成的目标文件
                        for (int i = 1; i <= statistics.TotalFiles; i++)
                        {
                            File.Delete(Path.Combine(targetPath,
                                SourceFile.NameNoExt + "." + i.ToString($"D{extLength}")));
                        }
                    }
                    else if (RequireACRC32VerificationFile)
                    {
                        // 创建CRC32验证文件
                        string summaryFilename;
                        if (SourceFile.NameNoExt == SourceFile.NameNoExt.ToUpper())
                            summaryFilename = Path.Combine(targetPath, SourceFile.NameNoExt + ".CRC");
                        else
                            summaryFilename = Path.Combine(targetPath, SourceFile.NameNoExt + ".crc");

                        using (var summaryFile = File.CreateText(summaryFilename))
                        {
                            summaryFile.WriteLine($"filename={ConvertStringToTCStringUTF8CharReplacedByUnderscore(SourceFile.Name)}");
                            summaryFile.WriteLine($"filenameutf8={SourceFile.Name}");
                            summaryFile.WriteLine($"size={SourceFile.Size}");
                            summaryFile.WriteLine($"crc32={currentCRC32:X8}");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                ShowError(Resources.MsgLogError + Resources.MsgErrEOpen + ": " + SourceFile.FullPath);
            }
        }

        private bool Split(FileStream sourceFileStream, string targetFile)
        {
            bool result = false;
            long totalBytesToRead = VolumeSize;
            int bytesToRead = (int)bufferSize;
            byte[] tempBuffer = new byte[bytesToRead]; // 临时缓冲区用于读写操作

            try
            {
                using (var targetFileStream = new FileStream(targetFile, FileMode.Create))
                {
                    while (totalBytesToRead > 0)
                    {
                        if (totalBytesToRead < bytesToRead)
                            bytesToRead = (int)totalBytesToRead;

                        bool retryRead;
                        do
                        {
                            retryRead = false;
                            // 使用临时缓冲区读取数据
                            int bytesRead = sourceFileStream.Read(tempBuffer, 0, bytesToRead);

                            if (bytesRead == 0)
                                throw new IOException(Marshal.GetLastWin32Error().ToString());

                            // 将数据复制到非托管内存
                            Marshal.Copy(tempBuffer, 0, buffer, bytesRead);

                            if (RequireACRC32VerificationFile)
                                currentCRC32 = CRC32.Calculate(buffer, bytesRead, currentCRC32);

                            totalBytesToRead -= bytesRead;
                            int bytesWritten = 0;

                            bool retryWrite;
                            do
                            {
                                retryWrite = false;
                                // 使用临时缓冲区写入数据
                                targetFileStream.Write(tempBuffer, bytesWritten, bytesRead - bytesWritten);
                                int bytesWrittenTry = bytesRead - bytesWritten; // 假设写入成功
                                bytesWritten += bytesWrittenTry;
                                if (bytesWrittenTry == 0)
                                {
                                    throw new IOException(Marshal.GetLastWin32Error().ToString());
                                }
                                else if (bytesWritten < bytesRead)
                                {
                                    retryWrite = true; // 重试写入剩余部分
                                }
                            } while (retryWrite);
                        } while (retryRead);

                        statistics.CurrentFileDoneBytes += bytesToRead;
                        statistics.DoneBytes += bytesToRead;
                        UpdateStatistics(statistics);

                        CheckOperationState();
                    }
                }

                result = true;
            }
            catch (IOException)
            {
                ShowError(Resources.MsgLogError + Resources.MsgErrEWrite + ": " + targetFile);
            }

            return result;
        }

        private void ShowError(string message)
        {
            if (GlobalSettings.SkipFileOpError)
            {
                LogMessage(message, LogOption.Error, true);
            }
            else
            {
                AskQuestion(message, "",
                    new[] { FileSourceOperationUIResponse.Abort },
                    FileSourceOperationUIResponse.Abort,
                    FileSourceOperationUIResponse.Abort);
                RaiseAbortOperation();
            }
        }

        private void LogMessage(string message, LogOption logOptions, bool logError)
        {
            if (logError && !GlobalSettings.LogErrors)
                return;

            if (logOptions <= GlobalSettings.LogOptions)
            {
                Logger.Write(message, logError ? LogOption.Error : LogOption.Info);
            }
        }
    }
}