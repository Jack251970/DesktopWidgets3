using System.Collections.Concurrent;
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
        // load all widgets
        AllWidgetsMetadata = WidgetsConfig.Parse(Directories);
        AllWidgets = WidgetsLoader.Widgets(AllWidgetsMetadata);

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
            var failedWidget = string.Join(",", failedPlugins.Select(x => x.Metadata.Name));
            _ = Task.Run(() =>
            {
                App.GetService<IAppNotificationService>().Show(
                    string.Format("AppNotificationWidgetInitializeErrorPayload".GetLocalized(),
                    failedWidget));
            });
        }
    }

    #endregion

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
                Id = widget.Metadata.ID,
                IndexTag = 0,
                Label = widget.Metadata.Name,
                IcoPath = widget.Metadata.IcoPath,
            });
        }

        return dashboardItemList;
    }

    public async Task<List<DashboardWidgetItem>> GetYourDashboardItemsAsync()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        List<DashboardWidgetItem> dashboardItemList = [];
        foreach (var widget in widgetList)
        {
            var widgetId = widget.Id;
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Id = widgetId,
                IndexTag = widget.IndexTag,
                Label = GetWidgetName(widgetId),
                IsEnabled = widget.IsEnabled,
                IcoPath = GetWidgetIcoPath(widgetId),
            });
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag)
    {
        return new DashboardWidgetItem()
        {
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = true,
            Label = GetWidgetName(widgetId),
            IcoPath = GetWidgetIcoPath(widgetId),
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

        return string.Empty;
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

        return string.Empty;
    }

    #endregion
}
