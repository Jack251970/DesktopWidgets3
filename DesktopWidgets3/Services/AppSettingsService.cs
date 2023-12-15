using Microsoft.Extensions.Options;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;
using DesktopWidgets3.Models.Widget;

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
    }

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            SilentStart = await GetSilentStartAsync();
            BatterySaver = await GetBatterySaverAsync();

            _isInitialized = true;
        }
    }

    #region Runtime Application Data

    public bool SilentStart
    {
        get; set;
    }

    private bool _batterySaver;
    public bool BatterySaver
    {
        get => _batterySaver;
        set
        {
            if (_batterySaver != value)
            {
                _batterySaver = value;
                if (_isInitialized)
                {
                    App.GetService<ISystemInfoService>().OnBatterySaverChanged(value);
                }
            }
        }
    }

    #endregion

    #region Local Application Data

    private const bool DefaultSilentStart = false;

    private async Task<bool> GetSilentStartAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.SilentStartKey, DefaultSilentStart);
        return data;
    }

    public async Task SetSilentStartAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.SilentStartKey, value);
        SilentStart = value;
    }

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
    private bool _isWidgetListInitialized;

    public async Task<List<JsonWidgetItem>> GetWidgetsList()
    {
        if (!_isWidgetListInitialized)
        {
            WidgetList = await _localSettingsService.ReadWidgetListAsync();

            _isWidgetListInitialized = true;
        }
        return WidgetList;
    }

    public async Task UpdateWidgetsList(JsonWidgetItem widgetItem)
    {
        var index = WidgetList.FindIndex(x => x.Type == widgetItem.Type && x.IndexTag == widgetItem.IndexTag);
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

    public async Task UpdateWidgetsList(List<JsonWidgetItem> widgetList)
    {
        foreach (var widget in widgetList)
        {
            var index = WidgetList.FindIndex(x => x.Type == widget.Type && x.IndexTag == widget.IndexTag);
            if (index != -1)
            {
                WidgetList[index] = widget;
            }
        }

        await _localSettingsService.SaveWidgetListAsync(WidgetList);
    }

    public async Task DeleteWidgetsList(JsonWidgetItem widgetItem)
    {
        var index = WidgetList.FindIndex(x => x.Type == widgetItem.Type && x.IndexTag == widgetItem.IndexTag);
        if (index != -1)
        {
            WidgetList.RemoveAt(index);
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
