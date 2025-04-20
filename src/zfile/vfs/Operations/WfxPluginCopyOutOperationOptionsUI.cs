using System.Windows.Forms;

namespace zfile
{
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

            CbWorkInBackground_CheckedChanged(cbWorkInBackground, System.EventArgs.Empty);
        }
    }
}
