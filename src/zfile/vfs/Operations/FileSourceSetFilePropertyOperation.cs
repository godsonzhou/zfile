using System.Threading;
namespace zfile
{
    /// <summary>
    /// Result of setting a file property
    /// </summary>
    public enum SetFilePropertyResult
    {
        /// <summary>
        /// Property was set successfully
        /// </summary>
        Success,

        /// <summary>
        /// Error occurred while setting property
        /// </summary>
        Error,

        /// <summary>
        /// Setting property was skipped
        /// </summary>
        Skipped
    }

    /// <summary>
    /// Delegate for reporting set file property results
    /// </summary>
    /// <param name="index">Index of the file</param>
    /// <param name="file">The file</param>
    /// <param name="template">The template property</param>
    /// <param name="result">The result of the operation</param>
    public delegate void SetFilePropertyResultFunction(int index, FileEntry file, FileProperty template, SetFilePropertyResult result);

    /// <summary>
    /// Statistics for set file property operation
    /// </summary>
    public class FileSourceSetFilePropertyOperationStatistics
    {
        /// <summary>
        /// Current file being processed
        /// </summary>
        public string CurrentFile;

        /// <summary>
        /// Total number of files to process
        /// </summary>
        public long TotalFiles;

        /// <summary>
        /// Number of files processed
        /// </summary>
        public long DoneFiles;

        /// <summary>
        /// Files processed per second
        /// </summary>
        public long FilesPerSecond;

        /// <summary>
        /// Estimated remaining time
        /// </summary>
        public DateTime RemainingTime;
    }

    /// <summary>
    /// Operation that can set any of the file properties supported by a file source.
    /// It doesn't have to support all the file properties supported by the file source, it can be a subset.
    ///
    /// There are two methods of setting properties available:
    ///
    /// - NewProperties
    ///   Set via constructor, this is a list of properties that should be set for
    ///   each file. If a property in this list is not assigned it is not set.
    ///   If a property in this list is not supported by the file source or by
    ///   this operation it is also not set.
    ///
    /// - TemplateFiles
    ///   Set by calling SetTemplateFiles.
    ///   Template files describe 1 to 1 correspondence between files and their
    ///   new properties. Each i-th file in the TargetFiles list will be assigned
    ///   properties based on propertes of i-th template file.
    ///   Template files need not be of the same type as target files,
    ///   it is enough for them to have properties supported by the target files.
    ///   If template file is not used for i-th file, then the i-th member
    ///   of the list should be set to null, but should be present to maintain
    ///   the correct correspondence between target and template files.
    ///   In other words number of target files must be the same as number of
    ///   template files.
    ///
    /// The two above methods can be used together.
    /// Template files, if present, always take precedence over NewProperties.
    /// If a template file is not present (= null), then the NewProperties are used as a template.
    /// Template files usually will not be used when Recursive is true,
    /// although this behaviour is dependent on the concrete descendant operations.
    /// If template files list is null, to indicate that the template files
    /// are not used, then only the NewProperties are used.
    /// </summary>
    public abstract class FileSourceSetFilePropertyOperation : FileSourceOperation
    {
        private FileSourceSetFilePropertyOperationStatistics _statistics;
        private FileSourceSetFilePropertyOperationStatistics _statisticsAtStartTime;
        private readonly object _statisticsLock = new object();
        //private IFileSource _fileSource;
        private FileEntries _targetFiles;
        private FileEntries _templateFiles;
        private FileProperties _newProperties;
        private bool _recursive;
        private bool _skipErrors;
        //private Thread _thread = Thread.CurrentThread;

        /// <summary>
        /// Supported properties
        /// </summary>
        protected FilePropertiesTypes _supportedProperties;

        /// <summary>
        /// Function to call when a property is set
        /// </summary>
        protected SetFilePropertyResultFunction _setFilePropertyResultFunction;

        /// <summary>
        /// Gets the operation type
        /// </summary>
        public override FileSourceOperationTypes OperationType => FileSourceOperationTypes.SetFileProperty;

        /// <summary>
        /// Attributes to include
        /// </summary>
        public FileAttributes IncludeAttributes { get; set; }

        /// <summary>
        /// Attributes to exclude
        /// </summary>
        public FileAttributes ExcludeAttributes { get; set; }

        /// <summary>
        /// Gets the target files
        /// </summary>
        public FileEntries TargetFiles => _targetFiles;

        /// <summary>
        /// Gets or sets the new properties
        /// </summary>
        public FileProperties NewProperties
        {
            get => _newProperties;
            set => _newProperties = value;
        }

        /// <summary>
        /// Gets the template files
        /// </summary>
        public FileEntries TemplateFiles => _templateFiles;

        /// <summary>
        /// Gets or sets whether to process recursively
        /// </summary>
        public bool Recursive
        {
            get => _recursive;
            set => _recursive = value;
        }

        /// <summary>
        /// Gets the supported properties
        /// </summary>
        public FilePropertiesTypes SupportedProperties { get => _supportedProperties; set => _supportedProperties = value; }

        /// <summary>
        /// Gets or sets whether to skip errors
        /// </summary>
        public bool SkipErrors
        {
            get => _skipErrors;
            set => _skipErrors = value;
        }

        /// <summary>
        /// Gets or sets the function to call when a property is set
        /// </summary>
        public SetFilePropertyResultFunction OnSetFilePropertyResult
        {
            set => _setFilePropertyResultFunction = value;
        }

        /// <summary>
        /// Creates a new instance of the FileSourceSetFilePropertyOperation class
        /// </summary>
        /// <param name="aTargetFileSource">File source on which the operation will be executed</param>
        /// <param name="theTargetFiles">List of files which properties should be changed</param>
        /// <param name="theNewProperties">Describes the set of properties that should be set for each file of theTargetFiles</param>
        protected FileSourceSetFilePropertyOperation(IFileSource aTargetFileSource, FileEntries theTargetFiles, FileProperties theNewProperties)
            : base(aTargetFileSource)
        {
            _statistics = new FileSourceSetFilePropertyOperationStatistics
            {
                CurrentFile = "",
                TotalFiles = 0,
                DoneFiles = 0,
                FilesPerSecond = 0,
                RemainingTime = DateTime.MinValue
            };

            _fileSource = aTargetFileSource;
            _targetFiles = theTargetFiles;
            _newProperties = theNewProperties;
            _templateFiles = null;
            _recursive = false;
            _skipErrors = GlobalSettings.SkipFileOpError;

            _supportedProperties = 0;
        }

        /// <summary>
        /// Reloads file sources after the operation
        /// </summary>
        protected override void DoReloadFileSources()
        {
            _fileSource.Reload(_targetFiles[0].Path);
        }

        /// <summary>
        /// Gets the description of the operation
        /// </summary>
        /// <param name="details">The level of detail</param>
        /// <returns>The description</returns>
        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            switch (details)
            {
                case FileSourceOperationDescriptionDetails.JobAndTarget:
                    if (_targetFiles.Count == 1)
                        return string.Format(Strings.OperSettingPropertyOf, _targetFiles[0].FullPath);
                    else
                        return string.Format(Strings.OperSettingPropertyIn, _targetFiles[0].Path);
                default:
                    return Strings.OperSettingProperty;
            }
        }

        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="newStatistics">The new statistics</param>
        protected void UpdateStatistics(FileSourceSetFilePropertyOperationStatistics newStatistics)
        {
            lock (_statisticsLock)
            {
                // Check if the value by which we calculate progress and remaining time has changed
                if (_statistics.DoneFiles != newStatistics.DoneFiles)
                {
                    newStatistics.RemainingTime = EstimateRemainingTime(
                        _statisticsAtStartTime.DoneFiles,
                        newStatistics.DoneFiles,
                        newStatistics.TotalFiles,
                        StartTime,
                        DateTime.Now);

                    // Update overall progress
                    if (newStatistics.TotalFiles != 0)
                        UpdateProgress((double)newStatistics.DoneFiles / newStatistics.TotalFiles);
                }

                _statistics = newStatistics;

                // Process any pending messages
                System.Windows.Forms.Application.DoEvents();
            }
        }

        /// <summary>
        /// Updates the statistics at start time
        /// </summary>
        protected override void UpdateStatisticsAtStartTime()
        {
            lock (_statisticsLock)
            {
                _statisticsAtStartTime = _statistics;
            }
        }

        /// <summary>
        /// Retrieves the current statistics
        /// </summary>
        /// <returns>The current statistics</returns>
        public FileSourceSetFilePropertyOperationStatistics RetrieveStatistics()
        {
            lock (_statisticsLock)
            {
                return _statistics;
            }
        }

        /// <summary>
        /// Sets the template files
        /// </summary>
        /// <param name="theTemplateFiles">The template files</param>
        public void SetTemplateFiles(FileEntries theTemplateFiles)
        {
            _templateFiles = theTemplateFiles;
        }

        /// <summary>
        /// Sets properties for a file
        /// </summary>
        /// <param name="index">Index of the file</param>
        /// <param name="aFile">The file</param>
        /// <param name="aTemplateFile">The template file</param>
        protected void SetProperties(int index, FileEntry aFile, FileEntry aTemplateFile)
        {
            // Iterate over all properties supported by this operation
            foreach (FilePropertiesTypes prop in Enum.GetValues(typeof(FilePropertiesTypes)))
            {
                if ((_supportedProperties & prop) == 0)
                    continue;

                bool retry;
                do
                {
                    retry = false;
                    SetFilePropertyResult setResult = SetFilePropertyResult.Success;
                    // Declare templateProperty at the do-while loop level
                    FileProperty? templateProperty = null;

                    // Double-check that the property really is supported by the file
                    if (((uint)aFile.SupportedProperties & (uint)prop) != 0 ||
                        ((uint)_fileSource.RetrievableFileProperties & (uint)prop) != 0)
                    {
                        // Get template property from template file (if exists) or NewProperties
                        if (aTemplateFile != null)
                            templateProperty = aTemplateFile.Properties[prop];
                        else if (_newProperties != null && (int)prop < _newProperties.Length)
                            templateProperty = _newProperties[prop];

                        // Check if there is a new property to be set
                        if (templateProperty != null)
                        {
                            // Special case for attributes property
                            if (templateProperty is FileAttributesProperty)
                            {
                                if (IncludeAttributes != 0 || ExcludeAttributes != 0)
                                {
                                    FileAttributes fileAttrs = aFile.Attributes;
                                    fileAttrs |= IncludeAttributes;
                                    fileAttrs &= ~ExcludeAttributes;
                                    ((FileAttributesProperty)templateProperty).Value = fileAttrs;
                                }
                            }

                            setResult = SetNewProperty(aFile, templateProperty);

                            if (_setFilePropertyResultFunction != null)
                            {
                                _setFilePropertyResultFunction(index, aFile, templateProperty, setResult);
                            }
                        }
                    }

                    if (setResult == SetFilePropertyResult.Error)
                    {
                        string errorString = templateProperty != null
                            ? GetErrorString(aFile, templateProperty)
                            : Strings.MsgLogError;
                        string message = Strings.MsgLogError + errorString;
                        string question = errorString;

                        if (_skipErrors)
                        {
                            Logger.Write(_thread, message, LogOption.Error);
                        }
                        else
                        {
                            FileSourceOperationUIResponse answer = AskQuestion(
                                question, "",
                                new[] { FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip,
                                       FileSourceOperationUIResponse.SkipAll, FileSourceOperationUIResponse.Abort },
                                FileSourceOperationUIResponse.Retry,
                                FileSourceOperationUIResponse.Abort);

                            switch (answer)
                            {
                                case FileSourceOperationUIResponse.Retry:
                                    retry = true;
                                    break;
                                case FileSourceOperationUIResponse.SkipAll:
                                    _skipErrors = true;
                                    break;
                                case FileSourceOperationUIResponse.Abort:
                                    RaiseAbortOperation();
                                    break;
                            }
                        }
                    }
                } while (retry);
            }
        }

        /// <summary>
        /// Sets a new property for a file
        /// </summary>
        /// <param name="aFile">The file</param>
        /// <param name="aTemplateProperty">The template property</param>
        /// <returns>Result of the operation</returns>
        protected abstract SetFilePropertyResult SetNewProperty(FileEntry aFile, FileProperty aTemplateProperty);

        /// <summary>
        /// Gets an error string for a property
        /// </summary>
        /// <param name="aFile">The file</param>
        /// <param name="aProperty">The property</param>
        /// <returns>The error string</returns>
        protected static string GetErrorString(FileEntry aFile, FileProperty aProperty)
        {
            if (aProperty == null)
                return Strings.MsgLogError;

            return aProperty.ID switch
            {
                FilePropertiesTypes.Name => string.Format(Strings.MsgErrRename, aFile.FullPath, ((FileNameProperty)aProperty).Value),
                FilePropertiesTypes.Attributes => string.Format(Strings.MsgErrSetAttribute, aFile.FullPath),
                FilePropertiesTypes.ModificationTime => string.Format(Strings.MsgErrSetDateTime, aFile.FullPath),
                FilePropertiesTypes.CreationTime => string.Format(Strings.MsgErrSetDateTime, aFile.FullPath),
                FilePropertiesTypes.LastAccessTime => string.Format(Strings.MsgErrSetDateTime, aFile.FullPath),
                FilePropertiesTypes.Owner => string.Format(Strings.MsgErrSetOwnership, aFile.FullPath),
                _ => Strings.MsgLogError
            };
        }

        /// <summary>
        /// Estimates the remaining time for an operation
        /// </summary>
        /// <param name="doneAtStart">Items done at start</param>
        /// <param name="doneNow">Items done now</param>
        /// <param name="totalItems">Total items</param>
        /// <param name="startTime">Start time</param>
        /// <param name="currentTime">Current time</param>
        /// <returns>Estimated remaining time</returns>
        private static DateTime EstimateRemainingTime(long doneAtStart, long doneNow, long totalItems,
                                                    DateTime startTime, DateTime currentTime)
        {
            if (doneNow <= doneAtStart || totalItems <= doneNow)
                return DateTime.MinValue;

            double elapsedTime = (currentTime - startTime).TotalSeconds;
            if (elapsedTime <= 0)
                return DateTime.MinValue;

            double itemsPerSec = (doneNow - doneAtStart) / elapsedTime;
            if (itemsPerSec <= 0)
                return DateTime.MinValue;

            double remainingTime = (totalItems - doneNow) / itemsPerSec;
            return DateTime.Now.AddSeconds(remainingTime);
        }
    }

    /// <summary>
    /// File source operation description details
    /// </summary>
    public enum FileSourceOperationDescriptionDetails
    {
        /// <summary>
        /// Basic description
        /// </summary>
        Basic,

        /// <summary>
        /// Job and target description
        /// </summary>
        JobAndTarget
    }

    /// <summary>
    /// Static class for string resources
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Setting property
        /// </summary>
        public static readonly string OperSettingProperty = "Setting property";

        /// <summary>
        /// Setting property of {0}
        /// </summary>
        public static readonly string OperSettingPropertyOf = "Setting property of {0}";

        /// <summary>
        /// Setting property in {0}
        /// </summary>
        public static readonly string OperSettingPropertyIn = "Setting property in {0}";

        /// <summary>
        /// Error:
        /// </summary>
        public static readonly string MsgLogError = "Error: ";

        /// <summary>
        /// Cannot rename {0} to {1}
        /// </summary>
        public static readonly string MsgErrRename = "Cannot rename {0} to {1}";

        /// <summary>
        /// Cannot set attributes of {0}
        /// </summary>
        public static readonly string MsgErrSetAttribute = "Cannot set attributes of {0}";

        /// <summary>
        /// Cannot set date/time of {0}
        /// </summary>
        public static readonly string MsgErrSetDateTime = "Cannot set date/time of {0}";

        /// <summary>
        /// Cannot set ownership of {0}
        /// </summary>
        public static readonly string MsgErrSetOwnership = "Cannot set ownership of {0}";
    }


}