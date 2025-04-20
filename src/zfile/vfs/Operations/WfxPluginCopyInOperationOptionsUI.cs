using System.Windows.Forms;

namespace zfile
{
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

            CbWorkInBackground_CheckedChanged(cbWorkInBackground, System.EventArgs.Empty);
        }
    }
}
