using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IThemeSelectorService
{
    ElementTheme Theme { get; }

    event Action<ElementTheme>? OnThemeChanged;

    Task InitializeAsync();

    Task SetRequestedThemeAsync(Window window);

    Task SetThemeAsync(ElementTheme theme);
}
