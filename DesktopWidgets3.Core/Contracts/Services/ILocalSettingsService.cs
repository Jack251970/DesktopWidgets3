using Newtonsoft.Json;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key);

    Task<T?> ReadSettingAsync<T>(string key, T defaultValue);

    Task SaveSettingAsync<T>(string key, T value);

    Task<T?> ReadJsonFileAsync<T>(string fileName, JsonSerializerSettings? jsonSerializerSettings = null);

    Task SaveJsonFileAsync<T>(string fileName, T value);
}
