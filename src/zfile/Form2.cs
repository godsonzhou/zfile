using System.Data;

namespace WinFormsApp1
{
    public partial class OptionsForm : Form
    {
        public Dictionary<string, Keys> commandHotkeys;
        private Dictionary<string, Label> commandLabels;
        private Dictionary<string, ComboBox> commandComboBoxes;
        private Panel optionPanel;
        private Panel fontPanel;
        private ComboBox fontComboBox;
        private Form1 mainForm;

        public OptionsForm(Dictionary<string, Keys> commandHotkeys, Form1 mainForm)
        {
            InitializeComponent();
            this.commandHotkeys = commandHotkeys;
            this.mainForm = mainForm;
            InitializeOptionPanel();
            InitializeFontPanel();
            InitializeTreeView();
        }

        private void InitializeOptionPanel()
        {
            optionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            commandLabels = new Dictionary<string, Label>();
            commandComboBoxes = new Dictionary<string, ComboBox>();

			var cmdtable = mainForm.cmdProcessor.cmdTable;
			var keymapR = mainForm.keyManager.keymapReverse;
			foreach (var cmd in cmdtable.GetAll())
			{
				string keystr = "None";
				//将keymap的内容补充到commandhotkeys中
				if (keymapR.ContainsKey(cmd.CmdName))
				{
					keystr = keymapR[cmd.CmdName];  //C+8 代表 ctrl+8, S+A 代表 shift+A A+8 代表 ALT+8 AS+0 代表 ALT+SHIFT+0	
				}
				
				var keys = keystr.Split('+', StringSplitOptions.RemoveEmptyEntries);
				var mainkey = keys[^1];
				//在keymap.values中查找cmd.CmdName
				var k = ConvertStringToKey(mainkey);
				if (!commandHotkeys.ContainsKey(cmd.CmdName))
				{
					commandHotkeys[cmd.CmdName] = k;
				}
			}
			int y = 10;
            foreach (var cmd in commandHotkeys)
            {
				if(cmd.Value == Keys.None) continue;
				Label label = new Label
                {
                    Text = cmd.Key,
                    Location = new Point(10, y),
                    AutoSize = true
                };
                optionPanel.Controls.Add(label);
                commandLabels[cmd.Key] = label;

                ComboBox comboBox = new ComboBox
                {
                    Location = new Point(250, y),
                    Width = 80,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                comboBox.Items.AddRange(Enum.GetNames(typeof(Keys)));
                comboBox.SelectedItem = cmd.Value.ToString();
                comboBox.SelectedIndexChanged += (sender, e) => UpdateHotkey(cmd.Key, comboBox);
                optionPanel.Controls.Add(comboBox);
                commandComboBoxes[cmd.Key] = comboBox;

                y += 26;
            }
		
			splitContainer1.Panel2.Controls.Add(optionPanel);
        }
		
		private Keys ConvertStringToKey(string str)
		{
			//F1 -> keys.F1
			//None -> keys.None
			//A -> keys.A
			//ControlKey -> keys.ControlKey
			//1 -> keys.D1
			if (str == "None")
				return Keys.None;
			if (int.TryParse(str, out _))
				str = "D" + str;
			else if (str.StartsWith("NUM"))
				str = str.Replace("NUM", "NumPad");
			else if (str.ToUpper().Equals("OEM_US`~"))
				str = "Oemtilde";
			else if (str.ToUpper().Equals("OEM_"))
				str = "Oemplus";
			else if (str.Equals("*"))
				str = "Multiply";
			else if (str.Equals("/"))
				str = "Divide";
			else if (str.Equals(","))
				str = "Oemcomma";
			else if (str.Equals("."))
				str = "OemPeriod";
			else if (str.Equals("-"))
				str = "OemMinus";
			//else if (str.Equals("+"))		// + is impossible, because of the seperator is +
			//	str = "Add";
			else if (str.Equals("["))
				str = "OemOpenBrackets";
			else if (str.Equals("]"))
				str = "OemCloseBrackets";
			else if (str.Equals("\\"))
				str = "OemPipe";
			else if (str.Equals(";"))
				str = "OemSemicolon";
			else if (str.Equals("'"))
				str = "OemQuotes";
			else if (str.Equals("="))
				str = "Oemplus";
			else if (str.Equals("`"))
				str = "Oemtilde";
			else if (str.Equals("\\"))
				str = "OemPipe";
			else if (str.Equals("ESC"))
				str = "Escape";
			else if (str.Equals("Oem_us/?"))
				str = "OemQuestion";
			else
			{
				//all is letter, use camel case
				str = str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower();
			}
			
			try
			{
				return (Keys)Enum.Parse(typeof(Keys), str);
			} catch { return Keys.None; }
		}
        private void InitializeFontPanel()
        {
            fontPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            Label fontLabel = new Label
            {
                Text = "字体设置",
                Location = new Point(10, 10),
                AutoSize = true
            };
            fontPanel.Controls.Add(fontLabel);

            fontComboBox = new ComboBox
            {
                Location = new Point(10, 40),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            fontComboBox.Items.AddRange(FontFamily.Families.Select(f => f.Name).ToArray());
            fontComboBox.SelectedIndexChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontComboBox);

            Label fontSizeLabel = new Label
            {
                Text = "字体大小",
                Location = new Point(10, 80),
                AutoSize = true
            };
            fontPanel.Controls.Add(fontSizeLabel);

            NumericUpDown fontSizeNumeric = new NumericUpDown
            {
                Location = new Point(10, 110),
                Width = 100,
                Minimum = 6,
                Maximum = 72,
                Value = 10
            };
            fontSizeNumeric.ValueChanged += FontComboBox_SelectedIndexChanged;
            fontPanel.Controls.Add(fontSizeNumeric);

            splitContainer1.Panel2.Controls.Add(fontPanel);
            fontPanel.Visible = false; // 初始隐藏
        }

        private void FontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fontComboBox.SelectedItem != null)
            {
                string selectedFont = fontComboBox.SelectedItem.ToString();
                float fontSize = (float)(fontPanel.Controls.OfType<NumericUpDown>().FirstOrDefault()?.Value ?? 10);
                Font newFont = new Font(selectedFont, fontSize);
                ApplyFontToControls(this, newFont);
                ApplyFontToControls(mainForm, newFont);
            }
        }

        private void ApplyFontToControls(Control control, Font font)
        {
            control.Font = font;
            foreach (Control child in control.Controls)
            {
                ApplyFontToControls(child, font);
            }
        }

        private void InitializeTreeView()
        {
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                ShowLines = true,
                ShowRootLines = true,
                ShowPlusMinus = true
            };

            TreeNode rootNode = new TreeNode("设置");
            TreeNode hotkeyNode = new TreeNode("快捷键设置");
            TreeNode fontNode = new TreeNode("字体设置");

            rootNode.Nodes.Add(hotkeyNode);
            rootNode.Nodes.Add(fontNode);
            treeView.Nodes.Add(rootNode);

            treeView.AfterSelect += TreeView_AfterSelect;

            splitContainer1.Panel1.Controls.Clear();
            splitContainer1.Panel1.Controls.Add(treeView);
			rootNode.Expand();
            treeView.BringToFront();
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                if (e.Node.Text == "快捷键设置")
                {
                    optionPanel.Visible = true;
                    fontPanel.Visible = false;
                }
                else if (e.Node.Text == "字体设置")
                {
                    optionPanel.Visible = false;
                    fontPanel.Visible = true;
                }
            }
        }

        private void UpdateHotkey(string cmdName, ComboBox comboBox)
        {
            if (comboBox.SelectedItem != null && Enum.TryParse(comboBox.SelectedItem.ToString(), out Keys newKey))
            {
                commandHotkeys[cmdName] = newKey;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 关闭此表单
            this.Close();
        }

        private void Okbutton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 其他代码...
    }
}
