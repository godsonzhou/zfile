namespace zfile
{
	using System;
	using System.ComponentModel;

	public class ThemeManager : IDisposable
	{
		private readonly Form mainForm;
		private readonly ToolStrip toolStrip;
		private readonly ToolStrip vtoolStrip;
		private readonly MenuStrip menuStrip;
		private readonly TreeView leftTree;
		private readonly TreeView rightTree;
		private readonly ListView leftList;
		private readonly ListView rightList;
		private readonly TextBox leftPreview;
		private readonly TextBox rightPreview;
		private readonly StatusStrip leftStatusStrip;
		private readonly StatusStrip rightStatusStrip;
		private bool disposed = false;
		public bool IsDarkMode;

		public ThemeManager(Form form, ToolStrip toolStrip, ToolStrip vtoolStrip, MenuStrip menuStrip,
			TreeView leftTree, TreeView rightTree,
			ListView leftList, ListView rightList,
			TextBox leftPreview, TextBox rightPreview,
			StatusStrip leftStatusStrip, StatusStrip rightStatusStrip)
		{
			this.mainForm = form;
			this.toolStrip = toolStrip;
			this.vtoolStrip = vtoolStrip;
			this.menuStrip = menuStrip;
			this.leftTree = leftTree;
			this.rightTree = rightTree;
			this.leftList = leftList;
			this.rightList = rightList;
			this.leftPreview = leftPreview;
			this.rightPreview = rightPreview;
			this.leftStatusStrip = leftStatusStrip;
			this.rightStatusStrip = rightStatusStrip;

			// 注册Application.OpenForms的FormAdded事件
			//Application.OpenForms.CollectionChanged += OpenForms_CollectionChanged;
		}

		private void OpenForms_CollectionChanged(object sender, CollectionChangeEventArgs e)
		{
			if (e.Action == CollectionChangeAction.Add && e.Element is Form form)
			{
				// 当新窗体被添加时，根据当前主题模式应用相应的主题
				if (IsDarkMode)
				{
					ApplyDarkThemeToForm(form);
				}
				else
				{
					ApplyLightThemeToForm(form);
				}
			}
		}

		private void ApplyDarkThemeToForm(Form form)
		{
			form.BackColor = Color.FromArgb(45, 45, 48);
			form.ForeColor = Color.White;
			ApplyDarkThemeToControls(form.Controls);
		}

		private void ApplyLightThemeToForm(Form form)
		{
			form.BackColor = SystemColors.Control;
			form.ForeColor = SystemColors.ControlText;
			ApplyLightThemeToControls(form.Controls);
		}

		public void ApplyDarkTheme()
		{
			mainForm.BackColor = Color.FromArgb(45, 45, 48);
			mainForm.ForeColor = Color.White;
			toolStrip.BackColor = Color.FromArgb(28, 28, 28);
			toolStrip.ForeColor = Color.White;
			vtoolStrip.BackColor = Color.FromArgb(28, 28, 28);
			vtoolStrip.ForeColor = Color.White;
			menuStrip.BackColor = Color.FromArgb(28, 28, 28);
			menuStrip.ForeColor = Color.White;

			// 应用主题到所有下拉菜单
			ApplyMenuStripDarkTheme(menuStrip);

			SetTreeViewDarkTheme(leftTree);
			SetTreeViewDarkTheme(rightTree);
			SetListViewDarkTheme(leftList);
			SetListViewDarkTheme(rightList);
			SetPreviewDarkTheme(leftPreview);
			SetPreviewDarkTheme(rightPreview);
			SetStatusStripDarkTheme(leftStatusStrip);
			SetStatusStripDarkTheme(rightStatusStrip);

			// 应用主题到所有子控件
			ApplyDarkThemeToControls(mainForm.Controls);
			IsDarkMode = true;
		}

		public void ApplyLightTheme()
		{
			mainForm.BackColor = SystemColors.Control;
			mainForm.ForeColor = SystemColors.ControlText;
			toolStrip.BackColor = SystemColors.Control;
			toolStrip.ForeColor = SystemColors.ControlText;
			vtoolStrip.BackColor = SystemColors.Control;
			vtoolStrip.ForeColor = SystemColors.ControlText;
			menuStrip.BackColor = SystemColors.Control;
			menuStrip.ForeColor = SystemColors.ControlText;

			// 应用主题到所有下拉菜单
			ApplyMenuStripLightTheme(menuStrip);

			SetTreeViewLightTheme(leftTree);
			SetTreeViewLightTheme(rightTree);
			SetListViewLightTheme(leftList);
			SetListViewLightTheme(rightList);
			SetPreviewLightTheme(leftPreview);
			SetPreviewLightTheme(rightPreview);
			SetStatusStripLightTheme(leftStatusStrip);
			SetStatusStripLightTheme(rightStatusStrip);

			// 应用主题到所有子控件
			ApplyLightThemeToControls(mainForm.Controls);
			IsDarkMode = false;
		}

		private void ApplyMenuStripDarkTheme(MenuStrip menuStrip)
		{
			foreach (ToolStripMenuItem item in menuStrip.Items)
			{
				ApplyToolStripMenuItemDarkTheme(item);
			}
		}

		private void ApplyMenuStripLightTheme(MenuStrip menuStrip)
		{
			foreach (ToolStripMenuItem item in menuStrip.Items)
			{
				ApplyToolStripMenuItemLightTheme(item);
			}
		}

		private void ApplyToolStripMenuItemDarkTheme(ToolStripMenuItem item)
		{
			item.BackColor = Color.FromArgb(28, 28, 28);
			item.ForeColor = Color.White;
			
			if (item.DropDown is ToolStripDropDown dropDown)
			{
				dropDown.BackColor = Color.FromArgb(28, 28, 28);
				dropDown.ForeColor = Color.White;
			}

			foreach (ToolStripItem subItem in item.DropDownItems)
			{
				if (subItem is ToolStripMenuItem menuItem)
				{
					ApplyToolStripMenuItemDarkTheme(menuItem);
				}
			}
		}

		private void ApplyToolStripMenuItemLightTheme(ToolStripMenuItem item)
		{
			item.BackColor = SystemColors.Control;
			item.ForeColor = SystemColors.ControlText;

			if (item.DropDown is ToolStripDropDown dropDown)
			{
				dropDown.BackColor = SystemColors.Control;
				dropDown.ForeColor = SystemColors.ControlText;
			}

			foreach (ToolStripItem subItem in item.DropDownItems)
			{
				if (subItem is ToolStripMenuItem menuItem)
				{
					ApplyToolStripMenuItemLightTheme(menuItem);
				}
			}
		}

		private void SetToolStripDarkTheme(ToolStrip toolStrip)
		{
			toolStrip.BackColor = Color.FromArgb(28, 28, 28);
			toolStrip.ForeColor = Color.White;

			foreach (ToolStripItem item in toolStrip.Items)
			{
				if (item is ToolStripDropDownItem dropDownItem)
				{
					dropDownItem.BackColor = Color.FromArgb(28, 28, 28);
					dropDownItem.ForeColor = Color.White;
					if (dropDownItem.DropDown != null)
					{
						dropDownItem.DropDown.BackColor = Color.FromArgb(28, 28, 28);
						dropDownItem.DropDown.ForeColor = Color.White;
					}
				}
				else
				{
					item.BackColor = Color.FromArgb(28, 28, 28);
					item.ForeColor = Color.White;
				}
			}
		}

		private void SetToolStripLightTheme(ToolStrip toolStrip)
		{
			toolStrip.BackColor = SystemColors.Control;
			toolStrip.ForeColor = SystemColors.ControlText;

			foreach (ToolStripItem item in toolStrip.Items)
			{
				if (item is ToolStripDropDownItem dropDownItem)
				{
					dropDownItem.BackColor = SystemColors.Control;
					dropDownItem.ForeColor = SystemColors.ControlText;
					if (dropDownItem.DropDown != null)
					{
						dropDownItem.DropDown.BackColor = SystemColors.Control;
						dropDownItem.DropDown.ForeColor = SystemColors.ControlText;
					}
				}
				else
				{
					item.BackColor = SystemColors.Control;
					item.ForeColor = SystemColors.ControlText;
				}
			}
		}

		private void ApplyDarkThemeToControls(Control.ControlCollection controls)
		{
			foreach (Control control in controls)
			{
				if (control is ComboBox comboBox)
				{
					comboBox.BackColor = Color.FromArgb(37, 37, 38);
					comboBox.ForeColor = Color.White;
				}
				else if (control is TextBox textBox)
				{
					textBox.BackColor = Color.FromArgb(37, 37, 38);
					textBox.ForeColor = Color.White;
				}
				else if (control is Form form)
				{
					form.BackColor = Color.FromArgb(45, 45, 48);
					form.ForeColor = Color.White;
				}
				else if (control is ListView listView)
				{
					SetListViewDarkTheme(listView);
				}
				else if (control is TreeView treeView)
				{
					SetTreeViewDarkTheme(treeView);
				}
				else if (control is ToolStrip toolStrip)
				{
					SetToolStripDarkTheme(toolStrip);
				}

				// 递归处理子控件
				if (control.Controls.Count > 0)
				{
					ApplyDarkThemeToControls(control.Controls);
				}

				// 处理右键菜单
				if (control.ContextMenuStrip != null)
				{
					control.ContextMenuStrip.BackColor = Color.FromArgb(28, 28, 28);
					control.ContextMenuStrip.ForeColor = Color.White;
					foreach (ToolStripItem item in control.ContextMenuStrip.Items)
					{
						if (item is ToolStripMenuItem menuItem)
						{
							ApplyToolStripMenuItemDarkTheme(menuItem);
						}
					}
				}
			}
		}

		private void ApplyLightThemeToControls(Control.ControlCollection controls)
		{
			foreach (Control control in controls)
			{
				if (control is ComboBox comboBox)
				{
					comboBox.BackColor = SystemColors.Window;
					comboBox.ForeColor = SystemColors.WindowText;
				}
				else if (control is TextBox textBox)
				{
					textBox.BackColor = SystemColors.Window;
					textBox.ForeColor = SystemColors.WindowText;
				}
				else if (control is Form form)
				{
					form.BackColor = SystemColors.Control;
					form.ForeColor = SystemColors.ControlText;
				}
				else if (control is ListView listView)
				{
					SetListViewLightTheme(listView);
				}
				else if (control is TreeView treeView)
				{
					SetTreeViewLightTheme(treeView);
				}
				else if (control is ToolStrip toolStrip)
				{
					SetToolStripLightTheme(toolStrip);
				}
				// 递归处理子控件
				if (control.Controls.Count > 0)
				{
					ApplyLightThemeToControls(control.Controls);
				}

				// 处理右键菜单
				if (control.ContextMenuStrip != null)
				{
					control.ContextMenuStrip.BackColor = SystemColors.Control;
					control.ContextMenuStrip.ForeColor = SystemColors.ControlText;
					foreach (ToolStripItem item in control.ContextMenuStrip.Items)
					{
						if (item is ToolStripMenuItem menuItem)
						{
							ApplyToolStripMenuItemLightTheme(menuItem);
						}
					}
				}
			}
		}

		private void SetTreeViewDarkTheme(TreeView treeView)
		{
			treeView.BackColor = Color.FromArgb(37, 37, 38);
			treeView.ForeColor = Color.White;
		}

		private void SetTreeViewLightTheme(TreeView treeView)
		{
			treeView.BackColor = SystemColors.Window;
			treeView.ForeColor = SystemColors.WindowText;
		}

		private void SetListViewDarkTheme(ListView listView)
		{
			listView.BackColor = Color.FromArgb(37, 37, 38);
			listView.ForeColor = Color.White;
		}

		private void SetListViewLightTheme(ListView listView)
		{
			listView.BackColor = SystemColors.Window;
			listView.ForeColor = SystemColors.WindowText;
		}

		private void SetPreviewDarkTheme(TextBox preview)
		{
			preview.BackColor = Color.FromArgb(37, 37, 38);
			preview.ForeColor = Color.White;
		}

		private void SetPreviewLightTheme(TextBox preview)
		{
			preview.BackColor = SystemColors.Window;
			preview.ForeColor = SystemColors.WindowText;
		}

		private void SetStatusStripDarkTheme(StatusStrip statusStrip)
		{
			statusStrip.BackColor = Color.FromArgb(28, 28, 28);
			statusStrip.ForeColor = Color.White;
		}

		private void SetStatusStripLightTheme(StatusStrip statusStrip)
		{
			statusStrip.BackColor = SystemColors.Control;
			statusStrip.ForeColor = SystemColors.ControlText;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 取消注册事件处理程序
					//Application.OpenForms.CollectionChanged -= OpenForms_CollectionChanged;
				}

				// 释放非托管资源
				disposed = true;
			}
		}

		~ThemeManager()
		{
			Dispose(false);
		}
	}
}