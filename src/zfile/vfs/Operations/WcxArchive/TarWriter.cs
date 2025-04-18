using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Tar;
using System;
using System.IO;

namespace zfile
{
    public class TarWriter : IDisposable
    {
        private readonly string _archiveFileName;
        private readonly FileSourceOperationAskQuestionFunction _askQuestion;
        private readonly FileSourceOperationAbortFunction _abortOperation;
        private readonly FileSourceOperationCheckStateFunction _checkOperationState;
        private readonly FileSourceOperationUpdateStatisticsFunction _updateStatistics;
        private readonly WcxModule _wcxModule;
        private TarArchive _tarArchive;
        private TarWriter _tarWriter;

        public TarWriter(string archiveFileName,
                        FileSourceOperationAskQuestionFunction askQuestion,
                        FileSourceOperationAbortFunction abortOperation,
                        FileSourceOperationCheckStateFunction checkOperationState,
                        FileSourceOperationUpdateStatisticsFunction updateStatistics)
        {
            _archiveFileName = archiveFileName;
            _askQuestion = askQuestion;
            _abortOperation = abortOperation;
            _checkOperationState = checkOperationState;
            _updateStatistics = updateStatistics;
            _wcxModule = null;
        }

        public TarWriter(string archiveFileName,
                        FileSourceOperationAskQuestionFunction askQuestion,
                        FileSourceOperationAbortFunction abortOperation,
                        FileSourceOperationCheckStateFunction checkOperationState,
                        FileSourceOperationUpdateStatisticsFunction updateStatistics,
                        WcxModule wcxModule)
        {
            _archiveFileName = archiveFileName;
            _askQuestion = askQuestion;
            _abortOperation = abortOperation;
            _checkOperationState = checkOperationState;
            _updateStatistics = updateStatistics;
            _wcxModule = wcxModule;
        }

        public bool ProcessTree(FileEntries files, FileSourceCopyOperationStatistics statistics)
        {
            try
            {
                // Create a new TAR archive
                using (var tarStream = File.Create(_archiveFileName))
                {
                    var writerOptions = new TarWriterOptions(CompressionType.None, true);
                    using (var writer = new SharpCompress.Writers.Tar.TarWriter(tarStream, writerOptions))
                    {
                        // Process all files in the tree
                        foreach (var file in files)
                        {
                            // Check if operation should be aborted
                            _checkOperationState();

                            // Skip if file doesn't exist
                            if (!File.Exists(file.FullPath) && !Directory.Exists(file.FullPath))
                                continue;

                            try
                            {
                                string relativePath = GetRelativePath(files.Path, file.FullPath);

                                if (file.IsDirectory)
                                {
                                    // Add directory entry
                                    writer.Write(relativePath, null, DateTime.Now, null);
                                }
                                else
                                {
                                    // Add file entry
                                    using (var fileStream = File.OpenRead(file.FullPath))
                                    {
                                        writer.Write(relativePath, fileStream, file.ModificationTime, file.Size);
                                        
                                        // Update statistics
                                        statistics.DoneFiles++;
                                        statistics.DoneBytes += file.Size;
                                        _updateStatistics(statistics);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Show error message
                                var response = _askQuestion(
                                    $"Error adding file {file.FullPath} to archive: {ex.Message}",
                                    "",
                                    new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                                    FileSourceOperationUIResponse.Skip,
                                    FileSourceOperationUIResponse.Abort);

                                if (response == FileSourceOperationUIResponse.Abort)
                                {
                                    _abortOperation();
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Show error message
                var response = _askQuestion(
                    $"Error creating TAR archive: {ex.Message}",
                    "",
                    new[] { FileSourceOperationUIResponse.Skip, FileSourceOperationUIResponse.Abort },
                    FileSourceOperationUIResponse.Skip,
                    FileSourceOperationUIResponse.Abort);

                if (response == FileSourceOperationUIResponse.Abort)
                {
                    _abortOperation();
                }

                return false;
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            // Ensure paths end with directory separator
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar;

            // Get relative path
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(basePath.Length);
            
            return Path.GetFileName(fullPath);
        }

        public void Dispose()
        {
            _tarArchive?.Dispose();
            _tarWriter?.Dispose();
        }
    }
}
