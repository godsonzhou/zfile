using System.Data;
namespace zfile
{
    public class AutoSwitchViewPanel : Panel
    {
        private DataGridView grid;
        private Form1 mainForm;
        private CheckBox enableAutoSwitchCheckBox;
        private ComboBox ruleTypeComboBox;
        
        //private ListBox fileTypeListBox;

        public AutoSwitchViewPanel(Form1 mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponents();
            LoadRules();
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;

            // 创建启用自动切换的复选框
            enableAutoSwitchCheckBox = new CheckBox
            {
                Text = "更改文件类型时自动切换视图模式(V)",
                AutoSize = true,
                Location = new Point(10, 10),
                Checked = true
            };
            Controls.Add(enableAutoSwitchCheckBox);

            // 创建DataGridView显示规则配置
            grid = new DataGridView
            {
                Location = new Point(10, 40),
                Width = 600,
                Height = 200,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // 添加列
            grid.Columns.Add("RuleType", "规则类型");
            grid.Columns.Add("FileTypes", "文件类型");
            grid.Columns.Add("ViewMode", "视图模式");

            Controls.Add(grid);

            // 添加按钮面板
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Location = new Point(620, 40),
                Width = 100,
                Height = 200,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5)
            };

            Button btnAdd = new Button { Text = "添加(A)", Width = 80 };
            Button btnDelete = new Button { Text = "删除(D)", Width = 80 };

            buttonPanel.Controls.AddRange(new Control[] { btnAdd, btnDelete });

            // 添加事件处理
            btnAdd.Click += (s, e) => AddRule();
            btnDelete.Click += (s, e) => DeleteRule();

            Controls.Add(buttonPanel);

            // 创建规则配置区域
            GroupBox ruleConfigGroup = new GroupBox
            {
                Text = "规则配置",
                Location = new Point(10, 250),
                Width = 710,
                Height = 200
            };

         
            // 文件类型列表
            

           

            // 添加控件到规则配置组
            ruleConfigGroup.Controls.AddRange(new Control[] {
               
                
            });

            Controls.Add(ruleConfigGroup);
        }

        private void LoadRules()
        {
            // 加载示例规则
            grid.Rows.Add("文件扩展名", "*.mp4;*.mkv;*.avi", "视频");
            grid.Rows.Add("文件扩展名", "*.jpg;*.png;*.gif", "图片");
            grid.Rows.Add("文件扩展名", "*.mp3;*.wav;*.flac", "音频");
            grid.Rows.Add("文件扩展名", "*.doc;*.docx;*.pdf", "文档");
            grid.Rows.Add("文件名通配符", "*源代码*;*source*", "源码");
            grid.Rows.Add("文件夹路径", "C:\\Program Files\\*", "程序");
        }

        private void AddRule()
        {
        
        
        }

        private void DeleteRule()
        {
            // 删除选中的规则
            if (grid.SelectedRows.Count > 0)
            {
                grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
            }
        }

     
    }
}