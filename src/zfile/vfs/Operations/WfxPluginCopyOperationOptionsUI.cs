using System.Windows.Forms;

namespace zfile
{
    public class WfxPluginCopyOperationOptionsUI : WfxPluginCopyMoveOperationOptionsUI
    {
        public WfxPluginCopyOperationOptionsUI(Control owner, IFileSource fileSource)
            : base(owner, fileSource)
        {
            // 特定于复制操作的初始化
        }
    }

	public class WfxPluginCopyMoveOperationOptionsUI : FileSourceOperationOptionsUI
	{
		public WfxPluginCopyMoveOperationOptionsUI(Control owner, IFileSource fileSource)
			: base(owner, fileSource)
		{
			// 特定于复制/移动操作的初始化
		}
	
	}
}
