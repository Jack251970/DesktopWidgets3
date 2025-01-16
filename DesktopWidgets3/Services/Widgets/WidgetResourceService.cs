using System.Collections.Concurrent;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(DispatcherQueue dispatcherQueue, IAppSettingsService appSettingsService, IWidgetIconService widgetIconService, IWidgetScreenshotService widgetScreenshotService, MicrosoftWidgetModel microsoftWidgetModel) : IWidgetResourceService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetResourceService));

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly IWidgetIconService _widgetIconService = widgetIconService;
    private readonly IWidgetScreenshotService _widgetScreenshotService = widgetScreenshotService;
    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;

    private List<WidgetGroupPair> InstalledWidgetGroupPairs { get; set; } = null!;
    private List<WidgetGroupMetadata> AllWidgetGroupMetadatas { get; set; } = null!;

    private readonly Dictionary<string, Dictionary<string, string>> WidgetsLanguageResources = [];

    private static readonly string[] WidgetsDirectories =
    [
        LocalSettingsHelper.DefaultUserWidgetsDirectory
    ];

    private static readonly string[] PreinstalledWigdetsIds =
    [
        "7A0C8F221280461E9B02D3CFF2D2BD35",  // Clock
        "34EAD000AD4840E985009002128F654C",  // System Info
    ];

    #region Initialization

    public async Task InitalizeAsync()
    {
        // initialize microsoft widget resources
        await _microsoftWidgetModel.InitializeResourcesAsync();

        // get all widget metadata
        GetAllWidgetsMetadata();

        // load all installed widgets
        await LoadAllInstalledWidgets();

        // initialize all widgets
        await InitWidgetsAsync();

        // initialize widgets language resources
        InitWidgetsLanguageResources();
    }

    private void GetAllWidgetsMetadata()
    {
        // get all widget metadata
        AllWidgetGroupMetadatas = WidgetsConfig.Parse(WidgetsDirectories, Constants.PreinstalledWidgetsDirectory);

        // check preinstalled widgets
        var errorPreinstalledWidgetsIds = AllWidgetGroupMetadatas
            .Where(x => x.Preinstalled && (!PreinstalledWigdetsIds.Contains(x.ID)))
            .Select(x => x.ID).ToList();

        // remove error preinstalled widgets
        AllWidgetGroupMetadatas = AllWidgetGroupMetadatas.Where(x => !errorPreinstalledWidgetsIds.Contains(x.ID)).ToList();
    }

    private async Task LoadAllInstalledWidgets()
    {
        // load widget store list
        var widgetStoreList = await _appSettingsService.InitializeWidgetStoreListAsync();

        // get preinstalled widget metadata
        var preinstalledWidgetsMetadata = AllWidgetGroupMetadatas.Where(x => x.Preinstalled).ToList();

        // check all preinstalled widgets
        var installingIds = new List<string>();
        foreach (var metadata in preinstalledWidgetsMetadata)
        {
            var index = widgetStoreList.FindIndex(x => x.Id == metadata.ID && x.IsPreinstalled);
            if (index == -1)
            {
                // install new preinstalled widgets
                widgetStoreList.Add(new JsonWidgetStoreItem()
                {
                    Id = metadata.ID,
                    Version = metadata.Version,
                    IsPreinstalled = true,
                    IsInstalled = true,
                    ResourcesFolder = string.Empty,
                });
                installingIds.Add(metadata.ID);
            }
            else
            {
                var widgetStoreItem = widgetStoreList[index];
                var isInstalled = widgetStoreItem.IsInstalled;
                var version = widgetStoreItem.Version;
                if (isInstalled)
                {
                    // update installed, preinstalled widgets
                    if (version != metadata.Version)
                    {
                        UninstallResourceFolder(widgetStoreItem.ResourcesFolder);
                        widgetStoreList[index].Version = metadata.Version;
                        widgetStoreList[index].ResourcesFolder = string.Empty;
                        installingIds.Add(metadata.ID);
                    }
                }
                else
                {
                    // set uninstalled, preinstalled widgets
                    var metadataIndex = AllWidgetGroupMetadatas.FindIndex(x => x.ID == metadata.ID);
                    if (metadataIndex != -1)
                    {
                        AllWidgetGroupMetadatas[metadataIndex].Installed = false;
                    }
                }
            }
        }

        // load all installed widgets
        var installWidgetsMetadata = AllWidgetGroupMetadatas.Where(x => x.Installed).ToList();
        (InstalledWidgetGroupPairs, var errorWidgets, var installedWidgets) = WidgetsLoader.Widgets(installWidgetsMetadata, installingIds);

        // save widget store list
        if (installingIds.Count != 0)
        {
            foreach (var item in installedWidgets)
            {
                var id = item.Key;
                var resourcesFolder = item.Value;
                var index = widgetStoreList.FindIndex(x => x.Id == id);
                if (index != -1)
                {
                    if (resourcesFolder != string.Empty)
                    {
                        var relativeResourcesFolder = Path.GetRelativePath(AppContext.BaseDirectory, resourcesFolder);
                        widgetStoreList[index].ResourcesFolder = relativeResourcesFolder;
                    }
                    else
                    {
                        // user failed widgets will be reinstalled in the next time opening the app
                        // preinstalled failed widgets will only be reinstalled in the next version
                        widgetStoreList[index].IsInstalled = widgetStoreList[index].IsPreinstalled;
                    }
                }
            }

            await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
        }

        // show error notification
        if (errorWidgets.Count > 0)
        {
            var errorWidgetString = string.Join(Environment.NewLine, errorWidgets);

            DependencyExtensions.GetRequiredService<IAppNotificationService>().RunShow(
                string.Format("AppNotificationWidgetLoadErrorPayload".GetLocalizedString(),
                $"{Environment.NewLine}{errorWidgetString}{Environment.NewLine}"));
        }
    }

    #endregion

    #region Widget Resources

    #region Xaml Resources

    // Note: InstallResourceFolder is in WidgetsLoader.cs.

    private static void UninstallResourceFolder(string resourcesFolder)
    {
        resourcesFolder = Path.Combine(AppContext.BaseDirectory, resourcesFolder);
        if (Directory.Exists(resourcesFolder))
        {
            Directory.Delete(resourcesFolder);
        }
    }

    #endregion

    #region Language Resources

    private void InitWidgetsLanguageResources()
    {
        foreach (var pair in InstalledWidgetGroupPairs)
        {
            var widgetLanguageResources = new Dictionary<string, string>();

            var resourceMap = ResourceExtensions.TryGetResourceMap(pair.Metadata.AssemblyName);
            if (resourceMap != null)
            {
                for (uint i = 0; i < resourceMap.ResourceCount; i++)
                {
                    (var key, var value) = resourceMap.GetValueByIndex(i);
                    widgetLanguageResources.TryAdd(key, value.ValueAsString);
                }
            }

            WidgetsLanguageResources.TryAdd(pair.Metadata.ID, widgetLanguageResources);
        }
    }

    private LanguageResourceDictionary? GetWidgetLanguageResources(string widgetId)
    {
        WidgetsLanguageResources.TryGetValue(widgetId, out var widgetLanguageResources);
        if (widgetLanguageResources == null)
        {
            return null;
        }

        return new LanguageResourceDictionary(widgetLanguageResources);
    }

    #endregion

    #endregion

    #region Dispose

    public async Task DisposeWidgetsAsync()
    {
        foreach (var widgetPair in InstalledWidgetGroupPairs)
        {
            switch (widgetPair.WidgetGroup)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
            }
            widgetPair.ExtensionAssembly.Dispose();
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IWidgetGroup

    private async Task InitWidgetsAsync()
    {
        var logService = DependencyExtensions.GetRequiredService<ILogService>();
        var settingsService = DependencyExtensions.GetRequiredService<ISettingsService>();
        var themeService = DependencyExtensions.GetRequiredService<IThemeService>();
        var widgetService = DependencyExtensions.GetRequiredService<IWidgetService>();

        var failedPlugins = new ConcurrentQueue<WidgetGroupPair>();

        var initTasks = InstalledWidgetGroupPairs.Select(pair => Task.Run(delegate
        {
            try
            {
                var localizationService = (LocalizationService)DependencyExtensions.GetRequiredService<ILocalizationService>();
                localizationService.AssemblyName = pair.Metadata.AssemblyName;
                pair.WidgetGroup.InitWidgetGroupAsync(
                    new WidgetInitContext()
                    {
                        WidgetGroupMetadata = pair.Metadata,
                        LocalizationService = localizationService,
                        LogService = logService,
                        SettingsService = settingsService,
                        ThemeService = themeService,
                        WidgetService = widgetService
                    });
            }
            catch (Exception e)
            {
                _log.Error(e, $"Fail to Init plugin: {pair.Metadata.Name}");
                pair.Metadata.Disabled = true;
                failedPlugins.Enqueue(pair);
            }
        }));

        await Task.WhenAll(initTasks);

        if (!failedPlugins.IsEmpty)
        {
            var failedWidgetString = string.Join(Environment.NewLine, failedPlugins.Select(x => x.Metadata.Name));

            DependencyExtensions.GetRequiredService<IAppNotificationService>().RunShow(
                string.Format("AppNotificationWidgetInitializeErrorPayload".GetLocalizedString(),
                $"{Environment.NewLine}{failedWidgetString}{Environment.NewLine}"));
        }
    }

    public FrameworkElement CreateWidgetContent(string widgetId, WidgetContext widgetContext)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            try
            {
                return pair.WidgetGroup.CreateWidgetContent(widgetContext, GetWidgetLanguageResources(pair.Metadata.ID));
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error creating widget framework element for widget {pair.Metadata.ID}");
            }
        }

        return new UserControl();
    }

    public void UnpinWidget(string widgetId, string widgetRuntimeId, BaseWidgetSettings widgetSettings)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            try
            {
                pair.WidgetGroup.UnpinWidget(widgetRuntimeId, widgetSettings);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error deleting widget {widgetId}");
            }
        }
    }

    public void DeleteWidget(string widgetId, string widgetRuntimeId, BaseWidgetSettings widgetSettings)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            try
            {
                pair.WidgetGroup.DeleteWidget(widgetRuntimeId, widgetSettings);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error deleting widget {widgetId}");
            }
        }
    }

    public void ActivateWidget(string widgetId, WidgetContext widgetContext)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            try
            {
                pair.WidgetGroup.ActivateWidget(widgetContext);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error activating widget {widgetId}");
            }
        }
    }

    public void DeactivateWidget(string widgetId, string widgetRuntimeId)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            try
            {
                pair.WidgetGroup.DeactivateWidget(widgetRuntimeId);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error deactivating widget {widgetId}");
            }
        }
    }

    #endregion

    #region IWidgetGroupSetting

    public BaseWidgetSettings GetDefaultSettings(string widgetId, string widgetType)
    {
        var (metadata, widgetGroupSetting) = GetWidgetGroupSetting(widgetId);
        if (metadata != null && widgetGroupSetting != null)
        {
            try
            {
                return widgetGroupSetting.GetDefaultSettings(widgetType);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error getting default settings for widget {metadata.ID}");
            }
        }

        return new BaseWidgetSettings();
    }

    public FrameworkElement CreateWidgetSettingContent(string widgetId, WidgetSettingContext widgetSettingContext)
    {
        var (metadata, widgetGroupSetting) = GetWidgetGroupSetting(widgetId);
        if (metadata != null && widgetGroupSetting != null)
        {
            try
            {
                return widgetGroupSetting.CreateWidgetSettingContent(widgetSettingContext, GetWidgetLanguageResources(metadata.ID));
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error creating setting framework element for widget {metadata.ID}");
            }
        }

        return new UserControl();
    }

    public void OnWidgetSettingsChanged(string widgetId, WidgetSettingsChangedArgs settingsChangedArgs)
    {
        var (metadata, widgetGroupSetting) = GetWidgetGroupSetting(widgetId);
        if (metadata != null && widgetGroupSetting != null)
        {
            try
            {
                widgetGroupSetting.OnWidgetSettingsChanged(settingsChangedArgs);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error on settings changed for widget {metadata.ID}");
            }
        }
    }

    private (WidgetGroupMetadata? metadata, IWidgetGroupSetting? widgetGroupSetting) GetWidgetGroupSetting(string widgetId)
    {
        var index = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var pair = InstalledWidgetGroupPairs[index];
            if (pair.WidgetGroup is IWidgetGroupSetting widgetGroupSetting)
            {
                return (pair.Metadata, widgetGroupSetting);
            }
        }

        return (null, null);
    }

    #endregion

    #region Metadata

    #region Widget Group

    #region Status

    public bool IsWidgetGroupUnknown(string widgetId, string widgetType)
    {
        (var installed, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, null);
        if (installed)
        {
            return !InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.WidgetTypes.Contains(widgetType);
        }
        else
        {
            return allIndex == -1;
        }
    }

    #endregion

    #region Name & Description

    private string GetWidgetGroupnName(int? allIndex, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count)
        {
            if (InstalledWidgetGroupPairs[installedIndex!.Value].WidgetGroup is IWidgetLocalization localization)
            {
                return localization.GetLocalizedWidgetGroupName();
            }

            return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Name;
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Name;
        }

        return string.Format("Unknown_Widget_Name".GetLocalizedString(), 1);
    }

    private string GetWidgetGroupDescription(int? allIndex, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count)
        {
            if (InstalledWidgetGroupPairs[installedIndex!.Value].WidgetGroup is IWidgetLocalization localization)
            {
                return localization.GetLocalizedWidgetGroupDescription();
            }

            return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Description;
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Description;
        }

        return string.Empty;
    }

    #endregion

    #region Icon

    private string GetWidgetGroupIcoPath(int? allIndex, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count)
        {
            return InstalledWidgetGroupPairs[installedIndex.Value].Metadata.IcoPath;
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].IcoPath;
        }

        return Constants.UnknownWidgetIconPath;
    }

    #endregion

    #endregion

    #region Widget

    #region Name & Description

    #region Desktop Widgets 3

    public string GetWidgetName(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType);
        }

        return string.Format("Unknown_Widget_Name".GetLocalizedString(), 1);
    }

    public string GetWidgetDescription(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetDescription(allIndex, installedIndex, widgetTypeIndex, widgetType);
        }

        return string.Empty;
    }

    private string GetWidgetName(int? allIndex, int? installedIndex, int? widgetTypeIndex, string widgetType)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count)
        {
            if (InstalledWidgetGroupPairs[installedIndex!.Value].WidgetGroup is IWidgetLocalization localization)
            {
                return localization.GetLocalizedWidgetName(widgetType);
            }

            if (widgetTypeIndex != null)
            {
                return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value].Name;
            }
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value].Name;
        }

        return string.Format("Unknown_Widget_Name".GetLocalizedString(), 1);
    }

    private string GetWidgetDescription(int? allIndex, int? installedIndex, int? widgetTypeIndex, string widgetType)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count)
        {
            if (InstalledWidgetGroupPairs[installedIndex!.Value].WidgetGroup is IWidgetLocalization localization)
            {
                return localization.GetLocalizedWidgetDescription(widgetType);
            }

            if (widgetTypeIndex != null)
            {
                return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value].Description;
            }
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value].Description;
        }

        return string.Empty;
    }

    #endregion

    #endregion

    #region Icon & Screenshot

    public async Task<Brush> GetWidgetIconBrushAsync(DispatcherQueue dispatcherQueue, WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            if (installedIndex != -1)
            {
                return await GetWidgetIconBrushAsync(dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
            }
        }
        else
        {
            var definitionIndex = GetWidgetIndex(widgetId, widgetType);
            if (definitionIndex != -1)
            {
                return await GetWidgetIconBrushAsync(dispatcherQueue, _microsoftWidgetModel.WidgetDefinitions.ElementAt(definitionIndex), actualTheme);
            }
        }

        return null!;
    }

    public async Task<Brush> GetWidgetScreenshotBrushAsync(DispatcherQueue dispatcherQueue, WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            if (installedIndex != -1)
            {
                return await GetWidgetScreenshotBrushAsync(dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
            }
        }
        else
        {
            var definitionIndex = GetWidgetIndex(widgetId, widgetType);
            if (definitionIndex != -1)
            {
                return await GetWidgetScreenshotBrushAsync(dispatcherQueue, _microsoftWidgetModel.WidgetDefinitions.ElementAt(definitionIndex), actualTheme);
            }
        }

        return null!;
    }

    #region Desktop Widgets 3

    #region Icon

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightIconCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkIconCache = new();

    // TODO: Add support for cache clean.
    private void RemoveIconsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightIconCache.TryRemove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkIconCache.TryRemove((widgetId, widgetType), out _);
    }

    private async Task<Brush> GetWidgetIconBrushAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetIconFromDesktopWidgets3CacheAsync(dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget icon missing for widget {widgetId} - {widgetType}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget icon for widget {widgetId} - {widgetType}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform,
        };

        return brush;
    }

    private async Task<BitmapImage> GetIconFromDesktopWidgets3CacheAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        BitmapImage? bitmapImage;

        // First, check the cache to see if the icon is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _desktopWidgets3WidgetDarkIconCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }
        else
        {
            _desktopWidgets3WidgetLightIconCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(dispatcherQueue, GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(dispatcherQueue, GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetLightIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    #endregion

    #region Screenshot

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightScreenshotCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkScreenshotCache = new();

    // TODO: Add support for cache clean.
    private void RemoveScreenshotsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightScreenshotCache.Remove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkScreenshotCache.Remove((widgetId, widgetType), out _);
    }

    private async Task<Brush> GetWidgetScreenshotBrushAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetScreenshotFromDesktopWidgets3CacheAsync(dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget screenshot missing for widget definition {widgetId} {widgetType}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget screenshot for widget definition {widgetId} {widgetType}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
        };

        return brush;
    }

    private async Task<BitmapImage> GetScreenshotFromDesktopWidgets3CacheAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        BitmapImage? bitmapImage;

        // First, check the cache to see if the screenshot is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _desktopWidgets3WidgetDarkScreenshotCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }
        else
        {
            _desktopWidgets3WidgetLightScreenshotCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the screenshot wasn't already in the cache, get it from the widget resources service and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(dispatcherQueue, GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(dispatcherQueue, GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Light));
            _desktopWidgets3WidgetLightScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    #endregion

    #region Image Path

    #region Icon

    private string GetWidgetIconPath(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }

        return string.Empty;
    }

    private string GetWidgetIconPath(int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            var widget = InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value];
            if (actualTheme == ElementTheme.Dark && !string.IsNullOrEmpty(widget.IcoPathDark))
            {
                return widget.IcoPathDark;
            }
            else
            {
                return widget.IcoPath;
            }
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            var widget = AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value];
            if (actualTheme == ElementTheme.Dark && !string.IsNullOrEmpty(widget.IcoPathDark))
            {
                return widget.IcoPathDark;
            }
            else
            {
                return widget.IcoPath;
            }
        }

        return Constants.UnknownWidgetIconPath;
    }

    #endregion

    #region Screenshot

    private string GetWidgetScreenshotPath(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }

        return string.Empty;
    }

    private string GetWidgetScreenshotPath(int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            var widget = InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value];
            if (actualTheme == ElementTheme.Dark && !string.IsNullOrEmpty(widget.ScreenshotPathDark))
            {
                return widget.ScreenshotPathDark;
            }
            else
            {
                return widget.ScreenshotPath;
            }
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            var widget = AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value];
            if (actualTheme == ElementTheme.Dark && !string.IsNullOrEmpty(widget.ScreenshotPathDark))
            {
                return widget.ScreenshotPathDark;
            }
            else
            {
                return widget.ScreenshotPath;
            }
        }

        return Constants.UnknownWidgetIconPath;
    }

    #endregion

    #endregion

    #endregion

    #region Microsoft

    #region Icon

    public async Task<Brush> GetWidgetIconBrushAsync(DispatcherQueue dispatcherQueue, ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        return await _widgetIconService.GetBrushForMicrosoftWidgetIconAsync(dispatcherQueue, widgetDefinition, actualTheme);
    }

    #endregion

    #region Screenshot

    public async Task<Brush> GetWidgetScreenshotBrushAsync(DispatcherQueue dispatcherQueue, ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        return await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(dispatcherQueue, widgetDefinition, actualTheme);
    }

    #endregion

    #endregion

    #endregion

    #region Default & Min & Max Size

    public RectSize GetWidgetDefaultSize(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetDefaultSize(allIndex, installedIndex, widgetTypeIndex);
        }

        return new RectSize(342.0, 201.0);
    }

    public (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetMinMaxSize(allIndex, installedIndex, widgetTypeIndex);
        }

        return (new(null, null), new(null, null));
    }

    private RectSize GetWidgetDefaultSize(int? allIndex, int? installedIndex, int? widgetTypeIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            var widget = InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value];
            return new RectSize(widget.DefaultWidth, widget.DefaultHeight);
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            var widget = AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value];
            return new RectSize(widget.DefaultWidth, widget.DefaultHeight);
        }

        return new RectSize(342.0, 201.0);
    }

    private (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(int? allIndex, int? installedIndex, int? widgetTypeIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            var widget = InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value];
            return (new RectSize(widget.MinWidth, widget.MinHeight), new RectSize(widget.MaxWidth, widget.MaxHeight));
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            var widget = AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value];
            return (new RectSize(widget.MinWidth, widget.MinHeight), new RectSize(widget.MaxWidth, widget.MaxHeight));
        }

        return (new(null, null), new(null, null));
    }

    #endregion

    #region AllowMultiple & IsCustomizable

    public bool IsWidgetSingleInstanceAndAlreadyPinned(string widgetId, string widgetType)
    {
        if (!GetWidgetAllowMultiple(widgetId, widgetType))
        {
            foreach (var pinnedWidget in _appSettingsService.GetWidgetsList())
            {
                if (pinnedWidget.ProviderType == WidgetProviderType.DesktopWidgets3 && pinnedWidget.Id == widgetId && pinnedWidget.Type == widgetType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool GetWidgetIsCustomizable(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetIsCustomizable(allIndex, installedIndex, widgetTypeIndex);
        }

        return false;
    }

    private bool GetWidgetAllowMultiple(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetAllowMultiple(allIndex, installedIndex, widgetTypeIndex);
        }

        return false;
    }

    private bool GetWidgetAllowMultiple(int? allIndex, int? installedIndex, int? widgetTypeIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value].AllowMultiple;
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value].AllowMultiple;
        }

        return false;
    }

    private bool GetWidgetIsCustomizable(int? allIndex, int? installedIndex, int? widgetTypeIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgetGroupPairs.Count && widgetTypeIndex != null)
        {
            return InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets[widgetTypeIndex!.Value].IsCustomizable;
        }

        if (allIndex != null && allIndex < AllWidgetGroupMetadatas.Count && widgetTypeIndex != null)
        {
            return AllWidgetGroupMetadatas[allIndex!.Value].Widgets[widgetTypeIndex!.Value].IsCustomizable;
        }

        return false;
    }

    #endregion

    #endregion

    #region Widget Group & Widget Index

    #region Desktop Widgets 3

    private (bool Installed, int? AllIndex, int? InstalledIndex) GetWidgetGroupIndex(string widgetId, bool? installed)
    {
        if (installed == true)
        {
            var installedIndex = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
            if (installedIndex != -1)
            {
                return (true, null, installedIndex);
            }

            var allIndex = AllWidgetGroupMetadatas.FindIndex(x => x.ID == widgetId);
            if (allIndex != -1)
            {
                return (false, allIndex, null);
            }

            return (false, null, null);
        }
        else if (installed == false)
        {
            var allIndex = AllWidgetGroupMetadatas.FindIndex(x => x.ID == widgetId);
            if (allIndex != -1)
            {
                var installedIndex = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
                if (installedIndex != -1)
                {
                    return (true, allIndex, installedIndex);
                }

                return (false, allIndex, null);
            }

            return (false, null, null);
        }
        else
        {
            var installedIndex = InstalledWidgetGroupPairs.FindIndex(x => x.Metadata.ID == widgetId);
            if (installedIndex != -1)
            {
                return (true, null, installedIndex);
            }

            var allIndex = AllWidgetGroupMetadatas.FindIndex(x => x.ID == widgetId);
            if (allIndex != -1)
            {
                return (false, allIndex, null);
            }

            return (false, null, null);
        }
    }

    private (bool Installed, int? AllIndex, int? InstalledIndex, int? WidgetTypeIndex) GetWidgetGroupAndWidgetTypeIndex(string widgetId, string widgetType, bool? installed)
    {
        (var actualInstalled, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, installed);
        if (actualInstalled)
        {
            var widgetTypeIndex = InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.Widgets.FindIndex(x => x.Type == widgetType);
            if (widgetTypeIndex != -1)
            {
                return (true, allIndex, installedIndex, widgetTypeIndex);
            }

            return (false, allIndex, installedIndex, null);
        }
        else
        {
            if (allIndex != null)
            {
                var widgetTypeIndex = AllWidgetGroupMetadatas[allIndex!.Value].Widgets.FindIndex(x => x.Type == widgetType);
                if (widgetTypeIndex != -1)
                {
                    return (false, allIndex, null, widgetTypeIndex);
                }

                return (false, allIndex, null, null);
            }

            return (false, null, null, null);
        }
    }

    #endregion

    #region Microsoft

    private int GetWidgetProviderIndex(string widgetId)
    {
        var providerDefinitionIndex = -1;
        foreach (var providerDefinition in _microsoftWidgetModel.WidgetProviderDefinitions)
        {
            providerDefinitionIndex++;
            var (_, widgetId1) = providerDefinition.GetWidgetProviderInfo();
            if (widgetId1 == widgetId)
            {
                return providerDefinitionIndex;
            }
        }

        return -1;
    }

    private int GetWidgetIndex(string widgetId, string widgetType)
    {
        var definitionIndex = -1;
        foreach (var definition in _microsoftWidgetModel.WidgetDefinitions)
        {
            definitionIndex++;
            var (_, _, _, widgetId1, widgetType1) = definition.GetWidgetProviderAndWidgetInfo();
            if (widgetId == widgetId1 && widgetType == widgetType1)
            {
                return definitionIndex;
            }
        }

        return -1;
    }

    #endregion

    #endregion

    #endregion

    #region Dashboard

    public List<DashboardWidgetGroupItem> GetInstalledDashboardGroupItems()
    {
        var dashboardGroupItemList = new List<DashboardWidgetGroupItem>();

        foreach (var widget in InstalledWidgetGroupPairs)
        {
            var widgetId = widget.Metadata.ID;
            (var installed, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, true);
            if (installed)
            {
                dashboardGroupItemList.Add(new DashboardWidgetGroupItem()
                {
                    Id = widgetId,
                    Name = GetWidgetGroupnName(allIndex, installedIndex),
                    IcoPath = GetWidgetGroupIcoPath(allIndex, installedIndex),
                    Types = widget.Metadata.WidgetTypes
                });
            }
        }

        return dashboardGroupItemList;
    }

    public async Task<List<DashboardWidgetItem>> GetYourDashboardWidgetItemsAsync(ElementTheme actualTheme)
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var dashboardItemList = new List<DashboardWidgetItem>();
        var unknownNotInstalledWidgetList = new List<(string, string)>();

        foreach (var widget in widgetList)
        {
            var providerType = widget.ProviderType;
            var widgetId = widget.Id;
            var widgetType = widget.Type;
            var widgetIndex = widget.Index;

            // TODO: IsUnknown support.
            // TODO: Icon fill here.
            if (providerType == WidgetProviderType.Microsoft)
            {
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    ProviderType = providerType,
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    Name = widget.Name,
                    IconFill = null,
                    Pinned = widget.Pinned,
                    IsUnknown = false,
                    IsInstalled = true
                });
                continue;
            }

            (var installed, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            if (installed && widgetTypeIndex != null)
            {
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    ProviderType = providerType,
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    Name = GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType),
                    IconFill = await GetWidgetIconBrushAsync(_dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme),
                    Pinned = widget.Pinned,
                    IsUnknown = false,
                    IsInstalled = true
                });
            }
            else
            {
                if (!unknownNotInstalledWidgetList.Contains((widgetId, widgetType)))
                {
                    unknownNotInstalledWidgetList.Add((widgetId, widgetType));
                }

                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    ProviderType = providerType,
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    Name = string.Format("Unknown_Widget_Name".GetLocalizedString(), unknownNotInstalledWidgetList.Count),
                    IconFill = await GetPathIconBrushAsync(_dispatcherQueue, Constants.UnknownWidgetIconPath),
                    Pinned = widget.Pinned,
                    IsUnknown = true,
                    IsInstalled = false,
                });
            }
        }

        return dashboardItemList;
    }

    public async Task<DashboardWidgetItem?> GetDashboardWidgetItemAsync(string widgetId, string widgetType, int widgetIndex, ElementTheme actualTheme)
    {
        (var installed, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installed)
        {
            return new DashboardWidgetItem()
            {
                ProviderType = WidgetProviderType.DesktopWidgets3,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Name = GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType),
                IconFill = await GetWidgetIconBrushAsync(_dispatcherQueue, widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme),
                Pinned = true,
                IsUnknown = false,
                IsInstalled = true
            };
        }

        return null;
    }

    public async Task<DashboardWidgetItem> GetDashboardWidgetItemAsync(WidgetViewModel widgetViewModel, ElementTheme actualTheme)
    {
        // get widget info
        var providerType = WidgetProviderType.Microsoft;
        var (_, widgetName, _, widgetId, widgetType) = widgetViewModel.GetWidgetProviderAndWidgetInfo();
        // TODO: Get widget index.
        var widgetIndex = 0;

        // get widget item
        return new DashboardWidgetItem()
        {
            ProviderType = providerType,
            Id = widgetId,
            Type = widgetType,
            Index = widgetIndex,
            Name = widgetName,
            IconFill = await GetWidgetIconBrushAsync(_dispatcherQueue, widgetViewModel.WidgetDefinition, actualTheme),
            Pinned = true,
            IsUnknown = false,
            IsInstalled = true
        };
    }

    #endregion

    #region Widget Store

    public async Task<List<WidgetStoreItem>> GetInstalledWidgetStoreItemsAsync()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metadata in AllWidgetGroupMetadatas)
        {
            var widgetId = metadata.ID;
            (var installed, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, null);
            if (installed)
            {
                widgetStoreItemList.Add(new WidgetStoreItem()
                {
                    Id = widgetId,
                    Name = GetWidgetGroupnName(allIndex, installedIndex),
                    Description = GetWidgetGroupDescription(allIndex, installedIndex),
                    Author = metadata.Author,
                    Version = metadata.Version,
                    Website = metadata.Website,
                    IconFill = await GetPathIconBrushAsync(_dispatcherQueue, GetWidgetGroupIcoPath(allIndex, installedIndex))
                });
            }
        }

        return widgetStoreItemList;
    }

    public async Task<List<WidgetStoreItem>> GetPreinstalledAvailableWidgetStoreItemsAsync()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metadata in AllWidgetGroupMetadatas)
        {
            if (metadata.Preinstalled)
            {
                var widgetId = metadata.ID;
                (var installed, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, null);
                if (!installed)
                {
                    widgetStoreItemList.Add(new WidgetStoreItem()
                    {
                        Id = metadata.ID,
                        Name = GetWidgetGroupnName(allIndex, installedIndex),
                        Description = GetWidgetGroupDescription(allIndex, installedIndex),
                        Author = metadata.Author,
                        Version = metadata.Version,
                        Website = metadata.Website,
                        IconFill = await GetPathIconBrushAsync(_dispatcherQueue, GetWidgetGroupIcoPath(allIndex, installedIndex))
                    });
                }
            }
        }

        return widgetStoreItemList;
    }

    public async Task InstallWidgetAsync(string widgetId)
    {
        var metadata = AllWidgetGroupMetadatas.Find(x => x.ID == widgetId);
        var widgetStoreList = _appSettingsService.GetWidgetStoreList();
        if (metadata == null)
        {
            // TODO(Furture): Install available widget from Github, not supported yet.
        }
        else
        {
            // install widget from preinstalled widgets
            var index = widgetStoreList.FindIndex(x => x.Id == widgetId);
            if (index != -1)
            {
                var widgetStoreItem = widgetStoreList[index];
                if (widgetStoreItem.IsInstalled)
                {
                    UninstallResourceFolder(widgetStoreItem.ResourcesFolder);
                }

                // remove widget from widget store list
                widgetStoreList.RemoveAt(index);
                await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
            }
        }

        App.RestartApplication();
    }

    public async Task UninstallWidgetAsync(string widgetId)
    {
        var metadata = AllWidgetGroupMetadatas.Find(x => x.ID == widgetId);
        if (metadata == null)
        {
            return;
        }

        var widgetStoreList = _appSettingsService.GetWidgetStoreList();
        var index = widgetStoreList.FindIndex(x => x.Id == widgetId);
        if (index == -1)
        {
            return;
        }

        var widgetStoreItem = widgetStoreList[index];
        if (widgetStoreItem.IsInstalled)
        {
            UninstallResourceFolder(widgetStoreItem.ResourcesFolder);
            widgetStoreList[index].IsInstalled = false;
            widgetStoreList[index].ResourcesFolder = string.Empty;
            await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
        }

        App.RestartApplication();
    }

    #endregion

    #region Path Icon

    private readonly ConcurrentDictionary<string, BitmapImage> _pathIconCache = new();

    private async Task<Brush> GetPathIconBrushAsync(DispatcherQueue dispatcherQueue, string iconPath)
    {
        var image = new BitmapImage();
        try
        {
            BitmapImage? bitmapImage;

            // First, check the cache to see if the icon is already there.
            _pathIconCache.TryGetValue(iconPath, out bitmapImage);

            if (bitmapImage != null)
            {
                image = bitmapImage;
            }
            else
            {
                // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
                bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(dispatcherQueue, iconPath);
                _pathIconCache.TryAdd(iconPath, bitmapImage);

                image = bitmapImage;
            }
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget icon missing for {iconPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget icon for {iconPath}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform,
        };

        return brush;
    }

    #endregion
}
