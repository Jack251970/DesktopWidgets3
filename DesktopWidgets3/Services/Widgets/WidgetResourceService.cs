using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    private static List<WidgetPair> Widgets => WidgetsManager.AllWidgets;

    public void Initalize()
    {
        WidgetsManager.LoadWidgets();
    }

    public FrameworkElement GetWidgetFrameworkElement(string widgetId)
    {
        foreach (var widget in Widgets)
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

        foreach (var widget in Widgets)
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
        foreach (var widget in Widgets)
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
        foreach (var widget in Widgets)
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
        foreach (var widget in Widgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return new RectSize(widget.Metadata.DefaultWidth, widget.Metadata.DefaultHeight);
            }
        }

        return new RectSize(240, 240);
    }

    public RectSize GetMinSize(string widgetId)
    {
        foreach (var widget in Widgets)
        {
            if (widget.Metadata.ID == widgetId)
            {
                return new RectSize(widget.Metadata.MinWidth, widget.Metadata.MinHeight);
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
            if (widget.Metadata.ID == widgetId)
            {
                return widget.Metadata.InNewThread;
            }
        }

        return false;
    }
}
