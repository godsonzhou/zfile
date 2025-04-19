using System;
using System.Collections;
using System.Collections.Generic;

namespace zfile
{
	public class WcxHeader : IDisposable, IEnumerable<WcxHeader>
	{
		public string ArcName { get; set; }
		public string FileName { get; set; }
		public int Flags { get; set; }
		public int HostOS { get; set; }
		public int UnpVer { get; set; }
		public long PackSize { get; set; }
		public long UnpSize { get; set; }
		public FileAttributes FileAttr { get; set; }
		public int FileTime { get; set; }
		public int CRC { get; set; }
		public int Method { get; set; }
		public string Cmt { get; set; }
		public int CmtState { get; set; }

		public bool IsDirectory => (FileAttr & FileAttributes.Directory) == FileAttributes.Directory;

		public WcxHeader Clone()
		{
			return new WcxHeader
			{
				FileName = FileName,
				PackSize = PackSize,
				UnpSize = UnpSize,
				FileAttr = FileAttr,
				FileTime = FileTime,
				CRC = CRC,
				Method = Method,
				Cmt = Cmt,
				CmtState = CmtState,
				ArcName = ArcName,
				Flags = Flags,
				HostOS = HostOS,
				UnpVer = UnpVer
			};
		}
		public WcxHeader() { }
		public WcxHeader(THeaderData headerData)
		{
			FileName = headerData.FileName;
			PackSize = headerData.PackSize;
			UnpSize = headerData.UnpSize;
			FileAttr = (FileAttributes)headerData.FileAttr;
			FileTime = headerData.FileTime;
			CRC = headerData.FileCRC;
			Method = headerData.Method;
			Cmt = headerData.CmtBuf;
			CmtState = headerData.CmtState;
			ArcName = headerData.ArcName;
			Flags = headerData.Flags;
			HostOS = headerData.HostOS;
			UnpVer = headerData.UnpVer;
		}
		public WcxHeader(THeaderDataExW headerDataExW)
		{
			FileName = headerDataExW.FileName;
			PackSize = headerDataExW.PackSizeHigh << 32 | headerDataExW.PackSizeLow;
			UnpSize = headerDataExW.PackSizeHigh << 32 | headerDataExW.PackSizeLow;
			FileAttr = (FileAttributes)headerDataExW.FileAttr;
			FileTime = headerDataExW.FileTime;
			CRC = headerDataExW.FileCRC;
			Method = headerDataExW.Method;
			Cmt = headerDataExW.CmtBuf;
			CmtState = headerDataExW.CmtState;
			ArcName = headerDataExW.ArcName;
			Flags = headerDataExW.Flags;
			HostOS = headerDataExW.HostOS;
			UnpVer = headerDataExW.UnpVer;
		}

		// Implement IDisposable
		public void Dispose()
		{
			// No unmanaged resources to dispose
			GC.SuppressFinalize(this);
		}

		// Implement IEnumerable<WcxHeader>
		public IEnumerator<WcxHeader> GetEnumerator()
		{
			// Return only this instance in the enumeration
			yield return this;
		}

		// Implement IEnumerable
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}