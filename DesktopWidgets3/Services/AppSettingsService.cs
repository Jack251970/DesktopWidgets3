using Microsoft.Extensions.Options;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;

namespace DesktopWidgets3.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys;

    public AppSettingsService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys)
    {
        _localSettingsService = localSettingsService;
        _localSettingsKeys = localSettingsKeys.Value;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        WidgetList = await _localSettingsService.ReadWidgetListAsync();

        BatterySaver = await GetBatterySaverAsync();
        ForbidQuit = await GetForbidQuitAsync();
        IsLocking = IsRelaxing = false;
    }

    /************ Runtime Application Data *************/

    public bool IsLocking
    {
        get; set;
    }

    public bool IsRelaxing
    {
        get; set;
    }

    public bool IsTiming => IsLocking || IsRelaxing;

    public bool BatterySaver
    {
        get; set;
    }

    public bool ForbidQuit
    {
        get; set;
    }

    /************ Default Storage Data *************/

    private const bool DefaultBatterySaver = false;
    private const bool DefaultShowSeconds = true;
    private const bool DefaultStrictMode = false;
    private const bool DefaultForbidQuit = false;
    private const int DefaultBreakInterval = 60;
    private static readonly DateTime DefaultTime = new(2020, 1, 1, 0, 0, 0);

    /************ Individually-Stored Data *************/

    public async Task<bool> GetBatterySaverAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.BatterySaverKey, DefaultBatterySaver);
        return data;
    }

    public async Task SetBatterySaverAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.BatterySaverKey, value);
        BatterySaver = value;
    }

    public async Task<bool> GetShowSecondsAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.ShowSecondsKey, DefaultShowSeconds);
        return data;
    }

    public async Task SetShowSecondsAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.ShowSecondsKey, value);
    }

    public async Task<bool> GetStrictModeAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.StrictModeKey, DefaultStrictMode);
        return data;
    }

    public async Task SetStrictModeAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.StrictModeKey, value);
    }

    public async Task<int> GetBreakIntervalAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.BreakIntervalKey, DefaultBreakInterval);
        return data;
    }

    public async Task SetBreakIntervalAsync(int value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.BreakIntervalKey, value);
    }

    public async Task<bool> GetForbidQuitAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.ForbidQuitKey, DefaultForbidQuit);
        return data;
    }

    public async Task SetForbidQuitAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.ForbidQuitKey, value);
        ForbidQuit = value;
    }

    /************ WidgetList *************/

    private List<JsonWidgetItem> WidgetList = new();

    public List<JsonWidgetItem> GetWidgetsList()
    {
        return WidgetList;
    }

    public async Task SaveWidgetsList(JsonWidgetItem widgetItem)
    {
        var index = WidgetList.FindIndex(x => x.Type == widgetItem.Type);
        if (index == -1)
        {
            WidgetList.Add(widgetItem);
        }
        else
        {
            WidgetList[index] = widgetItem;
        }

        await _localSettingsService.SaveWidgetListAsync(WidgetList);
    }

    /************ Simultaneously-Stored Data & Method *************/

    public async Task<Dictionary<string, object>> GetLockPeriod()
    {
        var startLockTime = await GetDataFromSettingsAsync(_localSettingsKeys.StartLockTimeKey, DefaultTime);
        var endLockTime = await GetDataFromSettingsAsync(_localSettingsKeys.EndLockTimeKey, DefaultTime);
        Dictionary<string, object> lockPeriod = new()
        {
            {
                "StartLockTime",
                startLockTime
            },
            {
                "EndLockTime",
                endLockTime
            }
        };
        return lockPeriod;
    }

    public async Task SaveLockPeriod(DateTime startLockTime, DateTime endLockTime)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.StartLockTimeKey, startLockTime);
        await SaveDataInSettingsAsync(_localSettingsKeys.EndLockTimeKey, endLockTime);
    }

    /************ Storage Ultility Method *************/

    private async Task<T> GetDataFromSettingsAsync<T>(string settingsKey, T defaultData)
    {
        var data = await _localSettingsService.ReadSettingAsync<string>(settingsKey);

        if (typeof(T) == typeof(bool) && bool.TryParse(data, out var cacheBoolData))
        {
            return (T)(object)cacheBoolData;
        }
        else if (typeof(T) == typeof(int) && int.TryParse(data, out var cacheIntData))
        {
            return (T)(object)cacheIntData;
        }
        else if (typeof(T) == typeof(DateTime) && DateTime.TryParse(data, out var cacheDateTimeData))
        {
            return (T)(object)cacheDateTimeData;
        }

        return defaultData;
    }

    private async Task SaveDataInSettingsAsync<T>(string settingsKey, T data)
    {
        await _localSettingsService.SaveSettingAsync(settingsKey, data!.ToString());
    }
}
