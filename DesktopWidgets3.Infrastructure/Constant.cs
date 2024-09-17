﻿namespace DesktopWidgets3.Infrastructure;

public static class Constant
{
    public const string DesktopWidgets3 = "DesktopWidgets3";

    #region Startup

    public const string StartupRegistryKey = "Desktop Widgets 3";

    public const string StartupTaskId = "StartAppOnLoginTask";

    #endregion
    
    #region Resources

    public const string DefaultResourceFileName = "Resources";

    public const string UnknownWidgetIcoPath = $"ms-appx:///Assets/FluentIcons/Unknown.png";

    #endregion

    #region Local Settings & Logs

#if DEBUG
    public const string ApplicationDataFolder = "ApplicationData(Debug)";
#else
    public const string ApplicationDataFolder = "ApplicationData";
#endif

    public const string LocalSettingsFolder = "Settings";
    
    public const string SettingsFile = "LocalSettings.json";

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
