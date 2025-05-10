using FluentFTP;
using System.Diagnostics;

namespace zfile
{
    /// <summary>
    /// FTP复制入操作，用于将文件从本地复制到FTP服务器
    /// </summary>
    public class FtpCopyInOperation : FileSourceCopyInOperation
    {
        private readonly FtpFileSource _ftpFileSource;

        /// <summary>
        /// 创建FTP复制入操作
        /// </summary>
        /// <param name="sourceFileSource">源文件源</param>
        /// <param name="targetFileSource">目标FTP文件源</param>
        /// <param name="sourceFiles">源文件列表</param>
        /// <param name="targetPath">目标路径</param>
        public FtpCopyInOperation(IFileSource sourceFileSource, FtpFileSource targetFileSource, FileEntries sourceFiles, string targetPath)
            : base(sourceFileSource, targetFileSource, sourceFiles, targetPath)
        {
            _ftpFileSource = targetFileSource;
            _statistics = new FileSourceCopyOperationStatistics();
        }

        /// <summary>
        /// 初始化操作
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            _statistics = RetrieveStatistics();
        }

        /// <summary>
        /// 执行主操作
        /// </summary>
        protected override void MainExecute()
        {
			var treebuilder = new FileSystemTreeBuilder(
				(caption, msg, possibleResponses, defaultResponse, skipResponse) =>
					AskQuestion(caption, msg, possibleResponses, defaultResponse, skipResponse),
				CheckOperationState)
			{
				SymLinkOption = SymLinkOption
				//SearchTemplate = SearchTemplate,
				//ExcludeEmptyTemplateDirectories = ExcludeEmptyTemplateDirectories
			};
			// 构建文件树

			treebuilder.BuildFromFiles(SourceFiles);
			var sourceFilesTree = treebuilder.ReleaseTree();
			_statistics.TotalFiles = treebuilder.FilesCount;
			_statistics.TotalBytes = treebuilder.FilesSize;
			//if (_verify)
			//	_statistics.TotalBytes *= 2;
			// 创建文件树
			//var sourceFilesTree = CreateFilesTree(SourceFiles);
            
            // 处理文件树
            ProcessNode(sourceFilesTree, TargetPath);
        }

        /// <summary>
        /// 处理文件树节点
        /// </summary>
        /// <param name="node">文件树节点</param>
        /// <param name="targetPath">目标路径</param>
        private void ProcessNode(FileTree node, string targetPath)
        {
            foreach (var subNode in node.SubNodes)
            {
                CheckOperationState();
                
                string newTargetPath = Path.Combine(targetPath, subNode.Name).Replace('\\', '/');
                
                if (subNode.IsDirectory)
                {
                    // 创建目录
                    try
                    {
                        _ftpFileSource.CreateDirectory(newTargetPath);
                        UpdateStatistics(1, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"创建FTP目录失败: {ex.Message}");
                        if (AskQuestion($"创建目录 {newTargetPath} 失败: {ex.Message}\n", "是否继续?", 
                            new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                            FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                            != FileSourceOperationUIResponse.Yes)
                        {
                            RaiseAbortOperation();
                            return;
                        }
                    }
                    
                    // 递归处理子目录
                    ProcessNode(subNode, newTargetPath);
                }
                else
                {
                    // 复制文件
                    CopyFile(subNode.Path, newTargetPath);
                }
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标文件路径</param>
        private void CopyFile(string sourcePath, string targetPath)
        {
            try
            {
                // 获取文件大小
                var fileInfo = new FileInfo(sourcePath);
                long fileSize = fileInfo.Length;
                
                // 更新统计信息
                _statistics.TotalBytes += fileSize;
                _statistics.TotalFiles++;
                UpdateStatistics(_statistics);
                
                // 上传文件
                bool success = _ftpFileSource.UploadFile(sourcePath, targetPath);
                
                if (success)
                {
                    // 更新统计信息
                    _statistics.DoneBytes += fileSize;
                    _statistics.DoneFiles++;
                    UpdateStatistics(_statistics);
                }
                else
                {
                    throw new Exception("上传文件失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制文件到FTP失败: {ex.Message}");
                if (AskQuestion($"复制文件 {sourcePath} 到 {targetPath} 失败: {ex.Message}\n", "是否继续?", 
                    new[] { FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No },
                    FileSourceOperationUIResponse.Yes, FileSourceOperationUIResponse.No) 
                    != FileSourceOperationUIResponse.Yes)
                {
                    RaiseAbortOperation();
                }
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="doneFiles">已完成文件数</param>
        /// <param name="doneBytes">已完成字节数</param>
        private void UpdateStatistics(int doneFiles, long doneBytes)
        {
            _statistics.DoneFiles += doneFiles;
            _statistics.DoneBytes += doneBytes;
            UpdateStatistics(_statistics);
        }
    }
}
