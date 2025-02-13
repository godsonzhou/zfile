using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class Bookmark
    {
        public string Path { get; set; }
        public TreeNode AssociatedNode { get; set; }
        public bool IsLocked { get; set; }
        public bool IsActive { get; set; }
        public Label Control { get; private set; }

        public Bookmark(string path, TreeNode node)
        {
            Path = path;
            AssociatedNode = node;
            IsLocked = false;
            IsActive = false;
            InitializeControl();
        }

        private void InitializeControl()
        {
            Control = new Label
            {
                Text = Path,
                AutoSize = true,
                Padding = new Padding(4),
                Margin = new Padding(2),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            UpdateStyle();
        }

        public void UpdateStyle()
        {
            Control.BackColor = IsActive ? Color.LightBlue : Color.LightGray;
            Control.ForeColor = Color.Black;
			Control.BorderStyle = IsLocked ? BorderStyle.FixedSingle : BorderStyle.None;
            //Control.BackColor = SystemColors.Control;
            //Control.ForeColor = SystemColors.ControlText;
        }
    }

    public class BookmarkManager : IDisposable
    {
        private readonly Form1 form;
        private readonly FlowLayoutPanel leftBookmarkPanel;
        private readonly FlowLayoutPanel rightBookmarkPanel;
        private readonly List<Bookmark> leftBookmarks = new();
        private readonly List<Bookmark> rightBookmarks = new();
        private Bookmark? activeLeftBookmark;
        private Bookmark? activeRightBookmark;
		private bool disposed = false;

        public BookmarkManager(Form1 form, FlowLayoutPanel leftPanel, FlowLayoutPanel rightPanel)
        {
            this.form = form;
            this.leftBookmarkPanel = leftPanel;
            this.rightBookmarkPanel = rightPanel;
            InitializeBookmarkPanels();
        }
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// 释放托管资源
					// 这里不需要释放控件，因为它们是由 Form 管理的
				}

				// 释放非托管资源
				disposed = true;
			}
		}

		~BookmarkManager()
		{
			Dispose(false);
		}
		private void InitializeBookmarkPanels()
        {
            // 配置左侧书签面板
            ConfigureBookmarkPanel(leftBookmarkPanel);
            // 配置右侧书签面板
            ConfigureBookmarkPanel(rightBookmarkPanel);
        }

        private void ConfigureBookmarkPanel(FlowLayoutPanel panel)
        {
            panel.Dock = DockStyle.Top;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.AllowDrop = true;
            panel.DragEnter += BookmarkPanel_DragEnter;
            panel.DragDrop += BookmarkPanel_DragDrop;
        }
		private void Refresh(FlowLayoutPanel panel)
		{
			var l = Helper.GetFlowLayoutPanelLineCount(panel);
			panel.Height = l * 30;
			panel.Refresh();
		}
		public void CreateDefaultBookmarks()
        {
            if (leftBookmarks.Count == 0)
            {
                //var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				var node = form.uiManager.LeftTree.Nodes[0];
				AddBookmark(node.FullPath, node, true);
            }

            if (rightBookmarks.Count == 0)
            {
                //var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				var node = form.uiManager.RightTree.Nodes[0];
				AddBookmark(node.FullPath, node, false);
            }
        }

        public void AddBookmark(string path, TreeNode node, bool isLeft)
        {
            var bookmarkList = isLeft ? leftBookmarks : rightBookmarks;
            var panel = isLeft ? leftBookmarkPanel : rightBookmarkPanel;

            // 检查是否已存在相同路径的书签
            if (bookmarkList.Any(b => b.Path == path)) return;

            var bookmark = new Bookmark(path, node);
            bookmarkList.Add(bookmark);

            // 设置事件处理
            bookmark.Control.MouseClick += (s, e) => BookmarkLabel_MouseClick(bookmark, e, isLeft);
            bookmark.Control.MouseDoubleClick += (s, e) => BookmarkLabel_MouseDoubleClick(bookmark, isLeft);

            panel.Controls.Add(bookmark.Control);
            SetActiveBookmark(bookmark, isLeft);
			Refresh(panel);
        }

        private void BookmarkLabel_MouseClick(Bookmark bookmark, MouseEventArgs e, bool isLeft)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 左键点击 - 激活书签并导航到路径
                SetActiveBookmark(bookmark, isLeft);
                NavigateToBookmark(bookmark, isLeft);
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 右键点击 - 显示上下文菜单
                ShowBookmarkContextMenu(bookmark, isLeft);
            }
        }

        private void BookmarkLabel_MouseDoubleClick(Bookmark bookmark, bool isLeft)
        {
            if (!bookmark.IsLocked)
            {
                RemoveBookmark(bookmark, isLeft);
            }
        }

        private void ShowBookmarkContextMenu(Bookmark bookmark, bool isLeft)
        {
            var menu = new ContextMenuStrip();
            
            var lockItem = new ToolStripMenuItem(bookmark.IsLocked ? "解锁书签" : "锁定书签");
            lockItem.Click += (s, e) => ToggleBookmarkLock(bookmark);
            
            var closeAllItem = new ToolStripMenuItem("关闭所有未锁定书签");
            closeAllItem.Click += (s, e) => RemoveAllUnlockedBookmarks(isLeft);
            
            var swapItem = new ToolStripMenuItem("与另一侧书签交换位置");
            swapItem.Click += (s, e) => SwapBookmarks();

            menu.Items.AddRange(new ToolStripItem[] { lockItem, closeAllItem, swapItem });
            menu.Show(bookmark.Control, new Point(0, bookmark.Control.Height));
        }

        public void ToggleBookmarkLock(Bookmark bookmark)
        {
            bookmark.IsLocked = !bookmark.IsLocked;
            bookmark.UpdateStyle();
        }
		public void ToggleCurrentBookmarkLock(bool isLeft)
		{
			Bookmark bookmark = isLeft ? activeLeftBookmark : activeRightBookmark;
			if (bookmark != null)
			{
				ToggleBookmarkLock(bookmark);
			}
		}

        private void RemoveAllUnlockedBookmarks(bool isLeft)
        {
            var bookmarkList = isLeft ? leftBookmarks : rightBookmarks;
            var panel = isLeft ? leftBookmarkPanel : rightBookmarkPanel;

            for (int i = bookmarkList.Count - 1; i >= 0; i--)
            {
                var bookmark = bookmarkList[i];
                if (!bookmark.IsLocked)
                {
                    panel.Controls.Remove(bookmark.Control);
                    bookmarkList.RemoveAt(i);
                }
            }

            // 如果没有书签了，创建默认书签
            if (bookmarkList.Count == 0)
            {
                CreateDefaultBookmarks();
            }
        }

        private void SwapBookmarks()
        {
            var tempControls = leftBookmarkPanel.Controls.Cast<Control>().ToList();
            leftBookmarkPanel.Controls.Clear();
            leftBookmarkPanel.Controls.AddRange(rightBookmarkPanel.Controls.Cast<Control>().ToArray());
            rightBookmarkPanel.Controls.Clear();
            rightBookmarkPanel.Controls.AddRange(tempControls.ToArray());

            var tempBookmarks = leftBookmarks.ToList();
            leftBookmarks.Clear();
            leftBookmarks.AddRange(rightBookmarks);
            rightBookmarks.Clear();
            rightBookmarks.AddRange(tempBookmarks);

            // 更新活动书签引用
            var tempActive = activeLeftBookmark;
            activeLeftBookmark = activeRightBookmark;
            activeRightBookmark = tempActive;
        }

        private void RemoveBookmark(Bookmark bookmark, bool isLeft)
        {
            if (bookmark.IsLocked) return;

            var bookmarkList = isLeft ? leftBookmarks : rightBookmarks;
            var panel = isLeft ? leftBookmarkPanel : rightBookmarkPanel;

            bookmarkList.Remove(bookmark);
            panel.Controls.Remove(bookmark.Control);
			Refresh(panel);
            // 如果删除的是活动书签，设置新的活动书签
            if (isLeft && activeLeftBookmark == bookmark)
            {
                activeLeftBookmark = bookmarkList.FirstOrDefault();
                if (activeLeftBookmark != null)
                {
                    SetActiveBookmark(activeLeftBookmark, true);
                }
            }
            else if (!isLeft && activeRightBookmark == bookmark)
            {
                activeRightBookmark = bookmarkList.FirstOrDefault();
                if (activeRightBookmark != null)
                {
                    SetActiveBookmark(activeRightBookmark, false);
                }
            }

            // 如果没有书签了，创建默认书签
            if (bookmarkList.Count == 0)
            {
                CreateDefaultBookmarks();
            }
        }

        private void SetActiveBookmark(Bookmark bookmark, bool isLeft)
        {
            var currentActive = isLeft ? activeLeftBookmark : activeRightBookmark;
            if (currentActive != null)
            {
                currentActive.IsActive = false;
                currentActive.UpdateStyle();
            }

            bookmark.IsActive = true;
            bookmark.UpdateStyle();

            if (isLeft)
                activeLeftBookmark = bookmark;
            else
                activeRightBookmark = bookmark;
        }

        private void NavigateToBookmark(Bookmark bookmark, bool isLeft)
        {
            var treeView = isLeft ? form.uiManager.LeftTree : form.uiManager.RightTree;
            var node = form.FindTreeNode(treeView.Nodes, bookmark.Path, true);
            if (node != null)
            {
                treeView.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        public void UpdateActiveBookmark(string newPath, TreeNode newNode, bool isLeft)
        {
            var activeBookmark = isLeft ? activeLeftBookmark : activeRightBookmark;
            
            if (activeBookmark != null && !activeBookmark.IsLocked)
            {
                // 更新现有活动书签
                activeBookmark.Path = newPath;
                activeBookmark.AssociatedNode = newNode;
                activeBookmark.Control.Text = newPath;
            }
            else
            {
                // 创建新书签
                AddBookmark(newPath, newNode, isLeft);
            }
        }
		
        private void BookmarkPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Bookmark)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void BookmarkPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (sender is FlowLayoutPanel targetPanel && e.Data.GetData(typeof(Bookmark)) is Bookmark bookmark)
            {
                var isTargetLeft = targetPanel == leftBookmarkPanel;
                var isSourceLeft = bookmark.Control.Parent == leftBookmarkPanel;

                if (isTargetLeft != isSourceLeft)
                {
                    // 移动书签到另一个面板
                    var sourceList = isSourceLeft ? leftBookmarks : rightBookmarks;
                    var targetList = isTargetLeft ? leftBookmarks : rightBookmarks;

                    sourceList.Remove(bookmark);
                    targetList.Add(bookmark);

                    bookmark.Control.Parent.Controls.Remove(bookmark.Control);
                    targetPanel.Controls.Add(bookmark.Control);

                    // 更新活动书签
                    if (isSourceLeft && activeLeftBookmark == bookmark)
                    {
                        activeLeftBookmark = null;
                        SetActiveBookmark(bookmark, false);
                    }
                    else if (!isSourceLeft && activeRightBookmark == bookmark)
                    {
                        activeRightBookmark = null;
                        SetActiveBookmark(bookmark, true);
                    }
                }
            }
        }
    }
} 