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
        BatterySaver = await GetBatterySaverAsync();
        ForbidQuit = await GetForbidQuitAsync();
        BlockList = await _localSettingsService.ReadBlockListAsync();
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

    /************ BlockList *************/

    private List<string> BlockList = new();

    public List<string> GetBlockList()
    {
        return BlockList;
    }

    public async Task SaveBlockList(string exeName, bool isBlock)
    {
        var count = BlockList.Count;
        if (isBlock)
        {
            BlockList.Add(exeName);
        }
        else
        {
            BlockList.Remove(exeName);
        }
#if DEBUG
        await Task.CompletedTask;
#else
        if (BlockList.Count != count)
        {
            await _localSettingsService.SaveBlockListAsync(BlockList);
        }
#endif
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
