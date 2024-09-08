namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    string GetWidgetLabel(string widgetId);

    string GetWidgetIconSource(string widgetId);

    RectSize GetMinSize(string widgetId);

    bool GetWidgetInNewThread(string widgetId);
}
