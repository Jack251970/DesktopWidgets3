namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    string GetWidgetLabel(WidgetType widgetType);

    string GetWidgetIconSource(WidgetType widgetType);

    RectSize GetDefaultSize(WidgetType widgetType);

    RectSize GetMinSize(WidgetType widgetType);

    BaseWidgetSettings GetDefaultSettings(WidgetType widgetType);

    bool GetWidgetInNewThread(WidgetType widgetType);
}
