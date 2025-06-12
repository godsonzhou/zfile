using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace zfile
{
    #region WDX常量和结构体
    public static class WdxConstants
    {
		// 字段类型常量
		public const int FT_NOMOREFIELDS = 0;   // 没有更多字段
		public const int FT_NUMERIC_32 = 1;      // 32位整数
        public const int FT_NUMERIC_64 = 2;      // 64位整数
        public const int FT_NUMERIC_FLOATING = 3; // 浮点数
        public const int FT_DATE = 4;            // 日期
        public const int FT_TIME = 5;            // 时间
        public const int FT_BOOLEAN = 6;         // 布尔值
		public const int FT_MULTIPLECHOICE = 7;  // 多选项
		public const int FT_STRING = 8;          // 字符串
        public const int FT_FULLTEXT = 9;       // 全文本
		public const int FT_DATETIME = 10;        // 日期时间
		public const int FT_STRINGW = 11;
		public const int FT_FULLTEXTW = 12;     // Unicode全文本

		public const int FT_NOSUCHFIELD = -1;    // 无此字段
		public const int FT_FILEERROR = -2;   // 文件错误
		public const int FT_FIELDEMPTY = -3;      // 字段为空
		public const int FT_ONDEMAND = -4;
		public const int FT_NOTSUPPORTED = -5;
		public const int FT_SETCANCEL = -6;
		public const int FT_DELAYED = 0;
		// 返回值常量
		public const int WDX_SUCCESS = 0;
        public const int WDX_ERROR = 1;
        public const int WDX_NOTFOUND = -1;

        // 特殊字段类型常量（用于全文处理）
        
    }

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class WdxField
    {
        //[MarshalAs(UnmanagedType.LPWStr)]
        public string Name;           // 字段名称
                                      //[MarshalAs(UnmanagedType.LPWStr)]
        public string Description;    // 字段描述
        public int Type;             // 字段类型
        public string[] Units;       // 单位列表
        public int DefaultUnitIndex; // 默认单位索引
        public int GetUnitIndex(string unit)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                if (Units[i].Equals(unit, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ContentDefaultParamStruct
    {
        public int Size;
        public int PluginInterfaceVersionLow;
        public int PluginInterfaceVersionHi;
        //[MarshalAs(UnmanagedType.LPStr)]
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DefaultIniName;
    }
    #endregion

    #region WDX函数委托
    // 必需的函数
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public delegate int ContentGetSupportedField(int FieldIndex, StringBuilder FieldName, StringBuilder UnitName, int MaxLen);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate int ContentGetValue(string FileName, int FieldIndex, int UnitIndex, StringBuilder FieldValue, int MaxLen, int Flags);
	public delegate int ContentGetValue([MarshalAs(UnmanagedType.LPStr)] string FileName, int FieldIndex, int UnitIndex, [Out] byte[] FieldValue, int MaxLen, int Flags);

	// Unicode版本
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	//public delegate int ContentGetValueW([MarshalAs(UnmanagedType.LPWStr)] string FileName, int FieldIndex, int UnitIndex, StringBuilder FieldValue, int MaxLen, int Flags);
	//[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int ContentGetValueW(
		[MarshalAs(UnmanagedType.LPWStr)] string FileName,
		int FieldIndex,
		int UnitIndex,
		[Out] byte[] FieldValue,   // 改为字节数组
		int MaxLen,                // 缓冲区字节大小
		int Flags
	);
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    // public delegate int ContentSetDefaultParams(ref ContentDefaultParamStruct dps);
    public delegate void ContentSetDefaultParams(IntPtr dps);

    // 可选函数
    public delegate void ContentPluginUnloading();
    public delegate void ContentStopGetValue(string FileName);
    public delegate int ContentGetDefaultSortOrder(int FieldIndex);
    public delegate int ContentSetValue(string FileName, int FieldIndex, int UnitIndex, string FieldValue, int Flags);

    // 新增可选函数
    public delegate void ContentGetDetectString(StringBuilder DetectString, int MaxLen);
    public delegate int ContentGetSupportedFieldFlags(int FieldIndex);
    public delegate int ContentEditValue(IntPtr Handle, int FieldIndex, int UnitIndex, int FieldType, StringBuilder FieldValue, int MaxLen, int Flags, string LangIdentifier);
    public delegate void ContentSendStateInformation(int State, string Path);

    // 新增Unicode版本的可选函数
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate void ContentStopGetValueW([MarshalAs(UnmanagedType.LPWStr)] string FileName);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate int ContentSetValueW([MarshalAs(UnmanagedType.LPWStr)] string FileName, int FieldIndex, int UnitIndex, int FieldType, IntPtr FieldValue, int Flags);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate void ContentSendStateInformationW(int State, [MarshalAs(UnmanagedType.LPWStr)] string Path);
    #endregion

    public class WdxModule : DcxModule, IDisposable
    {
        #region 字段
        //private IntPtr ModuleHandle;
        //private string ModulePath;
        private string _pluginName;
        public string Name => _pluginName;
        private bool _isUnicode;
        private List<WdxField> _fields;
        private Dictionary<string, string> _translations;

        // 必需的函数指针
        private ContentGetSupportedField? _contentGetSupportedField;
        private ContentGetValue? _contentGetValue;
        private ContentGetValueW? _contentGetValueW;
        private ContentSetDefaultParams? _contentSetDefaultParams;

        // 可选的函数指针
        private ContentPluginUnloading? _contentPluginUnloading;
        private ContentStopGetValue? _contentStopGetValue;
        private ContentGetDefaultSortOrder? _contentGetDefaultSortOrder;
        private ContentSetValue? _contentSetValue;

        // 新增可选函数指针
        private ContentGetDetectString? _contentGetDetectString;
        private ContentGetSupportedFieldFlags? _contentGetSupportedFieldFlags;
        private ContentEditValue? _contentEditValue;
        private ContentSendStateInformation? _contentSendStateInformation;

        // 新增Unicode版本的可选函数指针
        private ContentStopGetValueW? _contentStopGetValueW;
        private ContentSetValueW? _contentSetValueW;
        private ContentSendStateInformationW? _contentSendStateInformationW;
        #endregion

        #region 属性
        //public string ModulePath => _modulePath;
        public string PluginName => _pluginName;
        public bool IsLoaded => ModuleHandle != IntPtr.Zero;
        public bool IsUnicode => _isUnicode;
        public IReadOnlyList<WdxField> Fields => _fields.AsReadOnly();
        public string FileName { get => ModulePath; set => ModulePath = value; }
        public string? DetectString;

        /// <summary>
        /// 获取字段索引
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段索引，如果未找到则返回-1</returns>
        private int GetFieldIndex(string fieldName)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
        #endregion

        #region 构造函数和初始化
        public WdxModule(string modulePath)
        {
            ModulePath = modulePath;
            _pluginName = Path.GetFileNameWithoutExtension(modulePath);
            _fields = new List<WdxField>();
            _translations = new Dictionary<string, string>();
        }
        public WdxModule(string pluginName, string modulePath)
        {
            ModulePath = modulePath;
            _pluginName = pluginName;
            _fields = new List<WdxField>();
            _translations = new Dictionary<string, string>();
        }

        public override bool LoadModule()
        {
            if (IsLoaded) return true;
            if (!File.Exists(ModulePath))
                return false;

            try
            {
				Debug.Print($"try to load wdx module {ModulePath}");
                ModuleHandle = NativeLibrary.Load(ModulePath);
                if (ModuleHandle == IntPtr.Zero) return false;

                // 加载必需的函数
                _contentGetSupportedField = GetDelegate<ContentGetSupportedField>("ContentGetSupportedField");
                _contentSetDefaultParams = GetDelegate<ContentSetDefaultParams>("ContentSetDefaultParams");

                if (_contentGetSupportedField == null) // || _contentSetDefaultParams == null)
                {
                    UnloadModule();
                    return false;
                }

                // 尝试加载Unicode版本函数
                _contentGetValueW = GetDelegate<ContentGetValueW>("ContentGetValueW");
                if (_contentGetValueW != null)
                {
                    _isUnicode = true;
                }
                else
                {
                    _contentGetValue = GetDelegate<ContentGetValue>("ContentGetValue");
                    if (_contentGetValue == null)
                    {
                        UnloadModule();
                        return false;
                    }
                }

                // 加载可选函数
                _contentPluginUnloading = GetDelegate<ContentPluginUnloading>("ContentPluginUnloading");
                _contentStopGetValue = GetDelegate<ContentStopGetValue>("ContentStopGetValue");
                _contentGetDefaultSortOrder = GetDelegate<ContentGetDefaultSortOrder>("ContentGetDefaultSortOrder");
                _contentSetValue = GetDelegate<ContentSetValue>("ContentSetValue");

                // 加载新增可选函数
                _contentGetDetectString = GetDelegate<ContentGetDetectString>("ContentGetDetectString");
                _contentGetSupportedFieldFlags = GetDelegate<ContentGetSupportedFieldFlags>("ContentGetSupportedFieldFlags");
                _contentEditValue = GetDelegate<ContentEditValue>("ContentEditValue");
                _contentSendStateInformation = GetDelegate<ContentSendStateInformation>("ContentSendStateInformation");

                // 加载新增Unicode版本的可选函数
                if (_isUnicode)
                {
                    _contentStopGetValueW = GetDelegate<ContentStopGetValueW>("ContentStopGetValueW");
                    _contentSetValueW = GetDelegate<ContentSetValueW>("ContentSetValueW");
                    _contentSendStateInformationW = GetDelegate<ContentSendStateInformationW>("ContentSendStateInformationW");
                }

                // 初始化插件
                var defaultParams = new ContentDefaultParamStruct
                {
                    Size = Marshal.SizeOf<ContentDefaultParamStruct>(),
                    PluginInterfaceVersionLow = 50, //1,
                    PluginInterfaceVersionHi = 1, //2,
                    DefaultIniName = Path.Combine(Constants.ZfileCfgPath, "wdx.ini")
                };
                try
                {
                    // if (_contentSetDefaultParams(ref defaultParams) != WdxConstants.WDX_SUCCESS)
                    // IntPtr pDps = Marshal.AllocHGlobal(defaultParams.Size);
                    // Marshal.StructureToPtr(defaultParams, pDps, false);

                    // int result = _contentSetDefaultParams != null ?
                    //     _contentSetDefaultParams(ref defaultParams) : WdxConstants.WDX_ERROR;

                    // Marshal.FreeHGlobal(pDps);

                    // if (result != WdxConstants.WDX_SUCCESS)
                    // {
                    //     UnloadModule();
                    //     return false;
                    // }
                    if (_contentSetDefaultParams != null)
                    {
                        IntPtr pDps = Marshal.AllocHGlobal(Marshal.SizeOf<ContentDefaultParamStruct>());
                        Marshal.StructureToPtr(defaultParams, pDps, false);
						GC.KeepAlive(pDps);
                        _contentSetDefaultParams(pDps);
                        Marshal.FreeHGlobal(pDps);
                    }
                    StringBuilder detectstring = new();
                    if (_contentGetDetectString != null)
                    {
                        var str = GetDetectString();
                        if (!string.IsNullOrEmpty(str))
                            DetectString = str;
                    }
                }
                catch (Exception ex)
                {
                    UnloadModule();
                    return false;
                }
                // 加载支持的字段
                LoadSupportedFields();
                Debug.Print($"{ModulePath} loaded completed.");
                return true;
            }
            catch
            {
                UnloadModule();
                return false;
            }
        }

        //private T? GetDelegate<T>(string functionName) where T : Delegate
        //{
        //    //IntPtr procAddress = NativeLibrary.GetExport(_moduleHandle, functionName);
        //    //return procAddress != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(procAddress) : null;
        //    IntPtr procAddress = DcxModule.NativeMethods.GetProcAddress(_moduleHandle, functionName);
        //    if (procAddress == IntPtr.Zero)
        //        return null;
        //    //return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
        //    return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
        //}

        private void LoadSupportedFields()
        {
            _fields.Clear();
            int fieldIndex = 0;
            const int MAX_LEN = 256;

            StringBuilder fieldNameBuffer = new StringBuilder(MAX_LEN);
            StringBuilder unitNameBuffer = new StringBuilder(MAX_LEN);

            while (true)
            {
                fieldNameBuffer.Clear();
                unitNameBuffer.Clear();

                int result = _contentGetSupportedField(fieldIndex, fieldNameBuffer, unitNameBuffer, MAX_LEN);
                if (result <= WdxConstants.FT_NOMOREFIELDS) break;

                string fieldName = fieldNameBuffer.ToString();
                string unitName = unitNameBuffer.ToString();

                // 解析单位列表
                string[] units = string.IsNullOrEmpty(unitName) ? new string[0] : unitName.Split('|');

                var field = new WdxField
                {
                    Name = fieldName,
                    Type = result,
                    Units = units,
                    DefaultUnitIndex = 0
                };

                _fields.Add(field);
                fieldIndex++;
            }
        }
        #endregion

        #region 公共方法
        public string GetDetectString()
        {
            if (_contentGetDetectString == null) return string.Empty;
            try
            {
                var sb = new StringBuilder(2048);
                _contentGetDetectString(sb, sb.Capacity);
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public int GetSupportedFieldFlags(int fieldIndex)
        {
            return _contentGetSupportedFieldFlags?.Invoke(fieldIndex) ?? 0;
        }
        public int EditValue(IntPtr handle, int fieldIndex, int unitIndex, int fieldType, StringBuilder fieldValue, int maxLen, int flags, string langIdentifier)
        {
            return _contentEditValue?.Invoke(handle, fieldIndex, unitIndex, fieldType, fieldValue, maxLen, flags, langIdentifier) ?? WdxConstants.WDX_ERROR;
        }
        public void SendStateInformation(int state, string path)
        {
            if (_isUnicode)
            {
                _contentSendStateInformationW?.Invoke(state, path);
            }
            else
            {
                _contentSendStateInformation?.Invoke(state, path);
            }
        }

        /// <summary>
        /// 通过字段索引和单位索引获取值的变体类型
        /// 对应 Pascal 的 CallContentGetValueV(FileName, FieldIndex, UnitIndex, flags)
        /// </summary>
        public object? GetValueV(string fileName, int fieldIndex, int unitIndex = 0, int flag = 0)
        {
            if (!IsLoaded || fieldIndex < 0 || fieldIndex >= _fields.Count)
                return null;

            try
            {
                const int bufferSize = 2048;
                //StringBuilder valuePtr = new(bufferSize);
				var valuePtr = new byte[bufferSize];
				int result;
				string value = "";
                if (_isUnicode)
                {
                    result = _contentGetValueW(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, flag);
					value = Encoding.Unicode.GetString(valuePtr);
				}
                else
                {
                    result = _contentGetValue(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, flag);
					value = Encoding.ASCII.GetString(valuePtr);
				}
				int nullIndex = value.IndexOf('\0');
				if (nullIndex >= 0)
					value = value.Substring(0, nullIndex);
				if (result <= 0)
                    return null;

                // 根据字段类型处理返回值
                WdxField field = _fields[fieldIndex];
                //string value = valuePtr.ToString();
				//value = value.TrimEnd('\0'); // 去除字符串末尾的 null 字符
				switch (field.Type)
                {
                    case WdxConstants.FT_NUMERIC_32:
                        if (int.TryParse(value, out int intValue))
                            return intValue;
                        return 0;
                    case WdxConstants.FT_NUMERIC_64:
                        if (long.TryParse(value, out long longValue))
                            return longValue;
                        return 0L;
                    case WdxConstants.FT_NUMERIC_FLOATING:
                        if (double.TryParse(value, out double doubleValue))
                            return doubleValue;
                        return 0.0;
                    case WdxConstants.FT_BOOLEAN:
                        if (int.TryParse(value, out int boolValue))
                            return boolValue != 0;
                        return false;
                    case WdxConstants.FT_DATE:
                    case WdxConstants.FT_TIME:
                    case WdxConstants.FT_DATETIME:
                        //if (long.TryParse(value, out long fileTime))
                        //    return DateTime.FromFileTime(fileTime);

                        //return DateTime.MinValue;
						// 从缓冲区的前8个字节读取Int64（小端序）
						long fileTime = BitConverter.ToInt64(valuePtr, 0);
						return DateTime.FromFileTime(fileTime).ToString();
					case WdxConstants.FT_STRING:
                    case WdxConstants.FT_FULLTEXT:
                    case WdxConstants.FT_MULTIPLECHOICE:
                        return value;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"GetValueV error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 通过字段名和单位名获取值的变体类型
        /// 对应 Pascal 的 CallContentGetValueV(FileName, FieldName, UnitName, flags)
        /// </summary>
        public object? GetValueV(string fileName, string fieldName, string unitName, int flag = 0)
        {
            int fieldIndex = GetFieldIndex(fieldName);
            if (fieldIndex == -1)
                return null;

            int unitIndex = _fields[fieldIndex].GetUnitIndex(unitName);
            return GetValueV(fileName, fieldIndex, unitIndex, flag);
        }

        /// <summary>
        /// 通过字段索引和单位索引获取字符串值
        /// 对应 Pascal 的 CallContentGetValue(FileName, FieldIndex, UnitIndex, flags)
        /// </summary>
        public string GetValue(string fileName, int fieldIndex, int unitIndex = 0, int flag = 0)
        {
            if (!IsLoaded || fieldIndex < 0 || fieldIndex >= _fields.Count)
                return string.Empty;

            try
            {
                const int bufferSize = 2048;
				//StringBuilder valuePtr = new(bufferSize);
				byte[] valuePtr = new byte[bufferSize];
				int result;
				string value = "";
                if (_isUnicode)
                {
                    result = _contentGetValueW(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, flag);
					// 解码为字符串（UTF - 16 Little - Endian）
					value = Encoding.Unicode.GetString(valuePtr);
				}
                else
                {
                    result = _contentGetValue(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, flag);
					value = Encoding.ASCII.GetString(valuePtr);
				}
				// 找到第一个 null 终止符并截断
				int nullIndex = value.IndexOf('\0');
				if (nullIndex >= 0)
					value = value.Substring(0, nullIndex);
				// 检查返回值是否为有效字段类型（大于0）而不是只检查WDX_SUCCESS
				if (result > 0)
                {
                    // 根据字段类型处理返回值
                    WdxField field = _fields[fieldIndex];
                    //string value = valuePtr.ToString();

                    switch (field.Type)
                    {
                        case WdxConstants.FT_STRING:
                        case WdxConstants.FT_FULLTEXT:
                        case WdxConstants.FT_MULTIPLECHOICE:
                            return value;
                        case WdxConstants.FT_NUMERIC_32:
                            //if (int.TryParse(value, out int intValue))
                            //    return intValue.ToString();
                            //return "0";
							return BitConverter.ToInt32(valuePtr, 0).ToString();

						case WdxConstants.FT_NUMERIC_64:
							long longValue = BitConverter.ToInt64(valuePtr, 0);
                            return longValue.ToString();
                            
                        case WdxConstants.FT_NUMERIC_FLOATING:
                            //if (double.TryParse(value, out double doubleValue))
                            //    return doubleValue.ToString();
                            //return "0.0";
							return BitConverter.ToDouble(valuePtr, 0).ToString();
						case WdxConstants.FT_BOOLEAN:
                            if (int.TryParse(value, out int boolValue))
                                return boolValue != 0 ? "True" : "False";
                            return "False";

                        case WdxConstants.FT_DATE:
                        case WdxConstants.FT_TIME:
                        case WdxConstants.FT_DATETIME:
							// 从缓冲区的前8个字节读取Int64（小端序）
							long fileTime = BitConverter.ToInt64(valuePtr, 0);
							return DateTime.FromFileTime(fileTime).ToString();
					
                        default:
                            return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"GetValue error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 通过字段名和单位名获取字符串值
        /// 对应 Pascal 的 CallContentGetValue(FileName, FieldName, UnitName, flags)
        /// </summary>
        public string GetValue(string fileName, string fieldName, string unitName, int flag = 0)
        {
            int fieldIndex = GetFieldIndex(fieldName);
            if (fieldIndex == -1)
                return string.Empty;

            int unitIndex = _fields[fieldIndex].GetUnitIndex(unitName);
            return GetValue(fileName, fieldIndex, unitIndex, flag);
        }

        /// <summary>
        /// 特殊版本，用于处理全文字段，会更新 UnitIndex
        /// 对应 Pascal 的 CallContentGetValue(FileName, FieldIndex, var UnitIndex)
        /// </summary>
        public string GetValue(string fileName, int fieldIndex, ref int unitIndex)
        {
            if (!IsLoaded || fieldIndex < 0 || fieldIndex >= _fields.Count)
                return string.Empty;

            try
            {
                const int bufferSize = 2048;
				//StringBuilder valuePtr = new(bufferSize);
				var valuePtr = new byte[bufferSize];
				int result = 0;
				string value = "";
                if (_isUnicode && _contentGetValueW != null)
                {
                    result = _contentGetValueW(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, 0);
					value = Encoding.Unicode.GetString(valuePtr);
				}
                else if (_contentGetValue != null)
                {
                    result = _contentGetValue(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, 0);
					value = Encoding.ASCII.GetString(valuePtr);
				}

				//string value = valuePtr.ToString();
				int nullIndex = value.IndexOf('\0');
				if (nullIndex >= 0)
					value = value.Substring(0, nullIndex);
				switch (result)
                {
                    case WdxConstants.FT_FIELDEMPTY:
                        return string.Empty;
                    case WdxConstants.FT_FULLTEXT:
                        // 在 Pascal 中，这里会增加 UnitIndex
                        unitIndex += value.Length;
                        return value;
                    case WdxConstants.FT_FULLTEXTW:
                        // 在 Pascal 中，这里会增加 UnitIndex * sizeof(WideChar)
                        unitIndex += value.Length * 2; // Unicode 字符在 C# 中是 2 字节
                        return value;
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"GetValue error: {ex.Message}");
                return string.Empty;
            }
        }

        public void StopGetValue(string fileName)
        {
            if (_isUnicode)
            {
                _contentStopGetValueW?.Invoke(fileName);
            }
            else
            {
                _contentStopGetValue?.Invoke(fileName);
            }

        }

        public int GetDefaultSortOrder(int fieldIndex)
        {
            return _contentGetDefaultSortOrder?.Invoke(fieldIndex) ?? 1;
        }

        public bool SetValue(string fileName, int fieldIndex, int unitIndex, string value, IntPtr vptr, int vallen)
        {

            try
            {
                if (_isUnicode)
                {
					if (_contentSetValueW == null) return false;
					return _contentSetValueW(fileName, fieldIndex, unitIndex, vallen, vptr, 0) == WdxConstants.WDX_SUCCESS;
                }
                else
                {
					if (_contentSetValue == null) return false;
					return _contentSetValue(fileName, fieldIndex, unitIndex, value, 0) == WdxConstants.WDX_SUCCESS;
                }
            }
            catch
            {
                return false;
            }
        }

        public void LoadTranslations(string languageFile)
        {
            if (!File.Exists(languageFile)) return;

            try
            {
                _translations.Clear();
                var lines = File.ReadAllLines(languageFile);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        _translations[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                // 更新字段的本地化名称
                for (int i = 0; i < _fields.Count; i++)
                {
                    if (_translations.TryGetValue(_fields[i].Name, out string translation))
                    {
                        var field = _fields[i];
                        field.Description = translation;
                        _fields[i] = field;
                    }
                }
            }
            catch
            {
                // 处理异常
            }
        }
        #endregion

        #region 资源释放
        public override void UnloadModule()
        {
            if (ModuleHandle != IntPtr.Zero)
            {
                _contentPluginUnloading?.Invoke();
                NativeLibrary.Free(ModuleHandle);
                ModuleHandle = IntPtr.Zero;
            }

            // 清除所有函数指针
            _contentGetSupportedField = null;
            _contentGetValue = null;
            _contentGetValueW = null;
            _contentSetDefaultParams = null;
            _contentPluginUnloading = null;
            _contentStopGetValue = null;
            _contentGetDefaultSortOrder = null;
            _contentSetValue = null;

            // 清除新增函数指针
            _contentGetDetectString = null;
            _contentGetSupportedFieldFlags = null;
            _contentEditValue = null;
            _contentSendStateInformation = null;

            // 清除新增Unicode版本函数指针
            _contentStopGetValueW = null;
            _contentSetValueW = null;
            _contentSendStateInformationW = null;

            // 清除字段列表
            _fields.Clear();
        }

        //public void Dispose()
        //{
        //    UnloadModule();
        //    GC.SuppressFinalize(this);
        //}

        ~WdxModule()
        {
            Dispose();
        }
        #endregion
    }

    public class WdxModuleList : StringList, IDisposable
    {
        public List<WdxModule> _modules = new List<WdxModule>();
        private string _configPath;
        public Dictionary<string, WdxModule> _exts = [];
        public bool isConfigChanged;
        private List<string> _config;
        public Dictionary<string, string> _configDict;

        public WdxModuleList(string configPath)
        {
            _configPath = configPath;
            LoadConfiguration();
        }

        public WdxModule? FindModuleByName(string name)
        {
            return _modules.FirstOrDefault(m => m.Name != null && m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        //   public void LoadConfiguration()
        //   {
        //       _modules.Clear();
        //       _exts.Clear();
        //       _cfg = Helper.ReadSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins");
        //       foreach (var line in _cfg)
        //       {
        //           var parts = line.Split('=');
        //           if (parts.Length == 2)
        //           {
        //               var detectstring = parts[0].Trim().ToLower();
        //               var part1 = parts[1].Trim();
        //               var path = part1.Split(',')[^1];
        //               path = path.Replace("%COMMANDER_PATH%", Constants.ZfileBinPath);
        //if (File.Exists(path))
        //{
        //	var name = Path.GetFileNameWithoutExtension(path);
        //	//try to find module in wcxmodulelist by name
        //	var module = FindModuleByName(name);
        //	if (module == null)
        //	{
        //		module = new WdxModule(name, path);
        //		if (module.LoadModule())
        //		{
        //			if (!module.DetectStrings.Contains(detectstring))
        //			{
        //				module.DetectStrings.Add(detectstring);
        //			}
        //			if (AddModule(module))
        //				_exts[parts[0].Trim()] = module;
        //			//}
        //			//WcxModule wcxModule = WcxPlugins.LoadModule(plugin);
        //			//if (wcxModule != null)
        //			//{
        //			//int flags = module.PluginCapabilities;
        //			//foreach (string ext in detectstring.Split(','))
        //			//{
        //			//	//var result = Add(ext, flags, path);
        //			//	//FileName[result] = name; // GetPluginFilenameToSave(plugin);
        //			//}
        //		}
        //		else
        //			Debug.Print($"LOAD MODULE : {path} FAILED");
        //	}
        //	else
        //	{
        //		if (!module.DetectStrings.Contains(detectstring))
        //		{
        //			module.DetectStrings.Add(detectstring);
        //			_exts[parts[0].Trim()] = module;
        //			//var result = Add(detectstring, module.PluginCapabilities, path);
        //			//FileName[result] = name;
        //		}
        //	}
        //}
        //else
        //	Debug.Print($"FILE NOT FOUND : {path}");
        //           }
        //       }
        //       //先按照配置读取插件（优先级高），然后按照目录读取插件
        //       //LoadModulesFromDirectory(Constants.ZfileBinPath + "Plugins\\wdx\\");
        //   }
        public void LoadConfiguration()
        {
            Debug.Print("load wdx module list configuration");
            _modules.Clear();
            _config = Helper.ReadSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins");
            _configDict = Helper.ParseConfig(_config, "wdx");

            foreach (var line in _config)
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var detectstring = parts[0].Trim().ToLower();
                    var part1 = parts[1].Trim();
                    var path = part1.Split(',')[^1];
                    path = path.Replace("%COMMANDER_PATH%", Constants.ZfileBinPath);
                    AddModule(path);
                }
            }
        }
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
        public void SetAFileName(int index, string value)
        {
            SetValueFromIndex(index, GetAFlags(index) + "," + value);
        }
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
        public int Add(string ext, int flags, string fileName)
        {
            return AddObject(ext + "=" + flags + "," + fileName, true);
        }
        //public void SaveConfiguration()
        //{
        //    if (!isConfigChanged) return;
        //    Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins", _config);
        //    LoadConfiguration();
        //    isConfigChanged = false;
        //}
        public void SaveConfiguration()
        {
            if (!isConfigChanged) return;
            List<string> configContent = new();
            foreach (var pair in _configDict)
            {
                if (!configContent.Contains(pair.Key))
                    configContent.Append(pair.Key + "=" + pair.Value + Environment.NewLine);
                else
                {
                    configContent[configContent.IndexOf(pair.Key)] += $",{pair.Value}";
                }
            }
            Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins", configContent);
            LoadConfiguration();
            isConfigChanged = false;
        }
        public bool IsModuleSupported(WdxModule module, string fileName)
        {
            if (string.IsNullOrEmpty(module.DetectString))
            {
                //return false;
                if (_configDict.TryGetValue(module.Name.ToUpper(), out string val))
                    return isModuleSupport(val, fileName);
                else
                    return true;
            }

            return isModuleSupport(module.DetectString, fileName);
        }
        private bool isModuleSupport(string DetectString, string filename)
        {
            var p = new Dictionary<string, string>();
            var ext = Path.GetExtension(filename).ToLower().Trim('.');
            DetectString = DetectString.ToLower().Replace('"', '\'').Replace("[", $"'{ext.Reverse()}'["); //replace " with '
            var evaluator = new ExpressionEvaluatorClaude();
            p["ext"] = $"'{ext}'";
            p["size"] = "1";
            p["multimedia"] = ".true."; //temp ignore multimedia &
            p["force"] = ".false."; // temp ignore force |
            if (string.IsNullOrEmpty(DetectString))
                return true;
            return (bool)evaluator.EvalExpr(DetectString, p);
        }
        public bool AddModule(WdxModule module)
        {
            if (module.Name != null && !_modules.Any(m => m.Name != null && m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _modules.Add(module);
                return true;
            }
            return false;
        }
        public void AddModule(string modulePath)
        {
            if (_modules.Any(m => m.ModulePath.Equals(modulePath, StringComparison.OrdinalIgnoreCase)))
                return;

            var module = new WdxModule(modulePath);
            if (module.LoadModule())
            {
                _modules.Add(module);
                //SaveConfiguration();
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
                //SaveConfiguration();
            }
        }

        public WdxModule? FindModule(string pluginName)
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
