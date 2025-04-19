using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace zfile
{
    /// <summary>
    /// Static class for managing WFX plugins
    /// </summary>
    public static class WfxPlugins
    {
        private static WfxModuleList _moduleList = new WfxModuleList(Path.Combine(Constants.ZfileCfgPath, "wfx.xml"));

        /// <summary>
        /// Gets the number of registered WFX plugins
        /// </summary>
        public static int Count => _moduleList._modules.Count;

        /// <summary>
        /// Gets or sets the file name of a plugin
        /// </summary>
        /// <param name="index">The index of the plugin</param>
        /// <returns>The file name of the plugin</returns>
        public static string GetFileName(int index)
        {
            if (index >= 0 && index < _moduleList._modules.Count)
            {
                return _moduleList._modules[index].ModulePath;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the file name of a plugin
        /// </summary>
        /// <param name="index">The index of the plugin</param>
        /// <param name="value">The file name to set</param>
        public static void SetFileName(int index, string value)
        {
            if (index >= 0 && index < _moduleList._modules.Count)
            {
                // 只更新路径，不重新加载模块
                _moduleList._modules[index].FileName = value;
                _moduleList.SaveConfiguration();
            }
        }

        /// <summary>
        /// Gets the WFX module at the specified index
        /// </summary>
        /// <param name="index">The index of the module</param>
        /// <returns>The WFX module</returns>
        public static WfxModule GetWfxModule(int index)
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
        /// <param name="rootName">The root name for the plugin</param>
        /// <param name="fileName">The file name of the plugin</param>
        /// <returns>The index of the added plugin</returns>
        public static int Add(string rootName, string fileName)
        {
            var module = LoadModule(fileName);
            if (module != null)
            {
                module.VFSRootName = rootName;
                _moduleList._modules.Add(module);
                _moduleList.SaveConfiguration();
                return _moduleList._modules.Count - 1;
            }
            return -1;
        }

        /// <summary>
        /// Loads a WFX module
        /// </summary>
        /// <param name="fileName">The file name of the module</param>
        /// <returns>The loaded module</returns>
        public static WfxModule LoadModule(string fileName)
        {
            var module = new WfxModule(fileName);
            if (module.LoadModule())
            {
                return module;
            }
            return null;
        }

        /// <summary>
        /// Initializes the WFX plugins
        /// </summary>
        static WfxPlugins()
        {
            _moduleList.LoadConfiguration();
        }
    }
}
