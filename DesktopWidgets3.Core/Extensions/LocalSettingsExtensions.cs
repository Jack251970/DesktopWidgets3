using DesktopWidgets3.Contracts.Services;
using Windows.Storage;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for local settings.
/// </summary>
public static class LocalSettingsExtensions
{
#if DEBUG
    private static readonly string DefaultApplicationDataFolder = "DesktopWidgets3/ApplicationData(Debug)";
#else
    private static readonly string DefaultApplicationDataFolder = "DesktopWidgets3/ApplicationData";
#endif
    private static string ApplicationDataFolder { get; set; } = null!;
    private static readonly List<string> SubFolders = new();

    private static ILocalSettingsService? FallbackLocalSettingsService;

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

    public static void RegisterService(ILocalSettingsService localSettingsService)
    {
        FallbackLocalSettingsService = localSettingsService;
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

    public static T? ReadLocalSetting<T>(string key)
    {
        var task = ReadLocalSettingAsync<T>(key);
        task.Wait();

        return task.Result;
    }

    public static T? ReadLocalSetting<T>(string key, T value)
    {
        var task = ReadLocalSettingAsync(key, value);
        task.Wait();

        return task.Result;
    }

    public static Task<T?> ReadLocalSettingAsync<T>(string key)
    {
        if (FallbackLocalSettingsService is null)
        {
            throw new InvalidOperationException("Local settings service not initialized.");
        }

        return FallbackLocalSettingsService.ReadSettingAsync<T>(key);
    }

    public static Task<T?> ReadLocalSettingAsync<T>(string key, T value)
    {
        if (FallbackLocalSettingsService is null)
        {
            throw new InvalidOperationException("Local settings service not initialized.");
        }

        return FallbackLocalSettingsService.ReadSettingAsync(key, value);
    }

    public static Task SaveLocalSettingAsync<T>(string key, T value)
    {
        if (FallbackLocalSettingsService is null)
        {
            throw new InvalidOperationException("Local settings service not initialized.");
        }

        return FallbackLocalSettingsService.SaveSettingAsync(key, value);
    }
}
