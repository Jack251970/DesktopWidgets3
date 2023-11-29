using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using Windows.Foundation;

namespace DesktopWidgets3.Services;

public class WidgetResourceService : IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"Widget{widgetType}Label".GetLocalized(),
        };
    }

    public string GetWidgetIconSource(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"ms-appx:///Assets/FluentIcons/{widgetType}.png"
        };
    }

    public Size GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new Size(300, 200),
            WidgetType.CPU => new Size(300, 200),
            WidgetType.Disk => new Size(300, 200),
            WidgetType.FolderView => new Size(500, 500),
            WidgetType.Network => new Size(300, 200),
            _ => new Size(300, 200),
        };
    }

    
}
