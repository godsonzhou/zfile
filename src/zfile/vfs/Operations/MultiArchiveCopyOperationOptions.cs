namespace zfile
{
    public class MultiArchiveCopyOperationOptions : FileSourceOperationOptionsUI
    {
        private ComboBox _cmbFileExists;
        private GroupBox _grpOptions;
        private Label _lblFileExists;

        public MultiArchiveCopyOperationOptions(Control owner, IFileSource fileSource)
            : base(owner, fileSource)
        {
            InitializeComponent();
            LoadOptions();
        }

        private void InitializeComponent()
        {
            _grpOptions = new GroupBox();
            _lblFileExists = new Label();
            _cmbFileExists = new ComboBox();

            _grpOptions.SuspendLayout();
            SuspendLayout();

            // grpOptions
            _grpOptions.Controls.Add(_lblFileExists);
            _grpOptions.Controls.Add(_cmbFileExists);
            _grpOptions.Location = new System.Drawing.Point(12, 12);
            _grpOptions.Name = "grpOptions";
            _grpOptions.Size = new System.Drawing.Size(360, 100);
            _grpOptions.TabIndex = 0;
            _grpOptions.TabStop = false;
            _grpOptions.Text = "选项";

            // lblFileExists
            _lblFileExists.AutoSize = true;
            _lblFileExists.Location = new System.Drawing.Point(6, 26);
            _lblFileExists.Name = "lblFileExists";
            _lblFileExists.Size = new System.Drawing.Size(65, 12);
            _lblFileExists.TabIndex = 0;
            _lblFileExists.Text = "文件已存在:";

            // cmbFileExists
            _cmbFileExists.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFileExists.FormattingEnabled = true;
            _cmbFileExists.Location = new System.Drawing.Point(77, 23);
            _cmbFileExists.Name = "cmbFileExists";
            _cmbFileExists.Size = new System.Drawing.Size(277, 20);
            _cmbFileExists.TabIndex = 1;

            // MultiArchiveCopyOperationOptions
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_grpOptions);
            Name = "MultiArchiveCopyOperationOptions";
            Size = new System.Drawing.Size(384, 124);
            _grpOptions.ResumeLayout(false);
            _grpOptions.PerformLayout();
            ResumeLayout(false);
        }

        private void LoadOptions()
        {
            // 加载文件存在选项
            _cmbFileExists.Items.AddRange(new string[] {
                "询问",
                "覆盖",
                "跳过"
            });

            // 加载默认选项
            switch (GlobalSettings.OperationOptionFileExists)
            {
                case FileSourceOperationOptionFileExists.None:
                    _cmbFileExists.SelectedIndex = 0;
                    break;
                case FileSourceOperationOptionFileExists.Overwrite:
                    _cmbFileExists.SelectedIndex = 1;
                    break;
                case FileSourceOperationOptionFileExists.Skip:
                    _cmbFileExists.SelectedIndex = 2;
                    break;
            }
        }

        public override void SaveOptions()
        {
            // TODO: 为每个文件源操作单独保存选项
        }

        public override void SetOperationOptions(object operation)
        {
            if (operation is MultiArchiveCopyInOperation copyInOperation)
            {
                SetOperationOptions(copyInOperation);
            }
            else if (operation is MultiArchiveCopyOutOperation copyOutOperation)
            {
                SetOperationOptions(copyOutOperation);
            }
        }

        private void SetOperationOptions(MultiArchiveCopyInOperation operation)
        {
            switch (_cmbFileExists.SelectedIndex)
            {
                case 0:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.None;
                    break;
                case 1:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.Overwrite;
                    break;
                case 2:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.Skip;
                    break;
            }
        }

        private void SetOperationOptions(MultiArchiveCopyOutOperation operation)
        {
            switch (_cmbFileExists.SelectedIndex)
            {
                case 0:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.None;
                    break;
                case 1:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.Overwrite;
                    break;
                case 2:
                    operation.FileExistsOption = FileSourceOperationOptionFileExists.Skip;
                    break;
            }
        }
    }
} 