using Newtonsoft.Json;
using Windows.Storage;

namespace DesktopWidgets3.Core.Services;

// For MSIX package:
// Settings saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\Settings\settings.dat
// File saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\LocalState\{FileName}
public class LocalSettingsService : ILocalSettingsService
{
    private readonly IFileService _fileService;

    private readonly string _applicationDataPath;

    private readonly string _localsettingsFile;

    private Dictionary<string, object>? _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService)
    {
        _fileService = fileService;

        _localsettingsFile = Constant.LocalSettingsFile;

        _applicationDataPath = LocalSettingsHelper.ApplicationDataPath;

        if (!Directory.Exists(_applicationDataPath))
        {
            Directory.CreateDirectory(_applicationDataPath);
        }
    }

    #region App Settings

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        return await ReadSettingAsync(key, default(T));
    }

    public async Task<T?> ReadSettingAsync<T>(string key, T defaultValue)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await JsonHelper.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            await InitializeSettingsAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                return await JsonHelper.ToObjectAsync<T>((string)obj);
            }
        }

        return defaultValue;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        var stringValue = await JsonHelper.StringifyAsync(value!);

        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj) && ((string)obj) == stringValue)
            {
                return;
            }

            ApplicationData.Current.LocalSettings.Values[key] = stringValue;
        }
        else
        {
            await InitializeSettingsAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj) && (string)obj == stringValue)
            {
                return;
            }

            _settings![key] = stringValue;

            await _fileService.SaveAsync(_applicationDataPath, _localsettingsFile, _settings, true);
        }
    }

    private async Task InitializeSettingsAsync()
    {
        if (!_isInitialized)
        {
            _settings = await _fileService.ReadAsync<Dictionary<string, object>>(_applicationDataPath, _localsettingsFile) ?? [];

            _isInitialized = true;
        }
    }

    #endregion

    #region Json Files

    public async Task<T?> ReadJsonFileAsync<T>(string fileName, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return await _fileService.ReadAsync<T>(_applicationDataPath, fileName, jsonSerializerSettings) ?? default;
    }

    public async Task SaveJsonFileAsync<T>(string fileName, T value)
    {
        await _fileService.SaveAsync(_applicationDataPath, fileName, value, true);
    }

    #endregion
}
