/*
主要特点：
完整的Unicode支持：
优先使用Unicode版本的函数
自动回退到ANSI版本
统一的字符串处理
资源管理：
实现IDisposable接口
自动释放非托管资源
引用计数管理
错误处理：
函数调用异常捕获
加载失败保护
配置错误处理
配置管理：
XML格式配置文件
插件启用/禁用状态
路径和名称存储
扩展性：
支持添加新插件
动态加载/卸载
配置即时保存
// 创建插件管理器
using (var moduleList = new WfxModuleList("plugins.xml"))
{
   // 加载插件
   moduleList.AddModule(@"C:\Plugins\ftp.wfx");

   // 查找插件
   var ftpModule = moduleList.FindModule("ftp");
   if (ftpModule != null)
   {
	   // 使用插件功能
	   foreach (var file in ftpModule.FindFiles("/"))
	   {
		   Console.WriteLine(file.FileName);
	   }
   }
}
*/
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace zfile
{
    #region WFX常量和结构体
    public static class WfxConstants
    {
        public const int WFX_SUCCESS = 0;
        public const int WFX_ERROR = 1;
        public const int WFX_USERABORT = 2;

        // 文件属性标志
        public const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const int FILE_ATTRIBUTE_NORMAL = 0x80;

        // 进度条状态
        public const int FS_STATUS_START = 0;
        public const int FS_STATUS_END = 1;
        public const int FS_STATUS_PROGRESS = 2;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WfxFindData
    {
        public int FileAttributes;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long FileSize;
        public int Reserved0;
        public int Reserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string AlternateFileName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RemoteInfo
    {
        public long SizeLow;
        public long SizeHigh;
        public long LastWriteTime;
        public int Attr;
    }
    #endregion

    #region WFX函数委托
    // 必需的函数
    public delegate int FsInit(int PluginNr, IntPtr ProgressProc, IntPtr LogProc, IntPtr RequestProc);
    public delegate IntPtr FsFindFirst(string Path, out WfxFindData FindData);
    public delegate bool FsFindNext(IntPtr Handle, out WfxFindData FindData);
    public delegate int FsFindClose(IntPtr Handle);
    
    // 可选的函数
    public delegate void FsSetCryptCallback(IntPtr CryptProc, int CryptoNr, int Flags);
    public delegate int FsGetFile(string RemoteName, string LocalName, int CopyFlags, RemoteInfo RemoteInfo);
    public delegate int FsPutFile(string LocalName, string RemoteName, int CopyFlags);
    public delegate bool FsDeleteFile(string RemoteName);
    public delegate bool FsRemoveDir(string RemoteName);
    public delegate bool FsMkDir(string Path);
    public delegate void FsStatusInfo(string RemoteDir, int InfoStartEnd, int InfoOperation);
    public delegate void FsSetDefaultParams(IntPtr DefaultParamStruct);
    public delegate int FsExecuteFile(IntPtr MainWin, string RemoteName, string Verb);
    public delegate void FsGetDefRootName(StringBuilder DefRootName, int MaxLen);
    public delegate bool FsSetAttr(string RemoteName, int NewAttr);
    public delegate bool FsSetTime(string RemoteName, IntPtr CreationTime, IntPtr LastAccessTime, IntPtr LastWriteTime);
    public delegate int FsExtractCustomIcon(string RemoteName, int ExtractFlags, out IntPtr TheIcon);
    public delegate int FsRenMovFile(string OldName, string NewName, bool Move, bool OverWrite, RemoteInfo RemoteInfo);
    public delegate bool FsDisconnect(string DisconnectRoot);
    public delegate int FsGetPreviewBitmap(string RemoteName, int Width, int Height, IntPtr ReturnedBitmap);
    public delegate bool FsLinksToLocalFiles();
    public delegate bool FsGetLocalName(string RemoteName, int MaxLen);
    public delegate int FsGetBackgroundFlags();
    public delegate void FsContentPluginUnloading();
    
    // Unicode版本
    public delegate int FsInitW(int PluginNr, IntPtr ProgressProcW, IntPtr LogProcW, IntPtr RequestProcW);
    public delegate IntPtr FsFindFirstW(string Path, out WfxFindData FindData);
    public delegate bool FsFindNextW(IntPtr Handle, out WfxFindData FindData);
    public delegate void FsSetCryptCallbackW(IntPtr CryptProcW, int CryptoNr, int Flags);
    public delegate bool FsMkDirW(string Path);
    public delegate int FsExecuteFileW(IntPtr MainWin, string RemoteName, string Verb);
    public delegate int FsRenMovFileW(string OldName, string NewName, bool Move, bool OverWrite, RemoteInfo RemoteInfo);
    public delegate int FsGetFileW(string RemoteName, string LocalName, int CopyFlags, RemoteInfo RemoteInfo);
    public delegate int FsPutFileW(string LocalName, string RemoteName, int CopyFlags);
    public delegate bool FsDeleteFileW(string RemoteName);
    public delegate bool FsRemoveDirW(string RemoteName);
    public delegate bool FsDisconnectW(string DisconnectRoot);
    public delegate bool FsSetAttrW(string RemoteName, int NewAttr);
    public delegate bool FsSetTimeW(string RemoteName, IntPtr CreationTime, IntPtr LastAccessTime, IntPtr LastWriteTime);
    public delegate void FsStatusInfoW(string RemoteDir, int InfoStartEnd, int InfoOperation);
    public delegate int FsExtractCustomIconW(string RemoteName, int ExtractFlags, out IntPtr TheIcon);
    public delegate int FsGetPreviewBitmapW(string RemoteName, int Width, int Height, IntPtr ReturnedBitmap);
    public delegate bool FsGetLocalNameW(string RemoteName, int MaxLen);
    #endregion

    public class WfxModule : IDisposable
    {
        #region 字段
        private IntPtr _moduleHandle;
        private string _modulePath;
        private string _pluginName;
        private bool _isUnicode;

        // 必需的函数指针
        private FsInit _fsInit;
        private FsFindFirst _fsFindFirst;
        private FsFindNext _fsFindNext;
        private FsFindClose _fsFindClose;

        // 可选的函数指针
        private FsSetCryptCallback _fsSetCryptCallback;
        private FsStatusInfo _fsStatusInfo;
        private FsSetDefaultParams _fsSetDefaultParams;
        private FsGetDefRootName _fsGetDefRootName;
        private FsSetAttr _fsSetAttr;
        private FsSetTime _fsSetTime;
        private FsExtractCustomIcon _fsExtractCustomIcon;
        private FsDisconnect _fsDisconnect;
        private FsLinksToLocalFiles _fsLinksToLocalFiles;
        private FsGetLocalName _fsGetLocalName;

        // Unicode版本函数指针
        private FsInitW _fsInitW;
        private FsFindFirstW _fsFindFirstW;
        private FsFindNextW _fsFindNextW;
        private FsSetCryptCallbackW _fsSetCryptCallbackW;
        private FsStatusInfoW _fsStatusInfoW;
        private FsSetAttrW _fsSetAttrW;
        private FsSetTimeW _fsSetTimeW;
        private FsExtractCustomIconW _fsExtractCustomIconW;
        private FsDisconnectW _fsDisconnectW;
        private FsGetLocalNameW _fsGetLocalNameW;
        private FsGetFileW _fsGetFileW;
        private FsPutFileW _fsPutFileW;
        private FsDeleteFileW _fsDeleteFileW;
        private FsRemoveDirW _fsRemoveDirW;
        private FsMkDirW _fsMkDirW;
        private FsExecuteFileW _fsExecuteFileW;
        private FsRenMovFileW _fsRenMovFileW;

        // ANSI版本函数指针
        private FsGetFile _fsGetFile;
        private FsPutFile _fsPutFile;
        private FsDeleteFile _fsDeleteFile;
        private FsRemoveDir _fsRemoveDir;
        private FsMkDir _fsMkDir;
        private FsExecuteFile _fsExecuteFile;
        private FsRenMovFile _fsRenMovFile;
        #endregion

        #region 属性
        public string ModulePath => _modulePath;
        public string PluginName => _pluginName;
        public bool IsLoaded => _moduleHandle != IntPtr.Zero;
        public bool IsUnicode => _isUnicode;
        #endregion

        #region 构造函数和初始化
        public WfxModule(string modulePath)
        {
            _modulePath = modulePath;
            _pluginName = Path.GetFileNameWithoutExtension(modulePath);
        }

        public bool LoadModule()
        {
            if (IsLoaded) return true;

            try
            {
                _moduleHandle = NativeLibrary.Load(_modulePath);
                if (_moduleHandle == IntPtr.Zero) return false;

                // 加载必需的函数
                _fsInit = GetFunction<FsInit>("FsInit");
				_fsInitW = GetFunction<FsInitW>("FsInitW");
				if (_fsInit == null && _fsInitW == null)
                {
                    UnloadModule();
                    return false;
                }
				_isUnicode = _fsInit == null;
				// 尝试加载Unicode版本函数
				try
                {
                    _fsFindFirstW = GetFunction<FsFindFirstW>("FsFindFirstW");
                    _fsFindNextW = GetFunction<FsFindNextW>("FsFindNextW");
                    _fsGetFileW = GetFunction<FsGetFileW>("FsGetFileW");
                    _fsPutFileW = GetFunction<FsPutFileW>("FsPutFileW");
                    _fsDeleteFileW = GetFunction<FsDeleteFileW>("FsDeleteFileW");
                    _fsRemoveDirW = GetFunction<FsRemoveDirW>("FsRemoveDirW");
                    _fsMkDirW = GetFunction<FsMkDirW>("FsMkDirW");
                    _fsExecuteFileW = GetFunction<FsExecuteFileW>("FsExecuteFileW");
                    _fsRenMovFileW = GetFunction<FsRenMovFileW>("FsRenMovFileW");
					_fsStatusInfoW = GetFunction<FsStatusInfoW>("FsStatusInfoW");
					_fsSetDefaultParams = GetFunction<FsSetDefaultParams>("FsSetDefaultParams");
					_fsGetDefRootName = GetFunction<FsGetDefRootName>("FsGetDefRootName");
					_fsSetAttrW = GetFunction<FsSetAttrW>("FsSetAttrW");
					_fsSetTimeW = GetFunction<FsSetTimeW>("FsSetTimeW");
					_fsExtractCustomIconW = GetFunction<FsExtractCustomIconW>("FsExtractCustomIconW");
					_fsDisconnectW = GetFunction<FsDisconnectW>("FsDisconnectW");
					_fsLinksToLocalFiles = GetFunction<FsLinksToLocalFiles>("FsLinksToLocalFiles");
					_fsGetLocalNameW = GetFunction<FsGetLocalNameW>("FsGetLocalNameW");


				}
                catch
                {
                    // Unicode函数加载失败，尝试加载ANSI版本
                    _fsFindFirst = GetFunction<FsFindFirst>("FsFindFirst");
                    _fsFindNext = GetFunction<FsFindNext>("FsFindNext");
                    _fsGetFile = GetFunction<FsGetFile>("FsGetFile");
                    _fsPutFile = GetFunction<FsPutFile>("FsPutFile");
                    _fsDeleteFile = GetFunction<FsDeleteFile>("FsDeleteFile");
                    _fsRemoveDir = GetFunction<FsRemoveDir>("FsRemoveDir");
                    _fsMkDir = GetFunction<FsMkDir>("FsMkDir");
                    _fsExecuteFile = GetFunction<FsExecuteFile>("FsExecuteFile");
                    _fsRenMovFile = GetFunction<FsRenMovFile>("FsRenMovFile");
					_fsStatusInfo = GetFunction<FsStatusInfo>("FsStatusInfo");
					_fsSetDefaultParams = GetFunction<FsSetDefaultParams>("FsSetDefaultParams");
					_fsGetDefRootName = GetFunction<FsGetDefRootName>("FsGetDefRootName");
					_fsSetAttr = GetFunction<FsSetAttr>("FsSetAttr");
					_fsSetTime = GetFunction<FsSetTime>("FsSetTime");
					_fsExtractCustomIcon = GetFunction<FsExtractCustomIcon>("FsExtractCustomIcon");
					_fsDisconnect = GetFunction<FsDisconnect>("FsDisconnect");
					_fsLinksToLocalFiles = GetFunction<FsLinksToLocalFiles>("FsLinksToLocalFiles");
					_fsGetLocalName = GetFunction<FsGetLocalName>("FsGetLocalName");
				}

                _fsFindClose = GetFunction<FsFindClose>("FsFindClose");

                // 初始化插件
                if (_fsInit(0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != WfxConstants.WFX_SUCCESS)
                {
                    UnloadModule();
                    return false;
                }

                return true;
            }
            catch
            {
                UnloadModule();
                return false;
            }
        }

        private T GetFunction<T>(string functionName) where T : Delegate
        {
            IntPtr procAddress = NativeLibrary.GetExport(_moduleHandle, functionName);
            return procAddress != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(procAddress) : null;
        }
        #endregion

        #region 文件操作方法
        public IEnumerable<WfxFindData> FindFiles(string path)
        {
            WfxFindData findData;
            IntPtr handle;

            if (_isUnicode)
            {
                handle = _fsFindFirstW(path, out findData);
            }
            else
            {
                handle = _fsFindFirst(path, out findData);
            }

            if (handle == IntPtr.Zero) yield break;

            try
            {
                do
                {
                    yield return findData;
                }
                while (_isUnicode ? _fsFindNextW(handle, out findData) : _fsFindNext(handle, out findData));
            }
            finally
            {
                _fsFindClose(handle);
            }
        }
		public bool SetAttr(string remoteName, int newAttr)
		{
			return _isUnicode ? _fsSetAttrW(remoteName, newAttr) : _fsSetAttr(remoteName, newAttr);
		}
		public bool SetTime(string remoteName, IntPtr creationTime, IntPtr lastAccessTime, IntPtr lastWriteTime)
		{
			return _isUnicode ? _fsSetTimeW(remoteName, creationTime, lastAccessTime, lastWriteTime) : _fsSetTime(remoteName, creationTime, lastAccessTime, lastWriteTime);
		}
		public int ExtractCustomIcon(string remoteName, int extractFlags, out IntPtr theIcon)
		{
			return _isUnicode ? _fsExtractCustomIconW(remoteName, extractFlags, out theIcon) : _fsExtractCustomIcon(remoteName, extractFlags, out theIcon);
		}
		public bool Disconnect(string disconnectRoot)
		{
			return _isUnicode ? _fsDisconnectW(disconnectRoot) : _fsDisconnect(disconnectRoot);
		}
		public bool LinksToLocalFiles()
		{
			return _isUnicode ? _fsLinksToLocalFiles() : false;
		}
		public bool GetLocalName(string remoteName, int maxLen)
		{
			return _isUnicode ? _fsGetLocalNameW(remoteName, maxLen) : false;
		}
		public void setStatusInfo(string remoteDir, int infoStartEnd, int infoOperation)
		{
			if (_isUnicode)
				_fsStatusInfoW(remoteDir, infoStartEnd, infoOperation);
			else
				_fsStatusInfo(remoteDir, infoStartEnd, infoOperation);
		}
		public void setCrypCallback(IntPtr cryptProc, int cryptoNr, int flags)
		{
			if (_isUnicode)
				_fsSetCryptCallbackW(cryptProc, cryptoNr, flags);
			else
				_fsSetCryptCallback(cryptProc, cryptoNr, flags);
		}
		public void setDefaultParams(IntPtr defaultParamStruct)
		{
			_fsSetDefaultParams(defaultParamStruct);
		}
		public void getDefRootName(StringBuilder defRootName, int maxLen)
		{
			_fsGetDefRootName(defRootName, maxLen);
		}
		public string getlocalname(string remoteName)
		{
			StringBuilder sb = new StringBuilder(260);
			if (_isUnicode)
			{
				_fsGetLocalNameW(remoteName, 260);
			}
			else
			{
				_fsGetLocalName(remoteName, 260);
			}
			return sb.ToString();
		}


		public bool DeleteFile(string remoteName)
        {
            return _isUnicode ? _fsDeleteFileW(remoteName) : _fsDeleteFile(remoteName);
        }

        public bool CreateDirectory(string path)
        {
            return _isUnicode ? _fsMkDirW(path) : _fsMkDir(path);
        }

        public bool RemoveDirectory(string remoteName)
        {
            return _isUnicode ? _fsRemoveDirW(remoteName) : _fsRemoveDir(remoteName);
        }

        public int CopyFile(string remoteName, string localName, int copyFlags, RemoteInfo remoteInfo)
        {
            return _isUnicode ? 
                _fsGetFileW(remoteName, localName, copyFlags, remoteInfo) :
                _fsGetFile(remoteName, localName, copyFlags, remoteInfo);
        }

        public int UploadFile(string localName, string remoteName, int copyFlags)
        {
            return _isUnicode ?
                _fsPutFileW(localName, remoteName, copyFlags) :
                _fsPutFile(localName, remoteName, copyFlags);
        }

        public int ExecuteFile(IntPtr mainWin, string remoteName, string verb)
        {
            return _isUnicode ?
                _fsExecuteFileW(mainWin, remoteName, verb) :
                _fsExecuteFile(mainWin, remoteName, verb);
        }

        public int MoveFile(string oldName, string newName, bool overWrite, RemoteInfo remoteInfo)
        {
            return _isUnicode ?
                _fsRenMovFileW(oldName, newName, true, overWrite, remoteInfo) :
                _fsRenMovFile(oldName, newName, true, overWrite, remoteInfo);
        }
        #endregion

        #region 资源释放
        public void UnloadModule()
        {
            if (_moduleHandle != IntPtr.Zero)
            {
                NativeLibrary.Free(_moduleHandle);
                _moduleHandle = IntPtr.Zero;
            }

            // 清除所有函数指针
            _fsInit = null;
            _fsInitW = null;
            _fsFindFirst = null;
            _fsFindNext = null;
            _fsFindClose = null;
            _fsFindFirstW = null;
            _fsFindNextW = null;
            _fsGetFile = null;
            _fsGetFileW = null;
            _fsPutFile = null;
            _fsPutFileW = null;
            _fsDeleteFile = null;
            _fsDeleteFileW = null;
            _fsRemoveDir = null;
            _fsRemoveDirW = null;
            _fsMkDir = null;
            _fsMkDirW = null;
            _fsExecuteFile = null;
            _fsExecuteFileW = null;
            _fsRenMovFile = null;
            _fsRenMovFileW = null;
            _fsSetCryptCallback = null;
            _fsSetCryptCallbackW = null;
            _fsStatusInfo = null;
            _fsStatusInfoW = null;
            _fsSetDefaultParams = null;
            _fsGetDefRootName = null;
            _fsSetAttr = null;
            _fsSetAttrW = null;
            _fsSetTime = null;
            _fsSetTimeW = null;
            _fsExtractCustomIcon = null;
            _fsExtractCustomIconW = null;
            _fsDisconnect = null;
            _fsDisconnectW = null;
            _fsLinksToLocalFiles = null;
            _fsGetLocalName = null;
            _fsGetLocalNameW = null;
        }

        public void Dispose()
        {
            UnloadModule();
            GC.SuppressFinalize(this);
        }

        ~WfxModule()
        {
            Dispose();
        }
        #endregion
    }

    public class WfxModuleList : IDisposable
    {
        private List<WfxModule> _modules = new List<WfxModule>();
        private string _configPath;

        public WfxModuleList(string configPath)
        {
            _configPath = configPath;
            LoadConfiguration();
        }

        public void LoadConfiguration()
        {
            if (!File.Exists(_configPath)) return;

            try
            {
                var doc = XDocument.Load(_configPath);
                foreach (var element in doc.Root.Elements("WfxPlugin"))
                {
                    var enabled = bool.Parse(element.Attribute("Enabled")?.Value ?? "false");
                    if (!enabled) continue;

                    var path = element.Element("Path")?.Value;
                    if (string.IsNullOrEmpty(path)) continue;

                    try
                    {
                        var module = new WfxModule(path);
                        if (module.LoadModule())
                        {
                            _modules.Add(module);
                        }
                    }
                    catch
                    {
                        // 加载失败的模块直接跳过
                    }
                }
            }
            catch
            {
                // 配置加载失败
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                var doc = new XDocument(
                    new XElement("WfxPlugins",
                        _modules.Select(m =>
                            new XElement("WfxPlugin",
                                new XAttribute("Enabled", "true"),
                                new XElement("Name", m.PluginName),
                                new XElement("Path", m.ModulePath)
                            )
                        )
                    )
                );
                doc.Save(_configPath);
            }
            catch
            {
                // 配置保存失败
            }
        }

        public void AddModule(string modulePath)
        {
            if (_modules.Any(m => m.ModulePath.Equals(modulePath, StringComparison.OrdinalIgnoreCase)))
                return;

            var module = new WfxModule(modulePath);
            if (module.LoadModule())
            {
                _modules.Add(module);
                SaveConfiguration();
            }
        }

        public void RemoveModule(string modulePath)
        {
            var module = _modules.FirstOrDefault(m => 
                m.ModulePath.Equals(modulePath, StringComparison.OrdinalIgnoreCase));
            if (module != null)
            {
                module.Dispose();
                _modules.Remove(module);
                SaveConfiguration();
            }
        }

        public WfxModule FindModule(string pluginName)
        {
            return _modules.FirstOrDefault(m => 
                m.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            foreach (var module in _modules)
            {
                module.Dispose();
            }
            _modules.Clear();
        }
    }
}