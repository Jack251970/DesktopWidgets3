using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget;

public interface IThemeService
{
    ElementTheme RootTheme { get; }

    event EventHandler<ElementTheme>? ThemeChanged;
}
