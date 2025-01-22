using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class ThemeService(IThemeSelectorService themeSelectorService) : IThemeService
{
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

    ElementTheme IThemeService.RootTheme => _themeSelectorService.Theme;

    public event EventHandler<ElementTheme> ThemeChanged = (_, _) => { };

    event EventHandler<ElementTheme>? IThemeService.ThemeChanged
    {
        add => _themeSelectorService.ThemeChanged += value;
        remove => _themeSelectorService.ThemeChanged -= value;
    }
}
