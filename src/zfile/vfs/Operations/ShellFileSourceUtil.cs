using System.Runtime.InteropServices;
using WinShell;
namespace zfile
{
    public class ItemList : List<IntPtr>
    {
        ~ItemList()
        {
            foreach (var item in this)
            {
                Marshal.FreeCoTaskMem(item);
            }
        }
    }

    public class FileShellProperty : FileLinkProperty
    {
        private IntPtr item;

        public IntPtr Item
        {
            get { return item; }
            set { item = value; }
        }

        ~FileShellProperty()
        {
            if (item != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(item);
            }
        }

        public override FileLinkProperty Clone()
        {
            var result = new FileShellProperty();
            CloneTo(result);
            return result;
        }

        public void CloneTo(FileProperty fileProperty)
        {
            if (fileProperty != null)
            {
                base.CloneTo(fileProperty);

                if (fileProperty is FileShellProperty shellProperty)
                {
                    shellProperty.item = API.ILClone(item);
                }
            }
        }
    }

    public delegate bool CheckOperationState();
    public delegate void UpdateCopyStatistics(ref FileSourceCopyOperationStatistics newStatistics);
    public delegate void UpdateDeleteStatistics(ref FileSourceDeleteOperationStatistics newStatistics);
    public delegate void UpdateSetFilePropertyStatistics(ref FileSourceSetFilePropertyOperationStatistics newStatistics);

    public class FileOperationProgressSink : IFileOperationProgressSink
    {
        private readonly CheckOperationState checkOperationState;
        private FileSourceCopyOperationStatistics copyStatistics;
        private readonly UpdateCopyStatistics updateCopyStatistics;
        private FileSourceDeleteOperationStatistics deleteStatistics;
        private readonly UpdateDeleteStatistics updateDeleteStatistics;
        private FileSourceSetFilePropertyOperationStatistics setFilePropertyStatistics;
        private readonly UpdateSetFilePropertyStatistics updateSetFilePropertyStatistics;

        public FileOperationProgressSink(
            ref FileSourceCopyOperationStatistics statistics,
            UpdateCopyStatistics updateStatistics,
            CheckOperationState checkState)
        {
            copyStatistics = statistics;
            updateCopyStatistics = updateStatistics;
            checkOperationState = checkState;
        }

        public FileOperationProgressSink(
            ref FileSourceDeleteOperationStatistics statistics,
            UpdateDeleteStatistics updateStatistics,
            CheckOperationState checkState)
        {
            deleteStatistics = statistics;
            updateDeleteStatistics = updateStatistics;
            checkOperationState = checkState;
        }

        public FileOperationProgressSink(
            ref FileSourceSetFilePropertyOperationStatistics statistics,
            UpdateSetFilePropertyStatistics updateStatistics,
            CheckOperationState checkState)
        {
            setFilePropertyStatistics = statistics;
            updateSetFilePropertyStatistics = updateStatistics;
            checkOperationState = checkState;
        }

        public int StartOperations()
        {
            return 0; // S_OK
        }

        public int FinishOperations(int hrResult)
        {
            return 0; // S_OK
        }

        public int PreRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName)
        {
            string fileName;
			IntPtr pszname;
			psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out pszname);
			if(pszname != IntPtr.Zero)
			{
				fileName = Marshal.PtrToStringUni(pszname);
				setFilePropertyStatistics.CurrentFile = fileName;
				Marshal.FreeCoTaskMem(pszname);
            }
            return 0; // S_OK
        }

        public int PostRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName, int hrRename, IShellItem psiNewlyCreated)
        {
            return 0; // S_OK
        }

        public int PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName)
        {
            return PreCopyItem(dwFlags, psiItem, psiDestinationFolder, pszNewName);
        }

        public int PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, int hrMove, IShellItem psiNewlyCreated)
        {
            if ((GlobalSettings.LogOptions & LogOption.CopyMoveLink) != 0 && hrMove != Constants.COPYENGINE_E_USER_CANCELLED)
            {
                if (hrMove == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogMove, 
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOption.CopyMoveLink, LogOption.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogMove,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOption.CopyMoveLink, LogOption.Error);
                }
            }
            return 0; // S_OK
        }

        public int PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName)
        {
            string fileName;
			IntPtr pszname;
			psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out pszname);
			if (pszname != IntPtr.Zero)
			{
				fileName = Marshal.PtrToStringUni(pszname);
				copyStatistics.CurrentFileFrom = fileName;
				Marshal.FreeCoTaskMem(pszname);
            }

			psiDestinationFolder.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out pszname);
			if (pszname != IntPtr.Zero)
			{
				fileName = Marshal.PtrToStringUni(pszname);
				copyStatistics.CurrentFileTo = fileName;
                if (!string.IsNullOrEmpty(pszNewName))
                {
                    copyStatistics.CurrentFileTo += pszNewName;
                }
                else
                {
                    copyStatistics.CurrentFileTo += Path.GetFileName(copyStatistics.CurrentFileFrom);
                }
				Marshal.FreeCoTaskMem(pszname);
			}

            updateCopyStatistics(ref copyStatistics);
            return 0; // S_OK
        }

        public int PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, int hrCopy, IShellItem psiNewlyCreated)
        {
            if ((GlobalSettings.LogOptions & LogOption.CopyMoveLink) != 0 && hrCopy != Constants.COPYENGINE_E_USER_CANCELLED)
            {
                if (hrCopy == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogCopy,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOption.CopyMoveLink, LogOption.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogCopy,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOption.CopyMoveLink, LogOption.Error);
                }
            }
            return 0; // S_OK
        }

        public int PreDeleteItem(uint dwFlags, IShellItem psiItem)
        {
            string fileName;
			IntPtr pszname;
			psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out pszname);
			if(pszname != IntPtr.Zero)
            {
				fileName = Marshal.PtrToStringUni(pszname);
				deleteStatistics.CurrentFile = fileName;
				Marshal.FreeCoTaskMem(pszname);
			}
            return 0; // S_OK
        }

        public int PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated)
        {
            if ((GlobalSettings.LogOptions & LogOption.Delete) != 0 && hrDelete != Constants.COPYENGINE_E_USER_CANCELLED)
            {
                //uint attributes;
                psiItem.GetAttributes(SFGAO.FOLDER, out var attributes);
                string text = ((uint)attributes & (uint)SFGAO.FOLDER) == 0 ? Resources.MsgLogDelete : Resources.MsgLogRmDir;

                if (hrDelete == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + text, deleteStatistics.CurrentFile),
                        LogOption.Delete, LogOption.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + text, deleteStatistics.CurrentFile),
                        LogOption.Delete, LogOption.Error);
                }
            }
            return 0; // S_OK
        }

        public int PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszNewName)
        {
            return 0; // S_OK
        }

        public int PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszNewName, string pszTemplateName, uint dwFileAttributes, int hrNew, IShellItem psiNewItem)
        {
            return 0; // S_OK
        }

        public int UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
        {
            if (!copyStatistics.Equals(default(FileSourceCopyOperationStatistics)))
            {
                copyStatistics.TotalBytes = iWorkTotal;
                copyStatistics.DoneBytes = iWorkSoFar;
                updateCopyStatistics(ref copyStatistics);
            }
            else if (deleteStatistics != null)
            {
                deleteStatistics.TotalFiles = iWorkTotal;
                deleteStatistics.DoneFiles = iWorkSoFar;
                updateDeleteStatistics(ref deleteStatistics);
            }
            else if (setFilePropertyStatistics != null)
            {
                setFilePropertyStatistics.TotalFiles = iWorkTotal;
                setFilePropertyStatistics.DoneFiles = iWorkSoFar;
                updateSetFilePropertyStatistics(ref setFilePropertyStatistics);
            }

            return checkOperationState() ? 0 : Constants.COPYENGINE_E_USER_CANCELLED;
        }

        public int ResetTimer()
        {
            return 0; // S_OK
        }

        public int PauseTimer()
        {
            return 0; // S_OK
        }

        public int ResumeTimer()
        {
            return 0; // S_OK
        }

        private void LogMessage(string message, LogOption logOptions, LogOption logMsgType)
        {
            if ((logOptions & GlobalSettings.LogOptions) != 0)
            {
                Logger.Write(null, message, logMsgType);
            }
        }
    }

	public interface IFileOperationProgressSink
	{
	}
} 