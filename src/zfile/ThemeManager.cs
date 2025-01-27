namespace WinFormsApp1
{
	public class ThemeManager
    {
        private readonly Form mainForm;
        private readonly ToolStrip toolStrip;
        private readonly MenuStrip menuStrip;
        private readonly TreeView leftTree;
        private readonly TreeView rightTree;
        private readonly ListView leftList;
        private readonly ListView rightList;
        private readonly TextBox leftPreview;
        private readonly TextBox rightPreview;
        private readonly StatusStrip leftStatusStrip;
        private readonly StatusStrip rightStatusStrip;

        public ThemeManager(Form form, ToolStrip toolStrip, MenuStrip menuStrip,
            TreeView leftTree, TreeView rightTree,
            ListView leftList, ListView rightList,
            TextBox leftPreview, TextBox rightPreview,
            StatusStrip leftStatusStrip, StatusStrip rightStatusStrip)
        {
            this.mainForm = form;
            this.toolStrip = toolStrip;
            this.menuStrip = menuStrip;
            this.leftTree = leftTree;
            this.rightTree = rightTree;
            this.leftList = leftList;
            this.rightList = rightList;
            this.leftPreview = leftPreview;
            this.rightPreview = rightPreview;
            this.leftStatusStrip = leftStatusStrip;
            this.rightStatusStrip = rightStatusStrip;
        }

        public void ApplyDarkTheme()
        {
            mainForm.BackColor = Color.FromArgb(45, 45, 48);
            mainForm.ForeColor = Color.White;
            toolStrip.BackColor = Color.FromArgb(28, 28, 28);
            toolStrip.ForeColor = Color.White;
            menuStrip.BackColor = Color.FromArgb(28, 28, 28);
            menuStrip.ForeColor = Color.White;

            SetTreeViewDarkTheme(leftTree);
            SetTreeViewDarkTheme(rightTree);
            SetListViewDarkTheme(leftList);
            SetListViewDarkTheme(rightList);
            SetPreviewDarkTheme(leftPreview);
            SetPreviewDarkTheme(rightPreview);
            SetStatusStripDarkTheme(leftStatusStrip);
            SetStatusStripDarkTheme(rightStatusStrip);
        }

        public void ApplyLightTheme()
        {
            mainForm.BackColor = SystemColors.Control;
            mainForm.ForeColor = SystemColors.ControlText;
            toolStrip.BackColor = SystemColors.Control;
            toolStrip.ForeColor = SystemColors.ControlText;
            menuStrip.BackColor = SystemColors.Control;
            menuStrip.ForeColor = SystemColors.ControlText;

            SetTreeViewLightTheme(leftTree);
            SetTreeViewLightTheme(rightTree);
            SetListViewLightTheme(leftList);
            SetListViewLightTheme(rightList);
            SetPreviewLightTheme(leftPreview);
            SetPreviewLightTheme(rightPreview);
            SetStatusStripLightTheme(leftStatusStrip);
            SetStatusStripLightTheme(rightStatusStrip);
        }

        private void SetTreeViewDarkTheme(TreeView treeView)
        {
            treeView.BackColor = Color.FromArgb(37, 37, 38);
            treeView.ForeColor = Color.White;
        }

        private void SetTreeViewLightTheme(TreeView treeView)
        {
            treeView.BackColor = SystemColors.Window;
            treeView.ForeColor = SystemColors.WindowText;
        }

        private void SetListViewDarkTheme(ListView listView)
        {
            listView.BackColor = Color.FromArgb(37, 37, 38);
            listView.ForeColor = Color.White;
        }

        private void SetListViewLightTheme(ListView listView)
        {
            listView.BackColor = SystemColors.Window;
            listView.ForeColor = SystemColors.WindowText;
        }

        private void SetPreviewDarkTheme(TextBox preview)
        {
            preview.BackColor = Color.FromArgb(37, 37, 38);
            preview.ForeColor = Color.White;
        }

        private void SetPreviewLightTheme(TextBox preview)
        {
            preview.BackColor = SystemColors.Window;
            preview.ForeColor = SystemColors.WindowText;
        }

        private void SetStatusStripDarkTheme(StatusStrip statusStrip)
        {
            statusStrip.BackColor = Color.FromArgb(28, 28, 28);
            statusStrip.ForeColor = Color.White;
        }

        private void SetStatusStripLightTheme(StatusStrip statusStrip)
        {
            statusStrip.BackColor = SystemColors.Control;
            statusStrip.ForeColor = SystemColors.ControlText;
        }
    }
} 