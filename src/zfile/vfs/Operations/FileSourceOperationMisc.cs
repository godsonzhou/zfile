using System.Globalization;

namespace zfile
{
    /// <summary>
    /// Miscellaneous functions for file source operations and queues
    /// </summary>
    public static class FileSourceOperationMisc
    {
        /// <summary>
        /// Gets a string representation of an operation state
        /// </summary>
        /// <param name="operationState">The operation state</param>
        /// <returns>String representation of the operation state</returns>
        public static string GetOperationStateString(FileSourceOperationState operationState)
        {
            if (operationState != FileSourceOperationState.Running)
                return " [" + GetFileSourceOperationStateText(operationState) + "]";
            else
                return string.Empty;
        }

        /// <summary>
        /// Gets the text representation of a file source operation state
        /// </summary>
        /// <param name="state">The operation state</param>
        /// <returns>Text representation of the state</returns>
        private static string GetFileSourceOperationStateText(FileSourceOperationState state)
        {
            switch (state)
            {
                case FileSourceOperationState.NotStarted:
                    return "Not Started";
                case FileSourceOperationState.Paused:
                    return "Paused";
                case FileSourceOperationState.Stopping:
                    return "Stopping";
                case FileSourceOperationState.Stopped:
                    return "Stopped";
                case FileSourceOperationState.WaitingForFeedback:
                    return "Waiting for Feedback";
                case FileSourceOperationState.WaitingForConnection:
                    return "Waiting for Connection";
                case FileSourceOperationState.Starting:
                    return "Starting";
                default:
                    return state.ToString();
            }
        }

        /// <summary>
        /// Gets a string representation of a progress value
        /// </summary>
        /// <param name="progress">The progress value (0.0 to 1.0)</param>
        /// <returns>String representation of the progress</returns>
        public static string GetProgressString(double progress)
        {
            return (progress * 100).ToString("F0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Shows an operation in a non-modal window
        /// </summary>
        /// <param name="operationItem">The operation item</param>
        public static void ShowOperation(OperationsManagerItem operationItem)
        {
            if (operationItem.Queue.IsFree || operationItem.Queue.Count == 1)
            {
                if (GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindow ||
                    GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindowMinimized)
                {
                    OperationProgressWindowOptions options = OperationProgressWindowOptions.None;

                    if (GlobalSettings.FileOperationsProgressKind == FileOperationsProgressKind.SeparateWindowMinimized)
                        options |= OperationProgressWindowOptions.StartMinimized;

                    FileOperationDialog.ShowFor(operationItem.Handle, options);
                }
            }
        }

        /// <summary>
        /// Shows an operation in a modal window
        /// </summary>
        /// <param name="operationItem">The operation item</param>
        public static void ShowOperationModal(OperationsManagerItem operationItem)
        {
            using (var dialog = new FileOperationDialog(operationItem.Handle))
            {
                dialog.ShowDialog();
            }
        }
    }

    /// <summary>
    /// File operations progress kind
    /// </summary>
    public enum FileOperationsProgressKind
    {
        /// <summary>
        /// No progress indication
        /// </summary>
        None,

        /// <summary>
        /// Progress in separate window
        /// </summary>
        SeparateWindow,

        /// <summary>
        /// Progress in separate minimized window
        /// </summary>
        SeparateWindowMinimized,

        /// <summary>
        /// Progress in operations panel
        /// </summary>
        OperationsPanel
    }

    /// <summary>
    /// Operation progress window options
    /// </summary>
    [Flags]
    public enum OperationProgressWindowOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0,

        /// <summary>
        /// Start minimized
        /// </summary>
        StartMinimized = 1,

        /// <summary>
        /// If window exists, bring it to front
        /// </summary>
        IfExistsBringToFront = 2
    }

    /// <summary>
    /// File operation dialog for displaying progress of file operations
    /// </summary>
    public class FileOperationDialog : Form
    {
        private int _operationHandle;
        private OperationsManagerItem _operationItem;
        private System.Windows.Forms.Timer _updateTimer;
        private static Dictionary<int, FileOperationDialog> _activeDialogs = new Dictionary<int, FileOperationDialog>();

        // UI Controls
        private Panel pnlClient;
        private Panel pnlQueue;
        private Label lblCurrentOperation;
        private Label lblCurrentOperationText;
        private Panel pnlFrom;
        private Label lblFrom;
        private Label lblFileNameFrom;
        private Panel pnlTo;
        private Label lblTo;
        private Label lblFileNameTo;
        private Label lblEstimated;
        private ProgressBar pbCurrent;
        private ProgressBar pbTotal;
        private Panel pnlButtons;
        private Button btnMinimizeToPanel;
        private Button btnViewOperations;
        private Button btnCancel;
        private Button btnPauseStart;
        private Label lblFileCount;

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

            InitializeComponent();
            InitializeTimer();
            UpdateControls();
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
            btnMinimizeToPanel.Click += btnMinimizeToPanelClick;
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
            btnViewOperations.Click += btnViewOperationsClick;
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
            btnCancel.Click += btnCancelClick;
            pnlButtons.Controls.Add(btnCancel);

            // Pause/Start button
            btnPauseStart = new Button
            {
                Dock = DockStyle.Right,
                Text = "&Pause",
                AutoSize = true,
                Width = 50
            };
            btnPauseStart.Click += btnPauseStartClick;
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
                if (statistics.RemainingTime.TotalSeconds > 0)
                {
                    TimeSpan time = statistics.RemainingTime;
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

        private void UpdatePauseStartButton(FileSourceOperationState state)
        {
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

        private void btnPauseStartClick(object sender, EventArgs e)
        {
            if (_operationItem?.Operation != null)
            {
                _operationItem.Operation.TogglePause();
                UpdateControls();
            }
        }

        private void btnCancelClick(object sender, EventArgs e)
        {
            if (_operationItem?.Operation != null)
            {
                _operationItem.Operation.Stop();
            }
        }

        private void btnMinimizeToPanelClick(object sender, EventArgs e)
        {
            // Switch to operations panel view
            GlobalSettings.FileOperationsProgressKind = FileOperationsProgressKind.OperationsPanel;
            Close();
        }

        private void btnViewOperationsClick(object sender, EventArgs e)
        {
            // Show operations manager window
            // This would typically open a window showing all operations
            // Implementation depends on how operations are managed in the application
        }

        private void FormClose(object sender, FormClosingEventArgs e)
        {
            _updateTimer.Stop();
            _updateTimer.Dispose();
            
            if (_activeDialogs.ContainsKey(_operationHandle))
                _activeDialogs.Remove(_operationHandle);
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