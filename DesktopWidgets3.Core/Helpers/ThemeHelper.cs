using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Helpers;

public static class ThemeHelper
{
    public static async Task SetRequestedThemeAsync(Window window, ElementTheme theme)
    {
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;

            TitleBarHelper.UpdateTitleBar(window, theme);
        }

        await Task.CompletedTask;
    }
}
