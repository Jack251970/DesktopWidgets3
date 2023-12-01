using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType);

    public string GetWidgetIconSource(WidgetType widgetType);

    public WidgetSize GetDefaultSize(WidgetType widgetType);
}
