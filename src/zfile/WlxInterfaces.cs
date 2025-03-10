using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using WinShell;
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
namespace WinFormsApp1
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

	[StructLayout(LayoutKind.Sequential)]
	public struct ListDefaultParamStruct
	{
		public int Size;
		public int PluginInterfaceVersionHi;
		public int PluginInterfaceVersionLow;
		public string DefaultIniName;
	}

	// 必需的函数委托定义
	public delegate IntPtr ListLoad(IntPtr parentWin, string fileToLoad, int showFlags);
	public delegate IntPtr ListLoadW(IntPtr parentWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int showFlags);
	public delegate int ListLoadNext(IntPtr parentWin, IntPtr pluginWin, string fileToLoad, int showFlags);
	public delegate int ListLoadNextW(IntPtr parentWin, IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int showFlags);
	public delegate void ListCloseWindow(IntPtr pluginWin);
	public delegate void ListGetDetectString(StringBuilder detectString, int maxLen);
	public delegate int ListSearchText(IntPtr pluginWin, string searchString, int searchParameter);
	public delegate int ListSearchDialog(IntPtr pluginWin, int findNext);
	public delegate int ListSendCommand(IntPtr pluginWin, int command, int parameter);
	public delegate int ListSetDefaultParams(ref ListDefaultParamStruct dps);
	public delegate int ListPrint(IntPtr pluginWin, string fileToPrint, string defPrinter, int printFlags, ref IntPtr margins);
	
	// 可选的函数委托定义
	public delegate int ListSearchTextW(IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string searchString, int searchParameter);
	public delegate int ListPrintW(IntPtr pluginWin, [MarshalAs(UnmanagedType.LPWStr)] string fileToPrint, [MarshalAs(UnmanagedType.LPWStr)] string defPrinter, int printFlags, ref IntPtr margins);
	public delegate int ListGetPreviewBitmap(string fileToLoad, int width, int height, IntPtr bitmapHandle);
	public delegate void ListNotificationReceived(IntPtr pluginWin, int message, IntPtr wParam, IntPtr lParam);
	public delegate int ListGetValue(int field, [MarshalAs(UnmanagedType.LPWStr)] string filePath, int unitIndex, int maxLen, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value);
	public delegate int ListGetPreviewBitmapW([MarshalAs(UnmanagedType.LPWStr)] string fileToLoad, int width, int height, IntPtr bitmapHandle);

	public class WlxModule : IDisposable
	{
		private IntPtr _moduleHandle;
		
		// 必需的函数指针
		private ListLoad _listLoad;
		private ListLoadW _listLoadW;
		private ListLoadNext _listLoadNext;
		private ListLoadNextW _listLoadNextW;
		private ListCloseWindow _listCloseWindow;
		private ListGetDetectString _listGetDetectString;
		private ListSearchText _listSearchText;
		private ListSearchDialog _listSearchDialog;
		private ListSendCommand _listSendCommand;
		private ListSetDefaultParams _listSetDefaultParams;
		private ListPrint _listPrint;
		
		// 可选的函数指针
		private ListSearchTextW _listSearchTextW;
		private ListPrintW _listPrintW;
		private ListGetPreviewBitmap _listGetPreviewBitmap;
		private ListGetPreviewBitmapW _listGetPreviewBitmapW;
		private ListNotificationReceived _listNotificationReceived;
		private ListGetValue _listGetValue;

		public string Name { get; set; }
		public string FilePath { get; set; }
		public string DetectString { get; set; }
		public bool IsMultimedia { get; set; }
		public bool IsLoaded => _moduleHandle != IntPtr.Zero;

		public WlxModule()
		{
			Name = string.Empty;
			FilePath = string.Empty;
			DetectString = string.Empty;
		}

		public bool LoadModule()
		{
			if (IsLoaded) return true;

			try
			{
				_moduleHandle = NativeLibrary.Load(FilePath);
				if (_moduleHandle == IntPtr.Zero) return false;

				// 加载必需的函数
				int flg = 0;
				try { _listLoad = GetFunction<ListLoad>("ListLoad"); } catch (Exception ex) { flg++;  }
				try { _listLoadW = GetFunction<ListLoadW>("ListLoadW"); } catch (Exception ex) { flg++; }
				if ( flg == 2)
					throw new Exception("required listload can not be found!");

				// 加载可选函数 // 可选函数加载失败不影响插件使用
				try { _listLoadNext = GetFunction<ListLoadNext>("ListLoadNext"); }
				catch (Exception ex) { }
				// 加载Unicode版本函数 // Unicode函数加载失败不影响插件使用
				try { _listLoadNextW = GetFunction<ListLoadNextW>("ListLoadNextW"); }
				catch (Exception ex) { }
				try { _listSearchText = GetFunction<ListSearchText>("ListSearchText"); }
				catch (Exception ex) { }
				try { _listSearchTextW = GetFunction<ListSearchTextW>("ListSearchTextW"); }
				catch (Exception ex) { }
				try { _listPrint = GetFunction<ListPrint>("ListPrint"); }
				catch (Exception ex) { }
				try { _listPrintW = GetFunction<ListPrintW>("ListPrintW"); }
				catch (Exception ex) { }
				try { _listGetPreviewBitmap = GetFunction<ListGetPreviewBitmap>("ListGetPreviewBitmap"); }
				catch (Exception ex) { }
				try { _listGetPreviewBitmapW = GetFunction<ListGetPreviewBitmapW>("ListGetPreviewBitmapW"); }
				catch (Exception ex) { }

				try { _listCloseWindow = GetFunction<ListCloseWindow>("ListCloseWindow"); }
				catch (Exception ex) { }
				try { _listGetDetectString = GetFunction<ListGetDetectString>("ListGetDetectString"); }
				catch (Exception ex) { }
				try { _listSearchDialog = GetFunction<ListSearchDialog>("ListSearchDialog"); }
				catch (Exception ex) { }
				try { _listSendCommand = GetFunction<ListSendCommand>("ListSendCommand"); }
				catch (Exception ex) { }
				try { _listNotificationReceived = GetFunction<ListNotificationReceived>("ListNotificationReceived");}
				catch (Exception ex) { }
				try { _listSetDefaultParams = GetFunction<ListSetDefaultParams>("ListSetDefaultParams"); }
				catch (Exception ex) { }

				// 初始化插件
				CallListSetDefaultParams();
				LoadDetectString();

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
			IntPtr functionPtr = NativeLibrary.GetExport(_moduleHandle, functionName);
			if (functionPtr == IntPtr.Zero)
				//throw new EntryPointNotFoundException($"Function {functionName} not found in module {FilePath}");
				return null;
			return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
		}

		private void LoadDetectString()
		{
			if (_listGetDetectString == null) return;

			StringBuilder detectStr = new StringBuilder(1024);
			_listGetDetectString(detectStr, detectStr.Capacity);
			DetectString = detectStr.ToString();
			IsMultimedia = DetectString.Contains("multimedia", StringComparison.OrdinalIgnoreCase);
		}

		private void CallListSetDefaultParams()
		{
			if (_listSetDefaultParams == null) return;

			var defaultParams = new ListDefaultParamStruct
			{
				Size = Marshal.SizeOf<ListDefaultParamStruct>(),
				PluginInterfaceVersionHi = 2,
				PluginInterfaceVersionLow = 0,
				DefaultIniName = "wlx.ini"
			};
			_listSetDefaultParams(ref defaultParams);
		}

		public IntPtr CallListLoad(IntPtr parentWin, string fileToLoad, int showFlags)
		{
			try
			{
				if (_listLoadW != null)
					return _listLoadW(parentWin, fileToLoad, showFlags);
				return _listLoad?.Invoke(parentWin, fileToLoad, showFlags) ?? IntPtr.Zero;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ListLoad error: {ex.Message}");
				return IntPtr.Zero;
			}
			
			//if (_listLoadW != null)
			//	return _listLoadW(parentWin, fileToLoad, showFlags);
			//return _listLoad != null ? _listLoad(parentWin, fileToLoad, showFlags) : IntPtr.Zero;
		}

		public int CallListLoadNext(IntPtr parentWin, IntPtr pluginWin, string fileToLoad, int showFlags)
		{
			if (_listLoadNextW != null)
				return _listLoadNextW(parentWin, pluginWin, fileToLoad, showFlags);
			return _listLoadNext != null ? _listLoadNext(parentWin, pluginWin, fileToLoad, showFlags) : WlxConstants.LISTPLUGIN_ERROR;
		}

		public void CallListCloseWindow(IntPtr pluginWin)
		{
			_listCloseWindow?.Invoke(pluginWin);
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

		public int CallListPrint(IntPtr pluginWin, string fileToPrint, string defPrinter, int printFlags, ref IntPtr margins)
		{
			if (_listPrintW != null)
				return _listPrintW(pluginWin, fileToPrint, defPrinter, printFlags, ref margins);
			return _listPrint != null ? _listPrint(pluginWin, fileToPrint, defPrinter, printFlags, ref margins) : WlxConstants.LISTPLUGIN_ERROR;
		}

		public int CallListGetPreviewBitmap(string fileToLoad, int width, int height, IntPtr bitmapHandle)
		{
			if (_listGetPreviewBitmapW != null)
				return _listGetPreviewBitmapW(fileToLoad, width, height, bitmapHandle);
			return _listGetPreviewBitmap?.Invoke(fileToLoad, width, height, bitmapHandle) ?? WlxConstants.LISTPLUGIN_ERROR;
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

		public void UnloadModule()
		{
			if (_moduleHandle != IntPtr.Zero)
			{
				NativeLibrary.Free(_moduleHandle);
				_moduleHandle = IntPtr.Zero;
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

		public void Dispose()
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
		public List<WlxModule> _modules = new List<WlxModule>();

		public List<WlxModule> Modules { get { return _modules; } }
		public bool isConfigChanged = false;
		public WlxModuleList()
		{
			LoadConfiguration();
		}
		public void LoadConfiguration(){
			_modules.Clear();
			_config = Helper.ReadSectionContent(Constants.ZfileCfgPath+"wincmd.ini", "ListerPlugins");
			_configDict = Helper.ParseConfig(_config);
		}
		public void SaveConfiguration()
		{
			if (!isConfigChanged) return;
			List<string> configContent = new();
			foreach (var pair in _configDict)
			{
				if(!configContent.Contains(pair.Key))
					configContent.Append(pair.Key + "=" + pair.Value + Environment.NewLine);
				else
				{
					configContent[configContent.IndexOf(pair.Key)] += $",{pair.Value}";
				}
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

		public WlxModule FindModuleForFile(string fileName)
		{
			foreach(var module in _modules)
			{
				if (IsModuleSupported(module, fileName))
				{
					return module;
				}
			}
			return null;
			//return _modules.FirstOrDefault(m => IsModuleSupported(m, fileName));//TODO BUGFIX: 应该按照configdict的配置次序依次查找， 而不是_modules的次序（文件系统的顺序）
		}

		private bool IsModuleSupported(WlxModule module, string fileName)
		{
			if (string.IsNullOrEmpty(module.DetectString))
			{
				//return false;
				if(_configDict.TryGetValue(module.Name.ToUpper(), out string val))
					return isModuleSupport(val, fileName);
				else
					return false;
			}

			return isModuleSupport(module.DetectString, fileName);
		}
		private bool isModuleSupport(string DetectString, string fileName)
		{
			DetectString = DetectString.ToLower();
			var isMultimedia = (DetectString.Contains("multimedia",StringComparison.OrdinalIgnoreCase));
			// 删除MULTIMEDIA FORCE ( ) & 空格
			DetectString = DetectString.Replace("multimedia", "").Replace("force", "").Replace("(", "").Replace(")", "").Replace("&", "").Replace(" ", "");
			// 解析检测字符串
			var detectParts = DetectString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			var fileExt = Path.GetExtension(fileName).ToLower().Trim('.');
			foreach (var part in detectParts)
			{
				if (part.StartsWith("ext="))    
				{
					var extensions = part.Substring(4).Split(',');
					//if necessary, remove the leading " and trailing " for each extensions item
					extensions = Helper.RemoveQuotes(extensions);
					if (extensions.Any(ext => fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase)))
						return true;
				}
				// 可以添加其他检测规则的支持
			}
			//TODO BUGFIX: 遇到MULTIMEDIA|特殊处理
			if (isMultimedia)
			{
				var mexts = "avi,mpg,mpeg,mp3,mp4,flv,wmv,rm,rmvb,3gp,ogg,webm,flac,wav,ape,alac,aac,ac3,amr,ape,au,awb,caf,dts,flac,m4a,mka,mlp,mp2,mpa,mpc,ofr,ofs,oga".Split(',');
				if (mexts.Any(ext => fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase)))
					return true;
			}
			return false;
		}
		public void LoadModulesFromDirectory(string directory)
		{
			if (!Directory.Exists(directory)) return;

			//读取pluginpath目录下所有子目录的plugins
			var subdirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
			foreach (var subdir in subdirs)
			{
				foreach (var file in Directory.GetFiles(subdir, "*.wlx*"))
				{
					try
					{
						var module = new WlxModule
						{
							FilePath = file,
							Name = Path.GetFileNameWithoutExtension(file)
						};
						if (module.LoadModule())
						{
							AddModule(module);
						}
					}
					catch
					{
						// 加载失败的模块直接跳过
					}
				}
			}
		}

		public void Dispose()
		{
			foreach (var module in _modules)
			{
				module.Dispose();
			}
			_modules.Clear();
		}

		public WlxModule FindModuleByName(string name)
		{
			return _modules.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
	}
}
