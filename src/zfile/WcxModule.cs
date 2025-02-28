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
	// 基础结构体定义
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct TOpenArchiveData
	{
		public IntPtr ArcName;
		public int OpenMode;
		public int OpenResult;
		public IntPtr CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TOpenArchiveDataW
	{
		public IntPtr ArcName;
		public int OpenMode;
		public int OpenResult;
		public IntPtr CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct THeaderData
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
		public byte[] ArcName;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
		public byte[] FileName;
		public int Flags;
		public int PackSize;
		public int UnpSize;
		public int HostOS;
		public int FileCRC;
		public int FileTime;
		public int UnpVer;
		public int Method;
		public int FileAttr;
		public IntPtr CmtBuf;
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
		public ulong PackSize;
		public ulong UnpSize;
		public int HostOS;
		public int FileCRC;
		public int FileTime;
		public int UnpVer;
		public int Method;
		public int FileAttr;
		public IntPtr CmtBuf;
		public int CmtBufSize;
		public int CmtSize;
		public int CmtState;
		public ulong Reserved;
	}

	// 委托定义
	public delegate IntPtr TOpenArchive(ref TOpenArchiveData archiveData);
	public delegate IntPtr TOpenArchiveW(ref TOpenArchiveDataW archiveData);
	public delegate int TReadHeader(IntPtr handle, ref THeaderData headerData);
	public delegate int TReadHeaderExW(IntPtr handle, ref THeaderDataExW headerData);
	public delegate int TProcessFile(IntPtr handle, int operation, string destPath, string destName);
	public delegate int TProcessFileW(IntPtr handle, int operation, string destPath, string destName);
	public delegate int TCloseArchive(IntPtr handle);
	public delegate int TPackFiles(string packedFile, string subPath, string srcPath, string addList, int flags);
	public delegate int TPackFilesW(string packedFile, string subPath, string srcPath, string addList, int flags);
	public delegate int TDeleteFiles(string packedFile, string deleteList);
	public delegate int TDeleteFilesW(string packedFile, string deleteList);
	public delegate int TGetPackerCaps();
	public delegate void TConfigurePacker(IntPtr parent, IntPtr dllInstance);
	public delegate void TSetChangeVolProc(IntPtr handle, IntPtr changeVolProc);
	public delegate void TSetChangeVolProcW(IntPtr handle, IntPtr changeVolProc);
	public delegate void TSetProcessDataProc(IntPtr handle, IntPtr processDataProc);
	public delegate void TSetProcessDataProcW(IntPtr handle, IntPtr processDataProc);
	public delegate IntPtr TStartMemPack(int options, string fileName);
	public delegate IntPtr TStartMemPackW(int options, string fileName);
	public delegate int TPackToMem(IntPtr memPack, IntPtr bufIn, int inLen, ref int taken, IntPtr bufOut, int outLen, ref int written, ref int seekBy);
	public delegate int TDoneMemPack(IntPtr memPack);
	public delegate bool TCanYouHandleThisFile(string fileName);
	public delegate bool TCanYouHandleThisFileW(string fileName);
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

				// 加载可选的Unicode函数
				_openArchiveW = GetDelegate<TOpenArchiveW>("OpenArchiveW");
				_readHeaderExW = GetDelegate<TReadHeaderExW>("ReadHeaderExW");
				_processFileW = GetDelegate<TProcessFileW>("ProcessFileW");

				_isUnicode = _openArchiveW != null && _readHeaderExW != null && _processFileW != null;

				// 加载其他可选函数
				_packFiles = GetDelegate<TPackFiles>("PackFiles");
				_packFilesW = GetDelegate<TPackFilesW>("PackFilesW");
				_deleteFiles = GetDelegate<TDeleteFiles>("DeleteFiles");
				_deleteFilesW = GetDelegate<TDeleteFilesW>("DeleteFilesW");
				_getPackerCaps = GetDelegate<TGetPackerCaps>("GetPackerCaps");
				_configurePacker = GetDelegate<TConfigurePacker>("ConfigurePacker");
				_setChangeVolProc = GetDelegate<TSetChangeVolProc>("SetChangeVolProc");
				_setChangeVolProcW = GetDelegate<TSetChangeVolProcW>("SetChangeVolProcW");
				_setProcessDataProc = GetDelegate<TSetProcessDataProc>("SetProcessDataProc");
				_setProcessDataProcW = GetDelegate<TSetProcessDataProcW>("SetProcessDataProcW");
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

		public IntPtr OpenArchive(string archiveName, int openMode)
		{
			if (_isUnicode && _openArchiveW != null)
			{
				var archiveDataW = new TOpenArchiveDataW
				{
					ArcName = Marshal.StringToHGlobalUni(archiveName),
					OpenMode = openMode,
					CmtBuf = IntPtr.Zero,
					CmtBufSize = 0
				};

				try
				{
					return _openArchiveW(ref archiveDataW);
				}
				finally
				{
					Marshal.FreeHGlobal(archiveDataW.ArcName);
				}
			}
			else if (_openArchive != null)
			{
				var archiveData = new TOpenArchiveData
				{
					ArcName = Marshal.StringToHGlobalAnsi(archiveName),
					OpenMode = openMode,
					CmtBuf = IntPtr.Zero,
					CmtBufSize = 0
				};

				try
				{
					return _openArchive(ref archiveData);
				}
				finally
				{
					Marshal.FreeHGlobal(archiveData.ArcName);
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
					headerData.ArcName = Encoding.Default.GetString(ansiHeader.ArcName).TrimEnd('\0');
					headerData.FileName = Encoding.Default.GetString(ansiHeader.FileName).TrimEnd('\0');
					headerData.Flags = ansiHeader.Flags;
					headerData.PackSize = (ulong)ansiHeader.PackSize;
					headerData.UnpSize = (ulong)ansiHeader.UnpSize;
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
						var module = new WcxModule
						{
							FilePath = file,
							Name = Path.GetFileNameWithoutExtension(file)
						};

						if (module.LoadModule())
						{
							if (AddModule(module))
								_exts[module.Name.ToLower()] = module;//TODO BUGFIX: HOW TO GET THE DETECTSTRING FOR WCX MODULE FILE
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
		}
		public void LoadConfiguration()
		{
			/*
		 * [PackerPlugins]
			lst=21,%COMMANDER_PATH%\Plugins\Wcx\DiskDir\DiskDir.wcx64
			ico=327,%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wcx64
			gif=327,%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wcx64
			ani=327,%COMMANDER_PATH%\Plugins\Wlx\Imagine\Imagine.wcx64
			bin=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			c2d=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			ima=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			img=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			iso=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			nrg=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			svcd=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			vcd=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			xcd=192,%COMMANDER_PATH%\Plugins\Wcx\ISO\Iso.wcx64
			7z=735,%COMMANDER_PATH%\Plugins\Wcx\Total7Zip\Total7Zip.wcx64
			7zip=735,%COMMANDER_PATH%\Plugins\Wcx\Total7Zip\Total7Zip.wcx64
			rsz=21,%COMMANDER_PATH%\Plugins\Wcx\TotalRSZ\TotalRSZ.wcx64
		 */
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
