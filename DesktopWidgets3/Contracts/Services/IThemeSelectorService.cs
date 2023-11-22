using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IThemeSelectorService
{
    ElementTheme Theme
    {
        get;
    }

    Task InitializeAsync();

    Task SetRequestedThemeAsync(Window window);

    Task SetThemeAsync(ElementTheme theme);
}
