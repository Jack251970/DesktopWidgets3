using Windows.Storage;

namespace DesktopWidgets3.Core.Helpers;

public class LocalSettingsHelper
{
    public static string ApplicationDataPath { get; private set; } = null!;

    public static void Initialize()
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationDataPath = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ApplicationDataPath = Path.Combine(localAppDataPath, Constant.DesktopWidgets3, Constant.ApplicationDataFolder);
        }
    }
}
