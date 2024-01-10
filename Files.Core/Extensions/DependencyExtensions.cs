using Files.Shared.Extensions;

namespace Files.Core.Extensions;

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

    public static T GetRequiredService<T>() where T : class
    {
        return SafetyExtensions.IgnoreExceptions(() => FallbackDependencyService?.GetService<T>()) ?? null!;
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
