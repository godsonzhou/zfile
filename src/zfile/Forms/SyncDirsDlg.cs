using System.Collections;
using System.Text;
using zfile.Forms;
namespace zfile
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
        public bool IsSelected { get; set; } = true; // 是否选中进行同步
    }

    public partial class SyncDirsDlg : Form
    {
        private string leftPath;
        private string rightPath;
        private bool includeSubDirs = true;
        private bool compareContent = false;
        private string filterPattern = "*.*";
        private List<SyncFileInfo> syncFiles = new();
        private List<SyncFileInfo> filteredFiles = new();
        
        // 显示过滤选项
        private bool showEqual = true;
        private bool showNotEqual = true;
        private bool showLeftOnly = true;
        private bool showRightOnly = true;
        private bool showLeftNewer = true;
        private bool showRightNewer = true;
        private bool showCommon = true;  // 共有的文件
        private bool showUnique = true;  // 独有的文件
        
        private ListView resultListView;
        private TextBox txtLeftPath;
        private TextBox txtRightPath;
        private CheckBox chkSubDirs;
        private CheckBox chkContent;
        private ComboBox cmbFilter;
        private Button btnCompare;
        private Button btnSync;
        private Button btnClose;
        private ProgressBar progressBar;
        private Label lblStatus;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusTotal;
        private ToolStripStatusLabel lblStatusEqual;
        private ToolStripStatusLabel lblStatusNotEqual;
        private ToolStripStatusLabel lblStatusLeftOnly;
        private ToolStripStatusLabel lblStatusRightOnly;
        private ToolStripStatusLabel lblStatusLeftNewer;
        private ToolStripStatusLabel lblStatusRightNewer;
        
        // 显示过滤按钮
        private CheckBox chkShowEqual;
        private CheckBox chkShowNotEqual;
        private CheckBox chkShowLeftOnly;
        private CheckBox chkShowRightOnly;
        private CheckBox chkShowLeftNewer;
        private CheckBox chkShowRightNewer;
        private CheckBox chkShowCommon;
        private CheckBox chkShowUnique;

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
            this.Size = new Size(1000, 730);
            this.StartPosition = FormStartPosition.CenterScreen;

			//不允许调整大小
			this.FormBorderStyle = FormBorderStyle.FixedDialog;

			// 创建路径和选项控件
			txtLeftPath = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(400, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = leftPath
            };

            txtRightPath = new TextBox
            {
                Location = new Point((int)(this.Width * 0.5), 10),
                Size = new Size(400, 23),
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
            
            // 过滤器
            var lblFilter = new Label
            {
                Text = "过滤:",
                Location = new Point(230, 70),
                Size = new Size(40, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            cmbFilter = new ComboBox
            {
                Location = new Point(275, 70),
                Size = new Size(135, 23),
                Text = "*.*"
            };
            cmbFilter.Items.AddRange(new object[] { "*.*", "*.txt", "*.zip", "*.exe", "*.dll" });

            btnCompare = new Button
            {
                Text = "比较",
                Location = new Point(420, 10),
                Size = new Size(75, 23)
            };

            btnSync = new Button
            {
                Text = "同步",
                Location = new Point(420, 40),
                Size = new Size(75, 23),
                Enabled = false
            };

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(420, 70),
                Size = new Size(75, 23)
            };

            // 创建显示过滤按钮
            var filterPanel = new Panel
            {
                Location = new Point(10, 100),
                Size = new Size(965, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            chkShowEqual = new CheckBox { Text = "=", Checked = true, Width = 50 };
            chkShowNotEqual = new CheckBox { Text = "<>", Checked = true, Width = 50 };
            chkShowLeftOnly = new CheckBox { Text = ">", Checked = true, Width = 50 };
            chkShowRightOnly = new CheckBox { Text = "<", Checked = true, Width = 50 };
            chkShowLeftNewer = new CheckBox { Text = "=>", Checked = true, Width = 50 };
            chkShowRightNewer = new CheckBox { Text = "<=", Checked = true, Width = 50 };
            chkShowCommon = new CheckBox { Text = "共有的", Checked = true, Width = 70 };
            chkShowUnique = new CheckBox { Text = "独有的", Checked = true, Width = 70 };
            
            var filterControls = new Control[] { 
                chkShowEqual, chkShowNotEqual, chkShowLeftOnly, chkShowRightOnly, 
                chkShowLeftNewer, chkShowRightNewer, chkShowCommon, chkShowUnique 
            };
            
            for (int i = 0; i < filterControls.Length; i++)
            {
                filterControls[i].Location = new Point(10 + i * (filterControls[i].Width + 10), 5);
                filterPanel.Controls.Add(filterControls[i]);
            }

            // 创建ListView
            resultListView = new ListView
            {
                Location = new Point(10, 140),
                Size = new Size(965, 450),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // 左侧文件信息
            resultListView.Columns.Add("左侧文件名", 150);
            resultListView.Columns.Add("大小", 80);
            resultListView.Columns.Add("修改时间", 130);
            
            // 状态列
            resultListView.Columns.Add("<=>", 40);
            
            // 右侧文件信息
            resultListView.Columns.Add("右侧文件名", 150);
            resultListView.Columns.Add("大小", 80);
            resultListView.Columns.Add("修改时间", 130);
            
            // 路径列
            resultListView.Columns.Add("路径", 200);

            // 创建状态栏
            statusStrip = new StatusStrip { SizingGrip = false };
            lblStatusTotal = new ToolStripStatusLabel();
            lblStatusEqual = new ToolStripStatusLabel();
            lblStatusNotEqual = new ToolStripStatusLabel();
            lblStatusLeftOnly = new ToolStripStatusLabel();
            lblStatusRightOnly = new ToolStripStatusLabel();
            lblStatusLeftNewer = new ToolStripStatusLabel();
            lblStatusRightNewer = new ToolStripStatusLabel();
            
            statusStrip.Items.AddRange(new ToolStripItem[] {
                lblStatusTotal,
                new ToolStripSeparator(),
                lblStatusEqual,
                new ToolStripSeparator(),
                lblStatusNotEqual,
                new ToolStripSeparator(),
                lblStatusLeftOnly,
                new ToolStripSeparator(),
                lblStatusRightOnly,
                new ToolStripSeparator(),
                lblStatusLeftNewer,
                new ToolStripSeparator(),
                lblStatusRightNewer
            });

            progressBar = new ProgressBar
            {
                Location = new Point(10, 600),
                Size = new Size(965, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            lblStatus = new Label
            {
                Location = new Point(10, 630),
                Size = new Size(965, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // 添加控件到窗体
            this.Controls.AddRange([
                txtLeftPath, txtRightPath, chkSubDirs, chkContent, lblFilter, cmbFilter,
                btnCompare, btnSync, btnClose, filterPanel,
                resultListView, progressBar, lblStatus
            ]);
            
            this.Controls.Add(statusStrip);
        }

        private void InitializeEvents()
        {
            btnCompare.Click += BtnCompare_Click;
            btnSync.Click += BtnSync_Click;
            btnClose.Click += (s, e) => this.Close();
            resultListView.ColumnClick += ListView_ColumnClick;
            resultListView.MouseClick += ResultListView_MouseClick;
            cmbFilter.TextChanged += CmbFilter_TextChanged;
            
            // 过滤按钮事件
            chkShowEqual.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowNotEqual.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowLeftOnly.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowRightOnly.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowLeftNewer.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowRightNewer.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowCommon.CheckedChanged += FilterCheckBox_CheckedChanged;
            chkShowUnique.CheckedChanged += FilterCheckBox_CheckedChanged;
        }
        
        private void ResultListView_MouseClick(object? sender, MouseEventArgs e)
        {
            var hitTestInfo = resultListView.HitTest(e.X, e.Y);
            if (hitTestInfo.Item != null && hitTestInfo.SubItem != null)
            {
                // 检查是否点击了状态列（第4列，索引为3）
                if (hitTestInfo.Item.SubItems.Count > 3 && hitTestInfo.SubItem == hitTestInfo.Item.SubItems[3])
                {
                    int index = hitTestInfo.Item.Index;
                    if (index < filteredFiles.Count)
                    {
                        // 切换选中状态
                        var fileInfo = filteredFiles[index];
                        fileInfo.IsSelected = !fileInfo.IsSelected;
                        
                        // 更新显示
                        if (fileInfo.IsSelected)
                        {
                            hitTestInfo.Item.SubItems[3].Text = GetStateSymbol(fileInfo.State);
                        }
                        else
                        {
                            hitTestInfo.Item.SubItems[3].Text = "";
                        }
                    }
                }
            }
        }
        
        private void CmbFilter_TextChanged(object? sender, EventArgs e)
        {
            filterPattern = cmbFilter.Text.Trim();
            if (string.IsNullOrEmpty(filterPattern))
            {
                filterPattern = "*.*";
            }
            UpdateListView();
        }
        
        private void FilterCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            showEqual = chkShowEqual.Checked;
            showNotEqual = chkShowNotEqual.Checked;
            showLeftOnly = chkShowLeftOnly.Checked;
            showRightOnly = chkShowRightOnly.Checked;
            showLeftNewer = chkShowLeftNewer.Checked;
            showRightNewer = chkShowRightNewer.Checked;
            showCommon = chkShowCommon.Checked;
            showUnique = chkShowUnique.Checked;
            
            UpdateListView();
        }

        private async void BtnCompare_Click(object? sender, EventArgs e)
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
            
            // 应用过滤器
            filteredFiles = ApplyFilters();

            foreach (var file in filteredFiles)
            {
                // 左侧文件信息
                string leftFileName = file.State == SyncFileState.RightOnly ? "" : Path.GetFileName(file.RelativePath);
                string leftSize = file.State == SyncFileState.RightOnly ? "" : 
                    (file.IsDirectory ? "<DIR>" : FileSystemManager.FormatFileSize(file.Size));
                string leftTime = file.State == SyncFileState.RightOnly ? "" : 
                    file.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 右侧文件信息
                string rightFileName = file.State == SyncFileState.LeftOnly ? "" : Path.GetFileName(file.RelativePath);
                
                // 右侧大小和时间需要从右侧文件获取，但当前代码中没有保存右侧文件的这些信息
                // 这里简化处理，使用左侧的信息
                string rightSize = file.State == SyncFileState.LeftOnly ? "" : 
                    (file.IsDirectory ? "<DIR>" : FileSystemManager.FormatFileSize(file.Size));
                string rightTime = file.State == SyncFileState.LeftOnly ? "" : 
                    file.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                
                var item = new ListViewItem(leftFileName);
                item.SubItems.Add(leftSize);
                item.SubItems.Add(leftTime);
                
                // 状态列
                item.SubItems.Add(file.IsSelected ? GetStateSymbol(file.State) : "");
                
                // 右侧信息
                item.SubItems.Add(rightFileName);
                item.SubItems.Add(rightSize);
                item.SubItems.Add(rightTime);
                
                // 路径
                item.SubItems.Add(Path.GetDirectoryName(file.RelativePath));

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
        
        private List<SyncFileInfo> ApplyFilters()
        {
            var result = new List<SyncFileInfo>();
            
            foreach (var file in syncFiles)
            {
                // 应用文件名过滤
                if (filterPattern != "*.*")
                {
                    if (!FileSystemManager.MatchesPattern(file.RelativePath, filterPattern))
                    {
                        continue;
                    }
                }
                
                // 应用状态过滤
                bool isCommon = file.State == SyncFileState.Equal || 
                               file.State == SyncFileState.NotEqual || 
                               file.State == SyncFileState.LeftNewer || 
                               file.State == SyncFileState.RightNewer;
                               
                bool isUnique = file.State == SyncFileState.LeftOnly || 
                               file.State == SyncFileState.RightOnly;
                
                // 检查是否显示该状态的文件
                if ((file.State == SyncFileState.Equal && !showEqual) ||
                    (file.State == SyncFileState.NotEqual && !showNotEqual) ||
                    (file.State == SyncFileState.LeftOnly && !showLeftOnly) ||
                    (file.State == SyncFileState.RightOnly && !showRightOnly) ||
                    (file.State == SyncFileState.LeftNewer && !showLeftNewer) ||
                    (file.State == SyncFileState.RightNewer && !showRightNewer) ||
                    (isCommon && !showCommon) ||
                    (isUnique && !showUnique))
                {
                    continue;
                }
                
                result.Add(file);
            }
            
            return result;
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
        
        private string GetStateSymbol(SyncFileState state)
        {
            return state switch
            {
                SyncFileState.Equal => "=",
                SyncFileState.NotEqual => "<>",
                SyncFileState.LeftOnly => ">",
                SyncFileState.RightOnly => "<",
                SyncFileState.LeftNewer => "=>",
                SyncFileState.RightNewer => "<=",
                _ => string.Empty
            };
        }

        private void UpdateStatus()
        {
            var stats = new Dictionary<SyncFileState, int>();
            var selectedStats = new Dictionary<SyncFileState, int>();
            
            // 计算所有文件的统计信息
            foreach (var file in syncFiles)
            {
                if (!stats.ContainsKey(file.State))
                    stats[file.State] = 0;
                stats[file.State]++;
            }
            
            // 计算已选中文件的统计信息
            foreach (var file in filteredFiles)
            {
                if (file.IsSelected)
                {
                    if (!selectedStats.ContainsKey(file.State))
                        selectedStats[file.State] = 0;
                    selectedStats[file.State]++;
                }
            }

            // 更新状态栏
            lblStatusTotal.Text = $"总文件数: {syncFiles.Count} (已选: {filteredFiles.Count(f => f.IsSelected)})";
            
            lblStatusEqual.Text = stats.ContainsKey(SyncFileState.Equal) ? 
                $"相同: {stats[SyncFileState.Equal]}" : "相同: 0";
                
            lblStatusNotEqual.Text = stats.ContainsKey(SyncFileState.NotEqual) ? 
                $"不同: {stats[SyncFileState.NotEqual]}" : "不同: 0";
                
            lblStatusLeftOnly.Text = stats.ContainsKey(SyncFileState.LeftOnly) ? 
                $"仅左侧: {stats[SyncFileState.LeftOnly]}" : "仅左侧: 0";
                
            lblStatusRightOnly.Text = stats.ContainsKey(SyncFileState.RightOnly) ? 
                $"仅右侧: {stats[SyncFileState.RightOnly]}" : "仅右侧: 0";
                
            lblStatusLeftNewer.Text = stats.ContainsKey(SyncFileState.LeftNewer) ? 
                $"左侧较新: {stats[SyncFileState.LeftNewer]}" : "左侧较新: 0";
                
            lblStatusRightNewer.Text = stats.ContainsKey(SyncFileState.RightNewer) ? 
                $"右侧较新: {stats[SyncFileState.RightNewer]}" : "右侧较新: 0";

            // 更新底部状态标签
            var sb = new StringBuilder();
            sb.AppendFormat("显示: {0}/{1}, ", filteredFiles.Count, syncFiles.Count);
            sb.AppendFormat("已选: {0}, ", filteredFiles.Count(f => f.IsSelected));
            sb.AppendFormat("过滤: {0}", filterPattern);

            lblStatus.Text = sb.ToString();
        }

        private void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            resultListView.ListViewItemSorter = new ListViewItemComparer(e.Column);
            resultListView.Sort();
        }

        private async void BtnSync_Click(object? sender, EventArgs e)
        {
            // 获取选中的文件
            var selectedFiles = filteredFiles.Where(f => f.IsSelected).ToList();
            
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("请至少选择一个文件进行同步", "提示");
                return;
            }
            
            var syncForm = new SyncOptionsForm(selectedFiles);
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

            if (column == 1) // 大小列
            {
                var sizeX = itemX.SubItems[column].Text;
                var sizeY = itemY.SubItems[column].Text;

                if (sizeX == "<DIR>" && sizeY == "<DIR>")
                    return 0;
                if (sizeX == "<DIR>")
                    return -1;
                if (sizeY == "<DIR>")
                    return 1;

                return int.Parse(sizeX) - int.Parse(sizeY);
            }
            else if (column == 2 || column == 6) // 日期列
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