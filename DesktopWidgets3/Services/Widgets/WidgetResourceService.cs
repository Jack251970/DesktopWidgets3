using DesktopWidget3.Clock;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private readonly Dictionary<WidgetMetadata, IAsyncWidget> Widgets = [];

    public void Initalize()
    {
        var metaData = new WidgetMetadata
        {
            ID = "0ECADE17459B49F587BF81DC3A125110",
            Name = "Digital Clock",
            IcoPath = "ms-appx:///Assets/Icons/Clock.png",
            DefaultHeight = 240,
            DefaultWidth = 240,
            MinHeight = 240,
            MinWidth = 240,
            InNewThread = true,
        };
        Widgets.Add(metaData, new Main());
    }

    public FrameworkElement GetWidgetFrameworkElement(string widgetId)
    {
        foreach (var widgetItem in Widgets)
        {
            if (widgetItem.Key.ID == widgetId)
            {
                var widget = widgetItem.Value;
                return widget.CreateWidgetPage();
            }
        }

        return null!;
    }

    public List<DashboardWidgetItem> GetAllWidgetItems()
    {
        List<DashboardWidgetItem> dashboardItemList = [];

        foreach (var widget in Widgets)
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Id = widget.Key.ID,
                IndexTag = 0,
                Label = widget.Key.Name,
                Icon = widget.Key.IcoPath,
            });
        }

        return dashboardItemList;
    }

    public string GetWidgetLabel(string widgetId)
    {
        foreach (var widget in Widgets)
        {
            if (widget.Key.ID == widgetId)
            {
                return widget.Key.Name;
            }
        }
        return string.Empty;
    }

    public string GetWidgetIconSource(string widgetId)
    {
        foreach (var widget in Widgets)
        {
            if (widget.Key.ID == widgetId)
            {
                return widget.Key.IcoPath;
            }
        }
        return string.Empty;
    }

    public RectSize GetDefaultSize(string widgetId)
    {
        foreach (var widget in Widgets)
        {
            if (widget.Key.ID == widgetId)
            {
                return new RectSize(widget.Key.DefaultWidth, widget.Key.DefaultHeight);
            }
        }
        return new RectSize(240, 240);
    }

    public RectSize GetMinSize(string widgetId)
    {
        foreach (var widget in Widgets)
        {
            if (widget.Key.ID == widgetId)
            {
                return new RectSize(widget.Key.MinWidth, widget.Key.MinHeight);
            }
        }
        return new RectSize(240, 240);
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

        foreach (var widget in Widgets)
        {
            if (widget.Key.ID == widgetId)
            {
                return widget.Key.InNewThread;
            }
        }
        return false;
    }
}
