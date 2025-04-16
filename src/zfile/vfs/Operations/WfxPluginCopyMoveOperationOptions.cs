using System.ComponentModel;

namespace zfile
{
	public class WfxPluginCopyMoveOperationOptionsUI : FileSourceOperationOptionsUI
	{
		private CheckBox cbCopyTime;
		private CheckBox cbWorkInBackground;
		private ComboBox cmbFileExists;
		private GroupBox grpOptions;
		private Label lblFileExists;
		private Panel pnlCheckboxes;
		private Panel pnlComboBoxes;

		public WfxPluginCopyMoveOperationOptionsUI(Component owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			InitializeComponent();
			LoadDefaultOptions();
		}

		private void InitializeComponent()
		{
			// Initialize UI components
			// This would be auto-generated in a Windows Forms designer
		}

		private void LoadDefaultOptions()
		{
			cmbFileExists.Items.AddRange(Resources.FileOpCopyMoveFileExistsOptions.Split('\n'));

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

			var wfxModule = ((IWfxPluginFileSource)FileSource).WfxModule;
			cbCopyTime.Visible = wfxModule.FsSetTime != null || wfxModule.FsSetTimeW != null;
			cbCopyTime.Checked = cbCopyTime.Visible && GlobalSettings.OperationOptionCopyTime;
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
				copyOperation.CopyAttributesOptions |= CopyAttributesOptions.CopyTime;
			}
			else
			{
				copyOperation.CopyAttributesOptions &= ~CopyAttributesOptions.CopyTime;
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

		private void CbWorkInBackground_CheckedChanged(object sender, EventArgs e)
		{
			var copyDialog = (CopyDialog)Owner;
			if (!cbWorkInBackground.Checked)
			{
				copyDialog.QueueIdentifier = ModalQueueId;
			}
			else
			{
				copyDialog.QueueIdentifier = SingleQueueId;
			}
			copyDialog.btnAddToQueue.Visible = cbWorkInBackground.Checked;
			copyDialog.btnCreateSpecialQueue.Visible = copyDialog.btnAddToQueue.Visible;
		}
	}

	public class WfxPluginCopyInOperationOptionsUI : WfxPluginCopyMoveOperationOptionsUI
	{
		public WfxPluginCopyInOperationOptionsUI(Component owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			const int CAN_UPLOAD = BackgroundFlags.Upload | BackgroundFlags.AskUser;
			var wfxModule = ((IWfxPluginFileSource)fileSource).WfxModule;

			cbWorkInBackground.Visible = (wfxModule.BackgroundFlags & CAN_UPLOAD) == CAN_UPLOAD;
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
		public WfxPluginCopyOutOperationOptionsUI(Component owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			const int CAN_DOWNLOAD = BackgroundFlags.Download | BackgroundFlags.AskUser;
			var wfxModule = ((IWfxPluginFileSource)fileSource).WfxModule;

			cbWorkInBackground.Visible = (wfxModule.BackgroundFlags & CAN_DOWNLOAD) == CAN_DOWNLOAD;
			if (cbWorkInBackground.Visible)
			{
				cbWorkInBackground.Checked = false;
			}
			else
			{
				cbWorkInBackground.Checked = (wfxModule.BackgroundFlags & BackgroundFlags.Download) != 0;
			}

			CbWorkInBackground_CheckedChanged(cbWorkInBackground, EventArgs.Empty);
		}
	}
}