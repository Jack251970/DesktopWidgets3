using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Services;

public interface IThemeService
{
    ElementTheme RootTheme { get; }

    event EventHandler<ElementTheme>? ThemeChanged;
}
