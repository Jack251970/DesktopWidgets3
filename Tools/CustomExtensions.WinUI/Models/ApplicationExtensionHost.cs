using System.Reflection;
using Microsoft.UI.Xaml;

namespace CustomExtensions.WinUI.Models;

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
	public static Windows.ApplicationModel.Resources.Core.ResourceMap? GetCoreResourceMapForAssembly(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyName = assembly.GetName().Name;
        if (assemblyName == null)
        {
            return null;
        }

        return !Windows.ApplicationModel.Resources.Core.ResourceManager.Current.AllResourceMaps.TryGetValue(assemblyName, out var map)
            ? null
            : map.GetSubtree($"{assemblyName}/Resources");
    }

    /// <summary>
    /// Gets the default resource map for the specified assembly, or the caller's executing assembly if not provided.
    /// </summary>
    /// <param name="assembly">Assembly for which to load the default resource map</param>
    /// <returns>A ResourceMap if one is found, otherwise null</returns>
    public static Microsoft.Windows.ApplicationModel.Resources.ResourceMap? GetWinResourceMapForAssembly(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyName = assembly.GetName().Name;
        var assemblyPath = assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        if (assemblyName == null)
        {
            return null;
        }

        return new Microsoft.Windows.ApplicationModel.Resources.ResourceManager($"{assemblyDirectory}\\{assemblyName}.pri").MainResourceMap.TryGetSubtree($"{assemblyName}/Resources");
    }
}
