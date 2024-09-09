using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private List<WidgetPair> AllWidgets { get; set; } = null!;

    private List<WidgetMetadata> AllWidgetsMetadata { get; set; } = null!;

    private static readonly string[] Directories =
    {
        Constant.WidgetsPreinstalledDirectory
    };

    public void Initalize()
    {
        AllWidgetsMetadata = WidgetsConfig.Parse(Directories);
        AllWidgets = WidgetsLoader.Widgets(AllWidgetsMetadata);
        InstallResourceFiles(AllWidgetsMetadata);
    }

    #region Xaml Resources Management

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

    public FrameworkElement GetWidgetFrameworkElement(string widgetId)
    {
        foreach (var widget in AllWidgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return widget.Widget.CreateWidgetPage();
            }
        }

        return null!;
    }

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
}
