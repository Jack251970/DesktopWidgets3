using DesktopWidgets3.Models;
using Windows.Foundation;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType);

    public string GetWidgetIconSource(WidgetType widgetType);

    public Size GetDefaultSize(WidgetType widgetType);
}
