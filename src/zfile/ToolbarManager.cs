using System.Text;
using zfile;

public struct ToolbarButton
{
	public string name;
	public string cmd;
	public string icon;
	public string path;
	public string param;
	public string iconic;
	public ToolbarButton(string _name, string _cmd, string _icon, string _path, string _param, string _iconic)
	{
		name = _name;
		cmd = _cmd;
		icon = _icon;
		path = _path;
		param = _param;
		iconic = _iconic;
	}
}
public class ToolbarManager : IDisposable
{
	private Form1 form;
	//private UIControlManager uiControlManager;

	private ToolStrip dynamicToolStrip;
	public ToolStrip DynamicToolStrip => dynamicToolStrip;
	public List<ToolbarButton> toolbarButtons = new List<ToolbarButton>();
	public int ButtonCount => toolbarButtons.Count;
	private string configfile;
	// 添加上下文菜单属性
	private readonly ContextMenuStrip buttonContextMenu;
	private ToolStripButton? currentButton;
	private ToolStripDropDownButton? currentDropDownButton;
	private bool disposed = false;
	ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除按钮");
	ToolStripMenuItem copyItem = new ToolStripMenuItem("复制按钮");
	ToolStripMenuItem editItem = new ToolStripMenuItem("编辑按钮");
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
				// 取消事件订阅
				dynamicToolStrip.DragEnter -= form.ToolbarButton_DragEnter;
				dynamicToolStrip.DragDrop -= form.ToolbarButton_DragDrop;
				// 取消DriveBox事件订阅
				// 取消所有按钮的事件订阅
				deleteItem.Click -= DeleteButton_Click;
				copyItem.Click -= CopyButton_Click;
				editItem.Click -= EditButton_Click;
				foreach (ToolStripItem item in dynamicToolStrip.Items)
				{
					if (item is ToolStripButton button)
					{
						button.MouseUp -= Button_MouseUp;
					}
					else if (item is ToolStripDropDownButton dropDownButton)
					{
						dropDownButton.MouseUp -= Button_MouseUp;
					}
				}
				// 释放上下文菜单
				buttonContextMenu.Dispose();
			}

			// 释放非托管资源
			disposed = true;
		}
	}

	~ToolbarManager()
	{
		Dispose(false);
	}
	public ToolbarManager(Form1 form, string cfgfile, bool isVertical)
	{
		// 加载配置文件中的工具栏按钮信息并初始化控件,实现逻辑参照 initializeDynamicToolbar
		dynamicToolStrip = new ToolStrip();
		this.form = form;
		this.configfile = Helper.GetPathByEnv(cfgfile);
		// 初始化上下文菜单
		buttonContextMenu = new ContextMenuStrip();
		//var deleteItem = new ToolStripMenuItem("删除按钮");
		//var copyItem = new ToolStripMenuItem("复制按钮");
		//var editItem = new ToolStripMenuItem("编辑按钮");

		deleteItem.Click += DeleteButton_Click;
		copyItem.Click += CopyButton_Click;
		editItem.Click += EditButton_Click;

		buttonContextMenu.Items.Add(deleteItem);
		buttonContextMenu.Items.Add(copyItem);
		buttonContextMenu.Items.Add(editItem);

		Init(configfile);
		GenerateDynamicToolbar();

		if (isVertical)
		{
			dynamicToolStrip.Dock = DockStyle.Left;
			dynamicToolStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			//form.uiManager.MainContainer.Panel2.Controls.Add(dynamicToolStrip);
			//将vertical dynamictoolstrip移动到rightuppanel的左边
			form.uiManager.RightUpperPanel.Controls.Add(dynamicToolStrip);
		}
		else
		{
			form.Controls.Add(dynamicToolStrip);
		}
		dynamicToolStrip.AllowDrop = true;
		dynamicToolStrip.DragEnter += form.ToolbarButton_DragEnter;
		dynamicToolStrip.DragDrop += form.ToolbarButton_DragDrop;
	}

	public void AddButton(string name, string cmd, string icon, string path, string param, string iconic)
	{
		toolbarButtons.Add(new ToolbarButton(name, cmd, icon, path, param, iconic));
	}
	public void RemoveButton(int index)
	{
		toolbarButtons.RemoveAt(index);
	}

	public void EditButton(string name, string cmd, string icon, string path, string param, string iconic)
	{
		var form = new Form
		{
			Text = "编辑按钮",
			Size = new Size(400, 350),
			StartPosition = FormStartPosition.CenterParent,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			MaximizeBox = false,
			MinimizeBox = false
		};

		// 创建控件
		var nameLabel = new Label { Text = "名称:", Location = new Point(20, 20) };
		var nameTextBox = new TextBox
		{
			Location = new Point(120, 20),
			Width = 200,
			Text = name
		};

		var cmdLabel = new Label { Text = "命令:", Location = new Point(20, 60) };
		var cmdTextBox = new TextBox
		{
			Location = new Point(120, 60),
			Width = 200,
			Text = cmd
		};

		var iconLabel = new Label { Text = "图标:", Location = new Point(20, 100) };
		var iconTextBox = new TextBox
		{
			Location = new Point(120, 100),
			Width = 200,
			Text = icon
		};

		var pathLabel = new Label { Text = "路径:", Location = new Point(20, 140) };
		var pathTextBox = new TextBox
		{
			Location = new Point(120, 140),
			Width = 200,
			Text = path
		};

		var paramLabel = new Label { Text = "参数:", Location = new Point(20, 180) };
		var paramTextBox = new TextBox
		{
			Location = new Point(120, 180),
			Width = 200,
			Text = param
		};

		var iconicLabel = new Label { Text = "图标模式:", Location = new Point(20, 220) };
		var iconicComboBox = new ComboBox
		{
			Location = new Point(120, 220),
			Width = 200,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		iconicComboBox.Items.AddRange(new string[] { "0", "1", "2" });
		iconicComboBox.SelectedItem = iconic;

		var okButton = new Button
		{
			Text = "确定",
			DialogResult = DialogResult.OK,
			Location = new Point(120, 260)
		};

		var cancelButton = new Button
		{
			Text = "取消",
			DialogResult = DialogResult.Cancel,
			Location = new Point(240, 260)
		};

		// 添加控件到窗体
		form.Controls.AddRange(new Control[]
		{
				nameLabel, nameTextBox,
				cmdLabel, cmdTextBox,
				iconLabel, iconTextBox,
				pathLabel, pathTextBox,
				paramLabel, paramTextBox,
				iconicLabel, iconicComboBox,
				okButton, cancelButton
		});

		// 显示对话框
		if (form.ShowDialog() == DialogResult.OK)
		{
			// 更新按钮属性
			var button = toolbarButtons.FirstOrDefault(b => b.name == name);
			var index = toolbarButtons.IndexOf(button);
			if (index >= 0)
			{
				toolbarButtons[index] = new ToolbarButton(
					nameTextBox.Text,
					cmdTextBox.Text,
					iconTextBox.Text,
					pathTextBox.Text,
					paramTextBox.Text,
					iconicComboBox.SelectedItem?.ToString() ?? "0"
				);
				// 保存更改并刷新工具栏
				SaveToconfig();
				GenerateDynamicToolbar();
			}
		}
	}

	public void RemoveButton(string name)
	{
		if (string.IsNullOrEmpty(name)) return;

		// 删除指定名称的所有工具栏按钮
		toolbarButtons.RemoveAll(b => b.name == name);
	}
	public void SaveToconfig()
	{
		try
		{
			string configPath = Path.Combine(Constants.ZfileCfgPath, configfile);
			using (StreamWriter writer = new StreamWriter(configPath, false, Encoding.GetEncoding("GB2312")))
			{
				writer.WriteLine("[Buttonbar]");
				writer.WriteLine($"Buttoncount={toolbarButtons.Count}");

				for (int i = 0; i < toolbarButtons.Count; i++)
				{
					int buttonNumber = i + 1;
					ToolbarButton button = toolbarButtons[i];

					writer.WriteLine($"button{buttonNumber}={button.icon}");
					writer.WriteLine($"cmd{buttonNumber}={button.cmd}");
					writer.WriteLine($"iconic{buttonNumber}={button.iconic}");
					writer.WriteLine($"menu{buttonNumber}={button.name}");
					writer.WriteLine($"path{buttonNumber}={button.path}");
					writer.WriteLine($"param{buttonNumber}={button.param}");
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"保存工具栏配置失败：{ex.Message}", "错误",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
	public void GenerateDynamicToolbar()
	{
		// 遍历toolbarButtons列表，为每个按钮创建ToolStripButton或ToolStripDropDownButton，并添加到dynamicToolStrip中
		// 如果按钮的cmd属性以"openbar "开头，则创建ToolStripDropDownButton，并调用InitializeDropdownMenu方法初始化下拉菜单
		dynamicToolStrip.Items.Clear();
		for (int i = 0; i < toolbarButtons.Count; i++)
		{
			ToolbarButton b = toolbarButtons[i];
			// var zhdesc = form.cmdProcessor.cmdTable.GetByCmdName(cmd)?.ZhDesc ?? "";
			ToolStripButton button = new ToolStripButton
			{
				Text = "",  //menuText,
				ToolTipText = b.name,
				Image = form.iconManager.LoadIcon(b.icon),
				Tag = b.cmd
			};

			if (b.cmd.StartsWith("openbar"))
			{
				string dropdownFilePath = b.cmd.Substring("openbar ".Length);
				ToolStripDropDownButton dropdownButton = new ToolStripDropDownButton
				{
					Text = "", //menuText,
					ToolTipText = b.name,
					Image = form.iconManager.LoadIcon(b.icon)
				};
				// 为下拉按钮添加右键菜单
				dropdownButton.MouseUp += Button_MouseUp;
				form.uiManager.InitializeDropdownMenu(dropdownButton, dropdownFilePath);
				dynamicToolStrip.Items.Add(dropdownButton);
			}
			else
			{
				button.Click += form.uiManager.ToolbarButton_Click;
				// 为普通按钮添加右键菜单
				button.MouseUp += Button_MouseUp;
				dynamicToolStrip.Items.Add(button);
			}
		}
		dynamicToolStrip.Refresh();

	}
	// 处理按钮的鼠标事件
	private void Button_MouseUp(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
		{
			if (sender is ToolStripItem button)
			{
				currentButton = button as ToolStripButton;
				//buttonContextMenu.Show(dynamicToolStrip.PointToScreen(new Point(e.X, e.Y)));
				// 获取鼠标的屏幕坐标
				Point screenPoint = Cursor.Position;
				buttonContextMenu.Show(screenPoint);
			}
			else if (sender is ToolStripDropDownButton dropDownButton)  //TODO: BUGFIX: 为下拉按钮添加右键菜单, 但是右键菜单不显示
			{
				currentDropDownButton = dropDownButton;
				//buttonContextMenu.Show(dynamicToolStrip.PointToScreen(new Point(e.X, e.Y)));
				// 获取鼠标的屏幕坐标
				Point screenPoint = Cursor.Position;
				buttonContextMenu.Show(screenPoint);
			}
		}
	}

	// 删除按钮
	private void DeleteButton_Click(object? sender, EventArgs e)
	{
		if (currentButton != null)
		{
			int index = dynamicToolStrip.Items.IndexOf(currentButton);
			if (index >= 0)
			{
				toolbarButtons.RemoveAt(index);
				SaveToconfig();
				GenerateDynamicToolbar();
			}
		}
	}
	private void EditButton_Click(object? sender, EventArgs e)
	{
		if (currentButton != null)
		{
			int index = dynamicToolStrip.Items.IndexOf(currentButton);
			if (index >= 0 && index < toolbarButtons.Count)
			{
				var button = toolbarButtons[index];
				EditButton(button.name, button.cmd, button.icon, button.path, button.param, button.iconic);
				//SaveToconfig();
				//GenerateDynamicToolbar();
			}
		}

	}
	// 复制按钮
	private void CopyButton_Click(object? sender, EventArgs e)
	{
		if (currentButton != null)
		{
			int index = dynamicToolStrip.Items.IndexOf(currentButton);
			if (index >= 0 && index < toolbarButtons.Count)
			{
				var button = toolbarButtons[index];
				AddButton(button.name, button.cmd, button.icon, button.path, button.param, button.iconic);
				SaveToconfig();
				GenerateDynamicToolbar();
			}
		}
	}
	public void Init(string path)
	{
		//load from config file
		string toolbarFilePath = path;// Path.Combine(Constants.ZfileCfgPath, path);
		if (!File.Exists(toolbarFilePath))
		{
			MessageBox.Show("工具栏配置文件不存在" + toolbarFilePath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		var zfile_path = Path.Combine(Constants.ZfileCfgPath, "WCMIcon2.dll");//bugfix: should use wcmicon2.dll which contains 317 icons
		//var iconManager = form.iconManager;
		var iconList = form.iconManager.LoadIconsFromFile(zfile_path, false);
		//var fileInfoList = new FileInfoList(new string[] { zfile_path });

		using (StreamReader reader = new StreamReader(toolbarFilePath, Encoding.GetEncoding("GB2312")))
		{
			// dynamicToolStrip = new ToolStrip();
			string? line;
			int buttonCount = 0;
			int buttonIndex;
			string buttonIcon = "";
			string cmd = "";
			string menuText = "";
			string pathText = "";
			string iconic = "";
			string paramText = "";
			List<int> emptybuttons = new List<int>();

			while ((line = reader.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.StartsWith("Buttoncount="))
				{
					buttonCount = int.Parse(line.Substring("Buttoncount=".Length));
					continue;
				}
				else if (line.StartsWith("iconic"))
				{
					var _buttonIndex = int.Parse(line.Substring(6, line.IndexOf('=') - 6));
					if (emptybuttons.Contains(_buttonIndex))  //如果emptybuttons中存在_buttonIndex，则跳过
						continue;
					iconic = line.Substring(line.IndexOf('=') + 1);
				}
				else if (line.StartsWith("cmd"))
				{
					int _buttonIndex = int.Parse(line.Substring(3, line.IndexOf('=') - 3));
					if (emptybuttons.Contains(_buttonIndex))
						continue;

					cmd = line.Substring(line.IndexOf('=') + 1);
				}
				else if (line.StartsWith("menu"))
				{
					int _buttonIndex = int.Parse(line.Substring(4, line.IndexOf('=') - 4));
					if (emptybuttons.Contains(_buttonIndex))
						continue;
					menuText = line.Substring(line.IndexOf('=') + 1);
				}
				else if (line.StartsWith("path"))
				{
					int _buttonIndex = int.Parse(line.Substring(4, line.IndexOf('=') - 4));
					if (emptybuttons.Contains(_buttonIndex))
						continue;
					pathText = line.Substring(line.IndexOf('=') + 1);
				}
				else if (line.StartsWith("param"))
				{
					int _buttonIndex = int.Parse(line.Substring(5, line.IndexOf('=') - 5));
					if (emptybuttons.Contains(_buttonIndex))
						continue;
					paramText = line.Substring(line.IndexOf('=') + 1);
				}
				else if (line.StartsWith("button"))
				{
					if (!cmd.Equals(""))
					{
						var zhdesc = form.cmdProcessor.cmdTable.GetByCmdName(cmd)?.ZhDesc ?? "";
						AddButton(menuText, cmd, buttonIcon, pathText, paramText, iconic);
						menuText = "";
						pathText = "";
						cmd = "";
						iconic = "";
						paramText = "";
					}

					buttonIndex = int.Parse(line.Substring(6, line.IndexOf('=') - 6));
					buttonIcon = line.Substring(line.IndexOf('=') + 1);
					//如果buttonIcon为空，则读取下一行，并记录当前buttonIndex,忽略下面所有编号为buttonIndex的行
					if (string.IsNullOrEmpty(buttonIcon))
					{
						emptybuttons.Add(buttonIndex);
						continue;
					}
				}
			}
		}
	}
}