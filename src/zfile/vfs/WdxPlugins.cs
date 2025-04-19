using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace zfile
{
    /// <summary>
    /// Static class for managing WDX plugins
    /// </summary>
    public static class WdxPlugins
    {
        private static WdxModuleList _moduleList = new WdxModuleList(Path.Combine(Constants.ZfileCfgPath, "wdx.xml"));

        /// <summary>
        /// Gets the number of registered WDX plugins
        /// </summary>
        public static int Count => _moduleList._modules.Count;

        /// <summary>
        /// Gets or sets the file name of a plugin
        /// </summary>
        /// <param name="index">The index of the plugin</param>
        /// <returns>The file name of the plugin</returns>
        public static string FileName
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the WDX module at the specified index
        /// </summary>
        /// <param name="index">The index of the module</param>
        /// <returns>The WDX module</returns>
        public static WdxModule GetWdxModule(int index)
        {
            if (index >= 0 && index < _moduleList._modules.Count)
            {
                return _moduleList._modules[index];
            }
            return null;
        }

        /// <summary>
        /// Adds a new plugin to the list
        /// </summary>
        /// <param name="pluginPath">The path to the plugin</param>
        /// <returns>The index of the added plugin</returns>
        public static int Add(string pluginPath)
        {
            _moduleList.AddModule(pluginPath);
            return _moduleList._modules.Count - 1;
        }

        /// <summary>
        /// Loads a WDX module
        /// </summary>
        /// <param name="fileName">The file name of the module</param>
        /// <returns>The loaded module</returns>
        public static WdxModule LoadModule(string fileName)
        {
            var module = new WdxModule(fileName);
            if (module.LoadModule())
            {
                return module;
            }
            return null;
        }

        /// <summary>
        /// Initializes the WDX plugins
        /// </summary>
        static WdxPlugins()
        {
            _moduleList.LoadConfiguration();
        }
    }
}
