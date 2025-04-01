using System.Collections;
using System.Text;
using Zfile.Forms;
namespace Zfile
{
    public enum SyncFileState
    {
        Equal,          // 文件相同
        NotEqual,       // 文件不同
        LeftOnly,       // 仅左侧存在
        RightOnly,      // 仅右侧存在
        LeftNewer,      // 左侧较新
        RightNewer      // 右侧较新
    }

    public class SyncFileInfo
    {
        public string RelativePath { get; set; }
        public string LeftPath { get; set; }
        public string RightPath { get; set; }
        public SyncFileState State { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsDirectory { get; set; }
    }

    public partial class SyncDirsDlg : Form
    {
        private string leftPath;
        private string rightPath;
        private bool includeSubDirs = true;
        private bool compareContent = false;
        private List<SyncFileInfo> syncFiles = new();
        private ListView resultListView;
        private TextBox txtLeftPath;
        private TextBox txtRightPath;
        private CheckBox chkSubDirs;
        private CheckBox chkContent;
        private Button btnCompare;
        private Button btnSync;
        private Button btnClose;
        private ProgressBar progressBar;
        private Label lblStatus;

        public SyncDirsDlg(string leftpath, string rightpath)
        {
			leftPath = leftpath;
			rightPath = rightpath;
			InitializeComponents();
            InitializeEvents();
        }

        private void InitializeComponents()
        {
            this.Text = "目录同步";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建控件
            txtLeftPath = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(300, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Text = leftPath
            };

            txtRightPath = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(300, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Text = rightPath
            };

            chkSubDirs = new CheckBox
            {
                Text = "包含子目录",
                Location = new Point(10, 70),
                Checked = true
            };

            chkContent = new CheckBox
            {
                Text = "比较文件内容",
                Location = new Point(120, 70),
                Checked = false
            };

            btnCompare = new Button
            {
                Text = "比较",
                Location = new Point(320, 10),
                Size = new Size(75, 23)
            };

            btnSync = new Button
            {
                Text = "同步",
                Location = new Point(320, 40),
                Size = new Size(75, 23),
                Enabled = false
            };

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(320, 70),
                Size = new Size(75, 23)
            };

            resultListView = new ListView
            {
                Location = new Point(10, 100),
                Size = new Size(765, 420),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            resultListView.Columns.Add("文件名", 200);
            resultListView.Columns.Add("状态", 100);
            resultListView.Columns.Add("大小", 100);
            resultListView.Columns.Add("修改时间", 150);
            resultListView.Columns.Add("路径", 200);

            progressBar = new ProgressBar
            {
                Location = new Point(10, 530),
                Size = new Size(765, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            lblStatus = new Label
            {
                Location = new Point(10, 500),
                Size = new Size(765, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] {
                txtLeftPath, txtRightPath, chkSubDirs, chkContent,
                btnCompare, btnSync, btnClose,
                resultListView, progressBar, lblStatus
            });
        }

        private void InitializeEvents()
        {
            btnCompare.Click += BtnCompare_Click;
            btnSync.Click += BtnSync_Click;
            btnClose.Click += (s, e) => this.Close();
            resultListView.ColumnClick += ListView_ColumnClick;
        }

        private async void BtnCompare_Click(object sender, EventArgs e)
        {
            leftPath = txtLeftPath.Text;
            rightPath = txtRightPath.Text;
            includeSubDirs = chkSubDirs.Checked;
            compareContent = chkContent.Checked;

            if (!Directory.Exists(leftPath) || !Directory.Exists(rightPath))
            {
                MessageBox.Show("请输入有效的目录路径", "错误");
                return;
            }

            btnCompare.Enabled = false;
            btnSync.Enabled = false;
            progressBar.Value = 0;
            syncFiles.Clear();
            resultListView.Items.Clear();

            try
            {
                await CompareDirectoriesAsync();
                UpdateListView();
                btnSync.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"比较目录时出错: {ex.Message}", "错误");
            }
            finally
            {
                btnCompare.Enabled = true;
            }
        }

        private async Task CompareDirectoriesAsync()
        {
            var leftFiles = GetFiles(leftPath);
            var rightFiles = GetFiles(rightPath);
            var allPaths = leftFiles.Keys.Union(rightFiles.Keys).ToList();
            progressBar.Maximum = allPaths.Count;
            var progress = 0;

            foreach (var relativePath in allPaths)
            {
                var syncInfo = new SyncFileInfo
                {
                    RelativePath = relativePath,
                    LeftPath = Path.Combine(leftPath, relativePath),
                    RightPath = Path.Combine(rightPath, relativePath)
                };

                if (leftFiles.ContainsKey(relativePath) && rightFiles.ContainsKey(relativePath))
                {
                    var leftFile = leftFiles[relativePath];
                    var rightFile = rightFiles[relativePath];

                    syncInfo.IsDirectory = leftFile.IsDirectory;
                    syncInfo.Size = leftFile.Size;
                    syncInfo.LastModified = leftFile.LastModified;

                    if (leftFile.IsDirectory != rightFile.IsDirectory)
                    {
                        syncInfo.State = SyncFileState.NotEqual;
                    }
                    else if (leftFile.IsDirectory)
                    {
                        syncInfo.State = SyncFileState.Equal;
                    }
                    else
                    {
                        if (compareContent)
                        {
                            syncInfo.State = await CompareFilesAsync(leftFile.Path, rightFile.Path) 
                                ? SyncFileState.Equal 
                                : SyncFileState.NotEqual;
                        }
                        else
                        {
                            if (leftFile.Size != rightFile.Size)
                                syncInfo.State = SyncFileState.NotEqual;
                            else if (leftFile.LastModified > rightFile.LastModified)
                                syncInfo.State = SyncFileState.LeftNewer;
                            else if (leftFile.LastModified < rightFile.LastModified)
                                syncInfo.State = SyncFileState.RightNewer;
                            else
                                syncInfo.State = SyncFileState.Equal;
                        }
                    }
                }
                else if (leftFiles.ContainsKey(relativePath))
                {
                    var leftFile = leftFiles[relativePath];
                    syncInfo.IsDirectory = leftFile.IsDirectory;
                    syncInfo.Size = leftFile.Size;
                    syncInfo.LastModified = leftFile.LastModified;
                    syncInfo.State = SyncFileState.LeftOnly;
                }
                else
                {
                    var rightFile = rightFiles[relativePath];
                    syncInfo.IsDirectory = rightFile.IsDirectory;
                    syncInfo.Size = rightFile.Size;
                    syncInfo.LastModified = rightFile.LastModified;
                    syncInfo.State = SyncFileState.RightOnly;
                }

                syncFiles.Add(syncInfo);
                progressBar.Value = ++progress;
                lblStatus.Text = $"正在比较: {progress}/{allPaths.Count}";
                Application.DoEvents();
            }
        }

        private Dictionary<string, (string Path, bool IsDirectory, long Size, DateTime LastModified)> GetFiles(string basePath)
        {
            var result = new Dictionary<string, (string, bool, long, DateTime)>();
            var baseUri = new Uri(basePath);

            void AddFile(string path)
            {
                var relativePath = baseUri.MakeRelativeUri(new Uri(path)).ToString();
                var info = new FileInfo(path);
                result[relativePath] = (path, false, info.Length, info.LastWriteTime);
            }

            void AddDirectory(string path)
            {
                var relativePath = baseUri.MakeRelativeUri(new Uri(path)).ToString();
                var info = new DirectoryInfo(path);
                result[relativePath] = (path, true, 0, info.LastWriteTime);
            }

            if (includeSubDirs)
            {
                foreach (var dir in Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories))
                {
                    AddDirectory(dir);
                }
            }

            foreach (var file in Directory.GetFiles(basePath, "*", 
                includeSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                AddFile(file);
            }

            return result;
        }

        private async Task<bool> CompareFilesAsync(string file1, string file2)
        {
            const int bufferSize = 4096;
            using var fs1 = File.OpenRead(file1);
            using var fs2 = File.OpenRead(file2);

            if (fs1.Length != fs2.Length)
                return false;

            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                var count1 = await fs1.ReadAsync(buffer1, 0, bufferSize);
                var count2 = await fs2.ReadAsync(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                for (int i = 0; i < count1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                        return false;
                }
            }
        }

        private void UpdateListView()
        {
            resultListView.BeginUpdate();
            resultListView.Items.Clear();

            foreach (var file in syncFiles)
            {
                var item = new ListViewItem(Path.GetFileName(file.RelativePath));
                item.SubItems.Add(GetStateText(file.State));
                item.SubItems.Add(file.IsDirectory ? "<DIR>" : FileSystemManager.FormatFileSize(file.Size));
                item.SubItems.Add(file.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(file.RelativePath);

                // 设置颜色
                switch (file.State)
                {
                    case SyncFileState.Equal:
                        item.BackColor = Color.White;
                        break;
                    case SyncFileState.NotEqual:
                        item.BackColor = Color.Yellow;
                        break;
                    case SyncFileState.LeftOnly:
                        item.BackColor = Color.LightBlue;
                        break;
                    case SyncFileState.RightOnly:
                        item.BackColor = Color.LightGreen;
                        break;
                    case SyncFileState.LeftNewer:
                        item.BackColor = Color.LightPink;
                        break;
                    case SyncFileState.RightNewer:
                        item.BackColor = Color.LightCoral;
                        break;
                }

                resultListView.Items.Add(item);
            }

            resultListView.EndUpdate();
            UpdateStatus();
        }

        private string GetStateText(SyncFileState state)
        {
            return state switch
            {
                SyncFileState.Equal => "相同",
                SyncFileState.NotEqual => "不同",
                SyncFileState.LeftOnly => "仅左侧",
                SyncFileState.RightOnly => "仅右侧",
                SyncFileState.LeftNewer => "左侧较新",
                SyncFileState.RightNewer => "右侧较新",
                _ => string.Empty
            };
        }

        private void UpdateStatus()
        {
            var stats = new Dictionary<SyncFileState, int>();
            foreach (var file in syncFiles)
            {
                if (!stats.ContainsKey(file.State))
                    stats[file.State] = 0;
                stats[file.State]++;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("总文件数: {0}, ", syncFiles.Count);
            if (stats.ContainsKey(SyncFileState.Equal))
                sb.AppendFormat("相同: {0}, ", stats[SyncFileState.Equal]);
            if (stats.ContainsKey(SyncFileState.NotEqual))
                sb.AppendFormat("不同: {0}, ", stats[SyncFileState.NotEqual]);
            if (stats.ContainsKey(SyncFileState.LeftOnly))
                sb.AppendFormat("仅左侧: {0}, ", stats[SyncFileState.LeftOnly]);
            if (stats.ContainsKey(SyncFileState.RightOnly))
                sb.AppendFormat("仅右侧: {0}, ", stats[SyncFileState.RightOnly]);
            if (stats.ContainsKey(SyncFileState.LeftNewer))
                sb.AppendFormat("左侧较新: {0}, ", stats[SyncFileState.LeftNewer]);
            if (stats.ContainsKey(SyncFileState.RightNewer))
                sb.AppendFormat("右侧较新: {0}", stats[SyncFileState.RightNewer]);

            lblStatus.Text = sb.ToString().TrimEnd(',', ' ');
        }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            resultListView.ListViewItemSorter = new ListViewItemComparer(e.Column);
            resultListView.Sort();
        }

        private async void BtnSync_Click(object sender, EventArgs e)
        {
            var syncForm = new SyncOptionsForm(syncFiles);
            if (syncForm.ShowDialog() == DialogResult.OK)
            {
                btnSync.Enabled = false;
                btnCompare.Enabled = false;
                progressBar.Value = 0;
                progressBar.Maximum = syncForm.SelectedFiles.Count;

                try
                {
                    await SynchronizeFilesAsync(syncForm.SelectedFiles, syncForm.SyncDirection);
                    MessageBox.Show("同步完成", "提示");
                    await CompareDirectoriesAsync();
                    UpdateListView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"同步文件时出错: {ex.Message}", "错误");
                }
                finally
                {
                    btnSync.Enabled = true;
                    btnCompare.Enabled = true;
                }
            }
        }

        private async Task SynchronizeFilesAsync(List<SyncFileInfo> files, SyncDirection direction)
        {
            var progress = 0;
            foreach (var file in files)
            {
                string sourcePath, targetPath;
                if (direction == SyncDirection.LeftToRight)
                {
                    sourcePath = file.LeftPath;
                    targetPath = file.RightPath;
                }
                else
                {
                    sourcePath = file.RightPath;
                    targetPath = file.LeftPath;
                }

                try
                {
                    if (file.IsDirectory)
                    {
                        if (!Directory.Exists(targetPath))
                            Directory.CreateDirectory(targetPath);
                    }
                    else
                    {
                        var targetDir = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir);

                        await Task.Run(() => File.Copy(sourcePath, targetPath, true));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"同步 {file.RelativePath} 失败: {ex.Message}");
                }

                progressBar.Value = ++progress;
                lblStatus.Text = $"正在同步: {progress}/{files.Count}";
                Application.DoEvents();
            }
        }
    }

    public enum SyncDirection
    {
        LeftToRight,
        RightToLeft
    }

    public class ListViewItemComparer : IComparer
    {
        private int column;

        public ListViewItemComparer(int column)
        {
            this.column = column;
        }

        public int Compare(object x, object y)
        {
            var itemX = (ListViewItem)x;
            var itemY = (ListViewItem)y;

            if (column == 2) // 大小列
            {
                var sizeX = itemX.SubItems[column].Text;
                var sizeY = itemY.SubItems[column].Text;

                if (sizeX == "<DIR>" && sizeY == "<DIR>")
                    return 0;
                if (sizeX == "<DIR>")
                    return -1;
                if (sizeY == "<DIR>")
                    return 1;

                return string.Compare(sizeX, sizeY);
            }
            else if (column == 3) // 日期列
            {
                return DateTime.Compare(
                    DateTime.Parse(itemX.SubItems[column].Text),
                    DateTime.Parse(itemY.SubItems[column].Text));
            }
            else // 其他列
            {
                return string.Compare(
                    itemX.SubItems[column].Text,
                    itemY.SubItems[column].Text);
            }
        }
    }
} 