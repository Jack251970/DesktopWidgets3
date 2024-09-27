using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IThemeSelectorService
{
    /// <summary>
    /// Occurs when the theme has changed, either due to user selection or the system theme changing.
    /// </summary>
    public event EventHandler<ElementTheme>? ThemeChanged;

    ElementTheme Theme { get; }

    Task InitializeAsync();

    Task SetRequestedThemeAsync(Window window);

    Task SetThemeAsync(ElementTheme theme);

    /// <summary>
    /// Checks if the <see cref="Theme"/> value resolves to dark
    /// </summary>
    /// <returns>True if the current theme is dark</returns>
    bool IsDarkTheme();

    ElementTheme GetActualTheme();
}
