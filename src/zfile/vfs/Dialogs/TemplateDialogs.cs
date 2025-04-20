using System.Windows.Forms;

namespace zfile.Dialogs
{
    /// <summary>
    /// 提供与搜索模板相关的对话框功能
    /// </summary>
    public static class TemplateDialogs
    {
        /// <summary>
        /// 显示搜索模板选择对话框
        /// </summary>
        /// <param name="template">搜索模板</param>
        /// <returns>如果用户选择了模板并点击确定，则返回true；否则返回false</returns>
        public static bool ShowUseTemplateDialog(SearchTemplate template)
        {
            // 创建模板选择对话框
            using var dialog = new Form
            {
                Text = "选择搜索模板",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog
            };

            // 创建模板列表
            var templateList = new ListBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                SelectionMode = SelectionMode.One
            };

            // 加载模板列表
            // TODO: 从全局模板列表加载模板
            // 这里需要实现从GlobalSettings.SearchTemplateList加载模板
            
            // 添加确定和取消按钮
            var okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            // 添加控件到表单
            dialog.Controls.Add(templateList);
            dialog.Controls.Add(cancelButton);
            dialog.Controls.Add(okButton);

            // 设置默认按钮
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            // 显示对话框
            if (dialog.ShowDialog() == DialogResult.OK && templateList.SelectedIndex >= 0)
            {
                // 如果用户选择了模板并点击确定
                // TODO: 设置选中的模板
                // template = GlobalSettings.SearchTemplateList[templateList.SelectedIndex];
                return true;
            }

            return false;
        }
    }
}
