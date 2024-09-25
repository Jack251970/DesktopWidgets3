using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private static string ClassName => typeof(WidgetResourceService).Name;

    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private List<WidgetPair> InstalledWidgets { get; set; } = null!;
    private List<WidgetMetadata> AllWidgetsMetadata { get; set; } = null!;

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> WidgetsLanguageResources = [];

    private static readonly string[] WidgetsDirectories =
    [
        LocalSettingsHelper.DefaultUserWidgetsDirectory
    ];

    private static readonly string[] PreinstalledWigdetsIds =
    [
        "949ADC2E912C4772BC3025A1E9DA32A0",  // Analog Clock
        "7A0C8F221280461E9B02D3CFF2D2BD35",  // Digital Clock
        "09613A71F3FE40E4AC5FF91563BD52B2",  // Disk
        "DB86CAFACFF0436C961D91E06B6F7FFC",  // Network
        "34EAD000AD4840E985009002128F654C",  // Performance
    ];

    #region Initialization

    public async Task InitalizeAsync()
    {
        // get all widget metadata
        GetAllWidgetsMetadata();

        // load all installed widgets
        await LoadAllInstalledWidgets();

        // initialize all widgets
        await InitWidgetsAsync();

        // initialize widgets localization
        await InitWidgetsLocalizationAsync();

        // initialize widget list
        await _appSettingsService.InitializeWidgetListAsync();
    }

    private void GetAllWidgetsMetadata()
    {
        // get all widget metadata
        AllWidgetsMetadata = WidgetsConfig.Parse(WidgetsDirectories, Constant.PreinstalledWidgetsDirectory);

        // check preinstalled widgets
        var errorPreinstalledWidgetsIds = AllWidgetsMetadata
            .Where(x => x.Preinstalled && (!PreinstalledWigdetsIds.Contains(x.ID)))
            .Select(x => x.ID).ToList();

        // remove error preinstalled widgets
        AllWidgetsMetadata = AllWidgetsMetadata.Where(x => !errorPreinstalledWidgetsIds.Contains(x.ID)).ToList();
    }

    private async Task LoadAllInstalledWidgets()
    {
        // load widget store list
        var widgetStoreList = await _appSettingsService.InitializeWidgetStoreListAsync();

        // get preinstalled widget metadata
        var preinstalledWidgetsMetadata = AllWidgetsMetadata.Where(x => x.Preinstalled).ToList();

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
                    var metadataIndex = AllWidgetsMetadata.FindIndex(x => x.ID == metadata.ID);
                    if (metadataIndex != -1)
                    {
                        AllWidgetsMetadata[metadataIndex].Installed = false;
                    }
                }
            }
        }

        // load all installed widgets
        var installWidgetsMetadata = AllWidgetsMetadata.Where(x => x.Installed).ToList();
        (InstalledWidgets, var errorWidgets, var installedWidgets) = WidgetsLoader.Widgets(installWidgetsMetadata, installingIds);

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
                string.Format("AppNotificationWidgetLoadErrorPayload".GetLocalized(),
                $"{Environment.NewLine}{errorWidgetString}{Environment.NewLine}"));
        }
    }

    #endregion

    #region Widget Resources

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

    #region Dispose

    public async Task DisposeWidgetsAsync()
    {
        foreach (var widgetPair in InstalledWidgets)
        {
            switch (widgetPair.Widget)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IWidget

    private async Task InitWidgetsAsync()
    {
        var logService = DependencyExtensions.GetRequiredService<ILogService>();
        var settingsService = DependencyExtensions.GetRequiredService<ISettingsService>();
        var themeService = DependencyExtensions.GetRequiredService<IThemeService>();
        var widgetService = DependencyExtensions.GetRequiredService<IWidgetService>();

        var failedPlugins = new ConcurrentQueue<WidgetPair>();

        var initTasks = InstalledWidgets.Select(pair => Task.Run(delegate
        {
            try
            {
                var localizationService = (LocalizationService)DependencyExtensions.GetRequiredService<ILocalizationService>();
                if (pair.Widget is IWidgetLocalization widgetLocalization)
                {
                    localizationService.AssemblyName = pair.Metadata.AssemblyName;
                }
                pair.Widget.InitWidgetAsync(
                    new WidgetInitContext(
                        pair.Metadata,
                        localizationService,
                        logService,
                        settingsService,
                        themeService,
                        widgetService));
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"Fail to Init plugin: {pair.Metadata.Name}");
                pair.Metadata.Disabled = true;
                failedPlugins.Enqueue(pair);
            }
        }));

        await Task.WhenAll(initTasks);

        if (!failedPlugins.IsEmpty)
        {
            var failedWidgetString = string.Join(Environment.NewLine, failedPlugins.Select(x => x.Metadata.Name));

            DependencyExtensions.GetRequiredService<IAppNotificationService>().RunShow(
                string.Format("AppNotificationWidgetInitializeErrorPayload".GetLocalized(),
                $"{Environment.NewLine}{failedWidgetString}{Environment.NewLine}"));
        }
    }

    public FrameworkElement GetWidgetFrameworkElement(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            try
            {
                return widget.Widget.CreateWidgetFrameworkElement(GetWidgetLanguageResources(widget.Metadata.ID));
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"Error creating widget framework element for widget {widget.Metadata.ID}");
            }
        }

        return new UserControl();
    }

    public async Task EnableWidgetAsync(string widgetId, bool firstWidget)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            switch (widget.Widget)
            {
                case IWidgetEnableDisable enableDisable:
                    enableDisable.EnableWidget(firstWidget);
                    break;
                case IAsyncWidgetEnableDisable asyncEnableDisable:
                    await asyncEnableDisable.EnableWidgetAsync(firstWidget);
                    break;
            }
        }

        await Task.CompletedTask;
    }

    public async Task DisableWidgetAsync(string widgetId, bool lastWidget)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            switch (widget.Widget)
            {
                case IWidgetEnableDisable enableDisable:
                    enableDisable.DisableWidget(lastWidget);
                    break;
                case IAsyncWidgetEnableDisable asyncEnableDisable:
                    await asyncEnableDisable.DisableWidgetAsync(lastWidget);
                    break;
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            if (widget.Widget is IWidgetSetting widgetSetting)
            {
                return widgetSetting.GetDefaultSetting();
            }
        }

        return new BaseWidgetSettings();
    }

    public FrameworkElement GetWidgetSettingFrameworkElement(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            if (widget.Widget is IWidgetSetting widgetSetting)
            {
                try
                {
                    return widgetSetting.CreateWidgetSettingFrameworkElement(GetWidgetLanguageResources(widget.Metadata.ID));
                }
                catch (Exception e)
                {
                    LogExtensions.LogError(ClassName, e, $"Error creating setting framework element for widget {widget.Metadata.ID}");
                }
            }
        }

        return new UserControl();
    }

    #endregion

    #region IWidgetLocalization

    private async Task InitWidgetsLocalizationAsync()
    {
        var initTasks = InstalledWidgets.Select(pair => Task.Run(delegate
        {
            if (pair.Widget is IWidgetLocalization widgetLocalization)
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
        }));

        await Task.WhenAll(initTasks);
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

    #region Metadata

    #region Public

    public string GetWidgetName(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            if (InstalledWidgets[index].Widget is IWidgetLocalization localization)
            {
                return localization.GetLocalizatedTitle();
            }
            else
            {
                return InstalledWidgets[index].Metadata.Name;
            }
        }

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
    }

    public string GetWidgetDescription(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            if (InstalledWidgets[index].Widget is IWidgetLocalization localization)
            {
                return localization.GetLocalizatedDescription();
            }
            else
            {
                return InstalledWidgets[index].Metadata.Description;
            }
        }

        return string.Empty;
    }

    public string GetWidgetIcoPath(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            return InstalledWidgets[index].Metadata.IcoPath;
        }

        return string.Empty;
    }

    public RectSize GetDefaultSize(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            return new RectSize(widget.Metadata.DefaultWidth, widget.Metadata.DefaultHeight);
        }

        return new RectSize(318, 200);
    }

    public (RectSize MinSize, RectSize MaxSize, bool NewThread) GetMinMaxSizeNewThread(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        var widget = InstalledWidgets[index];

        if (index != -1)
        {
            return (new(widget.Metadata.MinWidth, widget.Metadata.MinHeight), 
                new(widget.Metadata.MaxWidth, widget.Metadata.MaxHeight),
                _appSettingsService.MultiThread && widget.Metadata.InNewThread);
        }

        return (new(null, null), new (null, null), false);
    }

    #endregion

    #region private

    private (bool Installed, int AllIndex, int? InstalledIndex) GetWidgetIndex(string widgetId, bool? installed)
    {
        if (installed == true)
        {
            var installedIndex = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
            return (true, 0, installedIndex);
        }
        else if (installed == false)
        {
            var allIndex = AllWidgetsMetadata.FindIndex(x => x.ID == widgetId);
            return (false, allIndex, null);
        }
        else
        {
            var installedIndex = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
            var allIndex = AllWidgetsMetadata.FindIndex(x => x.ID == widgetId);
            installed = installedIndex != -1;
            if (installed == true)
            {
                return (true, allIndex, installedIndex);
            }
            else
            {
                return (false, allIndex, null);
            }
        }
    }

    private string GetWidgetName(int index, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgets.Count)
        {
            if (InstalledWidgets[installedIndex.Value].Widget is IWidgetLocalization localization)
            {
                return localization.GetLocalizatedTitle();
            }
            else
            {
                return InstalledWidgets[installedIndex.Value].Metadata.Name;
            }
        }

        if (index < AllWidgetsMetadata.Count)
        {
            return AllWidgetsMetadata[index].Name;
        }

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
    }

    private string GetWidgetDescription(int index, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgets.Count)
        {
            if (InstalledWidgets[installedIndex.Value].Widget is IWidgetLocalization localization)
            {
                return localization.GetLocalizatedDescription();
            }
            else
            {
                return InstalledWidgets[installedIndex.Value].Metadata.Description;
            }
        }

        if (index < AllWidgetsMetadata.Count)
        {
            return AllWidgetsMetadata[index].Description;
        }

        return string.Empty;
    }

    private string GetWidgetIcoPath(int index, int? installedIndex)
    {
        if (installedIndex != null && installedIndex < InstalledWidgets.Count)
        {
            return InstalledWidgets[installedIndex.Value].Metadata.IcoPath;
        }

        if (index < AllWidgetsMetadata.Count)
        {
            return AllWidgetsMetadata[index].IcoPath;
        }

        return Constant.UnknownWidgetIcoPath;
    }

    #endregion

    #endregion

    #region Dashboard

    public List<DashboardWidgetItem> GetInstalledDashboardItems()
    {
        var dashboardItemList = new List<DashboardWidgetItem>();

        foreach (var widget in InstalledWidgets)
        {
            var widgetId = widget.Metadata.ID;
            (var installed, var allIndex, var installedIndex) = GetWidgetIndex(widgetId, true);
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Id = widgetId,
                IndexTag = 0,
                Name = GetWidgetName(allIndex, installedIndex),
                IcoPath = GetWidgetIcoPath(allIndex, installedIndex),
                IsUnknown = false,
                IsInstalled = installed,
            });
        }

        return dashboardItemList;
    }

    public List<DashboardWidgetItem> GetYourDashboardItems()
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var dashboardItemList = new List<DashboardWidgetItem>();
        var unknownWidgetIdList = new List<string>();

        foreach (var widget in widgetList)
        {
            var widgetId = widget.Id;
            var indexTag = widget.IndexTag;
            if (IsWidgetUnknown(widgetId))
            {
                if (!unknownWidgetIdList.Contains(widgetId))
                {
                    unknownWidgetIdList.Add(widgetId);
                }
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
                    Name = string.Format("Unknown_Widget_Name".GetLocalized(), unknownWidgetIdList.Count),
                    IcoPath = Constant.UnknownWidgetIcoPath,
                    IsEnabled = widget.IsEnabled,
                    IsUnknown = true,
                    IsInstalled = false,
                });
            }
            else
            {
                (var installed, var allIndex, var installedIndex) = GetWidgetIndex(widgetId, null);
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
                    Name = GetWidgetName(allIndex, installedIndex),
                    IcoPath = GetWidgetIcoPath(allIndex, installedIndex),
                    IsEnabled = widget.IsEnabled,
                    IsUnknown = false,
                    IsInstalled = installed
                });
            }
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem? GetDashboardItem(string widgetId, int indexTag)
    {
        (var installed, var allIndex, var installedIndex) = GetWidgetIndex(widgetId, null);
        if (allIndex != -1)
        {
            return new DashboardWidgetItem()
            {
                Id = widgetId,
                IndexTag = indexTag,
                Name = GetWidgetName(allIndex, installedIndex),
                IcoPath = GetWidgetIcoPath(allIndex, installedIndex),
                IsEnabled = true,
                IsUnknown = false,
                IsInstalled = installed
            };
        }

        return null;
    }

    public bool IsWidgetUnknown(string widgetId)
    {
        return !AllWidgetsMetadata.Any(x => x.ID == widgetId);
    }

    #endregion

    #region Widget Store

    public List<WidgetStoreItem> GetInstalledWidgetStoreItems()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metadata in AllWidgetsMetadata)
        {
            var widgetId = metadata.ID;
            (var installed, var allIndex, var installedIndex) = GetWidgetIndex(widgetId, null);
            if (installed)
            {
                widgetStoreItemList.Add(new WidgetStoreItem()
                {
                    Id = widgetId,
                    Name = GetWidgetName(allIndex, installedIndex),
                    Description = GetWidgetDescription(allIndex, installedIndex),
                    Author = metadata.Author,
                    Version = metadata.Version,
                    Website = metadata.Website,
                    IcoPath = GetWidgetIcoPath(allIndex, installedIndex)
                });
            }
        }

        return widgetStoreItemList;
    }

    public List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metadata in AllWidgetsMetadata)
        {
            if (metadata.Preinstalled)
            {
                var widgetId = metadata.ID;
                (var installed, var allIndex, var installedIndex) = GetWidgetIndex(widgetId, null);
                if (!installed)
                {
                    widgetStoreItemList.Add(new WidgetStoreItem()
                    {
                        Id = metadata.ID,
                        Name = GetWidgetName(allIndex, installedIndex),
                        Description = GetWidgetDescription(allIndex, installedIndex),
                        Author = metadata.Author,
                        Version = metadata.Version,
                        Website = metadata.Website,
                        IcoPath = GetWidgetIcoPath(allIndex, installedIndex)
                    });
                }
            }
        }

        return widgetStoreItemList;
    }

    public async Task InstallWidgetAsync(string widgetId)
    {
        var metadata = AllWidgetsMetadata.Find(x => x.ID == widgetId);
        var widgetStoreList = _appSettingsService.GetWidgetStoreList();
        if (metadata == null)
        {
            // TODO: Install available widget from Github, not supported yet.
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
        var metadata = AllWidgetsMetadata.Find(x => x.ID == widgetId);
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
}
