using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zfile
{
    /// <summary>
    /// Extension methods for FileSystemOperation related classes
    /// </summary>
    public static class FileSystemOperationExtensions
    {
        /// <summary>
        /// Gets the name of the file associated with this node
        /// </summary>
        public static string Name(this FileTreeNode node)
        {
            return node.TheFile?.Name ?? string.Empty;
        }

        /// <summary>
        /// Checks if the node represents a directory
        /// </summary>
        public static bool IsDirectory(this FileTreeNode node)
        {
            return node.TheFile?.IsDirectory ?? false;
        }

        /// <summary>
        /// Checks if the node represents a symbolic link
        /// </summary>
        public static bool IsLink(this FileTreeNode node)
        {
            return node.TheFile?.IsLink ?? false;
        }

        /// <summary>
        /// Gets the files associated with this node
        /// </summary>
        public static FileEntries Files(this FileTreeNode node)
        {
            // This is a placeholder implementation - you may need to adjust based on your actual data structure
            return new FileEntries();
        }

        /// <summary>
        /// Gets the size of the file associated with this node
        /// </summary>
        public static long Size(this FileTreeNode node)
        {
            return node.TheFile?.Size ?? 0;
        }

        /// <summary>
        /// Gets the file entry associated with this node
        /// </summary>
        public static FileEntry FileEntry(this FileTreeNode node)
        {
            return node.TheFile;
        }

        /// <summary>
        /// Checks if the result represents a directory
        /// </summary>
        public static bool IsDirectory(this FileSystemOperationTargetExistsResult result)
        {
            return result == FileSystemOperationTargetExistsResult.IsDirectory;
        }

        /// <summary>
        /// Checks if the result represents a file
        /// </summary>
        public static bool IsFile(this FileSystemOperationTargetExistsResult result)
        {
            return result == FileSystemOperationTargetExistsResult.IsFile;
        }

        /// <summary>
        /// Checks if the result represents a symbolic link
        /// </summary>
        public static bool IsLink(this FileSystemOperationTargetExistsResult result)
        {
            return result == FileSystemOperationTargetExistsResult.IsLink;
        }

        /// <summary>
        /// Gets the Delete option for directory exists
        /// </summary>
        public static bool Delete(this FileSourceOperationOptionDirectoryExists option)
        {
            return option == FileSourceOperationOptionDirectoryExists.Delete;
        }

        /// <summary>
        /// Gets the Append option for file exists
        /// </summary>
        public static bool Append(this FileSourceOperationOptionFileExists option)
        {
            return option == FileSourceOperationOptionFileExists.Append;
        }


    }
}
