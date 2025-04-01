using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MCPSharp;
using System.Linq;
using System.Windows.Forms;

namespace Zfile
{
	public class AIassistDlg : Form
	{
		private MainForm form;
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
		
		// 远程API相关控件
		private RadioButton rdoLocalModel;
		private RadioButton rdoRemoteAPI;
		private ComboBox cboAPIProfiles;
		private TextBox txtAPIUrl;
		private TextBox txtAPIKey;
		private TextBox txtAPIModel; // 新增：模型名称输入框
		private Button btnSaveAPI;
		private Button btnDeleteAPI;
		private Dictionary<string, (string Url, string Key, string Model)> apiProfiles; // 更新：添加模型名称
		private const string API_SECTION = "zfile";
		private const string API_PROFILE_PREFIX = "APIProfile_";
		private const string API_URL_SUFFIX = "_URL";
		private const string API_KEY_SUFFIX = "_KEY";
		private const string API_MODEL_SUFFIX = "_MODEL"; // 新增：模型名称后缀
		private const string SELECTED_API_PROFILE = "SelectedAPIProfile";
		private const string USE_REMOTE_API = "UseRemoteAPI";
		private const string DEFAULT_MODEL_NAME = "gpt-3.5-turbo"; // 新增：默认模型名称
		public AIassistDlg(List<string> files, LLM_Helper llm, CmdProc cmdproc, MainForm owner)
		{
			form = owner;
			LLMhelper = llm;
			filelist = files;
			apiProfiles = new Dictionary<string, (string Url, string Key, string Model)>();
			InitializeComponents();
			LoadAPIProfiles();
			LoadModels();
			LoadFiles();
			this.cmdProc = cmdproc;
		}

		// 加载API配置
		private void LoadAPIProfiles()
		{
			apiProfiles.Clear();
			cboAPIProfiles.Items.Clear();

			// 从配置文件加载API配置
			var apiSection = form.userConfigLoader.GetConfigSection(API_SECTION);
			if (apiSection != null)
			{
				// 查找所有API配置
				foreach (var item in apiSection.Items)
				{
					if (item.Key.StartsWith(API_PROFILE_PREFIX) && !item.Key.EndsWith(API_URL_SUFFIX) && !item.Key.EndsWith(API_KEY_SUFFIX) && !item.Key.EndsWith(API_MODEL_SUFFIX))
					{
						string profileName = item.Value;
						string profileKey = item.Key;
						
						// 获取对应的URL、Key和Model
						string urlKey = profileKey + API_URL_SUFFIX;
						string keyKey = profileKey + API_KEY_SUFFIX;
						string modelKey = profileKey + API_MODEL_SUFFIX;
						
						string url = apiSection.FindValue(urlKey) ?? string.Empty;
						string apiKey = apiSection.FindValue(keyKey) ?? string.Empty;
						string apiModel = apiSection.FindValue(modelKey) ?? DEFAULT_MODEL_NAME;
						
						// 添加到字典和下拉框
						apiProfiles[profileName] = (url, apiKey, apiModel);
						cboAPIProfiles.Items.Add(profileName);
					}
				}

				// 设置选中的API配置
				string selectedProfile = apiSection.FindValue(SELECTED_API_PROFILE);
				if (!string.IsNullOrEmpty(selectedProfile) && cboAPIProfiles.Items.Contains(selectedProfile))
				{
					cboAPIProfiles.SelectedItem = selectedProfile;
				}
				else if (cboAPIProfiles.Items.Count > 0)
				{
					cboAPIProfiles.SelectedIndex = 0;
				}

				// 设置是否使用远程API
				string useRemoteApiStr = apiSection.FindValue(USE_REMOTE_API);
				if (!string.IsNullOrEmpty(useRemoteApiStr) && bool.TryParse(useRemoteApiStr, out bool useRemoteApi))
				{
					rdoRemoteAPI.Checked = useRemoteApi;
					rdoLocalModel.Checked = !useRemoteApi;
				}
			}
		}

		// 保存API配置
		private void SaveAPIProfile(string profileName, string url, string apiKey, string apiModel)
		{
			if (string.IsNullOrWhiteSpace(profileName))
				return;

			// 更新内存中的配置
			apiProfiles[profileName] = (url, apiKey, apiModel);

			// 如果下拉框中不存在该配置，则添加
			if (!cboAPIProfiles.Items.Contains(profileName))
			{
				cboAPIProfiles.Items.Add(profileName);
			}

			// 设置为当前选中的配置
			cboAPIProfiles.SelectedItem = profileName;

			// 保存到配置文件
			var apiSection = form.userConfigLoader.GetConfigSection(API_SECTION);
			if (apiSection == null)
			{
				// 如果节不存在，创建新节
				apiSection = new ConfigSection { Name = API_SECTION };
				form.userConfigLoader.sections.Add(apiSection);
			}

			// 生成唯一的配置键
			string profileKey = API_PROFILE_PREFIX + Guid.NewGuid().ToString("N").Substring(0, 8);

			// 检查是否已存在同名配置，如果存在则使用原来的键
			foreach (var item in apiSection.Items)
			{
				if (item.Key.StartsWith(API_PROFILE_PREFIX) && 
				    !item.Key.EndsWith(API_URL_SUFFIX) && 
				    !item.Key.EndsWith(API_KEY_SUFFIX) && 
				    !item.Key.EndsWith(API_MODEL_SUFFIX) && 
				    item.Value == profileName)
				{
					profileKey = item.Key;
					break;
				}
			}

			// 保存配置
			form.userConfigLoader.SetConfigValue(API_SECTION, profileKey, profileName);
			form.userConfigLoader.SetConfigValue(API_SECTION, profileKey + API_URL_SUFFIX, url);
			form.userConfigLoader.SetConfigValue(API_SECTION, profileKey + API_KEY_SUFFIX, apiKey);
			form.userConfigLoader.SetConfigValue(API_SECTION, profileKey + API_MODEL_SUFFIX, apiModel);
			form.userConfigLoader.SetConfigValue(API_SECTION, SELECTED_API_PROFILE, profileName);
			form.userConfigLoader.SetConfigValue(API_SECTION, USE_REMOTE_API, rdoRemoteAPI.Checked.ToString());

			// 保存配置文件
			form.userConfigLoader.SaveConfig();
		}

		// 删除API配置
		private void DeleteAPIProfile(string profileName)
		{
			if (string.IsNullOrWhiteSpace(profileName) || !apiProfiles.ContainsKey(profileName))
				return;

			// 从内存中移除
			apiProfiles.Remove(profileName);

			// 从下拉框中移除
			cboAPIProfiles.Items.Remove(profileName);

			// 从配置文件中移除
			var apiSection = form.userConfigLoader.GetConfigSection(API_SECTION);
			if (apiSection != null)
			{
				// 找到对应的配置项
				string profileKeyToRemove = null;
				foreach (var item in apiSection.Items)
				{
					if (item.Key.StartsWith(API_PROFILE_PREFIX) && 
					    !item.Key.EndsWith(API_URL_SUFFIX) && 
					    !item.Key.EndsWith(API_KEY_SUFFIX) && 
					    item.Value == profileName)
					{
						profileKeyToRemove = item.Key;
						break;
					}
				}

				// 移除配置项
				if (profileKeyToRemove != null)
				{
					// 移除配置项及其URL、Key和Model
					apiSection.Items.RemoveAll(i => i.Key == profileKeyToRemove || 
					                           i.Key == profileKeyToRemove + API_URL_SUFFIX || 
					                           i.Key == profileKeyToRemove + API_KEY_SUFFIX ||
					                           i.Key == profileKeyToRemove + API_MODEL_SUFFIX);

					// 如果删除的是当前选中的配置，则更新选中的配置
					if (apiSection.FindValue(SELECTED_API_PROFILE) == profileName)
					{
						if (cboAPIProfiles.Items.Count > 0)
						{
							cboAPIProfiles.SelectedIndex = 0;
							form.userConfigLoader.SetConfigValue(API_SECTION, SELECTED_API_PROFILE, cboAPIProfiles.SelectedItem.ToString());
						}
						else
						{
							// 如果没有配置了，则清空选中的配置
							var selectedItem = apiSection.Items.FirstOrDefault(i => i.Key == SELECTED_API_PROFILE);
							if (selectedItem != null)
							{
								apiSection.Items.Remove(selectedItem);
							}
						}
					}

					// 保存配置文件
					form.userConfigLoader.SaveConfig();
				}
			}

			// 清空输入框
			if (cboAPIProfiles.Items.Count > 0)
			{
				cboAPIProfiles.SelectedIndex = 0;
			}
			else
			{
				txtAPIUrl.Text = string.Empty;
				txtAPIKey.Text = string.Empty;
			}
		}

		// 更新API控件状态
		private void UpdateAPIControlsState()
		{
			bool useRemoteAPI = rdoRemoteAPI.Checked;

			// 本地模型控件
			cboModels.Enabled = !useRemoteAPI;
			btnRefresh.Enabled = !useRemoteAPI;

			// 远程API控件
			cboAPIProfiles.Enabled = useRemoteAPI;
			txtAPIUrl.Enabled = useRemoteAPI;
			txtAPIKey.Enabled = useRemoteAPI;
			txtAPIModel.Enabled = useRemoteAPI;
			btnSaveAPI.Enabled = useRemoteAPI;
			btnDeleteAPI.Enabled = useRemoteAPI && cboAPIProfiles.SelectedIndex >= 0;
		}

		// 单选按钮状态变化事件处理
		private void RdoModelType_CheckedChanged(object sender, EventArgs e)
		{
			UpdateAPIControlsState();

			// 保存选择状态到配置
			form.userConfigLoader.SetConfigValue(API_SECTION, USE_REMOTE_API, rdoRemoteAPI.Checked.ToString());
			form.userConfigLoader.SaveConfig();
		}

		// API配置下拉框选择变化事件处理
		private void CboAPIProfiles_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboAPIProfiles.SelectedIndex >= 0)
			{
				string profileName = cboAPIProfiles.SelectedItem.ToString();
				if (apiProfiles.TryGetValue(profileName, out var profile))
				{
					txtAPIUrl.Text = profile.Url;
					txtAPIKey.Text = profile.Key;
					txtAPIModel.Text = profile.Model;

					// 保存选中的配置到配置文件
					form.userConfigLoader.SetConfigValue(API_SECTION, SELECTED_API_PROFILE, profileName);
					form.userConfigLoader.SaveConfig();
				}
			}

			// 更新删除按钮状态
			btnDeleteAPI.Enabled = rdoRemoteAPI.Checked && cboAPIProfiles.SelectedIndex >= 0;
		}

		// 保存API配置按钮点击事件
		private void BtnSaveAPI_Click(object sender, EventArgs e)
		{
			string url = txtAPIUrl.Text.Trim();
			string apiKey = txtAPIKey.Text.Trim();
			string apiModel = txtAPIModel.Text.Trim();

			if (string.IsNullOrWhiteSpace(url))
			{
				MessageBox.Show("请输入API URL", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			
			if (string.IsNullOrWhiteSpace(apiModel))
			{
				apiModel = DEFAULT_MODEL_NAME; // 使用默认模型名称
			}

			// 弹出对话框让用户输入配置名称
			string profileName = cboAPIProfiles.SelectedIndex >= 0 ? cboAPIProfiles.SelectedItem.ToString() : "";
			using (var inputDialog = new Form()
			{
				Width = 300,
				Height = 150,
				Text = "保存API配置",
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false
			})
			{
				var lblName = new Label()
				{
					Text = "配置名称：",
					Location = new Point(20, 20),
					AutoSize = true
				};

				var txtName = new TextBox()
				{
					Text = profileName,
					Location = new Point(100, 17),
					Width = 160
				};

				var btnOK = new Button()
				{
					Text = "确定",
					Location = new Point(100, 60),
					DialogResult = DialogResult.OK
				};

				var btnCancel = new Button()
				{
					Text = "取消",
					Location = new Point(180, 60),
					DialogResult = DialogResult.Cancel
				};

				inputDialog.Controls.AddRange(new Control[] { lblName, txtName, btnOK, btnCancel });
				inputDialog.AcceptButton = btnOK;
				inputDialog.CancelButton = btnCancel;

				if (inputDialog.ShowDialog() == DialogResult.OK)
				{
					profileName = txtName.Text.Trim();
					if (!string.IsNullOrWhiteSpace(profileName))
					{
						// 保存API配置
						SaveAPIProfile(profileName, url, apiKey, apiModel);
						MessageBox.Show($"API配置 '{profileName}' 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show("请输入配置名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
		}

		// 删除API配置按钮点击事件
		private void BtnDeleteAPI_Click(object sender, EventArgs e)
		{
			if (cboAPIProfiles.SelectedIndex < 0)
			{
				MessageBox.Show("请先选择要删除的API配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			string profileName = cboAPIProfiles.SelectedItem.ToString();
			if (MessageBox.Show($"确定要删除API配置 '{profileName}' 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				DeleteAPIProfile(profileName);
				MessageBox.Show($"API配置 '{profileName}' 已删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void InitializeComponents()
		{
			this.Text = "AI 助手";
			this.Size = new Size(800, 600);
			this.StartPosition = FormStartPosition.CenterParent;

			// 模型选择区域 - 本地/远程选择
			rdoLocalModel = new RadioButton
			{
				Text = "使用本地模型",
				Location = new Point(10, 15),
				AutoSize = true,
				Checked = true
			};
			rdoLocalModel.CheckedChanged += RdoModelType_CheckedChanged;

			rdoRemoteAPI = new RadioButton
			{
				Text = "使用远程API",
				Location = new Point(130, 15),
				AutoSize = true
			};
			rdoRemoteAPI.CheckedChanged += RdoModelType_CheckedChanged;

			// 本地模型选择
			var lblModel = new Label
			{
				Text = "选择模型：",
				Location = new Point(10, 45),
				AutoSize = true
			};

			cboModels = new ComboBox
			{
				Location = new Point(80, 42),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};

			btnRefresh = new Button
			{
				Text = "刷新",
				Location = new Point(290, 42),
				Width = 60
			};
			btnRefresh.Click += BtnRefresh_Click;

			// 远程API配置区域
			var lblAPIProfile = new Label
			{
				Text = "API配置：",
				Location = new Point(10, 75),
				AutoSize = true
			};

			cboAPIProfiles = new ComboBox
			{
				Location = new Point(80, 72),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			cboAPIProfiles.SelectedIndexChanged += CboAPIProfiles_SelectedIndexChanged;

			var lblAPIUrl = new Label
			{
				Text = "API URL：",
				Location = new Point(10, 105),
				AutoSize = true
			};

			txtAPIUrl = new TextBox
			{
				Location = new Point(80, 102),
				Width = 400
			};

			var lblAPIKey = new Label
			{
				Text = "API Key：",
				Location = new Point(10, 135),
				AutoSize = true
			};

			txtAPIKey = new TextBox
			{
				Location = new Point(80, 132),
				Width = 400,
				UseSystemPasswordChar = true
			};
			
			var lblAPIModel = new Label
			{
				Text = "模型名称：",
				Location = new Point(10, 165),
				AutoSize = true
			};

			txtAPIModel = new TextBox
			{
				Location = new Point(80, 162),
				Width = 400,
				Text = DEFAULT_MODEL_NAME
			};

			btnSaveAPI = new Button
			{
				Text = "保存配置",
				Location = new Point(490, 102),
				Width = 80
			};
			btnSaveAPI.Click += BtnSaveAPI_Click;

			btnDeleteAPI = new Button
			{
				Text = "删除配置",
				Location = new Point(490, 132),
				Width = 80
			};
			btnDeleteAPI.Click += BtnDeleteAPI_Click;

			chkboxSave = new CheckBox
			{
				Text = "保存结果到文件备注",
				Location = new Point(600, 15),
				Width = 260,
				Checked = true
			};

			// 文件列表
			lstFiles = new ListView
			{
				Location = new Point(10, 200),
				Size = new Size(765, 220),
				CheckBoxes = true,
				View = View.Details
			};
			lstFiles.Columns.Add("文件", 380);
			lstFiles.Columns.Add("处理结果", 380);

			// 提示词输入
			txtPrompt = new TextBox
			{
				Location = new Point(10, 430),
				Size = new Size(765, 80),
				Multiline = true,
				ScrollBars = ScrollBars.Vertical,
				Text = "开始处理以下文件或文件夹"
			};

			// 按钮区域
			btnSend = new Button
			{
				Text = "发送",
				Location = new Point(610, 520),
				Width = 80
			};
			btnSend.Click += BtnSend_Click;

			btnClose = new Button
			{
				Text = "关闭",
				Location = new Point(695, 520),
				Width = 80
			};
			btnClose.Click += BtnClose_Click;

			// 添加控件到窗体
			this.Controls.AddRange(new Control[]
			{
				rdoLocalModel, rdoRemoteAPI, lblModel, cboModels, btnRefresh,
				lblAPIProfile, cboAPIProfiles, lblAPIUrl, txtAPIUrl, lblAPIKey, txtAPIKey,
				lblAPIModel, txtAPIModel, btnSaveAPI, btnDeleteAPI, chkboxSave, 
				lstFiles, txtPrompt, btnSend, btnClose
			});

			// 初始化API配置相关控件状态
			UpdateAPIControlsState();
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

			// 检查远程API配置
			if (rdoRemoteAPI.Checked)
			{
				if (cboAPIProfiles.SelectedIndex < 0)
				{
					MessageBox.Show("请选择API配置或创建新的API配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				string profileName = cboAPIProfiles.SelectedItem.ToString();
				if (!apiProfiles.TryGetValue(profileName, out var profile) || 
				    string.IsNullOrWhiteSpace(profile.Url))
				{
					MessageBox.Show("所选API配置无效，请重新配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				// 设置LLM_Helper的远程API配置
				LLMhelper.SetRemoteAPI(profile.Url, profile.Key, profile.Model);
			}
			else
			{
				// 使用本地模型
				if (cboModels.SelectedIndex < 0)
				{
					MessageBox.Show("请选择本地模型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				// 设置LLM_Helper使用本地模型
				LLMhelper.UseLocalAPI();
			}

			btnSend.Enabled = false;
			try
			{
				StringBuilder prompt = new("你好，你是一个专家程序员，精通各种编程语言。");
				prompt.Append("\n你的目标是使用合适的MCP工具找到用户指定对象{TARGETPATH}，如果该对象是文件，理解其程序功能并将对它的完整功能分析通过MCP工具写入同名ION文件(比如：hello.pas -> hello.ion)，再将该PAS程序转化为C#语言并写入同名cs文件(比如：hello.pas -> hello.cs)\n如果该对象是文件夹，则需要对该文件夹下所有的后缀名（类型）为PAS的文件做上述处理。以下是你可以调用的各种MCP工具，通过它们来增强你的能力。");
				foreach (var s in form.mcpClientMgr.MCPToolsDict)
					prompt.Append($"[{s.Key}] :\n {string.Join('\n', s.Value)}");
				prompt.Append("如果你想使用以上MCP工具，请使用以下格式输出:\n<use_mcp_tool>\n<server_name>server1</server_name>\n<tool_name>tool1</tool_name>\n<arguments>{\"arg1\":\"value1\"}</arguments>\n</use_mcp_tool>\n");
				prompt.Append("注意：一次最多使用一种MCP工具");
				prompt.Append(txtPrompt.Text);
				foreach (var file in selectedFiles)
					process_file(file, prompt.ToString(), false);
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
				var res = await LLMhelper.CallOllamaApiAsync((needFileRead ? prompt + File.ReadAllText(file) : prompt.Replace("TARGETPATH", file)));
				try
				{
					var i = lstFiles.Items.Cast<ListViewItem>().First(m => m.Text.Equals(file));
					if (i != null)
					{
						//将response写入第2列
						i.SubItems[1].Text = res;
						lstFiles.Refresh();
					}
				} catch { }

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
					process_file(f, prompt, needFileRead);
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

	}