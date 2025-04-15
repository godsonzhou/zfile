using System;

namespace Zfile.FileSources
{
    /// <summary>
    /// Base exception class for file source exceptions
    /// </summary>
    public class FileSourceException : Exception
    {
        /// <summary>
        /// Creates a new instance of the FileSourceException class
        /// </summary>
        public FileSourceException() : base()
        {
        }

        /// <summary>
        /// Creates a new instance of the FileSourceException class with the specified message
        /// </summary>
        /// <param name="message">The message</param>
        public FileSourceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of the FileSourceException class with the specified message and inner exception
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        public FileSourceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a file is not found
    /// </summary>
    public class FileNotFound : FileSourceException
    {
        private readonly string _filePath;

        /// <summary>
        /// Gets the file path that was not found
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Creates a new instance of the FileNotFound class with the specified file path
        /// </summary>
        /// <param name="filePath">The file path</param>
        public FileNotFound(string filePath) : base($"File not found: {filePath}")
        {
            _filePath = filePath;
        }
    }
}