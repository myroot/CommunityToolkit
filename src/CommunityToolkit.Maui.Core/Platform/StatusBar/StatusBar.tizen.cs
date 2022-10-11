using System.Runtime.Versioning;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Platform;

static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("WinUI does not currently support changing the status bar color");
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		throw new NotSupportedException("WinUI does not currently support changing the status bar color");
	}
}