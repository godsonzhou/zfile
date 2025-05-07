using System.Diagnostics.Eventing.Reader;

namespace zfile
{
	/// <summary>
	/// File operation dialog look options
	/// </summary>
	[Flags]
	public enum FileOpDlgLook
	{
		/// <summary>
		/// Show "From" label
		/// </summary>
		FromLabel = 1,

		/// <summary>
		/// Show "To" label
		/// </summary>
		ToLabel = 2,

		/// <summary>
		/// Show current progress bar
		/// </summary>
		CurrentProgressBar = 4,

		/// <summary>
		/// Show total progress bar
		/// </summary>
		TotalProgressBar = 8
	}

	/// <summary>
	/// Operation progress window event
	/// </summary>
	public enum OperationProgressWindowEvent
	{
		/// <summary>
		/// Window opened
		/// </summary>
		Opened,

		/// <summary>
		/// Window closed
		/// </summary>
		Closed
	}

	/// <summary>
	/// Operation progress window event handler
	/// </summary>
	/// <param name="operationHandle">Operation handle</param>
	/// <param name="event">Event</param>
	public delegate void OperationProgressWindowEventProc(int operationHandle, OperationProgressWindowEvent @event);

	/// <summary>
	/// File operation dialog for displaying progress of file operations
	/// </summary>
	public class FileOperationDialog : Form
	{
		private int _operationHandle;
		private OperationsManagerItem? _operationItem;
		private int _queueIdentifier;
		private System.Windows.Forms.Timer? _updateTimer;
		private FileSourceOperationUI? _userInterface;
		private static readonly Dictionary<int, FileOperationDialog> _activeDialogs = new();
		private bool _stopOperationOnClose = true;

		// UI Controls - marked as nullable to avoid constructor warnings
		private Panel? pnlClient;
		private Panel? pnlQueue;
		private Label? lblCurrentOperation;
		private Label? lblCurrentOperationText;
		private Panel? pnlFrom;
		private Label? lblFrom;
		private Label? lblFileNameFrom;
		private Panel? pnlTo;
		private Label? lblTo;
		private Label? lblFileNameTo;
		private Label? lblEstimated;
		private ProgressBar? pbCurrent;
		private ProgressBar? pbTotal;
		private Panel? pnlButtons;
		private Button? btnMinimizeToPanel;
		private Button? btnViewOperations;
		private Button? btnCancel;
		private Button? btnPauseStart;
		private Label? lblFileCount;

		// Event listeners
		private static readonly Dictionary<OperationProgressWindowEvent, List<OperationProgressWindowEventProc>> _eventListeners =
			new();

		// Static constructor to initialize event listeners
		static FileOperationDialog()
		{
			// Initialize event listeners for each event type
			foreach (OperationProgressWindowEvent eventType in Enum.GetValues(typeof(OperationProgressWindowEvent)))
			{
				_eventListeners[eventType] = new List<OperationProgressWindowEventProc>();
			}
		}

		// Implementation of missing methods

		/// <summary>
		/// Closes the dialog without stopping the operation
		/// </summary>
		private void CloseDialog()
		{
			_stopOperationOnClose = false;
			Close();
		}

		/// <summary>
		/// Gets the first operation handle from a queue
		/// </summary>
		/// <param name="queueIdentifier">The queue identifier</param>
		/// <returns>The first operation handle or -1 if none</returns>
		private static int GetFirstOperationHandle(int queueIdentifier)
		{
			var queue = OperationsManager.Instance.GetQueueByIdentifier(queueIdentifier);
			if (queue != null && queue.Count > 0)
			{
				// Get the first operation from the queue
				return queue.GetItem(0).Handle;
			}

			return OperationsManager.InvalidOperationHandle; // Invalid operation handle
		}

		/// <summary>
		/// Initializes the operation
		/// </summary>
		/// <returns>True if successful, false otherwise</returns>
		private bool InitializeOperation()
		{
			var opManItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);

			// If operation is in a queue, get the first operation from that queue
			if (opManItem != null && !opManItem.Queue.IsFree)
			{
				_operationHandle = GetFirstOperationHandle(opManItem.Queue.Identifier);
				opManItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
			}

			if (opManItem != null)
			{
				_queueIdentifier = opManItem.Queue.Identifier;

				if (AddToOpenedForms(opManItem))
				{
					if (_userInterface == null)
					{
						_userInterface = new FileSourceOperationMessageBoxesUI();
					}

					opManItem.Operation.AddUserInterface(_userInterface);

					// Set progress bar style to marquee initially
					SetProgressBarStyle(ProgressBarStyle.Marquee);

					// Initialize controls based on operation type
					switch (opManItem.Operation.OperationType)
					{
						case FileSourceOperationType.Copy:
						case FileSourceOperationType.CopyIn:
						case FileSourceOperationType.CopyOut:
							InitializeCopyOperation(opManItem);
							break;
						case FileSourceOperationType.Move:
							InitializeMoveOperation(opManItem);
							break;
						case FileSourceOperationType.Delete:
							InitializeDeleteOperation(opManItem);
							break;
						case FileSourceOperationType.Wipe:
							InitializeWipeOperation(opManItem);
							break;
						case FileSourceOperationType.Split:
							InitializeSplitOperation(opManItem);
							break;
						case FileSourceOperationType.Combine:
							InitializeCombineOperation(opManItem);
							break;
						case FileSourceOperationType.CalcChecksum:
							InitializeCalcChecksumOperation(opManItem);
							break;
						case FileSourceOperationType.TestArchive:
							InitializeTestArchiveOperation(opManItem);
							break;
						case FileSourceOperationType.CalcStatistics:
							InitializeCalcStatisticsOperation(opManItem);
							break;
						case FileSourceOperationType.SetFileProperty:
							InitializeSetFilePropertyOperation(opManItem);
							break;
						default:
							InitializeControls(opManItem, FileOpDlgLook.TotalProgressBar);
							break;
					}

					UpdatePauseStartButton(opManItem);
					Text = string.Empty;
					_updateTimer?.Start();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Finalizes the operation
		/// </summary>
		private void FinalizeOperation()
		{
			if (_updateTimer != null)
			{
				_updateTimer.Enabled = false;
			}

			if (_operationHandle != -1)
			{
				if (_userInterface != null)
				{
					var opManItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
					if (opManItem != null)
					{
						opManItem.Operation.RemoveUserInterface(_userInterface);
					}
				}

				RemoveFromOpenedForms();
				_operationHandle = -1;
			}
		}

		/// <summary>
		/// Adds the form to the opened forms list
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		/// <returns>True if successful, false otherwise</returns>
		private bool AddToOpenedForms(OperationsManagerItem opManItem)
		{
			// Check if another form is not already opened for the operation or queue
			foreach (var item in _activeDialogs)
			{
				if (item.Key == _operationHandle ||
					(!opManItem.Queue.IsFree && item.Key == _queueIdentifier))
				{
					return false;
				}
			}

			_activeDialogs[_operationHandle] = this;
			NotifyEvents(new[] { OperationProgressWindowEvent.Opened });
			return true;
		}

		/// <summary>
		/// Removes the form from the opened forms list
		/// </summary>
		private void RemoveFromOpenedForms()
		{
			if (_activeDialogs.ContainsKey(_operationHandle))
			{
				_activeDialogs.Remove(_operationHandle);
				NotifyEvents(new[] { OperationProgressWindowEvent.Closed });
			}
		}

		/// <summary>
		/// Notifies event listeners
		/// </summary>
		/// <param name="events">The events to notify</param>
		private void NotifyEvents(OperationProgressWindowEvent[] events)
		{
			foreach (var evt in events)
			{
				if (_eventListeners.TryGetValue(evt, out var listeners))
				{
					foreach (var listener in listeners)
					{
						listener(_operationHandle, evt);
					}
				}
			}
		}

		/// <summary>
		/// Sets the progress bar style
		/// </summary>
		/// <param name="style">The style</param>
		private void SetProgressBarStyle(ProgressBarStyle style)
		{
			if (pbCurrent != null) pbCurrent.Style = style;
			if (pbTotal != null) pbTotal.Style = style;
		}

		/// <summary>
		/// Gets the progress bar style
		/// </summary>
		/// <returns>The style</returns>
		private ProgressBarStyle GetProgressBarStyle()
		{
			if (pbCurrent != null && pbTotal != null &&
				pbCurrent.Style == ProgressBarStyle.Marquee &&
				pbTotal.Style == ProgressBarStyle.Marquee)
			{
				return ProgressBarStyle.Marquee;
			}
			return ProgressBarStyle.Continuous;
		}

		/// <summary>
		/// Sets the label caption with proper ellipsis
		/// </summary>
		/// <param name="label">The label</param>
		/// <param name="text">The text</param>
		private static void SetLabelCaption(Label label, string text)
		{
			if (label != null)
			{
				label.Text = text;
				// Set tooltip using Tag property since Label doesn't have ToolTip property
				label.Tag = text;
			}
		}

		/// <summary>
		/// Initializes controls based on operation type
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		/// <param name="look">The look options</param>
		private void InitializeControls(OperationsManagerItem opManItem, FileOpDlgLook look)
		{
			if (pnlQueue != null)
				pnlQueue.Visible = !opManItem.Queue.IsFree;

			if (lblFrom != null)
				lblFrom.Visible = look.HasFlag(FileOpDlgLook.FromLabel);

			if (lblFileNameFrom != null)
				lblFileNameFrom.Visible = look.HasFlag(FileOpDlgLook.FromLabel);

			if (lblTo != null)
				lblTo.Visible = look.HasFlag(FileOpDlgLook.ToLabel);

			if (lblFileNameTo != null)
				lblFileNameTo.Visible = look.HasFlag(FileOpDlgLook.ToLabel);

			if (pbCurrent != null)
				pbCurrent.Visible = look.HasFlag(FileOpDlgLook.CurrentProgressBar);

			if (pbTotal != null)
				pbTotal.Visible = look.HasFlag(FileOpDlgLook.TotalProgressBar);

			// Clear labels
			if (lblFileNameFrom != null)
				lblFileNameFrom.Text = string.Empty;

			if (lblFileNameTo != null)
				lblFileNameTo.Text = string.Empty;

			if (lblEstimated != null)
				lblEstimated.Text = " ";

			if (lblFileCount != null)
				lblFileCount.Text = string.Empty;
		}

		/// <summary>
		/// Initializes a copy operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeCopyOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.ToLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a move operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeMoveOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.ToLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a delete operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeDeleteOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a wipe operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeWipeOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a split operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeSplitOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.ToLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a combine operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeCombineOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.ToLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a calculate checksum operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeCalcChecksumOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a test archive operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeTestArchiveOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.ToLabel |
				FileOpDlgLook.CurrentProgressBar | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Initializes a calculate statistics operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeCalcStatisticsOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel);
		}

		/// <summary>
		/// Initializes a set file property operation
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void InitializeSetFilePropertyOperation(OperationsManagerItem opManItem)
		{
			InitializeControls(opManItem, FileOpDlgLook.FromLabel | FileOpDlgLook.TotalProgressBar);
		}

		/// <summary>
		/// Creates a new instance of the FileOperationDialog class
		/// </summary>
		/// <param name="handle">The operation handle</param>
		public FileOperationDialog(int handle)
		{
			_operationHandle = handle;
			_operationItem = OperationsManager.Instance.GetItemByHandle(handle);

			if (_operationItem == null)
				throw new ArgumentException("Invalid operation handle", nameof(handle));

			_queueIdentifier = _operationItem.Queue.Identifier;
			_userInterface = new FileSourceOperationMessageBoxesUI();
			_stopOperationOnClose = true;

			InitializeComponent();
			InitializeTimer();

			if (!InitializeOperation())
			{
				CloseDialog();
			}
		}

		/// <summary>
		/// Creates a new instance of the FileOperationDialog class for a queue
		/// </summary>
		/// <param name="queueIdentifier">The queue identifier</param>
		public FileOperationDialog(int queueIdentifier, bool isQueueIdentifier)
		{
			if (!isQueueIdentifier)
			{
				// Call the other constructor if this is not a queue identifier
				_operationHandle = queueIdentifier;
				_operationItem = OperationsManager.Instance.GetItemByHandle(queueIdentifier);

				if (_operationItem == null)
					throw new ArgumentException("Invalid operation handle", nameof(queueIdentifier));

				_queueIdentifier = _operationItem.Queue.Identifier;
			}
			else
			{
				// This is a queue identifier
				_queueIdentifier = queueIdentifier;
				_operationHandle = GetFirstOperationHandle(queueIdentifier);
				_operationItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);

				if (_operationItem == null)
					throw new ArgumentException("Invalid queue identifier", nameof(queueIdentifier));
			}

			_userInterface = new FileSourceOperationMessageBoxesUI();
			_stopOperationOnClose = true;

			InitializeComponent();
			InitializeTimer();

			if (!InitializeOperation())
			{
				CloseDialog();
			}
		}

		private void InitializeComponent()
		{
			// Form settings
			this.Text = "File Operation";
			this.ClientSize = new Size(522, 212);
			this.MinimumSize = new Size(500, 200);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ShowInTaskbar = true;
			this.FormClosing += FormClose;

			// Main panel
			pnlClient = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Padding = new Padding(3)
			};
			this.Controls.Add(pnlClient);

			// Queue panel
			pnlQueue = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Height = 21
			};
			pnlClient.Controls.Add(pnlQueue);

			// Current operation label
			lblCurrentOperation = new Label
			{
				Dock = DockStyle.Top,
				Text = "Current operation:",
				AutoSize = true
			};
			pnlQueue.Controls.Add(lblCurrentOperation);

			// Current operation text
			lblCurrentOperationText = new Label
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Top = 20
			};
			pnlQueue.Controls.Add(lblCurrentOperationText);

			// From panel
			pnlFrom = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Height = 15,
				Top = 21
			};
			pnlClient.Controls.Add(pnlFrom);

			// From label
			lblFrom = new Label
			{
				Dock = DockStyle.Left,
				Text = "From:",
				AutoSize = true,
				Width = 40
			};
			pnlFrom.Controls.Add(lblFrom);

			// From filename
			lblFileNameFrom = new Label
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				AutoEllipsis = true
			};
			pnlFrom.Controls.Add(lblFileNameFrom);

			// To panel
			pnlTo = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Height = 15,
				Top = 39
			};
			pnlClient.Controls.Add(pnlTo);

			// To label
			lblTo = new Label
			{
				Dock = DockStyle.Left,
				Text = "To:",
				AutoSize = true,
				Width = 40
			};
			pnlTo.Controls.Add(lblTo);

			// To filename
			lblFileNameTo = new Label
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				AutoEllipsis = true
			};
			pnlTo.Controls.Add(lblFileNameTo);

			// Estimated time
			lblEstimated = new Label
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Top = 57
			};
			pnlClient.Controls.Add(lblEstimated);

			// Current progress bar
			pbCurrent = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 22,
				Top = 61,
				Style = ProgressBarStyle.Continuous
			};
			pnlClient.Controls.Add(pbCurrent);

			// Total progress bar
			pbTotal = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 22,
				Top = 86,
				Style = ProgressBarStyle.Continuous
			};
			pnlClient.Controls.Add(pbTotal);

			// Buttons panel
			pnlButtons = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Height = 30,
				Top = 111
			};
			pnlClient.Controls.Add(pnlButtons);

			// Minimize to panel button
			btnMinimizeToPanel = new Button
			{
				Dock = DockStyle.Left,
				Text = "&To panel",
				AutoSize = true,
				Width = 72
			};
			btnMinimizeToPanel.Click += BtnMinimizeToPanelClick;
			pnlButtons.Controls.Add(btnMinimizeToPanel);

			// View operations button
			btnViewOperations = new Button
			{
				Dock = DockStyle.Left,
				Text = "&View all",
				AutoSize = true,
				Width = 66,
				Left = 75
			};
			btnViewOperations.Click += BtnViewOperationsClick;
			pnlButtons.Controls.Add(btnViewOperations);

			// Cancel button
			btnCancel = new Button
			{
				Dock = DockStyle.Right,
				Text = "&Cancel",
				AutoSize = true,
				Width = 86,
				DialogResult = DialogResult.Cancel
			};
			btnCancel.Click += BtnCancelClick;
			pnlButtons.Controls.Add(btnCancel);

			// Pause/Start button
			btnPauseStart = new Button
			{
				Dock = DockStyle.Right,
				Text = "&Pause",
				AutoSize = true,
				Width = 50
			};
			btnPauseStart.Click += BtnPauseStartClick;
			pnlButtons.Controls.Add(btnPauseStart);

			// File count label
			lblFileCount = new Label
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleCenter
			};
			pnlButtons.Controls.Add(lblFileCount);

			// Set tab order
			this.CancelButton = btnCancel;
		}

		private void InitializeTimer()
		{
			_updateTimer = new System.Windows.Forms.Timer
			{
				Interval = 100 // Update 10 times per second
			};
			_updateTimer.Tick += UpdateTimer_Tick;
			_updateTimer.Start();
		}

		private void UpdateTimer_Tick(object sender, EventArgs e)
		{
			UpdateControls();
		}

		private void UpdateControls()
		{
			if (_operationItem == null || _operationItem.Operation == null)
				return;

			var operation = _operationItem.Operation;

			// Update operation text
			lblCurrentOperationText.Text = operation.Description + (operation.State).ToString();

			// Get statistics based on operation type
			FileSourceCopyOperationStatistics statistics = null;

			// Try to get statistics from different operation types
			if (operation is FileSourceCopyOperation copyOperation)
			{
				statistics = copyOperation.RetrieveStatistics();
			}
			else if (operation is FileSourceMoveOperation moveOperation)
			{
				statistics = moveOperation.RetrieveStatistics();
			}

			// Update UI with statistics if available
			if (statistics != null)
			{
				lblFileNameFrom.Text = statistics.CurrentFileFrom;
				lblFileNameTo.Text = statistics.CurrentFileTo;

				// Update progress bars
				if (statistics.TotalBytes > 0)
				{
					pbTotal.Value = (int)Math.Min(100, (statistics.DoneBytes * 100) / statistics.TotalBytes);
					pbTotal.Style = ProgressBarStyle.Continuous;
				}
				else
				{
					pbTotal.Style = ProgressBarStyle.Marquee;
				}

				if (statistics.CurrentFileTotalBytes > 0)
				{
					pbCurrent.Value = (int)Math.Min(100, (statistics.CurrentFileDoneBytes * 100) / statistics.CurrentFileTotalBytes);
					pbCurrent.Style = ProgressBarStyle.Continuous;
				}
				else
				{
					pbCurrent.Style = ProgressBarStyle.Marquee;
				}

				// Update file count
				lblFileCount.Text = $"{statistics.DoneFiles} / {statistics.TotalFiles} 文件";

				// Update estimated time
				var time = Helper.ConvertDateTimeToTimeSpan(statistics.RemainingTime);
				if (time.TotalSeconds > 0)
				{
					lblEstimated.Text = $"预计剩余时间: {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
				}
				else
				{
					lblEstimated.Text = "";
				}
			}
			else
			{
				// If no statistics available, show progress based on operation progress
				pbTotal.Value = (int)(operation.Progress * 100);
				pbCurrent.Style = ProgressBarStyle.Marquee;
			}

			// Update pause/start button
			UpdatePauseStartButton(operation.State);
		}

		/// <summary>
		/// Updates the pause/start button based on operation state
		/// </summary>
		/// <param name="state">The operation state</param>
		private void UpdatePauseStartButton(FileSourceOperationState state)
		{
			if (btnPauseStart == null)
				return;

			switch (state)
			{
				case FileSourceOperationState.Paused:
					btnPauseStart.Text = "&Start";
					btnPauseStart.Enabled = true;
					break;
				case FileSourceOperationState.Running:
					btnPauseStart.Text = "&Pause";
					btnPauseStart.Enabled = true;
					break;
				default:
					btnPauseStart.Enabled = false;
					break;
			}
		}

		/// <summary>
		/// Updates the pause/start button based on operation manager item
		/// </summary>
		/// <param name="opManItem">The operation manager item</param>
		private void UpdatePauseStartButton(OperationsManagerItem opManItem)
		{
			if (opManItem?.Operation == null || btnPauseStart == null)
				return;

			//// Get operation state
			//FileSourceOperationState state = FileSourceOperationState.Running;

			//// Check if operation is paused
			//if (opManItem.Operation.IsPaused)
			//	state = FileSourceOperationState.Paused;

			//// Update button based on state
			//UpdatePauseStartButton(state);
			if (opManItem.Queue != null && opManItem.Queue.IsFree) { 
				switch (opManItem.Operation.State) {
					case FileSourceOperationState.NotStarted:
					case FileSourceOperationState.Stopped:
					case FileSourceOperationState.Paused:
						btnPauseStart.Text = "&Start";
						btnPauseStart.Enabled = true;
						//setplayglyph;
						break;
					case FileSourceOperationState.Starting:
					case FileSourceOperationState.Stopping:
					case FileSourceOperationState.WaitingForFeedback:
						btnPauseStart.Enabled = false;
						break;
					case FileSourceOperationState.Running:
					case FileSourceOperationState.WaitingForConnection:
						btnPauseStart.Enabled = true;
						//setpausedglyph;
						break;
					default:
						btnPauseStart.Enabled = false;
						break;
				}
			}
			else
			{
				btnPauseStart.Enabled = true;
				//if(opManItem.Queue.Paused)
				//	//setplayglyph;
				//else
				//	//setpausedglyph;
			}
		}

		private void BtnPauseStartClick(object sender, EventArgs e)
		{
			if (_operationItem?.Operation != null)
			{
				_operationItem.Operation.TogglePause();
				UpdateControls();
			}
		}

		private void BtnCancelClick(object sender, EventArgs e)
		{
			if (StopOperationOrQueue())
			{
				DialogResult = DialogResult.Cancel;
			}
		}

		/// <summary>
		/// Stops the current operation or queue after confirmation
		/// </summary>
		/// <returns>True if operation was stopped, false otherwise</returns>
		private bool StopOperationOrQueue()
		{
			bool result = true;
			var opManItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);

			if (opManItem != null)
			{
				bool paused = false;

				// Pause operation or queue before asking for confirmation
				if (opManItem.Queue.IsFree)
				{
					paused = opManItem.Operation.State == FileSourceOperationState.Running ||
							opManItem.Operation.State == FileSourceOperationState.Starting ||
							opManItem.Operation.State == FileSourceOperationState.WaitingForConnection;

					if (paused) opManItem.Operation.Pause();
				}
				else
				{
					paused = !opManItem.Queue.Paused;
					if (paused) opManItem.Queue.Pause();
				}

				// Ask for confirmation
				result = MessageBox.Show("确定要取消操作吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

				if (result)
				{
					// Stop operation or queue
					if (opManItem.Queue.IsFree)
					{
						opManItem.Operation.Stop();
					}
					else
					{
						opManItem.Queue.Stop();
					}
				}
				else if (paused)
				{
					// Resume operation or queue if user canceled
					if (opManItem.Queue.IsFree)
					{
						opManItem.Operation.TogglePause();
					}
					else
					{
						opManItem.Queue.TogglePause();
					}
				}
			}

			return result;
		}

		private void BtnMinimizeToPanelClick(object sender, EventArgs e)
		{
			// Switch to operations panel view
			GlobalSettings.FileOperationsProgressKind = FileOperationsProgressKind.OperationsPanel;
			_stopOperationOnClose = false;
			Close();
		}

		private void BtnViewOperationsClick(object sender, EventArgs e)
		{
			if (_operationItem?.Operation != null)
			{
				if (_operationItem.Queue.IsFree)
				{
					// Show operations viewer for this specific operation
					ShowOperationsViewer(_operationItem.Handle);
				}
				else
				{
					// Show operations viewer for the entire queue
					ShowOperationsViewer(_operationItem.Queue.Identifier);
				}
			}
			else
			{
				ShowOperationsViewer();
			}
		}

		/// <summary>
		/// Shows the operations viewer window
		/// </summary>
		public static void ShowOperationsViewer()
		{
			// 在实际实现中，这里应该创建或显示一个操作查看器窗口
			// 类似于Pascal版本中的frmViewOperations
			// 由于C#版本中可能还没有实现OperationsViewer类，这里先留空
			// 实际项目中应该实现一个类似于Pascal版本中的frmViewOperations的窗体
			MessageBox.Show("Operations Viewer not implemented yet.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Shows the operations viewer window and focuses on a specific operation
		/// </summary>
		/// <param name="handle">The operation handle to focus</param>
		public static void ShowOperationsViewer(int handle)
		{
			ShowOperationsViewer();
			// 在实际实现中，这里应该设置焦点到指定的操作
			// 类似于Pascal版本中的frmViewOperations.SetFocusItem(AOperationHandle)
		}

		///// <summary>
		///// Shows the operations viewer window and focuses on a specific queue
		///// </summary>
		///// <param name="queueIdentifier">The queue identifier to focus</param>
		//public static void ShowOperationsViewer(int queueIdentifier)
		//{
		//	ShowOperationsViewer();
		//	// 在实际实现中，这里应该设置焦点到指定的队列
		//	// 类似于Pascal版本中的frmViewOperations.SetFocusItem(AQueueIdentifier)
		//}

		private void FormClose(object sender, FormClosingEventArgs e)
		{
			if (_updateTimer != null)
			{
				_updateTimer.Stop();
				_updateTimer.Dispose();
			}

			if (_activeDialogs.TryGetValue(_operationHandle, out _))
			{
				_activeDialogs.Remove(_operationHandle);
			}
		}

		/// <summary>
		/// Handles form closing confirmation
		/// </summary>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			// If form is being closed by user (not programmatically) and we should stop operation on close
			if (e.CloseReason == CloseReason.UserClosing && _stopOperationOnClose)
			{
				// Check if operation is still running and ask for confirmation
				var opManItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
				if (opManItem != null && opManItem.Operation != null &&
					(opManItem.Operation.State == FileSourceOperationState.Running ||
					 opManItem.Operation.State == FileSourceOperationState.Paused))
				{
					// If user cancels the operation stop, cancel the form closing
					if (!StopOperationOrQueue())
					{
						e.Cancel = true;
						return;
					}
				}
			}

			base.OnFormClosing(e);
		}

		/// <summary>
		/// Shows the dialog for an operation
		/// </summary>
		/// <param name="handle">The operation handle</param>
		/// <param name="options">The options</param>
		public static void ShowFor(int handle, OperationProgressWindowOptions options)
		{
			// Check if dialog already exists for this operation
			if (_activeDialogs.TryGetValue(handle, out var existingDialog))
			{
				// If dialog exists and we want to bring it to front
				if (options.HasFlag(OperationProgressWindowOptions.IfExistsBringToFront))
				{
					existingDialog.BringToFront();
				}
				return;
			}

			// Create new dialog
			var dialog = new FileOperationDialog(handle);
			_activeDialogs[handle] = dialog;

			// Show minimized if requested
			if (options.HasFlag(OperationProgressWindowOptions.StartMinimized))
			{
				dialog.WindowState = FormWindowState.Minimized;
			}

			dialog.Show();
		}

		/// <summary>
		/// Shows the dialog modally
		/// </summary>
		public new DialogResult ShowDialog()
		{
			return base.ShowDialog();
		}

		/// <summary>
		/// Disposes the dialog
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_updateTimer?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
