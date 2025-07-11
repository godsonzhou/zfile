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
		private static string SmartDetectString(byte[] buffer, out string encode)
		{
			// 先用GB2312解码
			string gbStr = Encoding.GetEncoding("gb2312").GetString(buffer).TrimEnd('\0');
			if (IsReadableSmart(gbStr)) {
				encode = "gb2312";
				return gbStr; 
			}

			// 再用Unicode解码
			string unicodeStr = Encoding.Unicode.GetString(buffer).TrimEnd('\0');
			if (IsReadableSmart(unicodeStr)) { encode = "unicode";  return unicodeStr; }

			// 最后尝试ASCII
			string asciiStr = Encoding.ASCII.GetString(buffer).TrimEnd('\0');
			encode = "ascii";
			return asciiStr;
		}

		// 更严格的可读性判断
		private static bool IsReadableSmart(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return false;
			// 包含中文、英文、数字
			if (str.Any(c => c >= 0x4e00 && c <= 0x9fa5)) return true; // 中文
			if (str.Any(c => char.IsLetterOrDigit(c))) return true;    // 英文/数字
																	   // 乱码常见特征：大量不可识别字符
			int badCharCount = str.Count(c => c == '�' || c == '\ufffd');
			if (badCharCount > str.Length / 4) return false;
			// 绝大多数为可见字符
			int visible = str.Count(c => c >= 0x20 && c < 0x7F || c > 0x80);
			return visible > str.Length / 2;
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
					//value = Encoding.Unicode.GetString(valuePtr);
				}
                else
                {
                    result = _contentGetValue(fileName, fieldIndex, unitIndex, valuePtr, bufferSize, flag);
					//value = Encoding.ASCII.GetString(valuePtr);
				}
				// 找到第一个 null 终止符并截断
				//int nullIndex = value.IndexOf('\0');
				//if (nullIndex >= 0)
				//	value = value.Substring(0, nullIndex);
				// 检查返回值是否为有效字段类型（大于0）而不是只检查WDX_SUCCESS
				if (result > 0)
                {
                    // 根据字段类型处理返回值
                    WdxField field = _fields[fieldIndex];
					//string value = valuePtr.ToString();
					var fieldname = field.Name;
					if (fieldname == "Created")
						//created 日期特殊处理，因为它的类型是ft_string，
						// 但实际存储的是日期时间格式的数据
						field.Type = WdxConstants.FT_DATETIME;
					switch (field.Type)
                    {
                        case WdxConstants.FT_STRING:
                        case WdxConstants.FT_FULLTEXT:
							// 获取GB2312编码实例
							Encoding encoding = Encoding.GetEncoding("gb2312");
							var resultstrgb2312 = encoding.GetString(valuePtr).TrimEnd('\0');
							encoding = Encoding.Unicode;
							//var encoding = Helper.SmartDetectEncoding(valuePtr);
							var resultstrunicode =  encoding.GetString(valuePtr).TrimEnd('\0');
							var resultstrsmart = SmartDetectString(valuePtr, out var encodestr);
							var RESULTASCII = Encoding.ASCII.GetString(valuePtr).TrimEnd('\0');

							Debug.WriteLine($"ISUNICODE: {IsUnicode}, GetValue(string/fulltext): {fieldname} gb2312: {resultstrgb2312}, unicode: {resultstrunicode}, ASCII: {RESULTASCII}, autodetect => [{encodestr}/{resultstrsmart}]");
							
							return resultstrsmart; // 返回Unicode字符串

						case WdxConstants.FT_STRINGW:
						case WdxConstants.FT_FULLTEXTW:
							// Unicode 字符串返回// 解码为字符串（UTF - 16 Little - Endian）
							var resultunicode =  Encoding.Unicode.GetString(valuePtr).TrimEnd('\0');
							Debug.Print($"GetValue(stringw/fulltextw): {fieldname} - unicode: {resultunicode}");
							return resultunicode;
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
                            //if (int.TryParse(value, out int boolValue))
                            //    return boolValue != 0 ? "True" : "False";
                            //return "False";
							bool bl = BitConverter.ToBoolean(valuePtr, 0);
							return bl ? "True" : "False";
						case WdxConstants.FT_DATE:
                        case WdxConstants.FT_TIME:
                        case WdxConstants.FT_DATETIME:
							// 从缓冲区的前8个字节读取Int64（小端序）
							long fileTime = BitConverter.ToInt64(valuePtr, 0);
							return DateTime.FromFileTime(fileTime).ToString();

						case WdxConstants.FT_MULTIPLECHOICE:
						default:
                            //return value;
							return Encoding.ASCII.GetString(valuePtr).TrimEnd('\0');		//default 使用ASCII编码string模式
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
		public Dictionary<string, string> _pathDict;
		public (Dictionary<string, string> , Dictionary<string, string> ) _wdxDicts;
		public WdxModuleList()
		{
			_configPath = Constants.ZfileCfgPath + "wincmd.ini";
			LoadConfiguration();
		}

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
            _configDict = Helper.ParseConfig(_config, out var pathdict, out var wdxdicts, "wdx");
			_wdxDicts = wdxdicts;
			_pathDict = pathdict;
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
			var i = 0;
            foreach (var pair in _configDict)
            {
				//if (!configContent.Contains(pair.Key))
				//    configContent.Append(pair.Key + "=" + pair.Value + Environment.NewLine);
				//else
				//{
				//    configContent[configContent.IndexOf(pair.Key)] += $",{pair.Value}";
				//}
				configContent.Add($"{i}={_pathDict[pair.Key]}");
				if(!string.IsNullOrEmpty(pair.Value))
					configContent.Add($"{i}_detect={pair.Value}");
				if (_wdxDicts.Item1.TryGetValue(pair.Key, out var date))
					configContent.Add($"{i}_date={date}");
				if (_wdxDicts.Item2.TryGetValue(pair.Key, out var flags))
					configContent.Add($"{i}_flags={flags}");
				i++;
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
/*
 * the official WDX plugin interface documentation attached as below:
 * =================================================================
 * ContentSetDefaultParams
 
ContentSetDefaultParams is called immediately after loading the DLL.
 
Declaration:
 
void __stdcall ContentSetDefaultParams(ContentDefaultParamStruct* dps);
 
Description of parameters:
 
dps This structure of type ContentDefaultParamStruct currently contains the version number of the plugin interface, and the suggested location for the settings file (ini file). It is recommended to store any plugin-specific information either directly in that file, or in that directory under a different name. Make sure to use a unique header when storing data in this file, because it is shared by other file system plugins! If your plugin needs more than 1kbyte of data, you should use your own ini file because ini files are limited to 64k.
 
Return value:
 
The function has no return value:
 
Note:
 
Since this is a new plugin interface, this function will be called in all versions of Total Commander supporting content plugins.
 * ==================================================================
 ContentCompareFiles
 
ContentCompareFiles is called in Synchronize dirs to compare two files by content, e.g. two text files with different line breaks, one Windows (CRLF) and one Unix (LF only).
 
Declaration:
 
int __stdcall ContentCompareFiles(PROGRESSCALLBACKPROC progresscallback, int compareindex, TCHAR* filename1, TCHAR* filename2, FileDetailsStruct* filedetails);
 
Description of parameters:
 
progresscallback Callback function to inform the calling program about the compare progress
If progresscallback returned a value other than zero (0), the user pressed the Abort/Cancel button and the comparison MUST be aborted with return value -2.
You should call this message only every 100-200 milliseconds or so, because callbacks are slow
compareindex The value returned by ContentGetSupportedField for this compare field (starting with 10000)
filename1 The first name to be compared
filename2 The second name to be compated
filedetails A structure which informs the plugin about the size, timestamp and attributes of both files
 
Return value:
 
1 The two files are equal, show equal sign in list
2 The two files are equal, show equal sign with 'TXT' below it in list
0 The two files are different
-1 Could not open at least one of the files
-2 Compare aborted
-3 The file cannot be compared with this function, please continue with the next plugin. Example: A plugin which compares two Word files by contents (even if the meta data is different) needs to return -3 if at least one of the two files isn't a Word file
100 or higher The two files are equal. The plugin file contains a 16x16 icon resource with this numeric ID, to be loaded via
LoadImage(DllHandle,MAKEINTRESOURCE(id),IMAGE_ICON,16,16,LR_SHARED);
which will be shown between the two files. The icon must contain a 16 color image, and may also contain higher color images. Please test your icon at least with black on white and white on black window settings (Control panel - Display).
There are two types of icons you can use:
1: If the value is <10000, the icon will be drawn in addition to the equal sign (which will be in black or white, depending on the color of the background). The upper 8 pixels (plus 1 pixel optional border) may be used for drawing the additional image or text.
2: If the value is >=10000, no equal sign will be drawn by TC, so the plugin can use the entire 16x16 space for drawing.
 
Remarks:
 
- Do not return 0 (files different) if the two files are binary identical (containing exactly the same data) just because the plugin cannot compare them - this would break the normal compare function. Return -3 instead.
- It's recommended to make a binary comparison first if the plugin is for a broad range of files, so the user may use *.* as the file type. Example_ See our "filesys" sample content plugin (text comparison), which makes a binary comparison until it finds the first difference, and then continues with using its text comparison method.

===================================================================== 
ContentEditValue
 
ContentEditValue allows a plugin to implement a custom input dialog to enter special values like date and time. This function is called in change attributes if you returned the flag contflags_fieldedit in ContentGetSupportedFieldFlags for a field. 
 
Declaration:
 
int __stdcall ContentEditValue(HWND ParentWin, int FieldIndex, int UnitIndex, int FieldType, void* FieldValue, int maxlen, int flags, char* langidentifier);
 
Description of parameters:
 
ParentWin The parent window handle for the dialog.
 
FieldIndex The index of the field for which the content editor is called. This is the same index as the FieldIndex value in ContentGetSupportedField.
 
UnitIndex The index of the unit used. Example:
If the plugin returned the following unit string in ContentGetSupportedField:
bytes|kbytes|Mbytes
Then a UnitIndex of 0 would mean bytes, 1 means kbytes and 2 means MBytes
If no unit string was returned, UnitIndex is 0.
 
FieldType The type of data passed to the plugin in FieldValue. This is the same type as returned by the plugin via ContentGetSupportedField.
 
FieldValue Here the plugin receives the data to be edited, and returns the result back to the caller. The data format depends on the field type:
ft_numeric_32: FieldValue points to a 32-bit signed integer variable.
ft_numeric_64: FieldValue points to a 64-bit signed integer variable.
ft_numeric_floating: FieldValue points to a 64-bit floating point variable (ISO standard double precision)
ft_date: FieldValue points to a structure containing year,month,day as 2 byte values.
ft_time: FieldValue points to a structure containing hour,minute,second as 2 byte values.
ft_boolean: Currently unsupported.
ft_string: FieldValue is a pointer to a 0-terminated string.
ft_multiplechoice: Currently unsupported.
ft_fulltext: Currently unsupported.
ft_fulltextw: Currently unsupported.
ft_datetime: A timestamp of type FILETIME, as returned e.g. by FindFirstFile(). It is a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601. The time MUST be relative to universal time (Greenwich mean time) as returned by the file system, not local time!
 
maxlen The maximum number of bytes fitting into the FieldValue variable.
 
flags Currently the following flags are defined:
editflags_initialize: The data passed in via FieldValue should be used to initialize the dialog.
 
langidentifier A 1-3 character language identifier, the same as the last part of the language file name used. Example: The German language file is called wcmd_deu.lng, so the langidentifier is "deu". May be used to translate the dialog.
 
Return value:
 
ft_setsuccess User confirmed dialog with OK, and the data is valid
ft_nosuchfield The given field index was invalid
ft_setcancel The user clicked on cancel
 
Remarks:
 
Total Commander already implements an internal editor for the fields ft_date, ft_time, and ft_datetime. You can override it with your own by specifying the  contflags_fieldedit flag for your date/time fields. Fields of type boolean and multiple choice do not currently support a field editor.
 *===================================================================
 ContentGetDefaultSortOrder
 
ContentGetDefaultSortOrder is called when the user clicks on the sorting header above the columns.
 
Declaration:
 
int __stdcall ContentGetDefaultSortOrder(int FieldIndex);
 
Description of parameters:
 
FieldIndex The index of the field for which the sort order should be returned.
 
Return value:
 
Return 1 for ascending (a..z, 1..9), or -1 for descending (z..a, 9..0).
 
Note:
 
You may implement this function if there are fields which are usually sorted in descending order, like the size field (largest file first) or the date/time fields (newest first). If the function isn't implemented, ascending will be the default.
 ====================================================================
 ContentGetDetectString
 
ContentGetDetectString is called when the plugin is loaded for the first time. It should return a parse function which allows Total Commander to find out whether your plugin can probably handle the file or not. You can use this as a first test - more thorough tests may be performed in ContentGetValue(). It's very important to define a good test string, especially when there are dozens of plugins loaded! The test string allows Total Commander to call only those plugins relevant for that specific file type.
 
Declaration:
 
int __stdcall ContentGetDetectString(char* DetectString,int maxlen);
 
Description of parameters:
 
DetectString Return the detection string here. See remarks for the syntax.
 
maxlen Maximum length, in bytes, of the detection string (currently 2k).
 
Return value:
 
The return value is unused and should be set to 0. You can also declare the function as "void __stdcall".
 
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
 
New in TC9:
Plugin detect string now supports wildcards ? (one char) and * (any number)
Examples:
ext="JP?" -> JPG, JPE
ext="JP*" -> the same but also JPEG
 ==================================================================
 * ContentGetSupportedField
 
ContentGetSupportedField is called to enumerate all supported fields. FieldIndex is increased by 1 starting from 0 until the plugin returns ft_nomorefields.
 
Declaration:
 
int __stdcall ContentGetSupportedField(int FieldIndex, char* FieldName, char* Units, int maxlen);
 
Description of parameters:
 
FieldIndex The index of the field for which TC requests information. Starting with 0, the FieldIndex is increased until the plugin returns an error.
 
FieldName Here the plugin has to return the name of the field with index FieldIndex. The field may not contain the following chars: . (dot) | (vertical line) : (colon). You may return a maximum of maxlen characters, including the trailing 0.
 
Units When a field supports several units like bytes, kbytes, Mbytes etc, they need to be specified here in the following form: bytes|kbytes|Mbytes . The separator is the vertical dash (Alt+0124). As field names, unit names may not contain a vertical dash, a dot, or a colon. You may return a maximum of maxlen characters, including the trailing 0.
If the field type is ft_multiplechoice, the plugin needs to return all possible values here. Example: The field "File Type" of the built-in content plugin can have the values "File", "Folder" and "Reparse point". The available choices need to be returned in the following form: File|Folder|Reparse point . The same separator is used as for Units. You may return a maximum of maxlen characters, including the trailing 0. The field type ft_multiplechoice does NOT support any units.
 
maxlen The maximum number of characters, including the trailing 0, which may be returned in each of the fields.
 
Return value:
 
The function needs to return one of the following values:
ft_nomorefields The FieldIndex is beyond the last available field.
ft_numeric_32 A 32-bit signed number
ft_numeric_64 A 64-bit signed number, e.g. for file sizes
ft_numeric_floating A double precision floating point number
ft_date A date value (year, month, day)
ft_time A time value (hour, minute, second). Date and time are in local time.
ft_boolean A true/false value
ft_multiplechoice A value allowing a limited number of choices. Use the Units field to return all possible values.
ft_string A text string. Values returned by ContentGetValue may be of type ft_stringw or ft_string.
ft_fulltext A full text (multiple text strings), only used for searching. Can be used e.g. for searching in the text portion of binary files, where the plugin makes the necessary translations. All fields of this type MUST be placed at the END of the field list, otherwise you will get errors in Total Commander!
ft_fulltextw New in 2.11: Same as ft_fulltext, but with UTF-16 encoding. You can report ft_fulltext in ContentGetSupportedField, and then either ft_fulltext or ft_fulltextw in ContentGetValue, depending on the file.
ft_datetime A timestamp of type FILETIME, as returned e.g. by FindFirstFile(). It is a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601. The time MUST be relative to universal time (Greenwich mean time) as returned by the file system, not local time!
ft_comparecontent: This type can be returned only for FieldIndex >= 10'000! It is used in "Synchronize dirs" only (see notes below). Requires plugin version>=2.10.
 
 
Remarks:
 
Please note that fields of type ft_fulltext only show up in the search function, not in the multi-rename tool or the file lists. All fields of this type MUST be placed at the END of the field list, otherwise you will get errors in Total Commander! This is necessary because these fields will be removed from field lists e.g. in the "configure custom column view" dialog. You should use the ft_string type for shorter one line texts suitable for displaying in file lists and for renaming.
 
Note about ft_comparecontent (New with TC 7.51, plugin interface version >=2.10): If ContentCompareFiles is exported by the plugin, Total Commander calls ContentGetSupportedField starting with index 10000 from "Synchronize dirs". "FieldName" must be filled with the name of the compare function, e.g. "Compare as text (ignore line breaks)", and the return value must be ft_comparecontent. The Units field is ignored. When comparing two files, Total Commander calls ContentCompareFiles with the FieldIndex value from this function (10000 for the first compare function, 10001 for the second etc).

 ====================================================================
 * ContentGetSupportedFieldFlags
 
ContentGetSupportedFieldFlags is called to get various information about a plugin variable. It's first called with FieldIndex=-1 to find out whether the plugin supports any special flags at all, and then for each field separately.
 
Declaration:
 
int __stdcall ContentGetSupportedFieldFlags(int FieldIndex);
 
Description of parameters:
 
FieldIndex The index of the field for which flags should be returned.
-1: Return a combination (or) of all supported flags, e.g. contflags_edit | contflags_substmask
>=0: Return the field-specific flags
 
Return value:
 
The function needs to return a combination of the following flags:
 
contflags_edit The plugin allows to edit (modify) this field via Files - Change attributes. This should only be returned for fields where it makes sense, e.g. a file date.
 
Only ONE of the following flags: (See description and example under "Note").
contflags_substsize use the file size
contflags_substdatetime use the file date+time (ft_datetime)
contflags_substdate use the file date (fd_date)
contflags_substtime use the file time (fd_time)
contflags_substattributes use the file attributes (numeric)
contflags_substattributestr use the file attribute string in form -a--
 
contflags_passthrough_size_float pass the size as ft_numeric_floating to ContentGetValue. The plugin will then apply the correct units, and return the formatted display string in the additional string field.
contflags_substmask A combination of all above substitution flags. Should be returned for index -1 if the content plugin contains ANY of the substituted fields.
contflags_fieldedit If set, TC will show a button >> in change attributes which lets the user call the function ContentEditValue. This allows plugins to have their own field editors, like the custom editor for tc.comments or tc.*date/time fields.
contflags_fieldsearch Pass the search string to the plugin via ContentFindValue instead of calling ContentGetValue and comparing the results. New in Total Commander 10.
contflags_searchpageonly Only show this field in the search dialog (also for color filters etc.), but not in the multi-rename tool or for custom columns. Useful for fields which require a parameter, like the field [tc.partner file with other extension] which can be used to find all jpg files which have a raw file companion. This field requires the extension of the companion as a parameter.
 
Note:
 
Returning one of the  contflags_subst* flags instructs Total Commander to replace (substitute) the returned variable by the indicated default internal value if no plugin variable can be retrieved. Example: Content plugins do not work on FTP servers. A field which shows the size of files and directories should be replaced by the size of the FTP files in this case, so return contflags_substsize. Alternatvely, you can also return contflags_passthrough_size_float - then Total Commander will call ContentGetValue and pass the size as ft_numeric_floating to your plugin, so you can format the display string yourself, and apply custom units.
 * ===================================================================
 * 
 * ContentGetValue
 
ContentGetValue is called to retrieve the value of a specific field for a given file, e.g. the date field of a file.
 
Declaration:
 
int __stdcall ContentGetValue(char* FileName, int FieldIndex, int UnitIndex, void* FieldValue, int maxlen, int flags);
 
Description of parameters:
 
FileName The name of the file for which the plugin needs to return the field data.
 
FieldIndex The index of the field for which the content has to be returned. This is the same index as the FieldIndex value in ContentGetSupportedField.
 
UnitIndex The index of the unit used. Example:
If the plugin returned the following unit string in ContentGetSupportedField:
bytes|kbytes|Mbytes
Then a UnitIndex of 0 would mean bytes, 1 means kbytes and 2 means MBytes
If no unit string was returned, UnitIndex is 0.
For ft_fulltext, UnitIndex contains the offset of the data to be read.
 
FieldValue Here the plugin needs to return the requested data. The data format depends on the field type:
ft_numeric_32: FieldValue points to a 32-bit signed integer variable.
ft_numeric_64: FieldValue points to a 64-bit signed integer variable.
ft_numeric_floating: FieldValue points to a 64-bit floating point variable (ISO standard double precision)
See remark below about additional string field!
ft_date: FieldValue points to a structure containing year,month,day as 2 byte values.
ft_time: FieldValue points to a structure containing hour,minute,second as 2 byte values.
ft_boolean: FieldValue points to a 32-bit number. 0 means false, anything else means true.
ft_string: FieldValue is a pointer to a 0-terminated string.
ft_stringw: FieldValue is a pointer to a 0-terminated wide string.
ft_fulltext: Read maxlen bytes of interpreted data starting at offset UnitIndex. The data must be a 0 terminated string.
ft_fulltextw: Same as ft_fulltext, size in bytes, not characters
ft_multiplechoice: FieldValue is a pointer to a 0-terminated ANSI string.
ft_datetime: A timestamp of type FILETIME, as returned e.g. by FindFirstFile(). It is a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601. The time MUST be relative to universal time (Greenwich mean time) as returned by the file system, not local time!
ft_delayed, ft_ondemand: You may return a zero-terminated string as in ft_string, which will be shown until the actual value has been extracted. Requires plugin version>=1.4.
 
 
maxlen The maximum number of bytes fitting into the FieldValue variable. Note: When using Unicode strings, you need to divide this value by 2 to get the maximum number of characters!
 
flags Currently only two flags are defined:
CONTENT_DELAYIFSLOW: If this flag is set, the plugin should return ft_delayed for fields which take a long time to extract, like file version information. Total Commander will then call the function again in a background thread without the CONTENT_DELAYIFSLOW flag. This means that your plugin must be implemented thread-safe if you plan to return ft_delayed.
The plugin may also return ft_ondemand if CONTENT_DELAYIFSLOW is set. In this case, the field will only be retrieved when the user presses <SPACEBAR>. This is only recommended for fields which take a VERY long time, e.g. directory content size. You should offer the same field twice in this case, once as delayed, and once as on demand. The field will be retrieved in the background thread also in this case.
CONTENT_PASSTHROUGH: If this flag is set, the FieldValue passes the file size to the plugin as ft_numeric_floating. This value is only set if you have returned the flag contflags_passthrough_size_float from the function ContentGetSupportedFieldFlags. No units have been applied yet, the size is passed to the plugin as bytes. You then need to apply the appropriate unit, and set the additional string field. This option is used to display the size even in locations where the plugin doesn't work, e.g. on ftp connections or inside archives.
 
Return value:
 
Return the field type in case of success, or one of the following error values otherwise:
ft_nosuchfield The given FieldIndex is invalid
ft_fileerror Error accessing the specified file FileName
ft_fieldempty The file does not contain the specified field
ft_delayed The extraction of the field would take a long time, so Total Commander should request it again in a background thread. This error may only be returned if the flag CONTENT_DELAYIFSLOW was set, and if the plugin is thread-safe.
ft_ondemand The extraction of the field would take a very long time, so it should only be retrieved when the user presses the space bar. This error may only be returned if the flag CONTENT_DELAYIFSLOW was set, and if the plugin is thread-safe.
 
Remarks:
 
ft_fulltext handling is a bit special. It is only used for searching in interpreted file contents, e.g. for finding text in binary files. For example, the ID3 plugin uses ft_fulltext to allow the user to search for a string in ALL header fields.
Calls work like this:
First, ContentGetValue is called with UnitIndex set to 0. The plugin then parses the file data, and (if necessary) keeps it in a cache. It writes the first block of maxlen-1 bytes to FieldValue and returns ft_fulltext. The data written must be a 0-terminated string! Total Commander then searches in the block, and requests the next block with offset maxlen-1, etc. Once there is no more data, the plugin needs to return ft_fieldempty. If there is a match, TC signals the plugin that it can delete the cached data by calling ContentGetValue with UnitIndex set to -1! The return value should be ft_fieldempty in this case. This call with UnitIndex=-1 does not happen when the plugin terminates the search with ft_fieldempty because it reached the end of the file.
 
Total Commander now accepts that ContentGetValue returns a different data type than ContentGetSupportedField for the same field, e.g. a string "no value" instead of a numeric field. Note that older versions of Total Commander crashed in the search function in this case, so if you want to do this, you MUST check that the plugin version is reported as >=1.3 (hi=1, low>=3 or hi>=2).
 
ft_numeric_floating (New with TC 6.52, plugin interface version >=1.4): You can now put a 0-terminated string immediately behind the 64bit floating point variable, which will then be shown instead in file lists. This is useful if the conversion precision used by TC isn't appropriate for your variables. The numeric variable will still be used for sorting and searching. If the string is empty, TC will ignore it (it is set to 0 before calling this function, so the function will remain backwards-compatible). Example: The numeric value is 0.000002. You can return this value as a 64-bit variable, and the string you find most appropriate, e.g. "2*10^-6" or "0.000002".
 
 
Note about Unicode: ft_delayed and ft_ondemand fields and alternate text for ft_numeric_floating must be UTF-16 Unicode when using the wide function ContentGetValueW.
 
About caching the data: Total Commander will not call a mix ContentGetValue for different files, it will only call it for the next file when the previous file can be closed. Therefore a single cache per running Total Commander would be sufficient. However, there may be other calls to ContentGetValue with requests to other fields in the background, e.g. for displaying result lists. There may also be multiple instances of Total Commander at the same time, so if you use a TEMP file for storing the cached data, make sure to give it a unique name (e.g. via GetTempFileName).
 ========================================

ContentPluginUnloading
 
ContentPluginUnloading is called just before the plugin is unloaded, e.g. to close buffers, abort operations etc.
 
Declaration:
 
void __stdcall ContentPluginUnloading(void);
 
Description of parameters:
 
There are no parameters.
 
Return value:
 
There is no return value.
 
Note:
 
This function was added by request from a user who needs to unload GDI+. It seems that GDI+ has a bug which makes it crash when unloading it in the DLL unload function, therefore a separate unload function is needed.
============================================

ContentSendStateInformation
 
ContentSendStateInformation is called to inform the plugin about a state change.
 
Declaration:
 
void __stdcall ContentSendStateInformation(int state,char* path);
 
Description of parameters:
 
state The state which has changed. The following states are defined:
contst_readnewdir: It is called when TC reads one of the file lists.
contst_refreshpressed: The user has pressed F2 or Ctrl+R to force a reload.
contst_showhint: A tooltip/hint window is shown for the current file.
 
path Current path. In case of contst_showhint, this is the path to the file, otherwise to the current directory.
 
Return value:
 
This function has no return value.
 
Remarks:
 
- This function may be used to clear a directory cache when called with parameter contst_refreshpressed or with contst_readnewdir, depending on the needs of the plugin.
- When the user presses F2 or Ctrl+R in custom columns view or thumbnails view, ContentSendStateInformation is called first with parameter contst_refreshpressed, then with contst_readnewdir.
- When the user changes to a different directory in custom columns view or thumbnails view, ContentSendStateInformation is called only with parameter contst_readnewdir.
- When the user switches from full view to custom columns view or thumbnails view, ContentSendStateInformation is not called at all!
- Do not ignore the state parameter, there may be more parameters added in future versions! 
==============================================

ContentSetValue
 
ContentSetValue is called to set the value of a specific field for a given file, e.g. to change the date field of a file.
 
Declaration:
 
int __stdcall ContentSetValue(char* FileName, int FieldIndex, int UnitIndex, int FieldType, void* FieldValue, int flags);
 
Description of parameters:
 
FileName The name of the file for which the plugin needs to change the field data.
This is set to NULL to indicate the end of change attributes (see remarks below).
 
FieldIndex The index of the field for which the content has to be returned. This is the same index as the FieldIndex value in ContentGetSupportedField. This is set to -1 to signal the end of change attributes (see remarks below).
 
UnitIndex The index of the unit used. Example:
If the plugin returned the following unit string in ContentGetSupportedField:
bytes|kbytes|Mbytes
Then a UnitIndex of 0 would mean bytes, 1 means kbytes and 2 means MBytes
If no unit string was returned, UnitIndex is 0.
ft_fulltext is currently unsupported.
 
FieldType The type of data passed to the plugin in FieldValue. This is the same type as returned by the plugin via ContentGetSupportedField. If the plugin returned a different type via ContentGetValue, the the FieldType _may_ be of that type too.
 
FieldValue Here the plugin receives the data to be changed. The data format depends on the field type:
ft_numeric_32: FieldValue points to a 32-bit signed integer variable.
ft_numeric_64: FieldValue points to a 64-bit signed integer variable.
ft_numeric_floating: FieldValue points to a 64-bit floating point variable (ISO standard double precision)
ft_date: FieldValue points to a structure containing year,month,day as 2 byte values.
ft_time: FieldValue points to a structure containing hour,minute,second as 2 byte values.
ft_boolean: FieldValue points to a 32-bit number. 0 neans false, anything else means true.
ft_string or ft_multiplechoice: FieldValue is a pointer to a 0-terminated string.
ft_fulltext: Currently unsupported.
ft_datetime: A timestamp of type FILETIME, as returned e.g. by FindFirstFile(). It is a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601. The time MUST be relative to universal time (Greenwich mean time) as returned by the file system, not local time!
ft_delayed, ft_ondemand: You may return a zero-terminated string as in ft_string, which will be shown until the actual value has been extracted. Requires plugin version>=1.4.
 
flags Currently the following flags are defined:
setflags_first_attribute: This is the first attribute to be set for this file via this plugin. May be used for optimization.
setflags_last_attribute: This is the last attribute to be set for this file via this plugin.
setflags_only_date: For field type ft_datetime only: User has only entered a date, don't change the time
 
Return value:
 
ft_setsuccess Change was successful
ft_fileerror Error accessing the specified file FileName, or cannot set the given value
ft_nosuchfield The given field index was invalid
 
Remarks:
 
About caching the data: Total Commander will not call a mix ContentSetValue for different files, it will only call it for the next file when the previous file can be closed. Therefore a single cache per running Total Commander should be sufficient.
 
About the flags: If the flags setflags_first_attribute and setflags_last_attribute are both set, then this is the only attribute of this plugin which is changed for this file.
 
FileName is set to NULL and FieldIndex to -1 to signal to the plugin that the change attributes operation has ended. This can be used to flush unsaved data to disk, e.g. when setting comments for multiple files.
=============================================

ContentStopGetValue
 
ContentStopGetValue is called to tell a plugin that a directory change has occurred, and the plugin should stop loading a value.
 
Declaration:
 
void __stdcall ContentStopGetValue(char* FileName);
 
Description of parameters:
 
FileName The name of the file for which ContentGetValue is currently being called.
 
Return value:
 
The function has no return value.
 
Note:
 
This function only needs to be implemented when handling very slow fields, e.g. the calculation of the total size of all files in a directory. It will be called only while a call to ContentGetValue is active in a background thread.
A plugin could handle this mechanism like this:
1. When ContentGetValue is called, set a variable GetAborted to false
2. When ContentStopGetValue is called, set GetAborted to true
3. Check GetAborted during the lengthy operation, and if it becomes true, return ft_fieldempty
===========================================

FileDetailsStruct
 
FileDetailsStruct is passed to ContentCompareFiles to inform the plugin about the file details of the left and right file.
 
Declaration:
 
typedef struct {
__int64 filesize1;
__int64 filesize2;
FILETIME filetime1;
FILETIME filetime2;
DWORD attr1;
DWORD attr2;
} FileDetailsStruct;
 
Description of struct members:
 
filesize1,filesize2 The size of the first/second file. If your compiler doesn't support 64-bit numbers, you can split these into 4 DWORD numbers: filesize1lo,filesize1hi,filesize2lo,filesize2hi;
 
filetime1,filetime2 The last modification time stamp of the files, in standard Windows format
 
attr1,attr2 File attributes of the two files. Besides the standard attributes archive, read only, hidden and system, only NTFS-compressed and NTFS-encrypted may be set.
========================================

pdateformat
 
pdateformat can be used for setting the date value in ContentGetValue.
 
Declaration:
 
typedef struct {
    WORD wYear;
    WORD wMonth;
    WORD wDay;
} *pdateformat;
 
Example usage:
 
int __stdcall ContentGetValue(char* FileName,int FieldIndex,int UnitIndex,void* FieldValue,int maxlen,int flags)
{
    WORD Year,Month,Day;
    if (FieldIndex==index_of_date_field) {
        GetFileDate(FileName,&Year,&Month,&Day);
        ((pdateformat)FieldValue)->wYear=Year;
        ((pdateformat)FieldValue)->wMonth=Month;
        ((pdateformat)FieldValue)->wDay=Day;
        return ft_date;
    }
} 
===========================================
ptimeformat
 
ptimeformat can be used for setting the time value in ContentGetValue.
 
Declaration:
 
typedef struct {
    WORD wHour;
    WORD wMinute;
    WORD wSecond;
} *ptimeformat;
 
Example usage:
 
int __stdcall ContentGetValue(char* FileName,int FieldIndex,int UnitIndex,void* FieldValue,int maxlen,int flags)
{
    WORD Hour,Min,Sec;
    if (FieldIndex==index_of_time_field) {
        GetFileTime(FileName,&Year,&Month,&Day);
        ((ptimeformat)FieldValue)->wHour=Hour;
        ((ptimeformat)FieldValue)->wMinute=Min;
        ((ptimeformat)FieldValue)->wSecond=Sec;
        return ft_time;
    }
} 

===========================================
' PowerBasic for Windows
 
' © 2005, Alexander Asyabrik aka Shura
 
#Compile Dll
#Include "WIN32API.INC"
 
%ft_nomorefields = 0
%ft_numeric_32 = 1
%ft_numeric_64 = 2
%ft_numeric_floating = 3
%ft_date = 4
%ft_time = 5
%ft_boolean = 6
%ft_multiplechoice = 7
%ft_string = 8
%ft_fulltext = 9
%ft_datetime = 10
%ft_stringw = 11
%ft_fulltextw = 12
 
' >> for ContentGetValue
%ft_nosuchfield = -1    'error, invalid field number given
%ft_fileerror = -2      'file i/o error
%ft_fieldempty = -3     'field valid, but Empty
%ft_ondemand = -4       'field will be retrieved only when User presses <SPACEBAR>
%ft_delayed = 0         'field takes a Long time To extract -> Try again In background
' >> for ContentFindValue:
%ft_found = 1           'value was found
%ft_notfound = -8       'value was NOT found
 
%CONTENT_DELAYIFSLOW=1   'ContentGetValue called in foreground
 
 
Type TimeFormat
    wHour As Word
    wMinute As Word
    wSecond As Word
End Type
 
Type DateFormat
    wYear As Word
    wMonth As Word
    wDay As Word
End Type
 
%MaxStr=2048 ' Maximum length of your string data, can be smaller
 
Union uFieldValue
  fvNumeric_32  As Long                ' ft_numeric_32
  fvNumeric_64 As Quad                 ' ft_numeric_64
  fvNumeric_floating As Double         ' ft_numeric_floating
  fvDate As DateFormat                 ' ft_date
  fvTime As TimeFormat                 ' ft_time
  fvBoolean As Long                    ' ft_boolean
  fvMultiplechoice As Asciiz * %MaxStr ' ft_multiplechoice
  fvString As Asciiz * %MaxStr         ' ft_string
  fvFulltext As Asciiz * %MaxStr       ' ft_fulltext
  fvDatetime As FILETIME               ' ft_datetime
End Union
 
Type ContentDefaultParamStruct
   iSize As Long
   PluginInterfaceVersionLow As Dword
   PluginInterfaceVersionHi As Dword
   DefaultIniName As Asciiz * %MAX_PATH
End Type
 
 
Function ContentGetSupportedField Alias _
   "ContentGetSupportedField"  (ByVal FieldIndex As Long, _
   FieldName As Asciiz, sUnits As Asciiz, _
   ByVal maxlen As Long) Export As Long
 
 
Function ContentGetValue Alias  "ContentGetValue"  ( _
   FileName As Asciiz, ByVal FieldIndex As Long, _
   ByVal UnitIndex As Long, FieldValue As uFieldValue, _
   ByVal maxlen As Long, ByVal flags As Long) Export As Long
 
 
         ' Some samples for FieldValue :
 
         'FieldValue.fvString = "Some String"
 
         'FieldValue.fvTime.wHour   = 10
         'FieldValue.fvTime.wMinute = 30
         'FieldValue.fvTime.wSecond = 15
 
         'FieldValue.fvMultiplechoice = "male|female|n/a"
 
         ' etc
 
Sub ContentGetDetectString Alias  "ContentGetDetectString" ( _
   DetectString As Asciiz, ByVal maxlen As Long) Export
 
 
Sub ContentSetDefaultParams Alias  "ContentSetDefaultParams" ( _
   dps As ContentDefaultParamStruct) Export
 
 
Sub ContentStopGetValue Alias  "ContentStopGetValue" ( _
   FileName As Asciiz) Export
 
 
Function ContentGetDefaultSortOrder Alias _
   "ContentGetDefaultSortOrder"  ( _
   ByVal FieldIndex As Long) Export As Long
 
 
Sub ContentPluginUnloading Alias  "ContentPluginUnloading" () Export
 
 
'-------------------------------------------------------------------------------
' Main DLL entry point called by Windows...
 
Function LibMain(ByVal hInstance As Long, _
   ByVal fwdReason As Long, ByVal lpvReserved As Long) As Long
 
   Function = 1
 
End Function
 
 
'-------------------------------------------------------------------------------
PROGRESSCALLBACKPROC
 
PROGRESSCALLBACKPROC is a callback function which your plugin needs to call during a call to ContentCompareFiles to inform the host program about the progress of the comparison.
 
Declaration:
 
typedef int (__stdcall *PROGRESSCALLBACKPROC)(int nextblockdata);
 
Description of parameters:
 
nextblockdata The number of bytes compared since the last call of PROGRESSCALLBACKPROC.
 
Return value:
 
0 All OK, continue
other than 0 The user pressed the Cancel/Abort button
 
===============================================================================
ContentFindValue
 
ContentFindValue is called to search directly for a specific search string. This function is new since Total Commander 10, plugin interface version 2.12.
Normally Total Commander would just call ContentGetValue and compare the returned value by itself with the search string. However, by returning the new flag contflags_fieldsearch, it's possible to tell Total Commander to call this function instead for specific fields. This can be useful e.g. for passing a search string directly to a search database. ContentGetSupportedOperators can be called to define your own search operators instead of the default like "contains".
 
Declaration:
 
int __stdcall ContentFindValue(char* FileName, int FieldIndex, int UnitIndex, int OperationIndex, int FieldType, int flags, void* FieldValue);
 
Description of parameters:
 
FileName The name of the file for which to do the search.
 
FieldIndex The index of the field for which to do the search. This is the same index as the FieldIndex value in ContentGetSupportedField.
 
UnitIndex The index of the unit used. Example:
If the plugin returned the following unit string in ContentGetSupportedField:
bytes|kbytes|Mbytes
Then a UnitIndex of 0 would mean bytes, 1 means kbytes and 2 means MBytes
If no unit string was returned, UnitIndex is 0.
 
OperationIndex The index of the operation as displayed in the "OP" column in Total Commander search function. If you use ContentGetSupportedOperators, you will get back the index of the operator chosen by the user from that list. Otherwise the index depends on the FieldType. The default operators are:
ft_numeric_32,ft_numeric_64,ft_numeric_floating,ft_datetime,ft_date,ft_time:
    >  <  >=  <=  =  !=
ft_boolean:
    =
ft_multiplechoice
    =  !=
ft_string, ft_stringw:
    contains  !contains  cont.(case)  !cont.(case)  =  !=  =(case)  !=(case)  regex  !regex
ft_fulltext, ft_fulltextw
    contains  !contains  cont.(case)  !cont.(case)  regex  !regex   
 
FieldType The type of data sent to the plugin in FieldValue
 
flags Currently only one flag is defined:
CONTENT_DELAYIFSLOW: If this flag is set, the plugin should return ft_delayed for fields which take a long time to extract, like file version information. Total Commander will then call the function again in a background thread without the CONTENT_DELAYIFSLOW flag. This means that your plugin must be implemented thread-safe if you plan to return ft_delayed.
 
FieldValue Here the plugin gets the search criteria. The data format depends on the field type:
ft_numeric_32: FieldValue points to a 32-bit signed integer variable.
ft_numeric_64: FieldValue points to a 64-bit signed integer variable.
ft_numeric_floating: FieldValue points to a 64-bit floating point variable (ISO standard double precision)
See remark below about additional string field!
ft_date: FieldValue points to a structure containing year,month,day as 2 byte values.
ft_time: FieldValue points to a structure containing hour,minute,second as 2 byte values.
ft_boolean: FieldValue points to a 32-bit number. 0 means false, anything else means true.
ft_string: FieldValue points to a single pointer pointing to a 0-terminated string.
ft_stringw,
ft_fulltext,
ft_fulltextw: FieldValue is a pointer to 2 separate pointers: The first points to a 0 terminated ANSI string, the second to a 0 terminated Unicode string. Behind it follows a BOOL value telling the user whether the Unicode string contains any characters which cannot be represented by the ANSI string.
ft_multiplechoice: FieldValue is a pointer to a 0-terminated ANSI string.
ft_datetime: A timestamp of type FILETIME, as returned e.g. by FindFirstFile(). It is a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601. The time MUST be relative to universal time (Greenwich mean time) as returned by the file system, not local time!
 
Return value:
 
Return the field type in case of success, or one of the following error values otherwise:
ft_found The search string was found
ft_notfound The search string was NOT found
ft_nosuchfield The given FieldIndex is invalid
ft_fileerror Error accessing the specified file FileName
ft_fieldempty The file does not contain the specified field
ft_delayed The extraction of the field would take a long time, so Total Commander should request it again in a background thread. This error may only be returned if the flag CONTENT_DELAYIFSLOW was set, and if the plugin is thread-safe.
 
Remarks:
 
Note about Unicode: ft_stringw, ft_fulltext and ft_fulltextw always pass two pointers to the search function, the first pointing to the ANSI search string, the second to the Unicode search string. It is recommended to 
========================================================
ContentGetSupportedOperators
 
ContentGetSupportedOperators is called to return a list of supported operators like "contains" or "=". This function is only called when the flag contflags_fieldsearch was returned for a field. This function is new since Total Commander 10, plugin interface version 2.12.
 
Declaration:
 
int __stdcall ContentGetSupportedOperators(int FieldIndex, char* FieldOperators, int maxlen);
 
Description of parameters:
 
FieldIndex The index of the field for which TC requests information. Starting with 0, the FieldIndex is increased until the plugin returns an error.
 
FieldOperators Return a list of supported operators here, in English, separated by spaces. Operators using the default names like "contains" will be translated with internal Total Commander strings, otherwise via plugin language file. The operators must NOT contain any spaces, and need to be returned as a string separated by spaces, e.g.
contains !contains = !=
 
maxlen The maximum number of characters, including the trailing 0, which may be returned in FieldOperators.
 
Return value:
 
The function needs to return the number of operators, or 0 if not supported.
 
Note: This function is only available as Ansi, not Unicode. 
==========================================================
// Contents of file contplug.h version 2.12
 
#define ft_nomorefields 0
#define ft_numeric_32 1
#define ft_numeric_64 2
#define ft_numeric_floating 3
#define ft_date 4
#define ft_time 5
#define ft_boolean 6
#define ft_multiplechoice 7
#define ft_string 8
#define ft_fulltext 9
#define ft_datetime 10
#define ft_stringw 11
#define ft_fulltextw 12
 
#define ft_comparecontent 100
 
// for ContentGetValue
#define ft_nosuchfield -1   // error, invalid field number given
#define ft_fileerror -2     // file i/o error
#define ft_fieldempty -3    // field valid, but empty
#define ft_ondemand -4      // field will be retrieved only when user presses <SPACEBAR>
#define ft_notsupported -5  // function not supported
#define ft_setcancel -6     // user clicked cancel in field editor
#define ft_delayed 0        // field takes a long time to extract -> try again in background
// for ContentFindValue:
#define ft_found 1          // Value was found
#define ft_notfound -8      // Value was NOT found
 
// for ContentSetValue
#define ft_setsuccess 0     // setting of the attribute succeeded
 
// for ContentGetSupportedFieldFlags
#define contflags_edit 1
#define contflags_substsize 2
#define contflags_substdatetime 4
#define contflags_substdate 6
#define contflags_substtime 8
#define contflags_substattributes 10
#define contflags_substattributestr 12
#define contflags_passthrough_size_float 14
#define contflags_substmask 14
#define contflags_fieldedit 16
#define contflags_fieldsearch 32
#define contflags_searchpageonly 64
 
#define contst_readnewdir 1
#define contst_refreshpressed 2
#define contst_showhint 4
 
#define setflags_first_attribute 1     // First attribute of this file
#define setflags_last_attribute  2     // Last attribute of this file
#define setflags_only_date       4     // Only set the date of the datetime value!
 
#define editflags_initialize     1     // The data passed to the plugin may be used to
                                       // initialize the edit dialog
 
#define CONTENT_DELAYIFSLOW 1  // ContentGetValue called in foreground
#define CONTENT_PASSTHROUGH 2  // If requested via contflags_passthrough_size_float: The size
                               // is passed in as floating value, TC expects correct value
                               // from the given units value, and optionally a text string
 
typedef struct {
    int size;
    DWORD PluginInterfaceVersionLow;
    DWORD PluginInterfaceVersionHi;
    char DefaultIniName[MAX_PATH];
} ContentDefaultParamStruct;
 
typedef struct {
WORD wYear;
WORD wMonth;
WORD wDay;
} tdateformat,*pdateformat;
 
typedef struct {
WORD wHour;
WORD wMinute;
WORD wSecond;
} ttimeformat,*ptimeformat;
 
typedef struct {
__int64 filesize1;
__int64 filesize2;
FILETIME filetime1;
FILETIME filetime2;
DWORD attr1;
DWORD attr2;
} FileDetailsStruct;
 
typedef int (__stdcall *PROGRESSCALLBACKPROC)(int nextblockdata);
 
int __stdcall ContentGetDetectString(char* DetectString,int maxlen);
int __stdcall ContentGetSupportedField(int FieldIndex,char* FieldName,char* Units,int maxlen);
int __stdcall ContentGetValue(char* FileName,int FieldIndex,int UnitIndex,void* FieldValue,int maxlen,int flags);
int __stdcall ContentGetValueW(WCHAR* FileName,int FieldIndex,int UnitIndex,void* FieldValue,int maxlen,int flags);
void __stdcall ContentSetDefaultParams(ContentDefaultParamStruct* dps);
void __stdcall ContentPluginUnloading(void);
void __stdcall ContentStopGetValue(char* FileName);
void __stdcall ContentStopGetValueW(WCHAR* FileName);
int __stdcall ContentGetDefaultSortOrder(int FieldIndex);
int __stdcall ContentGetSupportedFieldFlags(int FieldIndex);
int __stdcall ContentSetValue(char* FileName,int FieldIndex,int UnitIndex,int FieldType,void* FieldValue,int flags);
int __stdcall ContentSetValueW(WCHAR* FileName,int FieldIndex,int UnitIndex,int FieldType,void* FieldValue,int flags);
int __stdcall ContentEditValue(HWND ParentWin,int FieldIndex,int UnitIndex,int FieldType,
                void* FieldValue,int maxlen,int flags,char* langidentifier);
void __stdcall ContentSendStateInformation(int state,char* path);
void __stdcall ContentSendStateInformationW(int state,WCHAR* path);
int __stdcall ContentCompareFiles(PROGRESSCALLBACKPROC progresscallback,
  int compareindex,char* filename1,char* filename2,FileDetailsStruct* filedetails);
int __stdcall ContentCompareFilesW(PROGRESSCALLBACKPROC progresscallback,
  int compareindex,WCHAR* filename1,WCHAR* filename2,FileDetailsStruct* filedetails);
int __stdcall ContentFindValue(char* FileName, int FieldIndex, int UnitIndex, int OperationIndex, int FieldType, int flags, void* FieldValue);
int __stdcall ContentFindValueW(WCHAR* FileName, int FieldIndex, int UnitIndex, int OperationIndex, int FieldType, int flags, void* FieldValue);
int __stdcall ContentGetSupportedOperators(int FieldIndex, char* FieldOperators, int maxlen); 
 */
