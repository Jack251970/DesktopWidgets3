namespace DesktopWidgets3.Infrastructure;

public static class Constants
{
    #region Program

    public const string DesktopWidgets3 = "DesktopWidgets3";

    #endregion

    #region Startup

    public const string StartupTaskId = "StartAppOnLoginTask";

    public const string StartupRegistryKey = DesktopWidgets3;

    public const string StartupLogonTaskName = $"{DesktopWidgets3} Startup";

    public const string StartupLogonTaskDesc = $"{DesktopWidgets3} Auto Startup";

    #endregion

    #region Resources

    public const string DefaultResourceFileName = "Resources";

    public static readonly string AppIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icon.ico");

    public const string UnknownWidgetIcoPath = $"ms-appx:///Assets/FluentIcons/Unknown.png";

    #endregion

    #region Settings & Logs

#if DEBUG
    public const string ApplicationDataFolder = "ApplicationData(Debug)";
#else
    public const string ApplicationDataFolder = "ApplicationData";
#endif

    public const string SettingsFolder = "Settings";
    
    public const string SettingsFile = "Settings.json";

    public const string WidgetListFile = "WidgetList.json";

    public const string WidgetStoreListFile = "WidgetStoreList.json";

    public const string LogsFolder = "Logs";

    #endregion

    #region Widgets

    public const string WidgetMetadataFileName = "widget.json";

    public static readonly string PreinstalledWidgetsDirectory = Path.Combine(AppContext.BaseDirectory, WidgetsFolder);

    public const string WidgetsFolder = "Widgets";

    #endregion
}
