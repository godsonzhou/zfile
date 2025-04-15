
namespace Zfile
{
    /// <summary>
    /// Base class that provides cloning functionality
    /// </summary>
    public abstract class ObjectEx
    {
        public abstract ObjectEx Clone();
    }

    /// <summary>
    /// Memory stream for blob data
    /// </summary>
    public class BlobStream : MemoryStream
    {
        public BlobStream(IntPtr ptr, int size)
        {
            unsafe
            {
                byte[] buffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, size);
                Write(buffer, 0, size);
                Position = 0;
            }
        }
    }

    /// <summary>
    /// Extended INI property storage with DPI awareness
    /// </summary>
    public class IniPropStorageEx
    {
        private int _pixelsPerInch;
        private Form _owner;
        private string _iniSection;
        private IniFile _iniFile;

        public IniPropStorageEx(Form owner, string iniSection, string iniFileName)
        {
            _owner = owner;
            _iniSection = iniSection;
            _iniFile = new IniFileEx(iniFileName);
        }

        public void SaveProperties()
        {
            // Save form properties
            SaveFormProperties();
            
            // Save screen DPI
            _iniFile.WriteInteger(_iniSection, "Screen_PixelsPerInch", Screen.PrimaryScreen.LogicalDpi);
        }

        public void Restore()
        {
            try
            {
                // Read screen DPI
                _pixelsPerInch = _iniFile.ReadInteger(_iniSection, "Screen_PixelsPerInch", Screen.PrimaryScreen.LogicalDpi);
                
                // Restore form properties
                RestoreFormProperties();
            }
            finally
            {
                // Free resources
            }

            if (_owner != null)
            {
                // Refresh monitor list
                Screen.AllScreens.GetEnumerator();

                // Make form fully visible on the monitor
                Screen screen = Screen.FromPoint(new Point(_owner.Left, _owner.Top));
                if (screen != null)
                {
                    MakeFullyVisible(screen);
                }

                // Workaround for minimized state
                if (_owner.WindowState == FormWindowState.Minimized)
                {
                    _owner.WindowState = FormWindowState.Normal;
                }
            }
        }

        private void SaveFormProperties()
        {
            if (_owner != null)
            {
                _iniFile.WriteString(_iniSection, ChangeIdent(_owner.Name + "_Left"), _owner.Left.ToString());
                _iniFile.WriteString(_iniSection, ChangeIdent(_owner.Name + "_Top"), _owner.Top.ToString());
                _iniFile.WriteString(_iniSection, ChangeIdent(_owner.Name + "_Width"), _owner.Width.ToString());
                _iniFile.WriteString(_iniSection, ChangeIdent(_owner.Name + "_Height"), _owner.Height.ToString());
                _iniFile.WriteString(_iniSection, ChangeIdent(_owner.Name + "_WindowState"), ((int)_owner.WindowState).ToString());
            }
        }

        private void RestoreFormProperties()
        {
            if (_owner != null)
            {
                int left = int.Parse(DoReadString(_iniSection, _owner.Name + "_Left", _owner.Left.ToString()));
                int top = int.Parse(DoReadString(_iniSection, _owner.Name + "_Top", _owner.Top.ToString()));
                int width = int.Parse(DoReadString(_iniSection, _owner.Name + "_Width", _owner.Width.ToString()));
                int height = int.Parse(DoReadString(_iniSection, _owner.Name + "_Height", _owner.Height.ToString()));
                int windowState = int.Parse(DoReadString(_iniSection, _owner.Name + "_WindowState", ((int)_owner.WindowState).ToString()));

                _owner.Left = left;
                _owner.Top = top;
                _owner.Width = width;
                _owner.Height = height;
                _owner.WindowState = (FormWindowState)windowState;
            }
        }

        private void MakeFullyVisible(Screen screen)
        {
            if (_owner != null && screen != null)
            {
                Rectangle workingArea = screen.WorkingArea;
                if (_owner.Left < workingArea.Left)
                    _owner.Left = workingArea.Left;
                if (_owner.Top < workingArea.Top)
                    _owner.Top = workingArea.Top;
                if (_owner.Left + _owner.Width > workingArea.Right)
                    _owner.Left = workingArea.Right - _owner.Width;
                if (_owner.Top + _owner.Height > workingArea.Bottom)
                    _owner.Top = workingArea.Bottom - _owner.Height;
            }
        }

        public string DoReadString(string section, string ident, string defaultValue)
        {
            string result = _iniFile.ReadString(section, ChangeIdent(ident), defaultValue);

            // Handle DPI scaling for width and height
            if (_owner != null && _owner.AutoScaleMode != AutoScaleMode.None)
            {
                if (ident.EndsWith("_Width") || ident.EndsWith("_Height"))
                {
                    if (int.TryParse(result, out int value))
                    {
                        int designTimeDpi = 96; // Default design-time DPI in Windows Forms
                        result = ((value * designTimeDpi) / _pixelsPerInch).ToString();
                    }
                }
            }

            return result;
        }

        public void DoWriteString(string section, string ident, string value)
        {
            _iniFile.WriteString(section, ChangeIdent(ident), value);
        }

        private string ChangeIdent(string ident)
        {
            // Change component name to class name
            if (_owner != null && ident.StartsWith(_owner.Name))
            {
                return _owner.GetType().Name + ident.Substring(_owner.Name.Length);
            }
            else
            {
                return ident;
            }
        }
    }

    /// <summary>
    /// Thread-safe object list
    /// </summary>
    public class ThreadSafeList<T> where T : class
    {
        private List<T> _list;
        private object _lock;

        public ThreadSafeList()
        {
            _list = new List<T>();
            _lock = new object();
        }

        ~ThreadSafeList()
        {
            lock (_lock)
            {
                Clear();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        public List<T> LockList()
        {
            Monitor.Enter(_lock);
            return _list;
        }

        public void UnlockList()
        {
            Monitor.Exit(_lock);
        }

        public int Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
                return _list.Count - 1;
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return _list[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    _list[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }

        public void Remove(T item)
        {
            lock (_lock)
            {
                _list.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _list.RemoveAt(index);
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                return new List<T>(_list);
            }
        }

		internal void Dispose()
		{
			throw new NotImplementedException();
		}

		internal IEnumerable<T> Clone()
		{
			throw new NotImplementedException();
		}
	}

    /// <summary>
    /// Helper class for SynEdit control
    /// </summary>
    public static class SynEditHelper
    {
        public static void FixDefaultKeystrokes(this SynEdit synEdit)
        {
            // Add standard keyboard shortcuts
            synEdit.AddKey(SynEditorCommand.Copy, Keys.C | Keys.Control, Keys.None);
            synEdit.AddKey(SynEditorCommand.SelectAll, Keys.A | Keys.Control, Keys.None);
        }

        private static void AddKey(this SynEdit synEdit, SynEditorCommand command, Keys key, Keys shiftMask)
        {
            // Add keystroke to the editor
            synEdit.Keystrokes.Add(new SynEditKeystroke
            {
                Key = key,
                ShiftMask = shiftMask,
                Command = command
            });
        }
    }

    /// <summary>
    /// Helper classes to support the implementation
    /// </summary>
    public class IniFile
    {
        private string _fileName;

        public IniFile(string fileName)
        {
            _fileName = fileName;
        }

        public virtual string ReadString(string section, string key, string defaultValue)
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(
                NativeMethods.GetPrivateProfileString(section, key, defaultValue, new string(' ', 255), 255, _fileName));
        }

        public virtual void WriteString(string section, string key, string value)
        {
            NativeMethods.WritePrivateProfileString(section, key, value, _fileName);
        }

        public int ReadInteger(string section, string key, int defaultValue)
        {
            string result = ReadString(section, key, defaultValue.ToString());
            return int.TryParse(result, out int value) ? value : defaultValue;
        }

        public void WriteInteger(string section, string key, int value)
        {
            WriteString(section, key, value.ToString());
        }
    }

    public class IniFileEx : IniFile
    {
        public IniFileEx(string fileName) : base(fileName) { }

        // UTF-8 support would be implemented here
    }

    public class SynEdit
    {
        public SynEditKeystrokes Keystrokes { get; set; } = new SynEditKeystrokes();
    }

    public class SynEditKeystrokes
    {
        private List<SynEditKeystroke> _keystrokes = new List<SynEditKeystroke>();

        public SynEditKeystroke Add()
        {
            var keystroke = new SynEditKeystroke();
            _keystrokes.Add(keystroke);
            return keystroke;
        }

        public void Add(SynEditKeystroke keystroke)
        {
            _keystrokes.Add(keystroke);
        }
    }

    public class SynEditKeystroke
    {
        public Keys Key { get; set; }
        public Keys ShiftMask { get; set; }
        public SynEditorCommand Command { get; set; }
    }

    public enum SynEditorCommand
    {
        Copy,
        SelectAll
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr GetPrivateProfileString(string section, string key, string defaultValue, string returnString, int size, string fileName);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool WritePrivateProfileString(string section, string key, string value, string fileName);
    }
}