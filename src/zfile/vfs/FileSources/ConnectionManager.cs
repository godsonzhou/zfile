using Zfile.FileSources;
namespace Zfile
{
    public class FileSourceRecord
    {
        public int Count { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public IFileSource FileSource { get; set; }
    }

    public static class ConnectionManager
    {
        private static readonly Dictionary<string, FileSourceRecord> WfxConnectionList = new Dictionary<string, FileSourceRecord>();
        private static ContextMenuStrip ContextMenu;

        static ConnectionManager()
        {
            ContextMenu = new ContextMenuStrip();
            var disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += OnNetworkDisconnect;
            ContextMenu.Items.Add(disconnectItem);
        }

        public static void AddNetworkConnection(string name, string path, IFileSource fileSource)
        {
            lock (WfxConnectionList)
            {
                string key = GetNetworkPath(name, path);
                if (WfxConnectionList.TryGetValue(key, out FileSourceRecord record))
                {
                    record.Count++;
                }
                else
                {
                    WfxConnectionList[key] = new FileSourceRecord
                    {
                        Count = 1,
                        Name = name,
                        Path = path,
                        FileSource = fileSource
                    };
                }

                UpdateDriveList();
            }
        }

        public static void RemoveNetworkConnection(string name, string path)
        {
            lock (WfxConnectionList)
            {
                string key = GetNetworkPath(name, path);
                if (WfxConnectionList.TryGetValue(key, out FileSourceRecord record))
                {
                    record.Count--;
                    if (record.Count <= 0)
                    {
                        WfxConnectionList.Remove(key);
                    }

                    UpdateDriveList();
                }
            }
        }

        public static void CloseNetworkConnection(string name, string path = "")
        {
            lock (WfxConnectionList)
            {
                if (string.IsNullOrEmpty(path))
                {
                    var connections = WfxConnectionList.Where(c => c.Value.Name == name).ToList();
                    foreach (var connection in connections)
                    {
                        DisconnectWfxConnection(connection.Value);
                        WfxConnectionList.Remove(connection.Key);
                    }
                }
                else
                {
                    string key = GetNetworkPath(name, path);
                    if (WfxConnectionList.TryGetValue(key, out FileSourceRecord record))
                    {
                        DisconnectWfxConnection(record);
                        WfxConnectionList.Remove(key);
                    }
                }

                UpdateDriveList();
            }
        }

        private static void DisconnectWfxConnection(FileSourceRecord record)
        {
            if (record.FileSource is IWfxPluginFileSource wfxFileSource)
            {
                wfxFileSource.WfxModule.Disconnect(record.Path);
            }
        }

        private static string GetNetworkPath(string name, string path)
        {
            return $"{name}:{path}";
        }

        public static void UpdateDriveList()
        {
            // Update the drive list in the main form
            // This would typically update a UI component that shows available drives
            // including the virtual network drives
            
            // In a real implementation, this would update the main form's drive list
            // For example:
            // MainForm.Instance.UpdateDriveList(WfxConnectionList);
        }

        public static void ShowVirtualDriveMenu(Control control, Point location, string name, string path)
        {
            string key = GetNetworkPath(name, path);
            if (WfxConnectionList.ContainsKey(key))
            {
                ContextMenu.Tag = key;
                ContextMenu.Show(control, location);
            }
        }

        private static void OnNetworkDisconnect(object sender, EventArgs e)
        {
            if (ContextMenu.Tag is string key && WfxConnectionList.TryGetValue(key, out FileSourceRecord record))
            {
                CloseNetworkConnection(record.Name, record.Path);
            }
        }
    }

    public interface IWfxPluginFileSource : IFileSource
    {
        WfxModule WfxModule { get; }
    }
}