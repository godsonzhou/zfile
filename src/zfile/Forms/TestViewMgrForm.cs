using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zfile
{
    public partial class TestViewMgrForm : Form
    {
        private MainForm mainForm;
        private ListView testListView;
        private ComboBox pathComboBox;
        private ComboBox fileSourceComboBox;
        private Button applyButton;
        private Label statusLabel;
        private TextBox statsTextBox;

        public TestViewMgrForm(MainForm mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "测试视图管理器";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建路径选择下拉框
            Label pathLabel = new Label
            {
                Text = "路径:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            pathComboBox = new ComboBox
            {
                Location = new Point(70, 12),
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            // 添加常用路径
            pathComboBox.Items.AddRange(new object[]
            {
                "C:\\",
                "C:\\Windows",
                "C:\\Program Files",
                "C:\\Users",
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            });

            // 创建FileSource类型选择下拉框
            Label fileSourceLabel = new Label
            {
                Text = "文件源:",
                Location = new Point(10, 45),
                AutoSize = true
            };

            fileSourceComboBox = new ComboBox
            {
                Location = new Point(70, 42),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 添加FileSource类型
            fileSourceComboBox.Items.AddRange(new object[]
            {
                "FileSystemFileSource",
                "RecycleBinFileSource",
                "ShellFileSource"
            });
            fileSourceComboBox.SelectedIndex = 0;

            // 创建应用按钮
            applyButton = new Button
            {
                Text = "应用视图",
                Location = new Point(480, 12),
                Width = 100,
                Height = 25
            };
            applyButton.Click += ApplyButton_Click;

            // 创建状态标签
            statusLabel = new Label
            {
                Text = "就绪",
                Location = new Point(10, 530),
                AutoSize = true
            };

            // 创建ListView
            testListView = new ListView
            {
                Location = new Point(10, 80),
                Size = new Size(760, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            // 创建统计信息文本框
            statsTextBox = new TextBox
            {
                Location = new Point(10, 390),
                Size = new Size(760, 130),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // 添加控件到窗体
            this.Controls.Add(pathLabel);
            this.Controls.Add(pathComboBox);
            this.Controls.Add(fileSourceLabel);
            this.Controls.Add(fileSourceComboBox);
            this.Controls.Add(applyButton);
            this.Controls.Add(testListView);
            this.Controls.Add(statusLabel);
            this.Controls.Add(statsTextBox);
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            string path = pathComboBox.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("请输入有效的路径", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 获取选择的FileSource类型
                IFileSource fileSource;
                switch (fileSourceComboBox.SelectedItem.ToString())
                {
                    case "RecycleBinFileSource":
                        fileSource = new RecycleBinFileSource();
                        break;
                    case "ShellFileSource":
                        fileSource = new ShellFileSource();
                        break;
                    case "FileSystemFileSource":
                    default:
                        fileSource = new FileSystemFileSource();
                        break;
                }

                // 清空ListView
                testListView.Items.Clear();
                testListView.Columns.Clear();

                // 获取文件夹统计信息
                var stats = FolderStatistics.GetFolderStats(path, fileSource);
                DisplayFolderStats(stats);

                // 应用视图管理器设置
                mainForm.viewMgr.ApplyViewToListView(testListView, path, fileSource, out _);

                // 获取当前应用的视图模式
                string viewMode = mainForm.viewMgr.GetCurrentViewMode(true);
                statusLabel.Text = $"已应用视图模式: {viewMode}";

                // 加载文件列表
                LoadFileList(path, fileSource);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用视图时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayFolderStats(FolderStatistics.FolderStats stats)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("文件夹统计信息:");
            sb.AppendLine($"总文件数: {stats.TotalFiles}");
            sb.AppendLine($"总文件夹数: {stats.TotalFolders}");
            sb.AppendLine($"图片文件数: {stats.ImageCount}");
            sb.AppendLine($"视频文件数: {stats.VideoCount}");
            sb.AppendLine($"音频文件数: {stats.AudioCount}");
            sb.AppendLine($"文档文件数: {stats.DocumentCount}");
            sb.AppendLine($"压缩文件数: {stats.ArchiveCount}");
            sb.AppendLine($"可执行文件数: {stats.ExecutableCount}");
            sb.AppendLine($"源代码文件数: {stats.SourceCodeCount}");
            sb.AppendLine($"总大小: {FormatFileSize(stats.TotalSize)}");
            sb.AppendLine($"最大文件大小: {FormatFileSize(stats.MaxFileSize)}");
            sb.AppendLine($"最小文件大小: {FormatFileSize(stats.MinFileSize == long.MaxValue ? 0 : stats.MinFileSize)}");
            sb.AppendLine($"平均文件大小: {FormatFileSize((long)stats.AverageFileSize)}");
            sb.AppendLine($"最新文件日期: {(stats.NewestFile == DateTime.MinValue ? "无" : stats.NewestFile.ToString())}");
            sb.AppendLine($"最旧文件日期: {(stats.OldestFile == DateTime.MaxValue ? "无" : stats.OldestFile.ToString())}");
            sb.AppendLine($"主要文件类型: {stats.DominantFileType}");
            sb.AppendLine($"是网络路径: {stats.IsNetworkPath}");
            sb.AppendLine($"是虚拟文件夹: {stats.IsVirtualFolder}");
            sb.AppendLine($"是FTP文件夹: {stats.IsFtpFolder}");
            sb.AppendLine($"是压缩文件: {stats.IsArchiveFolder}");
            sb.AppendLine($"是插件文件夹: {stats.IsPluginFolder}");

            statsTextBox.Text = sb.ToString();
        }

        private string FormatFileSize(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void LoadFileList(string path, IFileSource fileSource)
        {
            try
            {
                // 创建列表操作
                var listOperation = fileSource.CreateListOperation(path);
                if (listOperation == null)
                {
                    statusLabel.Text = "无法创建列表操作";
                    return;
                }

                // 执行列表操作
                OperationsManager.Instance.AddOperation(listOperation);
                listOperation._Thread.WaitFor();

                // 获取文件列表结果
                if (listOperation is not FileSourceListOperation fileListOperation)
                {
                    statusLabel.Text = "操作不是 FileSourceListOperation 类型";
                    return;
                }

                var files = fileListOperation.Files;
                if (files == null)
                {
                    statusLabel.Text = "列表操作未返回文件列表";
                    return;
                }

                // 添加所有项目到 ListView
                foreach (var file in files)
                {
                    if (file.Name == "." || file.Name == "..")
                        continue;

                    var item = new ListViewItem(file.Name);
                    
                    // 添加子项
                    if (testListView.Columns.Count > 1)
                    {
                        item.SubItems.Add(FormatFileSize(file.Size));
                        
                        if (testListView.Columns.Count > 2)
                            item.SubItems.Add(file.IsDirectory ? "文件夹" : Path.GetExtension(file.Name));
                        
                        if (testListView.Columns.Count > 3)
                            item.SubItems.Add(file.ModificationTime.ToString());
                        
                        if (testListView.Columns.Count > 4)
                            item.SubItems.Add(file.Attributes.ToString());
                    }
                    
                    testListView.Items.Add(item);
                }

                statusLabel.Text = $"已加载 {testListView.Items.Count} 个项目";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"加载文件列表失败: {ex.Message}";
            }
        }
    }
}
