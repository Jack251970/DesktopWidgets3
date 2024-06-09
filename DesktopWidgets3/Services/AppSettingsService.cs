﻿using Microsoft.Extensions.Options;

namespace DesktopWidgets3.Services;

internal class AppSettingsService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys) : IAppSettingsService
{
    private readonly ILocalSettingsService _localSettingsService = localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys = localSettingsKeys.Value;

    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            SilentStart = await GetSilentStartAsync();
            BatterySaver = await GetBatterySaverAsync();
            MultiThread = await GetMultiThreadAsync();

            _isInitialized = true;
        }
    }

    #region Runtime Application Data

    private bool silentStart;
    public bool SilentStart
    {
        get => silentStart;
        private set
        {
            if (silentStart != value)
            {
                silentStart = value;
            }
        }
    }

    public event EventHandler<bool>? OnBatterySaverChanged;

    private bool batterySaver;
    public bool BatterySaver
    {
        get => batterySaver;
        private set
        {
            if (batterySaver != value)
            {
                batterySaver = value;
                if (_isInitialized)
                {
                    OnBatterySaverChanged?.Invoke(this, value);
                }
            }
        }
    }

    private bool multiThread;
    public bool MultiThread
    {
        get => multiThread;
        private set
        {
            if (multiThread != value)
            {
                multiThread = value;
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

    private const bool DefaultMultiThread = false;

    private async Task<bool> GetMultiThreadAsync()
    {
        var data = await GetDataFromSettingsAsync(_localSettingsKeys.MultiThreadKey, DefaultMultiThread);
        return data;
    }

    public async Task SetMultiThreadAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.MultiThreadKey, value);
        MultiThread = value;
    }

    #endregion

    #region Widget List Data

    private List<JsonWidgetItem> WidgetList = null!;

    public async Task<List<JsonWidgetItem>> GetWidgetsList()
    {
        await InitializeWidgetList();

        return WidgetList;
    }

    public async Task UpdateWidgetsList(JsonWidgetItem widgetItem)
    {
        await InitializeWidgetList();

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
        await InitializeWidgetList();

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
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Type == widgetItem.Type && x.IndexTag == widgetItem.IndexTag);
        if (index != -1)
        {
            WidgetList.RemoveAt(index);
        }

        await _localSettingsService.SaveWidgetListAsync(WidgetList);
    }

    private async Task InitializeWidgetList()
    {
        WidgetList ??= (List<JsonWidgetItem>)await _localSettingsService.ReadWidgetListAsync();
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
