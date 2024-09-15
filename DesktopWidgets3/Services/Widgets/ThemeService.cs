using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class ThemeService : IThemeService
{
    private static IThemeSelectorService ThemeSelectorService => DependencyExtensions.GetRequiredService<IThemeSelectorService>();

    ElementTheme IThemeService.RootTheme => ThemeSelectorService.Theme;

    event Action<ElementTheme>? IThemeService.OnThemeChanged
    {
        add => ThemeSelectorService.OnThemeChanged += value;
        remove => ThemeSelectorService.OnThemeChanged -= value;
    }
}
