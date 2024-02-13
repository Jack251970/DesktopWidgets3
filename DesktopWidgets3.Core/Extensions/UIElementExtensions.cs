using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for UI elements.
/// </summary>
public static class UIElementExtensions
{
    private static IWindowService? FallbackWindowService;

    public static readonly List<Window> WindowInstances = new();

    public static void Initialize(IWindowService windowService)
    {
        FallbackWindowService = windowService;
    }

    public static BlankWindow GetWindowEx()
    {
        if (FallbackWindowService is null)
        {
            throw new InvalidOperationException("Window service is not initialized.");
        }

        var window = new BlankWindow();

        // Handle window registration and unregistration
        RegisterWindow(window);

        // Activate window using fallback service
        FallbackWindowService.ActivateWindow(window);

        return window;
    }

    public static void RegisterWindow(Window window)
    {
        if (!WindowInstances.Contains(window))
        {
            WindowInstances.Add(window);
            window.Closed += (sender, args) => UnregisterWindow(window);
        }
    }

    public static void UnregisterWindow(Window window)
    {
        if (WindowInstances.Contains(window))
        {
            WindowInstances.Remove(window);
        }
    }

    public static void CloseAllWindows()
    {
        var windowInstances = WindowInstances.ToList();
        foreach (var window in windowInstances)
        {
            window.Close();
        }
    }
}
