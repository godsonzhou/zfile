namespace zfile
{
    public class WfxPluginSetFilePropertyOperation : FileSourceSetFilePropertyOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private FileEntries _fullFilesTree;
        private FileSourceSetFilePropertyOperationStatistics _statistics;
        private FileSourceOperationSymlinkOption _symLinkOption;

        public WfxPluginSetFilePropertyOperation(IFileSource targetFileSource, ref FileEntries targetFiles, ref FileProperties newProperties)
            : base(targetFileSource, ref targetFiles, ref newProperties)
        {
            _symLinkOption = FileSourceOperationSymlinkOption.None;
            _fullFilesTree = null;
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;

            // Assign after calling inherited constructor.
            SupportedProperties = new[]
            {
                FilePropertyType.Name,
                FilePropertyType.Attributes,
                FilePropertyType.ModificationTime,
                FilePropertyType.CreationTime,
                FilePropertyType.LastAccessTime
            };
        }

        protected override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(TargetFiles.Path, FsStatus.Start, FsStatusOperation.Attrib);
            _statistics = RetrieveStatistics;

            if (!Recursive)
            {
                _fullFilesTree = TargetFiles;
                _statistics.TotalFiles = _fullFilesTree.Count;
            }
            else
            {
                long totalBytes;
                _wfxPluginFileSource.FillAndCount(TargetFiles, true, false, ref _fullFilesTree, ref _statistics.TotalFiles, ref totalBytes);
            }
        }

        protected override void MainExecute()
        {
            for (int currentFileIndex = 0; currentFileIndex < _fullFilesTree.Count; currentFileIndex++)
            {
                var file = _fullFilesTree[currentFileIndex];
                _statistics.CurrentFile = file.FullPath;
                UpdateStatistics(_statistics);

                var templateFile = TemplateFiles != null && currentFileIndex < TemplateFiles.Count
                    ? TemplateFiles[currentFileIndex]
                    : null;

                SetProperties(currentFileIndex, file, templateFile);

                _statistics.DoneFiles++;
                UpdateStatistics(_statistics);

                CheckOperationState();
            }
        }

        protected override void Finalize()
        {
            _wfxPluginFileSource.WfxModule.WfxStatusInfo(TargetFiles.Path, FsStatus.End, FsStatusOperation.Attrib);
        }

        protected override SetFilePropertyResult SetNewProperty(FileEntry file, FileProperty templateProperty)
        {
            var result = SetFilePropertyResult.Success;

            switch (templateProperty.ID)
            {
                case FilePropertyType.Name:
                    var nameProperty = (FileNameProperty)templateProperty;
                    if (nameProperty.Value != file.Name)
                    {
                        if (!WfxRenameFile(_wfxPluginFileSource, file, nameProperty.Value))
                        {
                            result = SetFilePropertyResult.Error;
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;

                case FilePropertyType.Attributes:
                    var attributesProperty = (FileAttributesProperty)templateProperty;
                    var currentAttributes = (FileAttributesProperty)file.Properties[FilePropertyType.Attributes];
                    if (attributesProperty.Value != currentAttributes.Value)
                    {
                        var newAttributes = attributesProperty.Value;
                        var fileName = file.FullPath;

                        if (templateProperty is NtfsFileAttributesProperty)
                        {
                            if (!_wfxPluginFileSource.WfxModule.WfxSetAttr(fileName, newAttributes))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else if (templateProperty is UnixFileAttributesProperty)
                        {
                            if (_wfxPluginFileSource.WfxModule.WfxExecuteFile(Application.MainForm.Tag, fileName,
                                "chmod " + Convert.ToString(newAttributes & ~S_IFMT, 8)) != FsExecResult.Ok)
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else
                        {
                            throw new Exception("Unsupported file attributes type");
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;

                case FilePropertyType.ModificationTime:
                    var modTimeProperty = (FileModificationDateTimeProperty)templateProperty;
                    var currentModTime = (FileModificationDateTimeProperty)file.Properties[FilePropertyType.ModificationTime];
                    if (modTimeProperty.Value != currentModTime.Value)
                    {
                        var ftTime = DateTimeToWfxFileTime(modTimeProperty.Value);
                        if (!_wfxPluginFileSource.WfxModule.WfxSetTime(file.FullPath, null, null, ref ftTime))
                        {
                            result = SetFilePropertyResult.Error;
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;

                case FilePropertyType.CreationTime:
                    var createTimeProperty = (FileCreationDateTimeProperty)templateProperty;
                    var currentCreateTime = (FileCreationDateTimeProperty)file.Properties[FilePropertyType.CreationTime];
                    if (createTimeProperty.Value != currentCreateTime.Value)
                    {
                        var ftTime = DateTimeToWfxFileTime(createTimeProperty.Value);
                        if (!_wfxPluginFileSource.WfxModule.WfxSetTime(file.FullPath, ref ftTime, null, null))
                        {
                            result = SetFilePropertyResult.Error;
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;

                case FilePropertyType.LastAccessTime:
                    var accessTimeProperty = (FileLastAccessDateTimeProperty)templateProperty;
                    var currentAccessTime = (FileLastAccessDateTimeProperty)file.Properties[FilePropertyType.LastAccessTime];
                    if (accessTimeProperty.Value != currentAccessTime.Value)
                    {
                        var ftTime = DateTimeToWfxFileTime(accessTimeProperty.Value);
                        if (!_wfxPluginFileSource.WfxModule.WfxSetTime(file.FullPath, null, ref ftTime, null))
                        {
                            result = SetFilePropertyResult.Error;
                        }
                    }
                    else
                    {
                        result = SetFilePropertyResult.Skipped;
                    }
                    break;

                default:
                    throw new Exception("Trying to set unsupported property");
            }

            return result;
        }

		private object DateTimeToWfxFileTime(DateTime value)
		{
			throw new NotImplementedException();
		}
	}
} 