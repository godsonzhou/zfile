namespace zfile
{
    /// <summary>
    /// A simple string list class that can store objects associated with strings
    /// </summary>
    public class StringList
    {
        private readonly List<string> _strings = new List<string>();
        private readonly List<object> _objects = new List<object>();

        /// <summary>
        /// Gets the number of strings in the list
        /// </summary>
        public int Count => _strings.Count;

        /// <summary>
        /// Gets or sets the string at the specified index
        /// </summary>
        /// <param name="index">The index of the string to get or set</param>
        /// <returns>The string at the specified index</returns>
        public string this[int index]
        {
            get => _strings[index];
            set => _strings[index] = value;
        }

        /// <summary>
        /// Gets or sets the object associated with the string at the specified index
        /// </summary>
        /// <param name="index">The index of the object to get or set</param>
        /// <returns>The object at the specified index</returns>
        public object this[string key]
        {
            get
            {
                int index = IndexOf(key);
                return index >= 0 ? _objects[index] : null;
            }
            set
            {
                int index = IndexOf(key);
                if (index >= 0)
                    _objects[index] = value;
                else
                    AddObject(key, value);
            }
        }

        /// <summary>
        /// Gets the objects collection
        /// </summary>
        public ObjectCollection Objects { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public StringList()
        {
            Objects = new ObjectCollection(this);
        }

        /// <summary>
        /// A collection of objects associated with strings in the StringList
        /// </summary>
        public class ObjectCollection
        {
            private readonly StringList _owner;

            internal ObjectCollection(StringList owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// Gets or sets the object at the specified index
            /// </summary>
            /// <param name="index">The index of the object to get or set</param>
            /// <returns>The object at the specified index</returns>
            public object this[int index]
            {
                get => _owner._objects[index];
                set => _owner._objects[index] = value;
            }
        }

        /// <summary>
        /// Adds a string to the list
        /// </summary>
        /// <param name="value">The string to add</param>
        /// <returns>The index of the added string</returns>
        public int Add(string value)
        {
            _strings.Add(value);
            _objects.Add(null);
            return _strings.Count - 1;
        }

        /// <summary>
        /// Adds a string and an associated object to the list
        /// </summary>
        /// <param name="value">The string to add</param>
        /// <param name="obj">The object to associate with the string</param>
        /// <returns>The index of the added string</returns>
        public int AddObject(string value, object obj)
        {
            _strings.Add(value);
            _objects.Add(obj);
            return _strings.Count - 1;
        }

        /// <summary>
        /// Finds the index of a string in the list
        /// </summary>
        /// <param name="value">The string to find</param>
        /// <returns>The index of the string, or -1 if not found</returns>
        public int IndexOf(string value)
        {
            return _strings.IndexOf(value);
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            _strings.Clear();
            _objects.Clear();
        }
    }
}
