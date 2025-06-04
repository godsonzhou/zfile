using System.Diagnostics;
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
	// 扩展方法，用于设置进度条样式和颜色
	static class ProgressBarExtensions
	{
		public static void SetStyle(this ProgressBar pb, ProgressBarStyle style)
		{
			pb.Style = style;
		}
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
		private System.Timers.Timer? _updateTimer;
		private FileSourceOperationUI? _userInterface;
		private static readonly Dictionary<int, FileOperationDialog> _activeDialogs = new();
		private bool _stopOperationOnClose = true;

		// UI Controls - marked as nullable to avoid constructor warnings
		private Panel? pnlClient;
		private Panel? pnlQueue;
		private Label? lblCurrentOperation;
		private Label lblCurrentOperationText;
		private Panel? pnlFrom;
		private Label? lblFrom;
		private Label lblFileNameFrom;
		private Panel? pnlTo;
		private Label? lblTo;
		private Label lblFileNameTo;
		private Label lblEstimated;
		private ProgressBar pbCurrent;
		private ProgressBar pbTotal;
		private Panel? pnlButtons;
		private Button? btnMinimizeToPanel;
		private Button? btnViewOperations;
		private Button? btnCancel;
		private Button? btnPauseStart;
		private Label lblFileCount;

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
			Debug.Print($"Closing file operation dialog ({_operationHandle})");
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
						case FileSourceOperationTypes.Copy:
						case FileSourceOperationTypes.CopyIn:
						case FileSourceOperationTypes.CopyOut:
							InitializeCopyOperation(opManItem);
							break;
						case FileSourceOperationTypes.Move:
							InitializeMoveOperation(opManItem);
							break;
						case FileSourceOperationTypes.Delete:
							InitializeDeleteOperation(opManItem);
							break;
						case FileSourceOperationTypes.Wipe:
							InitializeWipeOperation(opManItem);
							break;
						case FileSourceOperationTypes.Split:
							InitializeSplitOperation(opManItem);
							break;
						case FileSourceOperationTypes.Combine:
							InitializeCombineOperation(opManItem);
							break;
						case FileSourceOperationTypes.CalcChecksum:
							InitializeCalcChecksumOperation(opManItem);
							break;
						case FileSourceOperationTypes.TestArchive:
							InitializeTestArchiveOperation(opManItem);
							break;
						case FileSourceOperationTypes.CalcStatistics:
							InitializeCalcStatisticsOperation(opManItem);
							break;
						case FileSourceOperationTypes.SetFileProperty:
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
		// Helper methods for operation updates
		private static string GetProgressString(double progress)
		{
			return $"{(int)(progress * 100)}%";
		}

		private static string GetOperationStateString(FileSourceOperationState state)
		{
			switch (state)
			{
				case FileSourceOperationState.Paused:
					return " [暂停]";
				case FileSourceOperationState.Stopping:
					return " [停止中]";
				case FileSourceOperationState.WaitingForFeedback:
					return " [等待反馈]";
				case FileSourceOperationState.WaitingForConnection:
					return " [等待连接]";
				default:
					return string.Empty;
			}
		}

		private void SetLabelText(Label label, string? text)
		{
			if (label != null && !string.IsNullOrEmpty(text))
			{
				label.Text = text;
				label.Tag = text; // Store full text in Tag for tooltip
			}
		}

		private void SetProgressBytes(FileSourceOperation operation, ProgressBar progressBar, long currentBytes, long totalBytes)
		{
			if (progressBar == null)
				return;

			if (currentBytes == -1)
			{
				progressBar.Style = ProgressBarStyle.Marquee;
			}
			else
			{
				if (operation.State == FileSourceOperationState.Running)
					progressBar.Style = ProgressBarStyle.Continuous;

				if (totalBytes > 0)
				{
					progressBar.Value = (int)Math.Min(100, (currentBytes * 100) / totalBytes);
				}
				else
				{
					progressBar.Style = ProgressBarStyle.Marquee;
				}
			}
		}

		private void SetProgressFiles(FileSourceOperation operation, ProgressBar progressBar, long currentFiles, long totalFiles)
		{
			if (progressBar == null)
				return;

			if (currentFiles == -1)
			{
				progressBar.Style = ProgressBarStyle.Marquee;
			}
			else
			{
				if (operation.State == FileSourceOperationState.Running)
					progressBar.Style = ProgressBarStyle.Continuous;

				if (totalFiles > 0)
				{
					progressBar.Value = (int)Math.Min(100, (currentFiles * 100) / totalFiles);
				}
				else
				{
					progressBar.Style = ProgressBarStyle.Marquee;
				}
			}
		}

		private void SetProgressCount(long doneFiles, long totalFiles)
		{
			if (lblFileCount == null)
				return;

			if (doneFiles < 0 || totalFiles == 0)
				lblFileCount.Text = string.Empty;
			else
				lblFileCount.Text = $"{doneFiles} / {totalFiles}";
		}

		private void SetSpeedAndTime(FileSourceOperation operation, DateTime remainingTime, string speed)
		{
			if (lblEstimated == null)
				return;

			string estimatedText = " ";

			if (operation.State == FileSourceOperationState.Running)
			{
				var time = Helper.ConvertDateTimeToTimeSpan(remainingTime);
				if (time.TotalSeconds > 0)
				{
					if (time.Days > 0)
						estimatedText = $"速度: {speed}, 剩余: {time.Days}天 {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
					else
						estimatedText = $"速度: {speed}, 剩余: {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
				}
				else
				{
					estimatedText = $"速度: {speed}";
				}
			}

			lblEstimated.Text = estimatedText;
		}

		// Helper method to format file size
		private static string FormatFileSize(long bytes)
		{
			if (bytes < 1024)
				return $"{bytes} B";
			else if (bytes < 1024 * 1024)
				return $"{bytes / 1024.0:F2} KB";
			else if (bytes < 1024 * 1024 * 1024)
				return $"{bytes / (1024.0 * 1024.0):F2} MB";
			else
				return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
		}

		// Operation update methods
		private void UpdateCopyOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceCopyOperation copyOperation)
			{
				var statistics = copyOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFileFrom);
				SetLabelText(lblFileNameTo, statistics.CurrentFileTo);

				SetProgressCount(statistics.DoneFiles, statistics.TotalFiles);
				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateMoveOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceMoveOperation moveOperation)
			{
				var statistics = moveOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFileFrom);
				SetLabelText(lblFileNameTo, statistics.CurrentFileTo);

				SetProgressCount(statistics.DoneFiles, statistics.TotalFiles);
				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateDeleteOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceDeleteOperation deleteOperation)
			{
				var statistics = deleteOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFile);

				SetProgressFiles(operation, pbTotal, statistics.DoneFiles, statistics.TotalFiles);
				SetSpeedAndTime(operation, statistics.RemainingTime, statistics.FilesPerSecond.ToString() + " 文件/秒");
			}
		}

		private void UpdateWipeOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceWipeOperation wipeOperation)
			{
				var statistics = wipeOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFile);

				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateSplitOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceSplitOperation splitOperation)
			{
				var statistics = splitOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFileFrom);
				SetLabelText(lblFileNameTo, statistics.CurrentFileTo);

				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateCombineOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceCombineOperation combineOperation)
			{
				var statistics = combineOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFileFrom);
				SetLabelText(lblFileNameTo, statistics.CurrentFileTo);

				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateCalcStatisticsOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceCalcStatisticsOperation calcStatisticsOperation)
			{
				var statistics = calcStatisticsOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFile);
				SetSpeedAndTime(operation, DateTime.MinValue, statistics.FilesPerSecond.ToString() + " 文件/秒");
			}
		}

		private void UpdateCalcChecksumOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceCalcChecksumOperation calcChecksumOperation)
			{
				var statistics = calcChecksumOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFile);

				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateTestArchiveOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceTestArchiveOperation testArchiveOperation)
			{
				var statistics = testArchiveOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.ArchiveFile);
				SetLabelText(lblFileNameTo, statistics.CurrentFile);

				SetProgressBytes(operation, pbCurrent, statistics.CurrentFileDoneBytes, statistics.CurrentFileTotalBytes);
				SetProgressBytes(operation, pbTotal, statistics.DoneBytes, statistics.TotalBytes);
				SetSpeedAndTime(operation, statistics.RemainingTime, FormatFileSize(statistics.BytesPerSecond) + "/s");
			}
		}

		private void UpdateSetFilePropertyOperation(FileSourceOperation operation)
		{
			if (operation is FileSourceSetFilePropertyOperation setFilePropertyOperation)
			{
				var statistics = setFilePropertyOperation.RetrieveStatistics();

				SetLabelText(lblFileNameFrom, statistics.CurrentFile);

				SetProgressFiles(operation, pbTotal, statistics.DoneFiles, statistics.TotalFiles);
				SetSpeedAndTime(operation, statistics.RemainingTime, statistics.FilesPerSecond.ToString() + " 文件/秒");
			}
		}

		private void UpdateOperation(OperationsManagerItem item)
		{
			if (item == null || item.Operation == null)
				return;

			var operation = item.Operation;

			// Update operation based on type
			switch (operation.OperationType)
			{
				case FileSourceOperationTypes.Copy:
				case FileSourceOperationTypes.CopyIn:
				case FileSourceOperationTypes.CopyOut:
					UpdateCopyOperation(operation);
					break;
				case FileSourceOperationTypes.Move:
					UpdateMoveOperation(operation);
					break;
				case FileSourceOperationTypes.Delete:
					UpdateDeleteOperation(operation);
					break;
				case FileSourceOperationTypes.Wipe:
					UpdateWipeOperation(operation);
					break;
				case FileSourceOperationTypes.Split:
					UpdateSplitOperation(operation);
					break;
				case FileSourceOperationTypes.Combine:
					UpdateCombineOperation(operation);
					break;
				case FileSourceOperationTypes.CalcChecksum:
					UpdateCalcChecksumOperation(operation);
					break;
				case FileSourceOperationTypes.CalcStatistics:
					UpdateCalcStatisticsOperation(operation);
					break;
				case FileSourceOperationTypes.TestArchive:
					UpdateTestArchiveOperation(operation);
					break;
				case FileSourceOperationTypes.SetFileProperty:
					UpdateSetFilePropertyOperation(operation);
					break;
				default:
					// Operation not currently supported for display.
					// Only show general progress.
					if (pbTotal != null)
						pbTotal.Value = (int)(operation.Progress * 100);
					break;
			}

			UpdatePauseStartButton(item);
			if (item.Queue == null)
				return;
			// Update window caption
			string newCaption;
			if (item.Queue.IsFree)
			{
				newCaption = GetProgressString(operation.Progress) + " " +
							 operation.Description +
							 GetOperationStateString(operation.State);
			}
			else
			{
				if (item.Queue.Paused)
				{
					newCaption = $"[{item.Queue.Count}] {item.Queue.GetDescription(false)}" +
								 GetOperationStateString(FileSourceOperationState.Paused);
				}
				else
				{
					newCaption = $"[{item.Queue.Count}] {GetProgressString(operation.Progress)} " +
								 $"{operation.Description} - " +
								 item.Queue.GetDescription(false);
				}

				if (lblCurrentOperationText != null)
				{
					lblCurrentOperationText.Text = operation.Description + " " +
											  GetProgressString(operation.Progress);
				}
			}

			Text = newCaption;
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
			Debug.Print($"FileOperationDialog.ctor({handle})");
			_operationHandle = handle;
			_operationItem = OperationsManager.Instance.GetItemByHandle(handle);

			if (_operationItem == null)
				CloseDialog();  //throw new ArgumentException("Invalid operation handle", nameof(handle));
			else
			{
				_queueIdentifier = _operationItem.Queue.Identifier;
				_userInterface = new FileSourceOperationMessageBoxesUI();
				_stopOperationOnClose = true;

				InitializeComponent();
				InitializeTimer();

				if (!InitializeOperation())
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
			this.ClientSize = new Size(500, 200);
			this.MinimumSize = new Size(500, 200);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ShowInTaskbar = true;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.FormClosing += FormClose;

			// Main panel
			pnlClient = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(10)
			};
			this.Controls.Add(pnlClient);

			// 创建布局 - 从上到下排列
			// 顶部信息区域
			var topInfoPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 20
			};
			pnlClient.Controls.Add(topInfoPanel);

			// 文件计数标签 - 显示在顶部右侧
			lblFileCount = new Label
			{
				Dock = DockStyle.Right,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleRight,
				Text = "0"
			};
			topInfoPanel.Controls.Add(lblFileCount);

			// 当前操作标签
			lblCurrentOperation = new Label
			{
				Dock = DockStyle.Left,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft
			};
			topInfoPanel.Controls.Add(lblCurrentOperation);

			// 当前操作文本
			lblCurrentOperationText = new Label
			{
				Dock = DockStyle.Left,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft,
				Left = lblCurrentOperation.Right + 5
			};
			topInfoPanel.Controls.Add(lblCurrentOperationText);

			// 进度条区域 - 两个进度条
			// 第一个进度条（当前文件进度）
			pbCurrent = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 20,
				Margin = new Padding(0, 10, 0, 5),
				Style = ProgressBarStyle.Continuous,
				ForeColor = Color.Green,
				Top = topInfoPanel.Bottom + 10
			};
			pnlClient.Controls.Add(pbCurrent);

			// 第二个进度条（总体进度）
			pbTotal = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 20,
				Margin = new Padding(0, 5, 0, 10),
				Style = ProgressBarStyle.Continuous,
				ForeColor = Color.Green,
				Top = pbCurrent.Bottom + 5
			};
			pnlClient.Controls.Add(pbTotal);

			// 文件信息区域
			// From panel
			pnlFrom = new Panel
			{
				Dock = DockStyle.Top,
				Height = 20,
				Top = 5,
				//Visible = false // 根据图片，这部分不显示
			};
			pnlClient.Controls.Add(pnlFrom);

			// From label
			lblFrom = new Label
			{
				Dock = DockStyle.Left,
				Text = "从:",
				AutoSize = true,
				Width = 40
			};
			pnlFrom.Controls.Add(lblFrom);

			// From filename
			lblFileNameFrom = new Label
			{
				Dock = DockStyle.Right,
				AutoSize = true,
				AutoEllipsis = true,
				Left = lblFrom.Width
			};
			pnlFrom.Controls.Add(lblFileNameFrom);

			// To panel
			pnlTo = new Panel
			{
				Dock = DockStyle.Top,
				Height = 20,
				Top = pnlFrom.Bottom,
				//Visible = false // 根据图片，这部分不显示
			};
			pnlClient.Controls.Add(pnlTo);

			// To label
			lblTo = new Label
			{
				Dock = DockStyle.Left,
				Text = "到:",
				AutoSize = true,
				Width = 40
			};
			pnlTo.Controls.Add(lblTo);

			// To filename
			lblFileNameTo = new Label
			{
				Dock = DockStyle.Right,
				AutoSize = true,
				AutoEllipsis = true,
				Left = lblTo.Width
			};
			pnlTo.Controls.Add(lblFileNameTo);

			// 估计时间标签
			lblEstimated = new Label
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Top = lblTo.Bottom
				//Visible = false // 根据图片，这部分不显示
			};
			pnlClient.Controls.Add(lblEstimated);

			// 按钮区域 - 底部
			pnlButtons = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 30,
				Padding = new Padding(0, 5, 0, 0)
			};
			pnlClient.Controls.Add(pnlButtons);

			// 左侧按钮 - 到面板
			btnMinimizeToPanel = new Button
			{
				Dock = DockStyle.Left,
				Text = "到面板(&T)",
				Width = 80,
				Height = 25
			};
			btnMinimizeToPanel.Click += BtnMinimizeToPanelClick;
			pnlButtons.Controls.Add(btnMinimizeToPanel);

			// 左侧按钮 - 查看全部
			btnViewOperations = new Button
			{
				Dock = DockStyle.Left,
				Text = "查看全部(&V)",
				Width = 80,
				Height = 25,
				Left = btnMinimizeToPanel.Right + 5
			};
			btnViewOperations.Click += BtnViewOperationsClick;
			pnlButtons.Controls.Add(btnViewOperations);

			// 右侧按钮 - 取消
			btnCancel = new Button
			{
				Dock = DockStyle.Right,
				Text = "取消(&C)",
				Width = 80,
				Height = 25,
				DialogResult = DialogResult.Cancel
			};
			btnCancel.Click += BtnCancelClick;
			pnlButtons.Controls.Add(btnCancel);

			// 右侧按钮 - 暂停/开始
			btnPauseStart = new Button
			{
				Dock = DockStyle.Right,
				Text = "暂停(&P)",
				Width = 80,
				Height = 25,
				//Right = btnCancel.Left + 5
			};
			btnPauseStart.Click += BtnPauseStartClick;
			pnlButtons.Controls.Add(btnPauseStart);

			// 设置Tab顺序
			this.CancelButton = btnCancel;

			// 设置进度条颜色
			pbCurrent.SetStyle(ProgressBarStyle.Continuous);
			pbTotal.SetStyle(ProgressBarStyle.Continuous);
		}

		private void InitializeTimer()
		{
			Debug.Print("InitializeTimer()");
			//_updateTimer = new System.Windows.Forms.Timer   //尝试用 System.Timers.Timer 替换 WinForms Timer
			//System.Windows.Forms.Timer 依赖UI线程消息泵，System.Timers.Timer 可以在后台线程触发，便于验证UI线程是否被阻塞。
			_updateTimer = new System.Timers.Timer   //尝试用 System.Timers.Timer 替换 WinForms Timer
			{
				Interval = 100 // Update 10 times per second
			};
			_updateTimer.Elapsed += UpdateTimer_Tick;
			_updateTimer.Start();
		}

		private void UpdateTimer_Tick(object? sender, EventArgs e)
		{
			//Debug.Print($"主线程ID:{MainForm.MainThreadId}, 当前线程ID:{Thread.CurrentThread.ManagedThreadId}");
			/*•	System.Timers.Timer.Elapsed 事件在线程池线程上触发，不能直接操作UI控件，必须用 Invoke 或 BeginInvoke 切回UI线程。
			•	OnUpdateTimer() 里如有UI操作，必须保证在UI线程执行。
			 */
			if (this.InvokeRequired)
				this.BeginInvoke(new Action(() => OnUpdateTimer()));
			else
				OnUpdateTimer();
		}
		private void OnUpdateTimer()
		{
			var _operationItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
			if (_operationItem != null && _operationItem.Queue?.Identifier != _queueIdentifier)
			{
				var queue = OperationsManager.Instance.GetQueueByIdentifier(_queueIdentifier);

				FinalizeOperation();
				if (queue != null && queue.IsFree)
				{
					_queueIdentifier = queue.Identifier;
					_operationHandle = GetFirstOperationHandle(queue.Identifier);
				}
				else
				{
					_queueIdentifier = _operationItem.Queue?.Identifier ?? 0;
					_operationHandle = _operationItem.Handle;
				}
				Debug.Print($"OnUpdateTimer() - queueIdentifier:{_queueIdentifier}, operationHandle:{_operationHandle}");
				if (!InitializeOperation())
				{
					CloseDialog();
					return;
				}
				_operationItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
			}
			//check if first operation in the queue has not changed
			if (_operationItem != null && !_operationItem.Queue.IsFree && GetFirstOperationHandle(_queueIdentifier) != _operationHandle)
			{
				FinalizeOperation();
				_operationHandle = GetFirstOperationHandle(_queueIdentifier);
				if (!InitializeOperation())
				{
					CloseDialog();
					return;
				}
				_operationItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
			}
			if (_operationItem != null)
			{
				UpdateOperation(_operationItem);
			}
			else // operation was destroyed
			{
				var queue = OperationsManager.Instance.GetQueueByIdentifier(_queueIdentifier);
				if (queue == null || queue.IsFree)
				{
					CloseDialog();
				}
				else
				{
					_operationHandle = GetFirstOperationHandle(_queueIdentifier);
					_operationItem = OperationsManager.Instance.GetItemByHandle(_operationHandle);
					_operationItem.Operation.AddUserInterface(_userInterface);
				}
			}
		}

		private void UpdateControls()
		{
			if (_operationItem == null || _operationItem.Operation == null)
				return;

			var operation = _operationItem.Operation;

			// Update operation text
			lblCurrentOperationText.Text = operation.Description + (operation.State).ToString();

			// Get statistics based on operation type
			FileSourceCopyOperationStatistics statistics = new();

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
			if (!statistics.Equals(default(FileSourceCopyOperationStatistics)))
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
			if (opManItem.Queue != null && opManItem.Queue.IsFree)
			{
				switch (opManItem.Operation.State)
				{
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

		private void BtnPauseStartClick(object? sender, EventArgs e)
		{
			if (_operationItem?.Operation != null)
			{
				_operationItem.Operation.TogglePause();
				UpdateControls();
			}
		}

		private void BtnCancelClick(object? sender, EventArgs e)
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

		private void BtnMinimizeToPanelClick(object? sender, EventArgs e)
		{
			// Switch to operations panel view
			GlobalSettings.FileOperationsProgressKind = FileOperationsProgressKind.OperationsPanel;
			_stopOperationOnClose = false;
			Close();
		}

		private void BtnViewOperationsClick(object? sender, EventArgs e)
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

		private void FormClose(object sender, FormClosingEventArgs e)
		{
			if (_updateTimer != null)
			{
				_updateTimer.Stop();
				_updateTimer.Dispose();
				_updateTimer = null; // 防止多次释放
			}

			if (_activeDialogs.TryGetValue(_operationHandle, out _))
			{
				_activeDialogs.Remove(_operationHandle);
				Debug.Print($"FormClosing: Removed active dialog for operation {_operationHandle}");
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

					if (!StopOperationOrQueue())
					{
						Debug.Print($"// user cancels the operation stop, cancel the form closing");
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
			dialog.Name = handle.ToString();
			_activeDialogs[handle] = dialog;

			// Show minimized if requested
			if (options.HasFlag(OperationProgressWindowOptions.StartMinimized))
			{
				dialog.WindowState = FormWindowState.Minimized;
			}
			if(!dialog.IsDisposed) 
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
