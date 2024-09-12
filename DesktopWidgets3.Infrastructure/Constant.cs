namespace DesktopWidgets3.Infrastructure;

public static class Constant
{
    public const string StartupRegistryKey = "Desktop Widgets 3";

    public const string StartupTaskId = "StartAppOnLoginTask";

    public const string Widgets = "Widgets";

    public const string WidgetMetadataFileName = "widget.json";

    public static readonly string WidgetsPreinstalledDirectory = Path.Combine(AppContext.BaseDirectory, Widgets);

    public const string UnknownWidgetIcoPath = $"ms-appx:///Assets/FluentIcons/Unknown.png";

    public const string DefaultLocalSettingsFile = "LocalSettings.json";

    public const string WidgetListFile = "WidgetList.json";

    public const string WidgetStoreListFile = "WidgetStoreList.json";
}
