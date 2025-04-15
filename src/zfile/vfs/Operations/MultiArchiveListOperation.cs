using System;
using System.Collections.Generic;
using System.IO;

namespace MultiArchive
{
    public class MultiArchiveListOperation : FileSourceOperation
    {
        private readonly IMultiArchiveFileSource _multiArchiveFileSource;
        private readonly string _targetPath;
        private readonly List<ArchiveItem> _archiveItems;

        public MultiArchiveListOperation(
            IMultiArchiveFileSource fileSource,
            string targetPath)
        {
            _multiArchiveFileSource = fileSource;
            _targetPath = targetPath;
            _archiveItems = new List<ArchiveItem>();
        }

        public override void Initialize()
        {
            // 初始化操作
        }

        public override void MainExecute()
        {
            try
            {
                // 读取归档文件列表
                if (_multiArchiveFileSource.ReadArchive())
                {
                    // 处理归档项
                    foreach (var item in _archiveItems)
                    {
                        ProcessArchiveItem(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                throw;
            }
        }

        public override void Finalize()
        {
            // 清理操作
        }

        private void ProcessArchiveItem(ArchiveItem item)
        {
            // 处理单个归档项
            if (_multiArchiveFileSource.FileIsDirectory(item))
            {
                // 处理目录
            }
            else if (_multiArchiveFileSource.FileIsLink(item))
            {
                // 处理链接
            }
            else
            {
                // 处理普通文件
            }
        }
    }
} 