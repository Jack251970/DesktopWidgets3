﻿using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private static string ClassName => typeof(WidgetResourceService).Name;

    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private List<WidgetPair> AllWidgets { get; set; } = null!;
    private List<WidgetMetadata> AllWidgetsMetadata { get; set; } = null!;

    private static readonly string[] Directories =
    {
        Constant.WidgetsPreinstalledDirectory
    };

    #region Initialization

    public async Task Initalize()
    {
        // check preinstalled directory
        if (!Directory.Exists(Constant.WidgetsPreinstalledDirectory))
        {
            Directory.CreateDirectory(Constant.WidgetsPreinstalledDirectory);
        }

        // load all widgets
        AllWidgetsMetadata = WidgetsConfig.Parse(Directories);
        (AllWidgets, var errorWidgets) = WidgetsLoader.Widgets(AllWidgetsMetadata);

        // show error notification
        if (errorWidgets.Count > 0)
        {
            var errorWidgetString = string.Join(Environment.NewLine, errorWidgets);

            _ = Task.Run(() =>
            {
                App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationWidgetLoadErrorPayload".GetLocalized(),
                    $"{Environment.NewLine}{errorWidgetString}{Environment.NewLine}"));
            });
        }

        // install resource files
        InstallResourceFiles(AllWidgetsMetadata);

        // initialize all widgets
        await InitAllWidgetsAsync();
    }

    #region Xaml Resources

    private static void InstallResourceFiles(List<WidgetMetadata> widgetsMetadata)
    {
        foreach (var metadata in widgetsMetadata)
        {
            InstallResourceFiles(metadata);
        }
    }

    private static void InstallResourceFiles(WidgetMetadata metadata)
    {
        var widgetDirectory = metadata.WidgetDirectory;
        var xbfFiles = Directory.EnumerateFiles(widgetDirectory, "*.xbf", SearchOption.AllDirectories);
        var resourceFiles = xbfFiles;

        foreach (var resourceFile in resourceFiles)
        {
            var relativePath = Path.GetRelativePath(widgetDirectory, resourceFile);
            // TODO: Initialize AppContext.BaseDirector in Constants.
            var destinationPath = Path.Combine(AppContext.BaseDirectory, relativePath);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!(string.IsNullOrEmpty(destinationDirectory) || Directory.Exists(destinationDirectory)))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Copy(resourceFile, destinationPath, true);
        }
    }

    #endregion

    #region IWidget

    private async Task InitAllWidgetsAsync()
    {
        var publicAPIService = App.GetService<IPublicAPIService>();

        var failedPlugins = new ConcurrentQueue<WidgetPair>();

        var InitTasks = AllWidgets.Select(pair => Task.Run(delegate
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

        await Task.WhenAll(InitTasks);

        if (!failedPlugins.IsEmpty)
        {
            var failedWidgetString = string.Join(Environment.NewLine, failedPlugins.Select(x => x.Metadata.Name));

            _ = Task.Run(() =>
            {
                App.GetService<IAppNotificationService>().Show(
                    string.Format("AppNotificationWidgetInitializeErrorPayload".GetLocalized(),
                    $"{Environment.NewLine}{failedWidgetString}{Environment.NewLine}"));
            });
        }
    }

    #endregion

    #endregion

    #region Dispose

    public async Task DisposeWidgetsAsync()
    {
        foreach (var widgetPair in AllWidgets)
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

    public FrameworkElement GetWidgetFrameworkElement(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                try
                {
                    return widget.Widget.CreateWidgetFrameworkElement();
                }
                catch (Exception e)
                {
                    LogExtensions.LogError(ClassName, e, $"Error creating widget framework element for widget {widget.Metadata.ID}");
                }
            }
        }

        return new UserControl();
    }

    public async Task EnvokeEnableWidgetAsync(string widgetId, bool firstWidget)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                await widget.Widget.EnableWidgetAsync(firstWidget);
            }
        }

        await Task.CompletedTask;
    }

    public async Task EnvokeDisableWidgetAsync(string widgetId, bool lastWidget)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                await widget.Widget.DisableWidgetAsync(lastWidget);
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                if (widget.Widget is IWidgetSetting widgetSetting)
                {
                    return widgetSetting.GetDefaultSetting();
                }
            }
        }

        return new BaseWidgetSettings();
    }

    public FrameworkElement GetWidgetSettingFrameworkElement(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
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
        }

        return new UserControl();
    }

    #endregion

    #region Metadata

    public RectSize GetDefaultSize(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return new RectSize(widget.Metadata.DefaultWidth, widget.Metadata.DefaultHeight);
            }
        }

        return new RectSize(318, 200);
    }

    public RectSize GetMinSize(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return new RectSize(widget.Metadata.MinWidth, widget.Metadata.MinHeight);
            }
        }

        return new RectSize(318, 200);
    }

    public bool GetWidgetInNewThread(string widgetId)
    {
        if (!_appSettingsService.MultiThread)
        {
            return false;
        }

        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return widget.Metadata.InNewThread;
            }
        }

        return false;
    }

    #endregion

    #region Dashboard

    public List<DashboardWidgetItem> GetAllDashboardItems()
    {
        List<DashboardWidgetItem> dashboardItemList = [];

        foreach (var widget in AllWidgets)
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                IsUnknown = false,
                Id = widget.Metadata.ID,
                IndexTag = 0,
                Name = widget.Metadata.Name,
                IcoPath = widget.Metadata.IcoPath,
            });
        }

        return dashboardItemList;
    }

    public async Task<List<DashboardWidgetItem>> GetYourDashboardItemsAsync()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var unknownWidgetIdList = new List<string>();

        List<DashboardWidgetItem> dashboardItemList = [];
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

    private static DashboardWidgetItem GetUnknownDashboardItem(string widgetId, int indexTag, bool isEnabled, int widgetIndex)
    {
        return new DashboardWidgetItem()
        {
            IsUnknown = true,
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = isEnabled,
            Name = string.Format("Unknown_Widget_Name".GetLocalized(), widgetIndex),
            IcoPath = Constant.UnknownWidgetIcoPath,
        };
    }

    private string GetWidgetName(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return widget.Metadata.Name;
            }
        }

        return string.Format("Unknown_Widget_Name".GetLocalized(), 1);
    }

    private string GetWidgetIcoPath(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return widget.Metadata.IcoPath;
            }
        }

        return Constant.UnknownWidgetIcoPath;
    }

    #endregion

    #region Widget Store

    public List<WidgetStoreItem> GetInstalledWidgetStoreItems()
    {
        List<WidgetStoreItem> widgetStoreItemList = [];
        foreach (var metaData in AllWidgetsMetadata)
        {
            widgetStoreItemList.Add(new WidgetStoreItem()
            {
                Id = metaData.ID,
                Name = metaData.Name,
                Description = metaData.Description,
                Author = metaData.Author,
                Version = metaData.Version,
                Website = metaData.Website,
                IcoPath = metaData.IcoPath,
                IsPreinstalled = true,  // TODO
                IsInstalled = true,  // TODO
            });
        }

        return widgetStoreItemList;
    }

    #endregion
}
