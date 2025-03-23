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
			using var addForm = new CustomViewEditForm();
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
			using var editForm = new CustomViewEditForm
			{
				ViewName = row.Cells["ViewName"].Value?.ToString() ?? ""
			};
			
			// 添加默认列（这里只是示例，实际应用中应该从配置或数据库加载）
			// 在实际应用中，应该根据保存的配置来初始化列
			
			if (editForm.ShowDialog() == DialogResult.OK)
			{
				row.Cells["ViewName"].Value = editForm.ViewName;
				
				// 更新列信息（简化处理，只更新列标题）
				if (editForm.Columns.Count > 0) row.Cells["Size"].Value = editForm.Columns[0].Title;
				if (editForm.Columns.Count > 1) row.Cells["Date"].Value = editForm.Columns[1].Title;
				if (editForm.Columns.Count > 2) row.Cells["Type"].Value = editForm.Columns[2].Title;
				if (editForm.Columns.Count > 3) row.Cells["CreationTime"].Value = editForm.Columns[3].Title;
				if (editForm.Columns.Count > 4) row.Cells["ModifyTime"].Value = editForm.Columns[4].Title;
				if (editForm.Columns.Count > 5) row.Cells["AccessTime"].Value = editForm.Columns[5].Title;
				if (editForm.Columns.Count > 6) row.Cells["Attributes"].Value = editForm.Columns[6].Title;
				if (editForm.Columns.Count > 7) row.Cells["Comment"].Value = editForm.Columns[7].Title;
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