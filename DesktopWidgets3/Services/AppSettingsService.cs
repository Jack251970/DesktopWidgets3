using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;

namespace DesktopWidgets3.Services;

internal class AppSettingsService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys) : IAppSettingsService
{
    private readonly ILocalSettingsService _localSettingsService = localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys = localSettingsKeys.Value;

    private bool _isInitialized;

    public void Initialize()
    {
        if (!_isInitialized)
        {
            // initialize local settings
            Language = GetLanguage();
            SilentStart = GetSilentStart();
            BatterySaver = GetBatterySaver();
            MultiThread = GetMultiThread();
            Theme = GetTheme();
            BackdropType = GetBackdropType();

            // initialize efficiency mode
            EfficiencyModeUtilities.SetEfficiencyMode(BatterySaver);
            OnBatterySaverChanged += EfficiencyModeUtilities.SetEfficiencyMode;

            _isInitialized = true;
        }
    }

    #region Language

    private string language = null!;
    public string Language
    {
        get => language;
        private set
        {
            if (language != value)
            {
                language = value;
            }
        }
    }

    private string GetLanguage()
    {
        var data = GetDataFromSettings(_localSettingsKeys.LanguageKey, AppLanguageHelper.DefaultCode);
        return data;
    }

    public async Task SaveLanguageInSettingsAsync(string language)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.LanguageKey, language);
        Language = language;
    }

    #endregion

    #region Silent Start

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

    private const bool DefaultSilentStart = false;

    private bool GetSilentStart()
    {
        var data = GetDataFromSettings(_localSettingsKeys.SilentStartKey, DefaultSilentStart);
        return data;
    }

    public async Task SetSilentStartAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.SilentStartKey, value);
        SilentStart = value;
    }

    #endregion

    #region Battery Saver

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
                    OnBatterySaverChanged?.Invoke(value);
                }
            }
        }
    }

    private const bool DefaultBatterySaver = false;

    private bool GetBatterySaver()
    {
        var data = GetDataFromSettings(_localSettingsKeys.BatterySaverKey, DefaultBatterySaver);
        return data;
    }

    public async Task SetBatterySaverAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.BatterySaverKey, value);
        BatterySaver = value;
    }

    public event Action<bool>? OnBatterySaverChanged;

    #endregion

    #region Multi Thread

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

    private const bool DefaultMultiThread = false;

    private bool GetMultiThread()
    {
        var data = GetDataFromSettings(_localSettingsKeys.MultiThreadKey, DefaultMultiThread);
        return data;
    }

    public async Task SetMultiThreadAsync(bool value)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.MultiThreadKey, value);
        MultiThread = value;
    }

    #endregion

    #region Theme

    private ElementTheme theme;
    public ElementTheme Theme
    {
        get => theme;
        private set
        {
            if (theme != value)
            {
                theme = value;
            }
        }
    }

    private const ElementTheme DefaultTheme = ElementTheme.Default;

    private ElementTheme GetTheme()
    {
        var data = GetDataFromSettings(_localSettingsKeys.ThemeKey, DefaultTheme);
        return data;
    }

    public async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.ThemeKey, theme);
        Theme = theme;
    }

    #endregion

    #region Backdrop

    private BackdropType backdropType;
    public BackdropType BackdropType
    {
        get => backdropType;
        private set
        {
            if (backdropType != value)
            {
                backdropType = value;
            }
        }
    }

    private const BackdropType DefaultBackdropType = BackdropType.Mica;

    private BackdropType GetBackdropType()
    {
        var data = GetDataFromSettings(_localSettingsKeys.BackdropTypeKey, DefaultBackdropType);
        return data;
    }

    public async Task SaveBackdropTypeInSettingsAsync(BackdropType type)
    {
        await SaveDataInSettingsAsync(_localSettingsKeys.BackdropTypeKey, type);
        BackdropType = type;
    }

    #endregion

    #region Widget List

    private List<JsonWidgetItem> WidgetList = null!;

    private JsonSerializerSettings? WidgetListJsonSerializerSettings;

    public async Task<List<JsonWidgetItem>> InitializeWidgetListAsync()
    {
        WidgetListJsonSerializerSettings = new JsonSerializerSettings { Converters = { new JsonWidgetItemConverter() } };

        WidgetList = await _localSettingsService.ReadJsonFileAsync<List<JsonWidgetItem>>(Constants.WidgetListFile, WidgetListJsonSerializerSettings) ?? [];

        return WidgetList;
    }

    public List<JsonWidgetItem> GetWidgetsList()
    {
        return WidgetList;
    }

    public JsonWidgetItem? GetWidget(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        var index = WidgetList.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1)
        {
            return WidgetList[index];
        }

        return null;
    }

    public async Task AddWidgetAsync(JsonWidgetItem item)
    {
        var index = WidgetList.FindIndex(x => x.Equals(item.ProviderType, item.Id, item.Type, item.Index));
        if (index == -1)
        {
            WidgetList.Add(item);

            await SaveWidgetListAsync();
        }
    }

    public async Task DeleteWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        var index = WidgetList.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1)
        {
            WidgetList.RemoveAt(index);

            await SaveWidgetListAsync();
        }
    }

    public async Task PinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        var index = WidgetList.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1 && WidgetList[index].Pinned == false)
        {
            WidgetList[index].Pinned = true;

            await SaveWidgetListAsync();
        }
    }

    public async Task UnpinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        var index = WidgetList.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1 && WidgetList[index].Pinned == true)
        {
            WidgetList[index].Pinned = false;

            await SaveWidgetListAsync();
        }
    }

    public async Task UpdateWidgetSettingsAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings)
    {
        var index = WidgetList.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1)
        {
            WidgetList[index].Settings = settings;

            await SaveWidgetListAsync();
        }
    }

    public async Task UpdateWidgetsListIgnoreSettingsAsync(List<JsonWidgetItem> list)
    {
        foreach (var item in list)
        {
            var index = WidgetList.FindIndex(x => x.Equals(item.ProviderType, item.Id, item.Type, item.Index));
            if (index != -1)
            {
                var setting = WidgetList[index].Settings;
                WidgetList[index] = item;
                WidgetList[index].Settings = setting;
            }
        }

        await SaveWidgetListAsync();
    }

    private async Task SaveWidgetListAsync()
    {
        var valueCopy = new List<JsonWidgetItem>(WidgetList);

        await _localSettingsService.SaveJsonFileAsync(Constants.WidgetListFile, valueCopy);
    }

    #endregion

    #region Widget Store

    private List<JsonWidgetStoreItem> WidgetStoreList = null!;

    public async Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync()
    {
        WidgetStoreList = await _localSettingsService.ReadJsonFileAsync<List<JsonWidgetStoreItem>>(Constants.WidgetStoreListFile) ?? [];

        return WidgetStoreList;
    }

    public async Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> list)
    {
        WidgetStoreList = list;

        await SaveWidgetStoreListAsync();
    }

    public List<JsonWidgetStoreItem> GetWidgetStoreList()
    {
        return WidgetStoreList;
    }

    private async Task SaveWidgetStoreListAsync()
    {
        var valueCopy = new List<JsonWidgetStoreItem>(WidgetStoreList);

        await _localSettingsService.SaveJsonFileAsync(Constants.WidgetStoreListFile, valueCopy);
    }

    #endregion

    #region Helper Methods

    private T GetDataFromSettings<T>(string settingsKey, T defaultData)
    {
        var data = _localSettingsService.ReadSetting<string>(settingsKey);

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
        else if (typeof(T) == typeof(string) && data?.ToString() is string cacheStringData)
        {
            return (T)(object)cacheStringData;
        }
        else if (typeof(T).IsEnum && Enum.TryParse(typeof(T), data, out var cacheEnumData))
        {
            return (T)cacheEnumData;
        }

        return defaultData;
    }

    private async Task SaveDataInSettingsAsync<T>(string settingsKey, T data)
    {
        await _localSettingsService.SaveSettingAsync(settingsKey, data!.ToString());
    }

    #endregion
}
