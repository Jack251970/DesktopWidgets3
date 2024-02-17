namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    string GetWidgetLabel(WidgetType widgetType);

    string GetWidgetIconSource(WidgetType widgetType);

    WidgetSize GetDefaultSize(WidgetType widgetType);

    WidgetSize GetMinSize(WidgetType widgetType);

    BaseWidgetSettings GetDefaultSettings(WidgetType widgetType);
}
