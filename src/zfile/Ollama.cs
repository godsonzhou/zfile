using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MCPSharp;

namespace zfile
{
	public class AIassistDlg : Form
	{
		private LLM_Helper LLMhelper;
		List<string> filelist;
		private ComboBox cboModels;
		private Button btnRefresh;
		private CheckBox chkboxSave;
		private ListView lstFiles;
		private TextBox txtPrompt;
		private Button btnSend;
		private Button btnClose;
		private CmdProc cmdProc;
		public AIassistDlg(List<string> files, LLM_Helper llm, CmdProc cmdproc)
		{
			LLMhelper = llm;
			filelist = files;
			InitializeComponents();
			LoadModels();
			LoadFiles();
			this.cmdProc = cmdproc;
		}

		private void InitializeComponents()
		{
			this.Text = "AI 助手";
			this.Size = new Size(600, 500);
			this.StartPosition = FormStartPosition.CenterParent;

			// 模型选择区域
			var lblModel = new Label
			{
				Text = "选择模型：",
				Location = new Point(10, 15),
				AutoSize = true
			};

			cboModels = new ComboBox
			{
				Location = new Point(80, 12),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			btnRefresh = new Button
			{
				Text = "刷新",
				Location = new Point(290, 12),
				Width = 60
			};
			btnRefresh.Click += BtnRefresh_Click;

			chkboxSave = new CheckBox
			{
				Text = "保存结果到文件备注",
				Location = new Point(450, 12),
				Width = 260,
				Checked = true
			};

			// 文件列表
			lstFiles = new ListView
			{
				Location = new Point(10, 50),
				Size = new Size(565, 250),
				CheckBoxes = true,
				View = View.Details
			};
			lstFiles.Columns.Add("文件", 280);
			lstFiles.Columns.Add("处理结果", 280);

			// 提示词输入
			txtPrompt = new TextBox
			{
				Location = new Point(10, 320),
				Size = new Size(565, 80),
				Multiline = true,
				ScrollBars = ScrollBars.Vertical,
				Text = "开始处理以下文件或文件夹"
			};

			// 按钮区域
			btnSend = new Button
			{
				Text = "发送",
				Location = new Point(410, 420),
				Width = 80
			};
			btnSend.Click += BtnSend_Click;

			btnClose = new Button
			{
				Text = "关闭",
				Location = new Point(495, 420),
				Width = 80
			};
			btnClose.Click += BtnClose_Click;

			// 添加控件到窗体
			this.Controls.AddRange(new Control[]
			{
				lblModel, cboModels, btnRefresh, chkboxSave, lstFiles, txtPrompt, btnSend, btnClose
			});
		}

		private void LoadModels()
		{
			cboModels.Items.Clear();
			if (LLMhelper.InstalledModels != null)
			{
				cboModels.Items.AddRange(LLMhelper.InstalledModels);
				if (cboModels.Items.Count > 0)
				{
					cboModels.SelectedIndex = 0;
				}
			}
		}

		private void LoadFiles()
		{
			lstFiles.Items.Clear();
			foreach (var file in filelist)
			{
				var ionfile = file + ".ion";
				var desc = File.Exists(ionfile) ? File.ReadAllText(ionfile) : "";
				var i = new ListViewItem([file, desc]);
				lstFiles.Items.Add(i);
				i.Checked = true;
			}
		}

		private async void BtnRefresh_Click(object sender, EventArgs e)
		{
			btnRefresh.Enabled = false;
			try
			{
				await LLMhelper.Prepare();
				LoadModels();
			}
			finally
			{
				btnRefresh.Enabled = true;
			}
		}

		private async void BtnSend_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtPrompt.Text))
			{
				MessageBox.Show("请输入提示词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var selectedFiles = lstFiles.CheckedItems.Cast<ListViewItem>().Select(item => item.Text).ToList();
			if (selectedFiles.Count == 0)
			{
				MessageBox.Show("请选择至少一个文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			btnSend.Enabled = false;
			try
			{
				var prompt = txtPrompt.Text;
				foreach (var file in selectedFiles)
					process_file(file, prompt);
			}
			finally
			{
				btnSend.Enabled = true;
			}
		}
		private async void process_file(string file, string prompt, bool needFileRead = true)
		{
			if (File.Exists(file))
			{
				var res = await LLMhelper.CallOllamaApiAsync(prompt + (needFileRead ? File.ReadAllText(file) : file));
				var i = lstFiles.Items.Cast<ListViewItem>().First(m => m.Text.Equals(file));
				if (i != null)
				{
					//将response写入第2列
					i.SubItems[1].Text = res;
					lstFiles.Refresh();
				}
				if (chkboxSave.Checked)
				{
					//save response to file, file's name is same as i.subitems[0] + "ion"
					var ionfile = file + ".ion";
					if (!File.Exists(ionfile))
						File.WriteAllText(ionfile, res);
					else
						MessageBox.Show($"{ionfile} already exist.");
				}
			}
			else if (Directory.Exists(file))
			{
				foreach (var f in Directory.GetFiles(file))
				{
					process_file(f, prompt);
				}
			}
		}
		private void BtnClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			// 清理资源
			LLMhelper = null;
			filelist = null;
		}
	}
	public class LLM_Helper
	{
		private MainForm form;
		CmdProc cmdProc;
		private static readonly string OllamaApiUrl = "http://localhost:11434/api"; // OLLAMA API 基础地址
		private static readonly string OllamaProcessName = "ollama"; // OLLAMA 进程名称
		private static readonly string OllamaExePath = @"%LOCALAPPDATA%\Programs\ollama\ollama.exe"; // OLLAMA 可执行文件路径
		private string[] installedModels;
		public string[] InstalledModels { get { return installedModels; } }
		public string currentModel { get ; private set; } = string.Empty;
		public bool IsPrepared { get => !currentModel.Equals(string.Empty); }
		
		// MCP工具调用队列，用于处理递归调用
		private Queue<MCPToolCall> mcpToolCallQueue = new Queue<MCPToolCall>();
		
		// MCP工具调用类，用于存储工具调用信息
		private class MCPToolCall
		{
			public string ServerName { get; set; }
			public string ToolName { get; set; }
			public string Arguments { get; set; }
			public string OriginalResponse { get; set; }
		}
		public LLM_Helper(MainForm form)
		{
			this.form = form;
			this.cmdProc = form.cmdProcessor;
		}
		public async Task Prepare(string newModel="deepseek-r1:1.5b")
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
						StringBuilder prompt = new("你好，你是一个专家程序员，精通各种编程语言。以下是你可以调用的各种工具来增强你的能力。");
						foreach(var s in form.mcpClientMgr.MCPToolsDict)
							prompt.Append($"{s.Key} :\n {string.Join('\n', s.Value)}");
						prompt.Append("如果你想使用以上工具，请使用以下格式:\n<use_mcp_tool>\n<server_name>server1</server_name>\n<tool_name>\ntool1 \n</tool_name>\n<arguments>{\"arg1\":\"value1\"}</arguments>\n</use_mcp_tool>");
						prompt.Append("\n你的目标是将用户指定文件夹下所有的后缀名为PAS的文件理解其功能并将功能分析写入同名的ion文件，再将该PAS程序转化为C#语言并写入同名文件(后缀名为CS)");
						string response = await CallOllamaApiAsync(prompt.ToString());
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
		private void StartOllama()
		{
			try
			{
				//Process.Start(Helper.GetPathByEnv(OllamaExePath));
				cmdProc.ExecCmd(OllamaExePath, "serve", Path.GetDirectoryName(Helper.GetPathByEnv(OllamaExePath)));
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
					client.Timeout = TimeSpan.FromMinutes(120); // 设置更长的超时时间，因为模型下载可能需要较长时间
					
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

		// 检查响应中是否包含MCP工具调用
		private bool ContainsMCPToolCall(string response)
		{
			if (string.IsNullOrEmpty(response))
				return false;
			
			// 使用正则表达式检查是否包含MCP工具调用标记
			string pattern = @"<use_mcp_tool>.*?</use_mcp_tool>";
			return Regex.IsMatch(response, pattern, RegexOptions.Singleline);
		}
		
		// 从响应中提取MCP工具调用信息
		private List<MCPToolCall> ExtractMCPToolCalls(string response)
		{
			List<MCPToolCall> toolCalls = new List<MCPToolCall>();
			
			if (string.IsNullOrEmpty(response))
				return toolCalls;
			
			// 使用正则表达式提取所有MCP工具调用
			string pattern = @"<use_mcp_tool>(.*?)</use_mcp_tool>";
			MatchCollection matches = Regex.Matches(response, pattern, RegexOptions.Singleline);
			
			foreach (Match match in matches)
			{
				string toolCallContent = match.Groups[1].Value;
				
				// 提取服务器名称
				string serverNamePattern = @"<server_name>(.*?)</server_name>";
				Match serverNameMatch = Regex.Match(toolCallContent, serverNamePattern, RegexOptions.Singleline);
				
				// 提取工具名称
				string toolNamePattern = @"<tool_name>(.*?)</tool_name>";
				Match toolNameMatch = Regex.Match(toolCallContent, toolNamePattern, RegexOptions.Singleline);
				
				// 提取参数
				string argumentsPattern = @"<arguments>(.*?)</arguments>";
				Match argumentsMatch = Regex.Match(toolCallContent, argumentsPattern, RegexOptions.Singleline);
				
				if (serverNameMatch.Success && toolNameMatch.Success && argumentsMatch.Success)
				{
					MCPToolCall toolCall = new MCPToolCall
					{
						ServerName = serverNameMatch.Groups[1].Value.Trim(),
						ToolName = toolNameMatch.Groups[1].Value.Trim(),
						Arguments = argumentsMatch.Groups[1].Value.Trim(),
						OriginalResponse = response
					};
					
					toolCalls.Add(toolCall);
				}
			}
			
			return toolCalls;
		}
		
		// 处理MCP工具调用
		private async Task<string> ProcessMCPToolCall(MCPToolCall toolCall)
		{
			try
			{
				Debug.Print($"处理MCP工具调用: 服务器={toolCall.ServerName}, 工具={toolCall.ToolName}");
				
				// 获取MCPClient实例
				MCPClient client = form.mcpClientMgr.GetClient(toolCall.ServerName);
				if (client == null)
				{
					Debug.Print($"错误: 找不到服务器 {toolCall.ServerName} 的MCPClient实例");
					return $"错误: 找不到服务器 {toolCall.ServerName} 的MCPClient实例";
				}
				
				// 解析参数
				Dictionary<string, object> parameters;
				try
				{
					parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(toolCall.Arguments);
				}
				catch (Exception ex)
				{
					Debug.Print($"解析参数失败: {ex.Message}");
					return $"错误: 解析参数失败: {ex.Message}";
				}
				
				// 调用MCP工具
				var result = await client.CallToolAsync(toolCall.ToolName, parameters);
				
				// 将结果转换为字符串
				string resultString = Newtonsoft.Json.JsonConvert.SerializeObject(result);
				Debug.Print($"MCP工具调用结果: {resultString}");
				
				return resultString;
			}
			catch (Exception ex)
			{
				Debug.Print($"处理MCP工具调用时发生错误: {ex.Message}");
				return $"错误: 处理MCP工具调用时发生错误: {ex.Message}";
			}
		}
		
		// 处理所有MCP工具调用
		private async Task<string> ProcessAllMCPToolCalls(string response)
		{
			if (!ContainsMCPToolCall(response))
				return response;
			
			// 提取所有MCP工具调用
			List<MCPToolCall> toolCalls = ExtractMCPToolCalls(response);
			if (toolCalls.Count == 0)
				return response;
			
			// 将工具调用添加到队列
			foreach (var toolCall in toolCalls)
			{
				mcpToolCallQueue.Enqueue(toolCall);
			}
			
			// 处理队列中的所有工具调用
			while (mcpToolCallQueue.Count > 0)
			{
				MCPToolCall currentToolCall = mcpToolCallQueue.Dequeue();
				
				// 处理当前工具调用
				string toolCallResult = await ProcessMCPToolCall(currentToolCall);
				
				// 构建包含工具调用结果的提示
				string prompt = $"我之前尝试使用MCP工具 {currentToolCall.ToolName}，以下是调用结果:\n{toolCallResult}\n请基于这个结果继续我们的对话。";
				
				// 调用大模型处理工具调用结果
				string newResponse = await CallOllamaApiRawAsync(prompt);
				
				// 检查新响应中是否包含更多工具调用
				if (ContainsMCPToolCall(newResponse))
				{
					List<MCPToolCall> newToolCalls = ExtractMCPToolCalls(newResponse);
					foreach (var newToolCall in newToolCalls)
					{
						mcpToolCallQueue.Enqueue(newToolCall);
					}
				}
				
				// 更新响应
				response = newResponse;
			}
			
			return response;
		}

		// 调用 OLLAMA API 与大模型交互 (原始版本，不处理MCP工具调用)
		private async Task<string> CallOllamaApiRawAsync(string prompt, bool needExtract = true)
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
					client.Timeout = TimeSpan.FromSeconds(600); // 设置超时时间
					client.DefaultRequestHeaders.ConnectionClose = true; // 确保连接关闭，避免连接池问题
					
					// 检查Ollama服务是否运行
					if (!IsOllamaRunning())
						return "错误：OLLAMA服务未运行，无法调用API。";

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
					using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600)))
					{
						Debug.Print("开始发送HTTP请求...");
						var task = client.PostAsync(url, content, cts.Token);

						// 添加超时保护
						Debug.Print("等待响应...");
						if (await Task.WhenAny(task, Task.Delay(600000)) != task)
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
						if (needExtract)
						{
							responseBody = Helper.ExtractResponseContent(responseBody);
							//将res中的"\n"替换为真正的换行符
							responseBody = responseBody.Replace("\\n", "\n");
							Debug.Print("AI 响应 : \n" + responseBody);
						}
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
		
		// 调用 OLLAMA API 与大模型交互 (增强版本，处理MCP工具调用)
		public async Task<string> CallOllamaApiAsync(string prompt)
		{
			Debug.Print("request ollama api: " + prompt);
			// 首先调用原始API
			string response = await CallOllamaApiRawAsync(prompt);
			//Debug.Print("ollama response: " + response);
			// 检查响应中是否包含MCP工具调用
			if (ContainsMCPToolCall(response))
			{
				Debug.Print("检测到MCP工具调用，开始处理...");
				response = await ProcessAllMCPToolCalls(response);
			}
			
			return response;
		}
	}
}