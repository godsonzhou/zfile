using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
/*
********************************************************************
*这个实现提供了：
完整的插件生命周期管理
Unicode和ANSI双重支持
灵活的文件类型检测机制
安全的资源管理
错误处理和容错机制
您可以通过以下方式使用这个实现：
// 创建插件列表管理器
var moduleList = new WlxModuleList();

// 从目录加载插件
moduleList.LoadModulesFromDirectory(@"C:\Plugins");

// 查找合适的插件处理文件
var module = moduleList.FindModuleForFile("test.txt");
if (module != null)
{
    // 使用插件预览文件
    var handle = module.CallListLoad(parentWindow, "test.txt", WlxConstants.LISTPLUGIN_SHOW);
    // ... 其他操作
}
*/
namespace zfile
{
	public class WlxConstants
	{
		public const int LISTPLUGIN_OK = 0;
		public const int LISTPLUGIN_ERROR = 1;

		// 显示标志
		public const int LISTPLUGIN_SHOW = 1;
		public const int LISTPLUGIN_HIDE = 0;

		// 搜索标志
		public const int LISTPLUGIN_SEARCH_FORWARD = 0;
		public const int LISTPLUGIN_SEARCH_BACKWARD = 1;
		public const int LISTPLUGIN_SEARCH_FIRST = 2;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct ListDefaultParamStruct
	{
		public int Size;        //in c version definition, use int , means 16bit signed, so we should use short in c# version, but in pascal definition, it is defined as long int （32 bit signed）, so we use int here
		//public short Size;	//try to use c version definition
		public uint PluginInterfaceVersionLow;
		public uint PluginInterfaceVersionHi;  //in c version definition, use DWORD , means 32bit unsigned, so we should use uint in c# version, but in pascal definition, it is defined as long int , so we use int here
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]	//bugfix: in pascal version max path is 32000, not 260
		public string DefaultIniName;
	}

	// 添加一个更安全的参数结构体，用于测试不同的版本
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct ListDefaultParamStructV2
	{
		public int Size;
		public int PluginInterfaceVersionLow;  // 使用 int 而不是 uint
		public int PluginInterfaceVersionHi;   // 使用 int 而不是 uint
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string DefaultIniName;
	}
	// 应使用结构体而非 IntPtr
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
			// 必需的函数委托定义
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		public delegate IntPtr ListLoad(IntPtr parentWin, string fileToLoad, int showFlags);
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
		public delegate IntPtr ListLoadW(IntPtr parentWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int showFlags);

		// 添加不同的调用约定版本
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate IntPtr ListLoadCdecl(IntPtr parentWin, string fileToLoad, int showFlags);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public delegate IntPtr ListLoadWCdecl(IntPtr parentWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int showFlags);
	public delegate int ListLoadNext(IntPtr parentWin, IntPtr pluginWin, string fileToLoad, int showFlags);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int ListLoadNextW(IntPtr parentWin, IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int showFlags);
	public delegate void ListCloseWindow(IntPtr pluginWin);

	public delegate void ListGetDetectString(StringBuilder detectString, int maxLen);
	public delegate int ListSearchText(IntPtr pluginWin, string searchString, int searchParameter);
	public delegate int ListSearchDialog(IntPtr pluginWin, int findNext);
	public delegate int ListSendCommand(IntPtr pluginWin, int command, int parameter);
	public delegate void ListSetDefaultParams(IntPtr dps);

	//public delegate int ListPrint(IntPtr pluginWin, string fileToPrint, string defPrinter, int printFlags, ref IntPtr margins);
	public delegate int ListPrint(IntPtr pluginWin, string fileToPrint,
	string defPrinter, int printFlags, ref RECT margins);
	// 可选的函数委托定义
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int ListPrintW(IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToPrint,
	[MarshalAs(UnmanagedType.LPWStr)] string defPrinter, int printFlags, ref RECT margins);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int ListSearchTextW(IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string searchString, int searchParameter);
	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	//public delegate int ListPrintW(IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToPrint, [MarshalAs(UnmanagedType.LPWStr)] string defPrinter, int printFlags, ref IntPtr margins);
	//public delegate int ListGetPreviewBitmap(string fileToLoad, int width, int height, IntPtr bitmapHandle);
	public delegate void ListNotificationReceived(IntPtr pluginWin, int message, IntPtr wParam, IntPtr lParam);

	public delegate int ListGetValue(int field, [MarshalAs(UnmanagedType.LPWStr)] string filePath, int unitIndex, int maxLen, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value);
	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	//public delegate int ListGetPreviewBitmapW([MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int width, int height, IntPtr bitmapHandle);
	// 修正委托定义
	public delegate IntPtr ListGetPreviewBitmap(string fileToLoad, int width, int height,
		IntPtr contentBuf, int contentBufLen);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate IntPtr ListGetPreviewBitmapW([MarshalAs(UnmanagedType.LPWStr)] string fileToLoad,
		int width, int height, IntPtr contentBuf, int contentBufLen);
	public class WlxModule : DcxModule, IDisposable
	{
		// 必需的函数指针
		private ListLoad? _listLoad;
		private ListLoadW? _listLoadW;
		private ListLoadNext? _listLoadNext;
		private ListLoadNextW? _listLoadNextW;
		private ListCloseWindow? _listCloseWindow;

		private ListGetDetectString? _listGetDetectString;
		private ListSearchText? _listSearchText;
		private ListSearchDialog? _listSearchDialog;
		private ListSendCommand? _listSendCommand;
		private ListSetDefaultParams? _listSetDefaultParams;
		private ListPrint? _listPrint;

		// 可选的函数指针
		private ListSearchTextW? _listSearchTextW;
		private ListPrintW? _listPrintW;
		private ListGetPreviewBitmap? _listGetPreviewBitmap;
		private ListGetPreviewBitmapW? _listGetPreviewBitmapW;
		private ListNotificationReceived? _listNotificationReceived;
		private ListGetValue? _listGetValue;

		public string Name { get; set; }
		public string FilePath { get; set; }
		public string DetectString { get; set; }
		public bool IsMultimedia { get; set; }
		public bool IsLoaded => ModuleHandle != IntPtr.Zero;
		public string FileName { get => FilePath; set => FilePath = value; }
		// 在类顶部添加
		private const int GWL_WNDPROC = -4;
		private static IntPtr _originalParentProc = IntPtr.Zero;
		private static IntPtr _originalPluginProc = IntPtr.Zero;
		public bool Enabled { get; set; } = true;
		// 窗口过程委托
		private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
		public IntPtr PluginWindow { get; private set; } = IntPtr.Zero;
		public bool IsDarkModeEnabled;
		public bool IsDarkModeSupported;
		private WndProcDelegate _parentWndProcDelegate;
		private WndProcDelegate _pluginWndProcDelegate;
		// 辅助方法：从窗口句柄获取.NET控件
		private Control? GetControlFromHandle(IntPtr hWnd)
		{
			// 在WinForms中，可以使用Control.FromHandle
			return Control.FromHandle(hWnd);

			/* 如果是其他UI框架（如WPF），需要不同的实现：
			HwndSource source = HwndSource.FromHwnd(hWnd);
			return source?.RootVisual as Control;
			*/
		}
		public void SetFocus()
		{
			if (PluginWindow != IntPtr.Zero)
				SetFocus(PluginWindow);
		}

		public void ResizeWindow(RECT rect)
		{
			if (PluginWindow == IntPtr.Zero) return;

			MoveWindow(PluginWindow,
				rect.Left, rect.Top,
				rect.Right - rect.Left,
				rect.Bottom - rect.Top,
				true);
		}

		// Win32 API
		[DllImport("user32.dll")]
		private static extern bool SetFocus(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeLibrary(IntPtr hModule);

		/// <summary>
		/// 检查插件的依赖项是否可用
		/// </summary>
		private bool CheckDependencies()
		{
			try
			{
				// 常见的依赖项列表
				string[] commonDependencies = {
					"msvcr120.dll", "msvcp120.dll",  // Visual C++ 2013
					"msvcr140.dll", "msvcp140.dll",  // Visual C++ 2015-2019
					"vcruntime140.dll", "vcruntime140_1.dll",  // Visual C++ 2015-2019
					"msvcr110.dll", "msvcp110.dll",  // Visual C++ 2012
					"msvcr100.dll", "msvcp100.dll",  // Visual C++ 2010
					"msvcr90.dll", "msvcp90.dll",    // Visual C++ 2008
					"msvcr80.dll", "msvcp80.dll",    // Visual C++ 2005
					"gdiplus.dll", "gdi32.dll", "user32.dll", "kernel32.dll"
				};

				foreach (var dep in commonDependencies)
				{
					IntPtr handle = LoadLibrary(dep);
					if (handle == IntPtr.Zero)
					{
						int error = Marshal.GetLastWin32Error();
						Debug.Print($"WlxModule: Dependency {dep} not available, error: {error}");
					}
					else
					{
						FreeLibrary(handle);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception checking dependencies - {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// 检查插件是否可能有兼容性问题
		/// </summary>
		private bool CheckPluginCompatibility()
		{
			try
			{
				// 检查插件文件大小，过大的插件可能有问题
				var fileInfo = new FileInfo(FilePath);
				if (fileInfo.Length > 50 * 1024 * 1024) // 50MB
				{
					Debug.Print($"WlxModule: Plugin {Name} is very large ({fileInfo.Length / 1024 / 1024}MB), may have compatibility issues");
					return false;
				}

				// 检查插件名称，某些类型的插件可能有问题
				string lowerName = Name.ToLower();
				if (lowerName.Contains("ie") || lowerName.Contains("html") || lowerName.Contains("web") || 
					lowerName.Contains("iclv") || lowerName.Contains("akfont"))
				{
					Debug.Print($"WlxModule: Plugin {Name} appears to be a potentially problematic plugin, may have compatibility issues");
					return false;
				}

				// 检查插件目录，某些目录下的插件可能有问题
				string pluginDir = Path.GetDirectoryName(FilePath)?.ToLower() ?? "";
				if (pluginDir.Contains("ie") || pluginDir.Contains("html") || pluginDir.Contains("web"))
				{
					Debug.Print($"WlxModule: Plugin {Name} is in a potentially problematic directory, may have compatibility issues");
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception checking plugin compatibility - {ex.Message}");
				return true; // 如果检查失败，默认允许加载
			}
		}

		/// <summary>
		/// 检查插件是否使用了可能有问题的方法
		/// </summary>
		private bool CheckPluginMethods()
		{
			try
			{
				// 检查插件是否导出了某些可能有问题的函数
				string[] problematicFunctions = {
					"DllMain", "DllEntryPoint", "DllRegisterServer", "DllUnregisterServer",
					"GetProcAddress", "LoadLibrary", "FreeLibrary"
				};

				foreach (var func in problematicFunctions)
				{
					IntPtr funcPtr = NativeMethods.GetProcAddress(ModuleHandle, func);
					if (funcPtr != IntPtr.Zero)
					{
						Debug.Print($"WlxModule: Plugin {Name} exports potentially problematic function: {func}");
						return false;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception checking plugin methods - {ex.Message}");
				return true;
			}
		}
		public WlxModule()
		{
			Name = string.Empty;
			FilePath = string.Empty;
			DetectString = string.Empty;
			// 初始化委托
			_parentWndProcDelegate = ParentWndProc;
			_pluginWndProcDelegate = PluginWndProc;
		}

		public override bool LoadModule()
		{
			if (IsLoaded) return true;

			try
			{
				// 检查文件是否存在
				if (!File.Exists(FilePath))
				{
					Debug.Print($"WlxModule: File not found - {FilePath}");
					return false;
				}

				// 检查文件是否可读
				try
				{
					using var stream = File.OpenRead(FilePath);
				}
				catch (Exception ex)
				{
					Debug.Print($"WlxModule: Cannot read file {FilePath} - {ex.Message}");
					return false;
				}

				// 检查依赖项
				CheckDependencies();

				// 检查插件兼容性
				if (!CheckPluginCompatibility())
				{
					Debug.Print($"WlxModule: Plugin {FilePath} failed compatibility check, skipping");
					//return false;
				}

				// 检查插件方法
				if (!CheckPluginMethods())
				{
					Debug.Print($"WlxModule: Plugin {FilePath} failed method check, skipping");
					//return false;
				}

				Debug.Print($"WlxModule: Attempting to load {FilePath}");
				
				// 尝试使用 LoadLibraryEx 进行更安全的加载
				try
				{
					// 首先尝试使用 LoadLibraryEx 从插件目录加载
					string pluginDir = Path.GetDirectoryName(FilePath);
					ModuleHandle = NativeMethods.LoadLibraryEx(FilePath, IntPtr.Zero, 
						NativeMethods.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | 
						NativeMethods.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
					
					if (ModuleHandle == IntPtr.Zero)
					{
						// 如果失败，尝试普通的 LoadLibrary
						ModuleHandle = NativeLibrary.Load(FilePath);
					}
				}
				catch (Exception ex)
				{
					Debug.Print($"WlxModule: Exception during LoadLibraryEx for {FilePath} - {ex.Message}");
					// 回退到普通的 LoadLibrary
					ModuleHandle = NativeLibrary.Load(FilePath);
				}
				
				if (ModuleHandle == IntPtr.Zero)
				{
					// 获取详细的错误信息
					int errorCode = Marshal.GetLastWin32Error();
					Debug.Print($"WlxModule: Failed to load {FilePath}, Error code: {errorCode}");
					return false;
				}

				Debug.Print($"WlxModule: Successfully loaded {FilePath}, Handle: {ModuleHandle}");

				// 加载必需的函数 - 尝试不同的调用约定
				_listLoad = GetDelegate<ListLoad>("ListLoad"); 
				_listLoadW = GetDelegate<ListLoadW>("ListLoadW"); 
				
				// 如果标准调用约定失败，尝试Cdecl调用约定
				//if (_listLoad == null)
				//{
				//	_listLoad = GetDelegate<ListLoadCdecl>("ListLoad") as ListLoad;
				//	if (_listLoad != null)
				//		Debug.Print($"WlxModule: Found ListLoad with Cdecl calling convention in {FilePath}");
				//}
				
				//if (_listLoadW == null)
				//{
				//	_listLoadW = GetDelegate<ListLoadWCdecl>("ListLoadW") as ListLoadW;
				//	if (_listLoadW != null)
				//		Debug.Print($"WlxModule: Found ListLoadW with Cdecl calling convention in {FilePath}");
				//}
				
				//if(_listLoad == null && _listLoadW == null)
				//{
				//	Debug.Print($"WlxModule: Required ListLoad function not found in {FilePath}");
				//	throw new Exception("required listload can not be found!");
				//}

				Debug.Print($"WlxModule: Required functions loaded successfully for {FilePath}");

				// 加载可选函数 // 可选函数加载失败不影响插件使用
				try
				{
					_listLoadNext = GetDelegate<ListLoadNext>("ListLoadNext"); 
					// 加载Unicode版本函数 // Unicode函数加载失败不影响插件使用
					_listLoadNextW = GetDelegate<ListLoadNextW>("ListLoadNextW"); 
					_listSearchText = GetDelegate<ListSearchText>("ListSearchText"); 
					_listSearchTextW = GetDelegate<ListSearchTextW>("ListSearchTextW"); 
					_listPrint = GetDelegate<ListPrint>("ListPrint"); 
					_listPrintW = GetDelegate<ListPrintW>("ListPrintW"); 
					_listGetPreviewBitmap = GetDelegate<ListGetPreviewBitmap>("ListGetPreviewBitmap"); 
					_listGetPreviewBitmapW = GetDelegate<ListGetPreviewBitmapW>("ListGetPreviewBitmapW"); 
					_listCloseWindow = GetDelegate<ListCloseWindow>("ListCloseWindow"); 
					_listGetDetectString = GetDelegate<ListGetDetectString>("ListGetDetectString"); 
					_listSearchDialog = GetDelegate<ListSearchDialog>("ListSearchDialog"); 
					_listSendCommand = GetDelegate<ListSendCommand>("ListSendCommand"); 
					_listNotificationReceived = GetDelegate<ListNotificationReceived>("ListNotificationReceived"); 
					_listSetDefaultParams = GetDelegate<ListSetDefaultParams>("ListSetDefaultParams");
					_listGetValue = GetDelegate<ListGetValue>("ListGetValue");
				}
				catch (Exception ex)
				{
					Debug.Print($"WlxModule: Exception while loading optional functions for {FilePath} - {ex.Message}");
					// 可选函数加载失败不应该阻止插件使用
				}

				//GC.KeepAlive(_listLoad);
				//GC.KeepAlive(_listLoadW);
				
				// 初始化插件 - 使用更安全的方式
				bool initSuccess = true;
				
				// 使用隔离的方式调用插件函数
				try
				{
					SafeCallListSetDefaultParams();
					Debug.Print($"WlxModule: ListSetDefaultParams completed for {FilePath}");
				}
				catch (Exception ex)
				{
					Debug.Print($"WlxModule: Exception in CallListSetDefaultParams for {FilePath} - {ex.Message}");
					initSuccess = false;
					// 继续执行，不要因为初始化失败而完全放弃插件
				}

				try
				{
					SafeLoadDetectString();
					Debug.Print($"WlxModule: LoadDetectString completed for {FilePath}");
				}
				catch (Exception ex)
				{
					Debug.Print($"WlxModule: Exception in LoadDetectString for {FilePath} - {ex.Message}");
					initSuccess = false;
					// 继续执行，不要因为检测字符串加载失败而完全放弃插件
				}

				if (initSuccess)
				{
					Debug.Print($"WlxModule: Successfully initialized {FilePath}");
				}
				else
				{
					Debug.Print($"WlxModule: Partially initialized {FilePath} (some optional functions failed)");
				}
				
				return true;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception while loading {FilePath} - {ex.Message}");
				Debug.Print($"WlxModule: Stack trace - {ex.StackTrace}");
				UnloadModule();
				return false;
			}
		}

		//private T? GetFunction<T>(string functionName) where T : Delegate
		//{
		//	IntPtr functionPtr = NativeLibrary.GetExport(_moduleHandle, functionName);
		//	if (functionPtr == IntPtr.Zero)
		//		return null;
		//	return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
		//}

		private void LoadDetectString()
		{
			if (_listGetDetectString == null) 
			{
				Debug.Print($"WlxModule: ListGetDetectString function not available for {FilePath}");
				return;
			}

			try
			{
				Debug.Print($"WlxModule: Calling ListGetDetectString for {FilePath}");
				StringBuilder detectStr = new StringBuilder(1024);
				_listGetDetectString(detectStr, detectStr.Capacity);
				DetectString = detectStr.ToString();
				IsMultimedia = DetectString.Contains("multimedia", StringComparison.OrdinalIgnoreCase);
				Debug.Print($"WlxModule: DetectString loaded for {FilePath}: {DetectString}");
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception in LoadDetectString for {FilePath} - {ex.Message}");
				Debug.Print($"WlxModule: Stack trace - {ex.StackTrace}");
				throw; // 重新抛出异常，让上层处理
			}
		}

		private void CallListSetDefaultParams()
		{
			if (_listSetDefaultParams == null) 
			{
				Debug.Print($"WlxModule: ListSetDefaultParams function not available for {FilePath}");
				return;
			}

			try
			{
				var inipath = $"{Path.GetDirectoryName(FilePath)}\\{Name}.ini";
				if (File.Exists(inipath))
				{
					// 如果插件目录下已经存在对应的ini文件，则设置默认参数
					Debug.Print($"WlxModule: {Name} has an ini file {inipath}, setting default params.");
				}
				else
				{
					inipath = "";
					Debug.Print($"WlxModule: {Name} no ini file found, using default config path.");
				}

				// 尝试不同的参数结构体版本
				bool success = false;
				Exception? lastException = null;

				// 尝试版本1（原始版本）
				try
				{
					success = TryCallListSetDefaultParamsV1();
					if (success)
					{
						Debug.Print($"WlxModule: ListSetDefaultParams V1 succeeded for {FilePath}");
						return;
					}
				}
				catch (Exception ex)
				{
					lastException = ex;
					Debug.Print($"WlxModule: ListSetDefaultParams V1 failed for {FilePath} - {ex.Message}");
				}

				// 尝试版本2（修改版本）
				try
				{
					success = TryCallListSetDefaultParamsV2();
					if (success)
					{
						Debug.Print($"WlxModule: ListSetDefaultParams V2 succeeded for {FilePath}");
						return;
					}
				}
				catch (Exception ex)
				{
					lastException = ex;
					Debug.Print($"WlxModule: ListSetDefaultParams V2 failed for {FilePath} - {ex.Message}");
				}

				// 如果都失败了，抛出最后一个异常
				if (lastException != null)
				{
					throw lastException;
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception in CallListSetDefaultParams for {FilePath} - {ex.Message}");
				Debug.Print($"WlxModule: Stack trace - {ex.StackTrace}");
				throw; // 重新抛出异常，让上层处理
			}
		}

		private bool TryCallListSetDefaultParamsV1()
		{
			var defaultParams = new ListDefaultParamStruct
			{
				Size = Marshal.SizeOf<ListDefaultParamStruct>(),
				PluginInterfaceVersionHi = 2,
				PluginInterfaceVersionLow = 0,
				DefaultIniName = (Constants.ZfileCfgPath + "wincmd.ini")
			};

			Debug.Print($"WlxModule: Trying V1 params structure, size: {defaultParams.Size}");
			var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(defaultParams));
			
			try
			{
				Debug.Print($"WlxModule: Copying V1 structure to unmanaged memory at {ptr}");
				Marshal.StructureToPtr(defaultParams, ptr, false);
				
				Debug.Print($"WlxModule: Calling ListSetDefaultParams V1 with ptr: {ptr}");
				_listSetDefaultParams(ptr);
				Debug.Print($"WlxModule: ListSetDefaultParams V1 call completed successfully");
				return true;
			}
			finally
			{
				Debug.Print($"WlxModule: Freeing unmanaged memory at {ptr}");
				Marshal.FreeHGlobal(ptr);
			}
		}

		private bool TryCallListSetDefaultParamsV2()
		{
			var defaultParams = new ListDefaultParamStructV2
			{
				Size = Marshal.SizeOf<ListDefaultParamStructV2>(),
				PluginInterfaceVersionHi = 2,
				PluginInterfaceVersionLow = 0,
				DefaultIniName = (Constants.ZfileCfgPath + "wincmd.ini")
			};

			Debug.Print($"WlxModule: Trying V2 params structure, size: {defaultParams.Size}");
			var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(defaultParams));
			
			try
			{
				Debug.Print($"WlxModule: Copying V2 structure to unmanaged memory at {ptr}");
				Marshal.StructureToPtr(defaultParams, ptr, false);
				
				Debug.Print($"WlxModule: Calling ListSetDefaultParams V2 with ptr: {ptr}");
				_listSetDefaultParams(ptr);
				Debug.Print($"WlxModule: ListSetDefaultParams V2 call completed successfully");
				return true;
			}
			finally
			{
				Debug.Print($"WlxModule: Freeing unmanaged memory at {ptr}");
				Marshal.FreeHGlobal(ptr);
			}
		}

		/// <summary>
		/// 安全的调用 ListSetDefaultParams
		/// </summary>
		private void SafeCallListSetDefaultParams()
		{
			if (_listSetDefaultParams == null) 
			{
				Debug.Print($"WlxModule: ListSetDefaultParams function not available for {FilePath}");
				return;
			}

			// 使用 try-catch 包装调用，防止插件崩溃影响主程序
			try
			{
				CallListSetDefaultParams();
			}
			catch (AccessViolationException ex)
			{
				Debug.Print($"WlxModule: AccessViolationException in ListSetDefaultParams for {FilePath} - {ex.Message}");
				throw;
			}
			catch (BadImageFormatException ex)
			{
				Debug.Print($"WlxModule: BadImageFormatException in ListSetDefaultParams for {FilePath} - {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception in ListSetDefaultParams for {FilePath} - {ex.Message}");
				throw;
			}
		}

		/// <summary>
		/// 安全的调用 LoadDetectString
		/// </summary>
		private void SafeLoadDetectString()
		{
			if (_listGetDetectString == null) 
			{
				Debug.Print($"WlxModule: ListGetDetectString function not available for {FilePath}");
				return;
			}

			// 使用 try-catch 包装调用，防止插件崩溃影响主程序
			try
			{
				LoadDetectString();
			}
			catch (AccessViolationException ex)
			{
				Debug.Print($"WlxModule: AccessViolationException in LoadDetectString for {FilePath} - {ex.Message}");
				throw;
			}
			catch (BadImageFormatException ex)
			{
				Debug.Print($"WlxModule: BadImageFormatException in LoadDetectString for {FilePath} - {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModule: Exception in LoadDetectString for {FilePath} - {ex.Message}");
				throw;
			}
		}
		public static byte[] StringToAnsiBytes(string str, int length)
		{
			var bytes = Encoding.Default.GetBytes(str ?? "");
			var arr = new byte[length];
			int copyLen = Math.Min(bytes.Length, length - 1); // 预留结尾0
			Array.Copy(bytes, arr, copyLen);
			arr[copyLen] = 0; // 结尾补0
			return arr;
		}
		public IntPtr CallListLoad(IntPtr parentWin, string fileToLoad, int showFlags)
		{
			try
			{
				// 添加深色模式支持
				if (IsDarkModeEnabled)
				{
					showFlags |= 0x10000000; // lcp_darkmode
					if (IsDarkModeSupported)
						showFlags |= 0x20000000; // lcp_darkmodenative
				}

				IntPtr result;
				if (_listLoadW != null)
					result = _listLoadW(parentWin, fileToLoad, showFlags);
				else
					result = _listLoad?.Invoke(parentWin, fileToLoad, showFlags) ?? IntPtr.Zero;

				if (result != IntPtr.Zero)
				{
					PluginWindow = result; // 存储插件窗口句柄

					// 子类化父窗口
					_originalParentProc = SetWindowLongPtr(
						parentWin,
						GWL_WNDPROC,
						Marshal.GetFunctionPointerForDelegate(_parentWndProcDelegate)
					);
					SetProp(parentWin, "ParentProc", _originalParentProc);

					// 子类化插件窗口
					_originalPluginProc = SetWindowLongPtr(
						result,
						GWL_WNDPROC,
						Marshal.GetFunctionPointerForDelegate(_pluginWndProcDelegate)
					);
					SetProp(result, "PluginProc", _originalPluginProc);
				}
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ListLoad error: {ex.Message}");
				return IntPtr.Zero;
			}
		}
		// 窗口过程实现
		private IntPtr ParentWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				// 调用原始窗口过程
				IntPtr originalProc = GetProp(hWnd, "ParentProc");
				IntPtr result;
				if (originalProc != IntPtr.Zero)
					result = CallWindowProc(originalProc, hWnd, msg, wParam, lParam);
				else
					result = DefWindowProc(hWnd, msg, wParam, lParam);

				if (result == IntPtr.Zero && msg == WM_COMMAND && lParam != IntPtr.Zero)
				{
					// 处理命令消息
					//TControl Lister:= TControl(GetLCLOwnerObject(hWnd));
					//	if Assigned(Lister) then Lister.Perform(Msg, wParam, lParam);
					// 获取关联的.NET控件
					// 注意：这里需要实现 GetControlFromHandle 方法
					//Control? control = GetControlFromHandle(hWnd);

					//if (control != null)
					//{
					//	// 转发消息给.NET控件
					//	Message m = Message.Create(hWnd, (int)msg, wParam, lParam);
					//	control.WndProc(ref m);
					//	result = m.Result;
					//}
					// 使用 SendMessage 代替直接调用 WndProc
					// 获取父窗口的父窗口（可能是主窗体）
					IntPtr mainWindow = GetParent(hWnd);
					if (mainWindow != IntPtr.Zero)
					{
						result = SendMessage(mainWindow, msg, wParam, lParam);
					}
				}
				return result;
			}
			catch (Exception ex)
			{  // 记录日志，防止崩溃
				Debug.WriteLine("ParentWndProc Exception: " + ex);
				return IntPtr.Zero;
			}
		}

		private IntPtr PluginWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				// 调用原始窗口过程
				IntPtr originalProc = GetProp(hWnd, "PluginProc");
				IntPtr result;
				if (originalProc != IntPtr.Zero)
					result = CallWindowProc(originalProc, hWnd, msg, wParam, lParam);
				else
					result = DefWindowProc(hWnd, msg, wParam, lParam);

				if (result == IntPtr.Zero && msg == WM_KEYDOWN)
				{
					// 处理热键（如 'n'/'p'）
					PostMessage(GetParent(hWnd), msg, wParam, lParam);
				}
				return result;
			}
			catch (Exception ex) {
				// 记录日志，防止崩溃
				Debug.WriteLine("PluginWndProc Exception: " + ex);
				return IntPtr.Zero;
			}
		}
		private const uint WM_COMMAND = 0x0111;
		private const uint WM_KEYDOWN = 0x0100;
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SetProp(IntPtr hWnd, string lpString, IntPtr hData);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetProp(IntPtr hWnd, string lpString);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr RemoveProp(IntPtr hWnd, string lpString);
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool DestroyWindow(IntPtr hWnd);

		// 所需的Win32 API
		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll")]
		private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll")]
		private static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		public int CallListLoadNext(IntPtr parentWin, IntPtr pluginWin, string fileToLoad, int showFlags)
		{
			if (_listLoadNextW != null)
				return _listLoadNextW(parentWin, pluginWin, fileToLoad, showFlags);
			return _listLoadNext != null ? _listLoadNext(parentWin, pluginWin, fileToLoad, showFlags) : WlxConstants.LISTPLUGIN_ERROR;
		}

		//public void CallListCloseWindow(IntPtr pluginWin)
		//{
		//	// 捕获异常以防止插件崩溃，比如inied.wlx插件关闭时会导致主程序退出。
		//	try
		//	{
		//		_listCloseWindow?.Invoke(pluginWin);
		//	}
		//	catch (Exception ex) {
		//		// 记录日志，防止插件异常导致主程序崩溃
		//		Debug.Print($"插件关闭异常: {ex.Message}");
		//	}   
		//}
		public void CallListCloseWindow(IntPtr pluginWin)
		{
			try
			{
				// 恢复原始窗口过程
				IntPtr parentWin = GetParent(pluginWin);
				IntPtr parentProc = GetProp(parentWin, "ParentProc");
				if (parentProc != IntPtr.Zero)
				{
					SetWindowLongPtr(parentWin, GWL_WNDPROC, parentProc);
					RemoveProp(parentWin, "ParentProc");
				}
				IntPtr pluginProc = GetProp(pluginWin, "PluginProc");
				if (pluginProc != IntPtr.Zero)
				{
					SetWindowLongPtr(pluginWin, GWL_WNDPROC, pluginProc);
					RemoveProp(pluginWin, "PluginProc");
				}
				// 关闭窗口
				if (_listCloseWindow != null)
					_listCloseWindow(pluginWin);
				else
					DestroyWindow(pluginWin);
			}
			catch(Exception ex)
			{
				// 记录日志，防止插件异常导致主程序崩溃
				Debug.Print($"插件关闭异常: {ex.Message}");
			}
			finally
			{
				PluginWindow = IntPtr.Zero;
			}
		}
		public int CallListSearchText(IntPtr pluginWin, string searchString, int searchParameter)
		{
			if (_listSearchTextW != null)
				return _listSearchTextW(pluginWin, searchString, searchParameter);
			return _listSearchText != null ? _listSearchText(pluginWin, searchString, searchParameter) : WlxConstants.LISTPLUGIN_ERROR;
		}

		public int CallListSearchDialog(IntPtr pluginWin, int findNext)
		{
			return _listSearchDialog?.Invoke(pluginWin, findNext) ?? WlxConstants.LISTPLUGIN_ERROR;
		}

		public int CallListSendCommand(IntPtr pluginWin, int command, int parameter)
		{
			return _listSendCommand?.Invoke(pluginWin, command, parameter) ?? WlxConstants.LISTPLUGIN_ERROR;
		}

		public int CallListPrint(IntPtr pluginWin, string fileToPrint, string defPrinter, int printFlags, ref RECT margins)
		{
			if (_listPrintW != null)
				return _listPrintW(pluginWin, fileToPrint, defPrinter, printFlags, ref margins);
			return _listPrint != null ? _listPrint(pluginWin, fileToPrint, defPrinter, printFlags, ref margins) : WlxConstants.LISTPLUGIN_ERROR;
		}

		//public int CallListGetPreviewBitmap(string fileToLoad, int width, int height, IntPtr bitmapHandle)
		//{
		//	if (_listGetPreviewBitmapW != null)
		//		return _listGetPreviewBitmapW(fileToLoad, width, height, bitmapHandle);
		//	return _listGetPreviewBitmap?.Invoke(fileToLoad, width, height, bitmapHandle) ?? WlxConstants.LISTPLUGIN_ERROR;
		//}
		// 修正调用方法
		public IntPtr CallListGetPreviewBitmap(string fileToLoad, int width, int height, byte[] contentBuf)
		{
			IntPtr contentPtr = Marshal.AllocHGlobal(contentBuf.Length);
			Marshal.Copy(contentBuf, 0, contentPtr, contentBuf.Length);

			try
			{
				if (_listGetPreviewBitmapW != null)
					return _listGetPreviewBitmapW(fileToLoad, width, height, contentPtr, contentBuf.Length);

				return _listGetPreviewBitmap?.Invoke(fileToLoad, width, height, contentPtr, contentBuf.Length)
					   ?? IntPtr.Zero;
			}
			finally
			{
				Marshal.FreeHGlobal(contentPtr);
			}
		}
		public void CallListNotificationReceived(IntPtr pluginWin, int message, IntPtr wParam, IntPtr lParam)
		{
			_listNotificationReceived?.Invoke(pluginWin, message, wParam, lParam);
		}

		public string CallListGetValue(int field, string filePath, int unitIndex)
		{
			if (_listGetValue == null) return string.Empty;

			StringBuilder value = new StringBuilder(1024);
			_listGetValue(field, filePath, unitIndex, value.Capacity, value);
			return value.ToString();
		}

		public override void UnloadModule()
		{
			if (ModuleHandle != IntPtr.Zero)
			{
				NativeLibrary.Free(ModuleHandle);
				ModuleHandle = IntPtr.Zero;
			}

			// 清除所有函数指针
			_listLoad = null;
			_listLoadW = null;
			_listLoadNext = null;
			_listLoadNextW = null;
			_listCloseWindow = null;

			_listGetDetectString = null;
			_listSearchText = null;
			_listSearchDialog = null;
			_listSendCommand = null;
			_listSetDefaultParams = null;

			_listPrint = null;
			_listSearchTextW = null;
			_listPrintW = null;
			_listGetPreviewBitmap = null;
			_listGetPreviewBitmapW = null;

			_listNotificationReceived = null;
			_listGetValue = null;
		}

		public override void Dispose()
		{
			UnloadModule();
			GC.SuppressFinalize(this);
		}

		~WlxModule()
		{
			Dispose();
		}
	}

	public class WlxModuleList
	{
		private List<string> _config;
		public Dictionary<string, string> _configDict;
		public List<WlxModule> _modules = [];
		private HashSet<string> _blacklistedPlugins = new(); // 黑名单插件

		public List<WlxModule> Modules { get { return _modules; } }
		public bool isConfigChanged = false;
		public bool ModuleLoaded = false;
		public Dictionary<string, string> pathdict = new();
		public WlxModuleList()
		{
			LoadConfiguration();
			LoadBlacklist();
			
			LoadModulesFromDirectory(Constants.ZfileBinPath + "\\plugins\\wlx");
		}

		/// <summary>
		/// 加载插件黑名单
		/// </summary>
		private void LoadBlacklist()
		{
			try
			{
				string blacklistFile = Path.Combine(Constants.ZfileCfgPath, "plugin_blacklist.txt");
				if (File.Exists(blacklistFile))
				{
					string[] blacklistedPlugins = File.ReadAllLines(blacklistFile);
					foreach (string plugin in blacklistedPlugins)
					{
						string trimmedPlugin = plugin.Trim();
						if (!string.IsNullOrEmpty(trimmedPlugin) && !trimmedPlugin.StartsWith("#"))
						{
							_blacklistedPlugins.Add(trimmedPlugin);
							Debug.Print($"WlxModuleList: Loaded blacklisted plugin from config: {trimmedPlugin}");
						}
					}
				}
				else
				{
					// 创建默认黑名单文件
					CreateDefaultBlacklist(blacklistFile);
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModuleList: Exception loading blacklist - {ex.Message}");
			}
		}

		/// <summary>
		/// 创建默认黑名单文件
		/// </summary>
		private void CreateDefaultBlacklist(string blacklistFile)
		{
			try
			{
				string[] defaultBlacklist = {
					"# Plugin Blacklist Configuration",
					"# Add plugin names (without extension) to this file to prevent them from loading",
					"# Lines starting with # are comments",
					"",
					"# Known problematic plugins",
					"AKFont",
					"ICLView",
					"SQLiteViewer",
					"# Web/IE-based plugins (often cause issues)",
					"# HTMLView",
					"# WebView",
					"",
					"# Add more problematic plugins here"
				};

				File.WriteAllLines(blacklistFile, defaultBlacklist);
				Debug.Print($"WlxModuleList: Created default blacklist file: {blacklistFile}");
				
				// 添加默认黑名单插件
				_blacklistedPlugins.Add("AKFont");
				_blacklistedPlugins.Add("ICLView");
				_blacklistedPlugins.Add("SQLiteViewer");
			}
			catch (Exception ex)
			{
				Debug.Print($"WlxModuleList: Exception creating default blacklist - {ex.Message}");
			}
		}
		public void LoadConfiguration()
		{
			Debug.Print("load configuration for wlxmodulelist ");	//检查是否重复初始化
			_modules.Clear();
			_config = Helper.ReadSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ListerPlugins");
			_configDict = Helper.ParseConfig(_config, out var _pathdict, out _);
			pathdict = _pathdict;
		}
		public void SaveConfiguration()
		{
			if (!isConfigChanged) return;
			List<string> configContent = [];
			var i = 0;
			foreach (var pair in _configDict)	//bug fixed: configcontent内容与实际不符
			{
				var modulepath = pathdict[pair.Key];
				configContent.Add($"{i}={modulepath}");
				if(!string.IsNullOrEmpty(pair.Value))
					configContent.Add($"{i}_detect={pair.Value}");
				i++;
			}
			Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ListerPlugins", configContent);
			LoadConfiguration();
			isConfigChanged = false;
		}
		public void AddModule(WlxModule module)
		{
			if (!_modules.Any(m => m.FilePath.Equals(module.FilePath, StringComparison.OrdinalIgnoreCase)))
			{
				_modules.Add(module);
			}
		}

		public void RemoveModule(string filePath)
		{
			var module = _modules.FirstOrDefault(m => m.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
			if (module != null)
			{
				module.Dispose();
				_modules.Remove(module);
			}
		}

		public WlxModule? FindModuleForFile(string fileName, ref int tryModuleIdx)
		{
			// 应该按照configdict的配置次序依次查找， 而不是_modules的次序（文件系统的顺序）
			var i = 0;
			foreach(var cfgitem in _configDict)
			{
				if (i > tryModuleIdx) //已经尝试过的模块不再尝试
				{
					var module = FindModuleByName(cfgitem.Key);
					if (module != null && IsModuleSupported(module, fileName))					
					{
						tryModuleIdx = i;
						Debug.Print($"try to use #{i} module {module.Name} for {fileName} ");
						return module;
					}
				}
				i++;
			}
			tryModuleIdx = i;
			return null;
		}

		private bool IsModuleSupported(WlxModule module, string fileName)
		{
			if (string.IsNullOrEmpty(module.DetectString))
			{
				if (_configDict.TryGetValue(module.Name.ToUpper(), out var val))
					return isModuleSupport(val, fileName);
				
				return true;
			}

			return isModuleSupport(module.DetectString, fileName);
		}
		private bool isModuleSupport(string DetectString, string filename)
		{
			if (string.IsNullOrEmpty(DetectString)) return true;
			var p = new Dictionary<string, string>();
			var ext = Path.GetExtension(filename).ToLower().Trim('.');
			DetectString = DetectString.ToLower().Replace('"', '\''); //replace " with '
			
			p["ext"] = $"'{ext}'";
			if (File.Exists(filename))
			{
				var fileinfo = new FileInfo(filename);
				p["size"] = fileinfo.Length.ToString();
				if (DetectString.Contains('['))		//如果使用了索引器，则代表需要读取文件的前8192个字节
					p["_FILE8192_"] = File.ReadAllBytes(filename).ToString().Substring(0, 8192);
			}
			else
				p["size"] = "1";
			p["multimedia"] = ".true."; //temp ignore multimedia &
			p["force"] = ".false."; // temp ignore force |
			//var evaluator = new ExpressionEvaluatorClaude();
			//return (bool)evaluator.EvalExpr(DetectString, p);
			//方括号特殊处理，dslang的方括号是通用索引器，比如p='abc', p[0] = 'a' is true
			//但是detectstring中可以省略直接用[0] 表示取文件的第一个字节，所以根据TOTALCMD SDK定义：
			//[5] The fifth byte in the file to be loaded. The first 8192 bytes can be checked for a match.
			//约定使用特殊参数_FILE8192_，获得文件的前8192个字节作为内容，同时将'['替换为'_FILE8192_['
			return (bool)ExpressionEvaluatorDS.EvalExpr(DetectString.Replace("[", "_FILE8192_["), p);
		}
	
		public void LoadModulesFromDirectory(string directory)
		{
			if (ModuleLoaded) return;
			if (!Directory.Exists(directory)) 
			{
				Debug.Print($"WlxModuleList: Directory not found - {directory}");
				return;
			}

			Debug.Print($"WlxModuleList: Loading modules from {directory}");

			//读取pluginpath目录下所有子目录的plugins
			var subdirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
			foreach (var subdir in subdirs)
			{
				Debug.Print($"WlxModuleList: Scanning subdirectory - {subdir}");
				foreach (var file in Directory.GetFiles(subdir, "*.wlx64"))
				{
					try
					{
						string pluginName = Path.GetFileNameWithoutExtension(file);
						
						// 检查是否在黑名单中
						if (_blacklistedPlugins.Contains(pluginName))
						{
							Debug.Print($"WlxModuleList: Skipping blacklisted plugin - {pluginName}");
							continue;
						}
						
						Debug.Print($"WlxModuleList: Attempting to load plugin - {file}");
						
						var module = new WlxModule
						{
							FilePath = file,
							Name = pluginName
						};
						
						if (module.LoadModule())
						{
							AddModule(module);
							Debug.Print($"WlxModuleList: Successfully added module - {module.Name}");
						}
						else
						{
							Debug.Print($"WlxModuleList: Failed to load module - {file}");
							// 如果加载失败，可以考虑加入黑名单
							// _blacklistedPlugins.Add(pluginName);
						}
						
						//记录插件的完整路径到_pathdict
						if (!pathdict.TryGetValue(module.Name.ToUpper(), out var fullpath))
							pathdict[module.Name] = file;
					}
					catch (Exception ex)
					{
						// 加载失败的模块直接跳过，但记录错误信息
						Debug.Print($"WlxModuleList: Exception while loading {file} - {ex.Message}");
						
						// 如果是致命错误，可以考虑将插件加入黑名单
						if (ex.Message.Contains("0x0EEDFADE") || ex.Message.Contains("Fatal Application Exit"))
						{
							string pluginName = Path.GetFileNameWithoutExtension(file);
							_blacklistedPlugins.Add(pluginName);
							Debug.Print($"WlxModuleList: Added {pluginName} to blacklist due to fatal error");
						}
					}
				}
			}
			ModuleLoaded = true;
			Debug.Print($"WlxModuleList: Finished loading modules, total loaded: {_modules.Count}");
		}

		public void Dispose()
		{
			foreach (var module in _modules)
				module.Dispose();
			
			_modules.Clear();
			ModuleLoaded = false;
		}

		public WlxModule? FindModuleByName(string name)
		{
			return _modules.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// 将插件加入黑名单
		/// </summary>
		public void AddToBlacklist(string pluginName)
		{
			_blacklistedPlugins.Add(pluginName);
			Debug.Print($"WlxModuleList: Manually added {pluginName} to blacklist");
		}

		/// <summary>
		/// 从黑名单中移除插件
		/// </summary>
		public void RemoveFromBlacklist(string pluginName)
		{
			_blacklistedPlugins.Remove(pluginName);
			Debug.Print($"WlxModuleList: Removed {pluginName} from blacklist");
		}

		/// <summary>
		/// 检查插件是否在黑名单中
		/// </summary>
		public bool IsBlacklisted(string pluginName)
		{
			return _blacklistedPlugins.Contains(pluginName);
		}
	}
}
/*
 * ListLoad
 
ListLoad is called when a user opens lister with F3 or the Quick View Panel with Ctrl+Q, and when the definition string either doesn't exist, or its evaluation returns true.
 
Declaration:
 
HWND __stdcall ListLoad(HWND ParentWin,char* FileToLoad,int ShowFlags);
 
Description of parameters:
 
ParentWin This is lister's window. Create your plugin window as a child of this window.
 
FileToLoad The name of the file which has to be loaded.
 
ShowFlags A combination of the following flags:
lcp_wraptext Text: Word wrap mode is checked
lcp_fittowindow Images: Fit image to window is checked
lcp_fitlargeronly Fit image to window only if larger than the window.
Always set together with lcp_fittowindow.
lcp_center Center image in viewer window
lcp_ansi Ansi charset is checked
lcp_ascii Ascii(DOS) charset is checked
lcp_variable Variable width charset is checked
lcp_forceshow User chose 'Image/Multimedia' from the menu. See remarks.
lcp_darkmode Total Commander is in dark mode
lcp_darkmodenative Windows 10/11 supports dark mode natively, e.g. for scroll bars
You may ignore these parameters if they don't apply to your document type.
 
Return value:
 
Return a handle to your window if load succeeds, NULL otherwise. If NULL is returned, Lister will try the next plugin.
 
Remarks:
 
Please note that multiple Lister windows can be open at the same time! Therefore you cannot save settings in global variables. You can call RegisterClass with the parameter cbWndExtra to reserve extra space for your data, which you can then access via GetWindowLong(). Or use an internal list, and store the list parameter via SetWindowLong(hwnd,GWL_ID,...).
Lister will subclass your window to catch some hotkeys like 'n' or 'p'.
When lister is activated, it will set the focus to your window. If your window contains child windows, then make sure that you set the focus to the correct child when your main window receives the focus!
If lcp_forceshow is defined, you may try to load the file even if the plugin wasn't made for it. Example: A plugin with line numbers may only show the file as such when the user explicitly chooses 'Image/Multimedia' from the menu.
 
Lister plugins which only create thumbnail images do not need to implement this function. 
------------------------------------------

ListSearchDialog
 
ListSearchDialog is called when the user tries to find text in the plugin. Only implement this function if your plugin requires a plugin-specific search dialog! For searching text, please implement ListSearchText instead!
 
Declaration:
 
int __stdcall ListSearchDialog(HWND ListWin,int FindNext);
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
FindNext 0: FindFirst was chosen by the user
1: FindNext was chosen from the menu
 
Return value:
 
Return LISTPLUGIN_OK if you implement this function, or LISTPLUGIN_ERROR if Total Commander should show its own text search dialog and call ListSearchText later. This allows a plugin to support both its own search method via ListSearchDialog, and the standard search method via ListSearchText! Do NOT return LISTPLUGIN_ERROR if the search fails!
 
Remarks:
 
The plugin needs to show the search dialog and highlight/select the found text by itself.
Requires Total Commander 7 or later.
------------------------------------------

ListSearchText
 
ListSearchText is called when the user tries to find text in the plugin. Don't implement this function if your plugin doesn't contain any text, or doesn't support text searches!
 
Declaration:
 
int __stdcall ListSearchText(HWND ListWin,char* SearchString,int SearchParameter);
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
SearchString String to be searched.
 
SearchParameter A combination of the following search flags:
lcs_findfirst Search from the beginning of the first displayed line (not set: find next)
lcs_matchcase The search string is to be treated case-sensitively.
lcs_wholewords Find whole words only.
lcs_backwards Search backwards towards the beginning of the file.
 
 
Return value:
 
Return either LISTPLUGIN_OK or LISTPLUGIN_ERROR.
 
Remarks:
 
The plugin needs to highlight/select the found text by itself.
 --------------------------------------------------

ListSendCommand
 
ListSendCommand is called when the user changes some options in Lister's menu.
 
Declaration:
 
int __stdcall ListSendCommand(HWND ListWin,int Command,int Parameter);
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
Command One of the following commands:
lc_copy Copy current selection to the clipboard
lc_newparams New parameters passed to plugin, see Parameter below
lc_selectall Select the whole contents
lc_setpercent Go to new position in document (in percent).
 
Parameter Used for lc_newparams. May be a combination of:
lcp_wraptext Text: Word wrap mode is checked
lcp_ansi Ansi charset is checked
lcp_ascii Ascii(DOS) charset is checked
lcp_variable Variable width charset is checked
lcp_fittowindow Images: Fit image to window is checked
lcp_fitlargeronly Images: Sent in addition to lcp_fittowindow if only images larger than
the client are should be resized - smaller should be shown centered
lcp_center Images: Sent when the image needs to be centered
may be combined with lcp_fittowindow and/or lcp_fitlargeronly
Also used for lc_setpercent. In this case, the value is the new position (in percent) to which to scroll.
lcp_darkmode The user interface was switched from normal to dark mode. The flag is missing when switching from dark to normal mode
lcp_darkmodenative Windows 10/11 supports dark mode natively, e.g. for scroll bars
You may ignore these parameters if they don't apply to your document type.
 
Return value:
 
Return either LISTPLUGIN_OK or LISTPLUGIN_ERROR.
------------------------------------------------------------

WM_COMMAND
 
WM_COMMAND can be sent to the parent window to set a new percentage value in Lister's title bar, or to check some menu items like fonts or word wrap mode.
 
Usage:
 
PostMessage(GetParent(ListWin),WM_COMMAND,MAKELONG(value,itemtype),(LPARAM)ListWin);
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
value The new value, depending on what is passed in itemtype (see below).
 
itemtype Item to change in the framework (Lister) window. Can be one of the following:
itm_percent Set the percent value in the menu bar of the main Lister window.
itm_fontstyle Set the font style: set value to lcp_ansi, lcp_ascii, or lcp_variable.
itm_wrap Word wrap mode on or off. Set value to 1 for on or 0 for off.
itm_fit Fit image to screen on or off. Set value to 1 for on or 0 for off.
New in 1.6: Set to 2 for lcp_fittowindow and
to 3 for lcp_fitlargeronly (if 1, the user-chosen option is not changed)
itm_center New in 1.6: Center image on screen on or off. Set value to 1 for on or 0 for off.
itm_next New in TC 5.52: Switch to next file if multiple opened (e.g. after playing an mp3). The value of "value" MUST be 0!
 
Return value:
 
No value is returned by Lister, so you may use PostMessage() or SendMessage().
 
Notes:
 
The message can also be sent during ListLoad, even though Lister doesn't yet know the window handle of the list window! It sets a special flag to handle this. Do not send this message if you don't want to modify any of the values!
--------------------------------------------------------------------

ListNotificationReceived
 
ListNotificationReceived is called when the parent window receives a notification message from the child window: WM_COMMAND, WM_NOTIFY, WM_MEASUREITEM or WM_DRAWITEM
 
Declaration:
 
int __stdcall ListNotificationReceived(HWND ListWin,int Message,WPARAM wParam,LPARAM lParam);
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
Message The received message, one of the following: WM_COMMAND, WM_NOTIFY, WM_MEASUREITEM or WM_DRAWITEM.
 
wParam The WPARAM parameter of the message.
 
lParam The LPARAM parameter of the message.
 
Return value:
 
Return the value described for that message in the Windows API help.
 
Notes:
 
Do not implement this function if you don't use any owner-drawn controls and don't require any notification messages! Possible applications: Owner-drawn Listview control, reacting to scroll messages, etc.
---------------------------------------------------

ListGetPreviewBitmap
 
ListGetPreviewBitmap is called to retrieve a bitmap for the thumbnails view. Please only implement and export this function if it makes sense to show preview pictures for the supported file types! This function is new in version 1.4. It requires Total Commander >=6.5, but is ignored by older versions.
 
Declaration:
 
HBITMAP __stdcall ListGetPreviewBitmap(char* FileToLoad,int width,int height,
    char* contentbuf,int contentbuflen);
 
Description of parameters:
 
FileToLoad The name of the file for which to load the preview bitmap.
 
width Requested maximum width of the bitmap.
 
height Requested maximum height of the bitmap
 
contentbuf The first 8 kBytes (8k) of the file. Often this is enough data to show a reasonable preview, e.g. the first few lines of a text file.
 
contentbuflen The length of the data passed in contentbuf. Please note that contentbuf is not a 0 terminated string, it may contains 0 bytes in the middle! It's just the 1:1 contents of the first 8k of the file.
 
Return value:
 
Return a device-dependent bitmap created with e.g. CreateCompatibleBitmap.
 
Notes:
 
1. This function is only called in Total Commander 6.5 and later. The plugin version will be >= 1.4.
2. The bitmap handle goes into possession of Total Commander, which will delete it after using it. The plugin must not delete the bitmap handle!
3. Make sure you scale your image correctly to the desired maximum width+height! Do not fill the rest of the bitmap - instead, create a bitmap which is SMALLER than requested! This way, Total Commander can center your image and fill the rest with the default background color.
 
The following sample code will stretch a bitmap with dimensions bigwidth*bigheight down to max. width*height keeping the correct aspect ratio (proportions):
 
HBITMAP __stdcall ListGetPreviewBitmap(char* FileToLoad,int width,int height,
    char* contentbuf,int contentbuflen)
{
int w,h;
int stretchx,stretchy;
OSVERSIONINFO vx;
BOOL is_nt;
BITMAP bmpobj;
HBITMAP bmp_image,bmp_thumbnail,oldbmp_image,oldbmp_thumbnail;
HDC maindc,dc_thumbnail,dc_image;
POINT pt;
 
// check for operating system: Windows 9x does NOT support the HALFTONE stretchblt mode!
vx.dwOSVersionInfoSize=sizeof(vx);
GetVersionEx(&vx);
is_nt=vx.dwPlatformId==VER_PLATFORM_WIN32_NT;
 
// here you load your image
bmp_image=SomeHowLoadImageFromFile(FileToLoad);
if (bmp_image && GetObject(bmp_image,sizeof(bmpobj),&bmpobj)) {
  bigx=bmpobj.bmWidth;
  bigy=bmpobj.bmHeight;
  // do we need to stretch?
  if ((bigx>=width || bigy>=height) && (bigx>0 && bigy>0)) {
    stretchy=MulDiv(width,bigy,bigx);
    if (stretchy<=height) {
      w=width;
      h=stretchy;
      if (h<1) h=1;
    } else {
      stretchx=MulDiv(height,bigx,bigy);
      w=stretchx;
      if (w<1) w=1;
      h=height;
    }
    maindc=GetDC(GetDesktopWindow());
    dc_thumbnail=CreateCompatibleDC(maindc);
    dc_image=CreateCompatibleDC(maindc);
    bmp_thumbnail=CreateCompatibleBitmap(maindc,w,h);
    ReleaseDC(GetDesktopWindow(),maindc);
    oldbmp_image=(HBITMAP)SelectObject(dc_image,bmp_image);
    oldbmp_thumbnail=(HBITMAP)SelectObject(dc_thumbnail,bmp_thumbnail);
    if(is_nt) {
      SetStretchBltMode(dc_thumbnail,HALFTONE);
      SetBrushOrgEx(dc_thumbnail,0,0,&pt);
    } else {
      SetStretchBltMode(dc_thumbnail,COLORONCOLOR);
    }
    StretchBlt(dc_thumbnail,0,0,w,h,dc_image,0,0,bigx,bigy,SRCCOPY);
    SelectObject(dc_image,oldbmp_image);
    SelectObject(dc_thumbnail,oldbmp_thumbnail);
    DeleteDC(dc_image);
    DeleteDC(dc_thumbnail);
    DeleteObject(bmp_image);
    bmp_image=bmp_thumbnail;
  }
}
return bmp_image;
}
--------------------------------------------------------------

ListGetDetectString
 
ListGetDetectString is called when the plugin is loaded for the first time. It should return a parse function which allows Lister to find out whether your plugin can probably handle the file or not. You can use this as a first test - more thorough tests may be performed in ListLoad(). It's very important to define a good test string, especially when there are dozens of plugins loaded! The test string allows lister to load only those plugins relevant for that specific file type.
 
Declaration:
 
void __stdcall ListGetDetectString(char* DetectString,int maxlen);
 
Description of parameters:
 
DetectString Return the detection string here. See remarks for the syntax.
 
maxlen Maximum length, in bytes, of the detection string (currently 2k).
 
Return value:
 
This function doesn't return any value.
 
Remarks:
 
The syntax of the detection string is as follows. There are operands, operators and functions.
Operands:
EXT The extension of the file to be loaded (always uppercase).
SIZE The size of the file to be loaded.
FORCE 1 if the user chose 'Image/Multimedia' from the menu, 0 otherwise.
MULTIMEDIA This detect string is special: It is always TRUE (also in older TC versions). If it is present in the string, this plugin overrides internal multimedia viewers in TC. If not, the internal viewers are used. Check the example below!
[5] The fifth byte in the file to be loaded. The first 8192 bytes can be checked for a match.
12345 The number 12345
"TEST" The string "TEST"
 
Operators
& AND. The left AND the right expression must be true (!=0).
| OR: Either the left OR the right expression needs to be true (!=0).
= EQUAL: The left and right expression need to be equal.
!= UNEQUAL: The left and right expression must not be equal.
< SMALLER: The left expression is smaller than the right expression. Comparing a number and a string returns false (0). Booleans are stored as 0 (false) and 1 (true).
> LARGER: The left expression is larger than the right expression.
 
Functions
() Braces: The expression inside the braces is evaluated as a whole.
!() NOT: The expression inside the braces will be inverted. Note that the braces are necessary!
FIND() The text inside the braces is searched in the first 8192 bytes of the file. Returns 1 for success and 0 for failure.
FINDI() The text inside the braces is searched in the first 8192 bytes of the file. Upper/lowercase is ignored.
 
Internal handling of variables
 
Varialbes can store numbers and strings. Operators can compare numbers with numbers and strings with strings, but not numbers with strings. Exception: A single char can also be compared with a number. Its value is its ANSI character code (e.g. "A"=65). Boolean values of comparisons are stored as 1 (true) and 0 (false).
 
Examples:
 
String Interpretation
EXT="WAV" | EXT="AVI" The file may be a Wave or AVI file.
 
EXT="WAV" & [0]="R" & [1]="I" & [2]="F" & [3]="F" & FIND("WAVEfmt")
Also checks for Wave header "RIFF" and string "WAVEfmt"
 
EXT="WAV" & (SIZE<1000000 | FORCE) Load wave files smaller than 1000000 bytes at startup/file change, and all wave files if the user explictly chooses 'Image/Multimedia' from the menu.
 
([0]="P" & [1]="K" & [2]=3 & [3]=4) | ([0]="P" & [1]="K" & [2]=7 & [3]=8)
Checks for the ZIP header PK#3#4 or PK#7#8 (the latter is used for multi-volume zip files).
 
EXT="TXT" & !(FINDI("<HEAD>") | FINDI("<BODY>")) This plugin handles text files which aren't HTML files. A first detection is done with the <HEAD> and <BODY> tags. If these are not found, a more thorough check may be done in the plugin itself.
 
MULTIMEDIA & (EXT="WAV" | EXT="MP3") Replace the internal player for WAV and MP3 files (which normally uses Windows Media Player as a plugin). Requires TC 6.0 or later!
 
Operator precedence:
 
The strongest operators are =, != < and >, then comes &, and finally |. What does this mean? Example:
expr1="a" & expr2 | expr3<5 & expr4!=b will be evaluated as ((expr1="a") & expr2) | ((expr3<5) & (expr4!="b"))
If in doubt, simply use braces to make the evaluation order clear.
 
--------------------------------------------------------------------------

ListLoadNext
 
New in Total Commander 7: ListLoadNext is called when a user switches to the next or previous file in lister with 'n' or 'p' keys, or goes to the next/previous file in the Quick View Panel, and when the definition string either doesn't exist, or its evaluation returns true.
 
Declaration:
 
int __stdcall ListLoadNext(HWND ParentWin,HWND ListWin,char* FileToLoad,int ShowFlags);
 
Description of parameters:
 
ParentWin This is lister's window. Your plugin window needs to be a child of this window
 
ListWin The plugin window returned by ListLoad
 
FileToLoad The name of the file which has to be loaded.
 
ShowFlags A combination of the following flags:
lcp_wraptext Text: Word wrap mode is checked
lcp_fittowindow Images: Fit image to window is checked
lcp_fitlargeronly Fit image to window only if larger than the window.
Always set together with lcp_fittowindow.
lcp_center Center image in viewer window
lcp_ansi Ansi charset is checked
lcp_ascii Ascii(DOS) charset is checked
lcp_variable Variable width charset is checked
lcp_forceshow User chose 'Image/Multimedia' from the menu. See remarks.
lcp_darkmode Total Commander is in dark mode
lcp_darkmodenative Windows 10/11 supports dark mode natively, e.g. for scroll bars
You may ignore these parameters if they don't apply to your document type.
 
Return value:
 
Return LISTPLUGIN_OK if load succeeds, LISTPLUGIN_ERROR otherwise. If LISTPLUGIN_ERROR is returned, Lister will try to load the file with the normal ListLoad function (also with other plugins).
 
Remarks:
 
Please note that multiple Lister windows can be open at the same time! Therefore you cannot save settings in global variables. You can call RegisterClass with the parameter cbWndExtra to reserve extra space for your data, which you can then access via GetWindowLong(). Or use an internal list, and store the list parameter via SetWindowLong(hwnd,GWL_ID,...).
Lister will subclass your window to catch some hotkeys like 'n' or 'p'.
When lister is activated, it will set the focus to your window. If your window contains child windows, then make sure that you set the focus to the correct child when your main window receives the focus!
If lcp_forceshow is defined, you may try to load the file even if the plugin wasn't made for it. Example: A plugin with line numbers may only show the file as such when the user explicitly chooses 'Image/Multimedia' from the menu.
 
Lister plugins which only create thumbnail images do not need to implement this function. If you do not implement LIstLoadNext but only ListLoad, then the plugin will be unloaded and loaded again when switching through files, which results in flickering.
------------------------------------------------------------------

ListPrint
 
ListPrint is called when the user chooses the print function.
 
Declaration:
 
int __stdcall ListPrint(HWND ListWin,char* FileToPrint,char* DefPrinter,
                        int PrintFlags,RECT* Margins)
 
Description of parameters:
 
ListWin Hande to your list window created with ListLoad
 
FileToPrint The full name of the file which needs to be printed. This is the same file as loaded with ListLoad.
 
DefPrinter Name of the printer currently chosen in Total Commander. May be NULL (use default printer).
 
PrintFlags Currently not used (set to 0). May be used in a later version.
 
Margins The left, top, right and bottom margins of the print area, in MM_LOMETRIC measurement units (1/10 mm).
May be ignored.
 
Return value:
 
Return either LISTPLUGIN_OK or LISTPLUGIN_ERROR.
 
Notes:
 
You need to show a print dialog, in which the user can choose what to print, and select a different printer. See the sample plugin on how to do this!
---------------------------------------------------------

ListSetDefaultParams
 
ListSetDefaultParams is called immediately after loading the DLL, before ListLoad. This function is new in version 1.2. It requires Total Commander >=5.51, but is ignored by older versions.
 
Declaration:
 
void __stdcall ListSetDefaultParams(ListDefaultParamStruct* dps);
 
Description of parameters:
 
dps This structure of type ListDefaultParamStruct currently contains the version number of the plugin interface, and the suggested location for the settings file (ini file). It is recommended to store any plugin-specific information either directly in that file, or in that directory under a different name. Make sure to use a unique header when storing data in this file, because it is shared by other file system plugins! If your plugin needs more than 1kbyte of data, you should use your own ini file because ini files are limited to 64k.
 
Return value:
 
The function has no return value:
 
Important note:
 
This function is only called in Total Commander 5.51 and later. The plugin version will be >= 1.2.
----------------------------------------------------------

ListDefaultParamStruct
 
ListDefaultParamStruct is passed to ListSetDefaultParams to inform the plugin about the current plugin interface version and ini file location.
 
Declaration:
 
typedef struct {
    int size;
    DWORD PluginInterfaceVersionLow;
    DWORD PluginInterfaceVersionHi;
    char DefaultIniName[MAX_PATH];
} ListDefaultParamStruct;
 
Description of struct members:
 
size The size of the structure, in bytes. Later revisions of the plugin interface may add more structure members, and will adjust this size field accordingly.
 
PluginInterfaceVersionLow Low value of plugin interface version. This is the value after the comma, multiplied by 100! Example. For plugin interface version 1.3, the low DWORD is 30 and the high DWORD is 1.
 
PluginInterfaceVersionHi High value of plugin interface version.
 
DefaultIniName Suggested location+name of the ini file where the plugin could store its data. This is a fully qualified path+file name, and will be in the same directory as the wincmd.ini. It's recommended to store the plugin data in this file or at least in this directory, because the plugin directory or the Windows directory may not be writable! 
---------------------------------------------------------


#define lc_copy   1
#define lc_newparams 2
#define lc_selectall 3
#define lc_setpercent 4

#define lcp_wraptext 1
#define lcp_fittowindow 2
#define lcp_ansi   4
#define lcp_ascii   8
#define lcp_variable 12
#define lcp_forceshow 16
#define lcp_fitlargeronly 32
#define lcp_center 64
#define lcp_darkmode 128
#define lcp_darkmodenative 256

#define lcs_findfirst 1
#define lcs_matchcase 2
#define lcs_wholewords 4
#define lcs_backwards 8

#define itm_percent 0xFFFE
#define itm_fontstyle 0xFFFD
#define itm_wrap   0xFFFC
#define itm_fit   0xFFFB
#define itm_next   0xFFFA
#define itm_center 0xFFF9

#define LISTPLUGIN_OK 0
#define LISTPLUGIN_ERROR 1

typedef struct {
int size;
DWORD PluginInterfaceVersionLow;
DWORD PluginInterfaceVersionHi;
char DefaultIniName[MAX_PATH];
} ListDefaultParamStruct;

HWND __stdcall ListLoad(HWND ParentWin,char* FileToLoad,int ShowFlags);
HWND __stdcall ListLoadW(HWND ParentWin,WCHAR* FileToLoad,int ShowFlags);
int __stdcall ListLoadNext(HWND ParentWin,HWND PluginWin,char* FileToLoad,int ShowFlags);
int __stdcall ListLoadNextW(HWND ParentWin,HWND PluginWin,WCHAR* FileToLoad,int ShowFlags);
void __stdcall ListCloseWindow(HWND ListWin);
void __stdcall ListGetDetectString(char* DetectString,int maxlen);
int __stdcall ListSearchText(HWND ListWin,char* SearchString,int SearchParameter);
int __stdcall ListSearchTextW(HWND ListWin,WCHAR* SearchString,int SearchParameter);
int __stdcall ListSearchDialog(HWND ListWin,int FindNext);
int __stdcall ListSendCommand(HWND ListWin,int Command,int Parameter);
int __stdcall ListPrint(HWND ListWin,char* FileToPrint,char* DefPrinter,
                        int PrintFlags,RECT* Margins);
int __stdcall ListPrintW(HWND ListWin,WCHAR* FileToPrint,WCHAR* DefPrinter,
                        int PrintFlags,RECT* Margins);
int __stdcall ListNotificationReceived(HWND ListWin,int Message,WPARAM wParam,LPARAM lParam);
void __stdcall ListSetDefaultParams(ListDefaultParamStruct* dps);
HBITMAP __stdcall ListGetPreviewBitmap(char* FileToLoad,int width,int height,
    char* contentbuf,int contentbuflen);
HBITMAP __stdcall ListGetPreviewBitmapW(WCHAR* FileToLoad,int width,int height,
    char* contentbuf,int contentbuflen);

 */
