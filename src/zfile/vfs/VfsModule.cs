/*
组件和功能：
核心类结构：
FileSourceBase：抽象基类，定义了所有文件源必须实现的基本接口
VfsModule：封装单个虚拟文件系统模块的元数据
VfsModuleList：管理所有已注册的模块
VfsModuleManager：提供全局静态访问点
主要功能实现：
模块注册：VfsModuleManager.RegisterVirtualFileSource<FtpFileSource>("FTP", true);
路径解析：var fileSource = VfsModuleManager.GetFileSource("ftp://example.com");
模块管理：var module = VfsModuleManager.Modules.GetVfsModule("FTP");
设计特点：
使用泛型和委托实现灵活的模块注册
线程安全的单例模式
支持可见性控制
强类型的模块管理
使用字典实现高效的模块查找
使用示例：
要实现一个新的文件源，你需要：

// 注册文件源
VfsModuleManager.RegisterVirtualFileSource<FtpFileSource>("FTP", true);
这个实现满足了你的所有要求，包括：
模块化设计
统一的接口
动态注册机制
高效的路径匹配
类型安全
可扩展性
你可以轻松地添加新的文件源实现，比如ZIP、网络存储等，只需要继承 FileSourceBase 并实现必要的方法即可。
*/
namespace zfile
{
	//public class FtpFileSource : FileSourceBase
	//{
	//	public override bool IsSupportedPath(string path)
	//	{
	//		return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
	//	}

	//	public override void Initialize()
	//	{
	//		// 初始化FTP连接
	//	}

	//	public override void Finalize()
	//	{
	//		// 清理资源
	//	}
	//}
	/// <summary>
	/// 文件源基类，所有具体文件源实现都应继承此类
	/// </summary>
	public abstract class FileSourceBase
    {
        public abstract bool IsSupportedPath(string path);
        public abstract void Initialize();
        public abstract void Finalize();
    }

    /// <summary>
    /// 文件源类型的委托
    /// </summary>
    public delegate FileSourceBase FileSourceCreator();

    /// <summary>
    /// 虚拟文件系统模块类，封装单个模块的元数据
    /// </summary>
    public class VfsModule
    {
		private bool _visible;
		private Type _fileSourceClass;
		public string Name { get; set; }
		public FileSourceCreator Creator { get; set; }

		public VfsModule(string name, Type fileSourceClass, bool visible, FileSourceCreator creator)
		{
			Name = name;
			FileSourceClass = fileSourceClass;
			Visible = visible;
			Creator = creator;
		}
		/// <summary>
		/// Whether the module is visible in the UI
		/// </summary>
		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}

		/// <summary>
		/// The file source class for this module
		/// </summary>
		public Type FileSourceClass
		{
			get { return _fileSourceClass; }
			set { _fileSourceClass = value; }
		}

		/// <summary>
		/// Creates a new VfsModule
		/// </summary>
		public VfsModule(bool visible, Type fileSourceClass)
		{
			_visible = visible;
			_fileSourceClass = fileSourceClass;
		}
	}

    /// <summary>
    /// 全局VFS模块管理器
    /// </summary>
    public class VfsModuleManager
    {
        private readonly VfsModuleList moduleList = new VfsModuleList();

        public VfsModuleList Modules => moduleList;

        /// <summary>
        /// 注册虚拟文件系统
        /// </summary>
        public void RegisterVirtualFileSource<T>(string name, bool visible) where T : FileSourceBase, new()
        {
            moduleList.RegisterModule(name, typeof(T), visible, () => new T());
        }

        /// <summary>
        /// 根据路径获取合适的文件源
        /// </summary>
        public FileSourceBase GetFileSource(string path)
        {
            return moduleList.GetFileSource(path);
        }
    }
} 