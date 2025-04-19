using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace zfile
{
    /// <summary>
    /// Static class for managing WLX plugins
    /// </summary>
    public static class WlxPlugins
    {
        private static WlxModuleList _moduleList = new WlxModuleList();

        /// <summary>
        /// Gets the number of registered WLX plugins
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
        /// Gets the WLX module at the specified index
        /// </summary>
        /// <param name="index">The index of the module</param>
        /// <returns>The WLX module</returns>
        public static WlxModule GetWlxModule(int index)
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
            var module = new WlxModule
            {
                FilePath = pluginPath,
                Name = Path.GetFileNameWithoutExtension(pluginPath)
            };
            
            if (module.LoadModule())
            {
                _moduleList.AddModule(module);
                _moduleList.isConfigChanged = true;
                _moduleList.SaveConfiguration();
                return _moduleList._modules.Count - 1;
            }
            
            return -1;
        }

        /// <summary>
        /// Loads a WLX module
        /// </summary>
        /// <param name="fileName">The file name of the module</param>
        /// <returns>The loaded module</returns>
        public static WlxModule LoadModule(string fileName)
        {
            var module = new WlxModule
            {
                FilePath = fileName,
                Name = Path.GetFileNameWithoutExtension(fileName)
            };
            
            if (module.LoadModule())
            {
                return module;
            }
            
            return null;
        }

        /// <summary>
        /// Initializes the WLX plugins
        /// </summary>
        static WlxPlugins()
        {
            _moduleList.LoadConfiguration();
        }
    }
}
