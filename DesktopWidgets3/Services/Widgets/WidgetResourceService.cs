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

    private static readonly string[] WidgetsDirectories = [];  // TODO: Add user widget directory.

    #region Initialization

    public async Task InitalizeAsync()
    {
        // get all widget metadata
        AllWidgetsMetadata = WidgetsConfig.Parse(WidgetsDirectories, Constant.WidgetsPreinstalledDirectory);

        // check preinstalled widgets
        await CheckPreinstalledWidgets();

        // load all installed widgets
        var installedWidgetsMetadata = AllWidgetsMetadata.Where(x => x.Installed).ToList();
        (InstalledWidgets, var errorWidgets) = WidgetsLoader.Widgets(installedWidgetsMetadata);

        // show error notification
        if (errorWidgets.Count > 0)
        {
            var errorWidgetString = string.Join(Environment.NewLine, errorWidgets);

            _ = Task.Run(() =>
            {
                DependencyExtensions.GetRequiredService<IAppNotificationService>().Show(
                    string.Format("AppNotificationWidgetLoadErrorPayload".GetLocalized(),
                    $"{Environment.NewLine}{errorWidgetString}{Environment.NewLine}"));
            });
        }

        // initialize all widgets
        await InitWidgetsAsync();

        // initialize widget list
        await _appSettingsService.InitializeWidgetListAsync();
    }

    private async Task CheckPreinstalledWidgets()
    {
        // load widget store list
        var widgetStoreList = await _appSettingsService.InitializeWidgetStoreListAsync();

        // get preinstalled widget metadata
        var preinstalledWidgetsMetadata = AllWidgetsMetadata.Where(x => x.Preinstalled).ToList();

        // check all preinstalled widgets
        var needSave = false;
        foreach (var metadata in preinstalledWidgetsMetadata)
        {
            var index = widgetStoreList.FindIndex(x => x.Id == metadata.ID && x.IsPreinstalled);
            if (index == -1)
            {
                // install new preinstalled widgets
                var resourcesFile = InstallResourceFiles(metadata);
                widgetStoreList.Add(new JsonWidgetStoreItem()
                {
                    Id = metadata.ID,
                    Version = metadata.Version,
                    IsPreinstalled = true,
                    IsInstalled = true,
                    ResourcesFile = resourcesFile,
                });
                needSave = true;
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
                        UninstallResourceFiles(widgetStoreItem.ResourcesFile);
                        var resourcesFile = InstallResourceFiles(metadata);
                        widgetStoreList[index].Version = metadata.Version;
                        widgetStoreList[index].ResourcesFile = resourcesFile;
                        needSave = true;
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

        // save widget store list
        if (needSave)
        {
            await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
        }
    }

    #endregion

    #region Xaml Resources

    private static List<string> InstallResourceFiles(WidgetMetadata metadata)
    {
        var widgetDirectory = metadata.WidgetDirectory;
        var xbfFiles = Directory.EnumerateFiles(widgetDirectory, "*.xbf", SearchOption.AllDirectories);
        var resourceFiles = xbfFiles;
        var destinationResources = new List<string>();

        foreach (var resourceFile in resourceFiles)
        {
            var relativePath = Path.GetRelativePath(widgetDirectory, resourceFile);
            var destinationPath = Path.Combine(AppContext.BaseDirectory, relativePath);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!(string.IsNullOrEmpty(destinationDirectory) || Directory.Exists(destinationDirectory)))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Copy(resourceFile, destinationPath, true);

            destinationResources.Add(destinationPath);
        }

        return destinationResources;
    }

    private static void UninstallResourceFiles(List<string> resourcesFiles)
    {
        foreach (var resourceFile in resourcesFiles)
        {
            if (resourceFile != null && File.Exists(resourceFile))
            {
                File.Delete(resourceFile);
                DeleteEmptyDirectory(resourceFile);
            }
        }
    }

    private static void DeleteEmptyDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (directory == AppContext.BaseDirectory)
        {
            return;
        }
        if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
        {
            Directory.Delete(directory);
            DeleteEmptyDirectory(directory);
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
        var publicAPIService = DependencyExtensions.GetRequiredService<IPublicAPIService>();

        var failedPlugins = new ConcurrentQueue<WidgetPair>();

        var initTasks = InstalledWidgets.Select(pair => Task.Run(delegate
        {
            try
            {
                pair.Widget.InitWidgetAsync(new WidgetInitContext(pair.Metadata, publicAPIService));
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

            _ = Task.Run(() =>
            {
                DependencyExtensions.GetRequiredService<IAppNotificationService>().Show(
                    string.Format("AppNotificationWidgetInitializeErrorPayload".GetLocalized(),
                    $"{Environment.NewLine}{failedWidgetString}{Environment.NewLine}"));
            });
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
                return widget.Widget.CreateWidgetFrameworkElement();
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
                    return widgetSetting.CreateWidgetSettingFrameworkElement();
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

    #region Metadata

    public string GetWidgetName(string widgetId)
    {
        var index = AllWidgetsMetadata.FindIndex(x => x.ID == widgetId);
        if (index != -1)
        {
            return AllWidgetsMetadata[index].Name;
        }

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
    }

    private string GetWidgetIcoPath(string widgetId)
    {
        var index = AllWidgetsMetadata.FindIndex(x => x.ID == widgetId);
        if (index != -1)
        {
            return AllWidgetsMetadata[index].IcoPath;
        }

        return Constant.UnknownWidgetIcoPath;
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

    public RectSize GetMinSize(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            return new(widget.Metadata.MinWidth, widget.Metadata.MinHeight);
        }

        return new(null, null);
    }

    public RectSize GetMaxSize(string widgetId)
    {
        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            return new(widget.Metadata.MaxWidth, widget.Metadata.MaxHeight);
        }

        return new(null, null);
    }

    public bool GetWidgetInNewThread(string widgetId)
    {
        if (!_appSettingsService.MultiThread)
        {
            return false;
        }

        var index = InstalledWidgets.FindIndex(x => x.Metadata.ID == widgetId);
        if (index != -1)
        {
            var widget = InstalledWidgets[index];
            return widget.Metadata.InNewThread;
        }

        return false;
    }

    private bool IsInstalled(string widgetId)
    {
        var index = AllWidgetsMetadata.FindIndex(x => x.ID == widgetId);
        if (index != -1)
        {
            return AllWidgetsMetadata[index].Installed;
        }

        return false;
    }

    #endregion

    #region Dashboard

    public List<DashboardWidgetItem> GetAllDashboardItems()
    {
        var dashboardItemList = new List<DashboardWidgetItem>();

        foreach (var widget in InstalledWidgets)
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                IsUnknown = false,
                IsInstalled = true,
                Id = widget.Metadata.ID,
                IndexTag = 0,
                Name = widget.Metadata.Name,
                IcoPath = widget.Metadata.IcoPath,
            });
        }

        return dashboardItemList;
    }

    public List<DashboardWidgetItem> GetYourDashboardItemsAsync()
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
                dashboardItemList.Add(GetUnknownDashboardItem(widgetId, indexTag, widget.IsEnabled, unknownWidgetIdList.Count));
            }
            else
            {
                dashboardItemList.Add(new DashboardWidgetItem()
                {
                    IsUnknown = false,
                    IsInstalled = IsInstalled(widgetId),
                    Id = widgetId,
                    IndexTag = indexTag,
                    Name = GetWidgetName(widgetId),
                    IsEnabled = widget.IsEnabled,
                    IcoPath = GetWidgetIcoPath(widgetId),
                });
            }
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag)
    {
        return new DashboardWidgetItem()
        {
            IsUnknown = false,
            IsInstalled = IsInstalled(widgetId),
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = true,
            Name = GetWidgetName(widgetId),
            IcoPath = GetWidgetIcoPath(widgetId),
        };
    }

    public bool IsWidgetUnknown(string widgetId)
    {
        return !AllWidgetsMetadata.Any(x => x.ID == widgetId);
    }

    private DashboardWidgetItem GetUnknownDashboardItem(string widgetId, int indexTag, bool isEnabled, int widgetIndex)
    {
        return new DashboardWidgetItem()
        {
            IsUnknown = true,
            IsInstalled = IsInstalled(widgetId),
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = isEnabled,
            Name = string.Format("Unknown_Widget_Name".GetLocalized(), widgetIndex),
            IcoPath = Constant.UnknownWidgetIcoPath,
        };
    }

    #endregion

    #region Widget Store

    public List<WidgetStoreItem> GetInstalledWidgetStoreItems()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metaData in AllWidgetsMetadata)
        {
            if (metaData.Installed)
            {
                widgetStoreItemList.Add(new WidgetStoreItem()
                {
                    Id = metaData.ID,
                    Name = metaData.Name,
                    Description = metaData.Description,
                    Author = metaData.Author,
                    Version = metaData.Version,
                    Website = metaData.Website,
                    IcoPath = metaData.IcoPath
                });
            }
        }

        return widgetStoreItemList;
    }

    public List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];

        foreach (var metaData in AllWidgetsMetadata)
        {
            if (metaData.Preinstalled && (!metaData.Installed))
            {
                widgetStoreItemList.Add(new WidgetStoreItem()
                {
                    Id = metaData.ID,
                    Name = metaData.Name,
                    Description = metaData.Description,
                    Author = metaData.Author,
                    Version = metaData.Version,
                    Website = metaData.Website,
                    IcoPath = metaData.IcoPath
                });
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
            // TODO: Install widget from Github, not supported yet.
        }
        else
        {
            // install widget from preinstalled widgets
            var index = widgetStoreList.FindIndex(x => x.Id == widgetId);
            if (index == -1)
            {
                // install new preinstalled widgets
                var resourcesFile = InstallResourceFiles(metadata);
                widgetStoreList.Add(new JsonWidgetStoreItem()
                {
                    Id = metadata.ID,
                    Version = metadata.Version,
                    IsPreinstalled = metadata.Preinstalled,
                    IsInstalled = true,
                    ResourcesFile = resourcesFile,
                });
            }
            else
            {
                var widgetStoreItem = widgetStoreList[index];
                if (widgetStoreItem.IsInstalled)
                {
                    if (widgetStoreItem.Version == metadata.Version)
                    {
                        // already installed and up to date
                        return;
                    }
                    // already installed and need to uninstall
                    UninstallResourceFiles(widgetStoreItem.ResourcesFile);
                }
                else
                {
                    widgetStoreList[index].IsInstalled = true;
                }
                var resourcesFile = InstallResourceFiles(metadata);
                widgetStoreList[index].ResourcesFile = resourcesFile;
            }

            await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
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
            UninstallResourceFiles(widgetStoreItem.ResourcesFile);
            widgetStoreList[index].IsInstalled = false;
            widgetStoreList[index].ResourcesFile = [];
            await _appSettingsService.SaveWidgetStoreListAsync(widgetStoreList);
        }

        App.RestartApplication();
    }

    #endregion
}
