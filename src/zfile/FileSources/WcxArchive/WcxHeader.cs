using System;
using System.IO;

namespace ZFile.FileSources.WcxArchive
{
    public class WcxHeader
    {
        public string FileName { get; set; }
        public long PackSize { get; set; }
        public long UnpSize { get; set; }
        public FileAttributes FileAttr { get; set; }
        public uint FileTime { get; set; }
        public int CRC { get; set; }
        public string Method { get; set; }

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
                Method = Method
            };
        }
    }
}