using Windows.Storage;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helpers for local settings.
/// </summary>
public class LocalSettingsHelper
{
    private static string applicationDataPath = string.Empty;
    public static string ApplicationDataPath => applicationDataPath;

    public static string WidgetsDirectory => Path.Combine(ApplicationDataPath, Constant.WidgetsFolder);

    public static void Initialize()
    {
        if (RuntimeHelper.IsMSIX)
        {
            applicationDataPath = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            applicationDataPath = Path.Combine(localAppDataPath, Constant.DesktopWidgets3, Constant.ApplicationDataFolder);
        }
    }
}
