using System.Drawing.Imaging;
using System.Text;
using System.Drawing.Printing;
//using FastColoredTextBoxNS;

namespace zfile.Forms
{
	public partial class frmViewer : Form
	{
		// 常量定义
		private const int HOTKEYS_CATEGORY = 0;
		private const int SBP_FILENAME = 4;
		private const int SBP_FILENR = 0;
		private const int SBP_POSITION = 1;
		private const int SBP_FILESIZE = 2;
		private const int SBP_TEXTENCODING = 3;
		private const int SBP_PLUGINNAME = 1;
		private const int SBP_CURRENTRESOLUTION = 1;
		private const int SBP_FULLRESOLUTION = 2;
		private const int SBP_IMAGESELECTION = 3;
		private const int CELL_SIZE = 8;

		// 文件列表和状态变量
		private List<string> fileList = new List<string>();
		private int activeFileIndex = 0;
		private bool isAnimation = false;
		private bool isImage = false;
		private bool isPlugin = false;
		private bool isQuickView = false;
		private bool isMDFlag = false;
		private bool isImgEdit = false;
		private int tmpX, tmpY, startX, startY, endX, endY;
		private int undoSX, undoSY, undoEX, undoEY;
		private int cas = 0;
		private int i_timer = 0;
		private int zoomFactor = 100;
		private int pluginEncoding = 0;
		private int mode = 0;

		// 图像相关
		private Bitmap originalBitmap;
		private Bitmap currentBitmap;
		private Bitmap tmpAll;
		private GifAnimator gifAnimator;

		// 控件
		private StatusStrip statusBar;
		private ToolStripStatusLabel fileNameStatus;
		private ToolStripStatusLabel fileNrStatus;
		private ToolStripStatusLabel positionStatus;
		private ToolStripStatusLabel fileSizeStatus;
		private ToolStripStatusLabel textEncodingStatus;
		private ToolStripStatusLabel pluginNameStatus;
		private ToolStripStatusLabel currentResolutionStatus;
		private ToolStripStatusLabel fullResolutionStatus;
		private ToolStripStatusLabel imageSelectionStatus;

		private PictureBox imageBox;
		private Panel pnlImage;
		private Panel pnlText;
		private Panel pnlCode;
		private Panel pnlFolder;
		private SplitContainer splitContainer;
		private DataGridView drawPreview;

		private MenuStrip mainMenu;
		private ToolStripMenuItem miFile;
		private ToolStripMenuItem miPrev;
		private ToolStripMenuItem miNext;
		private ToolStripMenuItem miExit;
		private ToolStripMenuItem miView;
		private ToolStripMenuItem miImage;
		private ToolStripMenuItem miStretch;
		private ToolStripMenuItem miStretchOnlyLarge;
		private ToolStripMenuItem miCenter;
		private ToolStripMenuItem miText;
		private ToolStripMenuItem miBin;
		private ToolStripMenuItem miHex;
		private ToolStripMenuItem miAbout;
		private ToolStripMenuItem miAbout2;
		private ToolStripMenuItem miDiv1;
		private ToolStripMenuItem miSearch;
		private ToolStripMenuItem miDiv2;
		private ToolStripMenuItem miGraphics;
		private ToolStripMenuItem miEdit;
		private ToolStripMenuItem miSelectAll;
		private ToolStripMenuItem miCopyToClipboard;
		private ToolStripMenuItem miDiv3;
		private ToolStripMenuItem miOffice;
		private ToolStripMenuItem miEncoding;
		private ToolStripMenuItem miPlugins;
		private ToolStripMenuItem miSeparator;
		private ContextMenuStrip pmEditMenu;
		private ToolStripMenuItem pmiCopy;
		private ToolStripMenuItem pmiSelectAll;
		private ToolStripMenuItem pmiCopyFormatted;
		private ToolStripMenuItem miDiv5;
		private ToolStripMenuItem miDiv4;
		private ToolStripMenuItem miPreview;
		private ToolStripMenuItem miScreenshot;
		private ToolStripMenuItem miFullScreen;
		private ToolStripMenuItem miSave;
		private ToolStripMenuItem miSaveAs;
		private ToolStripMenuItem miZoomIn;
		private ToolStripMenuItem miZoomOut;
		private ToolStripMenuItem miRotate;
		private ToolStripMenuItem mi270;
		private ToolStripMenuItem mi180;
		private ToolStripMenuItem mi90;
		private ToolStripMenuItem miGotoLine;
		private ToolStripMenuItem miSearchPrev;
		private ToolStripMenuItem miPrint;
		private ToolStripMenuItem miSearchNext;
		private ToolStripMenuItem miReload;
		private ToolStripMenuItem miLookBook;
		private ToolStripMenuItem miScreenshotImmediately;
		private ToolStripMenuItem miScreenshot3sec;
		private ToolStripMenuItem miScreenshot5sec;
		private ToolStripMenuItem miDec;
		private ToolStripMenuItem miAutoReload;
		private ToolStripMenuItem miCode;
		private ToolStripMenuItem miShowTransparency;
		private ToolStripMenuItem miWrapText;
		private ToolStripMenuItem miPen;
		private ToolStripMenuItem miRect;
		private ToolStripMenuItem miEllipse;
		private ToolStripMenuItem miShowCaret;
		private ToolStripMenuItem miPrintSetup;

		private ToolStrip toolBar;
		private ToolStripButton btnReload;
		private ToolStripButton btnGrayscale;
		private ToolStripButton btnBrightness;
		private ToolStripButton btnContrast;
		private ToolStripButton btnCutToImage;
		private ToolStripButton btn270;
		private ToolStripButton btn90;
		private ToolStripButton btnMirror;
		private ToolStripButton btnCutTuImage;
		private ToolStripButton btnRedEye;
		private ToolStripSeparator btnPaintSeparator;
		private ToolStripButton btnUndo;
		private ToolStripButton btnPenMode;
		private ToolStripSeparator btnGifSeparator;
		private ToolStripButton btnGifMove;
		private ToolStripButton btnPrevGifFrame;
		private ToolStripButton btnNextGifFrame;
		private ToolStripButton btnGifToBmp;
		private ToolStripButton btnPenWidth;
		private ToolStripButton btnPrev;
		private ToolStripButton btnNext;
		private ToolStripButton btnCopyFile;
		private ToolStripButton btnMoveFile;
		private ToolStripButton btnDeleteFile;
		private ToolStripSeparator btnSeparator;
		private ToolStripButton btnSlideShow;
		private ToolStripButton btnFullScreen;
		private ToolStripButton btnResize;
		private ToolStripButton btnPaint;
		private ToolStripSeparator btnZoomSeparator;
		private ToolStripButton btnZoomIn;
		private ToolStripButton btnZoomOut;
		private ToolStripSeparator btnHightlightSeparator;
		private ToolStripButton btnHightlight;

		private SaveFileDialog savePictureDialog;
		private System.Windows.Forms.Timer timerReload;
		private System.Windows.Forms.Timer timerScreenshot;
		private System.Windows.Forms.Timer timerViewer;

		private string fileName = "";
		private Size thumbnailSize = new Size(128, 128);
		private List<Image> thumbnails = new List<Image>();
		private bool isFullScreen = false;
		private FormWindowState previousWindowState;
		private Rectangle previousWindowBounds;
		private WlxModuleList? _wlxmodulelist;

		// 新增控件和变量声明（补全缺失部分）
		private TextBox txtContent; // 文本查看器
		private RichTextBox codeBox; // 代码查看器
		private Panel panelText, panelCode, panelTools; // 文本/代码/工具栏面板
		private StatusStrip statusStrip; // 状态栏
		private ToolStripStatusLabel statusFileName, statusFileSize, statusFileType, statusImageSize; // 状态栏项
		private ListView listViewFolder; // 文件夹浏览
		private int currentPrintLine = 0; // 打印时的行计数
		private bool MDFlag = false; // 鼠标绘制标志
		private int UndoSX, UndoSY, UndoEX, UndoEY; // 撤销用坐标

		// 面板类型枚举
		private enum PanelType { Image, Text, Code, Folder }

		// 绘图模式类型
		private enum DrawMode { None, Pen, Rect, Ellipse, Highlight }
		private DrawMode currentDrawMode = DrawMode.None;

		// 多步撤销栈
		private Stack<Bitmap> undoStack = new Stack<Bitmap>();

		private void PushUndo()
		{
			if (currentBitmap != null)
			{
				// 限制撤销栈大小，防止内存溢出
				if (undoStack.Count > 20) undoStack.Clear();
				undoStack.Push(new Bitmap(currentBitmap));
				btnUndo.Enabled = true;
			}
		}

		public frmViewer(List<string> filesToView, WlxModuleList wlx, bool quickView = false)
		{
			_wlxmodulelist = wlx;
			InitializeComponent();
			InitializeViewer(filesToView, quickView);
		}

		private void InitializeComponent()
		{
			// 初始化窗体
			this.Text = "Double Commander Viewer";
			this.Size = new Size(800, 600);
			this.FormClosing += FrmViewer_FormClosing;
			this.KeyDown += FrmViewer_KeyDown;
			this.Resize += FrmViewer_Resize;
			pnlFolder = new Panel { Dock = DockStyle.Fill, Visible = false };

			// 初始化panelTools
			toolBar = new ToolStrip { Dock = DockStyle.Top, Height = 40 };
			this.Controls.Add(toolBar);

			// 初始化panelText
			pnlText = new Panel { Dock = DockStyle.Fill, Visible = false };
			this.Controls.Add(pnlText);

			// 初始化panelCode
			pnlCode = new Panel { Dock = DockStyle.Fill, Visible = false };
			this.Controls.Add(pnlCode);

			// 初始化txtContent
			txtContent = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10) };
			pnlText.Controls.Add(txtContent);

			// 初始化codeBox（需要FastColoredTextBox库）
			//codeBox = new FastColoredTextBoxNS.FastColoredTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
			pnlCode.Controls.Add(codeBox);

			// 初始化listViewFolder
			listViewFolder = new ListView { Dock = DockStyle.Fill, View = View.Details };
			listViewFolder.Columns.Add("名称", 200);
			listViewFolder.Columns.Add("大小", 100);
			listViewFolder.Columns.Add("类型", 80);
			listViewFolder.Columns.Add("修改时间", 150);
			listViewFolder.FullRowSelect = true;
			listViewFolder.DoubleClick += listViewFolder_DoubleClick;
			pnlFolder.Controls.Add(listViewFolder);

			// 初始化状态栏及其项
			statusBar = new StatusStrip();
			fileNameStatus = new ToolStripStatusLabel();
			fileNrStatus = new ToolStripStatusLabel();
			positionStatus = new ToolStripStatusLabel();
			fileSizeStatus = new ToolStripStatusLabel();
			textEncodingStatus = new ToolStripStatusLabel();
			pluginNameStatus = new ToolStripStatusLabel();
			currentResolutionStatus = new ToolStripStatusLabel();
			fullResolutionStatus = new ToolStripStatusLabel();
			imageSelectionStatus = new ToolStripStatusLabel();
			statusFileType = new ToolStripStatusLabel();
			statusImageSize = new ToolStripStatusLabel();
			statusBar.Items.AddRange(new ToolStripItem[] {
				fileNrStatus, positionStatus, fileSizeStatus, textEncodingStatus, fileNameStatus,
				pluginNameStatus, currentResolutionStatus, fullResolutionStatus, imageSelectionStatus, statusFileType, statusImageSize
			});

			this.Controls.Add(statusBar);

			// 初始化图片显示面板
			pnlImage = new Panel { Dock = DockStyle.Fill, Visible = false };
			imageBox = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
			pnlImage.Controls.Add(imageBox);
			this.Controls.Add(pnlImage);

			// 初始化文本显示面板
			pnlText = new Panel { Dock = DockStyle.Fill, Visible = false };
			// 这里添加文本查看控件
			this.Controls.Add(pnlText);

			// 初始化代码显示面板
			pnlCode = new Panel { Dock = DockStyle.Fill, Visible = false };
			// 这里添加代码查看控件
			this.Controls.Add(pnlCode);

			// 初始化文件夹显示面板
			pnlFolder = new Panel { Dock = DockStyle.Fill, Visible = false };
			// 这里添加文件夹内容显示控件
			this.Controls.Add(pnlFolder);

			// 初始化菜单
			mainMenu = new MenuStrip();
			miFile = new ToolStripMenuItem("文件(&F)");
			miPrev = new ToolStripMenuItem("上一个(&P)", null, (s, e) => cm_LoadPrevFile());
			miNext = new ToolStripMenuItem("下一个(&N)", null, (s, e) => cm_LoadNextFile());
			miExit = new ToolStripMenuItem("退出(&X)", null, (s, e) => Close());
			miSearchNext = new ToolStripMenuItem("查找下一个", null);
			miSearchPrev = new ToolStripMenuItem("查找上一个", null);
			miView = new ToolStripMenuItem("查看(&V)");
			// 其他菜单项初始化...

			mainMenu.Items.Add(miFile);
			mainMenu.Items.Add(miView);
			this.Controls.Add(mainMenu);
			this.MainMenuStrip = mainMenu;

			// 初始化工具栏
			toolBar = new ToolStrip();
			btnPrev = new ToolStripButton("上一个", null, (s, e) => cm_LoadPrevFile());
			btnNext = new ToolStripButton("下一个", null, (s, e) => cm_LoadNextFile());
			btnReload = new ToolStripButton("重新加载", null, (s, e) => cm_Reload());
			// 其他工具栏按钮初始化...

			// 初始化图片编辑相关按钮
			btnGrayscale = new ToolStripButton("灰度", null, btnGrayscale_Click) { ToolTipText = "灰度" };
			btnBrightness = new ToolStripButton("亮度+", null, btnBrightness_Click) { ToolTipText = "增加亮度" };
			btnContrast = new ToolStripButton("对比度+", null, btnContrast_Click) { ToolTipText = "增加对比度" };
			btnCutToImage = new ToolStripButton("裁剪", null, btnCutToImage_Click) { ToolTipText = "裁剪选区" };
			btnRedEye = new ToolStripButton("红眼", null, btnRedEye_Click) { ToolTipText = "去红眼" };
			btnUndo = new ToolStripButton("撤销", null, btnUndo_Click) { ToolTipText = "撤销" };
			btnPenMode = new ToolStripButton("画笔", null, btnPenMode_Click) { ToolTipText = "画笔模式", CheckOnClick = true };
			btnHightlight = new ToolStripButton("高亮", null, null) { ToolTipText = "高亮模式", CheckOnClick = true };
			btnHightlight.Click += btnHightlight_Click;

			// 初始化绘图工具相关按钮
			btnRect = new ToolStripButton("矩形", null, btnRect_Click) { ToolTipText = "矩形工具", CheckOnClick = true };
			btnEllipse = new ToolStripButton("椭圆", null, btnEllipse_Click) { ToolTipText = "椭圆工具", CheckOnClick = true };
			btnPenColor = new ToolStripButton("颜色", null, btnPenColor_Click) { ToolTipText = "画笔颜色" };
			toolBar.Items.Add(btnPrev);
			toolBar.Items.Add(btnNext);
			toolBar.Items.Add(btnReload);
			toolBar.Items.Add(btnGrayscale);
			toolBar.Items.Add(btnBrightness);
			toolBar.Items.Add(btnContrast);
			toolBar.Items.Add(btnCutToImage);
			toolBar.Items.Add(btnRedEye);
			toolBar.Items.Add(btnUndo);
			toolBar.Items.Add(btnPenMode);
			toolBar.Items.Add(btnHightlight);
			toolBar.Items.Add(btnRect);
			toolBar.Items.Add(btnEllipse);
			toolBar.Items.Add(btnPenColor);
			this.Controls.Add(toolBar);

			// 初始化预览面板
			splitContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 600 };
			drawPreview = new DataGridView
			{
				Dock = DockStyle.Fill,
				RowTemplate = new DataGridViewRow(),
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
				Visible = false
			};
			splitContainer.Panel2.Controls.Add(drawPreview);
			this.Controls.Add(splitContainer);

			// 初始化对话框
			savePictureDialog = new SaveFileDialog
			{
				Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|位图|*.bmp|GIF 图片|*.gif|图标|*.ico|所有文件|*.*"
			};

			// 初始化定时器
			timerReload = new System.Windows.Forms.Timer { Interval = 5000 };
			timerReload.Tick += TimerReload_Tick;

			timerScreenshot = new System.Windows.Forms.Timer();
			timerScreenshot.Tick += TimerScreenshot_Tick;

			timerViewer = new System.Windows.Forms.Timer { Interval = 1000 };
			timerViewer.Tick += TimerViewer_Tick;

			// 菜单项和快捷键绑定
			miSearchNext.ShortcutKeys = Keys.F3;
			miSearchNext.Click += (s, e) => FindNext();

			miSearchPrev.ShortcutKeys = Keys.Shift | Keys.F3;
			miSearchPrev.Click += (s, e) => FindPrev();
			miGotoLine = new ToolStripMenuItem("跳转到行");
			miGotoLine.ShortcutKeys = Keys.Control | Keys.G;
			miGotoLine.Click += (s, e) => {
				string input = Microsoft.VisualBasic.Interaction.InputBox("跳转到行号：", "跳转行", "1");
				if (int.TryParse(input, out int lineNum))
				{
					GotoLine(lineNum);
				}
			};
			miSelectAll = new ToolStripMenuItem("选中全部");
			miSelectAll.ShortcutKeys = Keys.Control | Keys.A;
			miSelectAll.Click += (s, e) => SelectAll();
			miCopyToClipboard = new ToolStripMenuItem("复制到剪贴板");
			miCopyToClipboard.ShortcutKeys = Keys.Control | Keys.C;
			miCopyToClipboard.Click += (s, e) => Copy();
			miBin = new ToolStripMenuItem("二进制");
			miHex = new ToolStripMenuItem("十六进制");
			miDec = new ToolStripMenuItem("十进制");
			miOffice = new ToolStripMenuItem("Office");
			miPlugins = new ToolStripMenuItem("插件");
			miCode = new ToolStripMenuItem("代码");
			// 菜单项绑定高级模式切换
			miBin.Click += (s, e) => ShowAsBin();
			miHex.Click += (s, e) => ShowAsHex();
			miDec.Click += (s, e) => ShowAsDec();
			miOffice.Click += (s, e) => ShowOffice();
			miPlugins.Click += (s, e) => ShowPlugins();
			miCode.Click += (s, e) => ShowCode();

			// 查找框
			txtFind = new TextBox { Width = 120, Location = new Point(10, 40) };
			this.Controls.Add(txtFind);

			// 画笔颜色按钮
			btnPenColor = new ToolStripButton("颜色", null, btnPenColor_Click) { ToolTipText = "画笔颜色" };
			toolBar.Items.Add(btnPenColor);

			// 打印文档
			printDocument = new PrintDocument();
			printDocument.PrintPage += printDocument_PrintPage;
		}

		private void cm_SearchNext()
		{
			throw new NotImplementedException();
		}

		private void FrmViewer_FormClosing(object? sender, FormClosingEventArgs e)
		{
			// 资源释放和设置保存
			// 释放缩略图
			foreach (var thumb in thumbnails)
			{
				thumb?.Dispose();
			}
			thumbnails.Clear();

			currentBitmap?.Dispose();
			originalBitmap?.Dispose();
			gifAnimator?.Dispose();
		}

		private void TimerReload_Tick(object? sender, EventArgs e)
		{
			// 自动重载当前文件
			cm_Reload();
		}

		private void InitializeViewer(List<string> filesToView, bool quickView)
		{
			isQuickView = quickView;
			fileList = filesToView;
			activeFileIndex = 0;

			// 加载第一个文件
			LoadFile(0);

			// 初始化预览
			if (fileList.Count > 0)
			{
				drawPreview.Rows.Clear();
				drawPreview.RowCount = fileList.Count;
				//CreateThumbnails();
			}
		}
		private void LoadFile(int index)
		{
			if (index < 0 || index >= fileList.Count) return;

			activeFileIndex = index;
			fileName = fileList[index];

			// 更新状态栏
			fileNameStatus.Text = fileName;
			fileNrStatus.Text = $"{index + 1}/{fileList.Count}";
			LoadFile(fileName);
		}
		private void LoadFile(string file)
		{
			// 检查文件类型并加载
			string ext = Path.GetExtension(fileName).ToLower();

			if (Directory.Exists(fileName))
			{
				LoadFolder(fileName);
				return;
			}

			// 图片文件
			string[] imageExts = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".ico" };
			if (imageExts.Contains(ext))
			{
				LoadImage(fileName);
				return;
			}

			// 文本文件
			string[] textExts = { ".txt", ".log", ".ini", ".cfg", ".xml", ".json", ".csv" };
			if (textExts.Contains(ext))
			{
				LoadText(fileName);
				return;
			}

			// 代码文件
			string[] codeExts = { ".cs", ".cpp", ".h", ".java", ".py", ".js", ".html", ".css", ".php" };
			if (codeExts.Contains(ext))
			{
				LoadText(fileName);
				return;
			}

			// 默认加载为文本
			LoadText(fileName);
		}

		//private void LoadImage(string filePath)
		//{
		//	if (filePath == null || !File.Exists(filePath)) return;

		//	activeFileIndex = fileList.IndexOf(filePath);
		//	fileName = filePath;

		//	// 更新状态栏
		//	fileNameStatus.Text = filePath;
		//	fileNrStatus.Text = $"{activeFileIndex + 1}/{fileList.Count}";

		//	// 检查文件类型并加载
		//	string ext = Path.GetExtension(filePath).ToLower();

		//	if (Directory.Exists(filePath))
		//	{
		//		LoadFolder(filePath);
		//		return;
		//	}

		//	// 图片文件
		//	string[] imageExts = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".ico" };
		//	if (imageExts.Contains(ext))
		//	{
		//		LoadImage(filePath);
		//		return;
		//	}

		//	// 文本文件
		//	string[] textExts = { ".txt", ".log", ".ini", ".cfg", ".xml", ".json", ".csv" };
		//	if (textExts.Contains(ext))
		//	{
		//		LoadText(filePath);
		//		return;
		//	}

		//	// 代码文件
		//	string[] codeExts = { ".cs", ".cpp", ".h", ".java", ".py", ".js", ".html", ".css", ".php" };
		//	if (codeExts.Contains(ext))
		//	{
		//		LoadCode(filePath);
		//		return;
		//	}

		//	// 默认加载为文本
		//	LoadText(filePath);
		//}

		private void LoadImage(string filePath)
		{
			// 清除之前的图像
			imageBox.Image?.Dispose();

			// 加载新图像
			if (Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
			{
				gifAnimator = new GifAnimator(filePath);
				imageBox.Image = gifAnimator.GetNextFrame();
				isAnimation = true;
				isImage = false;

				// 启动GIF动画
				timerViewer.Interval = gifAnimator.FrameDelay;
				timerViewer.Start();
			}
			else
			{
				currentBitmap = new Bitmap(filePath);
				originalBitmap = new Bitmap(currentBitmap);
				imageBox.Image = currentBitmap;
				isAnimation = false;
				isImage = true;
			}

			// 更新状态栏
			currentResolutionStatus.Text = $"{imageBox.Image.Width}x{imageBox.Image.Height}";
			fullResolutionStatus.Text = currentResolutionStatus.Text;

			// 激活图片面板
			ActivatePanel(PanelType.Image);
			UpdateStatusBarForImage();
		}

		private void LoadText(string filePath)
		{
			try
			{
				// 读取文本内容
				string content = File.ReadAllText(filePath, GetEncoding(filePath));
				txtContent.Text = content;

				// 更新状态栏
				var fileInfo = new FileInfo(filePath);
				fileNameStatus.Text = Path.GetFileName(filePath);
				fileSizeStatus.Text = $"{fileInfo.Length:N0} bytes";
				statusFileType.Text = "Text";
				statusImageSize.Text = $"{content.Length:N0} chars";

				// 激活文本面板
				ActivatePanel(PanelType.Text);
				UpdateStatusBarForText();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading text file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		//private void LoadCode(string filePath)
		//{
		//	try
		//	{
		//		// 读取代码内容
		//		string content = File.ReadAllText(filePath, GetEncoding(filePath));
		//		codeBox.Text = content;

		//		// 根据文件扩展名设置语法高亮
		//		string extension = Path.GetExtension(filePath).ToLower();
		//		switch (extension)
		//		{
		//			case ".cs":
		//				codeBox.Language = FastColoredTextBoxNS.Language.CSharp;
		//				break;
		//			case ".vb":
		//				codeBox.Language = FastColoredTextBoxNS.Language.VB;
		//				break;
		//			case ".js":
		//				codeBox.Language = FastColoredTextBoxNS.Language.JS;
		//				break;
		//			case ".html":
		//				codeBox.Language = FastColoredTextBoxNS.Language.HTML;
		//				break;
		//			case ".xml":
		//				codeBox.Language = FastColoredTextBoxNS.Language.XML;
		//				break;
		//			case ".sql":
		//				codeBox.Language = FastColoredTextBoxNS.Language.SQL;
		//				break;
		//			case ".lua":
		//				codeBox.Language = FastColoredTextBoxNS.Language.Lua;
		//				break;
		//			case ".php":
		//				codeBox.Language = FastColoredTextBoxNS.Language.PHP;
		//				break;
		//			default:
		//				codeBox.Language = FastColoredTextBoxNS.Language.Custom;
		//				break;
		//		}

		//		// 更新状态栏
		//		var fileInfo = new FileInfo(filePath);
		//		fileNameStatus.Text = Path.GetFileName(filePath);
		//		fileSizeStatus.Text = $"{fileInfo.Length:N0} bytes";
		//		statusFileType.Text = "Code";
		//		statusImageSize.Text = $"{content.Length:N0} chars, {codeBox.LinesCount} lines";

		//		// 激活代码面板
		//		ActivatePanel(PanelType.Code);
		//		UpdateStatusBarForCode();
		//	}
		//	catch (Exception ex)
		//	{
		//		MessageBox.Show($"Error loading code file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//	}
		//}

		private Encoding GetEncoding(string filePath)
		{
			try
			{
				// 读取文件的前几个字节来检测编码
				using (var reader = new StreamReader(filePath, Encoding.Default, true))
				{
					reader.Peek(); // 触发编码检测
					return reader.CurrentEncoding;
				}
			}
			catch
			{
				// 如果检测失败，返回默认编码
				return Encoding.Default;
			}
		}

		private void btnFind_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(txtFind.Text)) return;

			if (pnlText.Visible)
			{
				// 在文本中查找
				int start = txtContent.SelectionStart + txtContent.SelectionLength;
				int index = txtContent.Text.IndexOf(txtFind.Text, start, StringComparison.CurrentCultureIgnoreCase);

				if (index == -1 && start > 0)
				{
					// 从头开始搜索
					index = txtContent.Text.IndexOf(txtFind.Text, 0, StringComparison.CurrentCultureIgnoreCase);
				}

				if (index != -1)
				{
					txtContent.Select(index, txtFind.Text.Length);
					txtContent.ScrollToCaret();
				}
			}
			else if (pnlCode.Visible)
			{
				// 在代码中查找
				//var range = codeBox.Range;
				//range.ClearStyle(FastColoredTextBoxNS.StyleIndex.All);

				//var matches = range.GetRanges(txtFind.Text);
				//if (matches.Count > 0)
				{
					// 高亮所有匹配项
					//foreach (var match in matches)
					{
						//match.SetStyle(FastColoredTextBoxNS.TextStyle.YellowSemiTransparent);
					}

					// 滚动到第一个匹配项
					//codeBox.Selection = matches[0];
					//codeBox.DoSelectionVisible();
				}
			}
		}

		private void LoadFolder(string folderPath)
		{
			try
			{
				// 清空列表视图
				listViewFolder.Items.Clear();

				// 获取文件夹信息
				var dirInfo = new DirectoryInfo(folderPath);

				// 添加父目录
				if (dirInfo.Parent != null)
				{
					var item = listViewFolder.Items.Add("..");
					item.SubItems.Add("");
					item.SubItems.Add("<DIR>");
					item.SubItems.Add(dirInfo.Parent.LastWriteTime.ToString());
					item.Tag = dirInfo.Parent.FullName;
				}

				// 添加子目录
				foreach (var dir in dirInfo.GetDirectories())
				{
					var item = listViewFolder.Items.Add(dir.Name);
					item.SubItems.Add("");
					item.SubItems.Add("<DIR>");
					item.SubItems.Add(dir.LastWriteTime.ToString());
					item.Tag = dir.FullName;
				}

				// 添加文件
				foreach (var file in dirInfo.GetFiles())
				{
					var item = listViewFolder.Items.Add(file.Name);
					item.SubItems.Add(FormatFileSize(file.Length));
					item.SubItems.Add(file.Extension.ToUpper());
					item.SubItems.Add(file.LastWriteTime.ToString());
					item.Tag = file.FullName;
				}

				// 更新状态栏
				fileNameStatus.Text = Path.GetFileName(folderPath);
				fileSizeStatus.Text = $"{listViewFolder.Items.Count:N0} items";
				statusFileType.Text = "Folder";
				statusImageSize.Text = "";

				// 激活文件夹面板
				ActivatePanel(PanelType.Folder);
				UpdateStatusBarForFolder(folderPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private string FormatFileSize(long bytes)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
			int suffixIndex = 0;
			double size = bytes;

			while (size >= 1024 && suffixIndex < suffixes.Length - 1)
			{
				size /= 1024;
				suffixIndex++;
			}

			return $"{size:N2} {suffixes[suffixIndex]}";
		}

		private void listViewFolder_DoubleClick(object sender, EventArgs e)
		{
			if (listViewFolder.SelectedItems.Count == 0) return;

			var selectedPath = listViewFolder.SelectedItems[0].Tag as string;
			if (selectedPath == null) return;

			if (Directory.Exists(selectedPath))
			{
				// 加载选中的文件夹
				LoadFolder(selectedPath);
			}
			else if (File.Exists(selectedPath))
			{
				// 加载选中的文件
				LoadFile(selectedPath);
			}
		}

		private void btnPrint_Click(object sender, EventArgs e)
		{
			if (!isImage && !pnlText.Visible && !pnlCode.Visible) return;

			using (var printDialog = new PrintDialog())
			{
				printDialog.Document = printDocument;
				if (printDialog.ShowDialog() == DialogResult.OK)
				{
					printDocument.Print();
				}
			}
		}

		private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
		{
			if (isImage && currentBitmap != null)
			{
				// 打印图像
				var rect = e.MarginBounds;
				var ratio = Math.Min((float)rect.Width / currentBitmap.Width,
									(float)rect.Height / currentBitmap.Height);

				var width = (int)(currentBitmap.Width * ratio);
				var height = (int)(currentBitmap.Height * ratio);

				var x = rect.X + (rect.Width - width) / 2;
				var y = rect.Y + (rect.Height - height) / 2;

				e.Graphics.DrawImage(currentBitmap, x, y, width, height);
			}
			else if (pnlText.Visible || pnlCode.Visible)
			{
				// 打印文本
				string text = pnlText.Visible ? txtContent.Text : codeBox.Text;
				var font = new Font("Consolas", 10);
				var brush = Brushes.Black;
				var rect = e.MarginBounds;

				// 计算每页能显示的行数
				int linesPerPage = (int)(rect.Height / font.GetHeight(e.Graphics));
				var lines = text.Split('\n');

				// 打印当前页的文本
				for (int i = 0; i < linesPerPage && currentPrintLine < lines.Length; i++)
				{
					var line = lines[currentPrintLine++].TrimEnd('\r');
					e.Graphics.DrawString(line, font, brush,
						rect.Left, rect.Top + i * font.GetHeight(e.Graphics));
				}

				// 检查是否还有更多页需要打印
				e.HasMorePages = currentPrintLine < lines.Length;

				// 如果打印完成，重置行计数器
				if (!e.HasMorePages)
				{
					currentPrintLine = 0;
				}
			}
		}
		private void ActivatePanel(Panel panel)
		{
			foreach(var pnl in new[] { pnlImage, pnlText, pnlCode, pnlFolder })
			{
				pnl.Visible = false;
			}
			panel.Visible = true;
		}
		private void ActivatePanel(PanelType type)
		{
			switch (type)
			{
				case PanelType.Image:
					ActivatePanel(pnlImage);
					break;
				case PanelType.Text:
					ActivatePanel(pnlText);
					break;
				case PanelType.Code:
					ActivatePanel(pnlCode);
					break;
				case PanelType.Folder:
					ActivatePanel(pnlFolder);
					break;
			}
		}

		private void AdjustImageSize()
		{
			if (imageBox.Image == null) return;

			double scale = zoomFactor / 100.0;
			int newWidth = (int)(imageBox.Image.Width * scale);
			int newHeight = (int)(imageBox.Image.Height * scale);

			// 保持宽高比
			double aspectRatio = (double)imageBox.Image.Width / imageBox.Image.Height;

			if (miStretch.Checked || miStretchOnlyLarge.Checked)
			{
				double containerAspect = (double)pnlImage.Width / pnlImage.Height;

				if (aspectRatio > containerAspect)
				{
					newWidth = pnlImage.Width;
					newHeight = (int)(pnlImage.Width / aspectRatio);
				}
				else
				{
					newHeight = pnlImage.Height;
					newWidth = (int)(pnlImage.Height * aspectRatio);
				}

				if (miStretchOnlyLarge.Checked &&
					imageBox.Image.Width <= pnlImage.Width &&
					imageBox.Image.Height <= pnlImage.Height)
				{
					newWidth = imageBox.Image.Width;
					newHeight = imageBox.Image.Height;
				}
			}

			imageBox.Size = new Size(newWidth, newHeight);

			if (miCenter.Checked)
			{
				imageBox.Left = (pnlImage.Width - imageBox.Width) / 2;
				imageBox.Top = (pnlImage.Height - imageBox.Height) / 2;
			}
			else
			{
				imageBox.Left = 0;
				imageBox.Top = 0;
			}

			// 更新状态栏
			currentResolutionStatus.Text = $"{newWidth}x{newHeight} ({zoomFactor}%)";
			fullResolutionStatus.Text = $"{imageBox.Image.Width}x{imageBox.Image.Height} (100%)";
		}

		private void RotateImage(int degrees)
		{
			if (currentBitmap == null) return;

			Bitmap rotatedBitmap = new Bitmap(currentBitmap.Width, currentBitmap.Height);

			using (Graphics g = Graphics.FromImage(rotatedBitmap))
			{
				g.TranslateTransform(currentBitmap.Width / 2, currentBitmap.Height / 2);
				g.RotateTransform(degrees);
				g.TranslateTransform(-currentBitmap.Width / 2, -currentBitmap.Height / 2);
				g.DrawImage(currentBitmap, Point.Empty);
			}

			currentBitmap.Dispose();
			currentBitmap = rotatedBitmap;
			imageBox.Image = currentBitmap;

			AdjustImageSize();
			UpdateStatusBarForImage();
		}

		private void MirrorImage(bool vertically = false)
		{
			if (currentBitmap == null) return;

			Bitmap mirroredBitmap = new Bitmap(currentBitmap.Width, currentBitmap.Height);

			using (Graphics g = Graphics.FromImage(mirroredBitmap))
			{
				if (vertically)
				{
					g.DrawImage(currentBitmap,
						new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height),
						new Rectangle(0, currentBitmap.Height, currentBitmap.Width, -currentBitmap.Height),
						GraphicsUnit.Pixel);
				}
				else
				{
					g.DrawImage(currentBitmap,
						new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height),
						new Rectangle(currentBitmap.Width, 0, -currentBitmap.Width, currentBitmap.Height),
						GraphicsUnit.Pixel);
				}
			}

			currentBitmap.Dispose();
			currentBitmap = mirroredBitmap;
			imageBox.Image = currentBitmap;

			AdjustImageSize();
			UpdateStatusBarForImage();
		}

		private void SaveImageAs()
		{
			if (savePictureDialog.ShowDialog() == DialogResult.OK)
			{
				string ext = Path.GetExtension(savePictureDialog.FileName).ToLower();
				ImageFormat format = ImageFormat.Png;

				switch (ext)
				{
					case ".jpg":
					case ".jpeg":
						format = ImageFormat.Jpeg;
						break;
					case ".bmp":
						format = ImageFormat.Bmp;
						break;
					case ".gif":
						format = ImageFormat.Gif;
						break;
					case ".ico":
						format = ImageFormat.Icon;
						break;
				}

				imageBox.Image.Save(savePictureDialog.FileName, format);
			}
		}

		private void CreateThumbnails()
		{
			thumbnails.Clear();
			drawPreview.Rows.Clear();

			foreach (string file in fileList)
			{
				try
				{
					using (var img = Image.FromFile(file))
					{
						var thumb = new Bitmap(thumbnailSize.Width, thumbnailSize.Height);
						using (var g = Graphics.FromImage(thumb))
						{
							g.DrawImage(img, new Rectangle(Point.Empty, thumbnailSize));
						}
						thumbnails.Add(thumb);
					}
				}
				catch
				{
					// 无法创建缩略图时使用默认图标
					thumbnails.Add(null);
				}
			}

			drawPreview.RowCount = thumbnails.Count;
		}

		private void cm_Reload()
		{
			LoadFile(activeFileIndex);
		}

		private void cm_LoadNextFile()
		{
			activeFileIndex = (activeFileIndex + 1) % fileList.Count;
			LoadFile(activeFileIndex);
		}

		private void cm_LoadPrevFile()
		{
			activeFileIndex = (activeFileIndex - 1 + fileList.Count) % fileList.Count;
			LoadFile(activeFileIndex);
		}

		private void cm_DeleteFile()
		{
			if (fileList.Count <= 1) return;

			if (MessageBox.Show($"确定要删除文件 {Path.GetFileName(fileList[activeFileIndex])} 吗?",
				"确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				File.Delete(fileList[activeFileIndex]);
				fileList.RemoveAt(activeFileIndex);

				if (activeFileIndex >= fileList.Count)
					activeFileIndex = fileList.Count - 1;

				LoadFile(activeFileIndex);
			}
		}

		private void cm_Rotate90()
		{
			RotateImage(90);
		}

		private void cm_Rotate180()
		{
			RotateImage(180);
		}

		private void cm_Rotate270()
		{
			RotateImage(270);
		}

		private void cm_MirrorHorz()
		{
			MirrorImage();
		}

		private void cm_MirrorVert()
		{
			MirrorImage(true);
		}

		private void cm_ZoomIn()
		{
			zoomFactor = Math.Min(zoomFactor + 10, 500);
			AdjustImageSize();
		}

		private void cm_ZoomOut()
		{
			zoomFactor = Math.Max(zoomFactor - 10, 10);
			AdjustImageSize();
		}

		private void cm_Screenshot()
		{
			try
			{
				// 创建截图
				var screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
											Screen.PrimaryScreen.Bounds.Height);

				using (var g = Graphics.FromImage(screenshot))
				{
					g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
				}

				// 保存截图
				var saveDialog = new SaveFileDialog
				{
					Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
					FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
				};

				if (saveDialog.ShowDialog() == DialogResult.OK)
				{
					screenshot.Save(saveDialog.FileName);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error taking screenshot: {ex.Message}", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void cm_ScreenshotWithDelay()
		{
			// 启动截图定时器
			timerScreenshot.Interval = 3000; // 3秒延迟
			timerScreenshot.Start();

			// 最小化窗口
			this.WindowState = FormWindowState.Minimized;
		}

		//private void TimerScreenshot_Tick(object sender, EventArgs e)
		//{
		//	// 停止定时器
		//	timerScreenshot.Stop();

		//	// 恢复窗口
		//	this.WindowState = FormWindowState.Normal;

		//	// 执行截图
		//	cm_Screenshot();
		//}

		private void cm_Fullscreen()
		{
			if (!isImage) return;

			if (this.FormBorderStyle == FormBorderStyle.None)
			{
				// 退出全屏
				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.WindowState = FormWindowState.Normal;
				this.TopMost = false;

				// 恢复控件可见性
				toolBar.Visible = true;
				statusBar.Visible = true;
			}
			else
			{
				// 进入全屏
				this.FormBorderStyle = FormBorderStyle.None;
				this.WindowState = FormWindowState.Maximized;
				this.TopMost = true;

				// 隐藏控件
				toolBar.Visible = false;
				statusBar.Visible = false;
			}

			// 调整图像大小
			AdjustImageSize();
		}

		private void cm_ExitViewer()
		{
			this.Close();
		}

		private void FrmViewer_Resize(object sender, EventArgs e)
		{
			AdjustImageSize();
		}

		private void FrmViewer_KeyDown(object sender, KeyEventArgs e)
		{
			// 快捷键处理
			switch (e.KeyCode)
			{
				case Keys.Escape when isFullScreen:
					cm_Fullscreen();
					break;

				case Keys.Right:
					cm_LoadNextFile();
					break;

				case Keys.Left:
					cm_LoadPrevFile();
					break;

				case Keys.Add:
				case Keys.Oemplus:
					cm_ZoomIn();
					break;

				case Keys.Subtract:
				case Keys.OemMinus:
					cm_ZoomOut();
					break;

				case Keys.F5:
					cm_Reload();
					break;

				case Keys.Delete:
					cm_DeleteFile();
					break;

				case Keys.F11:
					cm_Fullscreen();
					break;

				case Keys.F when e.Control:
					// 查找功能
					break;
			}
		}

		private void TimerScreenshot_Tick(object sender, EventArgs e)
		{
			timerScreenshot.Stop();
			cm_Screenshot();
		}

		private void TimerViewer_Tick(object sender, EventArgs e)
		{
			if (isAnimation && gifAnimator != null)
			{
				imageBox.Image = gifAnimator.GetNextFrame();
			}
		}

		private void ImageBox_MouseWheel(object sender, MouseEventArgs e)
		{
			if (e.Delta > 0)
			{
				cm_ZoomIn();
			}
			else
			{
				cm_ZoomOut();
			}
		}

		private void DrawPreview_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.RowIndex < fileList.Count)
			{
				LoadFile(fileList[e.RowIndex]);
			}
		}

		private void DrawPreview_CellPaint(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex >= 0 && e.RowIndex < thumbnails.Count)
			{
				var thumb = thumbnails[e.RowIndex];
				if (thumb != null)
				{
					e.Graphics.DrawImage(thumb, e.CellBounds);
					e.Handled = true;
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			// 清理资源
			foreach (var thumb in thumbnails)
			{
				thumb?.Dispose();
			}
			thumbnails.Clear();

			currentBitmap?.Dispose();
			originalBitmap?.Dispose();
			gifAnimator?.Dispose();
		}

		// 1. 新增拖拽模式枚举和变量
		private enum SelectionDragMode { None, Move, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
		private SelectionDragMode currentDragMode = SelectionDragMode.None;
		private Point dragOffset; // 拖动时的偏移
		private ToolStripButton btnRect;
		private ToolStripButton btnEllipse;
		private ToolStripButton btnPenColor;
		private TextBox txtFind;
		private PrintDocument printDocument;
		private const int HANDLE_SIZE = 8; // 手柄大小

		// 2. 辅助方法：判断鼠标在哪个手柄/边/内部
		private SelectionDragMode HitTestSelection(Point pt)
		{
			var rect = GetSelectionRect();
			Rectangle[] handles = new Rectangle[] {
				new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE), // 左上
				new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE), // 右上
				new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE), // 左下
				new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE), // 右下
			};
			if (handles[0].Contains(pt)) return SelectionDragMode.TopLeft;
			if (handles[1].Contains(pt)) return SelectionDragMode.TopRight;
			if (handles[2].Contains(pt)) return SelectionDragMode.BottomLeft;
			if (handles[3].Contains(pt)) return SelectionDragMode.BottomRight;
			// 边
			if (Math.Abs(pt.X - rect.Left) <= HANDLE_SIZE && pt.Y > rect.Top && pt.Y < rect.Bottom) return SelectionDragMode.Left;
			if (Math.Abs(pt.X - rect.Right) <= HANDLE_SIZE && pt.Y > rect.Top && pt.Y < rect.Bottom) return SelectionDragMode.Right;
			if (Math.Abs(pt.Y - rect.Top) <= HANDLE_SIZE && pt.X > rect.Left && pt.X < rect.Right) return SelectionDragMode.Top;
			if (Math.Abs(pt.Y - rect.Bottom) <= HANDLE_SIZE && pt.X > rect.Left && pt.X < rect.Right) return SelectionDragMode.Bottom;
			// 内部
			if (rect.Contains(pt)) return SelectionDragMode.Move;
			return SelectionDragMode.None;
		}

		// 3. 鼠标按下
		private void imageBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (!isImage) return;
			if (btnHightlight.Checked && e.Button == MouseButtons.Left)
			{
				var pt = e.Location;
				var mode = HitTestSelection(pt);
				var rect = GetSelectionRect();
				currentDragMode = mode;
				if (mode == SelectionDragMode.None)
				{
					// 新建选区
					startX = endX = pt.X;
					startY = endY = pt.Y;
					currentDragMode = SelectionDragMode.BottomRight;
				}
				else if (mode == SelectionDragMode.Move)
				{
					dragOffset = new Point(pt.X - rect.Left, pt.Y - rect.Top);
				}
				else if (mode != SelectionDragMode.None)
				{
					dragOffset = pt;
				}
				MDFlag = true;
				imageBox.Capture = true;
			}
			else
			{
				// 开始绘制
				if (btnPenMode.Checked)
				{
					if (tmpAll == null)
					{
						tmpAll = new Bitmap(currentBitmap);
					}
				}
			}
		}

		// 4. 鼠标移动
		private void imageBox_MouseMove(object sender, MouseEventArgs e)
		{
			if (!isImage) return;
			var pt = e.Location;
			var rect = GetSelectionRect();
			// 鼠标指针变化
			if (!MDFlag && btnHightlight.Checked)
			{
				var mode = HitTestSelection(pt);
				switch (mode)
				{
					case SelectionDragMode.TopLeft:
					case SelectionDragMode.BottomRight:
						imageBox.Cursor = Cursors.SizeNWSE; break;
					case SelectionDragMode.TopRight:
					case SelectionDragMode.BottomLeft:
						imageBox.Cursor = Cursors.SizeNESW; break;
					case SelectionDragMode.Left:
					case SelectionDragMode.Right:
						imageBox.Cursor = Cursors.SizeWE; break;
					case SelectionDragMode.Top:
					case SelectionDragMode.Bottom:
						imageBox.Cursor = Cursors.SizeNS; break;
					case SelectionDragMode.Move:
						imageBox.Cursor = Cursors.SizeAll; break;
					default:
						imageBox.Cursor = Cursors.Cross; break;
				}
			}
			if (MDFlag && btnHightlight.Checked)
			{
				int minX = 0, minY = 0, maxX = imageBox.Image.Width, maxY = imageBox.Image.Height;
				switch (currentDragMode)
				{
					case SelectionDragMode.TopLeft:
						startX = Math.Max(minX, Math.Min(e.X, endX - 1));
						startY = Math.Max(minY, Math.Min(e.Y, endY - 1));
						break;
					case SelectionDragMode.TopRight:
						endX = Math.Min(maxX, Math.Max(e.X, startX + 1));
						startY = Math.Max(minY, Math.Min(e.Y, endY - 1));
						break;
					case SelectionDragMode.BottomLeft:
						startX = Math.Max(minX, Math.Min(e.X, endX - 1));
						endY = Math.Min(maxY, Math.Max(e.Y, startY + 1));
						break;
					case SelectionDragMode.BottomRight:
						endX = Math.Min(maxX, Math.Max(e.X, startX + 1));
						endY = Math.Min(maxY, Math.Max(e.Y, startY + 1));
						break;
					case SelectionDragMode.Left:
						startX = Math.Max(minX, Math.Min(e.X, endX - 1));
						break;
					case SelectionDragMode.Right:
						endX = Math.Min(maxX, Math.Max(e.X, startX + 1));
						break;
					case SelectionDragMode.Top:
						startY = Math.Max(minY, Math.Min(e.Y, endY - 1));
						break;
					case SelectionDragMode.Bottom:
						endY = Math.Min(maxY, Math.Max(e.Y, startY + 1));
						break;
					case SelectionDragMode.Move:
						int w = rect.Width, h = rect.Height;
						int newLeft = Math.Max(minX, Math.Min(e.X - dragOffset.X, maxX - w));
						int newTop = Math.Max(minY, Math.Min(e.Y - dragOffset.Y, maxY - h));
						startX = newLeft;
						startY = newTop;
						endX = newLeft + w;
						endY = newTop + h;
						break;
					default:
						endX = Math.Min(maxX, Math.Max(e.X, startX + 1));
						endY = Math.Min(maxY, Math.Max(e.Y, startY + 1));
						break;
				}
				imageBox.Invalidate();
				var selRect = GetSelectionRect();
				imageSelectionStatus.Text = $"{selRect.Width}x{selRect.Height}";
			}
			else if (btnPenMode.Checked)
			{
				// 绘制线条
				using (Graphics g = Graphics.FromImage(currentBitmap))
				{
					g.DrawLine(new Pen(btnPenColor.BackColor, 2),
						new Point(startX, startY),
						new Point(endX, endY));
				}

				imageBox.Image = currentBitmap;
				startX = endX;
				startY = endY;
			}
			else if (currentDrawMode == DrawMode.Rect)
			{
				// 绘制矩形
				using (Graphics g = Graphics.FromImage(currentBitmap))
				{
					g.DrawRectangle(new Pen(btnPenColor.BackColor, 2),
						new Rectangle(startX, startY, endX - startX, endY - startY));
				}
				imageBox.Image = currentBitmap;
			}
			else if (currentDrawMode == DrawMode.Ellipse)
			{
				// 绘制椭圆
				using (Graphics g = Graphics.FromImage(currentBitmap))
				{
					g.DrawEllipse(new Pen(btnPenColor.BackColor, 2),
						new Rectangle(startX, startY, endX - startX, endY - startY));
				}
				imageBox.Image = currentBitmap;
			}
			else if (currentDrawMode == DrawMode.Highlight)
			{
				// 绘制高亮矩形
				using (Graphics g = Graphics.FromImage(currentBitmap))
				{
					g.DrawRectangle(new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash },
						new Rectangle(startX, startY, endX - startX, endY - startY));
				}
				imageBox.Image = currentBitmap;
			}
			else
			{
				// 显示选择区域
				rect = GetSelectionRect();
				imageSelectionStatus.Text = $"{rect.Width}x{rect.Height}";
			}
		}

		// 5. 鼠标松开
		private void imageBox_MouseUp(object sender, MouseEventArgs e)
		{
			if (!isImage) return;

			MDFlag = false;
			currentDragMode = SelectionDragMode.None;
			imageBox.Capture = false;

			if (btnPenMode.Checked)
			{
				// 保存绘制结果
				UndoSX = startX;
				UndoSY = startY;
				UndoEX = endX;
				UndoEY = endY;
			}
			else if (currentDrawMode == DrawMode.Rect)
			{
				// 保存矩形绘制结果
				UndoSX = startX;
				UndoSY = startY;
				UndoEX = endX;
				UndoEY = endY;
			}
			else if (currentDrawMode == DrawMode.Ellipse)
			{
				// 保存椭圆绘制结果
				UndoSX = startX;
				UndoSY = startY;
				UndoEX = endX;
				UndoEY = endY;
			}
			else if (currentDrawMode == DrawMode.Highlight)
			{
				// 保存高亮绘制结果
				UndoSX = startX;
				UndoSY = startY;
				UndoEX = endX;
				UndoEY = endY;
			}
			else
			{
				// 清除选择区域信息
				imageSelectionStatus.Text = "";
			}
		}

		private Rectangle GetSelectionRect()
		{
			int x = Math.Min(startX, endX);
			int y = Math.Min(startY, endY);
			int width = Math.Abs(endX - startX);
			int height = Math.Abs(endY - startY);
			return new Rectangle(x, y, width, height);
		}

		private void btnPenMode_Click(object sender, EventArgs e)
		{
			if (btnPenMode.Checked)
			{
				// 进入绘制模式
				tmpAll = new Bitmap(currentBitmap);
				imageBox.Cursor = Cursors.Cross;
			}
			else
			{
				// 退出绘制模式
				imageBox.Cursor = Cursors.Default;
			}
		}

		private void btnUndo_Click(object sender, EventArgs e)
		{
			if (undoStack.Count > 0)
			{
				currentBitmap.Dispose();
				currentBitmap = undoStack.Pop();
				imageBox.Image = currentBitmap;
				UpdateStatusBarForImage();
			}
			btnUndo.Enabled = undoStack.Count > 0;
		}

		private void btnRedEye_Click(object sender, EventArgs e)
		{
			if (!isImage || currentBitmap == null) return;

			// 获取选择区域
			var rect = GetSelectionRect();

			// 处理红眼
			using (Graphics g = Graphics.FromImage(currentBitmap))
			{
				using (var brush = new SolidBrush(Color.Black))
				{
					g.FillEllipse(brush, rect);
				}
			}

			imageBox.Image = currentBitmap;
			UpdateStatusBarForImage();
		}

		private void btnCutToImage_Click(object sender, EventArgs e)
		{
			if (!isImage || currentBitmap == null) return;

			// 获取选择区域
			var rect = GetSelectionRect();

			// 创建新图像
			var newBitmap = new Bitmap(rect.Width, rect.Height);
			using (Graphics g = Graphics.FromImage(newBitmap))
			{
				g.DrawImage(currentBitmap, new Rectangle(0, 0, rect.Width, rect.Height),
					rect, GraphicsUnit.Pixel);
			}

			currentBitmap.Dispose();
			currentBitmap = newBitmap;
			imageBox.Image = currentBitmap;

			AdjustImageSize();
			UpdateStatusBarForImage();
		}
		//{
		//	if (e.Button == MouseButtons.Right)
		//	{
		//		// 显示上下文菜单
		//		pmEditMenu.Show(imageBox, e.Location);
		//	}
		//}

		//private void imageBox_MouseMove(object sender, MouseEventArgs e)
		//{
		//	if (!isMDFlag) return;

		//	Point imagePoint = TranslatePoint(e.Location);

		//	if (btnHightlight.Checked)
		//	{
		//		endX = imagePoint.X;
		//		endY = imagePoint.Y;
		//		imageBox.Invalidate();
		//	}
		//	else if (btnPaint.Checked)
		//	{
		//		// 绘图逻辑
		//	}
		//}

		//private void imageBox_MouseUp(object sender, MouseEventArgs e)
		//{
		//	isMDFlag = false;

		//	if (btnHightlight.Checked)
		//	{
		//		// 完成高亮选择
		//		Point imagePoint = TranslatePoint(e.Location);
		//		endX = imagePoint.X;
		//		endY = imagePoint.Y;

		//		// 确保正确的选择区域
		//		if (endX < startX)
		//		{
		//			int temp = startX;
		//			startX = endX;
		//			endX = temp;
		//		}

		//		if (endY < startY)
		//		{
		//			int temp = startY;
		//			startY = endY;
		//			endY = temp;
		//		}

		//		// 更新状态栏
		//		imageSelectionStatus.Text = $"{endX - startX}x{endY - startY}";
		//	}
		//}

		private void imageBox_Paint(object sender, PaintEventArgs e)
		{
			if (btnHightlight.Checked)
			{
				var rect = GetSelectionRect();
				if (rect.Width > 0 && rect.Height > 0)
				{
					using (Pen pen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
					{
						e.Graphics.DrawRectangle(pen, rect);
					}
					// 绘制手柄
					Brush handleBrush = Brushes.White;
					Pen handlePen = Pens.Black;
					Rectangle[] handles = new Rectangle[] {
						new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE),
						new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE),
						new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE),
						new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE),
					};
					foreach (var h in handles)
					{
						e.Graphics.FillRectangle(handleBrush, h);
						e.Graphics.DrawRectangle(handlePen, h);
					}
				}
			}
		}

		private Point TranslatePoint(Point screenPoint, bool reverse = false)
		{
			if (imageBox.Image == null) return screenPoint;

			// 将屏幕坐标转换为图像坐标（考虑缩放和位置）
			float scaleX = (float)imageBox.Image.Width / imageBox.Width;
			float scaleY = (float)imageBox.Image.Height / imageBox.Height;

			if (reverse)
			{
				// 图像坐标 -> 屏幕坐标
				return new Point(
					(int)(screenPoint.X * scaleX) + imageBox.Left,
					(int)(screenPoint.Y * scaleY) + imageBox.Top);
			}
			else
			{
				// 屏幕坐标 -> 图像坐标
				return new Point(
					(int)((screenPoint.X - imageBox.Left) / scaleX),
					(int)((screenPoint.Y - imageBox.Top) / scaleY));
			}
		}

		// 查找下一个
		private void FindNext()
		{
			if (pnlText.Visible)
			{
				btnFind_Click(null, null); // 复用查找逻辑
			}
			else if (pnlCode.Visible)
			{
				btnFind_Click(null, null);
			}
		}

		// 查找上一个（简单实现，实际可扩展为反向查找）
		private void FindPrev()
		{
			// 这里只做简单提示，实际可实现反向查找
			MessageBox.Show("暂未实现反向查找，可扩展！", "提示");
		}

		// 跳转到指定行
		private void GotoLine(int lineNumber)
		{
			if (pnlText.Visible)
			{
				int pos = 0;
				string text = txtContent.Text;
				string[] lines = text.Split('\n');
				for (int i = 0; i < Math.Min(lineNumber - 1, lines.Length); i++)
				{
					pos += lines[i].Length + 1;
				}
				txtContent.SelectionStart = Math.Min(pos, text.Length);
				txtContent.ScrollToCaret();
			}
			else if (pnlCode.Visible)
			{
				//if (lineNumber > 0 && lineNumber <= codeBox.LinesCount)
				{
					//codeBox.Selection.Start = new FastColoredTextBoxNS.Place(0, lineNumber - 1);
					//codeBox.DoSelectionVisible();
				}
			}
		}

		// 选择全部
		private void SelectAll()
		{
			if (pnlText.Visible)
			{
				txtContent.SelectAll();
			}
			else if (pnlCode.Visible)
			{
				codeBox.SelectAll();
			}
		}

		// 复制
		private void Copy()
		{
			if (pnlText.Visible)
			{
				if (!string.IsNullOrEmpty(txtContent.SelectedText))
					Clipboard.SetText(txtContent.SelectedText);
			}
			else if (pnlCode.Visible)
			{
				if (!string.IsNullOrEmpty(codeBox.SelectedText))
					Clipboard.SetText(codeBox.SelectedText);
			}
		}

		// 高亮按钮点击事件
		private void btnHightlight_Click(object sender, EventArgs e)
		{
			if (btnHightlight.Checked)
			{
				btnPenMode.Checked = false;
				imageBox.Cursor = Cursors.Cross;
			}
			else
			{
				imageBox.Cursor = Cursors.Default;
			}
		}


		// 工具按钮事件
		private void btnRect_Click(object sender, EventArgs e)
		{
			currentDrawMode = btnRect.Checked ? DrawMode.Rect : DrawMode.None;
			if (btnRect.Checked) imageBox.Cursor = Cursors.Cross; else imageBox.Cursor = Cursors.Default;
		}
		private void btnEllipse_Click(object sender, EventArgs e)
		{
			currentDrawMode = btnEllipse.Checked ? DrawMode.Ellipse : DrawMode.None;
			if (btnEllipse.Checked) imageBox.Cursor = Cursors.Cross; else imageBox.Cursor = Cursors.Default;
		}

		// 二进制模式切换（占位）
		private void ShowAsBin()
		{
			MessageBox.Show("二进制模式暂未实现，可扩展！", "提示");
		}
		// 十六进制模式切换（占位）
		private void ShowAsHex()
		{
			MessageBox.Show("十六进制模式暂未实现，可扩展！", "提示");
		}
		// 十进制模式切换（占位）
		private void ShowAsDec()
		{
			MessageBox.Show("十进制模式暂未实现，可扩展！", "提示");
		}
		// Office模式切换（占位）
		private void ShowOffice()
		{
			MessageBox.Show("Office文件查看暂未实现，可扩展！", "提示");
		}
		// 插件模式切换（占位）
		private void ShowPlugins()
		{
			MessageBox.Show("插件模式暂未实现，可扩展！", "提示");
		}
		// 代码高亮模式切换（占位）
		private void ShowCode()
		{
			MessageBox.Show("代码高亮模式暂未实现，可扩展！", "提示");
		}

		private void UpdateStatusBarForImage()
		{
			if (currentBitmap != null)
			{
				var fileInfo = new FileInfo(fileName);
				fileNameStatus.Text = Path.GetFileName(fileName);
				fileSizeStatus.Text = fileInfo.Exists ? $"{fileInfo.Length:N0} bytes" : "";
				statusFileType.Text = "Image";
				currentResolutionStatus.Text = $"{currentBitmap.Width}x{currentBitmap.Height}";
				fullResolutionStatus.Text = currentResolutionStatus.Text;
			}
		}

		private void UpdateStatusBarForText()
		{
			var fileInfo = new FileInfo(fileName);
			fileNameStatus.Text = Path.GetFileName(fileName);
			fileSizeStatus.Text = fileInfo.Exists ? $"{fileInfo.Length:N0} bytes" : "";
			statusFileType.Text = "Text";
			statusImageSize.Text = $"{txtContent.Text.Length:N0} chars";
		}

		private void UpdateStatusBarForCode()
		{
			var fileInfo = new FileInfo(fileName);
			fileNameStatus.Text = Path.GetFileName(fileName);
			fileSizeStatus.Text = fileInfo.Exists ? $"{fileInfo.Length:N0} bytes" : "";
			statusFileType.Text = "Code";
			//statusImageSize.Text = $"{codeBox.Text.Length:N0} chars, {codeBox.LinesCount} lines";
		}

		private void UpdateStatusBarForFolder(string folderPath)
		{
			fileNameStatus.Text = Path.GetFileName(folderPath);
			fileSizeStatus.Text = $"{listViewFolder.Items.Count:N0} items";
			statusFileType.Text = "Folder";
			statusImageSize.Text = "";
		}



		private void btnPenColor_Click(object sender, EventArgs e)
		{
			using (var colorDialog = new ColorDialog())
			{
				colorDialog.Color = btnPenColor.BackColor;
				if (colorDialog.ShowDialog() == DialogResult.OK)
				{
					btnPenColor.BackColor = colorDialog.Color;
				}
			}
		}

		private void btnGrayscale_Click(object sender, EventArgs e)
		{
			if (!isImage || currentBitmap == null) return;

			var rect = GetSelectionRect();
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				rect = new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height);
			}

			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				for (int x = rect.Left; x < rect.Right; x++)
				{
					Color c = currentBitmap.GetPixel(x, y);
					int gray = (int)((c.R * 0.3) + (c.G * 0.59) + (c.B * 0.11));
					currentBitmap.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
				}
			}

			imageBox.Image = currentBitmap;
			UpdateStatusBarForImage();
		}

		private void btnBrightness_Click(object sender, EventArgs e)
		{
			if (!isImage || currentBitmap == null) return;

			var rect = GetSelectionRect();
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				rect = new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height);
			}

			// 调整亮度值（-255到255）
			int brightness = 30;

			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				for (int x = rect.Left; x < rect.Right; x++)
				{
					Color c = currentBitmap.GetPixel(x, y);
					int r = Math.Max(0, Math.Min(255, c.R + brightness));
					int g = Math.Max(0, Math.Min(255, c.G + brightness));
					int b = Math.Max(0, Math.Min(255, c.B + brightness));
					currentBitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
				}
			}

			imageBox.Image = currentBitmap;
			UpdateStatusBarForImage();
		}

		private void btnContrast_Click(object sender, EventArgs e)
		{
			if (!isImage || currentBitmap == null) return;

			var rect = GetSelectionRect();
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				rect = new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height);
			}

			// 调整对比度值（0.1到5.0）
			double contrast = 1.3;

			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				for (int x = rect.Left; x < rect.Right; x++)
				{
					Color c = currentBitmap.GetPixel(x, y);
					double r = ((c.R / 255.0 - 0.5) * contrast + 0.5) * 255.0;
					double g = ((c.G / 255.0 - 0.5) * contrast + 0.5) * 255.0;
					double b = ((c.B / 255.0 - 0.5) * contrast + 0.5) * 255.0;
					r = Math.Max(0, Math.Min(255, r));
					g = Math.Max(0, Math.Min(255, g));
					b = Math.Max(0, Math.Min(255, b));
					currentBitmap.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));
				}
			}

			imageBox.Image = currentBitmap;
			UpdateStatusBarForImage();
		}
		// 在相关操作后调用
		// 1. 图片加载、缩放、旋转、裁剪、编辑后
		// 2. 文本/代码加载、切换后
		// 3. 文件夹浏览后
		// 4. 选区变化时
	}

	// GIF动画处理类
	public class GifAnimator : IDisposable
	{
		private Image gifImage;
		private FrameDimension dimension;
		private int frameCount;
		private int currentFrame = 0;
		private int[] frameDelays;
		private bool isDisposed = false;

		public GifAnimator(string filePath)
		{
			gifImage = Image.FromFile(filePath);
			dimension = new FrameDimension(gifImage.FrameDimensionsList[0]);
			frameCount = gifImage.GetFrameCount(dimension);

			// 获取帧延迟
			PropertyItem propItem = gifImage.GetPropertyItem(0x5100); // PropertyTagFrameDelay
			frameDelays = new int[frameCount];

			for (int i = 0; i < frameCount; i++)
			{
				frameDelays[i] = BitConverter.ToInt32(propItem.Value, i * 4) * 10;
			}
		}

		public Image GetNextFrame()
		{
			if (isDisposed) return null;

			gifImage.SelectActiveFrame(dimension, currentFrame);
			Image frame = new Bitmap(gifImage);

			currentFrame = (currentFrame + 1) % frameCount;
			return frame;
		}

		public int FrameDelay => frameDelays.Length > 0 ? frameDelays[currentFrame] : 100;

		public void Dispose()
		{
			if (!isDisposed)
			{
				gifImage.Dispose();
				isDisposed = true;
			}
		}



	}
}