using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace FileSystemOperations
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

        public override void CloneTo(FileProperty fileProperty)
        {
            if (fileProperty != null)
            {
                base.CloneTo(fileProperty);

                if (fileProperty is FileShellProperty shellProperty)
                {
                    shellProperty.item = ILClone(item);
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
            if (psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out fileName) == 0)
            {
                setFilePropertyStatistics.CurrentFile = fileName;
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
            if ((GlobalSettings.LogOptions & LogOptions.CopyMoveLink) != 0 && hrMove != COPYENGINE_E_USER_CANCELLED)
            {
                if (hrMove == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogMove, 
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOptions.CopyMoveLink, LogMsgType.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogMove,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOptions.CopyMoveLink, LogMsgType.Error);
                }
            }
            return 0; // S_OK
        }

        public int PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName)
        {
            string fileName;
            if (psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out fileName) == 0)
            {
                copyStatistics.CurrentFileFrom = fileName;
            }

            if (psiDestinationFolder.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out fileName) == 0)
            {
                copyStatistics.CurrentFileTo = fileName;
                if (!string.IsNullOrEmpty(pszNewName))
                {
                    copyStatistics.CurrentFileTo += pszNewName;
                }
                else
                {
                    copyStatistics.CurrentFileTo += Path.GetFileName(copyStatistics.CurrentFileFrom);
                }
            }

            updateCopyStatistics(ref copyStatistics);
            return 0; // S_OK
        }

        public int PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, int hrCopy, IShellItem psiNewlyCreated)
        {
            if ((GlobalSettings.LogOptions & LogOptions.CopyMoveLink) != 0 && hrCopy != COPYENGINE_E_USER_CANCELLED)
            {
                if (hrCopy == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + Resources.MsgLogCopy,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOptions.CopyMoveLink, LogMsgType.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + Resources.MsgLogCopy,
                        copyStatistics.CurrentFileFrom + " -> " + copyStatistics.CurrentFileTo),
                        LogOptions.CopyMoveLink, LogMsgType.Error);
                }
            }
            return 0; // S_OK
        }

        public int PreDeleteItem(uint dwFlags, IShellItem psiItem)
        {
            string fileName;
            if (psiItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEEDITING, out fileName) == 0)
            {
                deleteStatistics.CurrentFile = fileName;
            }
            return 0; // S_OK
        }

        public int PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated)
        {
            if ((GlobalSettings.LogOptions & LogOptions.Delete) != 0 && hrDelete != COPYENGINE_E_USER_CANCELLED)
            {
                uint attributes;
                psiItem.GetAttributes(SFGAO.FOLDER, out attributes);
                string text = (attributes & (uint)SFGAO.FOLDER) == 0 ? Resources.MsgLogDelete : Resources.MsgLogRmDir;

                if (hrDelete == 0)
                {
                    LogMessage(string.Format(Resources.MsgLogSuccess + text, deleteStatistics.CurrentFile),
                        LogOptions.Delete, LogMsgType.Success);
                }
                else
                {
                    LogMessage(string.Format(Resources.MsgLogError + text, deleteStatistics.CurrentFile),
                        LogOptions.Delete, LogMsgType.Error);
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
            if (copyStatistics != null)
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

            return checkOperationState() ? 0 : COPYENGINE_E_USER_CANCELLED;
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

        private void LogMessage(string message, LogOptions logOptions, LogMsgType logMsgType)
        {
            if ((logOptions & GlobalSettings.LogOptions) != 0)
            {
                Log.Write(null, message, logMsgType);
            }
        }
    }

    public static class Shell32
    {
        [DllImport("shell32.dll")]
        public static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out object ppv, out IntPtr ppidlLast);

        [DllImport("shell32.dll")]
        public static extern int SHGetIDListFromObject([MarshalAs(UnmanagedType.IUnknown)] object punk, out IntPtr ppidl);

        [DllImport("shell32.dll")]
        public static extern int SHCreateItemFromIDList(IntPtr pidl, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        [DllImport("shell32.dll")]
        public static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        [DllImport("shell32.dll")]
        public static extern int SHCreateShellItemArray(IntPtr pidlParent, IShellFolder psf, uint cidl, IntPtr[] ppidl, out IShellItemArray ppsiItemArray);

        [DllImport("shell32.dll")]
        public static extern int SHCreateShellItemArrayFromIDLists(uint cidl, IntPtr[] rgpidl, out IShellItemArray ppsiItemArray);

        [DllImport("shell32.dll")]
        public static extern IntPtr ILClone(IntPtr pidl);
    }

  
} 