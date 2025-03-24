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
            this.Text = "���ʽ������";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            // ���ʽ�����
            Label exprLabel = new Label
            {
                Text = "���ʽ:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            exprTextBox = new TextBox
            {
                Location = new Point(70, 12),
                Size = new Size(500, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // �����б���ͼ
            paramListView = new ListView
            {
                Location = new Point(10, 50),
                Size = new Size(560, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            paramListView.Columns.Add("������", 200);
            paramListView.Columns.Add("����ֵ", 340);

            // ����������ť
            addParamButton = new Button
            {
                Text = "��Ӳ���",
                Location = new Point(10, 260),
                Size = new Size(80, 30),
            };
            addParamButton.Click += AddParamButton_Click;

            deleteParamButton = new Button
            {
                Text = "ɾ������",
                Location = new Point(100, 260),
                Size = new Size(80, 30),
            };
            deleteParamButton.Click += DeleteParamButton_Click;

            clearParamButton = new Button
            {
                Text = "��ղ���",
                Location = new Point(190, 260),
                Size = new Size(80, 30),
            };
            clearParamButton.Click += ClearParamButton_Click;
			// ���˫���¼�����
			paramListView.DoubleClick += ParamListView_DoubleClick;
			// ��������
			Label resultLabel = new Label
            {
                Text = "������:",
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

            // �ײ���ť
            calculateButton = new Button
            {
                Text = "����",
                Location = new Point(400, 330),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            calculateButton.Click += CalculateButton_Click;

            closeButton = new Button
            {
                Text = "�ر�",
                Location = new Point(490, 330),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            closeButton.Click += CloseButton_Click;

            // ��ӿؼ�������
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
		// ����µ��¼�������
		private void ParamListView_DoubleClick(object sender, EventArgs e)
		{
			if (paramListView.SelectedItems.Count > 0)
			{
				ListViewItem selectedItem = paramListView.SelectedItems[0];
				Point mousePos = paramListView.PointToClient(Cursor.Position);
				ListViewHitTestInfo hitTest = paramListView.HitTest(mousePos);

				if (hitTest.SubItem != null)
				{
					// �����ı�����б༭
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
                MessageBox.Show("��������ʽ", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"�������: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
