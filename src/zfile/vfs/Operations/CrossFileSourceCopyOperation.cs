//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using zfile.vfs.FileSources;

//namespace zfile.vfs.Operations
//{
//    /// <summary>
//    /// 跨文件源复制操作
//    /// </summary>
//    public class CrossFileSourceCopyOperation : FileSourceOperation
//    {
//        private readonly IFileSource _sourceFileSource;
//        private readonly IFileSource _targetFileSource;
//        private readonly FileEntries _fileEntries;
//        private readonly string _targetPath;
//        private readonly string _tempDir;

//        public CrossFileSourceCopyOperation(
//            IFileSource sourceFileSource,
//            IFileSource targetFileSource,
//            FileEntries fileEntries,
//            string targetPath)
//        {
//            _sourceFileSource = sourceFileSource;
//            _targetFileSource = targetFileSource;
//            _fileEntries = fileEntries;
//            _targetPath = targetPath;
//            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
//            Directory.CreateDirectory(_tempDir);
//        }

//        public override void Execute()
//        {
//            try
//            {
//                // 1. 从源文件源复制到临时目录
//                var tempFileEntries = new FileEntries();
//                foreach (var entry in _fileEntries)
//                {
//                    // 创建临时文件路径
//                    string tempFilePath = Path.Combine(_tempDir, entry.Name);
                    
//                    // 从源文件源复制到临时目录
//                    var sourceOperation = _sourceFileSource.CreateCopyOutOperation(
//                        new FileEntries { entry },
//                        _tempDir);
                    
//                    if (sourceOperation != null)
//                    {
//                        sourceOperation.Execute();
                        
//                        // 添加临时文件到临时文件条目列表
//                        if (File.Exists(tempFilePath) || Directory.Exists(tempFilePath))
//                        {
//                            var tempEntry = new FileEntry
//                            {
//                                Name = entry.Name,
//                                FullPath = tempFilePath,
//                                IsDirectory = Directory.Exists(tempFilePath),
//                                Size = File.Exists(tempFilePath) ? new FileInfo(tempFilePath).Length : 0,
//                                ModificationTime = File.GetLastWriteTime(tempFilePath),
//                                Attributes = File.GetAttributes(tempFilePath)
//                            };
//                            tempFileEntries.Add(tempEntry);
//                        }
//                    }
//                }

//                // 2. 从临时目录复制到目标文件源
//                if (tempFileEntries.Count > 0)
//                {
//                    var targetOperation = _targetFileSource.CreateCopyInOperation(
//                        tempFileEntries,
//                        _targetPath);
                    
//                    if (targetOperation != null)
//                    {
//                        targetOperation.Execute();
//                    }
//                }
//            }
//            finally
//            {
//                // 清理临时目录
//                if (Directory.Exists(_tempDir))
//                {
//                    try
//                    {
//                        Directory.Delete(_tempDir, true);
//                    }
//                    catch
//                    {
//                        // 忽略清理临时目录时的错误
//                    }
//                }
//            }
//        }
//    }
//}
