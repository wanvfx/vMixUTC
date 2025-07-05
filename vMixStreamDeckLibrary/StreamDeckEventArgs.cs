using System;

namespace vMixStreamDeckLibrary
{
	// Token: 0x0200000F RID: 15
	public class StreamDeckEventArgs : EventArgs
	{
		// Token: 0x06000038 RID: 56 RVA: 0x00002141 File Offset: 0x00000341
		public StreamDeckEventArgs(StreamDeckEvent t, string msg, StreamDeckButton btn, int v)
		{
			this.Type = t;
			this.Message = msg;
			this.Button = btn;
			this.Value = v;
		}

		// Token: 0x04000017 RID: 23
		public StreamDeckEvent Type;

		// Token: 0x04000018 RID: 24
		public string Message;

		// Token: 0x04000019 RID: 25
		public StreamDeckButton Button;

		// Token: 0x0400001A RID: 26
		public int Value;
	}
}
