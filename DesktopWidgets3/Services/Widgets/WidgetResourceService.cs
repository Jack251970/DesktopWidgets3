﻿using System.Collections.Concurrent;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(DispatcherQueue dispatcherQueue, MicrosoftWidgetModel microsoftWidgetModel, IAppSettingsService appSettingsService, IWidgetIconService widgetIconService, IWidgetScreenshotService widgetScreenshotService) : IWidgetResourceService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetResourceService));

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;

    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly IWidgetIconService _widgetIconService = widgetIconService;
    private readonly IWidgetScreenshotService _widgetScreenshotService = widgetScreenshotService;

    private List<WidgetGroupPair> InstalledWidgetGroupPairs { get; set; } = null!;
    private List<WidgetGroupMetadata> AllWidgetGroupMetadatas { get; set; } = null!;

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
        _log.Information("Initializing widget resources");

        try
        {
            // initialize microsoft widget resources if enabled them
            if (_appSettingsService.EnableMicrosoftWidgets)
            {
                await _microsoftWidgetModel.InitializeResourcesAsync();
            }

            // get all widget metadata
            GetAllWidgetsMetadata();

            // load all installed widgets
            await LoadAllInstalledWidgets();

            // initialize all widgets
            await InitWidgetsAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to initialize widget resources");
        }

        _log.Debug("Widget resources initialized");
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

        _log.Information($"Filling available Desktop Widgets 3 widget list, found {AllWidgetGroupMetadatas.Count} widgets");
    }

    private async Task LoadAllInstalledWidgets()
    {
        // get widget store list
        var widgetStoreList = _appSettingsService.GetWidgetStoreList();

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

        _log.Information($"Filling installed Desktop Widgets 3 widget list, found {InstalledWidgetGroupPairs.Count} widgets");

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
                return pair.WidgetGroup.CreateWidgetContent(widgetContext);
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
                return widgetGroupSetting.CreateWidgetSettingContent(widgetSettingContext);
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

    #region Name

    #region Desktop Widgets 3

    private string GetWidgetGroupName(int? allIndex, int? installedIndex)
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

    #endregion

    #region Microsoft

    private string GetWidgetGroupName(int providerDefinitionIndex)
    {
        if (providerDefinitionIndex != -1)
        {
            var (widgetGroupName, _) = _microsoftWidgetModel.WidgetProviderDefinitions.ElementAt(providerDefinitionIndex).GetWidgetProviderInfo();
            return widgetGroupName;
        }

        return string.Format("Unknown_Widget_Name".GetLocalizedString(), 1);
    }

    #endregion

    #endregion

    #region Description

    #region Desktop Widgets 3

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

    #endregion

    #region Icon

    public async Task<Brush> GetWidgetGroupIconBrushAsync(WidgetProviderType providerType, string widgetId)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, true);
            return await GetWidgetGroupIconBrushAsync(widgetId, allIndex, installedIndex);
        }
        else
        {
            var providerDefinitionIndex = GetWidgetProviderDefinitionIndex(widgetId);
            return await GetWidgetGroupIconBrushAsync(providerDefinitionIndex);
        }
    }

    #region Desktop Widgets 3

    private readonly ConcurrentDictionary<string, BitmapImage> _desktopWidgets3WidgetGroupIconCache = new();

    private void RemoveIconsFromDesktopWidgets3Cache(string widgetId)
    {
        _desktopWidgets3WidgetGroupIconCache.TryRemove(widgetId, out _);
    }

    private async Task<Brush> GetWidgetGroupIconBrushAsync(string widgetId, int? allIndex, int? installedIndex)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetGroupIconFromDesktopWidgets3CacheAsync(widgetId, allIndex, installedIndex);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget group icon missing for widget {widgetId}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget group icon for widget {widgetId}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform
        };

        return brush;
    }

    private async Task<BitmapImage> GetGroupIconFromDesktopWidgets3CacheAsync(string widgetId, int? allIndex, int? installedIndex)
    {
        BitmapImage? bitmapImage;

        // First, check the cache to see if the icon is already there.
        _desktopWidgets3WidgetGroupIconCache.TryGetValue(widgetId, out bitmapImage);

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, GetWidgetGroupIcoPath(allIndex, installedIndex));
        _desktopWidgets3WidgetGroupIconCache.TryAdd(widgetId, bitmapImage);

        return bitmapImage;
    }

    #region Image Path

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

    #region Microsoft

    public async Task<Brush> GetWidgetGroupIconBrushAsync(WidgetProviderDefinition widgetProviderDefinition)
    {
        return await _widgetIconService.GetBrushForMicrosoftWidgetProviderIconAsync(widgetProviderDefinition);
    }

    private async Task<Brush> GetWidgetGroupIconBrushAsync(int providerDefinitionIndex)
    {
        if (providerDefinitionIndex != -1)
        {
            return await GetWidgetGroupIconBrushAsync(_microsoftWidgetModel.WidgetProviderDefinitions.ElementAt(providerDefinitionIndex));
        }

        return null!;
    }

    #endregion

    #endregion

    #endregion

    #region Widget

    #region Status

    public bool IsWidgetGroupUnknown(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var installed, var allIndex, var installedIndex) = GetWidgetGroupIndex(widgetId, null);
            if (installed)
            {
                return !InstalledWidgetGroupPairs[installedIndex!.Value].Metadata.WidgetTypes.Contains(widgetType);
            }
            else
            {
                return allIndex != null;
            }
        }
        else
        {
            var definitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return definitionIndex == -1;
        }
    }

    #endregion

    #region Name

    public string GetWidgetName(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType);
        }
        else
        {
            var widgetDefinitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return GetWidgetName(widgetDefinitionIndex);
        }
    }

    #region Desktop Widgets 3

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

    #endregion

    #region Microsoft

    private string GetWidgetName(int providerDefinitionIndex)
    {
        if (providerDefinitionIndex != -1)
        {
            var (widgetTypeName, _, _) = _microsoftWidgetModel.WidgetDefinitions.ElementAt(providerDefinitionIndex).GetWidgetInfo();
            return widgetTypeName;
        }

        return string.Format("Unknown_Widget_Name".GetLocalizedString(), 1);
    }

    #endregion

    #endregion

    #region Description

    public string GetWidgetDescription(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return GetWidgetDescription(allIndex, installedIndex, widgetTypeIndex, widgetType);
        }
        else
        {
            var widgetDefinitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return GetWidgetDescription(widgetDefinitionIndex);
        }
    }

    #region Desktop Widgets 3

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

    #region Microsoft

    private string GetWidgetDescription(int providerDefinitionIndex)
    {
        if (providerDefinitionIndex != -1)
        {
            var (_, widgetTypeDescription, _) = _microsoftWidgetModel.WidgetDefinitions.ElementAt(providerDefinitionIndex).GetWidgetInfo();
            return widgetTypeDescription;
        }

        return string.Empty;
    }

    #endregion

    #endregion

    #region Icon

    public async Task<Brush> GetWidgetIconBrushAsync(WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return await GetWidgetIconBrushAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }
        else
        {
            var definitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return await GetWidgetIconBrushAsync(definitionIndex, actualTheme);
        }
    }

    #region Desktop Widgets 3

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightIconCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkIconCache = new();

    private void RemoveIconsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightIconCache.TryRemove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkIconCache.TryRemove((widgetId, widgetType), out _);
    }

    private async Task<Brush> GetWidgetIconBrushAsync(string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetIconFromDesktopWidgets3CacheAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
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
            Stretch = Stretch.Uniform
        };

        return brush;
    }

    private async Task<BitmapImage> GetIconFromDesktopWidgets3CacheAsync(string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
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
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetLightIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    #region Image Path

    private string GetWidgetIconPath(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        return GetWidgetIconPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
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

    #endregion

    #region Microsoft

    public async Task<Brush> GetWidgetIconBrushAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        return await _widgetIconService.GetBrushForMicrosoftWidgetIconAsync(widgetDefinition, actualTheme);
    }

    private async Task<Brush> GetWidgetIconBrushAsync(int definitionIndex, ElementTheme actualTheme)
    {
        if (definitionIndex != -1)
        {
            return await GetWidgetIconBrushAsync(_microsoftWidgetModel.WidgetDefinitions.ElementAt(definitionIndex), actualTheme);
        }

        return null!;
    }

    #endregion

    #endregion

    #region Screenshot

    public async Task<Brush> GetWidgetScreenshotBrushAsync(WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return await GetWidgetScreenshotBrushAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
        }
        else
        {
            var definitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return await GetWidgetScreenshotBrushAsync(definitionIndex, actualTheme);
        }
    }

    #region Desktop Widgets 3

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightScreenshotCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkScreenshotCache = new();

    private void RemoveScreenshotsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightScreenshotCache.Remove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkScreenshotCache.Remove((widgetId, widgetType), out _);
    }

    private async Task<Brush> GetWidgetScreenshotBrushAsync(string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetScreenshotFromDesktopWidgets3CacheAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme);
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
            Stretch = Stretch.Uniform
        };

        return brush;
    }

    private async Task<BitmapImage> GetScreenshotFromDesktopWidgets3CacheAsync(string widgetId, string widgetType, int? allIndex, int? installedIndex, int? widgetTypeIndex, ElementTheme actualTheme)
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
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, ElementTheme.Light));
            _desktopWidgets3WidgetLightScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    #region Image Path

    private string GetWidgetScreenshotPath(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        return GetWidgetScreenshotPath(allIndex, installedIndex, widgetTypeIndex, actualTheme);
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

        return string.Empty;
    }

    #endregion

    #endregion

    #region Microsoft

    public async Task<Brush> GetWidgetScreenshotBrushAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        return await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(widgetDefinition, actualTheme);
    }

    private async Task<Brush> GetWidgetScreenshotBrushAsync(int definitionIndex, ElementTheme actualTheme)
    {
        if (definitionIndex != -1)
        {
            return await GetWidgetScreenshotBrushAsync(_microsoftWidgetModel.WidgetDefinitions.ElementAt(definitionIndex), actualTheme);
        }

        return null!;
    }

    #endregion

    #endregion

    #region Size

    #region Default

    #region Desktop Widgets 3

    public RectSize GetWidgetDefaultSize(string widgetId, string widgetType)
    {
        (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        return GetWidgetDefaultSize(allIndex, installedIndex, widgetTypeIndex);
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

        return WidgetConstants.DefaultWidgetSize;
    }

    #endregion

    #region Microsoft

    public RectSize GetWidgetDefaultSize(WidgetViewModel widgetViewModel)
    {
        var widgetSizeHeight = GetPixelHeightFromWidgetSize(widgetViewModel.WidgetSize);
        var widgetSizeWidth = WidgetHelpers.WidgetPxWidth;

        return new RectSize(widgetSizeWidth, widgetSizeHeight);
    }

    private static double GetPixelHeightFromWidgetSize(WidgetSize size)
    {
        return size switch
        {
            WidgetSize.Small => WidgetHelpers.WidgetPxHeightSmall,
            WidgetSize.Medium => WidgetHelpers.WidgetPxHeightMedium,
            WidgetSize.Large => WidgetHelpers.WidgetPxHeightLarge,
            _ => 0,
        };
    }

    #endregion

    #endregion

    #region Min & Max

    public (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        if (providerType == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return GetWidgetMinMaxSize(allIndex, installedIndex, widgetTypeIndex);
        }
        else
        {
            var definitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return GetWidgetMinMaxSizeMicrosoft(definitionIndex);
        }
    }

    #region Desktop Widgets 3

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

        return (RectSize.NULL, RectSize.NULL);
    }

    #endregion

    #region Microsoft

    private static (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSizeMicrosoft(int definitionIndex)
    {
        if (definitionIndex != -1)
        {
            // We don't set min & max size for Microsoft widgets.
            return (RectSize.NULL, RectSize.NULL);
        }

        return (RectSize.NULL, RectSize.NULL);
    }

    #endregion

    #endregion

    #endregion

    #region Allow Multiple

    public bool GetWidgetAllowMultiple(WidgetProviderType widgetProvider, string widgetId, string widgetType)
    {
        if (widgetProvider == WidgetProviderType.DesktopWidgets3)
        {
            (var _, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
            return GetWidgetAllowMultiple(allIndex, installedIndex, widgetTypeIndex);
        }
        else
        {
            var definitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
            return GetWidgetAllowMultiple(definitionIndex);
        }
    }

    #region Desktop Widgets 3

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

    #endregion

    #region Microsoft

    private bool GetWidgetAllowMultiple(int definitionIndex)
    {
        if (definitionIndex != -1)
        {
            return _microsoftWidgetModel.WidgetDefinitions.ElementAt(definitionIndex).AllowMultiple;
        }

        return false;
    }

    #endregion

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

    private int GetWidgetProviderDefinitionIndex(string widgetId)
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

    private int GetWidgetDefinitionIndex(string widgetId, string widgetType)
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

    #region Add Widget Dialog

    public async Task<List<DashboardWidgetGroupItem>> GetInstalledDashboardGroupItems()
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
                    Name = GetWidgetGroupName(allIndex, installedIndex),
                    IconFill = await GetWidgetGroupIconBrushAsync(widgetId, allIndex, installedIndex),
                    Types = widget.Metadata.WidgetTypes
                });
            }
        }

        return dashboardGroupItemList;
    }

    #endregion

    #region Dashboard

    public async Task<List<DashboardWidgetItem>> GetYourDashboardWidgetItemsAsync(ElementTheme actualTheme)
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var dashboardItemList = new List<DashboardWidgetItem>();
        var unknownNotInstalledWidgetList = new List<(string, string)>();
        var widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

        foreach (var widget in widgetList)
        {
            var providerType = widget.ProviderType;
            var widgetId = widget.Id;
            var widgetType = widget.Type;
            var widgetIndex = widget.Index;

            if (providerType == WidgetProviderType.DesktopWidgets3)
            {
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
                        IconFill = await GetWidgetIconBrushAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme),
                        Pinned = widget.Pinned,
                        IsUnknown = false,
                        IsInstalled = true,
                        Editable = widget.Pinned || (!await widgetManagerService.IsWidgetSingleInstanceAndAlreadyPinnedAsync(providerType, widgetId, widgetType))
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
                        IconFill = await GetIconBrushFromPathAsync(Constants.UnknownWidgetIconPath),
                        Pinned = widget.Pinned,
                        IsUnknown = true,
                        IsInstalled = false,
                        Editable = false
                    });
                }
            }
            else
            {
                var widgetDefinitionIndex = GetWidgetDefinitionIndex(widgetId, widgetType);
                if (widgetDefinitionIndex != -1)
                {
                    dashboardItemList.Add(new DashboardWidgetItem()
                    {
                        ProviderType = providerType,
                        Id = widgetId,
                        Type = widgetType,
                        Index = widgetIndex,
                        Name = GetWidgetName(widgetDefinitionIndex),
                        IconFill = await GetWidgetIconBrushAsync(widgetDefinitionIndex, actualTheme),
                        Pinned = widget.Pinned,
                        IsUnknown = false,
                        IsInstalled = true,
                        Editable = widget.Pinned || (!await widgetManagerService.IsWidgetSingleInstanceAndAlreadyPinnedAsync(providerType, widgetId, widgetType))
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
                        IconFill = await GetIconBrushFromPathAsync(Constants.UnknownWidgetIconPath),
                        Pinned = widget.Pinned,
                        IsUnknown = true,
                        IsInstalled = false,
                        Editable = false
                    });
                }
            }
        }

        return dashboardItemList;
    }

    public async Task<DashboardWidgetItem?> GetDashboardWidgetItemAsync(string widgetId, string widgetType, int widgetIndex, ElementTheme actualTheme)
    {
        var providerType = WidgetProviderType.DesktopWidgets3;
        (var installed, var allIndex, var installedIndex, var widgetTypeIndex) = GetWidgetGroupAndWidgetTypeIndex(widgetId, widgetType, true);
        if (installed)
        {
            return new DashboardWidgetItem()
            {
                ProviderType = providerType,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Name = GetWidgetName(allIndex, installedIndex, widgetTypeIndex, widgetType),
                IconFill = await GetWidgetIconBrushAsync(widgetId, widgetType, allIndex, installedIndex, widgetTypeIndex, actualTheme),
                Pinned = true,
                IsUnknown = false,
                IsInstalled = true,
                Editable = true  // all widgets are pinned by default, so they are always editable
            };
        }

        return null;
    }

    public async Task<DashboardWidgetItem> GetDashboardWidgetItemAsync(string widgetId, string widgetType, int widgetIndex, WidgetViewModel widgetViewModel, ElementTheme actualTheme)
    {
        // get widget info
        var providerType = WidgetProviderType.Microsoft;
        var (_, widgetName, _, _, _) = widgetViewModel.GetWidgetProviderAndWidgetInfo();

        // get widget item
        return new DashboardWidgetItem()
        {
            ProviderType = providerType,
            Id = widgetId,
            Type = widgetType,
            Index = widgetIndex,
            Name = widgetName,
            IconFill = await GetWidgetIconBrushAsync(widgetViewModel.WidgetDefinition, actualTheme),
            Pinned = true,
            IsUnknown = false,
            IsInstalled = true,
            Editable = true  // all widgets are pinned by default, so they are always editable
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
                    ProviderType = WidgetProviderType.DesktopWidgets3,
                    Id = widgetId,
                    FamilyName = string.Empty,
                    Name = GetWidgetGroupName(allIndex, installedIndex),
                    Description = GetWidgetGroupDescription(allIndex, installedIndex),
                    Author = metadata.Author,
                    Version = metadata.Version,
                    Website = metadata.Website,
                    IconFill = await GetWidgetGroupIconBrushAsync(widgetId, allIndex, installedIndex)
                });
            }
        }

        foreach (var widgetProvider in _microsoftWidgetModel.WidgetProviderDefinitions)
        {
            if (WidgetHelpers.IsIncludedWidgetProvider(widgetProvider))
            {
                var familyName = widgetProvider.GetFamilyName();
                var extension = _microsoftWidgetModel.InstalledExtensions.FirstOrDefault(x => x.PackageFamilyName == familyName);
                if (extension != null)
                {
                    widgetStoreItemList.Add(await GetWidgetStoreItem(widgetProvider, extension));
                }
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
                        ProviderType = WidgetProviderType.DesktopWidgets3,
                        Id = metadata.ID,
                        FamilyName = string.Empty,
                        Name = GetWidgetGroupName(allIndex, installedIndex),
                        Description = GetWidgetGroupDescription(allIndex, installedIndex),
                        Author = metadata.Author,
                        Version = metadata.Version,
                        Website = metadata.Website,
                        IconFill = await GetWidgetGroupIconBrushAsync(widgetId, allIndex, installedIndex)
                    });
                }
            }
        }

        return widgetStoreItemList;
    }

    public async Task<WidgetStoreItem?> GetWidgetStoreItemAsync(IExtensionWrapper extension)
    {
        var widgetProvider = _microsoftWidgetModel.WidgetProviderDefinitions.FirstOrDefault(x => x.GetFamilyName() == extension.PackageFamilyName);
        if (widgetProvider != null)
        {
            return await GetWidgetStoreItem(widgetProvider, extension);
        }

        return null;
    }

    public async Task InstallWidgetAsync(string widgetId)
    {
        var metadata = AllWidgetGroupMetadatas.Find(x => x.ID == widgetId);
        var widgetStoreList = _appSettingsService.GetWidgetStoreList();
        if (metadata == null)
        {
            // TODO(Future): Install available widget from Github, not supported yet.
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
    }

    private async Task<WidgetStoreItem> GetWidgetStoreItem(WidgetProviderDefinition widgetProvider, IExtensionWrapper extension)
    {
        var version = extension.Version;
        var installedDate = extension.InstalledDate;
        var lastUpdatedLabel = "Last_Updated".GetLocalizedString(Constants.DevHomeDashboard);
        return new WidgetStoreItem()
        {
            ProviderType = WidgetProviderType.Microsoft,
            Id = widgetProvider.Id,
            FamilyName = extension.PackageFamilyName,
            Name = widgetProvider.DisplayName,
            Description = $"{lastUpdatedLabel} {installedDate.LocalDateTime}",
            Author = extension.Publisher,
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}",
            Website = $"https://apps.microsoft.com/home",  // We are unable to get the product id from the extension.
            IconFill = await GetWidgetGroupIconBrushAsync(widgetProvider)
        };
    }

    #endregion

    #region Path Icon

    private readonly ConcurrentDictionary<string, BitmapImage> _pathIconCache = new();

    private async Task<Brush> GetIconBrushFromPathAsync(string iconPath)
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
                bitmapImage = await BitmapImageHelper.ImagePathToBitmapImageAsync(_dispatcherQueue, iconPath);
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
            Stretch = Stretch.Uniform
        };

        return brush;
    }

    #endregion
}
