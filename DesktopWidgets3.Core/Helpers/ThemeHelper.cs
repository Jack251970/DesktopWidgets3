using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Helpers;

public static class ThemeHelper
{
    public static void SetRequestedThemeAsync(Window window, ElementTheme theme)
    {
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;

            TitleBarHelper.UpdateTitleBar(window, window.AppWindow.TitleBar, theme);
        }
    }
}
