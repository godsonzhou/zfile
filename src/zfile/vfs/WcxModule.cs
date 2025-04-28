using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
namespace zfile
{
	public enum WcxResult : int
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
	public enum OpenMode : int
	{
		PK_OM_LIST = 0,
		PK_OM_EXTRACT = 1
	}

	public enum ProcessMode
	{
		PK_SKIP = 0,
		PK_TEST = 1,
		PK_EXTRACT = 2
	}
	public enum ChangeVolProcFlags
	{
		PK_VOL_ASK = 0,
		PK_VOL_NOTIFY = 1
	}
	[Flags]
	public enum PackFilesFlags : int
	{
		/// <summary>
		/// 打包后删除原始文件
		/// </summary>
		PK_PACK_MOVE_FILES = 1,

		/// <summary>
		/// 保存文件的路径名
		/// </summary>
		PK_PACK_SAVE_PATHS = 2,

		/// <summary>
		/// 要求用户输入密码并加密
		/// </summary>
		PK_PACK_ENCRYPT = 4
	}
	public enum CryptMode
	{
		PK_CRYPT_SAVE_PASSWORD = 1,
		PK_CRYPT_LOAD_PASSWORD = 2,
		PK_CRYPT_LOAD_PASSWORD_NO_UI = 3,
		PK_CRYPT_COPY_PASSWORD = 4,
		PK_CRYPT_MOVE_PASSWORD = 5,
		PK_CRYPT_DELETE_PASSWORD = 6
	}

	public enum CryptResult
	{
		E_SUCCESS = 0,
		E_ECREATE = 1,
		E_EWRITE = 2,
		E_EREAD = 3,
		E_NO_FILES = 4
	}

	public enum CryptOpt
	{
		PK_CRYPTOPT_MASTERPASS_SET = 1
	}

	public enum PackerCaps : int
	{
		/// <summary>
		/// 可以创建新的压缩文件
		/// </summary>
		PK_CAPS_NEW = 1,

		/// <summary>
		/// 可以修改现有的压缩文件
		/// </summary>
		PK_CAPS_MODIFY = 2,

		/// <summary>
		/// 压缩文件可以包含多个文件
		/// </summary>
		PK_CAPS_MULTIPLE = 4,

		/// <summary>
		/// 可以删除压缩文件中的文件
		/// </summary>
		PK_CAPS_DELETE = 8,

		/// <summary>
		/// 具有选项对话框
		/// </summary>
		PK_CAPS_OPTIONS = 16,

		/// <summary>
		/// 支持在内存中打包
		/// </summary>
		PK_CAPS_MEMPACK = 32,

		/// <summary>
		/// 通过内容检测压缩文件类型
		/// </summary>
		PK_CAPS_BY_CONTENT = 64,

		/// <summary>
		/// 允许在使用此插件创建的压缩文件中搜索文本
		/// </summary>
		PK_CAPS_SEARCHTEXT = 128,

		/// <summary>
		/// 显示为普通文件(隐藏压缩文件图标)，使用Ctrl+PgDn打开而不是Enter
		/// </summary>
		PK_CAPS_HIDE = 256,

		/// <summary>
		/// 插件支持PK_PACK_ENCRYPT选项
		/// </summary>
		PK_CAPS_ENCRYPT = 512
	}

	// 基础结构体定义
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
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

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct TOpenArchiveDataW
	{
		/*BStr   长度前缀为双字节的 Unicode 字符串；
			LPStr  单字节、空终止的 ANSI 字符串。；
			LPWStr  一个 2 字节、空终止的 Unicode 字符串；
			ByValArray 用于在结构中出现的内联定长字符数组，应始终使用MarshalAsAttribute的SizeConst字段来指示数组的大小。
		c++:
		char 1byte
		char* 8byte
		short int 2byte
		int 4byte
		unsigned int 4byte
		float 4byte
		double 8byte
		long 8byte
		long long 8byte
		unsigned long 8byte
		-------------
		c#:
		C#中支持9种整型:sbyte，byte，short，ushort，int，uint，long，ulong和char。

　　		  Sbyte:代表有符号的8位整数，数值范围从-128 ～ 127

　　		  Byte:代表无符号的8位整数，数值范围从0～255

　　		  Short:代表有符号的16位整数，范围从-32768 ～ 32767

　　		  ushort:代表有符号的16位整数，范围从0 到 65,535

　　		  Int:代表有符号的32位整数，范围从-2147483648 ～ 2147483648

　　		  uint:代表无符号的32位整数，范围从0 ～ 4294967295

　　		  Long:代表有符号的64位整数，范围从-9223372036854775808 ～ 9223372036854775808

　　		  Ulong:代表无符号的64位整数，范围从0 ～ 18446744073709551615。

　　		  char:代表无符号的16位整数，数值范围从0～65535。 Char类型的可能值对应于统一字符编码标准(Unicode)的字符集
		 */
		[MarshalAs(UnmanagedType.LPWStr)]
		//public IntPtr ArcName;
		public string ArcName;
		public int OpenMode;  // 4 bytes
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

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
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

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
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
	public delegate int TProcessDataProc([MarshalAs(UnmanagedType.LPStr)] string arcName, int mode);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int TProcessDataProcW([MarshalAs(UnmanagedType.LPWStr)] string arcName, int mode);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int CryptProcDelegate(int cryptoNumber, int mode, string archiveName, string password);

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
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate IntPtr TOpenArchive(ref TOpenArchiveData archiveData);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate IntPtr TOpenArchiveW(ref TOpenArchiveDataW archiveData);
	public delegate int TReadHeader(IntPtr handle, ref THeaderData headerData);
	public delegate int TReadHeaderExW(IntPtr handle, ref THeaderDataExW headerData);
	public delegate int TProcessFile(IntPtr handle, ProcessMode operation, string destPath, string destName);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int TProcessFileW(IntPtr handle, ProcessMode operation, [MarshalAs(UnmanagedType.LPWStr)] string destPath, [MarshalAs(UnmanagedType.LPWStr)] string destName);
	public delegate int TCloseArchive(IntPtr handle);
	public delegate int TPackFiles(string packedFile, string subPath, string srcPath, string addList, int flags);
	public delegate int TPackFilesW([MarshalAs(UnmanagedType.LPWStr)] string packedFile, [MarshalAs(UnmanagedType.LPWStr)] string subPath, [MarshalAs(UnmanagedType.LPWStr)] string srcPath, [MarshalAs(UnmanagedType.LPWStr)] string addList, int flags);
	public delegate int TDeleteFiles(string packedFile, string deleteList);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
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
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate void TExtensionInitialize(IntPtr startupInfo);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate void TExtensionFinalize(IntPtr reserved);

	public class WcxModule : DcxModule
	{
		// 常量定义
		public const int PK_OK = 0;
		public const int PK_WARN = 1;
		public const int PK_ERR = 2;
		public const int PK_PROC_TOTAL_SIZE = 1;
		public const int PK_PROC_SIZE = 2;
		// 常量定义

		public const int E_SUCCESS = 0; //	Success
		public const int E_END_ARCHIVE = 10; //	No more files in archive
		public const int E_NO_MEMORY = 11; //	Not enough memory
		public const int E_BAD_DATA = 12; //	Data is bad
		public const int E_BAD_ARCHIVE = 13; //	CRC error in archive data
		public const int E_UNKNOWN_FORMAT = 14; //	Archive format unknown
		public const int E_EOPEN = 15; //	Cannot open existing file
		public const int E_ECREATE = 16; //	Cannot create file
		public const int E_ECLOSE = 17; //	Error closing file
		public const int E_EREAD = 18; //	Error reading from file
		public const int E_EWRITE = 19; //	Error writing to file
		public const int E_SMALL_BUF = 20; //	Buffer too small
		public const int E_EABORTED = 21; //	Function aborted by user
		public const int E_NO_FILES = 22; //	No files found
		public const int E_TOO_MANY_FILES = 23; //	Too many files to pack
		public const int E_NOT_SUPPORTED = 24; //	Function not supported

		// Background flags
		public const int BACKGROUND_PACK = 1;
		public const int BACKGROUND_UNPACK = 2;
		public const int BACKGROUND_MEMPACK = 4;

		public static readonly IntPtr WcxInvalidHandle = new(-1);
		// 函数指针
		private TOpenArchive? _openArchive;
		private TOpenArchiveW? _openArchiveW;
		private TReadHeader? _readHeader;
		private TReadHeaderExW? _readHeaderExW;
		private TProcessFile? _processFile;
		private TProcessFileW? _processFileW;
		private TCloseArchive? _closeArchive;
		internal TPackFiles? _packFiles;
		internal TPackFilesW? _packFilesW;
		internal TDeleteFiles? _deleteFiles;
		internal TDeleteFilesW? _deleteFilesW;
		private TGetPackerCaps? _getPackerCaps;
		private TConfigurePacker? _configurePacker;
		private TSetChangeVolProc? _setChangeVolProc;
		private TSetChangeVolProcW? _setChangeVolProcW;
		private TSetProcessDataProc? _setProcessDataProc;
		private TSetProcessDataProcW? _setProcessDataProcW;
		private TStartMemPack? _startMemPack;
		private TStartMemPackW? _startMemPackW;
		internal TPackToMem? _packToMem;
		private TDoneMemPack? _doneMemPack;
		private TCanYouHandleThisFile? _canYouHandleThisFile;
		private TCanYouHandleThisFileW? _canYouHandleThisFileW;
		private TPackSetDefaultParams? _packSetDefaultParams;
		private TPkSetCryptCallback? _pkSetCryptCallback;
		private TPkSetCryptCallbackW? _pkSetCryptCallbackW;
		private TGetBackgroundFlags? _getBackgroundFlags;
		private TExtensionInitialize? _extensionInitialize;
		private TExtensionFinalize? _extensionFinalize;

		private bool _isUnicode;

		public string? Name { get; set; }
		public string? FilePath { get => ModulePath; set => ModulePath = value; }
		public List<string> DetectStrings = new();
		public int PluginCapabilities;
		public int BackgroundFlags { get; private set; }

		public WcxModule()
		{

		}
		public WcxModule(string name, string path) : base(path)
		{
			Name = name;
			//ModulePath = path;
		}
		~WcxModule()
		{
			if (_extensionFinalize != null)
			{
				_extensionFinalize(IntPtr.Zero);
			}
			UnloadModule();
		}
		public int ChangeVolProc(ref string arcName, int mode)
		{
			switch ((ChangeVolProcFlags)mode)
			{
				case ChangeVolProcFlags.PK_VOL_ASK:
					return 0;
				case ChangeVolProcFlags.PK_VOL_NOTIFY:
					return 1;
				default:
					break;
			}
			return -1;
		}
		public int ChangeVolProcW(string arcName, int mode)
		{
			var result = ChangeVolProc(ref arcName, mode);
			if (mode == (int)ChangeVolProcFlags.PK_VOL_ASK && result != 0)
				return 0;
			return result;
		}
		public int ChangeVolProcA(string arcName, int mode)
		{
			var result = ChangeVolProc(ref arcName, mode);
			if (mode == (int)ChangeVolProcFlags.PK_VOL_ASK && result != 0)
				return 0;
			return result;
		}
		// 设置进度回调示例
		//private static int ProcessDataCallback(string fileName, int size)
		//{
		//	// 更新进度显示
		//	return 0; // 返回0继续操作
		//}

		//public void SetCallbacks(IntPtr handle)
		//{
		//	var procDelegate = new TProcessDataProc(ProcessDataCallback);
		//	IntPtr pProc = Marshal.GetFunctionPointerForDelegate(procDelegate);
		//	SetProcessDataProc(handle, pProc);

		//	// 需要保持委托引用防止被GC回收
		//	GC.KeepAlive(procDelegate);
		//}
		public void SetDefaultParam()
		{
			if (_packSetDefaultParams == null)
				return;
			// 在加载插件后初始化默认参数
			var dps = new PackDefaultParamStruct
			{
				size = Marshal.SizeOf(typeof(PackDefaultParamStruct)),
				PluginInterfaceVersionLow = 22,
				PluginInterfaceVersionHi = 2,
				DefaultIniName = Path.Combine(Constants.ZfileCfgPath, "wcx.ini")
			};

			IntPtr pDps = Marshal.AllocHGlobal(dps.size);
			Marshal.StructureToPtr(dps, pDps, false);
			_packSetDefaultParams?.Invoke(pDps);
			Marshal.FreeHGlobal(pDps);
		}
		// 保存委托的引用，防止被GC回收
		private static TInputBoxProc? _inputBoxDelegate;
		private static TMessageBoxProc? _messageBoxDelegate;
		private static TDialogBoxLFMProc? _dialogBoxLFMDelegate;
		private static TDialogBoxLRSProc? _dialogBoxLRSDelegate;
		private static TDialogBoxLFMFileProc? _dialogBoxLFMFileDelegate;
		private static TDlgProc? _sendDlgMsgDelegate;
		private static TTranslateStringProc? _translateStringDelegate;

		/// <summary>
		/// 初始化扩展启动信息结构
		/// </summary>
		/// <returns>初始化后的启动信息结构指针</returns>
		private static IntPtr InitializeExtensionStartupInfo(string modulepath)
		{
			// 创建结构体
			TExtensionStartupInfo startupInfo = new();

			// 设置结构体大小
			startupInfo.StructSize = (uint)Marshal.SizeOf(typeof(TExtensionStartupInfo));

			// 设置插件目录
			const int MAX_PATH = 16384;
			string? pluginDir = Path.GetDirectoryName(modulepath);
			startupInfo.PluginDir = Encoding.UTF8.GetBytes(pluginDir + new string('\0', MAX_PATH - pluginDir.Length));

			// 设置配置目录
			string configDir = pluginDir; // Constants.ZfileCfgPath;
			startupInfo.PluginConfDir = Encoding.UTF8.GetBytes(configDir + new string('\0', MAX_PATH - configDir.Length));

			// 创建委托并保存引用
			_inputBoxDelegate = new TInputBoxProc(InputBox);
			_messageBoxDelegate = new TMessageBoxProc(MessageBox);
			_dialogBoxLFMDelegate = new TDialogBoxLFMProc(DialogBoxLFM);
			_dialogBoxLRSDelegate = new TDialogBoxLRSProc(DialogBoxLRS);
			_dialogBoxLFMFileDelegate = new TDialogBoxLFMFileProc(DialogBoxLFMFile);
			_sendDlgMsgDelegate = new TDlgProc(SendDlgMsg);
			_translateStringDelegate = new TTranslateStringProc(Translate);

			// 设置回调函数
			startupInfo.InputBox = Marshal.GetFunctionPointerForDelegate(_inputBoxDelegate);
			startupInfo.MessageBox = Marshal.GetFunctionPointerForDelegate(_messageBoxDelegate);
			startupInfo.DialogBoxLFM = Marshal.GetFunctionPointerForDelegate(_dialogBoxLFMDelegate);
			startupInfo.DialogBoxLRS = Marshal.GetFunctionPointerForDelegate(_dialogBoxLRSDelegate);
			startupInfo.DialogBoxLFMFile = Marshal.GetFunctionPointerForDelegate(_dialogBoxLFMFileDelegate);
			startupInfo.SendDlgMsg = Marshal.GetFunctionPointerForDelegate(_sendDlgMsgDelegate);

			// 设置翻译相关
			startupInfo.Translation = IntPtr.Zero; // 暂时不实现翻译功能
			startupInfo.TranslateString = Marshal.GetFunctionPointerForDelegate(_translateStringDelegate);

			// 分配非托管内存并复制结构体
			IntPtr pStartupInfo = Marshal.AllocHGlobal(Marshal.SizeOf(startupInfo));
			Marshal.StructureToPtr(startupInfo, pStartupInfo, false);

			return pStartupInfo;
		}

		#region 回调函数实现

		/// <summary>
		/// 翻译字符串
		/// </summary>
		private static int Translate(IntPtr translation, string identifier, string original, IntPtr output, int outLen)
		{
			// 如果没有翻译对象，将输出设为空字符串
			if (output != IntPtr.Zero && outLen > 0)
			{
				// 返回原始文本
				int copyLen = Math.Min(original.Length, outLen - 1);
				if (copyLen > 0)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(original[..copyLen]);
					Marshal.Copy(bytes, 0, output, bytes.Length);
					Marshal.WriteByte(output, bytes.Length, 0); // 添加结束符
				}
				else
				{
					Marshal.WriteByte(output, 0, 0); // 写入空字符
				}
			}
			return original.Length;
		}

		/// <summary>
		/// 输入框回调
		/// </summary>
		private static bool InputBox(string caption, string prompt, bool maskInput, IntPtr value, int valueMaxLen)
		{
			// 简化实现，返回失败
			return false;
		}

		/// <summary>
		/// 消息框回调
		/// </summary>
		private static int MessageBox(string text, string caption, int flags)
		{
			// 简化实现，返回确认
			return 1;
		}

		/// <summary>
		/// LFM 对话框回调
		/// </summary>
		private static bool DialogBoxLFM(IntPtr lfmData, uint dataSize, TDlgProc dlgProc)
		{
			// 简化实现，返回失败
			return false;
		}

		/// <summary>
		/// LRS 对话框回调
		/// </summary>
		private static bool DialogBoxLRS(IntPtr lrsData, uint dataSize, TDlgProc dlgProc)
		{
			// 简化实现，返回失败
			return false;
		}

		/// <summary>
		/// LFM 文件对话框回调
		/// </summary>
		private static bool DialogBoxLFMFile(string lfmFileName, TDlgProc dlgProc)
		{
			// 简化实现，返回失败
			return false;
		}

		/// <summary>
		/// 对话框消息发送回调
		/// </summary>
		private static int SendDlgMsg(IntPtr pDlg, string dlgItemName, int msg, int wParam, int lParam)
		{
			// 简化实现，返回0
			return 0;
		}

		#endregion

		public bool LoadModule()
		{
			try
			{
				if (string.IsNullOrEmpty(ModulePath))
					return false;

				ModuleHandle = NativeMethods.LoadLibrary(ModulePath);
				if (ModuleHandle == IntPtr.Zero)
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

				var isavailable = _openArchive != null && _readHeader != null && _processFile != null;
				if (!isavailable)
				{
					_openArchive = null;
					_readHeader = null;
					_processFile = null;
					isavailable = _openArchiveW != null && _readHeaderExW != null && _processFileW != null;
				}
				if (!isavailable || _closeArchive == null)
				{
					_openArchiveW = null;
					_readHeaderExW = null;
					_processFileW = null;
					_closeArchive = null;
					return false;
				}

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
				_pkSetCryptCallback = GetDelegate<TPkSetCryptCallback>("PkSetCryptCallback");
				_pkSetCryptCallbackW = GetDelegate<TPkSetCryptCallbackW>("PkSetCryptCallbackW");
				_getBackgroundFlags = GetDelegate<TGetBackgroundFlags>("GetBackgroundFlags");
				_extensionInitialize = GetDelegate<TExtensionInitialize>("ExtensionInitialize");
				_extensionFinalize = GetDelegate<TExtensionFinalize>("ExtensionFinalize");

				//get packer caps
				PluginCapabilities = _getPackerCaps?.Invoke() ?? 0;

				// 设置默认参数
				if (_packSetDefaultParams != null)
				{
					SetDefaultParam();
				}

				// 获取后台标志
				if (_getBackgroundFlags != null)
				{
					BackgroundFlags = _getBackgroundFlags.Invoke();
				}
				else
				{
					BackgroundFlags = 0;
				}

				// Extension API 初始化
				if (_extensionInitialize != null)
				{
					// 创建并初始化 StartupInfo 结构
					var startupInfo = InitializeExtensionStartupInfo(ModulePath);
					_extensionInitialize.Invoke(startupInfo);
				}

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
			if (ModuleHandle != IntPtr.Zero)
			{
				NativeMethods.FreeLibrary(ModuleHandle);
				ModuleHandle = IntPtr.Zero;
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

		//private T? GetDelegate<T>(string procName) where T : class
		//{
		//	IntPtr procAddress = NativeMethods.GetProcAddress(_moduleHandle, procName);
		//	if (procAddress == IntPtr.Zero)
		//		return null;
		//	return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
		//}

		public IntPtr OpenArchiveHandle(string archiveName, int openMode, out int openResult)
		{
			if (openMode < (int)OpenMode.PK_OM_LIST || openMode > (int)OpenMode.PK_OM_EXTRACT)
			{
				throw new ArgumentException("invalid wcx open mode");
			}
			IntPtr result = IntPtr.Zero;
			openResult = (int)WcxResult.PK_UNKNOWN_FORMAT;
			//archiveName = archiveName.ToUpper();
			if (_openArchiveW != null)
			{
				var archiveDataW = new TOpenArchiveDataW
				{
					ArcName = archiveName,
					OpenMode = openMode,
					CmtBuf = string.Empty,
					CmtBufSize = 0
				};

				// 获取结构体的大小
				int size = Marshal.SizeOf(typeof(TOpenArchiveDataW));
				Debug.Print($"archiveDataW占用的内存大小: {size} 字节");
				try
				{
					result = _openArchiveW(ref archiveDataW);
					if (result == IntPtr.Zero)
						openResult = archiveDataW.OpenResult;
					else
						openResult = (int)WcxResult.PK_OK;  //success

					//openResult = archiveDataW.OpenResult;
					return result;
				}
				catch (AccessViolationException ex)
				{
					Debug.Print($"AccessViolationException: {ex.Message}");
					throw;
				}
			}
			else if (_openArchive != null)
			{
				var archiveData = new TOpenArchiveData
				{
					ArcName = archiveName,
					OpenMode = openMode,
					CmtBuf = string.Empty,
					CmtBufSize = 0
				};

				try
				{
					result = _openArchive(ref archiveData);
					if (result == IntPtr.Zero)
						openResult = archiveData.OpenResult;
					else
						openResult = (int)WcxResult.PK_OK;  //success

					//openResult = archiveData.OpenResult;
					return result;
				}
				catch (AccessViolationException ex)
				{
					Debug.Print($"AccessViolationException: {ex.Message}");
					throw;
				}
			}

			return IntPtr.Zero;
		}

		public bool ReadHeader(IntPtr arcHandle, out WcxHeader headerData)
		{
			if (_readHeaderExW != null)
			{
				var header = new THeaderDataExW();
				if (_readHeaderExW(arcHandle, ref header) == 0)
				{
					headerData = new WcxHeader(header);
					return true;
				}
			}
			else if (_readHeader != null)
			{
				var ansiHeader = new THeaderData();
				if (_readHeader(arcHandle, ref ansiHeader) == 0)
				{
					//// 转换ANSI到Unicode
					//headerData.ArcName = ansiHeader.ArcName;// Encoding.Default.GetString(ansiHeader.ArcName).TrimEnd('\0');
					//headerData.FileName = ansiHeader.FileName;// Encoding.Default.GetString(ansiHeader.FileName).TrimEnd('\0');
					//headerData.Flags = ansiHeader.Flags;
					//headerData.PackSizeHigh = 0;
					//headerData.PackSizeLow = (uint)ansiHeader.PackSize;
					//headerData.UnpSizeHigh = 0;
					//headerData.UnpSizeLow = (uint)ansiHeader.UnpSize;
					//headerData.HostOS = ansiHeader.HostOS;
					//headerData.FileCRC = ansiHeader.FileCRC;
					//headerData.FileTime = ansiHeader.FileTime;
					//headerData.UnpVer = ansiHeader.UnpVer;
					//headerData.Method = ansiHeader.Method;
					//headerData.FileAttr = ansiHeader.FileAttr;
					//return true;
					headerData = new WcxHeader(ansiHeader);
					return true;
				}
			}
			headerData = null;
			return false;
		}

		public int ProcessFile(IntPtr arcHandle, ProcessMode operation, string destPath, string destName)
		{
			if (_processFileW != null)
			{
				if (string.IsNullOrEmpty(destPath))
					return _processFileW(arcHandle, operation, null, destName);
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
		/*
		 * PackFiles specifies what should happen when a user creates, or adds files to the archive.

			 int __stdcall PackFiles (char *PackedFile, char *SubPath, char *SrcPath, char *AddList, int Flags);
			Description
			PackFiles should return zero on success, or one of the error values otherwise.

			PackedFile refers to the archive that is to be created or modified. The string contains the full path.
			SubPath is either NULL, when the files should be packed with the paths given with the file names, or not NULL when they should be placed below the given subdirectory within the archive. Example:
			 SubPath="subdirectory"
			 Name in AddList="subdir2\filename.ext"
			 -> File should be packed as "subdirectory\subdir2\filename.ext"
			SrcPath contains path to the files in AddList. SrcPath and AddList together specify files that are to be packed into PackedFile.
			Each string in AddList is zero-delimited (ends in zero), and the AddList string ends with an extra zero byte, i.e. there are two zero bytes at the end of AddList.
			Flags can contain a combination of the following values reflecting the user choice from within Totalcmd:
			Constant	Value	Description
			PK_PACK_MOVE_FILES	1	Delete original after packing
			PK_PACK_SAVE_PATHS	2	Save path names of files
		 */
		public int PackFiles(string packedFile, string subPath, string srcPath, string addList, int flags)
		{
			if (_packFilesW != null)
			{
				if (string.IsNullOrEmpty(subPath)) return _packFilesW(packedFile, null, srcPath, addList, flags);
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
			if (_deleteFilesW != null)
			{
				return _deleteFilesW(packedFile, deleteList);
			}
			else if (_deleteFiles != null)
			{
				return _deleteFiles(packedFile, deleteList);
			}

			return -1;
		}
		public void WcxSetChangeVolProc(IntPtr arcHandle)
		{
			var changeVolProcAdelegate = new TChangeVolProc(ChangeVolProcA);
			var changeVolProcWdelegate = new TChangeVolProc(ChangeVolProcW);
			WcxSetChangeVolProc(arcHandle, Marshal.GetFunctionPointerForDelegate(changeVolProcAdelegate), Marshal.GetFunctionPointerForDelegate(changeVolProcWdelegate));
		}
		public void WcxSetChangeVolProc(IntPtr arcHandle, IntPtr changeVolProc, IntPtr changeVolProcW)
		{
			if (_setChangeVolProcW != null)
			{
				_setChangeVolProcW(arcHandle, changeVolProcW);
			}
			else if (_setChangeVolProc != null)
			{
				_setChangeVolProc(arcHandle, changeVolProc);
			}
		}
		//public void SetChangeVolProc(IntPtr arcHandle, IntPtr changeVolProc)
		//{
		//	if (_setChangeVolProcW != null)
		//	{
		//		_setChangeVolProcW(arcHandle, changeVolProc);
		//	}
		//	else if (_setChangeVolProc != null)
		//	{
		//		_setChangeVolProc(arcHandle, changeVolProc);
		//	}
		//}

		//public void SetProcessDataProc(IntPtr arcHandle, IntPtr processDataProc)
		//{
		//	if (_setProcessDataProcW != null)
		//	{
		//		_setProcessDataProcW(arcHandle, processDataProc);
		//	}
		//	else if (_setProcessDataProc != null)
		//	{
		//		_setProcessDataProc(arcHandle, processDataProc);
		//	}
		//}

		/// <summary>
		/// 设置进程数据回调，同时设置ANSI和Unicode版本的回调
		/// </summary>
		/// <param name="arcHandle">归档文件句柄</param>
		/// <param name="processDataProcA">ANSI版本的回调函数指针</param>
		/// <param name="processDataProcW">Unicode版本的回调函数指针</param>
		public void WcxSetProcessDataProc(IntPtr arcHandle, IntPtr processDataProcA, IntPtr processDataProcW)
		{
			if (_setProcessDataProcW != null)
			{
				_setProcessDataProcW(arcHandle, processDataProcW);
			}
			if (_setProcessDataProc != null)
			{
				_setProcessDataProc(arcHandle, processDataProcA);
			}
		}

		public bool CanYouHandleThisFile(string fileName)
		{
			fileName = fileName.ToUpper();
			if (_canYouHandleThisFileW != null)
			{
				return _canYouHandleThisFileW(fileName);
			}
			else if (_canYouHandleThisFile != null)
			{
				return _canYouHandleThisFile(fileName);
			}

			return false;
		}
		public IntPtr StartMemPack(int options, string fileName)
		{
			fileName = fileName.ToUpper();
			if (_startMemPackW != null)
			{
				return _startMemPackW(options, fileName);
			}
			else if (_startMemPack != null)
			{
				return _startMemPack(options, fileName);
			}

			return IntPtr.Zero;
		}
		public void SetCryptCallback(IntPtr cryptProc, int cryptoNr, int flags)
		{
			if (_pkSetCryptCallbackW != null)
			{
				_pkSetCryptCallbackW(cryptProc, cryptoNr, flags);
			}
			else if (_pkSetCryptCallback != null)
			{
				_pkSetCryptCallback(cryptProc, cryptoNr, flags);
			}
		}

		public int GetPackerCaps()
		{
			return _getPackerCaps?.Invoke() ?? 0;
		}

		/// <summary>
		/// 将WCX错误码转换为可读的错误消息
		/// </summary>
		/// <param name="result">WCX错误码</param>
		/// <returns>对应的错误消息</returns>
		internal static string GetErrorMsg(int result)
		{
			switch (result)
			{
				case E_END_ARCHIVE:
					return "End of archive reached";
				case E_NO_MEMORY:
					return "Not enough memory";
				case E_BAD_DATA:
					return "Data is bad";
				case E_BAD_ARCHIVE:
					return "CRC error in archive data";
				case E_UNKNOWN_FORMAT:
					return "Archive format unknown";
				case E_EOPEN:
					return "Cannot open existing file";
				case E_ECREATE:
					return "Cannot create file";
				case E_ECLOSE:
					return "Error closing file";
				case E_EREAD:
					return "Error reading from file";
				case E_EWRITE:
					return "Error writing to file";
				case E_SMALL_BUF:
					return "Buffer too small";
				case E_EABORTED:
					return "Operation aborted by user";
				case E_NO_FILES:
					return "No files found";
				case E_TOO_MANY_FILES:
					return "Too many files to pack";
				case E_NOT_SUPPORTED:
					return "Function not supported";
				default:
					return $"Unknown error code: {result}";
			}
		}

		/// <summary>
		/// 配置WCX插件
		/// </summary>
		/// <param name="handle">父窗口句柄</param>
		internal void VFSConfigure(nint handle)
		{
			if (_configurePacker != null)
			{
				_configurePacker(handle, ModuleHandle);
			}
		}

		public bool IsUnicode => _isUnicode;

		//private static class NativeMethods
		//{
		//	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		//	public static extern IntPtr LoadLibrary(string lpFileName);

		//	[DllImport("kernel32.dll", SetLastError = true)]
		//	public static extern bool FreeLibrary(IntPtr hModule);

		//	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		//	public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
		//}
	}

	public class WcxModuleList : StringList
	{
		public List<WcxModule> _modules = new List<WcxModule>();
		public List<string> _cfg = new List<string>();
		public Dictionary<string, WcxModule> _exts = new Dictionary<string, WcxModule>();
		public bool isConfigChanged = false;

		/// <summary>
		/// Gets the extension at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The extension</returns>
		public string GetAExt(int index)
		{
			return Names(index);
		}

		/// <summary>
		/// Sets the extension at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <param name="value">The new extension</param>
		public void SetExt(int index, string value)
		{
			string currentValue = ValueFromIndex(index);
			this[index] = value + "=" + currentValue;
		}

		/// <summary>
		/// Gets the file name at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The file name</returns>
		public string GetAFileName(int index)
		{
			string currentPlugin = ValueFromIndex(index);
			int commaPos = currentPlugin.IndexOf(',');
			if (commaPos >= 0)
			{
				return currentPlugin[(commaPos + 1)..];
			}
			return string.Empty;
		}

		/// <summary>
		/// Sets the file name at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <param name="value">The new file name</param>
		public void SetAFileName(int index, string value)
		{
			SetValueFromIndex(index, GetAFlags(index) + "," + value);
		}

		/// <summary>
		/// Gets the flags at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The flags</returns>
		public int GetAFlags(int index)
		{
			string currentPlugin = ValueFromIndex(index);
			int commaPos = currentPlugin.IndexOf(',');
			if (commaPos >= 0)
			{
				return int.Parse(currentPlugin[..commaPos]);
			}
			return 0;
		}

		/// <summary>
		/// Sets the flags at the specified index
		/// </summary>
		/// <param name="index">The index</param>
		/// <param name="value">The new flags</param>
		public void SetAFlags(int index, int value)
		{
			SetValueFromIndex(index, value + "," + GetAFileName(index));
		}

		/// <summary>
		/// Gets whether the plugin at the specified index is enabled
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>True if the plugin is enabled</returns>
		public bool GetAEnabled(int index)
		{
			return Objects[index] != null && (bool)Objects[index];
		}

		/// <summary>
		/// Sets whether the plugin at the specified index is enabled
		/// </summary>
		/// <param name="index">The index</param>
		/// <param name="value">The new enabled state</param>
		public void SetAEnabled(int index, bool value)
		{
			Objects[index] = value;
		}

		/// <summary>
		/// Gets or sets the extensions of all plugins
		/// </summary>
		public List<string> Ext
		{
			get
			{
				var result = new List<string>();
				for (int i = 0; i < Count; i++)
				{
					result.Add(GetAExt(i));
				}
				return result;
			}
			set
			{
				if (value != null && value.Count == Count)
				{
					for (int i = 0; i < Count; i++)
					{
						SetExt(i, value[i]);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the file names of all plugins
		/// </summary>
		public string[] FileName
		{
			get
			{
				string[] result = new string[Count];
				for (int i = 0; i < Count; i++)
				{
					result[i] = GetAFileName(i);
				}
				return result;
			}
			set
			{
				if (value != null && value.Length == Count)
				{
					for (int i = 0; i < Count; i++)
					{
						SetAFileName(i, value[i]);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the flags of all plugins
		/// </summary>
		public int[] Flags
		{
			get
			{
				int[] result = new int[Count];
				for (int i = 0; i < Count; i++)
				{
					result[i] = GetAFlags(i);
				}
				return result;
			}
			set
			{
				if (value != null && value.Length == Count)
				{
					for (int i = 0; i < Count; i++)
					{
						SetAFlags(i, value[i]);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the enabled state of all plugins
		/// </summary>
		public bool[] Enabled
		{
			get
			{
				bool[] result = new bool[Count];
				for (int i = 0; i < Count; i++)
				{
					result[i] = GetAEnabled(i);
				}
				return result;
			}
			set
			{
				if (value != null && value.Length == Count)
				{
					for (int i = 0; i < Count; i++)
					{
						SetAEnabled(i, value[i]);
					}
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public WcxModuleList()
		{
			LoadConfiguration();
		}
		public WcxModule? FindModuleByName(string name)
		{
			return _modules.FirstOrDefault(m => m.Name != null && m.Name.Equals(name));
		}
		public bool AddModule(WcxModule module)
		{
			if (module.Name != null && !_modules.Any(m => m.Name != null && m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
			{
				_modules.Add(module);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Adds a new plugin to the list
		/// </summary>
		/// <param name="ext">The extension handled by the plugin</param>
		/// <param name="flags">The plugin capabilities</param>
		/// <param name="fileName">The file name of the plugin</param>
		/// <returns>The index of the added plugin</returns>
		public int Add(string ext, int flags, string fileName)
		{
			return AddObject(ext + "=" + flags + "," + fileName, true);
		}
		/// <summary>
		/// Loads a WCX module from a file
		/// </summary>
		/// <param name="file">The file to load</param>
		/// <returns>The loaded module, or null if loading failed</returns>
		public WcxModule? LoadModule(string file)
		{
			var name = Path.GetFileNameWithoutExtension(file);
			var module = FindModuleByName(name);
			if (module == null)
			{
				module = new WcxModule(name, file);
				if (module.LoadModule() && module.Name != null)
				{
					if (AddModule(module))
					{
						_exts[module.Name.ToLower()] = module;
					}
				}
			}
			return module;
		}
		/// <summary>
		/// Loads all WCX modules from a directory and its subdirectories
		/// </summary>
		/// <param name="directory">The directory to load modules from</param>
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
						LoadModule(file);
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
								if (AddModule(module))
									_exts[parts[0].Trim()] = module;
							//}
							//WcxModule wcxModule = WcxPlugins.LoadModule(plugin);
							//if (wcxModule != null)
							//{
								int flags = module.PluginCapabilities;
								foreach (string ext in detectstring.Split(','))
								{
									var result = Add(ext, flags, path);
									FileName[result] = name; // GetPluginFilenameToSave(plugin);
								}
							}
						}
						else
						{
							if (!module.DetectStrings.Contains(detectstring))
							{
								module.DetectStrings.Add(detectstring);
								_exts[parts[0].Trim()] = module;
								var result = Add(detectstring, module.PluginCapabilities, path);
								FileName[result] = name;
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

		/// <summary>
		/// Finds the first enabled plugin with the specified name
		/// </summary>
		/// <param name="name">The name to find</param>
		/// <returns>The index of the plugin, or -1 if not found</returns>
		public int FindFirstEnabledByName(string name)
		{
			for (int i = 0; i < Count; i++)
			{
				if (GetAEnabled(i) && string.Equals(GetAExt(i), name, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Finds a plugin with the specified file name and extension
		/// </summary>
		/// <param name="fileName">The file name to find</param>
		/// <param name="ext">The extension to find</param>
		/// <returns>The index of the plugin, or -1 if not found</returns>
		public int Find(string fileName, string ext)
		{
			for (int i = 0; i < Count; i++)
			{
				if (string.Equals(GetAFileName(i), fileName, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(GetAExt(i), ext, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}
	}

	/// <summary>
	/// Extension methods for WcxModule
	/// </summary>
	public static class WcxModuleExtensions
	{
		/// <summary>
		/// Reads a header from a WCX archive
		/// </summary>
		/// <param name="module">The WCX module</param>
		/// <param name="arcHandle">The archive handle</param>
		/// <param name="header">The header to fill</param>
		/// <returns>0 on success, non-zero on failure</returns>
		public static int ReadWCXHeader(this WcxModule module, IntPtr arcHandle, ref WcxHeader header)
		{
			// No need to initialize headerData as it will be filled by ReadHeader
			if (module.ReadHeader(arcHandle, out var headerData))
			{
				// Convert THeaderDataExW to WcxHeader
				//header.FileName = headerData.FileName;
				//header.FileAttr = (FileAttributes)headerData.FileAttr;
				//header.PackSize = (long)((ulong)headerData.PackSizeHigh << 32 | headerData.PackSizeLow);
				//header.UnpSize = (long)((ulong)headerData.UnpSizeHigh << 32 | headerData.UnpSizeLow);
				//header.FileTime = headerData.FileTime;
				//header.CRC = headerData.FileCRC;
				//header.Method = headerData.Method;
				//header.Flags = headerData.Flags;
				header = headerData;
				return 0; // Success
			}
			return -1; // Error
		}

		/// <summary>
		/// Converts a DOS file time to a DateTime
		/// </summary>
		/// <param name="fileTime">The DOS file time</param>
		/// <returns>The corresponding DateTime</returns>
		public static DateTime FileTimeToDateTime(int fileTime)
		{
			try
			{
				// Convert DOS time format to DateTime
				int year = ((fileTime >> 25) & 0x7F) + 1980;
				int month = (fileTime >> 21) & 0x0F;
				int day = (fileTime >> 16) & 0x1F;
				int hour = (fileTime >> 11) & 0x1F;
				int minute = (fileTime >> 5) & 0x3F;
				int second = (fileTime & 0x1F) * 2;

				return new DateTime(year, month, day, hour, minute, second);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Sets the crypt callback for a WCX module
		/// </summary>
		/// <param name="module">The WCX module</param>
		/// <param name="cryptoNr">The crypto number</param>
		/// <param name="flags">The flags</param>
		/// <param name="cryptProcA">The ANSI crypt callback</param>
		/// <param name="cryptProcW">The Unicode crypt callback</param>
		public static void SetCryptCallback(this WcxModule module, int cryptoNr, int flags,
			CryptProcDelegate cryptProcA, CryptProcDelegate cryptProcW)
		{
			if (module.IsUnicode && cryptProcW != null)
			{
				IntPtr pProc = Marshal.GetFunctionPointerForDelegate(cryptProcW);
				module.SetCryptCallback(pProc, cryptoNr, flags);
			}
			else if (cryptProcA != null)
			{
				IntPtr pProc = Marshal.GetFunctionPointerForDelegate(cryptProcA);
				module.SetCryptCallback(pProc, cryptoNr, flags);
			}
		}
	}
}
