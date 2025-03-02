using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
/*
主要功能：
基础结构定义：
TOpenArchiveData/TOpenArchiveDataW：用于打开压缩文件的数据结构
THeaderData/THeaderDataExW：用于存储压缩文件中每个文件的头部信息
所有结构都使用了StructLayout特性确保与原始C结构对齐
委托定义：
定义了所有WCX插件函数的委托类型
包括必需函数（如OpenArchive、ReadHeader等）
包括可选函数（如PackFiles、DeleteFiles等）
包括Unicode版本的函数（以W结尾）
核心功能：
LoadModule()：加载WCX插件DLL并获取所有函数指针
UnloadModule()：卸载插件并清理资源
GetDelegate<T>()：通过函数名获取函数指针并转换为委托
主要操作方法：
OpenArchive()：打开压缩文件，支持Unicode和ANSI
ReadHeader()：读取文件头信息，自动处理字符编码转换
ProcessFile()：处理压缩文件中的单个文件
CloseArchive()：关闭压缩文件
PackFiles()：打包文件
DeleteFiles()：从压缩包中删除文件
辅助功能：
SetChangeVolProc()：设置多卷切换回调
SetProcessDataProc()：设置进度回调
CanYouHandleThisFile()：检查插件是否支持特定文件
GetPackerCaps()：获取插件功能
Unicode支持：
通过_isUnicode标志判断是否使用Unicode版本的函数
自动在ANSI和Unicode版本之间选择
处理字符编码转换
内存管理：
使用Marshal类进行非托管内存操作
使用try-finally确保正确释放非托管资源
处理字符串转换和内存分配
错误处理：
所有关键操作都有错误检查
使用返回值指示操作成功或失败
优雅处理可选函数缺失的情况
使用示例：
这个实现完全兼容原始的WCX插件格式，支持Unicode，并提供了安全的资源管理。它可以：
动态加载WCX插件
读取和写入各种压缩格式
支持多卷压缩文件
处理Unicode文件名
提供进度回调
安全地管理非托管资源
*/
namespace WinFormsApp1
{
	// 常量定义
	/*
	 * E_SUCCESS	0	Success
		E_END_ARCHIVE	10	No more files in archive
		E_NO_MEMORY	11	Not enough memory
		E_BAD_DATA	12	Data is bad
		E_BAD_ARCHIVE	13	CRC error in archive data
		E_UNKNOWN_FORMAT	14	Archive format unknown
		E_EOPEN	15	Cannot open existing file
		E_ECREATE	16	Cannot create file
		E_ECLOSE	17	Error closing file
		E_EREAD	18	Error reading from file
		E_EWRITE	19	Error writing to file
		E_SMALL_BUF	20	Buffer too small
		E_EABORTED	21	Function aborted by user
		E_NO_FILES	22	No files found
		E_TOO_MANY_FILES	23	Too many files to pack
		E_NOT_SUPPORTED	24	Function not supported
	 */
	public enum WcxResult:int
	{
		PK_OK = 0,
		PK_END_ARCHIVE = 10,
		PK_NO_MEMORY = 11,
		PK_BAD_DATA = 12,
		PK_BAD_ARCHIVE = 13,
		PK_UNKNOWN_FORMAT = 14,
		PK_EOPEN = 15,
		PK_ECREATE = 16,
		PK_ECLOSE = 17,
		PK_EREAD = 18,
		PK_EWRITE = 19,
		PK_SMALL_BUF = 20,
		PK_EABORTED = 21,
		PK_NO_FILES = 22,
		PK_TOO_MANY_FILES = 23,
		PK_NOT_SUPPORTED = 24
	}
	
	// 基础结构体定义
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct TOpenArchiveData
	{
		[MarshalAs(UnmanagedType.LPStr)]
		public string ArcName;
		public int OpenMode;
		public int OpenResult;
		[MarshalAs(UnmanagedType.LPStr)]
		public string CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TOpenArchiveDataW
	{
		[MarshalAs(UnmanagedType.LPWStr)]
		//public IntPtr ArcName;
		public string ArcName;
		public int OpenMode;
		public int OpenResult;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CmtBuf;
		//public StringBuilder CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
	}
	
    public class OpenArchiveData
    {
        private readonly IntPtr ptr;
        private TOpenArchiveData data;
        private TOpenArchiveDataW dataW;
        private bool isUnicode;

        #region Properties

        public string ArchiveName { get; private set; }
        public int Mode { get; private set; }
        public WcxResult Result { get; set; }

        #endregion Properties

        #region Constructors

        public OpenArchiveData(IntPtr ptr, bool isUnicode)
        {
            this.ptr = ptr;
            this.isUnicode = isUnicode;
            if (ptr != IntPtr.Zero)
            {
                if (isUnicode)
                {
                    dataW = (TOpenArchiveDataW)Marshal.PtrToStructure(ptr, typeof(TOpenArchiveDataW));
					ArchiveName = dataW.ArcName;// Marshal.PtrToStringUni(dataW.ArcName);
                    Mode = dataW.OpenMode;
                }
                else
                {
                    data = (TOpenArchiveData)Marshal.PtrToStructure(ptr, typeof(TOpenArchiveData));
                    ArchiveName = data.ArcName;
                    Mode = data.OpenMode;
                }
            }
        }

        #endregion Constructors

        public void Update()
        {
            if (ptr != IntPtr.Zero)
            {
                if (isUnicode)
                {
                    dataW.OpenResult = (int)Result;
                    Marshal.StructureToPtr(dataW, ptr, false);
                }
                else
                {
                    data.OpenResult = (int)Result;
                    Marshal.StructureToPtr(data, ptr, false);
                }
            }
        }
    }

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct THeaderData
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string ArcName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string FileName;
		public int Flags;
		public int PackSize;
		public int UnpSize;
		public int HostOS;
		public int FileCRC;
		public int FileTime;
		public int UnpVer;
		public int Method;
		public int FileAttr;
		[MarshalAs(UnmanagedType.LPStr)]
		public string CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct THeaderDataExW
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
		public string ArcName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
		public string FileName;
		public int Flags;
		
		public uint PackSizeLow;
        public uint PackSizeHigh;
        public uint UnpSizeLow;
        public uint UnpSizeHigh;
		 
		//public ulong PackSize;
		//public ulong UnpSize;
		public int HostOS;
		public int FileCRC;
		public int FileTime;
		public int UnpVer;
		public int Method;
		public int FileAttr;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		public byte[] Reserved;
	}
	/*
	 *  typedef struct {
		   int size;
			 DWORD PluginInterfaceVersionLow;
			 DWORD PluginInterfaceVersionHi;
			 char DefaultIniName[MAX_PATH];
		   } PackDefaultParamStruct;

		* Definition of callback functions called by the DLL
		 Ask to swap disk for multi-volume archive *
			typedef int (__stdcall* tChangeVolProc) (char* ArcName, int Mode);
		 * Notify that data is processed - used for progress dialog *
		 typedef int (__stdcall* tProcessDataProc) (char* FileName, int Size);
	 */
	// 回调函数定义
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int TChangeVolProc(string arcName, int mode);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int TProcessDataProc(string arcName, int mode);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct PackDefaultParamStruct
	{
		public int size;
		public uint PluginInterfaceVersionLow;
		public uint PluginInterfaceVersionHi;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string DefaultIniName;
	}

	// 委托定义
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet=CharSet.Ansi)]
	public delegate IntPtr TOpenArchive(ref TOpenArchiveData archiveData);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet=CharSet.Unicode)]
	public delegate IntPtr TOpenArchiveW(ref TOpenArchiveDataW archiveData);
	public delegate int TReadHeader(IntPtr handle, ref THeaderData headerData);
	public delegate int TReadHeaderExW(IntPtr handle, ref THeaderDataExW headerData);
	public delegate int TProcessFile(IntPtr handle, int operation, string destPath, string destName);
	public delegate int TProcessFileW(IntPtr handle, int operation, [MarshalAs(UnmanagedType.LPWStr)] string destPath, [MarshalAs(UnmanagedType.LPWStr)] string destName);
	public delegate int TCloseArchive(IntPtr handle);
	public delegate int TPackFiles(string packedFile, string subPath, string srcPath, string addList, int flags);
	public delegate int TPackFilesW([MarshalAs(UnmanagedType.LPWStr)] string packedFile, [MarshalAs(UnmanagedType.LPWStr)] string subPath, [MarshalAs(UnmanagedType.LPWStr)] string srcPath, [MarshalAs(UnmanagedType.LPWStr)] string addList, int flags);
	public delegate int TDeleteFiles(string packedFile, string deleteList);
	public delegate int TDeleteFilesW([MarshalAs(UnmanagedType.LPWStr)] string packedFile, [MarshalAs(UnmanagedType.LPWStr)] string deleteList);
	public delegate int TGetPackerCaps();
	public delegate void TConfigurePacker(IntPtr parent, IntPtr dllInstance);
	public delegate void TSetChangeVolProc(IntPtr handle, IntPtr changeVolProc);
	public delegate void TSetChangeVolProcW(IntPtr handle, IntPtr changeVolProc);
	public delegate void TSetProcessDataProc(IntPtr handle, IntPtr processDataProc);
	public delegate void TSetProcessDataProcW(IntPtr handle, IntPtr processDataProc);
	public delegate IntPtr TStartMemPack(int options, string fileName);
	public delegate IntPtr TStartMemPackW(int options, [MarshalAs(UnmanagedType.LPWStr)] string fileName);
	public delegate int TPackToMem(IntPtr memPack, IntPtr bufIn, int inLen, ref int taken, IntPtr bufOut, int outLen, ref int written, ref int seekBy);
	public delegate int TDoneMemPack(IntPtr memPack);
	public delegate bool TCanYouHandleThisFile(string fileName);
	public delegate bool TCanYouHandleThisFileW([MarshalAs(UnmanagedType.LPWStr)] string fileName);
	public delegate void TPackSetDefaultParams(IntPtr dps);
	public delegate void TPkSetCryptCallback(IntPtr cryptProc, int cryptoNr, int flags);
	public delegate void TPkSetCryptCallbackW(IntPtr cryptProc, int cryptoNr, int flags);
	public delegate int TGetBackgroundFlags();

	public class WcxModule
	{
		// 常量定义
		public const int PK_OK = 0;
		public const int PK_WARN = 1;
		public const int PK_ERR = 2;
		public const int PK_PROC_TOTAL_SIZE = 1;
		public const int PK_PROC_SIZE = 2;

		// 函数指针
		private TOpenArchive _openArchive;
		private TOpenArchiveW _openArchiveW;
		private TReadHeader _readHeader;
		private TReadHeaderExW _readHeaderExW;
		private TProcessFile _processFile;
		private TProcessFileW _processFileW;
		private TCloseArchive _closeArchive;
		private TPackFiles _packFiles;
		private TPackFilesW _packFilesW;
		private TDeleteFiles _deleteFiles;
		private TDeleteFilesW _deleteFilesW;
		private TGetPackerCaps _getPackerCaps;
		private TConfigurePacker _configurePacker;
		private TSetChangeVolProc _setChangeVolProc;
		private TSetChangeVolProcW _setChangeVolProcW;
		private TSetProcessDataProc _setProcessDataProc;
		private TSetProcessDataProcW _setProcessDataProcW;
		private TStartMemPack _startMemPack;
		private TStartMemPackW _startMemPackW;
		private TPackToMem _packToMem;
		private TDoneMemPack _doneMemPack;
		private TCanYouHandleThisFile _canYouHandleThisFile;
		private TCanYouHandleThisFileW _canYouHandleThisFileW;
		private TPackSetDefaultParams _packSetDefaultParams;
		private TPkSetCryptCallback _pkSetCryptCallback;
		private TPkSetCryptCallbackW _pkSetCryptCallbackW;
		private TGetBackgroundFlags _getBackgroundFlags;

		private IntPtr _moduleHandle;
		private bool _isUnicode;
		private string _modulePath;
		
		public string Name { get;  set; }
		public string FilePath { get => _modulePath; set => _modulePath = value; }
		public List<string> DetectStrings = new();

		public WcxModule()
		{
			
		}
		public WcxModule(string name, string path)
		{
			Name = name;
			_modulePath = path;
		}
		~WcxModule()
		{
			UnloadModule();
		}
		// 设置进度回调示例
		private static int ProcessDataCallback(string fileName, int size)
		{
			// 更新进度显示
			return 0; // 返回0继续操作
		}

		public void SetCallbacks(IntPtr handle)
		{
			var procDelegate = new TProcessDataProc(ProcessDataCallback);
			IntPtr pProc = Marshal.GetFunctionPointerForDelegate(procDelegate);
			SetProcessDataProc(handle, pProc);

			// 需要保持委托引用防止被GC回收
			GC.KeepAlive(procDelegate);
		}
		public void SetDefaultParam()
		{
			if(_packSetDefaultParams == null)
				return;
			// 在加载插件后初始化默认参数
			var dps = new PackDefaultParamStruct
			{
				size = Marshal.SizeOf(typeof(PackDefaultParamStruct)),
				DefaultIniName = Path.Combine(Path.GetDirectoryName(_modulePath), "wcx.ini")
			};

			IntPtr pDps = Marshal.AllocHGlobal(dps.size);
			Marshal.StructureToPtr(dps, pDps, false);
			_packSetDefaultParams?.Invoke(pDps);
			Marshal.FreeHGlobal(pDps);
		}
		public bool LoadModule()
		{
			try
			{
				_moduleHandle = NativeMethods.LoadLibrary(_modulePath);
				if (_moduleHandle == IntPtr.Zero)
					return false;

				// 加载必需函数
				_openArchive = GetDelegate<TOpenArchive>("OpenArchive");
				_readHeader = GetDelegate<TReadHeader>("ReadHeader");
				_processFile = GetDelegate<TProcessFile>("ProcessFile");
				_closeArchive = GetDelegate<TCloseArchive>("CloseArchive");
				_setChangeVolProc = GetDelegate<TSetChangeVolProc>("SetChangeVolProc");
				_setProcessDataProc = GetDelegate<TSetProcessDataProc>("SetProcessDataProc");

				// 加载可选的Unicode函数
				_openArchiveW = GetDelegate<TOpenArchiveW>("OpenArchiveW");
				_readHeaderExW = GetDelegate<TReadHeaderExW>("ReadHeaderExW");
				_processFileW = GetDelegate<TProcessFileW>("ProcessFileW");
				_setChangeVolProcW = GetDelegate<TSetChangeVolProcW>("SetChangeVolProcW");
				_setProcessDataProcW = GetDelegate<TSetProcessDataProcW>("SetProcessDataProcW");

				_isUnicode = _openArchiveW != null && _readHeaderExW != null && _processFileW != null;

				// 加载其他可选函数
				_packFiles = GetDelegate<TPackFiles>("PackFiles");
				_packFilesW = GetDelegate<TPackFilesW>("PackFilesW");
				_deleteFiles = GetDelegate<TDeleteFiles>("DeleteFiles");
				_deleteFilesW = GetDelegate<TDeleteFilesW>("DeleteFilesW");
				_getPackerCaps = GetDelegate<TGetPackerCaps>("GetPackerCaps");
				_configurePacker = GetDelegate<TConfigurePacker>("ConfigurePacker");
				_startMemPack = GetDelegate<TStartMemPack>("StartMemPack");
				_startMemPackW = GetDelegate<TStartMemPackW>("StartMemPackW");
				_packToMem = GetDelegate<TPackToMem>("PackToMem");
				_doneMemPack = GetDelegate<TDoneMemPack>("DoneMemPack");
				_canYouHandleThisFile = GetDelegate<TCanYouHandleThisFile>("CanYouHandleThisFile");
				_canYouHandleThisFileW = GetDelegate<TCanYouHandleThisFileW>("CanYouHandleThisFileW");
				_packSetDefaultParams = GetDelegate<TPackSetDefaultParams>("PackSetDefaultParams");
				SetDefaultParam();
				_pkSetCryptCallback = GetDelegate<TPkSetCryptCallback>("PkSetCryptCallback");
				_pkSetCryptCallbackW = GetDelegate<TPkSetCryptCallbackW>("PkSetCryptCallbackW");
				_getBackgroundFlags = GetDelegate<TGetBackgroundFlags>("GetBackgroundFlags");

				return true;
			}
			catch
			{
				UnloadModule();
				return false;
			}
		}

		public void UnloadModule()
		{
			if (_moduleHandle != IntPtr.Zero)
			{
				NativeMethods.FreeLibrary(_moduleHandle);
				_moduleHandle = IntPtr.Zero;
			}

			// 清除所有函数指针
			_openArchive = null;
			_openArchiveW = null;
			_readHeader = null;
			_readHeaderExW = null;
			_processFile = null;
			_processFileW = null;
			_closeArchive = null;
			_packFiles = null;
			_packFilesW = null;
			_deleteFiles = null;
			_deleteFilesW = null;
			_getPackerCaps = null;
			_configurePacker = null;
			_setChangeVolProc = null;
			_setChangeVolProcW = null;
			_setProcessDataProc = null;
			_setProcessDataProcW = null;
			_startMemPack = null;
			_startMemPackW = null;
			_packToMem = null;
			_doneMemPack = null;
			_canYouHandleThisFile = null;
			_canYouHandleThisFileW = null;
			_packSetDefaultParams = null;
			_pkSetCryptCallback = null;
			_pkSetCryptCallbackW = null;
			_getBackgroundFlags = null;
		}

		private T GetDelegate<T>(string procName) where T : class
		{
			IntPtr procAddress = NativeMethods.GetProcAddress(_moduleHandle, procName);
			if (procAddress == IntPtr.Zero)
				return null;
			return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
		}

		public IntPtr OpenArchive(string archiveName, int openMode, out int openResult )
		{
			IntPtr result = IntPtr.Zero;
			openResult = (int)WcxResult.PK_UNKNOWN_FORMAT;
			if (_isUnicode && _openArchiveW != null)
			{
				var archiveDataW = new TOpenArchiveDataW
				{
					ArcName = archiveName, //Marshal.StringToHGlobalUni(archiveName),
					OpenMode = openMode,
					CmtBuf = string.Empty,
					CmtBufSize = 0
				};

				try
				{
					result =  _openArchiveW(ref archiveDataW);
					if(result == IntPtr.Zero)
						openResult = archiveDataW.OpenResult;
					else
						openResult = (int)WcxResult.PK_OK;	//success
					return result;
				}
				finally
				{
					//Marshal.FreeHGlobal(archiveDataW.ArcName);
				}
			}
			else if (_openArchive != null)
			{
				var archiveData = new TOpenArchiveData
				{
					ArcName = archiveName,// Marshal.StringToHGlobalAnsi(archiveName),
					OpenMode = openMode,
					CmtBuf = string.Empty,
					CmtBufSize = 0
				};

				try
				{
					result = _openArchive(ref archiveData);
					if(result == IntPtr.Zero)
						openResult = archiveData.OpenResult;
					else
						openResult = (int)WcxResult.PK_OK;  //success
					return result;
				}
				finally
				{
					//Marshal.FreeHGlobal(archiveData.ArcName);
				}
			}

			return IntPtr.Zero;
		}

		public bool ReadHeader(IntPtr arcHandle, out THeaderDataExW headerData)
		{
			headerData = new THeaderDataExW();

			if (_isUnicode && _readHeaderExW != null)
			{
				return _readHeaderExW(arcHandle, ref headerData) == 0;
			}
			else if (_readHeader != null)
			{
				var ansiHeader = new THeaderData();
				if (_readHeader(arcHandle, ref ansiHeader) == 0)
				{
					// 转换ANSI到Unicode
					headerData.ArcName = ansiHeader.ArcName;// Encoding.Default.GetString(ansiHeader.ArcName).TrimEnd('\0');
					headerData.FileName = ansiHeader.FileName;// Encoding.Default.GetString(ansiHeader.FileName).TrimEnd('\0');
					headerData.Flags = ansiHeader.Flags;
					headerData.PackSizeHigh = 0;
					headerData.PackSizeLow = (uint)ansiHeader.PackSize;
					headerData.UnpSizeHigh = 0;
					headerData.UnpSizeLow = (uint)ansiHeader.UnpSize;
					headerData.HostOS = ansiHeader.HostOS;
					headerData.FileCRC = ansiHeader.FileCRC;
					headerData.FileTime = ansiHeader.FileTime;
					headerData.UnpVer = ansiHeader.UnpVer;
					headerData.Method = ansiHeader.Method;
					headerData.FileAttr = ansiHeader.FileAttr;
					return true;
				}
			}

			return false;
		}

		public int ProcessFile(IntPtr arcHandle, int operation, string destPath, string destName)
		{
			if (_isUnicode && _processFileW != null)
			{
				return _processFileW(arcHandle, operation, destPath, destName);
			}
			else if (_processFile != null)
			{
				return _processFile(arcHandle, operation, destPath, destName);
			}

			return -1;
		}

		public bool CloseArchive(IntPtr arcHandle)
		{
			return _closeArchive != null && _closeArchive(arcHandle) == 0;
		}

		public int PackFiles(string packedFile, string subPath, string srcPath, string addList, int flags)
		{
			if (_isUnicode && _packFilesW != null)
			{
				return _packFilesW(packedFile, subPath, srcPath, addList, flags);
			}
			else if (_packFiles != null)
			{
				return _packFiles(packedFile, subPath, srcPath, addList, flags);
			}

			return -1;
		}

		public int DeleteFiles(string packedFile, string deleteList)
		{
			if (_isUnicode && _deleteFilesW != null)
			{
				return _deleteFilesW(packedFile, deleteList);
			}
			else if (_deleteFiles != null)
			{
				return _deleteFiles(packedFile, deleteList);
			}

			return -1;
		}

		public void SetChangeVolProc(IntPtr arcHandle, IntPtr changeVolProc)
		{
			if (_isUnicode && _setChangeVolProcW != null)
			{
				_setChangeVolProcW(arcHandle, changeVolProc);
			}
			else if (_setChangeVolProc != null)
			{
				_setChangeVolProc(arcHandle, changeVolProc);
			}
		}

		public void SetProcessDataProc(IntPtr arcHandle, IntPtr processDataProc)
		{
			if (_isUnicode && _setProcessDataProcW != null)
			{
				_setProcessDataProcW(arcHandle, processDataProc);
			}
			else if (_setProcessDataProc != null)
			{
				_setProcessDataProc(arcHandle, processDataProc);
			}
		}

		public bool CanYouHandleThisFile(string fileName)
		{
			fileName = fileName.ToUpper();
			if (_isUnicode && _canYouHandleThisFileW != null)
			{
				return _canYouHandleThisFileW(fileName);
			}
			else if (_canYouHandleThisFile != null)
			{
				return _canYouHandleThisFile(fileName);
			}

			return false;
		}

		public int GetPackerCaps()
		{
			return _getPackerCaps?.Invoke() ?? 0;
		}

		public bool IsUnicode => _isUnicode;

		private static class NativeMethods
		{
			[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern IntPtr LoadLibrary(string lpFileName);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool FreeLibrary(IntPtr hModule);

			[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
		}
	}
	
	public class WcxModuleList
	{
		public List<WcxModule> _modules = new List<WcxModule>();
		public List<string> _cfg = new List<string>();
		public Dictionary<string, WcxModule> _exts = new Dictionary<string, WcxModule>();
		public bool isConfigChanged = false;
		public WcxModuleList()
		{
			LoadConfiguration();
			//string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins\\wcx");
			//读取pluginpath目录下所有子目录的plugins
			//var subdirs = Directory.GetDirectories(pluginPath, "*", SearchOption.AllDirectories);
			//foreach (var subdir in subdirs)
			//{
			//	LoadModulesFromDirectory(subdir);
			//}
		}
		public WcxModule? FindModuleByName(string name)
		{
			return _modules.FirstOrDefault(m => m.Name.Equals(name));
		}
		public bool AddModule(WcxModule module)
		{
			if (!_modules.Any(m => m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
			{
				_modules.Add(module);
				return true;
			}
			return false;
		}
		public void LoadModulesFromDirectory(string directory)
		{
			if (!Directory.Exists(directory)) return;

			var subdirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
			foreach (var subdir in subdirs)
			{
				foreach (var file in Directory.GetFiles(subdir, "*.wcx*"))
				{
					try
					{
						var name = Path.GetFileNameWithoutExtension(file);
						var module = FindModuleByName(name);
						if (module == null)
						{
							module = new WcxModule
							{
								FilePath = file,
								Name = name
							};
							if (module.LoadModule())
							{
								if (AddModule(module))
									_exts[module.Name.ToLower()] = module;//TODO BUGFIX: HOW TO GET THE DETECTSTRING FOR WCX MODULE FILE
							}
						}
					}
					catch
					{
						// 加载失败的模块直接跳过
					}
				}
			}
		}
		public void SaveConfiguration()
		{
			if (!isConfigChanged) return;
			Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "PackerPlugins", _cfg);
			LoadConfiguration();
			isConfigChanged = false;
		}
		public void LoadConfiguration()
		{
			/* [PackerPlugins]
			lst=21,%COMMANDER_PATH%\Plugins\Wcx\DiskDir\DiskDir.wcx64
			ico=327,%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wcx64
			gif=327,%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wcx64
			vcd=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			xcd=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			7z=735,%COMMANDER_PATH%\Plugins\Wcx\Total7Zip\Total7Zip.wcx64
			7zip=735,%COMMANDER_PATH%\Plugins\Wcx\Total7Zip\Total7Zip.wcx64
			rsz=21,%COMMANDER_PATH%\Plugins\Wcx\TotalRSZ\TotalRSZ.wcx64
		 */
			_modules.Clear();
			_exts.Clear();
			_cfg = Helper.ReadSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "PackerPlugins");
			foreach (var line in _cfg)
			{
				var parts = line.Split('=');
				if (parts.Length == 2)
				{
					var detectstring = parts[0].Trim().ToLower();
					var part1 = parts[1].Trim();
					var path = part1.Split(',')[^1];
					path = path.Replace("%COMMANDER_PATH%", Constants.ZfileBinPath);
					if (File.Exists(path))
					{
						var name = Path.GetFileNameWithoutExtension(path);
						//try to find module in wcxmodulelist by name
						var module = FindModuleByName(name);
						if (module == null)
						{
							module = new WcxModule(name, path);
							if (module.LoadModule())
							{
								if (!module.DetectStrings.Contains(detectstring))
								{
									module.DetectStrings.Add(detectstring);
								}
								if(AddModule(module))
									_exts[parts[0].Trim()] = module;
							}
						}
						else
						{
							if (!module.DetectStrings.Contains(detectstring))
							{
								module.DetectStrings.Add(detectstring);
								_exts[parts[0].Trim()] = module;
							}
						}
					}
				}
			}
			//先按照配置读取插件（优先级高），然后按照目录读取插件
			LoadModulesFromDirectory(Constants.ZfileBinPath + "Plugins\\wcx\\");
		}
		public WcxModule? GetModuleByExt(string ext)
		{
			if (string.IsNullOrEmpty(ext))
				return null;

			ext = ext.ToLower();
			if (ext.StartsWith('.'))
				ext = ext.TrimStart('.');

			if (_exts.TryGetValue(ext, out var module))
				return module;

			return null;
		}
	}
}
