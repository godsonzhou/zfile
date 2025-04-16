namespace zfile
{
    public class MultiArchiveTestArchiveOperation : FileSourceOperation
    {
        private readonly IMultiArchiveFileSource _multiArchiveFileSource;
        private readonly FileEntries _sourceFiles;
        private bool _testResult;

        public MultiArchiveTestArchiveOperation(
            IMultiArchiveFileSource fileSource,
            ref FileEntries sourceFiles)
        {
            _multiArchiveFileSource = fileSource;
            _sourceFiles = sourceFiles;
        }

        protected override void Initialize()
        {
            // 初始化操作
        }

        protected override void MainExecute()
        {
            try
            {
                // 执行归档测试
                _testResult = TestArchive();
            }
            catch (Exception ex)
            {
                // 处理异常
                throw;
            }
        }

        protected override void Finalize()
        {
            // 清理操作
        }

        private bool TestArchive()
        {
            try
            {
                // 使用归档工具测试文件
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _multiArchiveFileSource.MultiArcItem.Packer,
                        Arguments = GetTestArguments(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetTestArguments()
        {
            var args = _multiArchiveFileSource.MultiArcItem.Test;
            args = args.Replace("%P", _multiArchiveFileSource.ArchiveFileName);
            args = args.Replace("%p", _multiArchiveFileSource.Password);
            return args;
        }
    }
} 