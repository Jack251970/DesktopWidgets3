using System.Reflection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Resources.Core;

namespace CustomExtensions.WinUI;

public static partial class ApplicationExtensionHost
{
	internal static bool IsHotReloadEnabled => Environment.GetEnvironmentVariable("ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO") == "1";

	private static IApplicationExtensionHost? _Current;
	public static IApplicationExtensionHost Current => _Current ?? throw new InvalidOperationException("ApplicationExtensionHost is not initialized");

	public static void Initialize<TApplication>(TApplication application) where TApplication : Application
	{
		if (_Current != null)
		{
			throw new InvalidOperationException("Cannot initialize application twice");
		}

		_Current = new ApplicationExtensionHostSingleton<TApplication>(application);
	}

	/// <summary>
	/// Gets the default resource map for the specified assembly, or the caller's executing assembly if not provided.
	/// </summary>
	/// <param name="assembly">Assembly for which to load the default resource map</param>
	/// <returns>A ResourceMap if one is found, otherwise null</returns>
	public static ResourceMap? GetResourceMapForAssembly(Assembly? assembly = null)
	{
		assembly ??= Assembly.GetCallingAssembly();
		string? assemblyName = assembly.GetName().Name;
		if (assemblyName == null)
		{
			return null;
		}

		return !ResourceManager.Current.AllResourceMaps.TryGetValue(assemblyName, out ResourceMap? map)
			? null
			: map.GetSubtree($"{assemblyName}/Resources");
	}
}
