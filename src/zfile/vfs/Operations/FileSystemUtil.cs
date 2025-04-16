using System.Runtime.InteropServices;

namespace zfile
{
	public static class FileSystemUtil
	{
		private const string HASH_TYPE = "HASH_BEST";

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

	public class FileSystemTreeBuilder : FileSourceTreeBuilder
	{
		protected override void AddLinkTarget(FileEntry file, FileTreeNode currentNode)
		{
			// 实现链接目标的添加
		}

		protected override void AddFilesInDirectory(string srcPath, FileTreeNode currentNode)
		{
			// 实现目录中文件的添加
		}
	}


}