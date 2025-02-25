using System.Collections.Generic;
using System.Diagnostics;
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
            Path = Helper.getFSpath(path);
            AssociatedNode = node;
            IsLocked = false;
            IsActive = false;
            InitializeControl();
        }

        private void InitializeControl()
        {
            Control = new Label
            {
                Text = IsLocked ? "*" : "" + Path.Split('\\', StringSplitOptions.RemoveEmptyEntries)[^1],
                AutoSize = true,
                Padding = new Padding(2),
                Margin = new Padding(1),
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
			var pathpart = Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
			Control.Text = (IsLocked ? "*" : "") + pathpart[pathpart.Length-1];
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
		private System.Windows.Forms.Timer clickTimer;
		private bool isDoubleClick;
		private bool isLeft;
		private DateTime lastClickTime;
		private string lastClickButton = string.Empty;
		private Bookmark lastClickBookmark;
		private List<Bookmark> GetBookmarkList(bool isleft)
		{
			return isleft ? leftBookmarks : rightBookmarks;
		}

        private Bookmark? activeLeftBookmark;
        private Bookmark? activeRightBookmark;
		private Bookmark? activeBookmark(bool isleft)
		{
			return isleft ? activeLeftBookmark : activeRightBookmark;
		}
		private bool disposed = false;

        public BookmarkManager(Form1 form, FlowLayoutPanel leftPanel, FlowLayoutPanel rightPanel)
        {
            this.form = form;
            this.leftBookmarkPanel = leftPanel;
            this.rightBookmarkPanel = rightPanel;
            InitializeBookmarkPanels();
			// 订阅鼠标点击事件
			//leftBookmarkPanel.MouseDown += bookmarkPanel_MouseDown;

			// 初始化定时器，用于处理单击延迟
			clickTimer = new System.Windows.Forms.Timer();
			clickTimer.Interval = SystemInformation.DoubleClickTime;
			clickTimer.Tick += ClickTimer_Tick;
		}
		private void bookmarkPanel_MouseDown(object sender, MouseEventArgs e, bool isleft, Bookmark bookmark)
		{
			isLeft = isleft;
			var _clickButton =  e.Button == MouseButtons.Left ? "Left" : "Right";
			// 计算当前点击和上次点击的时间间隔
			TimeSpan interval = DateTime.Now - lastClickTime;
			if (interval.TotalMilliseconds < SystemInformation.DoubleClickTime && _clickButton == lastClickButton)
			{
				// 判定为双击
				isDoubleClick = true;
				clickTimer.Stop();
				OnDoubleClick(sender, e, isleft, bookmark);
			}
			else
			{
				// 可能是单击，启动定时器等待确认
				isDoubleClick = false;
				lastClickTime = DateTime.Now;
				lastClickButton = _clickButton;
				//var bl = GetBookmarkList(isleft);
				lastClickBookmark = bookmark;
				clickTimer.Start();
			}
		}
		private void ClickTimer_Tick(object sender, EventArgs e)
		{
			//MouseEventArgs me = e as MouseEventArgs;
			clickTimer.Stop();
			if (!isDoubleClick)
			{
				// 确认是单击
				OnSingleClick(lastClickBookmark, lastClickButton);
			}
		}

		private void OnSingleClick(Bookmark bookmark, string clickButton)
		{
			//Debug.Print("这是鼠标单击事件");
			//var bookmark = (Bookmark)sender;
			//if (e.Button == MouseButtons.Left)
			if(clickButton.Equals("Left"))
			{
				// 左键点击 - 激活书签并导航到路径
				SetActiveBookmark(bookmark, isLeft);
				NavigateToBookmark(bookmark, isLeft);
			}
			//else if (e.Button == MouseButtons.Right)
			else if(clickButton.Equals("Right"))
			{
				// 右键点击 - 显示上下文菜单
				ShowBookmarkContextMenu(bookmark, isLeft);
			}
		}

		private void OnDoubleClick(object sender, MouseEventArgs e, bool isLeft, Bookmark bookmark)
		{
			//Debug.Print("这是鼠标双击事件");
			//Bookmark bookmark = (Bookmark)sender;
			if (!bookmark.IsLocked && e.Button == MouseButtons.Left)
			{
				RemoveBookmark(bookmark, isLeft);
			}
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
					UnregisterEvent(leftBookmarkPanel);
					UnregisterEvent(rightBookmarkPanel);
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
            //panel.FlowDirection = FlowDirection.LeftToRight;
            //panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.AllowDrop = true;
            panel.DragEnter += BookmarkPanel_DragEnter;
            panel.DragDrop += BookmarkPanel_DragDrop;
			panel.MouseDoubleClick += BookmarkPanel_MouseDoubleClick;
        }
		private void UnregisterEvent(FlowLayoutPanel panel)
		{
			panel.DragEnter -= BookmarkPanel_DragEnter;
			panel.DragDrop -= BookmarkPanel_DragDrop;
			panel.MouseDoubleClick -= BookmarkPanel_MouseDoubleClick;

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
				AddBookmark(node, true);
            }

            if (rightBookmarks.Count == 0)
            {
                //var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				var node = form.uiManager.RightTree.Nodes[0];
				AddBookmark(node, false);
            }
        }

        public void AddBookmark(TreeNode node, bool isLeft)
        {
            var bookmarkList = isLeft ? leftBookmarks : rightBookmarks;
            var panel = isLeft ? leftBookmarkPanel : rightBookmarkPanel;
			var path = node.FullPath;
			// 检查是否已存在相同路径的书签
			//if (bookmarkList.Any(b => b.Path == path)) return;

			var bookmark = new Bookmark(path, node);
            bookmarkList.Add(bookmark);

			// 设置事件处理
			bookmark.Control.MouseClick += (s, e) => bookmarkPanel_MouseDown(s, e, isLeft, bookmark);//(s, e) => BookmarkLabel_MouseClick(bookmark, e, isLeft);
            bookmark.Control.MouseDoubleClick += (s, e) => bookmarkPanel_MouseDown(s, e, isLeft, bookmark);//(s, e) => BookmarkLabel_MouseDoubleClick(bookmark, isLeft, e);

			panel.Controls.Add(bookmark.Control);
            SetActiveBookmark(bookmark, isLeft);
			Refresh(panel);
        }

        //private void BookmarkLabel_MouseClick(Bookmark bookmark, MouseEventArgs e, bool isLeft)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        // 左键点击 - 激活书签并导航到路径
        //        SetActiveBookmark(bookmark, isLeft);
        //        NavigateToBookmark(bookmark, isLeft);
        //    }
        //    else if (e.Button == MouseButtons.Right)
        //    {
        //        // 右键点击 - 显示上下文菜单
        //        ShowBookmarkContextMenu(bookmark, isLeft);
        //    }
        //}

        //private void BookmarkLabel_MouseDoubleClick(Bookmark bookmark, bool isLeft, MouseEventArgs e)
        //{
        //    if (!bookmark.IsLocked && e.Button == MouseButtons.Left)
        //    {
        //        RemoveBookmark(bookmark, isLeft);
        //    }
        //}

        private void ShowBookmarkContextMenu(Bookmark bookmark, bool isLeft)
        {
            var menu = new ContextMenuStrip();
            
            var lockItem = new ToolStripMenuItem(bookmark.IsLocked ? "解锁书签" : "锁定书签");
            lockItem.Click += (s, e) => ToggleBookmarkLock(bookmark);
            
            var closeAllItem = new ToolStripMenuItem("关闭其他所有未锁定书签");
            closeAllItem.Click += (s, e) => RemoveAllUnlockedBookmarks(isLeft);
            
            var swapItem = new ToolStripMenuItem("与另一侧书签交换位置");
            swapItem.Click += (s, e) => SwapBookmarks();

			var closeItem = new ToolStripMenuItem("关闭当前书签");
			closeItem.Click += (s, e) => RemoveBookmark(bookmark, isLeft);

			menu.Items.AddRange(new ToolStripItem[] { lockItem, closeAllItem, swapItem, closeItem });
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

			var bookmarkList = GetBookmarkList(isLeft);
			if (bookmarkList.Count < 2) return;
			var panel = isLeft ? leftBookmarkPanel : rightBookmarkPanel;
			var idx = Math.Max(bookmarkList.IndexOf(bookmark) - 1, 0);

			bookmarkList.Remove(bookmark);
            panel.Controls.Remove(bookmark.Control);
			Refresh(panel);

			// 如果删除的是活动书签，设置新的活动书签
			if (activeBookmark(isLeft) == bookmark)
			{
				SetActiveBookmark(bookmarkList[idx], isLeft);
				NavigateToBookmark(bookmarkList[idx], isLeft);
			}
            // 如果没有书签了，创建默认书签
            if (bookmarkList.Count == 0)
                CreateDefaultBookmarks();
		}

        private void SetActiveBookmark(Bookmark bookmark, bool isLeft)
        {
			if(bookmark == null) return;
			var currentActive = isLeft ? activeLeftBookmark : activeRightBookmark;
			if (bookmark == currentActive) return;
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
            var treeview = isLeft ? form.uiManager.LeftTree : form.uiManager.RightTree;
            var node = form.FindTreeNode(form.thispc.Nodes, bookmark.Path);
			//var node = bookmark.AssociatedNode;//bugfix: the associatedNode has been removed from the treeview will lead to exception
			if (node != null)
            {
                treeview.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        public void UpdateActiveBookmark(string newPath, TreeNode newNode, bool isLeft)
        {
            var activeBookmark = isLeft ? activeLeftBookmark : activeRightBookmark;

			if (activeBookmark != null )
            {
				if (activeBookmark.Path == newPath)
					return;
				if (activeBookmark.IsLocked)
					// 创建新书签
					AddBookmark(newNode, isLeft);
				else
				{
					// 更新现有活动书签
					activeBookmark.Path = newPath;
					activeBookmark.AssociatedNode = newNode;
					activeBookmark.Control.Text = activeBookmark.IsLocked ? "*" : "" + newPath.Split('\\', StringSplitOptions.RemoveEmptyEntries)[^1];
				}
            }
        }
		
		private void BookmarkPanel_MouseDoubleClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				//Debug.Print("BookmarkPanel_MouseDoubleClick");
				var panel = (FlowLayoutPanel)sender;
				isLeft = panel == leftBookmarkPanel;
				AddBookmark(form.activeTreeview.SelectedNode, isLeft);
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