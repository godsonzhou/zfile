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
			try
			{
				// 检查 OLLAMA 服务是否已启动
				if (!IsOllamaRunning())
				{
					Debug.Print("OLLAMA 服务未启动，正在启动...");
					StartOllama();
					// 等待服务启动，添加超时检查
					int maxWaitTime = 10000; // 最大等待10秒
					int waitInterval = 500; // 每500毫秒检查一次
					int totalWaitTime = 0;
					
					while (!IsOllamaRunning() && totalWaitTime < maxWaitTime)
					{
						await Task.Delay(waitInterval);
						totalWaitTime += waitInterval;
					}
					
					if (!IsOllamaRunning())
					{
						Debug.Print("OLLAMA 服务启动失败或超时，请手动检查。");
						return;
					}
				}

				// 获取已安装的模型列表
				try
				{
					installedModels = await GetInstalledModelsAsync();
					Debug.Print("已安装的模型：");
					foreach (var model in installedModels)
					{
						Debug.Print(model);
					}
					currentModel = newModel.Contains(':') ? newModel : newModel + ":latest"; // 要下载的模型名称
					if (!installedModels.Any(m => m == currentModel)) // 检查是否已安装大模型
					{
						// 下载并安装新模型
						Debug.Print($"正在下载并安装模型: {newModel}...");
						try
						{
							await DownloadAndInstallModelAsync(newModel);
							//更新已安装模型列表
							installedModels = await GetInstalledModelsAsync();
						}
						catch (Exception ex)
						{
							Debug.Print($"下载模型失败: {ex.Message}");
							return;
						}
					}
					
					// 调用 OLLAMA API 与大模型交互
					try
					{
						string prompt = "你好，介绍一下你自己。";
						string response = await CallOllamaApiAsync(prompt);
						Debug.Print($"OLLAMA ({currentModel})响应：");
						Debug.Print(response);
					}
					catch (Exception ex)
					{
						Debug.Print($"调用模型API失败: {ex.Message}");
					}
				}
				catch (Exception ex)
				{
					Debug.Print($"获取模型列表失败: {ex.Message}");
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"准备Ollama环境时发生错误: {ex.Message}");
			}
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
				Debug.Print($"启动 OLLAMA 失败: {ex.Message}");
			}
		}

		// 获取已安装的模型列表
		private static async Task<string[]> GetInstalledModelsAsync()
		{
			string[] modelNames = [];
			try
			{
				// 创建HttpClientHandler以便配置更多选项
				var handler = new HttpClientHandler
				{
					UseProxy = false, // 禁用默认代理
					AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
				};

				using (HttpClient client = new HttpClient(handler))
				{
					client.Timeout = TimeSpan.FromSeconds(10); // 减少超时时间
					client.DefaultRequestHeaders.ConnectionClose = true; // 确保连接关闭，避免连接池问题

					// 添加连接检查
					if (!IsOllamaRunning())
					{
						Debug.Print("OLLAMA服务未运行，无法获取模型列表。");
						return modelNames;
					}

					string url = $"{OllamaApiUrl}/tags"; // 获取模型列表的 API 地址
					Debug.Print($"正在连接到: {url}");

					try
					{
						// 添加取消令牌以确保请求可以被取消，并设置更短的超时时间
						using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
						{
							Debug.Print("开始发送HTTP请求...");
							var task = client.GetAsync(url, cts.Token);

							// 添加超时保护，确保即使网络问题也能返回
							Debug.Print("等待响应...");
							if (await Task.WhenAny(task, Task.Delay(9000)) != task)
							{
								// 如果请求没有在9秒内完成，则取消请求
								cts.Cancel();
								Debug.Print("获取模型列表请求超时，已取消请求");
								return modelNames;
							}
							
							Debug.Print("收到HTTP响应");
							HttpResponseMessage response = await task;
							Debug.Print($"HTTP状态码: {response.StatusCode}");
							response.EnsureSuccessStatusCode();
							
							// 同样为读取内容添加超时保护
							Debug.Print("开始读取响应内容...");
							var readTask = response.Content.ReadAsStringAsync(cts.Token);
							if (await Task.WhenAny(readTask, Task.Delay(3000)) != readTask)
							{
								cts.Cancel();
								Debug.Print("读取响应内容超时，已取消请求");
								return modelNames;
							}
							
							string responseBody = await readTask;
							Debug.Print($"成功读取响应，长度: {responseBody?.Length ?? 0} 字符");
							
							// 检查响应内容是否为空
							if (string.IsNullOrEmpty(responseBody))
							{
								Debug.Print("API返回了空响应");
								return modelNames;
							}

							try
							{
								JObject jsonResponse = JObject.Parse(responseBody);
								if (jsonResponse["models"] == null)
								{
									Debug.Print("API响应中没有models字段");
									return modelNames;
								}

								JArray models = (JArray)jsonResponse["models"];

								modelNames = new string[models.Count];
								for (int i = 0; i < models.Count; i++)
								{
									modelNames[i] = models[i]["name"].ToString();
								}
							}
							catch (Newtonsoft.Json.JsonException ex)
							{
								Debug.Print($"JSON解析失败: {ex.Message}");
								Debug.Print($"原始响应: {responseBody}");
							}
						}
					}
					catch (TaskCanceledException ex)
					{
						Debug.Print($"HTTP请求超时: {ex.Message}");
					}
					catch (HttpRequestException ex)
					{
						Debug.Print($"HTTP请求失败: {ex.Message}");
					}
					catch (Exception ex)
					{
						Debug.Print($"请求失败: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Print($"获取模型列表时发生未预期的错误: {ex.Message}");
			}
			return modelNames;
		}

		// 下载并安装新模型
		private static async Task DownloadAndInstallModelAsync(string modelName)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.Timeout = TimeSpan.FromMinutes(10); // 设置更长的超时时间，因为模型下载可能需要较长时间
					
					var requestBody = new
					{
						name = modelName
					};

					string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
					var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

					string url = $"{OllamaApiUrl}/pull"; // 下载并安装模型的 API 地址
					
					// 使用取消令牌，但设置较长的超时时间
					using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(9)))
					{
						Debug.Print($"开始下载模型 {modelName}，这可能需要几分钟时间...");
						HttpResponseMessage response = await client.PostAsync(url, content, cts.Token);
						response.EnsureSuccessStatusCode();
						Debug.Print($"模型 {modelName} 下载并安装完成。");
					}
				}
			}
			catch (TaskCanceledException ex)
			{
				Debug.Print($"下载模型超时: {ex.Message}");
				throw new Exception($"下载模型 {modelName} 超时，请检查网络连接或手动下载。", ex);
			}
			catch (HttpRequestException ex)
			{
				Debug.Print($"下载模型HTTP请求失败: {ex.Message}");
				throw new Exception($"下载模型 {modelName} 失败，HTTP请求错误: {ex.Message}", ex);
			}
			catch (Exception ex)
			{
				Debug.Print($"下载模型时发生未知错误: {ex.Message}");
				throw new Exception($"下载模型 {modelName} 时发生未知错误: {ex.Message}", ex);
			}
		}

		// 调用 OLLAMA API 与大模型交互
		public async Task<string> CallOllamaApiAsync(string prompt)
		{
			try
			{
				// 创建HttpClientHandler以便配置更多选项
				var handler = new HttpClientHandler
				{
					UseProxy = false, // 禁用默认代理
					AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
				};

				using (HttpClient client = new HttpClient(handler))
				{
					client.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间
					client.DefaultRequestHeaders.ConnectionClose = true; // 确保连接关闭，避免连接池问题
					
					// 检查Ollama服务是否运行
					if (!IsOllamaRunning())
					{
						return "错误：OLLAMA服务未运行，无法调用API。";
					}

					var requestBody = new
					{
						model = currentModel, // 使用的模型名称
						prompt = prompt
					};

					string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
					var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

					string url = $"{OllamaApiUrl}/generate"; // 生成响应的 API 地址
					Debug.Print($"正在连接到: {url}");

					// 使用取消令牌
					using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25)))
					{
						Debug.Print("开始发送HTTP请求...");
						var task = client.PostAsync(url, content, cts.Token);

						// 添加超时保护
						Debug.Print("等待响应...");
						if (await Task.WhenAny(task, Task.Delay(26000)) != task)
						{
							cts.Cancel();
							Debug.Print("API请求超时，已取消请求");
							return "错误：API请求超时，请检查网络连接和Ollama服务状态。";
						}

						Debug.Print("收到HTTP响应");
						HttpResponseMessage response = await task;
						Debug.Print($"HTTP状态码: {response.StatusCode}");
						response.EnsureSuccessStatusCode();

						Debug.Print("开始读取响应内容...");
						var readTask = response.Content.ReadAsStringAsync(cts.Token);
						if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
						{
							cts.Cancel();
							Debug.Print("读取响应内容超时，已取消请求");
							return "错误：读取API响应超时，请检查网络连接和Ollama服务状态。";
						}

						string responseBody = await readTask;
						Debug.Print($"成功读取响应，长度: {responseBody?.Length ?? 0} 字符");
						return responseBody;
					}
				}
			}
			catch (TaskCanceledException ex)
			{
				Debug.Print($"API调用超时: {ex.Message}");
				return $"错误：API调用超时，请检查网络连接和Ollama服务状态。";
			}
			catch (HttpRequestException ex)
			{
				Debug.Print($"HTTP请求失败: {ex.Message}");
				return $"错误：HTTP请求失败，{ex.Message}";
			}
			catch (Exception ex)
			{
				Debug.Print($"调用API时发生错误: {ex.Message}");
				return $"错误：调用API时发生未知错误，{ex.Message}";
			}
		}
	}
}