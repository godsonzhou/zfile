using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace zfile
{
    /// <summary>
    /// 文件属性设置窗口
    /// </summary>
    public class SetFilePropertiesForm : Form
    {
        private GroupBox gbTimestamp;
        private GroupBox gbWinAttributes;
        private CheckBox chkCreationTime;
        private CheckBox chkLastWriteTime;
        private CheckBox chkLastAccessTime;
        private DateTimePicker dtpCreationTime;
        private DateTimePicker dtpLastWriteTime;
        private DateTimePicker dtpLastAccessTime;
        private Button btnCreationTimeNow;
        private Button btnLastWriteTimeNow;
        private Button btnLastAccessTimeNow;
        private CheckBox chkArchive;
        private CheckBox chkReadOnly;
        private CheckBox chkHidden;
        private CheckBox chkSystem;
        private CheckBox chkRecursive;
        private Button btnOK;
        private Button btnCancel;

        private FileSourceSetFilePropertyOperation _operation;
        private bool _changeTriggersEnabled;

        /// <summary>
        /// 创建文件属性设置窗口
        /// </summary>
        /// <param name="operation">文件属性设置操作</param>
        public SetFilePropertiesForm(FileSourceSetFilePropertyOperation operation)
        {
            _operation = operation;
            _changeTriggersEnabled = true;
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.gbTimestamp = new GroupBox();
            this.chkCreationTime = new CheckBox();
            this.chkLastWriteTime = new CheckBox();
            this.chkLastAccessTime = new CheckBox();
            this.dtpCreationTime = new DateTimePicker();
            this.dtpLastWriteTime = new DateTimePicker();
            this.dtpLastAccessTime = new DateTimePicker();
            this.btnCreationTimeNow = new Button();
            this.btnLastWriteTimeNow = new Button();
            this.btnLastAccessTimeNow = new Button();
            this.gbWinAttributes = new GroupBox();
            this.chkArchive = new CheckBox();
            this.chkReadOnly = new CheckBox();
            this.chkHidden = new CheckBox();
            this.chkSystem = new CheckBox();
            this.chkRecursive = new CheckBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();

            this.gbTimestamp.SuspendLayout();
            this.gbWinAttributes.SuspendLayout();
            this.SuspendLayout();

            // gbTimestamp
            this.gbTimestamp.Controls.Add(this.chkCreationTime);
            this.gbTimestamp.Controls.Add(this.chkLastWriteTime);
            this.gbTimestamp.Controls.Add(this.chkLastAccessTime);
            this.gbTimestamp.Controls.Add(this.dtpCreationTime);
            this.gbTimestamp.Controls.Add(this.dtpLastWriteTime);
            this.gbTimestamp.Controls.Add(this.dtpLastAccessTime);
            this.gbTimestamp.Controls.Add(this.btnCreationTimeNow);
            this.gbTimestamp.Controls.Add(this.btnLastWriteTimeNow);
            this.gbTimestamp.Controls.Add(this.btnLastAccessTimeNow);
            this.gbTimestamp.Location = new Point(12, 12);
            this.gbTimestamp.Name = "gbTimestamp";
            this.gbTimestamp.Size = new Size(400, 120);
            this.gbTimestamp.TabIndex = 0;
            this.gbTimestamp.TabStop = false;
            this.gbTimestamp.Text = "时间戳记录性";

            // chkCreationTime
            this.chkCreationTime.AutoSize = true;
            this.chkCreationTime.Location = new Point(20, 25);
            this.chkCreationTime.Name = "chkCreationTime";
            this.chkCreationTime.Size = new Size(80, 17);
            this.chkCreationTime.TabIndex = 0;
            this.chkCreationTime.Text = "创建时间:";
            this.chkCreationTime.UseVisualStyleBackColor = true;
            this.chkCreationTime.CheckedChanged += new EventHandler(this.chkCreationTime_CheckedChanged);

            // chkLastWriteTime
            this.chkLastWriteTime.AutoSize = true;
            this.chkLastWriteTime.Location = new Point(20, 55);
            this.chkLastWriteTime.Name = "chkLastWriteTime";
            this.chkLastWriteTime.Size = new Size(80, 17);
            this.chkLastWriteTime.TabIndex = 3;
            this.chkLastWriteTime.Text = "修改时间:";
            this.chkLastWriteTime.UseVisualStyleBackColor = true;
            this.chkLastWriteTime.CheckedChanged += new EventHandler(this.chkLastWriteTime_CheckedChanged);

            // chkLastAccessTime
            this.chkLastAccessTime.AutoSize = true;
            this.chkLastAccessTime.Location = new Point(20, 85);
            this.chkLastAccessTime.Name = "chkLastAccessTime";
            this.chkLastAccessTime.Size = new Size(80, 17);
            this.chkLastAccessTime.TabIndex = 6;
            this.chkLastAccessTime.Text = "访问时间:";
            this.chkLastAccessTime.UseVisualStyleBackColor = true;
            this.chkLastAccessTime.CheckedChanged += new EventHandler(this.chkLastAccessTime_CheckedChanged);

            // dtpCreationTime
            this.dtpCreationTime.CustomFormat = "yyyy/MM/dd  HH:mm:ss.fff";
            this.dtpCreationTime.Format = DateTimePickerFormat.Custom;
            this.dtpCreationTime.Location = new Point(110, 23);
            this.dtpCreationTime.Name = "dtpCreationTime";
            this.dtpCreationTime.Size = new Size(200, 20);
            this.dtpCreationTime.TabIndex = 1;
            this.dtpCreationTime.ValueChanged += new EventHandler(this.dtpCreationTime_ValueChanged);
            this.dtpCreationTime.Enabled = false;

            // dtpLastWriteTime
            this.dtpLastWriteTime.CustomFormat = "yyyy/MM/dd  HH:mm:ss.fff";
            this.dtpLastWriteTime.Format = DateTimePickerFormat.Custom;
            this.dtpLastWriteTime.Location = new Point(110, 53);
            this.dtpLastWriteTime.Name = "dtpLastWriteTime";
            this.dtpLastWriteTime.Size = new Size(200, 20);
            this.dtpLastWriteTime.TabIndex = 4;
            this.dtpLastWriteTime.ValueChanged += new EventHandler(this.dtpLastWriteTime_ValueChanged);
            this.dtpLastWriteTime.Enabled = false;

            // dtpLastAccessTime
            this.dtpLastAccessTime.CustomFormat = "yyyy/MM/dd  HH:mm:ss.fff";
            this.dtpLastAccessTime.Format = DateTimePickerFormat.Custom;
            this.dtpLastAccessTime.Location = new Point(110, 83);
            this.dtpLastAccessTime.Name = "dtpLastAccessTime";
            this.dtpLastAccessTime.Size = new Size(200, 20);
            this.dtpLastAccessTime.TabIndex = 7;
            this.dtpLastAccessTime.ValueChanged += new EventHandler(this.dtpLastAccessTime_ValueChanged);
            this.dtpLastAccessTime.Enabled = false;

            // btnCreationTimeNow
            this.btnCreationTimeNow.Location = new Point(320, 22);
            this.btnCreationTimeNow.Name = "btnCreationTimeNow";
            this.btnCreationTimeNow.Size = new Size(60, 23);
            this.btnCreationTimeNow.TabIndex = 2;
            this.btnCreationTimeNow.Text = "现在";
            this.btnCreationTimeNow.UseVisualStyleBackColor = true;
            this.btnCreationTimeNow.Click += new EventHandler(this.btnCreationTimeNow_Click);
            this.btnCreationTimeNow.Enabled = false;

            // btnLastWriteTimeNow
            this.btnLastWriteTimeNow.Location = new Point(320, 52);
            this.btnLastWriteTimeNow.Name = "btnLastWriteTimeNow";
            this.btnLastWriteTimeNow.Size = new Size(60, 23);
            this.btnLastWriteTimeNow.TabIndex = 5;
            this.btnLastWriteTimeNow.Text = "现在";
            this.btnLastWriteTimeNow.UseVisualStyleBackColor = true;
            this.btnLastWriteTimeNow.Click += new EventHandler(this.btnLastWriteTimeNow_Click);
            this.btnLastWriteTimeNow.Enabled = false;

            // btnLastAccessTimeNow
            this.btnLastAccessTimeNow.Location = new Point(320, 82);
            this.btnLastAccessTimeNow.Name = "btnLastAccessTimeNow";
            this.btnLastAccessTimeNow.Size = new Size(60, 23);
            this.btnLastAccessTimeNow.TabIndex = 8;
            this.btnLastAccessTimeNow.Text = "现在";
            this.btnLastAccessTimeNow.UseVisualStyleBackColor = true;
            this.btnLastAccessTimeNow.Click += new EventHandler(this.btnLastAccessTimeNow_Click);
            this.btnLastAccessTimeNow.Enabled = false;

            // gbWinAttributes
            this.gbWinAttributes.Controls.Add(this.chkArchive);
            this.gbWinAttributes.Controls.Add(this.chkReadOnly);
            this.gbWinAttributes.Controls.Add(this.chkHidden);
            this.gbWinAttributes.Controls.Add(this.chkSystem);
            this.gbWinAttributes.Location = new Point(12, 138);
            this.gbWinAttributes.Name = "gbWinAttributes";
            this.gbWinAttributes.Size = new Size(400, 80);
            this.gbWinAttributes.TabIndex = 1;
            this.gbWinAttributes.TabStop = false;
            this.gbWinAttributes.Text = "属性";

            // chkArchive
            this.chkArchive.AutoSize = true;
            this.chkArchive.Location = new Point(20, 25);
            this.chkArchive.Name = "chkArchive";
            this.chkArchive.Size = new Size(74, 17);
            this.chkArchive.TabIndex = 0;
            this.chkArchive.Text = "存档";
            this.chkArchive.UseVisualStyleBackColor = true;
            this.chkArchive.ThreeState = true;

            // chkReadOnly
            this.chkReadOnly.AutoSize = true;
            this.chkReadOnly.Location = new Point(20, 50);
            this.chkReadOnly.Name = "chkReadOnly";
            this.chkReadOnly.Size = new Size(74, 17);
            this.chkReadOnly.TabIndex = 1;
            this.chkReadOnly.Text = "只读";
            this.chkReadOnly.UseVisualStyleBackColor = true;
            this.chkReadOnly.ThreeState = true;

            // chkHidden
            this.chkHidden.AutoSize = true;
            this.chkHidden.Location = new Point(120, 25);
            this.chkHidden.Name = "chkHidden";
            this.chkHidden.Size = new Size(74, 17);
            this.chkHidden.TabIndex = 2;
            this.chkHidden.Text = "隐藏";
            this.chkHidden.UseVisualStyleBackColor = true;
            this.chkHidden.ThreeState = true;

            // chkSystem
            this.chkSystem.AutoSize = true;
            this.chkSystem.Location = new Point(120, 50);
            this.chkSystem.Name = "chkSystem";
            this.chkSystem.Size = new Size(74, 17);
            this.chkSystem.TabIndex = 3;
            this.chkSystem.Text = "系统";
            this.chkSystem.UseVisualStyleBackColor = true;
            this.chkSystem.ThreeState = true;

            // chkRecursive
            this.chkRecursive.AutoSize = true;
            this.chkRecursive.Location = new Point(32, 230);
            this.chkRecursive.Name = "chkRecursive";
            this.chkRecursive.Size = new Size(150, 17);
            this.chkRecursive.TabIndex = 2;
            this.chkRecursive.Text = "包括子文件夹";
            this.chkRecursive.UseVisualStyleBackColor = true;

            // btnOK
            this.btnOK.Location = new Point(247, 230);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "确定(O)";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(337, 230);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "取消(C)";
            this.btnCancel.UseVisualStyleBackColor = true;

            // SetFilePropertiesForm
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(424, 265);
            this.Controls.Add(this.gbTimestamp);
            this.Controls.Add(this.gbWinAttributes);
            this.Controls.Add(this.chkRecursive);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetFilePropertiesForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "更改属性";

            this.gbTimestamp.ResumeLayout(false);
            this.gbTimestamp.PerformLayout();
            this.gbWinAttributes.ResumeLayout(false);
            this.gbWinAttributes.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeForm()
        {
            // 设置日期时间选择器的初始值
            dtpCreationTime.Value = DateTime.Now;
            dtpLastWriteTime.Value = DateTime.Now;
            dtpLastAccessTime.Value = DateTime.Now;

            // 启用仅支持的文件属性
            with_Operation();
        }

        private void with_Operation()
        {
            // 启用仅支持的文件属性
            if (_operation.SupportedProperties.HasFlag(FilePropertiesTypes.Attributes))
            {
                UpdateAllowGrayed(_operation.TargetFiles.Count > 1 || _operation.TargetFiles[0].IsDirectory);

                if (_operation.NewProperties[FilePropertiesTypes.Attributes] is NtfsFileAttributesProperty)
                {
                    if (_operation.TargetFiles.Count == 1)
                    {
                        ShowAttr((_operation.NewProperties[FilePropertiesTypes.Attributes] as NtfsFileAttributesProperty).Value);
                    }
                    gbWinAttributes.Visible = true;
                }
            }
            else
            {
                gbWinAttributes.Visible = false;
            }

            if (_operation.SupportedProperties.HasFlag(FilePropertiesTypes.CreationTime) && _operation.NewProperties[FilePropertiesTypes.CreationTime] != null)
            {
                dtpCreationTime.Value = (_operation.NewProperties[FilePropertiesTypes.CreationTime] as FileCreationDateTimeProperty).Value;
                dtpCreationTime.Enabled = true;
                chkCreationTime.Enabled = true;
                btnCreationTimeNow.Enabled = true;
            }

            if (_operation.SupportedProperties.HasFlag(FilePropertiesTypes.ModificationTime) && _operation.NewProperties[FilePropertiesTypes.ModificationTime] != null)
            {
                dtpLastWriteTime.Value = (_operation.NewProperties[FilePropertiesTypes.ModificationTime] as FileModificationDateTimeProperty).Value;
                dtpLastWriteTime.Enabled = true;
                chkLastWriteTime.Enabled = true;
                btnLastWriteTimeNow.Enabled = true;
            }

            if (_operation.SupportedProperties.HasFlag(FilePropertiesTypes.LastAccessTime) && _operation.NewProperties[FilePropertiesTypes.LastAccessTime] != null)
            {
                dtpLastAccessTime.Value = (_operation.NewProperties[FilePropertiesTypes.LastAccessTime] as FileLastAccessDateTimeProperty).Value;
                dtpLastAccessTime.Enabled = true;
                chkLastAccessTime.Enabled = true;
                btnLastAccessTimeNow.Enabled = true;
            }

            // 默认不选中时间复选框
            chkCreationTime.Checked = false;
            chkLastWriteTime.Checked = false;
            chkLastAccessTime.Checked = false;
        }

        private void UpdateAllowGrayed(bool allowGrayed)
        {
            foreach (Control control in gbWinAttributes.Controls)
            {
                if (control is CheckBox checkBox)
                {
                    checkBox.ThreeState = allowGrayed;
                }
            }
        }

        private void ShowAttr(FileAttributes attr)
        {
            chkArchive.Checked = (attr & FileAttributes.Archive) != 0;
            chkReadOnly.Checked = (attr & FileAttributes.ReadOnly) != 0;
            chkHidden.Checked = (attr & FileAttributes.Hidden) != 0;
            chkSystem.Checked = (attr & FileAttributes.System) != 0;
        }

        private FileAttributes GetAttrFromForm(out FileAttributes excludeAttrs)
        {
            FileAttributes result = 0;
            excludeAttrs = 0;

            switch (chkArchive.CheckState)
            {
                case CheckState.Checked:
                    result |= FileAttributes.Archive;
                    break;
                case CheckState.Unchecked:
                    excludeAttrs |= FileAttributes.Archive;
                    break;
            }

            switch (chkReadOnly.CheckState)
            {
                case CheckState.Checked:
                    result |= FileAttributes.ReadOnly;
                    break;
                case CheckState.Unchecked:
                    excludeAttrs |= FileAttributes.ReadOnly;
                    break;
            }

            switch (chkHidden.CheckState)
            {
                case CheckState.Checked:
                    result |= FileAttributes.Hidden;
                    break;
                case CheckState.Unchecked:
                    excludeAttrs |= FileAttributes.Hidden;
                    break;
            }

            switch (chkSystem.CheckState)
            {
                case CheckState.Checked:
                    result |= FileAttributes.System;
                    break;
                case CheckState.Unchecked:
                    excludeAttrs |= FileAttributes.System;
                    break;
            }

            return result;
        }

        private void SetOtherDateLikeThis(DateTimePicker referenceDateTimePicker)
        {
            if (referenceDateTimePicker != dtpCreationTime)
            {
                dtpCreationTime.Value = referenceDateTimePicker.Value;
                chkCreationTime.Checked = true;
            }

            if (referenceDateTimePicker != dtpLastWriteTime)
            {
                dtpLastWriteTime.Value = referenceDateTimePicker.Value;
                chkLastWriteTime.Checked = true;
            }

            if (referenceDateTimePicker != dtpLastAccessTime)
            {
                dtpLastAccessTime.Value = referenceDateTimePicker.Value;
                chkLastAccessTime.Checked = true;
            }

            referenceDateTimePicker.Focus();
        }

        private void UpdateDateTimePickerColor(DateTimePicker picker, bool enabled)
        {
            picker.Enabled = enabled;
        }

        #region Event Handlers

        private void chkCreationTime_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDateTimePickerColor(dtpCreationTime, chkCreationTime.Checked);
            if (chkCreationTime.Checked && Visible)
                dtpCreationTime.Focus();
        }

        private void chkLastWriteTime_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDateTimePickerColor(dtpLastWriteTime, chkLastWriteTime.Checked);
            if (chkLastWriteTime.Checked && Visible)
                dtpLastWriteTime.Focus();
        }

        private void chkLastAccessTime_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDateTimePickerColor(dtpLastAccessTime, chkLastAccessTime.Checked);
            if (chkLastAccessTime.Checked && Visible)
                dtpLastAccessTime.Focus();
        }

        private void dtpCreationTime_ValueChanged(object sender, EventArgs e)
        {
            chkCreationTime.Checked = true;
        }

        private void dtpLastWriteTime_ValueChanged(object sender, EventArgs e)
        {
            chkLastWriteTime.Checked = true;
        }

        private void dtpLastAccessTime_ValueChanged(object sender, EventArgs e)
        {
            chkLastAccessTime.Checked = true;
        }

        private void btnCreationTimeNow_Click(object sender, EventArgs e)
        {
            dtpCreationTime.Value = DateTime.Now;
            if (!chkCreationTime.Checked)
                chkCreationTime.Checked = true;

            if (ModifierKeys.HasFlag(Keys.Control))
                SetOtherDateLikeThis(dtpCreationTime);
        }

        private void btnLastWriteTimeNow_Click(object sender, EventArgs e)
        {
            dtpLastWriteTime.Value = DateTime.Now;
            if (!chkLastWriteTime.Checked)
                chkLastWriteTime.Checked = true;

            if (ModifierKeys.HasFlag(Keys.Control))
                SetOtherDateLikeThis(dtpLastWriteTime);
        }

        private void btnLastAccessTimeNow_Click(object sender, EventArgs e)
        {
            dtpLastAccessTime.Value = DateTime.Now;
            if (!chkLastAccessTime.Checked)
                chkLastAccessTime.Checked = true;

            if (ModifierKeys.HasFlag(Keys.Control))
                SetOtherDateLikeThis(dtpLastAccessTime);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var theNewProperties = _operation.NewProperties;

            if (_operation.SupportedProperties.HasFlag(FilePropertiesTypes.Attributes))
            {
                if (theNewProperties[FilePropertiesTypes.Attributes] is NtfsFileAttributesProperty)
                {
                    _operation.IncludeAttributes = GetAttrFromForm(out var excludeAttributes);
                    _operation.ExcludeAttributes = excludeAttributes;

                    // 如果没有变化，清除新属性
                    if (_operation.IncludeAttributes == 0 && _operation.ExcludeAttributes == 0)
                    {
                        theNewProperties[FilePropertiesTypes.Attributes] = null;
                    }
                }
            }

            if (chkCreationTime.Checked)
            {
                (theNewProperties[FilePropertiesTypes.CreationTime] as FileCreationDateTimeProperty).Value = dtpCreationTime.Value;
            }
            else
            {
                theNewProperties[FilePropertiesTypes.CreationTime] = null;
            }

            if (chkLastWriteTime.Checked)
            {
                (theNewProperties[FilePropertiesTypes.ModificationTime] as FileModificationDateTimeProperty).Value = dtpLastWriteTime.Value;
            }
            else
            {
                theNewProperties[FilePropertiesTypes.ModificationTime] = null;
            }

            if (chkLastAccessTime.Checked)
            {
                (theNewProperties[FilePropertiesTypes.LastAccessTime] as FileLastAccessDateTimeProperty).Value = dtpLastAccessTime.Value;
            }
            else
            {
                theNewProperties[FilePropertiesTypes.LastAccessTime] = null;
            }

            _operation.NewProperties = theNewProperties;
            _operation.Recursive = chkRecursive.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }

        #endregion
    }

    /// <summary>
    /// 显示文件属性设置对话框
    /// </summary>
    public static class SetFilePropertiesDialog
    {
        /// <summary>
        /// 显示文件属性设置对话框
        /// </summary>
        /// <param name="operation">文件属性设置操作</param>
        /// <returns>如果用户点击确定，则返回true；否则返回false</returns>
        public static bool ShowDialog(FileSourceSetFilePropertyOperation operation)
        {
            using var form = new SetFilePropertiesForm(operation);
            return form.ShowDialog() == DialogResult.OK;
        }
    }
}