using Files.Core.Services;
using Microsoft.Extensions.Logging;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for dependency injection.
/// </summary>
public static class DependencyExtensions
{
	private static IDependencyService? FallbackDependencyService;

    private static bool _isInitialized;

    public static void Initialize(IDependencyService dependencyService)
    {
        if (!_isInitialized)
        {
            FallbackDependencyService = dependencyService;

            _isInitialized = true;
        }
    }

    private static T GetRequiredService<T>() where T : class
    {
        try
        {
            return FallbackDependencyService?.GetService<T>()!;
        }
        catch (Exception)
        {
            return null!;
        }
    }

    public static T GetService<T>() where T : class
    {
        if (typeof(T) == typeof(ILogger))
        {
            return GetRequiredService<T>();
        }

        if (FallbackDependencyService is null)
        {
            throw new InvalidOperationException("Dependency service is not initialized.");
        }

        return FallbackDependencyService.GetService<T>();
    }
}
