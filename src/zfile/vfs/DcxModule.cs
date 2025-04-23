using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace zfile
{
	// 扩展API回调函数委托定义
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int TDlgProc(IntPtr pDlg, string dlgItemName, int msg, int wParam, int lParam);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate bool TInputBoxProc(string caption, string prompt, bool maskInput, IntPtr value, int valueMaxLen);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int TMessageBoxProc(string text, string caption, int flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate bool TDialogBoxLFMProc(IntPtr lfmData, uint dataSize, TDlgProc dlgProc);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate bool TDialogBoxLRSProc(IntPtr lrsData, uint dataSize, TDlgProc dlgProc);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate bool TDialogBoxLFMFileProc(string lfmFileName, TDlgProc dlgProc);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	public delegate int TTranslateStringProc(IntPtr translation, string identifier, string original, IntPtr output, int outLen);
	// 委托定义，对应Pascal中的回调函数类型
	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate int TDlgProc(IntPtr pDlg, string dlgItemName, int msg, int wParam, int lParam);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate bool TInputBoxProc(string caption, string prompt, bool maskInput, IntPtr value, int valueMaxLen);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate int TMessageBoxProc(string text, string caption, int flags);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate bool TDialogBoxLFMProc(IntPtr lfmData, uint dataSize, TDlgProc dlgProc);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate bool TDialogBoxLRSProc(IntPtr lrsData, uint dataSize, TDlgProc dlgProc);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate bool TDialogBoxLFMFileProc(string lfmFileName, TDlgProc dlgProc);

	//[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	//public delegate int TTranslateStringProc(IntPtr translation, string identifier, string original, IntPtr output, int outLen);

	///// <summary>
	///// 扩展启动信息结构体，对应Pascal的TExtensionStartupInfo
	///// </summary>
	//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	//public struct TExtensionStartupInfo
	//{
	//    /// <summary>
	//    /// 结构体大小（字节）
	//    /// </summary>
	//    public uint StructSize;

	//    /// <summary>
	//    /// 插件所在目录（UTF-8编码）
	//    /// </summary>
	//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16384)]
	//    public byte[] PluginDir;

	//    /// <summary>
	//    /// 插件配置文件所在目录（UTF-8编码）
	//    /// </summary>
	//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16384)]
	//    public byte[] PluginConfDir;

	//    /// <summary>
	//    /// 输入框回调函数
	//    /// </summary>
	//    public IntPtr InputBox;

	//    /// <summary>
	//    /// 消息框回调函数
	//    /// </summary>
	//    public IntPtr MessageBox;

	//    /// <summary>
	//    /// LFM对话框回调函数
	//    /// </summary>
	//    public IntPtr DialogBoxLFM;

	//    /// <summary>
	//    /// LRS对话框回调函数
	//    /// </summary>
	//    public IntPtr DialogBoxLRS;

	//    /// <summary>
	//    /// LFM文件对话框回调函数
	//    /// </summary>
	//    public IntPtr DialogBoxLFMFile;

	//    /// <summary>
	//    /// 对话框消息发送回调函数
	//    /// </summary>
	//    public IntPtr SendDlgMsg;

	//    /// <summary>
	//    /// 翻译指针
	//    /// </summary>
	//    public IntPtr Translation;

	//    /// <summary>
	//    /// 字符串翻译回调函数
	//    /// </summary>
	//    public IntPtr TranslateString;

	//    /// <summary>
	//    /// 为未来API扩展预留的空间
	//    /// </summary>
	//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4094 * 8)]
	//    public byte[] Reserved;
	//}
	/// <summary>
	/// C# 实现的 DcxModule 类，对应 Pascal 的 TDcxModule
	/// </summary>
	public class DcxModule
    {
        // 常量定义
        private const int MAX_PATH = 16384;

        // 字段定义，对应 Pascal 的 FPOFile, FModulePath, FModuleHandle
        protected object FPOFile; // 在C#中使用适当的翻译类对象
        protected string FModulePath;
        protected IntPtr FModuleHandle;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DcxModule(string modulePath)
        {
            FModulePath = modulePath;
        }

        /// <summary>
        /// 析构函数，对应 Pascal 的 Destroy
        /// </summary>
        ~DcxModule()
        {
            Dispose();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            // 释放GCHandle
            if (_poFileHandle.IsAllocated)
            {
                _poFileHandle.Free();
            }
            
            // 释放POFile资源
            if (FPOFile != null)
            {
                // 在实际实现中，这里需要调用相应的释放方法
                FPOFile = null;
            }
            
            // 清除委托引用
            _inputBoxDelegate = null;
            _messageBoxDelegate = null;
            _dialogBoxLFMDelegate = null;
            _dialogBoxLRSDelegate = null;
            _dialogBoxLFMFileDelegate = null;
            _sendDlgMsgDelegate = null;
            _translateStringDelegate = null;
            
            // 卸载模块
            UnloadModule();
        }

        /// <summary>
        /// 加载模块
        /// </summary>
        public bool LoadModule()
        {
            try
            {
                FModuleHandle = NativeMethods.LoadLibrary(FModulePath);
                return FModuleHandle != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 卸载模块
        /// </summary>
        public void UnloadModule()
        {
            if (FModuleHandle != IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(FModuleHandle);
                FModuleHandle = IntPtr.Zero;
            }
        }

        // 保存委托的引用，防止被GC回收
        private TInputBoxProc _inputBoxDelegate;
        private TMessageBoxProc _messageBoxDelegate;
        private TDialogBoxLFMProc _dialogBoxLFMDelegate;
        private TDialogBoxLRSProc _dialogBoxLRSDelegate;
        private TDialogBoxLFMFileProc _dialogBoxLFMFileDelegate;
        private TDlgProc _sendDlgMsgDelegate;
        private TTranslateStringProc _translateStringDelegate;
        private GCHandle _poFileHandle;

        /// <summary>
        /// 初始化扩展，对应 Pascal 的 InitializeExtension
        /// </summary>
        public void InitializeExtension(IntPtr startupInfoPtr)
        {
            // 获取结构体引用
            TExtensionStartupInfo startupInfo = new TExtensionStartupInfo();

            // 加载语言文件
            string fileName = FModulePath;
            string path = Path.Combine(Path.GetDirectoryName(fileName), "language");
            string language = Path.GetExtension(Path.GetFileNameWithoutExtension(GetPOFileName()));
            fileName = Path.Combine(path, Path.GetFileNameWithoutExtension(fileName) + language + ".po");
            
            if (File.Exists(fileName))
            {
                // 在实际实现中，这里需要加载 PO 文件
                // FPOFile = LoadPOFile(fileName);
            }

            // 设置结构体字段
            startupInfo.StructSize = (uint)Marshal.SizeOf(typeof(TExtensionStartupInfo));
            
            // 设置插件目录
            string pluginDir = Path.GetDirectoryName(FModulePath);
            startupInfo.PluginDir = Encoding.UTF8.GetBytes(pluginDir + new string('\0', MAX_PATH - pluginDir.Length));
            
            // 设置配置目录
            string configDir = GetConfigDir();
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
            if (FPOFile != null)
            {
                _poFileHandle = GCHandle.Alloc(FPOFile);
                startupInfo.Translation = GCHandle.ToIntPtr(_poFileHandle);
            }
            else
            {
                startupInfo.Translation = IntPtr.Zero;
            }
            startupInfo.TranslateString = Marshal.GetFunctionPointerForDelegate(_translateStringDelegate);
            
            // 将结构体复制到非托管内存
            Marshal.StructureToPtr(startupInfo, startupInfoPtr, false);
        }

        /// <summary>
        /// 获取 PO 文件名
        /// </summary>
        protected virtual string GetPOFileName()
        {
            // 在实际实现中，这里需要返回实际的 PO 文件名
            return "default.po";
        }

        /// <summary>
        /// 获取配置目录
        /// </summary>
        protected virtual string GetConfigDir()
        {
            // 在实际实现中，这里需要返回实际的配置目录
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "zfile");
        }

        #region 回调函数实现

        /// <summary>
        /// 翻译字符串，对应 Pascal 的 Translate 函数
        /// </summary>
        protected static int Translate(IntPtr translation, string identifier, string original, IntPtr output, int outLen)
        {
            if (translation == IntPtr.Zero)
            {
                // 如果没有翻译对象，将输出设为空字符串
                if (output != IntPtr.Zero)
                    Marshal.WriteByte(output, 0, 0); // 写入空字符
                return 0;
            }
            else
            {
                try
                {
                    // 获取翻译对象
                    var poFile = GCHandle.FromIntPtr(translation).Target;
                    
                    // 在实际实现中，这里需要调用 POFile 的翻译方法
                    string text = original; // 默认返回原始文本
                    // 如果有翻译，则使用翻译后的文本
                    // text = poFile.Translate(identifier, original);
                    
                    // 计算复制长度
                    int copyLen = Math.Min(text.Length, outLen - 1);
                    
                    // 将文本复制到输出缓冲区
                    if (output != IntPtr.Zero && copyLen > 0)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(text.Substring(0, copyLen));
                        Marshal.Copy(bytes, 0, output, bytes.Length);
                        Marshal.WriteByte(output, bytes.Length, 0); // 添加结束符
                    }
                    
                    return copyLen;
                }
                catch
                {
                    // 出错时返回原始文本
                    if (output != IntPtr.Zero)
                        Marshal.WriteByte(output, 0, 0); // 写入空字符
                    return 0;
                }
            }
        }

        /// <summary>
        /// 输入框回调
        /// </summary>
        protected static bool InputBox(string caption, string prompt, bool maskInput, IntPtr value, int valueMaxLen)
        {
            // 在实际实现中，这里需要显示输入对话框
            // 由于C#中不能直接修改字符串参数，实际使用时需要使用Marshal类进行内存操作
            return false;
        }

        /// <summary>
        /// 消息框回调
        /// </summary>
        protected static int MessageBox(string text, string caption, int flags)
        {
            // 在实际实现中，这里需要显示消息对话框
            return 0;
        }

        /// <summary>
        /// LFM 对话框回调
        /// </summary>
        protected static bool DialogBoxLFM(IntPtr lfmData, uint dataSize, TDlgProc dlgProc)
        {
            // 在实际实现中，这里需要显示 LFM 对话框
            return false;
        }

        /// <summary>
        /// LRS 对话框回调
        /// </summary>
        protected static bool DialogBoxLRS(IntPtr lrsData, uint dataSize, TDlgProc dlgProc)
        {
            // 在实际实现中，这里需要显示 LRS 对话框
            return false;
        }

        /// <summary>
        /// LFM 文件对话框回调
        /// </summary>
        protected static bool DialogBoxLFMFile(string lfmFileName, TDlgProc dlgProc)
        {
            // 在实际实现中，这里需要显示 LFM 文件对话框
            return false;
        }

        /// <summary>
        /// 对话框消息发送回调
        /// </summary>
        protected static int SendDlgMsg(IntPtr pDlg, string dlgItemName, int msg, int wParam, int lParam)
        {
            // 在实际实现中，这里需要发送对话框消息
            return 0;
        }

        #endregion

        /// <summary>
        /// 通过函数名获取函数指针并转换为委托
        /// </summary>
        protected T GetDelegate<T>(string procName) where T : class
        {
            IntPtr procAddress = NativeMethods.GetProcAddress(FModuleHandle, procName);
            if (procAddress == IntPtr.Zero)
                return null;
            return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
        }

        /// <summary>
        /// 本地方法调用
        /// </summary>
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
}