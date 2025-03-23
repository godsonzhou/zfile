using System.Data;
namespace zfile
{
	public class CustomViewForm : Form
	{
		private DataGridView grid;
		private Form1 mainForm;

		public CustomViewForm(Form1 mainForm)
		{
			this.mainForm = mainForm;
			InitializeComponents();
			LoadCustomViews();
		}

		private void InitializeComponents()
		{
			Text = "自定义视图配置";
			Size = new Size(800, 500);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;

			// 创建DataGridView显示自定义视图配置
			grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = true,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false
			};

			// 添加列
			grid.Columns.Add("ViewName", "视图名称");
			grid.Columns.Add("ColumnsDescription", "包含的列描述");

			// 添加按钮面板
			FlowLayoutPanel buttonPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				FlowDirection = FlowDirection.RightToLeft,
				Height = 40,
				Padding = new Padding(5)
			};

			Button btnNew = new Button { Text = "新建(N)", Width = 80 };
			Button btnEdit = new Button { Text = "编辑(E)", Width = 80 };
			Button btnDelete = new Button { Text = "删除(D)", Width = 80 };
			Button btnCopy = new Button { Text = "复制(C)", Width = 80 };

			buttonPanel.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete, btnCopy });

			// 添加事件处理
			btnNew.Click += (s, e) => AddCustomView();
			btnEdit.Click += (s, e) => EditCustomView();
			btnDelete.Click += (s, e) => DeleteCustomView();
			btnCopy.Click += (s, e) => CopyCustomView();

			Controls.Add(grid);
			Controls.Add(buttonPanel);
		}

		private void LoadCustomViews()
		{
			// 从viewmgr.viewmodes中加载数据
			foreach(var v in mainForm.viewMgr.colDefDict.Keys)
				grid.Rows.Add(v, mainForm.viewMgr.GetColDef(v));
		}

		private void AddCustomView()
		{
			using var addForm = new CustomViewEditForm("新建自定义视图", mainForm);
			if (addForm.ShowDialog() == DialogResult.OK)
			{
				// 将所有列标题连接成一个字符串，用逗号分隔
				var columnTitles = string.Join(",", addForm.Columns.Select(c => c.Title));
				
				// 添加视图名称和列描述字符串
				grid.Rows.Add(addForm.ViewName, columnTitles);
			}
		}

		private void EditCustomView()
		{
			if (grid.SelectedRows.Count == 0) return;

			var row = grid.SelectedRows[0];
			string viewName = row.Cells["ViewName"].Value?.ToString() ?? "";
			
			// 从viewMgr.colDefDict中获取选中视图的列定义数据
			if (!mainForm.viewMgr.colDefDict.ContainsKey(viewName))
			{
				MessageBox.Show($"找不到视图 '{viewName}' 的定义", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			
			using var editForm = new CustomViewEditForm(viewName, mainForm);
			
			// 从colDefDict中加载列定义数据到CustomViewEditForm
			var colDefs = mainForm.viewMgr.colDefDict[viewName];
			editForm.Columns.Clear();
			foreach (var colDef in colDefs)
			{
				string alignment = "<-"; // 默认左对齐
				// 根据实际情况设置对齐方式
				if (colDef.content.Contains("->]") || colDef.content.Contains("=tc.大小"))
				{
					alignment = "->";
				}
				
				editForm.Columns.Add(new CustomViewEditForm.CustomViewColumn(
					colDef.header,
					colDef.width,
					alignment,
					colDef.content
				));
			}
			
			// 保存原始视图名称，用于在视图名称更改时更新字典键
			string originalViewName = viewName;
			
			if (editForm.ShowDialog() == DialogResult.OK)
			{
				// 更新UI中的视图名称
				row.Cells["ViewName"].Value = editForm.ViewName;
				
				// 创建新的ColDef列表
				List<Zfile.ColDef> newColDefs = new List<Zfile.ColDef>();
				foreach (var column in editForm.Columns)
				{
					newColDefs.Add(new Zfile.ColDef
					{
						header = column.Title,
						width = column.Width,
						content = column.Content
					});
				}
				
				// 如果视图名称已更改，则需要删除旧键并添加新键
				if (originalViewName != editForm.ViewName)
				{
					mainForm.viewMgr.colDefDict.Remove(originalViewName);
					mainForm.viewMgr.colDefDict[editForm.ViewName] = newColDefs;
				}
				else
				{
					// 直接更新现有键的值
					mainForm.viewMgr.colDefDict[editForm.ViewName] = newColDefs;
				}
				
				// 更新列描述
				row.Cells["ColumnsDescription"].Value = mainForm.viewMgr.GetColDef(editForm.ViewName);
			}
		}

		private void DeleteCustomView()
		{
			if (grid.SelectedRows.Count > 0)
			{
				if (MessageBox.Show("确定要删除所选视图吗？", "确认删除",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
				}
			}
		}

		private void CopyCustomView()
		{
			if (grid.SelectedRows.Count == 0) return;

			var row = grid.SelectedRows[0];
			int newIndex = grid.Rows.Add();
			DataGridViewRow newRow = grid.Rows[newIndex];

			foreach (DataGridViewCell cell in row.Cells)
			{
				newRow.Cells[cell.ColumnIndex].Value = cell.Value + " (复制)";
			}

			grid.ClearSelection();
			newRow.Selected = true;
		}
	}
}