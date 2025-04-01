using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zfile
{
	using System;
	using System.Net.Http;
	using System.Text;
	using System.Text.Json;
	using System.Threading.Tasks;

	// 与 OpenAI 兼容的通用请求/响应结构
	public class OpenAIRequest
	{
		public string model { get; set; }
		public Message[] messages { get; set; }
		public float? temperature { get; set; }
		public int? max_tokens { get; set; }
		// 可扩展其他参数：stream, top_p, frequency_penalty 等
	}

	public class Message
	{
		public string role { get; set; }
		public string content { get; set; }
	}

	public class OpenAIResponse
	{
		public string id { get; set; }
		public Choice[] choices { get; set; }
		public ErrorDetail error { get; set; }
	}

	public class Choice
	{
		public Message message { get; set; }
		public string finish_reason { get; set; }
	}

	public class ErrorDetail
	{
		public string message { get; set; }
		public string type { get; set; }
	}

	// 通用 OpenAI 兼容客户端
	public class OpenAIClient
	{
		private readonly HttpClient _httpClient;
		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		public OpenAIClient(
			string apiKey,
			string baseUrl = "https://api.openai.com/v1/",
			string apiKeyHeader = "Authorization")
		{
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(baseUrl)
			};

			// 支持不同认证方式（如 "Authorization" 或 "Api-Key"）
			_httpClient.DefaultRequestHeaders.Add(apiKeyHeader,
				apiKeyHeader == "Authorization" ? $"Bearer {apiKey}" : apiKey);
		}

		public async Task<OpenAIResponse> CreateChatCompletionAsync(OpenAIRequest request)
		{
			try
			{
				var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
				var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync("chat/completions", content);
				var jsonResponse = await response.Content.ReadAsStringAsync();

				if (!response.IsSuccessStatusCode)
				{
					// 尝试解析错误信息
					var errorResponse = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse, _jsonOptions);
					throw new HttpRequestException(
						$"API Error ({response.StatusCode}): {errorResponse?.error?.message ?? "Unknown error"}");
				}

				return JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse, _jsonOptions);
			}
			catch (JsonException ex)
			{
				throw new Exception($"JSON解析失败: {ex.Message}");
			}
		}
	}

	// 使用示例
	class openai_api
	{
		static async Task run(string url, string key)
		{
			// 示例1：调用原生OpenAI
			var openaiClient = new OpenAIClient(
				apiKey: key,
				baseUrl: string.IsNullOrEmpty(url) ? "https://api.openai.com/v1/" : url
			);

			// 示例2：调用兼容服务商（如 LocalAI）
			var localaiClient = new OpenAIClient(
				apiKey: key,
				baseUrl: "http://localhost:8080/v1/",
				apiKeyHeader: "Authorization" // 根据服务商要求调整
			);

			var request = new OpenAIRequest
			{
				model = "gpt-3.5-turbo", // 根据服务商支持的模型调整
				messages = new[]
				{
				new Message { role = "user", content = "你好，请用C#写个冒泡排序" }
			},
				temperature = 0.7f,
				max_tokens = 500
			};

			try
			{
				var response = await openaiClient.CreateChatCompletionAsync(request);

				if (response?.choices?.Length > 0)
				{
					Console.WriteLine(response.choices[0].message.content);
				}
				else if (response?.error != null)
				{
					Console.WriteLine($"错误: {response.error.message}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"请求失败: {ex.Message}");
			}
		}
	}
}
