using System.ComponentModel;
using zfile.Dialogs;

namespace zfile
{

    public class FileSystemCopyMoveOperationOptionsUI : FileSourceOperationOptionsUI
    {
        private Button btnSearchTemplate;
        private CheckBox cbCheckFreeSpace;
        private CheckBox cbCorrectLinks;
        private CheckBox cbDropReadOnlyFlag;
        private CheckBox cbFollowLinks;
        private CheckBox cbCopyAttributes;
        private CheckBox cbCopyTime;
        private CheckBox cbCopyOwnership;
        private CheckBox cbExcludeEmptyDirectories;
        private CheckBox cbReserveSpace;
        private CheckBox cbCopyPermissions;
        private CheckBox chkCopyOnWrite;
        private CheckBox chkVerify;
        private ComboBox cmbDirectoryExists;
        private ComboBox cmbFileExists;
        private ComboBox cmbSetPropertyError;
        private GroupBox gbFileTemplate;
        private GroupBox grpOptions;
        private Label lblSetPropertyError;
        private Label lblTemplateName;
        private Label lblDirectoryExists;
        private Label lblFileExists;
        private Panel pnlComboBoxes;
        private Panel pnlCheckboxes;

        private SearchTemplate template;

        public FileSystemCopyMoveOperationOptionsUI(Control owner, IFileSource fileSource)
            : base(owner, fileSource)
        {
            InitializeComponents();
            LoadDefaultOptions();
        }

        private void InitializeComponents()
        {
            // 初始化所有控件
            btnSearchTemplate = new Button();
            cbCheckFreeSpace = new CheckBox();
            cbCorrectLinks = new CheckBox();
            cbDropReadOnlyFlag = new CheckBox();
            cbFollowLinks = new CheckBox();
            cbCopyAttributes = new CheckBox();
            cbCopyTime = new CheckBox();
            cbCopyOwnership = new CheckBox();
            cbExcludeEmptyDirectories = new CheckBox();
            cbReserveSpace = new CheckBox();
            cbCopyPermissions = new CheckBox();
            chkCopyOnWrite = new CheckBox();
            chkVerify = new CheckBox();
            cmbDirectoryExists = new ComboBox();
            cmbFileExists = new ComboBox();
            cmbSetPropertyError = new ComboBox();
            gbFileTemplate = new GroupBox();
            grpOptions = new GroupBox();
            lblSetPropertyError = new Label();
            lblTemplateName = new Label();
            lblDirectoryExists = new Label();
            lblFileExists = new Label();
            pnlComboBoxes = new Panel();
            pnlCheckboxes = new Panel();

            // 设置控件属性
            btnSearchTemplate.Text = "搜索模板";
            btnSearchTemplate.Click += BtnSearchTemplate_Click;

            cbCopyAttributes.CheckedChanged += CbCopyAttributes_CheckedChanged;
            cbReserveSpace.CheckedChanged += CbReserveSpace_CheckedChanged;

            // 添加控件到面板
            pnlCheckboxes.Controls.AddRange(new Control[] {
                cbCheckFreeSpace, cbCorrectLinks, cbDropReadOnlyFlag,
                cbFollowLinks, cbCopyAttributes, cbCopyTime,
                cbCopyOwnership, cbExcludeEmptyDirectories, cbReserveSpace,
                cbCopyPermissions, chkCopyOnWrite, chkVerify
            });

            pnlComboBoxes.Controls.AddRange(new Control[] {
                cmbDirectoryExists, cmbFileExists, cmbSetPropertyError,
                lblSetPropertyError, lblTemplateName, lblDirectoryExists,
                lblFileExists
            });

            // 设置布局
            this.Controls.AddRange(new Control[] { pnlComboBoxes, pnlCheckboxes });
        }

        private void LoadDefaultOptions()
        {
            // 加载默认选项
            cmbFileExists.Items.AddRange(new string[] {
                "None",
                "Overwrite",
                "Overwrite Older",
                "Skip"
            });

            cmbDirectoryExists.Items.AddRange(new string[] {
                "None",
                "Copy Into",
                "Skip"
            });

            cmbSetPropertyError.Items.AddRange(new string[] {
                "None",
                "Don't Set",
                "Ignore Errors"
            });

            // 设置默认值
            cmbFileExists.SelectedIndex = 0;
            cmbDirectoryExists.SelectedIndex = 0;
            cmbSetPropertyError.SelectedIndex = 0;

            cbCopyAttributes.Checked = true;
            cbCopyTime.Checked = true;
            cbCopyOwnership.Checked = true;
            cbCopyPermissions.Checked = true;
            cbDropReadOnlyFlag.Checked = true;
        }

        private void BtnSearchTemplate_Click(object sender, EventArgs e)
        {
            // 实现搜索模板功能
            if (template == null)
                template = new SearchTemplate("*");

            if (TemplateDialogs.ShowUseTemplateDialog(template) && template != null)
            {
                lblTemplateName.Text = string.IsNullOrEmpty(template.TemplateName)
                    ? "未命名模板"
                    : template.TemplateName;
            }
        }

        private void CbCopyAttributes_CheckedChanged(object sender, EventArgs e)
        {
            cbDropReadOnlyFlag.Enabled = cbCopyAttributes.Checked;
        }

        private void CbReserveSpace_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbReserveSpace.Checked)
            {
                cbCheckFreeSpace.Checked = (bool)cbCheckFreeSpace.Tag;
            }
            else
            {
                cbCheckFreeSpace.Tag = cbCheckFreeSpace.Checked;
                cbCheckFreeSpace.Checked = cbReserveSpace.Checked;
            }
            cbCheckFreeSpace.Enabled = !cbReserveSpace.Checked;
        }

        public override void SaveOptions()
        {
            // 保存选项到全局设置
            GlobalSettings.OperationOptionFileExists = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
            GlobalSettings.OperationOptionDirectoryExists = (FileSourceOperationOptionDirectoryExists)cmbDirectoryExists.SelectedIndex;
            GlobalSettings.OperationOptionSetPropertyError = (FileSourceOperationOptionSetPropertyError)cmbSetPropertyError.SelectedIndex;
            GlobalSettings.OperationOptionCopyOnWrite = (FileSourceOperationOptionGeneral)chkCopyOnWrite.CheckState;
            GlobalSettings.OperationOptionVerify = chkVerify.Checked;
            GlobalSettings.OperationOptionCopyAttributes = cbCopyAttributes.Checked;
            GlobalSettings.OperationOptionCopyTime = cbCopyTime.Checked;
            GlobalSettings.OperationOptionCopyOwnership = cbCopyOwnership.Checked;
            GlobalSettings.OperationOptionCopyPermissions = cbCopyPermissions.Checked;
            GlobalSettings.DropReadOnlyFlag = cbDropReadOnlyFlag.Checked;
            GlobalSettings.OperationOptionSymLinks = (FileSourceOperationSymLinkOption)cbFollowLinks.CheckState;
            GlobalSettings.OperationOptionCorrectLinks = cbCorrectLinks.Checked;
            GlobalSettings.OperationOptionReserveSpace = cbReserveSpace.Checked;
            GlobalSettings.OperationOptionCheckFreeSpace = cbCheckFreeSpace.Checked;
            GlobalSettings.OperationOptionExcludeEmptyDirectories = cbExcludeEmptyDirectories.Checked;
        }

        public override void SetOperationOptions(object operation)
        {
            if (operation is FileSystemCopyOperation copyOperation)
            {
                SetOperationOptions(copyOperation);
            }
            else if (operation is FileSystemMoveOperation moveOperation)
            {
                SetOperationOptions(moveOperation);
            }
        }

        private void SetOperationOptions(FileSystemCopyOperation operation)
        {
            operation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
            operation.DirExistsOption = (FileSourceOperationOptionDirectoryExists)cmbDirectoryExists.SelectedIndex;
            operation.SetPropertyErrorOption = (FileSourceOperationOptionSetPropertyError)cmbSetPropertyError.SelectedIndex;
            if (cbCopyAttributes.Checked)
                operation.CopyAttributesOptions |= CopyAttributesOption.CopyAttributes;
            else
                operation.CopyAttributesOptions &= ~CopyAttributesOption.CopyAttributes;
            operation.CopyTime = cbCopyTime.Checked;
            operation.CopyOwnership = cbCopyOwnership.Checked;
            operation.CopyPermissions = cbCopyPermissions.Checked;
            operation.DropReadOnlyFlag = cbDropReadOnlyFlag.Checked;
            operation.FollowLinks = cbFollowLinks.Checked;
            operation.CorrectLinks = cbCorrectLinks.Checked;
            operation.ReserveSpace = cbReserveSpace.Checked;
            operation.CheckFreeSpace = cbCheckFreeSpace.Checked;
            operation.ExcludeEmptyDirectories = cbExcludeEmptyDirectories.Checked;
            operation.Verify = chkVerify.Checked;
            operation.CopyOnWrite = chkCopyOnWrite.Checked ? FileSourceOperationOptionGeneral.Yes : FileSourceOperationOptionGeneral.No;
        }

        private void SetOperationOptions(FileSystemMoveOperation operation)
        {
            // 设置移动操作选项
            operation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
            operation.DirExistsOption = (FileSourceOperationOptionDirectoryExists)cmbDirectoryExists.SelectedIndex;
            operation.SetPropertyErrorOption = (FileSourceOperationOptionSetPropertyError)cmbSetPropertyError.SelectedIndex;
            if (cbCopyAttributes.Checked)
                operation.CopyAttributesOptions |= CopyAttributesOption.CopyAttributes;
            else
                operation.CopyAttributesOptions &= ~CopyAttributesOption.CopyAttributes;
            operation.CopyTime = cbCopyTime.Checked;
            operation.CopyOwnership = cbCopyOwnership.Checked;
            operation.CopyPermissions = cbCopyPermissions.Checked;
            operation.DropReadOnlyFlag = cbDropReadOnlyFlag.Checked;
            operation.FollowLinks = cbFollowLinks.Checked;
            operation.CorrectLinks = cbCorrectLinks.Checked;
            operation.ReserveSpace = cbReserveSpace.Checked;
            operation.CheckFreeSpace = cbCheckFreeSpace.Checked;
            operation.ExcludeEmptyDirectories = cbExcludeEmptyDirectories.Checked;
            operation.Verify = chkVerify.Checked;
        }
    }

    public class FileSystemCopyOperationOptionsUI : FileSystemCopyMoveOperationOptionsUI
    {
        public FileSystemCopyOperationOptionsUI(Control owner, IFileSource fileSource)
            : base(owner, fileSource)
        {
        }
    }

    public class FileSystemMoveOperationOptionsUI : FileSystemCopyMoveOperationOptionsUI
    {
        public FileSystemMoveOperationOptionsUI(Control owner, IFileSource fileSource) : base(owner, fileSource)
        {
        }
    }

    // 枚举定义

}