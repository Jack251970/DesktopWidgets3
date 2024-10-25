using Windows.Storage;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helpers for local settings.
/// </summary>
public class LocalSettingsHelper
{
    private static string applicationDataPath = string.Empty;
    public static string ApplicationDataPath => applicationDataPath;

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

    public static void Initialize()
    {
        if (RuntimeHelper.IsMSIX)
        {
            applicationDataPath = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            applicationDataPath = Path.Combine(localAppDataPath, Constants.LocalAppDataFolder, Constants.ApplicationDataFolder);
        }
    }
}
