using System.Runtime.InteropServices;

namespace zfile
{
	public static class FileSystemUtil
	{
		private const string HASH_TYPE = "HASH_BEST";
		public static bool IsInPath(string path1, string path2, bool allowPartial, bool caseSensitive)
		{
			return path1.StartsWith(path2, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
		}
		public static string ApplyRenameMask(FileEntry file, string nameMask, string extMask)
		{
			// 只对文件进行重命名
			if (file.IsDirectory || file.IsLink)
				return file.Name;
			else
				return ApplyRenameMask(file, nameMask, extMask);
		}

		public static void FillAndCount(
			FileEntries files,
			bool countDirs,
			bool excludeRootDir,
			out FileEntries newFiles,
			out long filesCount,
			out long filesSize)
		{
			filesCount = 0;
			filesSize = 0;

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
						// 处理链接文件
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							filesCount++;
						FillAndCountRec(file.FullPath + Path.DirectorySeparatorChar);
					}
					else
					{
						filesSize += file.Size;
						filesCount++;
					}
				}
			}

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
						// 处理链接文件
					}
					else if (file.IsDirectory)
					{
						if (countDirs)
							filesCount++;
						FillAndCountRec(Path.Combine(srcPath, Path.GetFileName(entry)));
					}
					else
					{
						filesSize += file.Size;
						filesCount++;
					}
				}
			}
		}

		public static string FileExistsMessage(string targetName, string sourceName, long sourceSize, DateTime sourceTime)
		{
			return string.Format(Resources.FileExistsMessage,
				targetName,
				sourceName,
				sourceSize,
				sourceTime);
		}
	}

	public delegate void UpdateStatisticsFunction(ref FileSourceCopyOperationStatistics newStatistics);

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
			// 实现链接目标的添加
		}

		protected override void AddFilesInDirectory(string srcPath, FileTreeNode currentNode)
		{
			// 实现目录中文件的添加
		}
	
		private readonly Action<string, bool> _askQuestion;
		private readonly Action _checkOperationState;
		private FileTree _currentTree;
		private long _filesCount;
		private long _filesSize;

		public FileSourceOperationOptionGeneral SymLinkOption { get; set; }
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