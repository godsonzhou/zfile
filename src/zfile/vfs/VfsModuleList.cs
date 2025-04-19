using System.Collections;

namespace zfile
{
 

    /// <summary>
    /// A list of virtual file system modules
    /// </summary>
    public class VfsModuleList : IEnumerable<string>
    {
        private readonly Dictionary<string, VfsModule> modules = new Dictionary<string, VfsModule>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _names = new List<string>();
		public static Dictionary<string, VfsModule> VfsModule = new();
        /// <summary>
        /// Gets the number of modules in the list
        /// </summary>
        public int Count => _names.Count;

        /// <summary>
        /// Gets the module name at the specified index
        /// </summary>
        public string this[int index] => _names[index];

        /// <summary>
        /// Gets the Objects collection which contains the VfsModule instances
        /// </summary>
        public VfsModuleObjectCollection Objects { get; }
		//private readonly Dictionary<string, VfsModule> modules;

		public VfsModuleList()
		{
			modules = new Dictionary<string, VfsModule>(StringComparer.OrdinalIgnoreCase);
			Objects = new VfsModuleObjectCollection(this);
		}

		/// <summary>
		/// 通过名称获取模块
		/// </summary>
		public VfsModule GetVfsModule(string name)
		{
			return modules.TryGetValue(name, out var module) ? module : null;
		}

		/// <summary>
		/// 根据路径获取支持的文件源
		/// </summary>
		public FileSourceBase GetFileSource(string path)
		{
			foreach (var module in modules.Values)
			{
				var fileSource = module.Creator();
				if (fileSource.IsSupportedPath(path))
				{
					return fileSource;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Gets a file source class that supports the specified path
		/// </summary>
		//public Type GetFileSource(string path)
		//{
		//	foreach (var name in _names)
		//	{
		//		var module = modules[name];
		//		var fileSourceInstance = Activator.CreateInstance(module.FileSourceClass) as FileSourceBase;
		//		if (fileSourceInstance != null && fileSourceInstance.IsSupportedPath(path))
		//		{
		//			return module.FileSourceClass;
		//		}
		//	}
		//	return null;
		//}
		/// <summary>
		/// 通过类名查找文件源
		/// </summary>
		public Type? FindFileSource(string className)
		{
			return modules.Values
				.FirstOrDefault(m => m.FileSourceClass.Name.Equals(className, StringComparison.OrdinalIgnoreCase))
				?.FileSourceClass;
		}

		/// <summary>
		/// 注册新的虚拟文件系统模块
		/// </summary>
		public void RegisterModule(string name, Type fileSourceClass, bool visible, FileSourceCreator creator)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if (fileSourceClass == null)
				throw new ArgumentNullException(nameof(fileSourceClass));

			if (creator == null)
				throw new ArgumentNullException(nameof(creator));

			if (modules.ContainsKey(name))
				throw new ArgumentException($"Module with name '{name}' is already registered.");

			modules[name] = new VfsModule(name, fileSourceClass, visible, creator);
		}

		/// <summary>
		/// 获取所有可见的模块名称
		/// </summary>
		public IEnumerable<string> GetVisibleModuleNames()
		{
			return modules.Values
				.Where(m => m.Visible)
				.Select(m => m.Name);
		}

		/// <summary>
		/// 注销模块
		/// </summary>
		public bool UnregisterModule(string name)
		{
			return modules.Remove(name);
		}

		/// <summary>
		/// 清除所有已注册的模块
		/// </summary>
		public void Clear()
		{
			modules.Clear();
		}
		/// <summary>
		/// Adds a new module to the list
		/// </summary>
		public void AddObject(string name, VfsModule module)
        {
            if (!modules.ContainsKey(name))
            {
                _names.Add(name);
                modules[name] = module;
            }
        }

        ///// <summary>
        ///// Finds a file source class by its class name
        ///// </summary>
        //public Type FindFileSource(string className)
        //{
        //    foreach (var name in _names)
        //    {
        //        var module = modules[name];
        //        if (module.FileSourceClass.Name == className)
        //        {
        //            return module.FileSourceClass;
        //        }
        //    }
        //    return null;
        //}

        /// <summary>
        /// Returns an enumerator that iterates through the module names
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            return _names.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the module names
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Collection of VfsModule objects
        /// </summary>
        public class VfsModuleObjectCollection
        {
            private readonly VfsModuleList _owner;

            internal VfsModuleObjectCollection(VfsModuleList owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// Gets the VfsModule at the specified index
            /// </summary>
            public VfsModule this[int index] => _owner.modules[_owner._names[index]];
        }
    }
}
