using System.Reflection;

namespace CustomExtensions.WinUI;

internal partial class ExtensionAssembly
{
	internal static readonly Assembly? EntryAssembly;
	internal static readonly string HostingProcessDir;

	static ExtensionAssembly()
	{
		EntryAssembly = Assembly.GetEntryAssembly();
		HostingProcessDir = Path.GetDirectoryName(EntryAssembly.AssertDefined().Location).AssertDefined();
	}
}
