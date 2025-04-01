using System;
using System.Windows.Forms;

namespace Zfile.Forms
{
    partial class IdmForm
    {
        // 设计器变量
        private MenuStrip menuStrip;
        private ToolStripMenuItem taskMenuItem;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem downloadMenuItem;
        private ToolStripMenuItem viewMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStrip toolStrip;
        private ToolStripButton newTaskButton;
        private ToolStripButton resumeButton;
        private ToolStripButton pauseButton;
        private ToolStripButton stopAllButton;
        private ToolStripButton resumeAllButton;
        private ToolStripButton deleteTaskButton;
        private ToolStripButton deleteAllButton;
        private ToolStripButton scheduleButton;
        private ToolStripButton startQueueButton;
        private ToolStripButton stopQueueButton;
        private ToolStripButton optionsButton;
        private SplitContainer splitContainer;
        private TreeView categoryTreeView;
        private ListView downloadListView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        
        // 下载列表右键菜单
        private ContextMenuStrip downloadListContextMenu;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem openWithMenuItem;
        private ToolStripMenuItem openFolderMenuItem;
        private ToolStripMenuItem moveRenameMenuItem;
        private ToolStripMenuItem redownloadMenuItem;
        private ToolStripMenuItem resumeDownloadMenuItem;
        private ToolStripMenuItem stopDownloadMenuItem;
        private ToolStripMenuItem copyUrlMenuItem;
        private ToolStripMenuItem removeMenuItem;
        private ToolStripMenuItem moveToQueueMenuItem;
        private ToolStripMenuItem removeFromQueueMenuItem;
        private ToolStripMenuItem backupMenuItem;
        private ToolStripMenuItem propertiesMenuItem;
    }
}