namespace zfile
{
	public enum FsStatus : int
	{
		Start,
		End
	}

	public class WfxPluginSetFilePropertyOperation : FileSourceSetFilePropertyOperation
    {
        private readonly IWfxPluginFileSource _wfxPluginFileSource;
        private FileEntries _fullFilesTree;
        private FileSourceSetFilePropertyOperationStatistics _statistics;
        private FileSourceOperationSymLinkOption _symLinkOption;

        public WfxPluginSetFilePropertyOperation(IFileSource targetFileSource, FileEntries targetFiles, FileProperty[] newProperties)
            : base(targetFileSource, targetFiles, newProperties)
        {
            _symLinkOption = FileSourceOperationSymLinkOption.None;
            _fullFilesTree = null;
            _wfxPluginFileSource = targetFileSource as IWfxPluginFileSource;

            // Assign after calling inherited constructor.
            SupportedProperties = FilePropertyType.Name | FilePropertyType.Attributes | FilePropertyType.ModificationTime |FilePropertyType.CreationTime | FilePropertyType.LastAccessTime;
        }

        protected override void Initialize()
        {
            _wfxPluginFileSource.WfxModule.setStatusInfo(TargetFiles.Path, (int)FsStatus.Start, (int)FsStatusOperation.Attrib);
            _statistics = RetrieveStatistics();

            if (!Recursive)
            {
                _fullFilesTree = TargetFiles;
                _statistics.TotalFiles = _fullFilesTree.Count;
            }
            else
            {
                long totalBytes;
                _wfxPluginFileSource.FillAndCount(TargetFiles, true, false, out _fullFilesTree, out _statistics.TotalFiles, out totalBytes);
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
            _wfxPluginFileSource.WfxModule.setStatusInfo(TargetFiles.Path, (int)FsStatus.End, (int)FsStatusOperation.Attrib);
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
						var remotefileinfo = new RemoteFileInfo();
						remotefileinfo.SizeLow = file.Size;
						remotefileinfo.LastWriteTime = file.ModificationTime.ToFileTime();
						if (_wfxPluginFileSource.WfxModule.MoveFile(file.Name, nameProperty.Value, false, false, remotefileinfo) != 0)
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
                            if (!_wfxPluginFileSource.WfxModule.SetAttr(fileName, (int)newAttributes))
                            {
                                result = SetFilePropertyResult.Error;
                            }
                        }
                        else if (templateProperty is UnixFileAttributesProperty)
                        {
                            //if (_wfxPluginFileSource.WfxModule.ExecuteFile(Application.MainForm.Tag, fileName,
                            //    "chmod " + Convert.ToString(newAttributes & ~S_IFMT, 8)) != FsExecResult.Ok)
                            //{
                            //    result = SetFilePropertyResult.Error;
                            //}
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
                        if (!_wfxPluginFileSource.WfxModule.SetTime(file.FullPath, 0, 0, ftTime))
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
                        if (!_wfxPluginFileSource.WfxModule.SetTime(file.FullPath, ftTime, 0, 0))
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
                        if (!_wfxPluginFileSource.WfxModule.SetTime(file.FullPath, 0, ftTime, 0))
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

		private nint DateTimeToWfxFileTime(DateTime value)
		{
			throw new NotImplementedException();
		}
	}
} 