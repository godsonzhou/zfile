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

      
        }
    }
}
