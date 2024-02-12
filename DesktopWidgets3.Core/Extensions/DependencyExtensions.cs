using Files.Core.Services;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for dependency injection.
/// </summary>
public static class DependencyExtensions
{
	private static IDependencyService? FallbackDependencyService;

    public static void Initialize(IDependencyService dependencyService)
    {
        FallbackDependencyService = dependencyService;
    }

    public static T GetService<T>() where T : class
    {
        if (FallbackDependencyService is null)
        {
            throw new InvalidOperationException("Dependency service is not initialized.");
        }

        return FallbackDependencyService.GetService<T>();
    }
}
