using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Windows.Storage;

namespace DesktopWidgets3.Services;

// For MSIX package:
// Settings saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\Settings\settings.dat
// WidgetList saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\LocalState\WidgetList.json
internal class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultLocalSettingsFile = "LocalSettings.json";
    private const string _defaultWidgetListFile = "WidgetList.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _applicationDataFolder;

    private readonly string _localsettingsFile;
    private readonly string _widgetListFile;

    private Dictionary<string, object>? _settings;

    private JsonSerializerSettings? _widgetListJsonSerializerSettings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;
        _widgetListFile = _options.WidgetListFile ?? _defaultWidgetListFile;

        _applicationDataFolder = LocalSettingsExtensions.GetApplicationDataFolder();

        if (!Directory.Exists(_applicationDataFolder))
        {
            Directory.CreateDirectory(_applicationDataFolder);
        }
    }

    public string GetApplicationDataFolder()
    {
        return _applicationDataFolder;
    }

    private async Task InitializeSettingsAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<Dictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? [];

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        return await ReadSettingAsync(key, default(T));
    }

    public async Task<T?> ReadSettingAsync<T>(string key, T value)
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

        return value;
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

            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings, false));
        }
    }

    public async Task<object> ReadWidgetListAsync()
    {
        _widgetListJsonSerializerSettings ??= new JsonSerializerSettings { Converters = { new JsonWidgetItemConverter() } };

        var _widgetList = await Task.Run(() => _fileService.Read<List<JsonWidgetItem>>(_applicationDataFolder, _widgetListFile, _widgetListJsonSerializerSettings)) ?? [];

        return _widgetList;
    }

    public async Task SaveWidgetListAsync(object value)
    {
        var valueCopy = new List<JsonWidgetItem>((List<JsonWidgetItem>)value);

        await Task.Run(() => _fileService.Save(_applicationDataFolder, _widgetListFile, valueCopy, true));
    }
}
