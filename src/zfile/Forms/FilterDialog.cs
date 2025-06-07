using zfile.Filter;

namespace zfile.Forms
{
    public partial class FilterDialog : Form
    {
        private List<FileFilter> _filter = [];
        private MainForm owner;
        private ListBox templatesListBox;
        private TextBox templateNameTextBox;

        public FilterDialog(MainForm owner = null, FileFilter? filter = null)
        {
            InitializeComponent();

            this.owner = owner;

            // Initialize filter object
            if (filter != null)
                _filter.Add(filter);
            else
                _filter = FilterManager.Instance.CurrentFilters;

            // 初始化模板面板
            InitializeTemplatesPanel();
        }

		private void InitializeComponent()
		{
			this.Text = "文件过滤器";
			this.Size = new Size(600, 550);
			this.StartPosition = FormStartPosition.CenterParent;
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			
			// 创建过滤器输入区域
			Panel filterPanel = new Panel();
			filterPanel.BorderStyle = BorderStyle.FixedSingle;
			filterPanel.Location = new Point(12, 12);
			filterPanel.Size = new Size(560, 150);
			this.Controls.Add(filterPanel);
			
			// 创建标题标签
			Label filterTitleLabel = new Label();
			filterTitleLabel.Text = "文件过滤条件";
			filterTitleLabel.Location = new Point(10, 10);
			filterTitleLabel.AutoSize = true;
			filterTitleLabel.Font = new Font(filterTitleLabel.Font, FontStyle.Bold);
			filterPanel.Controls.Add(filterTitleLabel);
			
			// 创建过滤器类型选择区域
			Label typeLabel = new Label();
			typeLabel.Text = "过滤器类型:";
			typeLabel.Location = new Point(10, 40);
			typeLabel.AutoSize = true;
			filterPanel.Controls.Add(typeLabel);
			
			ComboBox typeComboBox = new ComboBox();
			typeComboBox.Location = new Point(100, 40);
			typeComboBox.Size = new Size(150, 23);
			typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			typeComboBox.Items.AddRange(new object[] { "包含", "排除", "正则表达式" });
			typeComboBox.SelectedIndex = 0;
			filterPanel.Controls.Add(typeComboBox);
			
			// 创建过滤器模式输入区域
			Label patternLabel = new Label();
			patternLabel.Text = "过滤模式:";
			patternLabel.Location = new Point(10, 70);
			patternLabel.AutoSize = true;
			filterPanel.Controls.Add(patternLabel);
			
			TextBox patternTextBox = new TextBox();
			patternTextBox.Location = new Point(100, 70);
			patternTextBox.Size = new Size(440, 23);
			filterPanel.Controls.Add(patternTextBox);
			
			// 创建过滤器说明
			Label hintLabel = new Label();
			hintLabel.Text = "提示: 使用通配符 * 和 ? 进行匹配，多个模式用 ; 分隔";
			hintLabel.Location = new Point(100, 100);
			hintLabel.AutoSize = true;
			hintLabel.ForeColor = Color.Gray;
			filterPanel.Controls.Add(hintLabel);
			
			// 创建按钮
			var buttonOK = new Button();
			buttonOK.Text = "确定";
			buttonOK.DialogResult = DialogResult.OK;
			buttonOK.Location = new Point(400, 530);
			buttonOK.Size = new Size(80, 30);
			this.Controls.Add(buttonOK);
			buttonOK.Click += buttonOK_Click;
			
			var buttonCancel = new Button();
			buttonCancel.Text = "取消";
			buttonCancel.DialogResult = DialogResult.Cancel;
			buttonCancel.Location = new Point(490, 530);
			buttonCancel.Size = new Size(80, 30);
			this.Controls.Add(buttonCancel);
			buttonCancel.Click += ButtonCancel_Click;
			
			var buttonClear = new Button();
			buttonClear.Text = "清除";
			buttonClear.Location = new Point(310, 530);
			buttonClear.Size = new Size(80, 30);
			this.Controls.Add(buttonClear);
		}

		/// <summary>
		/// 初始化模板面板
		/// </summary>
		private void InitializeTemplatesPanel()
        {
            // 创建模板面板
            Panel templatesPanel = new Panel();
            templatesPanel.BorderStyle = BorderStyle.FixedSingle;
            templatesPanel.Location = new Point(12, 170);
            templatesPanel.Size = new Size(560, 250);

            // 创建标题标签
            Label titleLabel = new Label();
            titleLabel.Text = "过滤器模板";
            titleLabel.Location = new Point(10, 10);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(titleLabel.Font, FontStyle.Bold);

            // 创建模板列表框
            templatesListBox = new ListBox();
            templatesListBox.Location = new Point(10, 30);
            templatesListBox.Size = new Size(200, 200);
            templatesListBox.SelectedIndexChanged += TemplatesListBox_SelectedIndexChanged;
            templatesListBox.DoubleClick += TemplatesListBox_DoubleClick;

            // 创建模板名称文本框
            Label nameLabel = new Label();
            nameLabel.Text = "模板名称:";
            nameLabel.Location = new Point(220, 30);
            nameLabel.AutoSize = true;

            templateNameTextBox = new TextBox();
            templateNameTextBox.Location = new Point(280, 30);
            templateNameTextBox.Size = new Size(150, 23);

            // 添加控件到面板
            templatesPanel.Controls.Add(titleLabel);
            templatesPanel.Controls.Add(templatesListBox);
            templatesPanel.Controls.Add(nameLabel);
            templatesPanel.Controls.Add(templateNameTextBox);

			var buttonOK = new Button() { Text = "确定" };
			var buttonCancel = new Button() { Text = "取消"};
			var buttonDefine = new Button() { Text = "定义"};
			buttonOK.Click += buttonOK_Click;
			buttonCancel.Click += ButtonCancel_Click;
			buttonDefine.Click += ButtonDefine_Click;

			var buttonPnl = new FlowLayoutPanel();
			buttonPnl.FlowDirection = FlowDirection.RightToLeft;
			buttonPnl.Dock = DockStyle.Bottom;
			buttonPnl.Width = 600;
			buttonPnl.Height = 50;
			buttonPnl.Controls.Add(buttonDefine);
			buttonPnl.Controls.Add(buttonOK);
			buttonPnl.Controls.Add(buttonCancel);

            // 添加面板到对话框
            this.Controls.Add(templatesPanel);
			this.Controls.Add(buttonPnl);

			// 加载模板列表
			Filter.FilterManager.Instance.LoadSearchTemplates(templatesListBox);
        }

		private void ButtonDefine_Click(object? sender, EventArgs e)
		{
			var searchdialog = new SearchforDialog(owner);
			searchdialog.ShowDialog();
		}

		private void ButtonCancel_Click(object? sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// 模板列表选择变更事件处理
		/// </summary>
		private void TemplatesListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (templatesListBox.SelectedItem != null)
            {
                templateNameTextBox.Text = templatesListBox.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// 模板列表双击事件处理
        /// </summary>
        private void TemplatesListBox_DoubleClick(object? sender, EventArgs e)
        {
            if (templatesListBox.SelectedItem != null)
            {
                Filter.FilterManager.Instance.LoadSearchTemplate(templatesListBox.SelectedItem.ToString());
            }
        }   

        /// <summary>
        /// Handle OK button click event
        /// </summary>
        private void buttonOK_Click(object? sender, EventArgs e)
        {
            // 更新FilterManager的当前过滤器
            FilterManager.Instance.CurrentFilters = _filter;

            // Set dialog result and close
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}