namespace zfile.Forms
{
    partial class FilterDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxFilterMode = new System.Windows.Forms.GroupBox();
            this.radioButtonByAttributes = new System.Windows.Forms.RadioButton();
            this.radioButtonByDate = new System.Windows.Forms.RadioButton();
            this.radioButtonBySize = new System.Windows.Forms.RadioButton();
            this.radioButtonByExtension = new System.Windows.Forms.RadioButton();
            this.radioButtonByName = new System.Windows.Forms.RadioButton();
            this.radioButtonNoFilter = new System.Windows.Forms.RadioButton();
            this.panelNameFilter = new System.Windows.Forms.Panel();
            this.checkBoxUseRegex = new System.Windows.Forms.CheckBox();
            this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
            this.textBoxNamePattern = new System.Windows.Forms.TextBox();
            this.labelNamePattern = new System.Windows.Forms.Label();
            this.panelExtensionFilter = new System.Windows.Forms.Panel();
            this.checkBoxExcludeExtensions = new System.Windows.Forms.CheckBox();
            this.textBoxExtensions = new System.Windows.Forms.TextBox();
            this.labelExtensions = new System.Windows.Forms.Label();
            this.panelSizeFilter = new System.Windows.Forms.Panel();
            this.labelSizeTo = new System.Windows.Forms.Label();
            this.numericUpDownMaxSize = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownMinSize = new System.Windows.Forms.NumericUpDown();
            this.radioButtonSizeBetween = new System.Windows.Forms.RadioButton();
            this.radioButtonSizeLess = new System.Windows.Forms.RadioButton();
            this.radioButtonSizeGreater = new System.Windows.Forms.RadioButton();
            this.radioButtonSizeEqual = new System.Windows.Forms.RadioButton();
            this.panelDateFilter = new System.Windows.Forms.Panel();
            this.groupBoxDateType = new System.Windows.Forms.GroupBox();
            this.radioButtonAccessDate = new System.Windows.Forms.RadioButton();
            this.radioButtonCreateDate = new System.Windows.Forms.RadioButton();
            this.radioButtonModDate = new System.Windows.Forms.RadioButton();
            this.labelDateTo = new System.Windows.Forms.Label();
            this.dateTimePickerMaxDate = new System.Windows.Forms.DateTimePicker();
            this.dateTimePickerMinDate = new System.Windows.Forms.DateTimePicker();
            this.radioButtonDateBetween = new System.Windows.Forms.RadioButton();
            this.radioButtonDateAfter = new System.Windows.Forms.RadioButton();
            this.radioButtonDateBefore = new System.Windows.Forms.RadioButton();
            this.radioButtonDateEqual = new System.Windows.Forms.RadioButton();
            this.panelAttributesFilter = new System.Windows.Forms.Panel();
            this.checkBoxIncludeReadOnly = new System.Windows.Forms.CheckBox();
            this.checkBoxIncludeSystem = new System.Windows.Forms.CheckBox();
            this.checkBoxIncludeHidden = new System.Windows.Forms.CheckBox();
            this.checkBoxIncludeDirectories = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.groupBoxFilterMode.SuspendLayout();
            this.panelNameFilter.SuspendLayout();
            this.panelExtensionFilter.SuspendLayout();
            this.panelSizeFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinSize)).BeginInit();
            this.panelDateFilter.SuspendLayout();
            this.groupBoxDateType.SuspendLayout();
            this.panelAttributesFilter.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxFilterMode
            // 
            this.groupBoxFilterMode.Controls.Add(this.radioButtonByAttributes);
            this.groupBoxFilterMode.Controls.Add(this.radioButtonByDate);
            this.groupBoxFilterMode.Controls.Add(this.radioButtonBySize);
            this.groupBoxFilterMode.Controls.Add(this.radioButtonByExtension);
            this.groupBoxFilterMode.Controls.Add(this.radioButtonByName);
            this.groupBoxFilterMode.Controls.Add(this.radioButtonNoFilter);
            this.groupBoxFilterMode.Location = new System.Drawing.Point(12, 12);
            this.groupBoxFilterMode.Name = "groupBoxFilterMode";
            this.groupBoxFilterMode.Size = new System.Drawing.Size(200, 180);
            this.groupBoxFilterMode.TabIndex = 0;
            this.groupBoxFilterMode.TabStop = false;
            this.groupBoxFilterMode.Text = "过滤模式";
            // 
            // radioButtonByAttributes
            // 
            this.radioButtonByAttributes.AutoSize = true;
            this.radioButtonByAttributes.Location = new System.Drawing.Point(15, 150);
            this.radioButtonByAttributes.Name = "radioButtonByAttributes";
            this.radioButtonByAttributes.Size = new System.Drawing.Size(83, 19);
            this.radioButtonByAttributes.TabIndex = 5;
            this.radioButtonByAttributes.Text = "按属性过滤";
            this.radioButtonByAttributes.UseVisualStyleBackColor = true;
            this.radioButtonByAttributes.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // radioButtonByDate
            // 
            this.radioButtonByDate.AutoSize = true;
            this.radioButtonByDate.Location = new System.Drawing.Point(15, 125);
            this.radioButtonByDate.Name = "radioButtonByDate";
            this.radioButtonByDate.Size = new System.Drawing.Size(83, 19);
            this.radioButtonByDate.TabIndex = 4;
            this.radioButtonByDate.Text = "按日期过滤";
            this.radioButtonByDate.UseVisualStyleBackColor = true;
            this.radioButtonByDate.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // radioButtonBySize
            // 
            this.radioButtonBySize.AutoSize = true;
            this.radioButtonBySize.Location = new System.Drawing.Point(15, 100);
            this.radioButtonBySize.Name = "radioButtonBySize";
            this.radioButtonBySize.Size = new System.Drawing.Size(83, 19);
            this.radioButtonBySize.TabIndex = 3;
            this.radioButtonBySize.Text = "按大小过滤";
            this.radioButtonBySize.UseVisualStyleBackColor = true;
            this.radioButtonBySize.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // radioButtonByExtension
            // 
            this.radioButtonByExtension.AutoSize = true;
            this.radioButtonByExtension.Location = new System.Drawing.Point(15, 75);
            this.radioButtonByExtension.Name = "radioButtonByExtension";
            this.radioButtonByExtension.Size = new System.Drawing.Size(95, 19);
            this.radioButtonByExtension.TabIndex = 2;
            this.radioButtonByExtension.Text = "按扩展名过滤";
            this.radioButtonByExtension.UseVisualStyleBackColor = true;
            this.radioButtonByExtension.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // radioButtonByName
            // 
            this.radioButtonByName.AutoSize = true;
            this.radioButtonByName.Location = new System.Drawing.Point(15, 50);
            this.radioButtonByName.Name = "radioButtonByName";
            this.radioButtonByName.Size = new System.Drawing.Size(83, 19);
            this.radioButtonByName.TabIndex = 1;
            this.radioButtonByName.Text = "按名称过滤";
            this.radioButtonByName.UseVisualStyleBackColor = true;
            this.radioButtonByName.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // radioButtonNoFilter
            // 
            this.radioButtonNoFilter.AutoSize = true;
            this.radioButtonNoFilter.Checked = true;
            this.radioButtonNoFilter.Location = new System.Drawing.Point(15, 25);
            this.radioButtonNoFilter.Name = "radioButtonNoFilter";
            this.radioButtonNoFilter.Size = new System.Drawing.Size(74, 19);
            this.radioButtonNoFilter.TabIndex = 0;
            this.radioButtonNoFilter.TabStop = true;
            this.radioButtonNoFilter.Text = "不过滤";
            this.radioButtonNoFilter.UseVisualStyleBackColor = true;
            this.radioButtonNoFilter.CheckedChanged += new System.EventHandler(this.radioButtonFilterMode_CheckedChanged);
            // 
            // panelNameFilter
            // 
            this.panelNameFilter.Controls.Add(this.checkBoxUseRegex);
            this.panelNameFilter.Controls.Add(this.checkBoxCaseSensitive);
            this.panelNameFilter.Controls.Add(this.textBoxNamePattern);
            this.panelNameFilter.Controls.Add(this.labelNamePattern);
            this.panelNameFilter.Enabled = false;
            this.panelNameFilter.Location = new System.Drawing.Point(218, 12);
            this.panelNameFilter.Name = "panelNameFilter";
            this.panelNameFilter.Size = new System.Drawing.Size(354, 80);
            this.panelNameFilter.TabIndex = 1;
            // 
            // checkBoxUseRegex
            // 
            this.checkBoxUseRegex.AutoSize = true;
            this.checkBoxUseRegex.Location = new System.Drawing.Point(150, 50);
            this.checkBoxUseRegex.Name = "checkBoxUseRegex";
            this.checkBoxUseRegex.Size = new System.Drawing.Size(99, 19);
            this.checkBoxUseRegex.TabIndex = 3;
            this.checkBoxUseRegex.Text = "使用正则表达式";
            this.checkBoxUseRegex.UseVisualStyleBackColor = true;
            // 
            // checkBoxCaseSensitive
            // 
            this.checkBoxCaseSensitive.AutoSize = true;
            this.checkBoxCaseSensitive.Location = new System.Drawing.Point(15, 50);
            this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
            this.checkBoxCaseSensitive.Size = new System.Drawing.Size(86, 19);
            this.checkBoxCaseSensitive.TabIndex = 2;
            this.checkBoxCaseSensitive.Text = "区分大小写";
            this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // textBoxNamePattern
            // 
            this.textBoxNamePattern.Location = new System.Drawing.Point(80, 15);
            this.textBoxNamePattern.Name = "textBoxNamePattern";
            this.textBoxNamePattern.Size = new System.Drawing.Size(260, 23);
            this.textBoxNamePattern.TabIndex = 1;
            // 
            // labelNamePattern
            // 
            this.labelNamePattern.AutoSize = true;
            this.labelNamePattern.Location = new System.Drawing.Point(15, 18);
            this.labelNamePattern.Name = "labelNamePattern";
            this.labelNamePattern.Size = new System.Drawing.Size(59, 15);
            this.labelNamePattern.TabIndex = 0;
            this.labelNamePattern.Text = "名称模式:";
            // 
            // panelExtensionFilter
            // 
            this.panelExtensionFilter.Controls.Add(this.checkBoxExcludeExtensions);
            this.panelExtensionFilter.Controls.Add(this.textBoxExtensions);
            this.panelExtensionFilter.Controls.Add(this.labelExtensions);
            this.panelExtensionFilter.Enabled = false;
            this.panelExtensionFilter.Location = new System.Drawing.Point(218, 98);
            this.panelExtensionFilter.Name = "panelExtensionFilter";
            this.panelExtensionFilter.Size = new System.Drawing.Size(354, 80);
            this.panelExtensionFilter.TabIndex = 2;
            // 
            // checkBoxExcludeExtensions
            // 
            this.checkBoxExcludeExtensions.AutoSize = true;
            this.checkBoxExcludeExtensions.Location = new System.Drawing.Point(15, 50);
            this.checkBoxExcludeExtensions.Name = "checkBoxExcludeExtensions";
            this.checkBoxExcludeExtensions.Size = new System.Drawing.Size(98, 19);
            this.checkBoxExcludeExtensions.TabIndex = 2;
            this.checkBoxExcludeExtensions.Text = "排除这些扩展名";
            this.checkBoxExcludeExtensions.UseVisualStyleBackColor = true;
            // 
            // textBoxExtensions
            // 
            this.textBoxExtensions.Location = new System.Drawing.Point(80, 15);
            this.textBoxExtensions.Name = "textBoxExtensions";
            this.textBoxExtensions.Size = new System.Drawing.Size(260, 23);
            this.textBoxExtensions.TabIndex = 1;
            // 
            // labelExtensions
            // 
            this.labelExtensions.AutoSize = true;
            this.labelExtensions.Location = new System.Drawing.Point(15, 18);
            this.labelExtensions.Name = "labelExtensions";
            this.labelExtensions.Size = new System.Drawing.Size(59, 15);
            this.labelExtensions.TabIndex = 0;
            this.labelExtensions.Text = "扩展名:";
            // 
            // panelSizeFilter
            // 
            this.panelSizeFilter.Controls.Add(this.labelSizeTo);
            this.panelSizeFilter.Controls.Add(this.numericUpDownMaxSize);
            this.panelSizeFilter.Controls.Add(this.numericUpDownMinSize);
            this.panelSizeFilter.Controls.Add(this.radioButtonSizeBetween);
            this.panelSizeFilter.Controls.Add(this.radioButtonSizeLess);
            this.panelSizeFilter.Controls.Add(this.radioButtonSizeGreater);
            this.panelSizeFilter.Controls.Add(this.radioButtonSizeEqual);
            this.panelSizeFilter.Enabled = false;
            this.panelSizeFilter.Location = new System.Drawing.Point(218, 184);
            this.panelSizeFilter.Name = "panelSizeFilter";
            this.panelSizeFilter.Size = new System.Drawing.Size(354, 110);
            this.panelSizeFilter.TabIndex = 3;
            // 
            // labelSizeTo
            // 
            this.labelSizeTo.AutoSize = true;
            this.labelSizeTo.Enabled = false;
            this.labelSizeTo.Location = new System.Drawing.Point(200, 80);
            this.labelSizeTo.Name = "labelSizeTo";
            this.labelSizeTo.Size = new System.Drawing.Size(15, 15);
            this.labelSizeTo.TabIndex = 6;
            this.labelSizeTo.Text = "到";
            // 
            // numericUpDownMaxSize
            // 
            this.numericUpDownMaxSize.Enabled = false;
            this.numericUpDownMaxSize.Location = new System.Drawing.Point(230, 78);
            this.numericUpDownMaxSize.Maximum = new decimal(new int[] {
            -1,
            2147483647,
            0,
            0});
            this.numericUpDownMaxSize.Name = "numericUpDownMaxSize";
            this.numericUpDownMaxSize.Size = new System.Drawing.Size(110, 23);
            this.numericUpDownMaxSize.TabIndex = 5;
            // 
            // numericUpDownMinSize
            // 
            this.numericUpDownMinSize.Location = new System.Drawing.Point(80, 78);
            this.numericUpDownMinSize.Maximum = new decimal(new int[] {
            -1,
            2147483647,
            0,
            0});
            this.numericUpDownMinSize.Name = "numericUpDownMinSize";
            this.numericUpDownMinSize.Size = new System.Drawing.Size(110, 23);
            this.numericUpDownMinSize.TabIndex = 4;
            // 
            // radioButtonSizeBetween
            // 
            this.radioButtonSizeBetween.AutoSize = true;
            this.radioButtonSizeBetween.Location = new System.Drawing.Point(15, 80);
            this.radioButtonSizeBetween.Name = "radioButtonSizeBetween";
            this.radioButtonSizeBetween.Size = new System.Drawing.Size(50, 19);
            this.radioButtonSizeBetween.TabIndex = 3;
            this.radioButtonSizeBetween.Text = "介于";
            this.radioButtonSizeBetween.UseVisualStyleBackColor = true;
            this.radioButtonSizeBetween.CheckedChanged += new System.EventHandler(this.radioButtonSizeOp_CheckedChanged);
            // 
            // radioButtonSizeLess
            // 
            this.radioButtonSizeLess.AutoSize = true;
            this.radioButtonSizeLess.Location = new System.Drawing.Point(15, 55);
            this.radioButtonSizeLess.Name = "radioButtonSizeLess";
            this.radioButtonSizeLess.Size = new System.Drawing.Size(50, 19);
            this.radioButtonSizeLess.TabIndex = 2;
            this.radioButtonSizeLess.Text = "小于";
            this.radioButtonSizeLess.UseVisualStyleBackColor = true;
            // 
            // radioButtonSizeGreater
            // 
            this.radioButtonSizeGreater.AutoSize = true;
            this.radioButtonSizeGreater.Location = new System.Drawing.Point(15, 30);
            this.radioButtonSizeGreater.Name = "radioButtonSizeGreater";
            this.radioButtonSizeGreater.Size = new System.Drawing.Size(50, 19);
            this.radioButtonSizeGreater.TabIndex = 1;
            this.radioButtonSizeGreater.Text = "大于";
            this.radioButtonSizeGreater.UseVisualStyleBackColor = true;
            // 
            // radioButtonSizeEqual
            // 
            this.radioButtonSizeEqual.AutoSize = true;
            this.radioButtonSizeEqual.Checked = true;
            this.radioButtonSizeEqual.Location = new System.Drawing.Point(15, 5);
            this.radioButtonSizeEqual.Name = "radioButtonSizeEqual";
            this.radioButtonSizeEqual.Size = new System.Drawing.Size(50, 19);
            this.radioButtonSizeEqual.TabIndex = 0;
            this.radioButtonSizeEqual.TabStop = true;
            this.radioButtonSizeEqual.Text = "等于";
            this.radioButtonSizeEqual.UseVisualStyleBackColor = true;
            // 
            // panelDateFilter
            // 
            this.panelDateFilter.Controls.Add(this.groupBoxDateType);
            this.panelDateFilter.Controls.Add(this.labelDateTo);
            this.panelDateFilter.Controls.Add(this.dateTimePickerMaxDate);
            this.panelDateFilter.Controls.Add(this.dateTimePickerMinDate);
            this.panelDateFilter.Controls.Add(this.radioButtonDateBetween);
            this.panelDateFilter.Controls.Add(this.radioButtonDateAfter);
            this.panelDateFilter.Controls.Add(this.radioButtonDateBefore);
            this.panelDateFilter.Controls.Add(this.radioButtonDateEqual);
            this.panelDateFilter.Enabled = false;
            this.panelDateFilter.Location = new System.Drawing.Point(12, 300);
            this.panelDateFilter.Name = "panelDateFilter";
            this.panelDateFilter.Size = new System.Drawing.Size(560, 140);
            this.panelDateFilter.TabIndex = 4;
            // 
            // groupBoxDateType
            // 
            this.groupBoxDateType.Controls.Add(this.radioButtonAccessDate);
            this.groupBoxDateType.Controls.Add(this.radioButtonCreateDate);
            this.groupBoxDateType.Controls.Add(this.radioButtonModDate);
            this.groupBoxDateType.Location = new System.Drawing.Point(350, 5);
            this.groupBoxDateType.Name = "groupBoxDateType";
            this.groupBoxDateType.Size = new System.Drawing.Size(200, 100);
            this.groupBoxDateType.TabIndex = 7;
            this.groupBoxDateType.TabStop = false;
            this.groupBoxDateType.Text = "日期类型";
            // 
            // radioButtonAccessDate
            // 
            this.radioButtonAccessDate.AutoSize = true;
            this.radioButtonAccessDate.Location = new System.Drawing.Point(15, 75);
            this.radioButtonAccessDate.Name = "radioButtonAccessDate";
            this.radioButtonAccessDate.Size = new System.Drawing.Size(74, 19);
            this.radioButtonAccessDate.TabIndex = 2;
            this.radioButtonAccessDate.Text = "访问日期";
            this.radioButtonAccessDate.UseVisualStyleBackColor = true;
            // 
            // radioButtonCreateDate
            // 
            this.radioButtonCreateDate.AutoSize = true;
            this.radioButtonCreateDate.Location = new System.Drawing.Point(15, 50);
            this.radioButtonCreateDate.Name = "radioButtonCreateDate";
            this.radioButtonCreateDate.Size = new System.Drawing.Size(74, 19);
            this.radioButtonCreateDate.TabIndex = 1;
            this.radioButtonCreateDate.Text = "创建日期";
            this.radioButtonCreateDate.UseVisualStyleBackColor = true;
            // 
            // radioButtonModDate
            // 
            this.radioButtonModDate.AutoSize = true;
            this.radioButtonModDate.Checked = true;
            this.radioButtonModDate.Location = new System.Drawing.Point(15, 25);
            this.radioButtonModDate.Name = "radioButtonModDate";
            this.radioButtonModDate.Size = new System.Drawing.Size(74, 19);
            this.radioButtonModDate.TabIndex = 0;
            this.radioButtonModDate.TabStop = true;
            this.radioButtonModDate.Text = "修改日期";
            this.radioButtonModDate.UseVisualStyleBackColor = true;
            // 
            // labelDateTo
            // 
            this.labelDateTo.AutoSize = true;
            this.labelDateTo.Enabled = false;
            this.labelDateTo.Location = new System.Drawing.Point(200, 110);
            this.labelDateTo.Name = "labelDateTo";
            this.labelDateTo.Size = new System.Drawing.Size(15, 15);
            this.labelDateTo.TabIndex = 6;
            this.labelDateTo.Text = "到";
            // 
            // dateTimePickerMaxDate
            // 
            this.dateTimePickerMaxDate.Enabled = false;
            this.dateTimePickerMaxDate.Location = new System.Drawing.Point(230, 105);
            this.dateTimePickerMaxDate.Name = "dateTimePickerMaxDate";
            this.dateTimePickerMaxDate.Size = new System.Drawing.Size(110, 23);
            this.dateTimePickerMaxDate.TabIndex = 5;
            // 
            // dateTimePickerMinDate
            // 
            this.dateTimePickerMinDate.Location = new System.Drawing.Point(80, 105);
            this.dateTimePickerMinDate.Name = "dateTimePickerMinDate";
            this.dateTimePickerMinDate.Size = new System.Drawing.Size(110, 23);
            this.dateTimePickerMinDate.TabIndex = 4;
            // 
            // radioButtonDateBetween
            // 
            this.radioButtonDateBetween.AutoSize = true;
            this.radioButtonDateBetween.Location = new System.Drawing.Point(15, 105);
            this.radioButtonDateBetween.Name = "radioButtonDateBetween";
            this.radioButtonDateBetween.Size = new System.Drawing.Size(50, 19);
            this.radioButtonDateBetween.TabIndex = 3;
            this.radioButtonDateBetween.Text = "介于";
            this.radioButtonDateBetween.UseVisualStyleBackColor = true;
            this.radioButtonDateBetween.CheckedChanged += new System.EventHandler(this.radioButtonDateOp_CheckedChanged);
            // 
            // radioButtonDateAfter
            // 
            this.radioButtonDateAfter.AutoSize = true;
            this.radioButtonDateAfter.Location = new System.Drawing.Point(15, 80);
            this.radioButtonDateAfter.Name = "radioButtonDateAfter";
            this.radioButtonDateAfter.Size = new System.Drawing.Size(50, 19);
            this.radioButtonDateAfter.TabIndex = 2;
            this.radioButtonDateAfter.Text = "晚于";
            this.radioButtonDateAfter.UseVisualStyleBackColor = true;
            // 
            // radioButtonDateBefore
            // 
            this.radioButtonDateBefore.AutoSize = true;
            this.radioButtonDateBefore.Location = new System.Drawing.Point(15, 55);
            this.radioButtonDateBefore.Name = "radioButtonDateBefore";
            this.radioButtonDateBefore.Size = new System.Drawing.Size(50, 19);
            this.radioButtonDateBefore.TabIndex = 1;
            this.radioButtonDateBefore.Text = "早于";
            this.radioButtonDateBefore.UseVisualStyleBackColor = true;
            // 
            // radioButtonDateEqual
            // 
            this.radioButtonDateEqual.AutoSize = true;
            this.radioButtonDateEqual.Checked = true;
            this.radioButtonDateEqual.Location = new System.Drawing.Point(15, 30);
            this.radioButtonDateEqual.Name = "radioButtonDateEqual";
            this.radioButtonDateEqual.Size = new System.Drawing.Size(50, 19);
            this.radioButtonDateEqual.TabIndex = 0;
            this.radioButtonDateEqual.TabStop = true;
            this.radioButtonDateEqual.Text = "等于";
            this.radioButtonDateEqual.UseVisualStyleBackColor = true;
            // 
            // panelAttributesFilter
            // 
            this.panelAttributesFilter.Controls.Add(this.checkBoxIncludeReadOnly);
            this.panelAttributesFilter.Controls.Add(this.checkBoxIncludeSystem);
            this.panelAttributesFilter.Controls.Add(this.checkBoxIncludeHidden);
            this.panelAttributesFilter.Controls.Add(this.checkBoxIncludeDirectories);
            this.panelAttributesFilter.Enabled = false;
            this.panelAttributesFilter.Location = new System.Drawing.Point(12, 446);
            this.panelAttributesFilter.Name = "panelAttributesFilter";
            this.panelAttributesFilter.Size = new System.Drawing.Size(560, 50);
            this.panelAttributesFilter.TabIndex = 5;
            // 
            // checkBoxIncludeReadOnly
            // 
            this.checkBoxIncludeReadOnly.AutoSize = true;
            this.checkBoxIncludeReadOnly.Checked = true;
            this.checkBoxIncludeReadOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeReadOnly.Location = new System.Drawing.Point(350, 15);
            this.checkBoxIncludeReadOnly.Name = "checkBoxIncludeReadOnly";
            this.checkBoxIncludeReadOnly.Size = new System.Drawing.Size(86, 19);
            this.checkBoxIncludeReadOnly.TabIndex = 3;
            this.checkBoxIncludeReadOnly.Text = "包含只读文件";
            this.checkBoxIncludeReadOnly.UseVisualStyleBackColor = true;
            // 
            // checkBoxIncludeSystem
            // 
            this.checkBoxIncludeSystem.AutoSize = true;
            this.checkBoxIncludeSystem.Checked = true;
            this.checkBoxIncludeSystem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeSystem.Location = new System.Drawing.Point(230, 15);
            this.checkBoxIncludeSystem.Name = "checkBoxIncludeSystem";
            this.checkBoxIncludeSystem.Size = new System.Drawing.Size(86, 19);
            this.checkBoxIncludeSystem.TabIndex = 2;
            this.checkBoxIncludeSystem.Text = "包含系统文件";
            this.checkBoxIncludeSystem.UseVisualStyleBackColor = true;
            // 
            // checkBoxIncludeHidden
            // 
            this.checkBoxIncludeHidden.AutoSize = true;
            this.checkBoxIncludeHidden.Checked = true;
            this.checkBoxIncludeHidden.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeHidden.Location = new System.Drawing.Point(120, 15);
            this.checkBoxIncludeHidden.Name = "checkBoxIncludeHidden";
            this.checkBoxIncludeHidden.Size = new System.Drawing.Size(86, 19);
            this.checkBoxIncludeHidden.TabIndex = 1;
            this.checkBoxIncludeHidden.Text = "包含隐藏文件";
            this.checkBoxIncludeHidden.UseVisualStyleBackColor = true;
            // 
            // checkBoxIncludeDirectories
            // 
            this.checkBoxIncludeDirectories.AutoSize = true;
            this.checkBoxIncludeDirectories.Checked = true;
            this.checkBoxIncludeDirectories.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeDirectories.Location = new System.Drawing.Point(15, 15);
            this.checkBoxIncludeDirectories.Name = "checkBoxIncludeDirectories";
            this.checkBoxIncludeDirectories.Size = new System.Drawing.Size(74, 19);
            this.checkBoxIncludeDirectories.TabIndex = 0;
            this.checkBoxIncludeDirectories.Text = "包含文件夹";
            this.checkBoxIncludeDirectories.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(316, 502);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "确定";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(397, 502);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.Location = new System.Drawing.Point(478, 502);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(94, 23);
            this.buttonClear.TabIndex = 8;
            this.buttonClear.Text = "清除过滤器";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // FilterDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 537);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.panelAttributesFilter);
            this.Controls.Add(this.panelDateFilter);
            this.Controls.Add(this.panelSizeFilter);
            this.Controls.Add(this.panelExtensionFilter);
            this.Controls.Add(this.panelNameFilter);
            this.Controls.Add(this.groupBoxFilterMode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FilterDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "文件过滤器";
            this.groupBoxFilterMode.ResumeLayout(false);
            this.groupBoxFilterMode.PerformLayout();
            this.panelNameFilter.ResumeLayout(false);
            this.panelNameFilter.PerformLayout();
            this.panelExtensionFilter.ResumeLayout(false);
            this.panelExtensionFilter.PerformLayout();
            this.panelSizeFilter.ResumeLayout(false);
            this.panelSizeFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinSize)).EndInit();
            this.panelDateFilter.ResumeLayout(false);
            this.panelDateFilter.PerformLayout();
            this.groupBoxDateType.ResumeLayout(false);
            this.groupBoxDateType.PerformLayout();
            this.panelAttributesFilter.ResumeLayout(false);
            this.panelAttributesFilter.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private GroupBox groupBoxFilterMode;
        private RadioButton radioButtonByAttributes;
        private RadioButton radioButtonByDate;
        private RadioButton radioButtonBySize;
        private RadioButton radioButtonByExtension;
        private RadioButton radioButtonByName;
        private RadioButton radioButtonNoFilter;
        private Panel panelNameFilter;
        private CheckBox checkBoxUseRegex;
        private CheckBox checkBoxCaseSensitive;
        private TextBox textBoxNamePattern;
        private Label labelNamePattern;
        private Panel panelExtensionFilter;
        private CheckBox checkBoxExcludeExtensions;
        private TextBox textBoxExtensions;
        private Label labelExtensions;
        private Panel panelSizeFilter;
        private Label labelSizeTo;
        private NumericUpDown numericUpDownMaxSize;
        private NumericUpDown numericUpDownMinSize;
        private RadioButton radioButtonSizeBetween;
        private RadioButton radioButtonSizeLess;
        private RadioButton radioButtonSizeGreater;
        private RadioButton radioButtonSizeEqual;
        private Panel panelDateFilter;
        private GroupBox groupBoxDateType;
        private RadioButton radioButtonAccessDate;
        private RadioButton radioButtonCreateDate;
        private RadioButton radioButtonModDate;
        private Label labelDateTo;
        private DateTimePicker dateTimePickerMaxDate;
        private DateTimePicker dateTimePickerMinDate;
        private RadioButton radioButtonDateBetween;
        private RadioButton radioButtonDateAfter;
        private RadioButton radioButtonDateBefore;
        private RadioButton radioButtonDateEqual;
        private Panel panelAttributesFilter;
        private CheckBox checkBoxIncludeReadOnly;
        private CheckBox checkBoxIncludeSystem;
        private CheckBox checkBoxIncludeHidden;
        private CheckBox checkBoxIncludeDirectories;
        private Button buttonOK;
        private Button buttonCancel;
        private Button buttonClear;
    }
}