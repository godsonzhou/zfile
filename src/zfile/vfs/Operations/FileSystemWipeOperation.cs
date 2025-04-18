using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace zfile
{
    public class FileSystemWipeOperation : FileSourceWipeOperation
    {
        private readonly RandomNumberGenerator random;
        private readonly byte[][] buffer;
        private readonly Description description;
        private FileEntries fullFilesTreeToDelete;
        private FileSourceWipeOperationStatistics statistics;
        private bool skipErrors;
        private int wipePassNumber;
        private FileSourceOperationSymLinkOption symLinkOption;
        private FileSourceOperationOptionGeneral deleteReadOnly;

        public FileSystemWipeOperation(IFileSource targetFileSource, FileEntries filesToWipe)
            : base(targetFileSource, filesToWipe)
        {
            skipErrors = false;
            symLinkOption = FileSourceOperationSymLinkOption.None;
            deleteReadOnly = FileSourceOperationOptionGeneral.None;
            fullFilesTreeToDelete = null;
            wipePassNumber = GlobalSettings.WipePassNumber;

            if (GlobalSettings.ProcessComments)
                description = new Description(true);
            else
                description = null;

            random = RandomNumberGenerator.Create();
            buffer = new byte[3][];
            for (int i = 0; i < 3; i++)
                buffer[i] = new byte[4096];
        }

        ~FileSystemWipeOperation()
        {
            if (description != null)
            {
                description.SaveDescription();
                description.Dispose();
            }

            random.Dispose();
        }

        protected override void Initialize()
        {
            Fill(0);
            Fill(1);

            // 获取初始化的统计信息
            statistics = RetrieveStatistics();

            FileSystemUtil.FillAndCount(FilesToWipe, true, false,
                out fullFilesTreeToDelete,
                out statistics.TotalFiles,
                out statistics.TotalBytes);     // 获取完整的文件列表（递归）

            if (GlobalSettings.ProcessComments)
                description.Clear();
        }

        protected override void MainExecute()
        {
            for (int currentFileIndex = fullFilesTreeToDelete.Count - 1; currentFileIndex >= 0; currentFileIndex--)
            {
                var file = fullFilesTreeToDelete[currentFileIndex];

                statistics.CurrentFile = file.FullPath;
                UpdateStatistics(statistics);

                // 如果Wipe中发生错误，DoneBytes值将不一致，所以在这里记住它
                long oldDoneBytes = statistics.DoneBytes;

                Wipe(file);

                statistics.DoneFiles++;
                // 如果文件未正确处理，则更正统计信息
                if (statistics.DoneBytes < (oldDoneBytes + file.Size))
                    statistics.DoneBytes = oldDoneBytes + file.Size;

                UpdateStatistics(statistics);

                CheckOperationState();
            }
        }

        private void Fill(int step)
        {
            switch (step)
            {
                case 0:
                    Array.Clear(buffer[step], 0, buffer[step].Length);
                    break;
                case 1:
                    Array.Fill(buffer[step], (byte)0xFF);
                    break;
                case 2:
                    random.GetBytes(buffer[step]);
                    break;
            }
        }

        private bool Rename(string fileName, out string newName)
        {
            bool retry;
            do
            {
                retry = false;
                newName = Path.Combine(Path.GetDirectoryName(fileName), 
                    Path.GetRandomFileName());
                if (!FileSystemUtil.RenameFileUAC(fileName, newName))
                {
                    retry = HandleError(string.Format(Resources.MsgErrRename, fileName, newName));
                }
            } while (retry);

            return !retry;
        }

        private bool WipeDir(string fileName)
        {
            bool retry;
            string tempFileName;
            bool result = Rename(fileName, out tempFileName);
            
            if (result)
            {
                do
                {
                    retry = false;
                    result = FileSystemUtil.RemoveDirectoryUAC(tempFileName);
                    if (!result)
                    {
                        retry = HandleError(string.Format(Resources.MsgCannotDeleteDirectory, tempFileName));
                    }
                } while (retry);
            }

            return result;
        }

        private bool WipeLink(string fileName)
        {
            bool retry;
            string tempFileName;
            bool result = Rename(fileName, out tempFileName);
            
            if (result)
            {
                do
                {
                    retry = false;
                    result = FileSystemUtil.DeleteFileUAC(tempFileName);
                    if (!result)
                    {
                        retry = HandleError(Resources.MsgNotDelete + tempFileName + Environment.NewLine + 
                            Marshal.GetLastWin32Error().ToString());
                    }
                } while (retry);
            }

            return result;
        }

        private bool WipeFile(string fileName)
        {
            // 检查文件访问权限
            bool retry;
            do
            {
                retry = false;
                if (!FileSystemUtil.FileAccess(fileName, FileAccess.Write))
                {
                    retry = HandleError(Resources.MsgErrEOpen + " " + fileName);
                    if (!retry) return false;
                }
            } while (retry);

            string tempFileName;
            if (!Rename(fileName, out tempFileName))
                return false;

            // 尝试打开文件
            FileStream targetFileStream = null;
            try
            {
                do
                {
                    retry = false;
                    try
                    {
                        targetFileStream = new FileStream(tempFileName, FileMode.Open, 
                            FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (Exception ex)
                    {
                        retry = HandleError(Resources.MsgErrEOpen + " " + tempFileName + 
                            Environment.NewLine + ex.Message);
                        if (!retry) return false;
                    }
                } while (retry);

                for (int i = 0; i < wipePassNumber; i++)
                {
                    CheckOperationState(); // 检查暂停和停止
                    statistics.CurrentFileTotalBytes = targetFileStream.Length * 3;
                    statistics.CurrentFileDoneBytes = 0;
                    UpdateStatistics(statistics);

                    for (int j = 0; j < 3; j++)
                    {
                        targetFileStream.Position = 0;
                        long totalBytesToWrite = targetFileStream.Length;

                        while (totalBytesToWrite > 0)
                        {
                            int bytesWritten = 0;

                            if (j == 2) Fill(j);

                            int bytesToWrite = (int)Math.Min(totalBytesToWrite, buffer[j].Length);

                            do
                            {
                                retry = false;
                                try
                                {
                                    targetFileStream.Write(buffer[j], 0, bytesToWrite);
                                    bytesWritten = bytesToWrite;
                                }
                                catch (Exception ex)
                                {
                                    retry = HandleError(Resources.MsgErrEWrite + " " + tempFileName + 
                                        Environment.NewLine + ex.Message);
                                    if (!retry) return false;
                                }
                            } while (retry);

                            totalBytesToWrite -= bytesWritten;

                            statistics.CurrentFileDoneBytes += bytesWritten;
                            statistics.DoneBytes += bytesWritten / (3 * wipePassNumber);
                            UpdateStatistics(statistics);
                            CheckOperationState(); // 检查暂停和停止
                        }

                        // 将数据刷新到磁盘
                        do
                        {
                            retry = false;
                            if (!FileSystemUtil.FileFlush(targetFileStream.Handle))
                            {
                                retry = HandleError(Resources.MsgErrEWrite + " " + tempFileName + 
                                    Environment.NewLine + Marshal.GetLastWin32Error().ToString());
                                if (!retry) return false;
                            }
                        } while (retry);

                        CheckOperationState(); // 检查暂停和停止
                    }
                }

                // 将文件大小截断为零
                do
                {
                    retry = false;
                    if (!FileSystemUtil.FileTruncate(targetFileStream.Handle, 0))
                    {
                        retry = HandleError(Resources.MsgErrEWrite + " " + tempFileName + 
                            Environment.NewLine + Marshal.GetLastWin32Error().ToString());
                        if (!retry) return false;
                    }
                } while (retry);
            }
            finally
            {
                targetFileStream?.Dispose();
            }

            bool result = true;
            do
            {
                retry = false;
                if (!FileSystemUtil.DeleteFileUAC(tempFileName))
                {
                    retry = HandleError(Resources.MsgNotDelete + tempFileName + 
                        Environment.NewLine + Marshal.GetLastWin32Error().ToString());
                }
            } while (retry);

            return result;
        }

        private void Wipe(FileEntry file)
        {
            string fileName = file.FullPath;
            bool wipeResult;

            if (file.IsReadOnly)
            {
                switch (deleteReadOnly)
                {
                    case FileSourceOperationOptionGeneral.None:
                        switch (AskQuestion(string.Format(Resources.MsgFileReadOnly, fileName), "",
                            new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.Skip, 
                                FileSourceOperationUIResponse.Abort, FileSourceOperationUIResponse.All, 
                                FileSourceOperationUIResponse.SkipAll },
                            FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.Abort))
                        {
                            case FileSourceOperationUIResponse.All:
                                deleteReadOnly = FileSourceOperationOptionGeneral.Yes;
                                break;
                            case FileSourceOperationUIResponse.Skip:
                                return;
                            case FileSourceOperationUIResponse.SkipAll:
                                deleteReadOnly = FileSourceOperationOptionGeneral.No;
                                return;
                            case FileSourceOperationUIResponse.Abort:
                                RaiseAbortOperation();
                                break;
                        }
                        break;
                    case FileSourceOperationOptionGeneral.No:
                        return;
                }
            }

            if (file.IsReadOnly)
                FileSystemUtil.FileSetReadOnlyUAC(fileName, false);

            if (file.IsDirectory) // 目录
                wipeResult = WipeDir(fileName);
            else if (file.IsLink) // 符号链接
                wipeResult = WipeLink(fileName);
            else // 普通文件
                wipeResult = WipeFile(fileName);

            if (file.IsDirectory)
            {
                if (!wipeResult)
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogWipeDir, fileName), 
                        LogOption.DirectoryOperation | LogOption.Delete, true);
                else
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogWipeDir, fileName), 
                        LogOption.DirectoryOperation | LogOption.Delete, false);
            }
            else
            {
                if (!wipeResult)
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogWipe, fileName), 
                        LogOption.Delete, true);
                else
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogWipe, fileName), 
                        LogOption.Delete, false);
            }

            // 如果需要，处理注释
            if (wipeResult && GlobalSettings.ProcessComments)
                description.DeleteDescription(fileName);
        }

        private bool HandleError(string message)
        {
            if (GlobalSettings.SkipFileOpError)
            {
                LogMessage(message, LogOption.Error, true);
                return false;
            }
            else if (!skipErrors)
            {
                switch (AskQuestion(message, "",
                    new[] { FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip, 
                        FileSourceOperationUIResponse.SkipAll, FileSourceOperationUIResponse.Abort },
                    FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip))
                {
                    case FileSourceOperationUIResponse.Retry:
                        return true;
                    case FileSourceOperationUIResponse.Abort:
                        RaiseAbortOperation();
                        break;
                    case FileSourceOperationUIResponse.Skip:
                        break;
                    case FileSourceOperationUIResponse.SkipAll:
                        skipErrors = true;
                        break;
                }
            }
            return false;
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