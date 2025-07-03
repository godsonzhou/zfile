namespace zfile
{
	public class MyListView : ListView
	{
		public event EventHandler HScroll;
		public event EventHandler VScroll;
		public event EventHandler MouseWheel;
		public MyListView()
		{
			//this.HScroll += new EventHandler(OnHScroll);
			//this.VScroll += new EventHandler(OnVScroll);
			//this.MouseWheel += new EventHandler(OnMouseWheel);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			this.UpdateStyles();
		}
		const int WM_HSCROLL = 0x0114;
		const int WM_VSCROLL = 0x0115;
		private const int WM_CHAR = 0x102;
		private const int WM_MOUSEWHEEL = 0x20a;
		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			// 拦截WM_CHAR消息，阻止ListView处理键盘输入
			if (m.Msg == WM_CHAR && !MainForm.Instance.uiManager.isquicksearch)
			{
				// 如果你想允许某些特定字符通过，可以在这里添加条件判断
				// 例如：if ((char)m.WParam == 'A') { base.WndProc(ref m); return; }
				return;
			}
			if (m.Msg == WM_HSCROLL)
			{
				//OnHScroll(this, new EventArgs());
				//if (HScroll != null)
				HScroll?.Invoke(this, new EventArgs());
			}
			else if (m.Msg == WM_VSCROLL)
			{
				//OnVScroll(this, new EventArgs());
				//if (VScroll != null)
				VScroll?.Invoke(this, new EventArgs());
				//Debug.Print("vscroll event trigger");
			}
			else if (m.Msg == WM_MOUSEWHEEL)
			{
				//OnMouseWheel(this, new EventArgs());
				//if (MouseWheel != null)
				MouseWheel?.Invoke(this, new EventArgs());
			}
			base.WndProc(ref m);
		}
		//virtual protected void OnMouseWheel(object sender, EventArgs e)
		//{
		//	Debug.Print("mouse wheel trigger!");
		//}
		//virtual protected void OnHScroll(object sender, EventArgs e) { }
		//virtual protected void OnVScroll(object sender, EventArgs e) 
		//{
		//	Debug.Print("onvscroll virtual");
		//}
	}
}