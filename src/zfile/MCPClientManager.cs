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
using System.Diagnostics;

namespace zfile
{
    public class MCPClientManager
    {
        private Dictionary<string, MCPClient> mcpClients;
        private readonly string configPath;
        private MCPSettings settings;
		public List<string> allMCPTools = new();
        private Dictionary<string, List<string>> MCPToolsDict = new();

		public MCPClientManager(string configPath)
        {
            this.configPath = configPath;
            mcpClients = new Dictionary<string, MCPClient>();
            LoadSettings();
			//connect to each mcp server to get the server's tools
			Task.Run(async () => {
                await GetAllMcpTools();
                Debug.Print($"MCP工具数量: {allMCPTools.Count}");
            });
			//var ServerList = GetServerNames();
			//List<Task<bool>> tasks = new();
			//foreach(var server in ServerList)
			//	tasks.Add(ConnectToServer(server));
			//Task.WaitAll(tasks.ToArray());
			//foreach (var server in ServerList)
			//	availableTools.AddRange(Task.Run(async () => await GetAvailableTools(server)).GetAwaiter().GetResult());
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

        public async Task<bool> ConnectToServerInConfig(string serverName)
        {
            if (settings.MCPServers.TryGetValue(serverName, out var serverConfig))
            {
                try
                {
                    var client = new MCPClient("aiclient", "1.0.0", serverConfig.Command, string.Join(' ', serverConfig.Args));
					//client.ConnectAsync(serverConfig.Command, serverConfig.Args);
					// 等待工具列表初始化完成
					await client.GetToolsAsync();
					//var prompts = await client.GetPromptListAsync();
					//var resources = await client.GetResourcesAsync();
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
					Debug.Print($"获取服务器{serverName}支持的所有工具");
					var capabilities = await client.GetFunctionsAsync();// client.GetCapabilitiesAsync();
					foreach (var capability in capabilities)
					{
						var cap = $"{capability.Name} : {capability.Description} : {capability.JsonSchema.ToString()}";
						Debug.Print(cap);
						tools.Add(cap);
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

        public async Task<List<string>> GetAllMcpTools()
        {
            allMCPTools.Clear();
            MCPToolsDict.Clear();
            
            var serverNames = GetServerNames();
            List<Task> connectionTasks = new List<Task>();
            
            foreach (var serverName in serverNames)
            {
                connectionTasks.Add(ProcessServerAsync(serverName));
            }
            
            await Task.WhenAll(connectionTasks);
            return allMCPTools;
        }
        
        private async Task ProcessServerAsync(string serverName)
        {
			Debug.Print($"start connect to server: {serverName}");
            bool connected = await ConnectToServerInConfig(serverName);
            if (connected)
            {
				Debug.Print($"server: {serverName} connected, start to get available tools...");
				var tools = await GetAvailableTools(serverName);
                if (tools.Count > 0)
                {
					Debug.Print($"server: {serverName} available tool : {tools.Count}");
					lock (MCPToolsDict)
                    {
                        MCPToolsDict[serverName] = tools;
                        allMCPTools.AddRange(tools);
                    }
                }
            }
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