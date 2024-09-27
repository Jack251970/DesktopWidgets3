using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class ThemeService : IThemeService
{
    private static IThemeSelectorService ThemeSelectorService => DependencyExtensions.GetRequiredService<IThemeSelectorService>();

    ElementTheme IThemeService.RootTheme => ThemeSelectorService.Theme;

    public event EventHandler<ElementTheme> ThemeChanged = (_, _) => { };

    event EventHandler<ElementTheme>? IThemeService.ThemeChanged
    {
        add => ThemeSelectorService.ThemeChanged += value;
        remove => ThemeSelectorService.ThemeChanged -= value;
    }
}
