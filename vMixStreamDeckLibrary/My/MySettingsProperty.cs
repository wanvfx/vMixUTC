using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace vMixStreamDeckLibrary.My
{
	// Token: 0x0200000A RID: 10
	[CompilerGenerated]
	[HideModuleName]
	[StandardModule]
	[DebuggerNonUserCode]
	internal sealed class MySettingsProperty
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000019 RID: 25 RVA: 0x0000246C File Offset: 0x0000066C
		[HelpKeyword("My.Settings")]
		internal static MySettings Settings
		{
			get
			{
				return MySettings.Default;
			}
		}
	}
}
