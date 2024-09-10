namespace DesktopWidgets3.Core.Widgets.Utils;

public static class StringUtils
{
    public static string GetRandomWidgetId()
    {
        return Guid.NewGuid().ToString("N").ToUpper();
    }
}
