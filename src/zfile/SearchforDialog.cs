using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Zfile
{
	public class SearchforDialog : Form
	{
		private TabControl tabControl;
		private TabPage generalTab;
		private TabPage advancedTab;
		private TabPage pluginsTab;
		private TabPage rulesTab;

		private TextBox searchBox;
		private TextBox locationBox;
		private Button locationBrowseButton;
		private CheckedListBox drivesList;
		private CheckBox regexCheckBox;
		private CheckBox selectedFilesCheckBox;
		private CheckBox everythingCheckBox;
		private CheckBox searchCompressedCheckBox;
		private ComboBox subFoldersComboBox;

		private CheckBox findTextCheckBox;
		private TextBox findTextBox;
		private CheckBox wholeWordCheckBox;
		private CheckBox caseSensitiveCheckBox;
		private CheckBox textRegexCheckBox;
		private CheckBox hexSearchCheckBox;
		private CheckBox invertTextSearchCheckBox;

		private CheckBox ansiEncodingCheckBox;
		private CheckBox asciiEncodingCheckBox;
		private CheckBox utf16EncodingCheckBox;
		private CheckBox utf8EncodingCheckBox;
		private CheckBox officeXmlCheckBox;
		private CheckBox pluginCheckBox;

		// 高级选项卡控件
		private DateTimePicker notBeforeDatePicker;
		private DateTimePicker beforeDatePicker;
		private ComboBox notBeforeUnitComboBox;
		private ComboBox beforeUnitComboBox;
		private TextBox notBeforeValueTextBox;
		private TextBox beforeValueTextBox;

		private ComboBox fileSizeOperatorComboBox;
		private TextBox fileSizeValueTextBox;
		private ComboBox fileSizeUnitComboBox;

		private CheckBox archivedCheckBox;
		private CheckBox hiddenCheckBox;
		private CheckBox readOnlyCheckBox;
		private CheckBox systemCheckBox;
		private CheckBox folderCheckBox;
		private CheckBox encryptedCheckBox;

		private CheckBox duplicateFilesCheckBox;
		private CheckBox sameNameCheckBox;
		private CheckBox sameSizeCheckBox;
		private CheckBox sameContentCheckBox;
		private CheckBox samePluginFieldsCheckBox;
		private ComboBox pluginFieldsComboBox;

		// 插件选项卡控件
		
		private CheckBox usePluginsCheckBox;

		// 加减规则选项卡控件
		private RadioButton andRuleRadioButton;
		private RadioButton orRuleRadioButton;
	
		private Button addRuleButton;
		private Button removeRuleButton;
		private DataGridView rulesDataGridView;

		// 结果区域控件
		private Button prevResultButton;
		private Button nextResultButton;
		private CheckBox searchInResultsCheckBox;
		private ListView resultsListView;
		private StatusStrip statusStrip;
		private ToolStripStatusLabel statusLabel;

		private Button viewButton;
		private Button editButton;
		private Button newSearchButton;
		private Button gotoFileButton;
		private Button exportListButton;
		private Button startSearchButton;
		private Button cancelButton;
		private Button helpButton;

		private List<string> searchHistory = new List<string>();
		private List<string> locationHistory = new List<string>();
		private List<string> searchResults = new List<string>();

		private Form owner;
		private bool isStandalone;

		public SearchforDialog(Form owner, bool isstandalone)
		{
			isStandalone = isstandalone;
			this.owner = owner;
			InitializeComponent();
			LoadHistory();
		}

		private void InitializeComponent()
		{
			this.Text = "搜索文件";
			this.Size = new Size(800, 800);
			this.StartPosition = FormStartPosition.CenterParent;
			this.MinimizeBox = true;
			this.MaximizeBox = true;
			this.FormBorderStyle = FormBorderStyle.Sizable;

			// 创建选项卡控件
			tabControl = new TabControl
			{
				Dock = DockStyle.Top,
				Height = 450,
				Location = new Point(10, 10)
			};

			// 创建各个选项卡
			generalTab = new TabPage("常规");
			advancedTab = new TabPage("高级");
			pluginsTab = new TabPage("插件");
			rulesTab = new TabPage("加载/保存");

			// 初始化各个选项卡的内容
			InitializeGeneralTab();
			InitializeAdvancedTab();
			InitializePluginsTab();
			InitializeRulesTab();

			// 添加选项卡到TabControl
			tabControl.TabPages.Add(generalTab);
			tabControl.TabPages.Add(advancedTab);
			tabControl.TabPages.Add(pluginsTab);
			tabControl.TabPages.Add(rulesTab);

			// 初始化结果区域
			InitializeResultsArea();

			// 添加控件到窗体
			this.Controls.Add(tabControl);

			// 创建底部按钮区域
			var buttonPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 40,
				Padding = new Padding(5)
			};

			startSearchButton = new Button { Text = "开始搜索(S)", Width = 100, Location = new Point(480, 5) };
			cancelButton = new Button { Text = "取消", Width = 80, Location = new Point(590, 5) };
			helpButton = new Button { Text = "帮助", Width = 80, Location = new Point(680, 5) };

			buttonPanel.Controls.AddRange(new Control[] { startSearchButton, cancelButton, helpButton });
			this.Controls.Add(buttonPanel);

			// 绑定事件处理程序
			startSearchButton.Click += StartSearchButton_Click;
			cancelButton.Click += CancelButton_Click;
			locationBrowseButton.Click += LocationBrowseButton_Click;
			gotoFileButton.Click += GotoFileButton_Click;
			exportListButton.Click += ExportListButton_Click;
			findTextCheckBox.CheckedChanged += FindTextCheckBox_CheckedChanged;
			duplicateFilesCheckBox.CheckedChanged += DuplicateFilesCheckBox_CheckedChanged;
			viewButton.Click += ViewButton_Click;
			editButton.Click += EditButton_Click;
			// 注意：addRuleButton.Click 事件在 InitializePluginsTab 方法中绑定
			// removeRuleButton.Click += RemoveRuleButton_Click;
		}
		private void CancelButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void LocationBrowseButton_Click(object sender, EventArgs e)
		{
			using (var folderDialog = new FolderBrowserDialog())
			{
				folderDialog.Description = "选择搜索位置";
				if (folderDialog.ShowDialog() == DialogResult.OK)
				{
					locationBox.Text = folderDialog.SelectedPath;
				}
			}
		}

		private void FindTextCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			// 启用/禁用文本搜索相关控件
			bool enabled = findTextCheckBox.Checked;
			findTextBox.Enabled = enabled;
			wholeWordCheckBox.Enabled = enabled;
			caseSensitiveCheckBox.Enabled = enabled;
			textRegexCheckBox.Enabled = enabled;
			hexSearchCheckBox.Enabled = enabled;
			invertTextSearchCheckBox.Enabled = enabled;

			// 启用/禁用编码选项
			ansiEncodingCheckBox.Enabled = enabled;
			asciiEncodingCheckBox.Enabled = enabled;
			utf16EncodingCheckBox.Enabled = enabled;
			utf8EncodingCheckBox.Enabled = enabled;
			officeXmlCheckBox.Enabled = enabled;
			pluginCheckBox.Enabled = enabled;
		}

		private void DuplicateFilesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			// 启用/禁用重复文件搜索选项
			bool enabled = duplicateFilesCheckBox.Checked;
			sameNameCheckBox.Enabled = enabled;
			sameSizeCheckBox.Enabled = enabled;
			sameContentCheckBox.Enabled = enabled;
			samePluginFieldsCheckBox.Enabled = enabled;
			pluginFieldsComboBox.Enabled = enabled && samePluginFieldsCheckBox.Checked;
		}

		private void ViewButton_Click(object sender, EventArgs e)
		{
			if (resultsListView.SelectedItems.Count > 0)
			{
				string filePath = resultsListView.SelectedItems[0].Tag as string;
				if (File.Exists(filePath))
				{
					try
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
						{
							FileName = filePath,
							UseShellExecute = true,
							Verb = "open"
						});
					}
					catch (Exception ex)
					{
						MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void EditButton_Click(object sender, EventArgs e)
		{
			if (resultsListView.SelectedItems.Count > 0)
			{
				string filePath = resultsListView.SelectedItems[0].Tag as string;
				if (File.Exists(filePath))
				{
					try
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
						{
							FileName = filePath,
							UseShellExecute = true,
							Verb = "edit"
						});
					}
					catch (Exception ex)
					{
						MessageBox.Show($"无法编辑文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void GotoFileButton_Click(object sender, EventArgs e)
		{
			if (resultsListView.SelectedItems.Count > 0)
			{
				string filePath = resultsListView.SelectedItems[0].Tag as string;
				if (File.Exists(filePath))
				{
					string directory = Path.GetDirectoryName(filePath);
					// 如果有父窗体,通知它跳转到指定目录
					owner?.GetType().GetMethod("NavigateToDirectory")?.Invoke(owner, new object[] { directory });
				}
			}
		}

		private void ExportListButton_Click(object sender, EventArgs e)
		{
			using (var saveDialog = new SaveFileDialog())
			{
				saveDialog.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
				saveDialog.FileName = "SearchResults.txt";

				if (saveDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						using (var writer = new StreamWriter(saveDialog.FileName))
						{
							foreach (var file in searchResults)
							{
								writer.WriteLine(file);
							}
						}
						MessageBox.Show("搜索结果已成功导出!", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"导出结果时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void InitializeGeneralTab()
		{
			// 搜索区域
			var searchLabel = new Label { Text = "搜索：", Location = new Point(10, 15), AutoSize = true };
			searchBox = new TextBox { Location = new Point(100, 12), Width = 400 };
			searchBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			searchBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

			// 位置区域
			var locationLabel = new Label { Text = "位置：", Location = new Point(10, 45), AutoSize = true };
			locationBox = new TextBox { Location = new Point(100, 42), Width = 400 };
			locationBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			locationBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

			locationBrowseButton = new Button { Text = ">>", Location = new Point(510, 41), Width = 30 };

			// 驱动器区域
			var drivesLabel = new Label { Text = "驱动器", Location = new Point(10, 75), AutoSize = true };
			drivesList = new CheckedListBox { Location = new Point(100, 75), Width = 400, Height = 60 };
			foreach (var drive in DriveInfo.GetDrives())
			{
				drivesList.Items.Add(drive.Name, false);
			}

			// 选项区域
			regexCheckBox = new CheckBox { Text = "正则表达式", Location = new Point(10, 145), AutoSize = true };
			selectedFilesCheckBox = new CheckBox { Text = "只在选定的文件（夹）中搜索", Location = new Point(150, 145), AutoSize = true };
			everythingCheckBox = new CheckBox { Text = "Everything", Location = new Point(10, 170), AutoSize = true };
			searchCompressedCheckBox = new CheckBox { Text = "搜索压缩文件", Location = new Point(150, 170), AutoSize = true };

			var subFoldersLabel = new Label { Text = "在子文件夹中搜索：", Location = new Point(10, 195), AutoSize = true };
			subFoldersComboBox = new ComboBox
			{
				Location = new Point(150, 192),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			subFoldersComboBox.Items.AddRange(new object[] { "仅当前文件夹", "所有子文件夹", "1级", "2级", "3级", "4级", "5级" });
			subFoldersComboBox.SelectedIndex = 1; // 默认选择"所有子文件夹"

			// 查找文本区域
			findTextCheckBox = new CheckBox { Text = "查找文本：", Location = new Point(10, 225), AutoSize = true };
			findTextBox = new TextBox { Location = new Point(100, 222), Width = 400, Enabled = false };

			// 文本搜索选项
			wholeWordCheckBox = new CheckBox { Text = "全字匹配", Location = new Point(100, 250), AutoSize = true, Enabled = false };
			caseSensitiveCheckBox = new CheckBox { Text = "区分大小写", Location = new Point(200, 250), AutoSize = true, Enabled = false };
			textRegexCheckBox = new CheckBox { Text = "正则表达式", Location = new Point(300, 250), AutoSize = true, Enabled = false };
			hexSearchCheckBox = new CheckBox { Text = "十六进制", Location = new Point(400, 250), AutoSize = true, Enabled = false };
			invertTextSearchCheckBox = new CheckBox { Text = "搜索不包含上述文本的文件", Location = new Point(100, 275), AutoSize = true, Enabled = false };

			// 编码选项
			var encodingLabel = new Label { Text = "编码：", Location = new Point(10, 300), AutoSize = true };
			ansiEncodingCheckBox = new CheckBox { Text = "ANSI 字符集(Windows)", Location = new Point(100, 300), AutoSize = true, Enabled = false };
			asciiEncodingCheckBox = new CheckBox { Text = "ASCII 字符集(DOS)", Location = new Point(300, 300), AutoSize = true, Enabled = false };
			utf16EncodingCheckBox = new CheckBox { Text = "Unicode UTF-16", Location = new Point(100, 325), AutoSize = true, Enabled = false };
			utf8EncodingCheckBox = new CheckBox { Text = "UTF8", Location = new Point(300, 325), AutoSize = true, Enabled = false };
			officeXmlCheckBox = new CheckBox { Text = "Office XML (docx, xlsx 等) 和 EPUB", Location = new Point(100, 350), AutoSize = true, Enabled = false };
			pluginCheckBox = new CheckBox { Text = "插件：", Location = new Point(100, 375), AutoSize = true, Enabled = false };

			// 添加控件到常规选项卡
			generalTab.Controls.AddRange(new Control[] {
				searchLabel, searchBox, locationLabel, locationBox, locationBrowseButton,
				drivesLabel, drivesList, regexCheckBox, selectedFilesCheckBox,
				everythingCheckBox, searchCompressedCheckBox, subFoldersLabel, subFoldersComboBox,
				findTextCheckBox, findTextBox, wholeWordCheckBox, caseSensitiveCheckBox,
				textRegexCheckBox, hexSearchCheckBox, invertTextSearchCheckBox,
				encodingLabel, ansiEncodingCheckBox, asciiEncodingCheckBox,
				utf16EncodingCheckBox, utf8EncodingCheckBox, officeXmlCheckBox, pluginCheckBox
			});
		}

		private void InitializeAdvancedTab()
		{
			// 文件日期区域
			var dateLabel = new Label { Text = "日期从(B)：", Location = new Point(10, 15), AutoSize = true };
			notBeforeDatePicker = new DateTimePicker { Location = new Point(100, 12), Width = 150, Format = DateTimePickerFormat.Short };
			var toLabel = new Label { Text = "到", Location = new Point(260, 15), AutoSize = true };
			beforeDatePicker = new DateTimePicker { Location = new Point(280, 12), Width = 150, Format = DateTimePickerFormat.Short };

			var notBeforeLabel = new Label { Text = "不早于(O)：", Location = new Point(10, 45), AutoSize = true };
			notBeforeValueTextBox = new TextBox { Location = new Point(100, 42), Width = 50 };
			notBeforeUnitComboBox = new ComboBox
			{
				Location = new Point(160, 42),
				Width = 80,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			notBeforeUnitComboBox.Items.AddRange(new object[] { "天", "周", "月", "年" });
			notBeforeUnitComboBox.SelectedIndex = 0;

			var beforeLabel = new Label { Text = "早于：", Location = new Point(260, 45), AutoSize = true };
			beforeValueTextBox = new TextBox { Location = new Point(310, 42), Width = 50 };
			beforeUnitComboBox = new ComboBox
			{
				Location = new Point(370, 42),
				Width = 80,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			beforeUnitComboBox.Items.AddRange(new object[] { "天", "周", "月", "年" });
			beforeUnitComboBox.SelectedIndex = 0;

			// 文件大小区域
			var fileSizeLabel = new Label { Text = "文件大小(D)：", Location = new Point(10, 75), AutoSize = true };
			fileSizeOperatorComboBox = new ComboBox
			{
				Location = new Point(100, 72),
				Width = 50,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			fileSizeOperatorComboBox.Items.AddRange(new object[] { "=", "<", ">" });
			fileSizeOperatorComboBox.SelectedIndex = 0;

			fileSizeValueTextBox = new TextBox { Location = new Point(160, 72), Width = 100 };
			fileSizeUnitComboBox = new ComboBox
			{
				Location = new Point(270, 72),
				Width = 80,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			fileSizeUnitComboBox.Items.AddRange(new object[] { "字节", "KB", "MB", "GB" });
			fileSizeUnitComboBox.SelectedIndex = 1;

			// 属性区域
			var attributesLabel = new Label { Text = "属性(T)：", Location = new Point(10, 105), AutoSize = true };
			archivedCheckBox = new CheckBox { Text = "存档(A)", Location = new Point(100, 105), AutoSize = true };
			hiddenCheckBox = new CheckBox { Text = "隐藏(H)", Location = new Point(200, 105), AutoSize = true };
			readOnlyCheckBox = new CheckBox { Text = "只读(R)", Location = new Point(300, 105), AutoSize = true };
			systemCheckBox = new CheckBox { Text = "系统(Y)", Location = new Point(100, 130), AutoSize = true };
			folderCheckBox = new CheckBox { Text = "文件夹(D)", Location = new Point(200, 130), AutoSize = true };
			encryptedCheckBox = new CheckBox { Text = "加密(E)", Location = new Point(300, 130), AutoSize = true };

			// 重复文件区域
			duplicateFilesCheckBox = new CheckBox { Text = "搜索重复的文件(P)：", Location = new Point(10, 160), AutoSize = true };
			sameNameCheckBox = new CheckBox { Text = "名称相同", Location = new Point(100, 185), AutoSize = true, Enabled = false };
			sameSizeCheckBox = new CheckBox { Text = "大小相同(Z)", Location = new Point(200, 185), AutoSize = true, Enabled = false };
			sameContentCheckBox = new CheckBox { Text = "内容相同(M)", Location = new Point(300, 185), AutoSize = true, Enabled = false };
			samePluginFieldsCheckBox = new CheckBox { Text = "插件字段相同(U)", Location = new Point(100, 210), AutoSize = true, Enabled = false };
			pluginFieldsComboBox = new ComboBox
			{
				Location = new Point(250, 207),
				Width = 200,
				Enabled = false
			};

			// 添加控件到高级选项卡
			advancedTab.Controls.AddRange(new Control[] {
				dateLabel, notBeforeDatePicker, toLabel, beforeDatePicker,
				notBeforeLabel, notBeforeValueTextBox, notBeforeUnitComboBox,
				beforeLabel, beforeValueTextBox, beforeUnitComboBox,
				fileSizeLabel, fileSizeOperatorComboBox, fileSizeValueTextBox, fileSizeUnitComboBox,
				attributesLabel, archivedCheckBox, hiddenCheckBox, readOnlyCheckBox,
				systemCheckBox, folderCheckBox, encryptedCheckBox,
				duplicateFilesCheckBox, sameNameCheckBox, sameSizeCheckBox,
				sameContentCheckBox, samePluginFieldsCheckBox, pluginFieldsComboBox
			});
		}

		private void InitializePluginsTab()
		{
			// 插件选项卡内容
			usePluginsCheckBox = new CheckBox { Text = "使用插件搜索(I)", Location = new Point(10, 15), AutoSize = true };

			// 匹配规则区域
			var matchRuleLabel = new Label { Text = "匹配规则：", Location = new Point(10, 45), AutoSize = true };
			andRuleRadioButton = new RadioButton { Text = "与(全部匹配)(A)", Location = new Point(100, 45), AutoSize = true, Checked = true };
			orRuleRadioButton = new RadioButton { Text = "或(部分匹配)(O)", Location = new Point(250, 45), AutoSize = true };

			// 规则按钮
			addRuleButton = new Button { Text = "增加规则(M)", Location = new Point(10, 75), Width = 100 };
			removeRuleButton = new Button { Text = "减少规则(E)", Location = new Point(120, 75), Width = 100 };

			// 规则列表 - 使用DataGridView替代ListView以支持嵌入控件
			rulesDataGridView = new DataGridView
			{
				Location = new Point(10, 105),
				Size = new Size(550, 320),
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = true,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				EditMode = DataGridViewEditMode.EditOnEnter,
				MultiSelect = true
			};

			// 添加列
			var pluginColumn = new DataGridViewComboBoxColumn
			{
				HeaderText = "插件",
				Name = "PluginColumn",
				Width = 100
			};
			pluginColumn.Items.AddRange(new object[] { "插件1", "插件2", "插件3" });

			var attributeColumn = new DataGridViewComboBoxColumn
			{
				HeaderText = "属性",
				Name = "AttributeColumn",
				Width = 100
			};
			attributeColumn.Items.AddRange(new object[] { "属性1", "属性2", "属性3" });

			var operatorColumn = new DataGridViewComboBoxColumn
			{
				HeaderText = "操作符",
				Name = "OperatorColumn",
				Width = 80
			};
			operatorColumn.Items.AddRange(new object[] { "包含", "等于", "大于", "小于", "开始于", "结束于" });

			var valueColumn = new DataGridViewTextBoxColumn
			{
				HeaderText = "值",
				Name = "ValueColumn",
				Width = 270
			};

			rulesDataGridView.Columns.AddRange(new DataGridViewColumn[] { pluginColumn, attributeColumn, operatorColumn, valueColumn });

			// 添加控件到插件选项卡
			pluginsTab.Controls.AddRange(new Control[] {
				usePluginsCheckBox, 
				matchRuleLabel, andRuleRadioButton, orRuleRadioButton,
				addRuleButton, removeRuleButton, rulesDataGridView
			});

			// 绑定事件处理程序
			addRuleButton.Click += AddRuleButton_Click;
			removeRuleButton.Click += RemoveRuleButton_Click;
		}
		

		private void InitializeRulesTab()
		{
			// 加载/保存页面内容留空
			// 这里的内容已经移动到插件页面
		}

		// 增加规则按钮点击事件处理程序
		private void AddRuleButton_Click(object sender, EventArgs e)
		{
			// 创建一个新的规则行项目
			AddRuleItem();
		}

		// 删除规则按钮点击事件处理程序
		private void RemoveRuleButton_Click(object sender, EventArgs e)
		{
			// 检查是否有选中的行
			if (rulesDataGridView.SelectedRows.Count > 0)
			{
				// 删除选中的行
				foreach (DataGridViewRow row in rulesDataGridView.SelectedRows)
				{
					rulesDataGridView.Rows.Remove(row);
				}
			}
			else
			{
				MessageBox.Show("请先选择要删除的规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		// 添加规则行项目的辅助方法
		private void AddRuleItem(string plugin = "插件1", string attribute = "属性1", string op = "包含", string value = "请输入值")
		{
			// 创建一个新的DataGridView行
			int rowIndex = rulesDataGridView.Rows.Add();
			DataGridViewRow row = rulesDataGridView.Rows[rowIndex];
			
			// 设置单元格的值
			row.Cells["PluginColumn"].Value = plugin;
			row.Cells["AttributeColumn"].Value = attribute;
			row.Cells["OperatorColumn"].Value = op;
			row.Cells["ValueColumn"].Value = value;

			// 选中新添加的行
			rulesDataGridView.ClearSelection();
			row.Selected = true;
			rulesDataGridView.CurrentCell = row.Cells[0];
			rulesDataGridView.FirstDisplayedScrollingRowIndex = rowIndex;
		}

		private void InitializeResultsArea()
		{
			// 创建结果区域
			var resultsPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(10)
			};

			// 创建结果导航区域
			var navigationPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 30
			};

			prevResultButton = new Button { Text = "<", Width = 30, Location = new Point(10, 5) };
			nextResultButton = new Button { Text = ">", Width = 30, Location = new Point(45, 5) };
			searchInResultsCheckBox = new CheckBox { Text = "F2 搜索找到的文件/文件夹", Location = new Point(85, 5), AutoSize = true };

			navigationPanel.Controls.AddRange(new Control[] { prevResultButton, nextResultButton, searchInResultsCheckBox });

			// 创建结果列表
			resultsListView = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				GridLines = true,
				MultiSelect = true
			};

			resultsListView.Columns.Add("文件名", 200);
			resultsListView.Columns.Add("路径", 300);
			resultsListView.Columns.Add("大小", 100);
			resultsListView.Columns.Add("日期", 150);

			// 创建结果操作按钮区域
			var resultButtonsPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 40,
				Padding = new Padding(5)
			};

			viewButton = new Button { Text = "F3 查看", Width = 80, Location = new Point(10, 5) };
			editButton = new Button { Text = "F4 编辑", Width = 80, Location = new Point(100, 5) };
			newSearchButton = new Button { Text = "新建搜索(N)", Width = 100, Location = new Point(190, 5) };
			gotoFileButton = new Button { Text = "转到此文件(G)", Width = 100, Location = new Point(300, 5) };
			exportListButton = new Button { Text = "输出到列表(L)", Width = 100, Location = new Point(410, 5) };

			resultButtonsPanel.Controls.AddRange(new Control[] { viewButton, editButton, newSearchButton, gotoFileButton, exportListButton });

			// 创建状态栏
			statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
			statusLabel = new ToolStripStatusLabel { Text = "准备就绪" };
			statusStrip.Items.Add(statusLabel);

			// 添加控件到结果区域
			resultsPanel.Controls.Add(resultsListView);
			resultsPanel.Controls.Add(navigationPanel);
			resultsPanel.Controls.Add(resultButtonsPanel);
			resultsPanel.Controls.Add(statusStrip);

			// 添加结果区域到窗体
			this.Controls.Add(resultsPanel);
		}

		private void LoadHistory()
		{
			// 加载搜索历史和位置历史
			try
			{
				string historyFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SearchHistory.txt");
				if (File.Exists(historyFile))
				{
					var lines = File.ReadAllLines(historyFile);
					foreach (var line in lines)
					{
						if (line.StartsWith("SEARCH:"))
						{
							searchHistory.Add(line.Substring(7));
						}
						else if (line.StartsWith("LOCATION:"))
						{
							locationHistory.Add(line.Substring(9));
						}
					}

					// 设置自动完成源
					var searchAutoComplete = new AutoCompleteStringCollection();
					searchAutoComplete.AddRange(searchHistory.ToArray());
					searchBox.AutoCompleteCustomSource = searchAutoComplete;

					var locationAutoComplete = new AutoCompleteStringCollection();
					locationAutoComplete.AddRange(locationHistory.ToArray());
					locationBox.AutoCompleteCustomSource = locationAutoComplete;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"加载搜索历史时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void SaveHistory()
		{
			// 保存搜索历史和位置历史
			try
			{
				string historyFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SearchHistory.txt");
				using (var writer = new StreamWriter(historyFile))
				{
					foreach (var item in searchHistory)
					{
						writer.WriteLine($"SEARCH:{item}");
					}

					foreach (var item in locationHistory)
					{
						writer.WriteLine($"LOCATION:{item}");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存搜索历史时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void StartSearchButton_Click(object sender, EventArgs e)
		{
			// 开始搜索
			if (string.IsNullOrWhiteSpace(searchBox.Text) && !findTextCheckBox.Checked)
			{
				MessageBox.Show("请输入搜索关键词或选择查找文本选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 保存搜索历史
			if (!string.IsNullOrWhiteSpace(searchBox.Text) && !searchHistory.Contains(searchBox.Text))
			{
				searchHistory.Insert(0, searchBox.Text);
				if (searchHistory.Count > 20) // 限制历史记录数量
				{
					searchHistory.RemoveAt(searchHistory.Count - 1);
				}
			}

			// 保存位置历史
			if (!string.IsNullOrWhiteSpace(locationBox.Text) && !locationHistory.Contains(locationBox.Text))
			{
				locationHistory.Insert(0, locationBox.Text);
				if (locationHistory.Count > 20) // 限制历史记录数量
				{
					locationHistory.RemoveAt(locationHistory.Count - 1);
				}
			}

			SaveHistory();

			// 清空结果列表
			resultsListView.Items.Clear();
			searchResults.Clear();

			// 获取搜索参数
			string searchPattern = searchBox.Text;
			string searchPath = string.IsNullOrWhiteSpace(locationBox.Text) ?
				Path.GetDirectoryName(Application.ExecutablePath) : locationBox.Text;

			// 设置搜索选项
			SearchOption searchOption = subFoldersComboBox.SelectedIndex == 0 ?
				SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

			// 更新状态栏
			statusLabel.Text = $"正在搜索: {searchPath}";
			Application.DoEvents();

			try
			{
				// 执行搜索
				List<string> files;
				if (regexCheckBox.Checked)
				{
					// 使用正则表达式搜索
					var regex = new Regex(searchPattern, RegexOptions.IgnoreCase);
					files = Directory.GetFiles(searchPath, "*.*", searchOption)
						.Where(file => regex.IsMatch(Path.GetFileName(file)))
						.ToList();
				}
				else
				{
					// 使用通配符搜索
					files = Directory.GetFiles(searchPath, $"*{searchPattern}*", searchOption).ToList();
				}

				// 如果需要搜索文件内容
				if (findTextCheckBox.Checked && !string.IsNullOrWhiteSpace(findTextBox.Text))
				{
					var textFiles = new List<string>();
					foreach (var file in files)
					{
						try
						{
							// 检查文件是否是文本文件
							if (Helper.IsTextFile(file))
							{
								string content = File.ReadAllText(file);
								bool found = false;

								if (textRegexCheckBox.Checked)
								{
									// 使用正则表达式搜索文本内容
									var regex = caseSensitiveCheckBox.Checked ?
										new Regex(findTextBox.Text) :
										new Regex(findTextBox.Text, RegexOptions.IgnoreCase);
									found = regex.IsMatch(content);
								}
								else
								{
									// 普通文本搜索
									StringComparison comparison = caseSensitiveCheckBox.Checked ?
										StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

									if (wholeWordCheckBox.Checked)
									{
										// 全字匹配
										string pattern = $"\\b{Regex.Escape(findTextBox.Text)}\\b";
										var regex = caseSensitiveCheckBox.Checked ?
											new Regex(pattern) :
											new Regex(pattern, RegexOptions.IgnoreCase);
										found = regex.IsMatch(content);
									}
									else
									{
										// 普通包含匹配
										found = content.IndexOf(findTextBox.Text, comparison) >= 0;
									}
								}

								// 如果选择了反向搜索，则取反结果
								if (invertTextSearchCheckBox.Checked)
								{
									found = !found;
								}

								if (found)
								{
									textFiles.Add(file);
									Debug.Print($"{file} found");
								}
							}
						}
						catch (Exception)
						{
							// 忽略无法读取的文件
							continue;
						}
					}

					files = textFiles;
				}

				// 显示搜索结果
				foreach (var file in files)
				{
					var fileInfo = new FileInfo(file);
					var item = new ListViewItem(fileInfo.Name);
					item.SubItems.Add(fileInfo.DirectoryName);
					item.SubItems.Add(FileSystemManager.FormatFileSize(fileInfo.Length, true));
					item.SubItems.Add(fileInfo.LastWriteTime.ToString());
					item.Tag = file; // 保存完整路径
					resultsListView.Items.Add(item);
					searchResults.Add(file);
				}

				// 更新状态栏
				statusLabel.Text = $"找到了 {files.Count} 个文件";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"搜索文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				statusLabel.Text = " ";
				var a = new ComboBox
				{
					Location = new Point(160, 42),
					Width = 80,
					DropDownStyle = ComboBoxStyle.DropDownList

				};
				notBeforeUnitComboBox.Items.AddRange(new object[] { "天", "周", "月", "年" });
				notBeforeUnitComboBox.SelectedIndex = 0;

				var beforeLabel = new Label { Text = "早于：", Location = new Point(260, 45), AutoSize = true };
				beforeValueTextBox = new TextBox { Location = new Point(310, 42), Width = 50 };
				//beforeUnitComboBox =
			}
		}
	}
}