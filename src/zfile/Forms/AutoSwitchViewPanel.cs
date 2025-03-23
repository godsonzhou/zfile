using System.Data;
namespace zfile
{
    public class AutoSwitchViewPanel : Panel
    {
        private DataGridView grid;
        private DataGridView ruleDetailGrid;
        private Form1 mainForm;
        private CheckBox enableAutoSwitchCheckBox;
        private ComboBox ruleTypeComboBox;
		private string ruletypeString = "+:完全符合\n-:完全不符合\n%:至少有一半符合\n2:至少有一个符合\nD:文件夹\nL:含驱动器符\nU:网络路径：IV服务器\nV:虚拟文件夹\nF: FTP 连接\nA:压缩文件\nP:文件系统插件\nS:搜索结果";

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
			grid.Columns[0].Width = 70;

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

            // 创建规则详情DataGridView
            ruleDetailGrid = new DataGridView
            {
                Location = new Point(10, 20),
                Width = 600,
                Height = 130,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
			// add operator column
			var operatorColumn = new DataGridViewComboBoxColumn
			{
				HeaderText = "操作符",
				Name = "OperatorColumn",
				Width = 60
			};
			operatorColumn.Items.AddRange(new object[] { "且", "或" });
			// 添加规则类型列（ComboBox列）
			var ruleTypeColumn = new DataGridViewComboBoxColumn
            {
                HeaderText = "规则",
                Name = "RuleTypeColumn",
                Width = 100
            };
            ruleTypeColumn.Items.AddRange(ruletypeString.Split('\n'));

            // 添加文件类型列（文本框列）
            var fileTypeColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "文件类型",
                Name = "FileTypeColumn",
                Width = 250
            };

            // 添加视图模式列（文本框列）
            var viewModeColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "视图模式",
                Name = "ViewModeColumn",
                Width = 250
            };

            // 添加列到DataGridView
			ruleDetailGrid.Columns.Add(operatorColumn);
            ruleDetailGrid.Columns.Add(ruleTypeColumn);
            ruleDetailGrid.Columns.Add(fileTypeColumn);
            ruleDetailGrid.Columns.Add(viewModeColumn);
			ruleDetailGrid.Columns[0].Width = 60;
            // 创建按钮面板
            FlowLayoutPanel subRuleButtonPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 160),
                Width = 600,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0)
            };

            Button btnAddSubRule = new Button { Text = "增加子规则(M)", Width = 120 };
            Button btnDeleteSubRule = new Button { Text = "减少子规则(F)", Width = 120 };
			Label applytoviewLabel = new Label { Text = "应用到视图", Location = new Point(260, 20),  Width = 120 };
			ComboBox applytoviewCombo = new ComboBox { Text = "默认", Width = 120 };
			applytoviewCombo.Items.AddRange(mainForm.viewMgr.viewModes.Values.Select(v => v.Name).ToArray());
			subRuleButtonPanel.Controls.AddRange(new Control[] { btnAddSubRule, btnDeleteSubRule, applytoviewLabel, applytoviewCombo });

            // 添加事件处理
            btnAddSubRule.Click += (s, e) => AddSubRule();
            btnDeleteSubRule.Click += (s, e) => DeleteSubRule();

            // 添加控件到规则配置组
            ruleConfigGroup.Controls.AddRange(new Control[] {
                ruleDetailGrid,
                subRuleButtonPanel
            });

            Controls.Add(ruleConfigGroup);
        }

        private void LoadRules()
        {
            // 加载示例规则
			foreach(var r in mainForm.viewMgr.viewSwitchRules.Values)
				grid.Rows.Add(r.rules.Substring(0,1), r.rules.Substring(1), r.mode);
			//grid.Rows.Add("文件扩展名", "*.mp4;*.mkv;*.avi", "视频");
			//grid.Rows.Add("文件扩展名", "*.jpg;*.png;*.gif", "图片");
			//grid.Rows.Add("文件扩展名", "*.mp3;*.wav;*.flac", "音频");
			//grid.Rows.Add("文件扩展名", "*.doc;*.docx;*.pdf", "文档");
			//grid.Rows.Add("文件名通配符", "*源代码*;*source*", "源码");
			//grid.Rows.Add("文件夹路径", "C:\\Program Files\\*", "程序");
		}

        private void AddRule()
        {
            // 添加新的规则
            string newRuleType = "文件扩展名";
            string newFileTypes = "*.txt";
            string newViewMode = "默认";
            grid.Rows.Add("且", newRuleType, newFileTypes, newViewMode);
            
            // 选中新添加的行
            int newRowIndex = grid.Rows.Count - 1;
            grid.ClearSelection();
            grid.Rows[newRowIndex].Selected = true;
        }

        private void DeleteRule()
        {
            // 删除选中的规则
            if (grid.SelectedRows.Count > 0)
            {
                grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
            }
        }
        
        private void AddSubRule()
        {
            // 添加新的子规则
            ruleDetailGrid.Rows.Add("且", "+:完全符合", "*.txt", "默认");
            
            // 选中新添加的行
            int newRowIndex = ruleDetailGrid.Rows.Count - 1;
            ruleDetailGrid.ClearSelection();
            ruleDetailGrid.Rows[newRowIndex].Selected = true;
        }
        
        private void DeleteSubRule()
        {
            // 删除选中的子规则
            if (ruleDetailGrid.SelectedRows.Count > 0)
            {
                ruleDetailGrid.Rows.RemoveAt(ruleDetailGrid.SelectedRows[0].Index);
            }
        }

     
    }
}