using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
			
		}

		/// <summary>
		/// 初始化模板面板
		/// </summary>
		private void InitializeTemplatesPanel()
        {
            // 创建模板面板
            Panel templatesPanel = new Panel();
            templatesPanel.BorderStyle = BorderStyle.FixedSingle;
            templatesPanel.Location = new Point(12, 400);
            templatesPanel.Size = new Size(560, 120);

            // 创建标题标签
            Label titleLabel = new Label();
            titleLabel.Text = "过滤器模板";
            titleLabel.Location = new Point(10, 10);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(titleLabel.Font, FontStyle.Bold);

            // 创建模板列表框
            templatesListBox = new ListBox();
            templatesListBox.Location = new Point(10, 30);
            templatesListBox.Size = new Size(200, 80);
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

            // 调整对话框大小
            this.Height += 140;
			var buttonOK = new Button();
			var buttonCancel = new Button();
			var buttonClear = new Button();
			buttonOK.Click += buttonOK_Click;
			buttonCancel.Click += ButtonCancel_Click;

			// 调整确定、取消和清除按钮的位置
			buttonOK.Location = new Point(buttonOK.Location.X, buttonOK.Location.Y + 140);
            buttonCancel.Location = new Point(buttonCancel.Location.X, buttonCancel.Location.Y + 140);
            buttonClear.Location = new Point(buttonClear.Location.X, buttonClear.Location.Y + 140);

            // 添加面板到对话框
            this.Controls.Add(templatesPanel);

            // 加载模板列表
            Filter.FilterManager.Instance.LoadSearchTemplates(templatesListBox);
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
        private void buttonOK_Click(object sender, EventArgs e)
        {
            // 更新FilterManager的当前过滤器
            FilterManager.Instance.CurrentFilters = _filter;

            // Set dialog result and close
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}