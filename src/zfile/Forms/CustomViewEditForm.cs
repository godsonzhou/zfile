using System.Data;
using System.Collections.Generic;
namespace zfile
{
    public class CustomViewEditForm : Form
    {
        private TextBox viewNameTextBox;
        private DataGridView columnsGrid;
        private Button addColumnButton;
        private Button deleteColumnButton;

        public string ViewName { get; set; } = "";
        public List<CustomViewColumn> Columns { get; set; } = new List<CustomViewColumn>();
		private Form1 form;

        public class CustomViewColumn
        {
            public string Title { get; set; }
            public int Width { get; set; }
            public string Alignment { get; set; }
            public string Content { get; set; }

            public CustomViewColumn(string title, int width, string alignment, string content)
            {
                Title = title;
                Width = width;
                Alignment = alignment;
                Content = content;
            }
        }

        public CustomViewEditForm(string viewName, Form1 form)
        {
			this.ViewName = viewName;
			this.form = form;
			InitializeComponents();
            // 添加一些默认列作为示例
            //if (Columns.Count == 0)
            //{
            //    Columns.Add(new CustomViewColumn("文件名", 150, "<-", "文件名"));
            //    Columns.Add(new CustomViewColumn("扩展名", 46, "<-", "扩展名"));
            //    Columns.Add(new CustomViewColumn("大小", 55, "->", "[=tc.大小]"));
            //    Columns.Add(new CustomViewColumn("属性", 23, "<-", "[=tc.属性]"));
            //    Columns.Add(new CustomViewColumn("修改日期", 66, "<-", "[=tc.修改日期]"));
            //    Columns.Add(new CustomViewColumn("LinkTarget", 161, "<-", "[=nl_info.Reparse Point Type] [=nl_info.Reparse Point Target]"));
            //    Columns.Add(new CustomViewColumn("备注", 151, "<-", "[=tc.备注]"));
            //}
			if(form.viewMgr.colDefDict.TryGetValue(viewName, out var colDefs))
			{
				foreach (var colDef in colDefs)
					Columns.Add(new CustomViewColumn(colDef.header, colDef.width, colDef.width > 0 ? "<-" : "->", colDef.content));
			}
			LoadColumnsToGrid();
        }

        private void InitializeComponents()
        {
            Text = "编辑自定义视图";
            Size = new Size(800, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // 主布局
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 视图名称面板
            Panel namePanel = new Panel { Dock = DockStyle.Fill, Height = 30 };
            Label nameLabel = new Label { Text = "配置自定义视图(C):", AutoSize = true, Location = new Point(5, 5) };
            Label numberLabel = new Label { Text = "1", AutoSize = true, Location = new Point(120, 5) };
            Label viewNameLabel = new Label { Text = "名称(N):", AutoSize = true, Location = new Point(150, 5) };
            viewNameTextBox = new TextBox { Text = ViewName, Location = new Point(210, 3), Width = 550 };
            
            namePanel.Controls.Add(nameLabel);
            namePanel.Controls.Add(numberLabel);
            namePanel.Controls.Add(viewNameLabel);
            namePanel.Controls.Add(viewNameTextBox);
            
            mainLayout.Controls.Add(namePanel, 0, 0);

            // 列表和标题面板
            Panel gridPanel = new Panel { Dock = DockStyle.Fill };
            
            // 标题行
            TableLayoutPanel headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                ColumnCount = 4,
                Width = 770
            };
            
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            
            headerPanel.Controls.Add(new Label { Text = "标题", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
            headerPanel.Controls.Add(new Label { Text = "宽度", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            headerPanel.Controls.Add(new Label { Text = "对齐", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 2, 0);
            headerPanel.Controls.Add(new Label { Text = "字段内容", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 3, 0);
            
            gridPanel.Controls.Add(headerPanel);
            
            // 数据网格
            columnsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 30),
                Height = 350,
                Width = 770,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            
            // 添加列
            columnsGrid.Columns.Add("Title", "标题");
            columnsGrid.Columns.Add("Width", "宽度");
            columnsGrid.Columns.Add("Alignment", "对齐");
            columnsGrid.Columns.Add("Content", "字段内容");
            
            columnsGrid.Columns[0].Width = 192;
            columnsGrid.Columns[1].Width = 192;
            columnsGrid.Columns[2].Width = 192;
            columnsGrid.Columns[3].Width = 192;
            
            // 添加按钮面板
            Panel buttonRow = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            
            addColumnButton = new Button { Text = "添加列(A)", Width = 100, Location = new Point(10, 5) };
            deleteColumnButton = new Button { Text = "删除所选列(D)", Width = 120, Location = new Point(120, 5) };
            
            addColumnButton.Click += AddColumn_Click;
            deleteColumnButton.Click += DeleteColumn_Click;
            
            buttonRow.Controls.Add(addColumnButton);
            buttonRow.Controls.Add(deleteColumnButton);
            
            gridPanel.Controls.Add(buttonRow);
            gridPanel.Controls.Add(columnsGrid);
            
            mainLayout.Controls.Add(gridPanel, 0, 1);

            // 底部按钮面板
            FlowLayoutPanel bottomButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
            Button btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 80 };
            Button btnApply = new Button { Text = "应用(P)", Width = 80 };
            
            bottomButtonPanel.Controls.Add(btnCancel);
            bottomButtonPanel.Controls.Add(btnOK);
            bottomButtonPanel.Controls.Add(btnApply);

            // 确定按钮事件
            btnOK.Click += (s, e) =>
            {
                ViewName = viewNameTextBox.Text;
                SaveColumnsFromGrid();
            };
            
            btnApply.Click += (s, e) =>
            {
                ViewName = viewNameTextBox.Text;
                SaveColumnsFromGrid();
            };

            mainLayout.Controls.Add(bottomButtonPanel, 0, 2);

            Controls.Add(mainLayout);
            
            // 加载列数据到网格
            //LoadColumnsToGrid();
        }
        
        private void LoadColumnsToGrid()
        {
            columnsGrid.Rows.Clear();
            foreach (var column in Columns)
            {
                columnsGrid.Rows.Add(column.Title, column.Width, column.Alignment, column.Content);
            }
        }
        
        private void SaveColumnsFromGrid()
        {
            Columns.Clear();
            foreach (DataGridViewRow row in columnsGrid.Rows)
            {
                string title = row.Cells[0].Value?.ToString() ?? "";
                int width = 0;
                int.TryParse(row.Cells[1].Value?.ToString() ?? "0", out width);
                string alignment = row.Cells[2].Value?.ToString() ?? "<-";
                string content = row.Cells[3].Value?.ToString() ?? "";
                
                Columns.Add(new CustomViewColumn(title, width, alignment, content));
            }
        }
        
        private void AddColumn_Click(object sender, EventArgs e)
        {
            // 添加一个新列
            columnsGrid.Rows.Add("新列", "100", "<-", "");
        }
        
        private void DeleteColumn_Click(object sender, EventArgs e)
        {
            // 删除选中的列
            if (columnsGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in columnsGrid.SelectedRows)
                {
                    columnsGrid.Rows.Remove(row);
                }
            }
        }
        

        //private ComboBox CreateColumnComboBox(string selectedValue)
        //{
        //    var comboBox = new ComboBox
        //    {
        //        Dock = DockStyle.Fill,
        //        DropDownStyle = ComboBoxStyle.DropDownList
        //    };

        //    // 添加常用列选项
        //    string[] options = new string[] {
        //        "大小", "日期", "类型", "作者", "创建时间", "修改时间", "访问时间", "属性", "备注",
        //        "尺寸", "拍摄时间", "相机型号", "光圈大小", "曝光时间", "ISO", "位深", "光圈", "曝光",
        //        "艺术家", "标题", "专辑", "年份", "曲目", "流派", "比特率", "产品名称", "产品版本",
        //        "公司", "文件版本", "原始文件名", "描述", "版权", "主题"
        //    };

        //    comboBox.Items.AddRange(options);

        //    // 设置选中项
        //    if (!string.IsNullOrEmpty(selectedValue) && comboBox.Items.Contains(selectedValue))
        //        comboBox.SelectedItem = selectedValue;
        //    else if (comboBox.Items.Count > 0)
        //        comboBox.SelectedIndex = 0;

        //    return comboBox;
        //}
    }
}