using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace WinFormsApp1
{
    #region WDX常量和结构体
    public static class WdxConstants
    {
        // 字段类型常量
        public const int FT_NUMERIC_32 = 1;      // 32位整数
        public const int FT_NUMERIC_64 = 2;      // 64位整数
        public const int FT_NUMERIC_FLOATING = 3; // 浮点数
        public const int FT_DATE = 4;            // 日期
        public const int FT_TIME = 5;            // 时间
        public const int FT_DATETIME = 6;        // 日期时间
        public const int FT_BOOLEAN = 7;         // 布尔值
        public const int FT_STRING = 8;          // 字符串
        public const int FT_MULTIPLECHOICE = 9;  // 多选项
        public const int FT_FULLTEXT = 10;       // 全文本
        public const int FT_NOSUCHFIELD = -1;    // 无此字段

        // 返回值常量
        public const int WDX_SUCCESS = 0;
        public const int WDX_ERROR = 1;
        public const int WDX_NOTFOUND = -1;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WdxField
    {
        public string Name;           // 字段名称
        public string Description;    // 字段描述
        public int Type;             // 字段类型
        public string[] Units;       // 单位列表
        public int DefaultUnitIndex; // 默认单位索引
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ContentDefaultParamStruct
    {
        public int Size;
        public int PluginInterfaceVersionLow;
        public int PluginInterfaceVersionHi;
        [MarshalAs(UnmanagedType.LPStr)]
        public string DefaultIniName;
    }
    #endregion

    #region WDX函数委托
    // 必需的函数
    public delegate int ContentGetSupportedField(int FieldIndex, out IntPtr FieldName, out int Units, out IntPtr UnitName);
    public delegate int ContentGetValue(string FileName, int FieldIndex, int UnitIndex, int MaxLen, out IntPtr FieldValue, int Flags);
    
    // Unicode版本
    public delegate int ContentGetValueW(string FileName, int FieldIndex, int UnitIndex, int MaxLen, out IntPtr FieldValue, int Flags);
    public delegate int ContentSetDefaultParams(ref ContentDefaultParamStruct dps);
    
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
    public delegate void ContentStopGetValueW(string FileName);
    public delegate int ContentSetValueW(string FileName, int FieldIndex, int UnitIndex, int FieldType, IntPtr FieldValue, int Flags);
    public delegate void ContentSendStateInformationW(int State, string Path);
    #endregion

    public class WdxModule : IDisposable
    {
        #region 字段
        private IntPtr _moduleHandle;
        private string _modulePath;
        private string _pluginName;
        private bool _isUnicode;
        private List<WdxField> _fields;
        private Dictionary<string, string> _translations;

        // 必需的函数指针
        private ContentGetSupportedField _contentGetSupportedField;
        private ContentGetValue _contentGetValue;
        private ContentGetValueW _contentGetValueW;
        private ContentSetDefaultParams _contentSetDefaultParams;

        // 可选的函数指针
        private ContentPluginUnloading _contentPluginUnloading;
        private ContentStopGetValue _contentStopGetValue;
        private ContentGetDefaultSortOrder _contentGetDefaultSortOrder;
        private ContentSetValue _contentSetValue;

        // 新增可选函数指针
        private ContentGetDetectString _contentGetDetectString;
        private ContentGetSupportedFieldFlags _contentGetSupportedFieldFlags;
        private ContentEditValue _contentEditValue;
        private ContentSendStateInformation _contentSendStateInformation;

        // 新增Unicode版本的可选函数指针
        private ContentStopGetValueW _contentStopGetValueW;
        private ContentSetValueW _contentSetValueW;
        private ContentSendStateInformationW _contentSendStateInformationW;
        #endregion

        #region 属性
        public string ModulePath => _modulePath;
        public string PluginName => _pluginName;
        public bool IsLoaded => _moduleHandle != IntPtr.Zero;
        public bool IsUnicode => _isUnicode;
        public IReadOnlyList<WdxField> Fields => _fields.AsReadOnly();
        #endregion

        #region 构造函数和初始化
        public WdxModule(string modulePath)
        {
            _modulePath = modulePath;
            _pluginName = Path.GetFileNameWithoutExtension(modulePath);
            _fields = new List<WdxField>();
            _translations = new Dictionary<string, string>();
        }

        public bool LoadModule()
        {
            if (IsLoaded) return true;

            try
            {
                _moduleHandle = NativeLibrary.Load(_modulePath);
                if (_moduleHandle == IntPtr.Zero) return false;

                // 加载必需的函数
                _contentGetSupportedField = GetFunction<ContentGetSupportedField>("ContentGetSupportedField");
                _contentSetDefaultParams = GetFunction<ContentSetDefaultParams>("ContentSetDefaultParams");

                if (_contentGetSupportedField == null || _contentSetDefaultParams == null)
                {
                    UnloadModule();
                    return false;
                }

                // 尝试加载Unicode版本函数
                _contentGetValueW = GetFunction<ContentGetValueW>("ContentGetValueW");
                if (_contentGetValueW != null)
                {
                    _isUnicode = true;
                }
                else
                {
                    _contentGetValue = GetFunction<ContentGetValue>("ContentGetValue");
                    if (_contentGetValue == null)
                    {
                        UnloadModule();
                        return false;
                    }
                }

                // 加载可选函数
                _contentPluginUnloading = GetFunction<ContentPluginUnloading>("ContentPluginUnloading");
                _contentStopGetValue = GetFunction<ContentStopGetValue>("ContentStopGetValue");
                _contentGetDefaultSortOrder = GetFunction<ContentGetDefaultSortOrder>("ContentGetDefaultSortOrder");
                _contentSetValue = GetFunction<ContentSetValue>("ContentSetValue");

                // 加载新增可选函数
                _contentGetDetectString = GetFunction<ContentGetDetectString>("ContentGetDetectString");
                _contentGetSupportedFieldFlags = GetFunction<ContentGetSupportedFieldFlags>("ContentGetSupportedFieldFlags");
                _contentEditValue = GetFunction<ContentEditValue>("ContentEditValue");
                _contentSendStateInformation = GetFunction<ContentSendStateInformation>("ContentSendStateInformation");

                // 加载新增Unicode版本的可选函数
                if (_isUnicode)
                {
                    _contentStopGetValueW = GetFunction<ContentStopGetValueW>("ContentStopGetValueW");
                    _contentSetValueW = GetFunction<ContentSetValueW>("ContentSetValueW");
                    _contentSendStateInformationW = GetFunction<ContentSendStateInformationW>("ContentSendStateInformationW");
                }

                // 初始化插件
                var defaultParams = new ContentDefaultParamStruct
                {
                    Size = Marshal.SizeOf<ContentDefaultParamStruct>(),
                    PluginInterfaceVersionLow = 1,
                    PluginInterfaceVersionHi = 2,
                    DefaultIniName = "wdx.ini"
                };

                if (_contentSetDefaultParams(ref defaultParams) != WdxConstants.WDX_SUCCESS)
                {
                    UnloadModule();
                    return false;
                }

                // 加载支持的字段
                LoadSupportedFields();

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

        private void LoadSupportedFields()
        {
            _fields.Clear();
            int fieldIndex = 0;

            while (true)
            {
                IntPtr fieldNamePtr, unitNamePtr;
                int units;

                int result = _contentGetSupportedField(fieldIndex, out fieldNamePtr, out units, out unitNamePtr);
                if (result == WdxConstants.WDX_NOTFOUND) break;

                var field = new WdxField
                {
                    Name = Marshal.PtrToStringAnsi(fieldNamePtr),
                    Type = result,
                    Units = new string[units],
                    DefaultUnitIndex = 0
                };

                // 加载单位列表
                if (units > 0)
                {
                    for (int i = 0; i < units; i++)
                    {
                        result = _contentGetSupportedField(fieldIndex, out _, out _, out unitNamePtr);
                        if (result != WdxConstants.WDX_ERROR)
                        {
                            field.Units[i] = Marshal.PtrToStringAnsi(unitNamePtr);
                        }
                    }
                }

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

		public string GetValue(string fileName, int fieldIndex, int unitIndex = 0)
        {
            if (!IsLoaded || fieldIndex < 0 || fieldIndex >= _fields.Count)
                return string.Empty;

            try
            {
                IntPtr valuePtr;
                int result;

                if (_isUnicode)
                {
                    result = _contentGetValueW(fileName, fieldIndex, unitIndex, 2048, out valuePtr, 0);
                }
                else
                {
                    result = _contentGetValue(fileName, fieldIndex, unitIndex, 2048, out valuePtr, 0);
                }

                if (result == WdxConstants.WDX_SUCCESS)
                {
                    return _isUnicode ? 
                        Marshal.PtrToStringUni(valuePtr) : 
                        Marshal.PtrToStringAnsi(valuePtr);
                }
            }
            catch
            {
                // 处理异常
            }

            return string.Empty;
        }

        public void StopGetValue(string fileName)
        {
			if(_isUnicode)
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
			if (_contentSetValue == null) return false;

			try
			{
				if (_isUnicode)
				{
					return _contentSetValueW(fileName, fieldIndex, unitIndex, vallen, vptr, 0) == WdxConstants.WDX_SUCCESS;
				}
				else
				{
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
        public void UnloadModule()
        {
            if (_moduleHandle != IntPtr.Zero)
            {
                _contentPluginUnloading?.Invoke();
                NativeLibrary.Free(_moduleHandle);
                _moduleHandle = IntPtr.Zero;
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

        public void Dispose()
        {
            UnloadModule();
            GC.SuppressFinalize(this);
        }

        ~WdxModule()
        {
            Dispose();
        }
        #endregion
    }

    public class WdxModuleList : IDisposable
    {
        private List<WdxModule> _modules = new List<WdxModule>();
        private string _configPath;

        public WdxModuleList(string configPath)
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
                foreach (var element in doc.Root.Elements("WdxPlugin"))
                {
                    var enabled = bool.Parse(element.Attribute("Enabled")?.Value ?? "false");
                    if (!enabled) continue;

                    var path = element.Element("Path")?.Value;
                    if (string.IsNullOrEmpty(path)) continue;

                    try
                    {
                        var module = new WdxModule(path);
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
                    new XElement("WdxPlugins",
                        _modules.Select(m =>
                            new XElement("WdxPlugin",
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

            var module = new WdxModule(modulePath);
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

        public WdxModule FindModule(string pluginName)
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