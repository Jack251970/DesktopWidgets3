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

    public void Initalize()
    {
        AllWidgetsMetadata = WidgetsConfig.Parse(Directories);
        AllWidgets = WidgetsLoader.Widgets(AllWidgetsMetadata);
        InstallResourceFiles(AllWidgetsMetadata);
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
                    return widget.Widget.CreateWidgetPage();
                }
                catch (Exception e)
                {
                    LogExtensions.LogError(ClassName, e, $"Error creating widget {widget.Metadata.ID}");
                }
            }
        }

        return new UserControl();
    }

    #endregion

    #region Metadata

    public string GetWidgetLabel(string widgetId)
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

    public string GetWidgetIconSource(string widgetId)
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

    public BaseWidgetSettings GetDefaultSetting(string widgetId)
    {
        // TODO: Interface.
        return new BaseWidgetSettings();
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

    public List<DashboardWidgetItem> GetAllWidgetItems()
    {
        List<DashboardWidgetItem> dashboardItemList = [];

        foreach (var widget in AllWidgets)
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Id = widget.Metadata.ID,
                IndexTag = 0,
                Label = widget.Metadata.Name,
                Icon = widget.Metadata.IcoPath,
            });
        }

        return dashboardItemList;
    }

    public async Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync()
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
                Label = GetWidgetLabel(widgetId),
                IsEnabled = widget.IsEnabled,
                Icon = GetWidgetIconSource(widgetId),
            });
        }

        return dashboardItemList;
    }

    #endregion
}
