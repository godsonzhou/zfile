using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Zfile.Operations;
namespace ZFile.FileSources.WcxArchive
{
    public class WcxArchiveCopyOperationOptionsUI : FileSourceOperationOptionsUI
    {
        private Button _btnConfig;
        private CheckBox _cbEncrypt;
        private ComboBox _cmbFileExists;
        private Label _lblFileExists;
        private Panel _pnlCheckboxes;
        private Panel _pnlComboBoxes;
        
        private IWcxArchiveFileSource _fileSource;

        public WcxArchiveCopyOperationOptionsUI(Component owner, IInterface fileSource) : base(owner, fileSource)
        {
            _fileSource = (IWcxArchiveFileSource)fileSource;
            InitializeComponent();
            
            ParseLineToList(ResourceStrings.FileOpCopyMoveFileExistsOptions, _cmbFileExists.Items);

            // Load default options.
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

        private void InitializeComponent()
        {
            _btnConfig = new Button();
            _cbEncrypt = new CheckBox();
            _cmbFileExists = new ComboBox();
            _lblFileExists = new Label();
            _pnlCheckboxes = new Panel();
            _pnlComboBoxes = new Panel();
            
            // btnConfig
            _btnConfig.Text = "Configure";
            _btnConfig.Click += BtnConfigClick;
            _btnConfig.Visible = false;
            
            // cbEncrypt
            _cbEncrypt.Text = "Encrypt";
            
            // lblFileExists
            _lblFileExists.Text = "When file exists:";
            
            // pnlCheckboxes
            _pnlCheckboxes.Controls.Add(_cbEncrypt);
            _pnlCheckboxes.Visible = false;
            
            // pnlComboBoxes
            _pnlComboBoxes.Controls.Add(_lblFileExists);
            _pnlComboBoxes.Controls.Add(_cmbFileExists);
            
            // Add controls to form
            Controls.Add(_pnlCheckboxes);
            Controls.Add(_pnlComboBoxes);
            Controls.Add(_btnConfig);
        }

        private void BtnConfigClick(object sender, EventArgs e)
        {
            try
            {
                _fileSource.WcxModule.VFSConfigure(Handle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetOperationOptions(WcxArchiveCopyInOperation copyInOperation)
        {
            int flags = copyInOperation.PackingFlags;
            if (_cbEncrypt.Checked) flags |= WcxModule.PK_PACK_ENCRYPT;
            copyInOperation.PackingFlags = flags;
        }

        public override void SaveOptions()
        {
            // TODO: Saving options for each file source operation separately.
        }

        public override void SetOperationOptions(object operation)
        {
            var copyOperation = operation as FileSourceCopyOperation;
            if (copyOperation != null)
            {
                switch (_cmbFileExists.SelectedIndex)
                {
                    case 0:
                        copyOperation.FileExistsOption = FileSourceOperationOptionFileExists.None;
                        break;
                    case 1:
                        copyOperation.FileExistsOption = FileSourceOperationOptionFileExists.Overwrite;
                        break;
                    case 2:
                        copyOperation.FileExistsOption = FileSourceOperationOptionFileExists.Skip;
                        break;
                }
            }
            
            if (operation is WcxArchiveCopyInOperation)
                SetOperationOptions((WcxArchiveCopyInOperation)operation);
        }
    }

    public class WcxArchiveCopyInOperationOptionsUI : WcxArchiveCopyOperationOptionsUI
    {
        public WcxArchiveCopyInOperationOptionsUI(Component owner, IInterface fileSource) : base(owner, fileSource)
        {
            _fileSource = (IWcxArchiveFileSource)fileSource;
            _pnlCheckboxes.Visible = true;
            _btnConfig.Visible = true;
        }
    }
}