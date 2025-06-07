using zfile.Filter;

namespace zfile.Forms
{
    public partial class FilterDialog : Form
    {
        private List<FileFilter>? _filter = [];
        private MainForm owner;
        private ListBox templatesListBox;
        private ListBox selectedFiltersListBox;
        private TextBox templateNameTextBox;
		public List<FileFilter>? SelectedFilter => _filter;
        
        // 保存初始状态，用于取消时恢复
        private List<FileFilter> _originalFilters = [];
        private bool _originalFilterAndOr;
        
        public FilterDialog(MainForm owner = null, FileFilter? filter = null)
        {
            InitializeComponent();

            this.owner = owner;

            // 保存FilterManager的初始状态
            _originalFilters = FilterManager.Instance.CurrentFilters.ToList();
            _originalFilterAndOr = FilterManager.Instance.CurrentFilterAndOr;
            
            // 初始化本地状态
            _currentAndOrState = FilterManager.Instance.CurrentFilterAndOr;
            
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
		}

		/// <summary>
		/// 初始化模板面板
		/// </summary>
		private void InitializeTemplatesPanel()
        {
            // 创建模板面板
            Panel templatesPanel = new Panel();
            templatesPanel.Location = new Point(12, 12);
            templatesPanel.Size = new Size(560, 400);

            // 创建可用过滤器标题标签
            Label titleLabel = new Label();
            titleLabel.Text = "可用过滤器模板";
            titleLabel.Location = new Point(10, 10);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(titleLabel.Font, FontStyle.Bold);

            // 创建可用过滤器列表框
            templatesListBox = new ListBox();
            templatesListBox.Location = new Point(10, 30);
            templatesListBox.Size = new Size(200, 350);
            templatesListBox.SelectedIndexChanged += TemplatesListBox_SelectedIndexChanged;
            templatesListBox.DoubleClick += TemplatesListBox_DoubleClick;

            // 创建已选择过滤器标题标签
            Label selectedTitleLabel = new Label();
            selectedTitleLabel.Text = "已选择的过滤器";
            selectedTitleLabel.Location = new Point(350, 10);
            selectedTitleLabel.AutoSize = true;
            selectedTitleLabel.Font = new Font(selectedTitleLabel.Font, FontStyle.Bold);

            // 创建已选择过滤器列表框
            selectedFiltersListBox = new ListBox();
            selectedFiltersListBox.Location = new Point(350, 30);
            selectedFiltersListBox.Size = new Size(200, 350);
            selectedFiltersListBox.DoubleClick += SelectedFiltersListBox_DoubleClick;

            // 创建操作按钮
            Button addButton = new Button();
            addButton.Text = "添加到已选择 >";
            addButton.Location = new Point(220, 100);
            addButton.Size = new Size(120, 30);
            addButton.Click += AddButton_Click;

            Button removeButton = new Button();
            removeButton.Text = "< 从已选择中移除";
            removeButton.Location = new Point(220, 150);
            removeButton.Size = new Size(120, 30);
            removeButton.Click += RemoveButton_Click;

            Button clearButton = new Button();
            clearButton.Text = "清空已选择";
            clearButton.Location = new Point(220, 200);
            clearButton.Size = new Size(120, 30);
            clearButton.Click += ClearButton_Click;

			RadioButton andRadio = new RadioButton();
			andRadio.Text = "并且";
			andRadio.Location = new Point(250, 250);
			andRadio.Size = new Size(120, 30);
			andRadio.Checked = _currentAndOrState;

			RadioButton orRadio = new RadioButton();
			orRadio.Text = "或者";
			orRadio.Location = new Point(250, 280);
			orRadio.Size = new Size(120, 30);
			orRadio.Checked = !_currentAndOrState;

			andRadio.CheckedChanged += AndRadio_CheckedChanged;
			
			// 添加控件到面板
			templatesPanel.Controls.Add(titleLabel);
            templatesPanel.Controls.Add(templatesListBox);
            templatesPanel.Controls.Add(selectedTitleLabel);
            templatesPanel.Controls.Add(selectedFiltersListBox);
            templatesPanel.Controls.Add(addButton);
            templatesPanel.Controls.Add(removeButton);
            templatesPanel.Controls.Add(clearButton);
			templatesPanel.Controls.Add(andRadio);
			templatesPanel.Controls.Add(orRadio);

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
			Filter.FilterManager.Instance.LoadSearchTemplates(templatesListBox, true);
            // 加载已选择的过滤器列表
            LoadSelectedFilters();
        }

		private bool _currentAndOrState;
		
		private void AndRadio_CheckedChanged(object? sender, EventArgs e)
		{
			// 只在本地保存状态，不直接修改FilterManager
			_currentAndOrState = !_currentAndOrState;
		}

		/// <summary>
		/// 加载已选择的过滤器列表
		/// </summary>
		private void LoadSelectedFilters()
        {
            selectedFiltersListBox.Items.Clear();
            foreach (var name in FilterManager.Instance.CurrentFilterNames)
            {
                selectedFiltersListBox.Items.Add(name);
            }
        }

        /// <summary>
        /// 添加按钮点击事件
        /// </summary>
        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (templatesListBox.SelectedItem != null)
            {
                string selectedTemplate = templatesListBox.SelectedItem.ToString();
                var filter = FilterManager.Instance.LoadSearchTemplate(selectedTemplate);
                if (filter != null)
                {
                    // 添加到已选择列表
                    selectedFiltersListBox.Items.Add(selectedTemplate);
                    // 从可用列表中移除
                    templatesListBox.Items.Remove(selectedTemplate);
                    // 更新过滤器列表
                    FilterManager.Instance.AddFilter(filter);
                }
            }
        }

        /// <summary>
        /// 移除按钮点击事件
        /// </summary>
        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (selectedFiltersListBox.SelectedItem != null)
            {
                string selectedFilter = selectedFiltersListBox.SelectedItem.ToString();
                // 创建一个临时过滤器用于移除
                var tempFilter = new List<FileFilter> { new FileFilter(selectedFilter) };
                // 从已选择列表中移除
                selectedFiltersListBox.Items.Remove(selectedFilter);
                // 添加到可用列表
                templatesListBox.Items.Add(selectedFilter);
                // 更新过滤器列表
                FilterManager.Instance.RemoveFilter(tempFilter);
            }
        }

        /// <summary>
        /// 清空按钮点击事件
        /// </summary>
        private void ClearButton_Click(object? sender, EventArgs e)
        {
            // 清空已选择列表
            while (selectedFiltersListBox.Items.Count > 0)
            {
                string filterName = selectedFiltersListBox.Items[0].ToString();
                selectedFiltersListBox.Items.RemoveAt(0);
                templatesListBox.Items.Add(filterName);
            }
            // 清空过滤器
            FilterManager.Instance.ClearFilter();
        }

		private void ButtonDefine_Click(object? sender, EventArgs e)
		{
			var searchdialog = new SearchforDialog(owner);
			searchdialog.ShowDialog();
		}

		private void ButtonCancel_Click(object? sender, EventArgs e)
		{
			// 恢复FilterManager的初始状态
			FilterManager.Instance.SetFilter(_originalFilters);
			FilterManager.Instance.CurrentFilterAndOr = _originalFilterAndOr;
			
			// 设置对话框结果并关闭
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// <summary>
		/// 模板列表选择变更事件处理
		/// </summary>
		private void TemplatesListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (templatesListBox.SelectedItem != null)
            {
                //templateNameTextBox.Text = templatesListBox.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// 可用过滤器列表双击事件处理
        /// </summary>
        private void TemplatesListBox_DoubleClick(object? sender, EventArgs e)
        {
            AddButton_Click(sender, e);
        }   

        /// <summary>
        /// 已选择过滤器列表双击事件处理
        /// </summary>
        private void SelectedFiltersListBox_DoubleClick(object? sender, EventArgs e)
        {
            RemoveButton_Click(sender, e);
        }

        /// <summary>
        /// Handle OK button click event
        /// </summary>
        private void buttonOK_Click(object? sender, EventArgs e)
        {
            // 应用本地状态到FilterManager
            FilterManager.Instance.CurrentFilterAndOr = _currentAndOrState;
            
            // 获取当前过滤器
            _filter = FilterManager.Instance.CurrentFilters;
			//if (templatesListBox.SelectedItem != null)
			//	_filter = Filter.FilterManager.Instance.LoadSearchTemplate(templatesListBox.SelectedItem.ToString());

			// Set dialog result and close
			DialogResult = DialogResult.OK;
            Close();
        }
    }
}