using System;
using System.IO;
using System.Runtime.InteropServices;

namespace zfile
{

	public class WcxArchiveDeleteOperation : FileSourceDeleteOperation
	{
		private IWcxArchiveFileSource _wcxArchiveFileSource;
		private FileSourceDeleteOperationStatistics _statistics;

		// WCX interface cannot discern different operations (for reporting progress),
		// so this static variable is used to store currently running operation.
		private static WcxArchiveDeleteOperation _wcxDeleteOperation = null;

		public WcxArchiveDeleteOperation(IFileSource targetFileSource, FileEntries filesToDelete)
			: base(targetFileSource, filesToDelete)
		{
			_wcxArchiveFileSource = (IWcxArchiveFileSource)targetFileSource;
		}

		protected override void Initialize()
		{
			if (_wcxDeleteOperation != null && _wcxDeleteOperation != this)
				throw new Exception("Another WCX delete operation is already running");

			_wcxDeleteOperation = this;

			// Get initialized statistics; then we change only what is needed.
			_statistics = RetrieveStatistics();

			CountFiles(FilesToDelete, "*.*");
		}
		~WcxArchiveDeleteOperation()
		{
			ClearCurrentOperation();
		}
		protected override void MainExecute()
		{
			var wcxModule = _wcxArchiveFileSource.WcxModule;

			wcxModule.WcxSetChangeVolProc(WcxModule.WcxInvalidHandle);

			// 设置进程数据回调
			// 创建符合TProcessDataProc签名的委托
			TProcessDataProc procA = (string arcName, int mode) =>
			{
				// 在这里我们只关心size参数，因为实际的文件名在统计信息中已经有了
				return ProcessDataProc(arcName ?? string.Empty, mode);
			};
			TProcessDataProcW procW = (string arcName, int mode) =>
			{
				return ProcessDataProc(arcName ?? string.Empty, mode);
			};

			// 获取委托的函数指针
			IntPtr procAPtr = Marshal.GetFunctionPointerForDelegate(procA);
			IntPtr procWPtr = Marshal.GetFunctionPointerForDelegate(procW);

			// 保持委托引用防止被GC回收
			GC.KeepAlive(procA);
			GC.KeepAlive(procW);

			wcxModule.WcxSetProcessDataProc(WcxModule.WcxInvalidHandle, procAPtr, procWPtr);

			int result = wcxModule.DeleteFiles(_wcxArchiveFileSource.ArchiveFileName,
											   GetFileEntries(FilesToDelete));

			// Check for errors.
			if (result != WcxModule.E_SUCCESS)
			{
				// User aborted operation.
				if (result == WcxModule.E_EABORTED) return;

				ShowError(string.Format("Error deleting from {0}: {1}",
						   _wcxArchiveFileSource.ArchiveFileName,
						   WcxModule.GetErrorMsg(result)), result, LogOption.ArcOp);
			}
			else
			{
				LogMessage(string.Format("Successfully deleted from {0}",
						   _wcxArchiveFileSource.ArchiveFileName), LogOption.ArcOp, LogOption.Success);
			}
		}

		// 注意：不要使用Finalize方法，因为它会干扰析构函数的调用
		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ClearCurrentOperation();
			}
			base.Dispose(disposing);
		}

		private void CountFiles(FileEntries files, string fileMask)
		{
			var arcFileEntries = _wcxArchiveFileSource.ArchiveFileEntries.LockList();
			try
			{
				foreach (var item in arcFileEntries)
				{
					var header = (WcxHeader)item;

					// Check if the file from the archive fits the selection given via theFiles.
					if ((header.FileAttr & FileAttributes.Directory) != FileAttributes.Directory &&           // Omit directories
						MatchesFileEntries(files, header.FileName) &&    // Check if it's included in the FileEntries
						(fileMask == "*.*" || fileMask == "*" ||    // And name matches file mask
						 MatchesMaskList(Path.GetFileName(header.FileName), fileMask)))
					{
						_statistics.TotalBytes += header.UnpSize;
						_statistics.TotalFiles++;
					}
				}
			}
			finally
			{
				_wcxArchiveFileSource.ArchiveFileEntries.UnlockList();
			}

			UpdateStatistics(_statistics);
		}

		private string GetFileEntries(FileEntries files)
		{
			string result = "";

			foreach (var file in files)
			{
				// Filenames must be relative to archive root and shouldn't start with path delimiter.
				string fileName = Helper.ExcludeFrontPathDelimiter(file.FullPath);

				// Special treatment of directories.
				if (file.IsDirectory)
					// TC ends paths to directories to be deleted with '\*.*'
					// (which means delete this directory and all files in it).
					fileName = Helper.IncludeTrailingPathDelimiter(fileName) + "*.*";

				result += fileName + "\0";
			}

			result += "\0";
			return result;
		}

		public static void ClearCurrentOperation()
		{
			_wcxDeleteOperation = null;
		}

		// WCX callback methods
		private static int ProcessDataProc(string fileName, int size)
		{
			// Implementation of process data callback
			int result = 1;

			if (_wcxDeleteOperation != null)
			{
				if (_wcxDeleteOperation.State == FileSourceOperationState.Stopping)  // Cancel operation
					return 0;

				var statistics = _wcxDeleteOperation._statistics;
				statistics.CurrentFile = fileName;

				// Get the number of bytes processed since the previous call
				if (size > 0)
				{
					statistics.TotalFiles = 100;
					statistics.DoneBytes += size;
					statistics.DoneFiles = statistics.DoneBytes * 100 / statistics.TotalBytes;
				}
				// Get progress percent value to directly set progress bar
				else if (size < 0)
				{
					// Total operation percent
					if (size >= -100 && size <= -1)
					{
						statistics.TotalFiles = 100;
						statistics.DoneFiles = -size;
					}
				}

				_wcxDeleteOperation.UpdateStatistics(statistics);
				if (!_wcxDeleteOperation.CheckOperationStateSafe()) return 0;
			}

			return result;
		}
	}
}