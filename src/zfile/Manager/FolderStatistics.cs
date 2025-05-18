using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zfile
{
    /// <summary>
    /// Class for calculating and caching folder statistics for view rule evaluation
    /// </summary>
    public class FolderStatistics
    {
        // Cache of folder statistics to avoid redundant calculations
        private static Dictionary<string, FolderStats> _statsCache = new Dictionary<string, FolderStats>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();

        // Maximum cache size to prevent memory issues
        private const int MAX_CACHE_SIZE = 100;

        /// <summary>
        /// Statistics for a folder
        /// </summary>
        public class FolderStats
        {
            // Basic counts
            public int TotalFiles { get; set; }
            public int TotalFolders { get; set; }
            
            // File type counts
            public int ImageCount { get; set; }
            public int VideoCount { get; set; }
            public int AudioCount { get; set; }
            public int DocumentCount { get; set; }
            public int ArchiveCount { get; set; }
            public int ExecutableCount { get; set; }
            public int SourceCodeCount { get; set; }
            
            // Size statistics
            public long TotalSize { get; set; }
            public long MaxFileSize { get; set; }
            public long MinFileSize { get; set; } = long.MaxValue;
            public double AverageFileSize { get; set; }
            
            // Date statistics
            public DateTime NewestFile { get; set; } = DateTime.MinValue;
            public DateTime OldestFile { get; set; } = DateTime.MaxValue;
            
            // Special folder flags
            public bool IsNetworkPath { get; set; }
            public bool IsVirtualFolder { get; set; }
            public bool IsFtpFolder { get; set; }
            public bool IsArchiveFolder { get; set; }
            public bool IsPluginFolder { get; set; }
            public bool IsSearchResult { get; set; }
            
            // Dominant file type (the most common file type in the folder)
            public string DominantFileType { get; set; } = string.Empty;
            
            // Last calculation time
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            
            // Calculate the dominant file type based on extension counts
            public void CalculateDominantType(Dictionary<string, int> extensionCounts)
            {
                if (extensionCounts.Count == 0)
                {
                    DominantFileType = string.Empty;
                    return;
                }
                
                DominantFileType = extensionCounts.OrderByDescending(x => x.Value).First().Key;
            }
        }

        /// <summary>
        /// Get statistics for a folder, using cached data if available and not expired
        /// </summary>
        public static FolderStats GetFolderStats(string folderPath, IFileSource fileSource, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(folderPath))
                return new FolderStats();
                
            // Normalize path for cache lookup
            string normalizedPath = folderPath.TrimEnd('\\', '/');
            
            lock (_cacheLock)
            {
                // Check if we have cached stats that are still valid
                if (!forceRefresh && _statsCache.TryGetValue(normalizedPath, out FolderStats? cachedStats))
                {
                    // Use cached stats if they're less than 30 seconds old
                    if ((DateTime.Now - cachedStats.LastUpdated).TotalSeconds < 30)
                    {
                        return cachedStats;
                    }
                }
                
                // Calculate new stats
                FolderStats stats = CalculateFolderStats(folderPath, fileSource);
                
                // Update cache
                _statsCache[normalizedPath] = stats;
                
                // Trim cache if it gets too large
                if (_statsCache.Count > MAX_CACHE_SIZE)
                {
                    // Remove oldest entries
                    var oldestEntries = _statsCache
                        .OrderBy(x => x.Value.LastUpdated)
                        .Take(_statsCache.Count - MAX_CACHE_SIZE)
                        .Select(x => x.Key)
                        .ToList();
                        
                    foreach (var key in oldestEntries)
                    {
                        _statsCache.Remove(key);
                    }
                }
                
                return stats;
            }
        }
        
        /// <summary>
        /// Calculate statistics for a folder
        /// </summary>
        private static FolderStats CalculateFolderStats(string folderPath, IFileSource fileSource)
        {
            FolderStats stats = new FolderStats();
            Dictionary<string, int> extensionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                // Set special folder flags based on file source type
                stats.IsNetworkPath = folderPath.StartsWith("\\\\");
                stats.IsFtpFolder = fileSource is FtpFileSource;
                stats.IsArchiveFolder = fileSource is WcxArchiveFileSource;
                stats.IsVirtualFolder = fileSource is ShellFileSource || fileSource is ControlPanelFileSource;
                stats.IsPluginFolder = fileSource is WfxPluginFileSource;
                
                // Get file list from file source
                var listOperation = fileSource.CreateListOperation(folderPath);
                if (listOperation != null)
                {
                    OperationsManager.Instance.AddOperation(listOperation);
                    listOperation._Thread.WaitFor();
                    
                    if (listOperation is FileSourceListOperation fileListOperation && fileListOperation.Files != null)
                    {
                        foreach (var file in fileListOperation.Files)
                        {
                            // Skip . and .. entries
                            if (file.Name == "." || file.Name == "..")
                                continue;
                                
                            if (file.IsDirectory)
                            {
                                stats.TotalFolders++;
                            }
                            else
                            {
                                stats.TotalFiles++;
                                stats.TotalSize += file.Size;
                                
                                // Update min/max file size
                                if (file.Size > stats.MaxFileSize)
                                    stats.MaxFileSize = file.Size;
                                if (file.Size < stats.MinFileSize)
                                    stats.MinFileSize = file.Size;
                                
                                // Update date statistics
                                if (file.ModificationTime > stats.NewestFile)
                                    stats.NewestFile = file.ModificationTime;
                                if (file.ModificationTime < stats.OldestFile)
                                    stats.OldestFile = file.ModificationTime;
                                
                                // Count file by extension
                                string ext = Path.GetExtension(file.Name).ToLowerInvariant();
                                if (!string.IsNullOrEmpty(ext))
                                {
                                    if (!extensionCounts.ContainsKey(ext))
                                        extensionCounts[ext] = 0;
                                    extensionCounts[ext]++;
                                    
                                    // Categorize file by type
                                    CategorizeFileByExtension(stats, ext);
                                }
                            }
                        }
                        
                        // Calculate average file size
                        if (stats.TotalFiles > 0)
                            stats.AverageFileSize = (double)stats.TotalSize / stats.TotalFiles;
                            
                        // Determine dominant file type
                        stats.CalculateDominantType(extensionCounts);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error calculating folder statistics: {ex.Message}");
            }
            
            stats.LastUpdated = DateTime.Now;
            return stats;
        }
        
        /// <summary>
        /// Categorize a file by its extension
        /// </summary>
        private static void CategorizeFileByExtension(FolderStats stats, string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                // Image files
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                case ".tiff":
                case ".webp":
                case ".svg":
                case ".ico":
                    stats.ImageCount++;
                    break;
                
                // Video files
                case ".mp4":
                case ".avi":
                case ".mkv":
                case ".mov":
                case ".wmv":
                case ".flv":
                case ".webm":
                case ".m4v":
                case ".mpg":
                case ".mpeg":
                    stats.VideoCount++;
                    break;
                
                // Audio files
                case ".mp3":
                case ".wav":
                case ".flac":
                case ".aac":
                case ".ogg":
                case ".wma":
                case ".m4a":
                case ".mid":
                case ".midi":
                    stats.AudioCount++;
                    break;
                
                // Document files
                case ".pdf":
                case ".doc":
                case ".docx":
                case ".xls":
                case ".xlsx":
                case ".ppt":
                case ".pptx":
                case ".txt":
                case ".rtf":
                case ".odt":
                case ".ods":
                case ".odp":
                    stats.DocumentCount++;
                    break;
                
                // Archive files
                case ".zip":
                case ".rar":
                case ".7z":
                case ".tar":
                case ".gz":
                case ".bz2":
                case ".iso":
                case ".cab":
                    stats.ArchiveCount++;
                    break;
                
                // Executable files
                case ".exe":
                case ".dll":
                case ".bat":
                case ".cmd":
                case ".msi":
                case ".com":
                    stats.ExecutableCount++;
                    break;
                
                // Source code files
                case ".cs":
                case ".java":
                case ".py":
                case ".js":
                case ".html":
                case ".css":
                case ".php":
                case ".c":
                case ".cpp":
                case ".h":
                case ".hpp":
                case ".go":
                case ".rb":
                case ".ts":
                case ".swift":
                case ".kt":
                case ".rs":
                    stats.SourceCodeCount++;
                    break;
            }
        }
        
        /// <summary>
        /// Clear the statistics cache
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _statsCache.Clear();
            }
        }
    }
}
