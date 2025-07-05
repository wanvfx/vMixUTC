using System;

namespace vMixStreamDeckLibrary
{
	// Token: 0x02000018 RID: 24
	public class WebSocketEventArgs : EventArgs
	{
		// Token: 0x06000067 RID: 103 RVA: 0x00002293 File Offset: 0x00000493
		public WebSocketEventArgs(string msg)
		{
			this.Message = msg;
		}

		// Token: 0x04000040 RID: 64
		public string Message;
	}
}
