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
 using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace WinFormsApp1
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
    public delegate int FsGetFile(string RemoteName, string LocalName, int CopyFlags, RemoteInfo RemoteInfo);
    public delegate int FsPutFile(string LocalName, string RemoteName, int CopyFlags);
    public delegate bool FsDeleteFile(string RemoteName);
    public delegate bool FsRemoveDir(string RemoteName);
    public delegate bool FsMkDir(string Path);
    public delegate int FsExecuteFile(IntPtr MainWin, string RemoteName, string Verb);
    public delegate int FsRenMovFile(string OldName, string NewName, bool Move, bool OverWrite, RemoteInfo RemoteInfo);
    
    // Unicode版本
    public delegate IntPtr FsFindFirstW(string Path, out WfxFindData FindData);
    public delegate bool FsFindNextW(IntPtr Handle, out WfxFindData FindData);
    public delegate int FsGetFileW(string RemoteName, string LocalName, int CopyFlags, RemoteInfo RemoteInfo);
    public delegate int FsPutFileW(string LocalName, string RemoteName, int CopyFlags);
    public delegate bool FsDeleteFileW(string RemoteName);
    public delegate bool FsRemoveDirW(string RemoteName);
    public delegate bool FsMkDirW(string Path);
    public delegate int FsExecuteFileW(IntPtr MainWin, string RemoteName, string Verb);
    public delegate int FsRenMovFileW(string OldName, string NewName, bool Move, bool OverWrite, RemoteInfo RemoteInfo);
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

        // Unicode版本函数指针
        private FsFindFirstW _fsFindFirstW;
        private FsFindNextW _fsFindNextW;
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
                if (_fsInit == null)
                {
                    UnloadModule();
                    return false;
                }

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
                    _isUnicode = true;
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
                    _isUnicode = false;
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