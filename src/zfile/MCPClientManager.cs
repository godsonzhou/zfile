using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPSharp;
using MCPSharp.Model;
using MCPSharp.Model.Schemas;
using Newtonsoft.Json;
using System.IO;

namespace zfile
{
    public class MCPClientManager
    {
        private Dictionary<string, MCPClient> mcpClients;
        private readonly string configPath;
        private MCPSettings settings;

        public MCPClientManager(string configPath)
        {
            this.configPath = configPath;
            mcpClients = new Dictionary<string, MCPClient>();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                settings = JsonConvert.DeserializeObject<MCPSettings>(json);
            }
            else
            {
                settings = new MCPSettings();
            }
        }

        public async Task<bool> ConnectToServer(string serverName)
        {
            if (settings.MCPServers.TryGetValue(serverName, out var serverConfig))
            {
                try
                {
                    var client = new MCPClient();
                    await client.ConnectAsync(serverConfig.Command, serverConfig.Args);
                    mcpClients[serverName] = client;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"连接到服务器 {serverName} 失败: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public async Task<bool> CheckServerStatus(string serverName)
        {
            if (mcpClients.TryGetValue(serverName, out var client))
            {
                try
                {
                    // 尝试执行一个简单的查询来检查服务器状态
                    await client.DescribeTableAsync("dummy");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<List<string>> GetAvailableTools(string serverName)
        {
            if (mcpClients.TryGetValue(serverName, out var client))
            {
                try
                {
                    var tools = new List<string>();
                    // 获取服务器支持的所有工具
                    var capabilities = await client.GetCapabilitiesAsync();
                    foreach (var capability in capabilities)
                    {
                        tools.Add(capability.Key);
                    }
                    return tools;
                }
                catch
                {
                    return new List<string>();
                }
            }
            return new List<string>();
        }

        public MCPClient GetClient(string serverName)
        {
            return mcpClients.TryGetValue(serverName, out var client) ? client : null;
        }

        public IEnumerable<string> GetServerNames()
        {
            return settings.MCPServers.Keys;
        }
    }

    public class MCPSettings
    {
        [JsonProperty("mcpServers")]
        public Dictionary<string, MCPServerConfig> MCPServers { get; set; } = new Dictionary<string, MCPServerConfig>();
    }

    public class MCPServerConfig
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("args")]
        public string[] Args { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("autoApprove")]
        public string[] AutoApprove { get; set; }
    }
}