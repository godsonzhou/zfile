using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WinShell
{
	public class ShellItem
	{
		
		public IntPtr PIDL { get; }
		public IShellFolder ShellFolder { get; }
		public IShellFolder ParentShellFolder { get; }
		public string parsepath = string.Empty;
		public bool IsVirtual = false;
		
		public ShellItem(IntPtr PIDL, IShellFolder ShellFolder, IShellFolder ParentShellFolder)
		{
			this.PIDL = PIDL;
			this.ShellFolder = ShellFolder;
			//if (ParentShellFolder == null)
			//{
			//	this.ParentShellFolder = w32.GetDesktopFolder(out IntPtr desktopPtr);
			//}
			//else
			this.ParentShellFolder = ParentShellFolder;
			IsVirtual = IsVirtualPath(ref parsepath);
		}
		public IntPtr[] GetChildPIDLs(SHCONTF shcontf = SHCONTF.FOLDERS)
		{
			List<IntPtr> pidls = new List<IntPtr>();
		
			if (ParentShellFolder != null)
			{
				if (ParentShellFolder.EnumObjects(IntPtr.Zero, shcontf, out IntPtr pEnumIDList) == w32.S_OK)
				{
					if (pEnumIDList != IntPtr.Zero)
					{
						var e = (IEnumIDList)Marshal.GetObjectForIUnknown(pEnumIDList);
						while (e.Next(1, out IntPtr pidlSub, out uint celtFetched) == 0 && celtFetched == w32.S_FALSE) //获取子节点的pidl
						{
							pidls.Add(pidlSub);
						}
					}
				}
			}
			return pidls.ToArray();
		}
		public bool hasChildren {  
			get { return GetChildPIDLs().Length != 0; } 
		}
		
		public string GetParsePath()
		{
			// 获取CLSID并检查是否为虚拟文件夹
			string parsedPath = string.Empty;
			try
			{
				if (ParentShellFolder != null)
				{
					IntPtr strr = Marshal.AllocCoTaskMem(w32.MAX_PATH * 2 + 4);
					Marshal.WriteInt32(strr, 0, 0);
					StringBuilder buf = new StringBuilder(w32.MAX_PATH);
					if (ParentShellFolder.GetDisplayNameOf(PIDL, SHGDN.FORPARSING, strr) == w32.S_OK)
					{
						API.StrRetToBuf(strr, PIDL, buf, w32.MAX_PATH);
						parsedPath = buf.ToString();
						Debug.Print("{0} ", parsedPath);
					}
					Marshal.FreeCoTaskMem(strr);
				}
				else
				{
					//is top node : desktop
					parsedPath = "::{00021400-0000-0000-C000-000000000046}";	// return desktop GUID
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"获取解析路径失败: {ex.Message}");
			}
			return parsedPath;
		}
		public bool IsVirtualPath(ref string p)
		{
			p = GetParsePath();
			return (p.Contains("::{"));
		}
		public SHFILEINFO GetIcon(bool smallIcon = true)
		{
			var flags = (SHGFI.PIDL | SHGFI.SYSICONINDEX | (smallIcon ? SHGFI.SMALLICON : SHGFI.LARGEICON));
			SHFILEINFO shfi = new SHFILEINFO();
			IntPtr hIcon = API.SHGetFileInfo(PIDL, 0, ref shfi, Marshal.SizeOf(shfi), flags);
			return shfi;
		}
		public SFGAO GetAttributes()
		{
			SFGAO attributes = SFGAO.FOLDER;
			ParentShellFolder?.GetAttributesOf(1, new[] { PIDL }, ref attributes);
			return attributes;
		}
	}				
}
