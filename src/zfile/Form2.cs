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

            int y = 10;
            foreach (var cmd in commandHotkeys)
            {
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
                    Location = new Point(150, y),
                    Width = 200,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                comboBox.Items.AddRange(Enum.GetNames(typeof(Keys)));
                comboBox.SelectedItem = cmd.Value.ToString();
                comboBox.SelectedIndexChanged += (sender, e) => UpdateHotkey(cmd.Key, comboBox);
                optionPanel.Controls.Add(comboBox);
                commandComboBoxes[cmd.Key] = comboBox;

                y += 30;
            }

            splitContainer1.Panel2.Controls.Add(optionPanel);
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

            splitContainer1.Panel2.Controls.Add(fontPanel);
            fontPanel.Visible = false; // 初始隐藏
        }

        private void FontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fontComboBox.SelectedItem != null)
            {
                string selectedFont = fontComboBox.SelectedItem.ToString();
                Font newFont = new Font(selectedFont, 10);
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
