using System.Runtime.InteropServices;

namespace zfile
{
	public struct FileAttributeData
	{
		public FileAttributes Attr;
		public long Size;
		public long LastWriteTime;
		public long CreationTime;
		public long LastAccessTime;
	}

	public static class FileSystemUtil
	{
		private const string HASH_TYPE = "HASH_BEST";

		public static DateTime FileTimeToDateTime(long fileTime)
		{
			return DateTime.FromFileTime(fileTime);
		}
		public static bool IsInPath(string path1, string path2, bool allowPartial, bool caseSensitive)
		{
			return path1.StartsWith(path2, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
		}
		public static string ApplyRenameMask(FileEntry file, string nameMask, string extMask)
		{
			// Only rename files, not directories or links
			if (file.IsDirectory || file.IsLink)
				return file.Name;
			else
				return ApplyRenameMask(file.Name, nameMask, extMask);
		}

		public static string ApplyRenameMask(string fileName, string nameMask, string extMask)
		{
			if (string.IsNullOrEmpty(nameMask) && string.IsNullOrEmpty(extMask))
				return fileName;

			string name = Path.GetFileNameWithoutExtension(fileName);
			string ext = Path.GetExtension(fileName);
			if (ext.StartsWith(".")) ext = ext.Substring(1);

			if (!string.IsNullOrEmpty(nameMask) && nameMask != "*")
				name = nameMask.Replace("*", name);

			if (!string.IsNullOrEmpty(extMask) && extMask != "*")
				ext = extMask.Replace("*", ext);

			if (string.IsNullOrEmpty(ext))
				return name;
			else
				return name + "." + ext;
		}

		public static void FillAndCount(
			FileEntries files,
			bool countDirs,
			bool excludeRootDir,
			out FileEntries newFiles,
			out long filesCount,
			out long filesSize)
		{
			// Create a class to hold the counters that can be modified in the local function
			var counters = new CounterHolder { FilesCount = 0, FilesSize = 0 };

			if (excludeRootDir)
			{
				if (files.Count != 1)
					throw new Exception("Only a single directory can be set with ExcludeRootDir=True");

				newFiles = new FileEntries(files[0].FullPath);
				FillAndCountRec(files[0].FullPath + Path.DirectorySeparatorChar);
			}
			else
			{
				newFiles = new FileEntries(files.Path);
				foreach (var file in files)
				{
					newFiles.Add(file);
					if (file.IsLink)
					{
						// Handle link files
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							counters.FilesCount++;
						FillAndCountRec(file.FullPath + Path.DirectorySeparatorChar);
					}
					else
					{
						counters.FilesSize += file.Size;
						counters.FilesCount++;
					}
				}
			}

			// Assign the final values to the out parameters
			filesCount = counters.FilesCount;
			filesSize = counters.FilesSize;

			void FillAndCountRec(string srcPath)
			{
				var searchResults = Directory.GetFileSystemEntries(srcPath);
				foreach (var entry in searchResults)
				{
					if (entry == "." || entry == "..") continue;

					var file = FileSystemFileSource.CreateFile(srcPath, entry);
					newFiles.Add(file);

					if (file.IsLink)
					{
						// Handle link files
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							counters.FilesCount++;
						FillAndCountRec(Path.Combine(srcPath, Path.GetFileName(entry)));
					}
					else
					{
						counters.FilesSize += file.Size;
						counters.FilesCount++;
					}
				}
			}
		}

		// Helper class to hold counters that can be modified in local functions
		private class CounterHolder
		{
			public long FilesCount { get; set; }
			public long FilesSize { get; set; }
		}

		public static string FileExistsMessage(string targetName, string sourceName, long sourceSize, DateTime sourceTime)
		{
			return string.Format(Resources.FileExistsMessage,
				targetName,
				sourceName,
				sourceSize,
				sourceTime);
		}

		internal static bool SetTimeExUAC(string fullPath, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
		{
			try
			{
				if (creationTime.HasValue)
					File.SetCreationTime(fullPath, creationTime.Value);
				if (lastAccessTime.HasValue)
					File.SetLastAccessTime(fullPath, lastAccessTime.Value);
				if (lastWriteTime.HasValue)
					File.SetLastWriteTime(fullPath, lastWriteTime.Value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static bool RenameFileUAC(string oldName, string newName)
		{
			try
			{
				File.Move(oldName, newName);
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static bool GetAttributesUAC(string newName, out FileAttributeData newAttr)
		{
			newAttr = new FileAttributeData();
			try
			{
				if (File.Exists(newName) || Directory.Exists(newName))
				{
					var fileInfo = new FileInfo(newName);
					newAttr.Attr = fileInfo.Attributes;
					newAttr.Size = fileInfo.Length;
					newAttr.LastWriteTime = fileInfo.LastWriteTime.ToFileTime();
					newAttr.CreationTime = fileInfo.CreationTime.ToFileTime();
					newAttr.LastAccessTime = fileInfo.LastAccessTime.ToFileTime();
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsDirectory(object attr)
		{
			if (attr is FileAttributes fileAttr)
			{
				return (fileAttr & FileAttributes.Directory) == FileAttributes.Directory;
			}
			return false;
		}

		internal static bool SetAttributesUAC(string fullPath, FileAttributes? value)
		{
			if (!value.HasValue)
				return false;

			try
			{
				File.SetAttributes(fullPath, value.Value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static void GetDiskFreeSpace(string targetPath, out long freeSpace, out long totalSpace)
		{
			freeSpace = 0;
			totalSpace = 0;

			try
			{
				string rootPath = Path.GetPathRoot(targetPath);
				if (!string.IsNullOrEmpty(rootPath))
				{
					DriveInfo drive = new DriveInfo(rootPath);
					freeSpace = drive.AvailableFreeSpace;
					totalSpace = drive.TotalSize;
				}
			}
			catch
			{
				// Return zeros on error
			}
		}

		internal static void FileSetReadOnlyUAC(string fileName, bool v)
		{
			try
			{
				FileAttributes attributes = File.GetAttributes(fileName);

				if (v)
					attributes |= FileAttributes.ReadOnly;
				else
					attributes &= ~FileAttributes.ReadOnly;

				File.SetAttributes(fileName, attributes);
			}
			catch
			{
				// Ignore errors
			}
		}

		internal static bool FileFlush(nint handle)
		{
			try
			{
				return FlushFileBuffers(handle);
			}
			catch
			{
				return false;
			}
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlushFileBuffers(nint hFile);

		internal static bool FileTruncate(nint handle, int size)
		{
			try
			{
				return SetFilePointerEx(handle, size, IntPtr.Zero, 0) && SetEndOfFile(handle);
			}
			catch
			{
				return false;
			}
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetEndOfFile(nint hFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetFilePointerEx(nint hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod);

		internal static bool FileAccess(string fileName, FileAccess access)
		{
			try
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Open, access, FileShare.ReadWrite))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		internal static bool DeleteFileUAC(string tempFileName)
		{
			try
			{
				File.Delete(tempFileName);
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static bool RemoveDirectoryUAC(string directoryName)
		{
			try
			{
				Directory.Delete(directoryName);
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsSameVolume(string? v1, string? v2)
		{
			throw new NotImplementedException();
		}

		internal static bool FileGetAttr(string linkTarget, out FileAttributeData attr)
		{
			return GetAttributesUAC(linkTarget, out attr);
		}
	}

	public delegate void UpdateStatisticsFunction(ref FileSourceCopyOperationStatistics newStatistics);
	public delegate void ShowCompareFilesUIFunction();
	public delegate void ShowCompareFilesUIByFileObjectFunction(FileEntry file1, FileEntry file2);


	public enum FileSystemOperationTargetExistsResult
	{
		NotExists,
		Deleted,
		AddToTarget,
		Resume,
		Skip,
		Renamed
	}

	public enum FileSystemOperationHelperMode
	{
		Copy,
		Move
	}

	public enum FileSystemOperationHelperCopyMode
	{
		Default,
		Append,
		Resume
	}

	public delegate bool FileSystemOperationHelperMoveOrCopy(
		FileEntry sourceFile,
		string targetFileName,
		FileSystemOperationHelperCopyMode mode);

	public class FileSystemTreeBuilder : FileSourceTreeBuilder, IDisposable
	{
		protected override void AddLinkTarget(FileEntry file, FileTreeNode currentNode)
		{
			string linkedFilePath = Path.GetFullPath(file.FullPath);
			if (!string.IsNullOrEmpty(linkedFilePath) && !(file.IsLinkToDirectory && FileSystemUtil.IsInPath(linkedFilePath, file.FullPath, true, true)))
			{
				try
				{
					FileEntry linkedFile = FileSystemFileSource.CreateFileFromFile(linkedFilePath);

					// Add link to current node
					var addedNode = currentNode.SubNodes[currentNode.AddSubNode(file)];

					// Then add linked file/directory as a subnode of the link
					AddItem(linkedFile, addedNode);
				}
				catch
				{
					// Link target doesn't exist - add symlink instead of target
					AddLink(file, currentNode);
				}
			}
			else
			{
				// Error - cannot follow symlink - adding symlink instead of target
				AddLink(file, currentNode);
			}
		}

		protected override void AddFilesInDirectory(string srcPath, FileTreeNode currentNode)
		{
			try
			{
				var entries = Directory.GetFileSystemEntries(srcPath);
				foreach (var entry in entries)
				{
					string fileName = Path.GetFileName(entry);
					if (fileName == "." || fileName == "..") continue;

					FileEntry file = FileSystemFileSource.CreateFileFromFile(entry);
					AddItem(file, currentNode);
				}
			}
			catch
			{
				// Ignore errors
			}
		}

		private readonly Action<string, bool> _askQuestion;
		private readonly Action _checkOperationState;
		private FileTree _currentTree;
		private long _filesCount;
		private long _filesSize;

		public FileSourceOperationSymLinkOption SymLinkOption { get; set; }
		public SearchTemplate SearchTemplate { get; set; }
		public bool ExcludeEmptyTemplateDirectories { get; set; }

		public long FilesCount => _filesCount;
		public long FilesSize => _filesSize;
		public bool Recursive { get; set; }

		public FileSystemTreeBuilder(Action<string, bool> askQuestion, Action checkOperationState)
		{
			_askQuestion = askQuestion;
			_checkOperationState = checkOperationState;
		}

		public void BuildFromFiles(FileEntries files)
		{
			_currentTree = new FileTree(string.Empty);
			_filesCount = 0;
			_filesSize = 0;

			foreach (var file in files)
			{
				_checkOperationState();
				ProcessFile(file);
			}
		}

		private void ProcessFile(FileEntry file)
		{
			if (SearchTemplate != null && !SearchTemplate.Check(file))
				return;

			_filesCount++;
			_filesSize += file.Size;
			_currentTree.AddFile(file);
		}

		public FileTree ReleaseTree()
		{
			var tree = _currentTree;
			_currentTree = null;
			return tree;
		}

		public void Dispose()
		{
			_currentTree?.Dispose();
		}
	}

}