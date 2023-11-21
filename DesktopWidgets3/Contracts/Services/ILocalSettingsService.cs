namespace DesktopWidgets3.Contracts.Services;

public interface ILocalSettingsService
{
    string GetApplicationDataFolder();

    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);

    Task<List<string>> ReadBlockListAsync();

    Task SaveBlockListAsync(List<string> value);
}
