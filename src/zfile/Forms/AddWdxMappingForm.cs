using System.Data;
namespace zfile.Forms
{
	public class AddWdxMappingForm : Form
	{
		public ComboBox pluginCombo;
		public TextBox extensionBox;
		private WdxModuleList wdxModules;

		public string SelectedPlugin => pluginCombo.SelectedItem?.ToString() ?? "";
		public string Extension => extensionBox.Text;

		public AddWdxMappingForm(WdxModuleList wdxModules)
		{
			this.wdxModules = wdxModules;
			Text = "添加WDX插件映射";
			Size = new Size(400, 150);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;

			TableLayoutPanel layout = new()
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 3,
				Padding = new Padding(10)
			};

			layout.Controls.Add(new Label { Text = "插件:" }, 0, 0);
			pluginCombo = new ComboBox { Dock = DockStyle.Fill };
			if (wdxModules != null && wdxModules._modules != null)
			{
				pluginCombo.Items.AddRange(wdxModules._modules.Select(m => m.Name).ToArray());
			}
			layout.Controls.Add(pluginCombo, 1, 0);

			layout.Controls.Add(new Label { Text = "规则表达式:" }, 0, 1);
			extensionBox = new TextBox { Dock = DockStyle.Fill };
			layout.Controls.Add(extensionBox, 1, 1);

			FlowLayoutPanel buttonPanel = new()
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft
			};

			Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
			Button btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK };
			buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

			layout.Controls.Add(buttonPanel, 1, 2);

			Controls.Add(layout);
		}
	}
}
