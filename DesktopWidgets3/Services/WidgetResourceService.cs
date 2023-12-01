using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;

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

    public WidgetSize GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new WidgetSize(300, 200),
            WidgetType.CPU => new WidgetSize(300, 200),
            WidgetType.Disk => new WidgetSize(300, 200),
            WidgetType.FolderView => new WidgetSize(500, 500),
            WidgetType.Network => new WidgetSize(300, 200),
            _ => new WidgetSize(300, 200),
        };
    }

    public double? GetDragZoneHeight(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => null,
        };
    }
}
