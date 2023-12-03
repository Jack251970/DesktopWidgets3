using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetResourceService
{
    string GetWidgetLabel(WidgetType widgetType);

    string GetWidgetIconSource(WidgetType widgetType);

    WidgetSize GetDefaultSize(WidgetType widgetType);

    WidgetSize GetMinSize(WidgetType widgetType);
}
