using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace WinShell
{
	public class ShellItem : IDisposable
	{
		private bool _disposed = false;
		public IntPtr PIDL { get; }
		public IShellFolder ShellFolder { get; }
		public IShellFolder ParentShellFolder { get; }
		public string parsepath = string.Empty;
		public bool IsVirtual = false;
		public string Name;
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
			Name = w32.GetNameByIShell(ParentShellFolder,PIDL);
			var name1 = w32.GetNameByPIDL(PIDL);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				API.ILFree(PIDL);
				Marshal.ReleaseComObject(ShellFolder);
				Marshal.ReleaseComObject(ParentShellFolder);
				// 释放子 PIDL 列表
				foreach (var pidl in GetChildPIDLs())
				{
					API.ILFree(pidl);
				}
				_disposed = true;
			}
		}

		~ShellItem() => Dispose(false);
		public IntPtr[] GetChildPIDLs(SHCONTF shcontf = SHCONTF.FOLDERS)
		{
			List<IntPtr> pidls = new List<IntPtr>();
			if (ShellFolder != null)
			{
				if (ShellFolder.EnumObjects(IntPtr.Zero, shcontf, out IntPtr pEnumIDList) == w32.S_OK)
				{
					if (pEnumIDList != IntPtr.Zero)
					{
						var e = (IEnumIDList)Marshal.GetObjectForIUnknown(pEnumIDList);
						while (e.Next(1, out IntPtr pidlSub, out uint celtFetched) == 0 && celtFetched == w32.S_FALSE) //获取子节点的pidl
						{
							pidls.Add(pidlSub);
						}
						//API.ILFree(pEnumIDList);
						Marshal.ReleaseComObject(e);
						Marshal.Release(pEnumIDList);  // 或使用 API.ILFree 如果适用
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
			SHFILEINFO shfi = new SHFILEINFO();
			var flags = (SHGFI.PIDL | SHGFI.SYSICONINDEX | (smallIcon ? SHGFI.SMALLICON : SHGFI.LARGEICON));
			API.SHGetFileInfo(PIDL, 0, ref shfi, Marshal.SizeOf(shfi), flags);
			//IntPtr hIcon = API.SHGetFileInfo(PIDL, 0, ref shfi, Marshal.SizeOf(shfi), flags);
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
