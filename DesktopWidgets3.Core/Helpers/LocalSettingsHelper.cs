using Windows.Storage;

namespace DesktopWidgets3.Core.Helpers;

public class LocalSettingsHelper
{
    private static string applicationDataPath = string.Empty;
    public static string ApplicationDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(applicationDataPath))
            {
                Initialize();
            }
            return applicationDataPath;
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
            applicationDataPath = Path.Combine(localAppDataPath, Constant.DesktopWidgets3, Constant.ApplicationDataFolder);
        }
    }
}
