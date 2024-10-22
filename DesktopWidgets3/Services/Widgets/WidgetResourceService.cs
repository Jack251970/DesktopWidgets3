using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService) : IWidgetResourceService
{
    private static string ClassName => typeof(WidgetResourceService).Name;

    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

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
        // get all widget metadata
        GetAllWidgetsMetadata();

        // load all installed widgets
        await LoadAllInstalledWidgets();

        // initialize all widgets
        await InitWidgetsAsync();

        // initialize widgets language resources
        InitWidgetsLanguageResources();

        // initialize widget list
        await _appSettingsService.InitializeWidgetListAsync();
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
                string.Format("AppNotificationWidgetLoadErrorPayload".GetLocalized(),
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
                LogExtensions.LogError(ClassName, e, $"Error creating widget framework element for widget {pair.Metadata.ID}");
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
                LogExtensions.LogError(ClassName, e, $"Error deleting widget {widgetId}");
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
                LogExtensions.LogError(ClassName, e, $"Error deleting widget {widgetId}");
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
                LogExtensions.LogError(ClassName, e, $"Error activating widget {widgetId}");
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
                LogExtensions.LogError(ClassName, e, $"Error deactivating widget {widgetId}");
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
                LogExtensions.LogError(ClassName, e, $"Error getting default settings for widget {metadata.ID}");
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
                LogExtensions.LogError(ClassName, e, $"Error creating setting framework element for widget {metadata.ID}");
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
                LogExtensions.LogError(ClassName, e, $"Error on settings changed for widget {metadata.ID}");
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

    #region Name & Description & Icon

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

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
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

        return Constants.UnknownWidgetIcoPath;
    }

    #endregion

    #endregion

    #region Widget

    #region Name & Description

    public string GetWidgetName(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType);
        }

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
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

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
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

    #region Icon & Screenshot

    public string GetWidgetIconPath(string widgetId, string widgetType, ElementTheme? actualTheme = null)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }

        return string.Empty;
    }

    public string GetWidgetScreenshotPath(string widgetId, string widgetType, ElementTheme? actualTheme = null)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installedIndex != -1)
        {
            return GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }

        return string.Empty;
    }

    private string GetWidgetIconPath(int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme? actualTheme = null)
    {
        actualTheme ??= _themeSelectorService.GetActualTheme();

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

        return Constants.UnknownWidgetIcoPath;
    }

    private string GetWidgetScreenshotPath(int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme? actualTheme = null)
    {
        actualTheme ??= _themeSelectorService.GetActualTheme();

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

        return Constants.UnknownWidgetIcoPath;
    }

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
                if (pinnedWidget.Id == widgetId & pinnedWidget.Type == widgetType)
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

    public List<DashboardWidgetItem> GetYourDashboardWidgetItems()
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var dashboardItemList = new List<DashboardWidgetItem>();
        var unknownNotInstalledWidgetList = new List<(string, string)>();

        foreach (var widget in widgetList)
        {
            var widgetId = widget.Id;
            var widgetType = widget.Type;
            var widgetIndex = widget.Index;

            (var installed, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            if (installed && widgetTypeIndex != null)
            {
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    Name = GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType),
                    IcoPath = GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex),
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
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    Name = string.Format("Unknown_Widget_Name".GetLocalized(), unknownNotInstalledWidgetList.Count),
                    IcoPath = Constants.UnknownWidgetIcoPath,
                    Pinned = widget.Pinned,
                    IsUnknown = true,
                    IsInstalled = false,
                });
            }
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem? GetDashboardWidgetItem(string widgetId, string widgetType, int widgetIndex)
    {
        (var installed, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installed)
        {
            return new DashboardWidgetItem()
            {
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Name = GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType),
                IcoPath = GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex),
                Pinned = true,
                IsUnknown = false,
                IsInstalled = true
            };
        }

        return null;
    }

    #endregion

    #region Widget Store

    public List<WidgetStoreItem> GetInstalledWidgetStoreItems()
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
                    IcoPath = GetWidgetGroupIcoPath(allIndex, installedIndex)
                });
            }
        }

        return widgetStoreItemList;
    }

    public List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems()
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
                        IcoPath = GetWidgetGroupIcoPath(allIndex, installedIndex)
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
}
