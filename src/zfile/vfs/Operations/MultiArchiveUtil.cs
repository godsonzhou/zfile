using System;
using System.IO;
using System.Text;

namespace MultiArchive
{
    public static class MultiArchiveUtil
    {
        public static string GetTempFileName(string extension)
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + extension);
        }

        public static string GetTempDirName()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        public static string GetArchiveFileName(string fileName)
        {
            return Path.GetFileName(fileName);
        }

        public static string GetArchivePath(string fileName)
        {
            return Path.GetDirectoryName(fileName);
        }

        public static string GetArchiveExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLower();
        }

        public static bool IsArchiveFile(string fileName)
        {
            var extension = GetArchiveExtension(fileName);
            return !string.IsNullOrEmpty(extension) && 
                   GlobalSettings.ArchiveExtensions.Contains(extension);
        }

        public static string GetPasswordFromEnvironment()
        {
            return Environment.GetEnvironmentVariable("MULTIARCHIVE_PASSWORD");
        }

        public static string EncodePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(bytes);
        }

        public static string DecodePassword(string encodedPassword)
        {
            if (string.IsNullOrEmpty(encodedPassword))
                return string.Empty;

            var bytes = Convert.FromBase64String(encodedPassword);
            return Encoding.UTF8.GetString(bytes);
        }

        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
} 