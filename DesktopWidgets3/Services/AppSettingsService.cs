using Microsoft.Extensions.Options;

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

    // TODO: Use await InitializeWidgetList() once.
    // TODO: Check GetWidgetsListAsync.
    public async Task<List<JsonWidgetItem>> GetWidgetsList()
    {
        await InitializeWidgetList();

        return WidgetList;
    }

    public async Task AddWidget(JsonWidgetItem widgetItem)
    {
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Id == widgetItem.Id && x.IndexTag == widgetItem.IndexTag);
        if (index == -1)
        {
            WidgetList.Add(widgetItem);

            await _localSettingsService.SaveWidgetListAsync(WidgetList);
        }
    }

    public async Task DeleteWidget(string widgetId, int indexTag)
    {
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (index != -1)
        {
            WidgetList.RemoveAt(index);

            await _localSettingsService.SaveWidgetListAsync(WidgetList);
        }
    }

    public async Task<JsonWidgetItem> EnableWidget(string widgetId, int indexTag)
    {
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (index != -1 && WidgetList[index].IsEnabled == false)
        {
            WidgetList[index].IsEnabled = true;

            await _localSettingsService.SaveWidgetListAsync(WidgetList);
        }

        return WidgetList[index];
    }

    public async Task DisableWidget(string widgetId, int indexTag)
    {
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (index != -1 && WidgetList[index].IsEnabled == true)
        {
            WidgetList[index].IsEnabled = false;

            await _localSettingsService.SaveWidgetListAsync(WidgetList);
        }
    }

    public async Task UpdateWidgetSettings(string widgetId, int indexTag, BaseWidgetSettings settings)
    {
        await InitializeWidgetList();

        var index = WidgetList.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (index != -1)
        {
            WidgetList[index].Settings = settings;

            await _localSettingsService.SaveWidgetListAsync(WidgetList);
        }
    }

    public async Task UpdateWidgetsListIgnoreSettings(List<JsonWidgetItem> widgetList)
    {
        await InitializeWidgetList();

        foreach (var widget in widgetList)
        {
            var index = WidgetList.FindIndex(x => x.Id == widget.Id && x.IndexTag == widget.IndexTag);
            if (index != -1)
            {
                var setting = WidgetList[index].Settings;
                WidgetList[index] = widget;
                WidgetList[index].Settings = setting;
            }
        }

        await _localSettingsService.SaveWidgetListAsync(WidgetList);
    }

    private async Task InitializeWidgetList()
    {
        WidgetList ??= (List<JsonWidgetItem>)await _localSettingsService.ReadWidgetListAsync();
    }

    #endregion

    #region Widget Store Data

    private List<JsonWidgetStoreItem> WidgetStoreList = null!;

    public async Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync()
    {
        WidgetStoreList = (List<JsonWidgetStoreItem>)await _localSettingsService.ReadWidgetStoreListAsync();

        return WidgetStoreList;
    }

    public async Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> widgetStoreList)
    {
        await _localSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
    }

    public List<JsonWidgetStoreItem> GetWidgetStoreList()
    {
        return WidgetStoreList;
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
