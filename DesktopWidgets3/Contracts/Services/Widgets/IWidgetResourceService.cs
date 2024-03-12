namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    string GetWidgetLabel(WidgetType widgetType);

    string GetWidgetIconSource(WidgetType widgetType);

    RectSize GetMinSize(WidgetType widgetType);

    bool GetWidgetInNewThread(WidgetType widgetType);
}
