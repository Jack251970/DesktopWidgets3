using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;
using Windows.Foundation;

namespace DesktopWidgets3.Services;

public class WidgetResourceService : IWidgetResourceService
{
    public Size GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new Size(300, 200),
            WidgetType.Folder => new Size(500, 500),
            WidgetType.CPU => new Size(300, 200),
            WidgetType.Disk => new Size(300, 200),
            WidgetType.Network => new Size(300, 200),
            _ => new Size(300, 200),
        };
    }
}
