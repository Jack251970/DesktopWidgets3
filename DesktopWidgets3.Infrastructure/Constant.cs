namespace DesktopWidgets3.Infrastructure;

public static class Constant
{
    public const string StartupRegistryKey = "Desktop Widgets 3";

    public const string StartupTaskId = "StartAppOnLoginTask";

    public const string Widgets = "Widgets";

    public const string WidgetMetadataFileName = "widget.json";

    public static readonly string WidgetsPreinstalledDirectory = Path.Combine(AppContext.BaseDirectory, Widgets);

    public static readonly string UnknownWidgetIcoPath = $"ms-appx:///Assets/FluentIcons/Unknown.png";
}
