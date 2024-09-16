namespace DesktopWidgets3.Infrastructure;

public static class Constant
{
    public const string DesktopWidgets3 = "DesktopWidgets3";

    public const string StartupRegistryKey = "Desktop Widgets 3";

    public const string StartupTaskId = "StartAppOnLoginTask";

    public const string WidgetsFolder = "Widgets";

    public const string WidgetMetadataFileName = "widget.json";

    public static readonly string WidgetsPreinstalledDirectory = Path.Combine(AppContext.BaseDirectory, WidgetsFolder);

    public const string UnknownWidgetIcoPath = $"ms-appx:///Assets/FluentIcons/Unknown.png";

    public const string LocalSettingsFile = "LocalSettings.json";

    public const string WidgetListFile = "WidgetList.json";

    public const string WidgetStoreListFile = "WidgetStoreList.json";

#if DEBUG
    public const string ApplicationDataFolder = "ApplicationData(Debug)";
#else
    public const string ApplicationDataFolder = "ApplicationData";
#endif

    public const string LogsFolder = "Logs";

    public const string DefaultResourceFileName = "Resources";
}
