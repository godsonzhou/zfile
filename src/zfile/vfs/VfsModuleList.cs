using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zfile
{
 

    /// <summary>
    /// A list of virtual file system modules
    /// </summary>
    public partial class VfsModuleList : IEnumerable<string>
    {
        private readonly Dictionary<string, VfsModule> modules = new Dictionary<string, VfsModule>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _names = new List<string>();

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
