using System.ComponentModel;

namespace zfile
{
    public class EvaluateExprDlg : Form
    {
        private TextBox exprTextBox;
        private ListView paramListView;
        private TextBox resultTextBox;
        private Button calculateButton;
        private Button closeButton;
        private Button addParamButton;
        private Button deleteParamButton;
        private Button clearParamButton;

        public EvaluateExprDlg(string expression) { 
            InitializeComponent();
			exprTextBox.Text = expression;
        }

        private void InitializeComponent()
        {
            this.Text = "表达式计算器";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            // 表达式输入框
            Label exprLabel = new Label
            {
                Text = "表达式:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            exprTextBox = new TextBox
            {
                Location = new Point(70, 12),
                Size = new Size(500, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 参数列表视图
            paramListView = new ListView
            {
                Location = new Point(10, 50),
                Size = new Size(560, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            paramListView.Columns.Add("参数名", 200);
            paramListView.Columns.Add("参数值", 340);

            // 参数操作按钮
            addParamButton = new Button
            {
                Text = "添加参数",
                Location = new Point(10, 260),
                Size = new Size(80, 30),
            };
            addParamButton.Click += AddParamButton_Click;

            deleteParamButton = new Button
            {
                Text = "删除参数",
                Location = new Point(100, 260),
                Size = new Size(80, 30),
            };
            deleteParamButton.Click += DeleteParamButton_Click;

            clearParamButton = new Button
            {
                Text = "清空参数",
                Location = new Point(190, 260),
                Size = new Size(80, 30),
            };
            clearParamButton.Click += ClearParamButton_Click;
			// 添加双击事件处理
			paramListView.DoubleClick += ParamListView_DoubleClick;
			// 结果输出框
			Label resultLabel = new Label
            {
                Text = "计算结果:",
                Location = new Point(10, 300),
                AutoSize = true
            };

            resultTextBox = new TextBox
            {
                Location = new Point(70, 297),
                Size = new Size(500, 23),
                ReadOnly = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // 底部按钮
            calculateButton = new Button
            {
                Text = "计算",
                Location = new Point(400, 330),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            calculateButton.Click += CalculateButton_Click;

            closeButton = new Button
            {
                Text = "关闭",
                Location = new Point(490, 330),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            closeButton.Click += CloseButton_Click;

            // 添加控件到窗体
            Controls.AddRange(new Control[] {
                exprLabel,
                exprTextBox,
                paramListView,
                addParamButton,
                deleteParamButton,
                clearParamButton,
                resultLabel,
                resultTextBox,
                calculateButton,
                closeButton
            });
        }
		// 添加新的事件处理方法
		private void ParamListView_DoubleClick(object sender, EventArgs e)
		{
			if (paramListView.SelectedItems.Count > 0)
			{
				ListViewItem selectedItem = paramListView.SelectedItems[0];
				Point mousePos = paramListView.PointToClient(Cursor.Position);
				ListViewHitTestInfo hitTest = paramListView.HitTest(mousePos);

				if (hitTest.SubItem != null)
				{
					// 创建文本框进行编辑
					TextBox editBox = new TextBox
					{
						Location = hitTest.SubItem.Bounds.Location,
						Size = hitTest.SubItem.Bounds.Size,
						Text = hitTest.SubItem.Text,
						BorderStyle = BorderStyle.FixedSingle
					};

					editBox.LostFocus += (s, ev) =>
					{
						hitTest.SubItem.Text = editBox.Text;
						paramListView.Controls.Remove(editBox);
					};

					editBox.KeyPress += (s, ev) =>
					{
						if (ev.KeyChar == (char)Keys.Enter)
						{
							hitTest.SubItem.Text = editBox.Text;
							paramListView.Controls.Remove(editBox);
							ev.Handled = true;
						}
						else if (ev.KeyChar == (char)Keys.Escape)
						{
							paramListView.Controls.Remove(editBox);
							ev.Handled = true;
						}
					};

					paramListView.Controls.Add(editBox);
					editBox.Focus();
					editBox.SelectAll();
				}
			}
		}
		private void AddParamButton_Click(object sender, EventArgs e)
        {
            ListViewItem item = new ListViewItem(new[] { "param" + (paramListView.Items.Count + 1), "0" });
            paramListView.Items.Add(item);
			paramListView.LabelEdit = true;
			item.Selected = true;
			item.BeginEdit();
        }

        private void DeleteParamButton_Click(object sender, EventArgs e)
        {
            if (paramListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in paramListView.SelectedItems)
                {
                    paramListView.Items.Remove(item);
                }
            }
        }

        private void ClearParamButton_Click(object sender, EventArgs e)
        {
            paramListView.Items.Clear();
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            string expr = exprTextBox.Text;
            if (string.IsNullOrWhiteSpace(expr))
            {
                MessageBox.Show("请输入表达式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            foreach (ListViewItem item in paramListView.Items)
            {
                parameters[item.SubItems[0].Text] = item.SubItems[1].Text;
            }

            try
            {
                var result = new ExpressionEvaluatorClaude().EvalExpr(expr, parameters);
                resultTextBox.Text = result.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
