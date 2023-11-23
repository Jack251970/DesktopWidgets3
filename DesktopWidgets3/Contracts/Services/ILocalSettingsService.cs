using DesktopWidgets3.Models;

namespace DesktopWidgets3.Contracts.Services;

public interface ILocalSettingsService
{
    string GetApplicationDataFolder();

    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);

    Task<List<JsonWidgetItem>> ReadWidgetListAsync();

    Task SaveWidgetListAsync(List<JsonWidgetItem> value);
}
