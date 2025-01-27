using System;
using System.Collections.Generic;
using System.Text;

namespace WinShell
{
    public class ShellItem
    {
        public ShellItem()
        {
        }

        public ShellItem(IntPtr PIDL, IShellFolder ShellFolder)
        {
            m_PIDL = PIDL;
            m_ShellFolder = ShellFolder;
        }

        private IntPtr m_PIDL;

        public IntPtr PIDL
        {
            get { return m_PIDL; }
            set { m_PIDL = value; }
        }


        private IShellFolder m_ShellFolder;

        public IShellFolder ShellFolder
        {
            get { return m_ShellFolder; }
            set { m_ShellFolder = value; }
        }

    }
}
