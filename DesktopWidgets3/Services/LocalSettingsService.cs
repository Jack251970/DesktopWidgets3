using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Windows.Storage;

// TODO: Move to DesktopWidgets3.Core.Services namespace
namespace DesktopWidgets3.Services;

// For MSIX package:
// Settings saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\Settings\settings.dat
// File saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\LocalState\{FileName}.json
internal class LocalSettingsService : ILocalSettingsService
{
    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _applicationDataFolder;

    private readonly string _localsettingsFile;

    private Dictionary<string, object>? _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _localsettingsFile = _options.LocalSettingsFile ?? Constant.DefaultLocalSettingsFile;

        _applicationDataFolder = LocalSettingsExtensions.GetApplicationDataFolder();

        if (!Directory.Exists(_applicationDataFolder))
        {
            Directory.CreateDirectory(_applicationDataFolder);
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

            await _fileService.SaveAsync(_applicationDataFolder, _localsettingsFile, _settings, false);
        }
    }

    private async Task InitializeSettingsAsync()
    {
        if (!_isInitialized)
        {
            _settings = await _fileService.ReadAsync<Dictionary<string, object>>(_applicationDataFolder, _localsettingsFile) ?? [];

            _isInitialized = true;
        }
    }

    #endregion

    #region Json Files

    public async Task<T?> ReadJsonFileAsync<T>(string fileName, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return await _fileService.ReadAsync<T>(_applicationDataFolder, fileName, jsonSerializerSettings) ?? default;
    }

    public async Task SaveJsonFileAsync(string fileName, object value)
    {
        await _fileService.SaveAsync(_applicationDataFolder, fileName, value, false);
    }

    #endregion
}
