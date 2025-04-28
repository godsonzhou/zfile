namespace zfile
{
    /// <summary>
    /// Static class for managing WCX plugins
    /// </summary>
    public static class WcxPlugins
    {
        private static WcxModuleList _moduleList = new WcxModuleList();

        /// <summary>
        /// Gets the number of registered WCX plugins
        /// </summary>
        public static int Count => _moduleList.Count;
		//public static int mCount => _moduleList._modules.Count;
        /// <summary>
        /// Gets the list of extensions supported by the plugins
        /// </summary>
        public static List<string> Ext => _moduleList.Ext;

		/// <summary>
		/// Gets or sets the enabled state of a plugin by index
		/// </summary>
		/// <param name="index">Plugin index</param>
		/// <returns>True if the plugin is enabled</returns>
		public static bool[] Enabled => _moduleList.Enabled;
        //{
        //    get
        //    {
        //        bool[] result = new bool[Count];
        //        for (int i = 0; i < Count; i++)
        //        {
        //            result[i] = true; // All plugins are enabled by default
        //        }
        //        return result;
        //    }
        //}

        /// <summary>
        /// Gets the file name of a plugin by index
        /// </summary>
        /// <param name="index">Plugin index</param>
        /// <returns>The file name of the plugin</returns>
        public static string[] FileName => _moduleList.FileName;
    //    {
    //        get
    //        {
				//var moduleCount = _moduleList._modules.Count;
				//string[] result = new string[moduleCount];
    //            for (int i = 0; i < moduleCount; i++)
    //            {
    //                result[i] = _moduleList._modules[i].FilePath;
    //            }
    //            return result;
    //        }
    //    }

        /// <summary>
        /// Gets the flags (capabilities) of a plugin by index
        /// </summary>
        /// <param name="index">Plugin index</param>
        /// <returns>The flags of the plugin</returns>
        public static int[] Flags => _moduleList.Flags;
		//{
		//    get
		//    {
		//        int[] result = new int[mCount];
		//        for (int i = 0; i < mCount; i++)
		//        {
		//            result[i] = _moduleList._modules[i].PluginCapabilities;
		//        }
		//        return result;
		//    }
		//}

		/// <summary>
		/// Loads a WCX module by file name
		/// </summary>
		/// <param name="fileName">The file name of the module to load</param>
		/// <returns>The loaded WCX module, or null if loading failed</returns>
		public static WcxModule LoadModule(string fileName)
        {
            var module = _moduleList.FindModuleByName(Path.GetFileNameWithoutExtension(fileName));
            if (module != null)
            {
                return module;
            }

            module = new WcxModule(Path.GetFileNameWithoutExtension(fileName), fileName);
            if (module.LoadModule())
            {
                _moduleList.AddModule(module);
                return module;
            }

            return null;
        }

        /// <summary>
        /// Adds a new plugin to the list
        /// </summary>
        /// <param name="ext">The extension handled by the plugin</param>
        /// <param name="flags">The plugin capabilities</param>
        /// <param name="fileName">The file name of the plugin</param>
        /// <returns>The index of the added plugin</returns>
        public static int Add(string ext, int flags, string fileName)
        {
            var module = LoadModule(fileName);
            if (module != null)
            {
                if (!module.DetectStrings.Contains(ext))
                {
                    module.DetectStrings.Add(ext);
                }
                _moduleList._exts[ext] = module;
                return _moduleList._modules.IndexOf(module);
            }
            return -1;
        }

        /// <summary>
        /// Initializes the WCX plugins
        /// </summary>
        static WcxPlugins()
        {
            _moduleList.LoadConfiguration();
        }
    }
}
