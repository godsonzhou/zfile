using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using zfile.Filter;

namespace zfile.Forms
{
    public partial class FilterDialog : Form
    {
        //private FileFilter _filter;

        //public FilterDialog(FileFilter filter = null)
        //{
        //    InitializeComponent();
        //    _filter = filter ?? new FileFilter();
        //    LoadFilterSettings();
        //}

        ///// <summary>
        ///// Load filter settings from the filter object to the UI
        ///// </summary>
        //private void LoadFilterSettings()
        //{
        //    // Set filter mode
        //    switch (_filter.FilterMode)
        //    {
        //        case FilterMode.None:
        //            radioButtonNoFilter.Checked = true;
        //            break;
        //        case FilterMode.ByName:
        //            radioButtonByName.Checked = true;
        //            break;
        //        case FilterMode.ByExtension:
        //            radioButtonByExtension.Checked = true;
        //            break;
        //        case FilterMode.BySize:
        //            radioButtonBySize.Checked = true;
        //            break;
        //        case FilterMode.ByDate:
        //            radioButtonByDate.Checked = true;
        //            break;
        //        case FilterMode.ByAttributes:
        //            radioButtonByAttributes.Checked = true;
        //            break;
        //    }

        //    // Name filter settings
        //    textBoxNamePattern.Text = _filter.NamePattern;
        //    checkBoxCaseSensitive.Checked = _filter.CaseSensitive;
        //    checkBoxUseRegex.Checked = _filter.UseRegex;

        //    // Extension filter settings
        //    textBoxExtensions.Text = _filter.Extensions;
        //    checkBoxExcludeExtensions.Checked = _filter.ExcludeExtensions;

        //    // Size filter settings
        //    switch (_filter.SizeComparisonType)
        //    {
        //        case ComparisonType.Equal:
        //            radioButtonSizeEqual.Checked = true;
        //            break;
        //        case ComparisonType.Greater:
        //            radioButtonSizeGreater.Checked = true;
        //            break;
        //        case ComparisonType.Less:
        //            radioButtonSizeLess.Checked = true;
        //            break;
        //        case ComparisonType.Between:
        //            radioButtonSizeBetween.Checked = true;
        //            break;
        //    }
        //    numericUpDownMinSize.Value = _filter.MinSize > 0 ? _filter.MinSize : 0;
        //    numericUpDownMaxSize.Value = _filter.MaxSize > 0 ? _filter.MaxSize : 0;

        //    // Date filter settings
        //    switch (_filter.DateComparisonType)
        //    {
        //        case ComparisonType.Equal:
        //            radioButtonDateEqual.Checked = true;
        //            break;
        //        case ComparisonType.Less:
        //            radioButtonDateBefore.Checked = true;
        //            break;
        //        case ComparisonType.Greater:
        //            radioButtonDateAfter.Checked = true;
        //            break;
        //        case ComparisonType.Between:
        //            radioButtonDateBetween.Checked = true;
        //            break;
        //    }
        //    switch (_filter.DateType)
        //    {
        //        case DateType.Modified:
        //            radioButtonModDate.Checked = true;
        //            break;
        //        case DateType.Created:
        //            radioButtonCreateDate.Checked = true;
        //            break;
        //        case DateType.Accessed:
        //            radioButtonAccessDate.Checked = true;
        //            break;
        //    }
        //    dateTimePickerMinDate.Value = _filter.MinDate;
        //    dateTimePickerMaxDate.Value = _filter.MaxDate;

        //    // Attributes filter settings
        //    checkBoxIncludeDirectories.Checked = _filter.IncludeDirectories;
        //    checkBoxIncludeHidden.Checked = _filter.IncludeHidden;
        //    checkBoxIncludeSystem.Checked = _filter.IncludeSystem;
        //    checkBoxIncludeReadOnly.Checked = _filter.IncludeReadOnly;

        //    // Update UI state based on filter mode
        //    UpdatePanelStates();
        //}

        ///// <summary>
        ///// Update filter settings from the UI to the filter object
        ///// </summary>
        //private void UpdateFilterSettings()
        //{
        //    // Set filter mode
        //    if (radioButtonNoFilter.Checked)
        //        _filter.FilterMode = FilterMode.None;
        //    else if (radioButtonByName.Checked)
        //        _filter.FilterMode = FilterMode.ByName;
        //    else if (radioButtonByExtension.Checked)
        //        _filter.FilterMode = FilterMode.ByExtension;
        //    else if (radioButtonBySize.Checked)
        //        _filter.FilterMode = FilterMode.BySize;
        //    else if (radioButtonByDate.Checked)
        //        _filter.FilterMode = FilterMode.ByDate;
        //    else if (radioButtonByAttributes.Checked)
        //        _filter.FilterMode = FilterMode.ByAttributes;

        //    // Name filter settings
        //    _filter.NamePattern = textBoxNamePattern.Text;
        //    _filter.CaseSensitive = checkBoxCaseSensitive.Checked;
        //    _filter.UseRegex = checkBoxUseRegex.Checked;

        //    // Extension filter settings
        //    _filter.Extensions = textBoxExtensions.Text;
        //    _filter.ExcludeExtensions = checkBoxExcludeExtensions.Checked;

        //    // Size filter settings
        //    if (radioButtonSizeEqual.Checked)
        //        _filter.SizeComparisonType = ComparisonType.Equal;
        //    else if (radioButtonSizeGreater.Checked)
        //        _filter.SizeComparisonType = ComparisonType.Greater;
        //    else if (radioButtonSizeLess.Checked)
        //        _filter.SizeComparisonType = ComparisonType.Less;
        //    else if (radioButtonSizeBetween.Checked)
        //        _filter.SizeComparisonType = ComparisonType.Between;

        //    _filter.MinSize = (long)numericUpDownMinSize.Value;
        //    _filter.MaxSize = (long)numericUpDownMaxSize.Value;

        //    // Date filter settings
        //    if (radioButtonDateEqual.Checked)
        //        _filter.DateComparisonType = ComparisonType.Equal;
        //    else if (radioButtonDateBefore.Checked)
        //        _filter.DateComparisonType = ComparisonType.Less;
        //    else if (radioButtonDateAfter.Checked)
        //        _filter.DateComparisonType = ComparisonType.Greater;
        //    else if (radioButtonDateBetween.Checked)
        //        _filter.DateComparisonType = ComparisonType.Between;

        //    if (radioButtonModDate.Checked)
        //        _filter.DateType = DateType.Modified;
        //    else if (radioButtonCreateDate.Checked)
        //        _filter.DateType = DateType.Created;
        //    else if (radioButtonAccessDate.Checked)
        //        _filter.DateType = DateType.Accessed;

        //    _filter.MinDate = dateTimePickerMinDate.Value;
        //    _filter.MaxDate = dateTimePickerMaxDate.Value;

        //    // Attributes filter settings
        //    _filter.IncludeDirectories = checkBoxIncludeDirectories.Checked;
        //    _filter.IncludeHidden = checkBoxIncludeHidden.Checked;
        //    _filter.IncludeSystem = checkBoxIncludeSystem.Checked;
        //    _filter.IncludeReadOnly = checkBoxIncludeReadOnly.Checked;
        //}

        ///// <summary>
        ///// Update the enabled state of filter option panels based on the selected filter mode
        ///// </summary>
        //private void UpdatePanelStates()
        //{
        //    panelNameFilter.Enabled = radioButtonByName.Checked;
        //    panelExtensionFilter.Enabled = radioButtonByExtension.Checked;
        //    panelSizeFilter.Enabled = radioButtonBySize.Checked;
        //    panelDateFilter.Enabled = radioButtonByDate.Checked;
        //    panelAttributesFilter.Enabled = radioButtonByAttributes.Checked;

        //    // Update size filter controls
        //    labelSizeTo.Enabled = radioButtonSizeBetween.Checked;
        //    numericUpDownMaxSize.Enabled = radioButtonSizeBetween.Checked;

        //    // Update date filter controls
        //    labelDateTo.Enabled = radioButtonDateBetween.Checked;
        //    dateTimePickerMaxDate.Enabled = radioButtonDateBetween.Checked;
        //}

        ///// <summary>
        ///// Handle filter mode radio button checked changed event
        ///// </summary>
        //private void radioButtonFilterMode_CheckedChanged(object sender, EventArgs e)
        //{
        //    UpdatePanelStates();
        //}

        ///// <summary>
        ///// Handle size comparison radio button checked changed event
        ///// </summary>
        //private void radioButtonSizeOp_CheckedChanged(object sender, EventArgs e)
        //{
        //    labelSizeTo.Enabled = radioButtonSizeBetween.Checked;
        //    numericUpDownMaxSize.Enabled = radioButtonSizeBetween.Checked;
        //}

        ///// <summary>
        ///// Handle date comparison radio button checked changed event
        ///// </summary>
        //private void radioButtonDateOp_CheckedChanged(object sender, EventArgs e)
        //{
        //    labelDateTo.Enabled = radioButtonDateBetween.Checked;
        //    dateTimePickerMaxDate.Enabled = radioButtonDateBetween.Checked;
        //}

        ///// <summary>
        ///// Handle OK button click event
        ///// </summary>
        //private void buttonOK_Click(object sender, EventArgs e)
        //{
        //    UpdateFilterSettings();
        //    FilterManager.Instance.CurrentFilter = _filter;
        //    DialogResult = DialogResult.OK;
        //    Close();
        //}

        ///// <summary>
        ///// Handle Cancel button click event
        ///// </summary>
        //private void buttonCancel_Click(object sender, EventArgs e)
        //{
        //    DialogResult = DialogResult.Cancel;
        //    Close();
        //}

        ///// <summary>
        ///// Handle Clear button click event
        ///// </summary>
        //private void buttonClear_Click(object sender, EventArgs e)
        //{
        //    _filter = new FileFilter();
        //    LoadFilterSettings();
        //}

        ///// <summary>
        ///// Get the filter object
        ///// </summary>
        //public FileFilter GetFilter()
        //{
        //    return _filter;
        //}
    }
}