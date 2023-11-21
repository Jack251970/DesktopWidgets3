using Microsoft.Extensions.Options;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Core.Contracts.Services;
using DesktopWidgets3.Core.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;

using Windows.Storage;

namespace DesktopWidgets3.Services;

// For MSIX package:
// Settings saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\Settings\settings.dat
// BlockList saved in C:\Users\<UserName>\AppData\Local\Packages\<PackageFamilyName>\LocalState\BlockList.json
public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "DesktopWidgets3/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";
    private const string _defaultBlockListFile = "BlockList.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _applicationDataFolder;
    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _localsettingsFile;
    private readonly string _blockListFile;

    private IDictionary<string, object>? _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;
        _blockListFile = _options.BlockListFile ?? _defaultBlockListFile;
        
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

            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
        }
    }

    public async Task<List<string>> ReadBlockListAsync()
    {
        var _blockList = await Task.Run(() => _fileService.Read<List<string>>(_applicationDataFolder, _blockListFile)) ?? new List<string>();

        return _blockList;
    }

    public async Task SaveBlockListAsync(List<string> value)
    {
        await Task.Run(() => _fileService.Save(_applicationDataFolder, _blockListFile, value));
    }
}
