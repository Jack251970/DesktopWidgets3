using Microsoft.Extensions.Options;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;

namespace DesktopWidgets3.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys;

    private bool _isInitialized;

    public AppSettingsService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys)
    {
        _localSettingsService = localSettingsService;
        _localSettingsKeys = localSettingsKeys.Value;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        BatterySaver = await GetBatterySaverAsync();
        // WidgetList = await _localSettingsService.ReadWidgetListAsync();
    }

    #region Runtime Application Data

    public bool BatterySaver
    {
        get; set;
    }

    #endregion

    #region Local Application Data

    private const bool DefaultBatterySaver = false;

    private async Task<bool> GetBatterySaverAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.BatterySaverKey, DefaultBatterySaver);
        return data;
    }

    public async Task SetBatterySaverAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.BatterySaverKey, value);
        BatterySaver = value;
    }

    #endregion

    #region Widget List Data

    private List<JsonWidgetItem> WidgetList = new();

    public async Task<List<JsonWidgetItem>> GetWidgetsList()
    {
        if (!_isInitialized)
        {
            WidgetList = await _localSettingsService.ReadWidgetListAsync();

            _isInitialized = true;
        }
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

    #endregion

    #region Storage Ultility Method

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

#endregion
}
