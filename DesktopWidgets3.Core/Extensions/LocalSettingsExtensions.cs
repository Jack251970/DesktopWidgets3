using DesktopWidgets3.Core.Helpers;
using Windows.Storage;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for local settings.
/// </summary>
public static class LocalSettingsExtensions
{
    private static readonly string DefaultApplicationDataFolder = "DesktopWidgets3/ApplicationData";
    private static string ApplicationDataFolder { get; set; } = null!;
    private static readonly List<string> SubFolders = new();

    public static void Initialize()
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationDataFolder = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ApplicationDataFolder = Path.Combine(localAppDataPath, DefaultApplicationDataFolder);
#if DEBUG
            ApplicationDataFolder = Path.Combine(ApplicationDataFolder, "Debug");
#endif
        }
    }

    public static bool RegisterSubFolder(string subFolder)
    {
        if (SubFolders.Contains(subFolder))
        {
            return false;
        }

        SubFolders.Add(subFolder);
        return true;
    }

    public static string GetApplicationDataFolder(string? subFolder = null)
    {
        if (subFolder is null)
        {
            return ApplicationDataFolder;
        }
        else if (SubFolders.Contains(subFolder))
        {
            var folder = Path.Combine(ApplicationDataFolder, subFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }
        else
        {
            throw new ArgumentException($"Sub folder \"{subFolder}\" needs to be registered in App.xaml.cs.");
        }
    }
}
