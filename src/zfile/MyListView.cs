using System.Diagnostics;

public class MyListView : ListView
{
	public event EventHandler HScroll;
	public event EventHandler VScroll;
	public event EventHandler MouseWheel;
	public MyListView() {
		//this.HScroll += new EventHandler(OnHScroll);
		//this.VScroll += new EventHandler(OnVScroll);
		//this.MouseWheel += new EventHandler(OnMouseWheel);
	}
	const int WM_HSCROLL = 0x0114;
	const int WM_VSCROLL = 0x0115;
	private const int WM_MOUSEWHEEL = 0x20a;
	protected override void WndProc(ref Message m) {
		if (m.Msg == WM_HSCROLL)
		{
			//OnHScroll(this, new EventArgs());
			if(HScroll != null)
				HScroll(this, new EventArgs());
		}
		else if (m.Msg == WM_VSCROLL) {
			//OnVScroll(this, new EventArgs());
			if(VScroll != null)
				VScroll(this, new EventArgs());
			//Debug.Print("vscroll event trigger");
		}
		else if (m.Msg == WM_MOUSEWHEEL)
		{
			//OnMouseWheel(this, new EventArgs());
			if (MouseWheel != null)
				MouseWheel(this, new EventArgs());
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
