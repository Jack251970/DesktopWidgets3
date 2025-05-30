﻿using Windows.Storage;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helpers for local settings.
/// </summary>
public class LocalSettingsHelper
{
    private static string applicationDataPath = string.Empty;
    public static string ApplicationDataPath => applicationDataPath;

    public static void Initialize()
    {
        if (RuntimeHelper.IsMSIX)
        {
            applicationDataPath = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            applicationDataPath = Path.Combine(appDataPath, Constants.DesktopWidgets3, Constants.ApplicationDataFolder);
        }
    }

    public static string LogDirectory
    {
        get
        {
            var logDirectory = Path.Combine(ApplicationDataPath, Constants.LogsFolder);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            return logDirectory;
        }
    }

    public static string DefaultUserWidgetsDirectory
    {
        get
        {
            var userWidgetsDirectory = Path.Combine(applicationDataPath, Constants.WidgetsFolder);
            if (!Directory.Exists(userWidgetsDirectory))
            {
                Directory.CreateDirectory(userWidgetsDirectory);
            }
            return userWidgetsDirectory;
        }
    }
}
