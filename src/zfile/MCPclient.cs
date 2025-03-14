using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using zfile;
using System.Diagnostics;

namespace zfile
{
	public class MCP
	{
		public static async Task Launcher(string[] args)
		{
			var client = new McpClient("http://localhost:5000/mcp");

			try
			{
				// 1. 创建会话
				var session = await client.CreateSessionAsync(new CreateSessionRequest
				{
					ModelType = "gpt-4",
					Parameters = new Dictionary<string, object> { { "temperature", 0.7 } }
				});
				Debug.Print($"Session created: {session.SessionId}");

				// 2. 更新上下文
				await client.UpdateContextAsync(new UpdateContextRequest
				{
					SessionId = session.SessionId,
					ContextData = new Dictionary<string, object>
				{
					{ "user_id", "123" },
					{ "preferences", new { language = "en" } }
				}
				});

				// 3. 执行查询
				var queryResult = await client.ExecuteQueryAsync(new QueryRequest
				{
					SessionId = session.SessionId,
					Input = "What's the weather today?"
				});
				Debug.Print($"AI Response: {queryResult.Output}");

				// 4. 关闭会话
				await client.CloseSessionAsync(session.SessionId);
			}
			catch (HttpRequestException ex)
			{
				Debug.Print($"HTTP Error: {ex.Message}");
			}
			catch (Exception ex)
			{
				Debug.Print($"Error: {ex.Message}");
			}
		}
	}
	// 请求模型
	public class CreateSessionRequest
	{
		public string ModelType { get; set; }  // 如 "gpt-4", "llama2"
		public Dictionary<string, object> Parameters { get; set; }
	}

	public class UpdateContextRequest
	{
		public string SessionId { get; set; }
		public Dictionary<string, object> ContextData { get; set; }
	}

	public class QueryRequest
	{
		public string SessionId { get; set; }
		public string Input { get; set; }
	}

	// 响应模型
	public class SessionResponse
	{
		public string SessionId { get; set; }
		public string Status { get; set; }
	}

	public class QueryResponse
	{
		public string Output { get; set; }
		public Dictionary<string, object> Metadata { get; set; }
	}

	public class McpClient : IDisposable
	{
		private readonly HttpClient _httpClient;
		private readonly string _baseUrl;

		public McpClient(string baseUrl)
		{
			_httpClient = new HttpClient();
			_baseUrl = baseUrl.TrimEnd('/');
		}

		// 创建会话
		public async Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request)
		{
			var url = $"{_baseUrl}/sessions";
			var content = new StringContent(
				JsonSerializer.Serialize(request),
				Encoding.UTF8,
				"application/json"
			);

			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();

			var responseBody = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<SessionResponse>(responseBody);
		}

		// 更新上下文
		public async Task UpdateContextAsync(UpdateContextRequest request)
		{
			var url = $"{_baseUrl}/sessions/{request.SessionId}/context";
			var content = new StringContent(
				JsonSerializer.Serialize(request.ContextData),
				Encoding.UTF8,
				"application/json"
			);

			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();
		}

		// 执行查询
		public async Task<QueryResponse> ExecuteQueryAsync(QueryRequest request)
		{
			var url = $"{_baseUrl}/sessions/{request.SessionId}/query";
			var content = new StringContent(
				JsonSerializer.Serialize(new { input = request.Input }),
				Encoding.UTF8,
				"application/json"
			);

			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();

			var responseBody = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<QueryResponse>(responseBody);
		}

		// 关闭会话
		public async Task CloseSessionAsync(string sessionId)
		{
			var url = $"{_baseUrl}/sessions/{sessionId}";
			var response = await _httpClient.DeleteAsync(url);
			response.EnsureSuccessStatusCode();
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}