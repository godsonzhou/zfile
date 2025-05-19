using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace zfile
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
        [MarshalAs(UnmanagedType.LPStr)]
        // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DefaultIniName;
    }
    #endregion

    #region WDX函数委托
    // 必需的函数
    //[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public delegate int ContentGetSupportedField(int FieldIndex, out IntPtr FieldName, out int Units, out IntPtr UnitName);
    public delegate int ContentGetValue(string FileName, int FieldIndex, int UnitIndex, int MaxLen, out IntPtr FieldValue, int Flags);

    // Unicode版本
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate int ContentGetValueW([MarshalAs(UnmanagedType.LPWStr)] string FileName, int FieldIndex, int UnitIndex, int MaxLen, out IntPtr FieldValue, int Flags);
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

    public class WdxModule : IDisposable
    {
        #region 字段
        private IntPtr _moduleHandle;
        private string _modulePath;
        private string _pluginName;
        public string Name => _pluginName;
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
        public string FileName { get => _modulePath; set => _modulePath = value; }
        public List<string> DetectStrings = [];
        #endregion

        #region 构造函数和初始化
        public WdxModule(string modulePath)
        {
            _modulePath = modulePath;
            _pluginName = Path.GetFileNameWithoutExtension(modulePath);
            _fields = new List<WdxField>();
            _translations = new Dictionary<string, string>();
        }
        public WdxModule(string pluginName, string modulePath)
        {
            _modulePath = modulePath;
            _pluginName = pluginName;
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
                    IntPtr pDps = Marshal.AllocHGlobal(Marshal.SizeOf<ContentDefaultParamStruct>());
                    Marshal.StructureToPtr(defaultParams, pDps, false);

                    if (_contentSetDefaultParams != null)
                    {
                        _contentSetDefaultParams(pDps);
                    }

                    Marshal.FreeHGlobal(pDps);
                    //return true;
                }
                catch (Exception ex)
                {
                    UnloadModule();
                    return false;
                }
                // 加载支持的字段
                LoadSupportedFields();
                Debug.Print($"{_modulePath} loaded completed.");
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
            //IntPtr procAddress = NativeLibrary.GetExport(_moduleHandle, functionName);
            //return procAddress != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(procAddress) : null;
            IntPtr procAddress = DcxModule.NativeMethods.GetProcAddress(_moduleHandle, functionName);
            if (procAddress == IntPtr.Zero)
                return null;
            //return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
            return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
        }

        private void LoadSupportedFields()
        {
            _fields.Clear();
            int fieldIndex = 0;

            while (true)
            {
                string fieldName = null;
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

    public class WdxModuleList : StringList, IDisposable
    {
        public List<WdxModule> _modules = new List<WdxModule>();
        private string _configPath;
        public List<string> _cfg = [];
        public Dictionary<string, WdxModule> _exts = [];
        bool isConfigChanged;

        public WdxModuleList(string configPath)
        {
            _configPath = configPath;
            LoadConfiguration();
        }

        public WdxModule? FindModuleByName(string name)
        {
            return _modules.FirstOrDefault(m => m.Name != null && m.Name.Equals(name));
        }
        public void LoadConfiguration()
        {
            _modules.Clear();
            _exts.Clear();
            _cfg = Helper.ReadSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins");
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
                            module = new WdxModule(name, path);
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
                                //int flags = module.PluginCapabilities;
                                //foreach (string ext in detectstring.Split(','))
                                //{
                                //	//var result = Add(ext, flags, path);
                                //	//FileName[result] = name; // GetPluginFilenameToSave(plugin);
                                //}
                            }
                        }
                        else
                        {
                            if (!module.DetectStrings.Contains(detectstring))
                            {
                                module.DetectStrings.Add(detectstring);
                                _exts[parts[0].Trim()] = module;
                                //var result = Add(detectstring, module.PluginCapabilities, path);
                                //FileName[result] = name;
                            }
                        }
                    }
                }
            }
            //先按照配置读取插件（优先级高），然后按照目录读取插件
            //LoadModulesFromDirectory(Constants.ZfileBinPath + "Plugins\\wdx\\");
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
        public void SaveConfiguration()
        {
            if (!isConfigChanged) return;
            Helper.WriteSectionContent(Constants.ZfileCfgPath + "wincmd.ini", "ContentPlugins", _cfg);
            LoadConfiguration();
            isConfigChanged = false;
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