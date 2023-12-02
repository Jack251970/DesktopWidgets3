using Microsoft.Extensions.Options;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Core.Contracts.Services;
using DesktopWidgets3.Core.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using DesktopWidgets3.Models.Widget;

using Windows.Storage;
using Newtonsoft.Json;

namespace DesktopWidgets3.Services;

// For MSIX package:
// Settings saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\Settings\settings.dat
// WidgetList saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\LocalState\WidgetList.json
public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "DesktopWidgets3/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";
    private const string _defaultWidgetListFile = "WidgetList.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _applicationDataFolder;
    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _localsettingsFile;
    private readonly string _widgetListFile;

    private IDictionary<string, object>? _settings;

    private readonly JsonSerializerSettings _widgetListJsonSerializerSettings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;
        _widgetListFile = _options.WidgetListFile ?? _defaultWidgetListFile;
        
        if (RuntimeHelper.IsMSIX)
        {
            _applicationDataFolder = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        }

        if (!Directory.Exists(_applicationDataFolder))
        {
            Directory.CreateDirectory(_applicationDataFolder);
        }

        _widgetListJsonSerializerSettings = new JsonSerializerSettings { Converters = { new JsonWidgetItemConverter() }};
    }

    public string GetApplicationDataFolder()
    {
        return _applicationDataFolder;
    }

    private async Task InitializeSettingsAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            await InitializeSettingsAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        var stringValue = await Json.StringifyAsync(value);

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

    public async Task<List<JsonWidgetItem>> ReadWidgetListAsync()
    {
        var _widgetList = await Task.Run(() => _fileService.Read<List<JsonWidgetItem>>(_applicationDataFolder, _widgetListFile, _widgetListJsonSerializerSettings)) ?? new List<JsonWidgetItem>();

        return _widgetList;
    }

    public async Task SaveWidgetListAsync(List<JsonWidgetItem> value)
    {
        await Task.Run(() => _fileService.Save(_applicationDataFolder, _widgetListFile, value, true));
    }
}
