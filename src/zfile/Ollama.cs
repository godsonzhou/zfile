using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WinFormsApp1
{
	public class LLM_Helper
	{
		private static readonly string OllamaApiUrl = "http://localhost:11434/api"; // OLLAMA API 基础地址
		private static readonly string OllamaProcessName = "ollama"; // OLLAMA 进程名称
		private static readonly string OllamaExePath = @"%LOCALAPPDATA%\Programs\ollama.exe"; // OLLAMA 可执行文件路径
		private string[] installedModels;
		public string currentModel { get ; private set; } = string.Empty;
		public bool IsPrepared { get => !currentModel.Equals(string.Empty); }
		public async Task Prepare(string newModel="qwq")
		{
			// 检查 OLLAMA 服务是否已启动
			if (!IsOllamaRunning())
			{
				Console.WriteLine("OLLAMA 服务未启动，正在启动...");
				StartOllama();
				await Task.Delay(5000); // 等待服务启动
			}

			// 获取已安装的模型列表
			installedModels = await GetInstalledModelsAsync();
			Console.WriteLine("已安装的模型：");
			foreach (var model in installedModels)
			{
				Console.WriteLine(model);
			}
			currentModel = newModel; // 要下载的模型名称
			if (!installedModels.Any(m => m == newModel)) // 检查是否已安装大模型
			{
				// 下载并安装新模型
				Console.WriteLine($"正在下载并安装模型: {newModel}...");
				await DownloadAndInstallModelAsync(newModel);
			}
			//TODO: ADD NEW MODEL TO INSTALLED MODELS
			installedModels = await GetInstalledModelsAsync();
			// 调用 OLLAMA API 与大模型交互
			string prompt = "你好，介绍一下你自己。";
			string response = await CallOllamaApiAsync(prompt);

			Console.WriteLine($"OLLAMA ({currentModel})响应：");
			Console.WriteLine(response);
		}

		// 检查 OLLAMA 服务是否已启动
		private static bool IsOllamaRunning()
		{
			Process[] processes = Process.GetProcessesByName(OllamaProcessName);
			return processes.Length > 0;
		}

		// 启动 OLLAMA 服务
		private static void StartOllama()
		{
			try
			{
				Process.Start(OllamaExePath);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"启动 OLLAMA 失败: {ex.Message}");
			}
		}

		// 获取已安装的模型列表
		private static async Task<string[]> GetInstalledModelsAsync()
		{
			string[] modelNames = [];
			using (HttpClient client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间
				string url = $"{OllamaApiUrl}/tags"; // 获取模型列表的 API 地址
				try
				{
					HttpResponseMessage response = await client.GetAsync(url);
					response.EnsureSuccessStatusCode();
					string responseBody = await response.Content.ReadAsStringAsync();
					JObject jsonResponse = JObject.Parse(responseBody);
					JArray models = (JArray)jsonResponse["models"];

					modelNames = new string[models.Count];
					for (int i = 0; i < models.Count; i++)
					{
						modelNames[i] = models[i]["name"].ToString();
					}

					return modelNames;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"请求失败: {ex.Message}");
				}
			}
			return modelNames;
		}

		// 下载并安装新模型
		private static async Task DownloadAndInstallModelAsync(string modelName)
		{
			using (HttpClient client = new HttpClient())
			{
				var requestBody = new
				{
					name = modelName
				};

				string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
				var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

				string url = $"{OllamaApiUrl}/pull"; // 下载并安装模型的 API 地址
				HttpResponseMessage response = await client.PostAsync(url, content);
				response.EnsureSuccessStatusCode();

				Console.WriteLine($"模型 {modelName} 下载并安装完成。");
			}
		}

		// 调用 OLLAMA API 与大模型交互
		public async Task<string> CallOllamaApiAsync(string prompt)
		{
			using (HttpClient client = new HttpClient())
			{
				var requestBody = new
				{
					model = currentModel, // 使用的模型名称
					prompt = prompt
				};

				string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
				var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

				string url = $"{OllamaApiUrl}/generate"; // 生成响应的 API 地址
				HttpResponseMessage response = await client.PostAsync(url, content);
				response.EnsureSuccessStatusCode();

				string responseBody = await response.Content.ReadAsStringAsync();
				return responseBody;
			}
		}
	}
}