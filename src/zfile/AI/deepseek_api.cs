using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// 定义请求数据结构
public class DeepSeekRequest
{
	public string model { get; set; }
	public Message[] messages { get; set; }
	public double temperature { get; set; }
	public int max_tokens { get; set; }
}

public class Message
{
	public string role { get; set; }
	public string content { get; set; }
}

// 定义响应数据结构
public class DeepSeekResponse
{
	public Choice[] choices { get; set; }
	// 可根据官方文档添加其他字段
}

public class Choice
{
	public Message message { get; set; }
	// 其他可能的字段
}

public class DeepSeekApiClient
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;

	public DeepSeekApiClient(string apiKey, string baseAddress = "https://api.deepseek.com/v1/")
	{
		_apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

		_httpClient = new HttpClient
		{
			BaseAddress = new Uri(baseAddress)
		};

		// 设置认证头
		_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
	}

	public async Task<DeepSeekResponse> CreateChatCompletionAsync(DeepSeekRequest request)
	{
		try
		{
			// 序列化请求数据
			var jsonRequest = JsonSerializer.Serialize(request);
			var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

			// 发送POST请求
			var response = await _httpClient.PostAsync("chat/completions", content);

			// 确保成功响应
			response.EnsureSuccessStatusCode();

			// 读取并反序列化响应内容
			var jsonResponse = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<DeepSeekResponse>(jsonResponse);
		}
		catch (HttpRequestException ex)
		{
			// 处理HTTP请求错误
			Console.WriteLine($"HTTP Error: {ex.Message}");
			throw;
		}
		catch (Exception ex)
		{
			// 处理其他错误
			Console.WriteLine($"Error: {ex.Message}");
			throw;
		}
	}
}

// 使用示例
class deepseek_api
{
	static async Task run(string[] args)
	{
		const string apiKey = "sk-2d1c28f9c7344043813f76ec31b791ac";
		var client = new DeepSeekApiClient(apiKey);

		var request = new DeepSeekRequest
		{
			model = "deepseek-chat",
			messages = new[]
			{
				new Message { role = "user", content = "Hello!" }
			},
			temperature = 0.7,
			max_tokens = 4000
		};

		try
		{
			var response = await client.CreateChatCompletionAsync(request);
			if (response?.choices?.Length > 0)
			{
				Console.WriteLine("API Response:");
				Console.WriteLine(response.choices[0].message.content);
			}
			else
			{
				Console.WriteLine("No response received");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error occurred: {ex.Message}");
		}
	}
}