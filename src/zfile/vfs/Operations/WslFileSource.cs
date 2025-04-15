using System;
using System.Runtime.InteropServices;

namespace FileSystemOperations
{
    public class WslFileSource : WinNetFileSource
    {
        public override string GetParentDir(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public override bool IsPathAtRoot(string path)
        {
            path = Path.Combine(path, "").ToLower();
            return path == "\\\\wsl$\\" || path == "\\\\wsl.localhost\\";
        }

        public override string GetRootDir(string path)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
                return "\\\\wsl.localhost\\";
            return "\\\\wsl$\\";
        }

        public override string GetRootDir()
        {
            return GetRootDir(string.Empty);
        }

        public static bool Available()
        {
            return GetServiceStatus("LxssManager") != 0;
        }

        public static bool IsSupportedPath(string path)
        {
            path = Path.Combine(path, "").ToLower();
            return path.StartsWith("\\\\wsl$\\") || path.StartsWith("\\\\wsl.localhost\\");
        }

        public static bool GetMainIcon(out string path)
        {
            if (IsWow64())
                path = "%SystemRoot%\\Sysnative\\wsl.exe";
            else
                path = "%SystemRoot%\\System32\\wsl.exe";
            return true;
        }

        public override IFileSourceOperation CreateListOperation(string targetPath)
        {
            return new WslListOperation(this, targetPath);
        }

        private static int GetServiceStatus(string serviceName)
        {
            // Implementation of service status check
            return 0;
        }

        private static bool IsWow64()
        {
            // Implementation of WOW64 check
            return false;
        }
    }
} 