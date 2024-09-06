namespace DesktopWidgets3.Core.Contracts.Services;

public interface ILocalSettingsService
{
    string GetApplicationDataFolder();

    Task<T?> ReadSettingAsync<T>(string key);

    Task<T?> ReadSettingAsync<T>(string key, T value);

    Task SaveSettingAsync<T>(string key, T value);

    Task<object> ReadWidgetListAsync();

    Task SaveWidgetListAsync(object value);
}
