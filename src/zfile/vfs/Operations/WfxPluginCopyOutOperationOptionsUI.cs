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

         
        }
    }
}
