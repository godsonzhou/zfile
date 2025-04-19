using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace zfile.vfs.Operations
{
	// Queue identifiers for operations
	public static class QueueIdentifiers
	{
		public const int FreeOperationsQueueId = 0;
		public const int ModalQueueId = -1;
		public const int SingleQueueId = 1;
	}

	/// <summary>
	/// Copy dialog interface for WFX plugin operations
	/// </summary>
	public interface ICopyDialog
	{
		int QueueIdentifier { get; set; }
		Button BtnAddToQueue { get; }
		Button BtnCreateSpecialQueue { get; }
	}

	public class WfxPluginCopyMoveOperationOptionsUI : FileSourceOperationOptionsUI
	{
		private readonly CheckBox cbCopyTime = new();
		protected readonly CheckBox cbWorkInBackground = new();
		private readonly ComboBox cmbFileExists = new();
		private readonly GroupBox grpOptions = new();
		private readonly Label lblFileExists = new();
		private readonly Panel pnlCheckboxes = new();
		private readonly Panel pnlComboBoxes = new();

		// Reference to the parent control that owns this UI
		protected Control? ParentControl { get; private set; }

		// Store the file source for later use
		protected IFileSource? FileSourceInstance { get; private set; }

		public WfxPluginCopyMoveOperationOptionsUI(Control owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			ParentControl = owner;
			FileSourceInstance = fileSource;
			InitializeComponent();
			LoadDefaultOptions();
		}

		private void InitializeComponent()
		{
			// Initialize UI components
			// This would be auto-generated in a Windows Forms designer

			// Set up the controls
			grpOptions.Text = "Options";
			lblFileExists.Text = "File exists:";

			// Set up the copy time checkbox
			cbCopyTime.Text = "Copy time";
			cbCopyTime.AutoSize = true;

			// Set up the work in background checkbox
			cbWorkInBackground.Text = "Work in background";
			cbWorkInBackground.AutoSize = true;
			cbWorkInBackground.CheckedChanged += CbWorkInBackground_CheckedChanged;
		}

		private void LoadDefaultOptions()
		{
			// Ensure the ComboBox is initialized
			if (cmbFileExists.Items.Count == 0 && Resources.FileOpCopyMoveFileExistsOptions != null)
			{
				cmbFileExists.Items.AddRange(Resources.FileOpCopyMoveFileExistsOptions.Split('\n'));
			}

			switch (GlobalSettings.OperationOptionFileExists)
			{
				case FileSourceOperationOptionFileExists.None:
					cmbFileExists.SelectedIndex = 0;
					break;
				case FileSourceOperationOptionFileExists.Overwrite:
					cmbFileExists.SelectedIndex = 1;
					break;
				case FileSourceOperationOptionFileExists.Skip:
					cmbFileExists.SelectedIndex = 2;
					break;
			}

			// Get the file source and check if it's a WFX plugin file source
			if (FileSourceInstance is IWfxPluginFileSource wfxFileSource)
			{
				var wfxModule = wfxFileSource.WfxModule;
				cbCopyTime.Visible = wfxModule._fsSetTime != null || wfxModule._fsSetTimeW != null;
				cbCopyTime.Checked = cbCopyTime.Visible && GlobalSettings.OperationOptionCopyTime;
			}
		}

		public override void SaveOptions()
		{
			// TODO: Implement saving options for each file source operation
		}

		public override void SetOperationOptions(object operation)
		{
			if (operation is WfxPluginCopyOperation copyOperation)
			{
				SetOperationOptions(copyOperation);
			}
			else if (operation is WfxPluginMoveOperation moveOperation)
			{
				SetOperationOptions(moveOperation);
			}
			else if (operation is WfxPluginCopyInOperation copyInOperation)
			{
				SetOperationOptions(copyInOperation);
			}
			else if (operation is WfxPluginCopyOutOperation copyOutOperation)
			{
				SetOperationOptions(copyOutOperation);
			}
		}

		private void SetCopyOptions(FileSourceCopyOperation copyOperation)
		{
			if (cbCopyTime.Checked)
			{
				copyOperation.CopyAttributesOptions |= CopyAttributesOption.CopyTime;
			}
			else
			{
				copyOperation.CopyAttributesOptions &= ~CopyAttributesOption.CopyTime;
			}
		}

		private void SetOperationOptions(WfxPluginCopyOperation copyOperation)
		{
			copyOperation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
			SetCopyOptions(copyOperation);
		}

		private void SetOperationOptions(WfxPluginMoveOperation moveOperation)
		{
			moveOperation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
		}

		private void SetOperationOptions(WfxPluginCopyInOperation copyInOperation)
		{
			copyInOperation.NeedsConnection = !cbWorkInBackground.Checked;
			copyInOperation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
			SetCopyOptions(copyInOperation);
		}

		private void SetOperationOptions(WfxPluginCopyOutOperation copyOutOperation)
		{
			copyOutOperation.NeedsConnection = !cbWorkInBackground.Checked;
			copyOutOperation.FileExistsOption = (FileSourceOperationOptionFileExists)cmbFileExists.SelectedIndex;
			SetCopyOptions(copyOutOperation);
		}

		protected void CbWorkInBackground_CheckedChanged(object? sender, EventArgs e)
		{
			// Check if the parent control implements ICopyDialog
			if (ParentControl is ICopyDialog copyDialog)
			{
				if (!cbWorkInBackground.Checked)
				{
					copyDialog.QueueIdentifier = QueueIdentifiers.ModalQueueId;
				}
				else
				{
					copyDialog.QueueIdentifier = QueueIdentifiers.SingleQueueId;
				}
				copyDialog.BtnAddToQueue.Visible = cbWorkInBackground.Checked;
				copyDialog.BtnCreateSpecialQueue.Visible = copyDialog.BtnAddToQueue.Visible;
			}
		}
	}

	public class WfxPluginCopyInOperationOptionsUI : WfxPluginCopyMoveOperationOptionsUI
	{
		public WfxPluginCopyInOperationOptionsUI(Control owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			const int CAN_UPLOAD = (int)(BackgroundFlags.Upload | BackgroundFlags.AskUser);
			var wfxModule = ((IWfxPluginFileSource)fileSource).WfxModule;

			cbWorkInBackground.Visible = ((int)wfxModule.BackgroundFlags & CAN_UPLOAD) == CAN_UPLOAD;
			if (cbWorkInBackground.Visible)
			{
				cbWorkInBackground.Checked = false;
			}
			else
			{
				cbWorkInBackground.Checked = (wfxModule.BackgroundFlags & BackgroundFlags.Upload) != 0;
			}

			CbWorkInBackground_CheckedChanged(cbWorkInBackground, EventArgs.Empty);
		}
	}

	public class WfxPluginCopyOutOperationOptionsUI : WfxPluginCopyMoveOperationOptionsUI
	{
		public WfxPluginCopyOutOperationOptionsUI(Control owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			const int CAN_DOWNLOAD = (int)(BackgroundFlags.Downloaded | BackgroundFlags.AskUser);
			var wfxModule = ((IWfxPluginFileSource)fileSource).WfxModule;

			cbWorkInBackground.Visible = ((int)wfxModule.BackgroundFlags & CAN_DOWNLOAD) == CAN_DOWNLOAD;
			if (cbWorkInBackground.Visible)
			{
				cbWorkInBackground.Checked = false;
			}
			else
			{
				cbWorkInBackground.Checked = (wfxModule.BackgroundFlags & BackgroundFlags.Downloaded) != 0;
			}

			CbWorkInBackground_CheckedChanged(cbWorkInBackground, EventArgs.Empty);
		}
	}
}