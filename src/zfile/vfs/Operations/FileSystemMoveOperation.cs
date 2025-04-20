namespace zfile
{
    public class FileSystemMoveOperation : FileSourceMoveOperation
    {
        private CopyAttributesOption copyAttributesOptions;
        private FileSystemOperationHelper operationHelper;
        private bool excludeEmptyTemplateDirectories;
        private SearchTemplate searchTemplate;
        private FileSourceOperationOptionSetPropertyError setPropertyError;
        private FileTree sourceFilesTree;
        private FileSourceCopyOperationStatistics statistics;

        // 选项
        private bool verify;
        private bool reserveSpace;
        private bool checkFreeSpace;
        private bool skipAllBigFiles;
        private bool correctSymlinks;

        public FileSystemMoveOperation(IFileSource fileSource, FileEntries sourceFiles, string targetPath)
            : base(fileSource, sourceFiles, targetPath)
        {
            // 读取全局设置
            copyAttributesOptions = new CopyAttributesOption();
            if (GlobalSettings.OperationOptionCopyAttributes)
                copyAttributesOptions |= CopyAttributesOption.CopyAttributes;
            if (GlobalSettings.OperationOptionCopyTime)
                copyAttributesOptions |= CopyAttributesOption.CopyTime;
            if (GlobalSettings.OperationOptionCopyOwnership)
                copyAttributesOptions |= CopyAttributesOption.CopyOwner;

            FileExistsOption = GlobalSettings.OperationOptionFileExists;
            DirExistsOption = GlobalSettings.OperationOptionDirectoryExists;
            setPropertyError = GlobalSettings.OperationOptionSetPropertyError;
            reserveSpace = GlobalSettings.OperationOptionReserveSpace;
            checkFreeSpace = GlobalSettings.OperationOptionCheckFreeSpace;
            skipAllBigFiles = false;
            correctSymlinks = GlobalSettings.OperationOptionCorrectLinks;
            excludeEmptyTemplateDirectories = true;
        }

        ~FileSystemMoveOperation()
        {
            sourceFilesTree?.Dispose();
            operationHelper?.Dispose();
            searchTemplate?.Dispose();
        }

        protected override void Initialize()
        {
            // 获取初始化的统计信息
            statistics = RetrieveStatistics();

            var treeBuilder = new FileSystemTreeBuilder(
                this.CreateAskQuestionDelegate(),
                CheckOperationState);
            try
            {
                treeBuilder.Recursive = Recursive();
                // 在移动操作中不跟随符号链接
                treeBuilder.SymLinkOption = FileSourceOperationSymLinkOption.DontFollow;
                treeBuilder.SearchTemplate = SearchTemplate;
                treeBuilder.ExcludeEmptyTemplateDirectories = ExcludeEmptyTemplateDirectories;

                treeBuilder.BuildFromFiles(SourceFiles);
                sourceFilesTree = treeBuilder.ReleaseTree();
                statistics.TotalFiles = treeBuilder.FilesCount;
                statistics.TotalBytes = treeBuilder.FilesSize;
            }
            finally
            {
                treeBuilder.Dispose();
            }

            operationHelper?.Dispose();
            operationHelper = new FileSystemOperationHelper(
                this.CreateAskQuestionDelegate(),
                () => RaiseAbortOperation(),
                () => AppProcessMessages(),
                () => CheckOperationState(),
                UpdateStatistics,
                (sourceFile, targetFilePath) => { /* 暂未实现 */ },
                _thread,
                FileSourceOperationHelperMode.Move,
                TargetPath,
                statistics);

            operationHelper.Verify = verify;
            operationHelper.RenameMask = RenameMask;
            operationHelper.ReserveSpace = reserveSpace;
            operationHelper.CheckFreeSpace = checkFreeSpace;
            operationHelper.CopyAttributesOptions = copyAttributesOptions;
            operationHelper.SkipAllBigFiles = skipAllBigFiles;
            operationHelper.CorrectSymLinks = correctSymlinks;
            operationHelper.FileExistsOption = FileExistsOption;
            operationHelper.DirExistsOption = DirExistsOption;
            operationHelper.SetPropertyError = setPropertyError;
            operationHelper.Initialize();
        }

        protected override void MainExecute()
        {
            operationHelper.ProcessTree(sourceFilesTree);
        }

        private void SetSearchTemplate(SearchTemplate value)
        {
            searchTemplate?.Dispose();
            searchTemplate = value;
        }

        private bool Recursive()
        {
            // 首先检查两个路径是否在同一卷上
            if (!FileSystemUtil.IsSameVolume(
                Path.GetDirectoryName(SourceFiles.Path),
                Path.GetDirectoryName(TargetPath)))
            {
                return true;
            }

            if ((RenameMask != "*.*" && !string.IsNullOrEmpty(RenameMask)) ||
                correctSymlinks || searchTemplate != null)
            {
                return true;
            }

            return false;
        }

        //protected override void Finalize()
        //{
        //	FileExistsOption = operationHelper.FileExistsOption;
        //	operationHelper?.Dispose();
        //}

        public Type GetOptionsUIClass()
        {
            return typeof(FileSystemMoveOperationOptionsUI);
        }

        // 属性
        public bool Verify
        {
            get => verify;
            set => verify = value;
        }

        public bool CheckFreeSpace
        {
            get => checkFreeSpace;
            set => checkFreeSpace = value;
        }

        public bool ReserveSpace
        {
            get => reserveSpace;
            set => reserveSpace = value;
        }

        public CopyAttributesOption CopyAttributesOptions
        {
            get => copyAttributesOptions;
            set => copyAttributesOptions = value;
        }

        public bool SkipAllBigFiles
        {
            get => skipAllBigFiles;
            set => skipAllBigFiles = value;
        }

        public bool CorrectSymLinks
        {
            get => correctSymlinks;
            set => correctSymlinks = value;
        }

        public FileSourceOperationOptionSetPropertyError SetPropertyError
        {
            get => setPropertyError;
            set => setPropertyError = value;
        }

        public bool ExcludeEmptyTemplateDirectories
        {
            get => excludeEmptyTemplateDirectories;
            set => excludeEmptyTemplateDirectories = value;
        }

        public SearchTemplate SearchTemplate
        {
            get => searchTemplate;
            set => SetSearchTemplate(value);
        }
    }
}