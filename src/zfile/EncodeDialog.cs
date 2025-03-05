using System.ComponentModel;
public class EncodeDialog : Form
{
    private ComboBox cboEncodeType;
    private TextBox txtTargetPath;
    private Button btnBrowse;
    private TextBox txtFileSize;
    private TextBox txtLineCount;
    private Button btnOK;
    private Button btnCancel;

    public string SelectedEncoding { get; private set; }
    public string TargetPath { get; private set; }
    public int FileSize { get; private set; }
    public int LineCount { get; private set; }

    public EncodeDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "文件编码";
        this.Size = new Size(400, 250);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var lblEncodeType = new Label { Text = "编码格式:", Location = new Point(10, 15) };
        cboEncodeType = new ComboBox
        {
            Location = new Point(100, 12),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboEncodeType.Items.AddRange(new string[] { "MIME (Base64)", "UUEncode", "XXEncode" });
        cboEncodeType.SelectedIndex = 0;

        var lblTargetPath = new Label { Text = "目标路径:", Location = new Point(10, 45) };
        txtTargetPath = new TextBox { Location = new Point(100, 42), Width = 200 };
        btnBrowse = new Button { Text = "浏览...", Location = new Point(310, 41), Width = 60 };
        btnBrowse.Click += BtnBrowse_Click;

        var lblFileSize = new Label { Text = "单个文件大小(字节):", Location = new Point(10, 75) };
        txtFileSize = new TextBox { Location = new Point(100, 72), Width = 200 };

        var lblLineCount = new Label { Text = "单个文件行数:", Location = new Point(10, 105) };
        txtLineCount = new TextBox { Location = new Point(100, 102), Width = 200 };

        btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(100, 170) };
        btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(200, 170) };

        this.Controls.AddRange(new Control[] { 
            lblEncodeType, cboEncodeType,
            lblTargetPath, txtTargetPath, btnBrowse,
            lblFileSize, txtFileSize,
            lblLineCount, txtLineCount,
            btnOK, btnCancel
        });

        this.AcceptButton = btnOK;
        this.CancelButton = btnCancel;
    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtTargetPath.Text = dialog.SelectedPath;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            SelectedEncoding = cboEncodeType.SelectedItem.ToString();
            TargetPath = txtTargetPath.Text;
            FileSize = string.IsNullOrEmpty(txtFileSize.Text) ? 0 : int.Parse(txtFileSize.Text);
            LineCount = string.IsNullOrEmpty(txtLineCount.Text) ? 0 : int.Parse(txtLineCount.Text);
        }
        base.OnClosing(e);
    }
}
