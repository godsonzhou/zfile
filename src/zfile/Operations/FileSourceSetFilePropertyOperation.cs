using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

namespace Zfile.Operations
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
    public struct FileSourceSetFilePropertyOperationStatistics
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
        private IFileSource _fileSource;
        private List<FileEntry> _targetFiles;
        private List<FileEntry> _templateFiles;
        private FileProperty[] _newProperties;
        private bool _recursive;
        private bool _skipErrors;

        /// <summary>
        /// Supported properties
        /// </summary>
        protected FilePropertyType _supportedProperties;

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
        public List<FileEntry> TargetFiles => _targetFiles;

        /// <summary>
        /// Gets or sets the new properties
        /// </summary>
        public FileProperty[] NewProperties
        {
            get => _newProperties;
            set => _newProperties = value;
        }

        /// <summary>
        /// Gets the template files
        /// </summary>
        public List<FileEntry> TemplateFiles => _templateFiles;

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
        public FilePropertyType SupportedProperties => _supportedProperties;

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
        protected FileSourceSetFilePropertyOperation(IFileSource aTargetFileSource, List<FileEntry> theTargetFiles, FileProperty[] theNewProperties)
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
            _skipErrors = Globals.SkipFileOpError;

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
                        DateTime.Now,
                        newStatistics.FilesPerSecond);

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
        public void SetTemplateFiles(List<FileEntry> theTemplateFiles)
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
            foreach (FilePropertyType prop in Enum.GetValues(typeof(FilePropertyType)))
            {
                if ((_supportedProperties & prop) == 0)
                    continue;

                bool retry;
                do
                {
                    retry = false;
                    SetFilePropertyResult setResult = SetFilePropertyResult.Success;

                    // Double-check that the property really is supported by the file
                    if ((aFile.SupportedProperties & prop) != 0 ||
                        (_fileSource.RetrievableFileProperties & prop) != 0)
                    {
                        // Get template property from template file (if exists) or NewProperties
                        FileProperty templateProperty = null;
                        if (aTemplateFile != null)
                            templateProperty = aTemplateFile.Properties[prop];
                        else if (_newProperties != null && (int)prop < _newProperties.Length)
                            templateProperty = _newProperties[(int)prop];

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
                        string errorString = GetErrorString(aFile, templateProperty);
                        string message = Strings.MsgLogError + errorString;
                        string question = errorString;

                        if (_skipErrors)
                        {
                            Logger.Log(Thread, message, LogMessageType.Error);
                        }
                        else
                        {
                            FileSourceOperationUIAnswer answer = AskQuestion(
                                question, "",
                                new[] { FileSourceOperationUIResponse.Retry, FileSourceOperationUIResponse.Skip, 
                                       FileSourceOperationUIResponse.SkipAll, FileSourceOperationUIResponse.Abort },
                                FileSourceOperationUIResponse.Retry,
                                FileSourceOperationUIAnswer.Abort);

                            switch (answer)
                            {
                                case FileSourceOperationUIAnswer.Retry:
                                    retry = true;
                                    break;
                                case FileSourceOperationUIAnswer.SkipAll:
                                    _skipErrors = true;
                                    break;
                                case FileSourceOperationUIAnswer.Abort:
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
        protected string GetErrorString(FileEntry aFile, FileProperty aProperty)
        {
            switch (aProperty.ID)
            {
                case FilePropertyType.Name:
                    return string.Format(Strings.MsgErrRename, aFile.FullPath, ((FileNameProperty)aProperty).Value);

                case FilePropertyType.Attributes:
                    return string.Format(Strings.MsgErrSetAttribute, aFile.FullPath);

                case FilePropertyType.ModificationTime:
                case FilePropertyType.CreationTime:
                case FilePropertyType.LastAccessTime:
                    return string.Format(Strings.MsgErrSetDateTime, aFile.FullPath);

                case FilePropertyType.Owner:
                    return string.Format(Strings.MsgErrSetOwnership, aFile.FullPath);

                default:
                    return Strings.MsgLogError;
            }
        }

        /// <summary>
        /// Estimates the remaining time for an operation
        /// </summary>
        /// <param name="doneAtStart">Items done at start</param>
        /// <param name="doneNow">Items done now</param>
        /// <param name="totalItems">Total items</param>
        /// <param name="startTime">Start time</param>
        /// <param name="currentTime">Current time</param>
        /// <param name="itemsPerSecond">Items per second</param>
        /// <returns>Estimated remaining time</returns>
        private static DateTime EstimateRemainingTime(long doneAtStart, long doneNow, long totalItems, 
                                                    DateTime startTime, DateTime currentTime, long itemsPerSecond)
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
    /// File property base class
    /// </summary>
    public abstract class FileProperty
    {
        /// <summary>
        /// Gets the property ID
        /// </summary>
        public abstract FilePropertyType ID { get; }
    }

    /// <summary>
    /// File name property
    /// </summary>
    public class FileNameProperty : FileProperty
    {
        /// <summary>
        /// Gets the property ID
        /// </summary>
        public override FilePropertyType ID => FilePropertyType.Name;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// File attributes property
    /// </summary>
    public class FileAttributesProperty : FileProperty
    {
        /// <summary>
        /// Gets the property ID
        /// </summary>
        public override FilePropertyType ID => FilePropertyType.Attributes;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public FileAttributes Value { get; set; }
    }

    /// <summary>
    /// File property type
    /// </summary>
    [Flags]
    public enum FilePropertyType
    {
        /// <summary>
        /// No property
        /// </summary>
        None = 0,

        /// <summary>
        /// File name
        /// </summary>
        Name = 1,

        /// <summary>
        /// File attributes
        /// </summary>
        Attributes = 2,

        /// <summary>
        /// File size
        /// </summary>
        Size = 4,

        /// <summary>
        /// Modification time
        /// </summary>
        ModificationTime = 8,

        /// <summary>
        /// Creation time
        /// </summary>
        CreationTime = 16,

        /// <summary>
        /// Last access time
        /// </summary>
        LastAccessTime = 32,

        /// <summary>
        /// File link
        /// </summary>
        Link = 64,

        /// <summary>
        /// File owner
        /// </summary>
        Owner = 128,

        /// <summary>
        /// File type
        /// </summary>
        Type = 256,

        /// <summary>
        /// File comment
        /// </summary>
        Comment = 512,

        /// <summary>
        /// All properties
        /// </summary>
        All = 1023
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

    /// <summary>
    /// Static class for global settings
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// Whether to skip file operation errors
        /// </summary>
        public static bool SkipFileOpError { get; set; } = false;

        /// <summary>
        /// File operations progress kind
        /// </summary>
        public static FileOperationsProgressKind FileOperationsProgressKind { get; set; } = FileOperationsProgressKind.SeparateWindow;
    }

    /// <summary>
    /// Logger class
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="thread">The thread</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        public static void Log(Thread thread, string message, LogMessageType messageType)
        {
            // Implementation would go here
        }
    }

    /// <summary>
    /// Log message type
    /// </summary>
    public enum LogMessageType
    {
        /// <summary>
        /// Error message
        /// </summary>
        Error,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning,

        /// <summary>
        /// Information message
        /// </summary>
        Info
    }
}