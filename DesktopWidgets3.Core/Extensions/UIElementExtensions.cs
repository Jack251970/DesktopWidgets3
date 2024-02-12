namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for UI elements.
/// </summary>
public static class UIElementExtensions
{
    private static IWindowService? FallbackWindowService;

    public static void Initialize(IWindowService windowService)
    {
        FallbackWindowService = windowService;
    }

    public static BlankWindow BlankWindow => GetWindowEx();

    private static BlankWindow GetWindowEx()
    {
        if (FallbackWindowService is null)
        {
            throw new InvalidOperationException("Window service is not initialized.");
        }

        var window = new BlankWindow();
        FallbackWindowService.RegisterWindowEx(window);
        return window;
    }
}
