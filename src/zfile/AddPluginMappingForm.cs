using System.Data;
namespace zfile
{
	public class AddPluginMappingForm : Form
	{
		public ComboBox pluginCombo;
		public TextBox extensionBox;
		private CheckBox[] capCheckBoxes;
		private WcxModule selectedModule;
		private WcxModuleList wcxModules;

		public string SelectedPlugin => pluginCombo.SelectedItem?.ToString() ?? "";
		public string Extension => extensionBox.Text;
		public int Caps { get; private set; }

		public AddPluginMappingForm(WcxModuleList wcxModules)
		{
			this.wcxModules = wcxModules;
			Text = "添加插件映射";
			Size = new Size(500, 400);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;

			TableLayoutPanel mainLayout = new()
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 3,
				Padding = new Padding(10)
			};

			// 创建上部面板（插件和扩展名）
			TableLayoutPanel topPanel = new()
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 2,
				Margin = new Padding(0, 0, 0, 10)
			};

			topPanel.Controls.Add(new Label { Text = "插件:" }, 0, 0);
			pluginCombo = new ComboBox { Dock = DockStyle.Fill };
			pluginCombo.Items.AddRange(wcxModules._modules.Select(m => m.Name).ToArray());
			pluginCombo.SelectedIndexChanged += PluginCombo_SelectedIndexChanged;
			topPanel.Controls.Add(pluginCombo, 1, 0);

			topPanel.Controls.Add(new Label { Text = "扩展名:" }, 0, 1);
			extensionBox = new TextBox { Dock = DockStyle.Fill };
			topPanel.Controls.Add(extensionBox, 1, 1);

			// 创建中部面板（功能标志复选框）
			Panel capsPanel = new()
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(0, 0, 0, 10)
			};

			// 创建标志复选框
			FlowLayoutPanel checkBoxPanel = new()
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = true,
				Padding = new Padding(5)
			};

			capCheckBoxes = new CheckBox[10];
			var capsValues = new[]
			{
				(PackerCaps.PK_CAPS_NEW, "创建新的压缩文件"),
				(PackerCaps.PK_CAPS_MODIFY, "修改现有的压缩文件"),
				(PackerCaps.PK_CAPS_MULTIPLE, "压缩文件可以包含多个文件"),
				(PackerCaps.PK_CAPS_DELETE, "可以删除压缩文件中的文件"),
				(PackerCaps.PK_CAPS_OPTIONS, "具有选项对话框"),
				(PackerCaps.PK_CAPS_MEMPACK, "支持在内存中打包"),
				(PackerCaps.PK_CAPS_BY_CONTENT, "通过内容检测压缩文件类型"),
				(PackerCaps.PK_CAPS_SEARCHTEXT, "允许在压缩文件中搜索文本"),
				(PackerCaps.PK_CAPS_HIDE, "隐藏压缩文件中的文件"),
				(PackerCaps.PK_CAPS_ENCRYPT, "支持加密")
			};

			for (int i = 0; i < capCheckBoxes.Length; i++)
			{
				var (cap, text) = capsValues[i];
				capCheckBoxes[i] = new CheckBox
				{
					Text = text,
					Tag = (int)cap,
					AutoSize = true,
					Enabled = false,
					Margin = new Padding(5),
					Width = 220
				};
				checkBoxPanel.Controls.Add(capCheckBoxes[i]);
			}

			capsPanel.Controls.Add(checkBoxPanel);

			// 创建底部面板（按钮）
			FlowLayoutPanel buttonPanel = new()
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft
			};

			Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
			Button btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK };
			buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

			// 添加所有面板到主布局
			mainLayout.Controls.Add(topPanel);
			mainLayout.Controls.Add(capsPanel);
			mainLayout.Controls.Add(buttonPanel);

			// 设置行高比例
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10));

			Controls.Add(mainLayout);
		}

		private void PluginCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var moduleName = SelectedPlugin;
			selectedModule = wcxModules.FindModuleByName(moduleName);

			if (selectedModule != null)
			{
				// 更新CAPS值
				Caps = selectedModule.caps;
				UpdateCapCheckBoxes();
			}
			else
			{
				Caps = 0;
				ResetCapCheckBoxes();
			}
		}

		private void UpdateCapCheckBoxes()
		{
			foreach (var cb in capCheckBoxes)
			{
				int capValue = (int)cb.Tag;
				cb.Checked = (Caps & capValue) != 0;
				cb.Enabled = true;
			}
		}

		private void ResetCapCheckBoxes()
		{
			foreach (var cb in capCheckBoxes)
			{
				cb.Checked = false;
				cb.Enabled = false;
			}
		}
	}
}