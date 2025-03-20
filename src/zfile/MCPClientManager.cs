using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPSharp;
using MCPSharp.Model;
using MCPSharp.Model.Schemas;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.AI;
using System.Collections;

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
                    var client = new MCPClient("aiclient", "1.0.0", serverConfig.Command, string.Join(' ', serverConfig.Args));
					//client.ConnectAsync(serverConfig.Command, serverConfig.Args);
					//IList<AIFunction> functions = await client.GetFunctionsAsync();
					//var prompts = await client.GetPromptListAsync();
					//var resources = await client.GetResourcesAsync();
					//var tools = await client.GetToolsAsync();
					//var resourceTemplates = await client.GetResourceTemplatesAsync();
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
					await client.SendPingAsync();//client.DescribeTableAsync("dummy");
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
					var capabilities = await client.GetFunctionsAsync();// client.GetCapabilitiesAsync();
                    foreach (var capability in capabilities)
                    {
                        tools.Add(capability.JsonSchema.ToString());
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
	class McpServerConfiguration
	{
		public required string Command { get; set; }
		public string[] Args { get; set; } = [];
		public Dictionary<string, string> Env { get; set; } = [];
	}
	class MCPClientPool : ICollection<MCPClient>
	{
		private readonly List<MCPClient> clients = [];

		public List<AITool> GetAllAIFunctions()
		{
			var functions = new List<AITool>();
			clients.ForEach(c => functions.AddRange(c.GetFunctionsAsync().Result));
			return functions;
		}

		public int Count => clients.Count;
		public bool IsReadOnly => false;
		public void Add(string name, McpServerConfiguration server, Func<Dictionary<string, object>, bool> permissionFunction = null)
		{
			clients.Add(new MCPClient(name, "0.1.0", server.Command, string.Join(' ', server.Args ?? []), server.Env)
			{
				GetPermission = permissionFunction ?? ((parameters) => true)
			});
		}

		public void Add(MCPClient item) => clients.Add(item);
		public void Clear() => clients.Clear();
		public bool Contains(MCPClient item) => clients.Contains(item);
		public void CopyTo(MCPClient[] array, int arrayIndex) => clients.CopyTo(array, arrayIndex);
		public IEnumerator<MCPClient> GetEnumerator() => clients.GetEnumerator();
		public bool Remove(MCPClient item) => clients.Remove(item);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}