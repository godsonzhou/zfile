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

        // 修改缓冲区
        private Dictionary<string, ViewSwitchRule> bufferViewSwitchRules;


		//private ListBox fileTypeListBox;

		public AutoSwitchViewPanel(Form1 mainForm)
        {
            this.mainForm = mainForm;
            // 初始化缓冲区
            InitializeBuffer();
            InitializeComponents();
            LoadRules();
        }

        private void InitializeBuffer()
        {
            // 创建缓冲区并从ViewMgr复制数据
            bufferViewSwitchRules = new Dictionary<string, ViewSwitchRule>();
            foreach (var kvp in mainForm.viewMgr.viewSwitchRules)
            {
                bufferViewSwitchRules[kvp.Key] = new ViewSwitchRule
                {
                    rules = kvp.Value.rules,
                    mode = kvp.Value.mode
                };
            }
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
            
            // 添加选择变更事件处理
            grid.SelectionChanged += Grid_SelectionChanged;

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
            
            // 添加子规则直接编辑事件处理
            ruleDetailGrid.CellValueChanged += RuleDetailGrid_CellValueChanged;

            // 添加控件到规则配置组
            ruleConfigGroup.Controls.AddRange(new Control[] {
                ruleDetailGrid,
                subRuleButtonPanel
            });

            Controls.Add(ruleConfigGroup);

            // 不再需要确定和取消按钮，因为使用OptionsForm中的按钮
        }

        private void LoadRules()
        {
            // 从缓冲区加载规则
			foreach(var r in bufferViewSwitchRules.Values)
				grid.Rows.Add(r.rules.Substring(0,1), r.rules.Substring(1), r.mode);
		}

        private void AddRule()
        {
            // 添加新的规则
            string newRuleType = "+";
            string newFileTypes = "*.txt";
            string newViewMode = "默认";
            
            // 添加到缓冲区
            string newKey = (bufferViewSwitchRules.Count + 1).ToString();
            bufferViewSwitchRules[newKey] = new ViewSwitchRule
            {
                rules = newRuleType + newFileTypes,
                mode = newViewMode
            };
            
            // 更新界面
            grid.Rows.Add(newRuleType, newFileTypes, newViewMode);
            
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
                var selectedRow = grid.SelectedRows[0];
                string ruleType = selectedRow.Cells[0].Value?.ToString() ?? "";
                string fileTypes = selectedRow.Cells[1].Value?.ToString() ?? "";
                string viewMode = selectedRow.Cells[2].Value?.ToString() ?? "";
                
                // 从缓冲区中删除
                string keyToRemove = null;
                foreach (var kvp in bufferViewSwitchRules)
                {
                    if (kvp.Value.rules.Substring(0, 1) == ruleType && 
                        kvp.Value.rules.Substring(1) == fileTypes && 
                        kvp.Value.mode == viewMode)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                
                if (keyToRemove != null)
                {
                    bufferViewSwitchRules.Remove(keyToRemove);
                }
                
                // 从界面中删除
                grid.Rows.RemoveAt(selectedRow.Index);
            }
        }
        
        private void AddSubRule()
        {
            // 检查是否有选中的主规则
            if (grid.SelectedRows.Count == 0)
                return;
                
            var selectedRow = grid.SelectedRows[0];
            string ruleType = selectedRow.Cells[0].Value?.ToString() ?? "";
            string fileTypes = selectedRow.Cells[1].Value?.ToString() ?? "";
            string viewMode = selectedRow.Cells[2].Value?.ToString() ?? "";
            
            // 查找对应的缓冲区规则
            string keyToUpdate = null;
            foreach (var kvp in bufferViewSwitchRules)
            {
                if (kvp.Value.rules.Substring(0, 1) == ruleType && 
                    kvp.Value.rules.Substring(1) == fileTypes && 
                    kvp.Value.mode == viewMode)
                {
                    keyToUpdate = kvp.Key;
                    break;
                }
            }
            
            if (keyToUpdate != null)
            {
                // 添加新的子规则
                string newSubRuleType = "+";
                string newSubRuleValue = "*.txt";
                
                // 更新缓冲区中的规则
                var rule = bufferViewSwitchRules[keyToUpdate];
                rule.rules += "|" + newSubRuleType + newSubRuleValue;
                
                // 更新界面
                ruleDetailGrid.Rows.Add("或", "+:完全符合", newSubRuleValue, viewMode);
                
                // 选中新添加的行
                int newRowIndex = ruleDetailGrid.Rows.Count - 1;
                ruleDetailGrid.ClearSelection();
                ruleDetailGrid.Rows[newRowIndex].Selected = true;
            }
        }
        
        private void DeleteSubRule()
        {
            // 检查是否有选中的主规则和子规则
            if (grid.SelectedRows.Count == 0 || ruleDetailGrid.SelectedRows.Count == 0)
                return;
                
            var selectedMainRow = grid.SelectedRows[0];
            var selectedSubRow = ruleDetailGrid.SelectedRows[0];
            
            string ruleType = selectedMainRow.Cells[0].Value?.ToString() ?? "";
            string fileTypes = selectedMainRow.Cells[1].Value?.ToString() ?? "";
            string viewMode = selectedMainRow.Cells[2].Value?.ToString() ?? "";
            
            // 获取选中的子规则信息
            string subRuleTypeText = selectedSubRow.Cells[1].Value?.ToString() ?? "";
            string subRuleValue = selectedSubRow.Cells[2].Value?.ToString() ?? "";
            
            // 从子规则文本中提取规则类型字符
            string subRuleType = subRuleTypeText.Split(':')[0];
            
            // 查找对应的缓冲区规则
            string keyToUpdate = null;
            foreach (var kvp in bufferViewSwitchRules)
            {
                if (kvp.Value.rules.Substring(0, 1) == ruleType && 
                    kvp.Value.rules.Substring(1) == fileTypes && 
                    kvp.Value.mode == viewMode)
                {
                    keyToUpdate = kvp.Key;
                    break;
                }
            }
            
            if (keyToUpdate != null)
            {
                // 更新缓冲区中的规则
                var rule = bufferViewSwitchRules[keyToUpdate];
                string[] subRules = rule.rules.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                
                // 构建新的规则字符串，排除要删除的子规则
                string newRules = "";
                for (int i = 0; i < subRules.Length; i++)
                {
                    string subRule = subRules[i];
                    if (subRule.Length > 1)
                    {
                        string currentSubRuleType = subRule.Substring(0, 1);
                        string currentSubRuleValue = subRule.Substring(1);
                        
                        // 如果不是要删除的子规则，则添加到新规则中
                        if (!(currentSubRuleType == subRuleType && currentSubRuleValue == subRuleValue) || i == 0) // 保留主规则
                        {
                            if (newRules.Length > 0)
                                newRules += "|";
                            newRules += subRule;
                        }
                    }
                }
                
                // 更新缓冲区中的规则
                rule.rules = newRules;
                
                // 从界面中删除
                ruleDetailGrid.Rows.RemoveAt(selectedSubRow.Index);
            }
        }

     
        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            // 清空子规则表格
            ruleDetailGrid.Rows.Clear();
            
            // 检查是否有选中的行
            if (grid.SelectedRows.Count > 0)
            {
                var selectedRow = grid.SelectedRows[0];
                string ruleType = selectedRow.Cells[0].Value?.ToString() ?? "";
                string fileTypes = selectedRow.Cells[1].Value?.ToString() ?? "";
                string viewMode = selectedRow.Cells[2].Value?.ToString() ?? "";
                
                // 构建完整规则字符串用于查找
                string fullRule = ruleType + fileTypes;
                
                // 在缓冲区中查找匹配的规则
                foreach (var rule in bufferViewSwitchRules.Values)
                {
                    if (rule.rules.Substring(1) == fileTypes && rule.mode == viewMode)
                    {
                        // 找到匹配的规则，解析子规则并添加到ruleDetailGrid
                        string[] subRules = rule.rules.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        // 第一个子规则是主规则类型，从第二个开始是子规则
                        for (int i = 0; i < subRules.Length; i++)
                        {
                            string subRule = subRules[i];
                            if (subRule.Length > 1)
                            {
                                string subRuleType = subRule.Substring(0, 1);
                                string subRuleValue = subRule.Substring(1);
                                
                                // 添加到子规则表格
                                string operatorValue = i == 1 ? "且" : "或"; // 第一个子规则默认为"且"，其余为"或"
                                
                                // 查找规则类型对应的显示文本
                                string ruleTypeText = ruletypeString.Split('\n')
                                    .FirstOrDefault(r => r.StartsWith(subRuleType)) ?? subRuleType + ":未知";
                                
                                ruleDetailGrid.Rows.Add(operatorValue, ruleTypeText, subRuleValue, viewMode);
                            }
                        }
                        break;
                    }
                }
            }
        }
        
        // 应用更改，将缓冲区数据写入ViewMgr
        public void ApplyChanges()
        {
            // 清空ViewMgr中的规则
            mainForm.viewMgr.viewSwitchRules.Clear();
            
            // 将缓冲区数据复制到ViewMgr
            foreach (var kvp in bufferViewSwitchRules)
            {
                mainForm.viewMgr.viewSwitchRules[kvp.Key] = new ViewSwitchRule
                {
                    rules = kvp.Value.rules,
                    mode = kvp.Value.mode
                };
            }
            
            // 保存配置
            SaveViewSwitchRules();
            
            MessageBox.Show("视图切换规则已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        // 取消更改，放弃缓冲区数据
        public void CancelChanges()
        {
            // 重新初始化缓冲区
            InitializeBuffer();
            
            // 重新加载规则到界面
            grid.Rows.Clear();
            ruleDetailGrid.Rows.Clear();
            LoadRules();
        }
        
        // 处理子规则直接编辑事件
        private void RuleDetailGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // 检查是否有选中的主规则
            if (grid.SelectedRows.Count == 0 || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
                
            var selectedMainRow = grid.SelectedRows[0];
            string ruleType = selectedMainRow.Cells[0].Value?.ToString() ?? "";
            string fileTypes = selectedMainRow.Cells[1].Value?.ToString() ?? "";
            string viewMode = selectedMainRow.Cells[2].Value?.ToString() ?? "";
            
            // 获取修改后的子规则信息
            var editedRow = ruleDetailGrid.Rows[e.RowIndex];
            string operatorValue = editedRow.Cells[0].Value?.ToString() ?? "";
            string subRuleTypeText = editedRow.Cells[1].Value?.ToString() ?? "";
            string subRuleValue = editedRow.Cells[2].Value?.ToString() ?? "";
            string subViewMode = editedRow.Cells[3].Value?.ToString() ?? "";
            
            // 从子规则文本中提取规则类型字符
            string subRuleType = subRuleTypeText.Split(':')[0];
            
            // 查找对应的缓冲区规则
            string keyToUpdate = null;
            foreach (var kvp in bufferViewSwitchRules)
            {
                if (kvp.Value.rules.Substring(0, 1) == ruleType && 
                    kvp.Value.rules.Substring(1) == fileTypes && 
                    kvp.Value.mode == viewMode)
                {
                    keyToUpdate = kvp.Key;
                    break;
                }
            }
            
            if (keyToUpdate != null)
            {
                // 更新缓冲区中的规则
                UpdateSubRuleInBuffer(keyToUpdate, e.RowIndex, subRuleType, subRuleValue, subViewMode);
            }
        }
        
        // 更新缓冲区中的子规则
        private void UpdateSubRuleInBuffer(string key, int rowIndex, string subRuleType, string subRuleValue, string subViewMode)
        {
            var rule = bufferViewSwitchRules[key];
            string[] subRules = rule.rules.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            
            // 如果是第一行（主规则），则更新主规则
            if (rowIndex == 0 && subRules.Length > 0)
            {
                // 更新主规则
                subRules[0] = subRuleType + subRuleValue;
                rule.mode = subViewMode;
            }
            else if (rowIndex < subRules.Length)
            {
                // 更新子规则
                subRules[rowIndex] = subRuleType + subRuleValue;
            }
            
            // 重建规则字符串
            rule.rules = string.Join("|", subRules);
        }
        
        // 保存视图切换规则到配置文件
        private void SaveViewSwitchRules()
        {
            try
            {
                // 创建配置项列表
                var items = new List<ConfigItem>();
                
                foreach (var kvp in mainForm.viewMgr.viewSwitchRules)
                {
                    items.Add(new ConfigItem
                    {
                        Key = kvp.Key + "_rules",
                        Value = kvp.Value.rules
                    });
                    
                    items.Add(new ConfigItem
                    {
                        Key = kvp.Key + "_mode",
                        Value = kvp.Value.mode
                    });
                }
                
                // 更新配置
                mainForm.configLoader.AddOrUpdateSection("ViewModeSwitch", items);
                mainForm.configLoader.SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存视图切换规则失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}