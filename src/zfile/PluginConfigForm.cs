//public partial class PluginConfigForm : Form
//{
//    private WcxModuleList _wcxModules;
//    private DataGridView _gridView;
//    private BindingList<PluginConfigItem> _configItems;

//    public class PluginConfigItem
//    {
//        public string PluginName { get; set; }
//        public string Extension { get; set; }
//    }

//    public PluginConfigForm(WcxModuleList wcxModules)
//    {
//        InitializeComponent();
//        _wcxModules = wcxModules;
//        InitializeUI();
//        LoadPluginConfig();
//    }

//    private void InitializeUI()
//    {
//        // ����������
//        TableLayoutPanel mainLayout = new TableLayoutPanel
//        {
//            Dock = DockStyle.Fill,
//            ColumnCount = 2,
//            RowCount = 1
//        };
//        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
//        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));

//        // ��������������б�
//        ListBox pluginTypes = new ListBox
//        {
//            Dock = DockStyle.Fill
//        };
//        pluginTypes.Items.AddRange(new string[] { "WCX", "WDX", "WLX", "WFX" });
//        pluginTypes.SelectedIndexChanged += PluginTypes_SelectedIndexChanged;

//        // �����Ҳ��������
//        Panel configPanel = new Panel { Dock = DockStyle.Fill };
        
//        // ����DataGridView
//        _gridView = new DataGridView
//        {
//            Dock = DockStyle.Fill,
//            AllowUserToAddRows = false,
//            AllowDrop = true,
//            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
//        };
//        _gridView.Columns.Add("PluginName", "�������");
//        _gridView.Columns.Add("Extension", "�ļ���չ��");

//        // ������ť���
//        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
//        {
//            Dock = DockStyle.Bottom,
//            FlowDirection = FlowDirection.RightToLeft,
//            Height = 40
//        };

//        Button btnDelete = new Button { Text = "ɾ��", Width = 80 };
//        Button btnAdd = new Button { Text = "���", Width = 80 };
//        Button btnMoveUp = new Button { Text = "����", Width = 80 };
//        Button btnMoveDown = new Button { Text = "����", Width = 80 };

//        btnDelete.Click += BtnDelete_Click;
//        btnAdd.Click += BtnAdd_Click;
//        btnMoveUp.Click += BtnMoveUp_Click;
//        btnMoveDown.Click += BtnMoveDown_Click;

//        buttonPanel.Controls.AddRange(new Control[] { btnDelete, btnAdd, btnMoveDown, btnMoveUp });
        
//        configPanel.Controls.Add(_gridView);
//        configPanel.Controls.Add(buttonPanel);

//        mainLayout.Controls.Add(pluginTypes, 0, 0);
//        mainLayout.Controls.Add(configPanel, 1, 0);

//        this.Controls.Add(mainLayout);
//        this.Size = new Size(800, 600);
//    }

//    private void LoadPluginConfig()
//    {
//        _configItems = new BindingList<PluginConfigItem>();
//        foreach (var ext in _wcxModules._exts)
//        {
//            _configItems.Add(new PluginConfigItem 
//            { 
//                PluginName = ext.Value.Name,
//                Extension = ext.Key
//            });
//        }
//        _gridView.DataSource = _configItems;
//    }

//    private void PluginTypes_SelectedIndexChanged(object sender, EventArgs e)
//    {
//        // ����������ѡ����
//        ListBox lb = sender as ListBox;
//        if (lb.SelectedItem.ToString() == "WCX")
//        {
//            LoadPluginConfig();
//        }
//    }

//    private void BtnDelete_Click(object sender, EventArgs e)
//    {
//        if (_gridView.SelectedRows.Count > 0)
//        {
//            var row = _gridView.SelectedRows[0];
//            _configItems.RemoveAt(row.Index);
//        }
//    }

//    private void BtnAdd_Click(object sender, EventArgs e)
//    {
//        using (var addForm = new AddPluginConfigForm(_wcxModules))
//        {
//            if (addForm.ShowDialog() == DialogResult.OK)
//            {
//                _configItems.Add(new PluginConfigItem
//                {
//                    PluginName = addForm.SelectedPlugin,
//                    Extension = addForm.Extension
//                });
//            }
//        }
//    }

//    private void BtnMoveUp_Click(object sender, EventArgs e)
//    {
//        MoveSelectedRow(-1);
//    }

//    private void BtnMoveDown_Click(object sender, EventArgs e)
//    {
//        MoveSelectedRow(1);
//    }

//    private void MoveSelectedRow(int offset)
//    {
//        if (_gridView.SelectedRows.Count == 0) return;

//        int currentIndex = _gridView.SelectedRows[0].Index;
//        int newIndex = currentIndex + offset;

//        if (newIndex >= 0 && newIndex < _configItems.Count)
//        {
//            var item = _configItems[currentIndex];
//            _configItems.RemoveAt(currentIndex);
//            _configItems.Insert(newIndex, item);
//            _gridView.ClearSelection();
//            _gridView.Rows[newIndex].Selected = true;
//        }
//    }
//}

//// ��Ӳ�����õĶԻ���
//public class AddPluginConfigForm : Form
//{
//    private ComboBox _pluginCombo;
//    private TextBox _extensionBox;
//    public string SelectedPlugin => _pluginCombo.SelectedItem.ToString();
//    public string Extension => _extensionBox.Text;

//    public AddPluginConfigForm(WcxModuleList wcxModules)
//    {
//        this.Text = "��Ӳ������";
//        this.Size = new Size(300, 150);
//        this.FormBorderStyle = FormBorderStyle.FixedDialog;
//        this.MaximizeBox = false;
//        this.MinimizeBox = false;
//        this.StartPosition = FormStartPosition.CenterParent;

//        TableLayoutPanel layout = new TableLayoutPanel
//        {
//            Dock = DockStyle.Fill,
//            ColumnCount = 2,
//            RowCount = 3,
//            Padding = new Padding(10)
//        };

//        layout.Controls.Add(new Label { Text = "���:" }, 0, 0);
//        _pluginCombo = new ComboBox { Dock = DockStyle.Fill };
//        _pluginCombo.Items.AddRange(wcxModules._modules.Select(m => m.Name).ToArray());
//        layout.Controls.Add(_pluginCombo, 1, 0);

//        layout.Controls.Add(new Label { Text = "��չ��:" }, 0, 1);
//        _extensionBox = new TextBox { Dock = DockStyle.Fill };
//        layout.Controls.Add(_extensionBox, 1, 1);

//        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
//        {
//            Dock = DockStyle.Fill,
//            FlowDirection = FlowDirection.RightToLeft
//        };

//        Button btnCancel = new Button { Text = "ȡ��", DialogResult = DialogResult.Cancel };
//        Button btnOK = new Button { Text = "ȷ��", DialogResult = DialogResult.OK };
//        buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnOK });

//        layout.Controls.Add(buttonPanel, 1, 2);

//        this.Controls.Add(layout);
//    }
//}
