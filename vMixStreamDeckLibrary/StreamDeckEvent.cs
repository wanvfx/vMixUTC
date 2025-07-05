using System;

namespace vMixStreamDeckLibrary
{
	// Token: 0x02000010 RID: 16
	public enum StreamDeckEvent
	{
		// Token: 0x0400001C RID: 28
		None,
		// Token: 0x0400001D RID: 29
		KeyDown,
		// Token: 0x0400001E RID: 30
		KeyUp,
		// Token: 0x0400001F RID: 31
		Error,
		// Token: 0x04000020 RID: 32
		Info,
		// Token: 0x04000021 RID: 33
		ButtonRegistered,
		// Token: 0x04000022 RID: 34
		TouchTap = 10,
		// Token: 0x04000023 RID: 35
		DialDown = 15,
		// Token: 0x04000024 RID: 36
		DialUp,
		// Token: 0x04000025 RID: 37
		DialRotate = 20
	}
}
