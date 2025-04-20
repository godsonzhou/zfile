using System.Windows.Forms;

namespace zfile
{
    public class WfxPluginMoveOperationOptionsUI : WfxPluginCopyMoveOperationOptionsUI
    {
        public WfxPluginMoveOperationOptionsUI(Control owner, IFileSource fileSource)
            : base(owner, fileSource)
        {
            // 特定于移动操作的初始化
        }
    }
}
