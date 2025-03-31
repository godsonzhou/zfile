using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using MCPSharp;

namespace zfile
{
    public class MCPClientForm : Form
    {
        private MCPClientManager mcpManager;
        private ComboBox serverComboBox;
        private Button connectButton;
        private Button checkStatusButton;
        private Button getToolsButton;
        private RichTextBox outputTextBox;
        private ListView toolsListView;

        public MCPClientForm(string configPath)
        {
            mcpManager = new MCPClientManager(configPath);
            InitializeComponents();
            LoadServerList();
        }

        private void InitializeComponents()
        {
            this.Text = "MCP客户端";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 服务器选择下拉框
            serverComboBox = new ComboBox
            {
                Location = new Point(10, 10),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 连接按钮
            connectButton = new Button
            {
                Text = "连接",
                Location = new Point(220, 10),
                Width = 80
            };
            connectButton.Click += async (s, e) => await ConnectToServer();

            // 检查状态按钮
            checkStatusButton = new Button
            {
                Text = "检查状态",
                Location = new Point(310, 10),
                Width = 80
            };
            checkStatusButton.Click += async (s, e) => await CheckServerStatus();

            // 获取工具按钮
            getToolsButton = new Button
            {
                Text = "获取工具列表",
                Location = new Point(400, 10),
                Width = 100
            };
            getToolsButton.Click += async (s, e) => await GetAvailableTools();

            // 工具列表视图
            toolsListView = new ListView
            {
                Location = new Point(10, 50),
                Width = 300,
                Height = 500,
                View = View.Details
            };
            toolsListView.Columns.Add("可用工具", 280);

            // 输出文本框
            outputTextBox = new RichTextBox
            {
                Location = new Point(320, 50),
                Width = 460,
                Height = 500,
                ReadOnly = true
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[]
            {
                serverComboBox,
                connectButton,
                checkStatusButton,
                getToolsButton,
                toolsListView,
                outputTextBox
            });
        }

        private void LoadServerList()
        {
            serverComboBox.Items.Clear();
            foreach (var serverName in mcpManager.GetServerNames())
            {
                serverComboBox.Items.Add(serverName);
            }
            if (serverComboBox.Items.Count > 0)
            {
                serverComboBox.SelectedIndex = 0;
            }
        }

        private async Task ConnectToServer()
        {
            if (serverComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择服务器", "提示");
                return;
            }

            string serverName = serverComboBox.SelectedItem.ToString();
            connectButton.Enabled = false;
            try
            {
                bool success = await mcpManager.ConnectToServerInConfig(serverName);
                if (success)
                {
                    AppendOutput($"成功连接到服务器: {serverName}");
                }
                else
                {
                    AppendOutput($"连接服务器失败: {serverName}");
                }
            }
            finally
            {
                connectButton.Enabled = true;
            }
        }

        private async Task CheckServerStatus()
        {
            if (serverComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择服务器", "提示");
                return;
            }

            string serverName = serverComboBox.SelectedItem.ToString();
            checkStatusButton.Enabled = false;
            try
            {
                bool isAvailable = await mcpManager.CheckServerStatus(serverName);
                AppendOutput($"服务器 {serverName} 状态: {(isAvailable ? "可用" : "不可用")}");
            }
            finally
            {
                checkStatusButton.Enabled = true;
            }
        }

        private async Task GetAvailableTools()
        {
            if (serverComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择服务器", "提示");
                return;
            }

            string serverName = serverComboBox.SelectedItem.ToString();
            getToolsButton.Enabled = false;
            toolsListView.Items.Clear();
            try
            {
                var tools = await mcpManager.GetAvailableTools(serverName);
                foreach (var tool in tools)
                {
                    toolsListView.Items.Add(new ListViewItem(tool));
                }
                AppendOutput($"已获取服务器 {serverName} 的可用工具列表");
            }
            finally
            {
                getToolsButton.Enabled = true;
            }
        }

        private void AppendOutput(string message)
        {
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action(() => AppendOutput(message)));
                return;
            }

            outputTextBox.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            outputTextBox.ScrollToCaret();
        }
    }
}